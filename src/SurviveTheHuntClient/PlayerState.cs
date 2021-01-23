using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient
{
    public class PlayerState
    {
        /// <summary>
        /// <para>Does the player have weapons?</para>
        /// <para>This is refreshed on each respawn.</para>
        /// </summary>
        public bool WeaponsGiven { get; set; } = false;

        /// <summary>
        /// <para>Was the player's death reported to the server yet?</para>
        /// <para>This is refreshed on each respawn.</para>
        /// </summary>
        public bool DeathReported { get; set; } = false;

        /// <summary>
        /// Last weapon the player had equipped.
        /// </summary>
        public Hash LastWeaponEquipped { get; set; } = new Hash();
    }
}
