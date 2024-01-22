using CitizenFX.Core;
using SurviveTheHuntServer.Utils;
using System;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public partial class MainScript
    {
        public void OnServerResourceStart(string resourceName)
        {
            Debug.WriteLine($"{resourceName} resource started!");

            if(resourceName == Constants.ResourceName)
            {
                Hunt.InitHuntedQueue(Players);

                // Reload the config file every time the resource is started.
                BroadcastConfig(Config.Init());
            }
        }

        public void OnResourceStop(string resourceName)
        {
            if(resourceName == Constants.ResourceName)
            {
                Shutdown();
            }
        }

        public void ClientStarted([FromSource] Player player)
        {
            BroadcastConfig(player, Config);
        }

        [EventHandler("sth:playerSpawned")]
        public async void PlayerSpawned(int playerId)
        {
            string playerName = Players[playerId].Name;
            Debug.WriteLine($"Player \"{playerName}\" spawned");
            int playerPed = Players[playerId].Character.Handle;
            SetEntityDistanceCullingRadius(playerPed, float.MaxValue);
            await Delay(1000);
            TriggerClientEvent("sth:updatePlayerBlip", playerPed, playerId, playerName);

            /*Debug.WriteLine($"Cleaning clothes for {Players[playerId].Name}");
            TriggerClientEvent("sth:cleanClothesForPlayer", new { PlayerId = playerId });*/
        }
    }
}
