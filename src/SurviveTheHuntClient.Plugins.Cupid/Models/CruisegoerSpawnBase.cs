using System;

namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal abstract class CruisegoerSpawnBase
    {
        internal readonly float X, Y, Z;
        internal readonly float Heading;

        internal abstract PedSpawnInfo[] Build();

        internal enum SpawnShrinkStrategy
        {
            Sliced,
            Sampled,
        }

        internal abstract SpawnShrinkStrategy ShrinkStrategy { get; }

        protected static readonly Random s_RNG = new Random();

        protected CruisegoerSpawnBase(float x, float y, float z, float heading)
        {
            X = x;
            Y = y;
            Z = z;
            Heading = heading;
        }
    }
}
