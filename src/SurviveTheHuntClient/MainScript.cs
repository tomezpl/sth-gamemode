using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public class MainScript : ClientScript
    {
        protected PlayerState PlayerState = new PlayerState();
        protected Dictionary<string, Action<dynamic>> STHEvents;
        protected GameState GameState = new GameState();

        public MainScript()
        {
            EventHandlers["onClientGameTypeStart"] += new Action<string>(OnClientGameTypeStart);
            EventHandlers["onClientResourceStart"] += new Action(OnClientResourceStart);

            CreateEvents();
            foreach(KeyValuePair<string, Action<dynamic>> ev in STHEvents)
            {
                EventHandlers[$"sth:{ev.Key}"] += ev.Value;
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
        }

        protected void AutoSpawnCallback()
        {
            Vec3 spawnLoc = Constants.DockSpawn;

            Exports["spawnmanager"].spawnPlayer(new { x = spawnLoc.X, y = spawnLoc.Y, z = spawnLoc.Z, model = "a_m_m_skater_01" });
        }

        protected void PlayerSpawnedCallback()
        {
            // Refresh player's death state.
            PlayerState.DeathReported = false;

            // Indicate that weapons need to be given to the player again.
            PlayerState.WeaponsGiven = false;
            PlayerState.LastWeaponEquipped = new WeaponAsset(WeaponHash.Unarmed).Hash;

            TriggerServerEvent("sth:clientClothes", new { PlayerId = PlayerId() });

            // Enable friendly fire.
            NetworkSetFriendlyFireOption(true);
            SetCanAttackFriendly(PlayerPedId(), true, true);

            if(GameState.Hunt.IsEnding)
            {
                HuntUI.DisplayObjective(ref GameState, ref PlayerState, true);
            }
        }

        protected async Task UpdateLoop()
        {
            if (Game.PlayerPed != null)
            {
                ResetPlayerStamina(PlayerId());
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
            if(Game.Player.IsDead && !PlayerState.DeathReported)
            {
                TriggerServerEvent("sth:playerDied", new { PlayerId = Game.Player.ServerId });
                PlayerState.DeathReported = true;
            }

            GameOverCheck();

            Wait(0);
        }

        protected void GameOverCheck()
        {
            if(!GameState.Hunt.IsEnding)
            {
                return;
            }

            if (GameState.Hunt.EndInMilliseconds > 0)
            {
                GameState.Hunt.EndInMilliseconds -= Convert.ToInt32(Math.Round(Game.LastFrameTime * 1000f));
            }
            else
            {
                GameState.Hunt.End();
                GameState.CurrentObjective = "";
            }
        }

        private void CreateEvents()
        {
            STHEvents = new Dictionary<string, Action<dynamic>>
            {
                {
                    "cleanClothesForPlayer", new Action<dynamic>(data =>
                    {
                        int playerId = data.PlayerId;

                        ClearPedBloodDamage(GetPlayerPed(playerId));
                    })
                },
                {
                    "notifyHuntedPlayer", new Action<dynamic>(data =>
                    {
                        //Console.WriteLine("I'm the hunted!");
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
                        GameState.Hunt.EndInMilliseconds = 5000;
                        HuntUI.DisplayObjective(ref GameState, ref PlayerState, true);
                    })
                },
                {
                    "huntStartedByServer", new Action<dynamic>(data =>
                    {
                        string endTimeStr = data.EndTime;
                        DateTime endTime = DateTime.ParseExact(endTimeStr, "F", CultureInfo.InvariantCulture);
                        GameState.Hunt.EndTime = endTime;
                        HuntUI.DisplayObjective(ref GameState, ref PlayerState);
                    })
                },
                {
                    "showPingOnMap", new Action<dynamic>(data =>
                    {
                        string playerName = data.PlayerName;
                        HuntUI.CreateBlipForPlayer(Players[playerName], data.Radius, data.OffsetX, data.OffsetY, DateTime.ParseExact(data.CreationDate, "F", CultureInfo.InvariantCulture), ref PlayerState);
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
                }
            };
        }
    }
}
