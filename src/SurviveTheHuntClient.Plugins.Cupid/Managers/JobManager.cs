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

        internal static JobControllerBase[] GetDefaultJobs(TriggerEventProxyDelegate triggerEventProxy, TriggerServerEventProxyDelegate triggerServerEventProxy, JobControllerBase.JobStateRpcUpdateDelegate syncState)
        {
            return new JobControllerBase[]
            {
                new SimpleRobberyJobController("fleeca_legion", syncState, SimpleRobberyJobController.RobberyType.Bank, Constants.Location.RobberyJob.FleecaLegion, Constants.Location.RobberyJob.FleecaLegion),
                new SimpleRobberyJobController("fleeca_rockford", syncState, SimpleRobberyJobController.RobberyType.Bank, Constants.Location.RobberyJob.FleecaRockford, Constants.Location.RobberyJob.FleecaRockfordObjective),
                new SimpleRobberyJobController("fleeca_chumash", syncState, SimpleRobberyJobController.RobberyType.Bank, Constants.Location.RobberyJob.FleecaChumash, Constants.Location.RobberyJob.FleecaChumash),

                new CarRobberyJobController("car_elysian", syncState, Constants.Location.CarRobberyJob.Elysian, Constants.Location.CarRobberyJob.ElysianHeading),
                new CarRobberyJobController("car_delperro", syncState, Constants.Location.CarRobberyJob.DelPerro, Constants.Location.CarRobberyJob.DelPerroHeading),

                new ShipJobController(triggerEventProxy, triggerServerEventProxy, syncState),
            };
        }

        private readonly IPlayerState _playerState;
        private readonly IGameState _gameState;

        private readonly TriggerServerEventProxyDelegate TriggerServerEvent;
        private readonly TriggerEventProxyDelegate TriggerEvent;
        internal event JobControllerBase.JobCompletedHandler JobCompleted;

        internal JobManager(TriggerEventProxyDelegate triggerEvent, TriggerServerEventProxyDelegate triggerServerEventProxy, IPlayerState playerState, IGameState gameState, IList<JobControllerBase> jobs) : this(triggerEvent, triggerServerEventProxy, playerState, gameState)
        {
            _jobs = new JobControllerBase[jobs.Count];
            jobs.CopyTo(_jobs, 0);
        }

        internal JobManager(TriggerEventProxyDelegate triggerEvent, TriggerServerEventProxyDelegate triggerServerEventProxy, IPlayerState playerState, IGameState gameState, JobControllerBase[] jobs = null) : this(triggerEvent, triggerServerEventProxy, playerState, gameState)
        {
            if(jobs == null)
            {
                jobs = GetDefaultJobs(triggerEvent, triggerServerEventProxy, SyncState);
            }

            _jobs = new JobControllerBase[jobs.Length];
            jobs.CopyTo(_jobs, 0);
        }

        private JobManager(TriggerEventProxyDelegate triggerEvent, TriggerServerEventProxyDelegate triggerServerEventProxy, IPlayerState playerState, IGameState gameState)
        {
            _playerState = playerState;
            _gameState = gameState;
            TriggerServerEvent = triggerServerEventProxy;
            TriggerEvent = triggerEvent;
        }

        private void ConfigureJobs()
        {
            foreach(JobControllerBase job in _jobs)
            {
                job.OnComplete += OnJobCompleted;
            }
        }

        private void OnJobCompleted(ushort heatValue)
        {
            JobCompleted.Invoke(heatValue);
        }

        private void SyncState(string jobId, int propId, object propValue)
        {
            TriggerServerEvent(SurviveTheHuntShared.Events.Server.CupidJobSyncState, jobId, propId, propValue);
        }

        internal void OnStateReceived(string jobId ,int propId, object propValue)
        {
            foreach(JobControllerBase job in _jobs)
            {
                if(job != null && job.Id == jobId)
                {
                    job.UpdateState(propId, propValue);
                    break;
                }
            }
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

        internal void OnNetEntityReceived(int netId, string name)
        {
            foreach(JobControllerBase job in _jobs)
            {
                job.OnNetEntityReceived(netId, name);
            }
        }

        internal LabelledItem[] CurrentJobUI => _currentJob != null && _currentJob.IsActive ? _currentJob.CurrentUI : LabelledItem.Empty;

        internal void Cleanup(bool force = false)
        {
            Debug.WriteLine($"{nameof(JobManager)}.{nameof(Cleanup)}(): cleaning up {_jobs.Length} jobs...");

            foreach(JobControllerBase job in _jobs)
            {
                job.Cleanup(force);
            }
        }

        private bool _hasStarted = false;

        internal void Start(bool force = false)
        {
            if(!_hasStarted)
            {
                ConfigureJobs();
            }

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
            if(newCurrentJob != _currentJob && (_currentJob == null || (!_currentJob.IsActive || !_currentJob.BlockOtherJobs)))
            {
                JobControllerBase oldJob = _currentJob;
                _currentJob = newCurrentJob;
                OnCurrentJobChanged(oldJob, newCurrentJob);
            }
        }
    }
}
