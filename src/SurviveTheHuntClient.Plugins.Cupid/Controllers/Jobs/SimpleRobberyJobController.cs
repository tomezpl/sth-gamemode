using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Models.UI;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs.SimpleRobberyJobState;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs
{
    internal class SimpleRobberyJobState : JobStateBase
    {
        internal enum Stage
        {
            /// <summary>
            /// Waiting for the local player to activate the trigger
            /// </summary>
            WaitForHelpTrigger,

            /// <summary>
            /// Player is presented with the help trigger and is aware that they need to press a button to start the robbery.
            /// </summary>
            WaitForStart,

            /// <summary>
            /// Player is holding out while grabbing the money.
            /// </summary>
            TakingMoney,

            /// <summary>
            /// Player has to leave.
            /// </summary>
            LeaveArea,
        }

        internal event GenericStateChangedEvent<Stage> StageChanged;

        private Stage _stage = Stage.WaitForHelpTrigger;
        internal Stage JobStage
        {
            get => _stage;
            set
            {
                Stage old = _stage;
                _stage = value;
                if(old != value)
                {
                    StageChanged.Invoke(old, value, CanSync && value > Stage.WaitForStart);
                }
            }
        }

        internal event GenericStateChangedEvent<float> GrabProgressChanged;

        private float _grabProgress = 0f;
        internal float GrabProgress
        {
            get => _grabProgress;
            set
            {
                float old = _grabProgress;
                _grabProgress = value;
                if(old != value)
                {
                    GrabProgressChanged.Invoke(old, value, CanSync);
                }
            }
        }

        private enum PropId
        {
            JobStage,
            GrabProgress
        }

        internal override void SetImpl(int statePropId, object statePropValue)
        {
            PropId propId = (PropId)statePropId;
            switch(propId)
            {
                case PropId.JobStage:
                    JobStage = (Stage)statePropValue;
                    break;
                case PropId.GrabProgress:
                    GrabProgress = (float)statePropValue;
                    break;
            }
        }

        internal override Dictionary<int, object> Get()
        {
            return new Dictionary<int, object>
            {
                { (int)PropId.JobStage, Get((int)PropId.JobStage) },
                { (int)PropId.GrabProgress, Get((int)PropId.GrabProgress) },
            };
        }

        internal override object Get(int statePropId)
        {
            switch((PropId)statePropId)
            {
                case PropId.JobStage:
                    return JobStage;
                case PropId.GrabProgress:
                    return GrabProgress;
            }

            throw new ArgumentException($"{nameof(statePropId)} {statePropId} is not a valid {nameof(PropId)}");
        }
    }

    internal class SimpleRobberyJobController : JobControllerBase
    {
        private const ushort _heatValue = (ushort)Constants.HeatValues.Low;

        internal override ushort HeatValue => _heatValue;

        private SimpleRobberyJobState _state = new SimpleRobberyJobState();

        protected override JobStateBase State => _state;

        private class UIState
        {
            private bool _isStartHelpTextShowing = false;

            internal bool IsStartHelpTextShowing
            {
                get => _isStartHelpTextShowing;
                set
                {
                    if(value == false && _isStartHelpTextShowing == true)
                    {
                        ClearAllHelpMessages();
                    }

                    _isStartHelpTextShowing = value;
                }
            }

            internal readonly LabelledItem GrabbingProgress = new LabelledItem("Progress", 0f, SurviveTheHuntShared.Utils.EncodingHelper.PackRgba(48, 224, 48, 255));
        }

        private UIState _uiState = new UIState();

        internal const float DefaultStartTriggerRadius = 2.2f;

        /// <summary>
        /// The distance after which the job will become inactive.
        /// </summary>
        internal const float ActiveRadius = 25f;

        internal const float ActiveRadiusSq = ActiveRadius * ActiveRadius;

        internal readonly float StartTriggerRadiusSq;
        private Vector3 _startTriggerPos;

        /// <summary>
        /// The position where the player starts the job
        /// </summary>
        internal Vector3 StartTriggerPos => _startTriggerPos;

        private Vector3 _targetGrabTriggerPos;

        /// <summary>
        /// The position of the target that the player will be stealing
        /// </summary>
        internal Vector3 TargetGrabTriggerPos => _targetGrabTriggerPos;

        /// <summary>
        /// The radius in which the target will be grabbed
        /// </summary>
        internal const float TargetGrabRadius = 1.35f;
        internal const float TargetGrabRadiusSq = TargetGrabRadius * TargetGrabRadius;

        internal const float TargetGrabRate = 0.1f;
        internal const float TargetGrabInterval = 0.1f;

        internal enum RobberyType
        {
            Bank,
            Store,
        }

        internal readonly RobberyType Type;

        private int _blipId;

        internal readonly float StartTriggerRadius;

        private bool _cleanedUp = false;

        private static BlipSprite GetBlipForType(RobberyType type)
        {
            switch(type)
            {
                case RobberyType.Bank:
                    return BlipSprite.DollarSign;
                case RobberyType.Store:
                    return BlipSprite.Store;
                default:
                    return BlipSprite.StrangersAndFreaks;
            }
        }

        internal SimpleRobberyJobController(string jobId, JobStateRpcUpdateDelegate updateState, RobberyType type, in Vector3 startPos, float radius = DefaultStartTriggerRadius) : base(jobId, updateState)
        {
            _startTriggerPos = startPos;
            StartTriggerRadiusSq = radius * radius;
            StartTriggerRadius = radius;

            Type = type;

            _state.StageChanged += OnStageChanged;
            _state.GrabProgressChanged += OnGrabProgressChanged;
        }

        private void OnGrabProgressChanged(float prev, float current, bool canSync)
        {
            _uiState.GrabbingProgress.Value = SurviveTheHuntShared.Utils.EncodingHelper.Utf16FromNormalFloat(current);

            if(canSync)
            {
                // TODO: can probably scope this to just the GrabProgress prop?
                SyncState();
            }
        }

        internal void SetStage(Stage stage)
        {
            Stage prevStage = _state.JobStage;
            _state.JobStage = stage;

            if(prevStage != stage)
            {
                Debug.WriteLine($"{nameof(SimpleRobberyJobController)}.{nameof(SetStage)}({stage}): job stage changed!");
            }
        }

        private void OnStageChanged(Stage prev, Stage current, bool canSync)
        {
            Debug.WriteLine($"{nameof(SimpleRobberyJobController)}.{nameof(OnStageChanged)}({nameof(prev)}: {prev}, {nameof(current)}: {current})");

            if (canSync)
            {
                SyncState();
            }
        }

        protected override void OnActiveChanged(bool isActive)
        {
            base.OnActiveChanged(isActive);

            if(_state.JobStage == SimpleRobberyJobState.Stage.WaitForHelpTrigger)
            {
                SetStage(_state.JobStage + 1);
            }

            _uiState.IsStartHelpTextShowing = false;

            // Hide the job blip when it's active, unhide when inactive
            SetBlipDisplay(_blipId, isActive ? 0 : 6);
        }

        internal override sealed bool IsInTrigger
        {
            get
            {
                float distanceSq = GetEntityCoords(PlayerPedId(), false).DistanceToSquared(StartTriggerPos);
                //Debug.WriteLine($"{nameof(SimpleRobberyJobController)}.{nameof(IsInTrigger)}: {nameof(distanceSq)} = {distanceSq}, needs to be less than {nameof(TargetTriggerRadiusSq)} = {TargetTriggerRadiusSq}: {distanceSq <= TargetTriggerRadiusSq}");
                return distanceSq <= StartTriggerRadiusSq;
            }
        }

        internal sealed override void Start(IPlayerState playerState, IGameState gameState)
        {
            base.Start(playerState, gameState);

            _blipId = AddBlipForCoord(_startTriggerPos.X, _startTriggerPos.Y, _startTriggerPos.Z);
            SetBlipSprite(_blipId, (int)GetBlipForType(Type));
            //SetBlipAlpha(_blipId, 64);
            SetBlipDisplay(_blipId, 6);
            //SetBlipColour(_blipId, (int)BlipColor.White);

            Debug.WriteLine($"{nameof(SimpleRobberyJobController)} started");
        }

        internal sealed override void Cleanup()
        {
            if(!_cleanedUp)
            {
                _cleanedUp = true;

                RemoveBlip(ref _blipId);
            }
        }

        public override sealed void Tick(float deltaTime)
        {
            TickAmbient(deltaTime);

            if (!IsActive || _cleanedUp)
            {
                return;
            }

            TickActive(deltaTime);
        }

        private const byte MaxUiItems = 1;
        private LabelledItem[] _currentUi = new LabelledItem[MaxUiItems];
        internal override LabelledItem[] CurrentUI
        {
            get
            {
                if(_state.JobStage == Stage.TakingMoney)
                {
                    _currentUi[0] = _uiState.GrabbingProgress;
                    return _currentUi;
                }

                return LabelledItem.Empty;
            }
        }

        /// <summary>
        /// Called every tick regardless of whether the job is active or not
        /// </summary>
        /// <param name="deltaTime"></param>
        private void TickAmbient(float deltaTime)
        {
            if (_state.JobStage <= SimpleRobberyJobState.Stage.WaitForStart)
            {
                DrawMarker((int)MarkerType.VerticalCylinder, _startTriggerPos.X, _startTriggerPos.Y, _startTriggerPos.Z, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 255, 255, 128, false, false, 2, false, null, null, false);
            }
        }

        /// <summary>
        /// Called every tick while the job is active
        /// </summary>
        /// <param name="deltaTime"></param>
        private void TickActive(float deltaTime)
        {
            int playerPed = PlayerPedId();

            bool isPlayerInArea = false;

            bool needsToDeactivate = false;

            // Deactivate the job if all hunted players are outside the area
            if(PlayerState.Team == SurviveTheHuntShared.Core.Teams.Team.Hunted)
            {
                bool allOutsideArea = true;

                foreach(HuntPlayer player in GameState.Hunt.HuntedPlayers)
                {
                    int ped = GetPlayerPed(player.PlayerHandle);

                    Vector3 coords = GetEntityCoords(ped, false);
                    if(coords.DistanceToSquared(_startTriggerPos) <= ActiveRadiusSq)
                    {
                        allOutsideArea = false;

                        // While we're scanning through players in the area, might as well store whether the local player is in the area
                        if(ped == playerPed)
                        {
                            isPlayerInArea = true;
                            break;
                        }
                    }
                }

                needsToDeactivate = needsToDeactivate || allOutsideArea;
            }

            if(isPlayerInArea)
            {
                if(_state.JobStage == SimpleRobberyJobState.Stage.WaitForStart)
                {
                    if(!_uiState.IsStartHelpTextShowing)
                    {
                        const string labelKey = "STH_CUPID_JOB_SIMPLEROBBERY_START";
                        AddTextEntry(labelKey, "Press ~INPUT_CONTEXT~ to start the robbery.");
                        BeginTextCommandDisplayHelp(labelKey);
                        EndTextCommandDisplayHelp(0, true, true, -1);

                        _uiState.IsStartHelpTextShowing = true;
                    }

                    if (IsControlJustPressed(0, (int)Control.Context))
                    {
                        SetStage(_state.JobStage + 1);
                        _uiState.IsStartHelpTextShowing = false;
                    }
                }

                if(_state.JobStage == Stage.TakingMoney)
                {
                    
                }
            }

            IsActive = !needsToDeactivate;
        }
    }
}
