using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Controllers;

namespace SurviveTheHuntClient.Plugins.Cupid.Managers
{
    internal class RepairShopManager : IHuntLifecycleListener, ITickable
    {
        private readonly RepairShopController[] _all =
        {
            // Beeker's Sandy
            new RepairShopController(new CitizenFX.Core.Vector3(1182.692f, 2638.985f, 37.2134f)),
            // Beeker's Paleto
            new RepairShopController(new CitizenFX.Core.Vector3(104.6837f, 6622.418f, 31.24745f)),
            // LSC Burton
            new RepairShopController(new CitizenFX.Core.Vector3(-327.8723f, -143.9452f, 38.4774f)),
            // LSC Airport
            new RepairShopController(new CitizenFX.Core.Vector3(-1166.405f, -2012.816f, 12.57152f)),
            // LSC La Mesa
            new RepairShopController(new CitizenFX.Core.Vector3(735.4589f, -1072.964f, 21.57229f))
        };

        private RepairShopController _currentFocus = null;

        public void OnHuntEnded(IGameState gameState, IPlayerState playerState)
        {
            foreach(RepairShopController controller in _all)
            {
                controller.OnHuntEnded(gameState, playerState);
            }
        }

        public void OnHuntStarted(IGameState gameState, IPlayerState playerState)
        {
            foreach(RepairShopController controller in _all)
            {
                controller.OnHuntStarted(gameState, playerState);
            }
        }

        private const float CurrentCheckIntervalSeconds = 10f;
        private float _timeSinceCurrentCheck = 0f;
        public void Tick(float deltaTime)
        {
            _timeSinceCurrentCheck += deltaTime;

            if(_timeSinceCurrentCheck >= CurrentCheckIntervalSeconds)
            {
                _timeSinceCurrentCheck = 0f;

                _currentFocus = null;

                foreach(RepairShopController controller in _all)
                {
                    if(controller.IsInFocus)
                    {
                        _currentFocus = controller;
                        break;
                    }
                }
            }

            if(_currentFocus != null)
            {
                _currentFocus.Tick(deltaTime);
            }
            else
            {
                foreach(RepairShopController controller in _all)
                {
                    controller.Tick(deltaTime);
                }
            }
        }
    }
}
