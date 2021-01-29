using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public class MainScript : BaseScript
    {
        private const string ResourceName = "sth-gamemode";
        protected GameState GameState = new GameState();
        private readonly Random RNG = new Random();
        private DateTime LastTimeSync = DateTime.UtcNow;

        /// <summary>
        /// Gamemode-specific network-aware events triggerable from the client(s).
        /// </summary>
        protected Dictionary<string, Action<dynamic>> STHEvents;

        public MainScript()
        {
            if (GetCurrentResourceName() != ResourceName)
            {
                try
                {
                    throw new Exception($"Survive the Hunt: Invalid resource name! Resource name should be {ResourceName}");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            else
            {
                CreateEvents();

                foreach (KeyValuePair<string, Action<dynamic>> ev in STHEvents)
                {
                    EventHandlers[$"sth:{ev.Key}"] += ev.Value;
                }

                EventHandlers["playerJoining"] += new Action<Player, string>(PlayerJoining);

                Tick += UpdateLoop;
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
                TriggerClientEvent(player, "sth:receiveTimeSync", new { CurrentServerTime = DateTime.UtcNow.ToString("F", CultureInfo.InvariantCulture) });
            }
        }

        private async Task UpdateLoop()
        {
            if(DateTime.UtcNow >= LastTimeSync + Constants.TimeSyncInterval)
            {
                TriggerClientEvent("sth:receiveTimeSync", new { CurrentServerTime = DateTime.UtcNow.ToString("F", CultureInfo.InvariantCulture) });
                LastTimeSync = DateTime.UtcNow;
            }

            if (GameState.Hunt.IsStarted)
            {
                if (GameState.Hunt.EndTime <= DateTime.UtcNow)
                {
                    GameState.Hunt.End(Teams.Team.Hunted);
                    NotifyWinner();
                }

                if(DateTime.UtcNow - GameState.Hunt.LastPingTime >= Constants.HuntedPingInterval)
                {
                    GameState.Hunt.LastPingTime = DateTime.UtcNow;
                    float radius = 200f;
                    float playerLocationRadius = radius * 0.875f;
                    float offsetX = (((float)RNG.NextDouble() * 2f) - 1f) * playerLocationRadius;
                    float offsetY = (((float)RNG.NextDouble() * 2f) - 1f) * playerLocationRadius;

                    TriggerClientEvent("sth:showPingOnMap", new
                    {
                        CreationDate = GameState.Hunt.LastPingTime.ToString("F", CultureInfo.InvariantCulture),
                        PlayerName = GameState.Hunt.HuntedPlayer.Name,
                        Radius = radius,
                        OffsetX = offsetX,
                        OffsetY = offsetY
                    });
                }
            }
        }

        private void NotifyWinner()
        {
            TriggerClientEvent("sth:notifyWinner", new { WinningTeam = (int)GameState.Hunt.WinningTeam });
        }

        private void CreateEvents()
        {
            STHEvents = new Dictionary<string, Action<dynamic>>
            {
                {
                    "cleanClothes", new Action<dynamic>(data =>
                    {
                        int playerId = data.PlayerId;
                        Console.WriteLine($"Cleaning clothes for {Players[playerId].Name}");

                        TriggerClientEvent("sth:cleanClothesForPlayer", new { PlayerId = playerId });
                    })
                },
                {
                    "playerDied", new Action<dynamic>(data =>
                    {
                        int playerId = data.PlayerId;
                        Console.WriteLine($"Player died: {GetPlayerName($"{playerId}")}");

                        // Did the hunted player die?
                        if(Hunt.CheckPlayerDeath(Players[GetPlayerName($"{playerId}")], ref GameState))
                        {
                            NotifyWinner();
                        }
                    })
                },
                {
                    "startHunt", new Action<dynamic>(data =>
                    {
                        Player randomPlayer = Hunt.ChooseRandomPlayer(Players, ref GameState);

                        GameState.Hunt.LastHuntedPlayer = randomPlayer;

                        TriggerClientEvent(randomPlayer, "sth:notifyHuntedPlayer");
                        TriggerClientEvent("sth:notifyHunters", new { HuntedPlayerName = randomPlayer.Name });

                        GameState.Hunt.Begin(randomPlayer);

                        TriggerClientEvent("sth:huntStartedByServer", new { EndTime = GameState.Hunt.EndTime.ToString("F", CultureInfo.InvariantCulture) });
                    })
                },
                {
                    "broadcastHuntedZone", new Action<dynamic>(data =>
                    {
                        Vector3 pos = data.Position;
                        TriggerClientEvent("sth:notifyAboutHuntedZone", new { PlayerName = GameState.Hunt.HuntedPlayer.Name, Position = pos });
                    })
                }
            };
        }
    }
}
