using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiPopupNavigationGateway
    {
        public bool TryHidePopup(UiShellPopupKind kind)
        {
            if(!TryGetBoundary(out var em,out var boundary)||!em.HasBuffer<UiShellPopupRequestComponent>(boundary))return false;
            em.GetBuffer<UiShellPopupRequestComponent>(boundary).Add(new UiShellPopupRequestComponent { PopupKind=kind,Intent=UiShellPopupIntent.Hide });
            return true;
        }
    }
}
