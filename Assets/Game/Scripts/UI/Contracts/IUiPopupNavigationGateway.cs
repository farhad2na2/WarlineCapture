namespace Game.UI.Contracts
{
    public interface IUiPopupNavigationGateway
    {
        bool TryHidePopup(UiShellPopupKind kind);
    }
}
