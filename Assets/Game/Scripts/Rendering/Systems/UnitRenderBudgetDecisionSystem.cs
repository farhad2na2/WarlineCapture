using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Game.Components;

namespace Game.Rendering
{
    using UnitDistance = UnitRenderBudgetDistance.UnitDistance;

    public readonly struct UnitRenderBudgetDecision
    {
        public struct Context
        {
            public EntityCommandBuffer RenderStateEcb;
            public NativeHashSet<Entity> SafetyTaggedThisFrame;
            public NativeHashSet<Entity> ReadyTaggedThisFrame;
            public BufferLookup<Child> ChildLookup;
            public ComponentLookup<MaterialAnimationIndex> AnimationIndexLookup;
            public ComponentLookup<UnitMoveVisualComponent> MoveVisualLookup;
            public ComponentLookup<UnitMovementBehavior> MovementBehaviorLookup;
            public ComponentLookup<UnitAirMovement> AirMovementLookup;
            public ComponentLookup<UnitHealth> HealthLookup;
            public ComponentLookup<UnitDeathAnimationComponent> DeathAnimationLookup;
            public ComponentLookup<UnitSourcePrefabKey> SourcePrefabKeyLookup;
            public ComponentLookup<Faction> FactionLookup;
            public ComponentLookup<SelectedUnitTag> SelectedLookup;
            public ComponentLookup<UnitRenderVisualComponent> VisualStateLookup;
            public ComponentLookup<UnitRenderBudgetCulledUnitTag> CulledUnitLookup;
            public EntityStorageInfoLookup EntityStorageInfoLookup;
            public ComponentLookup<Disabled> DisabledLookup;
            public ComponentLookup<DisableRendering> DisableRenderingLookup;
            public ComponentLookup<UnitRenderBudgetCulledTag> CulledTagLookup;
            public NativeList<UnitDistance> Distances;
            public NativeHashSet<Entity> DetailedUnits;
            public NativeHashSet<Entity> MidLodUnits;
            public NativeHashSet<Entity> LowLodUnits;
            public NativeList<Entity> EntitiesToShow;
            public NativeList<Entity> EntitiesToHide;
            public NativeList<Entity> UnitsToShowDetailed;
            public NativeList<Entity> UnitsToShowFarImpostor;
            public bool CameraMotionActive;
            public float AlwaysDetailedDistanceSq;
            public float VisibleCharacterLowDistanceSq;
            public float VisibleCharacterImpostorNearDistance;
            public float VisibleCharacterImpostorFarDistance;
            public float EnemyAlwaysDetailedDistanceSq;
            public float EnemyLowLodDistanceSq;
            public float EnemyImpostorDistanceSq;
            public UnitRenderBudgetClassification ClassificationSystem;
            public UnitRenderBudgetCharacterPolicy CharacterPolicySystem;
            public UnitRenderBudgetLodReferences LodReferenceSystem;
            public UnitRenderBudgetLodReferences.Lookups LodReferenceLookups;
            public UnitRenderBudgetAnimationReadiness AnimationReadinessSystem;
            public UnitRenderBudgetAnimationReadiness.Lookups AnimationReadinessLookups;
            public UnitRenderBudgetRenderableState RenderableQuerySystem;
            public UnitRenderBudgetRenderableState.Lookups RenderableQueryLookups;
            public UnitRenderBudgetVisualState VisualStateSystem;
            public UnitRenderBudgetReadiness ReadinessSystem;
            public UnitRenderBudgetReadiness.Lookups ReadinessLookups;
            public UnitRenderBudgetRenderSafety RenderSafetySystem;
            public UnitRenderBudgetRenderSafety.Lookups RenderSafetyLookups;
            public UnitRenderBudgetVisualPlan VisualPlanSystem;
            public UnitRenderBudgetVisibilityChange VisibilityChangeSystem;
            public UnitRenderBudgetImpostorTag ImpostorTagSystem;
            public int CurrentFrame;
        }

        public struct Result
        {
            public int Changed;
            public int MidShown;
            public int LowShown;
            public int FarCount;
            public int MissingMidInstance;
            public int MissingLowInstance;
            public int VisualStateChanges;
            public int VisualStatePending;
            public int VisualTransitionsCommitted;
            public int VisibleCharacterSafeGate;
            public int VisibleCharacterMidInstances;
            public int VisibleCharacterSafeMidInstances;
            public int VisibleCharacterLowInstances;
            public int VisibleCharacterSafeLowInstances;
            public int VisibleCharacterUsingSafeMid;
            public int VisibleCharacterUsingSafeLow;
            public int VisibleCharacterUsingFarImpostor;
            public int VisibleCharacterForcedDetailByUnsafeMid;
            public int VisibleCharacterBudgetDetail;
            public int VisibleCharacterSafetyDetail;
            public int VisibleMidSafetyPatched;
            public int VisibleNearDetail;
            public int VisibleNearMid;
        }

        public Result Process(ref Context context)
        {
            Result result = default;

            for (int i = 0; i < context.Distances.Length; i++)
            {
                Entity unit = context.Distances[i].Unit;
                UnitRenderBudgetLodReferences.UnitReferences lodReferences = context.LodReferenceSystem.ResolveUnitReferences(
                    unit,
                    context.LodReferenceLookups);
                if (context.HealthLookup.HasComponent(unit) &&
                    context.HealthLookup[unit].Current <= 0 &&
                    new UnitDeathRenderPolicy().ShouldHideDeadLiveVisualRoots(
                        context.DeathAnimationLookup.HasComponent(unit)))
                {
                    HideDeadUnitLiveVisualRoots(ref context, lodReferences, ref result);
                    continue;
                }

                bool isCharacter = context.ClassificationSystem.IsCharacterUnit(
                    unit,
                    context.MovementBehaviorLookup,
                    context.SourcePrefabKeyLookup);
                bool isAirUnit = context.AirMovementLookup.HasComponent(unit);
                byte factionId = context.FactionLookup.HasComponent(unit) ? context.FactionLookup[unit].Id : (byte)0;
                bool isEnemyUnit = FactionIdentity.IsHostileToPlayer(factionId);
                bool isSelectedUnit = context.SelectedLookup.HasComponent(unit);
                bool isMovingUnit =
                    context.MoveVisualLookup.HasComponent(unit) &&
                    context.MoveVisualLookup[unit].IsMoving != 0;
                bool hasMidLodPrefab = lodReferences.HasMidLodPrefab;
                bool hasMidLodInstance = lodReferences.HasMidLodInstance;
                Entity midRoot = lodReferences.MidRoot;
                if (hasMidLodPrefab && !hasMidLodInstance)
                    result.MissingMidInstance++;
                bool hasLowLodPrefab = lodReferences.HasLowLodPrefab;
                bool hasLowLodInstance = lodReferences.HasLowLodInstance;
                Entity lowRoot = lodReferences.LowRoot;
                if (hasLowLodPrefab && !hasLowLodInstance)
                    result.MissingLowInstance++;
                bool waitingForLow = hasLowLodPrefab && !hasLowLodInstance && context.LowLodUnits.Contains(unit);
                bool isProtectedVisibleCharacter = isCharacter && context.Distances[i].Visible != 0;
                bool midRootSafe =
                    isCharacter &&
                    hasMidLodInstance &&
                    context.RenderableQuerySystem.IsSafeVisibleCharacterLod(midRoot, context.RenderableQueryLookups);
                bool lowRootSafe =
                    isCharacter &&
                    hasLowLodInstance &&
                    context.RenderableQuerySystem.IsSafeVisibleCharacterLod(lowRoot, context.RenderableQueryLookups);
                bool hasSafeVisibleMid =
                    midRootSafe &&
                    (isProtectedVisibleCharacter || context.RenderableQuerySystem.HasRenderableRecursive(midRoot, context.ChildLookup, context.RenderableQueryLookups));
                bool hasSafeVisibleLow =
                    lowRootSafe &&
                    (isProtectedVisibleCharacter || context.RenderableQuerySystem.HasRenderableRecursive(lowRoot, context.ChildLookup, context.RenderableQueryLookups));
                bool midRootAnimatable = hasMidLodInstance && context.AnimationReadinessSystem.HasAnimationIndexRecursive(midRoot, context.AnimationIndexLookup, context.ChildLookup);
                bool lowRootAnimatable = hasLowLodInstance && context.AnimationReadinessSystem.HasAnimationIndexRecursive(lowRoot, context.AnimationIndexLookup, context.ChildLookup);
                UnitRenderBudgetVisualPlan.Result visualPlan = context.VisualPlanSystem.CreateDesiredVisualPlan(
                    context.RenderStateEcb,
                    context.ReadyTaggedThisFrame,
                    context.ChildLookup,
                    new UnitRenderBudgetVisualPlan.Request
                    {
                        Unit = unit,
                        MidRoot = midRoot,
                        LowRoot = lowRoot,
                        DetailedBand = context.DetailedUnits.Contains(unit),
                        MidBand = context.MidLodUnits.Contains(unit),
                        LowBand = context.LowLodUnits.Contains(unit),
                        HasMidLodInstance = hasMidLodInstance,
                        HasLowLodInstance = hasLowLodInstance,
                        HasAnyMeshLodPrefab = lodReferences.HasAnyMeshLodPrefab,
                        HasAnyMeshLodInstance = lodReferences.HasAnyMeshLodInstance,
                        WaitingForLow = waitingForLow,
                        IsCharacter = isCharacter,
                        IsAirUnit = isAirUnit,
                        IsEnemyUnit = isEnemyUnit,
                        IsSelectedUnit = isSelectedUnit,
                        IsMovingUnit = isMovingUnit,
                        CameraMotionActive = context.CameraMotionActive,
                        Visible = context.Distances[i].Visible,
                        DistanceSq = context.Distances[i].DistanceSq,
                        MidRootSafe = midRootSafe,
                        LowRootSafe = lowRootSafe,
                        HasSafeVisibleMid = hasSafeVisibleMid,
                        MidRootAnimatable = midRootAnimatable,
                        LowRootAnimatable = lowRootAnimatable,
                        AlwaysDetailedDistanceSq = context.AlwaysDetailedDistanceSq,
                        EnemyAlwaysDetailedDistanceSq = context.EnemyAlwaysDetailedDistanceSq,
                        EnemyLowLodDistanceSq = context.EnemyLowLodDistanceSq,
                        EnemyImpostorDistanceSq = context.EnemyImpostorDistanceSq,
                        VisibleCharacterLowDistanceSq = context.VisibleCharacterLowDistanceSq,
                        VisibleCharacterImpostorNearDistance = context.VisibleCharacterImpostorNearDistance,
                        VisibleCharacterImpostorFarDistance = context.VisibleCharacterImpostorFarDistance
                    },
                    context.CharacterPolicySystem,
                    context.ReadinessSystem,
                    context.AnimationReadinessSystem,
                    context.RenderableQuerySystem,
                    context.ReadinessLookups,
                    context.AnimationReadinessLookups,
                    context.RenderableQueryLookups);
                UnitRenderBudgetVisualPlan.Counters planCounters = visualPlan.Counters;
                result.VisibleCharacterSafeGate += planCounters.VisibleCharacterSafeGate;
                result.VisibleCharacterMidInstances += planCounters.VisibleCharacterMidInstances;
                result.VisibleCharacterSafeMidInstances += planCounters.VisibleCharacterSafeMidInstances;
                result.VisibleCharacterLowInstances += planCounters.VisibleCharacterLowInstances;
                result.VisibleCharacterSafeLowInstances += planCounters.VisibleCharacterSafeLowInstances;
                result.VisibleCharacterUsingSafeMid += planCounters.VisibleCharacterUsingSafeMid;
                result.VisibleCharacterUsingSafeLow += planCounters.VisibleCharacterUsingSafeLow;
                result.VisibleCharacterUsingFarImpostor += planCounters.VisibleCharacterUsingFarImpostor;
                result.VisibleCharacterForcedDetailByUnsafeMid += planCounters.VisibleCharacterForcedDetailByUnsafeMid;
                result.VisibleCharacterBudgetDetail += planCounters.VisibleCharacterBudgetDetail;
                result.VisibleCharacterSafetyDetail += planCounters.VisibleCharacterSafetyDetail;
                bool shouldShowDetail = visualPlan.ShouldShowDetail;
                bool shouldShowMid = visualPlan.ShouldShowMid;
                bool shouldShowLow = visualPlan.ShouldShowLow;
                bool shouldShowFar = visualPlan.ShouldShowFar;
                UnitRenderVisualKind desiredVisual = visualPlan.DesiredVisual;
                bool hadVisualState = context.VisualStateLookup.HasComponent(unit);
                UnitRenderVisualKind previousVisual = hadVisualState
                    ? (UnitRenderVisualKind)context.VisualStateLookup[unit].Current
                    : UnitRenderVisualKind.Unknown;
                bool forceImmediateMeshVisual =
                    previousVisual == UnitRenderVisualKind.Far &&
                    desiredVisual != UnitRenderVisualKind.Far;
                UnitRenderVisualKind activeVisual = context.VisualStateSystem.ResolveStableUnitRenderVisualState(
                    context.VisualStateLookup,
                    context.RenderStateEcb,
                    unit,
                    desiredVisual,
                    forceImmediateMeshVisual ||
                    (visualPlan.ForceImmediateDetailVisual && desiredVisual == UnitRenderVisualKind.Detail),
                    context.CurrentFrame,
                    ref result.VisualStateChanges,
                    ref result.VisualStatePending,
                    ref result.VisualTransitionsCommitted);
                shouldShowDetail = activeVisual == UnitRenderVisualKind.Detail;
                shouldShowMid = activeVisual == UnitRenderVisualKind.Mid;
                shouldShowLow = activeVisual == UnitRenderVisualKind.Low;
                shouldShowFar = activeVisual == UnitRenderVisualKind.Far;
                bool forceSelectedNonCharacterDetailRoots = isSelectedUnit && !isCharacter;
                bool applyVisualRoots =
                    forceSelectedNonCharacterDetailRoots ||
                    !hadVisualState ||
                    previousVisual != activeVisual ||
                    (activeVisual != desiredVisual && (desiredVisual == UnitRenderVisualKind.Mid || desiredVisual == UnitRenderVisualKind.Low));
                if (shouldShowFar)
                    result.FarCount++;

                context.ImpostorTagSystem.CollectUnitImpostorTagRequest(
                    unit,
                    shouldShowFar,
                    context.CulledUnitLookup,
                    context.UnitsToShowDetailed,
                    context.UnitsToShowFarImpostor,
                    ref result.Changed);

                Entity detailRoot = lodReferences.DetailRoot;
                if (applyVisualRoots)
                {
                    if (detailRoot != Entity.Null)
                    {
                        if (shouldShowDetail)
                            result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(context.RenderStateEcb, context.SafetyTaggedThisFrame, detailRoot, context.ChildLookup, context.RenderSafetyLookups);
                        context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(detailRoot, shouldShowDetail, context.ChildLookup, context.EntityStorageInfoLookup, context.DisabledLookup, context.DisableRenderingLookup, context.CulledTagLookup, context.EntitiesToShow, context.EntitiesToHide, ref result.Changed);
                    }
                    else
                    {
                        if (shouldShowDetail)
                            result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(context.RenderStateEcb, context.SafetyTaggedThisFrame, unit, context.ChildLookup, context.RenderSafetyLookups);
                        context.VisibilityChangeSystem.CollectRenderVisibilityChanges(unit, shouldShowDetail, context.ChildLookup, context.EntityStorageInfoLookup, context.DisabledLookup, context.DisableRenderingLookup, context.CulledTagLookup, context.EntitiesToShow, context.EntitiesToHide, ref result.Changed);
                    }

                    if (hasMidLodInstance)
                    {
                        if (shouldShowMid)
                            result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(context.RenderStateEcb, context.SafetyTaggedThisFrame, midRoot, context.ChildLookup, context.RenderSafetyLookups);
                        context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(midRoot, shouldShowMid, context.ChildLookup, context.EntityStorageInfoLookup, context.DisabledLookup, context.DisableRenderingLookup, context.CulledTagLookup, context.EntitiesToShow, context.EntitiesToHide, ref result.Changed);
                    }

                    if (hasLowLodInstance)
                    {
                        if (shouldShowLow)
                            result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(context.RenderStateEcb, context.SafetyTaggedThisFrame, lowRoot, context.ChildLookup, context.RenderSafetyLookups);
                        context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(lowRoot, shouldShowLow, context.ChildLookup, context.EntityStorageInfoLookup, context.DisabledLookup, context.DisableRenderingLookup, context.CulledTagLookup, context.EntitiesToShow, context.EntitiesToHide, ref result.Changed);
                    }
                }
                else if (isProtectedVisibleCharacter && shouldShowMid && hasMidLodInstance)
                {
                    result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(context.RenderStateEcb, context.SafetyTaggedThisFrame, midRoot, context.ChildLookup, context.RenderSafetyLookups);
                }

                if (shouldShowMid)
                    result.MidShown++;
                if (shouldShowLow)
                    result.LowShown++;
                if (isProtectedVisibleCharacter && context.Distances[i].DistanceSq <= context.AlwaysDetailedDistanceSq)
                {
                    if (shouldShowDetail)
                        result.VisibleNearDetail++;
                    else if (shouldShowMid)
                        result.VisibleNearMid++;
                }
            }

            return result;
        }

        private static void HideDeadUnitLiveVisualRoots(
            ref Context context,
            UnitRenderBudgetLodReferences.UnitReferences lodReferences,
            ref Result result)
        {
            Entity detailRoot = lodReferences.DetailRoot;
            if (detailRoot != Entity.Null)
                context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(
                    detailRoot,
                    visible: false,
                    context.ChildLookup,
                    context.EntityStorageInfoLookup,
                    context.DisabledLookup,
                    context.DisableRenderingLookup,
                    context.CulledTagLookup,
                    context.EntitiesToShow,
                    context.EntitiesToHide,
                    ref result.Changed);

            if (lodReferences.HasMidLodInstance)
                context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(
                    lodReferences.MidRoot,
                    visible: false,
                    context.ChildLookup,
                    context.EntityStorageInfoLookup,
                    context.DisabledLookup,
                    context.DisableRenderingLookup,
                    context.CulledTagLookup,
                    context.EntitiesToShow,
                    context.EntitiesToHide,
                    ref result.Changed);

            if (lodReferences.HasLowLodInstance)
                context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(
                    lodReferences.LowRoot,
                    visible: false,
                    context.ChildLookup,
                    context.EntityStorageInfoLookup,
                    context.DisabledLookup,
                    context.DisableRenderingLookup,
                    context.CulledTagLookup,
                    context.EntitiesToShow,
                    context.EntitiesToHide,
                    ref result.Changed);
        }
    }
}
