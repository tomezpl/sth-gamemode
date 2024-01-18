using CitizenFX.Core;
using SurviveTheHuntServer.Utils;

namespace SurviveTheHuntServer
{
    public partial class MainScript
    {
        public void OnServerResourceStart(string resourceName)
        {
            Debug.WriteLine($"{resourceName} resource started!");

            if(resourceName == Constants.ResourceName)
            {
                Hunt.InitHuntedQueue(Players);

                // Reload the config file every time the resource is started.
                BroadcastConfig(Config.Init());
            }
        }

        public void OnServerResourceStop(string resourceName)
        {
            if(resourceName == Constants.ResourceName)
            {
                Shutdown();
            }
        }

        public void ClientStarted([FromSource] Player player)
        {
            BroadcastConfig(player, Config);
        }
    }
}
