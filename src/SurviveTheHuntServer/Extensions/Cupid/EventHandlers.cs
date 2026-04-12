using CitizenFX.Core;
using System.Collections.Generic;

namespace SurviveTheHuntServer
{
    public partial class MainScript
    {
        [EventHandler(SurviveTheHuntShared.Events.Server.CupidNotifyRevivable)]
        public void CupidNotifyRevivable([FromSource] Player player, int pedNetId)
        {
            Debug.WriteLine($"{nameof(CupidNotifyRevivable)}: {player.Name} is downed and needs reviving");

            bool isHunted = false;
            List<string> huntedHandles = new List<string>(GameState.Hunt.HuntedPlayers.Length);
            foreach(Player huntedPlayer in GameState.Hunt.HuntedPlayers)
            {
                huntedHandles.Add(huntedPlayer.Handle);
                if(huntedPlayer.Handle == player.Handle)
                {
                    isHunted = true;
                }
            }


            foreach(Player playerToNotify in Players)
            {
                // Don't notify hunters about hunted players being in a DBNO state, and vice-versa.
                if(huntedHandles.Contains(playerToNotify.Handle) == isHunted)
                {
                    TriggerClientEvent(playerToNotify, SurviveTheHuntShared.Events.Client.CupidReceiveRevivable, pedNetId);
                }
            }
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.CupidNotifyReviveEnd)]
        public void CupidNotifyRevived([FromSource] Player reviver, int pedNetId, bool cancelled)
        {
            Debug.WriteLine($"{nameof(CupidNotifyRevived)}: {reviver.Name} finished reviving {pedNetId} ({nameof(cancelled)} = {cancelled})");

            TriggerClientEvent(SurviveTheHuntShared.Events.Client.CupidReceiveEndRevive, pedNetId, cancelled);
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.CupidNotifyReviveStart)]
        public void CupidNotifyReviveStart([FromSource] Player player, int pedNetId)
        {
            Debug.WriteLine($"{nameof(CupidNotifyReviveStart)}: {player.Name} started reviving {pedNetId}");

            TriggerClientEvent(SurviveTheHuntShared.Events.Client.CupidReceiveStartRevive, pedNetId);
        }
    }
}
