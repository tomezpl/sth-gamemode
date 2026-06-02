using SurviveTheHuntClient.Models;

namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    internal class UIMenuHelper
    {
        private readonly TriggerEventProxyDelegate TriggerEvent;
        internal UIMenuHelper(TriggerEventProxyDelegate triggerEventProxy)
        {
            TriggerEvent = triggerEventProxy;
        }

        internal void SetItemBlocked(SurviveTheHuntShared.Models.UI.BlockedItem item, bool blocked = true)
        {
            TriggerEvent(SurviveTheHuntShared.Events.Client.UISetItemBlocked, item, blocked);
        }
    }
}
