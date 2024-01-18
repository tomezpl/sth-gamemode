using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public class PantoHunt : Hunt
    {
        private readonly Vector3[] PantoSpawns = 
        {
            new Vector3(113.58f, -1942.39f, 20.35f), // Grove Street
            new Vector3(-51.22f, -1116.22f, 26.06f), // Simeon
            new Vector3(-478.12f, -757.52f, 44.64f), // Little Seoul car park
            new Vector3(-1374.03f, -1119.92f, 4.13f), // Vespucci Beach
            new Vector3(1085.65f, -698.56f, 58.03f), // Mirror Park
            new Vector3(-815.16f, 159.24f, 71.15f), // Michael's
            new Vector3(-1667.97f, -541.91f, 34.27f), // Hotel (trivago)
            new Vector3(-1797.87f, 399.3f, 112.05f), // Vinewood Hills
            new Vector3(-107.19f, -608.57f, 34.42f), // Arcadius
            new Vector3(1272.11f, -1728.78f, 55.03f) // Lester's
        };

        private bool CalledStart = false;

        PantoHunt(IEnumerable<Player> playerHandles) : base(playerHandles)
        {
            CalledStart = false;
        }

        private void Start()
        {
            CalledStart = true;
        }

        public override void Tick(MainScript main)
        {
            base.Tick(main);

            if(!CalledStart)
            {
                Start();
            }
        }
    }
}
