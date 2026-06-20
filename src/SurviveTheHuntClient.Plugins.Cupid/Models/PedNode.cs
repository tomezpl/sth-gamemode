using System.Collections.Generic;
using CitizenFX.Core;

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

            internal PositionInfo(string encoded)
            {
                string[] comps = encoded.Split(' ');
                X = float.Parse(comps[0]);
                Y = float.Parse(comps[1]);
                Z = float.Parse(comps[2]);
                Heading = float.Parse(comps[3]);
            }

            internal readonly float X, Y, Z;
            internal readonly float Heading;

            public override string ToString()
            {
                return $"{X} {Y} {Z} {Heading}";
            }

            internal const int WhitespaceCount = 3;
        }

        internal PositionInfo Position;

        internal struct AnimInfo
        {
            internal AnimInfo(string dict, string clip)
            {
                Dict = dict;
                Clip = clip;
            }

            internal AnimInfo(string encoded)
            {
                int spaceIndex = encoded.IndexOf(" ");
                if (spaceIndex != -1)
                {
                    Dict = encoded.Substring(0, spaceIndex);
                    Clip = encoded.Substring(spaceIndex + 1);
                }
                else
                {
                    Debug.WriteLine($"Could not decode {encoded}, missing whitespace");
                    Dict = encoded;
                    Clip = encoded;
                }
            }

            internal readonly string Dict;
            internal readonly string Clip;

            public override string ToString()
            {
                return $"{Dict} {Clip}";
            }

            internal const int WhitespaceCount = 1;
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
            if (value)
            {
                Flags |= bit;
            }
            else
            {
                if (HasFlag(flag))
                {
                    Flags ^= bit;
                }
            }

            return Flags;
        }

        internal static uint SetFlags(params PedNodeFlag[] flags)
        {
            uint flagsCombined = 0;
            foreach (PedNodeFlag flag in flags)
            {
                flagsCombined |= (uint)1 << (int)flag;
            }

            return flagsCombined;
        }

        /// <summary>
        /// Provides a string-encoded representation of the PedNode that can be used to serialise it for other clients.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Position} {Anim} {Flags}";
        }

        internal PedNode(string encoded)
        {
            int posAnimSeparatorIndex = -1, animFlagsSeparatorIndex = -1, flagsPedModelSeparatorIndex = -1;

            int whitespaceCount = 0;

            for (int i = 0; i < encoded.Length; i++)
            {
                char c = encoded[i];
                if (c == ' ')
                {
                    if (animFlagsSeparatorIndex != -1 && posAnimSeparatorIndex != -1)
                    {
                        flagsPedModelSeparatorIndex = i;
                    }

                    if (whitespaceCount > AnimInfo.WhitespaceCount && animFlagsSeparatorIndex == -1)
                    {
                        animFlagsSeparatorIndex = i;
                        whitespaceCount = -1;
                    }

                    if (whitespaceCount > PositionInfo.WhitespaceCount && posAnimSeparatorIndex == -1)
                    {
                        posAnimSeparatorIndex = i;
                        whitespaceCount = -1;
                    }

                    whitespaceCount++;
                }

                if (animFlagsSeparatorIndex != -1 && posAnimSeparatorIndex != -1 && flagsPedModelSeparatorIndex != -1)
                {
                    break;
                }
            }

            string posEncoded = encoded.Substring(0, posAnimSeparatorIndex);
            Debug.WriteLine($"{nameof(posEncoded)}: {posEncoded}");
            string animEncoded = encoded.Substring(posAnimSeparatorIndex + 1, (animFlagsSeparatorIndex - posAnimSeparatorIndex) - 1);
            Debug.WriteLine($"{nameof(animEncoded)}: {animEncoded}");
            string flagsEncoded = encoded.Substring(animFlagsSeparatorIndex + 1, (flagsPedModelSeparatorIndex - animFlagsSeparatorIndex) - 1);
            Debug.WriteLine($"{nameof(flagsEncoded)}: {flagsEncoded}");
            string pedModelEncoded = encoded.Substring(flagsPedModelSeparatorIndex + 1);
            Debug.WriteLine($"{nameof(pedModelEncoded)}: {pedModelEncoded}");

            Position = new PositionInfo(posEncoded);
            Anim = new AnimInfo(animEncoded);
            Flags = uint.Parse(flagsEncoded);
            PedModel = uint.Parse(pedModelEncoded);
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
