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
}