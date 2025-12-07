using SurviveTheHuntClient.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Models
{
    /// <summary>
    /// An <see cref="ITickable"/> that executes a provided callback on each tick.
    /// Useful for temporarily injecting per-tick functions.
    /// </summary>
    internal class DynamicTickable : ITickable
    {
        internal Action<DynamicTickable, float> _tickImpl;

        internal DynamicTickable(Action<DynamicTickable, float> tickAction)
        {
            if(tickAction == null)
            {
                throw new ArgumentNullException(nameof(tickAction));
            }

            _tickImpl = tickAction;
        }

        public void Tick(float deltaTime)
        {
            _tickImpl(this, deltaTime);
        }
    }
}
