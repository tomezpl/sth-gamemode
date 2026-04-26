using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Models.UI;
using SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs;
using System;
using System.Collections.Generic;

namespace SurviveTheHuntClient.Plugins.Cupid.Managers
{
    internal sealed class JobManager : ITickable
    {
        private JobControllerBase _currentJob;

        private JobControllerBase[] _jobs = new JobControllerBase[0];

        internal static JobControllerBase[] GetDefaultJobs(JobControllerBase.JobStateRpcUpdateDelegate syncState)
        {
            return new JobControllerBase[]
            {
                new SimpleRobberyJobController("fleeca_legion", syncState, SimpleRobberyJobController.RobberyType.Bank, Constants.Location.RobberyJob.FleecaLegion, Constants.Location.RobberyJob.FleecaLegion),
                new SimpleRobberyJobController("fleeca_rockford", syncState, SimpleRobberyJobController.RobberyType.Bank, Constants.Location.RobberyJob.FleecaRockford, Constants.Location.RobberyJob.FleecaRockfordObjective),
                new SimpleRobberyJobController("fleeca_chumash", syncState, SimpleRobberyJobController.RobberyType.Bank, Constants.Location.RobberyJob.FleecaChumash, Constants.Location.RobberyJob.FleecaChumash),
            };
        }

        private readonly IPlayerState _playerState;
        private readonly IGameState _gameState;

        private readonly TriggerServerEventProxyDelegate TriggerServerEvent;

        internal JobManager(TriggerServerEventProxyDelegate triggerServerEventProxy, IPlayerState playerState, IGameState gameState, IList<JobControllerBase> jobs) : this(triggerServerEventProxy, playerState, gameState)
        {
            _jobs = new JobControllerBase[jobs.Count];
            jobs.CopyTo(_jobs, 0);
        }

        internal JobManager(TriggerServerEventProxyDelegate triggerServerEventProxy, IPlayerState playerState, IGameState gameState, JobControllerBase[] jobs = null) : this(triggerServerEventProxy, playerState, gameState)
        {
            if(jobs == null)
            {
                jobs = GetDefaultJobs(SyncState);
            }

            _jobs = new JobControllerBase[jobs.Length];
            jobs.CopyTo(_jobs, 0);
        }

        private JobManager(TriggerServerEventProxyDelegate triggerServerEventProxy, IPlayerState playerState, IGameState gameState)
        {
            _playerState = playerState;
            _gameState = gameState;
            TriggerServerEvent = triggerServerEventProxy;
        }

        private void SyncState(string jobId, int propId, object propValue)
        {
            TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidJobSyncState, jobId, propId, propValue);
        }

        private void OnCurrentJobChanged(JobControllerBase prev, JobControllerBase current)
        {
            Debug.WriteLine($"{nameof(JobManager)}: current job changed from {prev?.Id} to {current?.Id}");

            if (prev != null)
            {
                prev.IsActive = false;
            }
            if (current != null)
            {
                current.IsActive = true;
            }
        }

        internal LabelledItem[] CurrentJobUI => _currentJob != null && _currentJob.IsActive ? _currentJob.CurrentUI : LabelledItem.Empty;

        internal void Cleanup()
        {
            Debug.WriteLine($"{nameof(JobManager)}.{nameof(Cleanup)}(): cleaning up {_jobs.Length} jobs...");

            foreach(JobControllerBase job in _jobs)
            {
                job.Cleanup();
            }
        }

        private bool _hasStarted = false;

        internal void Start(bool force = false)
        {
            if(force || !_hasStarted)
            {
                _hasStarted = true;

                Debug.WriteLine($"{nameof(JobManager)}.{nameof(Start)}(): starting {_jobs.Length} jobs...");

                foreach(JobControllerBase job in _jobs)
                {
                    job.Start(_playerState, _gameState);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            if(!_hasStarted)
            {
                return;
            }

            JobControllerBase newCurrentJob = null;

            // TODO: probably don't need to tick the job controller for hunters?

            if (_currentJob != null)
            {
                _currentJob.Tick(deltaTime);
            }

            foreach (JobControllerBase job in _jobs)
            {
                if (job != _currentJob)
                {
                    // Allow jobs to tick even if they're inactive - they should be responsible for deactivating as needed
                    job.Tick(deltaTime);
                }

                if (newCurrentJob == null && job.IsInTrigger)
                {
                    newCurrentJob = job;
                }
            }

            // Only switch current job if we don't have an active one already
            if(newCurrentJob != _currentJob && (_currentJob == null || !_currentJob.IsActive))
            {
                JobControllerBase oldJob = _currentJob;
                _currentJob = newCurrentJob;
                OnCurrentJobChanged(oldJob, newCurrentJob);
            }
        }
    }
}
