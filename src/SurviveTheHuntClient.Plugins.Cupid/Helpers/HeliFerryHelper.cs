using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Interfaces;
using SurviveTheHuntShared.Core;
using System;
using static CitizenFX.Core.Native.API;


namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    internal class HeliFerryHelper : ITickable, IHuntLifecycleListener, ISpecialEventListener
    {
        internal readonly uint PilotPedHash;
        internal readonly uint HelicopterModelHash;

        private Vector4 _landingSpotA, _landingSpotB;

        private bool _destroyed = false;

        private bool _canSpawn = false;

        internal Vector4 LandingSpotA
        {
            get => _landingSpotA;
        }

        internal Vector4 LandingSpotB
        {
            get => _landingSpotB;
        }

        private struct SpawnResult
        {
            internal readonly int PilotPedNetId;
            internal readonly int HeliNetId;

            internal SpawnResult(int pilotPedNetId, int heliNetId)
            {
                PilotPedNetId = pilotPedNetId;
                HeliNetId = heliNetId;
            }
        }

        private SpawnResult? _spawnResult = null;

        private bool _isLocalPlayerGod = false;

        internal bool IsLocalPlayerGod => _isLocalPlayerGod;

        private readonly TriggerServerEventProxyDelegate TriggerServerEvent;

        private const string HelpTextKey = "STH_CUPID_HELP_HELIFERRY";
        private const string HelpTextContent = "Press ~INPUT_CONTEXT~ to take off.";

        private static readonly bool s_HasInit = Init();

        private int _blipId = 0;

        private static bool Init()
        {
            if(!s_HasInit)
            {
                AddTextEntry(HelpTextKey, HelpTextContent);
            }

            return true;
        }

        internal readonly string Id;

        internal HeliFerryHelper(TriggerServerEventProxyDelegate triggerServerEvent, string id, uint pilotPedHash, uint helicopterModelHash, Vector4 landingSpotA, Vector4 landingSpotB)
        {
            PilotPedHash = pilotPedHash;
            HelicopterModelHash = helicopterModelHash;
            _landingSpotA = landingSpotA;
            _landingSpotB = landingSpotB;
            TriggerServerEvent = triggerServerEvent;
            Id = id;
        }

        private bool SpawnHeliAndPilot(out SpawnResult result)
        {
            int heli = CreateVehicle(HelicopterModelHash, _landingSpotA.X, _landingSpotA.Y, _landingSpotA.Z, _landingSpotA.W, true, true);
            int ped = CreatePedInsideVehicle(heli, 0, PilotPedHash, -1, true, true);

            result = new SpawnResult(PedToNet(ped), VehToNet(heli));

            return true;
        }

        private const float ActivationCheckIntervalSeconds = 1.1f;

        private float _timeSinceActivationCheckSeconds = 0f;

        private Vector4? _currentTarget = null;

        private bool _waitForPlayerToLeaveHeli = false;

        private bool _canRequestTakeOff = false;

        private const float WarpDistance = 6f;
        private const float WarpDistanceSq = WarpDistance * WarpDistance;

        private float _secondsSinceTakeOff = 0f;

        private bool _taskedWithLanding = false;

        public void Tick(float deltaTime)
        {
            if(!_destroyed)
            {
                if(_canSpawn)
                {
                    if(HasModelLoaded(HelicopterModelHash) && HasModelLoaded(PilotPedHash))
                    {
                        if(!(_canSpawn = !SpawnHeliAndPilot(out SpawnResult spawnResult)))
                        {
                            _spawnResult = spawnResult;
                            Debug.WriteLine($"[{nameof(HeliFerryHelper)}.{nameof(Tick)}]: spawned heli and pilot");
                            TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent, Constants.SpecialEvent.SyncHeliFerry, Id, spawnResult.PilotPedNetId, spawnResult.HeliNetId);
                        }
                    }
                    else
                    {
                        RequestModel(HelicopterModelHash);
                        RequestModel(PilotPedHash);
                    }
                }

                if ((_blipId == 0 || !DoesBlipExist(_blipId)))
                {
                    _blipId = AddBlipForCoord(_landingSpotA.X, _landingSpotA.Y, _landingSpotA.Z);
                    SetBlipSprite(_blipId, (int)BlipSprite.Helicopter);
                    SetBlipDisplay(_blipId, 6);
                }

                if (_spawnResult.HasValue)
                {
                    int heliNetId = _spawnResult.Value.HeliNetId, pilotNetId = _spawnResult.Value.PilotPedNetId;

                    int? heli = null, pilot = null;

                    if(NetworkDoesNetworkIdExist(heliNetId) && NetworkDoesEntityExistWithNetworkId(heliNetId))
                    {
                        heli = NetToVeh(heliNetId);
                        SetVehicleEngineHealth(heli.Value, 1000f);
                        SetEntityInvincible(heli.Value, true);
                    }

                    if(NetworkDoesNetworkIdExist(pilotNetId) && NetworkDoesEntityExistWithNetworkId(pilotNetId))
                    {
                        pilot = NetToPed(pilotNetId);
                        SetEntityInvincible(pilot.Value, true);
                        SetBlockingOfNonTemporaryEvents(pilot.Value, true);
                    }

                    if(heli.HasValue && pilot.HasValue)
                    {
                        SetVehicleExclusiveDriver_2(heli.Value, pilot.Value, 0);
                    }

                    _timeSinceActivationCheckSeconds += deltaTime;

                    bool couldRequestTakeOffBefore = false;
                    if(_timeSinceActivationCheckSeconds > ActivationCheckIntervalSeconds)
                    {
                        _timeSinceActivationCheckSeconds = 0f;

                        if(heli.HasValue && pilot.HasValue)
                        {
                            int playerPedId = PlayerPedId();

                            if (IsPedInVehicle(playerPedId, heli.Value, true))
                            {
                                _canRequestTakeOff = !_waitForPlayerToLeaveHeli;
                            }
                            else
                            {
                                _canRequestTakeOff = false;
                                _waitForPlayerToLeaveHeli = false;
                            }
                        }
                    }

                    if(_canRequestTakeOff != couldRequestTakeOffBefore)
                    {
                        if(_canRequestTakeOff)
                        {
                            BeginTextCommandDisplayHelp(HelpTextKey);
                            EndTextCommandDisplayHelp(0, true, true, -1);
                        }
                        else
                        {
                            ClearAllHelpMessages();
                        }
                    }

                    if(_canRequestTakeOff)
                    {
                        if(IsControlJustPressed(0, (int)Control.Context))
                        {
                            ClearAllHelpMessages();
                            _canRequestTakeOff = false;
                            _waitForPlayerToLeaveHeli = true;
                            _taskedWithLanding = false;
                            _secondsSinceTakeOff = 0f;

                            TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent, Constants.SpecialEvent.ToggleHeliFerry, Id, GetPlayerServerId(PlayerId()));
                        }
                    }

                    //if(_waitForPlayerToLeaveHeli && _currentTarget.HasValue)
                    {
                        _secondsSinceTakeOff += deltaTime;
                    }

                    // failsafe
                    const float HeliTimeoutSeconds = 25f;

                    if(_secondsSinceTakeOff >= 4f && _currentTarget.HasValue && !_taskedWithLanding)
                    {
                        if(pilot.HasValue)
                        {
                            Vector3 coords = GetEntityCoords(heli.Value, false);
                            float a = coords.X - _currentTarget.Value.X;
                            float b = coords.Y - _currentTarget.Value.Y;
                            float distSq = (a * a) + (b * b);
                            
                            // cyllinder
                            if (IsEntityInWater(heli.Value) || _secondsSinceTakeOff >= HeliTimeoutSeconds || (coords.Z > _currentTarget.Value.Z && distSq <= WarpDistanceSq && coords.Z <= _currentTarget.Value.Z + WarpDistance))
                            {
                                Debug.WriteLine($"[{nameof(HeliFerryHelper)}.{nameof(Tick)}]: Tasking ped with landing");
                                ClearPedTasks(pilot.Value);
                                ClearVehicleTasks(heli.Value);
                                TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent, Constants.SpecialEvent.CancelHeliFerryTask);
                                SetEntityVelocity(heli.Value, 0f, 0f, 0f);
                                SetEntityAngularVelocity(heli.Value, 0f, 0f, 0f);
                                SetEntityCoords(heli.Value, _currentTarget.Value.X, _currentTarget.Value.Y, _currentTarget.Value.Z, false, false, false, false);
                                SetEntityHeading(heli.Value, _currentTarget.Value.W);
                                //TaskHeliMission(pilot.Value, heli.Value, 0, 0, _currentTarget.Value.X, _currentTarget.Value.Y, _currentTarget.Value.Z, 20, 5f, 2f, -1f, -1, -1f, 3, 2 | 4 | 16 | 64);
                                _taskedWithLanding = true;
                            }
                        }
                    }
                }
            }
        }

        public void OnHuntStarted(IGameState gameState, IPlayerState playerState)
        {
            _isLocalPlayerGod = Utils.PlayerUtils.GetPlayerType(PlayerId(), gameState.Hunt.HuntedPlayers) == SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType.HuntedJ;
            _canSpawn = _isLocalPlayerGod;
        }

        public void OnHuntEnded(Teams.Team localPlayerTeam, IGameState gameState, IPlayerState playerState)
        {
            Cleanup();
        }

        internal void Cleanup()
        {
            if (!_destroyed)
            {
                _destroyed = true;

                if (_spawnResult.HasValue)
                {
                    if (NetworkDoesNetworkIdExist(_spawnResult.Value.PilotPedNetId) && NetworkDoesEntityExistWithNetworkId(_spawnResult.Value.PilotPedNetId))
                    {
                        int entity = NetToVeh(_spawnResult.Value.PilotPedNetId);
                        SetEntityAsMissionEntity(entity, true, true);
                        DeleteEntity(ref entity);
                    }

                    if (NetworkDoesNetworkIdExist(_spawnResult.Value.HeliNetId) && NetworkDoesEntityExistWithNetworkId(_spawnResult.Value.HeliNetId))
                    {
                        int entity = NetToVeh(_spawnResult.Value.HeliNetId);
                        SetEntityAsMissionEntity(entity, true, true);
                        DeleteEntity(ref entity);
                    }

                    _spawnResult = null;
                }

                if(_blipId != 0 && DoesBlipExist(_blipId))
                {
                    RemoveBlip(ref _blipId);
                }

                ClearAllHelpMessages();
            }
        }

        public void OnSpecialEvent(Constants.SpecialEvent specialEvent, object[] args)
        {
            if(specialEvent == Constants.SpecialEvent.SyncHeliFerry && !_spawnResult.HasValue)
            {
                string id = Convert.ToString(args[0]);
                int pilotPedNetId = Convert.ToInt32(args[1]);
                int heliNetId = Convert.ToInt32(args[2]);
                _spawnResult = new SpawnResult(pilotPedNetId, heliNetId);
                Debug.WriteLine($"[{nameof(HeliFerryHelper)}.{nameof(OnSpecialEvent)}]: helper \"{Id}\" received pilot ped net ID {pilotPedNetId} and heli net ID {heliNetId}");
            }

            if(specialEvent == Constants.SpecialEvent.ToggleHeliFerry)
            {
                string id = Convert.ToString(args[0]);
                if (id == Id)
                {
                    int sender = Convert.ToInt32(args[1]);

                    if (_currentTarget.HasValue)
                    {
                        _currentTarget = _currentTarget.Value == _landingSpotA ? _landingSpotB : _landingSpotA;
                    }
                    else
                    {
                        _currentTarget = _landingSpotB;
                    }

                    _waitForPlayerToLeaveHeli = true;

                    if (_spawnResult.HasValue)
                    {
                        if (NetworkDoesNetworkIdExist(_spawnResult.Value.HeliNetId) && NetworkDoesEntityExistWithNetworkId(_spawnResult.Value.HeliNetId))
                        {
                            int heli = NetToVeh(_spawnResult.Value.HeliNetId);
                            _canRequestTakeOff = false;
                            if (IsPedInVehicle(PlayerPedId(), heli, true))
                            {
                                ClearAllHelpMessages();
                            }
                        }
                    }

                    _secondsSinceTakeOff = 0f;
                    _taskedWithLanding = false;

                    SetBlipCoords(_blipId, _currentTarget.Value.X, _currentTarget.Value.Y, _currentTarget.Value.Z);

                    // FIXME: currently this doesn't work if the sender is non-pedgod, maybe let's have all clients task the ped so we don't have to negotiate ownership
                    if (sender == GetPlayerServerId(PlayerId()))
                    {
                        Debug.WriteLine($"{Constants.SpecialEvent.ToggleHeliFerry} sender was local player - tasking pilot ped");
                        if (_spawnResult.HasValue && NetworkDoesNetworkIdExist(_spawnResult.Value.PilotPedNetId) && NetworkDoesEntityExistWithNetworkId(_spawnResult.Value.PilotPedNetId))
                        {
                            int pilot = NetToPed(_spawnResult.Value.PilotPedNetId);
                            int vehicle = NetToVeh(_spawnResult.Value.HeliNetId);

                            Vector3 pos = GetEntityCoords(vehicle, false);
                            float offsetX = _currentTarget.Value.X - pos.X, offsetY = _currentTarget.Value.Y - pos.Y;
                            float length = (float)Math.Sqrt((offsetX * offsetX) + (offsetY * offsetY));
                            float dirX = offsetX / length, dirY = offsetY / length;
                            const float fwdX = 0f, fwdY = 1f;
                            float dot = (dirX * fwdX) + (dirY * fwdY);
                            float angle = (float)Math.Acos(dot) * (180f / (float)Math.PI) - 90f;

                            Debug.WriteLine($"Flying to {_currentTarget.Value} with heading {Math.Round(angle)}deg");

                            FreezeEntityPosition(vehicle, false);

                            // FIXME: this flag may need looking at
                            TaskHeliMission(pilot, vehicle, 0, 0, _currentTarget.Value.X, _currentTarget.Value.Y, _currentTarget.Value.Z, 4, 20f, 5f, angle, 5, 0f /*Math.Max(_landingSpotA.Z, _landingSpotB.Z)*/, 40, 1 | 8192 | 128 | 32);
                        }
                    }
                }
            }

            if(specialEvent == Constants.SpecialEvent.CancelHeliFerryTask)
            {
                if(_spawnResult.HasValue)
                {
                    int? pilot = null, heli = null;
                    if (NetworkDoesNetworkIdExist(_spawnResult.Value.HeliNetId) && NetworkDoesEntityExistWithNetworkId(_spawnResult.Value.HeliNetId))
                    {
                        heli = NetToVeh(_spawnResult.Value.HeliNetId);
                    }
                    if (NetworkDoesNetworkIdExist(_spawnResult.Value.PilotPedNetId) && NetworkDoesEntityExistWithNetworkId(_spawnResult.Value.PilotPedNetId))
                    {
                        pilot = NetToPed(_spawnResult.Value.PilotPedNetId);
                    }

                    if (pilot.HasValue)
                    {
                        ClearPedTasks(pilot.Value);
                    }

                    if(heli.HasValue)
                    {
                        ClearVehicleTasks(heli.Value);
                        SetEntityVelocity(heli.Value, 0f, 0f, 0f);
                        SetEntityAngularVelocity(heli.Value, 0f, 0f, 0f);

                        if(_currentTarget.HasValue)
                        {
                            SetEntityCoords(heli.Value, _currentTarget.Value.X, _currentTarget.Value.Y, _currentTarget.Value.Z, false, false, false, false);
                            SetEntityHeading(heli.Value, _currentTarget.Value.W);

                            FreezeEntityPosition(heli.Value, true);
                        }
                    }
                }
            }
        }
    }
}
