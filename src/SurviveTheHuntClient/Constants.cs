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
        /// <summary>
        /// Spawn location (Terminal, Port of Los Santos)
        /// </summary>
        public static readonly Vector3 DockSpawn = new Vector3 { X = 851.379f, Y = -3140.005f, Z = 5.900808f };

        /// <summary>
        /// How long the hunted player's radius stays at max. opacity on the radar (in seconds).
        /// </summary>
        public static readonly float HuntedBlipLifespan = 50f;

        /// <summary>
        /// How long it takes for the hunted player's radius to fade from max opacity to 0 (in seconds).
        /// </summary>
        public static readonly float HuntedBlipFadeoutTime = 5f;

        /// <summary>
        /// How long each feed post message stays on the screen (in seconds) assuming the duration multiplier of 1.
        /// </summary>
        /// <remarks>
        /// This was manually measured in gameplay.
        /// </remarks>
        public static readonly float FeedPostMessageDuration = 15f;

        /// <summary>
        /// Y-axis out of bounds limit for the allowed hunt area.
        /// </summary>
        public static readonly float OutOfBoundsYLimit = 1130f;

        /// <summary>
        /// How often should the player mugshot textures be regenerated.
        /// </summary>
        public static readonly TimeSpan MugshotGenerationInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Amount of time (in seconds) a player's death blip stays on the map.
        /// </summary>
        public const int DefaultDeathBlipLifespan = 5;

        /// <summary>
        /// Weapon loadouts for each team.
        /// </summary>
        public static Dictionary<Teams.Team, Weapons.WeaponAmmo[]> WeaponLoadouts = new Dictionary<Teams.Team, Weapons.WeaponAmmo[]>
        {
            {
                Teams.Team.Hunters, new Weapons.WeaponAmmo[0]
            },
            {
                Teams.Team.Hunted, new Weapons.WeaponAmmo[0]
            }
        };

        /// <summary>
        /// Vehicles that can be spawned.
        /// </summary>
        public static readonly VehicleHash[] Vehicles = new VehicleHash[]
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
        };

        /// <summary>
        /// Locations for spawning cars in the spawn area.
        /// </summary>
        public static readonly Coord[] CarSpawnPoints = new Coord[]
        {
            new Coord() { Position = new Vector3(818.21f, -3128.38f, 5.9f), Heading = 180f }, // 1
            new Coord() { Position = new Vector3(822.22f, -3129.37f, 5.9f), Heading = 180f }, // 2
            new Coord() { Position = new Vector3(826.22f, -3129.37f, 5.9f), Heading = 180f }, // 3
            new Coord() { Position = new Vector3(830.22f, -3129.37f, 5.9f), Heading = 180f }, // 4
            new Coord() { Position = new Vector3(834.22f, -3129.37f, 5.9f), Heading = 180f }, // 5
            new Coord() { Position = new Vector3(838.22f, -3129.37f, 5.9f), Heading = 180f }, // 6
            new Coord() { Position = new Vector3(842.22f, -3129.37f, 5.9f), Heading = 180f }, // 7
            new Coord() { Position = new Vector3(846.22f, -3129.37f, 5.9f), Heading = 180f }, // 8
            new Coord() { Position = new Vector3(850.22f, -3129.37f, 5.9f), Heading = 180f }, // 9
            new Coord() { Position = new Vector3(854.22f, -3129.37f, 5.9f), Heading = 180f }, // 10
            new Coord() { Position = new Vector3(858.22f, -3129.37f, 5.9f), Heading = 180f }, // 11
            new Coord() { Position = new Vector3(862.22f, -3129.37f, 5.9f), Heading = 180f }, // 12
            new Coord() { Position = new Vector3(866.22f, -3129.37f, 5.9f), Heading = 180f }, // 13
            new Coord() { Position = new Vector3(866.22f, -3143.73f, 5.9f), Heading = 0f }, // 14
            new Coord() { Position = new Vector3(862.22f, -3143.73f, 5.9f), Heading = 0f }, // 15
            new Coord() { Position = new Vector3(858.22f, -3143.73f, 5.9f), Heading = 0f }, // 16
            new Coord() { Position = new Vector3(854.22f, -3143.73f, 5.9f), Heading = 0f }, // 17
            new Coord() { Position = new Vector3(850.22f, -3143.73f, 5.9f), Heading = 0f }, // 18
            new Coord() { Position = new Vector3(846.22f, -3143.73f, 5.9f), Heading = 0f }, // 19
            new Coord() { Position = new Vector3(842.22f, -3143.73f, 5.9f), Heading = 0f }, // 20
            new Coord() { Position = new Vector3(838.22f, -3143.73f, 5.9f), Heading = 0f }, // 21
            new Coord() { Position = new Vector3(834.22f, -3143.73f, 5.9f), Heading = 0f }, // 22
            new Coord() { Position = new Vector3(830.22f, -3143.73f, 5.9f), Heading = 0f }, // 23
            new Coord() { Position = new Vector3(826.22f, -3143.73f, 5.9f), Heading = 0f }, // 24
            new Coord() { Position = new Vector3(822.22f, -3143.73f, 5.9f), Heading = 0f }, // 25
            new Coord() { Position = new Vector3(818.22f, -3143.73f, 5.9f), Heading = 0f }  // 26
        };

        /// <summary>
        /// The expected resource name for this gamemode.
        /// </summary>
        public const string ResourceName = "sth-gamemode";
    }
}
