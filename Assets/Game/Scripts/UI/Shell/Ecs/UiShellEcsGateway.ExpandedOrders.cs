using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiExpandedSkirmishCommandGateway
    {
        bool IUiExpandedSkirmishCommandGateway.TryReadExpandedSquadPage(out UiExpandedSquadPage page)
        {
            page = default;
            if (!TrySkirmish(out EntityManager em, out Entity session, out _))
                return false;
            if (!SkirmishExpandedPresentedOrders.TryReadPage(
                    em,
                    session,
                    out int pageIndex,
                    out bool nextPage,
                    out SkirmishPresentedSlot slot0,
                    out SkirmishPresentedSlot slot1,
                    out SkirmishPresentedSlot slot2,
                    out SkirmishPresentedSlot slot3))
                return false;

            page.Expanded = true;
            page.PageIndex = pageIndex;
            page.NextPage = nextPage;
            page.AssaultMask = Mask(slot0, 0, true) | Mask(slot1, 1, true) | Mask(slot2, 2, true) | Mask(slot3, 3, true);
            page.SelectedMask = Mask(slot0, 0, false) | Mask(slot1, 1, false) | Mask(slot2, 2, false) | Mask(slot3, 3, false);
            page.StructureMask = StructureBit(slot0, 0) | StructureBit(slot1, 1) | StructureBit(slot2, 2) | StructureBit(slot3, 3);
            page.AttackOrderMask = OrderBit(slot0, 0) | OrderBit(slot1, 1) | OrderBit(slot2, 2) | OrderBit(slot3, 3);
            return true;
        }

        bool IUiExpandedSkirmishCommandGateway.TrySelectExpandedPresentedSlot(int slotIndex)
        {
            if (!TrySkirmish(out EntityManager em, out Entity session, out _))
                return false;
            return SkirmishExpandedPresentedOrders.TryPresentedSlot(em, session, slotIndex);
        }

        bool IUiExpandedSkirmishCommandGateway.TryHoldExpandedSelection()
        {
            if (!TrySkirmish(out EntityManager em, out Entity session, out _))
                return false;
            return SkirmishExpandedPresentedOrders.TryHoldSelection(em, session);
        }

        bool IUiExpandedSkirmishCommandGateway.TryAttackExpandedEnemyBase()
        {
            if (!TrySkirmish(out EntityManager em, out Entity session, out _))
                return false;
            return SkirmishExpandedPresentedOrders.TryAttackEnemyBase(em, session);
        }

        private static int Mask(in SkirmishPresentedSlot slot, int index, bool assault)
        {
            if (!slot.Occupied)
                return 0;
            if (assault)
                return slot.Assault ? 1 << index : 0;
            return slot.Selected ? 1 << index : 0;
        }

        private static int StructureBit(in SkirmishPresentedSlot slot, int index)
        {
            return slot.Occupied && slot.Structure ? 1 << index : 0;
        }

        private static int OrderBit(in SkirmishPresentedSlot slot, int index)
        {
            return slot.Occupied && slot.AttackOrdered ? 1 << index : 0;
        }
    }
}
