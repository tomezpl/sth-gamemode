﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer
{
    /// <remarks>TODO: These need to be overrideable through convars!</remarks>
    public static class Constants
    {
        /// <summary>
        /// Expected resource name for the gamemode on the server.
        /// </summary>
        public const string ResourceName = "sth-gamemode";

        /// <summary>
        /// Amount of time the hunt should last for.
        /// </summary>
        public static readonly TimeSpan HuntDuration = TimeSpan.FromMinutes(24);

        /// <summary>
        /// How often the hunted player should be pinged on the map.
        /// </summary>
        public static readonly TimeSpan HuntedPingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// How often the server should sync time with clients.
        /// </summary>
        public static readonly TimeSpan TimeSyncInterval = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Path to the team weapon loadout config file, relative to the resource root.
        /// </summary>
        public const string WeaponConfigPath = "configs/team_loadouts.json";

        /// <summary>
        /// GTA V Weapon hashes from <see href="https://wiki.rage.mp/index.php?title=Weapons"/>.
        /// </summary>
        public static readonly Dictionary<string, uint> WeaponHashes = new Dictionary<string, uint>
        // Extracted using the following, extremely cursed JS:
        // console.log("new Dictionary<string, uint>\n{\n" + [...document.getElementById("Melee").parentElement.parentElement.children].filter((child) => child.nodeName === "UL").flatMap((weaponCategory) => [...weaponCategory.children].map(({innerText: weapon}) => ({id: weapon.split("\n")[0].substring("ID:".length).trim(), hash: weapon.split("\n")[2].substring(weapon.split("\n")[2].indexOf(" ")).trim()}))).map(({id, hash}) => `\t{"${id}", ${hash}}`).join(",\n") + "\n};"); 
        {
            {"weapon_dagger", 0x92A27487},
            {"weapon_bat", 0x958A4A8F},
            {"weapon_bottle", 0xF9E6AA4B},
            {"weapon_crowbar", 0x84BD7BFD},
            {"weapon_unarmed", 0xA2719263},
            {"weapon_flashlight", 0x8BB05FD7},
            {"weapon_golfclub", 0x440E4788},
            {"weapon_hammer", 0x4E875F73},
            {"weapon_hatchet", 0xF9DCBF2D},
            {"weapon_knuckle", 0xD8DF3C3C},
            {"weapon_knife", 0x99B507EA},
            {"weapon_machete", 0xDD5DF8D9},
            {"weapon_switchblade", 0xDFE37640},
            {"weapon_nightstick", 0x678B81B1},
            {"weapon_wrench", 0x19044EE0},
            {"weapon_battleaxe", 0xCD274149},
            {"weapon_poolcue", 0x94117305},
            {"weapon_stone_hatchet", 0x3813FC08},
            {"weapon_candycane", 0x6589186A},
            {"weapon_pistol", 0x1B06D571},
            {"weapon_pistol_mk2", 0xBFE256D4},
            {"weapon_combatpistol", 0x5EF9FEC4},
            {"weapon_appistol", 0x22D8FE39},
            {"weapon_stungun", 0x3656C8C1},
            {"weapon_pistol50", 0x99AEEB3B},
            {"weapon_snspistol", 0xBFD21232},
            {"weapon_snspistol_mk2", 0x88374054},
            {"weapon_heavypistol", 0xD205520E},
            {"weapon_vintagepistol", 0x83839C4},
            {"weapon_flaregun", 0x47757124},
            {"weapon_marksmanpistol", 0xDC4DB296},
            {"weapon_revolver", 0xC1B3C3D1},
            {"weapon_revolver_mk2", 0xCB96392F},
            {"weapon_doubleaction", 0x97EA20B8},
            {"weapon_raypistol", 0xAF3696A1},
            {"weapon_ceramicpistol", 0x2B5EF5EC},
            {"weapon_navyrevolver", 0x917F6C8C},
            {"weapon_gadgetpistol", 0x57A4368C},
            {"weapon_stungun_mp", 0x45CD9CF3},
            {"weapon_pistolxm3", 0x1BC4FDB9},
            {"weapon_microsmg", 0x13532244},
            {"weapon_smg", 0x2BE6766B},
            {"weapon_smg_mk2", 0x78A97CD0},
            {"weapon_assaultsmg", 0xEFE7E2DF},
            {"weapon_combatpdw", 0x0A3D4D34},
            {"weapon_machinepistol", 0xDB1AA450},
            {"weapon_minismg", 0xBD248B55},
            {"weapon_raycarbine", 0x476BF155},
            {"weapon_tecpistol", 0x14E5AFD5},
            {"weapon_pumpshotgun", 0x1D073A89},
            {"weapon_pumpshotgun_mk2", 0x555AF99A},
            {"weapon_sawnoffshotgun", 0x7846A318},
            {"weapon_assaultshotgun", 0xE284C527},
            {"weapon_bullpupshotgun", 0x9D61E50F},
            {"weapon_musket", 0xA89CB99E},
            {"weapon_heavyshotgun", 0x3AABBBAA},
            {"weapon_dbshotgun", 0xEF951FBB},
            {"weapon_autoshotgun", 0x12E82D3D},
            {"weapon_combatshotgun", 0x5A96BA4},
            {"weapon_assaultrifle", 0xBFEFFF6D},
            {"weapon_assaultrifle_mk2", 0x394F415C},
            {"weapon_carbinerifle", 0x83BF0278},
            {"weapon_carbinerifle_mk2", 0xFAD1F1C9},
            {"weapon_advancedrifle", 0xAF113F99},
            {"weapon_specialcarbine", 0xC0A3098D},
            {"weapon_specialcarbine_mk2", 0x969C3D67},
            {"weapon_bullpuprifle", 0x7F229F94},
            {"weapon_bullpuprifle_mk2", 0x84D6FAFD},
            {"weapon_compactrifle", 0x624FE830},
            {"weapon_militaryrifle", 0x9D1F17E6},
            {"weapon_heavyrifle", 0xC78D71B4},
            {"weapon_tacticalrifle", 0xD1D5F52B},
            {"weapon_mg", 0x9D07F764},
            {"weapon_combatmg", 0x7FD62962},
            {"weapon_combatmg_mk2", 0xDBBD7280},
            {"weapon_gusenberg", 0x61012683},
            {"weapon_sniperrifle", 0x05FC3C11},
            {"weapon_heavysniper", 0x0C472FE2},
            {"weapon_heavysniper_mk2", 0xA914799},
            {"weapon_marksmanrifle", 0xC734385A},
            {"weapon_marksmanrifle_mk2", 0x6A6C02E0},
            {"weapon_precisionrifle", 0x6E7DDDEC},
            {"weapon_rpg", 0xB1CA77B1},
            {"weapon_grenadelauncher", 0xA284510B},
            {"weapon_grenadelauncher_smoke", 0x4DD2DC56},
            {"weapon_minigun", 0x42BF8A85},
            {"weapon_firework", 0x7F7497E5},
            {"weapon_railgun", 0x6D544C99},
            {"weapon_hominglauncher", 0x63AB0442},
            {"weapon_compactlauncher", 0x0781FE4A},
            {"weapon_rayminigun", 0xB62D1F67},
            {"weapon_emplauncher", 0xDB26713A},
            {"weapon_railgunxm3", 0xFEA23564},
            {"weapon_grenade", 0x93E220BD},
            {"weapon_bzgas", 0xA0973D5E},
            {"weapon_molotov", 0x24B17070},
            {"weapon_stickybomb", 0x2C3731D9},
            {"weapon_proxmine", 0xAB564B93},
            {"weapon_snowball", 0x787F0BB},
            {"weapon_pipebomb", 0xBA45E8B8},
            {"weapon_ball", 0x23C9F95C},
            {"weapon_smokegrenade", 0xFDBC8A50},
            {"weapon_flare", 0x497FACC3},
            {"weapon_acidpackage", 0xF7F1E25E},
            {"weapon_petrolcan", 0x34A67B97},
            {"gadget_parachute", 0xFBAB5776},
            {"weapon_fireextinguisher", 0x060EC506},
            {"weapon_hazardcan", 0xBA536372},
            {"weapon_fertilizercan", 0x184140A1}
        };
    }
}
