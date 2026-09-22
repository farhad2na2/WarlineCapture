using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadOperationsMission(out UiOperationsMissionModel model)
        {
            model = default;
            return current is IUiOperationsMissionGateway gateway && gateway.TryReadOperationsMission(out model);
        }
        public static bool TryRequestOperationsMission(UiOperationsMissionAction action, int siteIndex = 0) =>
            current is IUiOperationsMissionGateway gateway && gateway.TryRequestOperationsMission(action, siteIndex);
    }
}
