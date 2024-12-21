using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    public static class MinimapHelper
    {
        /// <summary>
        /// Checks whether the coord is visible in the minimap right now
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="playerPos"></param>
        /// <returns></returns>
        /// <remarks>TODO: this could work better if we took rotation into account, as the minimap's origin is shifted down slightly, but this works well enough</remarks>
        public static bool IsCoordInRadarBounds(Vector3 coord, Vector3? playerPos = null)
        {
            if(IsPauseMenuActive())
            {
                return true;
            }

            float x = coord.X;
            float y = coord.Y;
            float z = coord.Z;

            if (playerPos == null)
            {
                if (IsPlayerPlaying(PlayerId()) && DoesEntityExist(PlayerPedId()))
                {
                    playerPos = GetEntityCoords(PlayerPedId(), false);
                }
                else
                {
                    return false;
                }
            }

            float maxRadarDistanceSqr = IsBigmapActive() ? 750f : 350f;
            maxRadarDistanceSqr *= maxRadarDistanceSqr;

            float a = x - playerPos.Value.X;
            a *= a;
            float b = y - playerPos.Value.Y;
            b *= b;
            float c = z - playerPos.Value.Z;

            return (a + b + c) <= maxRadarDistanceSqr;
        }
    }
}
