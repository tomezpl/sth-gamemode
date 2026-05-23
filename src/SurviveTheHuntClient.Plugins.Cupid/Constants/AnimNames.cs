using SurviveTheHuntClient.Plugins.Cupid.Models;

namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal partial class Constants
    {
        internal static class AnimNames
        {
            internal sealed class ShowerAnim : GenderedAnimBase
            {
                private readonly static PedNode.AnimInfo s_Female = new PedNode.AnimInfo("mp_safehouseshower@female@", "shower_idle_a");
                private readonly static PedNode.AnimInfo s_Male = new PedNode.AnimInfo("mp_safehouseshower@male@", "male_shower_idle_a");

                internal override PedNode.AnimInfo Female => s_Female;
                internal override PedNode.AnimInfo Male => s_Male;
            }

            internal static readonly ShowerAnim Shower = new ShowerAnim();

            internal sealed class DanceAnim : GenderedAnimBase
            {
                private static readonly PedNode.AnimInfo s_Female = new PedNode.AnimInfo("anim@amb@nightclub@dancers@podium_dancers@", "hi_dance_facedj_17_v2_female^2");
                internal override PedNode.AnimInfo Female => s_Female;

                private static readonly PedNode.AnimInfo s_Male = new PedNode.AnimInfo("anim@amb@nightclub@dancers@podium_dancers@", "hi_dance_facedj_17_v2_male^5");
                internal override PedNode.AnimInfo Male => s_Male;
            }

            internal static readonly DanceAnim Dance = new DanceAnim();

            internal static readonly GenderedAnimBase[] All =
            {
                Dance,
            };

            internal abstract class GenderedAnimBase
            {
                internal abstract PedNode.AnimInfo Female { get; }
                internal abstract PedNode.AnimInfo Male { get; }
                internal PedNode.AnimInfo Get(bool isMale)
                {
                    return isMale ? Male : Female;
                }
            }
        }
    }
}
