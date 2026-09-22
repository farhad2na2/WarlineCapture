using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
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

        public static bool TryHoldExpandedSelection() =>
            current is IUiExpandedSkirmishCommandGateway gateway &&
            gateway.TryHoldExpandedSelection();

        public static bool TryAttackExpandedEnemyBase() =>
            current is IUiExpandedSkirmishCommandGateway gateway &&
            gateway.TryAttackExpandedEnemyBase();
    }
}
