using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntShared.Core
{
    public struct PlayerDiedPayload
    {
        public int PlayerId;
        public float PlayerPosX, PlayerPosY, PlayerPosZ;
        public Teams.Team PlayerTeam;
        public KillFeedClientPayload KillInfo;

        public static string Serialize(PlayerDiedPayload payload)
        {
            return $"{payload.PlayerId};{payload.PlayerPosX};{payload.PlayerPosY};{payload.PlayerPosZ};{(int)payload.PlayerTeam};{payload.KillInfo.AttackerServerId ?? ""};{payload.KillInfo.VictimServerId ?? ""}";
        }

        public static PlayerDiedPayload Deserialize(string serialized)
        {
            //Debug.WriteLine("WRITING SERIALIZED DATA");
            //Debug.WriteLine(serialized);
            string[] serializedParts = serialized.Split(';');

            return new PlayerDiedPayload
            {
                PlayerId = int.Parse(serializedParts[0]),
                PlayerPosX = float.Parse(serializedParts[1]),
                PlayerPosY = float.Parse(serializedParts[2]),
                PlayerPosZ = float.Parse(serializedParts[3]),
                PlayerTeam = (Teams.Team)int.Parse(serializedParts[4]),
                KillInfo = new KillFeedClientPayload
                {
                    AttackerServerId = string.IsNullOrEmpty(serializedParts[5]) ? null : serializedParts[5],
                    VictimServerId = string.IsNullOrEmpty(serializedParts[6]) ? null : serializedParts[6],
                }
            };
        }
    }
}
