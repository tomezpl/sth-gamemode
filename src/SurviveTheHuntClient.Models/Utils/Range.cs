using System;

namespace SurviveTheHuntClient.Models.Utils
{
    public struct Range<T> where T : IComparable
    {
        public readonly T Min;
        public readonly T Max;

        public Range(params T[] values)
        {
            T lowest = values[0];
            T highest = values[0];

            foreach(T value in values)
            {
                if(lowest.CompareTo(value) > 0)
                {
                    lowest = value;
                }
                
                if(highest.CompareTo(value) < 0)
                {
                    highest = value;
                }
            }

            Min = lowest;
            Max = highest;
        }
    }
}
