﻿using CitizenFX.Core;
using SurviveTheHuntShared.Core;
using System;
using static CitizenFX.Core.Native.API;
using SharedConstants = SurviveTheHuntShared.Constants;

namespace SurviveTheHuntServer
{
    public class GameState
    {
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
            /// The currently hunted player.
            /// </summary>
            public Player HuntedPlayer { get; set; } = null;

            /// <summary>
            /// The player hunted during the last session - this aims to prevent the same player being hunted twice consecutively.
            /// </summary>
            public Player LastHuntedPlayer { get; set; } = null;

            /// <summary>
            /// UTC time of starting the hunt session.
            /// </summary>
            public DateTime StartTime { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// UTC time when the hunt session should end (it could end before that).
            /// </summary>
            public DateTime EndTime { get { return StartTime + SharedConstants.HuntDuration + EndTimeOffset; } }

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

            /// <summary>
            /// Starts the hunt for a given player.
            /// </summary>
            /// <param name="huntedPlayer"></param>
            public void Begin(Player huntedPlayer, ulong prepPhaseSeconds = 0)
            {
                IsStarted = true;
                HuntedPlayer = huntedPlayer;
                WinningTeam = Teams.Team.Hunted;
                StartTime = DateTime.UtcNow;
                LastPingTime = DateTime.UtcNow + TimeSpan.FromSeconds(prepPhaseSeconds) - SharedConstants.HuntedPingInterval;
                PrepPhaseEndTime = StartTime + TimeSpan.FromSeconds(prepPhaseSeconds);
                EndTimeOffset = TimeSpan.FromSeconds(prepPhaseSeconds);
            }

            /// <summary>
            /// Ends the hunt with <paramref name="winningTeam"/> being marked as the winner(s).
            /// </summary>
            /// <param name="winningTeam">Team that should win this hunt.</param>
            public void End(Teams.Team winningTeam)
            {
                WinningTeam = winningTeam;
                IsStarted = false;
                HuntedPlayer = null;
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
            TriggerClientEvent
            (
                player, 
                SurviveTheHuntShared.Events.Client.ReceiveGameState, 
                gameState.Hunt.IsStarted, 
                gameState.Hunt.HuntedPlayer != null ? int.Parse(gameState.Hunt.HuntedPlayer.Handle) : int.MinValue, 
                gameState.Hunt.StartTime.Ticks, gameState.Hunt.EndTime.Ticks, 
                gameState.Hunt.LastPingTime.Ticks, 
                gameState.Hunt.PrepPhaseEndTime.Ticks
            );
        }
    }
}
