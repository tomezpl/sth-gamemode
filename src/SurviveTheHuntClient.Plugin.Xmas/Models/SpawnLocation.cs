namespace SurviveTheHuntClient.Plugins.Xmas.Models
{
    internal class SpawnLocation
    {
        internal CitizenFX.Core.Vector4 PosAndHeading;
        internal CitizenFX.Core.Vector3 CameraPos;

        internal CitizenFX.Core.Vector3 Pos => new CitizenFX.Core.Vector3(PosAndHeading.X, PosAndHeading.Y, PosAndHeading.Z);
        internal float Heading => PosAndHeading.W;

        internal readonly string Name;

        internal SpawnLocation(CitizenFX.Core.Vector4 posAndHeading, CitizenFX.Core.Vector3 cameraPos, string name = null)
        {
            PosAndHeading = posAndHeading;
            CameraPos = cameraPos;
            Name = name;
        }
    }
}
