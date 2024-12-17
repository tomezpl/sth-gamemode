using SurviveTheHuntShared.Utils;
using System;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public partial class MainScript
    {
        private ushort AmmoCheckTimer = 0;

        private const ushort AmmoCheckInterval = 500;

        private Weapons.WeaponAmmo[] AmmoState = new Weapons.WeaponAmmo[0];

        private int? HealthState = null;

        private long? LastSpawnTime = null;

        public void TickLbgCharNeoIntegration()
        {
            AmmoCheckTimer += (ushort)Math.Round(GetFrameTime() * 1000f);
            if (AmmoCheckTimer >= AmmoCheckInterval)
            {
                Weapons.WeaponAmmo[] selectedLoadout = Constants.WeaponLoadouts[PlayerState.Team];

                AmmoState = new Weapons.WeaponAmmo[selectedLoadout.Length];

                int playerPed = PlayerPedId();

                for (int i = 0; i < AmmoState.Length; i++)
                {
                    AmmoState[i] = new Weapons.WeaponAmmo(selectedLoadout[i].Hash, (ushort)GetAmmoInPedWeapon(playerPed, selectedLoadout[i].Hash));
                }

                AmmoCheckTimer = 0;

                HealthState = GetEntityHealth(playerPed);
            }
        }
    }
}
