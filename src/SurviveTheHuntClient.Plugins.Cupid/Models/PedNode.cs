namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal struct PedNode
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

        /// <summary>
        /// Bitwise combined flags. You only want to set these to the return value of <see cref="SetFlags(PedNodeFlag[])"/>
        /// </summary>
        internal uint Flags;

        internal bool HasFlag(PedNodeFlag flag)
        {
            return (Flags & (1 << (int)flag)) != 0;
        }

        internal uint SetFlag(PedNodeFlag flag, bool value)
        {
            uint bit = (uint)1 << (int)flag;
            if(value)
            {
                Flags |= bit;
            }
            else
            {
                if(HasFlag(flag))
                {
                    Flags ^= bit;
                }
            }

            return Flags;
        }

        internal static uint SetFlags(params PedNodeFlag[] flags)
        {
            uint flagsCombined = 0;
            foreach(PedNodeFlag flag in flags)
            {
                flagsCombined |= (uint)1 << (int)flag;
            }

            return flagsCombined;
        }

        /// <summary>
        /// Represents the bit index of a flag. These are not actual bit flags, so you need to use <see cref="HasFlag(PedNodeFlag)"/> and <see cref="SetFlag(PedNodeFlag, bool)"/>.
        /// </summary>
        internal enum PedNodeFlag
        {
            Shower,
            NeedsWarp,
            RandomScenario,
            RandomAnim,
        }
    }
}
