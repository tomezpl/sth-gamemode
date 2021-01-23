using static CitizenFX.Core.Native.API;
using static SurviveTheHuntClient.Teams;

namespace SurviveTheHuntClient
{
    public static class HuntUI
    {
        public static void DisplayObjective(ref GameState gameState, ref PlayerState playerState)
        {
            if(!string.IsNullOrWhiteSpace(gameState.CurrentObjective))
            {
                AddTextEntry("CURRENT_OBJECTIVE", "~a~~a~");
                BeginTextCommandPrint("CURRENT_OBJECTIVE");

                if(gameState.Hunt.IsInProgress)
                {
                    if(playerState.Team == Team.Hunters)
                    {
                        // Make the next text component colour yellow, as it'll contain the hunted player's name.
                        SetColourOfNextTextComponent(12);
                        AddTextComponentString(gameState.Hunt.HuntedPlayer.Name);
                    }
                }

                // Switch back to default (white) text colour.
                SetColourOfNextTextComponent(0);
                AddTextComponentString(gameState.CurrentObjective);
                EndTextCommandPrint(1, true);
            }
        }
    }
}