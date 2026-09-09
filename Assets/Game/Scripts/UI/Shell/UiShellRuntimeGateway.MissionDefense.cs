using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadMissionCameraTour()=>current is IUiMissionDefenseGateway gateway && gateway.TryReadMissionCameraTour();
        public static bool TryReadMissionRadioArchive()=>current is IUiMissionDefenseGateway gateway && gateway.TryReadMissionRadioArchive();
        public static bool IsMissionFieldGuidePresenting()=>current is IUiMissionDefenseGateway gateway && gateway.IsMissionFieldGuidePresenting();
        public static bool TryReadMissionDefense(out UiMissionDefenseModel model)
        {
            model=default;
            return current is IUiMissionDefenseGateway gateway && gateway.TryReadMissionDefense(out model);
        }
        public static bool TryRequestMissionDefenseAction(UiMissionDefenseAction action, int warningElementIndex=-1, int guidanceId=0) =>
            current is IUiMissionDefenseGateway gateway && gateway.TryRequestMissionDefenseAction(action,warningElementIndex,guidanceId);
    }
}
