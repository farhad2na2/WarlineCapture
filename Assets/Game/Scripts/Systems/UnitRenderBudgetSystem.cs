using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using SnivelerCode.GpuAnimation.Scripts.Components;

[UpdateAfter(typeof(UnitMassRenderSettingsSystem))]
public partial struct UnitRenderBudgetSystem : ISystem
{
    private static readonly bool EnableRenderBudgetDiagnostics = false;
    private static readonly bool EnableRenderBudgetFreezeLogs = false;
    private const double FreezeLogThresholdSeconds = 0.05d;
    private const int MaxDetailedUnits = 12;
    private const int MaxMidLodUnits = 36;
    private const int MaxLowLodUnits = 48;
    private const int MaxUpdatesPerFrame = 4096;
    private const int UpdateIntervalFrames = 10;
    private const int DiagnosticIntervalFrames = 120;
    private const float AlwaysDetailedDistanceSq = 18f * 18f;
    private const float VisibleCharacterLowDistanceSq = 32f * 32f;
    private const float VisibleCharacterImpostorNearDistance = 48f;
    private const float VisibleCharacterImpostorFarDistance = 48f;
    private const float EnemyAlwaysDetailedDistanceSq = 14f * 14f;
    private const float EnemyLowLodDistanceSq = 20f * 20f;
    private const float EnemyImpostorDistanceSq = 28f * 28f;
    private const int CameraSettleFrames = 8;
    private const float CameraMoveThresholdSq = 0.0004f;
    private const float CameraRotateThresholdDegrees = 0.03f;
    private const int VisualTransitionStableFrames = 2;
    private const int MaxVisualStateTransitionsPerUpdate = 32;
    private const float VisibleCharacterViewportPadding = 0.35f;
    private const float VisibleCharacterEdgeSafetyMargin = 0.18f;
    private const int AlwaysVisibleLodMask = 0xFF;
    private const float AlwaysVisibleLodDistance = 1048576f;
    private static readonly float3 RenderBoundsMinExtents = new float3(64f, 64f, 64f);

    private EntityQuery _unitQuery;
    private EntityQuery _allUnitGridQuery;
    private EntityQuery _spawnConfigQuery;
    private EntityQuery _spawnProgressQuery;
    private EntityQuery _spawnInitializedQuery;
    private EntityQuery _cameraReferenceQuery;
    private int _nextUpdateFrame;
    private int _nextDiagnosticFrame;
    private int _lodResumeFrame;
    private bool _budgetStable;
    private int _stableUnitCount;
    private bool _hasCameraSnapshot;
    private Vector3 _lastCameraPosition;
    private Quaternion _lastCameraRotation;

    private struct UnitDistance
    {
        public Entity Unit;
        public float DistanceSq;
        public byte Priority;
        public byte Visible;
        public byte ScreenEdge;
    }

    private struct UnitDistanceComparer : System.Collections.Generic.IComparer<UnitDistance>
    {
        public int Compare(UnitDistance x, UnitDistance y)
        {
            int priorityCompare = x.Priority.CompareTo(y.Priority);
            if (priorityCompare != 0)
                return priorityCompare;

            return x.DistanceSq.CompareTo(y.DistanceSq);
        }
    }

    public void OnCreate(ref SystemState state)
    {
        _unitQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<Disabled>(),
            }
        });
        _allUnitGridQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitGrid>());
        _spawnConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        _spawnProgressQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnProgress>());
        _spawnInitializedQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
        _cameraReferenceQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeCameraReferenceComponent>());
        state.RequireForUpdate<RuntimeGameplayStateComponent>();

    }

    public void OnUpdate(ref SystemState state)
    {
        RuntimeGameplayStateComponent runtimeGameplayState = SystemAPI.GetSingleton<RuntimeGameplayStateComponent>();
        if (runtimeGameplayState.PlayRequested == 0)
            return;

        if (!RuntimeCameraReferenceSystem.TryGetWorldCamera(state.EntityManager, _cameraReferenceQuery, out Camera camera))
            return;

        bool cameraMotionActive = IsCameraMotionActive(camera);
        int currentUnitCount = _unitQuery.CalculateEntityCount();
        if (!cameraMotionActive && _budgetStable && currentUnitCount == _stableUnitCount)
            return;

        if (!cameraMotionActive && Time.frameCount < _nextUpdateFrame)
            return;

        _nextUpdateFrame = Time.frameCount + (cameraMotionActive ? 1 : UpdateIntervalFrames);
        double startTime = Time.realtimeSinceStartupAsDouble;
        EntityManager em = state.EntityManager;
        var renderStateEcb = new EntityCommandBuffer(Allocator.Temp);
        var childLookup = SystemAPI.GetBufferLookup<Child>(true);
        var animationIndexLookup = SystemAPI.GetComponentLookup<MaterialAnimationIndex>(true);
        var moveVisualLookup = SystemAPI.GetComponentLookup<UnitMoveVisualState>(true);
        float3 cameraPosition = camera.transform.position;

        using NativeArray<Entity> units = _unitQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<LocalTransform> transforms = _unitQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using NativeHashSet<Entity> safetyTaggedThisFrame = new(math.max(1, units.Length * 3), Allocator.Temp);
        using NativeHashSet<Entity> readyTaggedThisFrame = new(math.max(1, units.Length * 3), Allocator.Temp);
        using NativeList<UnitDistance> distances = new(units.Length, Allocator.Temp);
        using NativeList<Entity> entitiesToShow = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> entitiesToHide = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> unitsToShowDetailed = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> unitsToShowFarImpostor = new(MaxUpdatesPerFrame, Allocator.Temp);
        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (!em.Exists(unit) || em.HasComponent<UnitTransportPassenger>(unit))
                continue;

            float3 unitPosition = transforms[i].Position;
            float distanceSq = math.distancesq(unitPosition, cameraPosition);
            Vector3 worldPosition = new(unitPosition.x, unitPosition.y, unitPosition.z);
            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPosition);
            bool visible =
                viewportPosition.z > 0f &&
                viewportPosition.x >= -VisibleCharacterViewportPadding && viewportPosition.x <= 1f + VisibleCharacterViewportPadding &&
                viewportPosition.y >= -VisibleCharacterViewportPadding && viewportPosition.y <= 1f + VisibleCharacterViewportPadding;
            bool screenEdge =
                visible &&
                (viewportPosition.x <= VisibleCharacterEdgeSafetyMargin ||
                 viewportPosition.x >= 1f - VisibleCharacterEdgeSafetyMargin ||
                 viewportPosition.y <= VisibleCharacterEdgeSafetyMargin ||
                 viewportPosition.y >= 1f - VisibleCharacterEdgeSafetyMargin ||
                 viewportPosition.x < 0f ||
                 viewportPosition.x > 1f ||
                 viewportPosition.y < 0f ||
                 viewportPosition.y > 1f);
            bool near = distanceSq <= AlwaysDetailedDistanceSq;
            byte priority = near
                ? (byte)(visible ? 0 : 1)
                : (byte)(visible ? 2 : 3);
            distances.Add(new UnitDistance
            {
                Unit = unit,
                DistanceSq = distanceSq,
                Priority = priority,
                Visible = visible ? (byte)1 : (byte)0,
                ScreenEdge = screenEdge ? (byte)1 : (byte)0
            });
        }

        if (distances.Length == 0)
        {
            renderStateEcb.Dispose();
            if (EnableRenderBudgetDiagnostics && Time.frameCount >= _nextDiagnosticFrame)
            {
                _nextDiagnosticFrame = Time.frameCount + DiagnosticIntervalFrames;
                Debug.Log($"[UnitRenderBudgetEmptyDiag] frame={Time.frameCount} queryUnits={units.Length} allUnitGrid={_allUnitGridQuery.CalculateEntityCount()} spawnConfigs={_spawnConfigQuery.CalculateEntityCount()} spawnProgress={_spawnProgressQuery.CalculateEntityCount()} spawnInitialized={_spawnInitializedQuery.CalculateEntityCount()} playRequested={(runtimeGameplayState.PlayRequested != 0 ? 1 : 0)}");
            }
            return;
        }

        distances.AsArray().Sort(new UnitDistanceComparer());
        using NativeHashSet<Entity> detailedUnits = new(math.max(1, distances.Length), Allocator.Temp);
        int detailedCount = 0;
        for (int i = 0; i < distances.Length && detailedCount < MaxDetailedUnits; i++)
        {
            if (distances[i].DistanceSq > AlwaysDetailedDistanceSq)
                continue;

            if (detailedUnits.Add(distances[i].Unit))
                detailedCount++;
        }

        for (int i = 0; i < distances.Length && detailedCount < MaxDetailedUnits; i++)
        {
            if (detailedUnits.Add(distances[i].Unit))
                detailedCount++;
        }

        using NativeHashSet<Entity> midLodUnits = new(math.max(1, distances.Length), Allocator.Temp);
        int midCount = 0;
        for (int i = 0; i < distances.Length && midCount < MaxMidLodUnits; i++)
        {
            Entity unit = distances[i].Unit;
            if (detailedUnits.Contains(unit))
                continue;

            if (midLodUnits.Add(unit))
                midCount++;
        }

        using NativeHashSet<Entity> lowLodUnits = new(math.max(1, distances.Length), Allocator.Temp);
        int lowCount = 0;
        for (int i = 0; i < distances.Length && lowCount < MaxLowLodUnits; i++)
        {
            Entity unit = distances[i].Unit;
            if (detailedUnits.Contains(unit) || midLodUnits.Contains(unit))
                continue;

            if (lowLodUnits.Add(unit))
                lowCount++;
        }

        int changed = 0;
        int hidden = 0;
        int shown = 0;
        int midShown = 0;
        int lowShown = 0;
        int farCount = 0;
        int missingMidInstance = 0;
        int missingLowInstance = 0;
        int visualStateChanges = 0;
        int visualStatePending = 0;
        int visualTransitionsCommitted = 0;
        int visibleCharacterSafeGate = 0;
        int visibleCharacterMidInstances = 0;
        int visibleCharacterSafeMidInstances = 0;
        int visibleCharacterLowInstances = 0;
        int visibleCharacterSafeLowInstances = 0;
        int visibleCharacterUsingSafeMid = 0;
        int visibleCharacterUsingSafeLow = 0;
        int visibleCharacterUsingFarImpostor = 0;
        int visibleCharacterMidSuppressed = 0;
        int visibleCharacterForcedDetailByUnsafeMid = 0;
        int visibleCharacterBudgetDetail = 0;
        int visibleCharacterSafetyDetail = 0;
        int visibleMidSafetyPatched = 0;
        int visibleNearDetail = 0;
        int visibleNearMid = 0;
        for (int i = 0; i < distances.Length; i++)
        {
            Entity unit = distances[i].Unit;
            bool shouldShowDetail = detailedUnits.Contains(unit);
            bool isCharacter = IsCharacterUnit(em, unit);
            byte factionId = em.GetComponentData<Faction>(unit).Id;
            bool isEnemyUnit = factionId != 0;
            bool isSelectedUnit = em.HasComponent<SelectedUnitTag>(unit);
            bool isMovingUnit =
                moveVisualLookup.HasComponent(unit) &&
                moveVisualLookup[unit].IsMoving != 0;
            bool enemyShouldUseImpostor =
                isEnemyUnit &&
                !isSelectedUnit &&
                distances[i].DistanceSq >= EnemyImpostorDistanceSq;
            bool enemyLowEnoughForSafeLow =
                isEnemyUnit &&
                !isSelectedUnit &&
                distances[i].DistanceSq >= EnemyLowLodDistanceSq;
            bool hasMidLodPrefab = em.HasComponent<UnitMidLodPrefabReference>(unit);
            bool hasMidLodInstance = em.HasComponent<UnitMidLodInstanceReference>(unit);
            Entity midRoot = hasMidLodInstance
                ? em.GetComponentData<UnitMidLodInstanceReference>(unit).Instance
                : Entity.Null;
            if (hasMidLodPrefab && !hasMidLodInstance)
                missingMidInstance++;
            bool hasLowLodPrefab = em.HasComponent<UnitLowLodPrefabReference>(unit);
            bool hasLowLodInstance = em.HasComponent<UnitLowLodInstanceReference>(unit);
            Entity lowRoot = hasLowLodInstance
                ? em.GetComponentData<UnitLowLodInstanceReference>(unit).Instance
                : Entity.Null;
            if (hasLowLodPrefab && !hasLowLodInstance)
                missingLowInstance++;
            bool waitingForLow = hasLowLodPrefab && !hasLowLodInstance && lowLodUnits.Contains(unit);
            bool isProtectedVisibleCharacter = isCharacter && distances[i].Visible != 0;
            bool midRootSafe =
                isCharacter &&
                hasMidLodInstance &&
                IsSafeVisibleCharacterLod(em, midRoot);
            bool lowRootSafe =
                isCharacter &&
                hasLowLodInstance &&
                IsSafeVisibleCharacterLod(em, lowRoot);
            bool hasSafeVisibleMid =
                midRootSafe &&
                (isProtectedVisibleCharacter || HasRenderableRecursive(em, midRoot, childLookup));
            bool hasSafeVisibleLow =
                lowRootSafe &&
                (isProtectedVisibleCharacter || HasRenderableRecursive(em, lowRoot, childLookup));
            bool midRootAnimatable = hasMidLodInstance && HasAnimationIndexRecursive(midRoot, animationIndexLookup, childLookup);
            bool lowRootAnimatable = hasLowLodInstance && HasAnimationIndexRecursive(lowRoot, animationIndexLookup, childLookup);
            bool shouldShowMid = !shouldShowDetail && hasMidLodInstance && (midLodUnits.Contains(unit) || waitingForLow || (distances[i].Visible != 0 && hasSafeVisibleMid));
            bool shouldShowLow = !shouldShowDetail && !shouldShowMid && hasLowLodInstance && lowLodUnits.Contains(unit);
            bool shouldShowFar = !shouldShowDetail && !shouldShowMid && !shouldShowLow;
            bool forceImmediateDetailVisual = false;
            if (isProtectedVisibleCharacter)
            {
                float alwaysDetailedDistanceSq = isEnemyUnit && !isSelectedUnit
                    ? EnemyAlwaysDetailedDistanceSq
                    : AlwaysDetailedDistanceSq;
                bool forceDetailNearVisible = distances[i].DistanceSq <= alwaysDetailedDistanceSq;
                if (hasMidLodInstance)
                    visibleCharacterMidInstances++;
                if (hasLowLodInstance)
                    visibleCharacterLowInstances++;
                bool hasSafeMid = midRootSafe;
                bool hasSafeLow = lowRootSafe;
                if (hasSafeMid)
                    visibleCharacterSafeMidInstances++;
                if (hasSafeLow)
                    visibleCharacterSafeLowInstances++;

                // Visible soldiers must never disappear during camera motion or LOD settling.
                // Use detail only near the camera, safe mesh LODs in the mid range, and billboard
                // impostors for distant visible characters so large RTS armies stay renderable.
                bool movingVisibleCharacter = isMovingUnit;
                bool farEnoughForImpostor =
                    enemyShouldUseImpostor ||
                    cameraPosition.y >= 80f ||
                    distances[i].DistanceSq >= VisibleCharacterImpostorFarDistance * VisibleCharacterImpostorFarDistance;
                bool forceTacticalImpostor = cameraPosition.y >= 80f && farEnoughForImpostor;
                bool lowEnoughForSafeLow =
                    enemyLowEnoughForSafeLow ||
                    distances[i].DistanceSq >= VisibleCharacterLowDistanceSq;
                bool forceDetailByBudget = shouldShowDetail && !cameraMotionActive && !farEnoughForImpostor && !lowEnoughForSafeLow;
                UnitRenderVisualKind visibleCharacterVisual = ResolveVisibleCharacterVisualKind(
                    movingVisibleCharacter,
                    forceDetailNearVisible,
                    forceDetailByBudget,
                    farEnoughForImpostor,
                    forceTacticalImpostor,
                    lowEnoughForSafeLow,
                    hasSafeMid,
                    midRootAnimatable,
                    hasSafeLow,
                    lowRootAnimatable);
                bool canUseFarImpostor = visibleCharacterVisual == UnitRenderVisualKind.Far;
                bool canUseSafeLow = visibleCharacterVisual == UnitRenderVisualKind.Low;
                bool canUseSafeMid = visibleCharacterVisual == UnitRenderVisualKind.Mid;
                bool mustShowDetailForSafety =
                    visibleCharacterVisual == UnitRenderVisualKind.Detail &&
                    !forceDetailNearVisible &&
                    !forceDetailByBudget;
                shouldShowDetail = visibleCharacterVisual == UnitRenderVisualKind.Detail;
                shouldShowMid = canUseSafeMid;
                shouldShowLow = canUseSafeLow;
                shouldShowFar = canUseFarImpostor;
                forceImmediateDetailVisual = shouldShowDetail && (forceDetailNearVisible || mustShowDetailForSafety);

                if (canUseFarImpostor)
                    visibleCharacterUsingFarImpostor++;
                else if (canUseSafeMid)
                    visibleCharacterUsingSafeMid++;
                else if (canUseSafeLow)
                    visibleCharacterUsingSafeLow++;
                else if (forceDetailByBudget)
                    visibleCharacterBudgetDetail++;
                else if (mustShowDetailForSafety)
                    visibleCharacterSafetyDetail++;
                else if (hasMidLodInstance && !hasSafeMid)
                    visibleCharacterForcedDetailByUnsafeMid++;
                visibleCharacterSafeGate++;
            }
            else if (isCharacter &&
                     shouldShowFar &&
                     distances[i].Visible != 0 &&
                     distances[i].DistanceSq <= VisibleCharacterImpostorNearDistance * VisibleCharacterImpostorNearDistance)
            {
                // Characters should not vanish into the far impostor path while they are near the
                // camera frustum. Prefer a mesh LOD, then detail as the last safe fallback.
                shouldShowFar = false;
                if (hasLowLodInstance)
                    shouldShowLow = true;
                else if (hasMidLodInstance)
                    shouldShowMid = true;
                else
                    shouldShowDetail = true;
            }
            if (enemyShouldUseImpostor && !(isProtectedVisibleCharacter && isMovingUnit))
            {
                shouldShowDetail = false;
                shouldShowMid = false;
                shouldShowLow = false;
                shouldShowFar = true;
                forceImmediateDetailVisual = false;
            }
            bool hasAnyMeshLodPrefab = hasMidLodPrefab || hasLowLodPrefab;
            bool hasAnyMeshLodInstance = hasMidLodInstance || hasLowLodInstance;
            bool keepDetailVisibleUntilReady = !shouldShowFar && !shouldShowDetail && hasAnyMeshLodPrefab && !hasAnyMeshLodInstance;
            if (keepDetailVisibleUntilReady)
            {
                shouldShowDetail = true;
                shouldShowMid = false;
                shouldShowLow = false;
                shouldShowFar = false;
                forceImmediateDetailVisual = true;
            }

            bool keepDetailVisibleDuringHandoff =
                (!shouldShowDetail && shouldShowMid && !IsVisualReadyForExclusiveDisplay(em, renderStateEcb, readyTaggedThisFrame, midRoot, childLookup)) ||
                (!shouldShowDetail && shouldShowLow && !IsVisualReadyForExclusiveDisplay(em, renderStateEcb, readyTaggedThisFrame, lowRoot, childLookup));
            if (keepDetailVisibleDuringHandoff)
            {
                shouldShowDetail = true;
                shouldShowFar = false;
                forceImmediateDetailVisual = true;
            }

            UnitRenderVisualKind desiredVisual = ResolveDesiredVisual(shouldShowDetail, shouldShowMid, shouldShowLow, shouldShowFar);
            bool hadVisualState = em.HasComponent<UnitRenderVisualState>(unit);
            UnitRenderVisualKind previousVisual = hadVisualState
                ? (UnitRenderVisualKind)em.GetComponentData<UnitRenderVisualState>(unit).Current
                : UnitRenderVisualKind.Unknown;
            UnitRenderVisualKind activeVisual = ResolveStableUnitRenderVisualState(
                em,
                renderStateEcb,
                unit,
                desiredVisual,
                forceImmediateDetailVisual && desiredVisual == UnitRenderVisualKind.Detail,
                ref visualStateChanges,
                ref visualStatePending,
                ref visualTransitionsCommitted);
            shouldShowDetail = activeVisual == UnitRenderVisualKind.Detail;
            shouldShowMid = activeVisual == UnitRenderVisualKind.Mid;
            shouldShowLow = activeVisual == UnitRenderVisualKind.Low;
            shouldShowFar = activeVisual == UnitRenderVisualKind.Far;
            bool applyVisualRoots =
                !hadVisualState ||
                previousVisual != activeVisual ||
                (activeVisual != desiredVisual && (desiredVisual == UnitRenderVisualKind.Mid || desiredVisual == UnitRenderVisualKind.Low));
            if (shouldShowFar)
                farCount++;

            bool farImpostor = em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit);
            if (!shouldShowFar && farImpostor)
            {
                unitsToShowDetailed.Add(unit);
                changed++;
            }
            else if (shouldShowFar && !farImpostor)
            {
                unitsToShowFarImpostor.Add(unit);
                changed++;
            }

            Entity detailRoot = Entity.Null;
            if (em.HasComponent<UnitDetailedVisualReference>(unit))
                detailRoot = em.GetComponentData<UnitDetailedVisualReference>(unit).Root;
            if (applyVisualRoots)
            {
                if (detailRoot != Entity.Null)
                {
                    if (shouldShowDetail)
                        visibleMidSafetyPatched += EnsureRenderSafetyRecursiveOnce(em, renderStateEcb, safetyTaggedThisFrame, detailRoot, childLookup);
                    CollectRenderVisibilityChangesRecursive(em, detailRoot, shouldShowDetail, childLookup, entitiesToShow, entitiesToHide, ref changed);
                }
                else
                {
                    if (shouldShowDetail)
                        visibleMidSafetyPatched += EnsureRenderSafetyRecursiveOnce(em, renderStateEcb, safetyTaggedThisFrame, unit, childLookup);
                    CollectRenderVisibilityChanges(em, unit, shouldShowDetail, childLookup, entitiesToShow, entitiesToHide, ref changed);
                }

                if (hasMidLodInstance)
                {
                    if (shouldShowMid)
                        visibleMidSafetyPatched += EnsureRenderSafetyRecursiveOnce(em, renderStateEcb, safetyTaggedThisFrame, midRoot, childLookup);
                    CollectRenderVisibilityChangesRecursive(em, midRoot, shouldShowMid, childLookup, entitiesToShow, entitiesToHide, ref changed);
                }

                if (hasLowLodInstance)
                {
                    if (shouldShowLow)
                        visibleMidSafetyPatched += EnsureRenderSafetyRecursiveOnce(em, renderStateEcb, safetyTaggedThisFrame, lowRoot, childLookup);
                    CollectRenderVisibilityChangesRecursive(em, lowRoot, shouldShowLow, childLookup, entitiesToShow, entitiesToHide, ref changed);
                }
            }
            else if (isProtectedVisibleCharacter && shouldShowMid && hasMidLodInstance)
            {
                visibleMidSafetyPatched += EnsureRenderSafetyRecursiveOnce(em, renderStateEcb, safetyTaggedThisFrame, midRoot, childLookup);
            }

            if (shouldShowMid)
                midShown++;
            if (shouldShowLow)
                lowShown++;
            if (isProtectedVisibleCharacter && distances[i].DistanceSq <= AlwaysDetailedDistanceSq)
            {
                if (shouldShowDetail)
                    visibleNearDetail++;
                else if (shouldShowMid)
                    visibleNearMid++;
            }
        }

        for (int i = 0; i < unitsToShowDetailed.Length; i++)
        {
            Entity unit = unitsToShowDetailed[i];
            if (em.Exists(unit) && em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit))
                em.RemoveComponent<UnitRenderBudgetCulledUnitTag>(unit);
        }

        for (int i = 0; i < unitsToShowFarImpostor.Length; i++)
        {
            Entity unit = unitsToShowFarImpostor[i];
            if (em.Exists(unit) && !em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit))
                em.AddComponent<UnitRenderBudgetCulledUnitTag>(unit);
        }

        for (int i = 0; i < entitiesToShow.Length; i++)
        {
            Entity entity = entitiesToShow[i];
            if (!em.Exists(entity))
                continue;

            if (em.HasComponent<Disabled>(entity))
                em.RemoveComponent<Disabled>(entity);
            if (em.HasComponent<DisableRendering>(entity))
                em.RemoveComponent<DisableRendering>(entity);
            if (em.HasComponent<UnitRenderBudgetCulledTag>(entity))
                em.RemoveComponent<UnitRenderBudgetCulledTag>(entity);
            shown++;
        }

        for (int i = 0; i < entitiesToHide.Length; i++)
        {
            Entity entity = entitiesToHide[i];
            if (!em.Exists(entity))
                continue;

            if (!em.HasComponent<DisableRendering>(entity))
                em.AddComponent<DisableRendering>(entity);
            if (!em.HasComponent<UnitRenderBudgetCulledTag>(entity))
                em.AddComponent<UnitRenderBudgetCulledTag>(entity);
            hidden++;
        }

        renderStateEcb.Playback(em);
        renderStateEcb.Dispose();

        double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
        if (EnableRenderBudgetDiagnostics && Time.frameCount >= _nextDiagnosticFrame)
        {
            _nextDiagnosticFrame = Time.frameCount + DiagnosticIntervalFrames;
            if (changed == 0 && visualStateChanges == 0 && missingMidInstance == 0 && missingLowInstance == 0)
            {
                LogRenderBudgetStateLight(em, distances, detailedCount, cameraMotionActive);
            }
            else
            {
                childLookup = SystemAPI.GetBufferLookup<Child>(true);
                LogMidLodDiagnostics(em, distances, detailedUnits, midLodUnits, lowLodUnits, childLookup, detailedCount, cameraMotionActive);
            }
        }

        if (EnableRenderBudgetFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect:ECS] UnitRenderBudgetSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms units={distances.Length} detailed={detailedCount} mid={midShown} low={lowShown} far={farCount} cameraMotion={(cameraMotionActive ? 1 : 0)} visibleCharacterSafeGate={visibleCharacterSafeGate} visibleCharacterMidInstances={visibleCharacterMidInstances} visibleCharacterSafeMidInstances={visibleCharacterSafeMidInstances} visibleCharacterLowInstances={visibleCharacterLowInstances} visibleCharacterSafeLowInstances={visibleCharacterSafeLowInstances} visibleCharacterUsingSafeMid={visibleCharacterUsingSafeMid} visibleCharacterUsingSafeLow={visibleCharacterUsingSafeLow} visibleCharacterUsingFarImpostor={visibleCharacterUsingFarImpostor} visibleCharacterBudgetDetail={visibleCharacterBudgetDetail} visibleCharacterSafetyDetail={visibleCharacterSafetyDetail} visibleCharacterMidSuppressed={visibleCharacterMidSuppressed} visibleNearDetail={visibleNearDetail} visibleNearMid={visibleNearMid} visibleCharacterLowDistance={math.sqrt(VisibleCharacterLowDistanceSq):F0} visibleCharacterImpostorBand={VisibleCharacterImpostorNearDistance:F0}-{VisibleCharacterImpostorFarDistance:F0} visibleCharacterForcedDetailByUnsafeMid={visibleCharacterForcedDetailByUnsafeMid} missingMid={missingMidInstance} missingLow={missingLowInstance} visualStateChanges={visualStateChanges} visualPending={visualStatePending} visualCommitted={visualTransitionsCommitted} visibleMidSafetyPatched={visibleMidSafetyPatched} changed={changed} shown={shown} hidden={hidden}");

        _budgetStable =
            !cameraMotionActive &&
            changed == 0 &&
            hidden == 0 &&
            shown == 0 &&
            visualStateChanges == 0 &&
            visualStatePending == 0 &&
            visualTransitionsCommitted == 0 &&
            visibleMidSafetyPatched == 0 &&
            missingMidInstance == 0 &&
            missingLowInstance == 0;
        _stableUnitCount = currentUnitCount;
    }

    private bool IsCameraMotionActive(Camera camera)
    {
        Vector3 currentPosition = camera.transform.position;
        Quaternion currentRotation = camera.transform.rotation;
        if (!_hasCameraSnapshot)
        {
            _hasCameraSnapshot = true;
            _lastCameraPosition = currentPosition;
            _lastCameraRotation = currentRotation;
            return false;
        }

        bool moved = Vector3.SqrMagnitude(currentPosition - _lastCameraPosition) > CameraMoveThresholdSq;
        bool rotated = Quaternion.Angle(currentRotation, _lastCameraRotation) > CameraRotateThresholdDegrees;
        _lastCameraPosition = currentPosition;
        _lastCameraRotation = currentRotation;

        if (moved || rotated)
        {
            _budgetStable = false;
            _lodResumeFrame = Time.frameCount + CameraSettleFrames;
            _nextDiagnosticFrame = 0;
            return true;
        }

        return Time.frameCount < _lodResumeFrame;
    }

    private static void LogRenderBudgetStateLight(
        EntityManager em,
        NativeList<UnitDistance> distances,
        int detailedCount,
        bool cameraMotionActive)
    {
        int targetDetail = 0;
        int targetMid = 0;
        int targetLow = 0;
        int targetFar = 0;
        int visibleCharacters = 0;
        int visibleCharacterNotDetail = 0;
        int visibleCharacterActiveMid = 0;
        int visibleCharacterActiveLow = 0;
        int visibleCharacterActiveFar = 0;
        int visibleCharacterScreenEdge = 0;
        int visibleCharacterScreenEdgeDetail = 0;
        int visibleNearDetail = 0;
        int visibleNearMid = 0;

        for (int i = 0; i < distances.Length; i++)
        {
            Entity unit = distances[i].Unit;
            if (!em.Exists(unit))
                continue;

            UnitRenderVisualKind activeVisual = em.HasComponent<UnitRenderVisualState>(unit)
                ? (UnitRenderVisualKind)em.GetComponentData<UnitRenderVisualState>(unit).Current
                : UnitRenderVisualKind.Detail;
            if (activeVisual == UnitRenderVisualKind.Detail)
                targetDetail++;
            else if (activeVisual == UnitRenderVisualKind.Mid)
                targetMid++;
            else if (activeVisual == UnitRenderVisualKind.Low)
                targetLow++;
            else
                targetFar++;

            bool isCharacter = IsCharacterUnit(em, unit);
            if (!isCharacter || distances[i].Visible == 0)
                continue;

            visibleCharacters++;
            if (distances[i].ScreenEdge != 0)
                visibleCharacterScreenEdge++;
            if (activeVisual != UnitRenderVisualKind.Detail)
                visibleCharacterNotDetail++;
            if (activeVisual == UnitRenderVisualKind.Mid)
                visibleCharacterActiveMid++;
            else if (activeVisual == UnitRenderVisualKind.Low)
                visibleCharacterActiveLow++;
            else if (activeVisual == UnitRenderVisualKind.Far)
                visibleCharacterActiveFar++;
            if (distances[i].ScreenEdge != 0 && activeVisual == UnitRenderVisualKind.Detail)
                visibleCharacterScreenEdgeDetail++;
            if (distances[i].DistanceSq <= AlwaysDetailedDistanceSq)
            {
                if (activeVisual == UnitRenderVisualKind.Detail)
                    visibleNearDetail++;
                else if (activeVisual == UnitRenderVisualKind.Mid)
                    visibleNearMid++;
            }
        }

        Debug.Log(
            $"[UnitRenderBudgetState] frame={Time.frameCount} units={distances.Length} targetDetail={targetDetail} targetMid={targetMid} targetLow={targetLow} targetFar={targetFar} " +
            $"cameraMotion={(cameraMotionActive ? 1 : 0)} visibleCharacters={visibleCharacters} visibleCharacterNotDetail={visibleCharacterNotDetail} visibleCharacterActiveMid={visibleCharacterActiveMid} visibleCharacterActiveLow={visibleCharacterActiveLow} visibleCharacterActiveFar={visibleCharacterActiveFar} visibleCharacterScreenEdge={visibleCharacterScreenEdge} visibleCharacterScreenEdgeDetail={visibleCharacterScreenEdgeDetail} visibleNearDetail={visibleNearDetail} visibleNearMid={visibleNearMid} detailedCap={detailedCount} light=1");
    }

    private static bool IsCharacterUnit(EntityManager em, Entity unit)
    {
        if (em.HasComponent<UnitMovementBehavior>(unit) &&
            em.GetComponentData<UnitMovementBehavior>(unit).UsesVehicleMotion != 0)
        {
            return false;
        }

        if (!em.HasComponent<UnitSourcePrefabKey>(unit))
            return false;

        FixedString64Bytes key = em.GetComponentData<UnitSourcePrefabKey>(unit).Value;
        return key.ToString().StartsWith("Unit_Chr_", System.StringComparison.Ordinal);
    }

    private static bool HasAnimationIndexRecursive(
        Entity entity,
        ComponentLookup<MaterialAnimationIndex> animationIndexLookup,
        BufferLookup<Child> childLookup)
    {
        if (entity == Entity.Null)
            return false;

        if (animationIndexLookup.HasComponent(entity))
            return true;

        if (!childLookup.HasBuffer(entity))
            return false;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            if (HasAnimationIndexRecursive(children[i].Value, animationIndexLookup, childLookup))
                return true;
        }

        return false;
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

    public static UnitRenderVisualKind ResolveVisibleCharacterVisualKind(
        bool movingVisibleCharacter,
        bool forceDetailNearVisible,
        bool forceDetailByBudget,
        bool farEnoughForImpostor,
        bool forceTacticalImpostor,
        bool lowEnoughForSafeLow,
        bool hasSafeMid,
        bool midRootAnimatable,
        bool hasSafeLow,
        bool lowRootAnimatable)
    {
        if (forceDetailNearVisible || forceDetailByBudget)
            return UnitRenderVisualKind.Detail;

        if (forceTacticalImpostor && farEnoughForImpostor)
            return UnitRenderVisualKind.Far;

        if (!movingVisibleCharacter && farEnoughForImpostor)
            return UnitRenderVisualKind.Far;

        if (!movingVisibleCharacter && lowEnoughForSafeLow && hasSafeLow && lowRootAnimatable)
            return UnitRenderVisualKind.Low;

        if (hasSafeMid && midRootAnimatable)
            return UnitRenderVisualKind.Mid;

        return UnitRenderVisualKind.Detail;
    }

    private static UnitRenderVisualKind ResolveDesiredVisualForDiagnostics(bool isCharacter, bool detail, bool mid, bool low)
    {
        if (detail)
            return UnitRenderVisualKind.Detail;
        if (mid)
            return UnitRenderVisualKind.Mid;
        if (low)
            return UnitRenderVisualKind.Low;

        return isCharacter ? UnitRenderVisualKind.Detail : UnitRenderVisualKind.Far;
    }

    private static float ResolveVisibleCharacterImpostorDistance(Entity unit)
    {
        uint hash = math.hash(new uint2((uint)unit.Index, (uint)unit.Version));
        float normalized = hash / (float)uint.MaxValue;
        return math.lerp(VisibleCharacterImpostorNearDistance, VisibleCharacterImpostorFarDistance, normalized);
    }

    private static UnitRenderVisualKind ResolveStableUnitRenderVisualState(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity unit,
        UnitRenderVisualKind desiredVisual,
        bool forceImmediate,
        ref int visualStateChanges,
        ref int visualStatePending,
        ref int visualTransitionsCommitted)
    {
        byte desired = (byte)desiredVisual;
        if (!em.HasComponent<UnitRenderVisualState>(unit))
        {
            ecb.AddComponent(unit, new UnitRenderVisualState
            {
                Current = desired,
                Desired = desired,
                LastChangedFrame = Time.frameCount
            });
            visualStateChanges++;
            return desiredVisual;
        }

        UnitRenderVisualState state = em.GetComponentData<UnitRenderVisualState>(unit);
        if (state.Desired != desired)
        {
            state.Desired = desired;
            state.LastChangedFrame = Time.frameCount;
            if (forceImmediate)
                state.Current = desired;
            em.SetComponentData(unit, state);
            visualStateChanges++;
            if (forceImmediate)
            {
                visualTransitionsCommitted++;
                return desiredVisual;
            }

            visualStatePending++;
            return (UnitRenderVisualKind)state.Current;
        }

        if (state.Current == desired)
            return (UnitRenderVisualKind)state.Current;

        visualStatePending++;
        if (forceImmediate)
        {
            state.Current = desired;
            state.Desired = desired;
            state.LastChangedFrame = Time.frameCount;
            em.SetComponentData(unit, state);
            visualStateChanges++;
            visualTransitionsCommitted++;
            return desiredVisual;
        }

        bool stableLongEnough = Time.frameCount - state.LastChangedFrame >= VisualTransitionStableFrames;
        bool transitionBudgetAvailable = visualTransitionsCommitted < MaxVisualStateTransitionsPerUpdate;
        if (!stableLongEnough || !transitionBudgetAvailable)
            return (UnitRenderVisualKind)state.Current;

        state.Current = desired;
        em.SetComponentData(unit, state);
        visualStateChanges++;
        visualTransitionsCommitted++;
        return desiredVisual;
    }

    private static bool IsVisualReadyForExclusiveDisplay(
        EntityManager em,
        EntityCommandBuffer ecb,
        NativeHashSet<Entity> readyTaggedThisFrame,
        Entity root,
        BufferLookup<Child> childLookup)
    {
        if (root == Entity.Null || !em.Exists(root))
            return false;
        if (em.HasComponent<UnitRenderVisualReadyTag>(root) || readyTaggedThisFrame.Contains(root))
            return true;

        bool hasRenderable = false;
        bool waitingForGpuAnimationMaterial = false;
        CheckVisualReadinessRecursive(em, root, childLookup, ref hasRenderable, ref waitingForGpuAnimationMaterial);
        bool ready = hasRenderable && !waitingForGpuAnimationMaterial;
        if (ready)
        {
            readyTaggedThisFrame.Add(root);
            ecb.AddComponent<UnitRenderVisualReadyTag>(root);
        }

        return ready;
    }

    private static bool IsVisualReadyForExclusiveDisplay(EntityManager em, Entity root, BufferLookup<Child> childLookup)
    {
        if (root == Entity.Null || !em.Exists(root))
            return false;
        if (em.HasComponent<UnitRenderVisualReadyTag>(root))
            return true;

        bool hasRenderable = false;
        bool waitingForGpuAnimationMaterial = false;
        CheckVisualReadinessRecursive(em, root, childLookup, ref hasRenderable, ref waitingForGpuAnimationMaterial);
        return hasRenderable && !waitingForGpuAnimationMaterial;
    }

    private static void CheckVisualReadinessRecursive(
        EntityManager em,
        Entity entity,
        BufferLookup<Child> childLookup,
        ref bool hasRenderable,
        ref bool waitingForGpuAnimationMaterial)
    {
        if (!em.Exists(entity))
            return;

        if (IsRenderableEntity(em, entity))
            hasRenderable = true;

        if (em.HasComponent<MeshLODComponent>(entity) &&
            !em.HasComponent<MaterialAlphaCompleteTag>(entity))
        {
            waitingForGpuAnimationMaterial = true;
        }

        if (!childLookup.HasBuffer(entity))
            return;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
            CheckVisualReadinessRecursive(em, children[i].Value, childLookup, ref hasRenderable, ref waitingForGpuAnimationMaterial);
    }

    private static bool HasMaterialAlphaCompleteRecursive(EntityManager em, Entity entity, BufferLookup<Child> childLookup)
    {
        if (!em.Exists(entity))
            return false;

        if (em.HasComponent<MaterialAlphaCompleteTag>(entity))
            return true;

        if (!childLookup.HasBuffer(entity))
            return false;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            if (HasMaterialAlphaCompleteRecursive(em, children[i].Value, childLookup))
                return true;
        }

        return false;
    }

    private static void LogMidLodDiagnostics(
        EntityManager em,
        NativeList<UnitDistance> distances,
        NativeHashSet<Entity> detailedUnits,
        NativeHashSet<Entity> midLodUnits,
        NativeHashSet<Entity> lowLodUnits,
        BufferLookup<Child> childLookup,
        int detailedCount,
        bool cameraMotionActive)
    {
        int targetDetail = 0;
        int targetMid = 0;
        int targetLow = 0;
        int targetFar = 0;
        int missingMidInstance = 0;
        int missingLowInstance = 0;
        int invisible = 0;
        int wrongDetail = 0;
        int wrongMid = 0;
        int wrongLow = 0;
        int wrongFar = 0;
        int doubleVisible = 0;
        int detailVisibleCount = 0;
        int midVisibleCount = 0;
        int lowVisibleCount = 0;
        int farVisibleCount = 0;
        int detailAndMidVisible = 0;
        int detailAndFarVisible = 0;
        int midAndFarVisible = 0;
        int lowOverlapVisible = 0;
        int visibleCharacters = 0;
        int visibleCharacterNotDetail = 0;
        int visibleCharacterSafeUnits = 0;
        int visibleCharacterMidInstances = 0;
        int visibleCharacterSafeMidInstances = 0;
        int visibleCharacterLowInstances = 0;
        int visibleCharacterSafeLowInstances = 0;
        int visibleCharacterActiveMid = 0;
        int visibleCharacterActiveLow = 0;
        int visibleCharacterActiveFar = 0;
        int visibleCharacterScreenEdge = 0;
        int visibleCharacterScreenEdgeDetail = 0;
        string sample = string.Empty;

        for (int i = 0; i < distances.Length; i++)
        {
            Entity unit = distances[i].Unit;
            if (!em.Exists(unit))
                continue;

            bool hasMidPrefab = em.HasComponent<UnitMidLodPrefabReference>(unit);
            bool hasMidInstance = em.HasComponent<UnitMidLodInstanceReference>(unit);
            if (hasMidPrefab && !hasMidInstance)
                missingMidInstance++;
            bool hasLowPrefab = em.HasComponent<UnitLowLodPrefabReference>(unit);
            bool hasLowInstance = em.HasComponent<UnitLowLodInstanceReference>(unit);
            if (hasLowPrefab && !hasLowInstance)
                missingLowInstance++;
            bool isCharacter = IsCharacterUnit(em, unit);
            if (isCharacter && distances[i].Visible != 0)
            {
                visibleCharacters++;
                if (distances[i].ScreenEdge != 0)
                    visibleCharacterScreenEdge++;
                if (em.HasComponent<UnitUsesSafeVisibleCharacterLodTag>(unit))
                    visibleCharacterSafeUnits++;
                if (hasMidInstance)
                {
                    visibleCharacterMidInstances++;
                    Entity midInstance = em.GetComponentData<UnitMidLodInstanceReference>(unit).Instance;
                    if (IsSafeVisibleCharacterLod(em, midInstance))
                        visibleCharacterSafeMidInstances++;
                }
                if (hasLowInstance)
                {
                    visibleCharacterLowInstances++;
                    Entity lowInstance = em.GetComponentData<UnitLowLodInstanceReference>(unit).Instance;
                    if (IsSafeVisibleCharacterLod(em, lowInstance))
                        visibleCharacterSafeLowInstances++;
                }
            }

            UnitRenderVisualKind activeVisual = em.HasComponent<UnitRenderVisualState>(unit)
                ? (UnitRenderVisualKind)em.GetComponentData<UnitRenderVisualState>(unit).Current
                : ResolveDesiredVisualForDiagnostics(
                    isCharacter,
                    detailedUnits.Contains(unit),
                    hasMidInstance && midLodUnits.Contains(unit),
                    hasLowInstance && lowLodUnits.Contains(unit));
            bool shouldDetail = activeVisual == UnitRenderVisualKind.Detail;
            bool shouldMid = activeVisual == UnitRenderVisualKind.Mid;
            bool shouldLow = activeVisual == UnitRenderVisualKind.Low;
            bool shouldFar = activeVisual == UnitRenderVisualKind.Far;
            if (isCharacter && distances[i].Visible != 0 && !shouldDetail)
                visibleCharacterNotDetail++;
            if (isCharacter && distances[i].Visible != 0 && shouldMid)
                visibleCharacterActiveMid++;
            if (isCharacter && distances[i].Visible != 0 && shouldLow)
                visibleCharacterActiveLow++;
            if (isCharacter && distances[i].Visible != 0 && shouldFar)
                visibleCharacterActiveFar++;
            if (isCharacter && distances[i].Visible != 0 && distances[i].ScreenEdge != 0 && shouldDetail)
                visibleCharacterScreenEdgeDetail++;

            if (shouldDetail)
                targetDetail++;
            else if (shouldMid)
                targetMid++;
            else if (shouldLow)
                targetLow++;
            else
                targetFar++;

            bool detailVisible = false;
            Entity detailRoot = Entity.Null;
            if (em.HasComponent<UnitDetailedVisualReference>(unit))
            {
                detailRoot = em.GetComponentData<UnitDetailedVisualReference>(unit).Root;
                detailVisible = IsRenderableVisibleRecursive(em, detailRoot, childLookup);
            }

            bool midVisible = false;
            Entity midRoot = Entity.Null;
            if (hasMidInstance)
            {
                midRoot = em.GetComponentData<UnitMidLodInstanceReference>(unit).Instance;
                midVisible = IsRenderableVisibleRecursive(em, midRoot, childLookup);
            }

            bool lowVisible = false;
            Entity lowRoot = Entity.Null;
            if (hasLowInstance)
            {
                lowRoot = em.GetComponentData<UnitLowLodInstanceReference>(unit).Instance;
                lowVisible = IsRenderableVisibleRecursive(em, lowRoot, childLookup);
            }

            bool farVisible = em.HasComponent<UnitRenderBudgetCulledUnitTag>(unit);
            if (detailVisible)
                detailVisibleCount++;
            if (midVisible)
                midVisibleCount++;
            if (lowVisible)
                lowVisibleCount++;
            if (farVisible)
                farVisibleCount++;

            int visibleCount = (detailVisible ? 1 : 0) + (midVisible ? 1 : 0) + (lowVisible ? 1 : 0) + (farVisible ? 1 : 0);
            if (visibleCount == 0)
            {
                invisible++;
                AppendDiagnosticSample(ref sample, em, childLookup, unit, "none", detailRoot, midRoot, lowRoot, farVisible);
            }
            bool expectedHandoff =
                detailVisible &&
                ((shouldMid && midVisible && !IsVisualReadyForExclusiveDisplay(em, midRoot, childLookup)) ||
                 (shouldLow && lowVisible && !IsVisualReadyForExclusiveDisplay(em, lowRoot, childLookup)));
            bool expectedVisibleCharacterSafeHandoff =
                isCharacter &&
                distances[i].Visible != 0 &&
                detailVisible &&
                shouldLow &&
                lowVisible &&
                hasLowInstance &&
                IsSafeVisibleCharacterLod(em, lowRoot);
            if (visibleCount > 1 && !expectedHandoff && !expectedVisibleCharacterSafeHandoff)
            {
                doubleVisible++;
                if (detailVisible && midVisible)
                    detailAndMidVisible++;
                if (detailVisible && farVisible)
                    detailAndFarVisible++;
                if (midVisible && farVisible)
                    midAndFarVisible++;
                if (lowVisible)
                    lowOverlapVisible++;
            }
            bool wrongDetailState = shouldDetail && !detailVisible;
            bool wrongMidState = shouldMid && !midVisible;
            bool wrongLowState = shouldLow && !lowVisible;
            bool wrongFarState = shouldFar && !farVisible;
            if (wrongDetailState)
                wrongDetail++;
            if (wrongMidState)
                wrongMid++;
            if (wrongLowState)
                wrongLow++;
            if (wrongFarState)
                wrongFar++;
            if (wrongDetailState || wrongMidState || wrongLowState || wrongFarState)
                AppendDiagnosticSample(ref sample, em, childLookup, unit, "wrong", detailRoot, midRoot, lowRoot, farVisible);
        }

        int mismatches = invisible + wrongDetail + wrongMid + wrongLow + wrongFar;
        bool hasProblems = mismatches != 0 ||
                           doubleVisible != 0 ||
                           missingMidInstance != 0 ||
                           missingLowInstance != 0 ||
                           visibleCharacterNotDetail > visibleCharacterSafeMidInstances + visibleCharacterSafeLowInstances + visibleCharacterActiveFar;
        if (!hasProblems)
        {
            Debug.Log(
                $"[UnitRenderBudgetState] frame={Time.frameCount} units={distances.Length} targetDetail={targetDetail} targetMid={targetMid} targetLow={targetLow} targetFar={targetFar} " +
                $"visibleDetail={detailVisibleCount} visibleMid={midVisibleCount} visibleLow={lowVisibleCount} visibleFar={farVisibleCount} cameraMotion={(cameraMotionActive ? 1 : 0)} visibleCharacters={visibleCharacters} visibleCharacterNotDetail={visibleCharacterNotDetail} visibleCharacterActiveMid={visibleCharacterActiveMid} visibleCharacterActiveLow={visibleCharacterActiveLow} visibleCharacterActiveFar={visibleCharacterActiveFar} visibleCharacterScreenEdge={visibleCharacterScreenEdge} visibleCharacterScreenEdgeDetail={visibleCharacterScreenEdgeDetail} visibleCharacterSafeUnits={visibleCharacterSafeUnits} visibleCharacterMidInstances={visibleCharacterMidInstances} visibleCharacterSafeMidInstances={visibleCharacterSafeMidInstances} visibleCharacterLowInstances={visibleCharacterLowInstances} visibleCharacterSafeLowInstances={visibleCharacterSafeLowInstances} visibleCharacterLowDistance={math.sqrt(VisibleCharacterLowDistanceSq):F0} visibleCharacterImpostorBand={VisibleCharacterImpostorNearDistance:F0}-{VisibleCharacterImpostorFarDistance:F0} detailedCap={detailedCount}");
            return;
        }

        Debug.LogWarning(
            $"[UnitRenderVisibilityDiag] frame={Time.frameCount} units={distances.Length} targetDetail={targetDetail} targetMid={targetMid} targetLow={targetLow} targetFar={targetFar} " +
            $"visibleDetail={detailVisibleCount} visibleMid={midVisibleCount} visibleLow={lowVisibleCount} visibleFar={farVisibleCount} cameraMotion={(cameraMotionActive ? 1 : 0)} invisible={invisible} doubleVisible={doubleVisible} " +
            $"detailMid={detailAndMidVisible} detailFar={detailAndFarVisible} midFar={midAndFarVisible} lowOverlap={lowOverlapVisible} " +
            $"wrongDetail={wrongDetail} wrongMid={wrongMid} wrongLow={wrongLow} wrongFar={wrongFar} missingMid={missingMidInstance} missingLow={missingLowInstance} visibleCharacters={visibleCharacters} visibleCharacterNotDetail={visibleCharacterNotDetail} visibleCharacterActiveMid={visibleCharacterActiveMid} visibleCharacterActiveLow={visibleCharacterActiveLow} visibleCharacterActiveFar={visibleCharacterActiveFar} visibleCharacterScreenEdge={visibleCharacterScreenEdge} visibleCharacterScreenEdgeDetail={visibleCharacterScreenEdgeDetail} visibleCharacterSafeUnits={visibleCharacterSafeUnits} visibleCharacterMidInstances={visibleCharacterMidInstances} visibleCharacterSafeMidInstances={visibleCharacterSafeMidInstances} visibleCharacterLowInstances={visibleCharacterLowInstances} visibleCharacterSafeLowInstances={visibleCharacterSafeLowInstances} visibleCharacterLowDistance={math.sqrt(VisibleCharacterLowDistanceSq):F0} visibleCharacterImpostorBand={VisibleCharacterImpostorNearDistance:F0}-{VisibleCharacterImpostorFarDistance:F0} detailedCap={detailedCount} samples={sample}");
    }

    private static void CollectRenderVisibilityChanges(
        EntityManager em,
        Entity root,
        bool visible,
        BufferLookup<Child> childLookup,
        NativeList<Entity> entitiesToShow,
        NativeList<Entity> entitiesToHide,
        ref int changed)
    {
        if (!childLookup.HasBuffer(root))
            return;

        DynamicBuffer<Child> children = childLookup[root];
        for (int i = 0; i < children.Length; i++)
            CollectRenderVisibilityChangesRecursive(em, children[i].Value, visible, childLookup, entitiesToShow, entitiesToHide, ref changed);
    }

    private static int EnsureRenderSafetyRecursive(EntityManager em, Entity entity, BufferLookup<Child> childLookup)
    {
        if (!em.Exists(entity))
            return 0;

        int patched = 0;
        if (em.HasComponent<Unity.Rendering.RenderBounds>(entity))
        {
            Unity.Rendering.RenderBounds bounds = em.GetComponentData<Unity.Rendering.RenderBounds>(entity);
            float3 extents = math.max(bounds.Value.Extents, RenderBoundsMinExtents);
            if (!math.all(bounds.Value.Extents == extents))
            {
                bounds.Value.Extents = extents;
                em.SetComponentData(entity, bounds);
                patched++;
            }
        }

        if (em.HasComponent<MeshLODComponent>(entity))
        {
            MeshLODComponent meshLod = em.GetComponentData<MeshLODComponent>(entity);
            if (meshLod.LODMask != AlwaysVisibleLodMask)
            {
                meshLod.LODMask = AlwaysVisibleLodMask;
                em.SetComponentData(entity, meshLod);
                patched++;
            }

            patched += PatchLodGroup(em, meshLod.Group);
            patched += PatchLodGroup(em, meshLod.ParentGroup);
        }

        if (!childLookup.HasBuffer(entity))
            return patched;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
            patched += EnsureRenderSafetyRecursive(em, children[i].Value, childLookup);

        return patched;
    }

    private static int EnsureRenderSafetyRecursiveOnce(
        EntityManager em,
        EntityCommandBuffer ecb,
        NativeHashSet<Entity> taggedThisFrame,
        Entity entity,
        BufferLookup<Child> childLookup)
    {
        if (!em.Exists(entity))
            return 0;
        if (em.HasComponent<UnitRenderSafetyPatchedTag>(entity) || taggedThisFrame.Contains(entity))
            return 0;

        int patched = EnsureRenderSafetyRecursive(em, entity, childLookup);
        taggedThisFrame.Add(entity);
        ecb.AddComponent<UnitRenderSafetyPatchedTag>(entity);

        return patched;
    }

    private static int PatchLodGroup(EntityManager em, Entity group)
    {
        if (group == Entity.Null || !em.Exists(group) || !em.HasComponent<MeshLODGroupComponent>(group))
            return 0;

        MeshLODGroupComponent lodGroup = em.GetComponentData<MeshLODGroupComponent>(group);
        bool changed =
            lodGroup.ParentMask != AlwaysVisibleLodMask ||
            !math.all(lodGroup.LODDistances0 == new float4(AlwaysVisibleLodDistance)) ||
            !math.all(lodGroup.LODDistances1 == new float4(AlwaysVisibleLodDistance));
        if (!changed)
            return 0;

        lodGroup.ParentMask = AlwaysVisibleLodMask;
        lodGroup.LODDistances0 = new float4(AlwaysVisibleLodDistance);
        lodGroup.LODDistances1 = new float4(AlwaysVisibleLodDistance);
        em.SetComponentData(group, lodGroup);
        return 1;
    }

    private static void CollectRenderVisibilityChangesRecursive(
        EntityManager em,
        Entity entity,
        bool visible,
        BufferLookup<Child> childLookup,
        NativeList<Entity> entitiesToShow,
        NativeList<Entity> entitiesToHide,
        ref int changed)
    {
        if (!em.Exists(entity))
            return;

        bool isCulled = em.HasComponent<UnitRenderBudgetCulledTag>(entity);
        bool isHidden = em.HasComponent<Disabled>(entity) || em.HasComponent<DisableRendering>(entity);
        if (visible)
        {
            if (isCulled || isHidden)
            {
                entitiesToShow.Add(entity);
                changed++;
            }
        }
        else if (!isCulled || !isHidden)
        {
            entitiesToHide.Add(entity);
            changed++;
        }

        if (!childLookup.HasBuffer(entity))
            return;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
            CollectRenderVisibilityChangesRecursive(em, children[i].Value, visible, childLookup, entitiesToShow, entitiesToHide, ref changed);
    }

    private static bool IsRenderableVisibleRecursive(EntityManager em, Entity entity, BufferLookup<Child> childLookup)
    {
        if (!em.Exists(entity))
            return false;

        if (IsRenderableEntity(em, entity) &&
            !em.HasComponent<Disabled>(entity) &&
            !em.HasComponent<DisableRendering>(entity) &&
            !em.HasComponent<UnitRenderBudgetCulledTag>(entity))
        {
            return true;
        }

        if (!childLookup.HasBuffer(entity))
            return false;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            if (IsRenderableVisibleRecursive(em, children[i].Value, childLookup))
                return true;
        }

        return false;
    }

    private static bool HasRenderableRecursive(EntityManager em, Entity entity, BufferLookup<Child> childLookup)
    {
        if (!em.Exists(entity))
            return false;

        if (IsRenderableEntity(em, entity))
            return true;

        if (!childLookup.HasBuffer(entity))
            return false;

        DynamicBuffer<Child> children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
        {
            if (HasRenderableRecursive(em, children[i].Value, childLookup))
                return true;
        }

        return false;
    }

    private static bool IsSafeVisibleCharacterLod(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
               em.Exists(entity) &&
               em.HasComponent<UnitSafeVisibleCharacterLodTag>(entity);
    }

    private static bool IsRenderableEntity(EntityManager em, Entity entity)
    {
        return em.HasComponent<RenderFilterSettings>(entity) ||
               em.HasComponent<Unity.Rendering.RenderBounds>(entity);
    }

    private static void AppendDiagnosticSample(
        ref string sample,
        EntityManager em,
        BufferLookup<Child> childLookup,
        Entity unit,
        string state,
        Entity detailRoot,
        Entity midRoot,
        Entity lowRoot,
        bool farVisible)
    {
        if (sample.Length > 900)
            return;

        if (sample.Length > 0)
            sample += " | ";

        string key = em.HasComponent<UnitSourcePrefabKey>(unit)
            ? em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString()
            : "unknown";
        sample += $"{unit}:{state}:{key} " +
                  $"detail={DescribeVisualRoot(em, childLookup, detailRoot)} " +
                  $"mid={DescribeVisualRoot(em, childLookup, midRoot)} " +
                  $"low={DescribeVisualRoot(em, childLookup, lowRoot)} " +
                  $"far={(farVisible ? 1 : 0)}";
    }

    private static string DescribeVisualRoot(EntityManager em, BufferLookup<Child> childLookup, Entity root)
    {
        if (root == Entity.Null)
            return "null";
        if (!em.Exists(root))
            return $"{root}:missing";

        int disabled = em.HasComponent<Disabled>(root) || em.HasComponent<DisableRendering>(root) ? 1 : 0;
        int culled = em.HasComponent<UnitRenderBudgetCulledTag>(root) ? 1 : 0;
        int alpha = HasMaterialAlphaCompleteRecursive(em, root, childLookup) ? 1 : 0;
        int renderable = HasRenderableRecursive(em, root, childLookup) ? 1 : 0;
        int visible = IsRenderableVisibleRecursive(em, root, childLookup) ? 1 : 0;
        return $"{root}:d{disabled}:c{culled}:a{alpha}:r{renderable}:v{visible}";
    }
}
