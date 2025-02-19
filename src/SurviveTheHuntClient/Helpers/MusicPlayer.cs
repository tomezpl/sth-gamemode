using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    /// <summary>
    /// Helper class to manage playing music events using the GTA V natives.
    /// </summary>
    internal static class MusicPlayer
    {
        /// <summary>
        /// All events that *can* be used by this script. This will be used to clear all music events.
        /// 
        /// Each field is an array of strings, as certain events require to be triggered in order before they can play.
        /// </summary>
        internal class AllEventsType
        {
            public readonly string[] Pursuit =
            {
                "CAR1_MISSION_START",
                "CAR1_CHASE_START"
            };
        }

        internal static AllEventsType AllEvents = new AllEventsType();

        internal static readonly string[] AllEventsNames = BuildAllEventsCache();

        /// <summary>
        /// Gets all music event names from <see cref="AllEvents"/> and creates a flat string array of music event names.
        /// 
        /// This should only be run once as it uses reflection so will be quite expensive
        /// </summary>
        /// <returns>A string array of all music event names (may contain duplicates)</returns>
        private static string[] BuildAllEventsCache()
        {
            FieldInfo[] allEvents = typeof(AllEventsType).GetFields();
            List<string> eventNames = new List<string>(allEvents.Length);
            foreach(FieldInfo field in allEvents)
            {
                string[] eventChain = field.GetValue(AllEvents) as string[];
                eventNames.AddRange(eventChain);
            }

            return eventNames.ToArray();
        }

        internal static void ClearMusic()
        {
            foreach(string eventName in AllEventsNames)
            {
                CancelMusicEvent(eventName);
            }
            PrepareMusicEvent("GTA_ONLINE_STOP_SCORE");
            TriggerMusicEvent("GTA_ONLINE_STOP_SCORE");
        }
    }
}
