using UnityEngine;
namespace Game.UI.Contracts
{
    // Only displayed controls/objective markers and HUD values cross this boundary.
    [System.Serializable]
    public struct AriaTouchTarget
    {
        public int Id;
        public Vector2 Position;
        public bool Available;
    }
    public enum AriaSkirmishIntent : byte { Recruit, SelectSquad, FindBase, Attack, TargetBase, ObserveBattle, FindThreat, TargetThreat, GroupForce, DefendBase, BuildDefense, Advance }
    [System.Serializable]
    public struct AriaSkirmishObservation
    {
        public bool Active, Finished, DrawerOpen, SelectionVisible, AttackMode, MapOpen, MapContactInView, SelectionMode, FocusThreatDrag, FocusGroupDrag, FocusAdvanceDrag, PlacementOpen, AdvancePreferred, ThreatNearForce, AssaultAtBase;
        public Vector2 FocusThreatDragEnd, FocusGroupDragEnd, FocusAdvanceDragEnd;
        public int SelectedSlot, SelectedCount, AvailableSquads, Infantry, Frame;
        public float Time, PlayerHealth, EnemyHealth, ForceHealth;
        public AriaTouchTarget Squad0, Squad1, Squad2, Squad3, Squad4;
        public AriaTouchTarget Site0, Site1, Site2, Site3, Site4, Site5;
        public AriaTouchTarget Site(int index) => index switch { 0 => Site0, 1 => Site1, 2 => Site2, 3 => Site3, 4 => Site4, _ => Site5 };
        public AriaTouchTarget AdvanceGround, ThreatGround, FocusAdvance, DefenseBuild, PlacementConfirm, PlacementCancel, FocusPlayer, Select, GroupStart, GroupEnd, FocusEnemy, Attack, Hold, EnemyBase, Recruit, CloseDrawer, Threat, FocusThreat, FocusGroup, CloseMap;
        public AriaTouchTarget Squad(int index) => index switch
        { 0 => Squad0, 1 => Squad1, 2 => Squad2, 3 => Squad3, _ => Squad4 };
    }
}
