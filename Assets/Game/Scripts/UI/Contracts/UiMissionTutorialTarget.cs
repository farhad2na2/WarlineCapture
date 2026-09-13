using UnityEngine;
namespace Game.UI.Contracts
{
    public readonly struct UiMissionTutorialTarget
    {
        public readonly Vector3 Selection, Destination;
        public readonly bool NeedsSelection, Moving;
        public UiMissionTutorialTarget(Vector3 selection, Vector3 destination, bool needsSelection, bool moving)
        { Selection=selection; Destination=destination; NeedsSelection=needsSelection; Moving=moving; }
    }
    public interface IUiMissionTutorialTargetGateway
    {
        bool TryReadMissionTutorialTarget(out UiMissionTutorialTarget target);
    }
}
