using CitizenFX.Core;
using SurviveTheHuntClient.Models.Utils;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal partial class Constants
    {
        internal static class CruisegoerSpawns
        {
            internal readonly static uint[] PedModels =
            {
                (uint)PedHash.Beach01AFM,
                (uint)PedHash.Beach01AFY,
                (uint)PedHash.Beachvesp01AMY,
                (uint)PedHash.Party01,
                (uint)PedHash.Baywatch01SFY,
                (uint)PedHash.Vinewood01AFY,
                (uint)PedHash.StripperLite,
            };

            internal static readonly CruisegoerSpawnBase[] Required =
            {
                // Brendan the Arsey
                new CruisegoerSpawnSingle(-2085.347f, -1017.773f, 12.7819f, 69.39364f, (uint)GetHashKey("mp_m_boatstaff_01")),
                
                // Bartenders
                new CruisegoerSpawnSingle(-2095.156f, -1015.901f, 8.98045f, 211.208f, (uint)GetHashKey("mp_f_boatstaff_01"), (uint)GetHashKey("s_f_y_casino_01"), (uint)GetHashKey("u_f_m_casinocash_01")),
                new CruisegoerSpawnSingle(-2094.295f, -1014.135f, 8.98045f, 284.7771f, (uint)GetHashKey("mp_f_boatstaff_01"), (uint)GetHashKey("s_f_y_casino_01"), (uint)GetHashKey("u_f_m_casinocash_01")),
            };

            internal static readonly CruisegoerSpawnBase[] Optional =
            {
                // looking overboard
                new CruisegoerSpawnLine
                (
                    -2077.613f, -1027.841f, 5.883708f,
                    -2041.367f, -1039.751f, 5.883331f,
                    165.7485f, 360f,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2029.636f, -1028.991f, 5.882014f,
                    -2076.386f, -1013.412f, 5.882023f,
                    340f, 180f,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2081.217f, -1026.638f, 8.971484f,
                    -2048.382f, -1037.368f, 8.971483f,
                    140.6088f, 90f,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2054.378f, -1021.386f, 11.90755f,
                    -2071.664f, -1015.628f, 11.90736f,
                    341.6285f, 45f,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2074.365f, -1028.586f, 11.90736f,
                    -2055.322f, -1034.304f, 11.90758f,
                    165.3188f, 30f,
                    new Range<byte>(3, 5), PedModels
                ),

                // interior clusters
                new CruisegoerSpawnCluster(-2079.552f, -1019.935f, 8.971482f, 5, 2.75f),
                new CruisegoerSpawnCluster(-2068.606f, -1023.439f, 11.90994f, 4, 4f),
                new CruisegoerSpawnCluster(-2043.808f, -1026.549f, 8.971483f, 3, 3f),
                new CruisegoerSpawnCluster(-2052.182f, -1029.052f, 8.971498f, 4, 4.5f),
                new CruisegoerSpawnCluster(-2053.477f, -1028.463f, 11.90758f, 3, 2f),
                new CruisegoerSpawnCluster(-2038.406f, -1032.982f, 8.971497f, 3, 2.5f),
                new CruisegoerSpawnCluster(-2089.762f, -1016.639f, 8.971189f, 5, 3.2f),
                new CruisegoerSpawnLine
                (
                    -2091.906f, -1021.52f, 5.907803f,
                    -2100.738f, -1017.874f, 5.88418f,
                    67f, 30f,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2082.918f, -1022.214f, 5.884103f,
                    -2087.266f, -1024.247f, 5.882385f,
                    200f, 360f,
                    new Range<byte>(3, 5), PedModels
                ),

                // showers
                new CruisegoerSpawnSingle(-2080.376f, -1020.604f, 5.875937f, 117.4093f, (uint)PedHash.Topless01AFY, (uint)PedHash.Musclbeac01AMY),
                new CruisegoerSpawnSingle(-2092.41f, -1018.078f, 5.888787f, 119.4851f, (uint)PedHash.Topless01AFY, (uint)PedHash.Musclbeac02AMY),
                new CruisegoerSpawnSingle(-2100.895f, -1008.169f, 5.878305f, 107.131f, (uint)PedHash.Topless01AFY, (uint)PedHash.Musclbeac01AMY),
            };
        }
    }
}
