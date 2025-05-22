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

namespace SurviveTheHuntServer
{
    public partial class MainScript
    {
        private readonly List<int> SpawnedVehicles = new List<int>();

        public void OnServerResourceStart(string resourceName)
        {
            Debug.WriteLine($"{resourceName} resource started!");

            if(resourceName == SharedConstants.ResourceName)
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
            SendGameState(player, GameState);
            SyncVehicles(SpawnedVehicles);
        }

        [EventHandler(Events.Server.RequestSpawnVehiclesPermission)]
        public void VehicleSpawnRequested([FromSource] Player player)
        {
            Debug.WriteLine($"{player.Name} is requesting to spawn cars");
            bool canSpawn = CarSpawner.RequestSpawn(player.Handle);
            if(canSpawn)
            {
                Debug.WriteLine($"{player.Name} (server ID {player.Handle}) is allowed to spawn cars, will time out in {CarSpawner.SpawnLockTimeoutSeconds}s");
            }
            else
            {
                Debug.WriteLine($"{player.Name} (server ID {player.Handle}) is not allowed to spawn cars as someone else is currently spawning.");
            }

            TriggerClientEvent(player, Events.Client.ReceiveVehicleSpawnPermission, canSpawn);
        }

        [EventHandler(Events.Server.NotifySpawnedVehicles)]
        public void AcknowledgeSpawnedVehicles([FromSource] Player player)
        {
            bool unlocked = CarSpawner.Unlock(player.Handle);
            if(unlocked)
            {
                Debug.WriteLine($"{player.Name} (server ID {player.Handle} has finished spawning vehicles");
            }
        }

        [EventHandler(Events.Server.RequestSyncVehicles)]
        public void SyncVehiclesRequested([FromSource] Player player, string vehicleNetIdsPacked)
        {
            Debug.WriteLine($"Received vehicleNetIdsPacked: {vehicleNetIdsPacked}");
            int validNetIdCount = 0;
            int[] vehicleNetIds = vehicleNetIdsPacked.Split(';').Select((netIdStr) => {
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

            if(validNetIdCount > 0)
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

            if(vehicleNetIdsPacked.Length > 1)
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
            catch(Exception ex)
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

            Console.WriteLine($"Player died: {GetPlayerName($"{playerId}")}");

            // Did the hunted player die?
            if (Hunt.CheckPlayerDeath(Players[playerId], ref GameState))
            {
                NotifyWinner();
                ResetTeams();
            }

            // Mark the player's death location with a blip for everyone.
            TriggerClientEvent(Events.Client.MarkPlayerDeath, data.PlayerPosX, data.PlayerPosY, data.PlayerPosZ, data.PlayerTeam);

            // Broadcast a killfeed message.
            KillFeedServerPayload killfeedPayload = KillFeedDispatcher.GetKillFeedPayload(killInfo, GameState, RNG);
            TriggerClientEvent(Events.Client.DisplayKill, KillFeedServerPayload.Serialize(killfeedPayload));
        }

        [EventHandler(Events.Server.ReceiveNewMusicIntensity)]
        public void BroadcastNewMusicIntensity(uint intensity)
        {
            TriggerClientEvent(Events.Client.ReceiveNewMusicIntensity, intensity);
        }
    }
}
