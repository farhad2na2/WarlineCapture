using Game.Skirmish.Contracts;
using System;
namespace Game.Simulation {
public enum AriaS002Scope:byte {None,NativeRendered,LogicalPublicData}
public enum AriaS002Action:byte {None,InspectHome,InspectObjective,InspectCohort,OpenInspection,CloseInspection,SelectMode,SelectSquad,PreviousPage,NextPage,ToggleSelected,ArmAttack,AttackVisibleObjective,AttackPublicTerrain,Hold,RecruitRifle,RecruitAntiAir,RecruitRocketeer,RecruitLogisticsTruck,RecruitTanker,CloseCatalog,CancelPlacement,UpgradeReadiness,UnsupportedAir}
public readonly struct AriaS002Point {public readonly float X,Y;public AriaS002Point(float x,float y){X=x;Y=y;}}
public enum AriaS002Intent : byte
    {
        Recruit, SelectSquad, FindBase, Attack, TargetBase, ObserveBattle, FindThreat, TargetThreat,
        GroupForce, DefendBase, BuildDefense, Advance, Hold, Inspect, Scout, Handback,
        BuildAirPad, WaitForAirPadSite, ReturnAircraft, ServiceAircraft,
        ControlZone, OpenObjectiveCorridor, EvacuateOriginals, EscortOriginalTruck, InspectObjective, ObserveObjective, UnloadOriginals, RepairVehicle, ServiceOriginalTruck, StopDamagedTruck
    }
public enum AriaS002ContactPhase : byte { Manual, Starting, Observing, Aiming, Touching, Verifying, Waiting, Blocked }
public enum AriaS002DecisionKind : byte { Unavailable, Control, WorldTarget, Waiting, Cinematic, Finished }
// Exact source FixedList512Bytes<uint> capacity; value storage, no allocations or Unity collections.
public unsafe struct AriaS002GroupHistory {
 public const int Maximum=127;private fixed uint values[Maximum];private int length;public int Length=>length;public int Capacity=>Maximum;
 public uint this[int index] {get{if((uint)index>=(uint)length)throw new ArgumentOutOfRangeException(nameof(index));fixed(uint* p=values)return p[index];}}
 public void Add(uint value){if((uint)length>=Maximum)throw new InvalidOperationException("Source precision history full");fixed(uint* p=values)p[length++]=value;}
}
public struct AriaS002ActionTarget {public AriaS002Action Action;public int Slot,Id;public AriaS002Point Position,DragEnd;public bool Admitted,Drag;public int ScrollTargetId;public float ScrollDistanceViewports;}
public struct AriaS002CommandReceipt {public AriaS002Scope Scope;public bool ResultKnown,ScopeReleaseVerified;public uint Revision,GroupId;public int Frame,RequestId,PublicTargetId,AcceptedMembers,SourceMembers;}
public struct AriaS002PublicSnapshot {
        public AriaS002ActionTarget OpenMap;
        public int Identity, MarkerFrame, ObjectiveHudIdentity;
        public uint MarkerRevision;
        public float MarkerTime;
        public AriaGroundPosition ObjectiveWorld;
        public bool OwnedIdentity, InspectionScopeOpen, Complete, SourceScopeAdmitted;
        public bool IsCurrent(int frame, float now) =>
            OwnedIdentity && Identity != 0 && ObjectiveHudIdentity != 0 &&
            float.IsFinite(ObjectiveWorld.X) && float.IsFinite(ObjectiveWorld.Y) && float.IsFinite(ObjectiveWorld.Z) &&
            Complete && SourceScopeAdmitted && MarkerRevision != 0 && MarkerFrame > 0 &&
            MarkerFrame <= frame && float.IsFinite(MarkerTime) &&
            float.IsFinite(now) && now >= MarkerTime && now - MarkerTime <= 1f;
    }
public struct AriaS002Observation {
public Game.Skirmish.Contracts.SkirmishPublicSelectedApproach SelectedOwnApproach;
public AriaS002Scope Scope;public int SourceScenarioIndex;
public bool Active;
public bool Finished;
public bool CatalogOpen;
public bool SelectionVisible;
public bool AttackMode;
public bool InspectionOpen;
public bool MapContactInView;
public bool SelectionMode;
public bool FocusThreatDrag;
public bool FocusGroupDrag;
public bool FocusAdvanceDrag;
public bool PlacementOpen;
public bool AdvancePreferred;
public bool ThreatNearForce;
public bool AssaultAtBase;
public bool ExpandedSession;
public bool PlayerDesignatedAlive;
public bool EnemyDesignatedAlive;
public bool CanAffordRifle;
public bool CanAffordRocketeer;
public bool CanAffordTank;
public bool CanAffordAntiAir;
public bool PadReady;
public bool AirQueueOffered;
public bool PadPresent;
public bool ReadinessEligible;
public bool CanUpgradeReadiness;
public bool CanAffordLogisticsTruck;
public bool LogisticsTruckCommitted;
public bool RifleRecruitPending;
public bool AirProfile;
public bool CanAffordTanker;
public int TankersCommitted;
public bool CombinedArmsField;
public bool RocketeerRecruitPending;
public bool PublicHomeThreat;
public int OwnStructureAttackUnits;
public bool CanBuildAirPad;
public bool AirRecruitPending;
public bool SelectedAircraft;
public int OwnAttackAirLive;
public int OwnAttackAirActive;
public int OwnAttackAirLanded;
public int OwnAirFuel;
public int OwnDefenseTowers;
public bool AntiAirCommitted;
public AriaS002Point FocusThreatDragEnd;
public AriaS002Point FocusGroupDragEnd;
public AriaS002Point FocusAdvanceDragEnd;
public int SelectedSlot;
public int SelectedCount;
public int AvailableSquads;
public int Infantry;
public int Frame;
public int VisibleHostileCombat;
public int OwnMaterials;
public int ExpandedRetries;
public int VisibleHostileAir;
public float Time;
public float PlayerHealth;
public float EnemyHealth;
public float ForceHealth;
public AriaS002PublicSnapshot PublicSnapshot;
public bool AssaultDistanceKnown;
public float NearestFriendlyObjectiveDistance;
public AriaS002ActionTarget Squad0;
public AriaS002ActionTarget Squad1;
public AriaS002ActionTarget Squad2;
public AriaS002ActionTarget Squad3;
public AriaS002ActionTarget Squad4;
public AriaS002ActionTarget PreviousSquadPage;
public AriaS002ActionTarget NextSquadPage;
public AriaS002ActionTarget SelectedSquadToToggle;
public AriaS002ActionTarget Site0;
public AriaS002ActionTarget Site1;
public AriaS002ActionTarget Site2;
public AriaS002ActionTarget Site3;
public AriaS002ActionTarget Site4;
public AriaS002ActionTarget Site5;
public AriaS002ActionTarget PadSite0;
public AriaS002ActionTarget PadSite1;
public AriaS002ActionTarget PadSite2;
public AriaS002ActionTarget PadSite3;
public AriaS002ActionTarget AdvanceGround;
public AriaS002ActionTarget ThreatGround;
public AriaS002ActionTarget FocusAdvance;
public AriaS002ActionTarget DefenseBuild;
public AriaS002ActionTarget PlacementConfirm;
public AriaS002ActionTarget PlacementCancel;
public AriaS002ActionTarget FocusPlayer;
public AriaS002ActionTarget Select;
public AriaS002ActionTarget GroupStart;
public AriaS002ActionTarget GroupEnd;
public AriaS002ActionTarget FocusEnemy;
public AriaS002ActionTarget Attack;
public AriaS002ActionTarget Hold;
public AriaS002ActionTarget EnemyBase;
public AriaS002ActionTarget Recruit;
public AriaS002ActionTarget CloseDrawer;
public AriaS002ActionTarget Threat;
public AriaS002ActionTarget FocusThreat;
public AriaS002ActionTarget FocusGroup;
public AriaS002ActionTarget CloseMap;
public AriaS002ActionTarget RecruitAntiAir;
public AriaS002ActionTarget AirPad;
public AriaS002ActionTarget UpgradeReadiness;
public AriaS002ActionTarget RecruitAir;
public AriaS002ActionTarget OpenAircraftActions;
public AriaS002ActionTarget ReturnAircraft;
public AriaS002ActionTarget RecruitLogisticsTruck;
public AriaS002ActionTarget RecruitTanker;
public AriaS002ActionTarget RecruitRocketeer;
public int ExpandedAssaultMask;
public int ExpandedSelectedMask;
public int ExpandedStructureMask;
public int ExpandedAttackOrderMask;
public SkirmishPublicArmyOrderGoal CurrentAssaultGoal;
public bool ExpandedBaseAssault;
public uint ExpandedGroup0;
public uint ExpandedGroup1;
public uint ExpandedGroup2;
public uint ExpandedGroup3;
public uint ExpandedGroup4;
public AriaS002CommandReceipt PrecisionAttack;
public int ExpandedAirMask;
public bool ExpandedCanPreviousPage;
public bool ExpandedCanNextPage;
public int ExpandedPageIndex;
public int ExpandedPageCount;
public int ExpandedTotalGroups;
public int ExpandedSelectedGroups;
public int ExpandedSelectedOffPage;
public uint ExpandedRosterRevision;
public AriaS002ActionTarget Squad(int index) => index switch
        { 0 => Squad0, 1 => Squad1, 2 => Squad2, 3 => Squad3, 4 => Squad4, _ => default };
public uint ExpandedGroup(int slot) => slot switch {
            0=>ExpandedGroup0,1=>ExpandedGroup1,2=>ExpandedGroup2,3=>ExpandedGroup3,4=>ExpandedGroup4,_=>0 };
}
public struct AriaS002PlanState {
public AriaS002Intent Intent;
public int DefenseSite;
public int DefenseSiteAction;
public int DefenseSitePending;
public int DefensePositioned;
public int DefenseStage;
public int DefenseActions;
public int DefensesPlaced;
public int Slot;
public int ActionsAtTarget;
public int Cycle;
public int RecruitBurstRemaining;
public int Infantry;
public int MapFocusActions;
public int GroupStage;
public int GroupAction;
public int RecruitAction;
public int MapNavigationStage;
public int OpeningSquads;
public float DefenseDeadline;
public float DefenseReadyAt;
public float RecruitUnavailableAt;
public float NextRecruitAt;
public float ObserveUntil;
public float LastProgressAt;
public float EnemyHealth;
public float PlayerHealth;
public float ForceHealth;
public float RegroupAt;
public float GroupReadyAt;
public float OpeningUntil;
public float MapNavigationReadyAt;
public float MapOpenedAt;
public byte AdvanceNavigation;
public byte TargetPending;
public byte RecruitDrawerSeen;
public byte AssaultStarted;
public byte ExpandedRetries;
public byte PagedToAssault;
public byte AssaultSelecting;
public byte AssaultIssued;
public SkirmishPublicArmyOrderGoal ObservedAssaultGoal;
public byte StructureOrdered;
public AriaS002GroupHistory PrecisionAcceptedGroups;
public AriaS002GroupHistory PrecisionExhaustedGroups;
public byte PrecisionStage;
public byte PrecisionAttempts;
public byte AssaultFocusStage;
public int AssaultFocusActions;
public int AssaultFocusTarget;
public int AssaultFocusFrame;
public float AssaultFocusDeadline;
public uint PrecisionGroup;
public uint PrecisionRoster;
public uint PrecisionBaseline;
public int PrecisionSlot;
public int PrecisionPage;
public int PrecisionIssueFrame;
public int PrecisionActions;
public float PrecisionDeadline;
public float PrecisionReadyAt;
public int SelectionResetPage;
public int AssaultPageSeen;
public int PagesOrdered;
public uint SweepRosterRevision;
public int EconomicMilestones;
public int MaterialsHighWater;
public int ObservedPageIndex;
public int HomeDefenseStage;
public int HomeDefenseAction;
public int HomeDefensePage;
public float HomeDefenseObserveUntil;
public byte AssaultDistanceObserved;
public float BestAssaultDistance;
public int AssaultObjectiveHudIdentity;
public AriaGroundPosition AssaultObjectiveWorld;
public byte AssaultInspectStage;
public int AssaultInspectActions;
public int AssaultInspectOpenTarget;
public int AssaultInspectMapIdentity;
public int AssaultInspectOpenedFrame;
public uint AssaultInspectRevision;
public float AssaultInspectDeadline;
public float AssaultInspectReadyAt;
public int AssaultInspectObjectiveHudIdentity;
public AriaGroundPosition AssaultInspectObjectiveWorld;
public int PadStage;
public int PadSiteIndex;
public int PadAction;
public int PadTargetId;
public int PadFeedbackFrame;
public float PadDeadline;
public float PadReadyAt;
public int AirCycleStage;
public int AirCycleAction;
public int AirCycleCount;
public float AirSortieObservedAt;
public float AirCycleDeadline;
public float AirServiceReadyAt;
}
public struct AriaS002ContactState {
public AriaS002ContactPhase Phase;
public int TargetId;
public int GoalId;
public int Attempts;
public int Actions;
public int LastCompletedTargetId;
public int LastCompletedFrame;
public AriaS002Point LastCompletedPosition;
public int ScrollTargetId;
public int ScrollCreditedActions;
public float BestScrollDistanceViewports;
public AriaS002Point Target;
public AriaS002Point Contact;
public AriaS002Point DragEnd;
public byte Drag;
public float DueAt;
public float LastProgressAt;
public float LastObjectiveProgressAt;
public ulong VisitedGoalMask;
public byte ObjectiveWatchdogInitialized;
public byte GestureRequested;
public byte Pressed;
public byte StopReason;
}
public struct AriaS002Decision {
public AriaS002Action Action;public int Slot;public AriaS002Intent Intent;
public AriaS002DecisionKind Kind;
public int TargetId;
public int GoalId;
public int Frame;
public AriaS002Point Position;
public AriaS002Point DragEnd;
public byte Drag;
public int ScrollTargetId;
public float ScrollDistanceViewports;
public float Time;
}
}
