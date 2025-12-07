using SurviveTheHuntClient.Models.UI;

namespace SurviveTheHuntClient.Interfaces
{
    abstract class Plugin
    {
        protected TriggerServerEventProxyDelegate TriggerServerEventProxy;
        protected TriggerEventProxyDelegate TriggerEventProxy;

        internal Plugin(TriggerEventProxyDelegate triggerEventProxy, TriggerServerEventProxyDelegate triggerServerEventProxy)
        {
            TriggerEventProxy = triggerEventProxy;
            TriggerServerEventProxy = triggerServerEventProxy;
        }

        internal delegate void TriggerServerEventProxyDelegate(string eventName, params object[] payload);
        internal delegate void TriggerEventProxyDelegate(string eventName, params object[] payload);
        /*{
            _triggerServerEventProxy(eventName, payload);
        }*/

        internal virtual void OnHuntStarted(GameState gameState, PlayerState playerState)
        {

        }

        internal virtual void OnHuntEnded(GameState gameState, PlayerState playerState)
        {

        }

        internal virtual void OnResourceStopping()
        {

        }

        internal virtual bool DoesPlayerNeedInvincibility { get { return false; } }

        internal virtual void OnClockReceived(int hours, int minutes, int seconds)
        {

        }

        internal virtual bool CanPingShow { get { return true; } }

        internal virtual SurviveTheHuntShared.Core.Teams.Team? WinningTeamOverride => null;

        internal virtual string CustomWastedText => null;
        internal virtual bool SkipAddingPlayerNameInObjective => false;

        internal virtual LabelledItem[] UICurrentItems => new LabelledItem[0];

        internal virtual SurviveTheHuntShared.Utils.Coord[] CarSpawnPointsOverride => null;

        internal virtual bool? IsVehicleWeaponAllowed(int vehicleHandle, uint weapon)
        {
            return null;
        }

        internal virtual void OnPlayerSpawned()
        {

        }
    }
}
