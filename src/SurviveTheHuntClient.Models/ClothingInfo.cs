using CitizenFX.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SurviveTheHuntClient.Models
{
    public class PedVariation
    {
        public int Drawable;
        public int Texture;

        public static PedVariation Default => new PedVariation { Drawable = 0, Texture = 0 };
    }

    public class PedOutfit
    {
        public Dictionary<PedComponents, PedVariation> ComponentsToApply = new Dictionary<PedComponents, PedVariation>();
        public IDictionary<PedProps, PedVariation> PropsToApply = new Dictionary<PedProps, PedVariation>();

        public static readonly ReadOnlyDictionary<PedProps, PedVariation> RemoveProps = new ReadOnlyDictionary<PedProps, PedVariation>(new Dictionary<PedProps, PedVariation>());

        public class SpecialProps : Dictionary<PedProps, PedVariation>
        {
            public SpecialProps(Dictionary<PedProps, PedVariation> props) : base(props) { }
        }
    }
}
