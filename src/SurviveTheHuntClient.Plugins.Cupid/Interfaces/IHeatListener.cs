namespace SurviveTheHuntClient.Plugins.Cupid.Interfaces
{
    internal interface IHeatListener
    {
        void OnHeatChanged(ushort heatScore, Constants.HeatThresholds heatThreshold);
    }
}
