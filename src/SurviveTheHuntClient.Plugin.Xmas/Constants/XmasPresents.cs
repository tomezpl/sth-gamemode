using SurviveTheHuntClient.Plugins.Xmas.Models;
using Vec3 = CitizenFX.Core.Vector3;

namespace SurviveTheHuntClient.Plugins.Xmas
{
    internal partial class Constants
    {
        internal enum PrezzieLocationTag
        {
            Default = 0,
            Chimney,
            Gerald,
            KnoWay,
            Lifeinvader,
            Richards,
            VespucciBagger,
            RaspberryJam,
            ConstructionTools
        }

        internal const float DefaultPrezzieRadius = 1.9f;

        internal static readonly PrezzieLocation[] PresentsLocations =
        {
            new PrezzieLocation(new Vec3(326.69f, -1851.81f, 32.36f), tag: PrezzieLocationTag.Chimney),
            new PrezzieLocation(new Vec3(349.258f, -1825.76f, 32.11f), tag: PrezzieLocationTag.Chimney),
            new PrezzieLocation(new Vec3{X = 192.7535f, Y = -1724.643f, Z = 34.03562f}, tag: PrezzieLocationTag.Chimney),
            new PrezzieLocation(new Vec3{X = -60.24393f, Y = -1530.259f, Z = 34.23524f}, tag: PrezzieLocationTag.Gerald),
            // TODO: xmas tree in legion
            new PrezzieLocation(new Vec3{X = 337.1546f, Y = -224.6435f, Z = 58.01926f}),
            new PrezzieLocation(new Vec3{X = 213.3294f, Y = -133.444f, Z = 69.3039f}),
            new PrezzieLocation(new Vec3{X = 156.8125f, Y = -116.7936f, Z = 62.44466f}),
            new PrezzieLocation(new Vec3{X = -21.37654f, Y = -24.57586f, Z = 73.24542f}),
            new PrezzieLocation(new Vec3{X = -437.7124f, Y = -428.6397f, Z = 32.9384f}, tag: PrezzieLocationTag.KnoWay),
            new PrezzieLocation(new Vec3{X = -1044.974f, Y = -230.2998f, Z = 39.01436f}, tag: PrezzieLocationTag.Lifeinvader),
            new PrezzieLocation(new Vec3{X = -1007.216f, Y = -487.1665f, Z = 39.97014f}, tag: PrezzieLocationTag.Richards),
            new PrezzieLocation(new Vec3{X = -911.5113f, Y = -446.8966f, Z = 39.60527f}),
            new PrezzieLocation(new Vec3{X = -989.6367f, Y = -880.392f, Z = 14.58203f}, tag: PrezzieLocationTag.Chimney),
            new PrezzieLocation(new Vec3{X = -1090.248f, Y = -936.4327f, Z = 10.1144f}),
            new PrezzieLocation(new Vec3{X = -1193.57f, Y = -1051.01f, Z = 7.859039f}),
            new PrezzieLocation(new Vec3{X = -1311.239f, Y = -1034.266f, Z = 15.82702f}),
            new PrezzieLocation(new Vec3{X = -1311.565f, Y = -1033.916f, Z = 21.47998f}),
            new PrezzieLocation(new Vec3{X = -1308.07f, Y = -1030.614f, Z = 28.72043f}),
            new PrezzieLocation(new Vec3{X = -1302.216f, Y = -1050.84f, Z = 15.82703f}),
            new PrezzieLocation(new Vec3{X = -1302.338f, Y = -1050.63f, Z = 21.48002f}),
            new PrezzieLocation(new Vec3{X = -1297.184f, Y = -1049.196f, Z = 28.72046f}),
            new PrezzieLocation(new Vec3{X = -1340.449f, Y = -1192.327f, Z = 8.761258f}),
            new PrezzieLocation(new Vec3{X = -1339.716f, Y = -1192.333f, Z = 12.83966f}),
            new PrezzieLocation(new Vec3{X = -1076.373f, Y = -1677.39f, Z = 4.575236f}, tag: PrezzieLocationTag.VespucciBagger),
            new PrezzieLocation(new Vec3{X = -1158.622f, Y = -1520.54f, Z = 10.63271f}, tag: PrezzieLocationTag.RaspberryJam),
            new PrezzieLocation(new Vec3{X = -1087.8f, Y = -1072.512f, Z = 12.5733f}, tag: PrezzieLocationTag.Chimney),
            new PrezzieLocation(new Vec3{X = -1176.516f, Y = 298.973f, Z = 73.64558f}),
            new PrezzieLocation(new Vec3{X = -1131.189f, Y = 371.4822f, Z = 74.93344f}),
            new PrezzieLocation(new Vec3{X = -1128.646f, Y = 364.1219f, Z = 78.52596f}),
            new PrezzieLocation(new Vec3{X = -1539.401f, Y = 134.5233f, Z = 70.86279f}, tag: PrezzieLocationTag.Chimney),
            new PrezzieLocation(new Vec3{X = -1645.527f, Y = 35.50418f, Z = 74.229f}, tag: PrezzieLocationTag.Chimney),
            new PrezzieLocation(new Vec3{X = -1549.291f, Y = -276.1512f, Z = 48.26821f}, tag: PrezzieLocationTag.ConstructionTools),
            new PrezzieLocation(new Vec3{X = -1667.861f, Y = -441.7934f, Z = 40.35627f}),
            new PrezzieLocation(new Vec3{X = -1756.69f, Y = -400.7248f, Z = 45.29438f}),
            new PrezzieLocation(new Vec3{X = -1777.664f, Y = -427.9722f, Z = 45.30296f}),
            new PrezzieLocation(new Vec3{X = -1769.977f, Y = -696.0801f, Z = 16.3307f}),
            new PrezzieLocation(new Vec3{X = -1930.335f, Y = -551.73f, Z = 23.29564f}, tag: PrezzieLocationTag.Chimney),
            new PrezzieLocation(new Vec3{X = -1947.215f, Y = -536.6019f, Z = 18.87211f}),
            new PrezzieLocation(new Vec3{X = -1953.829f, Y = -533.1736f, Z = 22.39793f}),
            new PrezzieLocation(new Vec3{X = -1977.146f, Y = -525.6816f, Z = 12.19073f}),
            new PrezzieLocation(new Vec3{X = -1894.833f, Y = 250.6315f, Z = 96.93793f}, tag: PrezzieLocationTag.Chimney),
        };
    }
}
