using CitizenFX.Core;
using SurviveTheHuntClient.Models;
using System.Collections.Generic;
using PlayerType = SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType;

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
                                { PedComponents.Special1, new PedVariation { Drawable = 0, Texture = 0 } },
                                { PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0 } },
                                { PedComponents.Torso2, new PedVariation { Drawable = 15, Texture = 0 } },
                            },
                            PropsToApply = new Dictionary<PedProps, PedVariation>
                            {
                                { PedProps.Hats, new PedVariation { Drawable = 110, Texture = 3 } },
                                { PedProps.Glasses, new PedVariation { Drawable = 4, Texture = 6 } },
                                { PedProps.EarPieces, new PedVariation { Drawable = 33, Texture = 0 } },
                                { PedProps.Watches, new PedVariation { Drawable = 2, Texture = 0 } }
                            }
                        },
                        Female = new PedOutfit
                        {
                            ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                            {
                                {PedComponents.Torso, new PedVariation { Drawable = 15, Texture = 0} },
                                {PedComponents.Legs, new PedVariation{ Drawable = 91, Texture = 16} },
                                {PedComponents.Hands, new PedVariation { Drawable = 0, Texture = 0 } },
                                {PedComponents.Shoes, new PedVariation { Drawable = 118, Texture = 16 } },
                                {PedComponents.Torso2, new PedVariation { Drawable = 168, Texture = 3} },
                                {PedComponents.Special2, new PedVariation { Drawable = 3 , Texture = 0 } }
                            },
                            PropsToApply = new Dictionary<PedProps, PedVariation>
                            {
                                {PedProps.Hats, new PedVariation { Drawable = 109, Texture = 3 } },
                                {PedProps.Glasses, new PedVariation { Drawable = 16, Texture = 1 } },
                                {PedProps.EarPieces, new PedVariation{ Drawable = 33, Texture = 0} },
                                {PedProps.Watches, new PedVariation { Drawable = 2, Texture = 0} }
                            }
                        }
                    }
                }
            };

            private static SceneOutfits CopOutfits = new SceneOutfits()
            {
                {
                    DirectedScene.JasonDrivingHood,
                    new OutfitPair
                    {
                        Female = new PedOutfit
                        {
                            ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                            {
                                {PedComponents.Torso2, new PedVariation { Drawable = 48, Texture = 0 } },
                                {PedComponents.Special2, new PedVariation { Drawable = 2, Texture = 0 } },
                                {PedComponents.Shoes, new PedVariation {Drawable = 25, Texture = 0} },
                                {PedComponents.Legs, new PedVariation { Drawable = 34, Texture = 0} },
                                {PedComponents.Torso, new PedVariation { Drawable = 14, Texture = 0 } },
                            },
                            PropsToApply = new Dictionary<PedProps, PedVariation>
                            {
                                {PedProps.Hats, new PedVariation { Drawable = 45, Texture = 0} },
                                {PedProps.Glasses, new PedVariation { Drawable = 11, Texture = 3} },
                            }
                        },
                        Male = new PedOutfit
                        {
                            ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                            {
                                {PedComponents.Torso2, new PedVariation { Drawable = 55, Texture = 0 } },
                                {PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0 } },
                                {PedComponents.Shoes, new PedVariation {Drawable = 25, Texture = 0} },
                                {PedComponents.Legs, new PedVariation { Drawable = 35, Texture = 0} },
                                {PedComponents.Torso, new PedVariation { Drawable = 0, Texture = 0 } },
                            },
                            PropsToApply = new Dictionary<PedProps, PedVariation>
                            {
                                {PedProps.Hats, new PedVariation { Drawable = 46, Texture = 0} },
                                {PedProps.Glasses, new PedVariation { Drawable = 5, Texture = 5} },
                            }
                        }
                    }
                }
            };

            internal static Dictionary<PlayerType, SceneOutfits> Outfits = new Dictionary<PlayerType, SceneOutfits>()
            {
                { PlayerType.HuntedJ, JasonOutfits },
                { PlayerType.Cop, CopOutfits },
            };
        }
    }
}
