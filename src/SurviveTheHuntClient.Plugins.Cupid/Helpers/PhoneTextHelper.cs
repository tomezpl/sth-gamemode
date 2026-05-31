using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Models;

namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    internal class PhoneTextHelper
    {
        internal const float DefaultMessageDurationSeconds = 20f;

        private readonly TriggerEventProxyDelegate TriggerEvent;

        internal PhoneTextHelper(TriggerEventProxyDelegate triggerEventProxy)
        {
            TriggerEvent = triggerEventProxy;
        }

        internal void SendText(PhoneContactInfo sender, string subject, string message, float duration = DefaultMessageDurationSeconds)
        {
            TriggerEvent(SurviveTheHuntShared.Events.Client.UISendText, sender.Name, subject, message, duration, sender.TextureName);
        }
    }
}
