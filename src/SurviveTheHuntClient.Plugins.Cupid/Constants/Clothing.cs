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

            private static PedOutfit JasMaleBarechest => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                            {
                                { PedComponents.Torso, new PedVariation { Drawable = 15, Texture = 0 } },
                                { PedComponents.Legs, new PedVariation { Drawable = 88, Texture = 16 } },
                                //{ PedComponents.Hands, new PedVariation { Drawable = 0, Texture = 0 } },
                                { PedComponents.Shoes, new PedVariation { Drawable = 12, Texture = 11 } },
                                { PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0 } },
                                {PedComponents.Special1, PedVariation.Default },
                                {PedComponents.Special3, PedVariation.Default },
                                {PedComponents.Textures, PedVariation.Default },
                                { PedComponents.Torso2, new PedVariation { Drawable = 15, Texture = 0 } },
                            },
                PropsToApply = new PedOutfit.SpecialProps(new Dictionary<PedProps, PedVariation>
                            {
                                { PedProps.Hats, new PedVariation { Drawable = 110, Texture = 3 } },
                                { PedProps.Glasses, new PedVariation { Drawable = 4, Texture = 6 } },
                                { PedProps.EarPieces, new PedVariation { Drawable = 33, Texture = 0 } },
                                { PedProps.Watches, new PedVariation { Drawable = 2, Texture = 0 } }
                            })
            };

            private static PedOutfit JasFemaleCargoShortsCreamTank => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                            {
                                {PedComponents.Torso, new PedVariation { Drawable = 15, Texture = 0} },
                                {PedComponents.Legs, new PedVariation{ Drawable = 91, Texture = 16} },
                                {PedComponents.Hands, new PedVariation { Drawable = 0, Texture = 0 } },
                                {PedComponents.Shoes, new PedVariation { Drawable = 118, Texture = 16 } },
                                {PedComponents.Torso2, new PedVariation { Drawable = 168, Texture = 3} },
                                {PedComponents.Special2, new PedVariation { Drawable = 3 , Texture = 0 } },
                                {PedComponents.Special1, PedVariation.Default },
                                {PedComponents.Special3, PedVariation.Default },
                                {PedComponents.Textures, PedVariation.Default },
                            },
                PropsToApply = new PedOutfit.SpecialProps(new Dictionary<PedProps, PedVariation>
                            {
                                {PedProps.Hats, new PedVariation { Drawable = 109, Texture = 3 } },
                                {PedProps.Glasses, new PedVariation { Drawable = 16, Texture = 1 } },
                                {PedProps.EarPieces, new PedVariation{ Drawable = 33, Texture = 0} },
                                {PedProps.Watches, new PedVariation { Drawable = 2, Texture = 0} }
                            })
            };

            private static PedOutfit JasMaleCargoShortsCreamTank => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation { Drawable = 5, Texture = 0 } },
                    {PedComponents.Torso2, new PedVariation { Drawable = 237, Texture = 1 } },
                    {PedComponents.Legs, new PedVariation { Drawable = 88, Texture = 16 } },
                    {PedComponents.Shoes, new PedVariation { Drawable = 75, Texture = 23 } },
                    {PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0 } },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                },
                PropsToApply = new PedOutfit.SpecialProps(new Dictionary<PedProps, PedVariation>
                {
                    {PedProps.Hats, new PedVariation { Drawable = 77, Texture = 6 }},
                })/* DEFAULT (beige tanktop, shorts, cap)
outfit: {
  "0": [
    0,
    7
  ],
  "1": [
    0,
    0
  ],
  "3": [
    5,
    0
  ],
  "4": [
    88,
    16
  ],
  "6": [
    75,
    23
  ],
  "7": [
    0,
    0
  ],
  "8": [
    15,
    0
  ],
  "9": [
    0,
    0
  ],
  "10": [
    0,
    0
  ],
  "11": [
    237,
    1
  ]
}
props: {
  "0": [
    77,
    6
  ],
  "1": [
    5,
    0
  ],
  "2": [
    7,
    0
  ],
  "3": [
    0,
    0
  ],
  "4": [
    0,
    -1
  ],
  "5": [
    0,
    -1
  ],
  "6": [
    0,
    0
  ],
  "7": [
    0,
    0
  ]
}
*/
            };

            private static PedOutfit JasMaleBlackTankJeans => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    { PedComponents.Torso, new PedVariation { Drawable = 3, Texture = 0} },
                    { PedComponents.Legs, new PedVariation { Drawable = 0, Texture = 1} },
                    {PedComponents.Shoes, new PedVariation { Drawable = 48, Texture = 0} },
                    {PedComponents.Torso2, new PedVariation { Drawable  = 5, Texture = 2} },
                    {PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0 } },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                },
                PropsToApply = PedOutfit.RemoveProps,

                /* DEFAULT 2 (black tanktop, jeans, sneakers)
outfit: {
  "1": [
    0,
    0
  ],
  "3": [
    5,
    0
  ],
  "4": [
    0,
    1
  ],
  "6": [
    48,
    0
  ],
  "7": [
    0,
    0
  ],
  "8": [
    15,
    0
  ],
  "9": [
    0,
    0
  ],
  "10": [
    0,
    0
  ],
  "11": [
    5,
    2
  ]
}
props: {
  "0": [
    8,
    0
  ],
  "1": [
    5,
    0
  ],
  "2": [
    7,
    0
  ],
  "3": [
    0,
    0
  ],
  "4": [
    0,
    -1
  ],
  "5": [
    0,
    -1
  ],
  "6": [
    0,
    0
  ],
  "7": [
    0,
    0
  ]
}
*/
            };

            private static PedOutfit JasMalePartyShirtJeans => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    { PedComponents.Legs, new PedVariation { Drawable = 0, Texture = 1 } },
                    {PedComponents.Torso, new PedVariation { Drawable = 0, Texture = 0 } },
                    {PedComponents.Shoes, new PedVariation { Drawable = 32, Texture = 1} },
                    {PedComponents.Torso2, new PedVariation { Drawable = 346, Texture = 0} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, PedVariation.Default },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                },
                PropsToApply = PedOutfit.RemoveProps,
                /*
 MALE PARTY (dark shirt, jeans, sneakers)
outfit: {
"1": [
0,
0
],
"3": [
0,
0
],
"4": [
0,
1
],
"6": [
32,
1
],
"7": [
0,
0
],
"8": [
0,
0
],
"9": [
0,
0
],
"10": [
0,
0
],
"11": [
346,
0
]
}
props: {
"0": [
-1,
-1
],
"1": [
5,
0
],
"2": [
7,
0
],
"4": [
0,
-1
],
"6": [
2,
0
],
"7": [
0,
0
]
}
*/
            };

            private static PedOutfit JasFemalePartyShirtJeans => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation{Drawable = 0, Texture = 0} },
                    {PedComponents.Legs, new PedVariation{Drawable = 1, Texture = 6} },
                    {PedComponents.Shoes, new PedVariation {Drawable = 33, Texture = 1} },
                    {PedComponents.Torso2, new PedVariation { Drawable = 364, Texture = 0} }
                },
                PropsToApply = PedOutfit.RemoveProps,
                /* FEMALE PARTY (same as male)
outfit: {
"1": [
0,
0
],
"3": [
0,
0
],
"4": [
1,
6
],
"6": [
33,
1
],
"7": [
0,
0
],
"8": [
0,
0
],
"9": [
0,
0
],
"10": [
0,
0
],
"11": [
364,
0
]
}
props: {
"0": [
-1,
-1
],
"1": [
5,
0
],
"2": [
7,
0
],
"4": [
0,
-1
],
"6": [
2,
0
],
"7": [
0,
0
]
}
*/
            };

            private static PedOutfit JasFemaleDarkTeeJeans => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation { Drawable = 14, Texture = 0} },
                    {PedComponents.Legs, new PedVariation { Drawable = 1, Texture = 4} },
                    {PedComponents.Shoes, new PedVariation { Drawable = 97, Texture = 12} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, new PedVariation { Drawable = 2, Texture = 0} },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                    {PedComponents.Torso2, new PedVariation { Drawable = 349, Texture = 13} },
                },
                PropsToApply = PedOutfit.RemoveProps,
            };

            private static SceneOutfits JasonOutfits = new SceneOutfits()
            {
                {
                    DirectedScene.IntroJason,
                    new OutfitPair
                    {
                        Male = JasMaleBarechest,
                        Female = JasFemaleCargoShortsCreamTank,
                    }
                },

                {
                    DirectedScene.JasonDrivingHood,
                    new OutfitPair
                    {
                        Male = JasMaleCargoShortsCreamTank,
                        Female = JasFemaleCargoShortsCreamTank,
                    }
                },

                {
                    DirectedScene.Bed,
                    new OutfitPair
                    {
                        Male = JasMaleBarechest,
                        Female = JasFemaleCargoShortsCreamTank,
                    }
                },

                {
                    DirectedScene.Default1,
                    new OutfitPair
                    {
                        Male = JasMaleCargoShortsCreamTank,
                        Female = JasFemaleCargoShortsCreamTank,
                    }
                },

                {
                    DirectedScene.Default2,
                    new OutfitPair
                    {
                        Male = JasMaleBlackTankJeans,
                        Female = JasFemaleDarkTeeJeans,
                    }
                },

                {
                    DirectedScene.Party,
                    new OutfitPair
                    {
                        Male = JasMalePartyShirtJeans,
                        Female = JasFemalePartyShirtJeans,
                    }
                }
            };

            private static PedOutfit LuFemalePrison => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation { Drawable = 14, Texture = 0} },
                    {PedComponents.Torso2, new PedVariation { Drawable = 126, Texture = 0} },
                    {PedComponents.Legs, new PedVariation { Drawable = 80, Texture = 5} },
                    {PedComponents.Shoes, new PedVariation { Drawable = 16, Texture = 0} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, new PedVariation {Drawable = 2, Texture = 0} },
                    {PedComponents.Special3, PedVariation.Default},
                }
                /* PRISON PICK UP (shirt, trousers, fip flops) :
 * outfit: {
"1": [
0,
0
],
"3": [
14,
0
],
"4": [
80,
5
],
"6": [
16,
0
],
"7": [
0,
0
],
"8": [
2,
0
],
"9": [
0,
0
],
"10": [
0,
0
],
"11": [
126,
0
]
}
props: {
"0": [
-1,
-1
],
"1": [
11,
4
],
"6": [
2,
0
],
"7": [
0,
0
]
}
 */
            };

            private static PedOutfit LuFemaleBed => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation{ Drawable = 15, Texture = 0 } },
                    {PedComponents.Legs, new PedVariation{Drawable= 16, Texture = 1} },
                    {PedComponents.Shoes, new PedVariation {Drawable = 35, Texture = 0} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, new PedVariation { Drawable = 60, Texture = 0} },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                    {PedComponents.Torso2, new PedVariation { Drawable = 74, Texture = 0} },
                },
                PropsToApply = PedOutfit.RemoveProps
                /* BED (shorts and tank top) :
outfit: {
"1": [
0,
0
],
"3": [
15,
0
],
"4": [
16,
1
],
"6": [
35,
0
],
"7": [
0,
0
],
"8": [
60,
0
],
"9": [
0,
0
],
"10": [
0,
0
],
"11": [
74,
0
]
}
props: {
"0": [
-1,
-1
],
"1": [
11,
4
],
"6": [
2,
0
],
"7": [
0,
0
]
}
 */
            };

            private static PedOutfit LuMalePrison => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, PedVariation.Default },
                    {PedComponents.Legs, new PedVariation {Drawable = 78, Texture = 5} },
                    {PedComponents.Shoes, new PedVariation { Drawable = 107, Texture = 8} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0} },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                    {PedComponents.Torso2, new PedVariation { Drawable = 81, Texture = 0} }
                },
                PropsToApply = PedOutfit.RemoveProps,
                /*
  PRISON PICKUP MALE (same as female)
outfit: {
  "1": [
    0,
    0
  ],
  "3": [
    0,
    0
  ],
  "4": [
    78,
    5
  ],
  "6": [
    107,
    8
  ],
  "7": [
    0,
    0
  ],
  "8": [
    15,
    0
  ],
  "9": [
    0,
    0
  ],
  "10": [
    0,
    0
  ],
  "11": [
    81,
    0
  ]
}
props: {
  "0": [
    8,
    0
  ],
  "1": [
    5,
    0
  ],
  "2": [
    7,
    0
  ],
  "3": [
    0,
    0
  ],
  "4": [
    0,
    -1
  ],
  "5": [
    0,
    -1
  ],
  "6": [
    0,
    0
  ],
  "7": [
    0,
    0
  ]
}
*/
            };

            private static PedOutfit LuMaleBed => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation{Drawable = 15, Texture = 0} },
                    {PedComponents.Legs, new PedVariation {Drawable = 14, Texture = 0} },
                    {PedComponents.Torso2, new PedVariation{Drawable = 15, Texture = 0} },
                    {PedComponents.Special2, new PedVariation{Drawable = 15, Texture = 0} },
                    {PedComponents.Textures, PedVariation.Default },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Shoes, new PedVariation {Drawable = 34, Texture = 0} },
                },
                PropsToApply = PedOutfit.RemoveProps,
            };

            private static PedOutfit LuFemaleDefault => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation {Drawable = 161, Texture = 0 } },
                    {PedComponents.Legs, new PedVariation {Drawable = 80, Texture = 2} },
                    {PedComponents.Shoes, new PedVariation {Drawable = 118, Texture = 12} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, new PedVariation{Drawable = 3, Texture = 0} },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                    {PedComponents.Torso2, new PedVariation { Drawable = 168, Texture = 3} },
                },
                PropsToApply = PedOutfit.RemoveProps,

                /*
                DEFAULT:
outfit: {
  "1": [
    0,
    0
  ],
  "3": [
    161,
    0
  ],
  "4": [
    80,
    2
  ],
  "6": [
    118,
    12
  ],
  "7": [
    0,
    0
  ],
  "8": [
    3,
    0
  ],
  "9": [
    0,
    0
  ],
  "10": [
    0,
    0
  ],
  "11": [
    168,
    3
  ]
}
props: {
  "0": [
    -1,
    -1
  ],
  "1": [
    11,
    4
  ],
  "6": [
    2,
    0
  ],
  "7": [
    0,
    0
  ]
}
*/
            };

            private static PedOutfit LuFemaleDress => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation {Drawable = 11, Texture = 0} },
                    {PedComponents.Legs, new PedVariation {Drawable = 21, Texture = 0} },
                    {PedComponents.Shoes, new PedVariation {Drawable = 14, Texture = 0} },
                    {PedComponents.Special1, new PedVariation { Drawable = 0 , Texture = 0} },
                    {PedComponents.Special2, new PedVariation { Drawable = 3, Texture = 0} },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                    {PedComponents.Torso2, new PedVariation {Drawable = 437, Texture = 11} }
                },

                PropsToApply = new PedOutfit.SpecialProps(new Dictionary<PedProps, PedVariation>
                {
                    // earrings
                    {PedProps.EarPieces, new PedVariation { Drawable = 7, Texture = 0} }
                })
                /*
            PARTY (dress, high heels, earrings)
outfit: {
  "1": [
    0,
    0
  ],
  "3": [
    11,
    0
  ],
  "4": [
    21,
    0
  ],
  "6": [
    14,
    0
  ],
  "7": [
    0,
    0
  ],
  "8": [
    3,
    0
  ],
  "9": [
    0,
    0
  ],
  "10": [
    0,
    0
  ],
  "11": [
    437,
    12
  ]
}
props: {
  "0": [
    -1,
    -1
  ],
  "1": [
    5,
    0
  ],
  "2": [
    7,
    0
  ],
  "4": [
    0,
    -1
  ],
  "6": [
    2,
    0
  ],
  "7": [
    0,
    0
  ]
}
*/
            };

            private static PedOutfit LuFemalePinkCrop => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation { Drawable = 15 , Texture = 0} },
                    {PedComponents.Legs, new PedVariation { Drawable = 87, Texture = 7} },
                    {PedComponents.Shoes, new PedVariation { Drawable = 3, Texture = 3} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, new PedVariation { Drawable = 3, Texture = 0} },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                    {PedComponents.Torso2, new PedVariation { Drawable = 195, Texture = 1} }
                },
                PropsToApply = PedOutfit.RemoveProps
                /*
            DEFAULT 2 (pink crop top, blue-cyan leggings, converse)
outfit: {
  "1": [
    0,
    0
  ],
  "3": [
    15,
    0
  ],
  "4": [
    87,
    7
  ],
  "6": [
    3,
    3
  ],
  "7": [
    0,
    0
  ],
  "8": [
    3,
    0
  ],
  "9": [
    0,
    0
  ],
  "10": [
    0,
    0
  ],
  "11": [
    195,
    1
  ]
}
props: {
  "0": [
    -1,
    -1
  ],
  "1": [
    5,
    0
  ],
  "2": [
    7,
    0
  ],
  "4": [
    0,
    -1
  ],
  "6": [
    2,
    0
  ],
  "7": [
    0,
    0
  ]
}
*/
            };

            private static PedOutfit LuFemaleFloralShirtShorts => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    { PedComponents.Legs, new PedVariation { Drawable = 25, Texture = 2 } },
                    {PedComponents.Torso, new PedVariation { Drawable = 15, Texture = 0 } },
                    {PedComponents.Shoes, new PedVariation { Drawable = 119, Texture = 14} },
                    {PedComponents.Torso2, new PedVariation { Drawable = 365, Texture = 0} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, new PedVariation { Drawable = 23, Texture = 9 } },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                },
                PropsToApply = PedOutfit.RemoveProps
            };

            private static PedOutfit LuMaleWhiteShirtJeans => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation { Drawable = 198, Texture = 0} },
                    {PedComponents.Legs, new PedVariation{ Drawable = 4, Texture = 1} },
                    {PedComponents.Shoes, new PedVariation {Drawable = 14, Texture = 15} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special2, new PedVariation { Drawable = 5, Texture = 2} },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                    {PedComponents.Torso2, new PedVariation { Drawable = 346, Texture = 3} },
                },
                PropsToApply = PedOutfit.RemoveProps
            };

            private static PedOutfit LuMalePinkTankBlueJeans => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation { Drawable = 5, Texture = 0} },
                    {PedComponents.Legs, new PedVariation { Drawable = 26, Texture = 4} },
                    {PedComponents.Shoes, new PedVariation { Drawable = 12, Texture = 7} },
                    {PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0} },
                    {PedComponents.Torso2, new PedVariation { Drawable = 237, Texture = 9} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                },
                PropsToApply = PedOutfit.RemoveProps,
            };

            private static PedOutfit LuMaleWhiteTankShorts => new PedOutfit
            {
                ComponentsToApply = new Dictionary<PedComponents, PedVariation>
                {
                    {PedComponents.Torso, new PedVariation { Drawable = 5, Texture = 0} },
                    {PedComponents.Legs, new PedVariation { Drawable = 42, Texture = 4} },
                    {PedComponents.Shoes, new PedVariation { Drawable = 12, Texture = 7} },
                    {PedComponents.Special2, new PedVariation { Drawable = 15, Texture = 0} },
                    {PedComponents.Torso2, new PedVariation { Drawable = 5, Texture = 9} },
                    {PedComponents.Special1, PedVariation.Default },
                    {PedComponents.Special3, PedVariation.Default },
                    {PedComponents.Textures, PedVariation.Default },
                },
                PropsToApply = PedOutfit.RemoveProps,
            };

            private static SceneOutfits LuOutfits = new SceneOutfits()
            {
                {
                    DirectedScene.IntroJason,
                    new OutfitPair
                    {
                        Female = LuFemalePrison,
                        Male = LuMalePrison,
                    }
                },
                {
                    DirectedScene.JasonDrivingHood,
                    new OutfitPair
                    {
                        Female = LuFemalePrison,
                        Male = LuMalePrison,
                    }
                },
                {
                    DirectedScene.Bed,
                    new OutfitPair
                    {
                        Female = LuFemaleBed,
                        Male = LuMaleBed
                    }
                },

                {
                    DirectedScene.Default1,
                    new OutfitPair
                    {
                        Female = LuFemaleDefault,
                        Male = LuMaleWhiteTankShorts,
                    }
                },

                {
                    DirectedScene.Default2,
                    new OutfitPair
                    {
                        Female = LuFemalePinkCrop,
                        Male = LuMalePinkTankBlueJeans,
                    }
                },

                {
                    DirectedScene.Party,
                    new OutfitPair
                    {
                        Female = LuFemaleDress,
                        Male = LuMaleWhiteShirtJeans,
                    }
                }

            };

            private static SceneOutfits CopOutfits = new SceneOutfits()
            {
                {
                    DirectedScene.IntroJason,
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
                            PropsToApply = new PedOutfit.SpecialProps(new Dictionary<PedProps, PedVariation>
                            {
                                {PedProps.Hats, new PedVariation { Drawable = 45, Texture = 0} },
                                {PedProps.Glasses, new PedVariation { Drawable = 11, Texture = 3} },
                            })
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
                            PropsToApply = new PedOutfit.SpecialProps(new Dictionary<PedProps, PedVariation>
                            {
                                {PedProps.Hats, new PedVariation { Drawable = 46, Texture = 0} },
                                {PedProps.Glasses, new PedVariation { Drawable = 5, Texture = 5} },
                            })
                        }
                    }
                }
            };

            internal static Dictionary<PlayerType, SceneOutfits> Outfits = new Dictionary<PlayerType, SceneOutfits>()
            {
                { PlayerType.HuntedJ, JasonOutfits },
                { PlayerType.HuntedL, LuOutfits },
                { PlayerType.Cop, CopOutfits },
            };
        }
    }
}
