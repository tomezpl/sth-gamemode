using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    // TODO: yes yes this is duplicated code i don't care
    internal static class MinimapHelper
    {
        internal const float MaxRadarDistanceMinimap = 750f;
        internal const float MaxRadarDistanceBigmap = 1050f;
        private const float _maxRadarDistanceMinimapSqr = MaxRadarDistanceMinimap * MaxRadarDistanceMinimap;
        private const float _maxRadarDistanceBigmapSqr = MaxRadarDistanceBigmap * MaxRadarDistanceBigmap;

        /// <summary>
        /// Checks whether the coord is visible in the minimap right now
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="playerPos"></param>
        /// <returns></returns>
        /// <remarks>TODO: this could work better if we took rotation into account, as the minimap's origin is shifted down slightly, but this works well enough</remarks>
        public static bool IsCoordInRadarBounds(float x, float y, float playerX, float playerY, bool allowPauseMenu = false)
        {
            if (IsPauseMenuActive())
            {
                return !allowPauseMenu;
            }

            float maxRadarDistanceSqr = IsBigmapActive() ? _maxRadarDistanceBigmapSqr : _maxRadarDistanceMinimapSqr;

            float a = x - playerX;
            a *= a;
            float b = y - playerY;
            b *= b;

            return (a + b) <= maxRadarDistanceSqr;
        }
    }
}
