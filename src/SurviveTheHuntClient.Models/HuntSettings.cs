using SurviveTheHuntShared.Core;
using System;
using System.Collections.Generic;
using static SurviveTheHuntShared.Core.Teams;

namespace SurviveTheHuntClient.Models
{
    public struct HuntSettings
    {
        public float SafeZoneRadius;

        /// <summary>
        /// A bitset representing indices of teams (<see cref="SurviveTheHuntShared.Core.Teams.Team"/>) that are allowed outside of the safe zone during the prep phase.
        /// Basically, each <see cref="SurviveTheHuntShared.Core.Teams.Team"/> is used as a bit index. If the team's bit is 0, they will not be alllowed outside the safezone until the prep phase is over.
        /// Compatible values should be obtained using <see cref="GetTeamsBitset(Teams.Team[])"/>
        /// </summary>
        public int TeamsAllowedOutOfSafeZoneDuringPrep;

        public static int GetTeamsBitset(params Teams.Team[] teams)
        {
            int bitset = 0;

            foreach(Teams.Team team in teams)
            {
                bitset |= 1 << (int)team;
            }

            return bitset;
        }

        public static Teams.Team[] GetTeamsFromBitset(int bitset)
        {
            int bitsToCheck = Math.Min(sizeof(int) * 8, Enum.GetValues(typeof(Teams.Team)).Length);
            List<Teams.Team> teams = new List<Teams.Team>();

            for(int i = 0; i < bitsToCheck; i++)
            {
                if (IsTeamInBitset(bitset, (Team)i))
                {
                    teams.Add((Team)i);
                }
            }

            return teams.ToArray();
        }

        public static bool IsTeamInBitset(int bitset, Teams.Team team)
        {
            if ((bitset & (1 << (int)team)) != 0)
            {
                return true;
            }

            return false;
        }
    }
}
