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
        /// Has the player's weapon been unequipped yet (e.g. due to being in a vehicle)?
        /// </summary>
        public bool ForcedUnarmed = false;

        /// <summary>
        /// <para>Was the player's death reported to the server yet?</para>
        /// <para>This is refreshed on each respawn.</para>
        /// </summary>
        public bool DeathReported { get; set; } = false;

        /// <summary>
        /// Last weapon the player had equipped.
        /// </summary>
        public uint LastWeaponEquipped = (uint)WeaponHash.Unarmed;

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
        /// Removes weapons from a player ped.
        /// </summary>
        /// <param name="playerPed">The player ped to remove weapons from.</param>
        /// <param name="takeAll">Should all weapons be removed, or just the ones given to the player by the gamemode?</param>
        public void TakeAwayWeapons(ref Ped playerPed)
        {
            RemoveAllPedWeapons(playerPed.Handle, true);

            WeaponsGiven = false;
            ForcedUnarmed = false;
        }

        /// <summary>
        /// Gives the player the right weapon loadout based on their assigned team.
        /// </summary>
        /// <remarks>The player's current weapons are removed, so this effectively resets their loadout.</remarks>
        /// <param name="playerPed">The player ped to give the weapons to.</param>
        private void GiveWeapons(ref Ped playerPed)
        {
            // First remove the existing weapons.
            RemoveAllPedWeapons(playerPed.Handle, false);

            foreach(Weapons.WeaponAmmo weapon in Constants.WeaponLoadouts[Team])
            {
                bool equip = weapon.Hash == LastWeaponEquipped;
                GiveWeaponToPed(playerPed.Handle, weapon.Hash, weapon.Ammo, false, equip);
            }

            WeaponsGiven = true;
        }

        /// <summary>
        /// Manages the player's weapons - forces unarmed while in vehicles (to prevent drive-by), reequips the last used weapon after getting out of a vehicle, etc.
        /// </summary>
        /// <param name="playerPed">The player ped whose weapons should be updated.</param>
        public void UpdateWeapons(Ped playerPed)
        {
            // Give (or reset) a player's weapons if needed.
            if(!WeaponsGiven)
            {
                GiveWeapons(ref playerPed);
            }

            // Weapons aren't allowed in vehicles.
            bool weaponsAllowed = !playerPed.IsGettingIntoAVehicle && !playerPed.IsInVehicle();

            // If the player has a weapon equipped, store the weapon in LastWeaponEquipped so we keep track in case we need to re-equip it.
            if(weaponsAllowed && !ForcedUnarmed)
            {
                GetCurrentPedWeapon(playerPed.Handle, ref LastWeaponEquipped, true);
            }

            // If the player has a weapon equipped but weapons aren't allowed, force them to be unarmed and prevent switching weapons.
            // We only want to do this once, so set ForcedUnarmed to true.
            if (!weaponsAllowed && !ForcedUnarmed)
            {
                SetCurrentPedWeapon(playerPed.Handle, (uint)WeaponHash.Unarmed, true);
                SetPedCanSwitchWeapon(playerPed.Handle, false);
                ForcedUnarmed = true;
            }

            // Revert the above, automatically equipping the player's last used weapon, if weapons are now allowed.
            if(ForcedUnarmed && weaponsAllowed)
            {
                SetCurrentPedWeapon(playerPed.Handle, LastWeaponEquipped, true);
                SetPedCanSwitchWeapon(playerPed.Handle, true);
                ForcedUnarmed = false;
            }
        }
    }
}
