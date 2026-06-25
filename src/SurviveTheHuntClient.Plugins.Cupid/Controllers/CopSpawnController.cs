using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Helpers;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using System;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers
{
    internal class CopSpawnController : ITickable
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

        internal CopSpawnController(AVControllerHelper avControllerHelper)
        {

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
            }

            if(_camera.HasValue)
            {
                SetCamActive(_camera.Value, current);
                SetCamCoord(_camera.Value, _selectedSpawn.SpawnInfo.Camera.Pos.X, _selectedSpawn.SpawnInfo.Camera.Pos.Y, _selectedSpawn.SpawnInfo.Camera.Pos.Z);
                SetCamRot(_camera.Value, _selectedSpawn.SpawnInfo.Camera.Rot.X, _selectedSpawn.SpawnInfo.Camera.Rot.Y, _selectedSpawn.SpawnInfo.Camera.Rot.Z, 2);
                RenderScriptCams(current, !current, current ? 0 : CamTransitionTime, !current, false);
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

        private void ConfirmSelectedSpawn()
        {
            _isSpawning = true;
            _timeTillTransitionEnd = CamTransitionTimeSeconds;
        }

        private float _timeTillTransitionEnd = float.MinValue;
        public void Tick(float deltaTime)
        {
            EnsureCamera();

            if (_timeTillTransitionEnd > 0f) 
            {
                float prevTimeTillTransitionEnd = _timeTillTransitionEnd;
                _timeTillTransitionEnd -= deltaTime;
                if (_timeTillTransitionEnd <= 0f)
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

            if(_isSpawning)
            {
                Vector3 pos = _selectedSpawn.SpawnInfo.Spawn.Pos;
                RequestCollisionAtCoord(pos.X, pos.Y, pos.Z);
                int playerPed = PlayerPedId();
                SetFocusEntity(playerPed);
                SetEntityCoords(playerPed, pos.X, pos.Y, pos.Z, false, false, false, false);
                SetEntityHeading(playerPed, _selectedSpawn.SpawnInfo.Spawn.Heading);
                SetEntityHealth(playerPed, GetEntityMaxHealth(playerPed));
                FreezeEntityPosition(playerPed, true);
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
        }
    }
}
