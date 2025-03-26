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

        private IHuntedQueue HuntedPlayerQueue = null;

        private ServerConfig Config;

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
            // If this is an FFA game then find new targets for players who were hunting this player
            if(HuntedPlayerQueue.Type == HuntedQueueType.FreeForAll)
            {
                FFAHuntedQueue ffaQueue = (FFAHuntedQueue)HuntedPlayerQueue;
                List<Player> playersToNotify = ffaQueue.RemoveTarget(player);
                foreach (Player playerToNotify in playersToNotify)
                {
                    ffaQueue.SetCurrentPlayer(playerToNotify);
                    TriggerClientEvent(playerToNotify, Events.Client.ReceiveFFAHuntedTarget, ffaQueue.PopNext());
                }
            }
            HuntedPlayerQueue.RemovePlayer(player);

            if (GameState.Hunt.IsStarted && player != null && GameState.Hunt.HuntedPlayer?.Handle == player.Handle)
            {
                Debug.WriteLine("Hunted player left, ending hunt.");
                GameState.Hunt.End(Teams.Team.Hunters);
                ResetTeams();
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
            if (string.IsNullOrWhiteSpace(player?.Name))
            {
                Console.WriteLine("Joining player name was null");
            }
            else
            {
                Console.WriteLine($"{player.Name} is joining; syncing time offset now.");
                TriggerClientEvent(player, Events.Client.ReceiveTimeSync, new { CurrentServerTime = $"{DateTime.UtcNow.Ticks}" });

                HuntedPlayerQueue.AddPlayer(player);
            }
        }

        private async Task UpdateLoop()
        {
            if (DateTime.UtcNow >= LastTimeSync + SharedConstants.TimeSyncInterval)
            {
                TriggerClientEvent(Events.Client.ReceiveTimeSync, new { CurrentServerTime = $"{DateTime.UtcNow.Ticks}" });
                LastTimeSync = DateTime.UtcNow;
            }

            if (GameState.Hunt.IsStarted)
            {
                if (GameState.Hunt.EndTime <= DateTime.UtcNow)
                {
                    GameState.Hunt.End(Teams.Team.Hunted);
                    ResetTeams();
                    NotifyWinner();
                }

                if (DateTime.UtcNow - GameState.Hunt.LastPingTime >= SharedConstants.HuntedPingInterval)
                {
                    GameState.Hunt.LastPingTime = DateTime.UtcNow;
                    float radius = 200f;
                    float playerLocationRadius = radius * 0.875f;
                    float offsetX = (((float)RNG.NextDouble() * 2f) - 1f) * playerLocationRadius;
                    float offsetY = (((float)RNG.NextDouble() * 2f) - 1f) * playerLocationRadius;

                    if (HuntedPlayerQueue.Type == HuntedQueueType.SingleHunted)
                    {
                        TriggerClientEvent(Events.Client.ShowPingOnMap, new
                        {
                            CreationDate = GameState.Hunt.LastPingTime.ToString("F", CultureInfo.InvariantCulture),
                            PlayerServerId = GameState.Hunt.HuntedPlayer.Handle,
                            Radius = radius,
                            OffsetX = offsetX,
                            OffsetY = offsetY
                        });
                    }
                    else if (HuntedPlayerQueue.Type == HuntedQueueType.FreeForAll)
                    {
                        FFAHuntedQueue ffaQueue = (FFAHuntedQueue)HuntedPlayerQueue;
                        Dictionary<Player, dynamic> targetsToNotify = new Dictionary<Player, dynamic>();
                        foreach (Player player in Players)
                        {
                            ffaQueue.SetCurrentPlayer(player);
                            Player currentTarget = ffaQueue.CurrentTarget;
                            if(currentTarget != null)
                            {
                                dynamic payload = new
                                {
                                    CreationDate = GameState.Hunt.LastPingTime.ToString("F", CultureInfo.InvariantCulture),
                                    PlayerServerId = currentTarget.Handle,
                                    Radius = radius,
                                    OffsetX = offsetX,
                                    OffsetY = offsetY
                                };
                                TriggerClientEvent(player, Events.Client.ShowPingOnMap, payload);
                                if(!targetsToNotify.ContainsKey(currentTarget))
                                {
                                    targetsToNotify.Add(currentTarget, payload);
                                }
                            }
                        }

                        foreach(KeyValuePair<Player, dynamic> targetToNotify in targetsToNotify)
                        {
                            TriggerClientEvent(targetToNotify.Key, Events.Client.ShowPingOnMap, targetToNotify.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Notify players from the winning team that they won the game.
        /// </summary>
        private void NotifyWinner()
        {
            SetConvarReplicated(SharedConstants.CharCreatorBlockCreatorConvar, "false");
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
                }
            };
        }
    }
}
