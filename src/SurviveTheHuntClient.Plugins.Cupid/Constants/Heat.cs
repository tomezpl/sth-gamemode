namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal static partial class Constants
    {
        internal enum HeatValues
        {
            Low = 25,

            Medium = 50,
            /// <summary>
            /// Awarded for doing the "extra" objective of a medium stakes job
            /// </summary>
            MediumBonus = 50,

            High = 175
        }

        internal enum HeatThresholds
        {
            Start = 0,
            Heat1 = 100,
            Heat2 = 200,

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
