using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    internal static class IntensityTracker
    {
        private const float MinDistanceScore = 0.3f;
        private const float MinTotalScore = 1.85f;
        private const float MinActivationSeconds = 8.5f;
        private const float MinSecondsCooldown = 10f;
        private const float MaxSecondsCooldown = 30f;

        private static float ActivationSecondsPassed = 0f;
        private static float CooldownSecondsPassed = 0f;
        private static float TargetCooldownSeconds = float.PositiveInfinity;

        internal enum Tier
        {
            None,
            Stealth,
            Pursuit
        }

        private static Tier currentTier = Tier.None;

        internal static Tier CurrentTier { get => currentTier; }

        /// <summary>
        /// Sets the current tier and returns true if it was different.
        /// </summary>
        /// <param name="tier"></param>
        /// <returns></returns>
        internal static bool SetTier(Tier tier)
        {
            bool changed = tier != currentTier;

            currentTier = tier;

            return changed;
        }


        /// <summary>
        /// Contains values for a "subject" that contributes to the intensity score - this will typically be a 
        /// </summary>
        internal class SubjectState
        {
            public CitizenFX.Core.Vector3 Velocity = new CitizenFX.Core.Vector3(0, 0, 0);
            public CitizenFX.Core.Vector3 Pos = CitizenFX.Core.Vector3.Zero;
            public float SecondsInLineOfSight = 0f;
            public float Heading = 0f;

            public const float ActivationSecondsLOS = 5f;
            public const float ProximityRadius = 60f;

            private const float ProximityRadiussSqr = ProximityRadius * ProximityRadius;

            internal struct Result
            {
                internal float SpeedFraction;
                internal float HeadingSine;
                internal float InvDistance;
                internal float LineOfSight;
            }

            public Result CalculateScoreFull(CitizenFX.Core.Vector3 myPos, float myHeading, CitizenFX.Core.Vector3 myVelocity, out float score)
            {
                score = 0f;
                Result result = new Result();

                // Get the % of speed this hunter has compared to ours
                result.SpeedFraction = 0f;
                if(!Velocity.IsZero && !myVelocity.IsZero)
                {
                    result.SpeedFraction = (float)Math.Min(1.0, Math.Sqrt((double)Velocity.LengthSquared() / (double)myVelocity.LengthSquared()));
                }
                else if(!Velocity.IsZero)
                {
                    result.SpeedFraction = 1f;
                }

                // Get relative heading sine
                const double deg2rad = Math.PI / 180.0;
                result.HeadingSine = (float)(1.0 - (Math.Abs(Math.Cos(Heading * deg2rad) - Math.Cos(myHeading * deg2rad))));

                float dist = Pos.DistanceToSquared(myPos);
                if (dist != 0f)
                {
                    //result.InvDistance = (float)(1.0 - Math.Sqrt(ProximityRadiussSqr / (double)dist));
                    result.InvDistance = (float)(1.0 - (Pos - myPos).Length() / ProximityRadius);
                } else
                {
                    result.InvDistance = 1f;
                }

                // TODO
                result.LineOfSight = 0f;

                score = result.LineOfSight + result.HeadingSine + result.InvDistance + result.SpeedFraction;

                return result;
            }
        }

        internal static bool Tick(Random rng, params SubjectState[] subjects)
        {
            bool tierChanged = false;

            float avgScore = 0f;
            int playerPed = PlayerPedId();
            CitizenFX.Core.Vector3 velocity = GetEntityVelocity(playerPed), pos = GetEntityCoords(playerPed, false);
            float heading = GetEntityHeading(playerPed);
            bool distanceCriterionMetOnce = false;
            foreach(SubjectState subject in subjects)
            {
                SubjectState.Result result = subject.CalculateScoreFull(pos, heading, velocity, out float score);
                avgScore += Math.Max(0f, score);
                distanceCriterionMetOnce = distanceCriterionMetOnce || result.InvDistance >= MinDistanceScore;
            }
            if(subjects.Length != 0)
            {
                avgScore /= subjects.Length;
            }

            float deltaTime = GetFrameTime();
            if(avgScore >= MinTotalScore && distanceCriterionMetOnce)
            {
                if (CurrentTier == Tier.None || CooldownSecondsPassed > 0f)
                {
                    ActivationSecondsPassed += deltaTime;
                }
            }
            else
            {
                // Reset the timer
                ActivationSecondsPassed = 0f;

                if(currentTier != Tier.None)
                {
                    if(CooldownSecondsPassed == 0f)
                    {
                        TargetCooldownSeconds = (float)(MinSecondsCooldown + rng.NextDouble() * ((MaxSecondsCooldown - (double)MinSecondsCooldown) + 1.0));
                    }

                    CooldownSecondsPassed += deltaTime;
                }
            }

            if(ActivationSecondsPassed >= MinActivationSeconds)
            {
                ActivationSecondsPassed = 0f;
                CooldownSecondsPassed = 0f;
                if(CurrentTier == Tier.None)
                {
                    currentTier = Tier.Pursuit;
                    tierChanged = true;
                }
            }

            // Check if we ran over the target cooldown and switch the tier to None if so
            if(CooldownSecondsPassed >= TargetCooldownSeconds)
            {
                currentTier = Tier.None;
                CooldownSecondsPassed = 0f;
                TargetCooldownSeconds = float.PositiveInfinity;
                tierChanged = true;
            }

            return tierChanged;
        }
    }
}
