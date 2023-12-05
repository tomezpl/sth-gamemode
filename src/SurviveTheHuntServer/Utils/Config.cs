using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Utils
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

    /// <summary>
    /// Parsed representation of the active gamemode config.
    /// </summary>
    public class Config
    {
        private TeamWeaponLoadouts _weaponLoadouts = null;

        /// <summary>
        /// Weapon loadouts for each team loaded from the JSON file.
        /// </summary>
        public TeamWeaponLoadouts WeaponLoadouts { get => _weaponLoadouts; }

        /// <summary>
        /// Speed at which config data should be sent. 
        /// Not a scientific measurement but should be enough to broadcast the loadout config in its current form in under a second. Tweak as needed.
        /// </summary>
        public const int ConfigBroadcastBytesPerSec = 64;

        /// <summary>
        /// Event name to use for sending the config. Needs to match in the client.
        /// </summary>
        public const string ReceiveConfigEvent = "sth:receiveConfig";

        /// <summary>
        /// Creates a new <see cref="Config"/> using the weapon loadouts JSON file pointed at by <paramref name="weaponConfigPath"/>.
        /// </summary>
        /// <param name="weaponConfigPath">Relative path to the weapon loadouts JSON file on the server.</param>
        public Config(string weaponConfigPath = Constants.WeaponConfigPath)
        {
            Init(weaponConfigPath);
        }

        /// <summary>
        /// Initialises this <see cref="Config"/> using the weapon loadouts JSON file pointed at by <paramref name="weaponConfigPath"/>.
        /// </summary>
        /// <param name="weaponConfigPath">Relative path to the weapon loadouts JSON file on the server.</param>
        public Config Init(string weaponConfigPath = Constants.WeaponConfigPath)
        {
            string loadoutsJson = CitizenFX.Core.Native.API.LoadResourceFile(CitizenFX.Core.Native.API.GetCurrentResourceName(), weaponConfigPath);
            _weaponLoadouts = JsonConvert.DeserializeObject<TeamWeaponLoadouts>(loadoutsJson);

            return this;
        }

        /// <summary>
        /// Converts the <see cref="Config"/> into serialized Cfx event parameters.
        /// </summary>
        /// <returns>A serialized representation of the <see cref="Config"/>.</returns>
        public Serialized Serialize()
        {
            // TODO: for now this will just choose the first loadout for each team
            return new Serialized(WeaponLoadouts.Hunters[0], WeaponLoadouts.Hunted[0]);
        }

        public struct Serialized
        {
            private readonly byte[] WeaponsHunted;
            private readonly byte[] WeaponsHunters;

            /// <summary>
            /// Creates a serialized representation of a weapons loadout config.
            /// </summary>
            /// <param name="huntersLoadout">Loadout for <see cref="Teams.Team.Hunters"/>.</param>
            /// <param name="huntedLoadout">Loadout for the <see cref="Teams.Team.Hunted"/>.</param>
            public Serialized(TeamWeaponLoadouts.WeaponLoadout huntersLoadout, TeamWeaponLoadouts.WeaponLoadout huntedLoadout)
            {
                WeaponsHunters = huntersLoadout.Serialize();
                WeaponsHunted = huntedLoadout.Serialize();
            }

            /// <summary>
            /// Serialized data to be passed as event parameters.
            /// </summary>
            public object[] EventParams { get => new object[] { WeaponsHunters, WeaponsHunted }; }
        }
    }
}

namespace SurviveTheHuntServer {
    public partial class MainScript
    {
        /// <summary>
        /// Broadcasts the config to all players.
        /// </summary>
        /// <param name="config">Active config</param>
        public void BroadcastConfig(Utils.Config config)
        {
            Debug.WriteLine("Sending serialized config to players");
            TriggerLatentClientEvent(Utils.Config.ReceiveConfigEvent, Utils.Config.ConfigBroadcastBytesPerSec, config.Serialize().EventParams);
        }

        /// <summary>
        /// Sends the config to a specific player.
        /// </summary>
        /// <param name="player">Player to send the config payload to.</param>
        /// <param name="config">Active config</param>
        public void BroadcastConfig(Player player, Utils.Config config)
        {
            Debug.WriteLine($"Sending serialized config to player {player.Name}");
            TriggerLatentClientEvent(player, Utils.Config.ReceiveConfigEvent, Utils.Config.ConfigBroadcastBytesPerSec, config.Serialize().EventParams);
        }
    }
}
