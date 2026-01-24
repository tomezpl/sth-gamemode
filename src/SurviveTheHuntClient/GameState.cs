using System;
using CitizenFX.Core;
using SharedConstants = SurviveTheHuntShared.Constants;

using static CitizenFX.Core.Native.API;
using SurviveTheHuntShared.Core;

namespace SurviveTheHuntClient
{
    public class GameState : Interfaces.IGameState
    {
        public string CurrentObjective { get; set; }


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

            public Player HuntedPlayer { get; set; } = null;

            public Models.Texture HuntedPlayerMugshot { get; set; } = null;

            /// <summary>
            /// The currently hunted player's mugshot texture generation time.
            /// </summary>
            public DateTime LastHuntedMugshotGeneration { get; set; } = Utility.CurrentTime;

            public DateTime NextMugshotTime { get; set; } = Utility.CurrentTime;

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

                // Remove the hunted player's mugshot texture, if it exists.
                if(HuntedPlayerMugshot != null)
                {
                    UnregisterPedheadshot(HuntedPlayerMugshot.Id);
                }
                HuntedPlayerMugshot = null;

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

            public void UpdateHuntedMugshot()
            {
                if(!IsStarted)
                {
                    return;
                }

                // If the mugshot texture requires regeneration, unregister it.
                if(HuntedPlayerMugshot != null && Utility.CurrentTime >= NextMugshotTime - SharedConstants.MugshotGenerationTimeout)
                {
                    UnregisterPedheadshot(HuntedPlayerMugshot.Id);
                    HuntedPlayerMugshot = null;

                    // "Predict" the next mugshot time for now (the actual time will be sent down from the server in an event, but it's a fixed interval)
                    // This is just to prevent the mugshot being re-registered on every tick (that would be bad...)
                    NextMugshotTime = NextMugshotTime + (NextMugshotTime - LastHuntedMugshotGeneration);
                }

                // If the mugshot texture has been unregistered, generate a new one.
                if (HuntedPlayerMugshot == null)
                {
                    Debug.WriteLine("Generating a mugshot!");
                    HuntedPlayerMugshot = new Models.Texture() { Id = RegisterPedheadshot(HuntedPlayer.Character.Handle) };
                    LastHuntedMugshotGeneration = Utility.CurrentTime;
                }

                // If the mugshot texture doesn't have a TXD string assigned yet, check that it's ready and assign it if so.
                if (!HuntedPlayerMugshot.IsValid)
                {
                    if(IsPedheadshotReady(HuntedPlayerMugshot.Id) && IsPedheadshotValid(HuntedPlayerMugshot.Id))
                    {
                        HuntedPlayerMugshot.Name = GetPedheadshotTxdString(HuntedPlayerMugshot.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the given ped has gone out of hunt area bounds.
        /// </summary>
        /// <param name="ped">The ped to check.</param>
        /// <returns>true if player is out of bounds, false otherwise.</returns>
        public static bool IsPedTooFar(Ped ped)
        {
            return ped.Position.Y >= SharedConstants.OutOfBoundsYLimit;
        }
    }

    public partial class MainScript
    {
        [EventHandler(SurviveTheHuntShared.Events.Client.ReceiveGameState)]
        public void ReceiveGameState(bool isStarted, int huntedPlayerServerId, long startTimeTicks, long endTimeTicks, long lastPingTimeTicks, long prepPhaseEndTicks)
        {
            Debug.WriteLine("Received game state");
            if (huntedPlayerServerId != int.MinValue && NetworkIsPlayerConnected(GetPlayerFromServerId(huntedPlayerServerId)))
            {
                Player huntedPlayer = new Player(GetPlayerFromServerId(huntedPlayerServerId));
                Debug.WriteLine($"Game state: isStarted={isStarted}, huntedPlayer={huntedPlayer.Name}, startTime={new DateTime(startTimeTicks)}, endTime={new DateTime(endTimeTicks)}, lastPingTime={new DateTime(lastPingTimeTicks)}");
                
                // TODO: shouldn't this use Utility.CurrentTime instead of DateTime.UtcNow?
                float secondsTillPing = (float)((new DateTime(lastPingTimeTicks, DateTimeKind.Utc) + SharedConstants.HuntedPingInterval) - DateTime.UtcNow).TotalSeconds;
                HuntStartedByServer(secondsTillPing, new DateTime(endTimeTicks, DateTimeKind.Utc), new DateTime(prepPhaseEndTicks, DateTimeKind.Utc) - Utility.CurrentTime);
                NotifyTeam(huntedPlayer == Player.Local ? Teams.Team.Hunted : Teams.Team.Hunters, huntedPlayer);
            }
        }
    }
}