using SurviveTheHuntClient.Models.UI;

namespace SurviveTheHuntClient.Interfaces
{
    public interface IPlugin
    {
        void OnHuntStarted(IGameState gameState, IPlayerState playerState);
        void OnHuntEnded(IGameState gameState, IPlayerState playerState);
        void OnResourceStarted();
        void OnResourceStopping();
        bool DoesPlayerNeedInvincibility { get; }
        void OnClockReceived(int hours, int minutes, int seconds);
        bool CanPingShow { get; }
        SurviveTheHuntShared.Core.Teams.Team? WinningTeamOverride { get; }
        string CustomWastedText { get; }
        bool SkipAddingPlayerNameInObjective { get; }
        LabelledItem[] UICurrentItems { get; }
        SurviveTheHuntShared.Utils.Coord[] CarSpawnPointsOverride { get; }
        bool? IsVehicleWeaponAllowed(int vehicleHandle, uint weapon);
        void OnPlayerSpawned();
    }
}
