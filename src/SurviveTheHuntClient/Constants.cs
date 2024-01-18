using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient
{
    /// <summary>
    /// Constant values used throughout the gamemode.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Spawn location (Terminal, Port of Los Santos)
        /// </summary>
        public static readonly Vector3 DockSpawn = new Vector3 { X = 851.379f, Y = -3140.005f, Z = 5.900808f };

        /// <summary>
        /// How long the hunted player's radius stays at max. opacity on the radar (in seconds).
        /// </summary>
        public static readonly float HuntedBlipLifespan = 50f;

        /// <summary>
        /// How long it takes for the hunted player's radius to fade from max opacity to 0 (in seconds).
        /// </summary>
        public static readonly float HuntedBlipFadeoutTime = 5f;

        /// <summary>
        /// How long each feed post message stays on the screen (in seconds) assuming the duration multiplier of 1.
        /// </summary>
        /// <remarks>
        /// This was manually measured in gameplay.
        /// </remarks>
        public static readonly float FeedPostMessageDuration = 15f;

        /// <summary>
        /// Y-axis out of bounds limit for the allowed hunt area.
        /// </summary>
        public static readonly float OutOfBoundsYLimit = 1130f;

        /// <summary>
        /// How long before a ping should the mugshot be generated?
        /// </summary>
        public static readonly TimeSpan MugshotGenerationTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Amount of time (in seconds) a player's death blip stays on the map.
        /// </summary>
        public const int DefaultDeathBlipLifespan = 5;

        /// <summary>
        /// Weapon loadouts for each team.
        /// </summary>
        public static Dictionary<Teams.Team, Weapons.WeaponAmmo[]> WeaponLoadouts = new Dictionary<Teams.Team, Weapons.WeaponAmmo[]>
        {
            {
                Teams.Team.Hunters, new Weapons.WeaponAmmo[0]
            },
            {
                Teams.Team.Hunted, new Weapons.WeaponAmmo[0]
            }
        };

        /// <summary>
        /// The expected resource name for this gamemode.
        /// </summary>
        public const string ResourceName = "sth-gamemode";
    }
}
