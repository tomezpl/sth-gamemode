using SurviveTheHuntClient.Models.UI;
using SurviveTheHuntShared.Core;
using System.Collections.Generic;

namespace SurviveTheHuntClient.Helpers
{
    internal partial class XmasModifier
    {
        internal partial class Constants
        {
            internal static partial class UI
            {
                internal static readonly TextSequenceItem BackupSleighTutorial = new TextSequenceItem(6f, "A backup Pole-Rider ~HUD_COLOUR_MENU_GREY~~BLIP_KART_MODERN~ ~w~ is provided for you at spawn.");

                internal static readonly Dictionary<Teams.Team, TextSequenceItem[]> TutorialSequences = new Dictionary<Teams.Team, TextSequenceItem[]>
                {
                    {
                        Teams.Team.Hunted, 
                        new TextSequenceItem[] 
                        {
                            new TextSequenceItem(6.5f, "You are Santa.\nThe elves are displeased with your AI-focused transformation of the business, and have hired Eberhard to shoot your plane down."),
                            new TextSequenceItem(4.5f, "You have crash-landed in Los Santos on Christmas eve."),
                            new TextSequenceItem(5f, "There are still presents ~HUD_COLOUR_RED~~BLIP_COMMUNITY_SERIES~~HUD_COLOUR_GREEN~~BLIP_COMMUNITY_SERIES~~HUD_COLOUR_YELLOW~~BLIP_COMMUNITY_SERIES~~w~ to be delivered, otherwise Christmas is ruined."),
                            new TextSequenceItem(5f, "ClausTech have kindly provided you with a refurbished Pole-Rider ~BLIP_KART_MODERN~ to achieve this task."),
                            new TextSequenceItem(10f, "The Pole-Rider ~BLIP_KART_MODERN~ will have its approximate radius displayed to Elves regularly while you're actively using it.\nYou can approach any vehicle with storage, such as vans, trucks etc. and press ~INPUT_CONTEXT~ to store the Pole-Rider."),
                            new TextSequenceItem(8.5f, "As Santa, nothing can kill you.\nHowever, you have just come off the Whiskey Giftset QA shift, and your motor functions haven't yet fully recovered."),
                            new TextSequenceItem(10f, "Elves will make use of this and attempt to knock you to the ground in order to capture you when they get near.\n\nAvoid falling over, as it leaves you vulnerable to be captured, and you take longer getting up.")
                        }
                    },
                    {
                        Teams.Team.Hunters,
                        new TextSequenceItem[]
                        {
                            new TextSequenceItem(9f, "You are one of Santa's elves.\nFollowing your termination from ClausTech, you joined forces with other elves as well as Eberhard to sabotage Santa's present delivery."),
                            new TextSequenceItem(6.5f, "Santa's flight crashed in the city. They will attempt to deliver 12 presents ~HUD_COLOUR_RED~~BLIP_COMMUNITY_SERIES~~HUD_COLOUR_GREEN~~BLIP_COMMUNITY_SERIES~~HUD_COLOUR_YELLOW~~BLIP_COMMUNITY_SERIES~~w~ over the next 12 hours."),
                            new TextSequenceItem(7f, "The GPS tracker in the ~y~ClausTech Pole-Rider~w~ will regularly ping the sleigh's approximate location. However, the tracker only works when the sleigh is in authorised use."),
                            new TextSequenceItem(6.5f, "In the event of GPS tracker failure, the Naughty List kids will still regularly broadcast Santa's current area for social media attention."),
                            new TextSequenceItem(7f, "Nothing can kill Santa.\nHowever, they have just come off the Whiskey Giftset QA shift, and their motor functions haven't yet fully recovered."),
                            new TextSequenceItem(6.5f, "Elves will need to knock Santa to the ground, then press ~INPUT_CONTEXT~ to capture them."),
                            new TextSequenceItem(8f, "Ammunition is scarce, but you will be able to refill ammo from delivered presents ~HUD_COLOUR_MENU_GREY~~BLIP_COMMUNITY_SERIES~~w~.")
                        }
                    }
                };
            }
        }
    }
}
