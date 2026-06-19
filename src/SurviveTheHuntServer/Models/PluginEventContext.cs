using CitizenFX.Core;

namespace SurviveTheHuntServer.Models
{
    internal class PluginEventContext : SurviveTheHuntShared.Models.PluginContextBase
    {
        internal delegate void TriggerPerClientEventProxy(Player player, string eventName, params object[] payload);

        internal readonly TriggerPerClientEventProxy TriggerPerClientEvent;

        private Player _sender;
        internal Player Sender => _sender;

        internal PluginEventContext(Player sender, TriggerEventProxy triggerEventProxy, TriggerClientEventProxy triggerClientEventProxy, TriggerPerClientEventProxy triggerPerClientEventProxy) : base(triggerEventProxy, triggerClientEventProxy)
        {
            TriggerPerClientEvent = triggerPerClientEventProxy;
            _sender = sender;
        }
    }
}
