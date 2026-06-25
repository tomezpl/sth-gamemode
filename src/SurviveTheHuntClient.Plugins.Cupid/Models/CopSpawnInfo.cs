using CitizenFX.Core;

namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal sealed class CopSpawnInfo
    {
        internal struct CameraInfo
        {
            internal Vector3 Pos;
            internal Vector3 Rot;

            internal CameraInfo(Vector3 pos, Vector3 rot)
            {
                Pos = pos;
                Rot = rot;
            }
        }

        internal CameraInfo Camera;

        internal struct SpawnPosInfo
        {
            internal Vector3 Pos;
            internal readonly float Heading;

            internal SpawnPosInfo(Vector3 pos, float heading = 0f)
            {
                Pos = pos;
                Heading = heading;
            }
        }

        internal SpawnPosInfo Spawn;

        internal CopSpawnInfo(CameraInfo camera, SpawnPosInfo spawn)
        {
            Camera = camera;
            Spawn = spawn;
        }
    }
}
