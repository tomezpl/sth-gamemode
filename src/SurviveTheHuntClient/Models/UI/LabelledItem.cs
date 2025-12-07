using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Models.UI
{
    internal struct LabelledItem
    {
        internal readonly string Label;
        internal readonly string Value;

        internal LabelledItem(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
