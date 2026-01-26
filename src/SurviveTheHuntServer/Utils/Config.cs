using CitizenFX.Core;
using Newtonsoft.Json;
using SurviveTheHuntServer.Helpers;
using SurviveTheHuntShared;
using SurviveTheHuntShared.Core;
using System.Collections.Generic;

namespace SurviveTheHuntServer {
    public class ServerConfig : Config
    {
        private ServerConfig() : base()
        {

        }

        /// <summary>
        /// Creates a <see cref="Config"/> using the JSON file paths in the parameters.
        /// </summary>
        /// <param name="weaponConfigPath">Relative path to the weapon loadouts JSON file on the server.</param>
        /// <param name="vehicleConfigPath">Relative path to the vehicle list JSON file on the server.</param>
        /// <returns>A valid <see cref="Config"/> that can be sent to players.</returns>
        public static ServerConfig FromJsonFile(string weaponConfigPath = Constants.WeaponConfigPath, string vehicleConfigPath = Constants.VehicleConfigPath)
        {
            string resourceName = CitizenFX.Core.Native.API.GetCurrentResourceName();
            string loadoutsJson = CitizenFX.Core.Native.API.LoadResourceFile(resourceName, weaponConfigPath);
            string vehicleJson = CitizenFX.Core.Native.API.LoadResourceFile(resourceName, vehicleConfigPath);
            return FromJson(loadoutsJson, vehicleJson);
        }

        /// <summary>
        /// Creates a <see cref="Config"/> using the supplied weapon loadouts and vehicle list JSON strings.
        /// </summary>
        /// <param name="weaponsJson">A string containing valid weapon loadouts JSON, ideally loaded from <see cref="Constants.WeaponConfigPath"/>.</param>
        /// <param name="vehicleJson">A string containing valid vehicle names JSON, ideally loaded from <see cref="Constants.VehicleConfigPath"/>.</param>
        /// <returns>A valid <see cref="Config"/> that can be sent to players.</returns>
        public static ServerConfig FromJson(string weaponsJson, string vehicleJson)
        {
            ServerConfig config = new ServerConfig();
            config._weaponLoadouts = JsonConvert.DeserializeObject<TeamWeaponLoadoutsConfig>(weaponsJson);
            config._vehicleWhitelist = JsonConvert.DeserializeObject<VehicleWhitelistConfig>(vehicleJson);

            return config;
        }

        protected TeamWeaponLoadoutsConfig _weaponLoadouts = null;

        /// <summary>
        /// Weapon loadouts for each team loaded from the JSON file.
        /// </summary>
        public TeamWeaponLoadoutsConfig WeaponLoadouts { get => _weaponLoadouts; }

        /// <summary>
        /// Get names of all plugins that have configs.
        /// </summary>
        /// <returns></returns>
        public string[] GetAllPluginNames()
        {
            List<string> names = new List<string>();

            IEnumerable<string>[] allNames =
            {
                _weaponLoadouts.Plugins?.Keys ?? (IEnumerable<string>)(new string[0]),
                _vehicleWhitelist.Plugins?.Keys ?? (IEnumerable<string>)(new string[0]),
            };

            foreach (IEnumerable<string> pluginNames in allNames)
            {
                foreach(string name in pluginNames)
                {
                    if(!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }
            }

            return names.ToArray();
        }


        /// <summary>
        /// Converts the <see cref="Config"/> into serialized Cfx event parameters.
        /// </summary>
        /// <returns>A serialized representation of the <see cref="Config"/>.</returns>
        public Serialized Serialize(string activePlugin = null)
        {
            TeamWeaponLoadouts activeLoadouts = null;
            VehicleWhitelist activeVehicles = null;
            if(!string.IsNullOrWhiteSpace(activePlugin))
            {
                if(!WeaponLoadouts.Plugins.TryGetValue(activePlugin, out activeLoadouts))
                {
                    activeLoadouts = null;
                }

                if(!_vehicleWhitelist.Plugins.TryGetValue(activePlugin, out activeVehicles))
                {
                    activeVehicles = null;
                }
            }
            else
            {
                activePlugin = "";
            }

            if (activeLoadouts == null)
            {
                activeLoadouts = WeaponLoadouts;
            }

            if(activeVehicles == null)
            {
                activeVehicles = VehicleWhitelist;
            }

            // TODO: for now this will just choose the first loadout for each team
            return new Serialized(activePlugin ?? "", activeLoadouts.Hunters[0], activeLoadouts.Hunted[0], activeVehicles);
        }
    }

    public partial class MainScript
    {

        /// <summary>
        /// Broadcasts the config to all players.
        /// </summary>
        /// <param name="config">Active config</param>
        public void BroadcastConfig(ServerConfig config)
        {
            Debug.WriteLine("Sending serialized config to players");
            TriggerLatentClientEvent(Events.Client.ReceiveConfig, ServerConfig.ConfigBroadcastBytesPerSec, config.Serialize().EventParams);

            string[] pluginNames = config.GetAllPluginNames();
            foreach(string pluginName in pluginNames)
            {
                TriggerLatentClientEvent(Events.Client.ReceiveConfig, ServerConfig.ConfigBroadcastBytesPerSec, config.Serialize(pluginName).EventParams);
            }
        }

        /// <summary>
        /// Sends the config to a specific player.
        /// </summary>
        /// <param name="player">Player to send the config payload to.</param>
        /// <param name="config">Active config</param>
        public void BroadcastConfig(Player player, ServerConfig config)
        {
            Debug.WriteLine($"Sending serialized config to player {player.Name}");
            TriggerLatentClientEvent(player, Events.Client.ReceiveConfig, ServerConfig.ConfigBroadcastBytesPerSec, config.Serialize().EventParams);

            string[] pluginNames = config.GetAllPluginNames();
            foreach (string pluginName in pluginNames)
            {
                TriggerLatentClientEvent(player, Events.Client.ReceiveConfig, ServerConfig.ConfigBroadcastBytesPerSec, config.Serialize(pluginName).EventParams);
            }
        }
    }
}
