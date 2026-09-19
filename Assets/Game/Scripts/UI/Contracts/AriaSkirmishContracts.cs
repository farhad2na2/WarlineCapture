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
    public enum AriaSkirmishIntent : byte { Recruit, SelectSquad, FindBase, Attack, TargetBase, ObserveBattle, FindThreat, TargetThreat }
    [System.Serializable]
    public struct AriaSkirmishObservation
    {
        public bool Active, Finished, DrawerOpen, SelectionVisible, AttackMode, MapOpen, MapContactInView;
        public int SelectedSlot, AvailableSquads, Infantry, Frame;
        public float Time, PlayerHealth, EnemyHealth, ForceHealth;
        public AriaTouchTarget Squad0, Squad1, Squad2, Squad3, Squad4;
        public AriaTouchTarget FocusEnemy, Attack, EnemyBase, Recruit, CloseDrawer, Threat, FocusThreat, CloseMap;
        public AriaTouchTarget Squad(int index) => index switch
        { 0 => Squad0, 1 => Squad1, 2 => Squad2, 3 => Squad3, _ => Squad4 };
    }
}
