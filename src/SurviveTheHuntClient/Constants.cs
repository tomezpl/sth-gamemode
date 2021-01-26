using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient
{
    /// <summary>
    /// Constant values used throughout the gamemode.
    /// </summary>
    public static class Constants
    {
        public static readonly Vec3 DockSpawn = new Vec3 { X = 851.379f, Y = -3140.005f, Z = 5.900808f };
        public static readonly float HuntedBlipLifespan = 50f;
        public static readonly float HuntedBlipFadeoutTime = 5f;

        public static readonly float ZLimit = 1130f;

        public static readonly Dictionary<Teams.Team, KeyValuePair<WeaponAsset, int>[]> WeaponLoadouts = new Dictionary<Teams.Team, KeyValuePair<WeaponAsset, int>[]>
        {
            {
                Teams.Team.Hunters, new KeyValuePair<WeaponAsset, int>[]
                {
                    new KeyValuePair<WeaponAsset, int>(new WeaponAsset(WeaponHash.PistolMk2), 9999),
                    new KeyValuePair<WeaponAsset, int>(new WeaponAsset(WeaponHash.PumpShotgunMk2), 9999)
                }
            },
            {
                Teams.Team.Hunted, new KeyValuePair<WeaponAsset, int>[]
                {
                    new KeyValuePair<WeaponAsset, int>(new WeaponAsset(WeaponHash.APPistol), 9999),
                    new KeyValuePair<WeaponAsset, int>(new WeaponAsset(WeaponHash.CarbineRifleMk2), 9999),
                    new KeyValuePair<WeaponAsset, int>(new WeaponAsset(WeaponHash.StickyBomb), 25),
                    new KeyValuePair<WeaponAsset, int>(new WeaponAsset(WeaponHash.RPG), 25),
                    new KeyValuePair<WeaponAsset, int>(new WeaponAsset(WeaponHash.AssaultShotgun), 9999)
                }
            }
        };
    }
}
