using CitizenFX.Core;
using System.Collections.Generic;

namespace SurviveTheHuntClient.Models
{
    public class PedVariation
    {
        public int Drawable;
        public int Texture;
    }

    public class PedOutfit
    {
        public Dictionary<PedComponents, PedVariation> ComponentsToApply = new Dictionary<PedComponents, PedVariation>();
        public Dictionary<PedProps, PedVariation> PropsToApply = new Dictionary<PedProps, PedVariation>();
    }
}
