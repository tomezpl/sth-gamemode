using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Models.UI
{
    internal struct TextSequenceItem
    {
        internal readonly float TimeInSeconds;
        internal readonly string Text;

        internal TextSequenceItem(float timeInSeconds, string text)
        {
            TimeInSeconds = timeInSeconds;
            Text = text;
        }
    }
}
