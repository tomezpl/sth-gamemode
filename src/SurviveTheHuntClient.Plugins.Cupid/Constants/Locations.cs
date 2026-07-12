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
                private static Vector3 DavisCarOrigin => new Vector3(410.331f, -1657.14f, 29.29194f);
                internal static readonly CopSpawnInfo Davis = new CopSpawnInfo
                (
                    "davis",
                    camera: new CopSpawnInfo.CameraInfo(pos: new Vector3(488.38f, -1664.34f, 435.65f), rot: new Vector3(-75.04f, -3.873f, 46.93f)),
                    spawn: new CopSpawnInfo.SpawnPosInfo(pos: new Vector3(376.9f, -1613.02f, 29.506f), heading: -119.54f),
                    carLayout: new CopSpawnInfo.CarPosInfo(origin: DavisCarOrigin, step: new Vector2(407.9486f, -1654.812f) - (Vector2)DavisCarOrigin, end: new Vector3(396.1904f, -1644.51f, 29.29192f), heading: 317f),
                    copCarModels: new uint[]
                    {
                        (uint)VehicleHash.Police,
                        (uint)VehicleHash.Police2,
                        (uint)VehicleHash.Police3,
                        (uint)VehicleHash.Police4
                    }
                );

                private static Vector3 VespucciCarOrigin => new Vector3(-1127.614f, -816.4182f, 15.985f);
                internal static readonly CopSpawnInfo Vespucci = new CopSpawnInfo
                (
                    "vespucci",
                    camera: new CopSpawnInfo.CameraInfo(pos: new Vector3(-1171.87f, -1018.1790f, 625.205f), rot: new Vector3(-74.137f, -3.871425390f, -23.05166f)),
                    spawn: new CopSpawnInfo.SpawnPosInfo(pos: new Vector3(-1092.609f, -806.330f, 19.07261f), heading: 34.363197f),
                    carLayout: new CopSpawnInfo.CarPosInfo(origin: VespucciCarOrigin, step: new Vector2(-1124.537f, -813.57f) - (Vector2)VespucciCarOrigin, end: new Vector3(-1108.424f, -799.98f, 18.055f), heading: 240f)
                );

                private static Vector3 VinewoodCarOrigin => new Vector3(581.382f, 39.2494f, 92.52736f);
                internal static readonly CopSpawnInfo Vinewood = new CopSpawnInfo
                (
                    "vinewood",
                    camera: new CopSpawnInfo.CameraInfo(pos: new Vector3(703.8f, 3.36f, 441f), rot: new Vector3(-70.73f, 0.001f, 14.6621f)),
                    spawn: new CopSpawnInfo.SpawnPosInfo(pos: new Vector3(619.8f, 19.56f, 88.07f), heading: 343.47f),
                    carLayout: new CopSpawnInfo.CarPosInfo(origin: VinewoodCarOrigin, step: new Vector2(586.4487f, 38.63542f) - (Vector2)VinewoodCarOrigin, end: new Vector3(627.0581f, 25.08042f, 87.82987f), 191f),
                    copCarModels: new uint[]
                    {
                        (uint)VehicleHash.Police,
                        (uint)VehicleHash.Police2,
                        (uint)VehicleHash.Police3,
                        (uint)VehicleHash.FBI
                    }
                );

                private static Vector3 SandyCarOrigin => new Vector3(1869.508f, 3682.058f, 33.65567f);
                internal static readonly CopSpawnInfo Sandy = new CopSpawnInfo
                (
                    "sandy",
                    camera: new CopSpawnInfo.CameraInfo(pos: new Vector3(2107.97f, 3449.28f, 577f), rot: new Vector3(-68.274f, 0f, 49.961826f)),
                    spawn: new CopSpawnInfo.SpawnPosInfo(pos: new Vector3(1852.732666015625f, 3686.292724609375f, 34.69072341918945f), heading: -140f),
                    carLayout: new CopSpawnInfo.CarPosInfo(origin: SandyCarOrigin, step: new Vector2(1862.668f, 3678.616f) - (Vector2)SandyCarOrigin, end: new Vector3(1847.775f, 3670.368f, 33.69377f), 170f),
                    copCarModels: new uint[]
                    {
                        (uint)VehicleHash.Sheriff,
                        (uint)VehicleHash.Sheriff2,
                        (uint)VehicleHash.Policeb,
                    }
                );

                private static Vector3 PaletoCarOrigin => new Vector3(-469.0205f, 6038.758f, 31.34055f);
                internal static readonly CopSpawnInfo Paleto = new CopSpawnInfo
                (
                    "paleto",
                    camera: new CopSpawnInfo.CameraInfo(pos: new Vector3(336.426f, 5588.21f, 1499f), rot: new Vector3(-80.15f, 0f, 66.356f)),
                    spawn: new CopSpawnInfo.SpawnPosInfo(pos: new Vector3(-444.42828369140625f, 6015.4248046875f, 31.74029541015625f), heading: -45f),
                    carLayout: new CopSpawnInfo.CarPosInfo(origin: PaletoCarOrigin, step: new Vector2(-472.7902f, 6035.144f) - (Vector2)PaletoCarOrigin, end: new Vector3(-483.4076f, 6025.106f, 31.34056f), 224f),
                    copCarModels: new uint[]
                    {
                        //(uint)VehicleHash.Sheriff,
                        //(uint)VehicleHash.Sheriff2,
                        (uint)VehicleHash.Policeb,
                        //(uint)VehicleHash.Pranger,
                    }
                );

                private static CopSpawnInfo[] _all = new CopSpawnInfo[]
                {
                    Davis,
                    Vespucci,
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
                        float a = pos.X - copSpawn.Spawn.Pos.X;
                        float b = pos.Y - copSpawn.Spawn.Pos.Y;
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

                internal static CopSpawnInfo FromName(string name)
                {
                    foreach(CopSpawnInfo info in _all)
                    {
                        if(info.Name == name)
                        {
                            return info;
                        }
                    }
                    return null;
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

            internal static class PartyClothesMarkers
            {
                private static Vector3[] _all => new Vector3[]
                {
                    new Vector3(117.5785f, -234.2292f, 54.55787f),
                    new Vector3(-166.9188f, -301.3444f, 39.73333f),
                    new Vector3(-706.3417f, -151.2523f, 37.41519f),
                    new Vector3(-1180.578f, -763.7936f, 17.32644f),
                    new Vector3(-3179.502f, 1033.954f, 20.86321f),
                    new Vector3(617.4792f, 2775.442f, 42.0881f)
                };

                private static Vector3[] _cached = null;
                internal static Vector3[] All
                {
                    get
                    {
                        if(_cached == null)
                        {
                            _cached = _all;
                        }

                        return _cached;
                    }
                }

                internal const float ZOffset = -0.85f;
            }
        }
    }
}
