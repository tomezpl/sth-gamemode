using CitizenFX.Core;
using CitizenFX.Core.Native;
using SurviveTheHuntShared;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    internal static class KillTracker
    {
        public const float AttackerTimeout = 5f;

        internal static float TimeSinceLastDamage = 0f;

        internal static int? LastAttacker = null;

        internal static int LastTickHealth = 0;

        internal static void Reset()
        {
            LastAttacker = null;
            LastTickHealth = 0;
            TimeSinceLastDamage = 0f;
        }

        internal static void Tick()
        {
            int playerPed = PlayerPedId();

            if(DoesEntityExist(playerPed))
            {
                int currentHealth = GetEntityHealth(playerPed);

                int currentAttacker = 0;
                if (currentHealth == 0)
                {
                    currentAttacker = GetPedSourceOfDeath(playerPed);
                }

                // If there was an attacker ped, set the controlling player as the latest attacker.
                if(currentAttacker != 0)
                {
                    LastAttacker = NetworkGetPlayerIndexFromPed(currentAttacker);
                }
                else if(TimeSinceLastDamage >= AttackerTimeout || currentHealth < LastTickHealth)
                {
                    LastAttacker = null;
                }

                if(currentHealth < LastTickHealth)
                {
                    TimeSinceLastDamage = 0f;
                }
                else
                {
                    if (TimeSinceLastDamage < AttackerTimeout)
                    {
                        TimeSinceLastDamage += GetFrameTime();
                    }
                }

                LastTickHealth = currentHealth;
            }
        }

        internal static KillFeedClientPayload GetKillInfo()
        {
            if(TimeSinceLastDamage < AttackerTimeout && LastAttacker.HasValue)
            {
                return new KillFeedClientPayload
                {
                    AttackerServerId = GetPlayerServerId(LastAttacker.Value).ToString(),
                    VictimServerId = GetPlayerServerId(PlayerId()).ToString()
                };
            }

            return new KillFeedClientPayload
            {
                AttackerServerId = null,
                VictimServerId = GetPlayerServerId(PlayerId()).ToString(),
            };
        }
    }
}
