using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Models.UI;
using SurviveTheHuntClient.Models.XmasModifier;
using SurviveTheHuntShared.Core;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public partial class MainScript
    {
        [EventHandler(SurviveTheHuntShared.Events.Client.XmasReceivePresentsLocations)]
        public void ReceivePresentsLocations(List<object> locationIndices)
        {
            ExecutePlugins(plugin =>
            {
                if(plugin is Helpers.XmasModifier xmasModifier)
                {
                    PrezzieLocation[] locations = new PrezzieLocation[locationIndices.Count];
                    for(int i = 0; i < locations.Length; i++)
                    {
                        locations[i] = Helpers.XmasModifier.Constants.PresentsLocations[(int)locationIndices[i]];
                    }
                    xmasModifier.OnPresentsLocationsReceived(locations);
                }
            });
        }

        [EventHandler(SurviveTheHuntShared.Events.Client.XmasReceiveDeliveryUpdate)]
        public void ReceiveXmasDeliveryUpdate(List<object> deliveredIndices, List<object> remainingIndices)
        {
            bool done = false;

            Debug.WriteLine($"Received delivery update: {deliveredIndices.Count} delivered, {remainingIndices.Count} remaining");

            int[] deliveredIndicesInts = new int[deliveredIndices.Count];
            for(int i = 0; i < deliveredIndicesInts.Length; i++) 
            { 
                deliveredIndicesInts[i] = (int)deliveredIndices[i]; 
            }

            int[] remainingIndicesInts = new int[remainingIndices.Count];
            for (int i = 0; i < remainingIndicesInts.Length; i++)
            {
                remainingIndicesInts[i] = (int)remainingIndices[i];
            }

            ExecutePlugins(plugin =>
            {
                if(!done && plugin is Helpers.XmasModifier xmasModifier)
                {
                    done = true;
                    xmasModifier.OnDeliveryUpdate(deliveredIndicesInts, remainingIndicesInts, ref GameState);
                }
            });
        }

        [EventHandler(SurviveTheHuntShared.Events.Client.XmasReceiveCapturableUpdate)]
        public void ReceiveXmasHuntedCapturableUpdate(int huntedServerId, bool isCapturable)
        {
            bool done = false;

            ExecutePlugins(plugin =>
            {
                if(!done && plugin is Helpers.XmasModifier xmasModifier)
                {
                    done = true;
                    xmasModifier.OnCapturableUpdate(GetPlayerFromServerId(huntedServerId), isCapturable);
                }
            });
        }

        [EventHandler(SurviveTheHuntShared.Events.Client.XmasReceiveHuntedCaptured)]
        public void ReceiveXmasHuntedCaptured(int capturedServerId)
        {
            bool done = false;

            ExecutePlugins(plugin =>
            {
                if (!done && plugin is Helpers.XmasModifier xmasModifier)
                {
                    done = true;
                    xmasModifier.OnHuntedWasCaptured(GetPlayerFromServerId(capturedServerId), ref PlayerState);
                }
            });
        }

        [EventHandler(SurviveTheHuntShared.Events.Client.XmasReceiveSleighSpawn)]
        public void ReceiveXmasSleighSpawn(Int64 sleighServerIdsPair)
        {
            int oppressorNetId = (int)(sleighServerIdsPair >> (sizeof(int) * 8));
            int sleighNetId = (int)sleighServerIdsPair;

            Debug.WriteLine($"{nameof(ReceiveXmasSleighSpawn)}({nameof(oppressorNetId)}: {oppressorNetId}, {nameof(sleighNetId)}: {sleighNetId})");
            
            bool done = false;

            Helpers.XmasModifier xmasModifier = null;

            ExecutePlugins(plugin =>
            {
                if (!done && plugin is Helpers.XmasModifier xmasModifierTemp)
                {
                    xmasModifier = xmasModifierTemp;
                    done = true;
                    xmasModifier.OnSleighSpawned(oppressorNetId: oppressorNetId, sleighNetId: sleighNetId);
                }
            });

            // I don't know if FiveM is guaranteed to have the net IDs for entities synced as soon as they are created so allow 5s for clients to catch up
            const float netUpdateTimeoutSeconds = 5f;
            float elapsedSeconds = 0f;
            if (xmasModifier != null)
            {
                Models.DynamicTickable netIdAwaiter = new Models.DynamicTickable((Models.DynamicTickable instance, float deltaTime) =>
                {
                    elapsedSeconds += deltaTime;
                    bool exists = NetworkDoesEntityExistWithNetworkId(oppressorNetId);

                    if (exists)
                    {
                        int oppressorHandle = NetToVeh(oppressorNetId);
                        Debug.WriteLine($"NetToVeh({nameof(oppressorNetId)}: {oppressorNetId}) = {oppressorHandle}");
                        xmasModifier.InvisibleEntities.Add(oppressorHandle);
                    }

                    // Remove the tickable once we're done
                    if(exists || elapsedSeconds > netUpdateTimeoutSeconds)
                    {
                        TickablesToRemove.Add(instance);
                    }
                });
                Tickables.Add(netIdAwaiter);
            }
        }

        [EventHandler(SurviveTheHuntShared.Events.Client.XmasReceiveSantaSpawn)]
        public void ReceiveXmasSantaSpawn(int santaSpawnIndex)
        {
            bool done = false;

            ExecutePlugins(plugin =>
            {
                if (!done && plugin is Helpers.XmasModifier xmasModifier)
                {
                    done = true;
                    xmasModifier.OnSantaSpawnReceived(Helpers.XmasModifier.Constants.SantaSpawnLocations[santaSpawnIndex]);
                }
            });
        }
    }
}

namespace SurviveTheHuntClient.Helpers
{
    internal sealed partial class XmasModifier : Plugin, ITickable
    {
        private static int SpawnVehicle(uint vehicleHash, Vector3 position, VehicleColor? colour = null)
        {
            int vehicle = CreateVehicle(vehicleHash, position.X, position.Y, position.Z, GetEntityHeading(PlayerPedId()), true, false);
            
            if(colour != null)
            {
                SetVehicleColours(vehicle, (int)colour.Value, (int)colour.Value);
            }

            return vehicle;
        }

        private readonly PresentSpawnerCollection _presentSpawners = new PresentSpawnerCollection
        {
            {
                Constants.PrezzieLocationTag.Chimney, new PresentSpawner(location =>
                {
                    Vector3 pos = location.Position;
                    int presentProp = CreateObject(s_PresentPropHashKey, pos.X, pos.Y, pos.Z, true, false, false);
                    SetEntityCompletelyDisableCollision(presentProp, false, true);
                    return presentProp;
                }, new uint[] { (uint)s_PresentPropHashKey })
            },

            {
                Constants.PrezzieLocationTag.VespucciBagger, new PresentSpawner(location => SpawnVehicle((uint)VehicleHash.Bagger, location.Position, VehicleColor.MetallicGreen), new uint[] { (uint)VehicleHash.Bagger })
            },

            {
                Constants.PrezzieLocationTag.Gerald, new PresentSpawner(location => SpawnVehicle((uint)VehicleHash.Bmx, location.Position, VehicleColor.MatteRed), new uint[] { (uint)VehicleHash.Bmx })
            }
        };

        private class SleighState
        {
            internal int PropHandle;
            internal int OppressorHandle;
            internal int Blip;
            internal bool NeedsNotification = false;
        }

        private SleighState _primarySleigh;
        private SleighState _backupSleigh;

        private enum SleighSpawnState
        {
            DontSpawn,
            NeedPrimary,
            NeedBackup
        }

        private SleighSpawnState _sleighSpawnState = SleighSpawnState.DontSpawn;

        private static int s_SleighHashKey = 0;
        private static int s_OppressorHashKey = 0;

        private static int s_PresentPropHashKey = 0;
        private WeaponHash _snowballHashKey = WeaponHash.Snowball;

        public const int SleighBlipId = 748; // "radar_kart_modern"
        public const int PresentsBlipId = 835; // "radar_community_series"

        public readonly byte[][] PresentColours =
        {
            // red
            new byte[] { 255, 0, 0, 255 },

            // green
            new byte[] { 0, 255, 0, 255 },

            // gold
            new byte[] { 230, 212, 55, 255 }
        };

        private int[] _presentBlips = { };

        /// <summary>
        /// Prevent an infinite loop of <see cref="SurviveTheHuntShared.Events.Server.ReceiveHuntedClock"/> events.
        /// </summary>
        private bool _timeSet = false;

        public bool IsHunted;
        private int _huntedPlayerId;

        private bool _hasStarted;
        public bool HasStarted => _hasStarted;

        public const float SleighStoreActivationDistance = 6.5f;
        private const float _sleighStoreActivationDistanceSq = SleighStoreActivationDistance * SleighStoreActivationDistance;

        private const string StoreSleighHelpLabelName = "STHXMASSTORESLEIGH";
        private const string StoreSleighHelpText = "Press ~INPUT_CONTEXT~ to store the Pole-Rider.";

        private const string RestoreSleighHelpLabelName = "STHXXMASRECOVERSLEIGH";
        private const string RestoreSleighHelpText = "Press ~INPUT_CONTEXT~ to recover the Pole-Rider.";

        private const string PlacePresentHelpLabelName = "STHXXMASPLACEPREZ";
        private const string PlacePresentHelpText = "Press ~INPUT_CONTEXT~ to place down the present when near the marker.\nAlternatively, press ~INPUT_AIM~ to aim and ~INPUT_ATTACK~ to throw the present from further away.";

        private readonly List<PrezzieState> _presentsToDeliver = new List<PrezzieState>();
        private readonly List<int> _presentProps = new List<int>();

        internal const byte MaxPresentsLocations = 12;
        internal const float MinDistanceBetweenPresents = 220f;

        private static readonly Random s_RNG = new Random(DateTime.UtcNow.Millisecond);

        private int _handPresentProp = 0;
        private Vector3 _handPresentOffset = new Vector3(0.12f, 0f, 0f);
        private Vector3 _handPresentRotEuler = new Vector3(0f, -90f, 0f);

        internal const string SleighVehicleName = "ClausTech Pole-Rider";

        private List<int> _capturablePlayers = new List<int>();

        private const string CapturableHelpLabelName = "STHXXMASRAGDOLLED";
        private const string CapturableHelpText = "When you fall over, you will take a long time to get up.\nHunters are able to apprehend you in this state.";
        private const string ApprehendHelpLabelName = "STHXXMASCAPTURE";
        private const string ApprehendHelpText = "Press ~INPUT_CONTEXT~ to capture Santa.";

        private bool _pendingFirstCapturableHelpText = true;

        private Vector3 _sleighSpawnPos = Vector3.Zero;

        /// <summary>
        /// Network IDs of objects to remove
        /// </summary>
        private List<int> _objectsToCleanup = new List<int>();

        /// <summary>
        /// Network IDs of vehicles to remove
        /// </summary>
        private List<int> _vehiclesToCleanup = new List<int>();

        /// <summary>
        /// Network IDs of Mk2s that Santa gets
        /// </summary>
        private List<int> _sleighOppressors = new List<int>();

        /// <summary>
        /// Handles of entities that need to be made invisible every frame.
        /// </summary>
        internal List<int> InvisibleEntities = new List<int>();

        internal partial class Constants
        {
            public const string PlacingDownPresentAnimDict = "anim@MP_FIREWORKS";
            public const string PlacingDownPresentAnimClip = "PLACE_FIREWORK_BOX2";

            public const string WakeUpAnimDict = "anim@scripted@heist@ig25_beach@male@";
            public const string WakeUpAnimClip = "action";
        }

        internal PlayerState PlayerState;
        internal GameState GameState;

        private bool _waitingToTeleportToSpawn = false;
        private SpawnLocation _santaSpawnLocation = null;

        private int _introCam = 0;
        private int _introSleigh = 0;
        private Vector3 _introSleighInitialPos = Vector3.Zero;

        private uint _deliveredCounter = 0;
        private bool _hasDeliveredAll = false;
        private bool _wasCaptured = false;

        internal override Teams.Team? WinningTeamOverride => IsHunted && !_wasCaptured && _hasDeliveredAll ? Teams.Team.Hunted : Teams.Team.Hunters;

        internal override SurviveTheHuntShared.Utils.Coord[] CarSpawnPointsOverride
        {
            get
            {
                SurviveTheHuntShared.Utils.Coord[] defaultSpawns = SurviveTheHuntShared.Constants.CarSpawnPoints;
                SurviveTheHuntShared.Utils.Coord[] reducedSpawns = new SurviveTheHuntShared.Utils.Coord[(int)Math.Floor(defaultSpawns.Length / 2.0)];

                for(int i = 0; i < reducedSpawns.Length; i++)
                {
                    reducedSpawns[i] = defaultSpawns[i * 2];
                }

                return reducedSpawns;
            }
        }

        private class TutorialState
        {
            internal TextSequenceItem[] Items = new TextSequenceItem[0];
            internal int CurrentItemIndex = 0;
            internal float CurrentItemTimeElapsed = 0f;
            internal float CurrentItemTimeTarget => Items.Length > CurrentItemIndex ? Items[CurrentItemIndex].TimeInSeconds : float.MaxValue;

            internal bool HasFinished => CurrentItemIndex >= Items.Length;
        }

        private TutorialState _tutorialState = new TutorialState();

        /// <summary>
        /// Hide the ping if santa is not currently using a sleigh
        /// </summary>
        internal override bool CanPingShow {
            get
            {
                bool anyPresentsRemaining = false;
                foreach (PrezzieState present in _presentsToDeliver)
                {
                    if (!present.HasPlaced)
                    {
                        anyPresentsRemaining = true;
                        break;
                    }
                }

                int huntedPlayerPed = GetPlayerPed(_huntedPlayerId);
                if (!DoesEntityExist(huntedPlayerPed))
                {
                    return true;
                }

                return !anyPresentsRemaining || _sleighOppressors.Contains(VehToNet(GetVehiclePedIsIn(GetPlayerPed(_huntedPlayerId), false)));
            }
        }

        internal override string CustomWastedText {
            get
            {
                string[] customWastedLines =
                {
                    "JOLLIED",
                    "EGGNOGGED",
                    "NAUGHTY-LISTED",
                    "CAROLLED"
                };

                return customWastedLines[s_RNG.Next(customWastedLines.Length)];
            }
        }

        internal override bool SkipAddingPlayerNameInObjective => true;

        internal override LabelledItem[] UICurrentItems => (_hasDeliveredAll || !HasStarted) ? new LabelledItem[0] : new LabelledItem[]
        {
            new LabelledItem("DELIVERED", $"{_deliveredCounter}/{_presentBlips.Length}")
        };

        private List<DynamicTickable> _dynamicTickables = new List<DynamicTickable>();
        private List<DynamicTickable> _tickablesToRemove = new List<DynamicTickable>();

        internal override bool? IsVehicleWeaponAllowed(int vehicleHandle, uint weapon)
        {
            uint modelHash = (uint)GetEntityModel(vehicleHandle);
            const uint HalfTrackGunHash = 1226518132;
            if (modelHash == (uint)VehicleHash.Rhino || modelHash == (uint)VehicleHash.TrailerSmall2 || (modelHash == (uint)VehicleHash.HalfTrack && (weapon == HalfTrackGunHash || weapon == 0)))
            {
                return true;
            }

            return false;
        }

        private bool _waitingForClothesChange = false;
        internal const float ClothesChangeDelaySeconds = 2f;
        private float _clothesChangeTimer = 0f;

        internal override void OnPlayerSpawned()
        {
            if(HasStarted)
            {
                _waitingForClothesChange = true;
                _clothesChangeTimer = 0f;
            }
        }

        internal XmasModifier(TriggerEventProxyDelegate triggerEventProxy, TriggerServerEventProxyDelegate triggerServerEventProxy, ref PlayerState playerState) : base(triggerEventProxy, triggerServerEventProxy)
        {
            PlayerState = playerState;
            Init();
        }

        private void Init()
        {
            _primarySleigh = null;
            _backupSleigh = null;
            _handPresentProp = 0;
            _huntedPlayerId = -1;
            _sleighSpawnState = SleighSpawnState.DontSpawn;
            _presentBlips = new int[] { };
            _hasThrownSnowball = false;
            _presentState = PresentPlacementState.TooFar;
            _pendingFirstCapturableHelpText = true;
            IsHunted = false;
            _timeSet = false;
            _santaSpawnLocation = null;
            _waitingToTeleportToSpawn = false;
            _introSleigh = 0;
            _introCam = 0;
            _currentIntroStage = IntroSequenceStage.WaitingToStart;
            _prevIntroStage = IntroSequenceStage.WaitingToStart;
            _introTimerActive = false;
            _introCurrentTimer = 0f;
            _introTimeElapsed = 0f;
            _introCurrentTargetTime = float.MaxValue;
            _hasDeliveredAll = false;
            _wasCaptured = false;
            _deliveredCounter = 0;
            _hasPickedUpAmmoLastTick = false;

            _tutorialState = new TutorialState();

            _sleighSpawnPos = Vector3.Zero;

            _capturablePlayers.Clear();

            AddTextEntry(StoreSleighHelpLabelName, StoreSleighHelpText);
            AddTextEntry(RestoreSleighHelpLabelName, RestoreSleighHelpText);
            AddTextEntry(PlacePresentHelpLabelName, PlacePresentHelpText);
            AddTextEntry(CapturableHelpLabelName, CapturableHelpText);
            AddTextEntry(ApprehendHelpLabelName, ApprehendHelpText);
            AddTextEntry("OPPRESSOR2", SleighVehicleName);
        }

        internal override bool DoesPlayerNeedInvincibility => IsHunted;

        public void OnPresentsLocationsReceived(PrezzieLocation[] presents)
        {
            Debug.WriteLine($"Received {presents.Length} presents locations");
            _presentsToDeliver.Capacity = presents.Length;
            int colourDivisor = PresentColours.Length;
            int counter = 0;
            foreach (PrezzieLocation presentLocation in presents)
            {
                byte[] colour = PresentColours[counter++ % colourDivisor];
                _presentsToDeliver.Add(new PrezzieState(presentLocation, colour));
            }
            _presentProps.Capacity = presents.Length;
            _presentProps.TrimExcess();

            _presentBlips = new int[presents.Length];
            AddTextEntry("PREZZIE_LABEL", "Present Delivery");
            for (int i = 0; i < presents.Length; i++)
            {
                _presentBlips[i] = AddBlipForCoord(presents[i].Position.X, presents[i].Position.Y, presents[i].Position.Z);
                SetBlipSprite(_presentBlips[i], PresentsBlipId);
                SetBlipNameFromTextFile(_presentBlips[i], "PREZZIE_LABEL");
                byte[] colour = _presentsToDeliver[i].Rgba;
                int packedRgba = SurviveTheHuntShared.Utils.EncodingHelper.HexFromRgba(colour[0], colour[1], colour[2], colour[3]);
                SetBlipColour(_presentBlips[i], packedRgba);

                // Hide all but the first presents from the hunted player's radar - they should go through them in order, to allow hunters to set up traps
                if (IsHunted && i != 0)
                {
                    SetBlipDisplay(_presentBlips[i], 0);
                }
            }
        }

        public void OnDeliveryUpdate(int[] deliveredIndices, int[] remainingIndices, ref GameState gameState)
        {
            PlaySoundFrontend(-1, "RACE_PLACED", "HUD_AWARDS", true);
            int greyBlip = SurviveTheHuntShared.Utils.EncodingHelper.HexFromRgba(96, 96, 96, 255);

            foreach (int deliveredIndex in deliveredIndices)
            {
                if (deliveredIndex < _presentBlips.Length && DoesBlipExist(_presentBlips[deliveredIndex]))
                {
                    SetBlipColour(_presentBlips[deliveredIndex], greyBlip);

                    _presentsToDeliver[deliveredIndex].HasPlaced = true;
                }
            }

            bool needsObjectiveUpdate = true;
            int nextUpIndex = -1;

            if (remainingIndices.Length > 0)
            {
                nextUpIndex = remainingIndices[0];

                if (nextUpIndex < _presentBlips.Length)
                {
                    SetBlipDisplay(_presentBlips[nextUpIndex], 2);
                }

                needsObjectiveUpdate = IsHunted;
            }
            else
            {
                _hasDeliveredAll = true;
            }

            _deliveredCounter++;

            if (needsObjectiveUpdate)
            {
                gameState.CurrentObjective = GenerateObjectiveText(IsHunted, _huntedPlayerId, remainingIndices.Length > 0, nextUpIndex);
                HuntUI.DisplayObjective(ref gameState, ref PlayerState, skipAddingHuntedName: true);
            }

            if(_hasDeliveredAll)
            {
                if(IsHunted)
                {
                    const float engineHealth = -100f;
                    
                    SleighState[] sleighs = { _primarySleigh, _backupSleigh };
                    foreach (SleighState sleigh in sleighs)
                    {
                        if (sleigh != null)
                        {
                            SetVehicleEngineHealth(sleigh.OppressorHandle, engineHealth);
                            SetBlipAlpha(sleigh.Blip, 0);
                        }
                    }

                    const string sleighDestroyedText = "STH_XMAS_SLEIGHNOTALLOWED";
                    AddTextEntry(sleighDestroyedText, "Your Pegassi ClausTech free trial has run out. Your Pole-Rider will now deactivate.");
                    BeginTextCommandDisplayHelp(sleighDestroyedText);
                    EndTextCommandDisplayHelp(0, false, true, 5000);
                }
            }
        }

        private string GenerateObjectiveText(bool isHunted, int huntedPlayerId, bool anyPresentsRemaining, int presentIndex)
        {
            string huntersObjectiveBase = $"~y~{GetPlayerName(huntedPlayerId)}~w~ is {((uint)GetEntityModel(GetPlayerPed(huntedPlayerId)) == (uint)PedHash.FreemodeFemale01 ? "Mrs." : "Santa")} Claus!";

            if (anyPresentsRemaining)
            {
                if (isHunted)
                {
                    bool isOdd = (presentIndex % 2) == 1;
                    byte[] nextUpColour = PresentColours[presentIndex % PresentColours.Length];
                    if (!isOdd)
                    {
                        SetScriptVariableHudColour(nextUpColour[0], nextUpColour[1], nextUpColour[2], nextUpColour[3]);
                    }
                    else
                    {
                        SetScriptVariable_2HudColour(nextUpColour[0], nextUpColour[1], nextUpColour[2], nextUpColour[3]);
                    }
                }

                return isHunted ? $"Deliver the ~{((presentIndex % 2) == 1 ? 'u' : 'v')}~presents~w~. Don't let the elves capture you." : $"{huntersObjectiveBase} Stop them from delivering the presents.";
            }
            else
            {
                return isHunted ? "Survive." : $"{huntersObjectiveBase} Hunt them down.";
            }
        }

        public void OnCapturableUpdate(int playerHandle, bool isCapturable)
        {
            if(isCapturable && !_capturablePlayers.Contains(playerHandle))
            {
                _capturablePlayers.Add(playerHandle);
            }
            else if(!isCapturable)
            {
                _capturablePlayers.Remove(playerHandle);
            }

            // On first ragdoll, explain the apprehend mechanic to the hunted player.
            if (isCapturable && _pendingFirstCapturableHelpText && playerHandle == PlayerId())
            {
                _pendingFirstCapturableHelpText = false;
                BeginTextCommandDisplayHelp(CapturableHelpLabelName);
                EndTextCommandDisplayHelp(0, false, true, 15 * 1000);
            }
        }

        public void OnHuntedWasCaptured(int playerHandle, ref PlayerState playerState)
        {
            if(_capturablePlayers.Contains(playerHandle))
            {
                _capturablePlayers.Remove(playerHandle);
            }

            if(PlayerId() == playerHandle)
            {
                SetEntityHealth(PlayerPedId(), 0);
                playerState.ReportDeathNextTick = true;
                _wasCaptured = true;
            }
        }

        public void OnSleighSpawned(int oppressorNetId, int sleighNetId)
        {
            _objectsToCleanup.Add(sleighNetId);
            _vehiclesToCleanup.Add(oppressorNetId);
            _sleighOppressors.Add(oppressorNetId);
        }

        internal static PrezzieLocation[] GetRandomPrezzieLocations(byte maxPresents = MaxPresentsLocations, IEnumerable<PrezzieLocation> prezzies = default)
        {
            if(prezzies == default || prezzies == null)
            {
                prezzies = Constants.PresentsLocations;
            }

            List<PrezzieLocation> remainingLocations = new List<PrezzieLocation>(prezzies);
            List<PrezzieLocation> discardedLocations = new List<PrezzieLocation>((int)Math.Ceiling(maxPresents / 4.0f));

            List<PrezzieLocation> locationsPicked = new List<PrezzieLocation>(maxPresents);

            const float minDistanceBetweenPresentsSq = MinDistanceBetweenPresents * MinDistanceBetweenPresents;

            int discardedUsedCount = 0;

            do
            {
                if (remainingLocations.Count > 0)
                {
                    int randomIndex = s_RNG.Next(remainingLocations.Count);
                    bool discard = false;

                    PrezzieLocation randomLocation = remainingLocations[randomIndex];

                    foreach (PrezzieLocation pickedLocation in locationsPicked)
                    {
                        if (pickedLocation.Position.DistanceToSquared(randomLocation.Position) < minDistanceBetweenPresentsSq)
                        {
                            discard = true;
                            break;
                        }
                    }

                    if (discard)
                    {
                        discardedLocations.Add(randomLocation);
                    }
                    else
                    {
                        locationsPicked.Add(randomLocation);
                    }

                    remainingLocations.RemoveAt(randomIndex);
                }
                else
                {
                    int randomIndex = s_RNG.Next(discardedLocations.Count);
                    locationsPicked.Add(discardedLocations[randomIndex]);
                    discardedLocations.RemoveAt(randomIndex);
                    discardedUsedCount++;
                }
            } while (locationsPicked.Count < maxPresents && (remainingLocations.Count > 0 || discardedLocations.Count > 0));

            Debug.WriteLine($"Found {locationsPicked.Count} presents locations. {discardedUsedCount} of them may be too close to other presents.");

            return locationsPicked.ToArray();
        }

        private void BroadcastRandomSpawnLocationForSanta()
        {
            TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.XmasBroadcastSantaSpawn, s_RNG.Next(Constants.SantaSpawnLocations.Length));
        }

        private bool _waitingTillSafeZone = false;

        internal override void OnHuntStarted(GameState gameState, PlayerState playerState)
        {
            GameState = gameState;

            SetRunSprintMultiplierForPlayer(PlayerId(), 1f);

            _hasStarted = true;
            _huntedPlayerId = gameState.Hunt.HuntedPlayer.Handle;
            IsHunted = _huntedPlayerId == PlayerId();

            // Pick a random spawn location for santa
            if(IsHunted)
            {
                BroadcastRandomSpawnLocationForSanta();
                _waitingTillSafeZone = !playerState.IsInSafeZone;
            }

            s_PresentPropHashKey = GetHashKey("xm3_prop_xm3_present_01a");
            RequestModel((uint)s_PresentPropHashKey);

            if (IsHunted)
            {
                // Spawn a special oppressor for the hunted player.
                s_SleighHashKey = GetHashKey("m23_2_prop_m32_sleigh_01a");
                RequestModel((uint)s_SleighHashKey);

                s_OppressorHashKey = GetHashKey("oppressor2");
                RequestModel((uint)s_OppressorHashKey);

                PrezzieLocation[] randomPrezzies = GetRandomPrezzieLocations();
                int[] prezzieIndices = new int[randomPrezzies.Length];
                for(int i = 0; i < prezzieIndices.Length; i++)
                {
                    for (int j = 0; j < Constants.PresentsLocations.Length; j++)
                    { 
                        PrezzieLocation location = Constants.PresentsLocations[j];
                        if(location == randomPrezzies[i])
                        {
                            prezzieIndices[i] = j;
                            break;
                        }
                    }
                }

                Debug.WriteLine($"Sending presents {prezzieIndices}");

                TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.XmasBroadcastPresentsLocations, prezzieIndices);
            }

            SetPlayerClothing(IsHunted);
            SetPlayerWeapons(IsHunted);

            gameState.CurrentObjective = GenerateObjectiveText(IsHunted, _huntedPlayerId, true, 0);
            HuntUI.DisplayObjective(ref gameState, ref playerState, skipAddingHuntedName: true);

            _tutorialState = new TutorialState()
            {
                Items = Constants.UI.TutorialSequences[playerState.Team]
            };
        }

        internal override void OnHuntEnded(GameState gameState, PlayerState playerState)
        {
            base.OnHuntEnded(gameState, playerState);

            Cleanup();
        }

        internal void SetPlayerWeapons(bool isSanta)
        {
            int playerPed = PlayerPedId();

            if(isSanta)
            {
                RemoveAllPedWeapons(playerPed, true);
                GiveWeaponToPed(playerPed, (uint)WeaponHash.Snowball, 9999, true, true);
            }
        }
        
        internal void SetPlayerClothing(bool isSanta)
        {
            int pedId = PlayerPedId();

            //if(isSanta)
            {
                ClearAllPedProps(pedId);
                List<int> ignoredComps = new List<int>{ (int)PedComponents.Hair, (int)PedComponents.Face };
                for (int i = 0; i <= 11; i++)
                {
                    if(!ignoredComps.Contains(i))
                    {
                        SetPedComponentVariation(pedId, i, 0, 0, 0);
                    }
                }

                Constants.PedOutfit outfit = isSanta ? Constants.MPMaleSantaOutfit : Constants.MPMaleElfOutfit;

                if(!IsPedMale(pedId) || (PedHash)GetEntityModel(pedId) == PedHash.FreemodeFemale01)
                {
                    outfit = isSanta ? Constants.MPFemaleSantaOutfit : Constants.MPFemaleElfOutfit;
                }

                foreach(KeyValuePair<PedComponents, Constants.PedVariation> comp in outfit.ComponentsToApply)
                {
                    if(!ignoredComps.Contains((int)comp.Key))
                    {
                        SetPedComponentVariation(pedId, (int)comp.Key, comp.Value.Drawable, comp.Value.Texture, 0);
                    }
                }

                foreach (KeyValuePair<PedProps, Constants.PedVariation> comp in outfit.PropsToApply)
                {
                    SetPedPropIndex(pedId, (int)comp.Key, comp.Value.Drawable, comp.Value.Texture, true);
                }
            }
            //else
            {
                // TODO: elf randomisation logic
            }
        }

        internal override void OnResourceStopping()
        {
            base.OnResourceStopping();

            Cleanup();
        }

        internal void Cleanup()
        {
            _hasStarted = false;

            SleighState[] sleighs = { _primarySleigh, _backupSleigh };
            foreach(SleighState sleigh in sleighs)
            {
                if((sleigh?.OppressorHandle ?? 0) != 0)
                {
                    SetEntityAsMissionEntity(sleigh.OppressorHandle, true, true);
                    DeleteEntity(ref sleigh.OppressorHandle);
                }
                if((sleigh?.PropHandle ?? 0) != 0)
                {
                    DeleteObject(ref sleigh.PropHandle);
                }
            }

            if (_handPresentProp != 0)
            {
                DeleteObject(ref _handPresentProp);
            }

            foreach(int blip in _presentBlips)
            {
                if(DoesBlipExist(blip))
                {
                    int blipCopy = blip;
                    RemoveBlip(ref blipCopy);
                }
            }

            for(int i = 0; i < _presentProps.Count; i++)
            {
                int presentEntity = _presentProps[i];
                if(DoesEntityExist(presentEntity))
                {
                    DeleteEntity(ref presentEntity);
                    // dunno why i bother, but hey if the native insists on passing a ref then surely it does something interesting with it right...
                    _presentProps[i] = presentEntity;
                }
            }

            foreach(int objNetId in _objectsToCleanup)
            {
                if(NetworkDoesEntityExistWithNetworkId(objNetId))
                {
                    int objHandle = NetToObj(objNetId);
                    DeleteObject(ref objHandle);
                }
            }
            
            foreach(int vehNetId in _vehiclesToCleanup)
            {
                if(NetworkDoesEntityExistWithNetworkId(vehNetId))
                {
                    int handle = NetToVeh(vehNetId);
                    SetEntityAsMissionEntity(handle, true, true);
                    DeleteEntity(ref handle);
                }
            }

            if(_introCam != 0)
            {
                DestroyCam(_introCam, true);
            }

            if(_introSleigh != 0 && DoesEntityExist(_introSleigh))
            {
                DeleteVehicle(ref _introSleigh);
                _introSleigh = 0;
            }

            _objectsToCleanup.Clear();
            _vehiclesToCleanup.Clear();
            _sleighOppressors.Clear();

            _presentsToDeliver.Clear();

            ClearAllHelpMessages();

            if(_sleighStorageVehicleHandle != 0)
            {
                SetEntityAsNoLongerNeeded(ref _sleighStorageVehicleHandle);
                _sleighStorageVehicleHandle = 0;
            }

            // TODO: maybe include a step for cleaning up oppressors that have changed ownership after a client left

            Init();
        }

        private void SpawnSleigh(out SleighState state, bool isBackup = false)
        {
            Debug.WriteLine($"Spawning a {(isBackup ? "backup" : "primary")} sleigh.");

            Vector3 spawnPos = _sleighSpawnPos;

            state = new SleighState();

            state.NeedsNotification = true;

            state.OppressorHandle = CreateVehicle((uint)s_OppressorHashKey, spawnPos.X, spawnPos.Y, spawnPos.Z, Player.Local.Character.Heading + 90f, true, true);
            state.PropHandle = CreateObject(s_SleighHashKey, spawnPos.X, spawnPos.Y, spawnPos.Z, true, false, false);
            SetEntityHasGravity(state.PropHandle, false);
            SetEntityCompletelyDisableCollision(state.PropHandle, false, false);
            SetEntityHeading(state.PropHandle, GetEntityHeading(state.OppressorHandle) - 90f);
            Vector3 sleighPos = spawnPos + new Vehicle(state.OppressorHandle).UpVector * -0.5f;
            AttachEntityToEntity(state.PropHandle, state.OppressorHandle, 0, 0f, -0.75f, -0.5f, 0f, 0f, -90f, false, false, false, false, 0, true);

            SetEntityAlpha(state.OppressorHandle, 0, 1);

            state.Blip = AddBlipForEntity(state.OppressorHandle);
            SetBlipSprite(state.Blip, SleighBlipId);
            AddTextEntry("SLEIGH_NAME", SleighVehicleName);
            SetBlipNameFromTextFile(state.Blip, "SLEIGH_NAME");

            // Mark backup sleighs with a light gray colour
            if(isBackup)
            {
                SetBlipColour(state.Blip, 39);
            }

            Int64 oppressorNetId = VehToNet(state.OppressorHandle);
            Int64 propNetId = ObjToNet(state.PropHandle);
            Debug.WriteLine($"sizeof({nameof(Int32)}): {sizeof(int)}");
            Int64 netIdsPacked = (oppressorNetId << (sizeof(int) * 8)) | propNetId;
            Debug.WriteLine($"Sending {SurviveTheHuntShared.Events.Server.XmasBroadcastSleighSpawn} with {nameof(oppressorNetId)}={oppressorNetId} and {nameof(propNetId)}={propNetId}. Packed = {netIdsPacked} (hex: {netIdsPacked:X})");
            TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.XmasBroadcastSleighSpawn, netIdsPacked);
        }

        /// <summary>
        /// Checks the player's vicinity for vehicles that can store the sleigh. 
        /// </summary>
        /// <returns>Non-zero handle if a storage vehicle was found.</returns>
        internal int GetNearestSleighStorage(out SleighStorageVehicle vehicleInfo)
        {
            Vector3 playerPos = Player.Local.Character.Position;
            Vector3 playerFwdVec = Player.Local.Character.ForwardVector;

            Vehicle[] vehicles = World.GetAllVehicles();

            //int nearestVehicle = GetClosestVehicle(searchOrigin.X, searchOrigin.Y, searchOrigin.Z, SleighStoreActivationDistance * 0.49f, 0, 1048576 | 2097152);

            vehicleInfo = null;

            Vehicle nearestVehicle = null;
            float shortestDistanceSq = float.MaxValue;
            float lowestDotDiff = float.MaxValue;

            foreach(Vehicle vehicle in vehicles)
            {
                if(vehicle.Handle == _primarySleigh?.OppressorHandle || vehicle.Handle == _backupSleigh?.OppressorHandle)
                {
                    continue;
                }

                float distanceSq = vehicle.Position.DistanceToSquared(playerPos);
                if (distanceSq < _sleighStoreActivationDistanceSq && distanceSq < shortestDistanceSq)
                {
                    Vector3 dir = vehicle.Position - playerPos;
                    dir.Normalize();
                    float dot = SurviveTheHuntShared.Utils.Vector3.Dot(dir.X, dir.Y, dir.Z, playerFwdVec.X, playerFwdVec.Y, playerFwdVec.Z);
                    const float minDot = 0.8f;
                    // Check we're facing the same direction as the vehicle
                    if(dot > minDot)
                    {
                        float dotDiff = dot - minDot;
                        if(dotDiff < lowestDotDiff)
                        {
                            nearestVehicle = vehicle;
                            shortestDistanceSq = distanceSq;
                            lowestDotDiff = dotDiff;
                        }
                    }
                }
            }

            // Check that a vehicle was found
            if (nearestVehicle != null)
            {
                VehicleHash vehicleHash = (VehicleHash)GetEntityModel(nearestVehicle.Handle);
                foreach (SleighStorageVehicle candidate in Constants.SleighStorageVehicles)
                {
                    if (candidate.Hash == vehicleHash)
                    {
                        vehicleInfo = candidate;
                        return nearestVehicle.Handle;
                    }
                }
            }

            return 0;
        }

        private bool _isShowingStoreSleighTooltip = false;
        private bool _isShowingRestoreSleighTooltip = false;
        private int _sleighStorageVehicleHandle = 0;

        /// <summary>
        /// Interval for scanning for suitable storage vehicles;
        /// this is to reduce the frequency at which we query the game for entities as that might be expensive
        /// (since we also need to do model hash & entity distance checks on them).
        /// </summary>
        private const float CheckForStorageInterval = 0.4f;
        private float _timeSinceStorageCheck = 0f;
        
        /// <summary>
        /// Set to true once the player ped has been tasked with exiting the sleigh
        /// </summary>
        private bool _waitingToGetOff = false;

        private void ProcessSleighStorage(bool isInSleigh, int playerPed)
        {
            if (isInSleigh)
            {
                if(_sleighStorageVehicleHandle != 0 && IsControlJustReleased(0, (int)Control.Context))
                {
                    TaskLeaveVehicle(playerPed, _primarySleigh.OppressorHandle, 0);
                    _waitingToGetOff = true;
                }

                // Don't look for nearby vehicles after we've tasked the player ped with leaving the vehicle - we're already committed to storing the sleigh on that one
                if (!_waitingToGetOff)
                {
                    if (_timeSinceStorageCheck > CheckForStorageInterval)
                    {
                        _sleighStorageVehicleHandle = GetNearestSleighStorage(out SleighStorageVehicle vehicleInfo);
                        if (_sleighStorageVehicleHandle != 0)
                        {
                            if (!_isShowingStoreSleighTooltip)
                            {
                                BeginTextCommandDisplayHelp(StoreSleighHelpLabelName);
                                EndTextCommandDisplayHelp(0, true, true, 0);
                            }

                            _isShowingStoreSleighTooltip = true;
                        }
                        else
                        {
                            if (_isShowingStoreSleighTooltip)
                            {
                                ClearAllHelpMessages();
                            }

                            _isShowingStoreSleighTooltip = false;
                        }

                        _timeSinceStorageCheck = 0f;
                    }
                }
            }
            else
            {
                if (_isShowingStoreSleighTooltip)
                {
                    ClearAllHelpMessages();
                    _isShowingStoreSleighTooltip = false;
                }

                if (_isShowingRestoreSleighTooltip && IsControlJustReleased(0, (int)Control.Context))
                {
                    Vector3 backVector = GetEntityForwardVector(_sleighStorageVehicleHandle) * -1f;
                    const float offset = 10f;
                    Vector3 newPos = GetEntityCoords(_primarySleigh.OppressorHandle, false) + backVector * offset;
                    DetachEntity(_primarySleigh.OppressorHandle, true, true);
                    SetEntityCoords(_primarySleigh.OppressorHandle, newPos.X, newPos.Y, newPos.Z, false, false, false, false);
                    SetEntityAsNoLongerNeeded(ref _sleighStorageVehicleHandle);
                    _sleighStorageVehicleHandle = 0;
                    ClearAllHelpMessages();
                    _isShowingRestoreSleighTooltip = false;
                }

                if (_waitingToGetOff)
                {
                    _waitingToGetOff = false;
                    AttachEntityToEntity(_primarySleigh.OppressorHandle, _sleighStorageVehicleHandle, 0, 0f, 0f, 0f, 0f, 0f, 0f, false, false, false, false, 0, true);
                    SetEntityAsMissionEntity(_sleighStorageVehicleHandle, true, true);
                }
                else
                {
                    if (_timeSinceStorageCheck >= CheckForStorageInterval)
                    {
                        bool wasShowingRestoreTooltip = _isShowingRestoreSleighTooltip;

                        if (_sleighStorageVehicleHandle != 0 && DoesEntityExist(_sleighStorageVehicleHandle))
                        {
                            if (GetEntityCoords(_sleighStorageVehicleHandle, false).DistanceToSquared(GetEntityCoords(playerPed, false)) <= _sleighStoreActivationDistanceSq)
                            {
                                _isShowingRestoreSleighTooltip = true;
                            }
                            else
                            {
                                _isShowingRestoreSleighTooltip = false;
                            }
                        }

                        if(wasShowingRestoreTooltip != _isShowingRestoreSleighTooltip)
                        {
                            if(_isShowingRestoreSleighTooltip)
                            {
                                BeginTextCommandDisplayHelp(RestoreSleighHelpLabelName);
                                EndTextCommandDisplayHelp(0, true, true, 0);
                            }
                            else
                            {
                                ClearAllHelpMessages();
                            }
                        }

                        _timeSinceStorageCheck = 0f;
                    }
                }
            }
        }

        private enum PresentPlacementState
        {
            TooFar,
            InRadius,
            WaitingForPlaceAnim,
            Placing,
            Placed
        }

        /// <summary>
        /// The time interval at which we'll check for present placement triggers around the player.
        /// </summary>
        private const float CheckForPresentTriggersInterval = 0.2f;
        private float _timeSincePresentTriggerCheck = 0f;
        private bool _isShowingPlacePresentTooltip = false;
        private PrezzieState _closestPrezzie = null;
        private PresentPlacementState _presentState = PresentPlacementState.TooFar;
        private bool _hasThrownSnowball = false;
        private const float SnowballThrowTimeout = 10f;
        private float _timeSinceSnowballThrow = 0f;
        private bool _startedPlayingPlaceDownAnim = false;

        private void ProcessPresentPlacement(int playerPed, Vector3 playerPos, bool inVehicle)
        {
            // Prevent placing presents while in a vehicle.
            if(inVehicle)
            {
                if(_isShowingPlacePresentTooltip)
                {
                    _isShowingPlacePresentTooltip = false;
                    ClearAllHelpMessages();
                }

                return;
            }

            uint currentWeapon = 0;

            const float maxSnowballRadius = 50f;
            int snowballHandle = 0;
            Vector3 snowballCoords = Vector3.Zero;
            bool snowballExists = GetProjectileNearPed(playerPed, (uint)WeaponHash.Snowball, maxSnowballRadius, ref snowballCoords, ref snowballHandle, false);

            if (snowballExists)
            {
                DetachEntity(_handPresentProp, false, false);
                // Make the present follow the snowball projectile so it looks like we're throwing a present box.
                SetEntityCoords(_handPresentProp, snowballCoords.X, snowballCoords.Y, snowballCoords.Z, false, false, false, false);

                _hasThrownSnowball = true;

                foreach (PrezzieState present in _presentsToDeliver)
                {
                    if (!present.HasPlaced && present.Location.Position.DistanceToSquared(snowballCoords) < present.Location.RadiusSq)
                    {
                        OnPresentPlaced(present);
                        break;
                    }
                }
            }
            else
            {
                if(_hasThrownSnowball)
                {
                    AttachPresentToHand(_handPresentProp, playerPed);
                }

                if (_timeSinceSnowballThrow > 0.7f)
                {
                    _hasThrownSnowball = false;
                    _timeSinceSnowballThrow = 0f;
                }
            }

            if (IsControlJustReleased(0, (int)Control.Attack) && GetCurrentPedWeapon(playerPed, ref currentWeapon, true) && currentWeapon == (uint)WeaponHash.Snowball)
            {
                DetachEntity(_handPresentProp, false, false);
                _hasThrownSnowball = true;
                _timeSinceSnowballThrow = 0f;
            }

            if (_timeSincePresentTriggerCheck >= CheckForPresentTriggersInterval)
            {
                GiveWeaponToPed(playerPed, (uint)WeaponHash.Snowball, 10, true, true);
                SetPedAmmo(playerPed, (uint)WeaponHash.Snowball, 10);
                SetCurrentPedWeapon(playerPed, (uint)WeaponHash.Snowball, true);

                _timeSincePresentTriggerCheck = 0f;

                if (_presentState == PresentPlacementState.TooFar || _presentState == PresentPlacementState.InRadius)
                {
                    PrezzieState triggeredPresent = null;
                    foreach (PrezzieState present in _presentsToDeliver)
                    {
                        if (!present.HasPlaced && present.Location.Position.DistanceToSquared(playerPos) < present.Location.RadiusSq)
                        {
                            triggeredPresent = present;
                            break;
                        }
                    }

                    bool wasShowingTooltip = _isShowingPlacePresentTooltip;
                    _isShowingPlacePresentTooltip = triggeredPresent != null;

                    if (wasShowingTooltip != _isShowingPlacePresentTooltip)
                    {
                        if (_isShowingPlacePresentTooltip)
                        {
                            BeginTextCommandDisplayHelp(PlacePresentHelpLabelName);
                            EndTextCommandDisplayHelp(0, true, true, 0);
                        }
                        else
                        {
                            ClearAllHelpMessages();
                        }
                    }

                    if (triggeredPresent != null)
                    {
                        _closestPrezzie = triggeredPresent;
                        _presentState = PresentPlacementState.InRadius;
                    }
                    else
                    {
                        _closestPrezzie = null;
                        _presentState = PresentPlacementState.TooFar;
                    }
                }
            }

            // Hide the snowball
            uint currentWeaponHash = 0;
            if(GetCurrentPedWeapon(playerPed, ref currentWeaponHash, false) && currentWeaponHash == (uint)WeaponHash.Snowball)
            {
                SetPedCurrentWeaponVisible(playerPed, false, false, false, false);
            }

            if (_presentState == PresentPlacementState.InRadius)
            {
                if (IsControlJustReleased(0, (int)Control.Context))
                {
                    _presentState = PresentPlacementState.WaitingForPlaceAnim;
                }
            }

            bool isPlacingDown = false;
            if(_presentState == PresentPlacementState.WaitingForPlaceAnim || _presentState == PresentPlacementState.Placing || _presentState == PresentPlacementState.Placed)
            {
                isPlacingDown = IsEntityPlayingAnim(playerPed, Constants.PlacingDownPresentAnimDict, Constants.PlacingDownPresentAnimClip, 3);
            }

            if (_presentState == PresentPlacementState.Placing || _presentState == PresentPlacementState.Placed)
            {
                if (!isPlacingDown)
                {
                    OnPresentPlaced(_closestPrezzie);
                }
                else
                {
                    float animTime = GetEntityAnimCurrentTime(playerPed, Constants.PlacingDownPresentAnimDict, Constants.PlacingDownPresentAnimClip);
                    
                    // At 50% of the animation, detach the present so it stays on the floor
                    if(animTime >= 0.5f)
                    {
                        DetachEntity(_handPresentProp, false, false);
                        _presentState = PresentPlacementState.Placed;
                    }
                }
            }

            if (_presentState == PresentPlacementState.WaitingForPlaceAnim)
            {
                if(!isPlacingDown)
                {
                    if (HasAnimDictLoaded(Constants.PlacingDownPresentAnimDict))
                    {
                        TaskPlayAnim(playerPed, Constants.PlacingDownPresentAnimDict, Constants.PlacingDownPresentAnimClip, 8f, -1f, -1, 0, 0f, true, true, true);
                        _presentState = PresentPlacementState.Placing;
                    }
                    else
                    {
                        RequestAnimDict(Constants.PlacingDownPresentAnimDict);
                    }
                }
            }
        }

        // Minimum number of seconds the hunted player has to spend in ragdoll state
        internal const float HuntedMinRagdollTime = 10f;
        private float _huntedRagdollTimeElapsed = 0f;
        private bool _isHuntedRagdolling = false;

        internal const float MinApprehendDistance = 2.75f;
        private const float HuntedDistanceCheckInterval = 0.65f;
        private float _timeSinceHuntedDistanceCheck = 0f;
        private int? _nearestHuntedToApprehend = null;

        private void ProcessHuntedRagdollState(int playerPed, float deltaTime, Vector3 playerPos)
        {
            bool wasHuntedRagdolling = _isHuntedRagdolling;

            if (IsHunted)
            {
                _isHuntedRagdolling = IsPedRagdoll(playerPed);

                if(_isHuntedRagdolling != wasHuntedRagdolling)
                {
                    if (_isHuntedRagdolling)
                    {
                        _huntedRagdollTimeElapsed = 0f;
                    }

                    TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.XmasBroadcastHuntedCapturableState, _isHuntedRagdolling);
                }

                if(_isHuntedRagdolling && _huntedRagdollTimeElapsed < HuntedMinRagdollTime)
                {
                    _huntedRagdollTimeElapsed += deltaTime;
                    ResetPedRagdollTimer(playerPed);
                }
            }
            else
            {
                _timeSinceHuntedDistanceCheck += deltaTime;

                int? prevNearestHunted = _nearestHuntedToApprehend;

                if(_timeSinceHuntedDistanceCheck >= HuntedDistanceCheckInterval)
                {
                    int? nearestPlayer = null;
                    float lowestDistanceSq = float.MaxValue;
                    foreach(int playerHandle in _capturablePlayers)
                    {
                        float distance = GetEntityCoords(GetPlayerPed(playerHandle), false).DistanceToSquared(playerPos);
                        if (distance <= MinApprehendDistance)
                        {
                            if(distance < lowestDistanceSq)
                            {
                                lowestDistanceSq = distance;
                                nearestPlayer = playerHandle;
                            }
                        }
                    }

                    _nearestHuntedToApprehend = nearestPlayer;
                    _timeSinceHuntedDistanceCheck = 0f;
                }

                if(_nearestHuntedToApprehend != prevNearestHunted)
                {
                    if(_nearestHuntedToApprehend.HasValue)
                    {
                        BeginTextCommandDisplayHelp(ApprehendHelpLabelName);
                        EndTextCommandDisplayHelp(0, true, true, -1);
                    }
                    else
                    {
                        ClearAllHelpMessages();
                    }
                }

                if(_nearestHuntedToApprehend.HasValue && IsControlJustPressed(0, (int)Control.Context))
                {
                    ClearAllHelpMessages();
                    TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.XmasBroadcastHuntedCaptured, GetPlayerServerId(_nearestHuntedToApprehend.Value));
                    _capturablePlayers.Remove(_nearestHuntedToApprehend.Value);
                }
            }
        }

        private void OnPresentPlaced(PrezzieState present)
        {
            _isShowingPlacePresentTooltip = false;
            ClearAllHelpMessages();
            present.HasPlaced = true;
            int presentProp = 0;
            if (_presentSpawners.TryGetValue(present.Location.Tag, out PresentSpawner spawner))
            {
                presentProp = spawner.Spawn(present.Location);
            }
            else
            {
                presentProp = CreateObject(s_PresentPropHashKey, present.Location.Position.X, present.Location.Position.Y, present.Location.Position.Z, true, true, false);
            }
            _presentProps.Add(presentProp);
            _presentState = PresentPlacementState.TooFar;

            SetEntityHasGravity(presentProp, true);
            ActivatePhysics(presentProp);
            SetEntityDynamic(presentProp, true);

            // Notify the server about us delivering a present so it can be broadcast to all other players
            TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.XmasNotifyDeliveredPresent, _presentsToDeliver.IndexOf(present));
        }

        private void AttachPresentToHand(int presentProp, int playerPed)
        {
            AttachEntityToEntity(presentProp, playerPed, GetPedBoneIndex(playerPed, (int)Bone.IK_R_Hand), _handPresentOffset.X, _handPresentOffset.Y, _handPresentOffset.Z, _handPresentRotEuler.X, _handPresentRotEuler.Y, _handPresentRotEuler.Z, false, false, false, false, 0, true);
        }

        private void OnBackupSleighEntered(ref SleighState currentPrimarySleigh, ref SleighState backupSleigh)
        {
            if(backupSleigh != null)
            {
                // Destroy the previous primary sleigh so the player can't dupe them
                if(currentPrimarySleigh != null)
                {
                    SetVehicleEngineHealth(currentPrimarySleigh.OppressorHandle, -4000f);
                    if(currentPrimarySleigh.Blip != default && DoesBlipExist(currentPrimarySleigh.Blip))
                    {
                        RemoveBlip(ref currentPrimarySleigh.Blip);
                    }
                }

                if(_sleighStorageVehicleHandle != 0)
                {
                    SetEntityAsNoLongerNeeded(ref _sleighStorageVehicleHandle);
                    _sleighStorageVehicleHandle = 0;
                }

                // Use the backup sleigh as the new primary sleigh
                currentPrimarySleigh = backupSleigh;

                SetBlipColour(backupSleigh.Blip, 0);

                backupSleigh = null;
            }
            else
            {
                throw new ArgumentNullException(nameof(backupSleigh));
            }
        }

        private void ShrinkPlayerPed(int playerPed, bool isLocal)
        {
            Vector3 pos = Vector3.Zero, fwd = Vector3.Zero, right = Vector3.Zero, up = Vector3.Zero;
            GetEntityMatrix(playerPed, ref fwd, ref right, ref up, ref pos);
            float upLengthSq = up.LengthSquared();
            const float minLength = 1f - float.Epsilon;
            const float maxLength = 1f + float.Epsilon;
            float fwdLenSq = fwd.LengthSquared();
            const float scale = 1.3f;
            if (fwdLenSq < scale)
            {
                fwd *= scale;
                right *= scale;
                //float bias = isLocal ? 0f : 0.05f;
                //pos += up * bias;
                up *= scale;
                SetEntityMatrix(playerPed, fwd.X, fwd.Y, fwd.Z, right.X, right.Y, right.Z, up.X, up.Y, up.Z, pos.X, pos.Y, pos.Z);
                pos -= up * (1f - scale);
                if (!isLocal)
                {
                    //SetEntityCoords(playerPed, pos.X, pos.Y, pos.Z, false, false, false, false);
                    //SetEntityCollision(playerPed, false, false);
                }
            }
        }

        internal override void OnClockReceived(int hours, int minutes, int seconds)
        {
            if (!_timeSet)
            {
                TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.ReceiveHuntedClock, 18, 0, 0);
                _timeSet = true;
            }
        }

        internal void OnSantaSpawnReceived(SpawnLocation spawnLocation)
        {
            _santaSpawnLocation = spawnLocation;
            if(IsHunted)
            {
                _waitingToTeleportToSpawn = true;
            }
        }

        private void ProcessSantaSpawn(int playerPed)
        {
            const float spawnDistance = 5f;

            if (!_waitingTillSafeZone)
            {
                if (_waitingToTeleportToSpawn && PlayerState.IsInSafeZone)
                {
                    RequestCollisionAtCoord(_santaSpawnLocation.PosAndHeading.X, _santaSpawnLocation.PosAndHeading.Y, _santaSpawnLocation.PosAndHeading.Z);
                    SetEntityCoords(playerPed, _santaSpawnLocation.PosAndHeading.X, _santaSpawnLocation.PosAndHeading.Y, _santaSpawnLocation.PosAndHeading.Z, false, false, false, false);
                    SetEntityHeading(playerPed, _santaSpawnLocation.Heading);
                    FreezeEntityPosition(playerPed, true);
                }
                else if (_waitingToTeleportToSpawn)
                {
                    if (HasCollisionLoadedAroundEntity(playerPed))
                    {
                        FreezeEntityPosition(playerPed, false);
                        _waitingToTeleportToSpawn = false;

                        // Once we've loaded, request an oppressor to be spawned.
                        _sleighSpawnState = SleighSpawnState.NeedPrimary;
                        _sleighSpawnPos = Player.Local.Character.Position + Player.Local.Character.ForwardVector * spawnDistance;
                    }
                }
            }
            else
            {
                if(PlayerState.IsInSafeZone)
                {
                    _waitingTillSafeZone = false;
                }
            }
        }

        private void StartIntroSequence()
        {
            SetFrontendActive(false);
            Debug.WriteLine("StartIntroSequence");
            _introTimeElapsed = 0f;
            _introCam = CreateCam("DEFAULT_SCRIPTED_CAMERA", true);
            SetCamCoord(_introCam, _santaSpawnLocation.CameraPos.X, _santaSpawnLocation.CameraPos.Y, _santaSpawnLocation.CameraPos.Z);

            /*Vector3 lookAtDir = (_santaSpawnLocation.Pos - _santaSpawnLocation.CameraPos);
            lookAtDir.Normalize();

            Vector3 right = Vector3.Right;
            Vector3 up = Vector3.Up;
            Vector3 fwd = Vector3.Forward;

            float yaw = (float)Math.Acos(Vector3.Dot(lookAtDir, right));
            float pitch = (float)Math.Acos(Vector3.Dot(lookAtDir, fwd));
            float roll = (float)Math.Acos(Vector3.Dot(lookAtDir, up));

            const float rad2deg = (float)(180.0 / Math.PI);

            pitch *= rad2deg;
            yaw *= rad2deg;
            roll *= rad2deg;

            //pitch += 90f;
            //yaw += 90f;
            //roll += 180f;

            SetCamRot(_introCam, pitch, roll, yaw, 0);
            Vector3 localRight = Vector3.Zero;
            Vector3 localUp = Vector3.Zero;
            Vector3 localFwd = Vector3.Zero;
            Vector3 pos = Vector3.Zero;
            GetCamMatrix(_introCam, ref localRight, ref localFwd, ref localUp, ref pos);
            float fix = (float)Math.Asin(Vector3.Dot(localRight, up)) * rad2deg;*/
            PointCamAtCoord(_introCam, _santaSpawnLocation.PosAndHeading.X, _santaSpawnLocation.PosAndHeading.Y, _santaSpawnLocation.PosAndHeading.Z);

            Vector3 localRight = Vector3.Zero;
            Vector3 localUp = Vector3.Zero;
            Vector3 localFwd = Vector3.Zero;
            Vector3 pos = Vector3.Zero;
            GetCamMatrix(_introCam, ref localRight, ref localFwd, ref localUp, ref pos);

            const float distance = 20f;
            Vector3 sleighPos = pos + localUp * distance;
            _introSleighInitialPos = sleighPos;

            //if (IsHunted)
            {
                _introSleigh = CreateVehicle((uint)VehicleHash.Nimbus, sleighPos.X, sleighPos.Y, sleighPos.Z, GetCamRot(_introCam, 0).Z, false, false);
                //int oppressor = CreateVehicle((uint)_oppressorHashKey, sleighPos.X, sleighPos.Y, sleighPos.Z, 0f, true, false);
                //Debug.WriteLine($"oppressor handle: {oppressor}");
                //AttachEntityToEntity(_introSleigh, oppressor, 0, 0f, 0f, 0f, 0f, 0f, 1f, false, false, false, false, 0, true);
                //FreezeEntityPosition(oppressor, true);
            }

            _currentIntroStage = IntroSequenceStage.Start;

            RequestAnimDict(Constants.WakeUpAnimDict);

            //SetCamRot(_introCam
        }

        internal const float IntroTimeSeconds = 15f;
        internal const float IntroSleighFlightTime = 0.35f * IntroTimeSeconds;
        private float _introTimeElapsed = 0f;

        private float _introCurrentTimer = float.MinValue;
        private float _introCurrentTargetTime = float.MaxValue;
        private bool _introTimerActive = false;

        private enum IntroSequenceStage
        {
            WaitingToStart,
            Start,
            WaitForSleighToCrash,
            SleighCrashed,
            WaitABit,
            PlayGetUpAnim,
            FadeOut,
            FadeIn,
            Done
        }

        private IntroSequenceStage _currentIntroStage = IntroSequenceStage.WaitingToStart, _prevIntroStage = IntroSequenceStage.WaitingToStart;

        private void EndIntroSequence(int playerPed)
        {
            SetCamActive(_introCam, false);
            RenderScriptCams(false, false, 0, false, false);
            FreezeEntityPosition(playerPed, false);
            SetFocusEntity(playerPed);
            HuntUI.DisplayObjective(ref GameState, ref PlayerState, skipAddingHuntedName: true);
        }

        private void ProcessIntroSequence(float deltaTime, int playerPed)
        {
            if(_introTimeElapsed > IntroTimeSeconds)
            {
                if (_prevIntroStage != IntroSequenceStage.Done)
                {
                    EndIntroSequence(playerPed);
                    ClearPedTasks(playerPed);
                    _prevIntroStage = IntroSequenceStage.Done;
                }
                else
                {
                    _currentIntroStage = IntroSequenceStage.Done;
                }
                return;
            }

            DisableAllControlActions(0);

            if(!IsHunted && _currentIntroStage != IntroSequenceStage.Done)
            {
                SetFocusEntity(GetPlayerPed(_huntedPlayerId));
                //SetFocusArea(_santaSpawnLocation.PosAndHeading.X, _santaSpawnLocation.PosAndHeading.Y, _santaSpawnLocation.PosAndHeading.Z, 0f, 0f, 0f);
            }

            if(_currentIntroStage == IntroSequenceStage.WaitForSleighToCrash && _introCurrentTimer > _introCurrentTargetTime * 0.75f)
            {
                if(_primarySleigh != null && _introCurrentTimer < _introCurrentTargetTime * 0.9f)
                {
                    SetEntityInvincible(_primarySleigh.OppressorHandle, true);
                }

                if(_introSleigh != 0 && DoesEntityExist(_introSleigh))
                {
                    Vector3 coords = GetEntityCoords(_introSleigh, false);
                    //for (int i = 0; i < 4; i++)
                    {
                        AddExplosion(coords.X, coords.Y, coords.Z, (int)ExplosionType.Plane, 100f, true, false, 0.6f);
                        //ExplodeVehicle(_introSleigh, true, false);
                    }
                    _currentIntroStage++;

                    if(_currentIntroStage == IntroSequenceStage.WaitABit)
                    {
                        _introTimerActive = true;
                        _introTimeElapsed = 0f;
                        _introCurrentTargetTime = 0.4f;
                    }
                }
            }

            if(_currentIntroStage == IntroSequenceStage.SleighCrashed)
            {
                _prevIntroStage++;
                _currentIntroStage++;
            }

            RequestAnimDict(Constants.WakeUpAnimDict);

            if (_currentIntroStage != _prevIntroStage)
            {
                Debug.WriteLine(_currentIntroStage.ToString());
                switch(_currentIntroStage)
                {
                    case IntroSequenceStage.Start:
                        if(IsHunted)
                        {
                            int oppressorHandle = GetEntityAttachedTo(_introSleigh);
                            FreezeEntityPosition(oppressorHandle, false);
                            SetEntityCompletelyDisableCollision(oppressorHandle, false, false);
                        }
                        _currentIntroStage = IntroSequenceStage.WaitForSleighToCrash;
                        _introCurrentTargetTime = IntroSleighFlightTime;
                        _introCurrentTimer = 0f;
                        _introTimerActive = true;
                        break;
                    case IntroSequenceStage.PlayGetUpAnim:
                        //int oppressor = GetEntityAttachedTo(_introSleigh);
                        //DeleteVehicle(ref oppressor);
                        if (IsHunted)
                        {
                            DeleteVehicle(ref _introSleigh);
                            _introSleigh = 0;
                        }

                        Vector3 rightVec = Vector3.Zero, upVec = Vector3.Zero, fwdVec = Vector3.Zero, pos = Vector3.Zero;
                        int huntedPlayerPed = GetPlayerPed(_huntedPlayerId);
                        GetEntityMatrix(huntedPlayerPed, ref fwdVec, ref rightVec, ref upVec, ref pos);
                        const float camDistance = 4.5f;
                        Vector3 camPos = pos + rightVec * (camDistance * -1f);
                        SetCamCoord(_introCam, camPos.X, camPos.Y, camPos.Z);
                        SetCamFov(_introCam, 40f);
                        PointCamAtEntity(_introCam, huntedPlayerPed, 0f, 0f, 0f, true);
                        TaskPlayAnim(huntedPlayerPed, Constants.WakeUpAnimDict, Constants.WakeUpAnimClip, 8f, -1f, -1, 0, 0f, true, true, true);
                        
                        _introTimerActive = true;
                        _introCurrentTargetTime = !IsHunted ? ((IntroTimeSeconds - _introTimeElapsed) - 1f) : 0f;
                        _introCurrentTimer = 0f;
                        break;
                    case IntroSequenceStage.FadeOut:
                        if (!IsHunted)
                        {
                            DoScreenFadeOut(400);
                            _introTimerActive = true;
                            _introCurrentTargetTime = 0.5f;
                            _introCurrentTimer = 0f;
                        }
                        break;
                    case IntroSequenceStage.FadeIn:
                        if (!IsHunted)
                        {
                            DoScreenFadeIn(600);
                            _introTimerActive = true;
                            _introCurrentTargetTime = 0f;
                            _introCurrentTimer = 0f;
                        }
                        break;
                    case IntroSequenceStage.Done:
                        EndIntroSequence(playerPed);
                        break;
                }
            }
            
            if(_currentIntroStage == IntroSequenceStage.WaitForSleighToCrash)
            {
                //int oppressor = GetEntityAttachedTo(_introSleigh);

                Vector3 velocity = _santaSpawnLocation.Pos - _introSleighInitialPos;
                float distance = velocity.Length();
                velocity.Normalize();
                velocity *= distance / IntroSleighFlightTime;
                Vector3 currentPos = _introSleighInitialPos + velocity * _introCurrentTimer;
                SetEntityCoords(_introSleigh, currentPos.X, currentPos.Y, currentPos.Z, false, false, false, false);
                Vector3 camRot = GetCamRot(_introCam, 0);
                //SetEntityHeading(_introSleigh, camRot.Z);
                SetEntityRotation(_introSleigh, -40f, -10f, camRot.Z, 0, true);
                SetEntityCompletelyDisableCollision(_introSleigh, false, true);
                //PointCamAtCoord(_introCam, currentPos.X, currentPos.Y, currentPos.Z);
            }

            _prevIntroStage = _currentIntroStage;

            if (_introTimerActive)
            {
                if (_introCurrentTimer >= _introCurrentTargetTime)
                {
                    _currentIntroStage++;
                    _introTimerActive = false;
                }
                else
                {
                    _introCurrentTimer += deltaTime;
                }
            }

            _introTimeElapsed += deltaTime;
            RenderScriptCams(true, false, 0, false, false);
        }

        private void ProcessTutorialSequence(float deltaTime)
        {
            bool reachedEnd = _tutorialState.HasFinished;
            if(!reachedEnd)
            {
                string labelKey = $"STH_XMAS_TUTORIAL{_tutorialState.CurrentItemIndex}";
                string text = _tutorialState.Items[_tutorialState.CurrentItemIndex].Text;
                AddTextEntry(labelKey, text ?? "");
                if(text != null)
                {
                    BeginTextCommandDisplayHelp(labelKey);
                    EndTextCommandDisplayHelp(0, false, true, (int)Math.Round(_tutorialState.CurrentItemTimeTarget * 1000.0));
                }
            }

            if (_tutorialState.CurrentItemTimeElapsed < _tutorialState.CurrentItemTimeTarget)
            {
                _tutorialState.CurrentItemTimeElapsed += deltaTime;
            }
            else if(!reachedEnd)
            {
                ++_tutorialState.CurrentItemIndex;
                _tutorialState.CurrentItemTimeElapsed = 0f;
            }
        }

        internal const float AmmoPickupIntervalSeconds = 0.5f;
        private float _timeSinceAmmoPickupCheck = 0f;
        private bool _hasPickedUpAmmoLastTick = false;

        /// <summary>
        /// Checks if the hunter has entered a present radius, and if so, refills their ammo.
        /// </summary>
        /// <param name="playerPed"></param>
        /// <param name="playerPos"></param>
        /// <param name="deltaTime"></param>
        private void ProcessAmmoPickups(int playerPed, Vector3 playerPos, float deltaTime)
        {
            if (!IsHunted)
            {
                if (_timeSinceAmmoPickupCheck >= AmmoPickupIntervalSeconds)
                {
                    bool pickedUp = false;
                    foreach (PrezzieState prezzie in _presentsToDeliver)
                    {
                        if (prezzie.HasPlaced && prezzie.Location.Position.DistanceToSquared(playerPos) <= prezzie.Location.RadiusSq)
                        {
                            pickedUp = true;
                            if (!_hasPickedUpAmmoLastTick)
                            {
                                PlayerState.TakeAwayWeapons(playerPed);
                                const string textKey = "STH_XMAS_AMMOREFIlL";
                                AddTextEntry(textKey, "Your ammo has been refilled.");
                                BeginTextCommandDisplayHelp(textKey);
                                EndTextCommandDisplayHelp(0, false, true, 3500);
                            }
                            break;
                        }
                    }

                    _hasPickedUpAmmoLastTick = pickedUp;

                    _timeSinceAmmoPickupCheck = 0f;
                }

                _timeSinceAmmoPickupCheck += deltaTime;
            }
        }

        private bool _wasStartedLastTick = false;

        public void Tick(float deltaTime)
        {
            int playerPed = PlayerPedId();
            Vector3 playerPos = GetEntityCoords(playerPed, false);

            if (_currentIntroStage == IntroSequenceStage.Done)
            {
                if (_sleighSpawnState != SleighSpawnState.DontSpawn)
                {
                    if (HasModelLoaded((uint)s_OppressorHashKey) && HasModelLoaded((uint)s_SleighHashKey))
                    {
                        bool isBackup = _sleighSpawnState == SleighSpawnState.NeedBackup;
                        
                        // Prevent spawning any backups after presents were delivered.
                        if (!isBackup || !_hasDeliveredAll)
                        {
                            SpawnSleigh(out (isBackup ? ref _backupSleigh : ref _primarySleigh), isBackup: isBackup);
                        }

                        _sleighSpawnState = SleighSpawnState.DontSpawn;
                    }
                    else
                    {
                        RequestModel((uint)s_OppressorHashKey);
                        RequestModel((uint)s_SleighHashKey);
                    }
                }
                else if (_primarySleigh != null)
                {
                    const float minDistanceFromSpawn = 50f;
                    const float minDistanceFromSpawnSq = minDistanceFromSpawn * minDistanceFromSpawn;

                    // If the primary sleigh is far enough, or has been destroyed, or the backup one somehow got destroyed, spawn a new backup one.
                    if ((_backupSleigh == null || GetVehicleEngineHealth(_backupSleigh.OppressorHandle) <= 0) && (GetEntityCoords(_primarySleigh.OppressorHandle, false).DistanceToSquared(_sleighSpawnPos) > minDistanceFromSpawnSq || GetVehicleEngineHealth(_primarySleigh.OppressorHandle) <= 0))
                    {
                        Debug.WriteLine("Need to spawn backup sleigh");
                        _sleighSpawnState = SleighSpawnState.NeedBackup;
                    }
                }
            }

            // Create a present prop and glue it to the character's hand (we'll show it while aiming the snowball, so it looks like we're throwing a present)
            if (_handPresentProp == 0 && HasModelLoaded((uint)s_PresentPropHashKey))
            {
                Vector3 coords = GetPedBoneCoords(playerPed, (int)Bone.IK_R_Hand, 0f, 0f, 0f);
                _handPresentProp = CreateObject(s_PresentPropHashKey, coords.X, coords.Y, coords.Z, true, false, false);
                SetEntityCompletelyDisableCollision(_handPresentProp, false, false);
                AttachPresentToHand(_handPresentProp, playerPed);
            }

            if(_handPresentProp != 0)
            {
                if(_presentState == PresentPlacementState.Placing || IsControlJustPressed(0, (int)Control.Aim) || IsControlJustPressed(0, (int)Control.Attack))
                {
                    AttachPresentToHand(_handPresentProp, playerPed);
                }
                SetEntityVisible(_handPresentProp, IsHunted && !IsPedInAnyVehicle(playerPed, true) && (_presentState == PresentPlacementState.Placed || _presentState == PresentPlacementState.Placing || _hasThrownSnowball || IsControlPressed(0, (int)Control.Aim) || IsControlPressed(0, (int)Control.Attack)), false);
            }

            if (HasStarted)
            {
                ProcessIntroSequence(deltaTime, playerPed);

                if(_currentIntroStage == IntroSequenceStage.WaitingToStart && _santaSpawnLocation != null && (!IsHunted || (HasModelLoaded((uint)VehicleHash.Nimbus))))
                {
                    StartIntroSequence();
                }
                else
                {
                    RequestModel((uint)VehicleHash.Nimbus);
                }
            }

            if (IsHunted && HasStarted)
            {
                DisableControlAction(0, (int)Control.SelectWeapon, true);
                SetPlayerSprint(PlayerId(), false);

                if(_hasStarted != _wasStartedLastTick)
                {
                    NetworkOverrideClockTime(18, 0, 0);
                }

                ProcessSantaSpawn(playerPed);
            }
            else
            {
                // Set elf speed to 149% during the hunt.
                if (_hasStarted != _wasStartedLastTick)
                {
                    SetRunSprintMultiplierForPlayer(PlayerId(), HasStarted ? 1.49f : 1f);
                }
            }

            bool sleighExists = _primarySleigh != null && _primarySleigh.Blip != default && _primarySleigh.OppressorHandle != default;

            _timeSinceStorageCheck += deltaTime;
            _timeSincePresentTriggerCheck += deltaTime;

            int currentVehicle = GetVehiclePedIsIn(playerPed, false);

            foreach(int invisibleEntity in InvisibleEntities)
            {
                SetEntityAlpha(invisibleEntity, 0, 0);
            }

            if (sleighExists)
            {
                bool isInBackupSleigh = _backupSleigh != null && currentVehicle == _backupSleigh.OppressorHandle;

                if(isInBackupSleigh)
                {
                    OnBackupSleighEntered(ref _primarySleigh, ref _backupSleigh);
                }

                bool isInSleigh = currentVehicle == _primarySleigh.OppressorHandle;

                if (isInSleigh)
                {
                    SetBlipAlpha(_primarySleigh.Blip, 0);
                }
                else
                {
                    SetBlipAlpha(_primarySleigh.Blip, 255);
                }

                ProcessSleighStorage(isInSleigh, playerPed);
            }

            // Add a tutorial notification when the backup sleigh is spawned.
            if(_backupSleigh != null && _backupSleigh.NeedsNotification && _tutorialState.HasFinished)
            {
                _tutorialState = new TutorialState()
                {
                    Items = new TextSequenceItem[] { Constants.UI.BackupSleighTutorial },
                };
                _backupSleigh.NeedsNotification = false;
            }

            if(IsHunted)
            {
                ProcessPresentPlacement(playerPed, playerPos, currentVehicle != 0);

                if (_hasThrownSnowball)
                {
                    _timeSinceSnowballThrow += deltaTime;
                }

                if(_timeSinceSnowballThrow >= SnowballThrowTimeout)
                {
                    _hasThrownSnowball = false;
                    _timeSinceSnowballThrow = 0f;
                    AttachPresentToHand(_handPresentProp, playerPed);
                }
            }

            foreach(PrezzieState prezzie in _presentsToDeliver)
            {
                Vector3 pos = prezzie.Location.Position;
                byte[] col = prezzie.HasPlaced ? new byte[4] { 255, 255, 255, 255 } : prezzie.Rgba;
                DrawMarker((int)MarkerType.VerticalCylinder, pos.X, pos.Y, pos.Z, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, col[0], col[1], col[2], col[3], false, false, 2, false, null, null, false);
            }

            _presentSpawners.LoadModels();

            ProcessTutorialSequence(deltaTime);

            ProcessAmmoPickups(playerPed, playerPos, deltaTime);

            ProcessHuntedRagdollState(playerPed, deltaTime, playerPos);

            if(_waitingForClothesChange)
            {
                if (_clothesChangeTimer >= ClothesChangeDelaySeconds)
                {
                    SetPlayerClothing(IsHunted);
                    _clothesChangeTimer = 0f;
                    _waitingForClothesChange = false;
                }
                else
                {
                    _clothesChangeTimer += deltaTime;
                }
            }

            _wasStartedLastTick = _hasStarted;
        }
    }
}
