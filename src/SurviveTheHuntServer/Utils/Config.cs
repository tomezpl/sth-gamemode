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

            public uint[] Serialize()
            {
                uint[] serialized = new uint[WeaponAmmo.Count * 2];

                ushort counter = 0;
                foreach(KeyValuePair<string, ushort> weapon in WeaponAmmo)
                {
                    // Weapon hash is first
                    serialized[counter++] = Constants.WeaponHashes[weapon.Key];
                    // Ammo count is second
                    serialized[counter++] = weapon.Value;
                }

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
        public readonly TeamWeaponLoadouts WeaponLoadouts;

        public struct Serialized
        {
            public uint[] WeaponsHunted;
            public uint[] WeaponsHunters;

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
