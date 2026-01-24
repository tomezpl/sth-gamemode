namespace SurviveTheHuntClient.Plugins.Xmas.Models
{
    public class PrezzieLocation
    {
        private CitizenFX.Core.Vector3 _position;
        internal CitizenFX.Core.Vector3 Position => new CitizenFX.Core.Vector3(_position.X, _position.Y, _position.Z + VerticalOffset);
        private float _radius;
        internal float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                _radiusSq = value * value;
            }
        }
        private float _radiusSq;
        internal float RadiusSq
        {
            get => _radiusSq;
        }
        internal Constants.PrezzieLocationTag Tag;

        internal readonly float VerticalOffset;

        internal PrezzieLocation(CitizenFX.Core.Vector3 pos, float radius = Constants.DefaultPrezzieRadius, Constants.PrezzieLocationTag tag = Constants.PrezzieLocationTag.Default, float verticalOffset = -0.9f)
        {
            _position = pos;
            Radius = radius;
            Tag = tag;
            VerticalOffset = verticalOffset;
        }
    }
}
