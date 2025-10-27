using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntClient.Interfaces
{
    internal interface ITickable
    {
        void Tick(float deltaTime);
    }
}
