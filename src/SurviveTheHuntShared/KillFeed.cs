using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntShared
{
    public struct KillFeedClientPayload
    {
        public string AttackerServerId;
        public string VictimServerId;

        public static string Serialize(KillFeedClientPayload data)
        {
            return $"{data.AttackerServerId ?? ""};{data.VictimServerId ?? ""}";
        }

        public static KillFeedClientPayload Deserialize(string serialized)
        {
            string[] parts = serialized.Split(';');
            return new KillFeedClientPayload
            {
                AttackerServerId = parts[0],
                VictimServerId = parts[1]
            };
        }
    }

    public struct KillFeedServerPayload
    {
        public KillFeedClientPayload KillInfo;
        public KillFeedLabel Label;

        public static string Serialize(KillFeedServerPayload data)
        {
            return $"{data.KillInfo.AttackerServerId ?? ""};{data.KillInfo.VictimServerId ?? ""};{data.Label.AttackerGxt};{data.Label.VictimGxt};{data.Label.WitnessGxt}"; 
        }

        public static KillFeedServerPayload Deserialize(string serializedData)
        {
            string[] parts = serializedData.Split(';');

            return new KillFeedServerPayload
            {
                KillInfo = new KillFeedClientPayload
                {
                    AttackerServerId = parts[0],
                    VictimServerId = parts[1]
                },
                Label = new KillFeedLabel
                {
                    AttackerGxt = parts[2],
                    VictimGxt = parts[3],
                    WitnessGxt = parts[4]
                }
            };
        }
    }

    public struct KillFeedLabel
    {
        public string AttackerGxt;
        public string VictimGxt;
        public string WitnessGxt;

        /// <summary>
        /// This returns either 0, 1, or 2 depending on how many player names need to be inserted into the text.
        /// </summary>
        /// <param name="gxtText">Text from the loaded GXT label.</param>
        /// <returns></returns>
        public static byte CountPlayerNames(string gxtText)
        {
            if (gxtText != null)
            {
                const string playerPlaceholder = "~a~";
                int index = gxtText.IndexOf(playerPlaceholder);
                if (index != -1)
                {
                    if (index != gxtText.LastIndexOf(playerPlaceholder))
                    {
                        return 2;
                    }

                    return 1;
                }
            }

            return 0;
        }
    }

    public static partial class Constants
    {
        public static class KillFeedMessages
        {
            public static KillFeedLabel[] RegularLabels =
            {
                new KillFeedLabel{AttackerGxt = "DM_TICK2", VictimGxt = "DM_TICK1", WitnessGxt = "DM_TICK6"}
            };

            public static KillFeedLabel SelfKillLabel = new KillFeedLabel { AttackerGxt = "DM_O_SUIC", WitnessGxt = "DM_O_SUIC", VictimGxt = "DM_U_SUIC" };

            public static KillFeedLabel FallbackLabel = new KillFeedLabel { VictimGxt = "DM_TK_YD1", AttackerGxt = "TICK_DIED", WitnessGxt = "TICK_DIED" };

            public static KillFeedLabel HuntedKillLabel = new KillFeedLabel { WitnessGxt = "FM_TGDM_KIL", AttackerGxt = "FM_TGDM_KIL", VictimGxt = "FM_TGDM_KIL" };
        }
    }
}
