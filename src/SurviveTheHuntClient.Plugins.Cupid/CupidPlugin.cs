using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Utils;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntClient.Plugins.Cupid.Constants;
using PlayerType = SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType;

namespace SurviveTheHuntClient.Plugins.Cupid
{
    public class CupidPlugin : Plugin<CupidPlugin.Events>, ITickable
    {
        public override string GameModeTitle => "Valentines";
        public override string GameModeDescription => "TODO";

        public override bool IsGameMode => true;

        internal IGameState GameState;

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
        }

        private int ScriptCamera;

        private PluginState _state = new PluginState();

        internal PluginState State { get => _state; }

        private Dictionary<DirectedScene, ISceneHandler> SceneHandlers;

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

        public CupidPlugin(PluginContext context) : base("cupid", context)
        {
            SceneHandlers = new Dictionary<DirectedScene, ISceneHandler>()
            {
                { DirectedScene.IntroJason, new SceneHandlers.Intro.IntroJ1Handler(context.TriggerEventProxy, context.TriggerServerEventProxy) },
                { DirectedScene.JasonDrivingHood, new SceneHandlers.Intro.IntroJ2Handler(context.TriggerEventProxy, context.TriggerServerEventProxy) }
            };

            DirectedScene[] allScenes = GetAllHandledScenes(SceneHandlers);
            _lastScene = allScenes[allScenes.Length - 1];
        }

        public class Events : PluginEvents
        {
        }

        public override void OnResourceStopping()
        {
            base.OnResourceStopping();

            foreach(ISceneHandler handler in SceneHandlers.Values)
            {
                handler.Cleanup();
            }
        }

        public override void OnHuntStarted(IGameState gameState, IPlayerState playerState)
        {
            base.OnHuntStarted(gameState, playerState);

            ScriptCamera = CreateCam("DEFAULT_SCRIPTED_CAMERA", true);

            GameState = gameState;
            _state = new PluginState();

            State.LocalRole = PlayerUtils.GetPlayerType(PlayerId(), GameState.Hunt.HuntedPlayers);

            Debug.WriteLine($"{nameof(CupidPlugin)}.{nameof(OnHuntStarted)}: Local player's role is {State.LocalRole}");

            State.RequestClothesChange();
            SetScene(DirectedScene.IntroJason);
        }

        private void SetScene(DirectedScene scene)
        {
            State.CurrentScene = scene;
            SceneHandlers[scene].StartScene(in GameState, ScriptCamera);
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

        public override void OnHuntEnded(IGameState gameState, IPlayerState playerState)
        {
            base.OnHuntEnded(gameState, playerState);

            DestroyCam(ScriptCamera, true);
            ScriptCamera = default;

            foreach(ISceneHandler handler in SceneHandlers.Values)
            {
                handler.Cleanup();
            }
        }

        public override void OnPlayerSpawned()
        {
            base.OnPlayerSpawned();

            if (GameState.Hunt.IsInProgress)
            {
                State.RequestClothesChange();
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

        internal void SetPlayerClothing(PlayerType playerType, DirectedScene scene)
        {
            int pedId = PlayerPedId();

            Debug.WriteLine($"Changing clothing to {playerType}");

            bool failed = false;

            State.RequestClothesChange(false);

            if (!Constants.Clothing.Outfits.TryGetValue(playerType, out Constants.Clothing.SceneOutfits sceneOutfits))
            {
                Debug.WriteLine($"Player type {playerType} doesn't have any scene outfits");
                return;
            }

            if(!sceneOutfits.TryGetValue(scene, out Constants.Clothing.OutfitPair currentSceneOutfit))
            {
                Debug.WriteLine($"Player type {playerType} does not have an outfit for scene {scene}");
                return;
            }

            bool isFemale = !IsPedMale(pedId) || (PedHash)GetEntityModel(pedId) == PedHash.FreemodeFemale01;

            PedOutfit outfit = currentSceneOutfit.GetOutfit(isFemale);

            if (outfit == null)
            {
                Debug.WriteLine($"{(isFemale ? "Female" : "Male")} {playerType} player model does not have an outfit for scene {scene}");
            }

            ClearAllPedProps(pedId);
            List<int> ignoredComps = new List<int> { (int)PedComponents.Hair, (int)PedComponents.Face };
            for (int i = 0; i <= 11; i++)
            {
                if (!ignoredComps.Contains(i))
                {
                    SetPedComponentVariation(pedId, i, 0, 0, 0);
                }
            }

            foreach (KeyValuePair<PedComponents, PedVariation> comp in outfit.ComponentsToApply)
            {
                if (!ignoredComps.Contains((int)comp.Key))
                {
                    SetPedComponentVariation(pedId, (int)comp.Key, comp.Value.Drawable, comp.Value.Texture, 0);
                }
            }

            foreach (KeyValuePair<PedProps, PedVariation> comp in outfit.PropsToApply)
            {
                SetPedPropIndex(pedId, (int)comp.Key, comp.Value.Drawable, comp.Value.Texture, true);
            }

            State.RequestClothesChange(false);
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
        }

        private bool _canShowHud = true;
        public sealed override bool CanShowHud => base.CanShowHud && _canShowHud;

        public sealed override float? YLimitOverride => 7180f;

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

            if(GameState != null)
            {
                if (!SceneHandlers[State.CurrentScene].IsOver)
                {
                    SceneHandlers[State.CurrentScene].Tick(deltaTime);
                }
                else
                {
                    isIntroOver = !AdvanceScene();
                }
            }

            // Hide the HUD for the majority of the intro until control is given back to the player
            _canShowHud = GameState?.Hunt?.IsStarted != true || isIntroOver || SceneHandlers[State.CurrentScene].CanShowHud;

            if (GameState?.Hunt?.IsStarted == true && State.CurrentScene != _lastScene && !isIntroOver)
            {
                DisableAllControlActions(0);
            }
        }
    }
}
