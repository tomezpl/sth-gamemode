using CitizenFX.Core;
using System;
using static CitizenFX.Core.Native.API;
using SharedConstants = SurviveTheHuntShared.Constants;

namespace SurviveTheHuntClient.Helpers
{
    /// <summary>
    /// Helper class that runs logic for checking the local player's proximity to play area limits
    /// and displaying the appropriate UI elements to help them avoid detection.
    /// </summary>
    internal static class BoundsTracker
    {
        private static int? PlayAreaBlip = null;
        private static bool WasApproachingBoundsLastTick = false;
        private const int ApproachingOutOfBoundsNotificationDuration = 20;
        private static float TimePassedSinceLastOOBNotification = 0;

        internal static void Init()
        {
            const float yOffset = 0f;
            PlayAreaBlip = AddBlipForArea(-100f , (SharedConstants.DockSpawn.Y + SharedConstants.OutOfBoundsYLimit) * .5f + yOffset, SharedConstants.DockSpawn.Z, 4250f, Math.Abs(SharedConstants.OutOfBoundsYLimit - SharedConstants.DockSpawn.Y) + Math.Abs(yOffset) * .5f);
            SetBlipDisplay(PlayAreaBlip.Value, 0);
            uint colour = 0xFFA83366;
            SetBlipColour(PlayAreaBlip.Value, (int)colour);
        }

        internal static void Tick()
        {
            bool approachingBounds = CheckIsApproachingBounds();
            if (approachingBounds != WasApproachingBoundsLastTick && PlayAreaBlip.HasValue)
            {
                SetBlipDisplay(PlayAreaBlip.Value, approachingBounds ? 10 : 0);

                if (approachingBounds && TimePassedSinceLastOOBNotification == 0)
                {
                    const string warningText = "ApproachingOutOfBoundsNotification";
                    AddTextEntry(warningText, "You are approaching the ~o~play area~w~ limits. If you leave the area marked in orange, you will appear on the hunters' radar.~n~~n~Try make your way back further into the ~o~play area~w~ to avoid detection.");
                    BeginTextCommandDisplayHelp(warningText);
                    EndTextCommandDisplayHelp(0, false, true, ApproachingOutOfBoundsNotificationDuration * 1000);

                    TimePassedSinceLastOOBNotification = GetFrameTime();
                }
            }

            // Debounce the notification
            if(TimePassedSinceLastOOBNotification != 0)
            {
                if (TimePassedSinceLastOOBNotification >= ApproachingOutOfBoundsNotificationDuration)
                {
                    TimePassedSinceLastOOBNotification = 0;
                }
                else
                {
                    TimePassedSinceLastOOBNotification += GetFrameTime();
                }
            }

            SetBlipRotation(PlayAreaBlip.Value, 0);

            WasApproachingBoundsLastTick = approachingBounds;
        }

        private static bool CheckIsApproachingBounds()
        {
            // Distance inside the bounds at which the player should be notified.
            const float margin = 250f;

            int localPlayerPed = PlayerPedId();
            if (DoesEntityExist(localPlayerPed))
            {
                // TODO: when we finally get to implementing #55 (configurable play areas),
                // this will not work because it only does a 1D check against the player's Y-coord. But that's good enough for now.
                Vector3 pos = GetEntityCoords(localPlayerPed, false);
                if(pos.Y >= SharedConstants.OutOfBoundsYLimit - margin)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
