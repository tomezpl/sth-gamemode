namespace SurviveTheHuntClient
{
    /// <summary>
    /// Represents TXD identifiers for a texture.
    /// </summary>
    public class Texture
    {
        /// <summary>
        /// Name of the texture as obtained from GetTxdString.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// ID/handle of the texture.
        /// </summary>
        public int Id { get; set; } = -1;

        /// <summary>
        /// Has the texture been assigned a name?
        /// </summary>
        public bool IsValid { get { return Name != null; } }
    }

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