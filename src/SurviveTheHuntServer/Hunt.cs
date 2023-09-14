using CitizenFX.Core;
using SurviveTheHuntServer.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    /// <summary>
    /// Helper methods for the Hunt gamemode/rules.
    /// </summary>
    public static class Hunt
    {
        private static HuntedQueue HuntedPlayerQueue = new HuntedQueue(Enumerable.Empty<Player>());

        public static HuntedQueue InitHuntedQueue(IEnumerable<Player> playerHandles)
        {
            HuntedPlayerQueue.Init(playerHandles);

            return HuntedPlayerQueue;
        }

        /// <summary>
        /// Chooses a random player for the next hunt. Attempts to prevent the same player being chosen twice in a row.
        /// </summary>
        /// <param name="players">All players.</param>
        /// <param name="gameState">Up-to-date game state.</param>
        /// <returns>Handle for player to use as next hunted player.</returns>
        public static Player ChooseRandomPlayer(PlayerList players, ref GameState gameState)
        {
            if(HuntedPlayerQueue.QueueSize == 0)
            {
                HuntedPlayerQueue.Init(players);
                Debug.WriteLine($"Reinitialising the hunted queue with ${players.Count()} players!");
            }

            string huntedOverride = GetConvar("sth_huntedOverride", "");
            if (!string.IsNullOrWhiteSpace(huntedOverride))
            {
                Debug.WriteLine($"Choosing {huntedOverride} as the hunted due to override. Set sth_huntedOverride to empty if you want to revert to random pick.");
                return players[huntedOverride];
            }

            int playerCount = GetNumPlayerIndices();
            List<string> playerNames = new List<string>();

            Console.WriteLine("Picking from:");
            foreach(Player player in HuntedPlayerQueue)
            {
                playerNames.Add(player.Name);
                Console.WriteLine($"\t{playerNames.Count}. {player.Name}");
            }

            Console.WriteLine($"Picking a random player from {playerNames.Count} players.");

            Player randomPlayer = HuntedPlayerQueue.PopNext();

            Console.WriteLine($"Picked as random player ({randomPlayer.Name})");

            return randomPlayer;
        }

        /// <summary>
        /// Checks if the <paramref name="player"/> who just died was a hunted player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="gameState"></param>
        /// <returns>Returns true if the hunt should end due to hunted player's death, false if it's still in progress.</returns>
        public static bool CheckPlayerDeath(Player player, ref GameState gameState)
        {
            if (gameState.Hunt.HuntedPlayer == player)
            {
                Console.WriteLine($"The hunted player ({player.Name}) died. Ending hunt.");
                gameState.Hunt.End(Teams.Team.Hunters);
                return true;
            }
            return false;
        }
    }
}