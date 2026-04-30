namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal partial class Constants
    {
        /// <summary>
        /// A static class that contains settings for the plugin; these are mostly supposed to aid debugging and need to be compile-time constants.
        /// </summary>
        internal static class Settings
        {
            /// <summary>
            /// If true, <see cref="Controllers.BleedoutController"/> should allow players to revive themselves.
            /// </summary>
            internal const bool AllowSelfRevive = true;

            /// <summary>
            /// If true, cutscenes will be reduced to 0.5s per scene
            /// </summary>
            internal const bool SkipThroughScenes = true;
        }
    }
}
