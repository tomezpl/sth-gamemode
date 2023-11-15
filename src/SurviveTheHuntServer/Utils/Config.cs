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
    public class TeamWeaponLoadouts
    {
        public class WeaponLoadout
        {
            [JsonProperty("weaponAmmo")]
            public Dictionary<string, ushort> WeaponAmmo = new Dictionary<string, ushort>();

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

                        Debug.WriteLine($"{Constants.WeaponHashes[weapon.Key]:X}: {weapon.Value:X}");
                    }
                    Debug.WriteLine($"Written ${ms.Position} bytes");
                }

                Debug.WriteLine($"Encoded: {Encoding.UTF8.GetString(serialized)}");
                Debug.WriteLine($"{serialized.Length} bytes");

                return serialized;
            }
        }

        [JsonProperty("hunters")]
        public WeaponLoadout[] Hunters = new WeaponLoadout[0];

        [JsonProperty("hunted")]
        public WeaponLoadout[] Hunted = new WeaponLoadout[0];
    }

    public class Config
    {
        private TeamWeaponLoadouts _weaponLoadouts = null;
        public TeamWeaponLoadouts WeaponLoadouts { get => _weaponLoadouts; }

        /// <summary>
        /// Speed at which config data should be sent. 
        /// Not a scientific measurement but should be enough to broadcast the loadout config in its current form in under a second. Tweak as needed.
        /// </summary>
        public const int ConfigBroadcastBytesPerSec = 256;

        public const string ReceiveConfigEvent = "sth:receiveConfig";

        public struct Serialized
        {
            public byte[] WeaponsHunted;
            public byte[] WeaponsHunters;

            public Serialized(TeamWeaponLoadouts.WeaponLoadout huntersLoadout, TeamWeaponLoadouts.WeaponLoadout huntedLoadout)
            {
                WeaponsHunters = huntersLoadout.Serialize();
                WeaponsHunted = huntedLoadout.Serialize();
            }

            public object[] EventParams { get => new object[] { WeaponsHunters, WeaponsHunted }; }
        }

        public Config(string weaponConfigPath = Constants.WeaponConfigPath)
        {
            Init(weaponConfigPath);
        }

        public Config Init(string weaponConfigPath = Constants.WeaponConfigPath)
        {
            string loadoutsJson = CitizenFX.Core.Native.API.LoadResourceFile(CitizenFX.Core.Native.API.GetCurrentResourceName(), weaponConfigPath);
            _weaponLoadouts = JsonConvert.DeserializeObject<TeamWeaponLoadouts>(loadoutsJson);

            return this;
        }

        public Serialized Serialize()
        {
            // TODO: for now this will just choose the first loadout for each team
            return new Serialized(WeaponLoadouts.Hunters[0], WeaponLoadouts.Hunted[0]);
        }
    }
}

namespace SurviveTheHuntServer {
    public partial class MainScript
    {
        public void BroadcastConfig(Utils.Config config)
        {
            Debug.WriteLine("Sending serialized config to players");
            TriggerLatentClientEvent(Utils.Config.ReceiveConfigEvent, Utils.Config.ConfigBroadcastBytesPerSec, config.Serialize().EventParams);
        }

        public void BroadcastConfig(Player player, Utils.Config config)
        {
            Debug.WriteLine($"Sending serialized config to player {player.Name}");
            TriggerLatentClientEvent(player, Utils.Config.ReceiveConfigEvent, Utils.Config.ConfigBroadcastBytesPerSec, config.Serialize().EventParams);
        }
    }
}
