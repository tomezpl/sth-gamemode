using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using System;
using static CitizenFX.Core.Native.API;
using SharedConstants = SurviveTheHuntShared.Constants;

namespace SurviveTheHuntClient.Helpers
{
    /// <summary>
    /// Helper class that runs logic for checking the local player's proximity to play area limits
    /// and displaying the appropriate UI elements to help them avoid detection.
    /// </summary>
    internal class BoundsTracker : ITickable
    {
        private int? PlayAreaBlip = null;
        private bool WasApproachingBoundsLastTick = false;
        private const int ApproachingOutOfBoundsNotificationDuration = 20;
        private float TimePassedSinceLastOOBNotification = 0;

        private float _oobYLimit = SharedConstants.OutOfBoundsYLimit;

        internal float OutOfBoundsYLimit
        {
            get => _oobYLimit;
            set
            {
                if (PlayAreaBlip != null && value != _oobYLimit)
                {
                    int playAreaBlip = PlayAreaBlip.Value;
                    RemoveBlip(ref playAreaBlip);
                    PlayAreaBlip = null;
                }
                
                _oobYLimit = value;
                
                if (PlayAreaBlip == null)
                {
                    PlayAreaBlip = CreatePlayAreaBlip(value);
                }
            }
        }

        private static int CreatePlayAreaBlip(float outOfBoundsYLimit = SharedConstants.OutOfBoundsYLimit)
        {
            const float yOffset = 0f;
            float height = Math.Abs(outOfBoundsYLimit - SharedConstants.OutOfBoundsYMin) + Math.Abs(yOffset) * .5f;
            int blip = AddBlipForArea(-100f, outOfBoundsYLimit - (height * .5f), SharedConstants.DockSpawn.Z, 4250f, height);

            SetBlipDisplay(blip, 0);
            uint colour = 0xFFA83366;
            SetBlipColour(blip, (int)colour);

            return blip;
        }

        internal void Init(float outOfBoundsYLimit = SharedConstants.OutOfBoundsYLimit)
        {
            _oobYLimit = outOfBoundsYLimit;

            PlayAreaBlip = CreatePlayAreaBlip(outOfBoundsYLimit);
        }

        public void Tick(float deltaTime)
        {
            bool approachingBounds = CheckIsApproachingBounds(OutOfBoundsYLimit);
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

        private static bool CheckIsApproachingBounds(float outOfBoundsYLimit = SharedConstants.OutOfBoundsYLimit)
        {
            int localPlayerPed = PlayerPedId();
            if (DoesEntityExist(localPlayerPed))
            {
                return CheckIsApproachingBounds(localPlayerPed, outOfBoundsYLimit) == BoundsTestResult.NearEdge;
            }

            return false;
        }

        public enum BoundsTestResult
        {
            Within,
            NearEdge,
            Outside
        }

        public static BoundsTestResult CheckIsApproachingBounds(int localPlayerPed, float outOfBoundsYLimit = SharedConstants.OutOfBoundsYLimit)
        {
            // Distance inside the bounds at which the player should be notified.
            const float margin = 250f;

            // TODO: when we finally get to implementing #55 (configurable play areas),
            // this will not work because it only does a 1D check against the player's Y-coord. But that's good enough for now.
            Vector3 pos = GetEntityCoords(localPlayerPed, false);
            if(pos.Y >= outOfBoundsYLimit)
            {
                return BoundsTestResult.Outside;
            }
            if(pos.Y >= outOfBoundsYLimit - margin)
            {
                return BoundsTestResult.NearEdge;
            }

            return BoundsTestResult.Within;
        }

        public BoundsTestResult CheckIsApproachingBounds(int localPlayerPed)
        {
            return CheckIsApproachingBounds(localPlayerPed, OutOfBoundsYLimit);
        }
    }
}
