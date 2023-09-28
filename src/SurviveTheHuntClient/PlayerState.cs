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

        /// <summary>
        /// The team the local player is on.
        /// </summary>
        public Teams.Team Team { get; set; } = Teams.Team.Hunters;

        /// <summary>
        /// Manages the state of the bigmap widget on the HUD.
        /// </summary>
        public class BigmapState
        {
            public bool Active { get { return IsBigmapActive(); } }

            /// <summary>
            /// How much time has passed (in miliseconds) since <see cref="Show"/> was called.
            /// </summary>
            /// <remarks>A value below 0 will prevent the timer from advancing.</remarks>
            public int TimeSinceActivated { get; set; } = -1;

            public void Show()
            {
                SetBigmapActive(true, false);
                TimeSinceActivated = 0;
            }

            /// <summary>
            /// Advances the bigmap's timer by <paramref name="frameTime"/>.
            /// </summary>
            /// <param name="frameTime">How much time has passed since last tick.</param>
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

        /// <summary>
        /// The local player's <see cref="BigmapState"/> used to show and hide the bigmap HUD widget as needed.
        /// </summary>
        public BigmapState Bigmap { get; set; } = new BigmapState();

        /// <summary>
        /// Gives the player the right weapon loadout based on their assigned team.
        /// </summary>
        /// <param name="playerPed">The player ped to give the weapons to.</param>
        private void GiveWeapons(ref Ped playerPed)
        {
            Dictionary<WeaponAsset, int> weapons = Constants.WeaponLoadouts[Team];

            foreach(KeyValuePair<WeaponAsset, int> weapon in weapons)
            {
                bool equip = weapon.Key.Hash == LastWeaponEquipped;
                GiveWeaponToPed(playerPed.Handle, (uint)weapon.Key.Hash, weapon.Value, false, equip);
            }

            WeaponsGiven = true;
        }

        /// <summary>
        /// Removes weapons from a player ped.
        /// </summary>
        /// <param name="playerPed">The player ped to remove weapons from.</param>
        /// <param name="takeAll">Should all weapons be removed, or just the ones given to the player by the gamemode?</param>
        public void TakeAwayWeapons(ref Ped playerPed, bool takeAll = false)
        {
            if (takeAll)
            {
                RemoveAllPedWeapons(playerPed.Handle, true);
            }
            else
            {
                Dictionary<WeaponAsset, int> weapons = Constants.WeaponLoadouts[Team];

                LastWeaponEquipped = GetSelectedPedWeapon(playerPed.Handle);

                foreach (KeyValuePair<WeaponAsset, int> weapon in weapons)
                {
                    RemoveWeaponFromPed(playerPed.Handle, (uint)weapon.Key.Hash);
                }
            }

            WeaponsGiven = false;
        }

        /// <summary>
        /// Manages the player's weapons - removes them while in vehicles (to prevent drive-by), reequips the last used weapon after getting out of a vehicle, etc.
        /// </summary>
        /// <param name="playerPed">The player ped whose weapons should be updated.</param>
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
