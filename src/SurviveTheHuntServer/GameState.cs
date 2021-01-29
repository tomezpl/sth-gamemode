using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer
{
    public class GameState
    {
        public class HuntDetails
        {
            public bool IsStarted { get; set; } = false;
            public Teams.Team WinningTeam { get; set; } = Teams.Team.Hunted;
            public Player HuntedPlayer { get; set; } = null;
            public Player LastHuntedPlayer { get; set; } = null;
            public DateTime StartTime { get; set; } = DateTime.UtcNow;
            public DateTime EndTime { get { return StartTime + Constants.HuntDuration; } }

            public DateTime LastPingTime { get; set; } = DateTime.UtcNow;

            public void Begin(Player huntedPlayer)
            {
                IsStarted = true;
                HuntedPlayer = huntedPlayer;
                WinningTeam = Teams.Team.Hunted;
                StartTime = DateTime.UtcNow;
                LastPingTime = DateTime.UtcNow;
            }

            public void End(Teams.Team winningTeam)
            {
                WinningTeam = winningTeam;
                IsStarted = false;
            }
        }

        public HuntDetails Hunt { get; set; } = new HuntDetails();
    }
}
