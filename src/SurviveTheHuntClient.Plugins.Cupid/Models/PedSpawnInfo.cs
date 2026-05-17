namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal struct PedSpawnInfo
    {
        internal struct PositionInfo
        {
            internal PositionInfo(float x, float y, float z, float heading)
            {
                X = x;
                Y = y;
                Z = z;
                Heading = heading;
            }

            internal readonly float X, Y, Z;
            internal readonly float Heading;
        }

        internal PositionInfo Position;

        internal struct AnimInfo
        {
            internal AnimInfo(string dict, string clip)
            {
                Dict = dict;
                Clip = clip;
            }

            internal readonly string Dict;
            internal readonly string Clip;
        }

        internal AnimInfo Anim;

        internal uint PedModel;

        internal bool NeedsWarp;
    }
}
