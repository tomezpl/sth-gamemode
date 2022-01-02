using System;
using CitizenFX.Core;

using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public class GameState
    {
        /// <summary>
        /// Current objective text. This will be displayed every frame at the bottom of the screen.
        /// </summary>
        public string CurrentObjective { get; set; }

        /// <summary>
        /// Details about the current hunt session.
        /// </summary>
        public HuntDetails Hunt { get; set; } = new HuntDetails();

        public class HuntDetails
        {
            /// <summary>
            /// Has the hunt been started?
            /// </summary>
            public bool IsStarted { get; set; } = false;

            /// <summary>
            /// Has the hunt finished?
            /// </summary>
            public bool IsOver { get; set; } = false;

            /// <summary>
            /// Is the hunt currently ongoing?
            /// </summary>
            public bool IsInProgress { get { return IsStarted && !IsOver; } }

            /// <summary>
            /// Is the hunt currently ending? (ie. winner notification being shown, etc.)
            /// </summary>
            public bool IsEnding { get { return !IsInProgress && IsOver; } }

            /// <summary>
            /// Can a hunt be started right now?
            /// </summary>
            public bool CanBeStarted { get { return !IsInProgress && !IsEnding; } }

            /// <summary>
            /// Currently hunted player.
            /// </summary>
            public Player HuntedPlayer { get; set; } = null;

            /// <summary>
            /// Mugshot texture of the currently hunted player.
            /// </summary>
            public Texture HuntedPlayerMugshot { get; set; } = null;

            /// <summary>
            /// The currently hunted player's mugshot texture generation time.
            /// </summary>
            public DateTime LastHuntedMugshotGeneration { get; set; } = Utility.CurrentTime;

            /// <summary>
            /// Time when hunt is meant to end & state be reset.
            /// This includes the delay for displaying the win/loss text.
            /// </summary>
            public DateTime ActualEndTime { get; set; } = new DateTime();

            /// <summary>
            /// Expected end time (set at start of the hunt).
            /// </summary>
            public DateTime InitialEndTime { get; set; } = new DateTime();

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
                playerState.TakeAwayWeapons(ref playerPed, true);
            }

            /// <summary>
            /// Regenerates the hunted player's mugshot texture.
            /// </summary>
            public void UpdateHuntedMugshot()
            {
                if(!IsStarted)
                {
                    return;
                }

                // If the mugshot texture requires regeneration, unregister it.
                if(HuntedPlayerMugshot != null && Utility.CurrentTime - LastHuntedMugshotGeneration >= Constants.MugshotGenerationInterval)
                {
                    UnregisterPedheadshot(HuntedPlayerMugshot.Id);
                    HuntedPlayerMugshot = null;
                }

                // If the mugshot texture has been unregistered, generate a new one.
                if (HuntedPlayerMugshot == null)
                {
                    HuntedPlayerMugshot = new Texture() { Id = RegisterPedheadshotTransparent(HuntedPlayer.Character.Handle) };
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
            return ped.Position.Y >= Constants.OutOfBoundsYLimit;
        }
    }
}