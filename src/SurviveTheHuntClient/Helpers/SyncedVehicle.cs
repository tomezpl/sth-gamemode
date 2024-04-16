using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Helpers
{
    public class SyncedVehicle
    {
        private readonly Func<Entity> _vehicleGetter;

        public Vehicle Vehicle { get => (Vehicle)(_vehicleGetter()); }

        private SyncedVehicle(int? entityHandle = null, int? netId = null)
        {
            if(entityHandle == null && netId == null)
            {
                throw new ArgumentNullException("Either entityHandle or netId needs to be a non-null value.");
            }

            if(entityHandle.HasValue)
            {
                _vehicleGetter = () => Vehicle.FromHandle(entityHandle.Value);
            }
            else if(netId.HasValue)
            {
                _vehicleGetter = () => Vehicle.FromNetworkId(netId.Value);
            }
        }

        public static SyncedVehicle FromNetId(int netId)
        {
            return new SyncedVehicle(netId: netId);
        }

        public static SyncedVehicle FromHandle(int entityHandle)
        {
            return new SyncedVehicle(entityHandle: entityHandle);
        }
    }
}
