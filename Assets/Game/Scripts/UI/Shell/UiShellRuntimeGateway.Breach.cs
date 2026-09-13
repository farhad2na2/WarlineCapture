using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadBreachInputMode(out int mode) {mode=0;return current is IUiMissionBreachGateway gateway && gateway.TryReadBreachInputMode(out mode);}
        public static bool IsBreachGuideContext() => current is IUiMissionBreachGateway gateway && gateway.IsBreachGuideContext();
        public static bool TrySelectBreachActor() => current is IUiMissionBreachGateway gateway && gateway.TrySelectBreachActor();
        public static bool TryContinueBreachPlan() => current is IUiMissionBreachGateway gateway && gateway.TryContinueBreachPlan();
    }
}
