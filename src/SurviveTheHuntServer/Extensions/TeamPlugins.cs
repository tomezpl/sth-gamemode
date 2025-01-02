using CitizenFX.Core;
using SurviveTheHuntShared.Core;
using System;
using System.Collections.Generic;

namespace SurviveTheHuntServer
{
    internal static class TeamPluginState
    {
        /// <summary>
        /// Lists of teams each player belongs to (key is player ID, value is list of teams they belong to).
        /// 
        /// Typically a player will only belong to one team but keeping it as a list might come in handy in the future.
        /// </summary>
        public static Dictionary<int, List<Teams.Team>> PlayerTeams = new Dictionary<int, List<Teams.Team>>();
    }

    public partial class MainScript
    {
        /// <summary>
        /// Event handler that updates the <see cref="TeamPluginState"/> when a player leaves.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        [EventHandler("playerDropped")]
        public void CleanupPlayerTeams([FromSource] Player player, string reason)
        {
            if (int.TryParse(player.Handle, out int droppedPlayerId))
            {
                // If the player belonged to any teams, run the "leave" plugin(s) for each of them (e.g. team chat) and remove the player's entry from the state.
                if (TeamPluginState.PlayerTeams.TryGetValue(droppedPlayerId, out List<Teams.Team> playerTeams))
                {
                    Debug.WriteLine($"Removing {player.Name} from all {playerTeams.Count} team(s) they belonged to");
                    foreach (Teams.Team team in playerTeams)
                    {
                        LeaveTeam(player, team, false);
                    }
                    TeamPluginState.PlayerTeams.Remove(droppedPlayerId);
                }
            }
        }

        /// <summary>
        /// Notifies plugins to add a <paramref name="player"/> to a <paramref name="team"/>.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="team"></param>
        private void JoinTeam(Player player, Teams.Team team)
        {
            if(!int.TryParse(player.Handle, out int playerId))
            {
                return;
            }

            Debug.WriteLine($"Adding player {player.Name} to team {team}");

            TriggerEvent("chat-hook-teams:joinTeam", team, playerId);

            // Record the player's new team in the state.
            if(TeamPluginState.PlayerTeams.TryGetValue(playerId, out List<Teams.Team> playerTeams))
            {
                playerTeams.Add(team);
            }
            else
            {
                TeamPluginState.PlayerTeams.Add(playerId, new List<Teams.Team>() { team });
            }
        }

        /// <summary>
        /// Notifies plugins to remove a <paramref name="player"/> from a <paramref name="team"/>.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="team"></param>
        private void LeaveTeam(Player player, Teams.Team team, bool updateState = true)
        {
            if (!int.TryParse(player.Handle, out int playerId))
            {
                return;
            }

            TriggerEvent("chat-hook-teams:unjoinTeam", team, playerId);
            Debug.WriteLine($"Removed player {player.Name} from team {team}");

            // Remove the player's team membership from the state.
            if (updateState && TeamPluginState.PlayerTeams.TryGetValue(playerId, out List<Teams.Team> playerTeams))
            {
                playerTeams.RemoveAll(playerTeam => team == playerTeam);
            }
        }

        /// <summary>
        /// Notifies plugins to reset their team state(s).
        /// </summary>
        private void ResetTeams()
        {
            TriggerEvent("chat-hook-teams:resetTeams");

            // Remove all players' teams.
            TeamPluginState.PlayerTeams.Clear();
        }
    }
}
