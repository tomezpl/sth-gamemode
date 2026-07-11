using CitizenFX.Core;
using SurviveTheHuntShared.Core;
using SurviveTheHuntShared.Utils;
using System.Collections.Generic;

namespace SurviveTheHuntClient
{
    /// <summary>
    /// Constant values used throughout the gamemode.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// How long the hunted player's radius stays at max. opacity on the radar (in seconds).
        /// </summary>
        public const float HuntedBlipLifespan = 50f;

        /// <summary>
        /// How long it takes for the hunted player's radius to fade from max opacity to 0 (in seconds).
        /// </summary>
        public const float HuntedBlipFadeoutTime = 5f;

        /// <summary>
        /// How long each feed post message stays on the screen (in seconds) assuming the duration multiplier of 1.
        /// </summary>
        /// <remarks>
        /// This was manually measured in gameplay.
        /// </remarks>
        public const float FeedPostMessageDuration = 15f;

        /// <summary>
        /// Weapon loadouts for each team.
        /// </summary>
        public static Dictionary<string, Dictionary<Teams.Team, Weapons.WeaponAmmo[][]>> WeaponLoadouts = new Dictionary<string, Dictionary<Teams.Team, Weapons.WeaponAmmo[][]>>
        {
            {
                "", new Dictionary<Teams.Team, Weapons.WeaponAmmo[][]>
                {
                    {Teams.Team.Hunters, new Weapons.WeaponAmmo[][] {new Weapons.WeaponAmmo[0] } },
                    {Teams.Team.Hunted, new Weapons.WeaponAmmo[][] {new Weapons.WeaponAmmo[0] } }
                }
            }
        };

        public static Dictionary<Teams.Team, Weapons.WeaponAmmo[][]> GetModeWeaponLoadouts(string mode)
        {
            if(WeaponLoadouts.TryGetValue(mode, out Dictionary<Teams.Team, Weapons.WeaponAmmo[][]> loadouts))
            {
                return loadouts;
            }

            return WeaponLoadouts[""];
        }

        /// <summary>
        /// Vehicles that can be spawned (per plugin - empty string for default).
        /// </summary>
        public static Dictionary<string, VehicleHash[]> Vehicles = new Dictionary<string, VehicleHash[]>
        {
            {
                "", new VehicleHash[]
                {
                    VehicleHash.Adder,
                    VehicleHash.Banshee2,
                    VehicleHash.Bati,
                    VehicleHash.BestiaGTS,
                    VehicleHash.BfInjection,
                    VehicleHash.Bifta,
                    VehicleHash.Blista,
                    VehicleHash.Bmx,
                    VehicleHash.Brawler,
                    VehicleHash.Buffalo2,
                    VehicleHash.Bullet,
                    VehicleHash.Carbonizzare,
                    VehicleHash.Casco,
                    VehicleHash.Cheetah2,
                    VehicleHash.Comet3,
                    VehicleHash.Comet2,
                    VehicleHash.Coquette3,
                    VehicleHash.Dilettante,
                    VehicleHash.Dubsta3,
                    VehicleHash.Dukes2,
                    VehicleHash.Elegy2,
                    VehicleHash.Exemplar,
                    VehicleHash.EntityXF,
                    VehicleHash.Fugitive,
                    VehicleHash.Furoregt,
                    VehicleHash.Fusilade,
                    VehicleHash.Gauntlet,
                    VehicleHash.Hotknife,
                    VehicleHash.Insurgent,
                    VehicleHash.Khamelion,
                    VehicleHash.Kuruma,
                    VehicleHash.Massacro,
                    VehicleHash.Mesa3,
                    VehicleHash.Nightshade,
                    VehicleHash.Ninef,
                    VehicleHash.Panto,
                    VehicleHash.Police,
                    VehicleHash.Police2,
                    VehicleHash.RapidGT,
                    VehicleHash.Riot,
                    VehicleHash.Rocoto,
                    VehicleHash.SabreGT2,
                    VehicleHash.Seven70,
                    VehicleHash.Sentinel2,
                    VehicleHash.Shotaro,
                    VehicleHash.Specter2,
                    VehicleHash.StingerGT,
                    VehicleHash.SultanRS,
                    VehicleHash.T20,
                    VehicleHash.Voltic2,
                    VehicleHash.Zentorno,
                    VehicleHash.ZType
                }
            }
        };

        public static VehicleHash[] GetModeVehicles(string mode)
        {
            if (Vehicles.TryGetValue(mode, out VehicleHash[] vehicles))
            {
                return vehicles;
            }

            return Vehicles[""];
        }

        public static class RelationshipGroups
        {
            public const string Hunters = "hunter_group";
            public const string Hunted = "hunted_group";
        }

        public enum TeleportPlayerStage
        {
            None,
            ZoomOut,
            ZoomIn
        }
    }
}
