using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using CitizenFX.Core;
using SurviveTheHuntServer.Helpers;
using SurviveTheHuntShared;
using SurviveTheHuntShared.Core;
using static CitizenFX.Core.Native.API;
using SharedConstants = SurviveTheHuntShared.Constants;

namespace SurviveTheHuntServer
{
    public partial class MainScript : BaseScript
    {
        protected GameState GameState = new GameState();

        private readonly Random RNG = new Random();

        /// <summary>
        /// UTC time when time was last synced with the clients (to prevent client-side timers desyncing).
        /// </summary>
        private DateTime LastTimeSync = DateTime.UtcNow;

        /// <summary>
        /// Gamemode-specific network-aware events triggerable from the client(s).
        /// </summary>
        protected Dictionary<string, Action<dynamic>> STHEvents;

        private readonly HuntedQueue HuntedPlayerQueue = null;

        private Config Config;

        public MainScript()
        {
            if (GetCurrentResourceName() != SharedConstants.ResourceName)
            {
                try
                {
                    throw new Exception($"Survive the Hunt: Invalid resource name! Resource name should be {SharedConstants.ResourceName}");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            else
            {
                EventHandlers["onServerResourceStart"] += new Action<string>(OnServerResourceStart);
                EventHandlers["playerJoining"] += new Action<Player, string>(PlayerJoining);
                EventHandlers["playerDropped"] += new Action<Player, string>(PlayerDisconnected);

                CreateEvents();

                foreach (KeyValuePair<string, Action<dynamic>> ev in STHEvents)
                {
                    EventHandlers[$"sth:{ev.Key}"] += ev.Value;
                }

                EventHandlers[Events.Server.ClientStarted] += new Action<Player>(ClientStarted);

                Tick += UpdateLoop;

                HuntedPlayerQueue = Hunt.InitHuntedQueue(Players);

                Config = ServerConfig.FromJsonFile();
                BroadcastConfig(Config);
                SyncVehicles(SpawnedVehicles);
            }
        }

        protected void PlayerDisconnected([FromSource] Player player, string reason)
        {
            HuntedPlayerQueue.RemovePlayer(player);

            if (GameState.Hunt.IsStarted && player != null && GameState.Hunt.HuntedPlayer?.Handle == player.Handle)
            {
                Debug.WriteLine("Hunted player left, ending hunt.");
                GameState.Hunt.End(Teams.Team.Hunters);
                NotifyWinner();
            }

            // FiveM docs don't seem to clearly communicate as to whether the player list is updated when this event fires,
            // so let's check if there are 0 players (or 1 player and it's the one who just left).
            int playerCount = GetNumPlayerIndices();
            if (playerCount == 0 || (playerCount == 1 && Players[GetPlayerFromIndex(0)] == player))
            {
                // Clear the spawned cars list as they will be gone for good once all players have left.
                Debug.WriteLine("All players have left, clearing vehicles!");
                SpawnedVehicles.Clear();
            }
        }

        protected void PlayerJoining([FromSource] Player player, string oldId)
        {
            if(string.IsNullOrWhiteSpace(player?.Name))
            {
                Console.WriteLine("Joining player name was null");
            }
            else
            {
                Console.WriteLine($"{player.Name} is joining; syncing time offset now.");
                TriggerClientEvent(player, Events.Client.ReceiveTimeSync, new { CurrentServerTime = DateTime.UtcNow.ToString("F", CultureInfo.InvariantCulture) });

                HuntedPlayerQueue.AddPlayer(player);
            }
        }

        private async Task UpdateLoop()
        {
            if(DateTime.UtcNow >= LastTimeSync + SharedConstants.TimeSyncInterval)
            {
                TriggerClientEvent(Events.Client.ReceiveTimeSync, new { CurrentServerTime = DateTime.UtcNow.ToString("F", CultureInfo.InvariantCulture) });
                LastTimeSync = DateTime.UtcNow;
            }

            if (GameState.Hunt.IsStarted)
            {
                if (GameState.Hunt.EndTime <= DateTime.UtcNow)
                {
                    GameState.Hunt.End(Teams.Team.Hunted);
                    NotifyWinner();
                }

                if(DateTime.UtcNow - GameState.Hunt.LastPingTime >= SharedConstants.HuntedPingInterval)
                {
                    GameState.Hunt.LastPingTime = DateTime.UtcNow;
                    float radius = 200f;
                    float playerLocationRadius = radius * 0.875f;
                    float offsetX = (((float)RNG.NextDouble() * 2f) - 1f) * playerLocationRadius;
                    float offsetY = (((float)RNG.NextDouble() * 2f) - 1f) * playerLocationRadius;

                    TriggerClientEvent(Events.Client.ShowPingOnMap, new
                    {
                        CreationDate = GameState.Hunt.LastPingTime.ToString("F", CultureInfo.InvariantCulture),
                        PlayerServerId = GameState.Hunt.HuntedPlayer.Handle,
                        Radius = radius,
                        OffsetX = offsetX,
                        OffsetY = offsetY
                    });
                }
            }
        }

        /// <summary>
        /// Notify players from the winning team that they won the game.
        /// </summary>
        private void NotifyWinner()
        {
            TriggerClientEvent(Events.Client.NotifyWinner, new { WinningTeam = (int)GameState.Hunt.WinningTeam });
        }

        /// <summary>
        /// Populates <see cref="STHEvents"/> with gamemode-specific event handlers.
        /// </summary>
        private void CreateEvents()
        {
            STHEvents = new Dictionary<string, Action<dynamic>>
            {
                {
                    Events.Server.RequestCleanClothes.EventName(), new Action<dynamic>(data =>
                    {
                        int playerId = data.PlayerId;
                        Console.WriteLine($"Cleaning clothes for {Players[playerId].Name}");

                        TriggerClientEvent(Events.Client.ReceiveCleanClothes, new { PlayerId = playerId });
                    })
                },
                {
                    Events.Server.PlayerDied.EventName(), new Action<dynamic>(data =>
                    {
                        int playerId = data.PlayerId;

                        Console.WriteLine($"Player died: {GetPlayerName($"{playerId}")}");

                        // Did the hunted player die?
                        if(Hunt.CheckPlayerDeath(Players[playerId], ref GameState))
                        {
                            NotifyWinner();
                        }

                        // Mark the player's death location with a blip for everyone.
                        TriggerClientEvent(Events.Client.MarkPlayerDeath, data.PlayerPosX, data.PlayerPosY, data.PlayerPosZ, data.PlayerTeam);
                    })
                },
                {
                    Events.Server.RequestStartHunt.EventName(), new Action<dynamic>(data =>
                    {
                        Player randomPlayer = Hunt.ChooseRandomPlayer(Players, ref GameState);

                        GameState.Hunt.LastHuntedPlayer = randomPlayer;

                        TriggerClientEvent(randomPlayer, Events.Client.NotifyHuntedPlayer);
                        TriggerClientEvent(Events.Client.NotifyHunters, new { HuntedPlayerServerId = int.Parse(randomPlayer.Handle) });

                        GameState.Hunt.Begin(randomPlayer);

                        TriggerClientEvent(Events.Client.HuntStartedByServer, new { EndTime = GameState.Hunt.EndTime.ToString("F", CultureInfo.InvariantCulture), NextNotification = (float)SharedConstants.HuntedPingInterval.TotalSeconds });
                    })
                },
                {
                    Events.Server.BroadcastHuntedZone.EventName(), new Action<dynamic>(data =>
                    {
                        Vector3 pos = data.Position;
                        TriggerClientEvent(Events.Client.NotifyAboutHuntedZone, new { PlayerServerId = GameState.Hunt.HuntedPlayer.Handle, Position = pos, NextNotification = (float)SharedConstants.HuntedPingInterval.TotalSeconds });
                    })
                }
            };
        }
    }
}
