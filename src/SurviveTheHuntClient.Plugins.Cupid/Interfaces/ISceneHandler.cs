using SurviveTheHuntClient.Interfaces;

namespace SurviveTheHuntClient.Plugins.Cupid.Interfaces
{
    internal interface ISceneHandler : ITickable
    {
        bool IsOver { get; }

        void StartScene(in IGameState gameState, int cameraId);
    }
}
