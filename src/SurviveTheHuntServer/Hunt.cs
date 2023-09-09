using CitizenFX.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    /// <summary>
    /// Helper methods for the Hunt gamemode/rules.
    /// </summary>
    public static class Hunt
    {
        private static Random rng = new Random();

        /// <summary>
        /// Chooses a random player for the next hunt. Attempts to prevent the same player being chosen twice in a row.
        /// </summary>
        /// <param name="players">All players.</param>
        /// <param name="gameState">Up-to-date game state.</param>
        /// <returns>Handle for player to use as next hunted player.</returns>
        public static Player ChooseRandomPlayer(PlayerList players, ref GameState gameState)
        {
            string huntedOverride = GetConvar("sth_huntedOverride", "");
            if (!string.IsNullOrWhiteSpace(huntedOverride))
            {
                Debug.WriteLine($"Choosing {huntedOverride} as the hunted due to override. Set sth_huntedOverride to empty if you want to revert to random pick.");
                return players[huntedOverride];
            }

            int playerCount = GetNumPlayerIndices();
            List<string> playerNames = new List<string>();

            Console.WriteLine("Picking from:");
            for (int i = 0; i < playerCount; i++)
            {
                string playerName = GetPlayerName(GetPlayerFromIndex(i));

                // Try to prevent LastHuntedPlayer being chosen again.
                if(playerName != gameState.Hunt.LastHuntedPlayer?.Name || (playerNames.Count == 0 && i == playerCount - 1))
                {
                    playerNames.Add(playerName);
                    Console.WriteLine($"\t{playerNames.Count}. {playerNames.Last()}");
                }
            }

            Console.WriteLine($"Picking a random player from {playerNames.Count} players.");

            int randomPlayerIndex = rng.Next(0, playerNames.Count);
            string randomPlayerName = playerNames[randomPlayerIndex];

            Console.WriteLine($"Picked player {randomPlayerIndex + 1} as random player ({randomPlayerName})");

            return players[randomPlayerName];
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