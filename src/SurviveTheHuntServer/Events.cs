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
            StringBuilder playerBlipInfoBuilder = new StringBuilder();
            foreach(Player player in Players)
            {
                bool isHunted = GameState.Hunt.HuntedPlayer?.Handle == player.Handle;
                playerBlipInfoBuilder.Append($"{NetworkGetEntityFromNetworkId(player.Character.Handle)},{player.Handle},{player.Name},{isHunted};");
            }
            playerBlipInfoBuilder.Length--;

            TriggerClientEvent(joinedPlayer, "sth:updatePlayerBlipBulk", playerBlipInfoBuilder.ToString());
        }

        [EventHandler("sth:playerSpawned")]
        public async void PlayerSpawned(int playerId)
        {
            string playerName = Players[playerId].Name;
            Debug.WriteLine($"Player \"{playerName}\" spawned");
            int playerPed = Players[playerId].Character.Handle;
            SetEntityDistanceCullingRadius(playerPed, float.MaxValue);
            await Delay(1000);
            TriggerClientEvent("sth:updatePlayerBlip", NetworkGetNetworkIdFromEntity(playerPed), playerId, playerName, GameState.Hunt.HuntedPlayer?.Handle == Players[playerId].Handle);

            /*Debug.WriteLine($"Cleaning clothes for {Players[playerId].Name}");
            TriggerClientEvent("sth:cleanClothesForPlayer", new { PlayerId = playerId });*/
        }
    }
}
