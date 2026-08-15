using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadMissionHudRestrictions(out UiMissionHudRestrictionsModel restrictions)
        {
            restrictions = UiMissionHudRestrictionsModel.Inactive;
            return current is IUiMissionHudRestrictionsGateway missionGateway &&
                   missionGateway.TryReadMissionHudRestrictions(out restrictions) &&
                   restrictions.IsActive;
        }
    }
}
