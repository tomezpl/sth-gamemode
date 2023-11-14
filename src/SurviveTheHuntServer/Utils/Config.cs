using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

            public string Serialize()
            {
                StringBuilder sb = new StringBuilder();

                foreach (KeyValuePair<string, ushort> weapon in WeaponAmmo)
                {
                    // Weapon hash is first
                    sb.Append(Constants.WeaponHashes[weapon.Key]);
                    sb.Append(":");
                    // Ammo count is second
                    sb.Append(weapon.Value);
                    sb.Append(";");
                }

                // Remove the trailing semicolon
                sb.Length -= 1;

                return sb.ToString();
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
            public string WeaponsHunted;
            public string WeaponsHunters;

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
