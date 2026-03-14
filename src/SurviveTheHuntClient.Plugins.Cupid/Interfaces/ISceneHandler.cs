using SurviveTheHuntClient.Interfaces;

namespace SurviveTheHuntClient.Plugins.Cupid.Interfaces
{
    internal interface ISceneHandler : ITickable
    {
        bool IsOver { get; }
        bool CanShowHud { get; }

        void StartScene(in IGameState gameState, int cameraId);

        void Cleanup();

        void OnNetEntityReceived(int netId, string name);
    }
}
