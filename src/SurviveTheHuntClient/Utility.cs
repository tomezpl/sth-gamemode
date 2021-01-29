using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient
{
    public static class Utility
    {
        public static TimeSpan ServerTimeOffset { get; set; } = TimeSpan.Zero;
        public static DateTime CurrentTime { get { return DateTime.UtcNow + ServerTimeOffset; } }
    }
}
