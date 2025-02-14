using static CitizenFX.Core.Native.API;


namespace SurviveTheHuntClient.Helpers
{
    public static class WantedLevelHelper
    {
        public static void DisableWantedLevel()
        {
            SetMaxWantedLevel(0);
        }
    }
}
