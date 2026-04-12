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

        public static uint PackRgba(byte r, byte g, byte b, byte a)
        {
            return (uint)((r << 24) | (g << 16) | (b << 8) | a);
        }

        public static void UnpackRgba(in uint packed, out byte r, out byte g, out byte b, out byte a)
        {
            r = (byte)(packed >> 24);
            g = (byte)((packed >> 16) & 0xFF);
            b = (byte)((packed >> 8) & 0xFF);
            a = (byte)(packed & 0xFF);
        }

        private const byte BitsInFloat = sizeof(float) * 8;

        /// <summary>
        /// "Converts" a unit float to a UTF-16 string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// IMPORTANT: This loses precision, because we can't do bitwise on floating point,
        /// so we scale the float to a 32-bit integer range, then split that into two UTF-16 characters
        /// </remarks>
        public static string Utf16FromNormalFloat(float value)
        {
            int asInt = (int)(value * int.MaxValue);
            char c1 = (char)(asInt >> (BitsInFloat / 2));
            char c2 = (char)(asInt & (1 << ((BitsInFloat / 2) - 1)));
            return string.Concat(c1, c2);
        }

        /// <summary>
        /// Converts a string back into a unit float. If used with <see cref="Utf16FromNormalFloat(float)"/>,
        /// the original float is pretty much guaranteed to have a minor error, but it's good enough for packing/unpacking to/from UTF-16.
        /// </summary>
        /// <param name="utf16"></param>
        /// <returns></returns>
        public static float NormalFloatFromUtf16(string utf16)
        {
            int asInt = (utf16[0] << (BitsInFloat / 2)) | utf16[1];
            const float correction = 1.0000915f;
            return ((float)asInt / int.MaxValue) * correction;
        }
    }
}
