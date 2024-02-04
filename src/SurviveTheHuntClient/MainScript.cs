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
        /// <remarks>TODO: Move this to server-side so any player can spawn vehicles with the previous batch deleting properly?</remarks>
        protected List<Vehicle> SpawnedVehicles = new List<Vehicle>();

        private Vector3 PlayerPos = Vector3.Zero;

        /// <summary>
        /// Blips used to represent player deaths on the radar.
        /// </summary>
        private readonly DeathBlips DeathBlips;

        private int PlayerPed;

        private int? PopAreaId = null;

        private int LastPopRefreshTime = 0;

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

            EventHandlers["sth:applyCarMods"] += new Action<List<object>>(ApplyCarMods);

            DeathBlips = new DeathBlips(GetConvarInt("sth_deathbliplifespan", Constants.DefaultDeathBlipLifespan));

            PlayerPed = PlayerPedId();
        }

        protected void OnResourceStopping(string resourceName)
        {
            // Only perform cleanup if the resource stopped was sth-gamemode.
            if(GetCurrentResourceName() != resourceName)
            {
                return;
            }

            foreach (Vehicle vehicleToDelete in SpawnedVehicles)
            {
                vehicleToDelete.Delete();
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

                RegisterCommand("spawncars", new Action(() =>
                {
                    TriggerServerEvent("sth:spawnCars");
                }), false);

                // Notify the server this client has started so the config can be sent down. This is needed for resource restarts etc.
                TriggerServerEvent("sth:clientStarted");
            }
        }

        /// <summary>
        /// Applies random mods to cars spawned by the server in the start area after invoking the /spawncars command.
        /// </summary>
        protected void ApplyCarMods(List<object> cars)
        {
            try
            {
                int counter = 0;
                Debug.WriteLine($"Spawned {cars.Count} cars");
                foreach (int networkId in cars)
                {
                    Vehicle vehicle = new Vehicle(Entity.FromNetworkId(networkId).Handle);

                    if (!DoesEntityExist(vehicle.Handle))
                    {
                        Debug.WriteLine($"Vehicle {vehicle.Handle} does not exist yet!");
                    }
                    while (!DoesEntityExist(vehicle.Handle) && !HasVehicleAssetLoaded(vehicle.Model))
                    {
                        RequestModel(vehicle.Model);
                        Wait(50);
                    }

                    SetVehicleModKit(vehicle.Handle, 0);

                    // Set all vehicle mods to maximum.
                    for (int i = 0; i < 50; i++)
                    {
                        int nbMods = GetNumVehicleMods(vehicle.Handle, i);
                        if (nbMods > 0)
                        {
                            SetVehicleMod(vehicle.Handle, i, nbMods - 1, false);
                        }
                    }
                    // Add neons.
                    for (int i = 0; i < 4; i++)
                    {
                        SetVehicleNeonLightEnabled(vehicle.Handle, i, RNG.NextDouble() >= 0.5);
                    }
                    SetVehicleNeonLightsColour(vehicle.Handle, RNG.Next(0, 255), RNG.Next(0, 255), RNG.Next(0, 255));
                    SetVehicleXenonLightsColour(vehicle.Handle, RNG.Next(0, 12));

                    SetVehicleCustomPrimaryColour(vehicle.Handle, RNG.Next(0, 255), RNG.Next(0, 255), RNG.Next(0, 255));
                    SetVehicleCustomSecondaryColour(vehicle.Handle, RNG.Next(0, 255), RNG.Next(0, 255), RNG.Next(0, 255));

                    // lol
                    if (vehicle.Model.Hash == (uint)VehicleHash.Bmx)
                    {
                        vehicle.EnginePowerMultiplier = 100f;
                        vehicle.EngineTorqueMultiplier = 100f;
                    }

                    counter++;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Failed to apply car mods. Exception: {ex}");
            }
        }

        protected void AutoSpawnCallback()
        {
            Vector3 spawnLoc = Constants.DockSpawn;

            Exports["spawnmanager"].spawnPlayer(new { x = spawnLoc.X, y = spawnLoc.Y, z = spawnLoc.Z, model = "a_m_m_skater_01" });
        }

        protected void PlayerSpawnedCallback()
        {
            if(!PopAreaId.HasValue)
            {
                try
                {
                    float x1 = Constants.PlayAreaNW.X, y1 = Constants.PlayAreaNW.Y, z1 = 0f;
                    float x2 = Constants.PlayAreaSE.X, y2 = Constants.PlayAreaSE.Y, z2 = 100f;
                    float pedMult = 2f, vehMult = 2f;
                    bool p8 = true;
                    PopAreaId = AddPopMultiplierArea(x1, y1, z1, x2, y2, z2, pedMult, vehMult, p8);

                    if (PopAreaId.HasValue && DoesPopMultiplierAreaExist(PopAreaId.Value))
                    {
                        Debug.WriteLine($"Pop area added successfuly with ID {PopAreaId.Value}");
                    }
                } catch(Exception ex)
                {
                    Debug.WriteLine($"Failed to add pop area: {ex.ToString()}");
                    PopAreaId = -1;
                }
            }
            else
            {
                Debug.WriteLine($"Skipping creating a pop area because {(PopAreaId.Value == -1 ? "it failed" : $"it already exists with ID {PopAreaId.Value}")}");
            }

            // Refresh player's death state.
            PlayerState.DeathReported = false;

            // Indicate that weapons need to be given to the player again.
            PlayerState.WeaponsGiven = false;
            PlayerState.ForcedUnarmed = false;

            TriggerServerEvent("sth:playerSpawned", GetPlayerServerId(PlayerId()));

            // Enable friendly fire.
            NetworkSetFriendlyFireOption(true);
            SetCanAttackFriendly(PlayerPedId(), true, true);

            if(GameState.Hunt.IsInProgress || GameState.Hunt.IsEnding)
            {
                Debug.WriteLine($"GameState: Hunt.IsInProgress: {GameState.Hunt.IsInProgress}, Hunt.IsEnding: {GameState.Hunt.IsEnding}");
                HuntUI.DisplayObjective(ref GameState, ref PlayerState, GameState.Hunt.IsEnding);
            }
        }

        [EventHandler("sth:updatePlayerBlipBulk")]
        private void CreateExistingPlayerBlips(string playersBlipsInfo)
        {
            string[] playerBlipInfo = playersBlipsInfo.Split(';');
            Debug.WriteLine(playersBlipsInfo);
            foreach(string playerBlipDataString in playerBlipInfo)
            {
                string[] playerBlipData = playerBlipDataString.Split(',');
                int pedNetId = int.Parse(playerBlipData[0]);
                int playerId = int.Parse(playerBlipData[1]);
                string playerName = playerBlipData[2];
                bool isHunted = playerBlipData[3] == "1";

                UpdatePlayerBlip(pedNetId, playerId, playerName, isHunted);
            }
        }

        protected async Task UpdateLoop()
        {
            if (Game.PlayerPed != null)
            {
                ResetPlayerStamina(PlayerId());
                PlayerPos = Game.PlayerPed.Position;
            }

            // Refresh population every 2.5s
            if(GetGameTimer() - LastPopRefreshTime > 2500)
            {
                PopulateNow();
                LastPopRefreshTime = GetGameTimer();
            }

            HandlePopulation();

            GameState.Hunt.UpdateHuntedMugshot();
            HuntUI.SetBigmap(ref PlayerState);
            HuntUI.DrawRemainingTime(ref GameState);
            HuntUI.FadeBlips();
            HuntUI.UpdateTeammateBlips(Players, ref GameState, ref PlayerState);
            HuntUI.UpdatePlayerOverheadNames(Players, ref GameState, ref PlayerState);

            PlayerState.UpdateWeapons(Game.PlayerPed);

            // Make sure the player can't get cops.
            ClearPlayerWantedLevel(PlayerId());

            // Check and report player death to the server if needed.
            if(!Game.Player.IsAlive && !PlayerState.DeathReported)
            {
                TriggerServerEvent("sth:playerDied", new { PlayerId = Game.Player.ServerId, PlayerPosX = PlayerPos.X, PlayerPosY = PlayerPos.Y, PlayerPosZ = PlayerPos.Z, PlayerTeam = PlayerState.Team });
                PlayerState.DeathReported = true;
            }

            int currentPlayerPed = PlayerPedId();
            if(currentPlayerPed != PlayerPed && NetworkDoesNetworkIdExist(NetworkGetNetworkIdFromEntity(currentPlayerPed)) && NetworkDoesEntityExistWithNetworkId(NetworkGetNetworkIdFromEntity(currentPlayerPed)))
            {
                Debug.WriteLine($"Player Ped changed from {PlayerPed} (net: {NetworkGetNetworkIdFromEntity(currentPlayerPed)}) to {currentPlayerPed} (net: {NetworkGetNetworkIdFromEntity(currentPlayerPed)}). Calling invalidate");
                PlayerPed = currentPlayerPed;
                TriggerServerEvent("sth:invalidatePlayerPed", NetworkGetNetworkIdFromEntity(currentPlayerPed), Player.Local.ServerId);
            }

            GameOverCheck();

            FixCarsInSpawn();

            DeathBlips.ClearExpiredBlips();

            Wait(0);
        }

        private void HandlePopulation()
        {
            SetAmbientPedRangeMultiplierThisFrame(1f);
            SetPedDensityMultiplierThisFrame(1f);
            SetAmbientVehicleRangeMultiplierThisFrame(1f);
            SetVehicleDensityMultiplierThisFrame(1f);
            SetParkedVehicleDensityMultiplierThisFrame(1f);
            SetRandomVehicleDensityMultiplierThisFrame(1f);
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

        [EventHandler("sth:updatePlayerBlip")]
        private void UpdatePlayerBlip(int playerPedNetId, int playerIndex, string playerName, bool isHunted)
        {
            Debug.WriteLine("UpdatePlayerBlip");

            if(isHunted)
            {
                GameState.Hunt.HuntedPlayerPedNetworkId = playerPedNetId;
            }

            HuntUI.CreatePlayerBlip(playerPedNetId, playerIndex, playerName);
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

            EventHandlers["sth:markPantoAsDead"] += new Action<int, int>((pantoNetworkId, pantoIndex) =>
            {
                int handle = NetworkGetEntityFromNetworkId(pantoNetworkId);
                if (DoesEntityExist(handle))
                {
                    int blip = GetBlipFromEntity(handle);
                    SetBlipColour(blip, 39);
                    char targetLetter = "ABCDEFGH"[pantoIndex];
                    AddTextEntry("PantoDeadNotif", $"Target {targetLetter} has been destroyed.");
                    BeginTextCommandDisplayHelp("PantoDeadNotif");
                    EndTextCommandDisplayHelp(0, false, true, 5000);
                }
            });

            EventHandlers["sth:applyPantoBlips"] += new Action<string>((pantoNetworkIdString) =>
            {
                string[] pantoNetworkIds = pantoNetworkIdString.Split(';');
                int counter = 0;
                foreach (string networkIdStr in pantoNetworkIds)
                {
                    if (int.TryParse(networkIdStr, out int networkId))
                    {
                        int handle = NetworkGetEntityFromNetworkId(networkId);
                        int blip = AddBlipForEntity(handle);
                        bool isHunter = PlayerState.Team == Teams.Team.Hunters;
                        SetBlipAsFriendly(blip, isHunter);
                        SetBlipSprite(blip, 535 + counter);
                        SetBlipColour(blip, isHunter ? 3 : 59);
                    }
                    counter++;
                }
            });

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
    }
}
