using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Helpers
{
    /// <summary>
    /// A helper class to lock the car spawning logic for clients while another client is already spawning cars.
    /// </summary>
    internal static class CarSpawner
    {
        private static string _currentSpawner = null;

        /// <summary>
        /// Time when the current spawner's "lock" expires
        /// </summary>
        private static DateTime _nextSpawnUnlock = DateTime.UtcNow;

        public const ushort SpawnLockTimeoutSeconds = 15;

        internal static bool RequestSpawn(string playerHandle)
        {
            if(_currentSpawner == null || _nextSpawnUnlock < DateTime.UtcNow)
            {
                _currentSpawner = playerHandle;
                _nextSpawnUnlock = DateTime.UtcNow.AddSeconds(SpawnLockTimeoutSeconds);

                return true;
            }

            return false;
        }

        internal static bool Unlock(string playerHandle)
        {
            if(_currentSpawner == playerHandle)
            {
                _currentSpawner = null;
                return true;
            }

            return false;
        }
    }
}
