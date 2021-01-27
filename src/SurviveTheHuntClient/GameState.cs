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

            public DateTime LastHuntedMugshotGeneration { get; set; } = DateTime.Now;

            public int EndInMilliseconds { get; set; } = -1;
            public DateTime EndTime { get; set; } = new DateTime();

            public void End()
            {
                IsStarted = false;
                IsOver = false;

                EndInMilliseconds = -1;

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

                if(HuntedPlayerMugshot != null && DateTime.Now - LastHuntedMugshotGeneration >= Constants.MugshotGenerationInterval)
                {
                    UnregisterPedheadshot(HuntedPlayerMugshot.Id);
                    HuntedPlayerMugshot = null;
                }

                if (HuntedPlayerMugshot == null)
                {
                    HuntedPlayerMugshot = new Texture() { Id = RegisterPedheadshotTransparent(HuntedPlayer.Character.Handle) };
                    LastHuntedMugshotGeneration = DateTime.Now;
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