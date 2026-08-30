using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Helpers;
using SurviveTheHuntClient.Plugins.Cupid.Utils;
using System;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.SceneHandlers.Outro
{
    internal class OutroHuntedWinSceneHandler : SceneHandlerBase<OutroHuntedWinSceneHandler.SceneStage, OutroHuntedWinSceneHandler.Tickers, OutroHuntedWinSceneHandler.State>
    {
        internal OutroHuntedWinSceneHandler(bool isWinner, HuntPlayer[] huntedPlayers, TriggerEventProxyDelegate triggerEventProxyDelegate, TriggerServerEventProxyDelegate triggerServerEventProxyDelegate) : base(triggerEventProxyDelegate, triggerServerEventProxyDelegate)
        {
            CurrentState.JPlayer = huntedPlayers[0].PlayerHandle;
            if(huntedPlayers.Length >= 2)
            {
                CurrentState.LPlayer = huntedPlayers[1].PlayerHandle;
            }

            CurrentState.AVControllerHelper = AVControllerHelper;
            CurrentState.Winner = isWinner;
        }

        internal enum SceneStage
        {
            Spawn,
            Drive,
            End,
        }

        private const float MaxSpawnSeconds = 5f;

        internal const float DriveStageDurationSeconds = 6f;

        internal override float GetStageDuration(SceneStage stage)
        {
            switch(stage)
            {
                case SceneStage.Spawn:
                    return MaxSpawnSeconds;
                case SceneStage.Drive:
                    return DriveStageDurationSeconds;
                case SceneStage.End:
                    return 20f;
            }

            return base.GetStageDuration(stage);
        }


        private class SceneStageTick : SceneStageTickBaseAttribute { internal SceneStageTick(SceneStage stage) : base((int)stage) { } }

        internal class Tickers
        {
            [SceneStageTick(SceneStage.Spawn)]
            public static void Spawn(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    state.JPed = ClonePed(GetPlayerPed(state.JPlayer.Value), 0f, false, false);
                    if(state.LPlayer.HasValue)
                    {
                        state.LPed = ClonePed(GetPlayerPed(state.LPlayer.Value), 0f, false, false);
                    }

                    state.Cam = CreateCam("DEFAULT_SCRIPTED_CAMERA", true);
                    SetFocusEntity(state.JPed);
                    state.AVControllerHelper.StartStage("Outro", state.Winner ? "WINNER" : "LOSER");

                    // sunset
                    SetClockTime(17, 30, 0);
                    NetworkOverrideClockTime(17, 30, 0);
                    SetOverrideWeather("EXTRASUNNY");
                }

                RequestCollisionAtCoord(DrivingStartX, DrivingStartY, DrivingStartZ);

                if(!HasModelLoaded(Constants.TulipHashKey))
                {
                    RequestModel(Constants.TulipHashKey);
                }
                else
                {
                    state.Car = CreateVehicle(Constants.TulipHashKey, DrivingStartX, DrivingStartY, DrivingStartZ, DrivingStartHeading, false, false);
                    SetVehicleEngineOn(state.Car, true, true, false);
                    SetPedIntoVehicle(state.JPed, state.Car, -1);
                    if(state.LPed != 0)
                    {
                        SetPedIntoVehicle(state.LPed, state.Car, 0);
                    }
                    SetVehicleForwardSpeed(state.Car, 5f);
                    TaskVehicleDriveToCoord(state.JPed, state.Car, DrivingEndX, DrivingEndY, DrivingEndZ, 100f, 0, Constants.TulipHashKey, 0, 0.2f, 1f);
                    state.CurrentStageTime = state.CurrentStageDuration;
                }
            }

            [SceneStageTick(SceneStage.Drive)]
            public static void Drive(float deltaTime, ref State state)
            {
                float progress = state.CurrentStageTime / state.CurrentStageDuration;
                float invProgress = 1f - progress;

                float smoothRate = Math.Max(0.1f, invProgress);

                const float CamDisplacementX = State.EndCamX - State.StartCamX;
                const float CamDisplacementY = State.EndCamY - State.StartCamY;
                const float CamDisplacementZ = State.EndCamZ - State.StartCamZ;
                const float CamAngDisplacementX = State.EndCamRotX - State.StartCamRotX;
                const float CamAngDisplacementY = State.EndCamRotY - State.StartCamRotY;
                const float CamAngDisplacementZ = State.EndCamRotZ - State.StartCamRotZ;
                const float CamFovChange = State.EndCamFov - State.StartCamFov;

                float baseRate = smoothRate * deltaTime;
                float posRate = baseRate * 0.47f, rotRate = baseRate * 0.4f, fovRate = baseRate * 0.2f;

                float zMultiplier = 0.4f;
                float xMultiplier = 0.35f;
                float yMultiplier = 1.2f;

                state.CamX += CamDisplacementX * posRate * xMultiplier;
                state.CamY += CamDisplacementY * posRate * yMultiplier;
                state.CamZ += CamDisplacementZ * posRate * zMultiplier;
                state.CamRotX += CamAngDisplacementX * rotRate;
                state.CamRotY += CamAngDisplacementY * rotRate;
                state.CamRotZ += CamDisplacementZ * rotRate;
                state.CamFov += CamFovChange * posRate;

                SetCamCoord(state.Cam, state.CamX, state.CamY, state.CamZ);
                SetCamRot(state.Cam, state.CamRotX, state.CamRotY, state.CamRotZ, 2);
                SetCamFov(state.Cam, state.CamFov);
                SetCamShakeAmplitude(state.Cam, 1f);
                ShakeCam(state.Cam, "HAND_SHAKE", 0.67f);
            }

            [SceneStageTick(SceneStage.End)]
            public static void End(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    SetFocusEntity(PlayerPedId());
                }
            }
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            RenderScriptCams(true, false, 0, false, false);
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if(CurrentState.Cam != 0)
            {
                RenderScriptCams(false, false, 0, false, false);
                SetCamActive(CurrentState.Cam, false);
                DestroyCam(CurrentState.Cam, true);
            }

            int[] entitiesToCleanup = {CurrentState.Car, CurrentState.JPed, CurrentState.LPed};

            foreach (int entityToCleanup in entitiesToCleanup)
            {
                if (entityToCleanup != 0)
                {
                    SetEntityAsMissionEntity(entityToCleanup, false, true);
                    int entity = entityToCleanup;
                    DeleteEntity(ref entity);
                }
            }
        }

        internal const float DrivingStartX = -1113.662f, DrivingStartY = -942.8062f, DrivingStartZ = 1.999906f, DrivingStartHeading = 211.8957f;
        internal const float DrivingEndX = -1023.809f, DrivingEndY = -1094.5f, DrivingEndZ = 1.410001f, DrivingEndHeading = 211.4957f;

        internal class State : SceneHandlerBaseState
        {
            internal int JPed = 0, LPed = 0;
            internal int Car = 0;
            internal int Cam = 0;
            internal int? JPlayer = null, LPlayer = null;
            internal float CamX = StartCamX, CamY = StartCamY, CamZ = StartCamZ, CamRotX = StartCamRotX, CamRotY = StartCamRotY, CamRotZ = StartCamRotZ, CamFov = StartCamFov;
            internal const float StartCamX = -1133.711059570312f, StartCamY = -929.741638183593f, StartCamZ = 5.260477066040039f,
                StartCamRotX = -11.268036842346191f, StartCamRotY = 0.002844972535967827f, StartCamRotZ = -112.48248291015625f,
                StartCamFov = 11.604503631591797f;
            internal const float EndCamX = -1132.16552734375f, EndCamY = -971.915771484375f, EndCamZ = 23.105695724487305f,
                EndCamRotX = -3.650254011154175f, EndCamRotY = 0.0030085048638284206f, EndCamRotZ = -113.09675598144531f,
                EndCamFov = 42.21721267700195f;

            internal AVControllerHelper AVControllerHelper = null;
            internal bool Winner = false;
        }
    }
}
