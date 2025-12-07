using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Helpers
{
    internal partial class XmasModifier
    {
        internal partial class Constants
        {
            internal const float DefaultPrezzieRadius = 1.9f;
        }
    }
}

namespace SurviveTheHuntClient.Models.XmasModifier
{
    internal class PrezzieLocation
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
        internal Helpers.XmasModifier.Constants.PrezzieLocationTag Tag;

        internal readonly float VerticalOffset;

        internal PrezzieLocation(CitizenFX.Core.Vector3 pos, float radius = Helpers.XmasModifier.Constants.DefaultPrezzieRadius, Helpers.XmasModifier.Constants.PrezzieLocationTag tag = Helpers.XmasModifier.Constants.PrezzieLocationTag.Default, float verticalOffset = -0.9f)
        {
            _position = pos;
            Radius = radius;
            Tag = tag;
            VerticalOffset = verticalOffset;
        }
    }
}
