using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Helpers;
using SurviveTheHuntClient.Plugins.Cupid.Interfaces;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers
{
    internal sealed class CellTowerPingController : ITickable, ISpecialEventListener
    {
        private int _targetPlayer;

        internal int TargetPlayer => _targetPlayer;

        internal const int Seed = 6767;

        private const int BlipPoolSize = 4;
        private RadiusBlipState[] _blipPool = new RadiusBlipState[BlipPoolSize];

        private const string BlipNameKey = "STH_CUPID_CELLTOWER_PING_BLIP_NAME";
        private const string BlipNameContent = "Cell Tower Signal";

        private class RadiusBlipState
        {
            internal readonly int RadiusBlipId, BlipId;
            internal float Alpha = 0f;

            private float _currentDistanceSq = float.PositiveInfinity;
            private const float MaxRadius = BlipBaseRadius + RadiusError;
            private float _currentRadius = MaxRadius;

            internal RadiusBlipState(int radiusBlipId, int blipId)
            {
                RadiusBlipId = radiusBlipId;
                BlipId = blipId;
            }

            internal bool UpdateState(float targetX, float targetY, float x, float y, float radius)
            {
                // TODO: we don't need the target position here - just compute velocity
                float a = targetX - x;
                float b = targetY - y;
                float distSq = a * a + b * b;
                if(distSq <= _currentDistanceSq + float.Epsilon || radius <= _currentRadius + float.Epsilon)
                {
                    _currentDistanceSq = distSq;
                    _currentRadius = radius;
                    return true;
                }

                return false;
            }

            internal void Reset()
            {
                _currentDistanceSq = float.PositiveInfinity;
                _currentRadius = MaxRadius;
                Alpha = 0f;
            }
        }

        private struct ScannedBlipInfo
        {
            internal int BlipIndex;
            internal float DistanceSquared;
        }

        /// <summary>
        /// Indices of the nearest <see cref="_radiusBlips"/>. This is a temporary store really, but we don't want to keep allocating memory
        /// </summary>
        private readonly List<ScannedBlipInfo> _nearestBlips = new List<ScannedBlipInfo>(BlipPoolSize), _prevNearestBlips = new List<ScannedBlipInfo>();

        private static readonly int[] s_NewBlips = new int[BlipPoolSize];

        private struct RadiusBlip
        {
            internal float Radius;
            internal float X, Y;
            internal Vec2 MaxRandomOffset;
        }

        private RadiusBlip[] _radiusBlips = new RadiusBlip[0];
        private float[] _radiusBlipTimeSpent = new float[0];
        private bool _hasStarted = false;

        private struct Vec2
        {
            internal float X, Y;
        }

        private const int BlipRows = 24;
        // this isn't ideal - ideally we'd have a variable number of blips per row, and fit them to hit blipcount, but this is easier for a start
        private const int BlipsPerRow = 20;
        private const int BlipCount = BlipRows * BlipsPerRow;
        internal const float BlipBaseRadius = 350f;
        internal const float RadiusError = 55f;

        private readonly Random _rng = new Random(Seed);

        internal const float GridStartX = -2750f;
        internal const float GridStartY = 6200f;
        internal const float GridEndX = 3400f;
        internal const float GridEndY = -4400f;

        internal const float MinBlipRadius = 80f;
        internal const float BlipFocusTimeSeconds = 12f;

        private static readonly bool s_HasInit = Init();

        private static bool Init()
        {
            if(!s_HasInit)
            {
                AddTextEntry(BlipNameKey, BlipNameContent);
            }

            return true;
        }

        internal readonly bool IsTargetPlayerLocal;

        private readonly TriggerServerEventProxyDelegate TriggerServerEvent;

        internal CellTowerPingController(int targetPlayer, TriggerServerEventProxyDelegate triggerServerEvent)
        {
            _targetPlayer = targetPlayer;
            IsTargetPlayerLocal = targetPlayer == PlayerId();
            TriggerServerEvent = triggerServerEvent;

            Debug.WriteLine($"{nameof(CellTowerPingController)}: creating blip pool ({_blipPool?.Length} blips)");
            for(int i = 0; i < _blipPool.Length; i++)
            {
                _blipPool[i] = new RadiusBlipState(AddBlipForRadius(0f, 0f, 0f, BlipBaseRadius), AddBlipForCoord(0f, 0f, 0f));
                // yellow objective colour
                SetBlipColour(_blipPool[i].RadiusBlipId, (int)BlipColor.Yellow);
                SetBlipColour(_blipPool[i].BlipId, (int)BlipColor.Yellow);
                // hide by default
                SetBlipDisplay(_blipPool[i].RadiusBlipId, 0);
                SetBlipDisplay(_blipPool[i].BlipId, 0);
                SetBlipNameFromTextFile(_blipPool[i].BlipId, BlipNameKey);
            }
        }

        private static float RandomFloatInRange(Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (2f * (max - min));
        }

        internal void Start()
        {
            if (!_hasStarted)
            {
                _hasReceivedRemoteTargetPos = false;
                _hasStarted = true;

                _radiusBlips = new RadiusBlip[BlipCount];
                _radiusBlipTimeSpent = new float[BlipCount];
                for(int i = 0; i < BlipCount; i++)
                {
                    _radiusBlipTimeSpent[i] = 0f;
                }

                const float greaterY = GridStartY > GridEndY ? GridStartY : GridEndY;
                const float lesserY = GridStartY > GridEndY ? GridEndY : GridStartY;
                const float greaterX = GridStartX > GridEndX ? GridStartX : GridEndX;
                const float lesserX = GridStartX > GridEndX ? GridEndX : GridStartX;
                const float GridHeight = greaterY - lesserY;
                const float GridWidth = greaterX - lesserX;
                const float RowGap = GridHeight / (float)BlipRows;
                const float ColGap = GridWidth / (float)BlipsPerRow;

                float currentBaseY = (GridStartY - RowGap * .5f);
                float currentBaseX = (GridStartX + ColGap * .5f);

                for (int i = 0; i < BlipCount; i++)
                {
                    int col = i % BlipsPerRow;

                    if (col == 0)
                    {
                        currentBaseX = GridStartX + ColGap * .5f;

                        int row = i / BlipsPerRow;
                        currentBaseY = GridStartY - RowGap * .5f - (row * RowGap);
                    }
                    else
                    {
                        currentBaseX += ColGap;
                        // FIXME: what the fuck
                        //currentBaseY += RowGap;
                    }

                    const float XError = ColGap * .2f;
                    const float YError = RowGap * .3f;
                    float radius = BlipBaseRadius + RandomFloatInRange(_rng, -RadiusError, RadiusError);
                    const float MaxRadiusFactorUsedForOffset = 0.1f;
                    _radiusBlips[i] = new RadiusBlip
                    {
                        Radius = radius,
                        X = currentBaseX + RandomFloatInRange(_rng, -XError, XError),
                        Y = currentBaseY + RandomFloatInRange(_rng, -YError, YError),
                        MaxRandomOffset = new Vec2
                        {
                            X = RandomFloatInRange(_rng, -MaxRadiusFactorUsedForOffset * radius, MaxRadiusFactorUsedForOffset * radius),
                            Y = RandomFloatInRange(_rng, -MaxRadiusFactorUsedForOffset * radius, MaxRadiusFactorUsedForOffset * radius),
                        },
                    };
                }
            }
        }

        internal const float BlipUpdateIntervalSeconds = 0.85f;
        private float _timeSinceBlipUpdateSeconds = 0f;

        private float _runningTime = 0f;

        private const float AverageSpeedBufferSizeSeconds = 1f;

        private float _averageSpeedBuffer = 0f;
        private float _averageSpeedTimeTrackedSeconds = 0f;

        private float _averageSpeed = 0f;

        private void TrackAverageSpeed(float currentSpeed, float deltaTime)
        {
            _averageSpeedBuffer += currentSpeed / deltaTime;
            _averageSpeedTimeTrackedSeconds += deltaTime;

            if(_averageSpeedTimeTrackedSeconds >= AverageSpeedBufferSizeSeconds)
            {
                _averageSpeed = _averageSpeedBuffer * (AverageSpeedBufferSizeSeconds / _averageSpeedTimeTrackedSeconds);
                _averageSpeedBuffer = 0f;
                _averageSpeedTimeTrackedSeconds = 0f;
            }
        }

        private bool _hasReceivedRemoteTargetPos = false, _acknowledgedLastSync = true;
        private Vector3 _lastInterpolatedRemoteTargetPos = Vector3.Zero;

        private float _secondsSinceRemoteTargetSync = 0f;
        private Vector3 _lastSyncedRemoteTargetPos = Vector3.Zero, _lastRemoteTargetVelocity = Vector3.Zero;

        private const float RemoteTargetSyncIntervalSeconds = 1.35f;

        private Vector3 InterpolateRemoteTargetPosition()
        {
            return _lastSyncedRemoteTargetPos + _lastRemoteTargetVelocity * _secondsSinceRemoteTargetSync;
        }

        private Vector3 GetTargetPosition(bool isTarget)
        {
            if(isTarget)
            {
                return GetEntityCoords(PlayerPedId(), false);
            }

            return _lastInterpolatedRemoteTargetPos;
        }

        private Vector3? _targetPlayerPrevPos = null;
        public void Tick(float deltaTime)
        {
            if(_hasStarted)
            {
                _timeSinceBlipUpdateSeconds += deltaTime;

                if(_timeSinceBlipUpdateSeconds >= BlipUpdateIntervalSeconds)
                {
                    _timeSinceBlipUpdateSeconds = 0f;
                    UpdateBlips();
                }

                if (!IsTargetPlayerLocal && _hasReceivedRemoteTargetPos)
                {
                    _lastInterpolatedRemoteTargetPos = InterpolateRemoteTargetPosition();
                }

                Vector3 targetPlayerPos = GetTargetPosition(IsTargetPlayerLocal);

                if (IsTargetPlayerLocal)
                {
                    if(_secondsSinceRemoteTargetSync >= RemoteTargetSyncIntervalSeconds)
                    {
                        // TODO: to fix jittery blip sync on other clients, we'll need to periodically send our position here,
                        // then remote clients will need to derive constant velocity from that and the previous position they received,
                        // so they can then interpolate between syncs.
                        _secondsSinceRemoteTargetSync = 0f;
                        float velX = 0f, velY = 0f, velZ = 0f;
                        if(_targetPlayerPrevPos.HasValue)
                        {
                            velX = (targetPlayerPos.X - _targetPlayerPrevPos.Value.X) / deltaTime;
                            velY = (targetPlayerPos.Y - _targetPlayerPrevPos.Value.Y) / deltaTime;
                            velZ = (targetPlayerPos.Z - _targetPlayerPrevPos.Value.Z) / deltaTime;
                        }
                        _acknowledgedLastSync = false;
                        TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidBroadcastSpecialEvent, (int)Constants.SpecialEvent.SyncHuntedTargetPos, targetPlayerPos.X, targetPlayerPos.Y, targetPlayerPos.Z, velX, velY, velZ);
                    }
                }

                Vector3 localPlayerPos = IsTargetPlayerLocal ? targetPlayerPos : GetEntityCoords(PlayerPedId(), false);

                if (_acknowledgedLastSync)
                {
                    _secondsSinceRemoteTargetSync += deltaTime;
                }

                if(!_targetPlayerPrevPos.HasValue)
                {
                    _targetPlayerPrevPos = targetPlayerPos;
                }

                float offsetX = targetPlayerPos.X - _targetPlayerPrevPos.Value.X, offsetY = targetPlayerPos.Y - _targetPlayerPrevPos.Value.Y;

                // TODO: we can't track instantaneous speed - we need to track average over frames, otherwise this is mega janky...
                //      actually no nvm
                float frameDisplacementSq = (offsetX * offsetX + offsetY * offsetY);
                float frameDisplacment = (float)Math.Sqrt(frameDisplacementSq);
                float speed = IsTargetPlayerLocal ?  frameDisplacment / deltaTime : ((float)Math.Sqrt(_lastRemoteTargetVelocity.X * _lastRemoteTargetVelocity.X + _lastRemoteTargetVelocity.Y * _lastRemoteTargetVelocity.Y));
                //TrackAverageSpeed(frameDisplacment, deltaTime);

                const float MaxSpeed = 155f;
                const float MaxSpeedSq = MaxSpeed * MaxSpeed;

                float unboundRate = speed / MaxSpeed;
                float normalRate = Math.Min(1f, unboundRate);
                float rate = Math.Max(0.15f, Math.Min(2f, unboundRate));

                _runningTime += deltaTime * rate;

                _runningTime %= (float)Math.PI * 2f;

                for (int i = 0; i < _nearestBlips.Count && i < _blipPool.Length; i++)
                {
                    int blipIndex = _nearestBlips[i].BlipIndex;
                    float timeSpent = _radiusBlipTimeSpent[blipIndex];

                    const float MaxFocus = 0.67f;
                    float progress = Math.Min(MaxFocus, timeSpent / BlipFocusTimeSeconds);

                    float MaxPosError = 2f * (1f - progress);

                    // TODO: do a sine of time to get a oscillating offset
                    float sine = (float)Math.Sin(_runningTime), cos = (float)Math.Cos(_runningTime);
                    float randomOffsetMultiplier = 1f - Math.Max(0f, normalRate);
                    Vec2 randomOffset = _radiusBlips[blipIndex].MaxRandomOffset;
                    //randomOffsetMultiplier = 0f;

                    float originalX = _radiusBlips[blipIndex].X, originalY = _radiusBlips[blipIndex].Y;
                    float xOffset = targetPlayerPos.X - originalX, yOffset = targetPlayerPos.Y - originalY;

                    float xError = 0f;// (((float)_rng.NextDouble() * 2f) - 1f) * MaxPosError * (1f - Math.Min(1f, rate));
                    float yError = 0f;// (((float)_rng.NextDouble() * 2f) - 1f) * MaxPosError * (1f - Math.Min(1f, rate));
                    const float CenteringStrength = 2.5f;
                    float originalRadius = _radiusBlips[blipIndex].Radius;

                    float newX = originalX + (xOffset * Math.Min(1f, progress * rate * CenteringStrength)) + xError + (randomOffset.X * randomOffsetMultiplier * sine);
                    float newY = originalY + (yOffset * Math.Min(1f, progress * rate * CenteringStrength)) + yError + (randomOffset.Y * randomOffsetMultiplier * cos);
                    float newRadius = originalRadius + (Math.Min(originalRadius, MinBlipRadius) - originalRadius) * Math.Min(1f, progress * rate);

                    //if (_blipPool[i].UpdateState(playerPos.X, playerPos.Y, newX, newY, newRadius))
                    {
                        SetBlipCoords(_blipPool[i].RadiusBlipId, newX, newY, 0f);
                        SetBlipCoords(_blipPool[i].BlipId, newX, newY, 0f);
                        SetBlipScale(_blipPool[i].RadiusBlipId, newRadius);
                        int alphaInt = (int)Math.Floor(MaxRadiusBlipAlpha * _blipPool[i].Alpha * (1f - progress));
                        SetBlipAlpha(_blipPool[i].RadiusBlipId, alphaInt);
                        SetBlipAlpha(_blipPool[i].BlipId, MinimapHelper.IsCoordInRadarBounds(newX, newY, localPlayerPos.X, localPlayerPos.Y, true) ? 0 : alphaInt);
                    }

                    if(timeSpent >= BlipFocusTimeSeconds || progress >= MaxFocus)
                    {
                        timeSpent = 0f;
                    }

                    _radiusBlipTimeSpent[blipIndex] = Math.Min(BlipFocusTimeSeconds, timeSpent + deltaTime);
                }

                _targetPlayerPrevPos = targetPlayerPos;
            }
        }

        private int _currentBlipIndex = 0;

        private const int MaxRadiusBlipAlpha = 200;


        private void UpdateBlips()
        {
            _nearestBlips.Clear();

            int ped = GetPlayerPed(_targetPlayer);

            Vector3 pos = GetEntityCoords(ped, false);

            const float MaxDistance = (BlipBaseRadius + RadiusError) * 2f + float.Epsilon;
            const float MaxDistanceSq = MaxDistance * MaxDistance;

            
            int direction = 1;
            float shortestDistSq = float.PositiveInfinity;
            int closestBlipIndex = -1;
            // TODO: when do we terminate?
            const int MaxIterations = BlipCount;
            // FIXME: this fucking sucks stop optimising and just run through the whole loop
            /*for (int leftIndex = Math.Max(0, _currentBlipIndex - 1), rightIndex = _currentBlipIndex, iterations = 0; iterations < MaxIterations; iterations++, direction *= -1)
            {
                int index = direction == 1 ? rightIndex : leftIndex;

                float distanceSq = ((pos.X - _radiusBlips[index].X) * (pos.X - _radiusBlips[index].X)) + ((pos.Y - _radiusBlips[index].Y) * (pos.Y - _radiusBlips[index].Y));
                float maxDistSq = (_radiusBlips[index].Radius * 2f + float.Epsilon) * (_radiusBlips[index].Radius * 2f + float.Epsilon);

                if(distanceSq < shortestDistSq)
                {
                    closestBlipIndex = index;
                    shortestDistSq = distanceSq;
                }

                if(distanceSq < maxDistSq)
                {
                    if(!s_NearestBlips.Contains(index))
                    {
                        s_NearestBlips.Add(index);
                    }
                }

                if(iterations == MaxIterations - 1 && s_NearestBlips.Count == 0)
                {
                    s_NearestBlips.Add(closestBlipIndex);
                }

                if(direction == 1)
                {
                    rightIndex = Math.Min(_radiusBlips.Length - 1, rightIndex + direction);
                }
                else
                {
                    leftIndex = Math.Max(0, leftIndex + direction);
                }
            }*/

            for(int i = 0; i < _radiusBlips.Length; i++)
            {
                // TODO: could do early reject by testing for like, triple the max radius or smth
                float x = _radiusBlips[i].X, y = _radiusBlips[i].Y;
                float distSq = ((x - pos.X) * (x - pos.X)) + ((y - pos.Y) * (y - pos.Y));
                for(int j = 0; j < _blipPool.Length; j++)
                {
                    ScannedBlipInfo info = new ScannedBlipInfo { BlipIndex = i, DistanceSquared = distSq };

                    if (i == 0 || !_nearestBlips.Contains(info))
                    {
                        if (j >= _nearestBlips.Count)
                        {
                            _nearestBlips.Add(info);
                            break;
                        }
                        if (distSq <= _nearestBlips[j].DistanceSquared)
                        {
                            _nearestBlips.Insert(j, info);

                            if (_nearestBlips.Count > _blipPool.Length)
                            {
                                _nearestBlips.RemoveAt(_nearestBlips.Count - 1);
                            }
                        }
                    }
                }
            }

            /*Comparison<ScannedBlipInfo> comparer = new Comparison<ScannedBlipInfo>((a, b) =>
            {
                float diff = a.DistanceSquared - b.DistanceSquared;
                if(diff > float.Epsilon)
                {
                    return 1;
                }

                if(diff < -float.Epsilon)
                {
                    return -1;
                }

                return 0;
            });

            s_NearestBlips.Sort(comparer);*/

            /*Debug.WriteLine($"Nearest {s_NearestBlips.Count} blips are:");
            for(int i = 0; i < s_NearestBlips.Count; i++)
            {
                Debug.WriteLine($"\t{i + 1}. {Math.Sqrt(s_NearestBlips[i].DistanceSquared)}m away: blip {s_NearestBlips[i].BlipIndex} at X = {_radiusBlips[s_NearestBlips[i].BlipIndex].X}, Y = {_radiusBlips[s_NearestBlips[i].BlipIndex].Y}");
            }*/

            Array.Clear(s_NewBlips, 0, s_NewBlips.Length);

            for(int i = 0, counter = 0; i < s_NewBlips.Length && i < _nearestBlips.Count; i++)
            {
                bool isNew = true;
                foreach(ScannedBlipInfo prevBlip in _prevNearestBlips)
                {
                    if (prevBlip.BlipIndex == _nearestBlips[i].BlipIndex)
                    {
                        isNew = false;
                        break;
                    }
                }

                if (isNew)
                {
                    s_NewBlips[counter++] = _nearestBlips[i].BlipIndex;
                }
            }

            for(int i = 0; i < _blipPool.Length; i++)
            {
                int radiusBlip = _blipPool[i].RadiusBlipId, blip = _blipPool[i].BlipId;
                if (i < _nearestBlips.Count)
                {
                    int radiusBlipIndex = _nearestBlips[i].BlipIndex;
                    bool isNew = Array.IndexOf(s_NewBlips, radiusBlipIndex) != -1;
                    float radius = _radiusBlips[radiusBlipIndex].Radius;
                    SetBlipDisplay(radiusBlip, 6);
                    SetBlipDisplay(blip, 6);
                    // TODO: we also need to have small regular blips that show on the radar at all times
                    if (isNew)
                    {
                        float x = _radiusBlips[radiusBlipIndex].X, y = _radiusBlips[radiusBlipIndex].Y;
                        SetBlipCoords(radiusBlip, x, y, 0f);
                        SetBlipCoords(blip, x, y, 0f);
                        SetBlipScale(radiusBlip, radius);
                        _radiusBlipTimeSpent[radiusBlipIndex] = 0f;
                        _blipPool[i].Reset();
                    }
                    //Debug.WriteLine($"Updating blip {radiusBlipIndex} (X = {x}, Y = {y}, Radius = {radius})");
                    float distance = (float)Math.Sqrt(_nearestBlips[i].DistanceSquared);
                    float alpha = 0f;
                    if (distance < MaxDistance)
                    {
                        alpha = (float)Math.Max(0f, 1f - (distance / (2f * radius)));
                        const float MinAlpha = 0.2f;
                        // remap to MinAlpha..1.0
                        alpha = MinAlpha + ((1f - MinAlpha) * alpha);
                    }

                    //Debug.WriteLine($"Alpha: {alpha}");
                    _blipPool[i].Alpha = alpha;
                    int alphaInt = (int)Math.Floor(MaxRadiusBlipAlpha * alpha);
                    SetBlipAlpha(radiusBlip, alphaInt);
                    SetBlipAlpha(_blipPool[i].BlipId, alphaInt);
                    //SetBlipAlpha(blip, 240);
                }
                else
                {
                    SetBlipDisplay(radiusBlip, 0);
                    SetBlipDisplay(blip, 0);
                    //SetBlipAlpha(blip, 240);
                }
            }

            /*float x = _radiusBlips[_currentBlipIndex].X, y = _radiusBlips[_currentBlipIndex].Y, radius = _radiusBlips[_currentBlipIndex].Radius;
            SetBlipCoords(_blipPool[0], x, y, 0f);
            SetBlipScale(_blipPool[0], radius);
            SetBlipDisplay(_blipPool[0], 6);
            Debug.WriteLine($"Current blip ({_currentBlipIndex} / {_radiusBlips.Length - 1}): X = {x}, Y = {y}, Radius = {radius}");*/

            // TODO: don't need this
            _currentBlipIndex = (_currentBlipIndex + 1) % _radiusBlips.Length;

            // TODO: make the blips concentrate on the player's position

            _prevNearestBlips.Clear();
            _prevNearestBlips.AddRange(_nearestBlips);
        }

        internal void Cleanup()
        {
            foreach(RadiusBlipState blip in _blipPool)
            {
                int blipRef = blip.RadiusBlipId;
                RemoveBlip(ref blipRef);
                blipRef = blip.BlipId;
                RemoveBlip(ref blipRef);
            }
        }

        public void OnSpecialEvent(Constants.SpecialEvent specialEvent, object[] args)
        {
            // Sync with the remote player's latest position and derive velocity
            if(specialEvent == Constants.SpecialEvent.SyncHuntedTargetPos)
            {
                _acknowledgedLastSync = true;
                if (!IsTargetPlayerLocal)
                {
                    float x = Convert.ToSingle(args[0]), y = Convert.ToSingle(args[1]), z = Convert.ToSingle(args[2]), velX = Convert.ToSingle(args[3]), velY = Convert.ToSingle(args[4]), velZ = Convert.ToSingle(args[5]);
                    Vector3 newPos = new Vector3(x, y, z);
                    if (_hasReceivedRemoteTargetPos)
                    {
                        // average the velocity
                        // _lastRemoteTargetVelocity = (_lastRemoteTargetVelocity + new Vector3(velX, velY, velZ)) * .5f;
                        // TODO: we should track like, 4-6 last velocities and average them - i think that's the only way we can make it smooth for remote clients
                        _lastRemoteTargetVelocity = new Vector3(velX, velY, velZ);
                    }
                    _lastSyncedRemoteTargetPos = newPos;
                    _lastInterpolatedRemoteTargetPos = newPos;
                }
                _secondsSinceRemoteTargetSync = 0f;
                _hasReceivedRemoteTargetPos = true;
            }
        }
    }
}
