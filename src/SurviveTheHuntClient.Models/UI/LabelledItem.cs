using System.Text;

namespace SurviveTheHuntClient.Models.UI
{
    public enum LabelledItemType
    {
        Text,
        Progress,
    }

    public class LabelledItem
    {
        public string Label;
        public string Value;
        public LabelledItemType Type;
        public uint Colour;

        public float XOffset = 0f;

        public LabelledItem(string label, string value, uint colour = uint.MaxValue)
        {
            Label = label;
            Value = value;
            Type = LabelledItemType.Text;
            Colour = colour;
        }

        public LabelledItem(string label, float progress, uint colour = uint.MaxValue)
        {
            Label = label;
            Value = SurviveTheHuntShared.Utils.EncodingHelper.Utf16FromNormalFloat(progress);
            Type = LabelledItemType.Progress;
            Colour = colour;
        }
    }
}
