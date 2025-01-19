using CitizenFX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SurviveTheHuntShared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using SharedConstants = SurviveTheHuntShared.Constants;

namespace SurviveTheHuntServer.Helpers
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
        public class WeaponLoadout : ISurviveTheHuntConfigSerializable
        {
            /// <summary>
            /// A map of weapons and ammo,
            /// ie. each key is a human-readable weapon ID (as seen in the keys for <see cref="Constants.WeaponHashes"/>)
            /// and each value is the ammo count to give for that weapon.
            /// </summary>
            //[JsonProperty("weaponAmmo")]
            //[JsonIgnore]
            [JsonConverter(typeof(WeaponAmmoConverter))]
            public Dictionary<string, WeaponInfo> WeaponAmmo = new Dictionary<string, WeaponInfo>();

            public class WeaponAmmoConverter : JsonConverter<Dictionary<string, WeaponInfo>>
            {
                public override Dictionary<string, WeaponInfo> ReadJson(JsonReader reader, Type objectType, Dictionary<string, WeaponInfo> existingValue, bool hasExistingValue, JsonSerializer serializer)
                {
                    Dictionary<string, dynamic> data = serializer.Deserialize<Dictionary<string, dynamic>>(reader);

                    Dictionary<string, WeaponInfo> parsed = new Dictionary<string, WeaponInfo>();

                    foreach(KeyValuePair<string, object> kvp in data)
                    {
                        try
                        {
                            if (kvp.Value.GetType().Name.StartsWith("Int") || kvp.Value.GetType().Name.StartsWith("UInt"))
                            {
                                JToken value = JValue.FromObject(kvp.Value);
                                parsed.Add(kvp.Key, new WeaponInfo
                                {
                                    Ammo = value.ToObject<ushort>(),
                                    Attachments = new string[0]
                                });
                            } else
                            {
                                JToken obj = JValue.FromObject(kvp.Value);
                                parsed.Add(kvp.Key, obj.ToObject<WeaponInfo>());
                            }
                        } catch(Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }

                    foreach(WeaponInfo key in parsed.Values)
                    {
                        Debug.WriteLine(key.Ammo.ToString());
                    }

                    return parsed;
                }

                public override void WriteJson(JsonWriter writer, Dictionary<string, WeaponInfo> value, JsonSerializer serializer)
                {
                    throw new NotImplementedException();
                }
            }

            public struct WeaponInfo
            {
                [JsonProperty("ammo")]
                public ushort Ammo { get; set; }

                [JsonProperty("attachments")]
                public string[] Attachments { get; set; }
            }

            [OnSerialized]
            internal void OnSerialized(StreamingContext context)
            {
                
            }

            /// <summary>
            /// Serializes the weapon loadout into a binary representation to be sent in events.
            /// </summary>
            /// <returns>A byte array containing each key-value pair from <see cref="WeaponAmmo"/>, in the order [hash, ammo, hash, ammo, hash, ammo...]. The first byte is the number of weapons.</returns>
            /// <remarks>In terms of size, each weapon hash is a <see cref="uint"/> and ammo count is a <see cref="ushort"/>.</remarks>
            public byte[] Serialize()
            {
                // Add a byte at the start for the weapon count.
                using (MemoryStream ms = new MemoryStream(WeaponAmmo.Count * (sizeof(uint) + sizeof(ushort)) + sizeof(byte)))
                {
                    // Write the weapon count
                    ms.WriteByte((byte)WeaponAmmo.Count);

                    foreach (KeyValuePair<string, WeaponInfo> weapon in WeaponAmmo)
                    {
                        // Weapon hash is first
                        ms.Write(BitConverter.GetBytes(SharedConstants.WeaponHashes[weapon.Key]), 0, sizeof(uint));
                        // Ammo count is second
                        ms.Write(BitConverter.GetBytes(weapon.Value.Ammo), 0, sizeof(ushort));
                        // Write attachment count (can be 0 but needs to be present)
                        ms.WriteByte((byte)weapon.Value.Attachments.Length);

                        if (weapon.Value.Attachments.Length > 0)
                        {
                            string commaSeparated = weapon.Value.Attachments[0];
                            for (int i = 1; i < weapon.Value.Attachments.Length; i++)
                            {
                                commaSeparated += $",{weapon.Value.Attachments[i]}";
                            }
                            byte[] attachmentStringBytes = Encoding.ASCII.GetBytes(commaSeparated);
                            ms.Write(attachmentStringBytes, 0, attachmentStringBytes.Length);
                            ms.WriteByte((byte)';');
                        }
                    }

                    return ms.ToArray();
                }
            }
        }
    }
}
