using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntShared.Utils
{
    public interface ISurviveTheHuntConfigSerializable
    {
        byte[] Serialize();
    }
}
