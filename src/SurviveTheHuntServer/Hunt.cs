using CitizenFX.Core;
using System;
using System.Collections;
using System.Collections.Generic;

using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public static class Hunt
    {
        private static Random rng = new Random();

        public static Player ChooseRandomPlayer(PlayerList players)
        {
            int playerCount = GetNumPlayerIndices();
            Console.WriteLine($"Picking a random player from {playerCount} players.");
            int randomPlayerIndex = rng.Next(0, playerCount - 1);
            string randomPlayerSrc = GetPlayerFromIndex(randomPlayerIndex);
            Console.WriteLine($"Picked player {randomPlayerIndex} as random player ({GetPlayerName(randomPlayerSrc)})");
            return players[GetPlayerName(randomPlayerSrc)];
        }

        /// <summary>
        /// Checks if the <paramref name="player"/> who just died was a hunted player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="gameState"></param>
        /// <returns>Returns true if the hunt should end due to hunted player's death, false if it's still in progress.</returns>
        public static bool CheckPlayerDeath(Player player, ref GameState gameState)
        {
            if(gameState.Hunt.HuntedPlayer == player)
            {
                Console.WriteLine($"The hunted player ({player.Name}) died. Ending hunt.");
                gameState.Hunt.End(Teams.Team.Hunters);
                return true;
            }
            return false;
        }
    }
}