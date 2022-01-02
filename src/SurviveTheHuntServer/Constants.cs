using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer
{
    /// <remarks>TODO: These need to be overrideable through convars!</remarks>
    public static class Constants
    {
        /// <summary>
        /// Amount of time the hunt should last for.
        /// </summary>
        public static readonly TimeSpan HuntDuration = TimeSpan.FromMinutes(24);

        /// <summary>
        /// How often the hunted player should be pinged on the map.
        /// </summary>
        public static readonly TimeSpan HuntedPingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// How often the server should sync time with clients.
        /// </summary>
        public static readonly TimeSpan TimeSyncInterval = TimeSpan.FromSeconds(20);
    }
}
