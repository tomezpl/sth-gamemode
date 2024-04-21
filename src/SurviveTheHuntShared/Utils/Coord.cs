namespace SurviveTheHuntShared.Utils
{

    /// <summary>
    /// Wrapper for a <see cref="Vector3"/> position component combined with the direction (heading).
    /// </summary>
    public class Coord
    {
        public Vector3 Position { get; set; } = new Vector3();

        /// <summary>
        /// Direction (angle) of the coord on the XY plane.
        /// </summary>
        public float Heading { get; set; } = 0f;
    }
}
