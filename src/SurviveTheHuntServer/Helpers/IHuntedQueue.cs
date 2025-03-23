using CitizenFX.Core;
using SurviveTheHuntShared.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Helpers
{

    internal interface IHuntedQueue : IEnumerable<Player>
    {
        HuntedQueueType Type { get; }

        void Init(IEnumerable<Player> players);
        void AddPlayer(Player player);
        void RemovePlayer(Player player);
        void Shuffle();
        Player PopNext();
    }
}
