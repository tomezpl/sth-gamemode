using System;
using CitizenFX.Core;

namespace SurviveTheHuntClient
{
    public class GameState
    {
        public string CurrentObjective { get; set; }

        public class HuntDetails
        {
            public bool IsStarted { get; set; } = false;
            public bool IsOver { get; set; } = false;
            public bool IsInProgress { get { return IsStarted && !IsOver; } }
            public bool IsEnding { get { return !IsInProgress && IsOver; } }
            public Player HuntedPlayer { get; set; } = null;

            public int EndInMilliseconds { get; set; } = -1;
            public DateTime EndTime { get; set; } = new DateTime();

            public void End()
            {
                IsStarted = false;
                IsOver = false;
                EndInMilliseconds = -1;
            }
        }

        public HuntDetails Hunt { get; set; } = new HuntDetails();

        public static bool IsPedTooFar(Ped ped)
        {
            return ped.Position.Y >= Constants.ZLimit;
        }
    }
}