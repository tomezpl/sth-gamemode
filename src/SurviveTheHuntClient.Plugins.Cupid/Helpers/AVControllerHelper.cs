using CitizenFX.Core;
using SurviveTheHuntClient.Models;
using System.Security.Policy;

namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    internal class AVControllerHelper
    {
        private readonly TriggerEventProxyDelegate TriggerEventProxy;

        private bool _isInStage = false;

        internal AVControllerHelper(TriggerEventProxyDelegate triggerEventProxyDelegate)
        {
            TriggerEventProxy = triggerEventProxyDelegate;
        }

        internal const string ResourceName = "sth-cupid-av";

        internal enum EventName
        {
            StartStage,
            EndStage,
            ShowStation,
            SetStationCoords,
        }

        internal static string BuildEventName(EventName eventName)
        {
            string eventNameKebab = SurviveTheHuntShared.Utils.TextHelper.CamelToKebab($"{eventName}");
            return $"{ResourceName}:{eventNameKebab}";
        }

        internal void StartStage(string stage)
        {
            string eventName = BuildEventName(EventName.StartStage);
            //Debug.WriteLine($"Sending event {eventName} with {stage}");

            TriggerEventProxy(eventName, stage, Constants.Settings.ApplySpoilerGuard);

            _isInStage = true;
        }

        internal void EndStage()
        {
            if (_isInStage)
            {
                TriggerEventProxy(BuildEventName(EventName.EndStage));
            }

            _isInStage = false;
        }

        private string _currentStation = null;
        internal string CurrentStation
        {
            get => _currentStation;
            set
            {
                if (_currentStation != value)
                {
                    _currentStation = value;
                    TriggerEventProxy(BuildEventName(EventName.ShowStation), _currentStation);
                }
            }
        }

        private float _prevStationX = 0.5f, _prevStationY = 0.5f;

        internal void SetStationPos(float x, float y)
        {
            if(_currentStation != null && (x != _prevStationX || y != _prevStationY))
            {
                _prevStationX = x;
                _prevStationY = y;
                TriggerEventProxy(BuildEventName(EventName.SetStationCoords), x * 100f, y * 100f);
            }
        }
    }
}
