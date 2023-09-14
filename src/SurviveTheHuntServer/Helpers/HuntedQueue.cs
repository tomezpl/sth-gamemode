using CitizenFX.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Helpers
{
    public class HuntedQueue : IEnumerable<Player>
    {
        private Queue<Player> Players;

        private readonly static Random RNG = new Random();

        public int QueueSize { get => Players.Count; }

        public HuntedQueue(IEnumerable<Player> playerHandles)
        {
            Init(playerHandles);
        }

        public void Init(IEnumerable<Player> playerHandles)
        {
            Players = new Queue<Player>(playerHandles);
            Shuffle();
        }

        public void AddPlayer(Player player)
        {
            Players.Enqueue(player);
            Shuffle();
        }

        public void RemovePlayer(Player player)
        {
            Players = new Queue<Player>(Players.Where(playerIndex => playerIndex.Handle != player.Handle));
            Shuffle();
        }

        public Player PopNext()
        {
            return Players.Dequeue();
        }

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
