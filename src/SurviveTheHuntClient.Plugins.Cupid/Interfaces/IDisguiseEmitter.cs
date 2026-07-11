using SurviveTheHuntClient.Plugins.Cupid.Models;

namespace SurviveTheHuntClient.Plugins.Cupid.Interfaces
{
    internal interface IDisguiseEmitter
    {
        event DisguiseStateChangedEvent DisguiseStateChanged;
    }
}
