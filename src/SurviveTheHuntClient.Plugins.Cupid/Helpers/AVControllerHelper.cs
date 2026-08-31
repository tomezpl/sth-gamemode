using CitizenFX.Core;
using SurviveTheHuntClient.Models;
using System.Collections.Generic;
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
            SetPlayerNames
        }

        internal static string BuildEventName(EventName eventName)
        {
            string eventNameKebab = SurviveTheHuntShared.Utils.TextHelper.CamelToKebab($"{eventName}");
            return $"{ResourceName}:{eventNameKebab}";
        }

        internal void StartStage(string stage, object param = null)
        {
            string eventName = BuildEventName(EventName.StartStage);
            //Debug.WriteLine($"Sending event {eventName} with {stage}");

            TriggerEventProxy(eventName, stage, Constants.Settings.ApplySpoilerGuard, param);

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
        }

        private bool _isCurrentStationBlocked = false;
        internal bool IsCurrentStationBlocked => _isCurrentStationBlocked;

        internal void SetCurrentStation(string stationName, bool blocked = false, bool force = false)
        {
            if (_currentStation != stationName || _isCurrentStationBlocked != blocked || force)
            {
                _currentStation = stationName;
                _isCurrentStationBlocked = blocked;
                TriggerEventProxy(BuildEventName(EventName.ShowStation), _currentStation, _isCurrentStationBlocked);
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

        internal void SetPlayerNames(string[] names)
        {
            TriggerEventProxy(BuildEventName(EventName.SetPlayerNames), new List<string>(names));
        }
    }
}
