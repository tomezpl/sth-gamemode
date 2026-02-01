using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Interfaces;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using static SurviveTheHuntShared.Plugins.Cupid.Constants;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core;

namespace SurviveTheHuntClient.Plugins.Cupid.SceneHandlers.Intro
{
    internal class IntroJ1Handler : ITickable, ISceneHandler
    {
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

        internal class SceneStageTick : Attribute
        {
            private readonly SceneStage _stage;
            internal SceneStage Stage { get => _stage; }

            internal SceneStageTick(SceneStage stage)
            {
                _stage = stage;
            }
        }

        internal SceneStage _currentStage = SceneStage.IntroWideShot;
        internal SceneStage CurrentStage { get => _currentStage; }

        protected bool IsStageOver = false;

        internal void SetStage(SceneStage stage)
        {
            Debug.WriteLine($"Changing stage to {stage}");
            _currentStage = stage;
            IsStageOver = false;
            CurrentStageDuration = GetStageDuration(stage);
            CurrentStageTime = 0f;
            if(SceneTickMethods.TryGetValue(stage, out SceneStageTickMethod method))
            {
                CurrentTickMethod = method;
            }
            else
            {
                CurrentTickMethod = FallbackSceneStageTick;
            }

                CurrentState.StageJustSwitched = true;
        }

        private float CurrentStageDuration = 0f;
        private float CurrentStageTime = 0f;

        private static readonly Array s_AllStages = Enum.GetValues(typeof(SceneStage));
        private static readonly SceneStage s_LastStage = (SceneStage)s_AllStages.GetValue(s_AllStages.Length - 1);

        public bool IsOver => CurrentStage == s_LastStage && IsStageOver;

        private static float GetStageDuration(SceneStage stage)
        {
            switch(stage)
            {
                case SceneStage.IntroWideShot:
                    return 5.25f;
                case SceneStage.GroundShot:
                    return 1.75f;
                case SceneStage.SimeonArriveOverhead:
                    return 1.2f;
                case SceneStage.JasDownLadder:
                    return 2f;
                case SceneStage.SimeonTalk1:
                    return 4.5f;
                case SceneStage.JasTalk1:
                    return 1.1f;
                case SceneStage.SimeonTalk2:
                    return 2.6f;
                case SceneStage.JasWalk:
                    return 3.5f;
                default:
                    return 0f;
            }
        }
        
        private struct State
        {
            internal int SimeonPed;
            internal int JasPed;
            internal int Camera;
            internal bool StageJustSwitched;

            internal State(int jasPed, int camera)
            {
                SimeonPed = 0;
                JasPed = jasPed;
                Camera = camera;
                StageJustSwitched = false;
            }
        }

        private State CurrentState = new State();

        private IGameState GameState;

        private delegate void SceneStageTickMethod(float deltaTime, ref State state);

        private static class Tickers
        {
            [SceneStageTick(SceneStage.IntroWideShot)]
            public static void IntroWideShot(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    SetCamCoord(state.Camera, Constants.CameraInitialPos.X, Constants.CameraInitialPos.Y, Constants.CameraInitialPos.Z);
                    SetCamRot(state.Camera, 0f, 0f, Constants.CameraInitialHeading, 0);
                    SetCamFov(state.Camera, Constants.CameraInitialFOV);

                    SetEntityCoords(state.JasPed, Constants.JasInitialPos.X, Constants.JasInitialPos.Y, Constants.JasInitialPos.Z, false, false, false, false);
                }
            }
        }

        private static Dictionary<SceneStage, SceneStageTickMethod> CreateTickMethods()
        {
            Dictionary<SceneStage, SceneStageTickMethod> methods = new Dictionary<SceneStage, SceneStageTickMethod>();

            MethodInfo[] tickersMethods = typeof(Tickers).GetMethods();
            foreach(MethodInfo tickerMethod in tickersMethods)
            {
                SceneStageTick tickInfo = tickerMethod.GetCustomAttribute<SceneStageTick>();
                if(tickInfo != null)
                {
                    methods[tickInfo.Stage] = (SceneStageTickMethod)tickerMethod.CreateDelegate(typeof(SceneStageTickMethod));
                }
            }

            return methods;
        }

        private readonly Dictionary<SceneStage, SceneStageTickMethod> SceneTickMethods = CreateTickMethods();

        private static void FallbackSceneStageTick(float deltaTime, ref State state)
        {
        }

        private SceneStageTickMethod CurrentTickMethod = FallbackSceneStageTick;

        public void StartScene(in IGameState gameState, int cameraId)
        {
            GameState = gameState;

            CurrentState = new State(GetPlayerPed(gameState.Hunt.HuntedPlayers[(int)PlayerType.HuntedJ].PlayerHandle), cameraId);

            SetFocusEntity(CurrentState.JasPed);

            RequestModel((int)PedHash.SiemonYetarian);

            SetStage(SceneStage.IntroWideShot);
        }

        private static class Constants
        {
            internal static Vector3 JasInitialPos = new Vector3(-3062.71f, 444.8849f, 13.08625f);
            internal const float JasInitialHeading = 258.6868f;
            internal static Vector3 CameraInitialPos = new Vector3(-3150f, 450f, 13.08625f);
            internal const float CameraInitialHeading = 250f;
            internal const float CameraInitialFOV = 45f;
        }

        private void EnsureSimeon()
        {
            if(CurrentState.SimeonPed != 0)
            {
                return;
            }

            if (!HasModelLoaded((int)PedHash.SiemonYetarian))
            {
                RequestModel((int)PedHash.SiemonYetarian);
            }
            
            if(HasModelLoaded((int)PedHash.SiemonYetarian))
            {
                Vector3 pos = Constants.JasInitialPos;
                CurrentState.SimeonPed = CreatePed(0, (uint)PedHash.SiemonYetarian, pos.X, pos.Y, pos.Z, Constants.JasInitialHeading, false, false);
                SetEntityAsMissionEntity(CurrentState.SimeonPed, false, true);
                Debug.WriteLine("Spawned simeon");
            }
        }

        public void Tick(float deltaTime)
        {
            EnsureSimeon();

            CurrentTickMethod(deltaTime, ref CurrentState);

            CurrentState.StageJustSwitched = false;

            if(CurrentStageTime >= CurrentStageDuration)
            {
                bool wasOver = IsStageOver;

                IsStageOver = true;

                if(!wasOver)
                {
                    Debug.WriteLine("Reached the end");
                }
            }
            else
            {
                CurrentStageTime += deltaTime;
            }

            if (IsStageOver && !IsOver)
            {
                SetStage(CurrentStage + 1);
            }

            RenderScriptCams(true, false, 0, false, false);
        }
    }
}
