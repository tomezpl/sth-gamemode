using SurviveTheHuntShared.Utils;
using System;
using System.Collections.Generic;
using System.IO;

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
            public Weapons.WeaponAmmo[][] HuntersWeapons;
            public Weapons.WeaponAmmo[][] HuntedWeapons;
            public VehicleWhitelist VehicleWhitelist;
        }

        public struct Serialized
        {
            private readonly byte[] WeaponsHunted;
            private readonly byte[] WeaponsHunters;
            private readonly byte LoadoutsHuntedCount;
            private readonly byte LoadoutsHuntersCount;
            private readonly string Vehicles;
            private readonly string PluginName;

            /// <summary>
            /// Creates a serialized representation of a weapons loadout config.
            /// </summary>
            /// <param name="huntersLoadout">Loadout for <see cref="Teams.Team.Hunters"/>.</param>
            /// <param name="huntedLoadout">Loadout for the <see cref="Teams.Team.Hunted"/>.</param>
            /// <param name="vehicleWhitelist">List of vehicle names that can be spawned for the hunt.</param>
            public Serialized(string pluginName, ISurviveTheHuntConfigSerializable huntersLoadout, ISurviveTheHuntConfigSerializable huntedLoadout, VehicleWhitelist vehicleWhitelist) : this(pluginName, new List<ISurviveTheHuntConfigSerializable> { huntersLoadout }, new List<ISurviveTheHuntConfigSerializable> { huntedLoadout}, vehicleWhitelist)
            {
            }

            /// <summary>
            /// Creates a serialized representation of multiple weapons loadout configs.
            /// </summary>
            /// <param name="huntersLoadouts">Loadouts for <see cref="Teams.Team.Hunters"/>.</param>
            /// <param name="huntedLoadouts">Loadouts for the <see cref="Teams.Team.Hunted"/>.</param>
            /// <param name="vehicleWhitelist">List of vehicle names that can be spawned for the hunt.</param>
            public Serialized(string pluginName, ICollection<ISurviveTheHuntConfigSerializable> huntersLoadouts, ICollection<ISurviveTheHuntConfigSerializable> huntedLoadouts, VehicleWhitelist vehicleWhitelist)
            {
                PluginName = pluginName;
                WeaponsHunted = SerializeCollection(huntedLoadouts, out LoadoutsHuntedCount);
                WeaponsHunters = SerializeCollection(huntersLoadouts, out LoadoutsHuntersCount);
                Vehicles = vehicleWhitelist.Serialize();
            }

            private static byte[] SerializeCollection(ICollection<ISurviveTheHuntConfigSerializable> collection, out byte count)
            {
                if(collection.Count > byte.MaxValue)
                {
                    throw new ArgumentException($"Collection can only have max. {byte.MaxValue} elements.", nameof(collection));
                }

                count = (byte)collection.Count;
                
                ulong headerSize = 0
                    // count
                    + sizeof(byte)
                    // int byte count for each element
                    + (sizeof(int) * (ulong)count);

                List<byte[]> elements = new List<byte[]>(count);
                ulong totalSize = 0;
                foreach(ISurviveTheHuntConfigSerializable serializable in collection)
                {
                    byte[] serialized = serializable.Serialize();
                    elements.Add(serialized);
                    totalSize += (ulong)serialized.Length;
                }

                using(MemoryStream ms = new MemoryStream(new byte[totalSize + headerSize]))
                {
                    // Write the header
                    ms.WriteByte(count);
                    // Sizes for each element
                    foreach (byte[] serialized in elements)
                    {
                        byte[] sizeBytes = BitConverter.GetBytes(serialized.Length);
                        ms.Write(sizeBytes, 0, sizeBytes.Length);
                    }

                    // Write the elements
                    foreach (byte[] serialized in elements)
                    {
                        ms.Write(serialized, 0, serialized.Length);
                    }

                    return ms.ToArray();
                }
            }

            /// <summary>
            /// Serialized data to be passed as event parameters.
            /// </summary>
            public object[] EventParams { get => new object[] { PluginName, WeaponsHunters, WeaponsHunted, Vehicles }; }

            public delegate void DebugWriteLineDelegate(string content);

            /// <summary>
            /// Helper method to deserialize a byte array of weapon&ammo data into WeaponAmmo objects.
            /// </summary>
            /// <param name="loadouts">Byte array defining ammo count for each weapon in all loadouts.</param>
            /// <returns>An array of <see cref="Weapons.WeaponAmmo"/> for each weapon in all loadouts.</returns>
            private static Weapons.WeaponAmmo[][] GetWeapons(byte[] loadouts, DebugWriteLineDelegate logger = null)
            {
                using (MemoryStream rootMs = new MemoryStream(loadouts, false))
                {
                    byte loadoutCount = (byte)rootMs.ReadByte();

                    if(logger != null)
                    {
                        logger($"Got {loadoutCount} loadouts");
                    }

                    List<Weapons.WeaponAmmo[]> output = new List<Weapons.WeaponAmmo[]>(loadoutCount);       

                    ulong headerSize = sizeof(byte) + (sizeof(int) * (ulong)loadoutCount);

                    if(logger != null)
                    {
                        logger($"Header is {headerSize} bytes");
                    }

                    ulong loadoutBytesRead = headerSize;

                    for(byte loadoutIndex = 0; loadoutIndex < loadoutCount; loadoutIndex++)
                    {
                        int loadoutSizeBytes = BitConverter.ToInt32(loadouts, sizeof(byte) + (sizeof(int) * loadoutIndex));

                        if (logger != null)
                        {
                            logger($"Loadout {loadoutIndex} is {loadoutSizeBytes} bytes. Starting from byte {loadoutBytesRead}");
                        }

                        // Each weapon is uint hash followed by ushort ammo count, followed by a byte indicating how many attachments are included.
                        // The weapon can then include those up to 255 comma-separated UTF-16 strings of attachment names.
                        const uint minWeaponSize = sizeof(uint) + sizeof(ushort) + sizeof(byte);
                        Weapons.WeaponAmmo[] currentLoadout = null;

                        byte[] buffer = new byte[minWeaponSize];
                        using (MemoryStream ms = new MemoryStream(loadouts, (int)loadoutBytesRead, loadoutSizeBytes, false))
                        {
                            // Create the array based on the weapon count in the first byte
                            currentLoadout = new Weapons.WeaponAmmo[ms.ReadByte()];
                            if(logger != null)
                            {
                                logger($"loadout {loadoutIndex} has {currentLoadout.Length} weapons");
                            }
                            int index = 0;

                            while (ms.Position < ms.Length && index < currentLoadout.Length)
                            {
                                if(logger != null)
                                {
                                    logger($"stream position: {ms.Position}");
                                }

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

                                    for (uint i = 0; lastChar != ';' && attachments.Count <= numAttachments && ms.Position < ms.Length; i += 2)
                                    {
                                        lastChar = EncodingHelper.CharFromUnicode((byte)ms.ReadByte(), (byte)ms.ReadByte());
                                        if (lastChar != ';')
                                        {
                                            attachmentListString += lastChar;
                                        }
                                    }

                                    attachments.AddRange(attachmentListString.Split(','));
                                }

                                // Store the weapon hash and ammo count in a WeaponAmmo object.
                                currentLoadout[index++] = new Weapons.WeaponAmmo(BitConverter.ToUInt32(buffer, 0), BitConverter.ToUInt16(buffer, sizeof(uint)), attachments);
                                if (logger != null)
                                {
                                    logger($"Got weapon with hash {currentLoadout[index - 1].Hash:X}, ammo {currentLoadout[index - 1].Ammo} and {currentLoadout[index - 1].Attachments?.Length ?? 0} attachments");
                                }
                            }
                        }

                        output.Add(currentLoadout);

                        loadoutBytesRead += (ulong)loadoutSizeBytes;
                    }

                    return output.ToArray();
                }
            }

            public static Deserialized Deserialize(byte[] weaponsHunters, byte[] weaponsHunted, string vehicles, DebugWriteLineDelegate logger = null)
            {
                Weapons.WeaponAmmo[][]
                    hunters = GetWeapons(weaponsHunters, logger),
                    hunted = GetWeapons(weaponsHunted, logger);

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
