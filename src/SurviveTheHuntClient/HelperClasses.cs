namespace SurviveTheHuntClient
{
    public struct Vec3
    {
        public float X, Y, Z;
    }

    public class Texture
    {
        public string Name { get; set; } = null;
        public int Id { get; set; } = -1;

        public bool IsValid { get { return Name != null; } }
    }

    public class Coord
    {
        public CitizenFX.Core.Vector3 Position { get; set; } = new CitizenFX.Core.Vector3();
        public float Heading { get; set; } = 0f;
    }
}