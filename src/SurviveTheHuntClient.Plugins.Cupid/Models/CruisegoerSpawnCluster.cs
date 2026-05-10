using CitizenFX.Core;
using System;

namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal sealed class CruisegoerSpawnCluster : CruisegoerSpawnBase
    {
        internal override SpawnShrinkStrategy ShrinkStrategy => SpawnShrinkStrategy.Sliced;

        internal override PedSpawnInfo[] Build()
        {
            float angle = (float)(s_RNG.NextDouble() * Math.PI * 2);
            float perpendicular = angle + (float)(Math.PI * .5f);

            PedSpawnInfo[] info = new PedSpawnInfo[PedCount];

            float rightX = (float)Math.Cos(angle), rightY = (float)Math.Sin(angle);
            float backX = (float)Math.Cos(perpendicular), backY = (float)Math.Sin(perpendicular);

            // sqrt(a^2 + b^2) / 2
            float halfHypot = 0.5f * (float)Math.Sqrt(2f * ((Radius * 2f) * (Radius * 2f)));

            float gap = PedSafeRadius * 2f;

            float squareEdge = Radius * 2f;

            // front-left
            float originX = X - (rightX * halfHypot) - (backX * halfHypot), originY = Y - (rightY * halfHypot) - (backY * halfHypot);

            byte spawned = 0;
            for(float currentY = originY + PedSafeRadius; spawned < PedCount && Math.Abs(currentY - originY) <= squareEdge + float.Epsilon; currentY += (backY * gap) + (rightY * gap))
            {
                float newOriginX = (originX + PedSafeRadius) + (backX * (currentY - (originY + PedSafeRadius)));
                for (float currentX = newOriginX; spawned < PedCount && Math.Abs(currentX - originX) <= squareEdge + float.Epsilon; currentX += (backX * gap) + (rightX * gap))
                {
                    info[spawned++] = new PedSpawnInfo
                    {
                        Position = new PedSpawnInfo.PositionInfo(currentX, currentY, Z, (float)((s_RNG.NextDouble() * 360f) - 180f)),
                        // TODO
                        Anim = new PedSpawnInfo.AnimInfo("", ""),
                        PedModel = PedModels[s_RNG.Next(0, PedModels.Length)],
                    };
                }
            }

            return info;
        }

        internal const float PedSafeRadius = 0.8f;

        internal readonly byte PedCount;

        internal readonly float Radius;

        internal readonly uint[] PedModels;

        internal CruisegoerSpawnCluster(float x, float y, float z, byte maxPedCount, float clusterRadius, uint[] pedModels, bool copyModels = true) : base(x, y, z, 0f)
        {
            Radius = clusterRadius;
            PedCount = Math.Min(maxPedCount, Math.Max((byte)1, CalculateMaxPedsInRadius(clusterRadius)));
            
            if (copyModels)
            {
                PedModels = new uint[pedModels.Length];
                Array.Copy(pedModels, PedModels, pedModels.Length);
            }
            else
            {
                PedModels = pedModels;
            }
        }

        internal CruisegoerSpawnCluster(float x, float y, float z, byte maxPedCount, float clusterRadius) : this(x, y, z, maxPedCount, clusterRadius, Constants.CruisegoerSpawns.PedModels, false)
        {

        }

        internal static byte CalculateMaxPedsInRadius(float radius)
        {
            // Use radius as square half-extents
            float squareEdge = radius * 2f;
            float totalArea = squareEdge * squareEdge;

            float pedSquareEdge = PedSafeRadius * 2f;
            float pedArea = pedSquareEdge * pedSquareEdge;

            return (byte)Math.Floor(totalArea / pedArea);
        }
    }
}
