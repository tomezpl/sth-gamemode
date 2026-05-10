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

        private ShipPedSpawnHelper.Spawner PedSpawner = null;
        private bool _hasStartedSpawningPeds = false;

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
            if(!_hasStartedSpawningPeds && GameState?.Hunt != null && GameState.Hunt.HuntedPlayers[0].PlayerHandle == PlayerId())
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
                PedSpawner = ShipPedSpawnHelper.CreateSpawner(Constants.CruisegoerSpawns.Required, randomOrderedOptionalSpawners);
                PedSpawner.SpawningCompleted += OnPedsSpawned;
            }

            if(PedSpawner != null)
            {
                PedSpawner.Tick(deltaTime);
            }
        }

        private void OnPedsSpawned(int[] entityHandles)
        {
            Debug.WriteLine($"{nameof(OnPedsSpawned)}: spawned {entityHandles.Length} peds");

            List<object> netIds = new List<object>(entityHandles.Length);

            foreach(int pedHandle in entityHandles)
            {
                netIds.Add(PedToNet(pedHandle));
            }

            _state.PedNetIds = netIds;

            PedSpawner = null;
        }

        private void TickActive(float deltaTime)
        {

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
