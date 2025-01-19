using CitizenFX.Core;
using Newtonsoft.Json;
using SurviveTheHuntServer.Helpers;
using SurviveTheHuntShared;
using SurviveTheHuntShared.Core;

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
            config._weaponLoadouts = JsonConvert.DeserializeObject<TeamWeaponLoadouts>(weaponsJson);
            config._vehicleWhitelist = JsonConvert.DeserializeObject<VehicleWhitelist>(vehicleJson);

            return config;
        }

        protected TeamWeaponLoadouts _weaponLoadouts = null;

        /// <summary>
        /// Weapon loadouts for each team loaded from the JSON file.
        /// </summary>
        public TeamWeaponLoadouts WeaponLoadouts { get => _weaponLoadouts; }


        /// <summary>
        /// Converts the <see cref="Config"/> into serialized Cfx event parameters.
        /// </summary>
        /// <returns>A serialized representation of the <see cref="Config"/>.</returns>
        public Serialized Serialize()
        {
            // TODO: for now this will just choose the first loadout for each team
            return new Serialized(WeaponLoadouts.Hunters[0], WeaponLoadouts.Hunted[0], VehicleWhitelist);
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
        }
    }
}
