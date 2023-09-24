using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly DeathBlips DeathBlips = new DeathBlips();

        public MainScript()
        {
            EventHandlers["onClientGameTypeStart"] += new Action<string>(OnClientGameTypeStart);
            EventHandlers["onClientResourceStart"] += new Action(OnClientResourceStart);
            EventHandlers["onResourceStopping"] += new Action<string>(OnResourceStopping);

            CreateEvents();
            foreach(KeyValuePair<string, Action<dynamic>> ev in STHEvents)
            {
                EventHandlers[$"sth:{ev.Key}"] += ev.Value;
            }
        }

        protected void OnResourceStopping(string resourceName)
        {
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

        private void OnClientResourceStart()
        {
            RegisterCommand("suicide", new Action(() => 
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
                SpawnCars();
            }), false);

            Vector3 spawn = Constants.DockSpawn;
            ClearAreaOfEverything(spawn.X, spawn.Y, spawn.Z, 1000f, false, false, false, false);
        }

        /// <summary>
        /// Spawns random cars (picked from <see cref="Constants.Vehicles"/>) in the start area.
        /// </summary>
        protected void SpawnCars()
        {
            List<VehicleHash> carsToSpawn = new List<VehicleHash>(Constants.CarSpawnPoints.Length);

            List<VehicleHash> spawnableCars = Constants.Vehicles.ToList();

            for(int i = 0; i < carsToSpawn.Capacity; i++)
            {
                int randomIndex = RNG.Next(0, spawnableCars.Count);
                VehicleHash randomVehicle = spawnableCars[randomIndex];
                carsToSpawn.Add(randomVehicle);

                spawnableCars.RemoveAt(randomIndex);
            }

            foreach(Vehicle vehicleToDelete in SpawnedVehicles)
            {
                vehicleToDelete.Delete();
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
                Wait(50);

                Coord spawnPoint = Constants.CarSpawnPoints[counter];
                Vector3 spawnPos = spawnPoint.Position;

                Vehicle spawnedVehicle = new Vehicle(CreateVehicle((uint)vehicle, spawnPos.X, spawnPos.Y, spawnPos.Z, spawnPoint.Heading, true, false));
                SpawnedVehicles.Add(spawnedVehicle);

                // Set all vehicle mods to maximum.
                for (int i = 0; i < 50; i++)
                {
                    int nbMods = GetNumVehicleMods(spawnedVehicle.Handle, i);
                    if(nbMods > 0)
                    {
                        SetVehicleModKit(spawnedVehicle.Handle, i);
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
            PlayerState.LastWeaponEquipped = new WeaponAsset(WeaponHash.Unarmed).Hash;

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
                TriggerEvent("sth:markPlayerDeath", PlayerPos.X, PlayerPos.Y, PlayerPos.Z);
                TriggerServerEvent("sth:playerDied", new { PlayerId = Game.Player.ServerId });
                PlayerState.DeathReported = true;
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
                        PlayerState.TakeAwayWeapons(ref playerPed);

                        GameState.Hunt.IsStarted = true;
                        GameState.Hunt.HuntedPlayer = Players[huntedPlayerName];

                        // TODO: Shouldn't current objective technically be player state?
                        GameState.CurrentObjective = " is the hunted! Track them down.";
                        PlayerState.Team = Teams.Team.Hunters;
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
                        string endTimeStr = data.EndTime;
                        DateTime endTime = DateTime.ParseExact(endTimeStr, "F", CultureInfo.InvariantCulture);
                        GameState.Hunt.InitialEndTime = endTime;
                        HuntUI.DisplayObjective(ref GameState, ref PlayerState);
                        Ped playerPed = Game.PlayerPed;
                        PlayerState.TakeAwayWeapons(ref playerPed, true);
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
                },
                {
                    "markPlayerDeath", new Action<dynamic>(data =>
                    {
                        Vector3 deathPos = new Vector3(data as float[]);
                        DeathBlips.Add(deathPos);
                    })
                }
            };
        }
    }
}
