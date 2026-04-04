using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
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

        private Dictionary<int, float> RevivableTimeRemaining = new Dictionary<int, float>();

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
        }

        internal void OnRespawn()
        {
            _elapsedBleedoutTime = 0f;
            _wasDyingLastTick = false;
        }

        internal void OnRevive(int revivedPed)
        {
            RevivableTimeRemaining.Remove(revivedPed);

            if (revivedPed == PlayerPedId())
            {
                _elapsedBleedoutTime = 0f;
                _wasDyingLastTick = false;
            }
        }

        private List<int> _bledOutPeds = new List<int>();

        private void TrackBleeding(float deltaTime)
        {
            float[] reviveTimes = new float[RevivableTimeRemaining.Count];
            int[] revivablePedIds = new int[reviveTimes.Length];
            RevivableTimeRemaining.Keys.CopyTo(revivablePedIds, 0);
            RevivableTimeRemaining.Values.CopyTo(reviveTimes, 0);

            for(int i = 0; i < revivablePedIds.Length; i++)
            {
                if (reviveTimes[i] <= 0f)
                {
                    _bledOutPeds.Add(revivablePedIds[i]);
                }

                RevivableTimeRemaining[revivablePedIds[i]] = reviveTimes[i] - deltaTime;
            }

            foreach(int bledOutPed in _bledOutPeds)
            {
                RevivableTimeRemaining.Remove(bledOutPed);
            }

            _bledOutPeds.Clear();
        }

        public void Tick(float deltaTime)
        {
            TrackBleeding(deltaTime);

            int playerPed = PlayerPedId();

            // If the player pressed F, revive a nearby bleeding out player
            if (IsControlJustPressed(0, (int)Control.Enter) || IsDisabledControlJustPressed(0, (int)Control.Enter))
            {
                const float MaxDistance = 2f;
                const float MaxDistanceSq = MaxDistance * MaxDistance;

                Vector3 playerPos = playerPos = GetEntityCoords(playerPed, false);

                int revivedPed = 0;
                foreach(int bleedingOutPed in RevivableTimeRemaining.Keys)
                {
                    if((CupidPlugin.AllowSelfRevive && bleedingOutPed == playerPed) || (GetEntityCoords(bleedingOutPed, false).DistanceToSquared(playerPos) < MaxDistanceSq))
                    {
                        TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.CupidNotifyRevived, PedToNet(bleedingOutPed));
                        revivedPed = bleedingOutPed;
                        break;
                    }
                    else
                    {
                        Debug.WriteLine($"ped {playerPed} cannot revive ped {bleedingOutPed}");
                    }
                }

                if(revivedPed != 0)
                {
                    RevivableTimeRemaining.Remove(revivedPed);
                }
            }

            if(!Enabled)
            {
                return;
            }

            if(IsPedDeadOrDying(playerPed, false))
            {
                if(!_wasDyingLastTick)
                {
                    StartedDying.Invoke(playerPed);
                    _elapsedBleedoutTime = 0f;
                }

                if(_elapsedBleedoutTime >= MaxBleedoutTimeSeconds)
                {
                    FinishedDying.Invoke(playerPed);
                    _elapsedBleedoutTime = 0f;
                }
                else
                {
                    _elapsedBleedoutTime += deltaTime;
                }

                _wasDyingLastTick = true;
            }
        }
    }
}
