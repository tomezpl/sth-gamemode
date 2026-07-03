using CitizenFX.Core;
using System;

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

        internal CarPosInfo CarLayout;

        internal struct CarPosInfo
        {
            internal Vector3 Origin;
            internal Vector2 Step;
            internal readonly byte Count;
            internal readonly float Heading;

            internal CarPosInfo(Vector3 origin, Vector2 step, Vector3 end, float heading) : this(origin, step, (byte)Math.Round((end - origin).Length() / step.Length()), heading)
            {
            }

            internal CarPosInfo(Vector3 origin, Vector2 step, byte count, float heading)
            {
                Origin = origin;
                Step = step;
                Count = count;
                Heading = heading;
            }
        }

        internal readonly string Name;

        internal readonly uint[] CopCarModels;

        private static readonly uint[] s_DefaultCarModels =
        {
            (uint)VehicleHash.Police,
            (uint)VehicleHash.Police2,
            (uint)VehicleHash.Police3,
        };

        internal CopSpawnInfo(string name, CameraInfo camera, SpawnPosInfo spawn, CarPosInfo carLayout) : this(name, camera, spawn, carLayout, copCarModels: s_DefaultCarModels) { }

        internal CopSpawnInfo(string name, CameraInfo camera, SpawnPosInfo spawn, CarPosInfo carLayout, params uint[] copCarModels)
        {
            Name = name;
            Camera = camera;
            Spawn = spawn;
            CarLayout = carLayout;
            CopCarModels = copCarModels;
        }
    }
}
