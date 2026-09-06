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
    internal class CopSpawnController : ITickable, INetEntityListener, IDisguiseEmitter, IHeatListener, IHuntLifecycleListener, ISpecialEventListener
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
            internal readonly bool Blocked;
            internal SelectedSpawn(CopSpawnInfo spawnInfo, byte index, bool blocked = false)
            {
                Index = index;
                SpawnInfo = spawnInfo;
                Blocked = blocked;
            }
        }

        private readonly AVControllerHelper AVControllerHelper;
        private readonly TriggerServerEventProxyDelegate TriggerServerEvent;

        private const string ServiceVehicleBlipTextKey = "STH_CUPID_COP_CAR_BLIP";
        private const string ServiceVehicleBlipTextContent = "Service Vehicle";

        private const string CarBootChangeClothesHintKey = "STH_CUPID_COP_CHANGE_CLOTHES_HELP";
        private const string CarBootChangeClothesHintContent = "Press ~INPUT_CONTEXT~ to change between uniform and undercover gear.";

        private const string ChangeClothesAnimDict = "anim_heist@hs3f@ig12_change_clothes@";
        private const string MaleChangeClothesAnimClip = "action_02_male";
        private const string FemaleChangeClothesAnimClip = "change_noose_female";

        private static readonly bool s_InitDone = Init();

        private static bool Init()
        {
            if(!s_InitDone)
            {
                AddTextEntry(ServiceVehicleBlipTextKey, ServiceVehicleBlipTextContent);
                AddTextEntry(CarBootChangeClothesHintKey, CarBootChangeClothesHintContent);
            }

            return true;
        }

        internal CopSpawnController(AVControllerHelper avControllerHelper, TriggerServerEventProxyDelegate triggerServerEvent)
        {
            AVControllerHelper = avControllerHelper;
            TriggerServerEvent = triggerServerEvent;

            State.VinewoodSpawnUnlocked += OnVinewoodSpawnUnlocked;
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

        private bool IsWearingUndercover => _clothingToRestore != null;
        private PedOutfit _clothingToRestore = null;
        private void OnEnabledChanged(bool prev, bool current)
        {
            if(current)
            {
                _timeTillTransitionEnd = float.MinValue;
                _clothingToRestore = null;
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

                        if(_scannedRemoteCars.ContainsKey(_currentCar.Value.NetId))
                        {
                            _scannedRemoteCars.Remove(_currentCar.Value.NetId);
                        }
                    }

                    string station = _currentCar.Value.StationName;
                    byte slot = _currentCar.Value.Slot;
                    bool needsToFreeOnServer = !_currentCar.Value.HasLeftSpawn;
                    _currentCar = null;

                    TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent, Constants.SpecialEvent.SetInvisible, PedToNet(PlayerPedId()), true);

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
                AVControllerHelper.SetCurrentStation(null);
            }
            else
            {
                AVControllerHelper.SetCurrentStation(_selectedSpawn.SpawnInfo.Name, _selectedSpawn.Blocked, true);
            }
        }

        /// <summary>
        /// Number of seconds after the hunt starts before the Vinewood station is unlocked.
        /// </summary>
        internal const float SecondsBeforeVinewoodSpawnUnlocked = 60f * 2f;

        private CopSpawnInfo[] GetAllowedSpawns()
        {
            if(State.HaveHuntedCompletedJob)
            {
                return new List<CopSpawnInfo>(Constants.Location.CopSpawn.All).ToArray();
            }

            if(State.VinewoodSpawnUnlockTimer < SecondsBeforeVinewoodSpawnUnlocked)
            {
                List<CopSpawnInfo> allowed = new List<CopSpawnInfo>(s_SpawnsToAllowBeforeHuntedCompletedJob.Length);

                foreach(CopSpawnInfo potentiallyAllowed in s_SpawnsToAllowBeforeHuntedCompletedJob)
                {
                    if(potentiallyAllowed != Constants.Location.CopSpawn.Vinewood)
                    {
                        allowed.Add(potentiallyAllowed);
                    }
                }

                return allowed.ToArray();
            }

            return s_SpawnsToAllowBeforeHuntedCompletedJob;
        }

        private void SelectNearestSpawn()
        {
            CopSpawnInfo nearest = Constants.Location.CopSpawn.FindNearest(GetEntityCoords(PlayerPedId(), false), GetAllowedSpawns());
            sbyte index = Constants.Location.CopSpawn.FindIndex(nearest);
            if(index == -1)
            {
                throw new Exception($"{nameof(SelectNearestSpawn)} failed because an index for {nameof(nearest)} cannot be found in {nameof(Constants.Location.CopSpawn)}");
            }

            CopSpawnInfo old = _selectedSpawn.SpawnInfo;
            SelectedSpawn newSpawn = new SelectedSpawn(nearest, (byte)index, !IsSpawnAllowed(nearest));

            SelectedSpawn oldSpawn = _selectedSpawn;
            _selectedSpawn = newSpawn;
            OnSelectedSpawnChanged(oldSpawn, newSpawn);
        }

        private int? _playerCopCar = null;

        private void CycleSelectedSpawn(bool up)
        {
            Debug.WriteLine($"Cycling selected spawn {nameof(up)}: {up}");

            int nextIndex = (_selectedSpawn.Index + Constants.Location.CopSpawn.Count + (up ? -1 : 1)) % Constants.Location.CopSpawn.Count;
            SelectedSpawn oldSpawn = _selectedSpawn;
            CopSpawnInfo nextSpawn = Constants.Location.CopSpawn.FromIndex((byte)nextIndex);
            _selectedSpawn = new SelectedSpawn(nextSpawn, (byte)nextIndex, !IsSpawnAllowed(nextSpawn));
            OnSelectedSpawnChanged(oldSpawn, _selectedSpawn);
        }

        private bool IsSpawnAllowed(CopSpawnInfo station)
        {
            if(!State.HaveHuntedCompletedJob)
            {
                foreach(CopSpawnInfo allowed in GetAllowedSpawns())
                {
                    if(station == allowed)
                    {
                        return true;
                    }
                }

                return false;
            }
            return true;
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

        private static readonly CopSpawnInfo[] s_SpawnsToAllowBeforeHuntedCompletedJob =
        {
            Constants.Location.CopSpawn.Paleto,
            Constants.Location.CopSpawn.Vinewood
        };

        private void OnSelectedSpawnChanged(in SelectedSpawn prev, in SelectedSpawn current)
        {
            if(GetOtherCamera(out int newCam))
            {
                SetCamCoord(newCam, current.SpawnInfo.Camera.Pos.X, current.SpawnInfo.Camera.Pos.Y, current.SpawnInfo.Camera.Pos.Z);
                SetCamRot(newCam, current.SpawnInfo.Camera.Rot.X, current.SpawnInfo.Camera.Rot.Y, current.SpawnInfo.Camera.Rot.Z, 2);
                SwitchCams();

                // TODO: get screen space pos of spawn and send to av controller
            }

            if(_camera.HasValue)
            {
                AVControllerHelper.SetCurrentStation(current.SpawnInfo.Name, current.Blocked);
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
            if(_selectedSpawn.Blocked)
            {
                PlaySoundFrontend(-1, "Click_Fail", "DLC_Biker_Computer_Sounds", true);
                return;
            }
            else
            {
                PlaySoundFrontend(-1, "CONTINUE", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
            }

            FreezeEntityPosition(PlayerPedId(), false);
            TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent, Constants.SpecialEvent.SetInvisible, PedToNet(PlayerPedId()), false);
            SetPlayerInvincible(PlayerId(), false);

            // Prevent cops from putting on helmet on bikes
            SetPedHelmet(PlayerPedId(), false);

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

            // Give cops armour
            SetPedArmour(PlayerPedId(), 100);
        }

        private static Vector3 GetSpawnPosForCar(CopSpawnInfo.CarPosInfo carLayout, byte slot, out float heading)
        {
            heading = carLayout.Heading;
            return new Vector3(carLayout.Origin.X + carLayout.Step.X * slot, carLayout.Origin.Y + carLayout.Step.Y * slot, carLayout.Origin.Z);
        }

        private static string GetChangeClothesAnimClipForPed(int ped)
        {
            if(IsMpPedMale(ped))
            {
                return MaleChangeClothesAnimClip;
            }

            return FemaleChangeClothesAnimClip;
        }

        private float _timeTillTransitionEnd = float.MinValue;
        private float _timeSinceVehicleCheck = 0f;
        private const float VehicleCheckIntervalSeconds = 1f;
        private struct CopCarInteractState
        {
            internal bool ChangeClothesFromBootHintActive;
            internal float TimeTillClothesChanged;
            internal int CarHandle;
            internal bool PlayedAnim;
            internal int AnimCam;
        }
        private CopCarInteractState _carInteractState = new CopCarInteractState
        {
            ChangeClothesFromBootHintActive = false,
            TimeTillClothesChanged = float.MinValue,
            CarHandle = 0,
            PlayedAnim = false,
            AnimCam = CreateCam("DEFAULT_SCRIPTED_CAMERA", false),
        };
        private const float ClothesChangeTimeSeconds = 7f;
        private const float ClothesChangeTriggerTValue = 0.5f;
        public void Tick(float deltaTime)
        {
            EnsureCamera();

            foreach(int netId in State.InvisibleNetIds)
            {
                if(NetworkDoesNetworkIdExist(netId) && NetworkDoesEntityExistWithNetworkId(netId))
                {
                    int entityHandle = NetToEnt(netId);
                    SetEntityLocallyInvisible(entityHandle);
                }
            }

            State.VinewoodSpawnUnlockTimer += deltaTime;

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

                FreezeEntityPosition(PlayerPedId(), true);

                SetPlayerInvincible(PlayerId(), true);

                if (IsControlJustPressed(0, (int)Control.FrontendUp))
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
                        StationName = station
                    };
                    SetEntityAsMissionEntity(handle, true, true);
                    SetNetworkIdCanMigrate(_currentCar.Value.NetId, false);
                    SetNetworkIdAlwaysExistsForPlayer(_currentCar.Value.NetId, PlayerId(), true);
                    SetBlipSprite(blip, (int)BlipSprite.PersonalVehicleCar);
                    SetBlipNameFromTextFile(blip, ServiceVehicleBlipTextKey);
                    TriggerServerEvent(SurviveTheHuntShared.Events.Server.NotifyNetEntity, _currentCar.Value.NetId, PoliceCarNetEntityName);
                    CarModHelper.ApplyModsForSpecialSpawnedCar(handle, model);
                }
            }

            if(_currentCar.HasValue && NetworkDoesEntityExistWithNetworkId(_currentCar.Value.NetId))
            {
                int car = NetToVeh(_currentCar.Value.NetId);

                if (!_currentCar.Value.HasLeftSpawn)
                {
                    // Give the car invincibility in spawn
                    SetEntityInvincible(car, true);
                }

                CarModHelper.TickSpecialVehicleProperties(car, _currentCar.Value.Model);
            }

            if(_timeSinceVehicleCheck >= VehicleCheckIntervalSeconds)
            {
                bool isInVehicle = IsPedInAnyVehicle(playerPed, true);

                if (_currentCar.HasValue)
                {
                    _timeSinceVehicleCheck = 0f;

                    if (NetworkDoesNetworkIdExist(_currentCar.Value.NetId) && NetworkDoesEntityExistWithNetworkId(_currentCar.Value.NetId))
                    {
                        int car = NetToVeh(_currentCar.Value.NetId);

                        if (isInVehicle)
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
                                SetEntityInvincible(car, false);
                            }

                            if (isDestroyed)
                            {
                                _currentCar = null;
                            }
                        }
                    }
                }

                if (!isInVehicle && _carInteractState.TimeTillClothesChanged <= 0f)
                {
                    bool nearBoot = false;
                    foreach (KeyValuePair<int, RemoteCarInfo> scannedCar in _scannedRemoteCars)
                    {
                        if (NetworkDoesNetworkIdExist(scannedCar.Key) && NetworkDoesEntityExistWithNetworkId(scannedCar.Key))
                        {
                            int nearestCarHandle = NetToVeh(scannedCar.Key);
                            Vector3 carDir = GetEntityForwardVector(nearestCarHandle);
                            float bootOffset = GetBootOffsetForModel(scannedCar.Value.Model);
                            Vector3 carPos = GetEntityCoords(nearestCarHandle, false);
                            bool isBike = IsThisModelABike(scannedCar.Value.Model);
                            Vector3 bootPos = isBike
                                ? (carPos - (bootOffset * carDir * 1.5f))
                                : (GetWorldPositionOfEntityBone(nearestCarHandle, scannedCar.Value.BootBoneIndex) - (bootOffset * carDir));

                            Vector3 playerPos = GetEntityCoords(playerPed, false);
                            float bootTriggerDistance = 0.85f * GetBootDistanceMultiplierForModel(scannedCar.Value.Model);
                            if (bootPos.DistanceToSquared(playerPos) < (bootTriggerDistance * bootTriggerDistance))
                            {
                                Vector3 playerToCarDir = playerPos - carPos;
                                playerToCarDir.Normalize();
                                // Check the player is actually behind the car
                                float dot = Vector3.Dot(playerToCarDir, carDir);
                                //Debug.WriteLine($"Found a car boot at {bootPos}, player is at {playerPos}. Dot is {dot}");
                                if (dot < -0.55f)
                                {
                                    nearBoot = true;
                                    _carInteractState.CarHandle = nearestCarHandle;
                                    break;
                                }
                            }
                        }
                    }

                    if (nearBoot != _carInteractState.ChangeClothesFromBootHintActive)
                    {
                        if (nearBoot)
                        {
                            BeginTextCommandDisplayHelp(CarBootChangeClothesHintKey);
                            EndTextCommandDisplayHelp(0, true, true, -1);
                        }
                        else
                        {
                            ClearAllHelpMessages();
                        }

                        _carInteractState.ChangeClothesFromBootHintActive = nearBoot;
                    }
                }

                ScanPendingCars();
            }

            if (_carInteractState.ChangeClothesFromBootHintActive)
            {
                if (IsControlJustPressed(0, (int)Control.Context))
                {
                    ClearAllHelpMessages();
                    _carInteractState.ChangeClothesFromBootHintActive = false;
                    _carInteractState.TimeTillClothesChanged = 7f;
                    SetVehicleDoorOpen(_carInteractState.CarHandle, (int)VehicleDoorIndex.Trunk, false, false);
                    Vector3 camPos = IsThisModelABike((uint)GetEntityModel(_carInteractState.CarHandle)) || !_scannedRemoteCars.TryGetValue(VehToNet(_carInteractState.CarHandle), out RemoteCarInfo carInfo)
                        ? (GetEntityCoords(_carInteractState.CarHandle, false) + (Vector3.Up * 0.55f) + GetEntityForwardVector(_carInteractState.CarHandle) * 0.95f)
                        : (GetWorldPositionOfEntityBone(_carInteractState.CarHandle, carInfo.BootBoneIndex));
                    SetCamCoord(_carInteractState.AnimCam, camPos.X, camPos.Y, camPos.Z);
                    PointCamAtEntity(_carInteractState.AnimCam, playerPed, 0f, 0f, 0f, true);
                    SetCamActive(_carInteractState.AnimCam, true);
                }
            }

            bool hasClothesTimerJustRunOut = false;
            bool shouldChangeClothesNow = false;
            if(_carInteractState.TimeTillClothesChanged > 0f)
            {
                const float triggerPoint = ClothesChangeTimeSeconds * ClothesChangeTriggerTValue;
                bool wasBeforeTrigger = _carInteractState.TimeTillClothesChanged > triggerPoint;
                _carInteractState.TimeTillClothesChanged -= deltaTime;
                if(_carInteractState.TimeTillClothesChanged <= 0f)
                {
                    hasClothesTimerJustRunOut = true;
                }
                if(wasBeforeTrigger && _carInteractState.TimeTillClothesChanged <= triggerPoint)
                {
                    shouldChangeClothesNow = true;
                }
                FreezeEntityPosition(playerPed, true);
                RequestAnimDict(ChangeClothesAnimDict);
                if(!_carInteractState.PlayedAnim && HasAnimDictLoaded(ChangeClothesAnimDict))
                {
                    _carInteractState.PlayedAnim = true;
                    TaskPlayAnim(playerPed, ChangeClothesAnimDict, GetChangeClothesAnimClipForPed(playerPed), 4f, 4f, -1, 0, 0f, false, false, false);
                }
            }

            if(hasClothesTimerJustRunOut)
            {
                _carInteractState.PlayedAnim = false;
                SetVehicleDoorShut(_carInteractState.CarHandle, (int)VehicleDoorIndex.Trunk, false);
                _carInteractState.CarHandle = 0;
                FreezeEntityPosition(playerPed, false);
                SetCamActive(_carInteractState.AnimCam, false);
            }

            if(shouldChangeClothesNow)
            {
                SwapPlayerClothes(playerPed);
            }

            if (_timeSinceVehicleCheck < VehicleCheckIntervalSeconds)
            {
                _timeSinceVehicleCheck += deltaTime;
            }

            bool isPlayingClothesChangeAnim = _carInteractState.TimeTillClothesChanged > 0f;
            RenderScriptCams(_enabled || isPlayingClothesChangeAnim, !isPlayingClothesChangeAnim && !hasClothesTimerJustRunOut, CamTransitionTime, !isPlayingClothesChangeAnim && !hasClothesTimerJustRunOut, false);
        }

        private float GetBootDistanceMultiplierForModel(uint model)
        {
            switch(model)
            {
                case (uint)VehicleHash.Pranger:
                case (uint)VehicleHash.Sheriff2:
                    return 1.55f;
                default:
                    return 1f;
            }
        }

        private float GetBootOffsetForModel(uint model)
        {
            switch(model)
            {
                case (uint)VehicleHash.Pranger:
                case (uint)VehicleHash.Sheriff2:
                    return 1.15f;
                default:
                    return 1f;
            }
        }

        private static bool IsMpPedMale(int ped)
        {
            uint model = (uint)GetEntityModel(ped);
            return (uint)PedHash.FreemodeMale01 == model;
        }

        private struct UndercoverClothes
        {
            internal struct AllowedDrawable
            {
                internal readonly int Drawable;
                internal readonly int[] Textures;

                internal AllowedDrawable(int drawable, params int[] textures)
                {
                    Drawable = drawable;
                    Textures = textures;
                }

                internal AllowedDrawable(int drawable) : this(drawable, 0) { }
            }

            internal readonly Dictionary<int, AllowedDrawable[]> Components, Props;

            internal UndercoverClothes(Dictionary<int, AllowedDrawable[]> components, Dictionary<int, AllowedDrawable[]> props = null)
            {
                Components = components;
                Props = props ?? new Dictionary<int, AllowedDrawable[]>();
            }
        }

        private static readonly UndercoverClothes FemaleUndercoverClothes = new UndercoverClothes
        (
            components: new Dictionary<int, UndercoverClothes.AllowedDrawable[]>
            {
                // Jackets
                {
                    (int)PedComponents.Torso2, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(8, 0, 1, 2, 12),
                        new UndercoverClothes.AllowedDrawable(35, 4, 7, 8, 9, 11)
                    }
                },

                // T-shirt
                {
                    (int)PedComponents.Special2, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(95, 0, 1, 2)
                    }
                },

                // Runners
                {
                    (int)PedComponents.Shoes, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(32, 0, 1, 2, 3)
                    }
                },

                // Torso
                {
                    (int)PedComponents.Torso, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(5)
                    }
                },

                // Jeans
                {
                    (int)PedComponents.Legs, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(0, 0, 1, 2, 8, 10),
                        new UndercoverClothes.AllowedDrawable(1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
                    }
                }
            },
            props: new Dictionary<int, UndercoverClothes.AllowedDrawable[]>
            {
                {
                    (int)PedProps.Hats, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(-1),
                        // cap
                        new UndercoverClothes.AllowedDrawable(155, 2, 3, 8, 9, 14, 18)
                    }
                },
                {
                    (int)PedProps.Glasses, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(-1),
                        // aviators
                        new UndercoverClothes.AllowedDrawable(11, 0, 1, 2, 3, 4, 5, 6, 7)
                    }
                }
            }
        );

        private static readonly UndercoverClothes MaleUndercoverClothes = new UndercoverClothes
        (
            components: new Dictionary<int, UndercoverClothes.AllowedDrawable[]>
            {
                // Tee
                {
                    (int)PedComponents.Special2, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(0, 0, 2, 4, 5, 7, 8, 11)
                    }
                },
                // Jacket
                {
                    (int)PedComponents.Torso2, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(387, 9, 5, 8, 12)
                    }
                },
                // Jeans
                {
                    (int)PedComponents.Legs, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(0, 0, 1, 2, 4, 5, 6, 8, 9, 10, 12)
                    }
                },
                // Shoes
                {
                    (int)PedComponents.Shoes, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(32, 0, 1, 2)
                    }
                },
                // Torso
                {
                    (int)PedComponents.Torso, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(4)
                    }
                }
            },
            props: new Dictionary<int, UndercoverClothes.AllowedDrawable[]>
            {
                {
                    (int)PedProps.Hats, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(-1),
                        // cap
                        new UndercoverClothes.AllowedDrawable(156, 2, 3, 8, 9, 14, 18)
                    }
                },
                {
                    (int)PedProps.Glasses, new UndercoverClothes.AllowedDrawable[]
                    {
                        new UndercoverClothes.AllowedDrawable(-1),
                        // aviators
                        new UndercoverClothes.AllowedDrawable(8, 0, 1, 2, 3, 4, 5, 6, 7)
                    }
                }
            }
        );

        private void SwapPlayerClothes(int playerPed)
        {
            bool useUndercover = !IsWearingUndercover;

            // Store the current clothing
            if(_clothingToRestore == null)
            {
                Dictionary<PedComponents, PedVariation> comps = new Dictionary<PedComponents, PedVariation>();

                for(int comp = 0; comp <= 11; comp++)
                {
                    int drawable = GetPedDrawableVariation(playerPed, comp);
                    int texture = GetPedTextureVariation(playerPed, comp);
                    comps[(PedComponents)comp] = new PedVariation { Drawable = drawable, Texture = texture };
                }

                Dictionary<PedProps, PedVariation> props = new Dictionary<PedProps, PedVariation>();
                for (int prop = 0; prop <= 9; prop++)
                {
                    int drawable = GetPedPropIndex(playerPed, prop);
                    int texture = GetPedPropTextureIndex(playerPed, prop);
                    props[(PedProps)prop] = new PedVariation { Drawable = drawable, Texture = texture };
                }

                _clothingToRestore = new PedOutfit { ComponentsToApply = comps, PropsToApply = props };
            }

            ClearAllPedProps(playerPed);
            if (useUndercover)
            {
                UndercoverClothes clothes = IsMpPedMale(playerPed) ? MaleUndercoverClothes : FemaleUndercoverClothes;

                SetPedDefaultComponentVariation(playerPed);
                PedComponents[] compsToKeep =
                {
                    PedComponents.Face,
                    PedComponents.Hair
                };
                foreach(PedComponents comp in compsToKeep)
                {
                    SetPedComponentVariation(playerPed, (int)comp, _clothingToRestore.ComponentsToApply[comp].Drawable, _clothingToRestore.ComponentsToApply[comp].Texture, 0);
                }

                foreach(KeyValuePair<int, UndercoverClothes.AllowedDrawable[]> comp in clothes.Components)
                {
                    UndercoverClothes.AllowedDrawable randomDrawable = comp.Value[s_RNG.Next(0, comp.Value.Length)];
                    int randomTexture = randomDrawable.Textures[s_RNG.Next(0, randomDrawable.Textures.Length)];
                    SetPedComponentVariation(playerPed, comp.Key, randomDrawable.Drawable, randomTexture, 0);
                }

                foreach (KeyValuePair<int, UndercoverClothes.AllowedDrawable[]> prop in clothes.Props)
                {
                    UndercoverClothes.AllowedDrawable randomDrawable = prop.Value[s_RNG.Next(0, prop.Value.Length)];
                    int randomTexture = randomDrawable.Textures[s_RNG.Next(0, randomDrawable.Textures.Length)];
                    SetPedPropIndex(playerPed, prop.Key, randomDrawable.Drawable, randomTexture, true);
                }
            }
            else if(_clothingToRestore != null)
            {
                foreach(KeyValuePair<PedComponents, PedVariation> comp in _clothingToRestore.ComponentsToApply)
                {
                    SetPedComponentVariation(playerPed, (int)comp.Key, comp.Value.Drawable, comp.Value.Texture, 0);
                }

                foreach(KeyValuePair<PedProps, PedVariation> prop in _clothingToRestore.PropsToApply)
                {
                    if (prop.Value.Drawable != -1)
                    {
                        SetPedPropIndex(playerPed, (int)prop.Key, prop.Value.Drawable, prop.Value.Texture, true);
                    }
                }

                _clothingToRestore = null;
            }

            DisguiseStateChanged.Invoke(useUndercover ? Constants.DisguiseState.UndercoverCop : Constants.DisguiseState.None);
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
                    int blip = _currentCar.Value.Blip;
                    RemoveBlip(ref blip);
                    _currentCar = null;
                }
            }

            if(DoesCamExist(_carInteractState.AnimCam))
            {
                DestroyCam(_carInteractState.AnimCam, true);
            }

            ClearAllHelpMessages();

            SetFocusEntity(PlayerPedId());

            AVControllerHelper.SetCurrentStation(null);
            SetPedHelmet(PlayerPedId(), true);
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

        private struct RemoteCarInfo
        {
            internal readonly int BootBoneIndex;
            internal readonly uint Model;

            internal RemoteCarInfo(uint model, int bootBoneIndex)
            {
                BootBoneIndex = bootBoneIndex;
                Model = model;
            }
        }

        private List<int> _pendingCarNetIdsToScan = new List<int>();
        private Dictionary<int, RemoteCarInfo> _scannedRemoteCars = new Dictionary<int, RemoteCarInfo>();

        private bool ScanPendingCars()
        {
            bool updated = false;

            if(_pendingCarNetIdsToScan.Count > 0)
            {
                List<int> toRemove = new List<int>(_pendingCarNetIdsToScan.Count);
                
                foreach(int netId in _pendingCarNetIdsToScan)
                {
                    if(NetworkDoesNetworkIdExist(netId) && NetworkDoesEntityExistWithNetworkId(netId))
                    {
                        int car = NetToVeh(netId);
                        toRemove.Add(netId);
                        _scannedRemoteCars[netId] = new RemoteCarInfo
                        (
                            model: (uint)GetEntityModel(car),
                            bootBoneIndex: GetEntityBoneIndexByName(car, "boot")
                        );
                        Debug.WriteLine($"Synced new car with net ID {netId}. Model is {_scannedRemoteCars[netId].Model}, boot bone is {_scannedRemoteCars[netId].BootBoneIndex}");
                    }
                }

                foreach(int netIdToRemove in toRemove)
                {
                    if(_pendingCarNetIdsToScan.Remove(netIdToRemove))
                    {
                        updated = true;
                    }
                }
            }

            return updated;
        }

        private const string PoliceCarNetEntityName = "cupid_policecar";

        public void OnNetEntityReceived(string name, int netId)
        {
            Debug.WriteLine($"{nameof(CopSpawnController)} received a net entity {name} with net ID {netId}");
            if (name == PoliceCarNetEntityName)
            {
                Debug.WriteLine("This is a police car so we will store it for scanning");
                _pendingCarNetIdsToScan.Add(netId);
            }
        }

        public void OnHeatChanged(ushort heatScore, Constants.HeatThresholds heatThreshold)
        {
            State.HaveHuntedCompletedJob = true;
            // Unlock all spawns once hunted completed a job
            _selectedSpawn = new SelectedSpawn(_selectedSpawn.SpawnInfo, _selectedSpawn.Index, false);

            if(Enabled)
            {
                RefreshStationUI();
            }
        }

        private void RefreshStationUI()
        {
            AVControllerHelper.SetCurrentStation(_selectedSpawn.SpawnInfo.Name, _selectedSpawn.Blocked, force: true);
        }

        public void OnHuntStarted(IGameState gameState, IPlayerState playerState)
        {
            if(State != null)
            {
                State.VinewoodSpawnUnlocked -= OnVinewoodSpawnUnlocked;
            }

            State = new HuntState();
            State.VinewoodSpawnUnlocked += OnVinewoodSpawnUnlocked;
        }

        private void OnVinewoodSpawnUnlocked(object sender, EventArgs e)
        {
            Debug.WriteLine($"{nameof(OnVinewoodSpawnUnlocked)}");
            if(Enabled && _selectedSpawn.SpawnInfo == Constants.Location.CopSpawn.Vinewood && _selectedSpawn.Blocked)
            {
                _selectedSpawn = new SelectedSpawn(_selectedSpawn.SpawnInfo, _selectedSpawn.Index, false);
                AVControllerHelper.SetCurrentStation(_selectedSpawn.SpawnInfo.Name, _selectedSpawn.Blocked, force: true);
            }
        }

        public void OnHuntEnded(SurviveTheHuntShared.Core.Teams.Team localPlayerTeam, IGameState gameState, IPlayerState playerState)
        {
            State.VinewoodSpawnUnlocked -= OnVinewoodSpawnUnlocked;
        }

        public void OnSpecialEvent(Constants.SpecialEvent specialEvent, object[] args)
        {
            switch(specialEvent)
            {
                case Constants.SpecialEvent.SetInvisible:
                    int entityNetId = Convert.ToInt32(args[0]);
                    bool invisible = Convert.ToBoolean(args[1]);
                    RegisterEntityVisibilityOverride(entityNetId, !invisible);
                    break;
            }
        }

        private void RegisterEntityVisibilityOverride(int entityNetId, bool visible)
        {
            bool isAlreadyInvisible = State.InvisibleNetIds.Contains(entityNetId);

            if (!visible && !isAlreadyInvisible)
            {
                Debug.WriteLine($"Marking net ID {entityNetId} invisible");
                State.InvisibleNetIds.Add(entityNetId);
            }

            if(visible && isAlreadyInvisible)
            {
                Debug.WriteLine($"Marking net ID {entityNetId} visible");
                while(State.InvisibleNetIds.Remove(entityNetId)) { }
            }
        }

        private HuntState State = new HuntState();

        private class HuntState
        {
            internal bool HaveHuntedCompletedJob = false;

            internal readonly List<int> InvisibleNetIds = new List<int>();

            private float _vinewoodSpawnUnlockTimer = 0f;
            internal float VinewoodSpawnUnlockTimer
            {
                get => _vinewoodSpawnUnlockTimer;
                set
                {
                    bool wasPreviouslyNotFinished = _vinewoodSpawnUnlockTimer < SecondsBeforeVinewoodSpawnUnlocked;
                    if (wasPreviouslyNotFinished)
                    {
                        _vinewoodSpawnUnlockTimer = value;
                    }

                    if (wasPreviouslyNotFinished && _vinewoodSpawnUnlockTimer >= SecondsBeforeVinewoodSpawnUnlocked)
                    {
                        VinewoodSpawnUnlocked.Invoke(this, new EventArgs());
                    }
                }
            }

            internal event EventHandler VinewoodSpawnUnlocked;
        }

        public event DisguiseStateChangedEvent DisguiseStateChanged;
    }
}
