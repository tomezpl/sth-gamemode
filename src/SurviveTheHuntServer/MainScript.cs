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

                Tick += UpdateLoop;
            }
        }

        private async Task UpdateLoop()
        {
            if (GameState.Hunt.IsStarted)
            {
                if (GameState.Hunt.EndTime <= DateTime.Now)
                {
                    GameState.Hunt.End(Teams.Team.Hunted);
                    NotifyWinner();
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
                        Player randomPlayer = Hunt.ChooseRandomPlayer(Players);

                        GameState.Hunt.LastHuntedPlayer = randomPlayer;

                        TriggerClientEvent(randomPlayer, "sth:notifyHuntedPlayer");
                        TriggerClientEvent("sth:notifyHunters", new { HuntedPlayerName = randomPlayer.Name });

                        GameState.Hunt.Begin(randomPlayer);

                        TriggerClientEvent("sth:huntStartedByServer", new { EndTime = GameState.Hunt.EndTime.ToString("F", CultureInfo.InvariantCulture) });
                    })
                }
            };
        }
    }
}
