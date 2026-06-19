using CitizenFX.Core;
using SurviveTheHuntServer.Helpers;
using SurviveTheHuntShared.Core;
using SurviveTheHuntShared.Interfaces;
using SurviveTheHuntShared.Plugins;
using System;
using System.Collections.Generic;
using SharedConstants = SurviveTheHuntShared.Constants;

namespace SurviveTheHuntServer
{
    public class GameState
    {
        public string ActiveMode { get; set; } = "";

        public bool IsDefaultMode { get => Utils.Mode.IsDefaultMode(ActiveMode); }

        /// <summary>
        /// Details about a hunt session.
        /// </summary>
        public class HuntDetails
        {
            /// <summary>
            /// Has the hunt started?
            /// </summary>
            public bool IsStarted { get; set; } = false;

            /// <summary>
            /// The team that should win when the hunt ends.
            /// </summary>
            public Teams.Team WinningTeam { get; set; } = Teams.Team.Hunted;

            /// <summary>
            /// The currently hunted players.
            /// </summary>
            public Player[] HuntedPlayers { get; set; } = new Player[0];

            /// <summary>
            /// The player hunted during the last session - this aims to prevent the same player being hunted twice consecutively.
            /// </summary>
            public Player[] LastHuntedPlayers { get; set; } = new Player[0];

            /// <summary>
            /// UTC time of starting the hunt session.
            /// </summary>
            public DateTime StartTime { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// UTC time when the hunt session should end (it could end before that).
            /// </summary>
            public DateTime EndTime { get { return StartTime + SharedConstants.HuntDuration + EndTimeOffset; } }

            /// <summary>
            /// UTC time when the next hunt can be started (this is to induce a cooldown so client scripts can catch up - yes it's gash but otherwise wrong weapon loadouts can be given out)
            /// If null, this means the hunt can be started right now.
            /// </summary>
            public DateTime? NextHuntStartTime = null;

            /// <summary>
            /// UTC time of last time the hunted player was pinged on the map.
            /// </summary>
            public DateTime LastPingTime { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// UTC time of when the prep phase is supposed to end and the actual hunt begins.
            /// </summary>
            public DateTime PrepPhaseEndTime { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// Any extra time that needs to be added to the round duration (e.g. prep phase, or whatever modifiers affect the end time).
            /// </summary>
            public TimeSpan EndTimeOffset { get; set; } = TimeSpan.Zero;

            private Dictionary<PluginIndex, IPluginState> _pluginStates = new Dictionary<PluginIndex, IPluginState>();
            internal Dictionary<PluginIndex, IPluginState> PluginStates => _pluginStates;

            internal IPluginState GetOrCreatePluginState(PluginIndex plugin)
            {
                if(_pluginStates.TryGetValue(plugin, out IPluginState state))
                {
                    return state;
                }
                
                try
                {
                    IPluginState newState = PluginStateFactory.Create(plugin);
                    _pluginStates.Add(plugin, newState);
                    return newState;
                } catch(Exception ex)
                {
                    Debug.WriteLine($"{nameof(GameState)}.{nameof(HuntDetails)}.{nameof(GetOrCreatePluginState)}({nameof(plugin)}: {plugin}): state does not exist and could not be created: {ex}");
                }

                return null;
            }

            /// <summary>
            /// Starts the hunt for a given player.
            /// </summary>
            /// <param name="huntedPlayers"></param>
            public void Begin(Player[] huntedPlayers, ulong prepPhaseSeconds = 0)
            {
                IsStarted = true;
                HuntedPlayers = huntedPlayers;
                WinningTeam = Teams.Team.Hunted;
                StartTime = DateTime.UtcNow;
                LastPingTime = DateTime.UtcNow + TimeSpan.FromSeconds(prepPhaseSeconds) - SharedConstants.HuntedPingInterval;
                PrepPhaseEndTime = StartTime + TimeSpan.FromSeconds(prepPhaseSeconds);
                EndTimeOffset = TimeSpan.FromSeconds(prepPhaseSeconds);

                _pluginStates = new Dictionary<PluginIndex, IPluginState>();
            }

            /// <summary>
            /// Ends the hunt with <paramref name="winningTeam"/> being marked as the winner(s).
            /// </summary>
            /// <param name="winningTeam">Team that should win this hunt.</param>
            public void End(Teams.Team winningTeam)
            {
                WinningTeam = winningTeam;
                IsStarted = false;
                HuntedPlayers = new Player[0];
                NextHuntStartTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(SharedConstants.HuntStartCooldown);
            }
        }

        /// <summary>
        /// Details about the current hunt session.
        /// </summary>
        public HuntDetails Hunt { get; set; } = new HuntDetails();
    }

    public partial class MainScript
    {
        public void SendGameState(Player player, GameState gameState)
        {
            int[] huntedPlayerServerIds = new int[gameState.Hunt.HuntedPlayers.Length];
            for(int i = 0; i < huntedPlayerServerIds.Length; i++)
            {
                huntedPlayerServerIds[i] = int.Parse(gameState.Hunt.HuntedPlayers[i].Handle);
            }

            TriggerClientEvent
            (
                player, 
                SurviveTheHuntShared.Events.Client.ReceiveGameState,
                GameState.ActiveMode ?? "",
                gameState.Hunt.IsStarted, 
                huntedPlayerServerIds, 
                gameState.Hunt.StartTime.Ticks, gameState.Hunt.EndTime.Ticks, 
                gameState.Hunt.LastPingTime.Ticks, 
                gameState.Hunt.PrepPhaseEndTime.Ticks
            );

            if(gameState.Hunt.IsStarted)
            {
                JoinTeam(player, Teams.Team.Hunters);
            }
        }
    }
}
