using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Models.UI;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers
{
    internal class BleedoutController : ITickable
    {
        private readonly ChangeGameModeSettingDelegate ChangeGameModeSetting;
        private readonly TriggerServerEventProxyDelegate TriggerServerEventProxy;

        internal BleedoutController(ChangeGameModeSettingDelegate changeGameModeSetting, TriggerServerEventProxyDelegate triggerServerEventProxy)
        {
            ChangeGameModeSetting = changeGameModeSetting;
            TriggerServerEventProxy = triggerServerEventProxy;
        }

        private bool _wasDyingLastTick = false;

        private bool _enabled = true;

        internal bool Enabled
        {
            get => _enabled;
            set
            {
                ChangeGameModeSetting(GameModeSetting.AllowAutoRespawn, !value);

                _enabled = value;
                _wasDyingLastTick = false;
                _elapsedBleedoutTime = 0f;
            }
        }

        internal delegate void BleedoutEventHandler(int playerPed);

        internal event BleedoutEventHandler StartedDying;
        internal event BleedoutEventHandler FinishedDying;

        private float _elapsedBleedoutTime = 0f;

        internal const float MaxBleedoutTimeSeconds = 10f;
        internal const float SecondsToRevive = 2.5f;

        private Dictionary<int, float> RevivableTimeRemaining = new Dictionary<int, float>();
        private List<int> PausedBleedout = new List<int>();

        internal float ReviveProgress => _reviveSecondsElapsed / SecondsToRevive;

        internal class BleedoutUIState
        {
            private LabelledItem _bleedoutTimer = new LabelledItem("DEATH", "00:10", SurviveTheHuntShared.Utils.EncodingHelper.PackRgba(200, 36, 12, 224))
            {
                XOffset = -0.015f
            };
            
            private LabelledItem _reviveProgress = new LabelledItem("REVIVE", 0f, SurviveTheHuntShared.Utils.EncodingHelper.PackRgba(248, 162, 70, 255));
            
            private float _lastProgress = 0f;
            private uint _lastBleedoutSeconds = (uint)MaxBleedoutTimeSeconds;

            internal bool IsReviving = false;
            internal bool IsBleedingOut = false;

            private LabelledItem[] _justBleedoutUI, _bleedoutAndReviveUI, _justReviveUI, _noUI;

            internal BleedoutUIState()
            {
                _justBleedoutUI = new LabelledItem[] { _bleedoutTimer };
                _bleedoutAndReviveUI = new LabelledItem[] { _bleedoutTimer, _reviveProgress };
                _justReviveUI = new LabelledItem[] { _reviveProgress };
                _noUI = new LabelledItem[0];
            }

            internal LabelledItem[] CurrentItems
            {
                get
                {
                    if(IsReviving)
                    {
                        return IsBleedingOut ? _bleedoutAndReviveUI : _justReviveUI;
                    }
                    else
                    {
                        return IsBleedingOut ? _justBleedoutUI : _noUI;
                    }
                }
            }

            internal void UpdateRevive(float progress)
            {
                if (_lastProgress != progress)
                {
                    _lastProgress = progress;
                    _reviveProgress.Value = SurviveTheHuntShared.Utils.EncodingHelper.Utf16FromNormalFloat(progress);
                }
            }

            internal void UpdateBleedout(float secondsLeft)
            {
                uint secondsLeftInt = (uint)secondsLeft;
                if(secondsLeftInt != _lastBleedoutSeconds)
                {
                    _lastBleedoutSeconds = secondsLeftInt;
                    uint seconds = secondsLeftInt % 60;
                    uint minutes = (secondsLeftInt - seconds) / 60;
                    string minutesStr = minutes < 10 ? $"0{(minutes == 0 ? "0" : minutes.ToString())}" : minutes.ToString();
                    string secondsStr = seconds < 10 ? $"0{(seconds == 0 ? "0" : seconds.ToString())}" : seconds.ToString();
                    _bleedoutTimer.Value = $"{minutes}:{secondsStr}";
                }
            }
        }

        internal readonly BleedoutUIState UIState = new BleedoutUIState();

        internal void SetRevivable(int pedId, bool revivable)
        {
            if(RevivableTimeRemaining.ContainsKey(pedId) && !revivable)
            {
                RevivableTimeRemaining.Remove(pedId);
                Debug.WriteLine($"{nameof(BleedoutController)}.{nameof(SetRevivable)}(): removing {pedId} from revivables");
            }
            else
            {
                RevivableTimeRemaining[pedId] = MaxBleedoutTimeSeconds;
                Debug.WriteLine($"{nameof(BleedoutController)}.{nameof(SetRevivable)}(): adding {pedId} to revivables");
            }

            if(pedId == PlayerPedId())
            {
                _elapsedBleedoutTime = 0f;
            }
        }

        internal void Reset()
        {
            _bledOutPeds.Clear();
            _elapsedBleedoutTime = 0f;
            _wasDyingLastTick = false;
            IsRevivingSomeone = false;

            UIState.IsReviving = false;
            UIState.IsBleedingOut = false;
        }

        internal void OnRespawn()
        {
            _elapsedBleedoutTime = 0f;
            _wasDyingLastTick = false;
            IsRevivingSomeone = false;

            UIState.IsReviving = false;
            UIState.IsBleedingOut = false;
        }

        internal void OnRevive(int revivedPed)
        {
            RevivableTimeRemaining.Remove(revivedPed);

            bool isReviveTargetLocal = revivedPed == PlayerPedId();

            if (isReviveTargetLocal)
            {
                _elapsedBleedoutTime = 0f;
                _wasDyingLastTick = false;
                UIState.IsBleedingOut = false;
            }

            // if we revived someone or we're the revived player, clear the revive state
            if(isReviveTargetLocal || IsRevivingSomeone)
            {
                IsRevivingSomeone = false;
            }

            while (PausedBleedout.Remove(revivedPed)) { }

            UIState.IsReviving = false;
        }

        private float _reviveSecondsElapsed = 0f;
        private int _currentlyRevivedPed = 0;

        /// <summary>
        /// True if the current player is reviving someone
        /// </summary>
        internal bool IsRevivingSomeone
        {
            get => _currentlyRevivedPed != 0;
            set
            {
                if(!value)
                {
                    _currentlyRevivedPed = 0;
                    _reviveSecondsElapsed = 0f;
                    UIState.IsReviving = false;
                }
            }
        }

        internal void OnCancelRevive(int almostRevivedPed)
        {
            if(_currentlyRevivedPed == almostRevivedPed || almostRevivedPed == PlayerPedId())
            {
                IsRevivingSomeone = false;
            }

            while (PausedBleedout.Remove(almostRevivedPed)) { }
        }

        internal void OnStartRevive(int pedToRevive)
        {
            if(_currentlyRevivedPed == pedToRevive || pedToRevive == PlayerPedId())
            {
                _reviveSecondsElapsed = float.Epsilon;
                UIState.IsReviving = true;
            }

            PausedBleedout.Add(pedToRevive);
        }

        private List<int> _bledOutPeds = new List<int>();

        /// <summary>
        /// Counts down time for non-local bleeding out players
        /// </summary>
        /// <param name="deltaTime">Time passed in seconds since last tick</param>
        private void TrackBleeding(float deltaTime)
        {
            float[] reviveTimes = new float[RevivableTimeRemaining.Count];
            int[] revivablePedIds = new int[reviveTimes.Length];
            RevivableTimeRemaining.Keys.CopyTo(revivablePedIds, 0);
            RevivableTimeRemaining.Values.CopyTo(reviveTimes, 0);

            for(int i = 0; i < revivablePedIds.Length; i++)
            {
                if (!PausedBleedout.Contains(revivablePedIds[i]))
                {
                    if (reviveTimes[i] <= 0f)
                    {
                        _bledOutPeds.Add(revivablePedIds[i]);
                    }

                    RevivableTimeRemaining[revivablePedIds[i]] = reviveTimes[i] - deltaTime;
                }
            }

            foreach(int bledOutPed in _bledOutPeds)
            {
                RevivableTimeRemaining.Remove(bledOutPed);
            }

            _bledOutPeds.Clear();
        }

        internal const Control ReviveControl = Control.Talk;

        public void Tick(float deltaTime)
        {
            TrackBleeding(deltaTime);

            if(!Enabled)
            {
                return;
            }

            int playerPed = PlayerPedId();

            bool isDead = IsPedDeadOrDying(playerPed, false);

            UIState.UpdateRevive(ReviveProgress);

            if (!isDead || CupidPlugin.AllowSelfRevive)
            {
                // If the player pressed E, revive a nearby bleeding out player
                // TODO: if we want to show a help notification instructing the player to revive when they're near a downed player, we'll have to flip this around so the distance checks come before input check
                if (IsControlJustPressed(0, (int)ReviveControl) || IsDisabledControlJustPressed(0, (int)ReviveControl))
                {
                    if (!IsRevivingSomeone)
                    {
                        const float MaxDistance = 2f;
                        const float MaxDistanceSq = MaxDistance * MaxDistance;

                        Vector3 playerPos = playerPos = GetEntityCoords(playerPed, false);

                        int revivedPed = 0;
                        foreach (int bleedingOutPed in RevivableTimeRemaining.Keys)
                        {
                            if (!PausedBleedout.Contains(bleedingOutPed) && ((CupidPlugin.AllowSelfRevive && bleedingOutPed == playerPed) || (GetEntityCoords(bleedingOutPed, false).DistanceToSquared(playerPos) < MaxDistanceSq)))
                            {
                                revivedPed = bleedingOutPed;
                                break;
                            }
                            else
                            {
                                Debug.WriteLine($"ped {playerPed} cannot revive ped {bleedingOutPed}");
                            }
                        }

                        if (revivedPed != 0)
                        {
                            TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.CupidNotifyReviveStart, PedToNet(revivedPed));
                            PausedBleedout.Add(revivedPed);
                            _currentlyRevivedPed = revivedPed;
                        }
                    }
                }

                if (IsRevivingSomeone)
                {
                    bool endRevive = !(IsControlPressed(0, (int)ReviveControl) || IsDisabledControlPressed(0, (int)ReviveControl));
                    bool cancelled = endRevive;

                    endRevive = endRevive || _reviveSecondsElapsed >= SecondsToRevive;

                    Debug.WriteLine($"{nameof(_reviveSecondsElapsed)} = {_reviveSecondsElapsed}, {nameof(SecondsToRevive)} = {SecondsToRevive}, {nameof(endRevive)} = {endRevive}");

                    if (endRevive)
                    {
                        TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.CupidNotifyReviveEnd, PedToNet(_currentlyRevivedPed), cancelled);
                        if (!cancelled)
                        {
                            RevivableTimeRemaining.Remove(_currentlyRevivedPed);
                        }
                        IsRevivingSomeone = false;
                    }
                }
            }
            
            if(isDead)
            {
                if (!_wasDyingLastTick)
                {
                    StartedDying.Invoke(playerPed);
                    _elapsedBleedoutTime = 0f;

                    UIState.IsBleedingOut = true;

                    // Cancel the current revive if we go down
                    if (IsRevivingSomeone)
                    {
                        TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.CupidNotifyReviveEnd, PedToNet(_currentlyRevivedPed), true);
                    }
                }

                float remaining = MaxBleedoutTimeSeconds - _elapsedBleedoutTime;

                if (remaining <= 0f)
                {
                    FinishedDying.Invoke(playerPed);
                    _elapsedBleedoutTime = 0f;

                    UIState.IsBleedingOut = false;
                }
                else
                {
                    if (!PausedBleedout.Contains(playerPed))
                    {
                        _elapsedBleedoutTime += deltaTime;
                    }
                }

                UIState.UpdateBleedout(remaining);

                _wasDyingLastTick = true;
            }

            if(_reviveSecondsElapsed != 0f)
            {
                _reviveSecondsElapsed += deltaTime;
            }
        }
    }
}
