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
            internal readonly static uint[] MalePedModels =
            {
                (uint)GetHashKey("a_m_y_beach_01"),
                (uint)GetHashKey("a_m_y_beach_02"),
                (uint)GetHashKey("a_m_y_beach_03"),
                (uint)GetHashKey("a_m_y_gencaspat_01"),
                (uint)GetHashKey("u_m_y_caleb"),
                (uint)GetHashKey("u_m_y_gabriel"),
                (uint)GetHashKey("u_m_y_ushi"),
                (uint)GetHashKey("u_m_m_vince"),
                (uint)GetHashKey("a_m_y_clubcust_01"),
                (uint)GetHashKey("a_m_y_clubcust_02"),
                (uint)GetHashKey("a_m_y_clubcust_03"),
                (uint)GetHashKey("a_m_y_clubcust_04"),
                (uint)GetHashKey("a_m_y_beach_04"),
                (uint)GetHashKey("a_m_y_beach_04"),
                (uint)GetHashKey("a_m_y_carclub_01"),
                (uint)GetHashKey("a_m_y_studioparty_01"),
                (uint)GetHashKey("ig_moodyman_02"),
                (uint)GetHashKey("ig_billionaire"),
                (uint)GetHashKey("ig_party_promo"),
                (uint)GetHashKey("s_m_y_clubbar_01"),
                (uint)GetHashKey("u_m_y_danceburl_01"),
                (uint)GetHashKey("u_m_y_dancelthr_01"),
                (uint)GetHashKey("u_m_y_dancerave_01"),
                (uint)GetHashKey("a_m_m_genbiker_01"),
                (uint)GetHashKey("s_m_m_studioassist_02"),
                (uint)GetHashKey("ig_security_a"),
                (uint)GetHashKey("mp_m_execpa_01"),
                (uint)GetHashKey("g_m_importexport_01"),
                (uint)GetHashKey("a_m_y_tattoocust_01"),
                (uint)PedHash.Party01,
            };

            internal readonly static uint[] FemalePedModels =
            {
                (uint)PedHash.Baywatch01SFY,
                (uint)PedHash.Vinewood01AFY,
                (uint)PedHash.StripperLite,
                (uint)PedHash.Beach01AFM,
                (uint)PedHash.Beach01AFY,
                (uint)GetHashKey("a_f_y_gencaspat_01"),
                (uint)GetHashKey("u_f_y_lauren"),
                (uint)GetHashKey("u_f_y_taylor"),
                (uint)GetHashKey("u_f_y_beth"),
                (uint)GetHashKey("a_f_y_clubcust_01"),
                (uint)GetHashKey("a_f_y_clubcust_02"),
                (uint)GetHashKey("a_f_y_clubcust_03"),
                (uint)GetHashKey("a_f_y_clubcust_04"),
                (uint)GetHashKey("a_f_y_beach_02"),
                (uint)GetHashKey("a_f_y_beach_02"),
                (uint)GetHashKey("s_f_m_retailstaff_01"),
                (uint)GetHashKey("a_f_y_carclub_01"),
                (uint)GetHashKey("a_f_y_studioparty_01"),
                (uint)GetHashKey("a_f_y_studioparty_02"),
                (uint)GetHashKey("s_f_y_clubbar_01"),
                (uint)GetHashKey("u_f_y_poppymich_02"),
                (uint)GetHashKey("u_f_y_danceburl_01"),
                (uint)GetHashKey("u_f_y_dancelthr_01"),
                (uint)GetHashKey("u_f_y_dancerave_01"),
                (uint)GetHashKey("ig_entourage_a"),
                (uint)GetHashKey("ig_entourage_b"),
                (uint)GetHashKey("ig_agent_02"),
                (uint)GetHashKey("a_f_m_genbiker_01"),
                (uint)GetHashKey("ig_soundeng_00"),
                (uint)GetHashKey("s_f_m_studioassist_01"),
                (uint)GetHashKey("mp_f_execpa_01"),
                (uint)GetHashKey("g_f_importexport_01"),
                (uint)GetHashKey("mp_f_cardesign_01"),
                (uint)GetHashKey("mp_f_execpa_02"),
                (uint)GetHashKey("a_f_y_bevhills_05"),
                (uint)GetHashKey("ig_wendy"),
                (uint)GetHashKey("ig_paige"),
                (uint)GetHashKey("a_f_y_femaleagent"),
            };

            private static uint[] _cachedPedModels = null;
            internal static uint[] PedModels
            {
                get
                {
                    if(_cachedPedModels == null)
                    {
                        _cachedPedModels = new uint[FemalePedModels.Length + MalePedModels.Length];
                        FemalePedModels.CopyTo(_cachedPedModels, 0);
                        MalePedModels.CopyTo(_cachedPedModels, FemalePedModels.Length);
                    }

                    return _cachedPedModels;
                }
            }

            internal static readonly CruisegoerSpawnBase[] Required =
            {
                // Brendan the Arsey
                new CruisegoerSpawnSingle(-2085.347f, -1017.773f, 12.7819f, 69.39364f, 0, (uint)GetHashKey("mp_m_boatstaff_01")),
                
                // Bartenders
                new CruisegoerSpawnSingle(-2095.156f, -1015.901f, 8.98045f, 211.208f, 0, (uint)GetHashKey("mp_f_boatstaff_01"), (uint)GetHashKey("s_f_y_casino_01"), (uint)GetHashKey("u_f_m_casinocash_01"), (uint)GetHashKey("s_f_y_beachbarstaff_01"), (uint)GetHashKey("s_f_y_clubbar_02")),
                new CruisegoerSpawnSingle(-2094.295f, -1014.135f, 8.98045f, 284.7771f, 0, (uint)GetHashKey("mp_f_boatstaff_01"), (uint)GetHashKey("s_f_y_casino_01"), (uint)GetHashKey("u_f_m_casinocash_01"), (uint)GetHashKey("s_f_y_beachbarstaff_01"), (uint)GetHashKey("s_f_y_clubbar_02")),
            };

            internal static uint GenericPedNodeFlags => PedNode.SetFlags(PedNode.PedNodeFlag.RandomScenario, PedNode.PedNodeFlag.RandomAnim);

            internal static readonly CruisegoerSpawnBase[] Optional =
            {
                // looking overboard
                new CruisegoerSpawnLine
                (
                    -2077.613f, -1027.841f, 5.883708f,
                    -2041.367f, -1039.751f, 5.883331f,
                    165.7485f, 360f,
                    GenericPedNodeFlags,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2029.636f, -1028.991f, 5.882014f,
                    -2076.386f, -1013.412f, 5.882023f,
                    340f, 180f,
                    GenericPedNodeFlags,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2081.217f, -1026.638f, 8.971484f,
                    -2048.382f, -1037.368f, 8.971483f,
                    140.6088f, 90f,
                    GenericPedNodeFlags,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2054.378f, -1021.386f, 11.90755f,
                    -2071.664f, -1015.628f, 11.90736f,
                    341.6285f, 45f,
                    GenericPedNodeFlags,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2074.365f, -1028.586f, 11.90736f,
                    -2055.322f, -1034.304f, 11.90758f,
                    165.3188f, 30f,
                    GenericPedNodeFlags,
                    new Range<byte>(3, 5), PedModels
                ),

                // interior clusters
                new CruisegoerSpawnCluster(-2079.552f, -1019.935f, 8.971482f, 5, GenericPedNodeFlags, 2.75f),
                new CruisegoerSpawnCluster(-2068.606f, -1023.439f, 11.90994f, 4, GenericPedNodeFlags, 4f),
                new CruisegoerSpawnCluster(-2043.808f, -1026.549f, 8.971483f, 3, GenericPedNodeFlags, 3f),
                new CruisegoerSpawnCluster(-2052.182f, -1029.052f, 8.971498f, 4, GenericPedNodeFlags, 4.5f),
                new CruisegoerSpawnCluster(-2053.477f, -1028.463f, 11.90758f, 3, GenericPedNodeFlags, 2f),
                new CruisegoerSpawnCluster(-2038.406f, -1032.982f, 8.971497f, 3, GenericPedNodeFlags, 2.5f),
                new CruisegoerSpawnCluster(-2089.762f, -1016.639f, 8.971189f, 5, GenericPedNodeFlags, 3.2f),
                new CruisegoerSpawnLine
                (
                    -2091.906f, -1021.52f, 5.907803f,
                    -2100.738f, -1017.874f, 5.88418f,
                    67f, 30f,
                    GenericPedNodeFlags,
                    new Range<byte>(3, 5), PedModels
                ),
                new CruisegoerSpawnLine
                (
                    -2082.918f, -1022.214f, 5.884103f,
                    -2087.266f, -1024.247f, 5.882385f,
                    200f, 360f,
                    GenericPedNodeFlags,
                    new Range<byte>(3, 5), PedModels
                ),

                // showers
                new CruisegoerSpawnSingle(-2080.376f, -1020.604f, 5.875937f, 117.4093f, PedNode.SetFlags(PedNode.PedNodeFlag.Shower, PedNode.PedNodeFlag.NeedsWarp), (uint)PedHash.Topless01AFY, (uint)PedHash.Musclbeac01AMY),
                new CruisegoerSpawnSingle(-2092.41f, -1018.078f, 5.888787f, 119.4851f, PedNode.SetFlags(PedNode.PedNodeFlag.Shower, PedNode.PedNodeFlag.NeedsWarp), (uint)PedHash.Topless01AFY, (uint)PedHash.Musclbeac02AMY),
                new CruisegoerSpawnSingle(-2100.895f, -1008.169f, 5.878305f, 107.131f, PedNode.SetFlags(PedNode.PedNodeFlag.Shower, PedNode.PedNodeFlag.NeedsWarp), (uint)PedHash.Topless01AFY, (uint)PedHash.Musclbeac01AMY),
            };
        }
    }
}
