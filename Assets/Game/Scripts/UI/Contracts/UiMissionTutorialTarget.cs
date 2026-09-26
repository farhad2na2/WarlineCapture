using UnityEngine;
namespace Game.UI.Contracts
{
    public enum UiTutorialBattleAction : byte { None, Move, Hold, Watch, Attack }
    public readonly struct UiMissionTutorialTarget
    {
        public readonly Vector3 Selection, Destination;
        public readonly bool NeedsSelection, Moving, ExecutingAttack;
        public readonly int RequiredSelectionCount;
        public readonly float AreaRadius;
        public readonly UiTutorialBattleAction BattleAction;
        public readonly string SelectionLabelKey;
        public readonly Vector3 SelectionMin, SelectionMax;
        public readonly bool DragSelection;
        public UiMissionTutorialTarget(Vector3 selection, Vector3 destination, bool needsSelection, bool moving, int requiredSelectionCount=1, UiTutorialBattleAction battleAction=UiTutorialBattleAction.None, string selectionLabelKey="ui.aria.select_group", bool executingAttack=false, float areaRadius=0,
            bool dragSelection=false,Vector3 selectionMin=default,Vector3 selectionMax=default)
        { Selection=selection; Destination=destination; NeedsSelection=needsSelection; Moving=moving; ExecutingAttack=executingAttack; AreaRadius=areaRadius; RequiredSelectionCount=requiredSelectionCount; BattleAction=battleAction; SelectionLabelKey=selectionLabelKey; DragSelection=dragSelection; SelectionMin=selectionMin; SelectionMax=selectionMax; }
    }
    public interface IUiMissionTutorialTargetGateway
    {
        bool TryReadMissionTutorialTarget(out UiMissionTutorialTarget target);
        bool TrySelectMissionTutorialGroup();
    }
}
