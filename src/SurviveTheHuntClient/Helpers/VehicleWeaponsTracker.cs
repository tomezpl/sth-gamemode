using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    /// <summary>
    /// Helper for disabling weaponised vehicles' guns
    /// </summary>
    internal static class VehicleWeaponsTracker
    {
        internal static void Tick()
        {
            Vehicle veh = Game.PlayerPed.CurrentVehicle;
            if(veh?.Exists() == true && DoesVehicleHaveWeapons(veh.Handle))
            {
                uint weapon = uint.MaxValue;

                // If the player ped has a vehicle weapon equipped, disable it
                if(GetCurrentPedVehicleWeapon(Game.PlayerPed.Handle, ref weapon))
                {
                    DisableVehicleWeapon(true, weapon, veh.Handle, Game.PlayerPed.Handle);
                }
            }
        }
    }
}
