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

        protected VehicleWhitelistConfig _vehicleWhitelist = null;

        /// <summary>
        /// An allowlist of vehicles that can be spawned for the players.
        /// </summary>
        public VehicleWhitelist VehicleWhitelist { get => _vehicleWhitelist; }

        /// <summary>
        /// Speed at which config data should be sent. 
        /// Not a scientific measurement but should be enough to broadcast the loadout config in its current form in under a second. Tweak as needed.
        /// </summary>
        public const int ConfigBroadcastBytesPerSec = 1024 * 2;

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
            private readonly string PluginName;

            /// <summary>
            /// Creates a serialized representation of a weapons loadout config.
            /// </summary>
            /// <param name="huntersLoadout">Loadout for <see cref="Teams.Team.Hunters"/>.</param>
            /// <param name="huntedLoadout">Loadout for the <see cref="Teams.Team.Hunted"/>.</param>
            /// <param name="vehicleWhitelist">List of vehicle names that can be spawned for the hunt.</param>
            public Serialized(string pluginName, ISurviveTheHuntConfigSerializable huntersLoadout, ISurviveTheHuntConfigSerializable huntedLoadout, VehicleWhitelist vehicleWhitelist)
            {
                PluginName = pluginName;
                WeaponsHunters = huntersLoadout.Serialize();
                WeaponsHunted = huntedLoadout.Serialize();
                Vehicles = vehicleWhitelist.Serialize();
            }

            /// <summary>
            /// Serialized data to be passed as event parameters.
            /// </summary>
            public object[] EventParams { get => new object[] { PluginName, WeaponsHunters, WeaponsHunted, Vehicles }; }
            
            /// <summary>
            /// Helper method to deserialize a byte array of weapon&ammo data into WeaponAmmo objects.
            /// </summary>
            /// <param name="weapons">Byte array defining ammo count for each weapon.</param>
            /// <returns>An array of <see cref="Weapons.WeaponAmmo"/> for each weapon.</returns>
            private static Weapons.WeaponAmmo[] GetWeapons(byte[] weapons)
            {
                // Each weapon is uint hash followed by ushort ammo count, followed by a byte indicating how many attachments are included.
                // The weapon can then include those up to 255 comma-separated UTF-16 strings of attachment names.
                const uint minWeaponSize = sizeof(uint) + sizeof(ushort) + sizeof(byte);
                Weapons.WeaponAmmo[] output = null;

                byte[] buffer = new byte[minWeaponSize];
                using (MemoryStream ms = new MemoryStream(weapons, false))
                {
                    // Create the array based on the weapon count in the first byte
                    output = new Weapons.WeaponAmmo[ms.ReadByte()];
                    int index = 0;

                    while (ms.Position < ms.Length && index < output.Length)
                    {
                        // Zero the buffer.
                        Array.Clear(buffer, 0, buffer.Length);

                        // Read the weapon hash.
                        ms.Read(buffer, 0, sizeof(uint));
                        // Read the ammo count.
                        ms.Read(buffer, sizeof(uint), sizeof(ushort));
                        // Read the attachment count.
                        ms.Read(buffer, sizeof(ushort) + sizeof(uint), sizeof(byte));

                        int numAttachments = buffer[sizeof(ushort) + sizeof(uint)];
                        List<string> attachments = new List<string>(numAttachments);
                        if (numAttachments > 0)
                        {
                            string attachmentListString = "";
                            char lastChar = (char)0;
                            
                            for(uint i = 0; lastChar != ';' && attachments.Count <= numAttachments && ms.Position < ms.Length; i += 2)
                            {
                                lastChar = EncodingHelper.CharFromUnicode((byte)ms.ReadByte(), (byte)ms.ReadByte());
                                if(lastChar != ';')
                                {
                                    attachmentListString += lastChar;
                                }
                            }

                            attachments.AddRange(attachmentListString.Split(','));
                        }

                        // Store the weapon hash and ammo count in a WeaponAmmo object.
                        output[index++] = new Weapons.WeaponAmmo(BitConverter.ToUInt32(buffer, 0), BitConverter.ToUInt16(buffer, sizeof(uint)), attachments);
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
