using System;

namespace SurviveTheHuntShared.Utils
{
    public class ReadonlyVector3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public ReadonlyVector3(float x = 0f, float y = 0f, float z = 0f)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static float Dot(ReadonlyVector3 a, ReadonlyVector3 b)
        {
            return Dot(a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public static float Dot(float aX, float aY, float aZ, float bX, float bY, float bZ)
        {
            return aX * bX + aY * bY + aZ * bZ;
        }
    }

    public class Vector3 : ReadonlyVector3
    {
        public new float X { get; set; }
        public new float Y { get; set; }
        public new float Z { get; set; }

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

        public static Vector3 Zero { get => new Vector3(); }
    }
}
