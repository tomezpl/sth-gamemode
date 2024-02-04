using CitizenFX.Core;
using SurviveTheHuntServer.Utils;
using System;
using System.Linq;
using System.Text;
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

        public void ClientStarted([FromSource] Player joinedPlayer)
        {
            BroadcastConfig(joinedPlayer, Config);
        }

        [EventHandler("sth:playerSpawned")]
        public async void PlayerSpawned(int playerId)
        {
            string playerName = Players[playerId].Name;
            Debug.WriteLine($"Player \"{playerName}\" spawned");
            int playerPed = Players[playerId].Character.Handle;
            //SetEntityDistanceCullingRadius(playerPed, float.MaxValue);
            await Delay(1000);
            TriggerClientEvent("sth:updatePlayerBlip", NetworkGetNetworkIdFromEntity(playerPed), playerId, playerName, GameState.Hunt.HuntedPlayer?.Handle == Players[playerId].Handle);


            // Wait 5s so that player peds have been replicated to the client (hopefully).
            if (Players[playerId].State.Get("sth:spawnedOnce") != true)
            {
                Players[playerId].State.Set("sth:spawnedOnce", true, false);

                await Delay(3000);
                StringBuilder playerBlipInfoBuilder = new StringBuilder();
                foreach (Player player in Players)
                {
                    bool isHunted = GameState.Hunt.HuntedPlayer?.Handle == player.Handle;
                    playerBlipInfoBuilder.Append($"{NetworkGetNetworkIdFromEntity(player.Character.Handle)},{player.Handle},{player.Name},{(isHunted ? '1' : '0')};");
                }
                playerBlipInfoBuilder.Length--;

                Debug.WriteLine($"Sending player blip info to {playerName} because they are spawning for the first time.");

                string playerBlipInfo = playerBlipInfoBuilder.ToString();
                TriggerLatentClientEvent(Players[playerId], "sth:updatePlayerBlipBulk", (playerBlipInfo.Length * sizeof(char)) / 2, playerBlipInfo);
            }

            /*Debug.WriteLine($"Cleaning clothes for {Players[playerId].Name}");
            TriggerClientEvent("sth:cleanClothesForPlayer", new { PlayerId = playerId });*/
        }
    }
}
