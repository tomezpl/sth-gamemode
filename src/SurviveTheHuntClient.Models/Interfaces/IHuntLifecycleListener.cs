using SurviveTheHuntShared.Core;

namespace SurviveTheHuntClient.Interfaces
{
    public interface IHuntLifecycleListener
    {
        void OnHuntStarted(IGameState gameState, IPlayerState playerState);
        void OnHuntEnded(Teams.Team winningTeam, IGameState gameState, IPlayerState playerState);
    }
}
