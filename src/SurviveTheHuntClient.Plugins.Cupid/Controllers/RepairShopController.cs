using SurviveTheHuntClient.Interfaces;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core.UI;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers
{
    internal sealed class RepairShopController : ITickable, IHuntLifecycleListener
    {
        internal const float InnerRadius = 4.5f;
        internal const float OuterRadius = 8.25f;

        private int _blipId;
        internal int BlipId
        {
            get => _blipId;
        }
        internal bool HasBlip
        {
            get => _blipId != 0;
        }

        private Vector3 _position;
        internal Vector3 Position
        {
            get => _position;
        }

        private bool _isInOuter = false;
        internal bool IsInFocus
        {
            get => _isInOuter;
        }

        private bool _isInInner = false;
        internal bool CanUse
        {
            get => _isInInner;
        }

        private const string BlipGxtEntryName = "STH_CUPID_BLIP_REPAIR";
        private const string BlipGxtEntryValue = "Repair Garage";

        private const string HintGxtEntryName = "STH_CUPID_HELP_REPAIR";
        private const string HintGxtEntryValue = "Press ~INPUT_CONTEXT~ to repair the car.";

        private static bool s_HasInit = Init();

        private static bool Init()
        {
            if (!s_HasInit)
            {
                AddTextEntry(BlipGxtEntryName, BlipGxtEntryValue);
                AddTextEntry(HintGxtEntryName, HintGxtEntryValue);
            }

            return true;
        }

        internal RepairShopController(Vector3 position)
        {
            _position = position;
        }

        internal const float RadiusCheckIntervalSeconds = 0.75f;

        private float _timeSinceRadiusCheck = 0f;

        private static bool s_WaitingToReleaseControl = false;

        public void Tick(float deltaTime)
        {
            if(CurrentSharedState == null)
            {
                return;
            }

            if(s_WaitingToReleaseControl)
            {
                DisableAllControlActions(0);
                if(IsControlJustReleased(0, (int)Control.Context) || IsDisabledControlJustReleased(0, (int)Control.Context))
                {
                    s_WaitingToReleaseControl = false;
                    EnableAllControlActions(0);
                }
            }

            _timeSinceRadiusCheck += deltaTime;

            if(_timeSinceRadiusCheck >= RadiusCheckIntervalSeconds)
            {
                _timeSinceRadiusCheck = 0f;

                bool wasInPreviously = _isInInner;

                int playerPed = PlayerPedId();
                bool isInVehicle = IsPedInAnyVehicle(playerPed, false); 
                Vector3 playerPos = GetEntityCoords(playerPed, false);
                float distSq = _position.DistanceToSquared2D(playerPos);
                if(distSq <= (OuterRadius * OuterRadius))
                {
                    _isInOuter = true;

                    _isInInner = isInVehicle && _position.DistanceToSquared(playerPos) <= (InnerRadius * InnerRadius);
                }
                else
                {
                    _isInOuter = false;
                    _isInInner = false;
                }

                if(wasInPreviously != _isInInner)
                {
                    OnTrigger(wasInPreviously, _isInInner);
                }
            }

            if(!CurrentSharedState.WaitingToLeave && CanUse)
            {
                if (IsControlJustPressed(0, (int)Control.Context))
                {
                    s_WaitingToReleaseControl = true;
                    DisableAllControlActions(0);

                    if(RepairCar())
                    {
                        CurrentSharedState.WaitingToLeave = true;
                        ClearAllHelpMessages();
                    }
                }
            }
        }

        private void OnTrigger(bool prev, bool current)
        {
            // TODO
            if(!current)
            {
                if (CurrentSharedState.WaitingToLeave)
                {
                    CurrentSharedState.WaitingToLeave = false;
                }
                else
                {
                    ClearAllHelpMessages();
                }
            }
            else
            {
                BeginTextCommandDisplayHelp(HintGxtEntryName);
                EndTextCommandDisplayHelp(0, true, true, -1);
            }
        }

        private class SharedState
        {
            internal bool WaitingToLeave = false;
        }

        private static SharedState CurrentSharedState = null;

        internal static bool RepairCar()
        {
            if(CurrentSharedState.WaitingToLeave)
            {
                return false;
            }

            Debug.WriteLine("Repairing car");

            int playerPed = PlayerPedId();
            int vehicle = GetVehiclePedIsIn(playerPed, false);

            if (vehicle == 0)
            {
                return false;
            }

            SetVehicleFixed(vehicle);
            SetVehicleEngineHealth(vehicle, 1000f);

            int nbWheels = GetVehicleNumberOfWheels(vehicle);
            for(int i = 0; i < nbWheels; i++)
            {
                SetVehicleWheelHealth(vehicle, i, 1000f);
            }

            SetVehicleBodyHealth(vehicle, 1000f);

            SetVehicleDirtLevel(vehicle, 0f);
            
            for(int i = 0; i < (int)VehicleWindowIndex.ExtraWindow4; i++)
            {
                FixVehicleWindow(vehicle, i);
            }

            WashDecalsFromVehicle(vehicle, 1f);

            StartScreenEffect("RaceTurbo", 0, false);

            // TODO: ideally we'd play a sound here, but i dont know the sound bank names

            return true;
        }

        public void OnHuntStarted(IGameState gameState, IPlayerState playerState)
        {
            if (CurrentSharedState == null)
            {
                CurrentSharedState = new SharedState();
            }

            _blipId = AddBlipForCoord(_position.X, _position.Y, _position.Z);
            SetBlipSprite(_blipId, 72);
            SetBlipNameFromTextFile(_blipId, BlipGxtEntryName);
        }

        public void OnHuntEnded(IGameState gameState, IPlayerState playerState)
        {
            CurrentSharedState = null;

            RemoveBlip(ref _blipId);
            _blipId = 0;
        }
    }
}
