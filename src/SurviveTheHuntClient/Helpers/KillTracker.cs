using CitizenFX.Core;
using CitizenFX.Core.Native;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntShared;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    internal class KillTracker : ITickable
    {
        public const float AttackerTimeout = 5f;

        internal float TimeSinceLastDamage = 0f;

        internal int? LastAttacker = null;

        internal int LastTickHealth = 0;

        internal void Reset()
        {
            LastAttacker = null;
            LastTickHealth = 0;
            TimeSinceLastDamage = 0f;
        }

        public void Tick(float deltaTime)
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

        internal KillFeedClientPayload GetKillInfo()
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
