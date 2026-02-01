using CitizenFX.Core;
using SurviveTheHuntClient.Attributes;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntShared.Plugins.Cupid.Constants;

namespace SurviveTheHuntClient.Plugins.Cupid
{
    public class CupidPlugin : Plugin<CupidPlugin.Events>, ITickable
    {
        public override string GameModeTitle => "Valentines";
        public override string GameModeDescription => "TODO";

        public override bool IsGameMode => true;

        internal IGameState GameState;

        public class PluginState
        {
            public PlayerType LocalRole = PlayerType.Cop;
        }

        private PluginState _state = new PluginState();

        public PluginState State { get => _state; }

        public CupidPlugin(PluginContext context) : base("cupid", context)
        {
        }

        public class Events : PluginEvents
        {
        }

        public override void OnHuntStarted(IGameState gameState, IPlayerState playerState)
        {
            base.OnHuntStarted(gameState, playerState);

            GameState = gameState;
            _state = new PluginState();

            PlayerType playerType = PlayerType.Cop;
            int localPlayerId = PlayerId();
            for(int i = 0; i < GameState.Hunt.HuntedPlayers.Length; i++)
            {
                if (GameState.Hunt.HuntedPlayers[i].PlayerHandle == localPlayerId)
                {
                    playerType = (PlayerType)i;
                    break;
                }
            }

            State.LocalRole = playerType;

            Debug.WriteLine($"{nameof(CupidPlugin)}.{nameof(OnHuntStarted)}: Local player's role is {State.LocalRole}");
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

        internal void SetPlayerClothing(PlayerType playerType)
        {
            int pedId = PlayerPedId();

            ClearAllPedProps(pedId);
            List<int> ignoredComps = new List<int> { (int)PedComponents.Hair, (int)PedComponents.Face };
            for (int i = 0; i <= 11; i++)
            {
                if (!ignoredComps.Contains(i))
                {
                    SetPedComponentVariation(pedId, i, 0, 0, 0);
                }
            }

            bool isFemale = !IsPedMale(pedId) || (PedHash)GetEntityModel(pedId) == PedHash.FreemodeFemale01;

            PedOutfit outfit = Constants.Clothing.Outfits[playerType][DirectedScene.IntroJason].GetOutfit(isFemale);

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
        }

        public void Tick(float deltaTime)
        {

        }
    }
}
