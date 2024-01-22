using CitizenFX.Core;
using SurviveTheHuntServer.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    /// <summary>
    /// Hunt gamemode logic/rules.
    /// </summary>
    public class Hunt
    {
        private HuntedQueue HuntedPlayerQueue = new HuntedQueue(Enumerable.Empty<Player>());

        public Hunt(IEnumerable<Player> playerHandles)
        {
            InitHuntedQueue(playerHandles);
        }

        /// <summary>
        /// Initialises a randomised hunted player queue using <paramref name="playerHandles"/> and provides a reference to the initialised queue.
        /// </summary>
        /// <param name="playerHandles">A list of players to consider, typically all players (<see cref="BaseScript.Players"/>).</param>
        /// <returns>A reference to the <see cref="HuntedQueue"/> used to determine hunted player order.</returns>
        public HuntedQueue InitHuntedQueue(IEnumerable<Player> playerHandles)
        {
            HuntedPlayerQueue.Init(playerHandles);

            return HuntedPlayerQueue;
        }

        /// <summary>
        /// Chooses a random player for the next hunt. Attempts to allow every player to be the hunted once before the queue loops.
        /// </summary>
        /// <param name="players">All players.</param>
        /// <returns>Handle for player to use as next hunted player.</returns>
        public Player ChooseRandomPlayer(PlayerList players)
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
        public bool CheckPlayerDeath(Player player, GameState gameState)
        {
            if (gameState.Hunt.HuntedPlayer == player)
            {
                Console.WriteLine($"The hunted player ({player.Name}) died. Ending hunt.");
                gameState.Hunt.End(Teams.Team.Hunters);
                return true;
            }
            return false;
        }

        public virtual void Shutdown(MainScript main)
        {

        }

        /// <summary>
        /// Gamemode logic to run on every tick.
        /// </summary>
        public virtual async Task Tick(MainScript main)
        {
            if (main.GameState.Hunt.IsStarted)
            {
                if (main.GameState.Hunt.EndTime <= DateTime.UtcNow)
                {
                    main.GameState.Hunt.End(Teams.Team.Hunted);
                    main.NotifyWinner();
                }

                if (DateTime.UtcNow - main.GameState.Hunt.LastPingTime >= Constants.HuntedPingInterval)
                {
                    main.GameState.Hunt.LastPingTime = DateTime.UtcNow;
                    float radius = 200f;
                    float playerLocationRadius = radius * 0.875f;
                    float offsetX = (((float)main.RNG.NextDouble() * 2f) - 1f) * playerLocationRadius;
                    float offsetY = (((float)main.RNG.NextDouble() * 2f) - 1f) * playerLocationRadius;

                    main.TriggerClientEventProxy("sth:showPingOnMap", new
                    {
                        CreationDate = main.GameState.Hunt.LastPingTime.ToString("F", CultureInfo.InvariantCulture),
                        PlayerName = main.GameState.Hunt.HuntedPlayer.Name,
                        Radius = radius,
                        OffsetX = offsetX,
                        OffsetY = offsetY
                    });
                }
            }
        }
    }
}