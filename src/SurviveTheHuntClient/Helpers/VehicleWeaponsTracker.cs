using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using System;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    /// <summary>
    /// Helper for disabling weaponised vehicles' guns
    /// </summary>
    internal class VehicleWeaponsTracker : ITickable
    {
        internal delegate void ExecutePluginsDelegate(Action<Plugin> pluginFunc);
        private ExecutePluginsDelegate ExecutePlugins;

        internal VehicleWeaponsTracker(ExecutePluginsDelegate executePlugins)
        {
            ExecutePlugins = executePlugins;
        }

        public void Tick(float deltaTime)
        {
            foreach (Player player in PlayerList.Players)
            {
                if (player.IsAlive && player.Character.Exists())
                {
                    Ped ped = player.Character;
                    Vehicle veh = ped.CurrentVehicle;


                    if (veh?.Exists() == true && DoesVehicleHaveWeapons(veh.Handle))
                    {
                        uint weapon = uint.MaxValue;

                        // If the player ped has a vehicle weapon equipped, disable it
                        if (GetCurrentPedVehicleWeapon(ped.Handle, ref weapon))
                        {
                            bool shouldDisable = true;
                            ExecutePlugins(plugin =>
                            {
                                bool? allowed = plugin.IsVehicleWeaponAllowed(veh.Handle, weapon);
                                if (allowed == true)
                                {
                                    shouldDisable = false;
                                }
                                else if(allowed == false)
                                {
                                    shouldDisable = true;
                                }
                            });

                            DisableVehicleWeapon(shouldDisable, weapon, veh.Handle, ped.Handle);
                            
                        }
                    }
                }
            }
        }
    }
}
