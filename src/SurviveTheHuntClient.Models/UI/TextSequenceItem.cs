using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Models.UI
{
    public struct TextSequenceItem
    {
        public readonly float TimeInSeconds;
        public readonly string Text;

        public TextSequenceItem(float timeInSeconds, string text)
        {
            TimeInSeconds = timeInSeconds;
            Text = text;
        }
    }
}
