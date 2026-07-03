using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Helpers;
using SurviveTheHuntClient.Plugins.Cupid.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers
{
    internal class CopSpawnController : ITickable, INetEntityListener
    {
        private bool _enabled = false;

        internal bool Enabled
        {
            get => _enabled;
            set
            {
                bool prev = _enabled;
                _enabled = value;
                if(prev != value)
                {
                    OnEnabledChanged(prev, value);
                }
            }
        }

        private int? _camera, _camera1, _camera2 = null;

        private SelectedSpawn _selectedSpawn = new SelectedSpawn(Constants.Location.CopSpawn.FromIndex(0), 0);
        private struct SelectedSpawn
        {
            internal readonly byte Index;
            internal CopSpawnInfo SpawnInfo;
            internal SelectedSpawn(CopSpawnInfo spawnInfo, byte index)
            {
                Index = index;
                SpawnInfo = spawnInfo;
            }
        }

        private readonly AVControllerHelper AVControllerHelper;
        private readonly TriggerServerEventProxyDelegate TriggerServerEvent;

        private const string ServiceVehicleBlipTextKey = "STH_CUPID_COP_CAR_BLIP";
        private const string ServiceVehicleBlipTextContent = "Service Vehicle";

        private static readonly bool s_InitDone = Init();

        private static bool Init()
        {
            if(!s_InitDone)
            {
                AddTextEntry(ServiceVehicleBlipTextKey, ServiceVehicleBlipTextContent);
            }

            return true;
        }

        internal CopSpawnController(AVControllerHelper avControllerHelper, TriggerServerEventProxyDelegate triggerServerEvent)
        {
            AVControllerHelper = avControllerHelper;
            TriggerServerEvent = triggerServerEvent;
        }

        private void EnsureCamera()
        {
            if(!_camera1.HasValue)
            {
                _camera1 = CreateCopSpawnCam();
                _camera2 = CreateCopSpawnCam();

                SwitchCams();
            }
        }

        private void SwitchCams()
        {
            int? prevCam = null;

            if (_camera1.HasValue)
            {
                if (!_camera.HasValue)
                {
                    _camera = _camera1.Value;
                }
                else
                {
                    prevCam = _camera.Value;

                    if (_camera.Value == _camera1.Value)
                    {
                        _camera = _camera2.Value;
                    } else
                    {
                        _camera = _camera1.Value;
                    }
                }
            }

            if(_camera.HasValue)
            {
                if (prevCam.HasValue)
                {
                    SetCamActiveWithInterp(_camera.Value, prevCam.Value, CamTransitionTime, 1, 1);
                }
                else
                {
                    SetCamActive(_camera.Value, _enabled);
                }
            }
        }

        private static int CreateCopSpawnCam()
        {
            int cam = CreateCam("DEFAULT_SCRIPTED_CAMERA", false);
            return cam;
        }

        internal const int CamTransitionTime = 1500;

        private void OnEnabledChanged(bool prev, bool current)
        {
            if(current)
            {
                _timeTillTransitionEnd = float.MinValue;
                SelectNearestSpawn();

                // Remove the current car if we have one, as we're likely respawning
                if(_currentCar.HasValue)
                {
                    if(NetworkDoesNetworkIdExist(_currentCar.Value.NetId))
                    {
                        if(DoesBlipExist(_currentCar.Value.Blip))
                        {
                            int blip = _currentCar.Value.Blip;
                            RemoveBlip(ref blip);
                        }

                        if(NetworkDoesEntityExistWithNetworkId(_currentCar.Value.NetId))
                        {
                            int entity = NetToVeh(_currentCar.Value.NetId);
                            if(DoesEntityExist(entity))
                            {
                                SetEntityAsMissionEntity(entity, false, false);
                                SetEntityAsNoLongerNeeded(ref entity);
                            }
                        }
                    }

                    string station = _currentCar.Value.StationName;
                    byte slot = _currentCar.Value.Slot;
                    bool needsToFreeOnServer = !_currentCar.Value.HasLeftSpawn;
                    _currentCar = null;

                    if (needsToFreeOnServer)
                    {
                        TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidCopCarLeftSpawn, station, slot);
                    }
                }
            }

            if(_camera.HasValue)
            {
                SetCamActive(_camera.Value, current);
                SetCamCoord(_camera.Value, _selectedSpawn.SpawnInfo.Camera.Pos.X, _selectedSpawn.SpawnInfo.Camera.Pos.Y, _selectedSpawn.SpawnInfo.Camera.Pos.Z);
                SetCamRot(_camera.Value, _selectedSpawn.SpawnInfo.Camera.Rot.X, _selectedSpawn.SpawnInfo.Camera.Rot.Y, _selectedSpawn.SpawnInfo.Camera.Rot.Z, 2);
                RenderScriptCams(current, !current, current ? 0 : CamTransitionTime, !current, false);
            }

            if(!current)
            {
                AVControllerHelper.CurrentStation = null;
            }
        }

        private void SelectNearestSpawn()
        {
            CopSpawnInfo nearest = Constants.Location.CopSpawn.FindNearest(GetEntityCoords(PlayerPedId(), false));
            sbyte index = Constants.Location.CopSpawn.FindIndex(nearest);
            if(index == -1)
            {
                throw new Exception($"{nameof(SelectNearestSpawn)} failed because an index for {nameof(nearest)} cannot be found in {nameof(Constants.Location.CopSpawn)}");
            }

            CopSpawnInfo old = _selectedSpawn.SpawnInfo;
            _selectedSpawn = new SelectedSpawn(nearest, (byte)index);

            if(old != nearest)
            {
                OnSelectedSpawnChanged(old, nearest);
            }
        }

        private int? _playerCopCar = null;

        private void CycleSelectedSpawn(bool up)
        {
            Debug.WriteLine($"Cycling selected spawn {nameof(up)}: {up}");

            int nextIndex = (_selectedSpawn.Index + Constants.Location.CopSpawn.Count + (up ? -1 : 1)) % Constants.Location.CopSpawn.Count;
            CopSpawnInfo old = _selectedSpawn.SpawnInfo;
            _selectedSpawn = new SelectedSpawn(Constants.Location.CopSpawn.FromIndex((byte)nextIndex), (byte)nextIndex);
            OnSelectedSpawnChanged(old, _selectedSpawn.SpawnInfo);
        }

        private bool GetCurrentCamera(out int cam)
        {
            if(!_camera.HasValue)
            {
                cam = default;
                return false;
            }

            cam = _camera.Value;
            return true;
        }

        private bool GetOtherCamera(out int cam)
        {
            if(!_camera.HasValue)
            {
                cam = default;
                return false;
            }

            cam = _camera.Value == _camera1.Value ? _camera2.Value : _camera1.Value;
            return true;
        }

        private void OnSelectedSpawnChanged(CopSpawnInfo prev, CopSpawnInfo current)
        {
            if(GetOtherCamera(out int newCam))
            {
                SetCamCoord(newCam, current.Camera.Pos.X, current.Camera.Pos.Y, current.Camera.Pos.Z);
                SetCamRot(newCam, current.Camera.Rot.X, current.Camera.Rot.Y, current.Camera.Rot.Z, 2);
                SwitchCams();

                // TODO: get screen space pos of spawn and send to av controller
            }

            if(_camera.HasValue)
            {
                AVControllerHelper.CurrentStation = current.Name;
            }
        }

        internal const float CamTransitionTimeSeconds = (1f * CamTransitionTime) / 1000f;

        private void OnTransitionEnded()
        {
            Debug.WriteLine("Camera transition ended");

            // Finished spawning
            if(_isSpawning)
            {
                _isSpawning = false;
                FreezeEntityPosition(PlayerPedId(), false);
                Enabled = false;
            }
        }

        private bool _isSpawning = false;

        internal const float CarSpawnSafeRadius = 10f;
        private const float CarSpawnSafeRadiusSq = CarSpawnSafeRadius * CarSpawnSafeRadius;

        private struct SpawnedCar
        {
            internal int Blip;
            internal int NetId;
            internal uint Model;
            // TODO: when this is set, the slot needs to be freed on the server
            internal bool HasLeftSpawn;
            internal Vector3 InitialPos;
            internal byte Slot;
            internal string StationName;
        }

        private struct CarSpawnRequest
        {
            internal Vector3 Pos;
            internal float Heading;
            internal uint Model;
            internal byte Slot;
            internal string StationName;
        }

        private SpawnedCar? _currentCar = null;
        private CarSpawnRequest? _currentCarSpawnRequest = null;
        private void ConfirmSelectedSpawn()
        {
            _isSpawning = true;
            _timeTillTransitionEnd = CamTransitionTimeSeconds;

            // Ask server for a car spawn
            Debug.WriteLine($"Asking for a car spawn at {_selectedSpawn.SpawnInfo.Name} in one of the {_selectedSpawn.SpawnInfo.CarLayout.Count} slots");
            TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidCopSpawning, _selectedSpawn.SpawnInfo.Name, _selectedSpawn.SpawnInfo.CarLayout.Count);
            /*
            Vector3 carPos = default;
            float carHeading = default;
            byte? carSlotIndex = GetSpawnPosForCar(_selectedSpawn.SpawnInfo, out carPos, out carHeading);

            if(carSlotIndex.HasValue)
            {
                TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent, Constants.SpecialEvent.BlockCopCarSpawn, _selectedSpawn.SpawnInfo.Name, carSlotIndex);
            }*/
        }

        private static Vector3 GetSpawnPosForCar(CopSpawnInfo.CarPosInfo carLayout, byte slot, out float heading)
        {
            heading = carLayout.Heading;
            return new Vector3(carLayout.Origin.X + carLayout.Step.X * slot, carLayout.Origin.Y + carLayout.Step.Y * slot, carLayout.Origin.Z);
        }

        private float _timeTillTransitionEnd = float.MinValue;
        private float _timeSinceVehicleCheck = 0f;
        private const float VehicleCheckIntervalSeconds = 1f;
        public void Tick(float deltaTime)
        {
            EnsureCamera();

            if (_timeTillTransitionEnd > 0f) 
            {
                float prevTimeTillTransitionEnd = _timeTillTransitionEnd;
                _timeTillTransitionEnd -= deltaTime;
                if (_timeTillTransitionEnd <= 0f && HasCollisionLoadedAroundEntity(PlayerPedId()))
                {
                    OnTransitionEnded();
                }
            }

            if(_enabled && !_isSpawning)
            {
                SetFocusArea(_selectedSpawn.SpawnInfo.Camera.Pos.X, _selectedSpawn.SpawnInfo.Camera.Pos.Y, _selectedSpawn.SpawnInfo.Camera.Pos.Z, 0f, 0f, 0f);

                if(IsControlJustPressed(0, (int)Control.FrontendUp))
                {
                    CycleSelectedSpawn(up: false);
                } else if(IsControlJustPressed(0, (int)Control.FrontendDown))
                {
                    CycleSelectedSpawn(up: true);
                } else if(IsControlJustReleased(0, (int)Control.FrontendAccept))
                {
                    ConfirmSelectedSpawn();
                }
            }

            if(_enabled)
            {
                float screenX = 0.5f, screenY = 0.5f;
                World3dToScreen2d(_selectedSpawn.SpawnInfo.Spawn.Pos.X, _selectedSpawn.SpawnInfo.Spawn.Pos.Y, _selectedSpawn.SpawnInfo.Spawn.Pos.Z, ref screenX, ref screenY);
                AVControllerHelper.SetStationPos(screenX, screenY);
            }

            int playerPed = PlayerPedId();

            if (_isSpawning)
            {
                Vector3 pos = _selectedSpawn.SpawnInfo.Spawn.Pos;
                RequestCollisionAtCoord(pos.X, pos.Y, pos.Z);
                SetFocusEntity(playerPed);
                SetEntityCoords(playerPed, pos.X, pos.Y, pos.Z, false, false, false, false);
                SetEntityHeading(playerPed, _selectedSpawn.SpawnInfo.Spawn.Heading);
                SetEntityHealth(playerPed, GetEntityMaxHealth(playerPed));
                FreezeEntityPosition(playerPed, true);
            }

            if(_currentCarSpawnRequest.HasValue)
            {
                uint model = _currentCarSpawnRequest.Value.Model;
                RequestModel(model);
                if(HasModelLoaded(model))
                {
                    Vector3 pos = _currentCarSpawnRequest.Value.Pos;
                    float heading = _currentCarSpawnRequest.Value.Heading;
                    byte slot = _currentCarSpawnRequest.Value.Slot;
                    string station = _currentCarSpawnRequest.Value.StationName;
                    _currentCarSpawnRequest = null;
                    int handle = CreateVehicle(model, pos.X, pos.Y, pos.Z, heading, true, true);
                    int blip = AddBlipForEntity(handle);
                    _currentCar = new SpawnedCar
                    {
                        Model = model,
                        NetId = VehToNet(handle),
                        Blip = blip,
                        HasLeftSpawn = false,
                        InitialPos = pos,
                        Slot = slot,
                        StationName = station,
                    };
                    SetEntityAsMissionEntity(handle, true, true);
                    SetNetworkIdCanMigrate(_currentCar.Value.NetId, false);
                    SetNetworkIdAlwaysExistsForPlayer(_currentCar.Value.NetId, PlayerId(), true);
                    SetBlipSprite(blip, (int)BlipSprite.PersonalVehicleCar);
                    SetBlipNameFromTextFile(blip, ServiceVehicleBlipTextKey);
                }
            }

            if(_timeSinceVehicleCheck >= VehicleCheckIntervalSeconds && _currentCar.HasValue)
            {
                _timeSinceVehicleCheck = 0f;

                if (NetworkDoesNetworkIdExist(_currentCar.Value.NetId) && NetworkDoesEntityExistWithNetworkId(_currentCar.Value.NetId))
                {
                    int car = NetToVeh(_currentCar.Value.NetId);

                    if(IsPedInAnyVehicle(playerPed, true))
                    {
                        if (GetVehiclePedIsIn(playerPed, false) == car)
                        {
                            SetBlipDisplay(_currentCar.Value.Blip, 0);
                        }
                    }
                    else
                    {
                        SetBlipDisplay(_currentCar.Value.Blip, 6);
                    }

                    if (!_currentCar.Value.HasLeftSpawn)
                    {
                        bool isDestroyed = !IsVehicleDriveable(car, false);
                        if (isDestroyed)
                        {
                            int entity = car;
                            SetEntityAsNoLongerNeeded(ref entity);
                            SetEntityAsMissionEntity(car, false, false);
                            int blip = _currentCar.Value.Blip;
                            RemoveBlip(ref blip);
                            Debug.WriteLine("Cop car destroyed, setting as not needed");
                        }

                        if (isDestroyed || GetEntityCoords(car, false).DistanceToSquared(_currentCar.Value.InitialPos) > CarSpawnSafeRadiusSq)
                        {
                            SpawnedCar updatedCar = _currentCar.Value;
                            updatedCar.HasLeftSpawn = true;
                            _currentCar = updatedCar;
                            TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidCopCarLeftSpawn, updatedCar.StationName, updatedCar.Slot);
                        }

                        if (isDestroyed)
                        {
                            _currentCar = null;
                        }
                    }
                }
            }

            if(_timeSinceVehicleCheck < VehicleCheckIntervalSeconds)
            {
                _timeSinceVehicleCheck += deltaTime;
            }

            RenderScriptCams(_enabled, true, CamTransitionTime, true, false);
        }

        internal void Cleanup()
        {
            Debug.WriteLine($"Destroying {nameof(CopSpawnController)}");

            if(_camera1.HasValue)
            {
                DestroyCam(_camera1.Value, true);
                DestroyCam(_camera2.Value, true);
            }

            if(_currentCar.HasValue)
            {
                if(NetworkDoesNetworkIdExist(_currentCar.Value.NetId) && NetworkDoesEntityExistWithNetworkId(_currentCar.Value.NetId))
                {
                    int handle = NetToVeh(_currentCar.Value.NetId);
                    NetworkRequestControlOfEntity(handle);
                    SetEntityAsMissionEntity(handle, true, true);
                    DeleteEntity(ref handle);
                    _currentCar = null;
                }
            }
        }

        private readonly static Random s_RNG = new Random();

        internal void OnCopCarSpawnGranted(CopSpawnInfo station, byte slot)
        {
            Vector3 pos = GetSpawnPosForCar(station.CarLayout, slot, out float heading);
            _currentCarSpawnRequest = new CarSpawnRequest
            {
                Pos = pos,
                Heading = heading,
                Model = station.CopCarModels[s_RNG.Next(0, station.CopCarModels.Length)],
                StationName = station.Name,
                Slot = slot,
            };
        }

        public void OnNetEntityReceived(string name, int netId)
        {

        }
    }
}
