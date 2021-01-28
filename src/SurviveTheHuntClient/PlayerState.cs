using CitizenFX.Core;
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
        public int LastWeaponEquipped { get; set; } = default;

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

        private void GiveWeapons(ref Ped playerPed)
        {
            KeyValuePair<WeaponAsset, int>[] weapons = Constants.WeaponLoadouts[Team];

            foreach(KeyValuePair<WeaponAsset, int> weapon in weapons)
            {
                bool equip = weapon.Key.Hash == LastWeaponEquipped;
                GiveWeaponToPed(playerPed.Handle, (uint)weapon.Key.Hash, weapon.Value, false, equip);
            }

            WeaponsGiven = true;
        }

        public void TakeAwayWeapons(ref Ped playerPed)
        {
            KeyValuePair<WeaponAsset, int>[] weapons = Constants.WeaponLoadouts[Team];

            LastWeaponEquipped = GetSelectedPedWeapon(playerPed.Handle);

            foreach (KeyValuePair<WeaponAsset, int> weapon in weapons)
            {
                RemoveWeaponFromPed(playerPed.Handle, (uint)weapon.Key.Hash);
            }

            WeaponsGiven = false;
        }

        public void UpdateWeapons(Ped playerPed)
        {
            bool weaponsAllowed = !playerPed.IsGettingIntoAVehicle && !playerPed.IsInVehicle();

            if(weaponsAllowed && !WeaponsGiven)
            {
                GiveWeapons(ref playerPed);
            }
            else if(!weaponsAllowed && WeaponsGiven)
            {
                TakeAwayWeapons(ref playerPed);
            }
        }
    }
}
