using CitizenFX.Core;
using SurviveTheHuntClient.Helpers;
using SurviveTheHuntShared.Core;
using SurviveTheHuntShared.Utils;
using System;

using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public class PlayerState
    {
        /// <summary>
        /// <para>Does the player have weapons?</para>
        /// <para>This is refreshed on each respawn.</para>
        /// </summary>
        public bool WeaponsGiven = false;

        /// <summary>
        /// Has the player's weapon been unequipped yet (e.g. due to being in a vehicle)?
        /// </summary>
        public bool ForcedUnarmed = false;

        /// <summary>
        /// <para>Was the player's death reported to the server yet?</para>
        /// <para>This is refreshed on each respawn.</para>
        /// </summary>
        public bool DeathReported = false;

        /// <summary>
        /// Should the player's death be reported to the server?
        /// </summary>
        public bool ReportDeathNextTick = false;

        /// <summary>
        /// Last weapon the player had equipped.
        /// </summary>
        public uint LastWeaponEquipped = (uint)WeaponHash.Unarmed;

        /// <summary>
        /// The team the local player is on.
        /// </summary>
        public Teams.Team Team = Teams.Team.Hunters;

        /// <summary>
        /// Is the player currently waiting to be teleported to spawn because of the hunt starting?
        /// </summary>
        public bool WaitingToTeleportToSpawn = false;

        /// <summary>
        /// Is the player currently in a character creator screen? This allows the creator resource to gracefully exit the screen before triggering the teleport etc.
        /// </summary>
        public bool IsInCharacterCreator = false;

        /// <summary>
        /// The stage that the player is currently in with regards to their request to teleport back to spawn.
        /// <remarks>This is NOT related to <see cref="WaitingToTeleportToSpawn"/>. In this case this is supposed to support the player's ability to teleport back to spawn outside of a hunt session.</remarks>
        /// </summary>
        public Constants.TeleportPlayerStage TeleportPlayerStage = Constants.TeleportPlayerStage.None;

        /// <summary>
        /// Is the player currently within the safezone bounds?
        /// </summary>
        public bool IsInSafeZone = false;

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
            public int TimeSinceActivated = -1;

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
        public BigmapState Bigmap = new BigmapState();

        /// <summary>
        /// Removes weapons from a player ped.
        /// </summary>
        /// <param name="playerPed">The player ped to remove weapons from.</param>
        /// <param name="takeAll">Should all weapons be removed, or just the ones given to the player by the gamemode?</param>
        public void TakeAwayWeapons(ref Ped playerPed)
        {
            TakeAwayWeapons(playerPed.Handle);
        }

        public void TakeAwayWeapons(int playerPedHandle)
        {
            RemoveAllPedWeapons(playerPedHandle, true);

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
                NativeHelpers.GivePedWeapon(playerPed.Handle, weapon, equip);
            }

            WeaponsGiven = true;
        }

        /// <summary>
        /// Manages the player's weapons - forces unarmed while in vehicles (to prevent drive-by), reequips the last used weapon after getting out of a vehicle, etc.
        /// </summary>
        /// <param name="playerPed">The player ped whose weapons should be updated.</param>
        public void UpdateWeapons(Ped playerPed, bool forceAllowWeapons = false)
        {
            // Give (or reset) a player's weapons if needed.
            if(!WeaponsGiven && !forceAllowWeapons)
            {
                GiveWeapons(ref playerPed);
            }

            bool weaponsAllowed = false;
            if (forceAllowWeapons)
            {
                weaponsAllowed = true;
            }
            else
            {
                // Weapons aren't allowed in vehicles.
                // However, the hunted player should be able to driveby if they're a passenger.
                weaponsAllowed = (!playerPed.IsGettingIntoAVehicle && !playerPed.IsInVehicle()) || (Team == Teams.Team.Hunted && playerPed.SeatIndex >= 0);
            }

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
                ForcedUnarmed = true;
            }

            // Allow the hunted player to change their weapon as a passenger.
            SetPedCanSwitchWeapon(playerPed.Handle, weaponsAllowed);

            // Revert the above, automatically equipping the player's last used weapon, if weapons are now allowed.
            if (ForcedUnarmed && weaponsAllowed)
            {
                SetCurrentPedWeapon(playerPed.Handle, LastWeaponEquipped, true);
                SetPedCanSwitchWeapon(playerPed.Handle, true);
                ForcedUnarmed = false;
            }
        }

        private Constants.TeleportPlayerStage _previousTickTeleportStage = Constants.TeleportPlayerStage.None;
        private int _teleportedEntity = -1;

        public void HandleTeleportToSpawn()
        {
            // Reset state once player switch has finished
            if(TeleportPlayerStage == Constants.TeleportPlayerStage.ZoomIn && !IsPlayerSwitchInProgress())
            {
                TeleportPlayerStage = Constants.TeleportPlayerStage.None;
            }

            // Load collision at the spawn so that the player doesn't fall through the map
            if(TeleportPlayerStage == Constants.TeleportPlayerStage.ZoomOut || (TeleportPlayerStage == Constants.TeleportPlayerStage.ZoomIn && IsPlayerSwitchInProgress()))
            {
                RequestCollisionAtCoord(SurviveTheHuntShared.Constants.DockSpawn.X, SurviveTheHuntShared.Constants.DockSpawn.Y, SurviveTheHuntShared.Constants.DockSpawn.Z);
            }

            // Once we've zoomed out, move the player to the spawn, but don't progress to the ZoomIn stage until collision has loaded there
            if(_previousTickTeleportStage == TeleportPlayerStage && TeleportPlayerStage == Constants.TeleportPlayerStage.ZoomOut && IsPlayerSwitchInProgress() && GetPlayerSwitchState() != 0)
            {
                SetEntityCoords(_teleportedEntity, SurviveTheHuntShared.Constants.DockSpawn.X, SurviveTheHuntShared.Constants.DockSpawn.Y, SurviveTheHuntShared.Constants.DockSpawn.Z, false, false, false, false);
                if (HasCollisionLoadedAroundEntity(_teleportedEntity))
                {
                    TeleportPlayerStage = Constants.TeleportPlayerStage.ZoomIn;
                }
            }

            if(_previousTickTeleportStage != TeleportPlayerStage)
            {
                switch(TeleportPlayerStage)
                {
                    case Constants.TeleportPlayerStage.ZoomOut:
                        if(_previousTickTeleportStage == Constants.TeleportPlayerStage.None)
                        {
                            Vehicle veh = Game.PlayerPed.CurrentVehicle;
                            
                            // Need to teleport the player together with the vehicle if they are inside one
                            int playerPed = PlayerPedId();
                            int entity = -1;
                            if (veh?.Exists() == true)
                            {
                                entity = veh.Handle;
                            }
                            else
                            {
                                entity = playerPed;
                            }
                            _teleportedEntity = entity;

                            // Freeze the player/vehicle for the duration of the switch, so they don't fall through the map
                            FreezeEntityPosition(_teleportedEntity, true);
                            SwitchOutPlayer(PlayerPedId(), 0, 1);
                        }
                        break;
                    case Constants.TeleportPlayerStage.ZoomIn:
                        if(_previousTickTeleportStage == Constants.TeleportPlayerStage.ZoomOut)
                        {
                            // Unfreeze
                            FreezeEntityPosition(_teleportedEntity, false);
                            SwitchInPlayer(PlayerPedId());
                        }
                        break;
                }

                _previousTickTeleportStage = TeleportPlayerStage;
            }
        }
    }
}
