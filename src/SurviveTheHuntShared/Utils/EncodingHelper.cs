using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntShared.Utils
{
    public static class EncodingHelper
    {
        public static char CharFromUnicode(byte b1, byte b2)
        {
            char c1 = (char)b1;
            char c2 = (char)b2;

            return (char)((c2 << 8) | c1);
        }

        public static int HexFromRgba(byte r, byte g, byte b, byte a)
        {
            return (r << 24) | (g << 16) | (b << 8) | a;
        }
    }
}
