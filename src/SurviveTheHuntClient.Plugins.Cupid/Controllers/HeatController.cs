using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models.UI;
using System;

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
                    OnHeatChanged(old, value);
                }
            }
        }

        private void OnHeatChanged(ushort old, ushort current)
        {
            float progress = Math.Max(0f, Math.Min(1f, (float)current / (float)Constants.HeatValues.Target));

            _heatBar.Value = SurviveTheHuntShared.Utils.EncodingHelper.Utf16FromNormalFloat(progress);
            _heatBar.Colour = GetHeatColour(progress);
        }

        private bool _hasStarted = false;

        internal HeatController()
        {
            _uiItems[0] = _heatBar;
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
