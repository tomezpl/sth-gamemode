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
                // Reload the config file every time the resource is started.
                BroadcastConfig(Config.Init());
            }
        }
    }
}
