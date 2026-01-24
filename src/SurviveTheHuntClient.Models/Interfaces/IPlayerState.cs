using CitizenFX.Core;
using SurviveTheHuntShared.Core;

namespace SurviveTheHuntClient.Interfaces
{
    public interface IPlayerState
    {
        /// <summary>
        /// The team the local player is on.
        /// </summary>
        Teams.Team Team { get; set; }

        /// <summary>
        /// Removes weapons from a player ped.
        /// </summary>
        /// <param name="playerPed">The player ped to remove weapons from.</param>
        /// <param name="takeAll">Should all weapons be removed, or just the ones given to the player by the gamemode?</param>
        void TakeAwayWeapons(Ped playerPed);
        void TakeAwayWeapons(int playerPedHandle);

        /// <summary>
        /// Is the player currently within the safezone bounds?
        /// </summary>
        bool IsInSafeZone { get; set; }

        /// <summary>
        /// Should the player's death be reported to the server?
        /// </summary>
        bool ReportDeathNextTick { get; set; }
    }
}
