namespace Game.UI.Runtime
{
    public sealed partial class MainMenuPlayUI
    {
        private ResourceExchangePopupView _resourceExchangePopupView;
        private ResourceExchangePopupRuntimeView resourceExchangeRuntimeView;

        public void BindResourceExchangePopup(ResourceExchangePopupView view)
        {
            _resourceExchangePopupView = view;
            resourceExchangeRuntimeView = view != null ? view.GetComponent<ResourceExchangePopupRuntimeView>() : null;
        }

        internal bool OwnsResourceExchangePopup(ResourceExchangePopupRuntimeView view) =>
            view != null && resourceExchangeRuntimeView == view;

        public void RefreshResourceExchangePopup()
        {
            if(resourceExchangeRuntimeView != null && resourceExchangeRuntimeView.isActiveAndEnabled)
                resourceExchangeRuntimeView.RefreshNow();
        }
    }
}
