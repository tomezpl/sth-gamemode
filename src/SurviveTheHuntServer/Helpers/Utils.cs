using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Helpers
{
    public static class Utils
    {
        public static int[] IndicesFromIndexCount(int count)
        {
            int[] indices = new int[count];

            for(int i = 0; i < count; i++)
            {
                indices[i] = i;
            }

            return indices;
        }
    }
}
