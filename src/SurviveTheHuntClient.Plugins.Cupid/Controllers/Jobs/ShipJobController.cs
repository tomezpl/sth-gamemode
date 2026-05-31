using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Models.UI;
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
                        Debug.WriteLine($"{logPrefix}{nameof(isDifferent)} = {isDifferent} because {nameof(value)} has {value.Count} elements, {nameof(_pedNetIds)} has {_pedNetIds.Count}");
                    }
                    int commonSize = Math.Min(value.Count, _pedNetIds.Count);
                    for(int i = 0; !isDifferent && i < commonSize; i++)
                    {
                        isDifferent = (int)_pedNetIds[i] != (int)value[i];
                        if(isDifferent)
                        {
                            Debug.WriteLine($"{logPrefix}{nameof(isDifferent)} = {isDifferent} because {nameof(value)}[{i}] == {(int)value[i]} and {nameof(_pedNetIds)}[{i}] == {(int)_pedNetIds[i]}");
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

            internal enum JobStage
            {
                WaitingToStart,
                FindDevice,
                StartHack,
                SurviveHack,
                RepairSignal,
                LeaveArea,
                Completed,
            }

            private JobStage _stage = JobStage.WaitingToStart;
            internal event GenericStateChangedEvent<JobStage> StageChanged;
            internal JobStage Stage
            {
                get => _stage;
                set
                {
                    JobStage prev = _stage;
                    if(prev != value)
                    {
                        _stage = value;
                        StageChanged.Invoke(prev, value, CanSync);
                    }
                }
            }

            private sbyte _pyreTwigSpawnLocationIndex = -1;
            internal event GenericStateChangedEvent<sbyte> PyreTwigSpawnLocationIndexChanged;
            internal sbyte PyreTwigSpawnLocationIndex
            {
                get => _pyreTwigSpawnLocationIndex;
                set
                {
                    sbyte prev = _pyreTwigSpawnLocationIndex;
                    _pyreTwigSpawnLocationIndex = value;
                    if(prev != value)
                    {
                        PyreTwigSpawnLocationIndexChanged.Invoke(prev, value, CanSync);
                    }
                }
            }

            private bool _huntersDiscoveredDevice = false;
            internal event GenericStateChangedEvent<bool> HuntersDiscoveredDeviceChanged;
            internal bool HuntersDiscoveredDevice
            {
                get => _huntersDiscoveredDevice;
                set
                {
                    bool prev = _huntersDiscoveredDevice;
                    _huntersDiscoveredDevice = value;
                    if(prev != value)
                    {
                        HuntersDiscoveredDeviceChanged.Invoke(prev, value, CanSync);
                    }
                }
            }

            private bool _huntedDiscoveredDevice = false;
            internal event GenericStateChangedEvent<bool> HuntedDiscoveredDeviceChanged;
            internal bool HuntedDiscoveredDevice
            {
                get => _huntedDiscoveredDevice;
                set
                {
                    bool prev = _huntedDiscoveredDevice;
                    _huntedDiscoveredDevice = value;
                    if (prev != value)
                    {
                        HuntedDiscoveredDeviceChanged.Invoke(prev, value, CanSync);
                    }
                }
            }

            private int _currentHackerPlayer = 0;
            internal event GenericStateChangedEvent<int> CurrentHackerPlayerChanged;
            internal int CurrentHackerPlayer
            {
                get => _currentHackerPlayer;
                set
                {
                    int prev = _currentHackerPlayer;
                    _currentHackerPlayer = value;
                    if(prev != value)
                    {
                        CurrentHackerPlayerChanged.Invoke(prev, value, CanSync);
                    }
                }
            }

            internal enum StateProp
            {
                PedNetIds,
                Stage,
                PyreTwigSpawnLocationIndex,
                HuntedDiscoveredDevice,
                HuntersDiscoveredDevice,
                CurrentHackerPlayer,
            }

            internal override Dictionary<int, object> Get()
            {
                return new Dictionary<int, object>
                {
                    {(int)StateProp.PedNetIds, Get((int)StateProp.PedNetIds) },
                    {(int)StateProp.Stage, Get((int)StateProp.Stage) },
                    {(int)StateProp.PyreTwigSpawnLocationIndex, Get((int)StateProp.PyreTwigSpawnLocationIndex) },
                    {(int)StateProp.HuntedDiscoveredDevice, Get((int)StateProp.HuntedDiscoveredDevice) },
                    {(int)StateProp.HuntersDiscoveredDevice, Get((int)StateProp.HuntersDiscoveredDevice) },
                    {(int)StateProp.CurrentHackerPlayer, Get((int)StateProp.CurrentHackerPlayer) },
                };
            }

            internal override object Get(int statePropId)
            {
                switch((StateProp)statePropId)
                {
                    case StateProp.PedNetIds:
                        return PedNetIds;
                    case StateProp.Stage:
                        return Stage;
                    case StateProp.PyreTwigSpawnLocationIndex:
                        return PyreTwigSpawnLocationIndex;
                    case StateProp.HuntedDiscoveredDevice:
                        return HuntedDiscoveredDevice;
                    case StateProp.HuntersDiscoveredDevice:
                        return HuntersDiscoveredDevice;
                    case StateProp.CurrentHackerPlayer:
                        return CurrentHackerPlayer;
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
                    case StateProp.Stage:
                        Stage = (JobStage)statePropValue;
                        break;
                    case StateProp.PyreTwigSpawnLocationIndex:
                        PyreTwigSpawnLocationIndex = (sbyte)statePropValue;
                        break;
                    case StateProp.HuntedDiscoveredDevice:
                        HuntedDiscoveredDevice = (bool)statePropValue;
                        break;
                    case StateProp.HuntersDiscoveredDevice:
                        HuntersDiscoveredDevice = (bool)statePropValue;
                        break;
                    case StateProp.CurrentHackerPlayer:
                        CurrentHackerPlayer = (int)statePropValue;
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

        internal const float ActiveRadius = 85f;

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

        private const string StartHackHelpTextKey = "STH_CUPID_SHIP_HACKSTART_HELP";
        private const string StartHackHelpTextLabel = "Press ~INPUT_CONTEXT~ to launch the Pyre Twig hack.";

        private const string ResumeHackHelpTextKey = "STH_CUPID_SHIP_HACKRESUME_HELP";
        private const string ResumeHackHelpTextLabel = "Press ~INPUT_CONTEXT~ to resume the Pyre Twig hack.";

        private const string SurviveHackObjectiveTextKey = "STH_CUPID_SHIP_HACKSURVIVE_OBJ";
        private const string SurviveHackObjectiveTextLabel = "Survive until the ~g~Pyre Twig~w~ completes the hack. Blend in with partygoers.";

        private const string ResumeHackObjectiveTextKey = "STH_CUPID_HELP_HACKRESUME_OBJ";
        private const string ResumeHackObjectiveTextLabel = "Return to the ~g~Pyre Twig~w~ and resume the hack.";

        private const string StartHackObjectiveTextKey = "STH_CUPID_HELP_HACKSTART_OBJ";
        private const string StartHackObjectiveTextLabel = "Start the hack using the ~g~Pyre Twig~w~.";
        
        private const string FindDeviceObjectiveTextKey = "STH_CUPID_HELP_FINDDEV_OBJ";
        private const string FindDeviceObjectiveTextLabel = "Find the Pyre Twig. Blend in to avoid being spotted by hunters.";

        private const string LeaveObjectiveTextKey = "STH_CUPID_HELP_LEAVE_OBJ";
        private const string LeaveObjectiveTextLabel = "Leave the yacht.";

        private const string PyreTwigBlipNameKey = "STH_CUPID_DEVICE_BLIP";
        private const string PyreTwigBlipNameLabel = "Sahara Pyre Twig 4K Ultra";

        private const string TrackerPutDownHelpTextKey = "STH_CUPID_TRACKER_PLACE_HELP";
        private const string TrackerPutDownHelpTextLabel = "Press ~INPUT_THROW_GRENADE~ to place down a tracker.";
        private const string TrackerPickUpHelpTextKey = "STH_CUPID_TRACKER_PICKUP_HELP";
        private const string TrackerPickUpHelpTextLabel = "Press ~INPUT_THROW_GRENADE~ to pick up the tracker.";

        private static readonly Dictionary<JobState.JobStage, KeyValuePair<string, string>> s_ObjectiveText = new Dictionary<JobState.JobStage, KeyValuePair<string, string>>
        {
            {JobState.JobStage.FindDevice, new KeyValuePair<string, string>(FindDeviceObjectiveTextKey, FindDeviceObjectiveTextLabel) },
            {JobState.JobStage.StartHack, new KeyValuePair<string, string>(StartHackObjectiveTextKey, StartHackObjectiveTextLabel) },
            {JobState.JobStage.RepairSignal, new KeyValuePair<string, string>(ResumeHackObjectiveTextKey, ResumeHackObjectiveTextLabel) },
            {JobState.JobStage.SurviveHack, new KeyValuePair<string, string>(SurviveHackObjectiveTextKey, SurviveHackObjectiveTextLabel) },
            {JobState.JobStage.LeaveArea, new KeyValuePair<string, string>(LeaveObjectiveTextKey, LeaveObjectiveTextLabel) },
        };

        private static readonly int PyreTwigModel = GetHashKey("reh_prop_reh_harddisk_01a");

        private int? _pyreTwigProp = null;
        private int? _pyreTwigBlip = null;
        private Vector3 _pyreTwigPos = Vector3.Zero;

        internal const float HackDurationSeconds = 100f;

        private LabelledItem[] _currentUI = new LabelledItem[0];

        private LabelledItem _hackUI = new LabelledItem("HACK", 0f);

        private List<LabelledItem> _trackerUI = new List<LabelledItem>(TrackerBudget);

        private static readonly int s_TrackerModel = GetHashKey("reh_prop_reh_gadget_01a");

        private Dictionary<int, float> _timeTillPedBrainTick = new Dictionary<int, float>();

        private static readonly bool s_HasDoneInit = Init();

        private static bool Init()
        {
            if(!s_HasDoneInit)
            {
                AddTextEntry(ShowerHelpTextKey, ShowerHelpTextLabel);
                AddTextEntry(BlendInHelpTextKey, BlendInHelpTextLabel);
                AddTextEntry(ResumeHackHelpTextKey, ResumeHackHelpTextLabel);
                AddTextEntry(StartHackHelpTextKey, StartHackHelpTextLabel);
                AddTextEntry(PyreTwigBlipNameKey, PyreTwigBlipNameLabel);
                AddTextEntry(TrackerPutDownHelpTextKey, TrackerPutDownHelpTextLabel);
                AddTextEntry(TrackerPickUpHelpTextKey, TrackerPickUpHelpTextLabel);

                foreach (KeyValuePair<string, string> label in s_ObjectiveText.Values)
                {
                    AddTextEntry(label.Key, label.Value);
                }
            }

            return true;
        }

        private void DisplayTextForObjective(JobState.JobStage stage)
        {
            if (GameState?.Hunt != null)
            {
                string textKey = "STRING";
                if (s_ObjectiveText.TryGetValue(stage, out KeyValuePair<string, string> text))
                {
                    textKey = text.Key;
                }

                BeginTextCommandPrint(textKey);
                EndTextCommandPrint((int)(GameState.Hunt.InitialEndTime - DateTime.UtcNow).TotalMilliseconds, true);
            }
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
            _state.PyreTwigSpawnLocationIndexChanged += OnPyreTwigSpawnLocationChanged;
            _state.StageChanged += OnJobStageChanged;
            _state.HuntedDiscoveredDeviceChanged += OnDiscoveredDeviceChanged;
            _state.HuntersDiscoveredDeviceChanged += OnDiscoveredDeviceChanged;
            _state.HuntedDiscoveredDeviceChanged += new JobStateBase.GenericStateChangedEvent<bool>(new Action<bool, bool, bool>((prev, current, canSync) =>
            {
                if(canSync)
                {
                    SyncState((int)JobState.StateProp.HuntedDiscoveredDevice);
                }
            }));
            _state.HuntersDiscoveredDeviceChanged += new JobStateBase.GenericStateChangedEvent<bool>(new Action<bool, bool, bool>((prev, current, canSync) =>
            {
                if(canSync)
                {
                    SyncState((int)JobState.StateProp.HuntersDiscoveredDevice);
                }
            }));

            _state.CurrentHackerPlayerChanged += OnCurrentHackerPlayerServerIdChanged;
        }

        private int? _currentHackerPlayerHandle = null;
        private void OnCurrentHackerPlayerServerIdChanged(int prev, int current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.CurrentHackerPlayer);
            }

            if(current == null)
            {
                _currentHackerPlayerHandle = null;
            }
            else
            {
                _currentHackerPlayerHandle = GetPlayerFromServerId(current);
            }
        }

        private void OnDiscoveredDeviceChanged(bool prev, bool current, bool canSync)
        {
            if(_pyreTwigBlip.HasValue)
            {
                SetBlipDisplay(_pyreTwigBlip.Value, current ? 6 : 0);
                if(current)
                {
                    SetBlipNameFromTextFile(_pyreTwigBlip.Value, PyreTwigBlipNameKey);
                    BeepPyreTwig("Crates_Blipped", "GTAO_Magnate_Boss_Modes_Soundset");
                }
            }
        }

        private void BeepPyreTwig()
        {
            BeepPyreTwig("Deliver_Item", "GTAO_Biker_Modes_Soundset");
        }

        private void BeepPyreTwig(string soundName, string soundSetName)
        {

            if (_pyreTwigBlip.HasValue)
            {
                SetBlipFlashTimer(_pyreTwigBlip.Value, 5000);
            }

            FlashMinimapDisplay();
            PlaySoundFrontend(-1, soundName, soundSetName, false);
        }

        protected override void OnJobFinished()
        {
            base.OnJobFinished();

            if(_pyreTwigBlip.HasValue)
            {
                SetBlipDisplay(_pyreTwigBlip.Value, 0);
            }
        }

        private LabelledItem[] BuildCurrentUI()
        {
            LabelledItem[] items = new LabelledItem[1 + _trackersPlaced.Count];
            items[0] = _hackUI;
            for(int i = 0; i < _trackersPlaced.Count; i++)
            {
                items[i + 1] = _trackersPlaced[i].UI;
            }
            return items;
        }

        private void OnJobStageChanged(JobState.JobStage prev, JobState.JobStage current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.Stage);
            }

            if(current == JobState.JobStage.SurviveHack && _canInteractWithDevice)
            {
                _canInteractWithDevice = false;
                ClearAllHelpMessages();
            }

            if((current == JobState.JobStage.SurviveHack || current == JobState.JobStage.LeaveArea) && PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
            {
                PlaySoundFrontend(-1, current == JobState.JobStage.LeaveArea ? "Hack_Complete" : "Hack_Start", "DLC_IE_SVM_Voltic2_Hacking_Sounds", true);
            }

            _isCurrentHacker = false;

            // canSync being true typically means the local player caused the state change, so we can assume they're the ones who started/resumed the hack
            if(canSync && current == JobState.JobStage.SurviveHack)
            {
                const float MinHackDurationFract = 0.35f;
                // Schedule a hack disruption.
                // The hack disruption should happen no sooner than 35% progress since starting/resuming. Should mean you'd get the disruption anywhere between 1-3 times.
                _timeTillHackDisrupted = (float)(MinHackDurationFract * HackDurationSeconds + Math.Max(0, s_RNG.NextDouble() * (HackDurationSeconds * (1f - MinHackDurationFract))));
                _isCurrentHacker = true;
                Debug.WriteLine($"Scheduled hack disruption in {_timeTillHackDisrupted} seconds");
            }

            if(current == JobState.JobStage.Completed)
            {
                _currentUI = new LabelledItem[0];
                OnJobFinished();
            }
            else if(current == JobState.JobStage.SurviveHack)
            {
                _currentUI = BuildCurrentUI(); 
            }
            // Don't remove the hack progress from hunters' POV.
            else if (prev != JobState.JobStage.SurviveHack || PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
            {
                _currentUI = new LabelledItem[0];
            }

            DisplayTextForObjective(current);
        }

        private void OnPyreTwigSpawnLocationChanged(sbyte prev, sbyte current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.PyreTwigSpawnLocationIndex);
            }
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
                        float distSq = pos.DistanceToSquared(playerPos);
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
                        Debug.WriteLine($"Blending in with scenario {scenarioToUse}");
                    }
                    else if(animToUse.HasValue)
                    {
                        _animRequests[PlayerPedId()] = animToUse.Value;
                        Debug.WriteLine($"Blending in with anim {animToUse.Value.Dict} {animToUse.Value.Clip}");
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

        protected override void OnActiveChanged(bool isActive)
        {
            base.OnActiveChanged(isActive);

            if(isActive)
            {
                if(_state.Stage == JobState.JobStage.WaitingToStart)
                {
                    _state.Stage++;
                }
            }
        }

        internal override LabelledItem[] CurrentUI => _currentUI;

        private void OnPyreTwigPropSpawned(int handle, Vector3 pos)
        {
            _pyreTwigProp = handle;
            _pyreTwigPos = pos;
            Debug.WriteLine($"Pyre twig spawned at {pos}");
            _pyreTwigBlip = AddBlipForEntity(handle);
            // radar_laptop
            SetBlipSprite(_pyreTwigBlip.Value, 521);
            SetBlipColour(_pyreTwigBlip.Value, (int)BlipColor.Green);
            SetBlipDisplay(_pyreTwigBlip.Value, 0);

            SetEntityHasGravity(handle, false);
            SetEntityCompletelyDisableCollision(handle, false, false);
            SetEntityAsMissionEntity(handle, false, true);
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
            if(_state.Stage > JobState.JobStage.WaitingToStart && !_hasStartedSpawningPeds && IsLocalPlayerPedGod)
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

            if(_state.PyreTwigSpawnLocationIndex == -1 && IsLocalPlayerPedGod)
            {
                _state.PyreTwigSpawnLocationIndex = (sbyte)s_RNG.Next(0, Constants.ShipDeviceLocations.All.Length);
            }

            if (!_pyreTwigProp.HasValue && _state.PyreTwigSpawnLocationIndex != -1)
            {
                if(!HasModelLoaded((uint)PyreTwigModel))
                {
                    RequestModel((uint)PyreTwigModel);
                }
                else
                {

                    Vector3 randomSpawn = Constants.ShipDeviceLocations.All[_state.PyreTwigSpawnLocationIndex];
                    _pyreTwigProp = CreateObject(PyreTwigModel, randomSpawn.X, randomSpawn.Y, randomSpawn.Z, false, false, false);
                    OnPyreTwigPropSpawned(_pyreTwigProp.Value, randomSpawn);
                    _pyreTwigPos = randomSpawn;
                    Debug.WriteLine($"Spawning pyre twig in location {_state.PyreTwigSpawnLocationIndex}");
                }
            }

            HandlePlayerShower(deltaTime);
            HandleBlendIn(deltaTime);

            if (_state.Stage > JobState.JobStage.WaitingToStart && _state.Stage < JobState.JobStage.Completed && IsLocalPlayerPedGod)
            {
                RunPedBrain(deltaTime);
            }
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

        private float _timeSinceObjectiveDistanceCheck = 0f;
        private const float ObjectiveDistanceCheckIntervalSeconds = 0.35f;
        private bool _canInteractWithDevice = false;
        private float _timeTillHackDisrupted = float.MaxValue;
        private float _hackSecondsElapsed = 0f;
        private bool _isCurrentHacker = false;
        private void HandleHuntedObjective(float deltaTime)
        {
            if (_state.Stage == JobState.JobStage.FindDevice || _state.Stage == JobState.JobStage.StartHack || _state.Stage == JobState.JobStage.RepairSignal)
            {
                _timeSinceObjectiveDistanceCheck += deltaTime;

                bool couldInteract = _canInteractWithDevice;

                if (_pyreTwigProp.HasValue)
                {
                    if (_timeSinceObjectiveDistanceCheck >= ObjectiveDistanceCheckIntervalSeconds)
                    {
                        _timeSinceObjectiveDistanceCheck = 0f;

                        const float InteractableDistance = 1.15f;
                        const float InteractableDistanceSq = InteractableDistance * InteractableDistance;

                        const float DiscoverDistance = 3.5f;
                        const float DiscoverDistanceSq = DiscoverDistance * DiscoverDistance;

                        Vector3 playerPos = GetEntityCoords(PlayerPedId(), false);
                        float distSq = playerPos.DistanceToSquared(_pyreTwigPos);
                        _canInteractWithDevice = distSq <= InteractableDistanceSq;

                        if (_canInteractWithDevice)
                        {
                            if (_state.Stage == JobState.JobStage.FindDevice)
                            {
                                _state.Stage = JobState.JobStage.StartHack;
                            }
                        }

                        const float MaxHeightDiff = 0.95f;

                        if (distSq < DiscoverDistanceSq && Math.Abs(playerPos.Z - _pyreTwigPos.Z) < MaxHeightDiff)
                        {
                            if (PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
                            {
                                _state.HuntedDiscoveredDevice = true;
                            }
                            else
                            {
                                _state.HuntersDiscoveredDevice = true;
                            }
                        }
                    }
                }
                else
                {
                    _canInteractWithDevice = false;
                }

                if (_canInteractWithDevice)
                {
                    if (IsControlJustPressed(0, (int)Control.Context))
                    {
                        _state.Stage = JobState.JobStage.SurviveHack;
                        _state.CurrentHackerPlayer = GetPlayerServerId(PlayerId());
                        _canInteractWithDevice = false;
                    }
                }

                if (_canInteractWithDevice != couldInteract)
                {
                    if (_canInteractWithDevice)
                    {
                        BeginTextCommandDisplayHelp(_state.Stage == JobState.JobStage.RepairSignal ? ResumeHackHelpTextKey : StartHackHelpTextKey);
                        EndTextCommandDisplayHelp(0, true, true, -1);
                    }
                    else
                    {
                        ClearAllHelpMessages();
                    }
                }
            }

            if(_state.Stage == JobState.JobStage.SurviveHack)
            {
                _hackSecondsElapsed += deltaTime;
                if (_isCurrentHacker)
                {
                    _timeTillHackDisrupted -= deltaTime;
                }

                _hackUI.Value = SurviveTheHuntShared.Utils.EncodingHelper.Utf16FromNormalFloat(Math.Min(1, _hackSecondsElapsed / HackDurationSeconds));
            }

            if(_hackSecondsElapsed > HackDurationSeconds && _isCurrentHacker)
            {
                _isCurrentHacker = false;
                _state.Stage = JobState.JobStage.LeaveArea;
            }

            if(_state.Stage == JobState.JobStage.SurviveHack && _isCurrentHacker && _timeTillHackDisrupted <= 0f)
            {
                _state.Stage = JobState.JobStage.RepairSignal;
                BeepPyreTwig("Hack_Stop", "DLC_IE_SVM_Voltic2_Hacking_Sounds");
            }

            if(_state.Stage == JobState.JobStage.LeaveArea && PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
            {
                bool allOutside = true;
                foreach(HuntPlayer player in GameState.Hunt.HuntedPlayers)
                {
                    int ped = GetPlayerPed(player.PlayerHandle);

                    Vector3 pos = GetEntityCoords(ped, false);
                    if(pos.DistanceToSquared(Origin) <= (ActiveRadius * ActiveRadius))
                    {
                        allOutside = false;
                        break;
                    }
                }

                if(allOutside)
                {
                    _state.Stage = JobState.JobStage.Completed;
                }
            }
        }

        private const float TrackerPlaceableCheckIntervalSeconds = 0.4f;
        private float _timeSinceTrackerPlaceableCheck = 0f;
        private const float TrackerProximityScanIntervalSeconds = 0.2f;
        private float _timeSinceTrackerProxCheck = 0f;
        private const byte TrackerBudget = 2;
        private bool _canPlaceTracker = false;
        private int? _trackerToPickup = null;
        private bool _isPlacingTracker = false;
        private List<DeviceTracker> _trackersPlaced = new List<DeviceTracker>(TrackerBudget);
        private List<Vector3> _trackerSpawnRequests = new List<Vector3>(1);
        private bool[] _trackerIndex = CreateTrackerIndex();
        private static bool[] CreateTrackerIndex()
        {
            bool[] index = new bool[TrackerBudget];
            for(int i = 0; i < index.Length; i++)
            {
                index[i] = true;
            }
            return index;
        }
        internal byte PlacedTrackerCount => (byte)Math.Max(_trackersPlaced.Count, _trackerSpawnRequests.Count);
        private void HandleHuntersObjective(float deltaTime)
        {
            bool isHunter = true;
            bool hasUndercoverGear = true;

            const float HalfPlayerHeight = 0.975f;

            int? spawnToRemoveIndex = null;
            for(int i = 0; i < _trackerSpawnRequests.Count; i++)
            {
                if(HasModelLoaded((uint)s_TrackerModel))
                {
                    Vector3 spawnPos = _trackerSpawnRequests[i];
                    spawnToRemoveIndex = i;

                    // Get first available tracker name
                    int trackerIndex = 0;
                    for(trackerIndex = 0; !_trackerIndex[trackerIndex] && trackerIndex < _trackerIndex.Length; trackerIndex++) { }

                    _trackerIndex[trackerIndex] = false;

                    const int SpriteA = 535;
                    const int SpriteH = 542;
                    const int SpriteStart = (SpriteH > SpriteA ? SpriteA : SpriteH);
                    const int SpriteRange = ((SpriteH > SpriteA ? SpriteH : SpriteA) - SpriteStart) + 1;

                    const string TrackerNames = "ABCDEFGH";

                    char trackerName = TrackerNames[trackerIndex % SpriteRange];
                    Debug.WriteLine($"Spawning tracker {trackerName}");

                    int entity = CreateObject(s_TrackerModel, spawnPos.X, spawnPos.Y, spawnPos.Z - HalfPlayerHeight, true, true, false);
                    SetEntityHasGravity(entity, false);
                    SetEntityCompletelyDisableCollision(entity, false, false);
                    int blip = AddBlipForEntity(entity);
                    SetBlipDisplay(blip, 6);
                    SetBlipSprite(blip, SpriteStart + (trackerIndex % SpriteRange));
                    _trackersPlaced.Add(new DeviceTracker
                    {
                        Blip = blip,
                        Entity = entity,
                        Index = trackerIndex,
                        Pos = spawnPos,
                        Name = trackerName,
                        UI = new LabelledItem($"TRACKER {trackerName}", 0f),
                    });
                    _currentUI = BuildCurrentUI();
                    PlaySoundFrontend(-1, "Deliver_Pick_Up", "HUD_FRONTEND_MP_COLLECTABLE_SOUNDS", true);
                    break;
                }
                else
                {
                    RequestModel((uint)s_TrackerModel);
                }
            }
            if(spawnToRemoveIndex.HasValue)
            {
                _trackerSpawnRequests.RemoveAt(spawnToRemoveIndex.Value);
            }
            if(_trackerSpawnRequests.Count == 0)
            {
                _isPlacingTracker = false;
            }

            bool couldPutDownTracker = _canPlaceTracker;
            bool couldPickUpTracker = !couldPutDownTracker && _trackerToPickup.HasValue;

            if(isHunter && hasUndercoverGear)
            {
                _timeSinceTrackerPlaceableCheck += deltaTime;

                if(!_isPlacingTracker && _timeSinceTrackerPlaceableCheck >= TrackerPlaceableCheckIntervalSeconds)
                {
                    _timeSinceTrackerPlaceableCheck = 0f;
                    _trackerToPickup = null;

                    int playerPed = PlayerPedId();

                    const float MaxVelocity = 0.3f;
                    bool anyTrackersRemaining = PlacedTrackerCount < TrackerBudget;
                    bool isStill = GetEntityVelocity(playerPed).LengthSquared() <= (MaxVelocity * MaxVelocity);
                    const float PickupRadius = 1.05f;
                    int? nearestTracker = null;
                    if (isStill)
                    {
                        Vector3 playerPos = GetEntityCoords(playerPed, false);
                        int index = -1;
                        foreach (DeviceTracker placedTracker in _trackersPlaced)
                        {
                            index++;
                            if (placedTracker.Pos.DistanceToSquared(playerPos) < (PickupRadius * PickupRadius))
                            {
                                nearestTracker = index;
                                break;
                            }
                        }
                    }
                    _canPlaceTracker = !nearestTracker.HasValue && isStill && PlacedTrackerCount < TrackerBudget; 
                    if(isStill && nearestTracker.HasValue)
                    {
                        _trackerToPickup = nearestTracker.Value;
                    }
                }
            }

            if(couldPutDownTracker != _canPlaceTracker || (!_canPlaceTracker && _trackerToPickup.HasValue) != couldPickUpTracker)
            {
                ClearAllHelpMessages();
                bool showPutDownText = _canPlaceTracker && !_trackerToPickup.HasValue;
                bool showPickUpText = !_canPlaceTracker && _trackerToPickup.HasValue;
                if(showPutDownText || showPickUpText)
                {
                    BeginTextCommandDisplayHelp(showPutDownText ? TrackerPutDownHelpTextKey : TrackerPickUpHelpTextKey);
                    EndTextCommandDisplayHelp(0, true, true, -1);
                }
            }

            const int PlaceTrackerControl = (int)Control.ThrowGrenade;
            if((_canPlaceTracker && !_isPlacingTracker && !_trackerToPickup.HasValue) || (_trackerToPickup.HasValue && !_isPlacingTracker))
            {
                if(IsControlJustPressed(0, PlaceTrackerControl))
                {
                    // Place new tracker
                    if (!_trackerToPickup.HasValue)
                    {
                        _isPlacingTracker = true;
                        _trackerSpawnRequests.Add(GetEntityCoords(PlayerPedId(), false));
                        Debug.WriteLine($"Trying to spawn tracker number {_trackersPlaced.Count + 1}");
                    }
                    // Pick up near tracker
                    else
                    {
                        int index = _trackerToPickup.Value;
                        DeviceTracker tracker = _trackersPlaced[index];
                        Debug.WriteLine($"Picking up tracker {tracker.Name}");
                        NetworkRequestControlOfEntity(tracker.Entity);
                        SetEntityAsMissionEntity(tracker.Entity, false, true);
                        DeleteEntity(ref tracker.Entity);
                        _trackersPlaced.RemoveAt(index);
                        RemoveBlip(ref tracker.Blip);
                        _trackerIndex[tracker.Index] = true;
                        _trackerToPickup = null;
                        _currentUI = BuildCurrentUI();
                        DisableControlAction(0, PlaceTrackerControl, true);
                        PlaySoundFrontend(-1, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
                    }
                    ClearAllHelpMessages();
                }
            }

            if(_isPlacingTracker)
            {
                DisableControlAction(0, PlaceTrackerControl, true);
            }

            _timeSinceTrackerProxCheck += deltaTime;
            if(_timeSinceTrackerProxCheck >= TrackerProximityScanIntervalSeconds)
            {
                _timeSinceTrackerProxCheck = 0f;

                if (_currentHackerPlayerHandle.HasValue)
                {
                    int hackerPed = GetPlayerPed(_currentHackerPlayerHandle.Value);
                    Vector3 hackerPos = GetEntityCoords(hackerPed, false);
                    foreach (DeviceTracker tracker in _trackersPlaced)
                    {
                        const float TrackableRadius = ActiveRadius * 1.25f;
                        if(hackerPos.X != tracker.Pos.X && hackerPos.Y != tracker.Pos.Y && hackerPos.Z != tracker.Pos.Z)
                        {
                            float t = _state.Stage == JobState.JobStage.SurviveHack 
                                ? Math.Min(1f, Math.Max(0f, (1f - Math.Min(1f, (float)Math.Sqrt(tracker.Pos.DistanceToSquared(hackerPos)) / TrackableRadius))))
                                : 0f;

                            tracker.UI.Value = SurviveTheHuntShared.Utils.EncodingHelper.Utf16FromNormalFloat(t);
                        }
                    }
                }
            }
        }

        private void TickActive(float deltaTime)
        {
            HandleHuntedObjective(deltaTime);
            HandleHuntersObjective(deltaTime);
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

            if(_pyreTwigProp.HasValue)
            {
                int prop = _pyreTwigProp.Value;
                DeleteObject(ref prop);
            }

            if(_pyreTwigBlip.HasValue)
            {
                int blip = _pyreTwigBlip.Value;
                RemoveBlip(ref blip);
            }

            foreach(DeviceTracker tracker in _trackersPlaced)
            {
                SetEntityAsMissionEntity(tracker.Entity, false, true);
                NetworkRequestControlOfEntity(tracker.Entity);
                int handle = tracker.Entity;
                DeleteEntity(ref handle);
                int blip = tracker.Blip;
                RemoveBlip(ref blip);
            }

            _trackersPlaced.Clear();
        }
    }
}
