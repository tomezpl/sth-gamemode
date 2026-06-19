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
using SurviveTheHuntShared.Interfaces;
using SurviveTheHuntShared.Plugins;

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
            if (GameState.Hunt?.IsStarted == true && GameState.Hunt?.HuntedPlayers.Length != 0)
            {
                Debug.WriteLine($"Requesting in-game clock to be re-synced from the hunted player");
                foreach(Player player in GameState.Hunt.HuntedPlayers)
                {
                    TriggerLatentClientEvent(player, Events.Client.ReceiveClockSyncRequest, 1);
                }
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

        [EventHandler(Events.Server.NotifyNetEntity)]
        public void NotifyNetEntity(int netId, string name)
        {
            Debug.WriteLine($"Notifying all players about entity {netId}{(string.IsNullOrWhiteSpace(name) ? "." : $" (named \"{name}\").")}");
            TriggerClientEvent(Events.Client.RecvNetEntity, netId, name);
        }

        [EventHandler(Events.Server.SetServerState)]
        public void SetServerState([FromSource] Player player, byte pluginIndex, byte serverStatePropId, object serverStatePropValue)
        {
            Debug.WriteLine($"{nameof(SetServerState)} received with {nameof(pluginIndex)} {pluginIndex}: prop {serverStatePropId} = {serverStatePropValue}");
            
            if(GameState?.Hunt?.PluginStates != null)
            {
                bool isPluginIndex = Enum.IsDefined(typeof(PluginIndex), (int)pluginIndex);
                if(isPluginIndex)
                {
                    IPluginState state = GameState.Hunt.GetOrCreatePluginState((PluginIndex)pluginIndex);
                    if(state != null)
                    {
                        Debug.WriteLine($"Setting state prop {serverStatePropId} to {serverStatePropValue} on state impl {state}");
                        state.Set(serverStatePropId, serverStatePropValue, CreatePluginContext(player));
                    }
                    else
                    {
                        Debug.WriteLine($"Plugin {(PluginIndex)pluginIndex} does not implement a state");
                    }
                }
                else
                {
                    Debug.WriteLine($"{nameof(pluginIndex)} {pluginIndex} is not a valid {nameof(PluginIndex)}");
                }
            }
        }
    }
}
