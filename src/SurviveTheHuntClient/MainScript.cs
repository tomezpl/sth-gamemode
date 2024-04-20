using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CitizenFX.Core;
using SurviveTheHuntClient.Helpers;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public class MainScript : ClientScript
    {
        /// <summary>
        /// Local player state maintained by the client script.
        /// </summary>
        protected PlayerState PlayerState = new PlayerState();

        /// <summary>
        /// Game state synced from the server.
        /// </summary>
        protected GameState GameState = new GameState();

        /// <summary>
        /// Event handlers specific to this implementation of the Survive the Hunt gamemode.
        /// </summary>
        protected Dictionary<string, Action<dynamic>> STHEvents;

        protected readonly Random RNG = new Random();

        /// <summary>
        /// Vehicles spawned by the gamemode at the spawn area. This is used to delete vehicles on the next <see cref="SpawnCars"/> call.
        /// </summary>
        protected List<SyncedVehicle> SpawnedVehicles = new List<SyncedVehicle>();

        /// <summary>
        /// Do <see cref="SpawnedVehicles"/> need their netIDs being sent to the server?
        /// </summary>
        private bool SpawnedVehiclesNeedSync = false;

        /// <summary>
        /// Is the player currently spawning cars? Prevent the spawn cars command from being invoked before the previous one finished.
        /// </summary>
        private bool IsSpawningCars = false;

        private Vector3 PlayerPos = Vector3.Zero;

        /// <summary>
        /// Blips used to represent player deaths on the radar.
        /// </summary>
        private readonly DeathBlips DeathBlips;

        public MainScript()
        {
            EventHandlers["onClientGameTypeStart"] += new Action<string>(OnClientGameTypeStart);
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
            EventHandlers["onResourceStopping"] += new Action<string>(OnResourceStopping);

            CreateEvents();
            foreach(KeyValuePair<string, Action<dynamic>> ev in STHEvents)
            {
                EventHandlers[$"sth:{ev.Key}"] += ev.Value;
            }

            DeathBlips = new DeathBlips(GetConvarInt("sth_deathbliplifespan", Constants.DefaultDeathBlipLifespan));
        }

        protected void OnResourceStopping(string resourceName)
        {
            // Only perform cleanup if the resource stopped was sth-gamemode.
            if(GetCurrentResourceName() != resourceName)
            {
                return;
            }

            foreach (SyncedVehicle vehicleToDelete in SpawnedVehicles)
            {
                Vehicle vehicle = vehicleToDelete.Vehicle;
                if (vehicle != null && vehicle.Exists())
                {
                    vehicle.Delete();
                }
            }
            SpawnedVehicles.Clear();

            Debug.WriteLine("Checking hunted player mugshot...");
            if(GameState.Hunt.HuntedPlayerMugshot != null)
            {
                Debug.WriteLine("Hunted player mugshot not null, deleting.");
                UnregisterPedheadshot(GameState.Hunt.HuntedPlayerMugshot.Id);
                GameState.Hunt.HuntedPlayerMugshot = null;
            }
        }

        protected void OnClientGameTypeStart(string resourceName)
        {
            // Since this event fires for several resources, not just the one from the current script, terminate early to avoid re-doing the init.
            if(GetCurrentResourceName() != resourceName)
            {
                return;
            }

            // Enable autospawn.
            Exports["spawnmanager"].setAutoSpawnCallback(new Action(AutoSpawnCallback));
            Exports["spawnmanager"].setAutoSpawn(true);
            Exports["spawnmanager"].forceRespawn();

            EventHandlers["playerSpawned"] += new Action(PlayerSpawnedCallback);

            Tick += UpdateLoop;
        }

        private void OnClientResourceStart(string resource)
        {
            // This event is fired for every client resource started.
            // We need to check that the resource name is sth-gamemode so we only perform init once!
            if (resource == Constants.ResourceName)
            {
                RegisterCommand("respawn", new Action(() =>
                {
                    Game.PlayerPed.HealthFloat = 0f;
                    TriggerEvent("baseevents:onPlayerKilled");
                }), false);

                RegisterCommand("starthunt", new Action(() =>
                {
                    TriggerServerEvent("sth:startHunt");
                }), false);

                RegisterCommand("spawncars", new Action(async () =>
                {
                    if (!IsSpawningCars)
                    {
                        IsSpawningCars = true;
                        await SpawnCars();
                        IsSpawningCars = false;
                    }
                }), false);

                Vector3 spawn = Constants.DockSpawn;
                ClearAreaOfEverything(spawn.X, spawn.Y, spawn.Z, 1000f, false, false, false, false);

                // Notify the server this client has started so the config can be sent down. This is needed for resource restarts etc.
                TriggerServerEvent("sth:clientStarted");
            }
        }

        /// <summary>
        /// Spawns random cars (picked from <see cref="Constants.Vehicles"/>) in the start area.
        /// </summary>
        protected async Task SpawnCars()
        {
            bool hasVehicleControl = true;
            for (int i = 0; i < 5; i++)
            {
                hasVehicleControl = true;
                foreach (SyncedVehicle spawnedVehicle in SpawnedVehicles)
                {
                    Vehicle vehicle = spawnedVehicle.Vehicle;
                    SetEntityAsMissionEntity(vehicle.Handle, true, true);
                    if (NetworkGetEntityOwner(vehicle.Handle) != PlayerId())
                    {
                        hasVehicleControl = false;
                        NetworkRequestControlOfEntity(vehicle.Handle);
                    }
                }

                if(!hasVehicleControl)
                {
                    Debug.WriteLine("Waiting 1s for vehicle control...");
                    await Delay(1000);
                }
            }

            List<VehicleHash> carsToSpawn = new List<VehicleHash>(Constants.CarSpawnPoints.Length);

            List<VehicleHash> spawnableCars = Constants.Vehicles.ToList();

            for(int i = 0; i < carsToSpawn.Capacity; i++)
            {
                int randomIndex = RNG.Next(0, spawnableCars.Count);
                VehicleHash randomVehicle = spawnableCars[randomIndex];
                carsToSpawn.Add(randomVehicle);

                spawnableCars.RemoveAt(randomIndex);
            }

            foreach(SyncedVehicle vehicleToDelete in SpawnedVehicles)
            {
                Vehicle vehicle = vehicleToDelete.Vehicle;
                if (vehicle != null && vehicle.Exists())
                {
                    Debug.WriteLine($"Deleting vehicle {vehicle.Handle}");
                    vehicle.Delete();
                }
            }
            SpawnedVehicles.Clear();

            int counter = 0;
            foreach(VehicleHash vehicle in carsToSpawn)
            {
                if(!IsModelInCdimage((uint)vehicle) || !IsModelAVehicle((uint)vehicle))
                {
                    continue;
                }
                RequestModel((uint)vehicle);
                await Delay(50);

                Coord spawnPoint = Constants.CarSpawnPoints[counter];
                Vector3 spawnPos = spawnPoint.Position;

                Vehicle spawnedVehicle = new Vehicle(CreateVehicle((uint)vehicle, spawnPos.X, spawnPos.Y, spawnPos.Z, spawnPoint.Heading, true, true));
                SpawnedVehicles.Add(SyncedVehicle.FromHandle(spawnedVehicle.Handle));

                SetEntityAsMissionEntity(spawnedVehicle.Handle, true, true);

                SetVehicleModKit(spawnedVehicle.Handle, 0);

                // Set all vehicle mods to maximum.
                for (int i = 0; i < 50; i++)
                {
                    int nbMods = GetNumVehicleMods(spawnedVehicle.Handle, i);
                    if(nbMods > 0)
                    {
                        SetVehicleMod(spawnedVehicle.Handle, i, nbMods - 1, false);
                    }
                }
                // Add neons.
                for(int i = 0; i < 4; i++)
                {
                    SetVehicleNeonLightEnabled(spawnedVehicle.Handle, i, RNG.NextDouble() >= 0.5);
                }
                SetVehicleNeonLightsColour(spawnedVehicle.Handle, RNG.Next(0, 255), RNG.Next(0, 255), RNG.Next(0, 255));
                SetVehicleXenonLightsColour(spawnedVehicle.Handle, RNG.Next(0, 12));

                SetVehicleCustomPrimaryColour(spawnedVehicle.Handle, RNG.Next(0, 255), RNG.Next(0, 255), RNG.Next(0, 255));
                SetVehicleCustomSecondaryColour(spawnedVehicle.Handle, RNG.Next(0, 255), RNG.Next(0, 255), RNG.Next(0, 255));

                // lol
                if(vehicle == VehicleHash.Bmx)
                {
                    spawnedVehicle.EnginePowerMultiplier = 100f;
                    spawnedVehicle.EngineTorqueMultiplier = 100f;
                }

                counter++;
            }
            SpawnedVehiclesNeedSync = true;
        }

        protected void AutoSpawnCallback()
        {
            Vector3 spawnLoc = Constants.DockSpawn;

            Exports["spawnmanager"].spawnPlayer(new { x = spawnLoc.X, y = spawnLoc.Y, z = spawnLoc.Z, model = "a_m_m_skater_01" });
        }

        protected void PlayerSpawnedCallback()
        {
            // Refresh player's death state.
            PlayerState.DeathReported = false;

            // Indicate that weapons need to be given to the player again.
            PlayerState.WeaponsGiven = false;
            PlayerState.ForcedUnarmed = false;

            TriggerServerEvent("sth:cleanClothes", new { PlayerId = GetPlayerServerId(PlayerId()) });

            // Enable friendly fire.
            NetworkSetFriendlyFireOption(true);
            SetCanAttackFriendly(PlayerPedId(), true, true);

            if(GameState.Hunt.IsInProgress || GameState.Hunt.IsEnding)
            {
                Debug.WriteLine($"GameState: Hunt.IsInProgress: {GameState.Hunt.IsInProgress}, Hunt.IsEnding: {GameState.Hunt.IsEnding}");
                HuntUI.DisplayObjective(ref GameState, ref PlayerState, GameState.Hunt.IsEnding);
            }
        }

        protected async Task UpdateLoop()
        {
            if (Game.PlayerPed != null)
            {
                ResetPlayerStamina(PlayerId());
                PlayerPos = Game.PlayerPed.Position;
            }

            GameState.Hunt.UpdateHuntedMugshot();
            HuntUI.SetBigmap(ref PlayerState);
            HuntUI.DrawRemainingTime(ref GameState);
            HuntUI.FadeBlips();
            HuntUI.UpdateTeammateBlips(Players, ref GameState, ref PlayerState);

            PlayerState.UpdateWeapons(Game.PlayerPed);

            // Make sure the player can't get cops.
            ClearPlayerWantedLevel(PlayerId());

            // Check and report player death to the server if needed.
            if(!Game.Player.IsAlive && !PlayerState.DeathReported)
            {
                TriggerServerEvent("sth:playerDied", new { PlayerId = Game.Player.ServerId, PlayerPosX = PlayerPos.X, PlayerPosY = PlayerPos.Y, PlayerPosZ = PlayerPos.Z, PlayerTeam = PlayerState.Team });
                PlayerState.DeathReported = true;
            }

            if (SpawnedVehiclesNeedSync)
            {
                string vehicleNetIdsPacked = "";
                foreach(SyncedVehicle spawnedVehicle in SpawnedVehicles)
                {
                    Vehicle vehicle = spawnedVehicle.Vehicle;
                    if (vehicle != null && vehicle.Exists())
                    {
                        vehicleNetIdsPacked += $"{vehicle.NetworkId};";
                    }
                }

                if(vehicleNetIdsPacked.Length > 1)
                {
                    vehicleNetIdsPacked = vehicleNetIdsPacked.Remove(vehicleNetIdsPacked.Length - 1, 1);
                    TriggerServerEvent("sth:reqSyncVehicles", vehicleNetIdsPacked);
                    SpawnedVehiclesNeedSync = false;
                }
            }

            GameOverCheck();

            FixCarsInSpawn();

            DeathBlips.ClearExpiredBlips();

            Wait(0);
        }

        /// <summary>
        /// Checks if the game should end and calls the right <see cref="GameState"/> methods if so.
        /// </summary>
        protected void GameOverCheck()
        {
            if(!GameState.Hunt.IsEnding)
            {
                return;
            }

            if (GameState.Hunt.ActualEndTime <= Utility.CurrentTime)
            {
                GameState.Hunt.End(ref PlayerState);
                GameState.CurrentObjective = "";
            }
        }

        /// <summary>
        /// Repairs cars spawned in the starting area.
        /// </summary>
        protected void FixCarsInSpawn()
        {
            foreach(Vehicle vehicle in World.GetAllVehicles())
            {
                bool closeToSpawn = 50f >= (Constants.DockSpawn - vehicle.Position).Length();
                vehicle.IsInvincible = closeToSpawn;
                if (closeToSpawn)
                {
                    if (vehicle.Health < vehicle.MaxHealth)
                    {
                        vehicle.Repair();
                        vehicle.Health = vehicle.MaxHealth;
                    }
                }
            }
        }

        /// <summary>
        /// Populates <see cref="STHEvents"/> with gamemode-specific events used by the resource.
        /// </summary>
        private void CreateEvents()
        {
            STHEvents = new Dictionary<string, Action<dynamic>>
            {
                {
                    "cleanClothesForPlayer", new Action<dynamic>(data =>
                    {
                        int playerId = data.PlayerId;

                        ClearPedBloodDamage(GetPlayerPed(GetPlayerFromServerId(playerId)));
                    })
                },
                {
                    "notifyHuntedPlayer", new Action<dynamic>(data =>
                    {
                        //Debug.WriteLine("I'm the hunted!");
                        GameState.Hunt.IsStarted = true;
                        GameState.Hunt.HuntedPlayer = Game.Player;

                        GameState.CurrentObjective = "Survive";
                        PlayerState.Team = Teams.Team.Hunted;

                        Ped playerPed = Game.PlayerPed;
                        PlayerState.TakeAwayWeapons(ref playerPed);
                    })
                },
                {
                    "notifyHunters", new Action<dynamic>(data =>
                    {
                        string huntedPlayerName = data.HuntedPlayerName;

                        // Since the event is sent out to everyone, make sure it is discarded by the hunted player.
                        if(huntedPlayerName == Game.Player.Name)
                        {
                            return;
                        }

                        Ped playerPed = Game.PlayerPed;

                        GameState.Hunt.IsStarted = true;
                        GameState.Hunt.HuntedPlayer = Players[huntedPlayerName];

                        // TODO: Shouldn't current objective technically be player state?
                        GameState.CurrentObjective = " is the hunted! Track them down.";
                        PlayerState.Team = Teams.Team.Hunters;

                        PlayerState.TakeAwayWeapons(ref playerPed);
                    })
                },
                {
                    "notifyWinner", new Action<dynamic>(data =>
                    {
                        int winningTeam = data.WinningTeam;
                        GameState.Hunt.IsOver = true;
                        if((Teams.Team)winningTeam == PlayerState.Team)
                        {
                            GameState.CurrentObjective = "You've won the hunt!";
                        }
                        else
                        {
                            GameState.CurrentObjective = "You've lost the hunt!";
                        }
                        GameState.Hunt.ActualEndTime = Utility.CurrentTime + TimeSpan.FromSeconds(5);
                        HuntUI.DisplayObjective(ref GameState, ref PlayerState, true);
                    })
                },
                {
                    "huntStartedByServer", new Action<dynamic>(data =>
                    {
                        float secondsTillPing = data.NextNotification;
                        GameState.Hunt.NextMugshotTime = Utility.CurrentTime + TimeSpan.FromSeconds(secondsTillPing);

                        string endTimeStr = data.EndTime;
                        DateTime endTime = DateTime.ParseExact(endTimeStr, "F", CultureInfo.InvariantCulture);
                        GameState.Hunt.InitialEndTime = endTime;
                        HuntUI.DisplayObjective(ref GameState, ref PlayerState);
                        Ped playerPed = Game.PlayerPed;
                    })
                },
                {
                    "showPingOnMap", new Action<dynamic>(data =>
                    {
                        string playerName = data.PlayerName;
                        HuntUI.CreateRadiusBlipForPlayer(Players[playerName], data.Radius, data.OffsetX, data.OffsetY, DateTime.ParseExact(data.CreationDate, "F", CultureInfo.InvariantCulture), ref PlayerState);
                        if(playerName == Game.Player.Name)
                        {
                            Vector3 position = GetEntityCoords(PlayerPedId(), false);
                            TriggerServerEvent("sth:broadcastHuntedZone", new { Position = position });
                        }
                    })
                },
                {
                    "notifyAboutHuntedZone", new Action<dynamic>(data =>
                    {
                        string playerName = data.PlayerName;
                        float nextNotificationTimeout = data.NextNotification;
                        GameState.Hunt.NextMugshotTime = Utility.CurrentTime + TimeSpan.FromSeconds(nextNotificationTimeout);
                        HuntUI.NotifyAboutHuntedZone(Players[playerName], data.Position, ref GameState);
                    })
                },
                {
                    "receiveTimeSync", new Action<dynamic>(data =>
                    {
                        string currentServerTimeStr = data.CurrentServerTime;
                        DateTime currentServerTime = DateTime.ParseExact(currentServerTimeStr, "F", CultureInfo.InvariantCulture);
                        Utility.ServerTimeOffset = currentServerTime - DateTime.UtcNow;
                    })
                }
            };

            // Event handler for gamemode config being sent by the server.
            EventHandlers["sth:receiveConfig"] += new Action<byte[], byte[]>((weaponsHunters, weaponsHunted) =>
            {
                Debug.WriteLine("sth:receiveConfig received!");

                // The weapons are sent as byte arrays, and therefore need to be deserialized into WeaponAmmo objects
                Func<byte[], Weapons.WeaponAmmo[]> getWeapons = (weapons) =>
                {
                    Weapons.WeaponAmmo[] output = new Weapons.WeaponAmmo[weapons.Length / (sizeof(uint) + sizeof(ushort))];
                 
                    // Each weapon is uint hash followed by ushort ammo count.
                    byte[] buffer = new byte[sizeof(uint) + sizeof(ushort)];
                    using (MemoryStream ms = new MemoryStream(weapons, false))
                    {
                        while (ms.Position < ms.Length)
                        {
                            // Zero the buffer.
                            Array.Clear(buffer, 0, buffer.Length);

                            // Get the weapon index based on the position in the byte array.
                            long index = ms.Position / (sizeof(uint) + sizeof(ushort));

                            // Read the weapon hash.
                            ms.Read(buffer, 0, sizeof(uint));
                            // Read the ammo count.
                            ms.Read(buffer, sizeof(uint), sizeof(ushort));

                            // Store the weapon hash and ammo count in a WeaponAmmo object.
                            output[index] = new Weapons.WeaponAmmo(BitConverter.ToUInt32(buffer, 0), BitConverter.ToUInt16(buffer, sizeof(uint)));
                        }
                    }

                    return output;
                };

                Weapons.WeaponAmmo[]
                    hunters = getWeapons(weaponsHunters),
                    hunted = getWeapons(weaponsHunted);

                Debug.WriteLine("parsed weapons config!");

                Constants.WeaponLoadouts[Teams.Team.Hunters] = hunters;
                Constants.WeaponLoadouts[Teams.Team.Hunted] = hunted;
            });

            EventHandlers["sth:markPlayerDeath"] += new Action<float, float, float, Teams.Team>((deathPosX, deathPosY, deathPosZ, team) =>
            {
                if (team == PlayerState.Team || string.Equals(GetConvar("sth_globalPlayerDeathBlips", "false"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    DeathBlips.Add(deathPosX, deathPosY, deathPosZ);
                }
            });
        }

        [EventHandler("sth:recvSyncVehicles")]
        public void SyncVehiclesReceived(string vehicleNetIdsPacked)
        {
            int validNetIdCount = 0;
            int[] vehicleNetIds = vehicleNetIdsPacked.Split(';').Select((netIdStr) => {
                if (int.TryParse(netIdStr, out int netId))
                {
                    validNetIdCount++;
                    return netId;
                }
                else
                {
                    return -1;
                }
            }).Where((netId) => netId != -1).ToArray();

            if (validNetIdCount > 0)
            {
                SpawnedVehicles.Clear();
                SpawnedVehicles.AddRange(vehicleNetIds.Select((netId) => SyncedVehicle.FromNetId(netId)));
            }
        }
    }
}
