using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Models.UI;

namespace SurviveTheHuntClient.Interfaces
{
    public interface IPlugin
    {
        string Name { get; }
        bool IsActive { get; set; }

        /// <summary>
        /// Is this a gamemode variant? The UI can use this to choose whether to create a menu item for it.
        /// </summary>
        bool IsGameMode { get; }

        /// <summary>
        /// The description to use for this plugin if it is a gamemode (see <see cref="IsGameMode"/>)
        /// </summary>
        string GameModeDescription { get; }

        void OnHuntStarted(IGameState gameState, IPlayerState playerState);
        void OnHuntEnded(IGameState gameState, IPlayerState playerState);

        /// <summary>
        /// This should always be called, regardless of whether the plugin is active or not.
        /// </summary>
        void OnResourceStarted();

        /// <summary>
        /// This should always be called, regardless of whether the plugin is active or not.
        /// </summary>
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
