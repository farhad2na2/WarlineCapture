using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Rendering
{
    public readonly struct UnitRenderBudgetVisualPlan
    {
        private const bool EnableFarImpostorVisuals = true;

        public struct Request
        {
            public Entity Unit;
            public Entity MidRoot;
            public Entity LowRoot;
            public bool DetailedBand;
            public bool MidBand;
            public bool LowBand;
            public bool HasMidLodInstance;
            public bool HasLowLodInstance;
            public bool HasAnyMeshLodPrefab;
            public bool HasAnyMeshLodInstance;
            public bool WaitingForLow;
            public bool IsCharacter;
            public bool IsAirUnit;
            public bool IsEnemyUnit;
            public bool IsSelectedUnit;
            public bool IsMovingUnit;
            public bool CameraMotionActive;
            public byte Visible;
            public float DistanceSq;
            public bool MidRootSafe;
            public bool LowRootSafe;
            public bool HasSafeVisibleMid;
            public bool MidRootAnimatable;
            public bool LowRootAnimatable;
            public float AlwaysDetailedDistanceSq;
            public float EnemyAlwaysDetailedDistanceSq;
            public float EnemyLowLodDistanceSq;
            public float EnemyImpostorDistanceSq;
            public float VisibleCharacterLowDistanceSq;
            public float VisibleCharacterImpostorNearDistance;
            public float VisibleCharacterImpostorFarDistance;
        }

        public struct Counters
        {
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
        }

        public readonly struct Result
        {
            public readonly bool ShouldShowDetail;
            public readonly bool ShouldShowMid;
            public readonly bool ShouldShowLow;
            public readonly bool ShouldShowFar;
            public readonly bool ForceImmediateDetailVisual;
            public readonly UnitRenderVisualKind DesiredVisual;
            public readonly Counters Counters;

            public Result(
                bool shouldShowDetail,
                bool shouldShowMid,
                bool shouldShowLow,
                bool shouldShowFar,
                bool forceImmediateDetailVisual,
                UnitRenderVisualKind desiredVisual,
                Counters counters)
            {
                ShouldShowDetail = shouldShowDetail;
                ShouldShowMid = shouldShowMid;
                ShouldShowLow = shouldShowLow;
                ShouldShowFar = shouldShowFar;
                ForceImmediateDetailVisual = forceImmediateDetailVisual;
                DesiredVisual = desiredVisual;
                Counters = counters;
            }
        }

        public Result CreateDesiredVisualPlan(
            EntityCommandBuffer ecb,
            NativeHashSet<Entity> readyTaggedThisFrame,
            BufferLookup<Child> childLookup,
            Request request,
            UnitRenderBudgetCharacterPolicy characterPolicySystem,
            UnitRenderBudgetReadiness readinessSystem,
            UnitRenderBudgetAnimationReadiness animationReadinessSystem,
            UnitRenderBudgetRenderableState renderableQuerySystem,
            UnitRenderBudgetReadiness.Lookups readinessLookups,
            UnitRenderBudgetAnimationReadiness.Lookups animationReadinessLookups,
            UnitRenderBudgetRenderableState.Lookups renderableQueryLookups)
        {
            return CreateDesiredVisualPlan(
                default,
                ecb,
                readyTaggedThisFrame,
                childLookup,
                request,
                characterPolicySystem,
                readinessSystem,
                animationReadinessSystem,
                renderableQuerySystem,
                readinessLookups,
                animationReadinessLookups,
                renderableQueryLookups,
                useLookupReadiness: true);
        }

        public Result CreateDesiredVisualPlan(
            EntityManager em,
            EntityCommandBuffer ecb,
            NativeHashSet<Entity> readyTaggedThisFrame,
            BufferLookup<Child> childLookup,
            Request request,
            UnitRenderBudgetCharacterPolicy characterPolicySystem,
            UnitRenderBudgetReadiness readinessSystem,
            UnitRenderBudgetAnimationReadiness animationReadinessSystem,
            UnitRenderBudgetRenderableState renderableQuerySystem,
            UnitRenderBudgetReadiness.Lookups readinessLookups = default,
            UnitRenderBudgetAnimationReadiness.Lookups animationReadinessLookups = default,
            UnitRenderBudgetRenderableState.Lookups renderableQueryLookups = default,
            bool useLookupReadiness = false)
        {
            bool shouldShowDetail = request.DetailedBand;
            bool shouldShowMid =
                !shouldShowDetail &&
                request.HasMidLodInstance &&
                (request.MidBand || request.WaitingForLow || (request.Visible != 0 && request.HasSafeVisibleMid));
            bool shouldShowLow =
                !shouldShowDetail &&
                !shouldShowMid &&
                request.HasLowLodInstance &&
                request.LowBand;
            bool shouldShowFar = !shouldShowDetail && !shouldShowMid && !shouldShowLow;
            if (request.IsAirUnit)
            {
                return new Result(
                    shouldShowDetail: true,
                    shouldShowMid: false,
                    shouldShowLow: false,
                    shouldShowFar: false,
                    forceImmediateDetailVisual: true,
                    desiredVisual: UnitRenderVisualKind.Detail,
                    counters: default);
            }

            if (!EnableFarImpostorVisuals && shouldShowFar)
                UseClosestMeshVisual(request, out shouldShowDetail, out shouldShowMid, out shouldShowLow, out shouldShowFar);

            bool forceImmediateDetailVisual = false;
            Counters counters = default;
            bool isProtectedVisibleCharacter = request.IsCharacter && request.Visible != 0;
            bool enemyShouldUseImpostor =
                EnableFarImpostorVisuals &&
                request.IsEnemyUnit &&
                !request.IsSelectedUnit &&
                request.DistanceSq >= request.EnemyImpostorDistanceSq;
            bool enemyLowEnoughForSafeLow =
                request.IsEnemyUnit &&
                !request.IsSelectedUnit &&
                request.DistanceSq >= request.EnemyLowLodDistanceSq;

            if (isProtectedVisibleCharacter)
            {
                float alwaysDetailedDistanceSq = request.IsEnemyUnit && !request.IsSelectedUnit
                    ? request.EnemyAlwaysDetailedDistanceSq
                    : request.AlwaysDetailedDistanceSq;
                bool forceDetailNearVisible = request.DistanceSq <= alwaysDetailedDistanceSq;
                if (request.HasMidLodInstance)
                    counters.VisibleCharacterMidInstances++;
                if (request.HasLowLodInstance)
                    counters.VisibleCharacterLowInstances++;
                bool hasSafeMid = request.MidRootSafe;
                bool hasSafeLow = request.LowRootSafe;
                if (hasSafeMid)
                    counters.VisibleCharacterSafeMidInstances++;
                if (hasSafeLow)
                    counters.VisibleCharacterSafeLowInstances++;

                bool farEnoughForImpostor =
                    !request.IsSelectedUnit &&
                    (enemyShouldUseImpostor ||
                     request.DistanceSq >= request.VisibleCharacterImpostorFarDistance * request.VisibleCharacterImpostorFarDistance);
                bool lowEnoughForSafeLow =
                    enemyLowEnoughForSafeLow ||
                    request.DistanceSq >= request.VisibleCharacterLowDistanceSq;
                bool forceDetailByBudget = shouldShowDetail && !request.CameraMotionActive && !farEnoughForImpostor && !lowEnoughForSafeLow;
                UnitRenderVisualKind visibleCharacterVisual = characterPolicySystem.ResolveVisibleCharacterVisualKind(
                    request.IsMovingUnit,
                    forceDetailNearVisible,
                    forceDetailByBudget,
                    farEnoughForImpostor,
                    lowEnoughForSafeLow,
                    hasSafeMid,
                    request.MidRootAnimatable,
                    hasSafeLow,
                    request.LowRootAnimatable);
                bool canUseFarImpostor = visibleCharacterVisual == UnitRenderVisualKind.Far;
                bool canUseSafeLow = visibleCharacterVisual == UnitRenderVisualKind.Low;
                bool canUseSafeMid = visibleCharacterVisual == UnitRenderVisualKind.Mid;
                if (!EnableFarImpostorVisuals && canUseFarImpostor)
                {
                    UseClosestMeshVisual(request, out shouldShowDetail, out shouldShowMid, out shouldShowLow, out shouldShowFar);
                    canUseFarImpostor = false;
                    canUseSafeLow = shouldShowLow;
                    canUseSafeMid = shouldShowMid;
                }
                else
                {
                    shouldShowDetail = visibleCharacterVisual == UnitRenderVisualKind.Detail;
                    shouldShowMid = canUseSafeMid;
                    shouldShowLow = canUseSafeLow;
                    shouldShowFar = canUseFarImpostor;
                }

                bool mustShowDetailForSafety =
                    visibleCharacterVisual == UnitRenderVisualKind.Detail &&
                    !forceDetailNearVisible &&
                    !forceDetailByBudget;
                forceImmediateDetailVisual = shouldShowDetail && (forceDetailNearVisible || mustShowDetailForSafety);

                if (canUseFarImpostor)
                    counters.VisibleCharacterUsingFarImpostor++;
                else if (canUseSafeMid)
                    counters.VisibleCharacterUsingSafeMid++;
                else if (canUseSafeLow)
                    counters.VisibleCharacterUsingSafeLow++;
                else if (forceDetailByBudget)
                    counters.VisibleCharacterBudgetDetail++;
                else if (mustShowDetailForSafety)
                    counters.VisibleCharacterSafetyDetail++;
                else if (request.HasMidLodInstance && !hasSafeMid)
                    counters.VisibleCharacterForcedDetailByUnsafeMid++;
                counters.VisibleCharacterSafeGate++;
            }
            else if (request.IsCharacter &&
                     shouldShowFar &&
                     request.Visible != 0 &&
                     request.DistanceSq <= request.VisibleCharacterImpostorNearDistance * request.VisibleCharacterImpostorNearDistance)
            {
                shouldShowFar = false;
                if (request.HasLowLodInstance)
                    shouldShowLow = true;
                else if (request.HasMidLodInstance)
                    shouldShowMid = true;
                else
                    shouldShowDetail = true;
            }

            if (enemyShouldUseImpostor && !(isProtectedVisibleCharacter && request.IsMovingUnit))
            {
                shouldShowDetail = false;
                shouldShowMid = false;
                shouldShowLow = false;
                shouldShowFar = true;
                forceImmediateDetailVisual = false;
            }

            if (request.IsSelectedUnit && !request.IsCharacter)
            {
                shouldShowDetail = true;
                shouldShowMid = false;
                shouldShowLow = false;
                shouldShowFar = false;
                forceImmediateDetailVisual = true;
            }

            bool keepDetailVisibleUntilReady =
                !shouldShowFar &&
                !shouldShowDetail &&
                request.HasAnyMeshLodPrefab &&
                !request.HasAnyMeshLodInstance;
            if (keepDetailVisibleUntilReady)
            {
                shouldShowDetail = true;
                shouldShowMid = false;
                shouldShowLow = false;
                shouldShowFar = false;
                forceImmediateDetailVisual = true;
            }

            bool keepDetailVisibleDuringHandoff =
                (!shouldShowDetail &&
                 shouldShowMid &&
                 !(useLookupReadiness
                     ? readinessSystem.IsVisualReadyForExclusiveDisplay(ecb, readyTaggedThisFrame, request.MidRoot, childLookup, animationReadinessSystem, renderableQuerySystem, readinessLookups, animationReadinessLookups, renderableQueryLookups)
                     : readinessSystem.IsVisualReadyForExclusiveDisplay(em, ecb, readyTaggedThisFrame, request.MidRoot, childLookup, animationReadinessSystem, renderableQuerySystem))) ||
                (!shouldShowDetail &&
                 shouldShowLow &&
                 !(useLookupReadiness
                     ? readinessSystem.IsVisualReadyForExclusiveDisplay(ecb, readyTaggedThisFrame, request.LowRoot, childLookup, animationReadinessSystem, renderableQuerySystem, readinessLookups, animationReadinessLookups, renderableQueryLookups)
                     : readinessSystem.IsVisualReadyForExclusiveDisplay(em, ecb, readyTaggedThisFrame, request.LowRoot, childLookup, animationReadinessSystem, renderableQuerySystem)));
            if (keepDetailVisibleDuringHandoff)
            {
                shouldShowDetail = true;
                shouldShowFar = false;
                forceImmediateDetailVisual = true;
            }

            if (characterPolicySystem.ShouldForceCharacterDetailVisual(request.IsCharacter))
            {
                shouldShowDetail = true;
                shouldShowMid = false;
                shouldShowLow = false;
                shouldShowFar = false;
                forceImmediateDetailVisual = true;
            }

            if (!EnableFarImpostorVisuals && shouldShowFar)
                UseClosestMeshVisual(request, out shouldShowDetail, out shouldShowMid, out shouldShowLow, out shouldShowFar);

            UnitRenderVisualKind desiredVisual = ResolveDesiredVisual(shouldShowDetail, shouldShowMid, shouldShowLow, shouldShowFar);
            return new Result(
                shouldShowDetail,
                shouldShowMid,
                shouldShowLow,
                shouldShowFar,
                forceImmediateDetailVisual,
                desiredVisual,
                counters);
        }

        public UnitRenderVisualKind ResolveDesiredVisualForDiagnostics(bool isCharacter, bool detail, bool mid, bool low)
        {
            if (detail)
                return UnitRenderVisualKind.Detail;
            if (mid)
                return UnitRenderVisualKind.Mid;
            if (low)
                return UnitRenderVisualKind.Low;

            return UnitRenderVisualKind.Detail;
        }

        private static void UseClosestMeshVisual(
            Request request,
            out bool shouldShowDetail,
            out bool shouldShowMid,
            out bool shouldShowLow,
            out bool shouldShowFar)
        {
            shouldShowFar = false;
            shouldShowLow = request.HasLowLodInstance;
            shouldShowMid = !shouldShowLow && request.HasMidLodInstance;
            shouldShowDetail = !shouldShowLow && !shouldShowMid;
        }

        private static UnitRenderVisualKind ResolveDesiredVisual(bool detail, bool mid, bool low, bool far)
        {
            if (detail)
                return UnitRenderVisualKind.Detail;
            if (mid)
                return UnitRenderVisualKind.Mid;
            if (low)
                return UnitRenderVisualKind.Low;
            if (far)
                return UnitRenderVisualKind.Far;

            return UnitRenderVisualKind.Detail;
        }
    }
}
