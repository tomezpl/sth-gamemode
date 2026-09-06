using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers
{
    internal sealed class HuntedPassiveHealController : ITickable
    {
        internal readonly bool AllowedLocal;
        private readonly int[] PlayerIdsToHealFrom;

        internal const float HealIntervalSeconds = 3f;
        internal const int HealRate = 5;

        internal const float HealRadius = 4.5f;
        private const float HealRadiusSquared = HealRadius * HealRadius;

        internal HuntedPassiveHealController(int localPlayerId, HuntPlayer[] huntedPlayers)
        {
            List<int> nonLocalPlayerIds = new List<int>(huntedPlayers.Length);

            AllowedLocal = false;
            foreach(HuntPlayer player in huntedPlayers)
            {
                if(player.PlayerHandle == localPlayerId)
                {
                    AllowedLocal = true;
                }
                else
                {
                    nonLocalPlayerIds.Add(player.PlayerHandle);
                }
            }

            PlayerIdsToHealFrom = nonLocalPlayerIds.ToArray();
        }

        private float _timeSinceLastHealSeconds = 0f;
        public void Tick(float deltaTime)
        {
            if(AllowedLocal)
            {
                _timeSinceLastHealSeconds += deltaTime;

                // intentionally healing on a timer as opposed to just multiplying by delta time because:
                // 1. precision
                // 2. performance hit from calling into the game's natives every tick
                if(_timeSinceLastHealSeconds >= HealIntervalSeconds)
                {
                    _timeSinceLastHealSeconds = 0f;

                    int localPlayerPed = PlayerPedId();
                    if (!IsPedDeadOrDying(localPlayerPed, false))
                    {
                        foreach (int healSourcePlayerId in PlayerIdsToHealFrom)
                        {
                            int playerPed = GetPlayerPed(healSourcePlayerId);

                            if (playerPed != 0 && !IsPedDeadOrDying(playerPed, false))
                            {
                                Vector3 pos = GetEntityCoords(playerPed, false);
                                Vector3 myPos = GetEntityCoords(localPlayerPed, false);

                                if (pos.DistanceToSquared(myPos) <= HealRadiusSquared)
                                {
                                    SetEntityHealth(localPlayerPed, GetEntityHealth(localPlayerPed) + HealRate);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
