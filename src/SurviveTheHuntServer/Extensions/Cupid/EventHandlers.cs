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

        [EventHandler(SurviveTheHuntShared.Events.Server.CupidJobSyncState)]
        public void CupidJobSyncState([FromSource] Player sender, string jobId, int propId, object propValue)
        {
            Debug.WriteLine($"{nameof(CupidJobSyncState)}: {sender.Name} ({sender.Handle}) is syncing prop {propId} with value of {propValue}");

            foreach(Player player in Players)
            {
                if(player.Handle != sender.Handle)
                {
                    TriggerClientEvent(player, SurviveTheHuntShared.Events.Client.CupidReceiveSyncState, jobId, propId, propValue);
                }
            }
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.CupidNotifyNewHeatScore)]
        public void CupidNotifyNewHeatScore([FromSource] Player player, ushort heatScore)
        {
            Debug.WriteLine($"{nameof(CupidNotifyNewHeatScore)}({nameof(player)}: {player.Name}, {nameof(heatScore)}: {heatScore})");

            // I feel like this is gonna be a bit problematic as every hunted player (there are more than 1 in this mode) can send this event.
            // Then this broadcasts the new score to all clients. Meaning we'll send the data to all clients twice. oh well
            TriggerLatentClientEvent(SurviveTheHuntShared.Events.Client.CupidReceiveHeatScore, sizeof(ushort), heatScore);
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent)]
        public void CupidBroadcastSpecialEvent(int specialEventType, params object[] args)
        {
            //Debug.WriteLine($"{nameof(CupidBroadcastSpecialEvent)}: received {SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent} for special event {specialEventType} with {args.Length} arguments:");
            int counter = 0;
            foreach(object arg in args)
            {
                //Debug.WriteLine($"{nameof(arg)}[{++counter}]: {arg}");
            }
            TriggerLatentClientEvent(SurviveTheHuntShared.Events.Client.CupidReceiveSpecialEvent, 128, specialEventType, args);
        }
    }
}
