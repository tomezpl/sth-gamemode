using System;
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

        public byte ProgressBarDividers = 0;

        public float XOffset = 0f;

        private static LabelledItem[] _empty = new LabelledItem[0];

        public static LabelledItem[] Empty => _empty;

        public LabelledItem(string label, string value, uint colour = uint.MaxValue)
        {
            Label = label;
            Value = value;
            Type = LabelledItemType.Text;
            Colour = colour;
        }

        public LabelledItem(string label, float progress, uint colour = uint.MaxValue, byte progressBarDividers = 0)
        {
            Label = label;
            Value = SurviveTheHuntShared.Utils.EncodingHelper.Utf16FromNormalFloat(progress);
            Type = LabelledItemType.Progress;
            Colour = colour;
            ProgressBarDividers = progressBarDividers;
        }
    }
}
