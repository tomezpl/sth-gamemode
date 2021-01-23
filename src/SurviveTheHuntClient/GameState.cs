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
            public Player HuntedPlayer { get; set; } = null;

            public void End()
            {
                IsStarted = false;
                IsOver = false;
            }
        }

        public HuntDetails Hunt { get; set; } = new HuntDetails();
    }
}