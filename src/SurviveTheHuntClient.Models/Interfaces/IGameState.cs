namespace SurviveTheHuntClient.Interfaces
{
    public interface IGameState
    {
        /// <summary>
        /// Current objective text. This will be displayed every frame at the bottom of the screen.
        /// </summary>
        string CurrentObjective { get; set; }

        /// <summary>
        /// Details about the current hunt session.
        /// </summary>
        IHuntDetails Hunt { get; }

        /// <summary>
        /// The name of the currently active mode
        /// </summary>
        string Mode { get; }
    }
}
