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

    /// <summary>
    /// Wrapper for a <see cref="CitizenFX.Core.Vector3"/> position component combined with the direction (heading).
    /// </summary>
    public class Coord
    {
        public CitizenFX.Core.Vector3 Position { get; set; } = new CitizenFX.Core.Vector3();

        /// <summary>
        /// Direction (angle) of the coord on the XY plane.
        /// </summary>
        public float Heading { get; set; } = 0f;
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