using System;
using CitizenFX.Core;
using SurviveTheHuntServer.Helpers;
using SurviveTheHuntServer.Models;
using SurviveTheHuntShared.Models;
using StateProp = SurviveTheHuntShared.Plugins.Cupid.Constants.ServerStateKey;

namespace SurviveTheHuntServer.Extensions.Cupid
{
    internal class CupidPluginState : SurviveTheHuntShared.Interfaces.IPluginState
    {
        /// <summary>
        /// Server ID of the first player to have reached the ship's active radius.
        /// </summary>
        private int? _firstPlayerOnShip = null;

        internal static CupidPluginState Create()
        {
            return new CupidPluginState();
        }

        public object Get(byte key)
        {
            switch((StateProp)key)
            {
                case StateProp.ShipOwner:
                    return _firstPlayerOnShip;
            }

            throw new ArgumentOutOfRangeException(nameof(key));
        }

        public T Get<T>(byte key)
        {
            return (T)Get(key);
        }

        public TRet Get<TRet, TEnum>(TEnum key) where TEnum : IComparable
        {
            return (TRet)Get(Convert.ToByte(key));
        }

        public void Set<TEnum>(TEnum key, object value, PluginContextBase pluginContext = null) where TEnum : IComparable
        {
            if(Enum.IsDefined(typeof(StateProp), Convert.ToInt32(key)))
            {
                StateProp keyCast = (StateProp)Convert.ToInt32(key);
                Debug.WriteLine($"Setting value for key {keyCast}");
                SetImpl(keyCast, value, (PluginEventContext)pluginContext);
            }
            else
            {
                Debug.WriteLine($"{key} is not a valid {nameof(StateProp)}");
            }
        }

        private void SetImpl(StateProp key, object value, PluginEventContext pluginContext)
        {
            switch(key)
            {
                // Whoever claims to be first at the ship, gets ownership
                case StateProp.ShipOwner:
                    const int FirstPlayerOnShipServerIdClientStatePropId = 8;
                    if (!_firstPlayerOnShip.HasValue && value != null)
                    {
                        _firstPlayerOnShip = Convert.ToInt32(value);
                        Debug.WriteLine($"Sending {SurviveTheHuntShared.Events.Client.CupidReceiveSyncState} to all clients to sync ship owner");
                        pluginContext.TriggerClientEvent(SurviveTheHuntShared.Events.Client.CupidReceiveSyncState, "ship", FirstPlayerOnShipServerIdClientStatePropId, _firstPlayerOnShip);
                    }
                    else
                    {
                        if(_firstPlayerOnShip.HasValue && value == null && _firstPlayerOnShip.Value == Convert.ToInt32(pluginContext.Sender.Handle))
                        {
                            _firstPlayerOnShip = null;
                            Debug.WriteLine($"Sending {SurviveTheHuntShared.Events.Client.CupidReceiveSyncState} to all clients to migrate ship owner (previous: {pluginContext.Sender.Name} [id: {pluginContext.Sender.Handle}])");
                            pluginContext.TriggerClientEvent(SurviveTheHuntShared.Events.Client.CupidReceiveSyncState, "ship", FirstPlayerOnShipServerIdClientStatePropId, null);
                        }
                    }
                    break;
            }
        }
    }
}
