using CitizenFX.Core;
using SurviveTheHuntShared.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Helpers
{
    internal class FFAHuntedQueue : IHuntedQueue
    {
        HuntedQueueType IHuntedQueue.Type => throw new NotImplementedException();

        private Player _currentPlayer;
        internal Player CurrentPlayer { get => _currentPlayer; }

        private Dictionary<Player, SingleHuntedQueue> _playerQueues = new Dictionary<Player, SingleHuntedQueue>();

        private List<Player> _allPlayers = new List<Player>();

        internal FFAHuntedQueue(IEnumerable<Player> players)
        {
            ((IHuntedQueue)this).Init(players);
        }

        public Player PopNext()
        {
            if(_currentPlayer == null)
            {
                throw new Exception("Current player cannot be null!");
            }

            if(!_playerQueues.TryGetValue(_currentPlayer, out SingleHuntedQueue queue))
            {
                throw new Exception("Failed accessing the current player's hunted queue");
            }

            Player next = queue.PopNext();

            if(queue.QueueSize == 0)
            {
                queue.Init(_allPlayers.Where(p => p != _currentPlayer));
            }

            return next;
        }

        void IHuntedQueue.AddPlayer(Player player)
        {
            if (_allPlayers.Contains(player))
            {
                return;
            }

            // Get list of all playerse excluding the currently added one.
            Player[] existingPlayers = new Player[_allPlayers.Count];
            _allPlayers.CopyTo(existingPlayers);

            // Update the all players list.
            _allPlayers.Add(player);

            // Initialise a new hunted queue for the new player. Their initial pool of players to pick from should consist of everyone but themselves.
            _playerQueues.Add(player, new SingleHuntedQueue(existingPlayers));

            // Update everyone else's queues with the new player.
            foreach(KeyValuePair<Player, SingleHuntedQueue> playerQueue in _playerQueues)
            {
                if(playerQueue.Key != player)
                {
                    playerQueue.Value.AddPlayer(player);
                }
            }
        }

        /// <summary>
        /// Provides the enumerator for the current player's queue.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the <see cref="CurrentPlayer"/>'s queue.</returns>
        IEnumerator<Player> IEnumerable<Player>.GetEnumerator()
        {
            return _playerQueues[_currentPlayer].GetEnumerator();
        }

        /// <summary>
        /// Provides the enumerator for the current player's queue.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> for the <see cref="CurrentPlayer"/>'s queue.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _playerQueues[_currentPlayer].GetEnumerator();
        }

        void IHuntedQueue.Init(IEnumerable<Player> players)
        {
            _allPlayers.Clear();
            _playerQueues.Clear();

            foreach(Player player in players)
            {
                ((IHuntedQueue)(this)).AddPlayer(player);
            }
        }

        void IHuntedQueue.RemovePlayer(Player player)
        {
            if (_allPlayers.Contains(player))
            {
                _allPlayers.Remove(player);
            }

            if (_playerQueues.ContainsKey(player))
            {
                _playerQueues.Remove(player);
            }

            foreach (KeyValuePair<Player, SingleHuntedQueue> playerQueue in _playerQueues)
            {
                if (playerQueue.Key != player)
                {
                    playerQueue.Value.RemovePlayer(player);
                }
            }
        }

        void IHuntedQueue.Shuffle()
        {
            SingleHuntedQueue toShuffle = null;
            if(_currentPlayer != null && _playerQueues.TryGetValue(_currentPlayer, out toShuffle))
            {
                toShuffle.Shuffle();
            }
            else
            {
                foreach(KeyValuePair<Player, SingleHuntedQueue> playerQueue in _playerQueues)
                {
                    toShuffle = playerQueue.Value;
                    toShuffle.Shuffle();
                }
            }
        }
    }
}
