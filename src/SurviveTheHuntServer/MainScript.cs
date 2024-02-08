using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using CitizenFX.Core.Native;
using SurviveTheHuntServer.Helpers;
using SurviveTheHuntServer.Utils;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public partial class MainScript : BaseScript
    {
        private readonly Hunt Hunt;

        public readonly GameState GameState = new GameState();

        public readonly Random RNG = new Random((int)DateTime.UtcNow.Ticks);

        /// <summary>
        /// UTC time when time was last synced with the clients (to prevent client-side timers desyncing).
        /// </summary>
        private DateTime LastTimeSync = DateTime.UtcNow;

        /// <summary>
        /// Gamemode-specific network-aware events triggerable from the client(s).
        /// </summary>
        protected Dictionary<string, Action<dynamic>> STHEvents;

        private readonly HuntedQueue HuntedPlayerQueue = null;

        private readonly Config Config = null;

        /// <summary>
        /// Entities that are created by the server and should also be deleted via an RPC upon <see cref="Shutdown"/>.
        /// </summary>
        public readonly List<int> EntityHandles = new List<int>();

        /// <summary>
        /// Cars that have been spawned by the /spawncars command and need to be deleted the next time it's called.
        /// </summary>
        public readonly List<int> SpawnedVehicles = new List<int>();

        private bool PendingDistanceCullRadiusReset = false;

        private Player LastPlayerToSpawnCars = null;

        public class BlipUpdateRequest
        {
            public int PlayerEntityNetworkId { get; set; }
            public int PlayerIndex { get; set; }
            public Player Player { get; set; }
        }

        public static List<BlipUpdateRequest> BlipsToUpdate = new List<BlipUpdateRequest>();

        public MainScript()
        {
            if (GetCurrentResourceName() != Constants.ResourceName)
            {
                try
                {
                    throw new Exception($"Survive the Hunt: Invalid resource name! Resource name should be {Constants.ResourceName}");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            else
            {
                Hunt = new PantoHunt(Players);

                EventHandlers["onServerResourceStart"] += new Action<string>(OnServerResourceStart);
                EventHandlers["onResourceStop"] += new Action<string>(OnResourceStop);
                EventHandlers["playerJoining"] += new Action<Player, string>(PlayerJoining);
                EventHandlers["playerDropped"] += new Action<Player, string>(PlayerDisconnected);

                CreateEvents();

                foreach (KeyValuePair<string, Action<dynamic>> ev in STHEvents)
                {
                    EventHandlers[$"sth:{ev.Key}"] += ev.Value;
                }

                EventHandlers["sth:clientStarted"] += new Action<Player>(ClientStarted);

                Tick += UpdateLoop;

                HuntedPlayerQueue = Hunt.InitHuntedQueue(Players);

                Config = new Config();
                BroadcastConfig(Config);
            }
        }

        protected void PlayerDisconnected([FromSource] Player player, string reason)
        {
            HuntedPlayerQueue.RemovePlayer(player);
        }

        protected void PlayerJoining([FromSource] Player player, string oldId)
        {
            if (string.IsNullOrWhiteSpace(player?.Name))
            {
                Console.WriteLine("Joining player name was null");
            }
            else
            {
                Console.WriteLine($"{player.Name} is joining; syncing time offset now.");
                TriggerClientEvent(player, "sth:receiveTimeSync", new { CurrentServerTime = DateTime.UtcNow.ToString("F", CultureInfo.InvariantCulture) });

                HuntedPlayerQueue.AddPlayer(player);
            }
        }

        private async Task UpdateLoop()
        {
            if (DateTime.UtcNow >= LastTimeSync + Constants.TimeSyncInterval)
            {
                TriggerClientEvent("sth:receiveTimeSync", new { CurrentServerTime = DateTime.UtcNow.ToString("F", CultureInfo.InvariantCulture) });
                LastTimeSync = DateTime.UtcNow;
            }

            if (PendingDistanceCullRadiusReset)
            {
                Debug.WriteLine("SetEntityDistanceCullingRadius needs to be called again!");
                bool allEntitiesExist = true;
                foreach (int vehicle in SpawnedVehicles)
                {
                    bool vehicleExists = DoesEntityExist(vehicle);
                    allEntitiesExist = vehicleExists && allEntitiesExist;

                    if (vehicleExists)
                    {
                        SetEntityDistanceCullingRadius(vehicle, float.MaxValue);
                    }
                }

                PendingDistanceCullRadiusReset = !allEntitiesExist;

                if (LastPlayerToSpawnCars != null && allEntitiesExist)
                {
                    Debug.WriteLine("(spawncars) All vehicles created, applying mods...");
                    // woohoo we're sending the cars' netIds as semicolon-separated values in a string because fivem likes to just not bind lists/arrays correctly at random in the client script?
                    TriggerClientEvent(LastPlayerToSpawnCars, "sth:applyCarMods", SpawnedVehicles.Select(handle => $"{NetworkGetNetworkIdFromEntity(handle)}").Aggregate((acc, curr) => string.IsNullOrWhiteSpace(acc) ? curr : $"{acc};{curr}"));
                    GlobalState.Set("sth:isSpawningCars", false, true);
                }
            }

            for(int i = 0; i < BlipsToUpdate.Count; i++)
            {
                Player player = BlipsToUpdate[i].Player;
                int playerServerId = BlipsToUpdate[i].PlayerIndex;
                int pedNetId = BlipsToUpdate[i].PlayerEntityNetworkId;
                int ped = NetworkGetEntityFromNetworkId(pedNetId);
                if (DoesEntityExist(ped) && player.Character?.Handle == ped)
                {
                    bool isHunted = GameState.Hunt.HuntedPlayer?.Handle == player.Handle;
                    Debug.WriteLine($"Updating player blip for {player.Name}.\n\tChanged from {player.Character?.Handle} (net: {player.Character?.NetworkId} to {ped} (net: {pedNetId}).\n\tHandle: {player.Handle}\n{(isHunted ? "\tThey are hunted\n" : "")}");
                    TriggerClientEvent("sth:updatePlayerBlip", pedNetId, playerServerId, player.Name, isHunted);
                    BlipsToUpdate.RemoveAt(i);
                    i--;
                }
            }

            Hunt.Tick(this);
        }

        public void TriggerClientEventProxy(string eventName, params object[] args)
        {
            TriggerClientEvent(eventName, args);
        }

        public void TriggerClientEventProxy(Player player, string eventName, params object[] args)
        {
            TriggerClientEvent(player, eventName, args);
        }

        [EventHandler("sth:invalidatePlayerPed")]
        private void InvalidatePlayerPed([FromSource] Player src, int ped, int playerServerId)
        {
            SetEntityDistanceCullingRadius(NetworkGetEntityFromNetworkId(ped), float.MaxValue);
            BlipsToUpdate.Add(new BlipUpdateRequest 
            { 
                PlayerEntityNetworkId = ped, 
                PlayerIndex = playerServerId,
                Player = src
            });
        }

        /// <summary>
        /// Notify players from the winning team that they won the game.
        /// </summary>
        public void NotifyWinner()
        {
            TriggerClientEvent("sth:notifyWinner", new { WinningTeam = (int)GameState.Hunt.WinningTeam });
        }

        [EventHandler("sth:spawnCars")]
        private async void SpawnCarsCommand([FromSource] Player source)
        {
            if(GlobalState.Get("sth:isSpawningCars") == true)
            {
                Debug.WriteLine("Received sth:spawnCars but the previous invocation of this command has not finished yet.");
                return;
            }

            GlobalState.Set("sth:isSpawningCars", true, true);

            List<int> carsToSpawn = new List<int>(Constants.CarSpawnPoints.Length);

            Debug.WriteLine("Getting hash keys");
            List<int> spawnableCars = Constants.Vehicles.Select(vehName => GetHashKey(vehName)).ToList();

            Debug.WriteLine("Picking random cars");
            for (int i = 0; i < carsToSpawn.Capacity; i++)
            {
                int randomIndex = RNG.Next(0, spawnableCars.Count);
                int randomVehicle = spawnableCars[randomIndex];
                carsToSpawn.Add(randomVehicle);

                spawnableCars.RemoveAt(randomIndex);
            }

            Debug.WriteLine("Removing already spawned vehicles.");
            foreach (int vehicleToDelete in SpawnedVehicles)
            {
                if (DoesEntityExist(vehicleToDelete))
                {
                    DeleteEntity(vehicleToDelete);

                    await Delay(1);
                }
                EntityHandles.Remove(vehicleToDelete);
            }
            SpawnedVehicles.Clear();

            int counter = 0;
            //int[] spawnedEntities = new int[carsToSpawn.Count];
            foreach (int vehicle in carsToSpawn)
            {
                Coord spawnPoint = Constants.CarSpawnPoints[counter];
                Vector3 spawnPos = spawnPoint.Position;
                
                Debug.WriteLine("Creating a vehicle");
                int spawnedVehicle = CreateVehicle((uint)vehicle, spawnPos.X, spawnPos.Y, spawnPos.Z, spawnPoint.Heading, true, false);
                SpawnedVehicles.Add(spawnedVehicle);
                //spawnedEntities[counter] = Entity.FromNetworkId(spawnedVehicle).NetworkId;
                Debug.WriteLine("Setting huge distance culling radius");
                await Delay(3);
                
                counter++;
            }

            LastPlayerToSpawnCars = source;

            PendingDistanceCullRadiusReset = true;
        }

        private void Shutdown()
        {
            Debug.WriteLine("Shutting down");

            Hunt.Shutdown(this);

            foreach(int entity in EntityHandles)
            {
                DeleteEntity(entity);
            }

            EntityHandles.Clear();
        }

        /// <summary>
        /// Populates <see cref="STHEvents"/> with gamemode-specific event handlers.
        /// </summary>
        private void CreateEvents()
        {
            STHEvents = new Dictionary<string, Action<dynamic>>
            {
                {
                    "playerDied", new Action<dynamic>(data =>
                    {
                        int playerId = data.PlayerId;

                        Console.WriteLine($"Player died: {GetPlayerName($"{playerId}")}");

                        // Did the hunted player die?
                        if(Hunt.CheckPlayerDeath(Players[GetPlayerName($"{playerId}")], GameState))
                        {
                            NotifyWinner();
                        }

                        // Mark the player's death location with a blip for everyone.
                        TriggerClientEvent("sth:markPlayerDeath", data.PlayerPosX, data.PlayerPosY, data.PlayerPosZ, data.PlayerTeam);
                    })
                },
                {
                    "startHunt", new Action<dynamic>(data =>
                    {
                        Player randomPlayer = Hunt.ChooseRandomPlayer(Players);

                        GameState.Hunt.LastHuntedPlayer = randomPlayer;

                        TriggerClientEvent(randomPlayer, "sth:notifyHuntedPlayer");
                        TriggerClientEvent("sth:notifyHunters", new { HuntedPlayerName = randomPlayer.Name });

                        GameState.Hunt.Begin(randomPlayer, Players);

                        TriggerClientEvent("sth:huntStartedByServer", new { EndTime = GameState.Hunt.EndTime.ToString("F", CultureInfo.InvariantCulture), NextNotification = (float)Constants.HuntedPingInterval.TotalSeconds });
                    })
                },
                {
                    "broadcastHuntedZone", new Action<dynamic>(data =>
                    {
                        Vector3 pos = data.Position;
                        TriggerClientEvent("sth:notifyAboutHuntedZone", new { PlayerName = GameState.Hunt.HuntedPlayer.Name, Position = pos, NextNotification = (float)Constants.HuntedPingInterval.TotalSeconds });
                    })
                }
            };
        }
    }
}
