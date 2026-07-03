namespace SurviveTheHuntClient.Plugins.Cupid.Interfaces
{
    internal interface INetEntityListener
    {
        void OnNetEntityReceived(string name, int netId);
    }
}
