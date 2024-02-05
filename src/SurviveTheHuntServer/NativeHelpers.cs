using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public partial class MainScript
    {
        /// <summary>
        /// Spawns a server-owned vehicle.
        /// </summary>
        /// <param name="carName"></param>
        /// <param name="pos"></param>
        /// <param name="heading"></param>
        /// <returns>Vehicle entity handle. Keep in mind the returned entity handle may not be usable until the next tick.</returns>
        /// <remarks>This is just a wrapper over <see cref="CreateVehicle(uint, float, float, float, float, bool, bool)"/> that registers the vehicle entity to be cleaned up on Shutdown.</remarks>
        public async Task<int> SpawnVehicle(string carName, Vector3 pos, float heading)
        {
            uint vehicleHash = (uint)GetHashKey(carName);
            int spawnedVehicle = CreateVehicle(vehicleHash, pos.X, pos.Y, pos.Z, heading, true, false);
            EntityHandles.Add(spawnedVehicle);
            await Delay(10);
            return spawnedVehicle;
        }
    }
}
