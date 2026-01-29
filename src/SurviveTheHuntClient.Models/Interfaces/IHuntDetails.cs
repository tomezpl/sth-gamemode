using CitizenFX.Core;
using SurviveTheHuntClient.Models;
using System;

namespace SurviveTheHuntClient.Interfaces
{
    public interface IHuntDetails : ITickable
    {
        /// <summary>
        /// Time when hunt is meant to end & state be reset.
        /// This includes the delay for displaying the win/loss text.
        /// </summary>
        DateTime ActualEndTime { get; set; }

        /// <summary>
        /// Expected end time (set at start of the hunt).
        /// </summary>
        DateTime InitialEndTime { get; set; }

        /// <summary>
        /// Has the hunt been started?
        /// </summary>
        bool IsStarted { get; set; }

        /// <summary>
        /// Has the hunt finished?
        /// </summary>
        bool IsOver { get; set; }

        /// <summary>
        /// Is the hunt currently ongoing?
        /// </summary>
        bool IsInProgress { get; }

        /// <summary>
        /// Is the hunt currently ending? (ie. winner notification being shown, etc.)
        /// </summary>
        bool IsEnding { get; }

        /// <summary>
        /// Can a hunt be started right now?
        /// </summary>
        bool CanBeStarted { get; }

        /// <summary>
        /// Is the prep phase still active?
        /// </summary>
        bool IsPrepPhase { get; }

        /// <summary>
        /// Currently hunted player(s).
        /// </summary>
        HuntPlayer[] HuntedPlayers { get; set; }

        /// <summary>
        /// Checks if the player is a hunted player.
        /// </summary>
        /// <param name="playerHandle">The local handle of the player to check.</param>
        /// <param name="huntedPlayerInfo">Details about the player.</param>
        /// <returns></returns>
        bool IsHunted(int playerHandle, out HuntPlayer? huntedPlayerInfo);

        /// <summary>
        /// Was <see cref="IsStarted"/> true last frame?
        /// This can be used for checking if the hunt has only just started.
        /// </summary>
        bool WasHuntInProgressLastFrame { get; }

        /// <summary>
        /// Regenerate the hunted players' mugshot texture(s).
        /// </summary>
        void UpdateHuntedMugshot();

        /// <summary>
        /// Time when the prep phase is already over.
        /// </summary>
        DateTime PrepPhaseEndTime { get; set; }
    }
}
