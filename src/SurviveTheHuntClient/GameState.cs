using System;
using CitizenFX.Core;
using SharedConstants = SurviveTheHuntShared.Constants;

using static CitizenFX.Core.Native.API;
using SurviveTheHuntShared.Core;
using SurviveTheHuntClient.Models;
using System.Collections.Generic;
using SurviveTheHuntClient.Helpers;

namespace SurviveTheHuntClient
{
    public class GameState : Interfaces.IGameState
    {
        public string CurrentObjective { get; set; }

        public string Mode { get; set; } = "";

        private HuntDetails _hunt = new HuntDetails();

        public Interfaces.IHuntDetails Hunt { get => _hunt; }

        public class HuntDetails : Interfaces.IHuntDetails
        {
            public bool IsStarted { get; set; } = false;

            public bool IsOver { get; set; } = false;

            public bool IsInProgress { get { return IsStarted && !IsOver; } }

            public bool IsEnding { get { return !IsInProgress && IsOver; } }

            public bool CanBeStarted { get { return !IsInProgress && !IsEnding; } }

            public bool IsPrepPhase { get { return Utility.CurrentTime < PrepPhaseEndTime; } }

            public HuntPlayer[] HuntedPlayers { get; set; } = new HuntPlayer[0];

            public static readonly HuntSettings DefaultHuntSettings = new HuntSettings
            {
                TeamsAllowedOutOfSafeZoneDuringPrep = HuntSettings.GetTeamsBitset(Teams.Team.Hunted),
                SafeZoneRadius = SharedConstants.DefaultSpawnSafeZoneRadius
            };

            public HuntSettings Settings { get; set; } = DefaultHuntSettings;

            public bool IsHunted(int playerHandle, out HuntPlayer? huntPlayerInfo)
            {
                for(int i = 0; i < HuntedPlayers.Length; i++)
                {
                    if (playerHandle == HuntedPlayers[i].PlayerHandle)
                    {
                        huntPlayerInfo = HuntedPlayers[i];
                        return true;
                    }
                }

                huntPlayerInfo = null;
                return false;
            }

            public DateTime ActualEndTime { get; set; } = new DateTime();

            public DateTime InitialEndTime { get; set; } = new DateTime();

            /// <summary>
            /// Time when the prep phase is already over. See <see cref="Interfaces.IHuntDetails.PrepPhaseEndTime"/>
            /// </summary>
            /// <remarks>
            /// Note: this does not always refer to the actual hunt start time. For a player joining in progress,
            /// this value will be equal to <see cref="Utility.CurrentTime"/>, as we only need to know
            /// how much longer the prep phase is going to last.
            /// </remarks>
            public DateTime PrepPhaseEndTime { get; set; } = new DateTime();

            private bool _wasHuntInProgressLastFrame = false;
            public bool WasHuntInProgressLastFrame { get => _wasHuntInProgressLastFrame; }

            /// <summary>
            /// Ends the hunt and resets the state so that it can be started again.
            /// </summary>
            /// <param name="playerState"></param>
            public void End(ref PlayerState playerState)
            {
                Debug.WriteLine("Ending hunt");

                // Reset state.
                IsStarted = false;
                IsOver = false;

                // Reset end time.
                ActualEndTime = new DateTime();

                for(int i = 0; i < HuntedPlayers.Length; i++)
                {
                    // Remove the hunted players' mugshot textures, if they exist.
                    if (HuntedPlayers[i].Mugshot != null)
                    {
                        UnregisterPedheadshot(HuntedPlayers[i].Mugshot.Id);
                        HuntedPlayers[i].Mugshot = null;
                    }
                }

                // Reset the player's team.
                playerState.Team = Teams.Team.Hunters;

                // Reset the player's weapons.
                Ped playerPed = Game.PlayerPed;
                playerState.TakeAwayWeapons(playerPed);
            }

            /// <summary>
            /// Logic tick - needs to be called on every script update!
            /// </summary>
            public void Tick(float deltaTime)
            {
                _wasHuntInProgressLastFrame = IsStarted;
            }

            public void UpdateHuntedMugshot(ref HuntPlayer playerToUpdate, in DateTime? currentTime = null)
            {
                Models.Texture mugshot = playerToUpdate.Mugshot;
                DateTime nextTimestamp = playerToUpdate.MugshotExpiry;
                DateTime lastTimestamp = playerToUpdate.MugshotTimestamp;

                // If the mugshot texture requires regeneration, unregister it.
                if (mugshot != null && (currentTime ?? Utility.CurrentTime) >= nextTimestamp - SharedConstants.MugshotGenerationTimeout)
                {
                    UnregisterPedheadshot(mugshot.Id);
                    mugshot = null;

                    // "Predict" the next mugshot time for now (the actual time will be sent down from the server in an event, but it's a fixed interval)
                    // This is just to prevent the mugshot being re-registered on every tick (that would be bad...)
                    nextTimestamp = nextTimestamp + (nextTimestamp - lastTimestamp);
                }

                // If the mugshot texture has been unregistered, generate a new one.
                if (mugshot == null)
                {
                    Debug.WriteLine($"Generating a mugshot for player {GetPlayerName(playerToUpdate.PlayerHandle)}");
                    int pedId = GetPlayerPed(playerToUpdate.PlayerHandle);
                    mugshot = new Models.Texture() { Id = RegisterPedheadshot(pedId) };
                    lastTimestamp = currentTime ?? Utility.CurrentTime;
                }

                // If the mugshot texture doesn't have a TXD string assigned yet, check that it's ready and assign it if so.
                if (!mugshot.IsValid)
                {
                    if (IsPedheadshotReady(mugshot.Id) && IsPedheadshotValid(mugshot.Id))
                    {
                        mugshot.Name = GetPedheadshotTxdString(mugshot.Id);
                    }
                }

                playerToUpdate.Mugshot = mugshot;
                playerToUpdate.MugshotTimestamp = lastTimestamp;
                playerToUpdate.MugshotExpiry = nextTimestamp;
            }

            public void UpdateHuntedMugshot()
            {
                if(!IsStarted)
                {
                    return;
                }

                DateTime currentTime = Utility.CurrentTime;
                for(int i = 0; i < HuntedPlayers.Length; i++)
                {
                    UpdateHuntedMugshot(ref HuntedPlayers[i], currentTime);
                }
            }
        }

        public void ScheduleNextMugshotTime(in DateTime time)
        {
            for(int i = 0; i < Hunt.HuntedPlayers.Length; i++)
            {
                Hunt.HuntedPlayers[i].MugshotExpiry = time;
            }
        }

        /// <summary>
        /// Checks if the given ped has gone out of hunt area bounds.
        /// </summary>
        /// <param name="ped">The ped to check.</param>
        /// <returns>true if player is out of bounds, false otherwise.</returns>
        internal static bool IsPedTooFar(Ped ped, BoundsTracker boundsTracker = null)
        {
            return boundsTracker == null ? ped.Position.Y >= SharedConstants.OutOfBoundsYLimit : (boundsTracker.CheckIsApproachingBounds(ped.Handle) == BoundsTracker.BoundsTestResult.Outside);
        }
    }

    public partial class MainScript
    {
        [EventHandler(SurviveTheHuntShared.Events.Client.ReceiveGameState)]
        public void ReceiveGameState(string mode, bool isStarted, List<object> huntedPlayerServerIds, long startTimeTicks, long endTimeTicks, long lastPingTimeTicks, long prepPhaseEndTicks)
        {
            Debug.WriteLine("Received game state");
            SetMode(mode);
            bool isLocalPlayerHunted = false;
            // Only need to check the first player
            if (huntedPlayerServerIds.Count != 0 && NetworkIsPlayerConnected(GetPlayerFromServerId((int)huntedPlayerServerIds[0])))
            {
                Player[] huntedPlayers = new Player[huntedPlayerServerIds.Count];
                string huntedPlayerNames = "";
                for (int i = 0; i < huntedPlayers.Length; i++)
                {
                    huntedPlayers[i] = new Player(GetPlayerFromServerId((int)huntedPlayerServerIds[i]));
                    isLocalPlayerHunted = isLocalPlayerHunted || huntedPlayers[i].Handle == Player.Local.Handle;
                    huntedPlayerNames += $"{huntedPlayers[i].Name}";
                    if(i != huntedPlayers.Length - 1)
                    {
                        huntedPlayerNames += ", ";
                    }
                }
                Debug.WriteLine($"Game state: isStarted={isStarted}, huntedPlayers={huntedPlayerNames}, startTime={new DateTime(startTimeTicks)}, endTime={new DateTime(endTimeTicks)}, lastPingTime={new DateTime(lastPingTimeTicks)}");
                
                // TODO: shouldn't this use Utility.CurrentTime instead of DateTime.UtcNow?
                float secondsTillPing = (float)((new DateTime(lastPingTimeTicks, DateTimeKind.Utc) + SharedConstants.HuntedPingInterval) - DateTime.UtcNow).TotalSeconds;
                HuntStartedByServer(secondsTillPing, new DateTime(endTimeTicks, DateTimeKind.Utc), new DateTime(prepPhaseEndTicks, DateTimeKind.Utc) - Utility.CurrentTime);
                NotifyTeam(isLocalPlayerHunted ? Teams.Team.Hunted : Teams.Team.Hunters, huntedPlayers);
            }
        }
    }
}