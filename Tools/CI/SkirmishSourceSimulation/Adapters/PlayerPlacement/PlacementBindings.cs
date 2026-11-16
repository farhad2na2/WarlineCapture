using Game.Components;namespace Game.Runtime {
internal sealed class SourceHostPlacementCommit {internal readonly struct CommitOutcome {
 internal readonly RuntimeBuildingEntity AutoSelectBuilding;internal readonly int CommittedInstanceCount,ExpectedInstanceCount;
 internal bool PlacementCommitted=>CommittedInstanceCount>0;internal bool FullyCommitted=>ExpectedInstanceCount>0&&CommittedInstanceCount==ExpectedInstanceCount;
 internal CommitOutcome(RuntimeBuildingEntity building,int committed,int expected){AutoSelectBuilding=building;CommittedInstanceCount=committed;ExpectedInstanceCount=expected;}
}}
internal sealed class SourceHostPlacementLifecycle {
 internal PlacementState ActivePlacement;private readonly SourceHostPlacementTransaction transaction=new SourceHostPlacementTransaction();
 internal bool Confirm(int transactionId,ConfirmContext context,out ConfirmFailureReason failureReason)=>transaction.Confirm(ActivePlacement,transactionId,context,out failureReason);
 internal bool Confirm(ConfirmContext context,out ConfirmFailureReason failureReason)=>transaction.Confirm(ActivePlacement,context,out failureReason);
 internal sealed class PlacementState {internal bool IsValid;internal RuntimeDefinition Definition;internal Vector2Int OriginCell,CommittedOriginCell,DragStartOriginCell,DragCurrentOriginCell;internal int DragFirstAxis;internal bool AutoRotateVertical,HideCurrentWallPreview;internal void ApplyHover(Vector2Int origin){OriginCell=origin;CommittedOriginCell=origin;DragCurrentOriginCell=origin;DragFirstAxis=BuildingPlacementInitialPreviewState.HoverAxis(DragStartOriginCell,origin);}}
 internal enum ConfirmFailureReason {None,MissingActivePlacement,BlockedPlacement,InvalidPlacement,InsufficientCredits,InsufficientMaterials,InsufficientCreditsAndMaterials,DuplicateTransaction,TransactionRejected,RegistrationFailed}
 internal delegate bool ValidateConfirmDelegate(PlacementState p);internal delegate FactionConstructionResourceMutationResult TryReserveCostDelegate(int id,int c,int m);internal delegate FactionConstructionResourceMutationResult SettleCostDelegate(int id);internal delegate SourceHostPlacementCommit.CommitOutcome CommitPlacementDelegate(PlacementState p);
 internal struct ConfirmContext {internal ValidateConfirmDelegate ValidateConfirm;internal TryReserveCostDelegate TryReserveCost;internal SettleCostDelegate FinalizeCost,RollbackCost;internal CommitPlacementDelegate CommitPlacement;}
}}
