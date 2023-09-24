using CitizenFX.Core;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    public class DeathBlips
    {
        private struct DeathBlip
        {
            public readonly int handle;
            public readonly long creationTime;

            public DeathBlip(int handle = -1, long creationTime = 0)
            {
                this.handle = handle;
                this.creationTime = creationTime;
            }
        }

        private readonly List<DeathBlip> _deathBlips = new List<DeathBlip>();

        private readonly long _blipLifetime;

        /// <summary>
        /// Creates the death blip helper with the specified lifetime for each death blip.
        /// </summary>
        /// <param name="blipLifetime">Lifetime of a death blip (in seconds)</param>
        public DeathBlips(int blipLifetime = Constants.DefaultDeathBlipLifespan)
        {
            _blipLifetime = (long)blipLifetime * 1000;
        }

        public void Add(float positionX, float positionY, float positionZ)
        {
            int blipHandle = AddBlipForCoord(positionX, positionY, positionZ);
            SetBlipSprite(blipHandle, 274);
            long creationTime = GetGameTimer();

            _deathBlips.Add(new DeathBlip(blipHandle, creationTime));
        }

        public void ClearExpiredBlips()
        {
            long currentTime = GetGameTimer();

            for(int i = _deathBlips.Count - 1; i >= 0; i--)
            {
                DeathBlip deathBlip = _deathBlips[i];

                if(currentTime - deathBlip.creationTime >= _blipLifetime)
                {
                    int handle = deathBlip.handle;
                    RemoveBlip(ref handle);
                    _deathBlips.RemoveAt(i);
                }
            }
        }
    }
}
