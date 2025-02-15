using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    internal static class PlayerPassenger
    {
        private static float SecondsEnterControlHeld = 0f;
        private const float PassengerEntryActivationSeconds = 0.25f;
        private const int PassengerEntryActivationFrames = 10;

        private static int FramesEnterControlHeld = 0;
        private static bool IsGettingInAsPassenger = false;
        private static int VehicleToEnterAsPassenger = 0;

        private static readonly int[] Buses =
        {
            GetHashKey("bus"),
            GetHashKey("airbus"),
            GetHashKey("coach")
        };

        internal static void Tick()
        {
            if(IsGettingInAsPassenger)
            {
                // Disable the "Enter vehicle" control for as long as our ped is trying to enter the vehicle as a passenger, to avoid conflicts
                DisableControlAction(0, (int)Control.Enter, true);
                
                // Check that we're still attempting to enter the vehicle as a passenger
                IsGettingInAsPassenger = IsGettingInAsPassenger && IsPedGettingIntoAVehicle(PlayerPedId()) && (GetVehiclePedIsTryingToEnter(PlayerPedId()) == VehicleToEnterAsPassenger || GetVehiclePedIsEntering(PlayerPedId()) == VehicleToEnterAsPassenger);
                if(IsGettingInAsPassenger)
                {
                    SecondsEnterControlHeld += GetFrameTime();
                    
                    // Failsafe after 15s to avoid the player freezing in place
                    if(SecondsEnterControlHeld >= 15f)
                    {
                        IsGettingInAsPassenger = false;
                        FramesEnterControlHeld = 0;
                        SecondsEnterControlHeld = 0f;
                        ClearPedTasksImmediately(PlayerPedId());
                    }

                    return;
                }
                else
                {
                    FramesEnterControlHeld = 0;
                    SecondsEnterControlHeld = 0f;
                }
            }

            if(!IsGettingInAsPassenger && (!DoesEntityExist(VehicleToEnterAsPassenger) || !IsPedInVehicle(PlayerPedId(), VehicleToEnterAsPassenger, true)))
            {
                // Reset PreventAutoShuffleToDriversSeat when the player is out of a vehicle
                SetPedConfigFlag(PlayerPedId(), 184, false);
            }

            if(!IsPedGettingIntoAVehicle(PlayerPedId()))
            {
                IsGettingInAsPassenger = false;
            }

            // Keep checking for whether the control is held
            if(IsControlPressed(0, (int)Control.Enter))
            {
                FramesEnterControlHeld++;
                SecondsEnterControlHeld += GetFrameTime();
            }

            if(IsControlReleased(0, (int)Control.Enter))
            {
                FramesEnterControlHeld = 0;
                SecondsEnterControlHeld = 0f;
            }

            // If the held activation threshold was passed, attempt to task the player ped with entering the vehicle as a passenger
            if(FramesEnterControlHeld >= PassengerEntryActivationFrames && SecondsEnterControlHeld >= PassengerEntryActivationSeconds)
            {
                int playerPed = PlayerPedId();
                if (IsPedGettingIntoAVehicle(playerPed))
                {
                    int vehicle = GetVehiclePedIsEntering(playerPed);

                    int vehicleModel = GetEntityModel(vehicle);

                    // If the vehicle is a bus then only allow entering it if it has a driver.
                    bool isBus = Buses.Contains(vehicleModel);
                    bool isDrivenBus = isBus && !IsVehicleSeatFree(vehicle, (int)VehicleSeat.Driver);

                    bool canEnter = isDrivenBus;
                    if(!canEnter)
                    {
                        // If it's not a bus with a driver, check if it's a completely empty car.
                        if(!isBus)
                        {
                            canEnter = GetVehicleNumberOfPassengers(vehicle) == 0 && IsVehicleSeatFree(vehicle, (int)VehicleSeat.Driver);
                        }
                    }

                    if (canEnter)
                    {
                        bool isRunning = IsPedRunning(playerPed);
                        if (!IsGettingInAsPassenger && IsAnyVehicleSeatEmpty(vehicle))
                        {
                            int seats = GetVehicleModelNumberOfSeats((uint)vehicleModel);
                         
                            // Look for a free passenger seat.
                            for(int i = 0; i < seats; i++)
                            {
                                if(IsVehicleSeatFree(vehicle, i))
                                {
                                    // PreventAutoShuffleToDriversSeat
                                    SetPedConfigFlag(playerPed, 184, true);

                                    // Store the vehicle handle
                                    VehicleToEnterAsPassenger = vehicle;
                                    
                                    // Task the player ped with entering the vehicle into the first free passenger seat, with a timeout of 10s
                                    TaskEnterVehicle(playerPed, vehicle, 10000, i, isRunning ? 2f : 1f, 1, 0);

                                    // Disable the panic behaviours on the bus driver.
                                    if (isDrivenBus)
                                    {
                                        int driver = GetPedInVehicleSeat(vehicle, -1);
                                        // DisablePanicInVehicle
                                        SetPedConfigFlag(driver, 229, true);
                                        // AICanDrivePlayerAsRearPassenger 
                                        SetPedConfigFlag(driver, 251, true);
                                        // AIDriverAllowFriendlyPassengerSeatEntry
                                        SetPedConfigFlag(driver, 255, true);
                                        //ClearPedTasksImmediately(driver);
                                        //SetPedIntoVehicle(driver, vehicle, -1);
                                        //TaskVehicleDriveWander(driver, vehicle, 10f, 959);
                                        SetPedStayInVehicleWhenJacked(driver, true);
                                        
                                    }

                                    DisableControlAction(0, (int)Control.Enter, true);
                                    break;
                                }
                            }
                        }
                        IsGettingInAsPassenger = true;
                    }
                }
            }
        }
    }
}
