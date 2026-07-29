using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Models.UI;
using SurviveTheHuntClient.Plugins.Cupid.Helpers;
using SurviveTheHuntClient.Plugins.Cupid.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using SurviveTheHuntClient.Plugins.Cupid.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntClient.Plugins.Cupid.Constants;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs
{
    internal sealed class ShipJobController : JobControllerBase, IDisguiseEmitter, IDisguiseListener
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
                        isDifferent = (int)_pedNetIds[i] != Convert.ToInt32(value[i]);
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
                            netIds[i] = Convert.ToInt32(value[i]);
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

            private int _pyreTwigSpawnLocationIndex = -1;
            internal event GenericStateChangedEvent<int> PyreTwigSpawnLocationIndexChanged;
            internal int PyreTwigSpawnLocationIndex
            {
                get => _pyreTwigSpawnLocationIndex;
                set
                {
                    int prev = _pyreTwigSpawnLocationIndex;
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

            private SpookedType _spookedState = SpookedType.NotSpooked;
            internal event GenericStateChangedEvent<SpookedType> SpookedStateChanged;
            internal SpookedType SpookedState
            {
                get => _spookedState;
                set
                {
                    SpookedType prev = _spookedState;
                    if(prev != value)
                    {
                        _spookedState = value;
                        SpookedStateChanged.Invoke(prev, value, CanSync);
                    }
                }
            }

            private float _hackProgress = 0f;
            internal event GenericStateChangedEvent<float> HackProgressChanged;
            internal float HackProgress
            {
                get => _hackProgress;
                set
                {
                    float prev = _hackProgress;
                    if(prev != value)
                    {
                        _hackProgress = value;
                        HackProgressChanged.Invoke(prev, value, CanSync);
                    }
                }
            }

            private int? _shipOwnerServerId = null;
            internal event GenericStateChangedEvent<int?> ShipOwnerServerIdChanged;
            internal int? ShipOwnerServerId
            {
                get => _shipOwnerServerId;
                set
                {
                    int? prev = _shipOwnerServerId;
                    if (!prev.HasValue)
                    {
                        _shipOwnerServerId = value;
                        ShipOwnerServerIdChanged.Invoke(prev, value, CanSync);
                    }
                }
            }

            private bool _jHasOutfit = false;
            internal bool JHasOutfit
            {
                get => _jHasOutfit;
                set
                {
                    bool prev = _jHasOutfit;
                    if (value != prev)
                    {
                        _jHasOutfit = value;
                        PlayerDisguiseChanged?.Invoke(SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType.HuntedJ, value);
                        JHasOutfitChanged?.Invoke(prev, value, CanSync);
                    }
                }
            }
            internal event GenericStateChangedEvent<bool> JHasOutfitChanged;

            private bool _lHasOutfit = false;
            internal bool LHasOutfit
            {
                get => _lHasOutfit;
                set
                {
                    bool prev = _lHasOutfit;
                    if (value != prev)
                    {
                        _lHasOutfit = value;
                        PlayerDisguiseChanged?.Invoke(SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType.HuntedL, value);
                        LHasOutfitChanged?.Invoke(prev, value, CanSync);
                    }
                }
            }
            internal event GenericStateChangedEvent<bool> LHasOutfitChanged;

            internal delegate void PlayerDisguiseChangedEvent(SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType player, bool hasDisguise);
            internal event PlayerDisguiseChangedEvent PlayerDisguiseChanged;

            internal enum SpookedType
            {
                NotSpooked,
                SpookedByGunfire,
                SpookedByCops,
            }

            internal enum StateProp
            {
                PedNetIds,
                Stage,
                PyreTwigSpawnLocationIndex,
                HuntedDiscoveredDevice,
                HuntersDiscoveredDevice,
                CurrentHackerPlayer,
                SpookedState,
                HackProgress,
                ShipOwner,
                JHasOutfit,
                LHasOutfit,
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
                    {(int)StateProp.SpookedState, Get((int)StateProp.SpookedState) },
                    {(int)StateProp.HackProgress, Get((int)StateProp.HackProgress) },
                    {(int)StateProp.ShipOwner, Get((int)StateProp.ShipOwner) },
                    {(int)StateProp.JHasOutfit, Get((int)StateProp.JHasOutfit) },
                    {(int)StateProp.LHasOutfit, Get((int)StateProp.LHasOutfit) },
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
                    case StateProp.SpookedState:
                        return SpookedState;
                    case StateProp.HackProgress:
                        return HackProgress;
                    case StateProp.ShipOwner:
                        return ShipOwnerServerId;
                    case StateProp.JHasOutfit:
                        return JHasOutfit;
                    case StateProp.LHasOutfit:
                        return LHasOutfit;
                    default:
                        throw new ArgumentException($"Needs to be a valid {nameof(StateProp)}", nameof(statePropId));
                }
            }

            internal override void SetImpl(int statePropId, object statePropValue)
            {
                Debug.WriteLine($"{nameof(ShipJobController)} Setting state prop {(StateProp)statePropId} from remote event with value {statePropValue}");
                Debug.WriteLine($"Prop value type {statePropValue.GetType().FullName}");
                switch((StateProp)statePropId)
                {
                    case StateProp.PedNetIds:
                        PedNetIds = (List<object>)statePropValue;
                        break;
                    case StateProp.Stage:
                        Stage = (JobStage)Convert.ToSByte(statePropValue);
                        break;
                    case StateProp.PyreTwigSpawnLocationIndex:
                        PyreTwigSpawnLocationIndex = Convert.ToInt32(statePropValue);
                        break;
                    case StateProp.HuntedDiscoveredDevice:
                        HuntedDiscoveredDevice = Convert.ToBoolean(statePropValue);
                        break;
                    case StateProp.HuntersDiscoveredDevice:
                        HuntersDiscoveredDevice = Convert.ToBoolean(statePropValue);
                        break;
                    case StateProp.CurrentHackerPlayer:
                        CurrentHackerPlayer = Convert.ToInt32(statePropValue);
                        break;
                    case StateProp.SpookedState:
                        SpookedState = (SpookedType)Convert.ToSByte(statePropValue);
                        break;
                    case StateProp.HackProgress:
                        HackProgress = Convert.ToSingle(statePropValue);
                        break;
                    case StateProp.ShipOwner:
                        ShipOwnerServerId = Convert.ToInt32(statePropValue);
                        break;
                    case StateProp.JHasOutfit:
                        JHasOutfit = Convert.ToBoolean(statePropValue);
                        break;
                    case StateProp.LHasOutfit:
                        LHasOutfit = Convert.ToBoolean(statePropValue);
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
        private int?[] _pedHandles = new int?[0];
        private bool _pedsNeedSyncing = false;

        private ShipPedSpawnHelper.Spawner _pedSpawner = null;
        private bool _hasStartedSpawningPeds = false;

        private int? _shipOwnerPlayerId = null;
        internal bool IsLocalPlayerPedGod => _shipOwnerPlayerId.HasValue && PlayerId() == _shipOwnerPlayerId.Value;

        private Dictionary<int, PedNode> _optionalPedInitStates = new Dictionary<int, PedNode>();
        private Dictionary<int, PedNode> _optionalPedTargetStates = new Dictionary<int, PedNode>();
        private PedNode[] _pedLocations = new PedNode[0];
        private int[] _optionalPedHandles = new int[0];
        private int[] _optionalPedNetIds = new int[0];

        private class AnimRequestHelper : ITickable
        {
            private readonly TriggerServerEventProxyDelegate TriggerServerEvent;
            private readonly ShipJobController Controller;
            internal AnimRequestHelper(TriggerServerEventProxyDelegate triggerServerEvent, ShipJobController controller)
            {
                TriggerServerEvent = triggerServerEvent;
                Controller = controller;
                Controller.RemoteAnimRequestReceived += OnAnimRequestReceived;
            }

            private void OnAnimRequestReceived(object pedNetId, object serialisedAnimInfo)
            {
                PedNode.AnimInfo animInfo = new PedNode.AnimInfo((string)serialisedAnimInfo);
                _current[Convert.ToInt32(pedNetId)] = animInfo;
                Debug.WriteLine($"ped {pedNetId} will play anim clip {animInfo.Clip} from dict {animInfo.Dict}");
            }

            /// <summary>
            /// Animations that need to be loaded for a specific net ID
            /// </summary>
            private Dictionary<int, PedNode.AnimInfo> _current = new Dictionary<int, PedNode.AnimInfo>();

            public void Tick(float deltaTime)
            {
                int localPlayerPed = PlayerPedId();
                // Only play one anim every tick
                int? animRequestToRemove = null;
                foreach (KeyValuePair<int, PedNode.AnimInfo> pedAnim in _current)
                {
                    if (HasAnimDictLoaded(pedAnim.Value.Dict))
                    {
                        bool entityExists = NetworkDoesNetworkIdExist(pedAnim.Key) && NetworkDoesEntityExistWithNetworkId(pedAnim.Key);
                        if(entityExists)
                        {
                            int pedHandle = NetToPed(pedAnim.Key);
                            // Only play if it's a spawned ped and we're ped god, OR if it's us
                            if ((!IsPedAPlayer(pedHandle) && Controller.IsLocalPlayerPedGod) || localPlayerPed == pedHandle)
                            {
                                TaskPlayAnim(pedHandle, pedAnim.Value.Dict, pedAnim.Value.Clip, 1f, 1f, -1, 1 | 4, 0f, false, false, false);
                            }
                            else
                            {

                            }
                        }
                        animRequestToRemove = pedAnim.Key;
                        break;
                    }
                    else
                    {
                        RequestAnimDict(pedAnim.Value.Dict);
                    }
                }

                if (animRequestToRemove != null)
                {
                    _current.Remove(animRequestToRemove.Value);
                }
            }

            internal void Request(int pedNetId, PedNode.AnimInfo anim)
            {
                TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent, Constants.SpecialEvent.RemoteAnimRequest, pedNetId, anim.ToString());
            }

            internal void Cleanup()
            {
                Controller.RemoteAnimRequestReceived -= OnAnimRequestReceived;
            }
        }
        private readonly AnimRequestHelper AnimRequests;

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

        private const string CopsSpookedNotifTextKey = "STH_CUPID_COPS_SPOOKED_NOTIF";
        private const string CopsSpookedNotifTextLabel = "Your police gear has spooked the cruisegoers.";

        private const string BlipNameTextKey = "STH_CUPID_YACHT_JOB_BLIP";
        private const string BlipNameTextLabel = "Yacht Party";

        private List<PendingText> _pendingTexts = new List<PendingText>();

        private static readonly Dictionary<JobState.JobStage, KeyValuePair<string, string>> s_ObjectiveText = new Dictionary<JobState.JobStage, KeyValuePair<string, string>>
        {
            {JobState.JobStage.FindDevice, new KeyValuePair<string, string>(FindDeviceObjectiveTextKey, FindDeviceObjectiveTextLabel) },
            {JobState.JobStage.StartHack, new KeyValuePair<string, string>(StartHackObjectiveTextKey, StartHackObjectiveTextLabel) },
            {JobState.JobStage.RepairSignal, new KeyValuePair<string, string>(ResumeHackObjectiveTextKey, ResumeHackObjectiveTextLabel) },
            {JobState.JobStage.SurviveHack, new KeyValuePair<string, string>(SurviveHackObjectiveTextKey, SurviveHackObjectiveTextLabel) },
            {JobState.JobStage.LeaveArea, new KeyValuePair<string, string>(LeaveObjectiveTextKey, LeaveObjectiveTextLabel) },
        };

        private const string HunterObjectiveTextKey = "STH_CUPID_YACHT_JOB_HUNTER_OBJ";
        private const string HunterObjectiveTextLabel = "Suspects are attempting to extract Righteous Slaughter dev data using a Pyre Twig TV dongle. Blend in and place trackers to locate them.";

        private static readonly int PyreTwigModel = GetHashKey("reh_prop_reh_harddisk_01a");

        private int? _pyreTwigProp = null;
        private int? _pyreTwigBlip = null;
        private Vector3 _pyreTwigPos = Vector3.Zero;

        internal const float HackDurationSeconds = 100f;

        private LabelledItem[] _currentUI = new LabelledItem[0];

        private LabelledItem _hackUI = new LabelledItem("HACK", 0f);

        private readonly List<LabelledItem> _trackerUI = new List<LabelledItem>(TrackerBudget);

        private static readonly int s_TrackerModel = GetHashKey("reh_prop_reh_gadget_01a");

        private Dictionary<int, float> _timeTillPedBrainTick = new Dictionary<int, float>();

        private static readonly bool s_HasDoneInit = Init();

        private int? _jobBlip;

        private const string PartyDisguiseBlipNameKey = "STH_CUPID_BLIP_HUNTED_DISGUISE";
        private const string PartyDisguiseBlipNameContent = "Party disguise";

        private const string PartyDisguisePickUpHelpKey = "STH_CUPID_HELP_HUNTED_DISGUISE";
        private const string PartyDisguisePickUpHelpText = "Press ~INPUT_CONTEXT~ to put on some party clothing.";

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
                AddTextEntry(CopsSpookedNotifTextKey, CopsSpookedNotifTextLabel);
                AddTextEntry(BlipNameTextKey, BlipNameTextLabel);
                AddTextEntry(PartyDisguiseBlipNameKey, PartyDisguiseBlipNameContent);
                AddTextEntry(PartyDisguisePickUpHelpKey, PartyDisguisePickUpHelpText);
                AddTextEntry(HunterObjectiveTextKey, HunterObjectiveTextLabel);

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
                string textKey = null;
                if (PlayerState?.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
                {
                    if (s_ObjectiveText.TryGetValue(stage, out KeyValuePair<string, string> text))
                    {
                        textKey = text.Key;
                    }
                }
                else
                {
                    textKey = HunterObjectiveTextKey;
                }

                if (textKey != null)
                {
                    BeginTextCommandPrint(textKey);
                    EndTextCommandPrint((int)(GameState.Hunt.InitialEndTime - DateTime.UtcNow).TotalMilliseconds, true);
                }
                else
                {
                    HUDUtils.ClearObjective();
                }
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

        private readonly TriggerServerEventProxyDelegate TriggerServerEvent;
        private readonly TriggerEventProxyDelegate TriggerEvent;
        private readonly PhoneTextHelper PhoneTextHelper;

        private delegate void RemoteAnimRequestReceivedDelegate(object pedNetId, object serialisedAnimInfo);
        private event RemoteAnimRequestReceivedDelegate RemoteAnimRequestReceived;

        public event DisguiseStateChangedEvent DisguiseStateChanged;

        private readonly int[] _clothingBlips;

        internal delegate void UpdatePlayerClothingDelegate(SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType playerType, DirectedScene scene, bool strict = false);

        private readonly UpdatePlayerClothingDelegate UpdatePlayerClothing;

        internal ShipJobController(UpdatePlayerClothingDelegate updatePlayerClothing, TriggerEventProxyDelegate triggerEventProxy, TriggerServerEventProxyDelegate triggerServerEventProxy, JobStateRpcUpdateDelegate updateJobStateRpc) : base("ship", updateJobStateRpc)
        {
            TriggerEvent = triggerEventProxy;
            TriggerServerEvent = triggerServerEventProxy;
            PhoneTextHelper = new PhoneTextHelper(triggerEventProxy);
            UpdatePlayerClothing = updatePlayerClothing;

            AnimRequests = new AnimRequestHelper(triggerServerEventProxy, this);

            _state.PedNetIdsChanged += OnPedNetIdsChanged;
            _state.PyreTwigSpawnLocationIndexChanged += OnPyreTwigSpawnLocationChanged;
            _state.StageChanged += OnJobStageChanged;
            _state.HuntedDiscoveredDeviceChanged += OnHuntedDiscoveredDeviceChanged;
            _state.HuntersDiscoveredDeviceChanged += OnHuntersDiscoveredDeviceChanged;
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

            _state.SpookedStateChanged += OnSpookedStateChanged;

            _state.HackProgressChanged += OnHackProgressChanged;

            _state.ShipOwnerServerIdChanged += OnShipOwnerServerIdChanged;

            _state.JHasOutfitChanged += OnJHasOutfitChanged;
            _state.LHasOutfitChanged += OnLHasOutfitChanged;

            _clothingBlips = new int[Constants.Location.PartyClothesMarkers.All.Length];
            for (int i = 0; i < Constants.Location.PartyClothesMarkers.All.Length; i++)
            {
                Vector3 pos = Constants.Location.PartyClothesMarkers.All[i];
                int blip = AddBlipForCoord(pos.X, pos.Y, pos.Z + Constants.Location.PartyClothesMarkers.ZOffset);
                _clothingBlips[i] = blip;
                SetBlipSprite(blip, (int)BlipSprite.Clothes);
                SetBlipNameFromTextFile(blip, PartyDisguiseBlipNameKey);
                SetBlipDisplay(blip, 0);
            }
        }

        private bool _clothesBlipsShowingDoNotSet = false;
        private void SetClothesBlipsShowing(bool show = true)
        {
            _clothesBlipsShowingDoNotSet = show;
            foreach(int blip in _clothingBlips)
            {
                SetBlipDisplay(blip, show ? 6 : 0);
            }
        }

        private void OnLHasOutfitChanged(bool prev, bool current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.LHasOutfit);
            }

            if (current)
            {
                TrySendTextAboutDisguisesReady();
            }
        }

        private void OnJHasOutfitChanged(bool prev, bool current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.JHasOutfit);
            }

            if (current)
            {
                TrySendTextAboutDisguisesReady();
            }
        }

        private bool _hasSentTextAboutDisguisesReady = false;
        private bool TrySendTextAboutDisguisesReady()
        {
            if(_hasSentTextAboutDisguisesReady)
            {
                return false;
            }

            bool canSend = _state.JHasOutfit && _state.LHasOutfit;
            if (canSend)
            {
                _hasSentTextAboutDisguisesReady = true;
                _pendingTexts.Add(new PendingText(2.5f, PhoneContacts.Esther, "nice threads", "Aw don't you two look cute! Now get your asses over to the yacht. I'll explain later.", 12));
            }

            return canSend;
        }

        private bool _shipNeedsOwner = true;
        private void OnShipOwnerServerIdChanged(int? prev, int? current, bool canSync)
        {
            if (canSync)
            {
                SyncState((int)JobState.StateProp.ShipOwner);
            }

            if (current.HasValue)
            {
                _shipNeedsOwner = false;
                _shipOwnerPlayerId = GetPlayerFromServerId(current.Value);
            }
        }

        private void TryClaimShipOwnership()
        {
            TriggerServerEvent(SurviveTheHuntShared.Events.Server.SetServerState, Convert.ToByte(SurviveTheHuntShared.Plugins.PluginIndex.Cupid), Convert.ToByte(SurviveTheHuntShared.Plugins.Cupid.Constants.ServerStateKey.ShipOwner), Convert.ToInt32(GetPlayerServerId(PlayerId())));
        }

        private void OnHuntersDiscoveredDeviceChanged(bool prev, bool current, bool canSync)
        {
            OnDiscoveredDeviceChanged(SurviveTheHuntShared.Core.Teams.Team.Hunters, prev, current, canSync);
        }

        private void OnHuntedDiscoveredDeviceChanged(bool prev, bool current, bool canSync)
        {
            OnDiscoveredDeviceChanged(SurviveTheHuntShared.Core.Teams.Team.Hunted, prev, current, canSync);
        }

        private void OnHackProgressChanged(float prev, float current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.HackProgress);
            }

            _hackSecondsElapsed = Math.Max(_hackSecondsElapsed, current);
        }

        private int? _gunfireEvent = null;
        private void OnSpookedStateChanged(JobState.SpookedType prev, JobState.SpookedType current, bool canSync)
        {
            if(canSync)
            {
                SyncState((int)JobState.StateProp.SpookedState);
            }

            Debug.WriteLine($"Spooked state changed from {prev} to {current}");

            if(IsLocalPlayerPedGod)
            {
                switch(current)
                {
                    case JobState.SpookedType.SpookedByGunfire:

                        _gunfireEvent = AddShockingEventAtPosition(90, Origin.X, Origin.Y, Origin.Z, (float)(GameState.Hunt.ActualEndTime - DateTime.UtcNow).TotalSeconds);
                        foreach (int pedNetId in _pedNetIds)
                        {
                            if (NetworkDoesNetworkIdExist(pedNetId) && NetworkDoesEntityExistWithNetworkId(pedNetId))
                            {
                                int ped = NetToPed(pedNetId);
                                SetBlockingOfNonTemporaryEvents(ped, false);
                                TaskShockingEventReact(ped, _gunfireEvent.Value);
                            }
                        }
                        if(PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunters)
                        {
                            _pendingTexts.Add(new PendingText(4f, PhoneContacts.Police, "Shots fired", "Reports of gunfire aboard the Dignity. Proceed with caution, Lima 6-7.", 15f));
                        }
                        break;
                }
            }

            if(current == JobState.SpookedType.SpookedByCops)
            {
                bool isCop = PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunters;

                if (isCop)
                {
                    BeginTextCommandThefeedPost(CopsSpookedNotifTextKey);
                    EndTextCommandThefeedPostTicker(true, true);
                    _pendingTexts.Add(new PendingText(10f, PhoneContacts.Police, "Cover blown", "Receiving reports of a disturbance aboard the Dignity. Lima 6 can you confirm plain clothes status?", 15f));
                }
                else
                {
                    _pendingTexts.Add(new PendingText((float)s_RNG.NextDouble() * 5f, Constants.PhoneContacts.Esther, "watch out", "Something's up. Seeing folk panicking on cams. You might have company."));
                }
            }
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

        private void OnDiscoveredDeviceChanged(SurviveTheHuntShared.Core.Teams.Team team, bool prev, bool current, bool canSync)
        {
            if(_pyreTwigBlip.HasValue)
            {
                bool isLocalTeam = PlayerState?.Team == team;
                if (isLocalTeam)
                {
                    SetBlipDisplay(_pyreTwigBlip.Value, current ? 6 : 0);
                }

                if(current)
                {
                    if (isLocalTeam)
                    {
                        SetBlipNameFromTextFile(_pyreTwigBlip.Value, PyreTwigBlipNameKey);
                        BeepPyreTwig("Crates_Blipped", "GTAO_Magnate_Boss_Modes_Soundset");
                    }

                    if(PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
                    {
                        _pendingTexts.Add(new PendingText(2f, Constants.PhoneContacts.Esther, "hack device", "Bingo, that's the one. You know how to jack in, right? I'll handle the rest."));
                    }
                }
            }
        }

        internal void OnRemoteAnimRequest(object pedNetId, object serialisedAnimInfo)
        {
            //Debug.WriteLine($"{nameof(OnRemoteAnimRequest)}({nameof(pedNetId)}: {pedNetId}, {nameof(serialisedAnimInfo)}: {serialisedAnimInfo})");
            RemoteAnimRequestReceived.Invoke(pedNetId, serialisedAnimInfo);
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

            IsActive = false;
        }

        private LabelledItem[] BuildCurrentUI()
        {
            if (_state.Stage == JobState.JobStage.SurviveHack || _state.Stage == JobState.JobStage.RepairSignal)
            {
                LabelledItem[] items = new LabelledItem[1 + _trackersPlaced.Count];
                items[0] = _hackUI;
                for (int i = 0; i < _trackersPlaced.Count; i++)
                {
                    items[i + 1] = _trackersPlaced[i].UI;
                }
                return items;
            }

            return LabelledItem.Empty;
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
                if (current == JobState.JobStage.LeaveArea)
                {
                    _pendingTexts.Add(new PendingText(1.5f, Constants.PhoneContacts.Esther, "hack done", "done. I'll send it over to SlaughterHouse forums. You'll get your cut. Now get out.", 10f));
                }
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

            if(current == JobState.JobStage.RepairSignal)
            {
                if(PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
                {
                    _pendingTexts.Add(new PendingText(1f, Constants.PhoneContacts.Esther, "FIX THE SIGNAL!!!", "ugh fucking thing dropped, need one of yous to check on the device", 10f));
                }
            }

            if(current == JobState.JobStage.Completed)
            {
                _currentUI = new LabelledItem[0];
                OnJobFinished();
            }
            else if(current == JobState.JobStage.SurviveHack)
            {
                _currentUI = BuildCurrentUI();


                if (prev != JobState.JobStage.RepairSignal)
                {
                    if (PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
                    {
                        _pendingTexts.Add(new PendingText(7.5f + (float)s_RNG.NextDouble() * 20f, Constants.PhoneContacts.Esther, "jammers", "oh btw. LSPD probs headed your way. watch out for signal trackers"));
                    }
                    else
                    {
                        _pendingTexts.Add(new PendingText(10f, PhoneContacts.Police, "URGENT", "VIP reported network intrusion. Suspects may be exfiltrating data as we speak.", 15f));
                    }
                }
            }
            // Don't remove the hack progress from hunters' POV.
            else if (prev != JobState.JobStage.SurviveHack || PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
            {
                _currentUI = new LabelledItem[0];
            }

            if(current == JobState.JobStage.FindDevice)
            {
                if(PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
                {
                    _pendingTexts.Add(new PendingText((float)s_RNG.NextDouble() * 5.5f, Constants.PhoneContacts.Esther, "the job", "find a Pyre Twig - the TV plug in thingy. looks kinda like a hard drive tho?"));
                    _pendingTexts.Add(new PendingText(5.5f, Constants.PhoneContacts.Esther, "the job", "it's remoted into a Righteous Slaughter dev's box. i'll RDP into the TV stick and then onto the box", 15f));
                    _pendingTexts.Add(new PendingText(20.5f, Constants.PhoneContacts.Esther, "the job", "look i get game leaks don't interest you. just find the hard drive looking TV stick okay? tyty x", 17.5f));
                    _pendingTexts.Add(new PendingText(45f, Constants.PhoneContacts.Esther, "act normal", "Also like, try to blend in? Grab a drink or dance or smth"));
                }
            }

            DisplayTextForObjective(current);
        }

        private void OnPyreTwigSpawnLocationChanged(int prev, int current, bool canSync)
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

        private void SetPedAsFriendly(int ped)
        {
            // treat as friendly
            SetPedConfigFlag(ped, 423, true);
            if (_state.SpookedState == JobState.SpookedType.NotSpooked)
            {
                // suppress agitation
                SetBlockingOfNonTemporaryEvents(ped, true);
            }
        }

        private void OnPedsSynced()
        {
            Debug.WriteLine($"{nameof(ShipJobController)}.{nameof(OnPedsSynced)}: {_pedHandles.Length} peds synced");
            AddNavmeshRequiredRegion(Origin.X, Origin.Y, ActiveRadius);
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
                foreach(int? ped in _pedHandles)
                {
                    if (ped.HasValue && IsPedStill(ped.Value))
                    {
                        Vector3 pos = GetEntityCoords(ped.Value, false);
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
                        AnimRequests.Request(PedToNet(PlayerPedId()), animToUse.Value);
                        Debug.WriteLine($"Blending in with anim {animToUse.Value.Dict} {animToUse.Value.Clip}");
                    }
                    else
                    {
                        Debug.WriteLine($"Both {nameof(scenarioToUse)} and {nameof(animToUse)} are null for ped {_blendInSourcePed}!");
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
        private bool _hasContactReactedToShowerYet = false;
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
                    AnimRequests.Request(PedToNet(playerPed), Constants.AnimNames.Shower.Get(isMale));
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
                            // FIXME: need to use something else here, doesn't seem to sync properly
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

                    if (!_hasContactReactedToShowerYet && PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
                    {
                        const double ContactCommentChance = 0.15;
                        bool shouldContactComment = s_RNG.NextDouble() > (1 - ContactCommentChance);
                        if (shouldContactComment)
                        {
                            float lineRng = (float)s_RNG.NextDouble();
                            lineRng = 0.76f;
                            string line = "wait did you just go into the sh- ah ok that's fine sure";
                            if (lineRng >= 0.95f)
                            {
                                line = "stinky";
                            }
                            else if (lineRng >= 0.75f)
                            {
                                line = "hah check this there's cams in bathrooms. WOAH ever thought of doing... \"content\"? just sayin";
                            }
                            else if (lineRng >= 0.5f)
                            {
                                line = "is this really the time?";
                            }

                            _hasContactReactedToShowerYet = true;
                            _pendingTexts.Add(new PendingText(0.75f, Constants.PhoneContacts.Esther, "shower?", line, 8.5f));
                        }
                    }
                }
            }
        }

        private bool _hasWarnedAboutDisguise = false;
        private bool _hasDispatchExplainedMission = false;

        protected override void OnActiveChanged(bool isActive)
        {
            base.OnActiveChanged(isActive);

            bool isAllowed = _jobUnlocked && (_state.LHasOutfit || Constants.Settings.IsDebug) && _state.JHasOutfit;
            if (isActive)
            {
                // Don't allow starting until it's unlocked
                if (_jobUnlocked)
                {
                    if (isAllowed)
                    {
                        if (_state.Stage == JobState.JobStage.WaitingToStart)
                        {
                            _state.Stage++;
                        }

                        if (_state.Stage == JobState.JobStage.LeaveArea)
                        {
                            _state.Stage = JobState.JobStage.Completed;
                        }

                        if (_shipNeedsOwner)
                        {
                            _shipNeedsOwner = false;
                            TryClaimShipOwnership();
                        }

                        if(!_hasDispatchExplainedMission && PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunters)
                        {
                            _hasDispatchExplainedMission = true;
                            _pendingTexts.Add(new PendingText(5f, PhoneContacts.Police, "Dignity target", "VIP holds top secret data on Pyre Twig TV dongle. New model looks like a hard drive.", 10f));
                            _pendingTexts.Add(new PendingText(12.5f, PhoneContacts.Police, "Dignity target", "Your plain clothes gear includes two trackers that'll geolocate incoming connections to the dongle.", 15f));
                        }
                    }
                    else
                    {
                        // Instruct the player to get a disguise
                        if(!_hasWarnedAboutDisguise && _localPlayerDisguise == DisguiseState.None)
                        {
                            _hasWarnedAboutDisguise = true;
                            if (PlayerState.Team != SurviveTheHuntShared.Core.Teams.Team.Hunters)
                            {
                                _pendingTexts.Add(new PendingText(0.75f, PhoneContacts.Esther, "dress code", "hey genius didn't i tell you to doll up a bit? they won't let you in wearing these rags"));
                            }
                        }
                    }
                }

                if(isAllowed)
                {
                    DisplayTextForObjective(_state.Stage);
                }

                if (isActive != isAllowed)
                {
                    IsActive = isAllowed;
                }
            }
            else if(isAllowed)
            {
                HUDUtils.ClearObjective();
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

        private bool _wasInDisguiseRangeLastTick = false;
        private void HandleDisguise(float deltaTime)
        {
            if(PlayerState.Team != SurviveTheHuntShared.Core.Teams.Team.Hunted || _localPlayerDisguise != Constants.DisguiseState.None)
            {
                return;
            }

            Vector3? closest = null;
            float closestSqDistance = float.MaxValue;
            const float MaximumDistance = 30f;
            const float MaxDistSq = MaximumDistance * MaximumDistance;
            Vector3 playerPos = GetEntityCoords(PlayerPedId(), false);
            Vector3[] markers = Constants.Location.PartyClothesMarkers.All;
            for (int i = 0; i < markers.Length; i++)
            {
                float distSq = playerPos.DistanceToSquared(markers[i]);
                if (distSq < closestSqDistance)
                {
                    closest = markers[i];
                    closestSqDistance = distSq;
                    if(distSq < MaxDistSq)
                    {
                        break;
                    }
                }
            }

            if(closest.HasValue)
            {
                const float Radius = 1.25f;
                DrawMarker((int)MarkerType.VerticalCylinder, closest.Value.X, closest.Value.Y, closest.Value.Z + Constants.Location.PartyClothesMarkers.ZOffset, 0f, 0f, 0f, 0f, 0f, 0f, Radius, Radius, 2f, Constants.Colours.ObjectiveMarkerColourR, Constants.Colours.ObjectiveMarkerColourG, Constants.Colours.ObjectiveMarkerColourB, Constants.Colours.ObjectiveMarkerColourA, false, false, 2, false, null, null, false);
                bool isInRange = closestSqDistance < (Radius * Radius);
                if(_wasInDisguiseRangeLastTick != isInRange)
                {
                    _wasInDisguiseRangeLastTick = isInRange;

                    if(isInRange)
                    {
                        BeginTextCommandDisplayHelp(PartyDisguisePickUpHelpKey);
                        EndTextCommandDisplayHelp(0, true, true, -1);
                    }
                    else
                    {
                        ClearAllHelpMessages();
                    }
                }

                if(isInRange)
                {
                    // Apply the hunted's partygoer disguise
                    if(IsControlJustPressed(0, (int)Control.Context))
                    {
                        SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType playerType = PlayerUtils.GetPlayerType(PlayerId(), GameState.Hunt.HuntedPlayers);
                        UpdatePlayerClothing(playerType, DirectedScene.Party, true);
                        _wasInDisguiseRangeLastTick = false;
                        _localPlayerDisguise = DisguiseState.Partygoer;
                        switch(playerType)
                        {
                            case SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType.HuntedJ:
                                _state.JHasOutfit = true;
                                break;
                            case SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType.HuntedL:
                                _state.LHasOutfit = true;
                                break;
                        }
                        ClearAllHelpMessages();
                        SetClothesBlipsShowing(false);
                        PlayerState.LoadoutIndex = 1;
                        PlayerState.TakeAwayWeapons(PlayerPedId());
                    }
                }
            }
        }

        private bool _jobUnlocked = false;

        public override sealed void OnHeatChanged(ushort heatScore, HeatThresholds heatThreshold)
        {
            base.OnHeatChanged(heatScore, heatThreshold);

            // Unlock the job after heat level 2
            if (heatThreshold >= HeatThresholds.Heat2 && !_jobUnlocked)
            {
                UnlockJob();
            }
        }

        private void UnlockJob()
        {
            if(_jobUnlocked)
            {
                return;
            }

            _jobUnlocked = true;
            bool needsDisguise = _localPlayerDisguise == DisguiseState.None;
            SetClothesBlipsShowing(needsDisguise && PlayerState?.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted);
            SetBlipDisplay(_jobBlip.Value, 6);

            bool isHunted = PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted;
            if (needsDisguise)
            {
                if (isHunted)
                {
                    _pendingTexts.Add(new PendingText(40f, PhoneContacts.Esther, "a job", "hey lovebirds. heard of the party at the Dignity? dress up and head there. i'll be in touch"));
                }
            }

            if(!isHunted)
            {
                _pendingTexts.Add(new PendingText(150f, PhoneContacts.Police, "ALL UNITS", "Be advised: assistance needed guarding VIP party at Dignity yacht. Code TRP2", 20f));
                _pendingTexts.Add(new PendingText(165f, PhoneContacts.Police, "Plain clothes policy", "Partygoer crowd must not see officers in uniform. Use undercover gear from your service vehicle.", 20f));
            }
        }

        private void TickAmbient(float deltaTime)
        {
            _timeSinceLastRangeCheck += deltaTime;

            if(_clothesBlipsShowingDoNotSet)
            {
                HandleDisguise(deltaTime);
            }

            bool wasInRangeBeforeCheck = _wasPlayerInRangeLastTick;
            if(_timeSinceLastRangeCheck >= RangeCheckIntervalSeconds)
            {
                _wasPlayerInRangeLastTick = GetEntityCoords(PlayerPedId(), false).DistanceToSquared(Origin) <= (ActiveRadius * ActiveRadius);
                _timeSinceLastRangeCheck = 0f;
            }

            // Deactivate the job when out of bounds
            if(wasInRangeBeforeCheck && !_wasPlayerInRangeLastTick)
            {
                IsActive = false;
            }

            if(IsLocalPlayerPedGod)
            {
                if(_optionalPedNetIds != null)
                {
                    foreach(int netId in _optionalPedNetIds)
                    {
                        // FIXME: one of these sometimes spams warnings that the net ID doesn't exist
                        SetNetworkIdAlwaysExistsForPlayer(netId, PlayerId(), true);
                        NetworkDisableProximityMigration(netId);
                        SetNetworkIdCanMigrate(netId, false);
                        NetworkRequestControlOfNetworkId(netId);

                        if (NetworkDoesNetworkIdExist(netId) && NetworkDoesEntityExistWithNetworkId(netId))
                        {
                            int ped = NetToPed(netId);
                            Vector3 pos = GetEntityCoords(ped, false);
                            RequestCollisionAtCoord(pos.X, pos.Y, pos.Z);
                            RequestAdditionalCollisionAtCoord(pos.X, pos.Y, pos.Z);
                        }
                    }
                }
            }
            else
            {
                if(_optionalPedNetIds != null)
                {
                    foreach(int netId in _optionalPedNetIds)
                    {
                        if (NetworkDoesNetworkIdExist(netId) && NetworkHasControlOfNetworkId(netId))
                        {
                            Debug.WriteLine($"We have control of net {netId} but we're not ped god. Relinquishing...");
                            SetNetworkIdCanMigrate(netId, true);
                            NetworkSetNetworkIdDynamic(netId, true);
                        }
                    }
                }
            }

            if (_pedsNeedSyncing)
            {
                if (_pedHandles.Length != _pedNetIds.Length)
                {
                    _pedHandles = new int?[_pedNetIds.Length];
                }

                bool allSynced = true;
                for (int i = 0; i < _pedNetIds.Length; i++)
                {
                    bool wasAlreadySynced = _pedHandles[i].HasValue;
                    bool exists = wasAlreadySynced || NetworkDoesNetworkIdExist(_pedNetIds[i]);
                    allSynced = allSynced && exists;
                    if (exists && !wasAlreadySynced)
                    {
                        _pedHandles[i] = NetToPed(_pedNetIds[i]);
                        SetPedAsFriendly(_pedHandles[i].Value);
                    }
                }

                if (allSynced)
                {
                    _pedsNeedSyncing = false;
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

            AnimRequests.Tick(deltaTime);

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

            if(!_jobBlip.HasValue)
            {
                _jobBlip = AddBlipForCoord(Origin.X, Origin.Y, Origin.Z);
                SetBlipSprite(_jobBlip.Value, 455);
                SetBlipDisplay(_jobBlip.Value, _jobUnlocked ? 6 : 0);
                SetBlipColour(_jobBlip.Value, (int)BlipColor.Yellow);
                SetBlipNameFromTextFile(_jobBlip.Value, BlipNameTextKey);
            }

            int pendingTextToProcess = -1;
            for(int i = 0; i < _pendingTexts.Count; i++)
            {
                _pendingTexts[i].RemainingTime -= deltaTime;
                if(pendingTextToProcess == -1 && _pendingTexts[i].RemainingTime <= 0f)
                {
                    pendingTextToProcess = i;
                }
            }

            if (pendingTextToProcess != -1)
            {
                PendingText pendingText = _pendingTexts[pendingTextToProcess];
                _pendingTexts.RemoveAt(pendingTextToProcess);
                PhoneTextHelper.SendText(pendingText.Sender, pendingText.Subject, pendingText.Message, pendingText.Duration);
            }
        }

        private void OnPedsSpawned(int[] entityHandles, Dictionary<int, PedNode> optionalPedInitStates)
        {
            Debug.WriteLine($"{nameof(OnPedsSpawned)}: spawned {entityHandles.Length} peds");

            List<object> netIds = new List<object>(entityHandles.Length);

            foreach(int pedHandle in entityHandles)
            {
                SetEntityMaxHealth(pedHandle, 500);
                int netId = PedToNet(pedHandle);
                netIds.Add(netId);
                Debug.WriteLine($"Sending net ID {netId} for ped handle {pedHandle}");
                _timeTillPedBrainTick[netId] = s_RNG.Next(0, 45);
                NetworkSetNetworkIdDynamic(netId, true);
            }

            _state.PedNetIds = netIds;

            _pedSpawner = null;

            _optionalPedInitStates = optionalPedInitStates;
            _pedLocations = new PedNode[optionalPedInitStates.Count];
            optionalPedInitStates.Values.CopyTo(_pedLocations, 0);
            _optionalPedHandles = new int[optionalPedInitStates.Count];
            optionalPedInitStates.Keys.CopyTo(_optionalPedHandles, 0);

            int counter = 0;
            _optionalPedNetIds = new int[_optionalPedHandles.Length];
            foreach(int pedHandle in _optionalPedHandles)
            {
                SetEntityLoadCollisionFlag(pedHandle, true);
                SetEntityAsMissionEntity(pedHandle, true, true);
                _optionalPedNetIds[counter++] = PedToNet(pedHandle);
            }

            AddNavmeshRequiredRegion(Origin.X, Origin.Y, ActiveRadius);
        }

        private float _timeSinceObjectiveDistanceCheck = 0f;
        private const float ObjectiveDistanceCheckIntervalSeconds = 0.35f;
        private bool _canInteractWithDevice = false;
        private float _timeTillHackDisrupted = float.MaxValue;
        private float _hackSecondsElapsed = 0f;
        private bool _isCurrentHacker = false;
        private const float HackProgressSyncIntervalSeconds = 3.5f;
        private float _timeSinceHackProgressSync = 0f;
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

                // Prevent hunters from being able to start the hack...
                if(_canInteractWithDevice && PlayerState?.Team == SurviveTheHuntShared.Core.Teams.Team.Hunters)
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

            // Periodically sync the hack progress to other players
            if(_state.Stage == JobState.JobStage.SurviveHack)
            {
                _timeSinceHackProgressSync += deltaTime;
                if(_timeSinceHackProgressSync >= HackProgressSyncIntervalSeconds)
                {
                    _timeSinceHackProgressSync = 0f;
                    _state.HackProgress = _hackSecondsElapsed;
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
        private const float CruisegoerSpookCheckIntervalSeconds = 5f;
        private float _timeSinceCopSpookCheck = 0f;
        private Constants.DisguiseState _localPlayerDisguise = Constants.DisguiseState.None;
        private void HandleHuntersObjective(float deltaTime)
        {
            bool isHunter = !GameState.Hunt.IsHunted(PlayerId(), out _);
            bool hasUndercoverGear = _localPlayerDisguise != Constants.DisguiseState.None;

            _timeSinceCopSpookCheck += deltaTime;
            if (_timeSinceCopSpookCheck > CruisegoerSpookCheckIntervalSeconds)
            {
                _timeSinceCopSpookCheck = 0f;
                if (isHunter && !hasUndercoverGear)
                {
                    if (_state.SpookedState == JobState.SpookedType.NotSpooked)
                    {
                        Debug.WriteLine($"Spooking {_pedHandles?.Length} peds because the fuzz showed up");
                        _state.SpookedState = JobState.SpookedType.SpookedByCops;
                    }

                    if (_state.SpookedState == JobState.SpookedType.SpookedByCops)
                    {
                        if (_pedHandles?.Length > 0)
                        {
                            int playerPed = PlayerPedId();
                            foreach (int? ped in _pedHandles)
                            {
                                if (ped.HasValue)
                                {
                                    //uint pedGroup = (uint)GetPedRelationshipGroupHash(ped.Value);
                                    SetPedCombatMovement(ped.Value, 3);
                                    SetPedCombatAbility(ped.Value, 2);
                                    SetBlockingOfNonTemporaryEvents(ped.Value, false);
                                    //TaskAgitatedAction(ped, playerPed);
                                    TaskCombatPed(ped.Value, playerPed, 0, 16);
                                    //TaskCombatHatedTargetsAroundPed(ped, ActiveRadius, 0);
                                }
                            }
                        }
                    }
                }
            }

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

        private bool _hasShotLastTick = false;
        private const float PlayerShootingCheckIntervalSeconds = 1f;
        private float _timeSincePlayerShootingCheck = 0f;
        private void TickActive(float deltaTime)
        {
            HandleHuntedObjective(deltaTime);
            HandleHuntersObjective(deltaTime);

            if(_pedHandles?.Length > 0)
            {
                if (IsPedShooting(PlayerPedId()))
                {
                    _hasShotLastTick = true;
                }
            }

            if (_state.SpookedState == JobState.SpookedType.NotSpooked)
            {
                _timeSincePlayerShootingCheck += deltaTime;
                if (_timeSincePlayerShootingCheck >= PlayerShootingCheckIntervalSeconds)
                {
                    const float HeardShotsRadius = ActiveRadius * 0.825f;
                    if (_hasShotLastTick && GetEntityCoords(PlayerPedId(), false).DistanceToSquared(Origin) <= (HeardShotsRadius * HeardShotsRadius))
                    {
                        _state.SpookedState = JobState.SpookedType.SpookedByGunfire;
                    }

                    _hasShotLastTick = false;
                }
            }
        }

        private float _timeSinceLastPedTargetCheck = 0f;
        private const float PedTargetCheckIntervalSeconds = 1.5f;

        private void RunPedBrain(float deltaTime)
        {
            // let peds panic
            if(_state.SpookedState != JobState.SpookedType.NotSpooked)
            {
                return;
            }

            if(_pedHandles != null)
            {
                bool tickedAnyBrains = false;

                List<int> pedsToTick = new List<int>();
                foreach(int pedNetId in _optionalPedNetIds)
                {
                    _timeTillPedBrainTick[pedNetId] -= deltaTime;
                    if (_timeTillPedBrainTick[pedNetId] <= 0f)
                    {
                        tickedAnyBrains = true;
                        pedsToTick.Add(pedNetId);
                    }
                }

                if (tickedAnyBrains)
                {

                    List<PedNode> pickablePedTargets = new List<PedNode>(_pedLocations);

                    foreach(int pedNetId in pedsToTick)
                    {
                        NetworkRequestControlOfNetworkId(pedNetId);
                        int ped = NetToPed(pedNetId);
                        NetworkRequestControlOfEntity(ped);
                        //Debug.WriteLine($"{nameof(RunPedBrain)}: updating ped {ped}'s task");


                        if (HasCollisionLoadedAroundEntity(ped))
                        {
                            //Debug.WriteLine($"We've got collision for ped {ped} (net: {pedNetId}), tasking...");
                        }
                        else
                        {
                            Debug.WriteLine($"Cannot task ped {ped} (net: {pedNetId}) as collision not loaded");
                        }

                        // Stop the previous task
                        ClearPedTasks(ped);
                        PedNode? initState = null;
                        if(_optionalPedInitStates.ContainsKey(pedNetId))
                        {
                            initState = _optionalPedInitStates[pedNetId];
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

                        _optionalPedTargetStates[pedNetId] = newTarget;

                        _optionalPedInitStates.Remove(pedNetId);

                        // Randomise the time between ped brain ticks so people don't just start walking all at the same time
                        _timeTillPedBrainTick[pedNetId] = PedBrainTickIntervalSeconds + (float)s_RNG.Next(0, 45);
                    }
                }
                else
                {
                    if (_timeSinceLastPedTargetCheck >= PedTargetCheckIntervalSeconds)
                    {
                        _timeSinceLastPedTargetCheck = 0f;

                        foreach (int pedNetId in _optionalPedNetIds)
                        {
                            if (_optionalPedTargetStates.ContainsKey(pedNetId))
                            {
                                PedNode target = _optionalPedTargetStates[pedNetId];
                                int ped = NetToPed(pedNetId);
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
                                        _optionalPedTargetStates.Remove(pedNetId);
                                        _optionalPedInitStates[pedNetId] = target;

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
                                            AnimRequests.Request(pedNetId, anim);
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
            RemoveNavmeshRequiredRegions();

            Debug.WriteLine($"{nameof(ShipJobController)}.{nameof(Cleanup)}({nameof(force)}: {force})");
            Debug.WriteLine($"Cleaning up {_pedHandles.Length} peds");
            for(int i = 0; i < _pedHandles.Length; i++)
            {
                Debug.WriteLine($"Trying to delete ped {_pedHandles[i]}");
                if (_pedHandles[i].HasValue)
                {
                    int ped = _pedHandles[i].Value;
                    SetEntityAsMissionEntity(ped, true, true);
                    DeletePed(ref ped);
                    Debug.WriteLine("Deleted");
                }
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

            if(_gunfireEvent.HasValue)
            {
                RemoveShockingEvent(_gunfireEvent.Value);
                _gunfireEvent = null;
            }

            if(_jobBlip.HasValue)
            {
                int blip = _jobBlip.Value;
                RemoveBlip(ref blip);
                _jobBlip = null;
            }

            foreach(int blip in _clothingBlips)
            {
                int blipCopy = blip;
                RemoveBlip(ref blipCopy);
            }

            AnimRequests.Cleanup();
        }

        public void OnDisguiseChanged(SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType playerType, Constants.DisguiseState disguise)
        {
            _localPlayerDisguise = disguise;
            bool hasOutfit = disguise == Constants.DisguiseState.Partygoer;
            switch (playerType)
            {
                case SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType.HuntedJ:
                    _state.JHasOutfit = hasOutfit;
                    break;
                case SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType.HuntedL:
                    _state.LHasOutfit = hasOutfit;
                    break;
            }
        }
    }
}
