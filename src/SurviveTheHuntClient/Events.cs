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
            bool error = false;
            if(numTokens != 0)
            {
                if (int.TryParse(isAttacker || string.IsNullOrEmpty(payload.KillInfo.AttackerServerId) ? payload.KillInfo.VictimServerId : payload.KillInfo.AttackerServerId, out int firstPlayerId))
                {
                    AddTextComponentSubstringPlayerName(GetPlayerName(GetPlayerFromServerId(firstPlayerId)));
                    Debug.WriteLine($"Added {(isAttacker ? "victim" : "attacker")} server ID");
                }
                else
                {
                    error = true;
                }
            }
            if(numTokens == 2 && !error)
            {
                if (int.TryParse(isAttacker ? payload.KillInfo.AttackerServerId : payload.KillInfo.VictimServerId, out int secondPlayerId))
                {
                    AddTextComponentSubstringPlayerName(GetPlayerName(GetPlayerFromServerId(secondPlayerId)));
                    Debug.WriteLine($"Added {(!isAttacker ? "victim" : "attacker")} server ID");
                }
            }

            EndTextCommandThefeedPostMpticker(true, true);
        }

        [EventHandler(Events.Client.ReceiveFFAHuntedTarget)]
        public void ReceiveFFAHuntedTarget(int huntedPlayerServerId)
        {
            GameState.Hunt.HuntedPlayer = new Player(GetPlayerFromServerId(huntedPlayerServerId));
            Debug.WriteLine($"Hunting {GameState.Hunt.HuntedPlayer.Name} ({GameState.Hunt.HuntedPlayer.Handle})");
            GameState.CurrentObjective = " is the hunted! Track them down. And watch your back...";
            HuntUI.DisplayObjective(ref GameState, ref PlayerState);
        }
    }
}
