using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models.UI;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs
{
    internal abstract class JobControllerBase : ITickable
    {
        /// <summary>
        /// How much heat completing this job will award
        /// </summary>
        internal abstract ushort HeatValue { get; }

        internal delegate void JobCompletedHandler(ushort heatValue);

        internal event JobCompletedHandler OnComplete;

        private bool _isActive = false;

        protected IPlayerState PlayerState = null;
        protected IGameState GameState = null;

        protected abstract JobStateBase State { get; }

        /// <summary>
        /// A delegate for syncing state updates with other hunters.
        /// </summary>
        /// <param name="jobId">The ID of the job to update state for.</param>
        /// <param name="propId">The ID of the prop to update. This needs to be understood by <see cref="JobStateBase.Set(int, object)"/>.</param>
        /// <param name="propValue">The value to update the prop with.</param>
        internal delegate void JobStateRpcUpdateDelegate(string jobId, int propId, object propValue);

        private readonly JobStateRpcUpdateDelegate _updateJobState;

        internal readonly string Id;

        protected JobControllerBase(string jobId, JobStateRpcUpdateDelegate updateJobState)
        {
            _updateJobState = updateJobState;
            Id = jobId;
        }

        protected void SyncState()
        {
            Dictionary<int, object> props = State.Get();

            foreach(KeyValuePair<int, object> prop in props)
            {
                SyncState(prop.Key, prop.Value);
            }
        }

        protected void SyncState(int propId)
        {
            SyncState(propId, State.Get(propId));
        }

        protected void SyncState(int propId, object propValue)
        {
            Debug.WriteLine($"{nameof(JobControllerBase)}.{nameof(SyncState)}({nameof(propId)}, {nameof(propValue)}): sending prop {propId} with value {propValue} to server...");
            _updateJobState(Id, propId, propValue);
        }

        /// <summary>
        /// Updates state from a remote event.
        /// </summary>
        /// <param name="statePropId"></param>
        /// <param name="statePropValue"></param>
        internal void UpdateState(int statePropId, object statePropValue)
        {
            State.Set(statePropId, statePropValue);
        }

        internal bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    OnActiveChanged(value);
                }
                _isActive = value;
            }
        }

        internal abstract bool IsInTrigger { get; }

        protected virtual void OnActiveChanged(bool isActive)
        {

        }

        protected virtual void OnJobFinished()
        {
            string heatAmount = (HeatValue / 100f).ToString(HeatValue % 100 == 0 ? "0" : "0.00");
            const string key = "STH_CUPID_HEAT_AWARD";
            AddTextEntry(key, $"Job complete. You have been awarded {heatAmount} Heat.");
            BeginTextCommandDisplayHelp(key);
            EndTextCommandDisplayHelp(0, false, true, 10 * 1000);
        }

        internal virtual void Start(IPlayerState playerState, IGameState gameState)
        {
            PlayerState = playerState;
            GameState = gameState;
        }

        internal abstract void Cleanup();

        internal virtual LabelledItem[] CurrentUI => LabelledItem.Empty;

        public abstract void Tick(float deltaTime);
    }
}
