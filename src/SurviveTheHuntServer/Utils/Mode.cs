using SurviveTheHuntShared.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Utils
{
    internal static class Mode
    {
        internal static bool IsDefaultMode(string modeName)
        {
            // Empty string means default mode
            return string.IsNullOrWhiteSpace(modeName);
        }

        /// <summary>
        /// Normalises <paramref name="modeName"/> to a <see cref="PluginIndex"/> enum.
        /// </summary>
        /// <param name="modeName"></param>
        /// <returns></returns>
        internal static PluginIndex GetPluginIndex(string modeName)
        {
            switch(modeName?.ToLower())
            {
                case "cupid":
                    return PluginIndex.Cupid;
                case "xmas":
                    return PluginIndex.Xmas;
                default:
                    return PluginIndex.Default;
            }
        }

        internal static int GetHuntedPlayerCount(string modeName)
        {
            switch(GetPluginIndex(modeName))
            {
                case PluginIndex.Cupid:
                    return 2;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// Returns the number of players for a given mode and a given player count.
        /// </summary>
        /// <param name="modeName"></param>
        /// <param name="playersInSession">Current player count</param>
        /// <returns></returns>
        internal static int GetHuntedPlayerCount(string modeName, int playersInSession)
        {
            int maxHuntedPlayers = GetHuntedPlayerCount(modeName);

            // Require at least 1 hunted and 1 hunter. Allow a single hunted to start a hunt if there is no one else on the server.
            return Math.Max(1, Math.Min(maxHuntedPlayers, playersInSession - 1));
        }
    }
}
