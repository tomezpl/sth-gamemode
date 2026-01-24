namespace SurviveTheHuntClient.Interfaces
{
    public interface IHuntUI
    {
        /// <summary>
        /// Display the current objective text at the bottom of the screen, as per the <paramref name="gameState"/>.
        /// </summary>
        /// <param name="gameState">Reference to this client's <see cref="IGameState"/>.</param>
        /// <param name="playerState">Reference to this client's <see cref="IPlayerState"/>.</param>
        /// <param name="ended">Pass true if the game is ending - this will make sure the objective text disappears on time.</param>
        /// <param name="skipAddingHuntedName">If true, the UI should not automatically inject the hunted player's name to the objective string.</param>
        void DisplayObjective(IGameState gameState, IPlayerState playerState, bool ended = false, bool skipAddingHuntedName = false);
    }
}
