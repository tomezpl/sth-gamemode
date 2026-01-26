using SurviveTheHuntShared.Utils;
using System;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public partial class MainScript
    {
        private float AmmoCheckTimer = 0f;

        private const float AmmoCheckInterval = 0.5f;

        private Weapons.WeaponAmmo[] AmmoState = new Weapons.WeaponAmmo[0];

        private int? HealthState = null;

        private long? LastSpawnTime = null;

        public void TickLbgCharNeoIntegration(float deltaTime)
        {
            AmmoCheckTimer += deltaTime;
            if (AmmoCheckTimer >= AmmoCheckInterval)
            {
                Weapons.WeaponAmmo[] selectedLoadout = Constants.GetModeWeaponLoadouts(GameState.Mode)[PlayerState.Team];

                AmmoState = new Weapons.WeaponAmmo[selectedLoadout.Length];

                int playerPed = PlayerPedId();

                for (int i = 0; i < AmmoState.Length; i++)
                {
                    AmmoState[i] = new Weapons.WeaponAmmo(selectedLoadout[i].Hash, (ushort)GetAmmoInPedWeapon(playerPed, selectedLoadout[i].Hash), selectedLoadout[i].Attachments);
                }

                AmmoCheckTimer = 0;

                HealthState = GetEntityHealth(playerPed);
            }
        }
    }
}
