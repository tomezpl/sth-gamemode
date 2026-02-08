using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntShared.Plugins.Cupid.Constants;

namespace SurviveTheHuntClient.Plugins.Cupid.SceneHandlers.Intro
{
    internal class IntroJ2Handler : SceneHandlerBase<IntroJ2Handler.SceneStage, IntroJ2Handler.Tickers, IntroJ2Handler.State>
    {
        internal class SceneStageTick : SceneStageTickBaseAttribute { internal SceneStageTick(SceneStage stage) : base((int)stage) { } }

        private static readonly int _carHash = GetHashKey("boor");

        internal IntroJ2Handler() : base()
        {
            RequestModel((uint)_carHash);
        }

        internal enum SceneStage
        {
            Driving
        }

        internal class State : SceneHandlerBaseState
        {
            internal int Car;
            internal readonly int JasPed;
            internal readonly int Camera;

            public State()
            {
                Car = 0;
                Camera = 0;
                JasPed = 0;
            }

            internal State(int jasPedId, int camera) : this()
            {
                JasPed = jasPedId;
                Camera = camera;
            }
        }

        internal override float GetStageDuration(SceneStage stage)
        {
            return 5.5f;
        }

        public override void StartScene(in IGameState gameState, int cameraId)
        {
            base.StartScene(gameState, cameraId);

            CurrentState = new State(GetPlayerPed(gameState.Hunt.HuntedPlayers[(int)PlayerType.HuntedJ].PlayerHandle), cameraId);

            SetFocusEntity(CurrentState.JasPed);
            RequestModel((uint)_carHash);
        }

        internal class Tickers
        {
            [SceneStageTick(SceneStage.Driving)]
            public static void Ticker(float deltaTime, ref State state)
            {
                if(state.Car == 0)
                {
                    RequestModel((uint)_carHash);

                    if(HasModelLoaded((uint)_carHash))
                    {
                        state.Car = CreateVehicle((uint)_carHash, -3051.95f, 428.1f, 6.45f, 159.6f, false, false);
                        SetEntityAsMissionEntity(state.Car, false, true);
                        SetVehicleEngineOn(state.Car, true, true, true);
                        SetPedIntoVehicle(state.JasPed, state.Car, -1);
                        TaskVehicleDriveWander(state.JasPed, state.Car, 10f, 0);
                        Vector3 fwdVec = GetEntityForwardVector(state.Car);
                        Vector3 rightVec = -Vector3.Cross(fwdVec, Vector3.Up);
                        Vector3 pedPos = GetEntityCoords(state.JasPed, false);
                        Vector3 offset = new Vector3(-1.35f, 0.18f, 0.78f);
                        //PointCamAtEntity(state.Camera, state.JasPed, 0f, 0f, 0f, true);
                        SetCamRot(state.Camera, 0f, 0f, 0f, 0);
                        Vector3 rot = GetCamRot(state.Camera, 0);
                        HardAttachCamToEntity(state.Camera, state.Car, 0, 0, -90f, offset.X, offset.Y, offset.Z, true);
                        SetVehicleRadioEnabled(state.Car, false);
                    }
                }
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if(CurrentState.Car != 0)
            {
                ClearPedTasks(CurrentState.JasPed);
                DeleteEntity(ref CurrentState.Car);
            }
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            RenderScriptCams(true, false, 0, false, false);
        }
    }
}
