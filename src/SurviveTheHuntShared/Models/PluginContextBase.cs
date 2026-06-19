namespace SurviveTheHuntShared.Models
{
    public class PluginContextBase
    {
        public delegate void TriggerEventProxy(string eventName, params object[] payload);

        public delegate void TriggerClientEventProxy(string eventName, params object[] payload);

        public readonly TriggerEventProxy TriggerEvent;
        public readonly TriggerClientEventProxy TriggerClientEvent;

        public PluginContextBase(TriggerEventProxy triggerEvent, TriggerClientEventProxy triggerClientEvent)
        {
            TriggerEvent = triggerEvent;
            TriggerClientEvent = triggerClientEvent;
        }
    }
}
