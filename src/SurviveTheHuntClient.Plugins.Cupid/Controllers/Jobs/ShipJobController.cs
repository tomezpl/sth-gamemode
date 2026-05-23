using CitizenFX.Core;
using SurviveTheHuntClient.Models;
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

        private Dictionary<int, PedNode> _optionalPedInitStates = new Dictionary<int, PedNode>();
        private Dictionary<int, PedNode> _optionalPedTargetStates = new Dictionary<int, PedNode>();
        private PedNode[] _pedLocations = new PedNode[0];
        private int[] _optionalPedHandles = new int[0];

        private Dictionary<int, PedNode.AnimInfo> _animRequests = new Dictionary<int, PedNode.AnimInfo>();

        internal const float PedBrainTickIntervalSeconds = 20f;

        private readonly static Random s_RNG = new Random();

        /// <summary>
        /// Previous clothing components of the player ped that need to be restored when the player ends their current animation
        /// </summary>
        private Dictionary<PedComponents, PedVariation> _compsToRestore = new Dictionary<PedComponents, PedVariation>();

        /// <summary>
        /// Previous clothing props of the player ped that need to be restored when the player ends their current animation.
        /// </summary>
        private Dictionary<PedProps, PedVariation> _propsToRestore = new Dictionary<PedProps, PedVariation>();

        private static readonly PedNode[] s_ShowerNodes = FindShowerNodes();

        private const float WarpDistance = 0.9f;

        private const float AlmostExtensionFactor = 1.75f;
        private const float ExtendedWarpDistance = AlmostExtensionFactor * WarpDistance;

        private const string ShowerHelpTextKey = "STH_CUPID_SHIP_SHOWER_HELP";
        private const string ShowerHelpTextLabel = "Press ~INPUT_CONTEXT~ to shower.";

        private const string BlendInHelpTextKey = "STH_CUPID_SHIP_BLENDIN_HELP";
        private const string BlendInHelpTextLabel = "Press ~INPUT_CONTEXT~ to blend in.";

        private Dictionary<int, float> _timeTillPedBrainTick = new Dictionary<int, float>();

        private static readonly bool s_HasDoneInit = Init();

        private static bool Init()
        {
            if(!s_HasDoneInit)
            {
                AddTextEntry(ShowerHelpTextKey, ShowerHelpTextLabel);
                AddTextEntry(BlendInHelpTextKey, BlendInHelpTextLabel);
            }

            return true;
        }

        private static bool IsPedAMaleModel(uint pedModel)
        {
            bool isMale = pedModel == (uint)PedHash.FreemodeMale01;
            if (!isMale && pedModel != (uint)PedHash.FreemodeFemale01)
            {
                foreach (uint model in Constants.CruisegoerSpawns.MalePedModels)
                {
                    if (model == pedModel)
                    {
                        isMale = true;
                        break;
                    }
                }
            }
            return isMale;
        }

        private static PedNode[] FindShowerNodes()
        {
            List<PedNode> showerNodes = new List<PedNode>(Constants.CruisegoerSpawns.Optional.Length);

            foreach(CruisegoerSpawnBase spawn in Constants.CruisegoerSpawns.Optional)
            {
                // Shower spots are only single spawns
                if(spawn is CruisegoerSpawnSingle)
                {
                    PedNode node = ((CruisegoerSpawnSingle)spawn).Build()[0];
                    if(node.HasFlag(PedNode.PedNodeFlag.Shower))
                    {
                        showerNodes.Add(node);
                    }
                }
            }

            return showerNodes.ToArray();
        }
        
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

        private int? _blendInSourcePed = null;
        private bool _isBlendingIn = false;
        private float _timeSinceBlendInCheck = 0f;
        private const float BlendInCheckIntervalSeconds = 0.65f;
        private void HandleBlendIn(float deltaTime)
        {
            _timeSinceBlendInCheck += deltaTime;

            bool couldStartBlendInBeforeCheck = !_isBlendingIn && _blendInSourcePed.HasValue;

            if(!_isBlendingIn && _timeSinceBlendInCheck >= BlendInCheckIntervalSeconds)
            {
                _timeSinceBlendInCheck = 0f;
                _blendInSourcePed = null;

                const float BlendInDistance = 1.2f;
                const float BlendInDistanceSq = BlendInDistance * BlendInDistance;

                Vector3 playerPos = GetEntityCoords(PlayerPedId(), false);

                float closestDist = float.MaxValue;
                // Need to iterate over _pedHandles as only that's synced
                foreach(int ped in _pedHandles)
                {
                    if (IsPedStill(ped))
                    {
                        Vector3 pos = GetEntityCoords(ped, false);
                        float
                            a = pos.X - playerPos.X,
                            b = pos.Y - playerPos.Y,
                            c = pos.Z - playerPos.Z;
                        float distSq = (a * a + b * b + c * c);
                        if (distSq <= BlendInDistanceSq && closestDist > distSq)
                        {
                            closestDist = distSq;
                            _blendInSourcePed = ped;
                        }
                    }
                }
            }

            if(_isBlendingIn)
            {
                if(IsControlJustPressed(0, (int)Control.Context))
                {
                    _isBlendingIn = false;
                    ClearPedTasks(PlayerPedId());
                }
            }

            if (_blendInSourcePed.HasValue)
            {
                if(IsControlJustPressed(0, (int)Control.Context))
                {
                    _isBlendingIn = true;

                    // Find the scenario this ped is using.
                    string scenarioToUse = null;
                    PedNode.AnimInfo? animToUse = null;
                    foreach (string scenario in Constants.Scenarios.All)
                    {
                        if(IsPedUsingScenario(_blendInSourcePed.Value, scenario))
                        {
                            scenarioToUse = scenario;
                            //Debug.WriteLine($"Copying scenario \"{scenario}\" from ped {_blendInSourcePed.Value}");
                            break;
                        }
                    }

                    if(scenarioToUse == null)
                    {
                        // Alternatively, try copying the anim
                        foreach(Constants.AnimNames.GenderedAnimBase anim in Constants.AnimNames.All)
                        {
                            PedNode.AnimInfo[] anims = { anim.Male, anim.Female };
                            foreach(PedNode.AnimInfo animInfo in anims)
                            {
                                if (IsEntityPlayingAnim(_blendInSourcePed.Value, animInfo.Dict, animInfo.Clip, 3))
                                {
                                    animToUse = animInfo;
                                    //Debug.WriteLine($"Copying anim {animInfo.Dict} {animInfo.Clip} from ped {_blendInSourcePed.Value}");
                                    break;
                                }
                            }

                            if(animToUse.HasValue)
                            {
                                break;
                            }
                        }

                        // Finally, do a coin toss between random scenario and random anim
                        if(!animToUse.HasValue)
                        {
                            bool useRandomScenario = s_RNG.NextDouble() >= 0.5;
                            int randomIndex = s_RNG.Next(0, useRandomScenario ? Constants.Scenarios.All.Length : Constants.AnimNames.All.Length);
                            
                            if(useRandomScenario)
                            {
                                scenarioToUse = Constants.Scenarios.All[randomIndex];
                                //Debug.WriteLine($"Picking random blend-in scenario {scenarioToUse}");
                            }
                            else
                            {
                                animToUse = Constants.AnimNames.All[randomIndex].Get(IsPedAMaleModel((uint)GetEntityModel(PlayerPedId())));
                                //Debug.WriteLine($"Picking random blend-in anim {animToUse.Value.Dict} {animToUse.Value.Clip}");
                            }
                        }
                    }

                    // Copy the scenario.
                    if (scenarioToUse != null)
                    {
                        TaskStartScenarioInPlace(PlayerPedId(), scenarioToUse, 0, true);
                    }
                    else if(animToUse.HasValue)
                    {
                        _animRequests[PlayerPedId()] = animToUse.Value;
                    }

                    _blendInSourcePed = null;
                }
            }

            if (!couldStartBlendInBeforeCheck && _blendInSourcePed.HasValue)
            {
                BeginTextCommandDisplayHelp(BlendInHelpTextKey);
                EndTextCommandDisplayHelp(0, true, true, -1);
            }
            else if (couldStartBlendInBeforeCheck && !_blendInSourcePed.HasValue)
            {
                ClearAllHelpMessages();
            }
        }

        private PedNode? _nearestLocalPlayerShower = null;
        private bool _isLocalPlayerInShower = false;
        private float _timeSinceLocalPlayerShowerDistanceCheck = 0f;
        private const float LocalPlayerShowerDistanceCheckIntervalSeconds = 0.3f;
        private void HandlePlayerShower(float deltaTime)
        {
            _timeSinceLocalPlayerShowerDistanceCheck += deltaTime;

            int playerPed = PlayerPedId();

            if(!_isLocalPlayerInShower && _timeSinceLocalPlayerShowerDistanceCheck >= LocalPlayerShowerDistanceCheckIntervalSeconds)
            {
                bool wasNearShower = _nearestLocalPlayerShower.HasValue;
                _nearestLocalPlayerShower = null;
                bool nearShower = false;

                Vector3 playerPos = GetEntityCoords(playerPed, false);

                foreach(PedNode showerNode in s_ShowerNodes)
                {
                    float a = playerPos.X - showerNode.Position.X;
                    float b = playerPos.Y - showerNode.Position.Y;
                    float c = playerPos.Z - showerNode.Position.Z;
                    float distSq = a * a + b * b + c * c;

                    if(distSq < (ExtendedWarpDistance * ExtendedWarpDistance))
                    {
                        _nearestLocalPlayerShower = showerNode;
                        nearShower = true;
                        break;
                    }
                }

                if(nearShower)
                {
                    BeginTextCommandDisplayHelp(ShowerHelpTextKey);
                    EndTextCommandDisplayHelp(0, true, true, -1);
                }
                else if(wasNearShower)
                {
                    ClearAllHelpMessages();
                }

                _timeSinceLocalPlayerShowerDistanceCheck = 0f;
            }

            const uint ComponentIdCount = 12;
            const uint PropIdCount = 10;
            if (_isLocalPlayerInShower)
            {

                // Take player out of shower
                if (IsControlJustPressed(0, (int)Control.Context))
                {
                    ClearPedTasks(playerPed);
                    _isLocalPlayerInShower = false;
                    if(_nearestLocalPlayerShower.HasValue)
                    {
                        ComputeDirVecFromHeading2D(_nearestLocalPlayerShower.Value.Position.Heading, out float backX, out float backY);
                        float
                            currentX = _nearestLocalPlayerShower.Value.Position.X,
                            currentY = _nearestLocalPlayerShower.Value.Position.Y,
                            currentZ = _nearestLocalPlayerShower.Value.Position.Z;

                        SetEntityCoords(playerPed, currentX + backX * ExtendedWarpDistance, currentY + backY * ExtendedWarpDistance, currentZ, false, false, false, false);

                        for(uint i = 0; i < ComponentIdCount; i++)
                        {
                            if (i != (uint)PedComponents.Hair && i != (uint)PedComponents.Head)
                            {
                                SetPedComponentVariation(playerPed, (int)i, _compsToRestore[(PedComponents)i].Drawable, _compsToRestore[(PedComponents)i].Texture, 0);
                            }
                        }
                        for(uint i = 0; i < PropIdCount; i++)
                        {
                            SetPedPropIndex(playerPed, (int)i, _propsToRestore[(PedProps)i].Drawable, _propsToRestore[(PedProps)i].Texture, true);
                        }

                        _compsToRestore.Clear();
                        _propsToRestore.Clear();
                    }
                }
            }
            else if(_nearestLocalPlayerShower.HasValue)
            {
                // Set player into shower
                if(IsControlJustPressed(0, (int)Control.Context))
                {
                    uint playerModel = (uint)GetEntityModel(playerPed);
                    bool isMale = IsPedAMaleModel(playerModel);
                    _animRequests[playerPed] = Constants.AnimNames.Shower.Get(isMale);
                    SetEntityCoords(playerPed, _nearestLocalPlayerShower.Value.Position.X, _nearestLocalPlayerShower.Value.Position.Y, _nearestLocalPlayerShower.Value.Position.Z, false, false, false, false);
                    SetEntityHeading(playerPed, _nearestLocalPlayerShower.Value.Position.Heading);
                    _isLocalPlayerInShower = true;

                    // Take the player's clothes
                    _compsToRestore.Clear();
                    for(uint i = 0; i < ComponentIdCount; i++)
                    {
                        _compsToRestore.Add((PedComponents)i, new PedVariation
                        {
                            Texture = GetPedTextureVariation(playerPed, (int)i),
                            Drawable = GetPedDrawableVariation(playerPed, (int)i),
                        });
                    }
                    _propsToRestore.Clear();
                    for(uint i = 0; i < PropIdCount; i++)
                    {
                        _propsToRestore.Add((PedProps)i, new PedVariation
                        {
                            Texture = GetPedPropTextureIndex(playerPed, (int)i),
                            Drawable = GetPedPropIndex(playerPed, (int)i),
                        });
                    }

                    ClearAllHelpMessages();

                    ClearAllPedProps(playerPed);
                    for(uint i = 0; i < ComponentIdCount; i++)
                    {
                        if(i != (uint)PedComponents.Hair && i != (uint)PedComponents.Head)
                        {
                            int drawable = -1;
                            if(i == (uint)PedComponents.Torso || i == (uint)PedComponents.Legs)
                            {
                                drawable = i == (uint)PedComponents.Legs && isMale ? 14 : 15;
                            }
                            if(i == (uint)PedComponents.Shoes)
                            {
                                drawable = isMale ? 34 : 35;
                            }
                            SetPedComponentVariation(playerPed, (int)i, drawable, 0, 0);
                        }
                    }
                }
            }
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

            // Only play one anim every tick
            int? animRequestToRemove = null;
            foreach(KeyValuePair<int, PedNode.AnimInfo> pedAnim in _animRequests)
            {
                if(HasAnimDictLoaded(pedAnim.Value.Dict))
                {
                    TaskPlayAnim(pedAnim.Key, pedAnim.Value.Dict, pedAnim.Value.Clip, 1f, 1f, -1, 1 | 4, 0f, false, false, false);
                    animRequestToRemove = pedAnim.Key;
                    break;
                }
                else
                {
                    RequestAnimDict(pedAnim.Value.Dict);
                }
            }

            if(animRequestToRemove != null)
            {
                _animRequests.Remove(animRequestToRemove.Value);
            }

            HandlePlayerShower(deltaTime);
            HandleBlendIn(deltaTime);
        }

        private void OnPedsSpawned(int[] entityHandles, Dictionary<int, PedNode> optionalPedInitStates)
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
            _pedLocations = new PedNode[optionalPedInitStates.Count];
            optionalPedInitStates.Values.CopyTo(_pedLocations, 0);
            _optionalPedHandles = new int[optionalPedInitStates.Count];
            optionalPedInitStates.Keys.CopyTo(_optionalPedHandles, 0);

            foreach(int pedHandle in _optionalPedHandles)
            {
                _timeTillPedBrainTick[pedHandle] = s_RNG.Next(0, 45);
            }
        }

        private void TickActive(float deltaTime)
        {
            if(IsLocalPlayerPedGod)
            {
                RunPedBrain(deltaTime);
            }
        }

        private float _timeSinceLastPedTargetCheck = 0f;
        private const float PedTargetCheckIntervalSeconds = 1.5f;

        private void RunPedBrain(float deltaTime)
        {
            if(_pedHandles != null)
            {
                bool tickedAnyBrains = false;

                List<int> pedsToTick = new List<int>();
                foreach(int pedHandle in _optionalPedHandles)
                {
                    _timeTillPedBrainTick[pedHandle] -= deltaTime;
                    if (_timeTillPedBrainTick[pedHandle] <= 0f)
                    {
                        tickedAnyBrains = true;
                        pedsToTick.Add(pedHandle);
                    }
                }

                if (tickedAnyBrains)
                {

                    List<PedNode> pickablePedTargets = new List<PedNode>(_pedLocations);

                    foreach(int ped in pedsToTick)
                    {
                        //Debug.WriteLine($"{nameof(RunPedBrain)}: updating ped {ped}'s task");

                        // Stop the previous task
                        ClearPedTasks(ped);
                        PedNode? initState = null;
                        if(_optionalPedInitStates.ContainsKey(ped))
                        {
                            initState = _optionalPedInitStates[ped];
                        }

                        // Check if the ped's current state requires them to be warped
                        // if so, warp back: use the heading to compute the back vector
                        if (initState?.HasFlag(PedNode.PedNodeFlag.NeedsWarp) == true)
                        {
                            PedNode.PositionInfo pos = initState.Value.Position;
                            ComputeDirVecFromHeading2D(pos.Heading, out float backX, out float backY);

                            SetEntityCoords(ped, pos.X + backX * WarpDistance, pos.Y + backY * WarpDistance, pos.Z, false, false, false, false);
                        }

                        // TODO: there's a chance we might pick the same target as current...
                        int randomNewTargetIndex = s_RNG.Next(0, pickablePedTargets.Count);
                        PedNode newTarget = pickablePedTargets[randomNewTargetIndex];

                        float offsetX = 0f, offsetY = 0f;
                        // Don't allow other peds wander to a warpable spot
                        if (newTarget.HasFlag(PedNode.PedNodeFlag.NeedsWarp))
                        {
                            pickablePedTargets.RemoveAt(randomNewTargetIndex);

                            ComputeDirVecFromHeading2D(newTarget.Position.Heading, out offsetX, out offsetY);

                            offsetX *= WarpDistance;
                            offsetY *= WarpDistance;
                        }

                        TaskGoToCoordAnyMeans(ped, newTarget.Position.X + offsetX, newTarget.Position.Y + offsetY, newTarget.Position.Z, 1f, 0, false, 0, 0.01f);

                        _optionalPedTargetStates[ped] = newTarget;

                        _optionalPedInitStates.Remove(ped);

                        // Randomise the time between ped brain ticks so people don't just start walking all at the same time
                        _timeTillPedBrainTick[ped] = PedBrainTickIntervalSeconds + (float)s_RNG.Next(0, 45);
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
                                PedNode target = _optionalPedTargetStates[ped];
                                Vector3 currentPos = GetEntityCoords(ped, false);
                                float distanceToTarget = currentPos.DistanceToSquared(new Vector3(target.Position.X, target.Position.Y, target.Position.Z));
                                bool hasAchieved = distanceToTarget < (WarpDistance * WarpDistance);
                                bool hasAlmostAchieved = hasAchieved || (distanceToTarget < (ExtendedWarpDistance * ExtendedWarpDistance));

                                if (hasAlmostAchieved)
                                {
                                    if (hasAchieved || target.HasFlag(PedNode.PedNodeFlag.NeedsWarp))
                                    {
                                        //Debug.WriteLine($"{nameof(RunPedBrain)}: ped {ped} has achieved their target at X = {target.Position.X}, Y = {target.Position.Y}, Z = {target.Position.Z}");

                                        ClearPedTasks(ped);
                                        _optionalPedTargetStates.Remove(ped);
                                        _optionalPedInitStates[ped] = target;

                                        PedNode.AnimInfo anim = target.Anim;
                                        // Need gendered anims
                                        if (target.HasFlag(PedNode.PedNodeFlag.Shower))
                                        {
                                            anim = Constants.AnimNames.Shower.Get(IsPedAMaleModel((uint)GetEntityModel(ped)));
                                        }

                                        if (string.IsNullOrEmpty(anim.Clip))
                                        {
                                            bool wantsRandomScenario = target.HasFlag(PedNode.PedNodeFlag.RandomScenario);
                                            bool wantsRandomAnim = target.HasFlag(PedNode.PedNodeFlag.RandomAnim);

                                            //Debug.WriteLine($"{nameof(target)}.{nameof(target.Flags)} = {target.Flags}, {nameof(wantsRandomScenario)} = {wantsRandomScenario}, {nameof(wantsRandomAnim)} = {wantsRandomAnim}");

                                            if (wantsRandomAnim && wantsRandomScenario)
                                            {
                                                if (s_RNG.NextDouble() >= 0.5)
                                                {
                                                    wantsRandomScenario = true;
                                                    wantsRandomAnim = false;
                                                }
                                                else
                                                {
                                                    wantsRandomAnim = true;
                                                    wantsRandomScenario = false;
                                                }

                                                //Debug.WriteLine($"Both were true. After a coin toss, {nameof(wantsRandomAnim)} = {wantsRandomAnim}, {nameof(wantsRandomScenario)} = {wantsRandomScenario}");
                                            }

                                            if (wantsRandomScenario)
                                            {
                                                int randomScenarioIndex = s_RNG.Next(0, Constants.Scenarios.All.Length);
                                                //Debug.WriteLine($"Starting scenario {Constants.Scenarios.All[randomScenarioIndex]} for ped {ped}");
                                                TaskStartScenarioInPlace(ped, Constants.Scenarios.All[randomScenarioIndex], 0, true);
                                            }

                                            if (wantsRandomAnim)
                                            {
                                                anim = Constants.AnimNames.All[s_RNG.Next(0, Constants.AnimNames.All.Length)].Get(IsPedAMaleModel((uint)GetEntityModel(ped)));
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(anim.Clip))
                                        {
                                            //Debug.WriteLine($"Requesting anim {anim.Dict} {anim.Clip} for ped {ped}");
                                            _animRequests[ped] = anim;
                                        }
                                    }

                                    if (target.HasFlag(PedNode.PedNodeFlag.NeedsWarp))
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

            _timeSinceLastPedTargetCheck += deltaTime;
        }

        /// <summary>
        /// Computes the "backward" vector from a heading angle
        /// </summary>
        /// <param name="heading">Z-axis angle (in degrees)</param>
        /// <param name="dirX"></param>
        /// <param name="dirY"></param>
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
