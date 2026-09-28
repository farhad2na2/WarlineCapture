using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadSupport(out UiSupportModel model)
        { if(current is IUiSupportGateway g)return g.TryReadSupport(out model);model=default;return false; }
        public static bool SelectSupport(byte kind)=>current is IUiSupportGateway g && g.SelectSupport(kind);
        public static bool BeginSupplyCollection()=>current is IUiSupportGateway g && g.BeginSupplyCollection();
        public static bool BeginSupportTargeting()=>current is IUiSupportGateway g && g.BeginSupportTargeting();
        public static bool PreviewSupport(Vector3 position)=>current is IUiSupportGateway g && g.PreviewSupport(position);
        public static bool PreviewSupportPointer(Vector2 screen,Vector3 ground)=>current is IUiSupportGateway g && g.PreviewSupportPointer(screen,ground);
        public static bool ConfirmSupport()=>current is IUiSupportGateway g && g.ConfirmSupport();
        public static void CancelSupport() { if(current is IUiSupportGateway g)g.CancelSupport(); }
        public static bool ProposeSupport()=>current is IUiSupportGateway g && g.ProposeSupport();
        public static bool ApproveSupport()=>current is IUiSupportGateway g && g.ApproveSupport();
        public static void DeclineSupport() { if(current is IUiSupportGateway g)g.DeclineSupport(); }
        public static void ShowSupportTarget() { if(current is IUiSupportGateway g)g.ShowSupportTarget(); }
    }
}
