namespace Game.Missions.Contracts
{
    public enum MissionGuidanceActionKind : byte
    { Explain=0, ReadWarning=1, FocusWarning=2, OpenBuild=3, Move=4, Hold=5, Stop=6, RadarPing=7, OpenProduction=8, InspectContact=9, ReviewResult=10 }
    public enum MissionGuidanceCompletionKind : byte
    { Acknowledged=0, WarningRead=1, RouteInspected=2, DefenseBuilt=3, SquadPositioned=4, Holding=5, StopAccepted=6, PingAccepted=7, ReinforcementProduced=8, HostileDefeated=9, MainElementActivated=10, ResultSettled=11 }
}
