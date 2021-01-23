using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public class MainScript : ClientScript
    {
        protected PlayerState playerState = new PlayerState();

        public MainScript()
        {
            EventHandlers["onClientGameTypeStart"] += new Action<string>(OnClientGameTypeStart);
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
        }

        protected void AutoSpawnCallback()
        {
            Vec3 spawnLoc = Constants.DockSpawn;

            Exports["spawnmanager"].spawnPlayer(new { x = spawnLoc.X, y = spawnLoc.Y, z = spawnLoc.Z, model = "a_m_m_skater_01" });
        }

        protected void PlayerSpawnedCallback()
        {
            // Refresh player's death state.
            playerState.DeathReported = false;

            // Indicate that weapons need to be given to the player again.
            playerState.WeaponsGiven = false;

            //TriggerServerEvent("sth:clientClothes", new { playerId = PlayerId() });

            // Enable friendly fire.
            NetworkSetFriendlyFireOption(true);
            SetCanAttackFriendly(PlayerPedId(), true, true);
        }
    }
}
