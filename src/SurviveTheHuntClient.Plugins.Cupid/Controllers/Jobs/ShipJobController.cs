using CitizenFX.Core;
using SurviveTheHuntClient.Plugins.Cupid.Helpers;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs
{
    internal sealed class ShipJobController : JobControllerBase
    {
        private class JobState : JobStateBase
        {
            internal delegate void PedNetIdsChangedEvent(int[] netIds, bool canSync);

            internal event PedNetIdsChangedEvent PedNetIdsChanged;

            private List<object> _pedNetIds = new List<object>(ShipPedSpawnHelper.MaxPeds);
            internal List<object> PedNetIds
            {
                get => _pedNetIds;
                set
                {
                    string logPrefix = $"{nameof(PedNetIds)}.set: ";
                    bool isDifferent = _pedNetIds.Count != value.Count;
                    if (isDifferent)
                    {
                        Debug.WriteLine($"{logPrefix}{nameof(isDifferent)} = true because {nameof(value)} has {value.Count} elements, {nameof(_pedNetIds)} has {_pedNetIds.Count}");
                    }
                    int commonSize = Math.Min(value.Count, _pedNetIds.Count);
                    for(int i = 0; !isDifferent && i < commonSize; i++)
                    {
                        isDifferent = (int)_pedNetIds[i] != (int)value[i];
                        if(isDifferent)
                        {
                            Debug.WriteLine($"{logPrefix}{nameof(isDifferent)} = true because {nameof(value)}[{i}] == {(int)value[i]} and {nameof(_pedNetIds)}[{i}] == {(int)_pedNetIds[i]}");
                        }
                    }

                    if(isDifferent)
                    {
                        _pedNetIds = value;
                        int[] netIds = new int[value.Count];
                        for(int i = 0; i < netIds.Length; i++)
                        {
                            netIds[i] = (int)value[i];
                        }

                        PedNetIdsChanged.Invoke(netIds, CanSync);
                    }
                }
            }

            internal enum StateProp
            {
                PedNetIds
            }

            internal override Dictionary<int, object> Get()
            {
                return new Dictionary<int, object>
                {
                    {(int)StateProp.PedNetIds, Get((int)StateProp.PedNetIds) }
                };
            }

            internal override object Get(int statePropId)
            {
                switch((StateProp)statePropId)
                {
                    case StateProp.PedNetIds:
                        return PedNetIds;
                    default:
                        throw new ArgumentException($"Needs to be a valid {nameof(StateProp)}", nameof(statePropId));
                }
            }

            internal override void SetImpl(int statePropId, object statePropValue)
            {
                switch((StateProp)statePropId)
                {
                    case StateProp.PedNetIds:
                        PedNetIds = (List<object>)statePropValue;
                        break;
                }
            }
        }

        private JobState _state = new JobState();

        protected override JobStateBase State => _state;

        internal override ushort HeatValue => (ushort)Constants.HeatValues.High;

        internal override bool IsInTrigger => _wasPlayerInRangeLastTick;

        private Vector3 _origin = new Vector3(-2053.477f, -1028.463f, 11.90758f);

        internal Vector3 Origin => _origin;

        internal const float ActiveRadius = 55f;

        internal const float RangeCheckIntervalSeconds = 0.45f;

        private float _timeSinceLastRangeCheck = 0f;

        private bool _wasPlayerInRangeLastTick = false;

        private int[] _pedNetIds = new int[0];
        private int[] _pedHandles = new int[0];
        private bool _pedsNeedSyncing = false;

        private ShipPedSpawnHelper.Spawner _pedSpawner = null;
        private bool _hasStartedSpawningPeds = false;

        internal bool IsLocalPlayerPedGod => GameState?.Hunt != null && GameState.Hunt.HuntedPlayers[0].PlayerHandle == PlayerId();

        private PedSpawnInfo[] _pedWanderNodes = new PedSpawnInfo[0];
        private Action<float>[] _pedTicks = new Action<float>[0];
        private Dictionary<int, PedSpawnInfo> _optionalPedInitStates = new Dictionary<int, PedSpawnInfo>();
        private Dictionary<int, PedSpawnInfo> _optionalPedTargetStates = new Dictionary<int, PedSpawnInfo>();
        private PedSpawnInfo[] _pedLocations = new PedSpawnInfo[0];
        private int[] _optionalPedHandles = new int[0];

        internal const float PedBrainTickIntervalSeconds = 20f;

        private readonly static Random s_RNG = new Random();

        internal ShipJobController(JobStateRpcUpdateDelegate updateJobStateRpc) : base("ship", updateJobStateRpc)
        {
            _state.PedNetIdsChanged += OnPedNetIdsChanged;
        }

        private void OnPedNetIdsChanged(int[] netIds, bool canSync)
        {
            _pedNetIds = netIds;
            _pedsNeedSyncing = true;

            if(canSync)
            {
                SyncState((int)JobState.StateProp.PedNetIds);
            }
        }

        private void OnPedsSynced()
        {
        }

        public override void Tick(float deltaTime)
        {
            TickAmbient(deltaTime);

            if(!IsActive)
            {
                return;
            }

            TickActive(deltaTime);
        }

        private void TickAmbient(float deltaTime)
        {
            _timeSinceLastRangeCheck += deltaTime;

            if(_timeSinceLastRangeCheck >= RangeCheckIntervalSeconds)
            {
                _wasPlayerInRangeLastTick = GetEntityCoords(PlayerPedId(), false).DistanceToSquared(Origin) <= (ActiveRadius * ActiveRadius);
                _timeSinceLastRangeCheck = 0f;
            }

            if (_pedsNeedSyncing)
            {
                int[] pedHandles = new int[_pedNetIds.Length];
                bool allSynced = true;
                for (int i = 0; allSynced && i < _pedNetIds.Length; i++)
                {
                    allSynced = NetworkDoesEntityExistWithNetworkId(_pedNetIds[i]);
                    if(allSynced)
                    {
                        pedHandles[i] = NetToPed(_pedNetIds[i]);
                    }
                }

                if(allSynced)
                {
                    _pedsNeedSyncing = false;
                    _pedHandles = pedHandles;
                    OnPedsSynced();
                }
            }

            // Only the first player should spawn
            if(IsActive && !_hasStartedSpawningPeds && IsLocalPlayerPedGod)
            {
                _hasStartedSpawningPeds = true;

                CruisegoerSpawnBase[] randomOrderedOptionalSpawners = new CruisegoerSpawnBase[Constants.CruisegoerSpawns.Optional.Length];
                List<CruisegoerSpawnBase> toAdd = new List<CruisegoerSpawnBase>(Constants.CruisegoerSpawns.Optional);
                Random rng = new Random();
                for(int i = 0; i < randomOrderedOptionalSpawners.Length; i++)
                {
                    int randomIndex = rng.Next(0, toAdd.Count);
                    randomOrderedOptionalSpawners[i] = toAdd[randomIndex];
                    toAdd.RemoveAt(randomIndex);
                }
                _pedSpawner = ShipPedSpawnHelper.CreateSpawner(Constants.CruisegoerSpawns.Required, randomOrderedOptionalSpawners);
                _pedSpawner.SpawningCompleted += OnPedsSpawned;
            }

            if(_pedSpawner != null)
            {
                _pedSpawner.Tick(deltaTime);
            }
        }

        private void OnPedsSpawned(int[] entityHandles, Dictionary<int, PedSpawnInfo> optionalPedInitStates)
        {
            Debug.WriteLine($"{nameof(OnPedsSpawned)}: spawned {entityHandles.Length} peds");

            List<object> netIds = new List<object>(entityHandles.Length);

            foreach(int pedHandle in entityHandles)
            {
                netIds.Add(PedToNet(pedHandle));
            }

            _state.PedNetIds = netIds;

            _pedSpawner = null;

            _optionalPedInitStates = optionalPedInitStates;
            _pedLocations = new PedSpawnInfo[optionalPedInitStates.Count];
            optionalPedInitStates.Values.CopyTo(_pedLocations, 0);
            _optionalPedHandles = new int[optionalPedInitStates.Count];
            optionalPedInitStates.Keys.CopyTo(_optionalPedHandles, 0);
        }

        private void TickActive(float deltaTime)
        {
            if(IsLocalPlayerPedGod)
            {
                RunPedBrain(deltaTime);
            }
        }

        private float _timeSinceLastPedBrainTick = 0f;
        private float _timeSinceLastPedTargetCheck = 0f;
        private const float PedTargetCheckIntervalSeconds = 1.5f;

        private void RunPedBrain(float deltaTime)
        {
            if(_pedHandles != null)
            {
                const float WarpDistance = 0.9f;

                if (_timeSinceLastPedBrainTick >= PedBrainTickIntervalSeconds)
                {
                    Debug.WriteLine($"{nameof(RunPedBrain)}: resetting peds' tasks");

                    List<PedSpawnInfo> pickablePedTargets = new List<PedSpawnInfo>(_pedLocations);

                    for (int i = 0; i < _optionalPedHandles.Length; i++)
                    {
                        int ped = _optionalPedHandles[i];

                        // Stop the previous task
                        ClearPedTasks(ped);
                        PedSpawnInfo? initState = null;
                        if(_optionalPedInitStates.ContainsKey(ped))
                        {
                            initState = _optionalPedInitStates[ped];
                        }

                        // Check if the ped's current state requires them to be warped
                        // if so, warp back: use the heading to compute the back vector
                        if (initState?.NeedsWarp == true)
                        {
                            PedSpawnInfo.PositionInfo pos = initState.Value.Position;
                            ComputeDirVecFromHeading2D(pos.Heading, out float backX, out float backY);

                            SetEntityCoords(ped, pos.X + backX * WarpDistance, pos.Y + backY * WarpDistance, pos.Z, false, false, false, false);
                        }

                        // TODO: there's a chance we might pick the same target as current...
                        int randomNewTargetIndex = s_RNG.Next(0, pickablePedTargets.Count);
                        PedSpawnInfo newTarget = pickablePedTargets[randomNewTargetIndex];

                        float offsetX = 0f, offsetY = 0f;
                        // Don't allow other peds wander to a warpable spot
                        if (newTarget.NeedsWarp)
                        {
                            pickablePedTargets.RemoveAt(randomNewTargetIndex);

                            ComputeDirVecFromHeading2D(newTarget.Position.Heading, out offsetX, out offsetY);

                            offsetX *= WarpDistance;
                            offsetY *= WarpDistance;
                        }

                        // fucking CFX types define flags as a bool?
                        //CitizenFX.Core.Native.Function.Call(CitizenFX.Core.Native.Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, newTarget.Position.X + offsetX, newTarget.Position.Y + offsetY, newTarget.Position.Z, 1f, 500, 0.2f, 1 | 2, newTarget.Position.Heading);
                        //TaskFollowNavMeshToCoord(ped, newTarget.Position.X + offsetX, newTarget.Position.Y + offsetY, newTarget.Position.Z, 1f, 500, 0.2f, false, newTarget.Position.Heading);
                        TaskGoToCoordAnyMeans(ped, newTarget.Position.X + offsetX, newTarget.Position.Y + offsetY, newTarget.Position.Z, 1f, 0, false, 0, 0.01f);

                        _optionalPedTargetStates[ped] = newTarget;

                        _optionalPedInitStates.Remove(ped);

                        _timeSinceLastPedBrainTick = 0f;
                    }
                }
                else
                {
                    if (_timeSinceLastPedTargetCheck >= PedTargetCheckIntervalSeconds)
                    {
                        _timeSinceLastPedTargetCheck = 0f;

                        foreach (int ped in _optionalPedHandles)
                        {
                            if (_optionalPedTargetStates.ContainsKey(ped))
                            {
                                PedSpawnInfo target = _optionalPedTargetStates[ped];
                                Vector3 currentPos = GetEntityCoords(ped, false);
                                float distanceToTarget = currentPos.DistanceToSquared(new Vector3(target.Position.X, target.Position.Y, target.Position.Z));
                                bool hasAchieved = distanceToTarget < (WarpDistance * WarpDistance);
                                const float AlmostExtensionFactor = 1.75f;
                                const float ExtendedWarpDistance = AlmostExtensionFactor * WarpDistance;
                                bool hasAlmostAchieved = hasAchieved || (distanceToTarget < (ExtendedWarpDistance * ExtendedWarpDistance));

                                if (hasAlmostAchieved)
                                {
                                    if (hasAchieved || target.NeedsWarp)
                                    {
                                        Debug.WriteLine($"{nameof(RunPedBrain)}: ped {ped} has achieved their target at X = {target.Position.X}, Y = {target.Position.Y}, Z = {target.Position.Z}");

                                        ClearPedTasks(ped);
                                        _optionalPedTargetStates.Remove(ped);
                                        _optionalPedInitStates[ped] = target;
                                    }

                                    if (target.NeedsWarp)
                                    {
                                        SetEntityCoords(ped, target.Position.X, target.Position.Y, target.Position.Z, false, false, false, false);
                                        SetEntityHeading(ped, target.Position.Heading);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _timeSinceLastPedBrainTick += deltaTime;
            _timeSinceLastPedTargetCheck += deltaTime;
        }

        private static void ComputeDirVecFromHeading2D(float heading, out float dirX, out float dirY)
        {
            const float Deg2Rad = (float)(Math.PI / 180.0);
            float headingRad = heading * Deg2Rad;
            dirX = (float)Math.Sin(headingRad);
            dirY = (float)Math.Cos(headingRad) * -1f;
        }

        internal override void Cleanup(bool force = false)
        {
            for(int i = 0; i < _pedHandles.Length; i++)
            {
                SetEntityAsMissionEntity(_pedHandles[i], true, true);
                DeletePed(ref _pedHandles[i]);
            }
        }
    }
}
