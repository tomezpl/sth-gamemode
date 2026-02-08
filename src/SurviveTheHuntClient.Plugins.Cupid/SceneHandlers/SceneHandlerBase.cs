using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SurviveTheHuntClient.Plugins.Cupid.SceneHandlers
{
    internal abstract class SceneStageTickBaseAttribute : Attribute
    {
        private readonly int _stage;
        internal int Stage { get => _stage; }

        internal SceneStageTickBaseAttribute(int stage)
        {
            _stage = stage;
        }
    }

    internal class SceneHandlerBaseState
    {
        internal float CurrentStageDuration = 0f;
        internal float CurrentStageTime = 0f;
        internal bool StageJustSwitched = false;
    }

    internal class SceneHandlerBase<ESceneStage, TTickers, TState> : ITickable, ISceneHandler where TTickers : class where TState : SceneHandlerBaseState, new() where ESceneStage : Enum
    {
        protected static readonly Array s_AllStages = Enum.GetValues(typeof(ESceneStage));
        protected static readonly ESceneStage s_LastStage = (ESceneStage)s_AllStages.GetValue(s_AllStages.Length - 1);

        protected TState CurrentState = new TState();

        protected IGameState GameState;

        protected bool IsStageOver = false;

        internal ESceneStage _currentStage = default;
        internal ESceneStage CurrentStage { get => _currentStage; }

        public bool IsOver => CurrentStage.Equals(s_LastStage) && IsStageOver;

        public virtual void Cleanup()
        {

        }

        public virtual void StartScene(in IGameState gameState, int cameraId)
        {
            GameState = gameState;

            SetStage((ESceneStage)(object)-1);
        }

        protected delegate void SceneStageTickMethod(float deltaTime, ref TState state);

        protected static Dictionary<ESceneStage, SceneStageTickMethod> CreateTickMethods()
        {
            Dictionary<ESceneStage, SceneStageTickMethod> methods = new Dictionary<ESceneStage, SceneStageTickMethod>();

            MethodInfo[] tickersMethods = typeof(TTickers).GetMethods();
            foreach (MethodInfo tickerMethod in tickersMethods)
            {
                SceneStageTickBaseAttribute tickInfo = tickerMethod.GetCustomAttribute<SceneStageTickBaseAttribute>();
                if (tickInfo != null)
                {
                    object stage = tickInfo.Stage;
                    ESceneStage castStage = (ESceneStage)stage;
                    methods[castStage] = (SceneStageTickMethod)tickerMethod.CreateDelegate(typeof(SceneStageTickMethod));
                }
            }

            return methods;
        }

        internal virtual float GetStageDuration(ESceneStage stage)
        {
            return 0f;
        }

        internal void SetStage(ESceneStage stage)
        {
            Debug.WriteLine($"Changing stage to {stage}");
            _currentStage = stage;
            IsStageOver = false;
            CurrentState.CurrentStageDuration = GetStageDuration(stage);
            CurrentState.CurrentStageTime = 0f;
            if (SceneTickMethods.TryGetValue(stage, out SceneStageTickMethod method))
            {
                CurrentTickMethod = method;
            }
            else
            {
                CurrentTickMethod = FallbackSceneStageTick;
            }

            CurrentState.StageJustSwitched = true;
        }

        private static void FallbackSceneStageTick(float deltaTime, ref TState state)
        {
        }

        private SceneStageTickMethod CurrentTickMethod = FallbackSceneStageTick;

        protected readonly Dictionary<ESceneStage, SceneStageTickMethod> SceneTickMethods = CreateTickMethods();

        public virtual void Tick(float deltaTime)
        {
            CurrentTickMethod(deltaTime, ref CurrentState);

            CurrentState.StageJustSwitched = false;

            if (CurrentState.CurrentStageTime >= CurrentState.CurrentStageDuration)
            {
                bool wasOver = IsStageOver;

                IsStageOver = true;

                if (!wasOver)
                {
                    Debug.WriteLine("Reached the end");
                }
            }
            else
            {
                CurrentState.CurrentStageTime += deltaTime;
            }

            if (IsStageOver && !IsOver)
            {
                object stage = CurrentStage;
                int newStage = (int)stage + 1;
                ESceneStage newStageDyn = (ESceneStage)(object)newStage;
                SetStage(newStageDyn);
            }
        }
    }
}
