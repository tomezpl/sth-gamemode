using CitizenFX.Core;
using SurviveTheHuntServer.Utils;
using System.Collections.Generic;
using System.Linq;

namespace SurviveTheHuntServer
{
    public partial class MainScript
    {
        private readonly List<int> SpawnedVehicles = new List<int>();

        public void OnServerResourceStart(string resourceName)
        {
            Debug.WriteLine($"{resourceName} resource started!");

            if(resourceName == Constants.ResourceName)
            {
                // Reload the config file every time the resource is started.
                BroadcastConfig(Config.Init());
                SyncVehicles(SpawnedVehicles);
            }
        }

        public void ClientStarted([FromSource] Player player)
        {
            BroadcastConfig(player, Config);
            SyncVehicles(SpawnedVehicles);
        }

        [EventHandler("sth:reqSyncVehicles")]
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
            TriggerClientEvent("sth:recvSyncVehicles", vehicleNetIdsPacked);
        }
    }
}
