namespace SurviveTheHuntClient.Interfaces
{
    public interface IHuntLifecycleListener
    {
        void OnHuntStarted(IGameState gameState, IPlayerState playerState);
        void OnHuntEnded(IGameState gameState, IPlayerState playerState);
    }
}
