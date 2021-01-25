using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient
{
    /// <summary>
    /// Constant values used throughout the gamemode.
    /// </summary>
    public static class Constants
    {
        public static readonly Vec3 DockSpawn = new Vec3 { X = 851.379f, Y = -3140.005f, Z = 5.900808f };
        public static readonly float HuntedBlipLifespan = 50f;
        public static readonly float HuntedBlipFadeoutTime = 5f;
    }
}
