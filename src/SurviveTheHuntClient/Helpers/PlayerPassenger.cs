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

        internal static void Tick()
        {
            if(IsGettingInAsPassenger)
            {
                DisableControlAction(0, (int)Control.Enter, true);
                IsGettingInAsPassenger = IsGettingInAsPassenger && IsPedGettingIntoAVehicle(PlayerPedId()) && (GetVehiclePedIsTryingToEnter(PlayerPedId()) == VehicleToEnterAsPassenger || GetVehiclePedIsEntering(PlayerPedId()) == VehicleToEnterAsPassenger);
                if(IsGettingInAsPassenger)
                {
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
                // Reset PreventAutoShuffleToDriversSeat
                SetPedConfigFlag(PlayerPedId(), 184, false);
            }

            if(!IsPedGettingIntoAVehicle(PlayerPedId()))
            {
                IsGettingInAsPassenger = false;
            }

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

            if(FramesEnterControlHeld >= PassengerEntryActivationFrames && SecondsEnterControlHeld >= PassengerEntryActivationSeconds)
            {
                int playerPed = PlayerPedId();
                if (IsPedGettingIntoAVehicle(playerPed))
                {
                    int vehicle = GetVehiclePedIsEntering(playerPed);

                    bool isRunning = IsPedRunning(playerPed);
                    if (!IsGettingInAsPassenger)
                    {
                        // PreventAutoShuffleToDriversSeat
                        SetPedConfigFlag(playerPed, 184, true);
                        VehicleToEnterAsPassenger = vehicle;
                        TaskEnterVehicle(playerPed, vehicle, 10000, (int)VehicleSeat.Passenger, isRunning ? 2f : 1f, 1, 0);
                        DisableControlAction(0, (int)Control.Enter, true);
                    }
                    IsGettingInAsPassenger = true;
                }
            }
        }
    }
}
