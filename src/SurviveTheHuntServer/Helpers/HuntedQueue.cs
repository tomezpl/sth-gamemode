using CitizenFX.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Helpers
{
    /// <summary>
    /// <para>A basic iterable queue class that randomises the order every time a player is added or removed via <see cref="AddPlayer(Player)"/> or <see cref="RemovePlayer(Player)"/></para>
    /// <para><see cref="QueueSize"/> can be used to check if the queue is empty (equals 0). If it is, the queue can be "reset" using <see cref="Init(IEnumerable{Player})"/>.</para>
    /// <para>Every time a random player is requested, use <see cref="PopNext"/> to return a reference to their <see cref="Player"/> object and remove them from the queue.</para>
    /// <para>The queue can be reshuffled at any moment by calling <see cref="Shuffle"/>.</para>
    /// </summary>
    public class HuntedQueue : IEnumerable<Player>
    {
        /// <summary>
        /// Current queue of players.
        /// </summary>
        private Queue<Player> Players;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private readonly static Random RNG = new Random();

        /// <summary>
        /// Current number of players still waiting in the queue.
        /// </summary>
        public int QueueSize { get => Players.Count; }

        /// <summary>
        /// Creates a queue instance by initialising it with the provided <paramref name="playerHandles"/>.
        /// </summary>
        /// <param name="playerHandles">List of players to consider. Typically this will be <see cref="BaseScript.Players"/>.</param>
        public HuntedQueue(IEnumerable<Player> playerHandles)
        {
            Init(playerHandles);
        }

        /// <summary>
        /// Initialises/resets the queue with the provided <paramref name="playerHandles"/>.
        /// </summary>
        /// <param name="playerHandles">List of players to consider. Typically this will be <see cref="BaseScript.Players"/>.</param>
        public void Init(IEnumerable<Player> playerHandles)
        {
            Players = new Queue<Player>(playerHandles);
            Shuffle();
        }

        /// <summary>
        /// Adds <paramref name="player"/> to the list of players to consider, then reshuffles the queue.
        /// </summary>
        /// <param name="player">Player to add.</param>
        public void AddPlayer(Player player)
        {
            Players.Enqueue(player);
            Shuffle();
        }

        /// <summary>
        /// Removes <paramref name="player"/> from the list of players to consider (if they were included in the first place), then reshuffles the queue.
        /// </summary>
        /// <param name="player">Player to remove.</param>
        public void RemovePlayer(Player player)
        {
            Players = new Queue<Player>(Players.Where(playerIndex => playerIndex.Handle != player.Handle));
            Shuffle();
        }

        /// <summary>
        /// Retrieves the next player to choose as the hunted and removes them from the queue.
        /// </summary>
        /// <returns>A reference to the player's <see cref="Player"/> object.</returns>
        public Player PopNext()
        {
            return Players.Dequeue();
        }

        /// <summary>
        /// Shuffles the queue order by swapping random elements with each other several times.
        /// </summary>
        public void Shuffle()
        {
            const int shuffles = 5;
            Player[] playersArray = Players.ToArray();

            // Swap player indices in the queue a number of times.
            for (int i = 0; i < shuffles; i++)
            {
                for(int element = 0; element < Players.Count; element++)
                {
                    int a = RNG.Next(0, playersArray.Length), b = RNG.Next(0, playersArray.Length);
                    Player tempA = playersArray[a];
                    playersArray[a] = playersArray[b];
                    playersArray[b] = tempA;
                }
            }

            Players = new Queue<Player>(playersArray);
        }

        public IEnumerator<Player> GetEnumerator()
        {
            return Players.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Players.GetEnumerator();
        }
    }
}
