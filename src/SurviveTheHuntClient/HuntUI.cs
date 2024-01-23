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
        /// <summary>
        /// Blip handle used for showing the hunted player's radius.
        /// </summary>
        private static Blip RadiusBlip = null;

        /// <summary>
        /// Regular player blips to show on the radar.
        /// </summary>
        /// <remarks>TODO: Change this to a hashset?</remarks>
        private static Dictionary<int, dynamic> PlayerBlips = new Dictionary<int, dynamic>();

        private static Dictionary<int, int> PlayerOverheadNames = new Dictionary<int, int>();

        /// <summary>
        /// Handles for currently active player peds; this is needed so blips aren't tracking dead player peds etc.
        /// </summary>
        private static List<int> ActivePeds = new List<int>();

        /// <summary>
        /// Blips with opacity fading over time.
        /// </summary>
        private static List<FadingBlip> FadingBlips = new List<FadingBlip>();

        private class FadingBlip
        {
            /// <summary>
            /// Blip handle.
            /// </summary>
            public Blip Blip { get; set; } = null;

            /// <summary>
            /// When has the fade out effect started?
            /// </summary>
            public DateTime FadeOutStart = default;

            /// <summary>
            /// When should the fade out effect end?
            /// </summary>
            public DateTime FadeOutEnd = default;

            /// <summary>
            /// Creates a helper for fading a blip on the radar.
            /// </summary>
            /// <param name="blip">The handle of the blip to be faded.</param>
            /// <param name="fadeOutStart">When the blip should start fading.</param>
            /// <param name="fadeOutEnd">When the blip should finish fading & disappear.</param>
            public FadingBlip(Blip blip, DateTime fadeOutStart, DateTime fadeOutEnd)
            {
                Blip = blip;
                FadeOutStart = fadeOutStart;
                FadeOutEnd = fadeOutEnd;
            }

            /// <summary>
            /// Computed alpha (opacity) based on the supplied <see cref="FadeOutStart"/> and <see cref="FadeOutEnd"/>.
            /// </summary>
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
                        // If we haven't reached the fade start timestamp yet, return max opacity.
                        if(Utility.CurrentTime <= FadeOutStart)
                        {
                            return 1f;
                        }
                        // Otherwise, compute an interval number based on time.
                        float magnitude = (float)(FadeOutEnd - FadeOutStart).TotalSeconds;
                        float t = (float)(Utility.CurrentTime - FadeOutStart).TotalSeconds;

                        // Prevent result going below 0.
                        if(t >= magnitude)
                        {
                            return 0f;
                        }

                        // Return the inverse of the normalised interval number to get the opacity.
                        return 1f - t / magnitude;
                    }
                }
            }
        }

        /// <summary>
        /// Display the current objective text at the bottom of the screen, as per the <paramref name="gameState"/>.
        /// </summary>
        /// <param name="gameState">Reference to this client's <see cref="GameState"/>.</param>
        /// <param name="playerState">Reference to this client's <see cref="PlayerState"/>.</param>
        /// <param name="ended">Pass true if the game is ending - this will make sure the objective text disappears on time.</param>
        public static void DisplayObjective(ref GameState gameState, ref PlayerState playerState, bool ended = false)
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

                // Determine how long the text should be displayed for.
                int objectiveTextDuration = 0;
                if (ended && gameState.Hunt.ActualEndTime > Utility.CurrentTime)
                {
                    // If the game is ending, display the text only for the "cooldown" time that occurs after each hunt.
                    objectiveTextDuration = Convert.ToInt32((gameState.Hunt.ActualEndTime - Utility.CurrentTime).TotalMilliseconds);
                }
                else
                {
                    // If the game isn't ending yet, display the text for the remaining hunt time.
                    objectiveTextDuration = Convert.ToInt32((gameState.Hunt.InitialEndTime - Utility.CurrentTime).TotalMilliseconds);
                }

                // Execute the text command.
                EndTextCommandPrint(objectiveTextDuration, true);
            }
        }

        /// <summary>
        /// Calls the right natives in order to show/hide the bigmap based on local player state.
        /// </summary>
        /// <param name="playerState">Local player state.</param>
        public static void SetBigmap(ref PlayerState playerState)
        {
            playerState.Bigmap.UpdateTime(GetFrameTime());

            // Is the map-expand control active?
            bool buttonPressed = IsControlJustReleased(0, 20);
            bool bigmapActive = IsBigmapActive();

            // Emulate GTAO bigmap behaviour.
            if (playerState.Bigmap.TimeSinceActivated >= 8000 || (buttonPressed && bigmapActive))
            {
                playerState.Bigmap.Hide();
            }
            else if (buttonPressed && !bigmapActive)
            {
                playerState.Bigmap.Show();
            }
        }

        /// <summary>
        /// Draws a timerbar in the bottom right corner indicating how long time there is remaining in the game.
        /// </summary>
        /// <param name="gameState">Most up-to-date game state.</param>
        public static void DrawRemainingTime(ref GameState gameState)
        {
            // Don't draw anything if game is not in progress or is ending.
            if (!gameState.Hunt.IsInProgress && !gameState.Hunt.IsEnding)
            {
                return;
            }

            // Format the time string.
            string timeStr = "";
            try
            {
                if (gameState.Hunt.InitialEndTime <= Utility.CurrentTime)
                {
                    timeStr = "00:00";
                }
                else
                {
                    TimeSpan remainingTime = gameState.Hunt.InitialEndTime - Utility.CurrentTime;
                    timeStr = $"{remainingTime.Minutes.ToString("00", CultureInfo.InvariantCulture)}:{remainingTime.Seconds.ToString("00", CultureInfo.InvariantCulture)}";
                }
            }
            catch (Exception)
            {
                timeStr = "00:00";
            }

            // Get rect width to fit the text.
            SetTextScale(0f, 0.55f);
            BeginTextCommandWidth("STRING");
            AddTextComponentString("TIME LEFT  00:00");
            float timebarWidth = EndTextCommandGetWidth(true);

            // Load and draw the timerbar using the rect width we've measured.
            RequestStreamedTextureDict("timerbars", true);
            if (HasStreamedTextureDictLoaded("timerbars"))
            {
                DrawSprite("timerbars", "all_black_bg", 0.92f, 0.855f, timebarWidth, 0.06f * 0.5f * 1.4f, 0f, 255, 255, 255, 128);
            }

            // Draw the time string on top of the timerbar.
            BeginTextCommandDisplayText("STRING");
            AddTextComponentString(timeStr);
            EndTextCommandDisplayText(0.94f, 0.835f);
            SetTextScale(0, 0.35f);
            BeginTextCommandDisplayText("STRING");
            AddTextComponentString("TIME LEFT");
            EndTextCommandDisplayText(0.94f - timebarWidth / 2.35f, 0.845f);
            SetTextScale(0, 1f);
        }

        /// <summary>
        /// Creates a radius blip for the hunted player, or updates the existing one's coordinates.
        /// </summary>
        /// <param name="player">Hunted player.</param>
        /// <param name="radius">Radius of the blip.</param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="creationTime"></param>
        /// <param name="playerState"></param>
        public static void CreateRadiusBlipForPlayer(Player player, float radius, float offsetX, float offsetY, DateTime creationTime, ref PlayerState playerState)
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

            // Set blip to be semi-transparent.
            SetBlipAlpha(RadiusBlip.Handle, 128);

            // Show the blip on both the map and the minimap.
            SetBlipDisplay(RadiusBlip.Handle, 6);

            // Attach the hunted player's name to the blip.
            SetBlipNameToPlayerName(RadiusBlip.Handle, player.Handle);

            // Show the blip in the map legend.
            SetBlipHiddenOnLegend(RadiusBlip.Handle, false);

            // If the local player is on the hunter team, display a GPS route to the hunted player's last pinged blip.
            if (playerState.Team == Team.Hunters)
            {
                SetBlipRoute(RadiusBlip.Handle, true);
            }

            // Starts fading the blip out.
            PingBlipOnMap(ref RadiusBlip, creationTime, TimeSpan.FromSeconds(Constants.HuntedBlipLifespan), TimeSpan.FromSeconds(Constants.HuntedBlipFadeoutTime));
        }

        /// <summary>
        /// Updates <see cref="FadingBlips"/>' alpha components and removes those that have reached 0 alpha.
        /// </summary>
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

        /// <summary>
        /// Adds the blip to <see cref="FadingBlips"/> so that it can begin fading over time.
        /// </summary>
        /// <param name="blip">The constructed blip to add.</param>
        /// <param name="creationTime">Spawn time of the blip.</param>
        /// <param name="lifespan">How long the blip should be visible for before starting to fade.</param>
        /// <param name="fadeOutTime">The time it takes for a blip to fade once <paramref name="lifespan"/> has been reached.</param>
        public static void PingBlipOnMap(ref Blip blip, DateTime creationTime, TimeSpan lifespan, TimeSpan fadeOutTime)
        {
            FadingBlips.Add(new FadingBlip(blip, creationTime + lifespan, creationTime + lifespan + fadeOutTime));
        }

        /// <summary>
        /// Sends a notification to all players containing the name of the zone the player is in on the map, and their mugshot.
        /// </summary>
        /// <param name="player">The hunted player.</param>
        /// <param name="position">The hunted player's position.</param>
        /// <param name="gameState">Most up-to-date game state.</param>
        public static void NotifyAboutHuntedZone(Player player, Vector3 position, ref GameState gameState)
        {
            string zoneName = GetLabelText(GetNameOfZone(position.X, position.Y, position.Z));
            string message = $"{player.Name} is somewhere in {zoneName} right now.";
            BeginTextCommandThefeedPost("STRING");
            if (gameState.Hunt.HuntedPlayerMugshot.IsValid)
            {
                // Attach the hunted player's mugshot texture if it is ready.
                AddTextComponentSubstringPlayerName(message);
                string txd = gameState.Hunt.HuntedPlayerMugshot.Name;
                EndTextCommandThefeedPostMessagetextTu(txd, txd, true, 0, player.Name, "Hunted Suspect", (Constants.HuntedBlipLifespan + Constants.HuntedBlipFadeoutTime) / Constants.FeedPostMessageDuration);
            }
            else
            {
                AddTextComponentString(message);
            }
            EndTextCommandThefeedPostTicker(true, true);
        }

        public static void CreatePlayerBlip(int playerPedEntity, int playerIndex, string playerName)
        {
            Blip blip = new Blip(AddBlipForEntity(playerPedEntity));
            blip.Name = playerName;
            Debug.WriteLine($"playerIndex: {playerIndex}");
            SetBlipColour(blip.Handle, playerIndex + 10);
            SetBlipDisplay(blip.Handle, 6);
            ShowHeadingIndicatorOnBlip(blip.Handle, true);
            SetBlipCategory(blip.Handle, 7);
            SetBlipShrink(blip.Handle, GetConvar("sth_shrinkPlayerBlips", "false") != "false");
            SetBlipScale(blip.Handle, 0.9f);

            dynamic playerBlip = new { blip, id = playerIndex };
            if (!PlayerBlips.ContainsKey(playerPedEntity))
            {
                PlayerBlips.Add(playerPedEntity, playerBlip);
            }
            else
            {
                if (DoesBlipExist(PlayerBlips[playerPedEntity].blip.Handle))
                {
                    int blipHandle = PlayerBlips[playerPedEntity].blip.Handle;
                    SetBlipDisplay(blipHandle, 0);
                    RemoveBlip(ref blipHandle);
                }
                PlayerBlips[playerPedEntity] = playerBlip;
            }
        }

        /// <summary>
        /// Manages the blips for the local player and their teammates.
        /// </summary>
        /// <param name="players">Currently playing players.</param>
        /// <param name="gameState">Most up-to-date game state.</param>
        /// <param name="playerState">Local player state.</param>
        public static void UpdateTeammateBlips(PlayerList players, ref GameState gameState, ref PlayerState playerState)
        {
            // Set the player's blip colour based on their ID so that it is unique & replicated across all clients.
            SetBlipColour(GetMainPlayerBlipId(), Game.Player.ServerId + 10);
            
            // TODO: The heavy use of collections in this method seems to increase the tick time by a considerable amount.
            // Need to only invoke the blip update when a player connects, disconnects or dies; Otherwise only update the visibility of existing blips.
            foreach (int ped in PlayerBlips.Keys)
            {
                if(ped == PlayerPedId())
                {
                    continue;
                }

                // Mark the player as an active ped to know that its blips & gamertag shouldn't be deleted.
                if (!IsPedDeadOrDying(ped, true))
                {
                    ActivePeds.Add(ped);
                }
            }

            // Display player names on blips (in bigmap).
            N_0x82cedc33687e1f50(true);

            List<int> pedsToDelete = new List<int>();

            foreach(int ped in PlayerBlips.Keys)
            {
                // Delete check
                if (!ActivePeds.Contains(ped))
                {
                    pedsToDelete.Add(ped);
                    continue;
                }
                
                if(gameState.Hunt.IsStarted && (playerState.Team == Team.Hunted || PlayerBlips[ped].id == gameState.Hunt.HuntedPlayer.Handle) && !GameState.IsPedTooFar(new Ped(ped)))
                {
                    // Hide the blip if it's within the play area bounds and the player is on the opposite team.
                    Blip blip = PlayerBlips[ped].blip;
                    SetBlipDisplay(blip.Handle, 0);
                }
                else
                {
                    // Show the blip if it's out of the play area.
                    Blip blip = PlayerBlips[ped].blip;
                    SetBlipDisplay(blip.Handle, 6);
                }
            }

            // Delete inactive peds.
            foreach(int ped in pedsToDelete)
            {
                Blip blip = PlayerBlips[ped].blip;
                blip.Delete();
                PlayerBlips.Remove(ped);
            }

            ActivePeds.Clear();
        }

        public static void UpdatePlayerOverheadNames(PlayerList players, ref GameState gameState, ref PlayerState playerState) 
        {
            foreach (Player player in players)
            {
                // Creates overhead player name labels if need be.
                if (!IsMpGamerTagActive(player.Handle))
                {
                    CreateMpGamerTagWithCrewColor(player.Handle, player.Name, false, false, "", 0, 0, 0, 0);
                    if(!PlayerOverheadNames.ContainsKey(player.Character.Handle))
                    {
                        PlayerOverheadNames.Add(player.Character.Handle, player.Handle);
                    }
                }

                if (IsMpGamerTagActive(player.Handle) && PlayerBlips.TryGetValue(player.Character.Handle, out dynamic playerBlip))
                {
                    SetMpGamerTagColour(player.Handle, 0, GetBlipHudColour(playerBlip.blip.Handle));
                }
                else if(IsMpGamerTagActive(player.Handle) && player.ServerId == Game.Player.ServerId)
                {
                    SetMpGamerTagColour(player.Handle, 0, GetBlipHudColour(GetMainPlayerBlipId()));
                }
            }

            List<int> playersToDelete = new List<int>();

            foreach (int playerPed in PlayerOverheadNames.Keys)
            {
                int playerHandle = PlayerOverheadNames[playerPed];

                // Delete check
                bool playerExists = false;
                try
                {
                    playerExists = DoesEntityExist(playerPed);
                } 
                catch
                {
                    playerExists = false;
                }

                if (!playerExists)
                {
                    playersToDelete.Add(playerPed);
                    continue;
                }
                else
                {
                    bool isPlayerOOB = GameState.IsPedTooFar(new Ped(playerPed));
                    if (gameState.Hunt.IsStarted && (playerState.Team == Team.Hunted || playerHandle == gameState.Hunt.HuntedPlayer.Handle || playerHandle == Game.Player.Handle) && !isPlayerOOB)
                    {
                        SetMpGamerTagVisibility(playerHandle, 0, false);
                    }
                    else if((gameState.Hunt.IsStarted && isPlayerOOB) || (gameState.Hunt.IsStarted && playerHandle != Game.Player.Handle))
                    {
                        SetMpGamerTagVisibility(playerHandle, 0, true);
                    }
                }
            }

            // Delete inactive peds.
            foreach (int playerPed in playersToDelete)
            {
                if (!IsMpGamerTagFree(PlayerOverheadNames[playerPed]))
                {
                    RemoveMpGamerTag(PlayerOverheadNames[playerPed]);
                }
                PlayerOverheadNames.Remove(playerPed);
            }
        }
    }
}