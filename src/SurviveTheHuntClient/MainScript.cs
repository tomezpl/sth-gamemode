﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CitizenFX.Core;
using SurviveTheHuntShared.Utils;
using Vector3 = SurviveTheHuntShared.Utils.Vector3;
using CfxVector3 = CitizenFX.Core.Vector3;
using SurviveTheHuntClient.Helpers;
using static CitizenFX.Core.Native.API;
using SharedConstants = SurviveTheHuntShared.Constants;
using SurviveTheHuntShared.Core;
using SurviveTheHuntShared;

namespace SurviveTheHuntClient
{
    public partial class MainScript : ClientScript
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

        private CfxVector3 PlayerPos = CfxVector3.Zero;

        /// <summary>
        /// Blips used to represent player deaths on the radar.
        /// </summary>
        private readonly DeathBlips DeathBlips;
        
        /// <summary>
        /// The local player's ped handle as of the previous tick. Used to track ped changes so max health can be applied correctly.
        /// </summary>
        private int PreviousTickPedHandle = 0;

        /// <summary>
        /// Was prep phase active during the last tick?
        /// </summary>
        private bool DidProtectionsApplyLastFrame = false;

        /// <summary>
        /// Handle to the blip displaying the safe zone radius.
        /// </summary>
        private int SafeZoneRadiusBlipHandle = 0;

        /// <summary>
        /// Has the player already spawned at least once?
        /// </summary>
        private bool SpawnedOnce = false;

        /// <summary>
        /// Clipset to apply to the player ped.
        /// </summary>
        private string TargetClipset = "";

        private bool IsTargetClipsetLoaded = false;

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

            DeathBlips = new DeathBlips(GetConvarInt("sth_deathbliplifespan", SharedConstants.DefaultDeathBlipLifespan));
        }

        protected void OnResourceStopping(string resourceName)
        {
            // Only perform cleanup if the resource stopped was sth-gamemode.
            if(GetCurrentResourceName() != resourceName)
            {
                return;
            }

            if(DoesBlipExist(SafeZoneRadiusBlipHandle))
            {
                RemoveBlip(ref SafeZoneRadiusBlipHandle);
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
            if (resource == SharedConstants.ResourceName)
            {
                RegisterCommand("respawn", new Action(() =>
                {
                    Game.PlayerPed.HealthFloat = 0f;
                    TriggerEvent("baseevents:onPlayerKilled");
                }), false);

                RegisterCommand("starthunt", new Action(() =>
                {
                    TriggerServerEvent(Events.Server.RequestStartHunt);
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

                Vector3 spawn = SharedConstants.DockSpawn;
                ClearAreaOfEverything(spawn.X, spawn.Y, spawn.Z, 1000f, false, false, false, false);

                // Notify the server this client has started so the config can be sent down. This is needed for resource restarts etc.
                TriggerServerEvent(Events.Server.ClientStarted);

                SafeZoneRadiusBlipHandle = AddBlipForRadius(SharedConstants.DockSpawn.X, SharedConstants.DockSpawn.Y, SharedConstants.DockSpawn.Z, SharedConstants.DefaultSpawnSafeZoneRadius);
                SetBlipColour(SafeZoneRadiusBlipHandle, 69);
                SetBlipAlpha(SafeZoneRadiusBlipHandle, 128);
                SetBlipDisplay(SafeZoneRadiusBlipHandle, 6);
            }
        }

        /// <summary>
        /// Spawns random cars (picked from <see cref="Constants.Vehicles"/>) in the start area.
        /// </summary>
        protected async Task SpawnCars()
        {
            List<VehicleHash> carsToSpawn = new List<VehicleHash>(SharedConstants.CarSpawnPoints.Length);

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
                bool hasNetId = vehicleToDelete.NetId.HasValue;
                int id = hasNetId ? vehicleToDelete.NetId.Value : vehicleToDelete.Handle.Value;
                if (hasNetId || Vehicle.Exists(vehicleToDelete.Vehicle))
                {
                    Debug.WriteLine($"Requesting to delete vehicle with {(hasNetId ? "net ID" : "entity handle")} {id}");
                    TriggerServerEvent(Events.Server.RequestDeleteVehicle, hasNetId ? id : VehToNet(id));
                }
                else
                {
                    Debug.WriteLine($"Vehicle with {(hasNetId ? "net ID" : "entity handle")} {id} doesn't exist!");
                }
            }
            await Delay(3500);
            SpawnedVehicles.Clear();

            int counter = 0;
            foreach(VehicleHash vehicle in carsToSpawn)
            {
                if(!IsModelInCdimage((uint)vehicle) || !IsModelAVehicle((uint)vehicle))
                {
                    continue;
                }
                RequestModel((uint)vehicle);
                // Wait up to 1500ms (in 150ms intervals) for the model to load.
                for (int i = 0; i < 10; i++)
                {
                    await Delay(150);
                    if(HasModelLoaded((uint)vehicle))
                    {
                        break;
                    }
                }

                Coord spawnPoint = SharedConstants.CarSpawnPoints[counter];
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
            Vector3 spawnLoc = SharedConstants.DockSpawn;

            int randomPedIndex = RNG.Next(0, SharedConstants.DefaultPlayerPeds.Length);
            string randomPed = SharedConstants.DefaultPlayerPeds[randomPedIndex];

            Exports["spawnmanager"].spawnPlayer(new { x = spawnLoc.X, y = spawnLoc.Y, z = spawnLoc.Z, model = randomPed });

            // Pick a random GTAO character clipset.
            bool isFemale = randomPed.StartsWith("a_f");
            string[] clipsets = isFemale ? SharedConstants.DefaultFemaleClipsets : SharedConstants.DefaultMaleClipsets;
            TargetClipset = clipsets[RNG.Next(0, clipsets.Length)];
            IsTargetClipsetLoaded = false;

            DidProtectionsApplyLastFrame = false;
        }

        protected void PlayerSpawnedCallback()
        {
            SpawnedOnce = true;

            // Refresh player's death state.
            PlayerState.DeathReported = false;

            // Indicate that weapons need to be given to the player again.
            PlayerState.WeaponsGiven = false;
            PlayerState.ForcedUnarmed = false;

            TriggerServerEvent(Events.Server.RequestCleanClothes, new { PlayerId = GetPlayerServerId(PlayerId()) });

            // Enable friendly fire.
            NetworkSetFriendlyFireOption(true);
            SetCanAttackFriendly(PlayerPedId(), true, true);

            if(GameState.Hunt.IsInProgress || GameState.Hunt.IsEnding)
            {
                Debug.WriteLine($"GameState: Hunt.IsInProgress: {GameState.Hunt.IsInProgress}, Hunt.IsEnding: {GameState.Hunt.IsEnding}");
                HuntUI.DisplayObjective(ref GameState, ref PlayerState, GameState.Hunt.IsEnding);
            }

            // Set the player's max health.
            ApplyMaxHealth(true);

            if (SharedConstants.DefaultPlayerPeds.Any(modelName => GetHashKey(modelName) == Player.Local.Character.Model.Hash))
            {
                SetPedRandomComponentVariation(Player.Local.Character.Handle, false);
                SetPedRandomProps(Player.Local.Character.Handle);
            }
        }

        /// <summary>
        /// Applies max health to the current player ped and optionally replenishes their health.
        /// </summary>
        /// <param name="restore">Should the player ped's health be replenished to max?</param>
        protected void ApplyMaxHealth(bool restore = false)
        {
            int maxHealth = GetConvarInt("sth_maxHealth", SharedConstants.DefaultMaxHealth);
            SetPedMaxHealth(PlayerPedId(), maxHealth);
            
            if (restore)
            {
                SetEntityHealth(PlayerPedId(), maxHealth);
            }
        }

        protected async Task UpdateLoop()
        {
            if (Game.PlayerPed != null)
            {
                ResetPlayerStamina(PlayerId());
                PlayerPos = Game.PlayerPed.Position;

                // Wait for the random clipset to load and apply it once it's loaded.
                bool wasClipsetLoadedLastFrame = IsTargetClipsetLoaded;
                if(!wasClipsetLoadedLastFrame)
                {
                    RequestClipSet(TargetClipset);
                }
                IsTargetClipsetLoaded = HasClipSetLoaded(TargetClipset);
                if (!wasClipsetLoadedLastFrame && IsTargetClipsetLoaded)
                {
                    SetPedMovementClipset(PlayerPedId(), TargetClipset, 1f);
                }
            }

            bool wasHuntStartedLastFrame = !GameState.Hunt.WasHuntInProgressLastFrame && GameState.Hunt.IsStarted;

            GameState.Hunt.Tick();

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
                TriggerServerEvent(Events.Server.PlayerDied, new { PlayerId = Game.Player.ServerId, PlayerPosX = PlayerPos.X, PlayerPosY = PlayerPos.Y, PlayerPosZ = PlayerPos.Z, PlayerTeam = PlayerState.Team });
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
                    TriggerServerEvent(Events.Server.RequestSyncVehicles, vehicleNetIdsPacked);
                    SpawnedVehiclesNeedSync = false;
                }
            }

            GameOverCheck();

            FixCarsInSpawn();

            CfxVector3 spawnPos = new CfxVector3(SharedConstants.DockSpawn.X, SharedConstants.DockSpawn.Y, SharedConstants.DockSpawn.Z);
            float safeZoneRadiusSqr = (SharedConstants.DefaultSpawnSafeZoneRadius * SharedConstants.DefaultSpawnSafeZoneRadius);

            // Apply spawn safe zone protections if the prep phase hasn't ended yet.
            bool inSafeZone = Player.Local?.Character?.Position != null
                ? Player.Local.Character.Position.DistanceToSquared(spawnPos) < safeZoneRadiusSqr
                : false;
            bool isPrepPhase = (GameState.Hunt.IsStarted && GameState.Hunt.IsPrepPhase);
            bool shouldProtectionsApply = isPrepPhase || inSafeZone;

            if (wasHuntStartedLastFrame && !inSafeZone)
            {
                PlayerState.WaitingToTeleportToSpawn = true;
            }

            bool canLeaveSpawn = (!isPrepPhase || PlayerState.Team == Teams.Team.Hunted) && !PlayerState.WaitingToTeleportToSpawn;

            ApplySafeZoneProtection(shouldProtectionsApply, canLeaveSpawn, spawnPos, SharedConstants.DefaultSpawnSafeZoneRadius);
            if(!canLeaveSpawn)
            {
                Vector3 spawn = SharedConstants.DockSpawn;
                if(Player.Local?.Character?.Position != null && Player.Local.Character.Position.DistanceToSquared(spawnPos) > safeZoneRadiusSqr * 0.9f)
                {
                    DrawSphere(spawn.X, spawn.Y, spawn.Z, SharedConstants.DefaultSpawnSafeZoneRadius, 206, 191, 78, 0.5f);
                }
            }
            if(!isPrepPhase && shouldProtectionsApply != DidProtectionsApplyLastFrame)
            {
                BeginTextCommandThefeedPost("STRING");
                AddTextComponentSubstringPlayerName(inSafeZone ? "Safe zone protections active." : "Exited safe zone.");
                EndTextCommandThefeedPostTicker(true, true);
            }
            DidProtectionsApplyLastFrame = shouldProtectionsApply;

            DeathBlips.ClearExpiredBlips();

            if(Game.PlayerPed?.Exists() == true)
            {
                if(PreviousTickPedHandle != Game.PlayerPed.Handle)
                {
                    Debug.WriteLine("Ped changed, setting max health");
                    ApplyMaxHealth();
                }

                PreviousTickPedHandle = Game.PlayerPed.Handle;
            }

            Wait(0);
        }

        void ApplySafeZoneProtection(bool protectionActive, bool canLeaveSpawn, CfxVector3 safeZoneOrigin, float safeZoneRadius)
        {
            SetPlayerInvincible(PlayerId(), protectionActive);

            // It's CRITICAL that you ensure this statement only runs if SpawnedOnce is true.
            // Otherwise, players joining in progress will get softlocked without an error because the game will attempt to teleport them
            // before they've fully loaded in.
            if (SpawnedOnce && !canLeaveSpawn)
            {
                if (Game.PlayerPed?.Exists() == true)
                {
                    CfxVector3 offset = Game.PlayerPed.Position - safeZoneOrigin;
                    float magnitudeSqr = offset.LengthSquared();
                    float radiusSqr = safeZoneRadius * safeZoneRadius;
                    bool isOut = magnitudeSqr > radiusSqr;

                    if (isOut)
                    {
                        // Get a unit-length direction vector (from the spawn point to the player's current pos)
                        CfxVector3 dir = offset;
                        dir.Normalize();

                        int entityId = Game.PlayerPed.IsInVehicle() ? Game.PlayerPed.CurrentVehicle.Handle : PlayerPedId();

                        CfxVector3 velocity = GetEntityVelocity(entityId);

                        // Get the relation between our current velocity and the direction vector (dot product of the two).
                        CfxVector3 normVel = velocity;
                        normVel.Normalize();
                        float dot = normVel.X * dir.X + normVel.Y * dir.Y + normVel.Z * dir.Z;

                        // Remove any velocity that points away from the spawn.
                        if (dot >= 0f && velocity.LengthSquared() > 0f)
                        {
                            float mult = Math.Max(-dot, 0f);
                            SetEntityVelocity(entityId, velocity.X * mult, velocity.Y * mult, velocity.Z * mult);
                        }

                        bool needsTeleport = PlayerState.WaitingToTeleportToSpawn || magnitudeSqr > radiusSqr * 1.2f;

                        if (needsTeleport)
                        {
                            if (!IsScreenFadingOut())
                            {
                                DoScreenFadeOut(500);
                            }

                            if (IsScreenFadedOut())
                            {
                                SetEntityCoordsNoOffset(entityId, safeZoneOrigin.X, safeZoneOrigin.Y, safeZoneOrigin.Z, true, false, false);
                                PlayerState.WaitingToTeleportToSpawn = false;
                                DoScreenFadeIn(500);
                                HuntUI.DisplayObjective(ref GameState, ref PlayerState);
                            }
                        }
                    }
                }
            }
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
                CfxVector3 spawnToVehicleOffset = vehicle.Position;
                spawnToVehicleOffset.X -= SharedConstants.DockSpawn.X;
                spawnToVehicleOffset.Y -= SharedConstants.DockSpawn.Y;
                spawnToVehicleOffset.Z -= SharedConstants.DockSpawn.Z;

                bool closeToSpawn = 50f >= spawnToVehicleOffset.Length();
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

        private void NotifyTeam(Teams.Team playerTeam, Player huntedPlayer)
        {
            Ped playerPed = Game.PlayerPed;
            GameState.Hunt.IsStarted = true;
            GameState.Hunt.HuntedPlayer = huntedPlayer;

            switch (playerTeam)
            {
                case Teams.Team.Hunters:
                    // TODO: Shouldn't current objective technically be player state?
                    GameState.CurrentObjective = " is the hunted! Track them down.";
                    PlayerState.Team = Teams.Team.Hunters;
                    break;
                case Teams.Team.Hunted:
                    GameState.CurrentObjective = "Survive";
                    PlayerState.Team = Teams.Team.Hunted;
                    break;
            }

            PlayerState.TakeAwayWeapons(ref playerPed);
        }

        private void HuntStartedByServer(float secondsTillPing, DateTime endTime, TimeSpan? prepPhase = null)
        {
            if (!prepPhase.HasValue)
            {
                prepPhase = TimeSpan.Zero;
            }

            GameState.Hunt.NextMugshotTime = Utility.CurrentTime + TimeSpan.FromSeconds(secondsTillPing);
            GameState.Hunt.InitialEndTime = endTime;
            GameState.Hunt.PrepPhaseEndTime = Utility.CurrentTime + prepPhase.Value;
            HuntUI.DisplayObjective(ref GameState, ref PlayerState);
        }

        /// <summary>
        /// Populates <see cref="STHEvents"/> with gamemode-specific events used by the resource.
        /// </summary>
        private void CreateEvents()
        {
            STHEvents = new Dictionary<string, Action<dynamic>>
            {
                {
                    Events.Client.ReceiveCleanClothes.EventName(), new Action<dynamic>(data =>
                    {
                        int playerId = data.PlayerId;

                        ClearPedBloodDamage(GetPlayerPed(GetPlayerFromServerId(playerId)));
                    })
                },
                {
                    Events.Client.NotifyHuntedPlayer.EventName(), new Action<dynamic>(data =>
                    {
                        NotifyTeam(Teams.Team.Hunted, Game.Player);
                    })
                },
                {
                    Events.Client.NotifyHunters.EventName(), new Action<dynamic>(data =>
                    {
                        int huntedPlayerServerId = data.HuntedPlayerServerId;

                        // Since the event is sent out to everyone, make sure it is discarded by the hunted player.
                        if(huntedPlayerServerId == Game.Player.ServerId)
                        {
                            return;
                        }

                        NotifyTeam(Teams.Team.Hunters, new Player(GetPlayerFromServerId(huntedPlayerServerId)));
                    })
                },
                {
                    Events.Client.NotifyWinner.EventName(), new Action<dynamic>(data =>
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
                    Events.Client.HuntStartedByServer.EventName(), new Action<dynamic>(data =>
                    {
                        float secondsTillPing = data.NextNotification;

                        string endTimeStr = data.EndTime;
                        DateTime endTime = DateTime.ParseExact(endTimeStr, "F", CultureInfo.InvariantCulture);

                        ulong prepPhaseDuration = (ulong)data.PrepPhaseDuration;

                        HuntStartedByServer(secondsTillPing, endTime, TimeSpan.FromSeconds(prepPhaseDuration));
                    })
                },
                {
                    Events.Client.ShowPingOnMap.EventName(), new Action<dynamic>(data =>
                    {
                        int playerServerId = int.Parse(data.PlayerServerId);
                        HuntUI.CreateRadiusBlipForPlayer(new Player(GetPlayerFromServerId(playerServerId)), data.Radius, data.OffsetX, data.OffsetY, DateTime.ParseExact(data.CreationDate, "F", CultureInfo.InvariantCulture), ref PlayerState);
                        if(playerServerId == Game.Player.ServerId)
                        {
                            CfxVector3 position = GetEntityCoords(PlayerPedId(), false);
                            TriggerServerEvent(Events.Server.BroadcastHuntedZone, new { Position = position });
                        }
                    })
                },
                {
                    Events.Client.NotifyAboutHuntedZone.EventName(), new Action<dynamic>(data =>
                    {
                        int playerServerId = int.Parse(data.PlayerServerId);
                        Player player = new Player(GetPlayerFromServerId(playerServerId));
                        string playerName = player.Name;
                        float nextNotificationTimeout = data.NextNotification;
                        GameState.Hunt.NextMugshotTime = Utility.CurrentTime + TimeSpan.FromSeconds(nextNotificationTimeout);
                        HuntUI.NotifyAboutHuntedZone(player, data.Position, ref GameState);
                    })
                },
                {
                    Events.Client.ReceiveTimeSync.EventName(), new Action<dynamic>(data =>
                    {
                        string currentServerTimeStr = data.CurrentServerTime;
                        DateTime currentServerTime = DateTime.ParseExact(currentServerTimeStr, "F", CultureInfo.InvariantCulture);
                        Utility.ServerTimeOffset = currentServerTime - DateTime.UtcNow;
                    })
                }
            };

            // Event handler for gamemode config being sent by the server.
            EventHandlers[Events.Client.ReceiveConfig] += new Action<byte[], byte[], string>((weaponsHunters, weaponsHunted, vehicleList) =>
            {
                Debug.WriteLine("sth:receiveConfig received!");

                Config.Deserialized deserialized = Config.Serialized.Deserialize(weaponsHunters, weaponsHunted, vehicleList);

                Debug.WriteLine("parsed config!");

                Constants.WeaponLoadouts[Teams.Team.Hunters] = deserialized.HuntersWeapons;
                Constants.WeaponLoadouts[Teams.Team.Hunted] = deserialized.HuntedWeapons;
                Constants.Vehicles = deserialized.VehicleWhitelist.Vehicles.Select((vehicleName) => (VehicleHash)GetHashKey(vehicleName)).ToArray();
            
                // In the event the player was already given weapons, remove them so that the new loadout can be applied.
                if(Player.Local?.Character != null && PlayerState?.WeaponsGiven == true)
                {
                    Ped localPlayerPed = Player.Local.Character;
                    PlayerState.TakeAwayWeapons(ref localPlayerPed);
                }
            });

            EventHandlers[Events.Client.MarkPlayerDeath] += new Action<float, float, float, Teams.Team>((deathPosX, deathPosY, deathPosZ, team) =>
            {
                if (team == PlayerState.Team || string.Equals(GetConvar("sth_globalPlayerDeathBlips", "false"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    DeathBlips.Add(deathPosX, deathPosY, deathPosZ);
                }
            });
        }

        [EventHandler(Events.Client.ReceiveSyncedVehicles)]
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

        [EventHandler(Events.Client.ReceiveDeleteVehicle)]
        public void DeleteVehicle(int vehicleNetId)
        {
            if(NetworkDoesNetworkIdExist(vehicleNetId) && Vehicle.Exists(Vehicle.FromNetworkId(vehicleNetId)))
            {
                SetEntityAsMissionEntity(NetToVeh(vehicleNetId), true, true);
                Vehicle.FromNetworkId(vehicleNetId).Delete();
            }
            else
            {
                Debug.WriteLine($"vehicleNetId {vehicleNetId} doesn't exist");
            }
        }
    }
}
