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
                if (killInfo.VictimServerId == gameState.Hunt?.HuntedPlayer?.Handle)
                {
                    payload.Label = SharedConstants.KillFeedMessages.HuntedKillLabel;
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
