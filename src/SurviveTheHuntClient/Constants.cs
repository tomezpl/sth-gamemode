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
        public static VehicleHash[] Vehicles = new VehicleHash[]
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

        public static class RelationshipGroups
        {
            public const string Hunters = "hunter_group";
            public const string Hunted = "hunted_group";
        }

        /// <summary>
        /// Spawnpoints for FFA - the first 3 floats are pos and the last one is heading
        /// </summary>
        public static readonly float[][] SpawnPoints =
        {
            new float[] { 1275.807f, -1722.85f, 54.65497f, 207.5776f},
            new float[] { 808.9508f, -1049.09f, 28.19691f, 60.07873f},
            new float[] { -61.14466f, -1093.862f, 26.48435f, 72.3289f },
            new float[] { -693.2744f, -611.2674f, 32.14494f, 176.6349f },
            new float[] { -1040.437f, -761.6415f, 19.83862f, 158.9809f },
            new float[] { -1178.053f, -891.3905f, 13.76581f, 281.4323f },
            new float[] { -1357.544f, -792.4648f, 20.24218f, 139.9525f },
            new float[] { -1628.161f, -1034.797f, 13.15362f, 146.2058f },
            new float[] { -1663.532f, -536.0691f, 35.31442f, 144.9864f },
            new float[] { -1551.294f, 209.9977f, 58.85605f, 297.2684f },
            new float[] { -640.2813f, 296.6248f, 82.4562f, 192.3964f },
            new float[] { 160.7246f, 173.9481f, 105.9196f, 69.03011f },
            new float[] { 412.0865f, 314.3899f, 103.0212f, 204.4563f },
            new float[] { 924.4755f, 46.93041f, 81.10635f, 72.73734f },
            new float[] { 473.4945f, -231.9204f, 53.78836f, 259.3065f },
            new float[] { 215.6438f, -918.4568f, 30.69199f, 324.021f },
            new float[] { 128.0095f, -1287.312f, 29.28273f, 175.2717f },
            new float[] { -609.533f, -1609.641f, 26.89828f, 9.604936f }
        };
    }
}
