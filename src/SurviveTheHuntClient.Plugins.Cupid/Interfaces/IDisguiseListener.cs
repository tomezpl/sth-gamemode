namespace SurviveTheHuntClient.Plugins.Cupid.Interfaces
{
    internal interface IDisguiseListener
    {
        void OnDisguiseChanged(SurviveTheHuntShared.Plugins.Cupid.Constants.PlayerType playerType, Constants.DisguiseState disguise);
    }
}
