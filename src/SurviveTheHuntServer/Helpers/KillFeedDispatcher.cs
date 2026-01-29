using CitizenFX.Core;
using SurviveTheHuntShared;
using System;
using SharedConstants = SurviveTheHuntShared.Constants;

namespace SurviveTheHuntServer.Helpers
{
    internal static class KillFeedDispatcher
    {
        internal static KillFeedServerPayload GetKillFeedPayload(KillFeedClientPayload killInfo, GameState gameState, Random rng)
        {
            KillFeedServerPayload payload = new KillFeedServerPayload
            {
                KillInfo = killInfo,
            };

            payload.Label = SharedConstants.KillFeedMessages.FallbackLabel;

            if(killInfo.AttackerServerId != null)
            {
                bool victimIsHunted = false;
                foreach(Player huntedPlayer in gameState.Hunt?.HuntedPlayers ?? new Player[0])
                {
                    if(killInfo.VictimServerId == huntedPlayer.Handle)
                    {
                        victimIsHunted = true;
                        break;
                    }
                }

                if (victimIsHunted)
                {
                    payload.Label = SharedConstants.KillFeedMessages.HuntedKillLabel;
                }
                else if(killInfo.VictimServerId == killInfo.AttackerServerId)
                {
                    payload.Label = SharedConstants.KillFeedMessages.SelfKillLabel;
                }
                else
                {
                    int randomMessage = rng.Next(SharedConstants.KillFeedMessages.RegularLabels.Length);
                    payload.Label = SharedConstants.KillFeedMessages.RegularLabels[randomMessage];
                }
            }

            return payload;
        }
    }
}
