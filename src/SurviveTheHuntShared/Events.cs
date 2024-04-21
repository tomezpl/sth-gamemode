using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntShared
{
    /// <summary>
    /// Event names that need to be registered on either the server or the client.
    /// </summary>
    /// <remarks>Each event name NEEDS to be prefixed by "sth:".</remarks>
    public static class Events
    {
        public static string EventNamePrefixed(this string eventName)
        {
            if(eventName.StartsWith("sth:"))
            {
                return eventName;
            }

            return $"sth:{eventName}";
        }

        public static string EventName(this string eventName)
        {
            if(eventName.StartsWith("sth:"))
            {
                return eventName.Substring("sth:".Length);
            }

            return eventName;
        }

        /// <summary>
        /// Client script event names.
        /// </summary>
        public static class Client
        {
            public const string ReceiveDeleteVehicle = "sth:recvDeleteVehicle";
            public const string ReceiveSyncedVehicles = "sth:recvSyncVehicles";
            public const string ReceiveConfig = "sth:receiveConfig";
            public const string MarkPlayerDeath = "sth:markPlayerDeath";
            public const string ReceiveCleanClothes = "sth:cleanClothesForPlayer";
            public const string NotifyHuntedPlayer = "sth:notifyHuntedPlayer";
            public const string NotifyHunters = "sth:notifyHunters";
            public const string NotifyWinner = "sth:notifyWinner";
            public const string HuntStartedByServer = "sth:huntStartedByServer";
            public const string ShowPingOnMap = "sth:showPingOnMap";
            public const string NotifyAboutHuntedZone = "sth:notifyAboutHuntedZone";
            public const string ReceiveTimeSync = "sth:receiveTimeSync";
        }

        /// <summary>
        /// Server script event names.
        /// </summary>
        public static class Server
        {
            public const string RequestCleanClothes = "sth:cleanClothes";
            public const string RequestDeleteVehicle = "sth:reqDeleteVehicle";
            public const string PlayerDied = "sth:playerDied";
            public const string BroadcastHuntedZone = "sth:broadcastHuntedZone";
            public const string RequestSyncVehicles = "sth:reqSyncVehicles";
            public const string ClientStarted = "sth:clientStarted";
            public const string RequestStartHunt = "sth:startHunt";
        }
    }
}
