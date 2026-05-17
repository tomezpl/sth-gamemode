namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal sealed class CruisegoerSpawnSingle : CruisegoerSpawnBase
    {
        internal override SpawnShrinkStrategy ShrinkStrategy => SpawnShrinkStrategy.Sliced;

        internal readonly uint PedModel;

        private readonly PedSpawnInfo[] _cached;

        internal CruisegoerSpawnSingle(float x, float y, float z, float heading, bool needsWarp, params uint[] pedModels) : base(x, y, z, heading)
        {
            PedModel = pedModels[s_RNG.Next(0, pedModels.Length)];
            _cached = new PedSpawnInfo[]{
                new PedSpawnInfo
                {
                    Position = new PedSpawnInfo.PositionInfo(x, y, z, heading),
                    Anim = new PedSpawnInfo.AnimInfo("", ""),
                    PedModel = PedModel,
                    NeedsWarp = needsWarp,
                }
            };
        }

        internal override PedSpawnInfo[] Build()
        {
            return _cached;
        }
    }
}
