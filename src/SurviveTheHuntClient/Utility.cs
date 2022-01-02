using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient
{
    public static class Utility
    {
        /// <summary>
        /// Stores the desync between the client and the server.
        /// </summary>
        public static TimeSpan ServerTimeOffset { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Stores the current UTC time, accounting for the server desync.
        /// </summary>
        public static DateTime CurrentTime { get { return DateTime.UtcNow + ServerTimeOffset; } }
    }
}
