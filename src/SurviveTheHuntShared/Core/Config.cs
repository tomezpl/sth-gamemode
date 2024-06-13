using SurviveTheHuntShared.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntShared.Core
{
    /// <summary>
    /// Parsed representation of the active gamemode config.
    /// </summary>
    public class Config
    {
        protected Config() { }

        protected TeamWeaponLoadouts _weaponLoadouts = null;
        protected VehicleWhitelist _vehicleWhitelist = null;

        /// <summary>
        /// Weapon loadouts for each team loaded from the JSON file.
        /// </summary>
        public TeamWeaponLoadouts WeaponLoadouts { get => _weaponLoadouts; }

        /// <summary>
        /// An allowlist of vehicles that can be spawned for the players.
        /// </summary>
        public VehicleWhitelist VehicleWhitelist { get => _vehicleWhitelist; }

        /// <summary>
        /// Speed at which config data should be sent. 
        /// Not a scientific measurement but should be enough to broadcast the loadout config in its current form in under a second. Tweak as needed.
        /// </summary>
        public const int ConfigBroadcastBytesPerSec = 1024 * 2;


        /// <summary>
        /// Converts the <see cref="Config"/> into serialized Cfx event parameters.
        /// </summary>
        /// <returns>A serialized representation of the <see cref="Config"/>.</returns>
        public Serialized Serialize()
        {
            // TODO: for now this will just choose the first loadout for each team
            return new Serialized(WeaponLoadouts.Hunters[0], WeaponLoadouts.Hunted[0], VehicleWhitelist);
        }

        public struct Deserialized
        {
            public Weapons.WeaponAmmo[] HuntersWeapons;
            public Weapons.WeaponAmmo[] HuntedWeapons;
            public VehicleWhitelist VehicleWhitelist;
        }

        public struct Serialized
        {
            private readonly byte[] WeaponsHunted;
            private readonly byte[] WeaponsHunters;
            private readonly string Vehicles;

            /// <summary>
            /// Creates a serialized representation of a weapons loadout config.
            /// </summary>
            /// <param name="huntersLoadout">Loadout for <see cref="Teams.Team.Hunters"/>.</param>
            /// <param name="huntedLoadout">Loadout for the <see cref="Teams.Team.Hunted"/>.</param>
            /// <param name="vehicleWhitelist">List of vehicle names that can be spawned for the hunt.</param>
            public Serialized(TeamWeaponLoadouts.WeaponLoadout huntersLoadout, TeamWeaponLoadouts.WeaponLoadout huntedLoadout, VehicleWhitelist vehicleWhitelist)
            {
                WeaponsHunters = huntersLoadout.Serialize();
                WeaponsHunted = huntedLoadout.Serialize();
                Vehicles = vehicleWhitelist.Serialize();
            }

            /// <summary>
            /// Serialized data to be passed as event parameters.
            /// </summary>
            public object[] EventParams { get => new object[] { WeaponsHunters, WeaponsHunted, Vehicles }; }
            
            /// <summary>
            /// Helper method to deserialize a byte array of weapon&ammo data into WeaponAmmo objects.
            /// </summary>
            /// <param name="weapons">Byte array defining ammo count for each weapon.</param>
            /// <returns>An array of <see cref="Weapons.WeaponAmmo"/> for each weapon.</returns>
            private static Weapons.WeaponAmmo[] GetWeapons(byte[] weapons)
            {
                Weapons.WeaponAmmo[] output = new Weapons.WeaponAmmo[weapons.Length / (sizeof(uint) + sizeof(ushort))];

                // Each weapon is uint hash followed by ushort ammo count.
                byte[] buffer = new byte[sizeof(uint) + sizeof(ushort)];
                using (MemoryStream ms = new MemoryStream(weapons, false))
                {
                    while (ms.Position < ms.Length)
                    {
                        // Zero the buffer.
                        Array.Clear(buffer, 0, buffer.Length);

                        // Get the weapon index based on the position in the byte array.
                        long index = ms.Position / (sizeof(uint) + sizeof(ushort));

                        // Read the weapon hash.
                        ms.Read(buffer, 0, sizeof(uint));
                        // Read the ammo count.
                        ms.Read(buffer, sizeof(uint), sizeof(ushort));

                        // Store the weapon hash and ammo count in a WeaponAmmo object.
                        output[index] = new Weapons.WeaponAmmo(BitConverter.ToUInt32(buffer, 0), BitConverter.ToUInt16(buffer, sizeof(uint)));
                    }
                }

                return output;
            }

            public static Deserialized Deserialize(byte[] weaponsHunters, byte[] weaponsHunted, string vehicles)
            {
                Weapons.WeaponAmmo[]
                    hunters = GetWeapons(weaponsHunters),
                    hunted = GetWeapons(weaponsHunted);

                string[] vehicleNames = vehicles.Split(';');

                Deserialized deserialized = new Deserialized
                {
                    HuntedWeapons = hunted,
                    HuntersWeapons = hunters,
                    VehicleWhitelist = new VehicleWhitelist() { Vehicles = vehicleNames[0] == vehicles ? new string[0] : vehicleNames }
                };

                return deserialized;
            }
        }
    }
}
