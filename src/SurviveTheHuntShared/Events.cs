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
            public const string ReceiveGameState = "sth:receiveGameState";
            public const string ReceiveHuntedClock = "sth:receiveHuntedClock";

            public const string CharCreatorPedChanged = "lbg-char-neo:pedChanged";
            public const string CharCreatorForceExit = "lbg-char-neo:forceExit";
            public const string CharCreatorCreatorExited = "lbg-char-neo:creatorExited";
            public const string CharCreatorCreatorEntered = "lbg-char-neo:creatorEntered";

            /// <summary>
            /// The hunted player's client should receive this event before relaying the current in-game clock to a newly joined player via the server.
            /// </summary>
            public const string ReceiveClockSyncRequest = "sth:receiveHuntedClockSyncRequest";

            /// <summary>
            /// A client-side event that will run the UI functions to display a notification about a player's death.
            /// </summary>
            public const string DisplayKill = "sth:displayKill";

            public const string SpawnCars = "sth:spawnCars";
            public const string Heal = "sth:heal";
            public const string Respawn = "sth:respawn";

            public const string ReceiveVehicleSpawnPermission = "sth:recvSpawnVehiclesPerm";

            public const string TeleportToSpawn = "sth:goToSpawn";

            public const string XmasReceivePresentsLocations = "sth:xmas:recvPrezzieLocations";
            public const string XmasReceiveDeliveryUpdate = "sth:xmas:recvDeliveryUpdate";
            public const string XmasReceiveCapturableUpdate = "sth:xmas:recvCapturableUpdate";
            public const string XmasReceiveHuntedCaptured = "sth:xmas:recvHuntedCaptured";
            public const string XmasReceiveSleighSpawn = "sth:xmas:recvSleighSpawn";
            public const string XmasReceiveSantaSpawn = "sth:xmas:recvSantaSpawn";

            public const string UIRecvGameModes = "sth:ui:recvGameModes";
            public const string UISendText = "sth:ui:sendText";
            public const string UISetItemBlocked = "sth:ui:setBlockItem";

            public const string RecvNetEntity = "sth:recvNetId";

            public const string CupidReceiveRevivable = "sth:cupid:recvRevivable";
            public const string CupidReceiveEndRevive = "sth:cupid:recvEndRevive";
            public const string CupidReceiveStartRevive = "sth:cupid:recvStartRevive";
            public const string CupidReceiveSyncState = "sth:cupid:recvJobSyncState";
            public const string CupidReceiveHeatScore = "sth:cupid:recvHeatScore";
            public const string CupidReceiveSpecialEvent = "sth:cupid:recvSpecialEvent";
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
            public const string RequestSpawnVehiclesPermission = "sth:reqSpawnVehicles";
            public const string NotifySpawnedVehicles = "sth:ackSpawnedVehicles";
            public const string RequestSyncVehicles = "sth:reqSyncVehicles";
            public const string ClientStarted = "sth:clientStarted";
            public const string RequestStartHunt = "sth:startHunt";
            public const string ReceiveHuntedClock = "sth:receiveClockFromHuntedPlayer";

            /// <summary>
            /// A newly joined client will trigger this server event, which will then send <see cref="Client.ReceiveClockSyncRequest"/> to the hunted player,
            /// which in turn sends back the in-game clock value to the server, which will finally send it back to the newly joined client.
            /// </summary>
            public const string HuntedClockSyncRequested = "sth:huntedClockSyncRequested";

            public const string XmasBroadcastPresentsLocations = "sth:xmas:broadcastPrezzies";
            public const string XmasNotifyDeliveredPresent = "sth:xmas:notifyPrezzieDelivery";
            public const string XmasBroadcastHuntedCapturableState = "sth:xmas:broadcastCapturableState";
            public const string XmasBroadcastHuntedCaptured = "sth:xmas:broadcastCaptured";
            public const string XmasBroadcastSleighSpawn = "sth:xmas:broadcastSleigh";
            public const string XmasBroadcastSantaSpawn = "sth:xmas:broadcastSantaSpawn";

            public const string NotifyNetEntity = "sth:notifyNetId";

            public const string CupidNotifyRevivable = "sth:cupid:notifyRevivable";
            public const string CupidNotifyReviveEnd = "sth:cupid:notifyReviveEnd";
            public const string CupidNotifyReviveStart = "sth:cupid:notifyReviveStart";
            public const string CupidJobSyncState = "sth:cupid:jobSyncState";
            public const string CupidNotifyNewHeatScore = "sth:cupid:notifyHeatScore";
            public const string CupidBroadcastSpecialEvent = "sth:cupid:broadcastSpecialEvent";
            public const string SetServerState = "sth:setServerState";
        }
    }
}
