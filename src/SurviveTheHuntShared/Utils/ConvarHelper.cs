namespace SurviveTheHuntShared.Utils
{
    public static class ConvarHelper
    {
        public static bool GetBoolean(string convarValue)
        {
            string lowerCase = convarValue.ToLower().Trim();

            return lowerCase == "true" || lowerCase == "\"true\"";
        }
    }
}
