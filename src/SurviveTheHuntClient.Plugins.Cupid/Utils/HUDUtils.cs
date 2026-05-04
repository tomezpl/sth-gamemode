using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Utils
{
    internal static class HUDUtils
    {
        internal static void ClearObjective()
        {
            BeginTextCommandPrint("STRING");
            AddTextComponentString(" ");
            EndTextCommandPrint(0, true);
        }
    }
}
