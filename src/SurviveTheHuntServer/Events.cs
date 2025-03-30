using CitizenFX.Core;
using SurviveTheHuntServer;
using SharedConstants = SurviveTheHuntShared.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using static CitizenFX.Core.Native.API;
using SurviveTheHuntShared;
using SurviveTheHuntShared.Core;
using SurviveTheHuntServer.Helpers;
using System.Dynamic;
using System.Globalization;

namespace SurviveTheHuntServer
{
    public partial class MainScript
    {
        private readonly List<int> SpawnedVehicles = new List<int>();

        public void OnServerResourceStart(string resourceName)
        {
            Debug.WriteLine($"{resourceName} resource started!");

            if (resourceName == SharedConstants.ResourceName)
            {
                // Reload the config file every time the resource is started.
                Config = ServerConfig.FromJsonFile();
                BroadcastConfig(Config);
                SyncVehicles(SpawnedVehicles);
            }
        }

        public void ClientStarted([FromSource] Player player)
        { 
            BroadcastConfig(player, Config);
            SendGameState(player, GameState, HuntedPlayerQueue);
            SyncVehicles(SpawnedVehicles);
        }

        [EventHandler(Events.Server.RequestSyncVehicles)]
        public void SyncVehiclesRequested([FromSource] Player player, string vehicleNetIdsPacked)
        {
            Debug.WriteLine($"Received vehicleNetIdsPacked: {vehicleNetIdsPacked}");
            int validNetIdCount = 0;
            int[] vehicleNetIds = vehicleNetIdsPacked.Split(';').Select((netIdStr) =>
            {
                Debug.WriteLine(netIdStr);
                if (int.TryParse(netIdStr, out int netId))
                {
                    Debug.WriteLine($"{netId}");
                    validNetIdCount++;
                    return netId;
                }
                else
                {
                    return -1;
                }
            }).ToArray();

            if (validNetIdCount > 0)
            {
                Debug.WriteLine($"Overwriting SpawnedVehicles with {vehicleNetIds.Length} netIds");
                SpawnedVehicles.Clear();
                SpawnedVehicles.AddRange(vehicleNetIds);

                SyncVehicles(vehicleNetIdsPacked);
            }
        }

        private void SyncVehicles(List<int> vehicleNetIds)
        {
            string vehicleNetIdsPacked = "";
            foreach (int netId in vehicleNetIds)
            {
                vehicleNetIdsPacked += $"{netId};";
            }

            if (vehicleNetIdsPacked.Length > 1)
            {
                // Remove trailing semicolon.
                vehicleNetIdsPacked = vehicleNetIdsPacked.Remove(vehicleNetIdsPacked.Length - 1, 1);

                SyncVehicles(vehicleNetIdsPacked);
            }
        }

        private void SyncVehicles(string vehicleNetIdsPacked)
        {
            TriggerClientEvent(Events.Client.ReceiveSyncedVehicles, vehicleNetIdsPacked);
        }

        [EventHandler(Events.Server.RequestDeleteVehicle)]
        public void DeleteVehicle([FromSource] Player player, int vehicleNetId)
        {
            Debug.WriteLine($"Player {player.Name} ({player.Handle}) is requesting to delete vehicle with net ID {vehicleNetId}");
            try
            {
                TriggerClientEvent(Events.Client.ReceiveDeleteVehicle, vehicleNetId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Couldn't delete vehicle: {ex.ToString()}");
            }
        }

        [EventHandler(Events.Server.ReceiveHuntedClock)]
        public void SyncHuntedClock(int hours, int minutes, int seconds)
        {
            Debug.WriteLine($"Sending {Events.Client.ReceiveHuntedClock} with {hours.ToString().PadLeft(2, '0')}:{minutes.ToString().PadLeft(2, '0')}:{seconds.ToString().PadLeft(2, '0')}");
            TriggerClientEvent(Events.Client.ReceiveHuntedClock, hours, minutes, seconds);
        }

        [EventHandler(Events.Server.HuntedClockSyncRequested)]
        public void RequestHuntedClockResync()
        {
            // TODO: this won't work for FFA
            if (GameState.Hunt?.IsStarted == true && GameState.Hunt?.HuntedPlayer != null)
            {
                Debug.WriteLine($"Requesting in-game clock to be re-synced from the hunted player");
                TriggerLatentClientEvent(GameState.Hunt.HuntedPlayer, Events.Client.ReceiveClockSyncRequest, 1);
            }
        }

        [EventHandler(Events.Server.PlayerDied)]
        public void OnPlayerDied(string dataSerialized)
        {
            Debug.WriteLine(dataSerialized);
            PlayerDiedPayload data = PlayerDiedPayload.Deserialize(dataSerialized);
            int playerId = data.PlayerId;
            KillFeedClientPayload killInfo = data.KillInfo;

            Debug.WriteLine($"Player died: {GetPlayerName($"{playerId}")}");

            // Did the hunted player die?
            if (Hunt.CheckPlayerDeath(Players[playerId], ref GameState))
            {
                NotifyWinner();
                ResetTeams();
            }

            if(GameState.Hunt.IsStarted && GameState.Hunt.GameMode == HuntedQueueType.FreeForAll)
            {
                FFAHuntedQueue ffaQueue = (FFAHuntedQueue)HuntedPlayerQueue;
                List<Player> playersToNotify = ffaQueue.RemoveTarget(Players[playerId]);
                /*foreach(Player player in playersToNotify)
                {
                    ffaQueue.SetCurrentPlayer(player);
                    Player newTarget = ffaQueue.PopNext();
                    Debug.WriteLine($"new target for {player.Name} ({player.Handle}) is {newTarget?.Name} ({newTarget?.Handle})");
                    if (newTarget != null)
                    {
                        TriggerClientEvent(player, Events.Client.ReceiveFFAHuntedTarget, int.Parse(newTarget.Handle));
                    }
                }*/
            }

            // Mark the player's death location with a blip for everyone.
            TriggerClientEvent(Events.Client.MarkPlayerDeath, data.PlayerPosX, data.PlayerPosY, data.PlayerPosZ, data.PlayerTeam);

            // Broadcast a killfeed message.
            KillFeedServerPayload killfeedPayload = KillFeedDispatcher.GetKillFeedPayload(killInfo, GameState, RNG);
            TriggerClientEvent(Events.Client.DisplayKill, KillFeedServerPayload.Serialize(killfeedPayload));
        }

        [EventHandler(Events.Server.RequestStartHunt)]
        public void HuntRequested([FromSource] Player source, int huntType, int? huntedPlayer)
        {
            // Prevent the next hunt from being started too quick.
            if (GameState.Hunt.IsStarted || (GameState.Hunt.NextHuntStartTime != null && GameState.Hunt.NextHuntStartTime > DateTime.UtcNow))
            {
                return;
            }

            // Set default arguments and try to read them from the event payload.
            int?[] args = { (int)HuntedQueueType.SingleHunted };
            try
            {
                args = new int?[] { huntType, null };
                args[1] = huntedPlayer;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"couldn't parse args: {ex}");
            }

            Debug.WriteLine($"huntType {huntType}");

            // Instantiate the correct queue
            if(args.Length >= 1)
            {
                switch (args[0])
                {
                    case (int)HuntedQueueType.SingleHunted:
                        if(HuntedPlayerQueue.Type != HuntedQueueType.SingleHunted)
                        {
                            Debug.WriteLine("Creating a SingleHuntedQueue");
                            HuntedPlayerQueue = new SingleHuntedQueue(Players);
                        }
                        break;
                    case (int)HuntedQueueType.FreeForAll:
                        Debug.WriteLine("Creating an FFAQueue");
                        HuntedPlayerQueue = new FFAHuntedQueue(Players);
                        break;
                }
            }

            Player randomPlayer = null;
            if (HuntedPlayerQueue.Type == HuntedQueueType.SingleHunted)
            {
                // Check if a specific player was requested when the hunt was started.
                int? requestedPlayer = null;
                try
                {
                    requestedPlayer = args[1];
                }
                catch
                {
                    requestedPlayer = null;
                }

                if (requestedPlayer != null)
                {
                    foreach (Player player in Players)
                    {
                        if (player.Handle == requestedPlayer.Value.ToString())
                        {
                            randomPlayer = player;
                            break;
                        }
                    }
                }

                if (randomPlayer == null)
                {
                    randomPlayer = Hunt.ChooseRandomPlayer(Players, ref GameState);
                }

                GameState.Hunt.LastHuntedPlayer = randomPlayer;

                TriggerClientEvent(randomPlayer, Events.Client.NotifyHuntedPlayer);
                TriggerClientEvent(Events.Client.NotifyHunters, new { HuntedPlayerServerId = int.Parse(randomPlayer.Handle), IsFFA = false });
            }
            else if(HuntedPlayerQueue.Type == HuntedQueueType.FreeForAll)
            {
                TriggerClientEvent(Events.Client.NotifyHuntedPlayer, new {IsFFA = true});
                /*foreach (Player player in Players)
                {
                    FFAHuntedQueue ffaQueue = (FFAHuntedQueue)HuntedPlayerQueue;
                    ffaQueue.SetCurrentPlayer(player);
                    Player target = ffaQueue.PopNext();
                    if(target != null)
                    {
                        Debug.WriteLine($"{player.Name} ({player.Handle}) will hunt {target.Name} ({target.Handle})");
                    }
                }*/
            }
            ulong prepPhaseSeconds = (ulong)GetConvarInt("sth_prepPhaseDuration", SharedConstants.DefaultPrepPhaseSeconds);

            SetConvarReplicated(SharedConstants.CharCreatorBlockCreatorConvar, "true");

            Debug.WriteLine($"Starting a {HuntedPlayerQueue.Type} hunt");
            GameState.Hunt.Begin(randomPlayer, prepPhaseSeconds, HuntedPlayerQueue.Type);

            Debug.WriteLine($"Sending HuntStartedByServer, source = {source}");
            TriggerClientEvent(Events.Client.HuntStartedByServer, new
            {
                EndTime = GameState.Hunt.EndTime.ToString("F", CultureInfo.InvariantCulture),
                NextNotification = (float)prepPhaseSeconds + (float)SharedConstants.HuntedPingInterval.TotalSeconds,
                PrepPhaseDuration = prepPhaseSeconds,
                GameMode = (int)HuntedPlayerQueue.Type,
                Requester = int.Parse(source.Handle)
            });

            foreach (Player player in Players)
            {
                JoinTeam(player, randomPlayer != null && player.Handle != randomPlayer.Handle ? Teams.Team.Hunted : Teams.Team.Hunters);
            }
        }

        [EventHandler(Events.Server.RequestFFAHuntedTarget)]
        public void FFAHuntTargetRequested([FromSource] Player hunter)
        {
            FFAHuntedQueue queue = (FFAHuntedQueue)HuntedPlayerQueue;
            queue.SetCurrentPlayer(hunter);
            Player target = queue.PopNext();
            if (target != null)
            {
                Debug.WriteLine($"{hunter.Name} ({hunter.Handle}) will hunt {target.Name} ({target.Handle})");
                TriggerClientEvent(hunter, Events.Client.ReceiveFFAHuntedTarget, int.Parse(target.Handle));
            }
            else
            {
                Debug.WriteLine($"No target available for {hunter.Name}.");
                TriggerClientEvent(hunter, Events.Client.ReceiveFFAHuntedTarget, SharedConstants.NoAvailableFFATargetServerId);
            }
        }

        [EventHandler(Events.Server.BroadcastHuntedZone)]
        public void BroadcastHuntedZone([FromSource] Player source, float posX, float posY, float posZ)
        {
            Vector3 pos = new Vector3(posX, posY, posZ);
            dynamic payload = new { PlayerServerId = source.Handle, Position = pos, NextNotification = (float)SharedConstants.HuntedPingInterval.TotalSeconds };

            if (HuntedPlayerQueue.Type == HuntedQueueType.SingleHunted)
            {
                TriggerClientEvent(Events.Client.NotifyAboutHuntedZone, payload);
            }
            else
            {
                //Debug.WriteLine($"Source is {source.Name} ({source.Handle})");
                foreach(Player player in Players)
                {
                    FFAHuntedQueue ffaQueue = (FFAHuntedQueue)HuntedPlayerQueue;
                    ffaQueue.SetCurrentPlayer(player);
                    //Debug.WriteLine($"{player.Name} ({player.Handle})'s current target is {ffaQueue.CurrentTarget?.Name} ({ffaQueue.CurrentTarget?.Handle})");
                    if(ffaQueue.CurrentTarget == source)
                    {
                        TriggerClientEvent(player, Events.Client.NotifyAboutHuntedZone, payload);
                    }
                }
            }
        }
    }
}
