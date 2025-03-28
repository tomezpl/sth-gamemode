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

            if(GameState.Hunt.GameMode == SurviveTheHuntShared.Core.HuntedQueueType.FreeForAll)
            {
                if(int.TryParse(payload.KillInfo.VictimServerId, out int victimServerId) && victimServerId == GameState.Hunt.HuntedPlayer?.ServerId)
                {
                    BeginTextCommandThefeedPost("STRING");
                    AddTextComponentSubstringPlayerName($"Your target {GetPlayerName(GetPlayerFromServerId(victimServerId))} has died. Standby for new mission...");
                    EndTextCommandThefeedPostMpticker(true, true);

                    PendingFFATargetRequest = true;

                    // Wait between 5-10s for a new target.
                    FFATargetRequestTimeDelay = RNG.Next(5, 11);
                    FFATargetTimeWaited = 0f;
                    BeginTextCommandPrint("CURRENT_OBJECTIVE");
                    AddTextComponentString("");
                    EndTextCommandPrint(1, true);
                }
            }
        }

        [EventHandler(Events.Client.ReceiveFFAHuntedTarget)]
        public void ReceiveFFAHuntedTarget(string huntedPlayerServerId)
        {
            bool needToRetry = false;

            // TODO: the obvious downside of this is that we can still be assigned a player who hasn't spawned in yet,
            // which means they won't have a ped yet. Ideally we should only be adding players to the FFA queue once they've spawned in for the first time.
            if (huntedPlayerServerId == SurviveTheHuntShared.Constants.NoAvailableFFATargetServerId)
            {
                GameState.Hunt.HuntedPlayer = null;
                GameState.CurrentObjective = "Searching for targets...";
                needToRetry = true;
            }
            else
            {
                GameState.Hunt.HuntedPlayer = new Player(GetPlayerFromServerId(int.Parse(huntedPlayerServerId)));
                if (GameState.Hunt.HuntedPlayer.Handle != -1)
                {
                    Debug.WriteLine($"Hunting {GameState.Hunt.HuntedPlayer.Name} ({GameState.Hunt.HuntedPlayer.Handle})");
                    GameState.CurrentObjective = " is the hunted! Track them down. And watch your back...";
                }
                else
                {
                    GameState.Hunt.HuntedPlayer = null;
                    needToRetry = true;
                }
            }
            HuntUI.DisplayObjective(ref GameState, ref PlayerState);

            if(needToRetry)
            {
                PendingFFATargetRequest = true;
                FFATargetTimeWaited = 0f;
                FFATargetRequestTimeDelay = RNG.Next(5, 11);
            }
        }
    }
}
