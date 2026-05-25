using CitizenFX.Core;
using SurviveTheHuntClient.Models.Utils;
using System;
using System.Collections.Generic;

namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal sealed class CruisegoerSpawnCluster : CruisegoerSpawnBase
    {
        internal override SpawnShrinkStrategy ShrinkStrategy => SpawnShrinkStrategy.Sliced;

        internal override PedNode[] Build()
        {
            // Random ped count in the given range
            byte pedCount = MinPedCount == MaxPedCount ? MinPedCount : (byte)s_RNG.Next(MinPedCount, MaxPedCount + 1);

            // create points in axis aligned square with half extents equal to radius
            // then filter out points that aren't in radius
            List<PedNode> spawns = new List<PedNode>(pedCount);

            float halfHypot = 0.5f * (float)Math.Sqrt(2f * ((Radius * 2f) * (Radius * 2f)));
            float originX = X - Radius;
            float originY = Y - Radius;
            float endX = X + Radius;
            float endY = Y + Radius;
            float centreX = originX + (endX - originX) * .5f;
            float centreY = originY + (endY - originY) * .5f;
            float radiusSq = Radius * Radius;

            float gap = (Radius * 2f) / (float)pedCount;

            List<uint> uniquePedModels = new List<uint>(PedModels);

            for(float currentX = originX; spawns.Count < pedCount && currentX <= endX + float.Epsilon; currentX += gap)
            {
                for(float currentY = originY; spawns.Count < pedCount && currentY <= endY + float.Epsilon; currentY += gap)
                {
                    float a = currentX - centreX;
                    float b = currentY - centreY;

                    if (((a * a) + (b * b)) <= radiusSq + float.Epsilon)
                    {
                        // introduce random error
                        const float MaxError = 0.55f;
                        const float MaxErrorHalf = MaxError * .5f;
                        float xError = (float)(s_RNG.NextDouble() * MaxError - MaxErrorHalf);
                        float yError = (float)(s_RNG.NextDouble() * MaxError - MaxErrorHalf);

                        int randomPedModelIndex = s_RNG.Next(0, uniquePedModels.Count);
                        spawns.Add(new PedNode
                        {
                            Anim = new PedNode.AnimInfo("", ""),
                            PedModel = uniquePedModels[randomPedModelIndex],
                            Position = new PedNode.PositionInfo(currentX + xError, currentY + yError, Z, (float)(s_RNG.NextDouble() * 360 - 180)),
                            Flags = PedNodeFlags,
                        });

                        uniquePedModels.RemoveAt(randomPedModelIndex);
                        if(uniquePedModels.Count == 0)
                        {
                            uniquePedModels.AddRange(PedModels);
                        }
                    }
                }
            }

            return spawns.ToArray();

            // old algorithm (spawning in a rotated square with half extents equal to radius)
            // doesn't work
            /*
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
            */
        }

        internal const float PedSafeRadius = 0.8f;

        internal readonly byte MinPedCount;
        internal readonly byte MaxPedCount;

        internal readonly float Radius;

        internal readonly uint[] PedModels;

        internal readonly uint PedNodeFlags;

        internal CruisegoerSpawnCluster(float x, float y, float z, byte maxPedCount, uint pedNodeFlags, float clusterRadius, uint[] pedModels, bool copyModels = true) : this(x, y, z, new Range<byte>(maxPedCount), pedNodeFlags, clusterRadius, pedModels, copyModels)
        {
            
        }

        internal CruisegoerSpawnCluster(float x, float y, float z, Range<byte> pedCountRange, uint pedNodeFlags, float clusterRadius, uint[] pedModels, bool copyModels = true) : base(x, y, z, 0f)
        {
            Radius = clusterRadius;
            MaxPedCount = Math.Min(pedCountRange.Max, Math.Max((byte)1, CalculateMaxPedsInRadius(clusterRadius)));
            MinPedCount = Math.Min(MaxPedCount, pedCountRange.Min);

            if (copyModels)
            {
                PedModels = new uint[pedModels.Length];
                Array.Copy(pedModels, PedModels, pedModels.Length);
            }
            else
            {
                PedModels = pedModels;
            }

            PedNodeFlags = pedNodeFlags;
            Debug.WriteLine($"new {nameof(CruisegoerSpawnCluster)}: {nameof(pedNodeFlags)} = {pedNodeFlags}");
        }

        internal CruisegoerSpawnCluster(float x, float y, float z, byte maxPedCount, uint pedNodeFlags, float clusterRadius) : this(x, y, z, maxPedCount, pedNodeFlags, clusterRadius, Constants.CruisegoerSpawns.PedModels, false)
        {

        }

        internal CruisegoerSpawnCluster(float x, float y, float z, Range<byte> pedCountRange, uint pedNodeFlags, float clusterRadius) : this(x, y, z, pedCountRange, pedNodeFlags, clusterRadius, Constants.CruisegoerSpawns.PedModels, false)
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
