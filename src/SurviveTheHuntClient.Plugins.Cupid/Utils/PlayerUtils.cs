using SurviveTheHuntClient.Models;
using static SurviveTheHuntShared.Plugins.Cupid.Constants;

namespace SurviveTheHuntClient.Plugins.Cupid.Utils
{
    internal static class PlayerUtils
    {
        internal static PlayerType GetPlayerType(int localPlayerId, in HuntPlayer[] huntedPlayers)
        {
            PlayerType playerType = PlayerType.Cop;

            for (int i = 0; i < huntedPlayers.Length; i++)
            {
                if (huntedPlayers[i].PlayerHandle == localPlayerId)
                {
                    playerType = (PlayerType)i;
                    break;
                }
            }

            return playerType;
        }
    }
}
