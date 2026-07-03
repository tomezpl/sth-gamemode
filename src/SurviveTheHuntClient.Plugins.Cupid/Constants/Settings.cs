namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal partial class Constants
    {
        /// <summary>
        /// A static class that contains settings for the plugin; these are mostly supposed to aid debugging and need to be compile-time constants.
        /// </summary>
        internal static class Settings
        {
            internal const bool IsDebug = true;

            /// <summary>
            /// If true, <see cref="Controllers.BleedoutController"/> should allow players to revive themselves.
            /// </summary>
            internal const bool AllowSelfRevive = IsDebug;

            /// <summary>
            /// If true, cutscenes will be reduced to 0.5s per scene
            /// </summary>
            internal const bool SkipThroughScenes = IsDebug;

            /// <summary>
            /// If true, sth-cupid-av-controller's spoiler guard will be enabled (used for testing).
            /// </summary>
            internal const bool ApplySpoilerGuard = IsDebug;
        }
    }
}
