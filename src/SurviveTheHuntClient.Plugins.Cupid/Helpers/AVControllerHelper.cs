using CitizenFX.Core;
using SurviveTheHuntClient.Models;

namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    internal class AVControllerHelper
    {
        private readonly TriggerEventProxyDelegate TriggerEventProxy;

        private bool _isInStage = false;

        internal AVControllerHelper(TriggerEventProxyDelegate triggerEventProxyDelegate)
        {
            TriggerEventProxy = triggerEventProxyDelegate;
        }

        internal const string ResourceName = "sth-cupid-av";

        internal enum EventName
        {
            StartStage,
            EndStage
        }

        internal static string BuildEventName(EventName eventName)
        {
            string eventNameKebab = SurviveTheHuntShared.Utils.TextHelper.CamelToKebab($"{eventName}");
            return $"{ResourceName}:{eventNameKebab}";
        }

        internal void StartStage(string stage)
        {
            string eventName = BuildEventName(EventName.StartStage);
            Debug.WriteLine($"Sending event {eventName} with {stage}");

            TriggerEventProxy(eventName, stage);

            _isInStage = true;
        }

        internal void EndStage()
        {
            if (_isInStage)
            {
                TriggerEventProxy(BuildEventName(EventName.EndStage));
            }

            _isInStage = false;
        }
    }
}
