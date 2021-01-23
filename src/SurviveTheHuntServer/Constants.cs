using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer
{
    public static class Constants
    {
        // TODO: These need to be overrideable through convars!
        public static readonly TimeSpan HuntDuration = TimeSpan.FromMinutes(24);
        public static readonly TimeSpan HuntedPingInterval = TimeSpan.FromMinutes(1);
    }
}
