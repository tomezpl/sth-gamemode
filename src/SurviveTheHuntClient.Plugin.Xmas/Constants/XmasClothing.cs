using CitizenFX.Core;
using SurviveTheHuntClient.Models;

namespace SurviveTheHuntClient.Plugins.Xmas
{
    internal static partial class Constants
    {
        internal static PedOutfit MPFemaleSantaOutfit = new PedOutfit
        {
            ComponentsToApply =
            {
                {PedComponents.Torso, new PedVariation { Drawable = 92, Texture = 0 } },
                {PedComponents.Legs, new PedVariation { Drawable = 78, Texture = 3 } },
                {PedComponents.Shoes, new PedVariation { Drawable = 45, Texture = 3 } },
                {PedComponents.Special2, new PedVariation { Drawable = 231, Texture = 0 } },
                {PedComponents.Torso2, new PedVariation { Drawable = 108, Texture = 1 } }
            },
            PropsToApply =
            {
                {PedProps.Hats, new PedVariation { Drawable = 23, Texture = 0 } },
                {PedProps.Glasses, new PedVariation { Drawable = 35, Texture = 5 } }
            }
        };

        internal static PedOutfit MPMaleSantaOutfit = new PedOutfit
        {
            ComponentsToApply =
            {
                { PedComponents.Torso, new PedVariation { Drawable = 83, Texture = 0 } },
                { PedComponents.Legs, new PedVariation { Drawable = 95, Texture = 5 } },
                { PedComponents.Shoes, new PedVariation { Drawable = 44, Texture = 3 } },
                { PedComponents.Special2, new PedVariation { Drawable = 188, Texture = 0 } },
                { PedComponents.Torso2, new PedVariation { Drawable = 116, Texture = 1 } }
            },
            PropsToApply =
            {
                { PedProps.Hats, new PedVariation { Drawable = 22, Texture = 0 } },
                { PedProps.Glasses, new PedVariation { Drawable = 31, Texture = 5 } }
            }
        };

        internal static PedOutfit MPFemaleElfOutfit = new PedOutfit
        {
            ComponentsToApply =
            {
                {PedComponents.Torso, new PedVariation { Drawable = 3, Texture = 0 } },
                {PedComponents.Legs, new PedVariation { Drawable = 31, Texture = 1 } },
                {PedComponents.Shoes, new PedVariation { Drawable = 17, Texture = 0 } },
                {PedComponents.Torso2, new PedVariation { Drawable = 196, Texture = 1 } }
            },
            PropsToApply =
            {
                {PedProps.Hats, new PedVariation { Drawable = 24, Texture = 0 } }
            }
        };

        internal static PedOutfit MPMaleElfOutfit = new PedOutfit
        {
            ComponentsToApply =
            {
                {PedComponents.Torso, new PedVariation { Drawable = 6, Texture = 0 } },
                {PedComponents.Legs, new PedVariation { Drawable = 32, Texture = 1 } },
                {PedComponents.Shoes, new PedVariation { Drawable = 17, Texture = 0 } },
                {PedComponents.Torso2, new PedVariation { Drawable = 194, Texture = 1 } },
                {PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0 } },
            },
            PropsToApply =
            {
                {PedProps.Hats, new PedVariation { Drawable = 23, Texture = 0 } }
            }
        };
    }
}
