using CitizenFX.Core;
using SurviveTheHuntClient.Attributes;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Models.UI;
using SurviveTheHuntClient.Plugins.Cupid.Controllers;
using SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs;
using SurviveTheHuntClient.Plugins.Cupid.Helpers;
using SurviveTheHuntClient.Plugins.Cupid.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Managers;
using SurviveTheHuntClient.Plugins.Cupid.SceneHandlers.Intro;
using SurviveTheHuntClient.Plugins.Cupid.Utils;
using SurviveTheHuntShared.Core;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntClient.Plugins.Cupid.Constants;
using PlayerType = SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType;

namespace SurviveTheHuntClient.Plugins.Cupid
{
    public sealed class CupidPlugin : Plugin<CupidPlugin.Events>, ITickable
    {
        public override string GameModeTitle => "Valentines";
        public override string GameModeDescription => "TODO";

        public override bool IsGameMode => true;

        internal IGameState GameState;
        internal IPlayerState PlayerState;

        private readonly BleedoutController BleedoutController;
        private JobManager JobManager = null;
        private HeatController HeatController = new HeatController();

        private CopSpawnController CopSpawnController;

        private readonly UIMenuHelper UIMenuHelper;

        private class SubscriberState
        {
            internal readonly List<INetEntityListener> NetEntity = new List<INetEntityListener>();
            internal readonly List<ISpecialEventListener> SpecialEvent = new List<ISpecialEventListener>();
            internal readonly List<IHuntLifecycleListener> HuntLifecycle = new List<IHuntLifecycleListener>();
            internal readonly List<IHeatListener> Heat = new List<IHeatListener>();
        }

        private SubscriberState Subscribers = new SubscriberState();

        internal class PluginState
        {
            public PlayerType LocalRole = PlayerType.Cop;
            private float CurrentClothesChangeDelaySeconds = -1f;
            public float CurrentClothesChangeElapsedSeconds = 0f;
            public const float ClothesChangeDelay = 1f;

            public void RequestClothesChange(bool request = true)
            {
                CurrentClothesChangeDelaySeconds = request ? ClothesChangeDelay : -1f;
                CurrentClothesChangeElapsedSeconds = 0f;
            }

            public bool IsWaitingForClothesChange => CurrentClothesChangeDelaySeconds >= 0f;
            public bool ShouldChangeClothes => CurrentClothesChangeElapsedSeconds > CurrentClothesChangeDelaySeconds;

            internal Constants.DirectedScene CurrentScene = Constants.DirectedScene.IntroJason;

            /// <summary>
            /// The "heat" score awarded to hunted players for doing jobs. They need to reach <see cref="Constants.HeatValues.Target"/> to win.
            /// </summary>
            internal ushort HuntedHeatScore = 0;

            internal Constants.Clothing.OutfitPair? LastWornOutfit = null;

            internal bool HasRunPostIntro = false;

            internal int TulipNetId = 0;

            internal int TulipBlip = 0;
        }

        private int ScriptCamera;

        private PluginState _state = new PluginState();

        internal PluginState State { get => _state; }

        private Dictionary<DirectedScene, ISceneHandler> SceneHandlers;

        private ISceneHandler PostGameScene = null;

        private static DirectedScene[] GetAllHandledScenes(Dictionary<DirectedScene, ISceneHandler> sceneHandlers)
        {
            Array allValues = Enum.GetValues(typeof(DirectedScene));
            List<DirectedScene> defined = new List<DirectedScene>();

            for(int i = 0; i < allValues.Length; i++)
            {
                foreach(DirectedScene handledScene in sceneHandlers.Keys)
                {
                    if(handledScene == (DirectedScene)allValues.GetValue(i))
                    {
                        defined.Add(handledScene);
                        break;
                    }
                }
            }

            return defined.ToArray();
        }

        private readonly DirectedScene _lastScene;

        private const string PersonalVehicleBlipNameKey = "STH_CUPID_BLIP_PERSONALVEH";
        private const string PersonalVehicleBlipNameContent = "Personal Vehicle";

        private readonly RepairShopManager RepairShopManager = new RepairShopManager();

        private CellTowerPingController CellTowerPingController = null;

        private HeliFerryHelper ShipHeliFerryHelper = null;

        private static bool Init()
        {
            if(!s_HasInit)
            {
                AddTextEntry(PersonalVehicleBlipNameKey, PersonalVehicleBlipNameContent);
            }

            return true;
        }

        private readonly static bool s_HasInit = Init();

        /// <summary>
        /// Constructor for the valentines plugin (except we're so fucking late for valentines...)
        /// </summary>
        /// <param name="context"></param>
        public CupidPlugin(PluginContext context) : base("cupid", context)
        {
            SceneHandlers.Intro.IntroJ2Handler jas2Handler = new SceneHandlers.Intro.IntroJ2Handler(context.TriggerEventProxy, context.TriggerServerEventProxy);

            SceneHandlers = new Dictionary<DirectedScene, ISceneHandler>()
            {
                { DirectedScene.IntroJason, new SceneHandlers.Intro.IntroJ1Handler(context.TriggerEventProxy, context.TriggerServerEventProxy) },
                { DirectedScene.JasonDrivingHood, jas2Handler }
            };

            jas2Handler.OutroReached += OnIntroOutroReached;

            if (Constants.Settings.IsDebug)
            {
                RegisterCommand("heat", new Action<int, List<object>, string>((player, args, raw) =>
                {
                    ushort heat = Convert.ToUInt16(args[0]);
                    HeatController.CurrentHeat = Math.Max(HeatController.CurrentHeat, heat);
                }), false);
            }

            DirectedScene[] allScenes = GetAllHandledScenes(SceneHandlers);
            _lastScene = allScenes[allScenes.Length - 1];

            BleedoutController = new BleedoutController(context.ChangeGameModeSetting, context.TriggerServerEventProxy);
            BleedoutController.StartedDying += OnStartedDying;
            BleedoutController.FinishedDying += OnFinishedDying;

            UIMenuHelper = new UIMenuHelper(context.TriggerEventProxy);

            CopSpawnController = CreateCopSpawnController(CopSpawnController);

            Subscribers.HuntLifecycle.Add(RepairShopManager);
        }

        private void OnIntroOutroReached()
        {
            SetPlayerClothing(State.LocalRole, DirectedScene.Default1, true);

            if (_state.LocalRole == PlayerType.Cop)
            {
                CopSpawnController.Enabled = true;
            }
        }

        private CopSpawnController CreateCopSpawnController(CopSpawnController old = null)
        {
            old?.Cleanup();

            if(old != null)
            {
                Subscribers.NetEntity.Remove(old);
                Subscribers.HuntLifecycle.Remove(old);
                Subscribers.Heat.Remove(old);
                Subscribers.SpecialEvent.Remove(old);
            }

            CopSpawnController newInstance = new CopSpawnController(new AVControllerHelper(TriggerEventProxy), TriggerServerEventProxy);

            Subscribers.NetEntity.Add(newInstance);
            Subscribers.HuntLifecycle.Add(newInstance);
            Subscribers.Heat.Add(newInstance);
            Subscribers.SpecialEvent.Add(newInstance);
            newInstance.DisguiseStateChanged += OnCopDisguiseStateChanged;
            
            return newInstance;
        }

        private void OnCopDisguiseStateChanged(DisguiseState disguise)
        {
            ShipJobController shipJob = JobManager?.Get<ShipJobController>("ship");
            if(shipJob != null)
            {
                shipJob.OnDisguiseChanged(PlayerType.Cop, disguise);
            }

            if(PlayerState != null)
            {
                PlayerState.LoadoutIndex = (byte)(disguise == DisguiseState.UndercoverCop ? 1 : 0);
                PlayerState.TakeAwayWeapons(PlayerPedId());
            }

            if(disguise == DisguiseState.UndercoverCop)
            {
                SetPedHelmet(PlayerPedId(), true);
            }
            
            if(disguise == DisguiseState.None && PlayerUtils.GetPlayerType(PlayerId(), GameState.Hunt.HuntedPlayers) == PlayerType.Cop)
            {
                SetPedHelmet(PlayerPedId(), false);
            }
        }

        private void OnHeatTierChanged(HeatThresholds prev, HeatThresholds current)
        {
            Debug.WriteLine($"Heat tier changed to {current}");
            HeatThresholds[] allThresholds = { HeatThresholds.Start, HeatThresholds.Heat1, HeatThresholds.Heat2, HeatThresholds.Target };
            foreach (HeatThresholds threshold in allThresholds)
            {
                if (threshold > prev && threshold <= current)
                {
                    switch (threshold)
                    {
                        case HeatThresholds.Heat1:
                            State.CurrentScene = DirectedScene.Default2;
                            break;
                    }
                }
            }

            JobManager?.OnHeatChanged((ushort)current, current);

            SetPlayerClothing(State.LocalRole, State.CurrentScene, true);
        }

        private void OnFinishedDying(int playerPed)
        {
            Debug.WriteLine($"{playerPed} died.");

            // If it's the local player that died, disable the BleedoutController,
            // so auto-respawn and death detection from the main client script can work.
            BleedoutController.Enabled = false;
        }

        private void OnStartedDying(int playerPed)
        {
            Debug.WriteLine($"{playerPed} started dying.");

            TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.CupidNotifyRevivable, PedToNet(playerPed));
        }

        public class Events : PluginEvents
        {
            private new CupidPlugin _plugin { get => (CupidPlugin)base._plugin; }

            [SthNamedEvent(SurviveTheHuntShared.Events.Client.CupidReceiveRevivable)]
            public void ReceiveRevivablePed(int revivablePedNetId)
            {
                int pedId = NetToPed(revivablePedNetId);

                if(Constants.Settings.AllowSelfRevive || pedId != PlayerPedId())
                {
                    _plugin.BleedoutController.SetRevivable(pedId, true);
                }
            }

            [SthNamedEvent(SurviveTheHuntShared.Events.Client.CupidReceiveEndRevive)]
            public void ReceiveEndRevive(int revivedPedNetId, bool cancelled)
            {
                int pedId = NetToPed(revivedPedNetId);

                if(!cancelled)
                {
                    _plugin.BleedoutController.SetRevivable(pedId, false);

                    ResurrectPed(pedId);
                    if (pedId == PlayerPedId())
                    {
                        Vector3 coords = GetEntityCoords(pedId, false);
                        float heading = GetEntityHeading(pedId);
                        NetworkResurrectLocalPlayer(coords.X, coords.Y, coords.Z, heading, false, false);
                    }
                    _plugin.BleedoutController.OnRevive(pedId);
                }
                else
                {
                    _plugin.BleedoutController.OnCancelRevive(pedId);
                }
            }

            [SthNamedEvent(SurviveTheHuntShared.Events.Client.CupidReceiveStartRevive)]
            public void ReceiveStartRevive(int revivedPetNetId)
            {
                int pedId = NetToPed(revivedPetNetId);

                _plugin.BleedoutController.OnStartRevive(pedId);
            }

            [SthNamedEvent(SurviveTheHuntShared.Events.Client.CupidReceiveHeatScore)]
            public void ReceiveNewHeatScore(int heatScore)
            {
                // if two clients sent a heat score at the "same" time, make sure we stick with the highest one
                ushort heatScoreFinal = Math.Max(_plugin.State.HuntedHeatScore, (ushort)heatScore);

                _plugin.HeatController.CurrentHeat = heatScoreFinal;
                _plugin.State.HuntedHeatScore = heatScoreFinal;
            }

            [SthNamedEvent(SurviveTheHuntShared.Events.Client.CupidReceiveSyncState)]
            public void ReceiveSyncJobState(string jobId, int statePropId, object statePropValue)
            {
                _plugin.JobManager.OnStateReceived(jobId, statePropId, statePropValue);
            }

            [SthNamedEvent(SurviveTheHuntShared.Events.Client.CupidReceiveSpecialEvent)]
            public void ReceiveSpecialEvent(object specialEventType, List<object> args)
            {
                object[] argsArray = args.ToArray();
                Constants.SpecialEvent specialEvent = (Constants.SpecialEvent)Convert.ToInt32(specialEventType);
                //Debug.WriteLine($"Received special event {specialEventType} with {args.Count} args");
                _plugin.JobManager.OnSpecialEvent(specialEvent, argsArray);

                foreach(ISpecialEventListener listener in _plugin.Subscribers.SpecialEvent)
                {
                    listener.OnSpecialEvent(specialEvent, argsArray);
                }
            }

            [SthNamedEvent(SurviveTheHuntShared.Events.Client.CupidReceiveCopCarSpawnPermission)]
            public void ReceiveCopCarSpawnGrant(string stationName, object slot)
            {
                _plugin.CopSpawnController.OnCopCarSpawnGranted(Constants.Location.CopSpawn.FromName(stationName), Convert.ToByte(slot));
            }
        }

        public override void OnResourceStopping()
        {
            base.OnResourceStopping();

            foreach(ISceneHandler handler in SceneHandlers.Values)
            {
                handler.Cleanup();
            }

            JobManager?.Cleanup(true);

            CopSpawnController?.Cleanup();

            CellTowerPingController?.Cleanup();
            CellTowerPingController = null;

            ShipHeliFerryHelper?.Cleanup();
            ShipHeliFerryHelper = null;
        }

        public override void OnHuntStarted(IGameState gameState, IPlayerState playerState)
        {
            base.OnHuntStarted(gameState, playerState);

            ScriptCamera = CreateCam("DEFAULT_SCRIPTED_CAMERA", true);

            GameState = gameState;
            PlayerState = playerState;
            _state = new PluginState();

            State.LocalRole = PlayerUtils.GetPlayerType(PlayerId(), GameState.Hunt.HuntedPlayers);

            Debug.WriteLine($"{nameof(CupidPlugin)}.{nameof(OnHuntStarted)}: Local player's role is {State.LocalRole}");

            State.RequestClothesChange();
            SetScene(DirectedScene.IntroJason);

            BleedoutController.Enabled = true;
            BleedoutController.Reset(State.LocalRole == PlayerType.Cop ? Teams.Team.Hunters : Teams.Team.Hunted);

            if(JobManager != null)
            {
                JobManager.Cleanup();
            }

            JobManager = new JobManager(SetPlayerClothing, TriggerEventProxy, TriggerServerEventProxy, playerState, gameState);
            JobManager.JobCompleted += OnJobCompleted;

            if(HeatController != null)
            {
                HeatController.Cleanup();
            }

            HeatController = new HeatController();
            HeatController.HeatTierChanged += OnHeatTierChanged;
            HeatController.HeatChanged += OnHeatChanged;

            UIMenuHelper.SetItemBlocked(SurviveTheHuntShared.Models.UI.BlockedItem.Appearance);

            CopSpawnController = CreateCopSpawnController(CopSpawnController);

            // Create a ping controller for HuntedL - though for testing purposes allow HuntedJ to be considered too
            CellTowerPingController = new CellTowerPingController(GameState.Hunt.HuntedPlayers[Math.Min(GameState.Hunt.HuntedPlayers.Length - 1, 1)].PlayerHandle, TriggerServerEventProxy);
            Subscribers.SpecialEvent.Add(CellTowerPingController);

            ShipHeliFerryHelper = new HeliFerryHelper(TriggerServerEventProxy, "ship", (uint)GetHashKey("U_M_Y_SmugMech_01"), (uint)VehicleHash.Supervolito, new Vector4(-1716.768f, -1010.044f, 5.556772f, 50.3356f), new Vector4(-2043.615f, -1031.756f, 11.98072f, 72.11658f));
            Subscribers.SpecialEvent.Add(ShipHeliFerryHelper);
            Subscribers.HuntLifecycle.Add(ShipHeliFerryHelper);

            string[] playerNames =
            {
                GetPlayerName(GameState.Hunt.HuntedPlayers[0].PlayerHandle),
                GetPlayerName(GameState.Hunt.HuntedPlayers[Math.Min(GameState.Hunt.HuntedPlayers.Length - 1, 1)].PlayerHandle)
            };

            new AVControllerHelper(TriggerEventProxy).SetPlayerNames(playerNames);

            foreach(IHuntLifecycleListener huntLifecycleListener in Subscribers.HuntLifecycle)
            {
                huntLifecycleListener.OnHuntStarted(gameState, playerState);
            }
        }

        private void OnJobCompleted(ushort heatValue)
        {
            // Heat can sometimes be awarded on both hunted clients - depending on the job - so we can't send just the delta; we have to send the full value
            TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.CupidNotifyNewHeatScore, _state.HuntedHeatScore + heatValue);
        }

        private void SetScene(DirectedScene scene)
        {
            State.CurrentScene = scene;
            if (SceneHandlers.TryGetValue(scene, out ISceneHandler sceneHandler))
            {
                sceneHandler.StartScene(in GameState, ScriptCamera);
            }
        }

        private bool AdvanceScene()
        {
            if(State.CurrentScene == _lastScene)
            {
                SetCamActive(ScriptCamera, false);
                RenderScriptCams(false, false, 0, false, false);

                SetFocusEntity(PlayerPedId());

                return false;
            } else
            {
                SetScene(State.CurrentScene + 1);
                return true;
            }
        }

        public override void OnHuntEnded(Teams.Team localPlayerTeam, IGameState gameState, IPlayerState playerState)
        {
            base.OnHuntEnded(localPlayerTeam, gameState, playerState);

            DestroyCam(ScriptCamera, true);
            ScriptCamera = default;

            foreach(ISceneHandler handler in SceneHandlers.Values)
            {
                handler.Cleanup();
            }

            foreach(IHuntLifecycleListener huntLifecycleListener in Subscribers.HuntLifecycle)
            {
                huntLifecycleListener.OnHuntEnded(localPlayerTeam, gameState, playerState);
            }

            if(JobManager != null)
            {
                JobManager.Cleanup();
                JobManager = null;
            }

            if(CopSpawnController != null)
            {
                CopSpawnController.Enabled = false;
                CopSpawnController.Cleanup();
                CopSpawnController = null;
            }

            if(State.TulipBlip != 0 && DoesBlipExist(State.TulipBlip))
            {
                RemoveBlip(ref State.TulipBlip);
            }
            State.TulipBlip = 0;

            if(State.TulipNetId != 0 && NetworkDoesEntityExistWithNetworkId(State.TulipNetId))
            {
                int veh = NetToVeh(State.TulipNetId);
                DeleteVehicle(ref veh);
            }
            State.TulipNetId = 0;

            UIMenuHelper.SetItemBlocked(SurviveTheHuntShared.Models.UI.BlockedItem.Appearance, false);

            if (CellTowerPingController != null)
            {
                CellTowerPingController.Cleanup();
                Subscribers.SpecialEvent.Remove(CellTowerPingController);
                CellTowerPingController = null;
            }

            if(ShipHeliFerryHelper != null)
            {
                ShipHeliFerryHelper.Cleanup();
                Subscribers.HuntLifecycle.Remove(ShipHeliFerryHelper);
                Subscribers.SpecialEvent.Remove(ShipHeliFerryHelper);
                ShipHeliFerryHelper = null;
            }

            // Start the outro scene
                    PostGameScene = new SceneHandlers.Outro.OutroHuntedWinSceneHandler(gameState.Hunt.WinningTeam == localPlayerTeam, gameState.Hunt.WinningTeam, gameState.Hunt.HuntedPlayers, TriggerEventProxy, TriggerServerEventProxy);
                    PostGameScene.StartScene(gameState, -1);

            // CopSpawnController may have us frozen
            FreezeEntityPosition(PlayerPedId(), false);
        }

        public override void OnPlayerSpawned()
        {
            base.OnPlayerSpawned();

            if (GameState.Hunt.IsInProgress)
            {
                State.RequestClothesChange();
            }

            bool isCupid = GameState?.Mode == "cupid";
            BleedoutController.Enabled = isCupid;
            BleedoutController.OnRespawn();

            if (isCupid && GameState.Hunt?.IsStarted == true && _state.LocalRole == PlayerType.Cop)
            {
                CopSpawnController.Enabled = true;
            }
        }

        internal bool TryGetPlayer(PlayerType playerType, out HuntPlayer? huntPlayer, int index = 0)
        {
            switch(playerType)
            {
                case PlayerType.HuntedJ:
                case PlayerType.HuntedL:
                    if(GameState.Hunt.HuntedPlayers.Length > (int)playerType)
                    {
                        huntPlayer = GameState.Hunt.HuntedPlayers[(int)playerType];
                        return true;
                    }
                    else
                    {
                        huntPlayer = null;
                        return false;
                    }
                default:
                    int nbPlayers = GetNumberOfPlayers();
                    int counter = 0;
                    foreach(Player player in PlayerList.Players)
                    {
                        if(player.Handle != GameState.Hunt.HuntedPlayers[(int)PlayerType.HuntedL].PlayerHandle && player.Handle != GameState.Hunt.HuntedPlayers[(int)PlayerType.HuntedJ].PlayerHandle)
                        {
                            if(counter == index)
                            {
                                // TODO: shouldn't we be doing server time offset here too?
                                huntPlayer = new HuntPlayer(player, System.DateTime.UtcNow);
                                return true;
                            }
                            counter++;
                        }
                    }
                    huntPlayer = null;
                    return false;
            }
        }

        internal void SetPlayerClothing(Constants.Clothing.OutfitPair currentSceneOutfit, PlayerType? playerType = null, DirectedScene? scene = null)
        {
            if(!playerType.HasValue)
            {
                playerType = State.LocalRole;
            }

            if(!scene.HasValue)
            {
                scene = State.CurrentScene;
            }

            int pedId = PlayerPedId();

            SetPlayerClothing(pedId, currentSceneOutfit, playerType, scene);

            State.RequestClothesChange(false);

            State.LastWornOutfit = currentSceneOutfit;
        }

        internal void SetPlayerClothing(PlayerType playerType, DirectedScene scene, bool strict = false)
        {
            int pedId = PlayerPedId();

            Debug.WriteLine($"Changing clothing to {playerType}");

            State.RequestClothesChange(false);

            if (!Constants.Clothing.Outfits.TryGetValue(playerType, out Constants.Clothing.SceneOutfits sceneOutfits))
            {
                Debug.WriteLine($"Player type {playerType} doesn't have any scene outfits");
                if (strict || !State.LastWornOutfit.HasValue)
                {
                    return;
                }
                Debug.WriteLine("Continuing with last worn outfit");
            }

            if(!sceneOutfits.TryGetValue(scene, out Constants.Clothing.OutfitPair currentSceneOutfit))
            {
                Debug.WriteLine($"Player type {playerType} does not have an outfit for scene {scene}");
                if (strict || !State.LastWornOutfit.HasValue)
                {
                    return;
                }
                Debug.WriteLine("Continuing with last worn outfit");
                currentSceneOutfit = State.LastWornOutfit.Value;
            }

            SetPlayerClothing(currentSceneOutfit, playerType, scene);
        }

        /// <summary>
        /// Instantly applies a matching outfit for <paramref name="playerType"/> in <paramref name="scene"/> to ped <paramref name="pedHandle"/>.
        /// </summary>
        /// <param name="pedHandle"></param>
        /// <param name="playerType"></param>
        /// <param name="scene"></param>
        internal static void SetPlayerClothing(int pedHandle, PlayerType playerType, DirectedScene scene)
        {
            if (!Constants.Clothing.Outfits.TryGetValue(playerType, out Constants.Clothing.SceneOutfits sceneOutfits))
            {
                Debug.WriteLine($"Player type {playerType} doesn't have any scene outfits");
                return;
            }

            if (!sceneOutfits.TryGetValue(scene, out Constants.Clothing.OutfitPair currentSceneOutfit))
            {
                Debug.WriteLine($"Player type {playerType} does not have an outfit for scene {scene}");
                return;
            }

            SetPlayerClothing(pedHandle, currentSceneOutfit, playerType, scene);
        }

        /// <summary>
        /// Instantly applies <paramref name="currentSceneOutfit"/> to ped <paramref name="pedHandle"/>.
        /// </summary>
        /// <param name="pedHandle"></param>
        /// <param name="currentSceneOutfit"></param>
        /// <param name="playerType"></param>
        /// <param name="scene"></param>
        internal static void SetPlayerClothing(int pedHandle, Constants.Clothing.OutfitPair currentSceneOutfit, PlayerType? playerType = null, DirectedScene? scene = null)
        {
            bool isFemale = !IsPedMale(pedHandle) || (PedHash)GetEntityModel(pedHandle) == PedHash.FreemodeFemale01;

            PedOutfit outfit = currentSceneOutfit.GetOutfit(isFemale);

            if (outfit == null)
            {
                Debug.WriteLine($"{(isFemale ? "Female" : "Male")} {playerType} player model does not have an outfit for scene {scene}");
            }

            ClearAllPedProps(pedHandle);
            List<int> ignoredComps = new List<int> { (int)PedComponents.Hair, (int)PedComponents.Face };
            for (int i = 0; i <= 11; i++)
            {
                if (!ignoredComps.Contains(i))
                {
                    SetPedComponentVariation(pedHandle, i, 0, 0, 0);
                }
            }

            foreach (KeyValuePair<PedComponents, PedVariation> comp in outfit.ComponentsToApply)
            {
                if (!ignoredComps.Contains((int)comp.Key))
                {
                    SetPedComponentVariation(pedHandle, (int)comp.Key, comp.Value.Drawable, comp.Value.Texture, 0);
                }
            }

            foreach (KeyValuePair<PedProps, PedVariation> comp in outfit.PropsToApply)
            {
                SetPedPropIndex(pedHandle, (int)comp.Key, comp.Value.Drawable, comp.Value.Texture, true);
            }
        }

        public override void OnClockReceived(int hours, int minutes, int seconds)
        {
            base.OnClockReceived(hours, minutes, seconds);

            // Set time to 10AM at the start
            SetClockTime(10, 0, 0);
            NetworkOverrideClockTime(10, 0, 0);
        }

        public override void OnNetEntityReceived(int netId, string name)
        {
            base.OnNetEntityReceived(netId, name);

            foreach(ISceneHandler handler in SceneHandlers.Values)
            {
                handler.OnNetEntityReceived(netId, name);
            }

            JobManager.OnNetEntityReceived(netId, name);

            foreach (INetEntityListener subscriber in Subscribers.NetEntity)
            {
                subscriber.OnNetEntityReceived(name, netId);
            }

            if (name == IntroJ2Handler.TulipNetEntName)
            {
                State.TulipNetId = netId;
            }
        }

        private bool _canShowHud = true;
        public sealed override bool CanShowHud => base.CanShowHud && _canShowHud;

        public sealed override float? YLimitOverride => 7180f;

        public sealed override bool PreventDeathDetection => BleedoutController.Enabled;
        public override LabelledItem[] UICurrentItems
        {
            get
            {
                // TODO: This could probably be made into a fixed size array; there's only so much we can show on the screen at once anyway
                List<LabelledItem> items = new List<LabelledItem>();

                if (HeatController != null)
                {
                    items.AddRange(HeatController.UIItems);
                }

                if (BleedoutController.Enabled)
                {
                    items.AddRange(BleedoutController.UIState.CurrentItems);
                }

                if (JobManager != null)
                {
                    items.AddRange(JobManager.CurrentJobUI);
                }

                return items.ToArray();
            }
        }

        public override Teams.Team? WinningTeamOverride
        {
            get
            {
                if(IsActive && GameState != null)
                {
                    return HeatController.CurrentHeat < (ushort)HeatThresholds.Target ? Teams.Team.Hunters : Teams.Team.Hunted;
                }

                Debug.WriteLine($"{nameof(CupidPlugin)}.{nameof(WinningTeamOverride)}: {nameof(IsActive)} is {IsActive} and {nameof(GameState)} is {GameState}; falling back to default winner pick logic");

                return null;
            }
        }

        private void OnIntroEnded()
        {
            Debug.WriteLine($"{nameof(CupidPlugin)}: intro ended, setting scene from {State.CurrentScene} to {DirectedScene.Default1}");
            State.CurrentScene = DirectedScene.Default1;

            CellTowerPingController.Start();
        }

        public void Tick(float deltaTime)
        {
            if(GameState?.Mode != "cupid")
            {
                return;
            }

            if(State.IsWaitingForClothesChange)
            {
                State.CurrentClothesChangeElapsedSeconds += deltaTime;

                if(State.ShouldChangeClothes)
                {
                    SetPlayerClothing(State.LocalRole, State.CurrentScene);
                }
            }

            bool isIntroOver = false;
            bool sceneAllowsHud = true;

            if(GameState != null)
            {
                if (SceneHandlers.TryGetValue(State.CurrentScene, out ISceneHandler scene))
                {
                    if (!scene.IsOver)
                    {
                        // Run the tick for the current scene.
                        scene.Tick(deltaTime);
                        sceneAllowsHud = scene.CanShowHud;
                    }
                    else
                    {
                        // When the scene is over, move to the next one.
                        isIntroOver = !AdvanceScene();
                        if(SceneHandlers.TryGetValue(State.CurrentScene, out ISceneHandler newScene))
                        {
                            sceneAllowsHud = newScene.CanShowHud;
                        }
                    }
                }
                else
                {
                    isIntroOver = true;
                }
            }

            // Hide the HUD for the majority of the intro until control is given back to the player
            _canShowHud = GameState?.Hunt?.IsStarted != true || isIntroOver || sceneAllowsHud;

            if (GameState?.Hunt?.IsStarted == true && State.CurrentScene != _lastScene && !isIntroOver)
            {
                DisableAllControlActions(0);
            }

            if(isIntroOver && JobManager != null && !State.HasRunPostIntro)
            {
                State.HasRunPostIntro = true;
                JobManager.Start();
                OnIntroEnded();
            }

            BleedoutController.Tick(deltaTime);
            JobManager?.Tick(deltaTime);
            HeatController?.Tick(deltaTime);
            CopSpawnController?.Tick(deltaTime);
            RepairShopManager.Tick(deltaTime);
            CellTowerPingController?.Tick(deltaTime);
            ShipHeliFerryHelper?.Tick(deltaTime);

            if(NetworkDoesNetworkIdExist(State.TulipNetId) && NetworkDoesEntityExistWithNetworkId(State.TulipNetId))
            {
                int tulip = NetToVeh(State.TulipNetId);
                CarModHelper.TickSpecialVehicleProperties(tulip, Constants.TulipHashKey);

                if(State.TulipBlip == 0 && State.LocalRole != PlayerType.Cop)
                {
                    State.TulipBlip = AddBlipForEntity(tulip);
                    SetBlipSprite(State.TulipBlip, (int)BlipSprite.PersonalVehicleCar);
                    SetBlipNameFromTextFile(State.TulipBlip, PersonalVehicleBlipNameKey);
                }

                if(State.TulipBlip != 0)
                {
                    SetBlipDisplay(State.TulipBlip, IsPedInVehicle(PlayerPedId(), tulip, true) ? 0 : 6);
                }


                if (State.LocalRole != PlayerType.Cop)
                {
                    // prevent shuffling into driver seat
                    SetPedConfigFlag(PlayerPedId(), 184, !isIntroOver);
                }
            }

            if(PostGameScene != null)
            {
                PostGameScene.Tick(deltaTime);
                if(PostGameScene.IsOver)
                {
                    PostGameScene.Cleanup();
                    PostGameScene = null;
                }
            }
        }

        // Allow a driver and a gunner
        public sealed override bool IsDrivebyAllowedForPassengers => true;

        public sealed override bool IsDrivebyAllowedForDrivers => IsPedOnAnyBike(PlayerPedId());

        public sealed override void InjectHuntSettings(ref HuntSettings settings)
        {
            base.InjectHuntSettings(ref settings);

            Debug.WriteLine($"{nameof(CupidPlugin)}.{nameof(InjectHuntSettings)}: allowing both teams to leave safe zone during prep");
            settings.TeamsAllowedOutOfSafeZoneDuringPrep = HuntSettings.GetTeamsBitset(Teams.Team.Hunted, Teams.Team.Hunters);
            Debug.WriteLine($"{nameof(CupidPlugin)}.{nameof(InjectHuntSettings)}: reducing safe zone radius");
            settings.SafeZoneRadius = float.Epsilon;
        }

        internal void OnHeatChanged(ushort old, ushort current)
        {
            foreach (IHeatListener heatListener in Subscribers.Heat)
            {
                heatListener?.OnHeatChanged(current, HeatController.GetTier(current));
            }
        }

        public override bool CanPedBlipBeDeleted(int pedHandle)
        {
            if(BleedoutController.IsRevivable(pedHandle))
            {
                return false;
            }

            return base.CanPedBlipBeDeleted(pedHandle);
        }

        public sealed override PlayerPingConfig CanPingShow(int playerHandle)
        {
            // only L gets pinged
            if(GameState?.Hunt != null)
            {
                HuntPlayer[] huntedPlayers = GameState.Hunt.HuntedPlayers;
                bool canShowArea = PlayerUtils.GetPlayerType(playerHandle, in huntedPlayers) == PlayerType.HuntedL;

                // leave radius blip to custom radius blip for this plugin
                return new PlayerPingConfig { CanNotifyWithArea = canShowArea, CanShowRadiusBlip = false };
            }

            return base.CanPingShow(playerHandle);
        }

        public sealed override TimeSpan HuntDurationOverride
        {
            get => TimeSpan.FromMinutes(36);
        }

        public sealed override ushort HuntedCount => 2;
    }
}
