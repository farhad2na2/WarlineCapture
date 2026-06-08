using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnitDistance = UnitRenderBudgetDistanceSystem.UnitDistance;

public readonly struct UnitRenderBudgetDecisionSystem
{
    public struct Context
    {
        public EntityManager Em;
        public EntityCommandBuffer RenderStateEcb;
        public NativeHashSet<Entity> SafetyTaggedThisFrame;
        public NativeHashSet<Entity> ReadyTaggedThisFrame;
        public BufferLookup<Child> ChildLookup;
        public ComponentLookup<MaterialAnimationIndex> AnimationIndexLookup;
        public ComponentLookup<UnitMoveVisualComponent> MoveVisualLookup;
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
        public UnitRenderBudgetClassificationSystem ClassificationSystem;
        public UnitRenderBudgetCharacterPolicySystem CharacterPolicySystem;
        public UnitRenderBudgetLodReferenceSystem LodReferenceSystem;
        public UnitRenderBudgetAnimationReadinessSystem AnimationReadinessSystem;
        public UnitRenderBudgetRenderableQuerySystem RenderableQuerySystem;
        public UnitRenderBudgetVisualStateSystem VisualStateSystem;
        public UnitRenderBudgetReadinessSystem ReadinessSystem;
        public UnitRenderBudgetRenderSafetySystem RenderSafetySystem;
        public UnitRenderBudgetVisualPlanSystem VisualPlanSystem;
        public UnitRenderBudgetVisibilityChangeSystem VisibilityChangeSystem;
        public UnitRenderBudgetImpostorTagSystem ImpostorTagSystem;
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
        EntityManager em = context.Em;

        for (int i = 0; i < context.Distances.Length; i++)
        {
            Entity unit = context.Distances[i].Unit;
            bool isCharacter = context.ClassificationSystem.IsCharacterUnit(em, unit);
            byte factionId = em.GetComponentData<Faction>(unit).Id;
            bool isEnemyUnit = FactionIdentitySystem.IsHostileToPlayer(factionId);
            bool isSelectedUnit = em.HasComponent<SelectedUnitTag>(unit);
            bool isMovingUnit =
                context.MoveVisualLookup.HasComponent(unit) &&
                context.MoveVisualLookup[unit].IsMoving != 0;
            UnitRenderBudgetLodReferenceSystem.UnitReferences lodReferences = context.LodReferenceSystem.ResolveUnitReferences(em, unit);
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
                context.RenderableQuerySystem.IsSafeVisibleCharacterLod(em, midRoot);
            bool lowRootSafe =
                isCharacter &&
                hasLowLodInstance &&
                context.RenderableQuerySystem.IsSafeVisibleCharacterLod(em, lowRoot);
            bool hasSafeVisibleMid =
                midRootSafe &&
                (isProtectedVisibleCharacter || context.RenderableQuerySystem.HasRenderableRecursive(em, midRoot, context.ChildLookup));
            bool hasSafeVisibleLow =
                lowRootSafe &&
                (isProtectedVisibleCharacter || context.RenderableQuerySystem.HasRenderableRecursive(em, lowRoot, context.ChildLookup));
            bool midRootAnimatable = hasMidLodInstance && context.AnimationReadinessSystem.HasAnimationIndexRecursive(midRoot, context.AnimationIndexLookup, context.ChildLookup);
            bool lowRootAnimatable = hasLowLodInstance && context.AnimationReadinessSystem.HasAnimationIndexRecursive(lowRoot, context.AnimationIndexLookup, context.ChildLookup);
            UnitRenderBudgetVisualPlanSystem.Result visualPlan = context.VisualPlanSystem.CreateDesiredVisualPlan(
                em,
                context.RenderStateEcb,
                context.ReadyTaggedThisFrame,
                context.ChildLookup,
                new UnitRenderBudgetVisualPlanSystem.Request
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
                context.RenderableQuerySystem);
            UnitRenderBudgetVisualPlanSystem.Counters planCounters = visualPlan.Counters;
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
            bool hadVisualState = em.HasComponent<UnitRenderVisualComponent>(unit);
            UnitRenderVisualKind previousVisual = hadVisualState
                ? (UnitRenderVisualKind)em.GetComponentData<UnitRenderVisualComponent>(unit).Current
                : UnitRenderVisualKind.Unknown;
            UnitRenderVisualKind activeVisual = context.VisualStateSystem.ResolveStableUnitRenderVisualState(
                em,
                context.RenderStateEcb,
                unit,
                desiredVisual,
                visualPlan.ForceImmediateDetailVisual && desiredVisual == UnitRenderVisualKind.Detail,
                ref result.VisualStateChanges,
                ref result.VisualStatePending,
                ref result.VisualTransitionsCommitted);
            shouldShowDetail = activeVisual == UnitRenderVisualKind.Detail;
            shouldShowMid = activeVisual == UnitRenderVisualKind.Mid;
            shouldShowLow = activeVisual == UnitRenderVisualKind.Low;
            shouldShowFar = activeVisual == UnitRenderVisualKind.Far;
            bool applyVisualRoots =
                !hadVisualState ||
                previousVisual != activeVisual ||
                (activeVisual != desiredVisual && (desiredVisual == UnitRenderVisualKind.Mid || desiredVisual == UnitRenderVisualKind.Low));
            if (shouldShowFar)
                result.FarCount++;

            context.ImpostorTagSystem.CollectUnitImpostorTagRequest(
                em,
                unit,
                shouldShowFar,
                context.UnitsToShowDetailed,
                context.UnitsToShowFarImpostor,
                ref result.Changed);

            Entity detailRoot = lodReferences.DetailRoot;
            if (applyVisualRoots)
            {
                if (detailRoot != Entity.Null)
                {
                    if (shouldShowDetail)
                        result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(em, context.RenderStateEcb, context.SafetyTaggedThisFrame, detailRoot, context.ChildLookup, context.LodReferenceSystem);
                    context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(em, detailRoot, shouldShowDetail, context.ChildLookup, context.EntitiesToShow, context.EntitiesToHide, ref result.Changed);
                }
                else
                {
                    if (shouldShowDetail)
                        result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(em, context.RenderStateEcb, context.SafetyTaggedThisFrame, unit, context.ChildLookup, context.LodReferenceSystem);
                    context.VisibilityChangeSystem.CollectRenderVisibilityChanges(em, unit, shouldShowDetail, context.ChildLookup, context.EntitiesToShow, context.EntitiesToHide, ref result.Changed);
                }

                if (hasMidLodInstance)
                {
                    if (shouldShowMid)
                        result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(em, context.RenderStateEcb, context.SafetyTaggedThisFrame, midRoot, context.ChildLookup, context.LodReferenceSystem);
                    context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(em, midRoot, shouldShowMid, context.ChildLookup, context.EntitiesToShow, context.EntitiesToHide, ref result.Changed);
                }

                if (hasLowLodInstance)
                {
                    if (shouldShowLow)
                        result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(em, context.RenderStateEcb, context.SafetyTaggedThisFrame, lowRoot, context.ChildLookup, context.LodReferenceSystem);
                    context.VisibilityChangeSystem.CollectRenderVisibilityChangesRecursive(em, lowRoot, shouldShowLow, context.ChildLookup, context.EntitiesToShow, context.EntitiesToHide, ref result.Changed);
                }
            }
            else if (isProtectedVisibleCharacter && shouldShowMid && hasMidLodInstance)
            {
                result.VisibleMidSafetyPatched += context.RenderSafetySystem.EnsureRenderSafetyRecursiveOnce(em, context.RenderStateEcb, context.SafetyTaggedThisFrame, midRoot, context.ChildLookup, context.LodReferenceSystem);
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
}
