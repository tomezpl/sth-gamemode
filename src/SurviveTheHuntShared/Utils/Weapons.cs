using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntShared.Utils
{
    public class Weapons
    {
        public static bool IsAttachmentHashKey(string attachment)
        {
            return attachment.StartsWith("COMPONENT_");
        }

        /// <summary>
        /// GTAV weapon component names grouped by generic attachment names to act as short-hands.
        /// </summary>
        public static ReadOnlyDictionary<string, string[]> AttachmentNames = new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>
        {
            {"suppressor", new string[] { "COMPONENT_AT_PI_SUPP_02", "COMPONENT_AT_PI_SUPP", "COMPONENT_AT_AR_SUPP_02", "COMPONENT_AT_AR_SUPP", "COMPONENT_CERAMICPISTOL_SUPP", "COMPONENT_AT_SR_SUPP", "COMPONENT_AT_SR_SUPP_03" } }
        });

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

            public string[] Attachments;

            /// <summary>
            /// Creates a new <see cref="WeaponAmmo"/> for the weapon with the given <paramref name="hash"/> and amount of <paramref name="ammo"/>.
            /// </summary>
            /// <param name="hash">Weapon hash</param>
            /// <param name="ammo">Ammo count</param>
            public WeaponAmmo(uint hash, ushort ammo, IEnumerable<string> attachments)
            {
                Hash = hash;
                Ammo = ammo;
                Attachments = attachments.ToArray();
            }
        }
    }
}
