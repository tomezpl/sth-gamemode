using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntShared.Utils
{
    public class Weapons
    {
        /// <summary>
        /// A helper structure to define a weapon with a specified ammo count.
        /// </summary>
        public struct WeaponAmmo
        {
            /// <summary>
            /// Weapon hash.
            /// </summary>
            public uint Hash;

            /// <summary>
            /// Ammo count
            /// </summary>
            public ushort Ammo;

            /// <summary>
            /// Creates a new <see cref="WeaponAmmo"/> for the weapon with the given <paramref name="hash"/> and amount of <paramref name="ammo"/>.
            /// </summary>
            /// <param name="hash">Weapon hash</param>
            /// <param name="ammo">Ammo count</param>
            public WeaponAmmo(uint hash, ushort ammo)
            {
                Hash = hash;
                Ammo = ammo;
            }
        }
    }
}
