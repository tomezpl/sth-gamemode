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

                foreach(KeyValuePair<string, ushort> weapon in WeaponAmmo)
                {
                    // Weapon hash is first
                    sb.Append(Constants.WeaponHashes[weapon.Key]);
                    sb.Append(":");
                    // Ammo count is second
                    sb.Append(weapon.Value);
                    sb.Append(";");
                }

                sb.Append("\b");

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
        public readonly TeamWeaponLoadouts WeaponLoadouts;

        public struct Serialized
        {
            public string WeaponsHunted;
            public string WeaponsHunters;

            public Serialized(TeamWeaponLoadouts.WeaponLoadout huntersLoadout, TeamWeaponLoadouts.WeaponLoadout huntedLoadout)
            {
                WeaponsHunters = huntersLoadout.Serialize();
                WeaponsHunted = huntedLoadout.Serialize();
            }
        }

        public Config(string weaponConfigPath = Constants.WeaponConfigPath)
        {
            string loadoutsJson = CitizenFX.Core.Native.API.LoadResourceFile(CitizenFX.Core.Native.API.GetCurrentResourceName(), weaponConfigPath);

            WeaponLoadouts = JsonConvert.DeserializeObject<TeamWeaponLoadouts>(loadoutsJson);
        }

        public Serialized Serialize()
        {
            // TODO: for now this will just choose the first loadout for each team
            return new Serialized(WeaponLoadouts.Hunters[0], WeaponLoadouts.Hunted[0]);
        }
    }
}
