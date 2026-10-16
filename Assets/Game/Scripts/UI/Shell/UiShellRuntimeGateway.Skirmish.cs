using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool HasSavedSkirmish(string catalogId) => current is IUiSkirmishGateway gateway && gateway.HasSavedSkirmish(catalogId);
        public static bool TryConsumeSkirmishResumeFailure() => current is IUiSkirmishGateway gateway && gateway.TryConsumeSkirmishResumeFailure();
        public static bool TryResumeSavedSkirmish(string catalogId) => current is IUiSkirmishGateway gateway && gateway.TryResumeSavedSkirmish(catalogId);
        public static bool TryDiscardSavedSkirmish(string catalogId) => current is IUiSkirmishGateway gateway && gateway.TryDiscardSavedSkirmish(catalogId);
        public static bool TryReadSkirmishReadiness(out UiSkirmishReadinessModel model)
        {
            model = default;
            return current is IUiSkirmishReadinessGateway gateway && gateway.TryReadSkirmishReadiness(out model);
        }
        public static bool TryRequestSkirmishReadiness(uint cancelResearchId = 0) =>
            current is IUiSkirmishReadinessGateway gateway && gateway.TryRequestSkirmishReadiness(cancelResearchId);

        public static bool TryReadSkirmishContext(out bool expanded)
        {
            expanded = false;
            return current is IUiSkirmishGateway gateway && gateway.TryReadSkirmishContext(out expanded);
        }

        public static bool TryReadSkirmish(out UiSkirmishModel model)
        {model=default;return current is IUiSkirmishGateway gateway && gateway.TryReadSkirmish(out model);}
        public static bool TryRequestSkirmish(UiSkirmishAction action) =>
            current is IUiSkirmishGateway gateway && gateway.TryRequestSkirmish(action);

        public static bool TryReadExpandedSquadPage(out UiExpandedSquadPage page)
        {
            page = default;
            return current is IUiExpandedSkirmishCommandGateway gateway &&
                   gateway.TryReadExpandedSquadPage(out page);
        }

        public static bool TrySelectExpandedPresentedSlot(int slotIndex) =>
            current is IUiExpandedSkirmishCommandGateway gateway &&
            gateway.TrySelectExpandedPresentedSlot(slotIndex);

        public static bool TrySelectExpandedGroup(uint groupId) =>
            current is IUiExpandedSkirmishCommandGateway gateway && gateway.TrySelectExpandedGroup(groupId);

        public static bool TryHoldExpandedSelection() =>
            current is IUiExpandedSkirmishCommandGateway gateway &&
            gateway.TryHoldExpandedSelection();

        public static bool TryChangeExpandedSquadPage(int delta) =>
            current is IUiExpandedSkirmishCommandGateway gateway && gateway.TryChangeExpandedSquadPage(delta);

        public static bool TryClearExpandedSquadSelection() =>
            current is IUiExpandedSkirmishCommandGateway gateway && gateway.TryClearExpandedSquadSelection();


    }
}
