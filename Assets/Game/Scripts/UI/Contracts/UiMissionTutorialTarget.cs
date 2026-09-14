using UnityEngine;
namespace Game.UI.Contracts
{
    public readonly struct UiMissionTutorialTarget
    {
        public readonly Vector3 Selection, Destination;
        public readonly bool NeedsSelection, Moving;
        public readonly int RequiredSelectionCount;
        public UiMissionTutorialTarget(Vector3 selection, Vector3 destination, bool needsSelection, bool moving, int requiredSelectionCount=1)
        { Selection=selection; Destination=destination; NeedsSelection=needsSelection; Moving=moving; RequiredSelectionCount=requiredSelectionCount; }
    }
    public interface IUiMissionTutorialTargetGateway
    {
        bool TryReadMissionTutorialTarget(out UiMissionTutorialTarget target);
    }
}
