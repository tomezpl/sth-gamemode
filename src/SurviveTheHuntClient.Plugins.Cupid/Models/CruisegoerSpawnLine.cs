using SurviveTheHuntClient.Models.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal sealed class CruisegoerSpawnLine : CruisegoerSpawnBase
    {
        internal override SpawnShrinkStrategy ShrinkStrategy => SpawnShrinkStrategy.Sampled;

        internal const float PedSafeRadius = 0.25f;

        internal const float PedDistanceError = 0.5f;

        internal readonly float StartX, StartY, StartZ, EndX, EndY, EndZ;
        internal readonly float DirX, DirY, DirZ;

        internal readonly float Length;

        internal readonly byte DesiredMinPedCount;
        internal readonly byte DesiredMaxPedCount;

        internal readonly uint[] PedModels;

        internal readonly float MaxHeadingError;

        internal CruisegoerSpawnLine(float startX, float startY, float startZ, float endX, float endY, float endZ, float heading, float maxHeadingChange, Range<byte> pedCountRange, uint[] pedModels)
            : base(startX + (endX - startX) * .5f, startY + (endY - startY) * .5f, startZ + (endZ - startZ) * .5f, heading)
        {
            StartX = startX;
            StartY = startY;
            StartZ = startZ;
            EndX = endX;
            EndY = endY;
            EndZ = endZ;

            Length = (float)Math.Sqrt(Math.Pow(startX - endX, 2) + Math.Pow(startY - endY, 2) + Math.Pow(startZ - endZ, 2));
            DirX = (endX - startX) / Length;
            DirY = (endY - startY) / Length;
            DirZ = (endZ - startZ) / Length;

            DesiredMaxPedCount = pedCountRange.Max;
            DesiredMinPedCount = pedCountRange.Min;
            PedModels = pedModels;

            if(pedModels.Length == 0)
            {
                throw new ArgumentException("At least one ped model is required", nameof(pedModels));
            }

            MaxHeadingError = maxHeadingChange;
        }

        internal CruisegoerSpawnLine(float startX, float startY, float startZ, float endX, float endY, float endZ, float heading, float maxHeadingChange, byte pedCount, uint[] pedModels) : this(startX, startY, startZ, endX, endY, endZ, heading, maxHeadingChange, new Range<byte>(pedCount), pedModels)
        {

        }

        internal static byte GetMaxPedCount(byte desired, float length)
        {
            return Math.Min(desired, (byte)Math.Floor(length / PedSafeRadius));
        }

        internal override PedSpawnInfo[] Build()
        {
            byte desiredPedCount = (byte)s_RNG.Next(DesiredMinPedCount, DesiredMaxPedCount + 1);

            PedSpawnInfo[] info = new PedSpawnInfo[GetMaxPedCount(desiredPedCount, Length)];

            float errorHalfRange = PedDistanceError * .5f;
            float step = Length / (info.Length + 1f);

            List<uint> modelQueue = new List<uint>(PedModels);

            for(byte i = 0; i < info.Length; i++)
            {
                if(modelQueue.Count == 0)
                {
                    modelQueue.AddRange(PedModels);
                }

                int modelIndex = s_RNG.Next(0, modelQueue.Count);
                uint modelToUse = modelQueue[modelIndex];
                modelQueue.RemoveAt(modelIndex);

                float randomError = errorHalfRange * (float)((s_RNG.NextDouble() * 2f) - 1f);
                float distanceAlongLine = step * i + randomError;
                float currentX = StartX + DirX * distanceAlongLine;
                float currentY = StartY + DirY * distanceAlongLine;
                float currentZ = StartZ + DirZ * distanceAlongLine;

                info[i] = new PedSpawnInfo
                {
                    PedModel = modelToUse,
                    Anim = new PedSpawnInfo.AnimInfo("", ""),
                    Position = new PedSpawnInfo.PositionInfo(currentX, currentY, currentZ, Heading + MaxHeadingError * (float)(s_RNG.NextDouble() - 0.5f)),
                };
            }

            return info;
        }
    }
}
