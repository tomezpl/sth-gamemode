namespace SurviveTheHuntClient.Models.UI
{
    public struct LabelledItem
    {
        public readonly string Label;
        public readonly string Value;

        public LabelledItem(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
