using CitizenFX.Core;
using SurviveTheHuntClient.Models;
using System.Collections.Generic;
using static SurviveTheHuntShared.Plugins.Cupid.Constants;

namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal static partial class Constants
    {
        internal static class Clothing
        {
            internal struct OutfitPair
            {
                internal PedOutfit Male;
                internal PedOutfit Female;

                internal PedOutfit GetOutfit(bool isFemale)
                {
                    return isFemale ? Female : Male;
                }
            }

            internal class SceneOutfits : Dictionary<DirectedScene, OutfitPair> { }

            private static SceneOutfits JasonOutfits = new SceneOutfits()
            {
                {
                    DirectedScene.IntroJason,
                    new OutfitPair
                    {
                        Male = new PedOutfit
                        {
                            ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                            {
                                { PedComponents.Torso, new PedVariation { Drawable = 15, Texture = 0 } },
                                { PedComponents.Legs, new PedVariation { Drawable = 88, Texture = 16 } },
                                //{ PedComponents.Hands, new PedVariation { Drawable = 0, Texture = 0 } },
                                { PedComponents.Shoes, new PedVariation { Drawable = 12, Texture = 11 } },
                                //{ PedComponents.Special1, new PedVariation { Drawable = 0, Texture = 0 } },
                                { PedComponents.Torso2, new PedVariation { Drawable = 15, Texture = 0 } },
                            }
                        },
                        Female = new PedOutfit
                        {
                            // TODO
                        }
                    }
                }
            };

            internal static Dictionary<PlayerType, SceneOutfits> Outfits = new Dictionary<PlayerType, SceneOutfits>()
            {
                { PlayerType.HuntedJ, JasonOutfits }
            };
        }
    }
}
