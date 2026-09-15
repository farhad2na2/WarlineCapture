using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadMissionTutorialTarget(out UiMissionTutorialTarget target)
        { target=default; return current is IUiMissionTutorialTargetGateway gateway && gateway.TryReadMissionTutorialTarget(out target); }
        public static bool TrySelectMissionTutorialGroup()
            => current is IUiMissionTutorialTargetGateway gateway && gateway.TrySelectMissionTutorialGroup();
    }
}
