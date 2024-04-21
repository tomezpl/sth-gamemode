using Newtonsoft.Json;
using SurviveTheHuntShared.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace SurviveTheHuntShared.Core
{
    /// <summary>
    /// JSON binding for the weapon loadouts config.
    /// </summary>
    public class TeamWeaponLoadouts
    {
        /// <summary>
        /// Weapon loadouts for <see cref="Teams.Team.Hunters"/>.
        /// </summary>
        [JsonProperty("hunters")]
        public WeaponLoadout[] Hunters = new WeaponLoadout[0];

        /// <summary>
        /// Weapon loadouts for <see cref="Teams.Team.Hunted"/>.
        /// </summary>
        [JsonProperty("hunted")]
        public WeaponLoadout[] Hunted = new WeaponLoadout[0];

        /// <summary>
        /// A weapon loadout that can be applied to a team/player.
        /// </summary>
        public class WeaponLoadout
        {
            /// <summary>
            /// A map of weapons and ammo,
            /// ie. each key is a human-readable weapon ID (as seen in the keys for <see cref="Constants.WeaponHashes"/>)
            /// and each value is the ammo count to give for that weapon.
            /// </summary>
            [JsonProperty("weaponAmmo")]
            public Dictionary<string, ushort> WeaponAmmo = new Dictionary<string, ushort>();

            /// <summary>
            /// Serializes the weapon loadout into a binary representation to be sent in events.
            /// </summary>
            /// <returns>A byte array containing each key-value pair from <see cref="WeaponAmmo"/>, in the order [hash, ammo, hash, ammo, hash, ammo...]</returns>
            /// <remarks>In terms of size, each weapon hash is a <see cref="uint"/> and ammo count is a <see cref="ushort"/>.</remarks>
            public byte[] Serialize()
            {
                byte[] serialized = new byte[WeaponAmmo.Count * (sizeof(uint) + sizeof(ushort))];

                using (MemoryStream ms = new MemoryStream(serialized, true))
                {
                    foreach (KeyValuePair<string, ushort> weapon in WeaponAmmo)
                    {
                        // Weapon hash is first
                        ms.Write(BitConverter.GetBytes(Constants.WeaponHashes[weapon.Key]), 0, sizeof(uint));
                        // Ammo count is second
                        ms.Write(BitConverter.GetBytes(weapon.Value), 0, sizeof(ushort));
                    }
                }

                return serialized;
            }
        }
    }
}
