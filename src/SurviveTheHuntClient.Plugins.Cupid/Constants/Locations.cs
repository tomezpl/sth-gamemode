using CitizenFX.Core;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using System.Collections.Generic;

namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal static partial class Constants
    {
        internal static class Location
        {
            internal static class RobberyJob
            {
                internal static Vector3 FleecaLegion => new Vector3(152.26f, -1034.67f, 29.15f);

                internal static Vector3 FleecaRockford => new Vector3(-1216.75f, -322.44f, 36.3f);
                internal static Vector3 FleecaRockfordObjective => new Vector3(-1210.809f, -336.4966f, 36.38103f);

                internal static Vector3 FleecaChumash => new Vector3(-2967.11f, 483f, 14.08f);
            }

            internal static class CarRobberyJob
            {
                internal static Vector3 Elysian = new Vector3(636.07f, -2661.24f, 48.73f);
                internal const float ElysianHeading = -79.67f;

                internal static Vector3 DelPerro = new Vector3(-756.39f, -503.48f, 27.32f);
                internal const float DelPerroHeading = 93.84f;

                internal const float DeliveryPosX = 1169.435302734375f, DeliveryPosY = -2970.55078125f, DeliveryPosZ = 5.15926456451416f;
                internal static Vector3 DeliveryPos = new Vector3(DeliveryPosX, DeliveryPosY, DeliveryPosZ);
                internal const float DeliveryRadius = 9f;
            }

            internal static class CopSpawn
            {
                internal static readonly CopSpawnInfo Vinewood = new CopSpawnInfo
                (
                    camera: new CopSpawnInfo.CameraInfo(pos: new Vector3(703.8f, 3.36f, 441f), rot: new Vector3(-70.73f, 0.001f, 14.6621f)),
                    spawn: new CopSpawnInfo.SpawnPosInfo(pos: new Vector3(619.8f, 19.56f, 88.07f), heading: 343.47f)
                );

                internal static readonly CopSpawnInfo Sandy = new CopSpawnInfo
                (
                    camera: new CopSpawnInfo.CameraInfo(pos: new Vector3(2107.97f, 3449.28f, 577f), rot: new Vector3(-68.274f, 0f, 49.961826f)),
                    spawn: new CopSpawnInfo.SpawnPosInfo(pos: new Vector3(1852.732666015625f, 3686.292724609375f, 34.69072341918945f), heading: -140f)
                );

                internal static readonly CopSpawnInfo Paleto = new CopSpawnInfo
                (
                    camera: new CopSpawnInfo.CameraInfo(pos: new Vector3(336.426f, 5588.21f, 1499f), rot: new Vector3(-80.15f, 0f, 66.356f)),
                    spawn: new CopSpawnInfo.SpawnPosInfo(pos: new Vector3(-444.42828369140625f, 6015.4248046875f, 31.74029541015625f), heading: -45f)
                );

                private static CopSpawnInfo[] _all = new CopSpawnInfo[]
                {
                    Vinewood,
                    Sandy,
                    Paleto,
                };

                internal static IEnumerable<CopSpawnInfo> All => _all;

                private static CopSpawnInfo Initial => Paleto;

                internal static byte Count => (byte)_all.Length;

                internal static CopSpawnInfo FindNearest(Vector3 pos)
                {
                    float shortestDistanceSq = float.MaxValue;
                    CopSpawnInfo closest = null;
                    foreach(CopSpawnInfo copSpawn in _all)
                    {
                        float a = pos.X - closest.Spawn.Pos.X;
                        float b = pos.Y - closest.Spawn.Pos.Y;
                        float distanceSq = a * a + b * b;

                        if(closest == null || distanceSq < shortestDistanceSq)
                        {
                            closest = copSpawn;
                            shortestDistanceSq = distanceSq;
                        }
                    }

                    return closest;
                }

                internal static CopSpawnInfo FromIndex(byte index)
                {
                    return _all[index];
                }

                internal static sbyte FindIndex(CopSpawnInfo spawn)
                {
                    for(sbyte i = 0; i < (sbyte)_all.Length; i++)
                    {
                        if (_all[i] == spawn)
                        {
                            return i;
                        }
                    }

                    return -1;
                }
            }
        }
    }
}
