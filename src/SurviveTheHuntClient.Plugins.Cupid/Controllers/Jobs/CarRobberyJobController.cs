using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using SurviveTheHuntClient.Plugins.Cupid.Utils;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs
{
    internal sealed class CarRobberyJobController : JobControllerBase
    {
        private sealed class JobState : JobStateBase
        {
            private bool _hasCompletedBonus = false;

            internal event GenericStateChangedEvent<bool> OnHasCompletedBonusChanged;
            /// <summary>
            /// True if the bonus objective was completed.
            /// </summary>
            internal bool HasCompletedBonus
            {
                get => _hasCompletedBonus;
                set
                {
                    if (_hasCompletedBonus != value)
                    {
                        bool old = _hasCompletedBonus;
                        _hasCompletedBonus = value;
                        OnHasCompletedBonusChanged.Invoke(old, value, CanSync);
                    }
                }
            }

            internal event GenericStateChangedEvent<int> OnCarNetIdChanged;
            private int _carNetId = 0;
            internal int CarNetId
            {
                get => _carNetId;
                set
                {
                    if(_carNetId != value)
                    {
                        int old = _carNetId;
                        _carNetId = value;
                        OnCarNetIdChanged.Invoke(old, value, CanSync);
                    }
                }
            }

            internal event GenericStateChangedEvent<int> OnTruckNetIdChanged;
            private int _truckNetId = 0;
            internal int TruckNetId
            {
                get => _truckNetId;
                set
                {
                    if (_truckNetId != value)
                    {
                        int old = _truckNetId;
                        _truckNetId = value;
                        OnTruckNetIdChanged.Invoke(old, value, CanSync);
                    }
                }
            }

            internal event GenericStateChangedEvent<JobStage> OnStageChanged;
            private JobStage _stage = JobStage.WaitingToStart;
            internal JobStage Stage
            {
                get => _stage;
                set
                {
                    if (_stage != value)
                    {
                        JobStage old = _stage;
                        _stage = value;
                        OnStageChanged.Invoke(old, value, CanSync);
                    }
                }
            }

            internal enum JobStage
            {
                WaitingToStart,
                ShootingDoors,
                PreparingJump,
                Jumping,
                WaitingToDriveOut,
                InitialObjectiveDone,
                WaitingToDeliver
            }

            internal enum StateProp
            {
                HasCompletedBonus,
                CarNetId,
                TruckNetId,
                Stage,
            }

            internal override sealed Dictionary<int, object> Get()
            {
                return new Dictionary<int, object>()
                {
                    {(int)StateProp.HasCompletedBonus, Get((int)StateProp.HasCompletedBonus) },
                    {(int)StateProp.CarNetId, Get((int)StateProp.CarNetId) },
                    {(int)StateProp.TruckNetId, Get((int)StateProp.TruckNetId) },
                    {(int)StateProp.Stage, Get((int)StateProp.Stage) }
                };
            }

            internal override sealed object Get(int statePropId)
            {
                switch(statePropId)
                {
                    case (int)StateProp.HasCompletedBonus:
                        return HasCompletedBonus;
                    case (int)StateProp.TruckNetId:
                        return TruckNetId;
                    case (int)StateProp.CarNetId:
                        return CarNetId;
                    case (int)StateProp.Stage:
                        return Stage;
                }

                throw new ArgumentException($"{nameof(statePropId)} is not a valid {nameof(SimpleRobberyJobController)}.{nameof(JobState)}.{nameof(StateProp)}");
            }

            internal override sealed void SetImpl(int statePropId, object statePropValue)
            {
                switch((StateProp)statePropId)
                {
                    case StateProp.HasCompletedBonus:
                        HasCompletedBonus = (bool)statePropValue;
                        break;
                    case StateProp.CarNetId:
                        CarNetId = (int)statePropValue;
                        break;
                    case StateProp.TruckNetId:
                        TruckNetId = (int)statePropValue;
                        break;
                    case StateProp.Stage:
                        Stage = (JobStage)statePropValue;
                        break;
                }
            }
        }

        private JobState _state = new JobState();

        protected override sealed JobStateBase State => _state;

        internal override ushort HeatValue => !_state.HasCompletedBonus
            ? (ushort)Constants.HeatValues.Medium
            : (ushort)Constants.HeatValues.MediumBonus;

        // screw testing on every frame - we can check distance every like 0.5s or something
        internal override bool IsInTrigger => !_state.HasCompletedBonus && _isInArea;

        private Vector3 _spawnPos;
        private readonly float _spawnHeading;

        private bool _waitingToSpawnTruck = false;
        private static readonly uint s_truckModel = (uint)GetHashKey("pounder2");
        private static readonly uint[] s_carModels = { (uint)GetHashKey("torero"), (uint)GetHashKey("tempesta") };

        internal readonly uint CarModel;

        private static readonly Random s_RNG = new Random();

        internal const float TruckCarOffsetX = 0f, TruckCarOffsetY = -4.75f, TruckCarOffsetZ = 0.85f;

        internal bool CarTruckSpawned => _carHandle != 0 && _truckHandle != 0;

        private int _carHandle = 0, _truckHandle = 0;

        private int _carBlip = 0;
        private int _carDeliveryBlip = 0;

        internal const int CarDestinationBlipSprite = 780;

        private bool _isInArea = false;

        internal const float ActiveRadius = 15f;
        internal const float ActiveRadiusSq = ActiveRadius * ActiveRadius;

        internal const float HitRadius = 2.5f;

        private bool _isPendingCarTruckSync = false;

        private float _timeElapsedInCurrentStage = 0f;

        private const string JumpHelpTextKey = "STH_CUPID_CARJOB_JUMP_HELP";
        private const string JumpHelpTextString = "Press ~INPUT_CONTEXT~ to jump to the car.";

        private const string DriveOutHelpTextKey = "STH_CUPID_CARJOB_DRIVEOUT_HELP";
        private const string DriveOutHelpTextString = "Press ~INPUT_VEH_BRAKE~ to drive out.";

        private const string BonusObjectiveTextKey = "STH_CUPID_CARJOB_BONUS_OBJ";
        private const string BonusObjectiveTextString = "Deliver the car to ~y~Terminal, LS~w~ for bonus heat.";

        private readonly static bool s_HasInit = InitShared();

        private const int DeliveryMarkerColourR = 224, DeliveryMarkerColourG = 210, DeliveryMarkerColourB = 15, DeliveryMarkerColourA = 128;

        internal sealed override bool BlockOtherJobs => false;

        internal CarRobberyJobController(string id, JobStateRpcUpdateDelegate updateJobState, Vector3 startPos, float startHeading) : base(id, updateJobState)
        {
            _spawnPos = startPos;
            _spawnHeading = startHeading;
            CarModel = s_carModels[s_RNG.Next(0, s_carModels.Length)];

            _state.OnTruckNetIdChanged += OnTruckNetIdChanged;
            _state.OnCarNetIdChanged += OnCarNetIdChanged;
            _state.OnStageChanged += OnStageChanged;
            _state.OnHasCompletedBonusChanged += OnHasCompletedBonusChanged;
        }

        private static bool InitShared()
        {
            if(!s_HasInit)
            {
                AddTextEntry(JumpHelpTextKey, JumpHelpTextString);
                AddTextEntry(DriveOutHelpTextKey, DriveOutHelpTextString);
                AddTextEntry(BonusObjectiveTextKey, BonusObjectiveTextString);

                return true;
            }

            return false;
        }

        private void OnTruckNetIdChanged(int prev, int current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.TruckNetId, current);
            }

            _isPendingCarTruckSync = true;
        }

        private void OnCarNetIdChanged(int prev, int current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.CarNetId, current);
            }

            _isPendingCarTruckSync = true;
        }

        private void OnStageChanged(JobState.JobStage prev, JobState.JobStage current, bool canSync)
        {
            _timeElapsedInCurrentStage = 0f;

            if(canSync)
            {
                SyncState((int)JobState.StateProp.Stage, current);
            }

            if(current == JobState.JobStage.ShootingDoors + 1)
            {
                VehicleDoorIndex[] doorsToDestroy = { VehicleDoorIndex.BackLeftDoor, VehicleDoorIndex.BackRightDoor };
                foreach (VehicleDoorIndex doorIndex in doorsToDestroy)
                {
                    SetVehicleDoorOpen(_truckHandle, (int)doorIndex, false, false);
                }
            }

            if(GameState?.Hunt != null && GameState.Hunt.IsHunted(PlayerId(), out HuntPlayer? _))
            {
                if(current == JobState.JobStage.PreparingJump)
                {
                    BeginTextCommandDisplayHelp(JumpHelpTextKey);
                    EndTextCommandDisplayHelp(0, false, true, 5000);
                }

                if(current == JobState.JobStage.WaitingToDriveOut)
                {
                    BeginTextCommandDisplayHelp(DriveOutHelpTextKey);
                    EndTextCommandDisplayHelp(0, true, true, 0);
                }
            }

            if (current == JobState.JobStage.InitialObjectiveDone)
            {
                // Award the initial heat value
                OnJobFinished();

                _state.Stage = JobState.JobStage.InitialObjectiveDone + 1;
            }
        }

        private void OnHasCompletedBonusChanged(bool prev, bool current, bool canSync)
        {
            if (canSync)
            {
                SyncState((int)JobState.StateProp.HasCompletedBonus, current);
            }

            // Award the bonus heat value
            if(current)
            {
                OnJobFinished();

                // make all peds in the car exit
                int playerPed = PlayerPedId();
                if(IsPedInVehicle(playerPed, _carHandle, true))
                {
                    TaskLeaveVehicle(playerPed, _carHandle, 0);
                }

                // Hide the blips
                SetBlipDisplay(_carDeliveryBlip, 0);
                SetBlipDisplay(_carBlip, 0);

                HUDUtils.ClearObjective();
            }
        }

        private void OnCarTruckSpawned()
        {
            _carBlip = CreateCarBlip(_carHandle);
        }

        private static int CreateCarBlip(int carHandle)
        {
            int blip = AddBlipForEntity(carHandle);

            SetBlipDisplay(blip, 6);
            // radar_export_vehicle
            SetBlipSprite(blip, 794);

            return blip;
        }

        private static int CreateCarDeliveryBlip()
        {
            int blip = AddBlipForCoord(Constants.Location.CarRobberyJob.DeliveryPosX, Constants.Location.CarRobberyJob.DeliveryPosY, Constants.Location.CarRobberyJob.DeliveryPosZ);

            SetBlipSprite(blip, CarDestinationBlipSprite);
            SetBlipDisplay(blip, 0);

            return blip;
        }

        internal override sealed void Start(IPlayerState playerState, IGameState gameState)
        {
            base.Start(playerState, gameState);

            if(gameState?.Hunt?.HuntedPlayers != null && playerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
            {
                // Make the first hunted player spawn the truck as it needs to be spawned once and just net synced.
                if (gameState.Hunt.HuntedPlayers[0].PlayerHandle == PlayerId())
                {
                    _waitingToSpawnTruck = true;
                }
            }

            _carDeliveryBlip = CreateCarDeliveryBlip();
        }

        public override sealed void Tick(float deltaTime)
        {
            TickAmbient(deltaTime);

            if(!IsActive)
            {
                return;
            }

            TickActive(deltaTime);
        }

        private const float TriggerDistanceCheckInterval = .5f;
        private float _timeSinceLastTriggerDistanceCheck = 0f;

        private void TickAmbient(float deltaTime)
        {
            _timeElapsedInCurrentStage += deltaTime;

            if(_waitingToSpawnTruck)
            {
                RequestModel(s_truckModel);
                RequestModel(CarModel);

                CarTruckSpawnResult spawnResult = TrySpawnCarAndTruck(_spawnPos, _spawnHeading, CarModel, s_truckModel);
                if (spawnResult.Success)
                {
                    _waitingToSpawnTruck = false;

                    _state.CarNetId = VehToNet(spawnResult.CarHandle);
                    _state.TruckNetId = VehToNet(spawnResult.TruckHandle);
                }
            }

            _timeSinceLastTriggerDistanceCheck += deltaTime;
            if(_timeSinceLastTriggerDistanceCheck >= TriggerDistanceCheckInterval)
            {
                _timeSinceLastTriggerDistanceCheck = 0f;

                int playerId = PlayerId();
                if (CarTruckSpawned && GameState?.Hunt != null && GameState.Hunt.IsHunted(playerId, out HuntPlayer? _))
                {
                    Vector3 playerPos = GetEntityCoords(GetPlayerPed(playerId), false);
                    float distSq = playerPos.DistanceToSquared(GetEntityCoords(_carHandle, false));

                    _isInArea = distSq < ActiveRadiusSq;
                    
                }
            }

            // Net IDs won't be synced instantly, so keep trying to look up vehicles from net IDs over several ticks
            if(_isPendingCarTruckSync)
            {
                if(_carHandle == 0 && NetworkDoesEntityExistWithNetworkId(_state.CarNetId))
                {
                    _carHandle = NetToVeh(_state.CarNetId);
                }

                if(_truckHandle == 0 && NetworkDoesEntityExistWithNetworkId(_state.TruckNetId))
                {
                    _truckHandle = NetToVeh(_state.TruckNetId);
                }

                if(CarTruckSpawned)
                {
                    _isPendingCarTruckSync = false;
                    OnCarTruckSpawned();
                }
            }
        }

        private struct CarTruckSpawnResult
        {
            internal bool Success;
            internal int CarHandle;
            internal int TruckHandle;
        }

        /// <summary>
        /// Tries to spawn the truck and the car inside it. Will return false if any of the models has not loaded yet.
        /// The car and truck have collision disabled and are invicible to begin with. It's the controller's responsibility to disable this later.
        /// </summary>
        /// <param name="truckSpawnPos"></param>
        /// <param name="truckSpawnHeading"></param>
        /// <param name="carModel"></param>
        /// <param name="truckModel"></param>
        /// <param name="carHandle"></param>
        /// <param name="truckHandle"></param>
        /// <returns></returns>
        private CarTruckSpawnResult TrySpawnCarAndTruck(Vector3 truckSpawnPos, float truckSpawnHeading, uint carModel, uint truckModel)
        {
            int carHandle = 0, truckHandle = 0;
            bool success = false;

            if(HasModelLoaded(carModel) && HasModelLoaded(truckModel))
            {
                
                float baseX = truckSpawnPos.X, baseY = truckSpawnPos.Y, baseZ = truckSpawnPos.Z;

                truckHandle = CreateVehicle(truckModel, baseX, baseY, baseZ, truckSpawnHeading, true, true);
                Vector3 up = Vector3.Up;
                Vector3 forward = GetEntityForwardVector(truckHandle), right = Vector3.Cross(forward, up);

                float 
                    offsetX = (forward.X * TruckCarOffsetY) + (right.X * TruckCarOffsetX) + (up.X * TruckCarOffsetZ),
                    offsetY = (forward.Y * TruckCarOffsetY) + (right.Y * TruckCarOffsetX) + (up.Y * TruckCarOffsetZ),
                    offsetZ = (forward.Z * TruckCarOffsetY) + (right.Z * TruckCarOffsetX) + (up.Z * TruckCarOffsetZ);

                carHandle = CreateVehicle(carModel, baseX + offsetX, baseY + offsetY, baseZ + offsetZ, truckSpawnHeading, true, true);

                SetEntityNoCollisionEntity(carHandle, truckHandle, false);
                SetEntityInvincible(truckHandle, true);
                SetEntityInvincible(carHandle, true);
                AttachEntityToEntity(carHandle, truckHandle, 0, TruckCarOffsetX, TruckCarOffsetY, TruckCarOffsetZ, 0f, 0f, 0f, false, false, false, false, 2, true);
                
                success = true;
            }

            return new CarTruckSpawnResult { Success = success, CarHandle = carHandle, TruckHandle = truckHandle };
        }

        private bool GetTruckDoorCoord(out Vector3 coord)
        {
            if(_truckHandle == 0)
            {
                coord = Vector3.Zero;
                return false;
            }


            const float doorOffset = 6.5f;
            const float vertOffset = 1.1f;
            Vector3 truckPos = GetEntityCoords(_truckHandle, false);
            Vector3 fwdVec = GetEntityForwardVector(_truckHandle);
            coord = truckPos - (fwdVec * doorOffset);
            coord.Z += vertOffset;
            return true;
        }

        private bool _playerInitiatedJump = false;

        private bool _wasDrivingCarLastTick = false;
        private bool _wasCarInDeliveryTargetLastTick = false;

        private void OnCarEnteredOrExited(bool entered)
        {
            if (!_state.HasCompletedBonus)
            {
                SetBlipDisplay(_carBlip, entered ? 0 : 6);
                SetBlipDisplay(_carDeliveryBlip, entered ? 6 : 0);

                if (entered)
                {
                    SetBlipFlashTimer(_carDeliveryBlip, 4000);

                    BeginTextCommandPrint(BonusObjectiveTextKey);
                    EndTextCommandPrint((int)(GameState.Hunt.ActualEndTime - DateTime.UtcNow).TotalMilliseconds, true);
                }
            }

            if(!entered)
            {
                HUDUtils.ClearObjective();
            }
        }

        private void TickActive(float deltaTime)
        {
            int playerPed = PlayerPedId();

            if(_state.Stage == JobState.JobStage.WaitingToDeliver && !_state.HasCompletedBonus)
            {
                bool isDrivingCar = GetVehiclePedIsIn(playerPed, false) == _carHandle;

                if(isDrivingCar != _wasDrivingCarLastTick)
                {
                    OnCarEnteredOrExited(isDrivingCar);
                    _wasDrivingCarLastTick = isDrivingCar;
                }

                if(isDrivingCar)
                {
                    DrawMarker((int)MarkerType.VerticalCylinder, Constants.Location.CarRobberyJob.DeliveryPosX, Constants.Location.CarRobberyJob.DeliveryPosY, Constants.Location.CarRobberyJob.DeliveryPosZ, 0f, 0f, 0f, 0f, 0f, 0f, Constants.Location.CarRobberyJob.DeliveryRadius, Constants.Location.CarRobberyJob.DeliveryRadius, Constants.Location.CarRobberyJob.DeliveryRadius, DeliveryMarkerColourR, DeliveryMarkerColourG, DeliveryMarkerColourB, DeliveryMarkerColourA, false, false, 2, false, null, null, false);
                }

                const float radiusSq = Constants.Location.CarRobberyJob.DeliveryRadius * Constants.Location.CarRobberyJob.DeliveryRadius;
                bool isCarInDeliveryTarget = GetEntityCoords(_carHandle, false).DistanceToSquared(Constants.Location.CarRobberyJob.DeliveryPos) <= radiusSq;
                if(isCarInDeliveryTarget)
                {
                    if(isCarInDeliveryTarget != _wasCarInDeliveryTargetLastTick)
                    {
                        _timeElapsedInCurrentStage = 0f;
                    }

                    // After 1s in the delivery target, turn off the engine, remove all peds from the car and lock it.
                    if(_timeElapsedInCurrentStage > 1f)
                    {
                        SetVehicleEngineOn(_carHandle, false, true, true);
                        SetVehicleDoorsLockedForAllPlayers(_carHandle, true);
                        _state.HasCompletedBonus = true;
                    }
                }

                _wasCarInDeliveryTargetLastTick = isCarInDeliveryTarget;
            }

            if(_state.Stage == JobState.JobStage.WaitingToDriveOut)
            {
                if (_playerInitiatedJump)
                {
                    if (IsControlJustPressed(0, (int)Control.VehicleBrake))
                    {
                        const float impulse = -35f;
                        Vector3 forward = GetEntityForwardVector(_carHandle);
                        DetachEntity(_carHandle, true, false);
                        ApplyForceToEntity(_carHandle, 1, forward.X * impulse, forward.Y * impulse, forward.Z * impulse, 0f, 0f, 0f, 0, false, false, true, true, true);
                        _timeElapsedInCurrentStage = 0f;
                        _playerInitiatedJump = false;
                        ClearAllHelpMessages();
                    }
                }

                if (_timeElapsedInCurrentStage >= 1f && !IsEntityAttached(_carHandle))
                {
                    SetEntityNoCollisionEntity(_carHandle, _truckHandle, true);
                    SetEntityCompletelyDisableCollision(_carHandle, true, true);
                    _state.Stage = JobState.JobStage.WaitingToDriveOut + 1;
                }
            }

            const float JumpTimeSeconds = 2f;

            if(_state.Stage == JobState.JobStage.Jumping)
            {
                if (_timeElapsedInCurrentStage >= JumpTimeSeconds && _playerInitiatedJump)
                {
                    _state.Stage = JobState.JobStage.Jumping + 1;
                    SetPedIntoVehicle(playerPed, _carHandle, -2);
                }
            }

            if(_state.Stage == JobState.JobStage.Jumping || _state.Stage == JobState.JobStage.PreparingJump)
            {

                // on foot
                if(!IsPedInAnyVehicle(playerPed, true))
                {
                    const float ExpensiveTestRadius = 2.5f;
                    const float ExpensiveTestRadiusSq = ExpensiveTestRadius * ExpensiveTestRadius;

                    // if we're "inside" the car then just warp into a seat and advance to next stage.
                    if(GetEntityCoords(playerPed, false).DistanceToSquared(GetEntityCoords(_carHandle, false)) < ExpensiveTestRadiusSq)
                    {
                        const float CarWidth = 1.5f;
                        const float CarLength = 4.5f;
                        const float CarHeight = 1.75f;

                        Vector3 fwdVec = GetEntityForwardVector(_carHandle);
                        Vector3 rightVec = Vector3.Cross(fwdVec, Vector3.Up);

                        Vector3 carOrigin = GetEntityCoords(_carHandle, false);
                        Vector3 offset = GetEntityCoords(playerPed, false) - carOrigin;
                        float distance = offset.Length();
                        Vector3 dir = offset / distance;

                        float localXOffset = distance * Vector3.Dot(rightVec, dir);
                        float localYOffset = distance * Vector3.Dot(fwdVec, dir);

                        /* Debug stuff 
                        Vector3 fullFrontLeftCorner = carOrigin + (fwdVec * CarLength * 0.5f) + (rightVec * CarWidth * -0.5f) + (Vector3.Up * CarHeight * 0.5f);
                        Vector3 fullRearRightCorner = carOrigin + (fwdVec * CarLength * -0.5f) + (rightVec * CarWidth * 0.5f) + (Vector3.Up * CarHeight * -0.5f);

                        Vector3 playerXMarkerPos = carOrigin + (fwdVec * CarLength * -0.5f) + (rightVec * localXOffset);
                        Vector3 playerYMarkerPos = carOrigin + (fwdVec * localYOffset) + (rightVec * CarWidth * -0.5f);

                        //DrawBox(fullFrontLeftCorner.X, fullFrontLeftCorner.Y, fullFrontLeftCorner.Z, fullRearRightCorner.X, fullRearRightCorner.Y, fullRearRightCorner.Z, 192, 100, 0, 128);
                        //const float debugRadius = 0.35f;
                        //DrawSphere(playerXMarkerPos.X, playerXMarkerPos.Y, playerXMarkerPos.Z, debugRadius, 255, 0, 0, 0.8f);
                        //DrawSphere(playerYMarkerPos.X, playerYMarkerPos.Y, playerYMarkerPos.Z, debugRadius, 255, 0, 0, 0.8f);
                        */

                        if (Math.Abs(localXOffset) < CarWidth * 0.5f && Math.Abs(localYOffset) < CarLength * 0.5f)
                        {
                            _state.Stage = JobState.JobStage.Jumping;
                            _timeElapsedInCurrentStage = JumpTimeSeconds;
                            _playerInitiatedJump = true;
                        }
                    }
                }
            }

            if(_state.Stage == JobState.JobStage.PreparingJump)
            {
                if(!_playerInitiatedJump && IsPedInAnyVehicle(PlayerPedId(), false) && IsControlJustPressed(0, (int)Control.Context))
                {
                    ClearAllHelpMessages();
                    _playerInitiatedJump = true;
                    _state.Stage = JobState.JobStage.Jumping;
                }
            }

            if (_state.Stage <= JobState.JobStage.ShootingDoors)
            {
                if (GetTruckDoorCoord(out Vector3 doorCoord))
                {
                    DrawMarker((int)MarkerType.DebugSphere, doorCoord.X, doorCoord.Y, doorCoord.Z, 0f, 0f, 0f, 0f, 0f, 0f, HitRadius, HitRadius, HitRadius, 255, 32, 32, 192, false, true, 2, false, null, null, false);
                    if (IsBulletInArea(doorCoord.X, doorCoord.Y, doorCoord.Z, HitRadius, false))
                    {
                        _state.Stage = JobState.JobStage.ShootingDoors + 1;
                    }
                }
            }
        }

        private static bool CanDeleteVehicle(in int handle, bool ignoreEmptyCheck = false)
        {
            bool exists = handle != 0 && DoesEntityExist(handle);
            if(exists)
            {
                if (!ignoreEmptyCheck)
                {
                    // Prevent the vehicle being removed if someone's driving it
                    foreach (int player in GetActivePlayers())
                    {
                        int playerPed = GetPlayerPed(player);
                        if (DoesEntityExist(playerPed))
                        {
                            if (GetVehiclePedIsIn(playerPed, false) == handle)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }

        internal override sealed void Cleanup(bool force = false)
        {
            if(CanDeleteVehicle(in _carHandle, ignoreEmptyCheck: force))
            {
                SetEntityAsMissionEntity(_carHandle, false, true);
                DeleteEntity(ref _carHandle);
            }

            if(CanDeleteVehicle(in _truckHandle, ignoreEmptyCheck: force))
            {
                SetEntityAsMissionEntity(_truckHandle, false, true);
                DeleteEntity(ref _truckHandle);
            }

            RemoveBlip(ref _carBlip);
            RemoveBlip(ref _carDeliveryBlip);
        }
    }
}
