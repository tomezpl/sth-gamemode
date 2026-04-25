using System;
using System.Collections.Generic;
using SurviveTheHuntClient.Plugins.Cupid.Controllers.Jobs;

namespace SurviveTheHuntClient.Plugins.Cupid.Models
{
    /// <summary>
    /// Base class for a job's state container.
    /// Where a state property change needs to be synced with other clients, <see cref="CanSync"/> needs to be checked before calling <see cref="JobControllerBase.SyncState"/>
    /// Otherwise, you will cause an infinite loop of state sync and probably crash both clients and the server...
    /// </summary>
    internal abstract class JobStateBase
    {
        private bool _canSync = true;
        protected bool CanSync => _canSync;
        
        /// <summary>
        /// Used to set a prop from a remote event, to sync with other clients.
        /// </summary>
        /// <param name="statePropId"></param>
        /// <param name="statePropValue"></param>
        internal void Set(int statePropId, object statePropValue)
        {
            // lock syncing while the property is updated
            _canSync = false;
            SetImpl(statePropId, statePropValue);
            _canSync = true;
        }

        /// <summary>
        /// The implementation for setting a state prop received from the server.
        /// </summary>
        /// <param name="statePropId"></param>
        /// <param name="statePropValue"></param>
        internal abstract void SetImpl(int statePropId, object statePropValue);

        internal abstract Dictionary<int, object> Get();
        internal abstract object Get(int statePropId);

        internal delegate void GenericStateChangedEvent<T>(T prev, T current, bool canSync);

        /// <summary>
        /// A wrapper for performing state updates locally, without triggering sync with other clients.
        /// </summary>
        /// <param name="action">A callback that can contain updates to the state.</param>
        internal void Local(Action action)
        {
            bool couldSyncBefore = _canSync;
            _canSync = false;
            action();
            _canSync = couldSyncBefore;
        }

        internal void Synced(Action action)
        {
            bool couldSyncBefore = _canSync;
            _canSync = true;
            action();
            _canSync = couldSyncBefore;
        }
    }
}
