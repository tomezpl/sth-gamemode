using SurviveTheHuntShared.Interfaces;
using SurviveTheHuntShared.Plugins;
using System;
using System.Collections.Generic;

namespace SurviveTheHuntServer.Helpers
{
    internal static class PluginStateFactory
    {
        internal delegate IPluginState PluginStateFactoryDelegate();

        private static readonly Dictionary<PluginIndex, PluginStateFactoryDelegate> s_StateFactories = new Dictionary<PluginIndex, PluginStateFactoryDelegate>
        {
            { PluginIndex.Cupid, Extensions.Cupid.CupidPluginState.Create }
        };

        internal static IPluginState Create(PluginIndex pluginIndex)
        {
            if (s_StateFactories.TryGetValue(pluginIndex, out PluginStateFactoryDelegate pluginStateFactory))
            {
                return pluginStateFactory();
            }

            throw new ArgumentException($"Could not get a state factory for plugin {pluginIndex}.", nameof(pluginIndex));
        }
    }
}
