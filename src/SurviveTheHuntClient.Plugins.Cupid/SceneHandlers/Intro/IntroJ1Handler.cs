using SurviveTheHuntClient.Interfaces;
using static SurviveTheHuntShared.Plugins.Cupid.Constants;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core;
using SurviveTheHuntClient.Plugins.Cupid.Utils;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Helpers;

namespace SurviveTheHuntClient.Plugins.Cupid.SceneHandlers.Intro
{
    internal class IntroJ1Handler : SceneHandlerBase<IntroJ1Handler.SceneStage, IntroJ1Handler.Tickers, IntroJ1Handler.State>
    {
        internal IntroJ1Handler(TriggerEventProxyDelegate triggerEventProxyDelegate) : base(triggerEventProxyDelegate) { }

        private class SceneStageTick : SceneStageTickBaseAttribute { internal SceneStageTick(SceneStage stage) : base((int)stage) { } }

        internal enum SceneStage
        {
            IntroWideShot,
            GroundShot,
            SimeonArriveOverhead,
            JasDownLadder,
            SimeonTalk1,
            JasTalk1,
            SimeonTalk2,
            JasWalk
        }

        internal override float GetStageDuration(SceneStage stage)
        {
            switch(stage)
            {
                case SceneStage.IntroWideShot:
                    return 8.32f;
                case SceneStage.GroundShot:
                    return 1.45f;
                case SceneStage.SimeonArriveOverhead:
                    return 1.65f;
                case SceneStage.JasDownLadder:
                    return 2.2f;
                case SceneStage.SimeonTalk1:
                    return 5.2f;
                case SceneStage.JasTalk1:
                    return 1.6f;
                case SceneStage.SimeonTalk2:
                    return 2.8f;
                case SceneStage.JasWalk:
                    return 4.1f;
                default:
                    return 0f;
            }
        }
        
        internal class State : SceneHandlerBaseState
        {
            internal int SimeonPed;
            internal int SimeonCar;
            internal int JasPed;
            internal int Camera;
            internal int PopSphereId;
            internal IntroJ1Handler Handler;

            public State() : base() { }

            internal State(int jasPed, int camera, IntroJ1Handler handler) : base()
            {
                SimeonPed = 0;
                SimeonCar = 0;
                JasPed = jasPed;
                Camera = camera;
                Handler = handler;
            }
        }

        internal class Tickers
        {
            [SceneStageTick(SceneStage.IntroWideShot)]
            public static void IntroWideShot(float deltaTime, ref State state)
            {
                const float initCamPosX = -3196.27783203125f, initCamPosY = 366.26983642578125f, initCamPosZ = 7.800143718719482f;
                const float initCamRotX = -2.3481221199035645f, initCamRotY = 0.000919112004339695f, initCamRotZ = -47.3736686706543f;
                const float initCamFov = 40.037540435791016f;

                const float targetCamPosX = -3179.07568359375f, targetCamPosY = 382.103271484375f, targetCamPosZ = 6.841424465179443f;
                const float targetCamRotX = -2.3481221199035645f, targetCamRotY = 0.000919112004339695f, targetCamRotZ = -47.3736686706543f;
                const float targetCamFov = initCamFov;

                if (state.StageJustSwitched)
                {
                    SetOverrideWeather("CLEAR");

                    SetEntityCoords(state.JasPed, Constants.JasInitialPos.X, Constants.JasInitialPos.Y, Constants.JasInitialPos.Z, false, false, false, false);
                    state.PopSphereId = AddPopMultiplierSphere(Constants.JasInitialPos.X, Constants.JasInitialPos.Y, Constants.JasInitialPos.Z, 100f, 0, 0, false, false);

                    // Start the music
                    state.Handler.AVControllerHelper.StartStage("Intro");
                }

                CamUtils.Lerp(state.Camera, initCamPosX, initCamPosY, initCamPosZ, initCamRotX, initCamRotY, initCamRotZ, initCamFov, targetCamPosX, targetCamPosY, targetCamPosZ, targetCamRotX, targetCamRotY, targetCamRotZ, targetCamFov, state.CurrentStageDuration, state.CurrentStageTime);
            }

            [SceneStageTick(SceneStage.GroundShot)]
            public static void GroundShot(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    SetCamCoord(state.Camera, -3044.657f, 427.0898f, 5.5f);
                    SetCamRot(state.Camera, 10f, 0f, 41f, 0);
                    SetCamFov(state.Camera, 30f);

                    SetEntityVisible(state.SimeonCar, false, false);
                    SetEntityVisible(state.SimeonPed, false, false);
                }
            }

            [SceneStageTick(SceneStage.SimeonArriveOverhead)]
            public static void SimeonArriveOverhead(float deltaTime, ref State state)
            {
                if (state.StageJustSwitched)
                {
                    SetEntityVisible(state.SimeonCar, true, false);
                    SetEntityVisible(state.SimeonPed, true, false);
                    SetCamCoord(state.Camera, -3059.81665039062f, 444.3040771484375f, 9.577336311340332f);
                    SetCamRot(state.Camera, -26.49459457397461f, 0.00015853659715503454f, -88.13221740722656f, 2);
                    SetCamFov(state.Camera, 32.3f);
                }
            }

            [SceneStageTick(SceneStage.JasDownLadder)]
            public static void JasDownLadder(float deltaTime, ref State state)
            {
                if (state.StageJustSwitched)
                {
                    SetCamCoord(state.Camera, -3054.14501953125f, 446.2530517578125f, 11.143379211425781f);
                    SetCamRot(state.Camera, 2.918590784072876f, 0.01671551540493965f, 117.45038604736328f, 2);
                    SetCamFov(state.Camera, 17.5f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0.15f);
                }
            }

            [SceneStageTick(SceneStage.SimeonTalk1)]
            public static void SimeonTalk1(float deltaTime, ref State state)
            {
                if (state.StageJustSwitched)
                {
                    SetCamCoord(state.Camera, -3057.07177734375f, 440.3551330566406f, 6.653750896453857f);
                    SetCamRot(state.Camera, -0.27313950657844543f, 0.017105573788285255f, -57.40663146972656f, 2);
                    SetCamFov(state.Camera, 21.6f);

                    // TODO: talking anim

                    SetEntityCoords(state.JasPed, -3061.032f, 443.9454f, 9.644347f, false, false, false, false);
                    SetEntityHeading(state.JasPed, 270f);
                }
            }

            [SceneStageTick(SceneStage.JasTalk1)]
            public static void JasTalk1(float deltaTime, ref State state)
            {
                if (state.StageJustSwitched)
                {
                    SetCamCoord(state.Camera, -3057.773681640625f, 445.1417541503906f, 9.433785438537598f);
                    SetCamRot(state.Camera, 13.307807922363281f, -3.7504117488861084f, 107.60187530517578f, 2);
                    SetCamFov(state.Camera, 20f);

                    // TODO slight camera shake

                }
            }

            [SceneStageTick(SceneStage.SimeonTalk2)]
            public static void SimeonTalk2(float deltaTime, ref State state)
            {
                if (state.StageJustSwitched)
                {
                    SetCamCoord(state.Camera, -3063.165283203125f, 441.1617126464844f, 11.294121742248535f);
                    SetCamRot(state.Camera, -23.594791412353516f, 4.382942199707031f, -60.67698669433594f, 2);
                    SetCamFov(state.Camera, 25.7f);

                    ShakeCam(state.Camera, "HAND_SHAKE", 0.25f);

                    TaskEnterVehicle(state.SimeonPed, state.SimeonCar, 4000, -1, 1f, 0, 0);
                }
            }

            [SceneStageTick(SceneStage.JasWalk)]
            public static void JasWalk(float deltaTime, ref State state)
            {
                const float initCamPosX = -3055.4833984375f, initCamPosY = 451.69708251953125f, initCamPosZ = 10.121474266052246f;
                const float initCamRotX = -2.042907238006592f, initCamRotY = 0.03173978254199028f, initCamRotZ = 128.16847229003906f;
                const float initCamFov = 25.7f;

                const float targetCamPosX = -3056.2998046875f, targetCamPosY = 449.7537841796875f, targetCamPosZ = 10.153057098388672f;
                const float targetCamRotX = -2.0154099464416504f, targetCamRotY = 0.03183150291442871f, targetCamRotZ = 67.65316009521484f;
                const float targetCamFov = 44.7f;

                if (state.StageJustSwitched)
                {
                    SetEntityCoords(state.JasPed, -3060.115f, 448.0133f, 9.043686f, false, false, false, true);
                    SetEntityHeading(state.JasPed, 253.25f);
                    SetCamShakeAmplitude(state.Camera, 0);
                    TaskGoStraightToCoord(state.JasPed, -3059.47f, 450f, 9.65f, 1f, 3000, 56.4f, 0.01f);
                }

                CamUtils.Lerp(state.Camera, initCamPosX, initCamPosY, initCamPosZ, initCamRotX, initCamRotY, initCamRotZ, initCamFov, targetCamPosX, targetCamPosY, targetCamPosZ, targetCamRotX, targetCamRotY, targetCamRotZ, targetCamFov, state.CurrentStageDuration, state.CurrentStageTime);
            }
        }

        public override void StartScene(in IGameState gameState, int cameraId)
        {
            base.StartScene(in gameState, cameraId);

            CurrentState = new State(GetPlayerPed(gameState.Hunt.HuntedPlayers[(int)PlayerType.HuntedJ].PlayerHandle), cameraId, this);

            SetFocusEntity(CurrentState.JasPed);

            ClearAreaOfVehicles(Constants.JasInitialPos.X, Constants.JasInitialPos.Y, Constants.JasInitialPos.Z, 100f, false, false, false, false, false);

            RequestModel((int)PedHash.SiemonYetarian);
        }

        private static class Constants
        {
            internal static Vector3 JasInitialPos = new Vector3(-3062.71f, 444.8849f, 13.08625f);
            internal const float JasInitialHeading = 258.6868f;
        }

        private void EnsureSimeon()
        {
            if(CurrentState.SimeonPed != 0)
            {
                return;
            }

            uint caracaraHash = (uint)GetHashKey("caracara2");

            if (!HasModelLoaded((int)PedHash.SiemonYetarian) || !HasModelLoaded(caracaraHash))
            {
                RequestModel((int)PedHash.SiemonYetarian);
                RequestModel(caracaraHash);
            }
            
            if(HasModelLoaded((int)PedHash.SiemonYetarian) && HasModelLoaded(caracaraHash))
            {
                Vector3 pos = new Vector3(-3051.50366210937f, 443.6261596679687f, 6.15f);
                CurrentState.SimeonPed = CreatePed(0, (uint)PedHash.SiemonYetarian, pos.X, pos.Y, pos.Z, 88f, false, false);
                Vector3 rightVec = Vector3.Cross(GetEntityForwardVector(CurrentState.SimeonPed), Vector3.Up);
                Vector3 carPos = pos + rightVec * 1.5f;
                CurrentState.SimeonCar = CreateVehicle(caracaraHash, carPos.X, carPos.Y, carPos.Z, 90f, false, false);
                SetVehicleColours(CurrentState.SimeonCar, (int)VehicleColor.MetallicSteelGray, (int)VehicleColor.MetallicSilver);
                SetEntityAsMissionEntity(CurrentState.SimeonPed, false, true);
                SetEntityAsMissionEntity(CurrentState.SimeonCar, false, true);
                Debug.WriteLine("Spawned simeon");
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if(DoesEntityExist(CurrentState.SimeonPed))
            {
                DeleteEntity(ref CurrentState.SimeonPed);
            }

            if(DoesEntityExist(CurrentState.SimeonCar))
            {
                DeleteEntity(ref CurrentState.SimeonCar);
            }

            RemovePopMultiplierSphere(CurrentState.PopSphereId, false);
            ClearOverrideWeather();
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            EnsureSimeon();

            RenderScriptCams(true, false, 0, false, false);
        }
    }
}
