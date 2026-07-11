using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    internal static class CarModHelper
    {
        private static readonly VehicleModType[] s_GlobalMaxedOutMods =
        {
            VehicleModType.Engine,
            VehicleModType.Turbo,
            VehicleModType.Transmission,
            VehicleModType.Brakes,
            VehicleModType.Armor,
        };

        private static readonly uint s_TulipHash = (uint)GetHashKey("tulip");

        internal static void ApplyModsForSpecialSpawnedCar(int vehicle, uint model)
        {
            // everyone gets bulletproofs
            SetVehicleBodyHealth(vehicle, 1000f);
            SetVehicleEngineHealth(vehicle, 1000f);
            SetVehicleFixed(vehicle);
            SetVehiclePetrolTankHealth(vehicle, 1000f);
            short wheelCount = (short)GetVehicleNumberOfWheels(vehicle);
            for (short i = 0; i < wheelCount; i++)
            {
                SetVehicleWheelHealth(vehicle, i, 1000f);
                SetTyreHealth(vehicle, i, 1000f);
                SetVehicleTyreFixed(vehicle, i);
                SetVehicleTyreBurst(vehicle, i, false, 0f);
                SetVehicleTyresCanBurst(vehicle, false);
            }
            ResetVehicleWheels(vehicle, true);

            SetVehicleModKit(vehicle, 0);
            // everyone gets max mods
            foreach (VehicleModType modType in s_GlobalMaxedOutMods)
            {
                int allowedMods = GetNumVehicleMods(vehicle, (int)modType);
                if (allowedMods > 0)
                {
                    SetVehicleMod(vehicle, (int)modType, allowedMods - 1, false);
                }
            }

            if(model == s_TulipHash)
            {
                SetVehicleColours(vehicle, (int)VehicleColor.MetallicSunriseOrange, (int)VehicleColor.MetallicSilver);
                SetVehicleExtraColours(vehicle, (int)VehicleColor.MetallicRaceYellow, (int)VehicleColor.MetallicBlack);
                // livery
                SetVehicleMod(vehicle, 48, 0, false);
            }
        }

        internal static void TickSpecialVehicleProperties(int vehicle, uint model)
        {
            // SUVs and bikes need a bit extra oomph
            switch (model)
            {
                case (uint)VehicleHash.Pranger:
                case (uint)VehicleHash.Sheriff2:
                    SetVehicleEnginePowerMultiplier(vehicle, 3f);
                    SetVehicleEngineTorqueMultiplier(vehicle, 2.25f);
                    break;
                case (uint)VehicleHash.Policeb:
                    SetVehicleEnginePowerMultiplier(vehicle, 2.6f);
                    SetVehicleEngineTorqueMultiplier(vehicle, 2.6f);
                    break;
                case (uint)VehicleHash.Police:
                case (uint)VehicleHash.Sheriff:
                case (uint)VehicleHash.Police4:
                    SetVehicleEnginePowerMultiplier(vehicle, 2f);
                    SetVehicleEngineTorqueMultiplier(vehicle, 2.2f);
                    break;
            }

            short wheelCount = (short)GetVehicleNumberOfWheels(vehicle);
            for (short i = 0; i < wheelCount; i++)
            {
                SetVehicleWheelHealth(vehicle, i, 1000f);
                SetTyreHealth(vehicle, i, 1000f);
                SetVehicleTyreFixed(vehicle, i);
                SetVehicleTyreBurst(vehicle, i, false, 0f);

                if (model == s_TulipHash)
                {
                    SetVehicleWheelBrakePressure(vehicle, i, 2f);
                }
            }

            if(model == s_TulipHash)
            {
                SetVehicleEnginePowerMultiplier(vehicle, 4f);
                SetVehicleEngineTorqueMultiplier(vehicle, 3.1f);
            }
        }
    }
}
