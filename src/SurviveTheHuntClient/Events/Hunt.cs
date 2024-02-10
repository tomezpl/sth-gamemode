using CitizenFX.Core;
using CitizenFX.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public partial class MainScript
    {
        private string GetSimeonText()
        {
            int playerPedHash = Player.Local.Character.Model.Hash;
            bool isMale = playerPedHash == GetHashKey("mp_m_freemode_01") || Constants.DefaultPlayerPeds.Where(pedName => pedName.StartsWith("a_m_") || pedName.Contains("moodyman")).Any((pedName) => GetHashKey(pedName) == playerPedHash);
            return $"Destroy these Pantos or you are fucked, my {(isMale ? "boy" : "girl")}! FUCKED!";
        }

        [EventHandler("sth:notifyHuntedPlayer")]
        private async void NotifyHuntedPlayer(dynamic data)
        {
            //Debug.WriteLine("I'm the hunted!");
            GameState.Hunt.IsStarted = true;
            GameState.Hunt.HuntedPlayer = Game.Player;

            GameState.CurrentObjective = "Survive";
            PlayerState.Team = Teams.Team.Hunted;

            Ped playerPed = Game.PlayerPed;
            PlayerState.TakeAwayWeapons(ref playerPed);

            dynamic msgData = new { contact = "Simeon", message = GetSimeonText(), isentthat = false, canOpenMenu = false, selectEvent = "test" };
            TriggerEvent("scalePhone.BuildMessageView", msgData, 1000);

            RequestStreamedTextureDict("char_simeon", false);
            int counter = 0;
            while (!HasStreamedTextureDictLoaded("char_simeon") && counter++ < 10)
            {
                await Delay(500);
            }

            BeginTextCommandThefeedPost("STRING");
            AddTextComponentSubstringPlayerName(GetSimeonText());
            EndTextCommandThefeedPostMessagetext("char_simeon", "char_simeon", false, 0, "Simeon", "");
            EndTextCommandThefeedPostTicker(false, true);
        }
    }
}
