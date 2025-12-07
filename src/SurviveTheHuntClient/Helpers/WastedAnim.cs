using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using System;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    internal class WastedAnim : ITickable
    {
        private bool NeedsToPlayWastedScaleform = false;
        private const string GfxName = "generic";
        private const string ScaleformName = "MP_BIG_MESSAGE_FREEMODE";
        private const string MethodName = "SHOW_SHARD_WASTED_MP_MESSAGE";

        private int? ScaleformHandle = null;
        private bool ShouldShow = false;

        private const float SecondsTillShard = 0.4f;
        private float SecondsPassedSinceDeath = 0f;
        private int? CurrentSoundId = null;

        internal delegate void ExecutePluginsDelegate(Action<Plugin> plugin);

        private ExecutePluginsDelegate ExecutePlugins;

        internal WastedAnim(ExecutePluginsDelegate executePlugins)
        {
            ExecutePlugins = executePlugins;
        }

        internal void NotifyDeath()
        {
            NeedsToPlayWastedScaleform = true;
            ShouldShow = true;
            if (!ScaleformHandle.HasValue || !HasScaleformMovieLoaded(ScaleformHandle.Value))
            {
                ScaleformHandle = RequestScaleformMovie(ScaleformName);
            }
            SecondsPassedSinceDeath = 0f;

            if(CurrentSoundId.HasValue)
            {
                ReleaseSoundId(CurrentSoundId.Value);
            }
            CurrentSoundId = GetSoundId();
            PlaySoundFrontend(CurrentSoundId.Value, "MP_Flash", "WastedSounds", true);
        }

        internal void StopShowing()
        {
            ShouldShow = false;
            NeedsToPlayWastedScaleform = false;
        }

        public void Tick(float deltaTime)
        {
            if(NeedsToPlayWastedScaleform && ScaleformHandle.HasValue && HasScaleformMovieLoaded(ScaleformHandle.Value))
            {
                string customWastedText = null;

                ExecutePlugins(plugin =>
                {
                    if(customWastedText == null)
                    {
                        customWastedText = plugin.CustomWastedText;
                    }
                });

                Debug.WriteLine("Playing scaleform!");
                NeedsToPlayWastedScaleform = false;
                BeginScaleformMovieMethod(ScaleformHandle.Value, MethodName);
                PushScaleformMovieMethodParameterString(customWastedText ?? GetLabelText("RESPAWN_W"));
                PushScaleformMovieMethodParameterString("");
                PushScaleformMovieMethodParameterInt(13);
                PushScaleformMovieMethodParameterBool(false);
                PushScaleformMovieMethodParameterBool(true);
                EndScaleformMovieMethod();
                ShouldShow = true;

                AnimpostfxPlay("DeathFailOut", 0, true);

                ShakeGameplayCam("DEATH_FAIL_IN_EFFECT_SHAKE", 0.75f);
            }

            if (ShouldShow)
            {
                if (ScaleformHandle.HasValue && SecondsPassedSinceDeath >= SecondsTillShard)
                {
                    DrawScaleformMovieFullscreen(ScaleformHandle.Value, 255, 255, 255, 255, 0);
                }

                SecondsPassedSinceDeath += GetFrameTime();
            }

            if(IsScreenFadingIn())
            {
                ShouldShow = false;
                SecondsPassedSinceDeath = 0f;
                AnimpostfxStop("DeathFailOut");
                StopGameplayCamShaking(true);
            }

            if(!ShouldShow && CurrentSoundId.HasValue)
            {
                StopSound(CurrentSoundId.Value);
                ReleaseSoundId(CurrentSoundId.Value);
                CurrentSoundId = null;
            }
        }
    }
}
