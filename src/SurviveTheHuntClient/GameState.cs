using System;
using CitizenFX.Core;

using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public class GameState
    {
        public string CurrentObjective { get; set; }

        public class HuntDetails
        {
            public bool IsStarted { get; set; } = false;
            public bool IsOver { get; set; } = false;
            public bool IsInProgress { get { return IsStarted && !IsOver; } }
            public bool IsEnding { get { return !IsInProgress && IsOver; } }
            public bool CanBeStarted { get { return !IsInProgress && !IsEnding; } }
            public Player HuntedPlayer { get; set; } = null;

            public Texture HuntedPlayerMugshot { get; set; } = null;

            public DateTime LastHuntedMugshotGeneration { get; set; } = Utility.CurrentTime;

            // Time when hunt is meant to end & state be reset 
            // This includes the delay for displaying the win/loss text.
            public DateTime ActualEndTime { get; set; } = new DateTime();
            
            // End time as planned (set at start of the hunt)
            public DateTime InitialEndTime { get; set; } = new DateTime();

            public void End()
            {
                Debug.WriteLine("Ending hunt");
                IsStarted = false;
                IsOver = false;

                ActualEndTime = new DateTime();

                if(HuntedPlayerMugshot != null)
                {
                    UnregisterPedheadshot(HuntedPlayerMugshot.Id);
                }
                HuntedPlayerMugshot = null;
            }

            public void UpdateHuntedMugshot()
            {
                if(!IsStarted)
                {
                    return;
                }

                if(HuntedPlayerMugshot != null && Utility.CurrentTime - LastHuntedMugshotGeneration >= Constants.MugshotGenerationInterval)
                {
                    UnregisterPedheadshot(HuntedPlayerMugshot.Id);
                    HuntedPlayerMugshot = null;
                }

                if (HuntedPlayerMugshot == null)
                {
                    HuntedPlayerMugshot = new Texture() { Id = RegisterPedheadshotTransparent(HuntedPlayer.Character.Handle) };
                    LastHuntedMugshotGeneration = Utility.CurrentTime;
                }

                if (!HuntedPlayerMugshot.IsValid)
                {
                    if(IsPedheadshotReady(HuntedPlayerMugshot.Id) && IsPedheadshotValid(HuntedPlayerMugshot.Id))
                    {
                        HuntedPlayerMugshot.Name = GetPedheadshotTxdString(HuntedPlayerMugshot.Id);
                    }
                }
            }
        }

        public HuntDetails Hunt { get; set; } = new HuntDetails();

        public static bool IsPedTooFar(Ped ped)
        {
            return ped.Position.Y >= Constants.ZLimit;
        }
    }
}