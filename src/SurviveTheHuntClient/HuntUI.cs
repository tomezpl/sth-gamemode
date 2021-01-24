using System;
using System.Globalization;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntClient.Teams;

namespace SurviveTheHuntClient
{
    public static class HuntUI
    {
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

            Exception ex = null;

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
    }
}