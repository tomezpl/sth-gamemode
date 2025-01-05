using CitizenFX.Core;
using System;
using Events = SurviveTheHuntShared.Events;
using static CitizenFX.Core.Native.API;
using SurviveTheHuntShared;

namespace SurviveTheHuntClient
{
    public partial class MainScript
    {
        [EventHandler(Events.Client.ReceiveClockSyncRequest)]
        public void SendIngameClock()
        {
            int hours = GetClockHours();
            int minutes = GetClockMinutes();
            int seconds = GetClockSeconds();
            Debug.WriteLine($"Server requested in-game clock resync, sending back {hours.ToString().PadLeft(2)}:{minutes.ToString().PadLeft(2)}:{seconds.ToString().PadLeft(2)}");
            TriggerLatentServerEvent(Events.Server.ReceiveHuntedClock, sizeof(int) * 3, hours, minutes, seconds);
        }

        [EventHandler(Events.Client.DisplayKill)]
        public void DisplayNewKill(string payloadSerialized)
        {
            Debug.WriteLine(payloadSerialized);
            KillFeedServerPayload payload = KillFeedServerPayload.Deserialize(payloadSerialized);
            string label = payload.Label.WitnessGxt;

            string localPlayerId = Player.Local.ServerId.ToString();
            bool isAttacker = false;
            if (payload.KillInfo.VictimServerId == localPlayerId)
            {
                label = payload.Label.VictimGxt;
            }
            else if(payload.KillInfo.AttackerServerId == localPlayerId)
            {
                label = payload.Label.AttackerGxt;
                isAttacker = true;
            }


            string labelText = GetLabelText(label);
            Debug.WriteLine($"Chosen label key: {label}, text: {labelText}");
            Debug.WriteLine($"AttackerServerId = {payload.KillInfo.AttackerServerId}, VictimServerId = {payload.KillInfo.VictimServerId}");
            BeginTextCommandThefeedPost(label);

            int numTokens = KillFeedLabel.CountPlayerNames(labelText);
            Debug.WriteLine($"label has {numTokens} tokens");
            if(numTokens != 0)
            {
                AddTextComponentSubstringPlayerName(GetPlayerName(GetPlayerFromServerId(int.Parse(isAttacker ? payload.KillInfo.VictimServerId : payload.KillInfo.AttackerServerId))));
                Debug.WriteLine($"Added {(isAttacker ? "victim" : "attacker")} server ID");
            }
            if(numTokens == 2)
            {
                AddTextComponentSubstringPlayerName(GetPlayerName(GetPlayerFromServerId(int.Parse(isAttacker ? payload.KillInfo.AttackerServerId : payload.KillInfo.VictimServerId))));
                Debug.WriteLine($"Added {(!isAttacker ? "victim" : "attacker")} server ID");
            }

            EndTextCommandThefeedPostMpticker(true, true);
        }
    }
}
