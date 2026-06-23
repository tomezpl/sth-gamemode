using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models.UI;
using System;
using CitizenFX.Core;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers
{
    internal sealed class HeatController : ITickable
    {
        private readonly LabelledItem _heatBar = new LabelledItem("HEAT", 0f, uint.MaxValue, 3);

        private readonly LabelledItem[] _uiItems = new LabelledItem[1];

        internal LabelledItem[] UIItems => _uiItems;

        private ushort _currentHeat;

        internal ushort CurrentHeat
        {
            get => _currentHeat;
            set
            {
                ushort old = _currentHeat;
                if(old != value)
                {
                    _currentHeat = value;
                    HeatChanged.Invoke(old, value);
                }
            }
        }

        internal static Constants.HeatThresholds GetTier(ushort score)
        {
            if(score >= (ushort)Constants.HeatThresholds.Target)
            {
                return Constants.HeatThresholds.Target;
            }

            if(score >= (ushort)Constants.HeatThresholds.Heat2)
            {
                return Constants.HeatThresholds.Heat2;
            }

            if (score >= (ushort)Constants.HeatThresholds.Heat1)
            {
                return Constants.HeatThresholds.Heat1;
            }

            return Constants.HeatThresholds.Start;
        }

        private bool _hasStarted = false;

        internal delegate void HeatTierChangedEvent(Constants.HeatThresholds prev, Constants.HeatThresholds current);
        internal event HeatTierChangedEvent HeatTierChanged;

        internal delegate void HeatChangedEvent(ushort old, ushort current);
        internal event HeatChangedEvent HeatChanged;

        internal Constants.HeatThresholds Tier => GetTier(_currentHeat);

        private void OnHeatChanged(ushort old, ushort current)
        {
            Constants.HeatThresholds prevTier = GetTier(old);
            Constants.HeatThresholds currentTier = GetTier(current);

            float progress = Math.Max(0f, Math.Min(1f, (float)current / (float)Constants.HeatThresholds.Target));

            _heatBar.Value = SurviveTheHuntShared.Utils.EncodingHelper.Utf16FromNormalFloat(progress);
            _heatBar.Colour = GetHeatColour(progress);

            if (prevTier != currentTier)
            {
                Debug.WriteLine($"{HeatTierChanged} {HeatChanged} {prevTier} {currentTier}");
                HeatTierChanged.Invoke(prevTier, currentTier);
            }
        }

        private static void HeatTierChangedNoop(Constants.HeatThresholds a, Constants.HeatThresholds b)
        {

        }

        internal HeatController()
        {
            _uiItems[0] = _heatBar;
            HeatChanged += OnHeatChanged;
            HeatTierChanged += HeatTierChangedNoop;
        }

        private static uint GetHeatColour(float progress)
        {
            float startR = 255f, startG = 255f, startB = 255f;
            float endR = 197f, endG = 38f, endB = 255f;

            float mixedR = (startR + (endR - startR) * progress);
            float mixedG = (startG + (endG - startG) * progress);
            float mixedB = (startB + (endB - startB) * progress);

            return SurviveTheHuntShared.Utils.EncodingHelper.PackRgba((byte)mixedR, (byte)mixedG, (byte)mixedB, 255);
        }

        private bool _hasCleanedUp = false;
        internal void Cleanup()
        {
            if(!_hasCleanedUp)
            {
                _hasCleanedUp = true;
            }
        }

        internal void Start()
        {
            _hasStarted = true;
        }

        public void Tick(float deltaTime)
        {
            if(_hasCleanedUp || !_hasStarted)
            {
                return;
            }

            TickImpl(deltaTime);
        }

        private void TickImpl(float deltaTime)
        {

        }
    }
}
