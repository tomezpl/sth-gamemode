using System.Collections.Generic;

namespace SurviveTheHuntClient.Plugins.Cupid.Interfaces
{
    internal interface ISpecialEventListener
    {
        void OnSpecialEvent(Constants.SpecialEvent specialEvent, object[] args);
    }
}
