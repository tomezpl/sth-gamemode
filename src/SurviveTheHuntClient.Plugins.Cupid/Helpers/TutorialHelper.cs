using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntShared.Core;
using static CitizenFX.Core.Native.API;
using System;

namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    internal class TutorialHelper : ITickable
    {
        internal static readonly TutorialHelper Instance = new TutorialHelper();

        private static readonly bool s_HasInit = Init();

        private struct TutorialText
        {
            internal readonly string Text;
            internal readonly float Duration;

            internal TutorialText(string text, float durationSeconds)
            {
                Text = text;
                Duration = durationSeconds;
            }
        }

        private static string GetKeyForText(Teams.Team audience, int index)
        {
            if(audience == Teams.Team.Hunted)
            {
                return $"STH_CUPID_TUT_HUNTD{index}";
            }

            return $"STH_CUPID_TUT_HUNTR{index}";
        }

        private static TutorialText s_Welcome => new TutorialText("Welcome to Survive the Heat.", 9f);

        private static TutorialText[] s_HunterTutorial => new TutorialText[]
        {
            s_Welcome,
            new TutorialText("LSSD and LSPD are carrying out a joint operation to hunt down a recently released criminal. We have received a tip-off warning of their next potential robbery targets.", 7.5f),
            new TutorialText("You can administer aid to incapacitated officers by approaching them and holding ~INPUT_CONTEXT~ within 10 seconds of them going down.", 7.5f),
            new TutorialText("The criminal's position is constantly triangulated through ~y~cell tower signals ~BLIP_RADIUS_BLIP~~w~~s~. The radius will close in quicker the faster the criminal is currently moving.", 12f),
            new TutorialText("Your service vehicle ~BLIP_GANG_VEHICLE~ stores a disguise you can choose to wear. Approach the trunk of the vehicle to equip it. You can always switch back to your service gear.", 10f),
            new TutorialText("Due to LSSD and LSPD cooperation on this matter, new recruits can choose which station to deploy at.", 7f),
        };

        private static TutorialText[] s_HuntedTutorial => new TutorialText[]
        {
            s_Welcome,
            new TutorialText("Finally reunited, you and your partner decide to head out on a crime spree to show San Andreas you're back in business.", 5f),
            new TutorialText("Hit up stores ~BLIP_CRIM_HOLDUPS~, banks ~BLIP_FINANCIER_STRAND~, and ~HUD_COLOUR_GREEN~car transporters ~BLIP_EXPORT_VEHICLE~~s~ to build up Heat and attract more serious jobs.", 10f),
            new TutorialText("Bolingbroke Penitentiary release conditions mandate continued use of an electronic tag. The police can use its signal to narrow down the wearer's location. The signal gets stronger the faster the wearer is moving.", 12.5f),
            new TutorialText("Should one of you fall during a fight, you can revive your partner by approaching them and holding ~INPUT_CONTEXT~ within 30 seconds of being incapacitated.", 12f),
            new TutorialText("You need to fill up your Heat bar before the time is up. Should you fail, you'll be tracked down by cops and jailed - that is, if you live long enough.", 15f),
        };

        private int _currentIndex = 0;
        private Teams.Team _currentAudience = Teams.Team.Hunters;

        private bool _isStarted = false;

        private static bool Init()
        {
            if(!s_HasInit)
            {
                for(int i = 0; i < Math.Max(s_HuntedTutorial.Length, s_HunterTutorial.Length); i++)
                {
                    if(s_HunterTutorial.Length > i)
                    {
                        AddTextEntry(GetKeyForText(Teams.Team.Hunters, i), s_HunterTutorial[i].Text);
                    }

                    if(s_HuntedTutorial.Length > i)
                    {
                        AddTextEntry(GetKeyForText(Teams.Team.Hunted, i), s_HuntedTutorial[i].Text);
                    }
                }
            }

            return true;
        }

        private float _timeSinceLastText = 0f;
        private void ShowText(int index)
        {
            if(index < _targetTexts.Length)
            {
                BeginTextCommandDisplayHelp(GetKeyForText(_currentAudience, index));
                EndTextCommandDisplayHelp(0, false, true, (int)Math.Round(_targetTexts[index].Duration * 1000f));
                _timeSinceLastText = 0f;
            }
        }

        private TutorialHelper()
        {

        }

        private TutorialText[] _targetTexts = s_HunterTutorial;

        internal void Start(Teams.Team audienceTeam)
        {
            _currentAudience = audienceTeam;
            _currentIndex = 0;
            _timeSinceLastText = 0f;
            _isStarted = true;
            _targetTexts = audienceTeam == Teams.Team.Hunters ? s_HunterTutorial : s_HuntedTutorial;
        }

        public void Tick(float deltaTime)
        {
            if (_isStarted)
            {
                _timeSinceLastText += deltaTime;

                if(_timeSinceLastText >= _targetTexts[_currentIndex].Duration)
                {
                    ShowText(_currentIndex);

                    if (_currentIndex < _targetTexts.Length - 1)
                    {
                        _currentIndex++;
                    }
                    else
                    {
                        _isStarted = false;
                    }
                }
            }
        }
    }
}
