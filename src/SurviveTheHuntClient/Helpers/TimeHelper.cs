using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Helpers
{
    /// <summary>
    /// Helper class to queue timeout/interval callbacks
    /// </summary>
    internal static class TimeHelper
    {
        internal class Timeout
        {
            /// <summary>
            /// Initial timeout - uint64 would be a safer choice but also we don't ever need a timeout longer than 24 * 60 * 1000 so 2^32 will suffice
            /// </summary>
            internal uint MsInitial;

            internal uint MsRemaining;
            internal Action Callback;
            internal bool Repeat;
        }

        private static List<Timeout> _timeouts = new List<Timeout>();

        private static List<Timeout> _toRemove = new List<Timeout>(_timeouts.Count);

        internal static Timeout AddTimeout(uint delay, Action callback, bool repeat = false)
        {
            Timeout timeout = new Timeout() { Repeat = repeat, Callback = callback, MsRemaining = delay, MsInitial = delay };
            _timeouts.Add(timeout);
            return timeout;
        }

        internal static Timeout AddInterval(uint interval, Action callback)
        {
            return AddTimeout(interval, callback, true);
        }

        internal static void Tick(float deltaTime)
        {
            uint deltaMs = Convert.ToUInt32(deltaTime * 1000f);

            _toRemove.Clear();
            _toRemove.Capacity = _timeouts.Count;

            foreach(Timeout timeout in _timeouts)
            {
                bool invokeCallback = deltaMs >= timeout.MsRemaining;
                if(invokeCallback)
                {
                    if(timeout.Repeat)
                    {
                        timeout.MsRemaining = timeout.MsInitial;
                    }
                    else
                    {
                        _toRemove.Add(timeout);
                    }

                    timeout.Callback();
                }
                else
                {
                    timeout.MsRemaining -= deltaMs;
                }
            }

            foreach(Timeout timeout in _toRemove)
            {
                _timeouts.Remove(timeout);
            }
        }
    }
}
