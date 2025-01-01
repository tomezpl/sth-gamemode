using CitizenFX.Core;
using SurviveTheHuntShared.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SurviveTheHuntShared.Core.Teams;

namespace SurviveTheHuntServer
{
    internal static class TeamPluginState
    {
        public static Dictionary<int, List<Teams.Team>> PlayerTeams = new Dictionary<int, List<Teams.Team>>();
        public static Dictionary<int, Action<Player, string>> PlayerCleanupActions = new Dictionary<int, Action<Player, string>>();
    }

    public partial class MainScript
    {
        private void CleanupPlayerTeams([FromSource] Player player, string reason)
        {
            if (int.TryParse(player.Handle, out int droppedPlayerId))
            {
                if (TeamPluginState.PlayerTeams.TryGetValue(droppedPlayerId, out List<Teams.Team> playerTeams))
                {
                    foreach (Teams.Team team in playerTeams)
                    {
                        LeaveTeam(player, team, false);
                    }
                    TeamPluginState.PlayerTeams.Remove(droppedPlayerId);
                    EventHandlers["playerDropped"] -= TeamPluginState.PlayerCleanupActions[droppedPlayerId];
                    TeamPluginState.PlayerCleanupActions.Remove(droppedPlayerId);
                }
            }
        }

        private void JoinTeam(Player player, Teams.Team team)
        {
            if(!int.TryParse(player.Handle, out int playerId))
            {
                return;
            }

            Debug.WriteLine($"Adding player {player.Name} to team {team}");

            TriggerEvent("chat-hook-teams:joinTeam", team, playerId);
            bool createCleanupAction = true;
            if(TeamPluginState.PlayerTeams.TryGetValue(playerId, out List<Teams.Team> playerTeams))
            {
                playerTeams.Add(team);
                createCleanupAction = false;
            }
            else
            {
                TeamPluginState.PlayerTeams.Add(playerId, new List<Teams.Team>() { team });
            }

            if(createCleanupAction)
            {
                Action<Player, string> playerCleanupAction = new Action<Player, string>(CleanupPlayerTeams);

                EventHandlers["playerDropped"] += playerCleanupAction;
                TeamPluginState.PlayerCleanupActions.Add(playerId, playerCleanupAction);
            }
            else
            {
                Debug.WriteLine($"Player {player.Name} is already in a team, so no cleanup action is being created.");
            }
        }

        private void LeaveTeam(Player player, Teams.Team team, bool updateState = true)
        {
            if (!int.TryParse(player.Handle, out int playerId))
            {
                return;
            }

            TriggerEvent("chat-hook-teams:unjoinTeam", team, playerId);
            Debug.WriteLine($"Removed player {player.Name} from team {team}");

            if (updateState && TeamPluginState.PlayerTeams.TryGetValue(playerId, out List<Teams.Team> playerTeams))
            {
                playerTeams.RemoveAll(playerTeam => team == playerTeam);
            }
        }

        private void ResetTeams()
        {
            TriggerEvent("chat-hook-teams:resetTeams");

            foreach(Action<Player, string> cleanupAction in TeamPluginState.PlayerCleanupActions.Values)
            {
                EventHandlers["playerDropped"] -= cleanupAction;
            }

            TeamPluginState.PlayerCleanupActions.Clear();
        }
    }
}
