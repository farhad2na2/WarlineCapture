using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadSkirmish(out UiSkirmishModel model)
        {model=default;return current is IUiSkirmishGateway gateway && gateway.TryReadSkirmish(out model);}
        public static bool TryRequestSkirmish(UiSkirmishAction action) =>
            current is IUiSkirmishGateway gateway && gateway.TryRequestSkirmish(action);
    }
}
