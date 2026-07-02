using Game.UI.Contracts;

namespace Game.Runtime
{
    public sealed class RoadMinimapEventUiSystemHelper
    {
        private IMatchRuntimeUi _mainMenuPlayUi;
        private bool _staticMinimapChanged;

        public void Configure(IMatchRuntimeUi mainMenuPlayUi)
        {
            _mainMenuPlayUi = mainMenuPlayUi;
        }

        public void PublishStaticMinimapChanged()
        {
            _staticMinimapChanged = true;
            Flush();
        }

        public void Flush()
        {
            if (!_staticMinimapChanged)
                return;

            _staticMinimapChanged = false;
            _mainMenuPlayUi?.NotifyStaticMinimapChanged();
        }

        public void Clear()
        {
            _staticMinimapChanged = false;
            _mainMenuPlayUi = null;
        }
    }
}
