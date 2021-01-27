using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntClient.Teams;

namespace SurviveTheHuntClient
{
    public static class HuntUI
    {
        private static Blip RadiusBlip = null;
        // TODO: Change this to a hashset?
        private static Dictionary<int, dynamic> PlayerBlips = new Dictionary<int, dynamic>();

        private class FadingBlip
        {
            public Blip Blip { get; set; } = null;
            public DateTime FadeOutStart = default, FadeOutEnd = default;

            public FadingBlip(Blip blip, DateTime fadeOutStart, DateTime fadeOutEnd)
            {
                Blip = blip;
                FadeOutStart = fadeOutStart;
                FadeOutEnd = fadeOutEnd;
            }

            public float Alpha
            {
                get
                {
                    if (FadeOutStart == default || FadeOutEnd == default)
                    {
                        return 0f;
                    }
                    else
                    {
                        if(DateTime.Now <= FadeOutStart)
                        {
                            return 1f;
                        }
                        
                        float magnitude = (float)(FadeOutEnd - FadeOutStart).TotalSeconds;
                        float t = (float)(DateTime.Now - FadeOutStart).TotalSeconds;

                        if(t >= magnitude)
                        {
                            return 0f;
                        }

                        // InvLerp
                        return 1f - t / magnitude;
                    }
                }
            }
        }

        private static List<FadingBlip> FadingBlips = new List<FadingBlip>();

        public static void DisplayObjective(ref GameState gameState, ref PlayerState playerState)
        {
            if (!string.IsNullOrWhiteSpace(gameState.CurrentObjective))
            {
                AddTextEntry("CURRENT_OBJECTIVE", "~a~~a~");
                BeginTextCommandPrint("CURRENT_OBJECTIVE");

                if (gameState.Hunt.IsInProgress)
                {
                    if (playerState.Team == Team.Hunters)
                    {
                        // Make the next text component colour yellow, as it'll contain the hunted player's name.
                        SetColourOfNextTextComponent(12);
                        AddTextComponentString(gameState.Hunt.HuntedPlayer.Name);
                    }
                }

                // Switch back to default (white) text colour.
                SetColourOfNextTextComponent(0);
                AddTextComponentString(gameState.CurrentObjective);
                EndTextCommandPrint(1, true);
            }
        }

        public static void SetBigmap(ref PlayerState playerState)
        {
            playerState.Bigmap.UpdateTime(GetFrameTime());

            // Is the map-expand control active?
            bool buttonPressed = IsControlJustReleased(0, 20);
            bool bigmapActive = IsBigmapActive();

            if (playerState.Bigmap.TimeSinceActivated >= 8000 || (buttonPressed && bigmapActive))
            {
                playerState.Bigmap.Hide();
            }
            else if (buttonPressed && !bigmapActive)
            {
                playerState.Bigmap.Show();
            }
        }

        public static void DrawRemainingTime(ref GameState gameState)
        {
            if (!gameState.Hunt.IsInProgress && !gameState.Hunt.IsEnding)
            {
                return;
            }

            string timeStr = "";
            try
            {
                if (gameState.Hunt.EndTime <= DateTime.Now)
                {
                    timeStr = "00:00";
                }
                else
                {
                    TimeSpan remainingTime = gameState.Hunt.EndTime - DateTime.Now;
                    timeStr = $"{remainingTime.Minutes.ToString("00", CultureInfo.InvariantCulture)}:{remainingTime.Seconds.ToString("00", CultureInfo.InvariantCulture)}";
                }
            }
            catch (Exception)
            {
                timeStr = "00:00";
            }

            // Get rect width.
            SetTextScale(0f, 0.55f);
            BeginTextCommandWidth("STRING");
            AddTextComponentString("TIME LEFT  00:00");
            float timebarWidth = EndTextCommandGetWidth(true);

            RequestStreamedTextureDict("timerbars", true);
            if (HasStreamedTextureDictLoaded("timerbars"))
            {
                DrawSprite("timerbars", "all_black_bg", 0.92f, 0.875f, timebarWidth, 0.06f * 0.5f * 1.4f, 0f, 255, 255, 255, 128);
            }

            BeginTextCommandDisplayText("STRING");
            AddTextComponentString(timeStr);
            EndTextCommandDisplayText(0.94f, 0.855f);
            SetTextScale(0, 0.35f);
            BeginTextCommandDisplayText("STRING");
            AddTextComponentString("TIME LEFT");
            EndTextCommandDisplayText(0.94f - timebarWidth / 2.35f, 0.865f);
            SetTextScale(0, 1f);
        }

        public static void CreateBlipForPlayer(Player player, float radius, float offsetX, float offsetY, DateTime creationTime, ref PlayerState playerState)
        {
            Vector3 position = GetEntityCoords(player.Character.Handle, false);

            if (RadiusBlip == null)
            {
                RadiusBlip = new Blip(AddBlipForRadius(position.X + offsetX, position.Y, position.Z + offsetY, radius));
            }
            else
            {
                SetBlipCoords(RadiusBlip.Handle, position.X + offsetX, position.Y, position.Z + offsetY);
            }

            // Set blip to be yellow (main objective).
            SetBlipColour(RadiusBlip.Handle, 66);
            SetBlipAlpha(RadiusBlip.Handle, 128);
            SetBlipDisplay(RadiusBlip.Handle, 6);
            SetBlipNameToPlayerName(RadiusBlip.Handle, player.Handle);
            SetBlipHiddenOnLegend(RadiusBlip.Handle, false);
            if (playerState.Team == Team.Hunters)
            {
                SetBlipRoute(RadiusBlip.Handle, true);
            }

            PingBlipOnMap(ref RadiusBlip, creationTime, TimeSpan.FromSeconds(Constants.HuntedBlipLifespan), TimeSpan.FromSeconds(Constants.HuntedBlipFadeoutTime));
        }

        public static void FadeBlips()
        {
            List<FadingBlip> blipsToDelete = new List<FadingBlip>();
            foreach(FadingBlip blip in FadingBlips)
            {
                SetBlipAlpha(blip.Blip.Handle, Convert.ToInt32(blip.Alpha * 128f));
                if(blip.Alpha == 0f)
                {
                    blipsToDelete.Add(blip);
                }
            }

            // Disable blip if fully transparent.
            foreach(FadingBlip blip in blipsToDelete)
            {
                SetBlipDisplay(blip.Blip.Handle, 0);
                SetBlipRoute(blip.Blip.Handle, false);
                SetBlipHiddenOnLegend(blip.Blip.Handle, true);
                FadingBlips.Remove(blip);
            }
        }

        public static void PingBlipOnMap(ref Blip blip, DateTime creationTime, TimeSpan lifespan, TimeSpan fadeOutTime)
        {
            FadingBlips.Add(new FadingBlip(blip, creationTime + lifespan, creationTime + lifespan + fadeOutTime));
        }

        public static void NotifyAboutHuntedZone(Player player, Vector3 position)
        {
            string zoneName = GetLabelText(GetNameOfZone(position.X, position.Y, position.Z));
            BeginTextCommandThefeedPost("STRING");
            AddTextComponentString($"{player.Name} is somewhere in {zoneName} right now.");
            EndTextCommandThefeedPostTicker(true, true);
        }

        public static void UpdateTeammateBlips(PlayerList players, ref GameState gameState, ref PlayerState playerState)
        {
            // TODO: The heavy use of collections in this method seems to increase the tick time by a considerable amount.
            // Need to only invoke the blip update when a player connects, disconnects or dies; Otherwise only update the visibility of existing blips.

            int counter = 0;
            List<int> activePeds = new List<int>();
            foreach(Player player in players)
            {
                if(player == Game.Player)
                {
                    SetBlipColour(GetMainPlayerBlipId(), player.Handle + 10);
                    continue;
                }

                if(!PlayerBlips.ContainsKey(player.Character.Handle))
                {
                    Blip blip = new Blip(AddBlipForEntity(player.Character.Handle));
                    blip.Name = player.Name;
                    SetBlipColour(blip.Handle, player.Handle + 10);
                    SetBlipDisplay(blip.Handle, 6);
                    ShowHeadingIndicatorOnBlip(blip.Handle, true);
                    SetBlipCategory(blip.Handle, 7);
                    SetBlipShrink(blip.Handle, GetConvar("sth_shrinkPlayerBlips", "false") != "false");
                    SetBlipScale(blip.Handle, 0.9f);
                    CreateMpGamerTagWithCrewColor(player.Handle, player.Name, false, false, "", 0, 0, 0, 0);
                    SetMpGamerTagColour(player.Handle, 0, GetBlipHudColour(blip.Handle));
                    SetMpGamerTagVisibility(player.Handle, 0, true);
                    PlayerBlips.Add(player.Character.Handle, new { blip, id = player.Handle });

                    counter++;
                }

                activePeds.Add(player.Character.Handle);
            }

            // Display player names on blips (in bigmap).
            N_0x82cedc33687e1f50(true);

            List<int> pedsToDelete = new List<int>();

            foreach(int ped in PlayerBlips.Keys)
            {
                // Delete check
                if (!activePeds.Contains(ped))
                {
                    pedsToDelete.Add(ped);
                    continue;
                }

                if(gameState.Hunt.IsStarted && (playerState.Team == Team.Hunted || ped == gameState.Hunt.HuntedPlayer.Character.Handle) && !GameState.IsPedTooFar(new Ped(ped)))
                {
                    // Hide the blip
                    Blip blip = PlayerBlips[ped].blip;
                    SetBlipDisplay(blip.Handle, 0);
                    int id = PlayerBlips[ped].id;
                    SetMpGamerTagVisibility(id, 0, false);
                }
                else
                {
                    // Show the blip
                    Blip blip = PlayerBlips[ped].blip;
                    SetBlipDisplay(blip.Handle, 6);
                    int id = PlayerBlips[ped].id;
                    SetMpGamerTagVisibility(id, 0, true);
                }
            }
            foreach(int ped in pedsToDelete)
            {
                Blip blip = PlayerBlips[ped].blip;
                blip.Delete();
                PlayerBlips.Remove(ped);
            }

        }
    }
}