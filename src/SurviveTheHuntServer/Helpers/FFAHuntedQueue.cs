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
        public HuntedQueueType Type => HuntedQueueType.FreeForAll;

        private Player _currentPlayer;
        internal Player CurrentPlayer { get => _currentPlayer; }

        /// <summary>
        /// The <see cref="CurrentPlayer"/>'s current target.
        /// </summary>
        internal Player CurrentTarget { get => CurrentPlayer == null ? null : _currentTargets[CurrentPlayer]; }

        private Dictionary<Player, SingleHuntedQueue> _playerQueues = new Dictionary<Player, SingleHuntedQueue>();

        private Dictionary<Player, Player> _lastHunted = new Dictionary<Player, Player>();

        private Dictionary<Player, Player> _currentTargets = new Dictionary<Player, Player>();

        private List<Player> _allPlayers = new List<Player>();

        internal FFAHuntedQueue(IEnumerable<Player> players)
        {
            Init(players);
        }

        public void SetCurrentPlayer(Player currentPlayer)
        {
            _currentPlayer = currentPlayer;
            if(!_allPlayers.Contains(currentPlayer))
            {
                AddPlayer(currentPlayer);
            }
        }

        /// <summary>
        /// Removes <paramref name="target"/> from the list of current player targets. The current target of the player who this target belonged to will be set to null.
        /// </summary>
        /// <param name="target"></param>
        /// <returns>Returns a list of hunter players that this target was removed from. This can be used to notify them of a target change.</returns>
        public List<Player> RemoveTarget(Player target)
        {
            List<Player> huntersToRemoveTargetFrom = new List<Player>(_currentTargets.Count);
            foreach(KeyValuePair<Player, Player> huntPair in _currentTargets)
            {
                if(huntPair.Value == target)
                {
                    huntersToRemoveTargetFrom.Add(huntPair.Key);
                }
            }

            foreach(Player hunter in huntersToRemoveTargetFrom)
            {
                _currentTargets[hunter] = null;
                _lastHunted[hunter] = target;
            }

            return huntersToRemoveTargetFrom;
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

            if(!_currentTargets.TryGetValue(_currentPlayer, out Player lastHunted))
            {
                throw new Exception("Failed accessing the current player's last target");
            }

            // Only pop the current target as lastHunted if the current target is not null (it may be when we remove a target from a player on death)
            if (lastHunted != null)
            {
                _lastHunted[_currentPlayer] = lastHunted;
            }

            Player next = null;

            // if we only have one possible target then that's our only choice
            if (queue.QueueSize == 1)
            {
                next = queue.PopNext();
                queue.Init(_allPlayers.Where(p => p != _currentPlayer));
            }
            // if we've exhausted the queue, reinitialise it and attempt to pick again
            else if(queue.QueueSize == 0)
            {
                queue.Init(_allPlayers.Where(p => p != _currentPlayer));
                if(queue.QueueSize != 0)
                {
                    next = PopNext();
                }
            }
            else
            {
                next = queue.PopNext();

                bool queueReinitialisedOnce = queue.QueueSize == 0;
                if(queueReinitialisedOnce)
                {
                    queue.Init(_allPlayers.Where(p => p != _currentPlayer && p != lastHunted));
                }

                // Try to avoid picking someone who was just our target, or someone who's currently someone's target
                while(next == lastHunted || _currentTargets.ContainsValue(next))
                {
                    next = queue.PopNext();
                    if(queue.QueueSize == 0 && !queueReinitialisedOnce)
                    {
                        queueReinitialisedOnce = true;
                        queue.Init(_allPlayers.Where(p => p != _currentPlayer && p != lastHunted));
                    }
                    else if(queue.QueueSize == 0)
                    {
                        // break if the queue got reinitialised twice, otherwise we may go into an infinite loop (cause we can't find an ideal pick)
                        break;
                    }
                }
            }

            _currentTargets[_currentPlayer] = next;

            return next;
        }

        public void AddPlayer(Player player)
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

            // Initialise last and current targets store
            _lastHunted.Add(player, null);
            _currentTargets.Add(player, null);

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

        public void Init(IEnumerable<Player> players)
        {
            _allPlayers.Clear();
            _playerQueues.Clear();
            _currentTargets.Clear();
            _lastHunted.Clear();

            foreach(Player player in players)
            {
                AddPlayer(player);
            }
        }

        public void RemovePlayer(Player player)
        {
            _allPlayers.Remove(player);
            _playerQueues.Remove(player);
            _lastHunted.Remove(player);
            _currentTargets.Remove(player);

            foreach (KeyValuePair<Player, SingleHuntedQueue> playerQueue in _playerQueues)
            {
                if (playerQueue.Key != player)
                {
                    playerQueue.Value.RemovePlayer(player);
                }
            }
        }

        public void Shuffle()
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
