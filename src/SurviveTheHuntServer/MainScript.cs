using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public class MainScript : BaseScript
    {
        private const string ResourceName = "sth-gamemode";

        public MainScript()
        {
            if(GetCurrentResourceName() != ResourceName)
            {
                try
                {
                    throw new Exception($"Survive the Hunt: Invalid resource name! Resource name should be {ResourceName}");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            else
            {

            }
        }
    }
}
