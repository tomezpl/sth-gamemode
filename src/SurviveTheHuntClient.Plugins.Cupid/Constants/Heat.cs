namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal static partial class Constants
    {
        internal enum HeatValues
        {
            Low = 25,

            Medium = 75,
            /// <summary>
            /// Awarded for doing the "extra" objective of a medium stakes job
            /// </summary>
            MediumBonus = 50,

            High = 175,

            /// <summary>
            /// Score needed to win
            /// </summary>
            Target = 300
        }

        internal enum JobType
        {
            LowStakes,
            MediumStakes,
            HighStakes
        }
    }
}
