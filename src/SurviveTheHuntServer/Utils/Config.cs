using CitizenFX.Core;
using Newtonsoft.Json;
using SurviveTheHuntShared.Core;

namespace SurviveTheHuntServer {
    public class ServerConfig : Config
    {
        private ServerConfig() : base()
        {

        }

        /// <summary>
        /// Creates a <see cref="Config"/> using the weapon loadouts JSON file pointed at by <paramref name="weaponConfigPath"/>.
        /// </summary>
        /// <param name="weaponConfigPath">Relative path to the weapon loadouts JSON file on the server.</param>
        /// <returns>A valid <see cref="Config"/> that can be sent to players.</returns>
        public static Config FromJsonFile(string weaponConfigPath = Constants.WeaponConfigPath)
        {
            string loadoutsJson = CitizenFX.Core.Native.API.LoadResourceFile(CitizenFX.Core.Native.API.GetCurrentResourceName(), weaponConfigPath);
            return FromJson(loadoutsJson);
        }

        /// <summary>
        /// Creates a <see cref="Config"/> using the supplied weapon loadouts JSON string.
        /// </summary>
        /// <param name="json">A string containing valid weapon loadouts JSON, ideally loaded from <see cref="Constants.WeaponConfigPath"/>.</param>
        /// <returns>A valid <see cref="Config"/> that can be sent to players.</returns>
        public static Config FromJson(string json)
        {
            ServerConfig config = new ServerConfig();
            config._weaponLoadouts = JsonConvert.DeserializeObject<TeamWeaponLoadouts>(json);

            return config;
        }
    }

    public partial class MainScript
    {

        /// <summary>
        /// Broadcasts the config to all players.
        /// </summary>
        /// <param name="config">Active config</param>
        public void BroadcastConfig(Config config)
        {
            Debug.WriteLine("Sending serialized config to players");
            TriggerLatentClientEvent(ServerConfig.ReceiveConfigEvent, ServerConfig.ConfigBroadcastBytesPerSec, config.Serialize().EventParams);
        }

        /// <summary>
        /// Sends the config to a specific player.
        /// </summary>
        /// <param name="player">Player to send the config payload to.</param>
        /// <param name="config">Active config</param>
        public void BroadcastConfig(Player player, Config config)
        {
            Debug.WriteLine($"Sending serialized config to player {player.Name}");
            TriggerLatentClientEvent(player, ServerConfig.ReceiveConfigEvent, ServerConfig.ConfigBroadcastBytesPerSec, config.Serialize().EventParams);
        }
    }
}
