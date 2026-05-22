namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    internal sealed class CruisegoerSpawnSingle : CruisegoerSpawnBase
    {
        internal override SpawnShrinkStrategy ShrinkStrategy => SpawnShrinkStrategy.Sliced;

        internal readonly uint PedModel;

        private readonly PedNode[] _cached;

        internal CruisegoerSpawnSingle(float x, float y, float z, float heading, uint pedNodeFlags, params uint[] pedModels) : base(x, y, z, heading)
        {
            PedModel = pedModels[s_RNG.Next(0, pedModels.Length)];
            _cached = new PedNode[]{
                new PedNode
                {
                    Position = new PedNode.PositionInfo(x, y, z, heading),
                    Anim = new PedNode.AnimInfo("", ""),
                    PedModel = PedModel,
                    Flags = pedNodeFlags,
                }
            };
        }

        internal override PedNode[] Build()
        {
            return _cached;
        }
    }
}
