using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Xmas.Models
{
    internal class SleighStorageVehicle
    {
        public readonly VehicleHash Hash;
        private Vector3 _offset;

        internal SleighStorageVehicle(string modelName, Vector3 attachOffset = default) : this((VehicleHash)GetHashKey(modelName), attachOffset)
        {
        }

        internal SleighStorageVehicle(VehicleHash modelHash, Vector3 attachOffset = default)
        {
            if (attachOffset == default)
            {
                attachOffset = Vector3.Zero;
            }

            _offset = attachOffset;
            Hash = modelHash;
        }
    }
}
