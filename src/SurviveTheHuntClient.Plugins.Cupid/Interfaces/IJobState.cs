using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Plugins.Cupid.Interfaces
{
    internal interface IJobState
    {
        byte[] Serialize();

        IJobState FromBytes(byte[] bytes);
    }
}
