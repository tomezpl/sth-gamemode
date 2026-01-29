using CitizenFX.Core;
using System;

namespace SurviveTheHuntClient.Models
{
    /// <summary>
    /// Gamemode-related info for a given player. 
    /// </summary>
    /// <remarks>Doesn't necessarily have to be the hunted player, but currently it's the only use for this</remarks>
    public struct HuntPlayer
    {
        public Texture Mugshot;
        public int PlayerHandle;

        /// <summary>
        /// <see cref="Mugshot"/> texture generation time.
        /// </summary>
        public DateTime MugshotTimestamp;

        /// <summary>
        /// Time when <see cref="Mugshot"/> needs to be re-generated.
        /// </summary>
        public DateTime MugshotExpiry;

        public HuntPlayer(Player player, DateTime currentTime)
        {
            PlayerHandle = player.Handle;
            MugshotTimestamp = currentTime;
            MugshotExpiry = currentTime;
            Mugshot = null;
        }
    }
}
