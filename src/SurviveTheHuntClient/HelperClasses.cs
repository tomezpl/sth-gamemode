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

    public struct ConfigPayload
    {
        public uint[] WeaponsHunted;
        public uint[] WeaponsHunters;
    }

    public class Weapons
    {
        public struct WeaponAmmo
        {
            public uint Hash;
            public ushort Ammo;

            public WeaponAmmo(uint hash, ushort ammo)
            {
                Hash = hash;
                Ammo = ammo;
            }
        }

        public static WeaponAmmo[] UnpackLoadout(uint[] serializedLoadout)
        {
            WeaponAmmo[] weapons = new WeaponAmmo[serializedLoadout.Length / 2];

            for(ushort i = 0; i < serializedLoadout.Length; i += 2)
            {
                weapons[i / 2] = new WeaponAmmo(serializedLoadout[i], (ushort)serializedLoadout[i + 1]);
            }

            return weapons;
        }
    }
}