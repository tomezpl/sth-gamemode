using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public class PlayerState
    {
        /// <summary>
        /// <para>Does the player have weapons?</para>
        /// <para>This is refreshed on each respawn.</para>
        /// </summary>
        public bool WeaponsGiven { get; set; } = false;

        /// <summary>
        /// <para>Was the player's death reported to the server yet?</para>
        /// <para>This is refreshed on each respawn.</para>
        /// </summary>
        public bool DeathReported { get; set; } = false;

        /// <summary>
        /// Last weapon the player had equipped.
        /// </summary>
        public Hash LastWeaponEquipped { get; set; } = new Hash();

        public Teams.Team Team { get; set; } = Teams.Team.Hunted;

        public class BigmapState
        {
            public bool Active { get { return IsBigmapActive(); } }
            public int TimeSinceActivated { get; set; } = -1;

            public void Show()
            {
                SetBigmapActive(true, false);
                TimeSinceActivated = 0;
            }

            public void UpdateTime(float frameTime)
            {
                if (TimeSinceActivated >= 0)
                {
                    TimeSinceActivated += Convert.ToInt32(Math.Round(frameTime * 1000f));
                }
            }

            public void Hide()
            {
                SetBigmapActive(false, false);
                TimeSinceActivated = -1;
            }
        }

        public BigmapState Bigmap { get; set; } = new BigmapState();
    }
}
