using System;

namespace SurviveTheHuntShared.Utils
{
    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x = 0f, float y = 0f, float z = 0f)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double GetSqrMagnitude()
        {
            return (X * X + Y * Y + Z * Z);
        }

        public double GetMagnitude()
        {
            return Math.Sqrt(GetSqrMagnitude());
        }

        public Vector3 GetNormalised()
        {
            double mag = GetMagnitude();
            float x = (float)(X / mag);
            float y = (float)(Y / mag);
            float z = (float)(Z / mag);

            return new Vector3(x, y, z);
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            return Dot(a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public static float Dot(float aX, float aY, float aZ, float bX, float bY, float bZ)
        {
            return aX * bX + aY * bY + aZ * bZ;
        }

        public static Vector3 Zero { get => new Vector3(); }
    }
}
