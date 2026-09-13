using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryFocusMissionTutorialTarget(bool selection) =>
            current is IUiMissionTutorialFocusGateway gateway && gateway.TryFocusMissionTutorialTarget(selection);
    }
}
