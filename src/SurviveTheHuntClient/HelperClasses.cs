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
}