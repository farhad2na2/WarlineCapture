using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using SnivelerCode.GpuAnimation.Scripts.Components;
using UnitDistance = UnitRenderBudgetDistance.UnitDistance;

[UpdateAfter(typeof(UnitMassRenderSettingsSystem))]
public partial struct UnitRenderBudgetSystem : ISystem
{
    private const int MaxDetailedUnits = 48;
    private const int MaxMidLodUnits = 128;
    private const int MaxLowLodUnits = 128;
    private const int MaxUpdatesPerFrame = 4096;
    private const int UpdateIntervalFrames = 10;
    private const float AlwaysDetailedDistanceSq = 48f * 48f;
    private const float VisibleCharacterLowDistanceSq = 72f * 72f;
    private const float VisibleCharacterImpostorNearDistance = 96f;
    private const float VisibleCharacterImpostorFarDistance = 128f;
    private const float EnemyAlwaysDetailedDistanceSq = 14f * 14f;
    private const float EnemyLowLodDistanceSq = 20f * 20f;
    private const float EnemyImpostorDistanceSq = 28f * 28f;
    private const float VisibleCharacterViewportPadding = 0.35f;
    private const float VisibleCharacterEdgeSafetyMargin = 0.18f;

    private UnitRenderBudgetSources _querySystem;
    private UnitRenderBudgetSources.Context _queryContext;
    private UnitRenderBudgetSchedule _scheduleSystem;
    private UnitRenderBudgetCameraMotion _cameraMotionSystem;
    private UnitRenderBudgetSnapshot _snapshotSystem;
    private UnitRenderBudgetDistance _distanceSystem;
    private UnitRenderBudgetSort _sortSystem;
    private UnitRenderBudgetBand _bandSystem;
    private UnitRenderBudgetClassification _classificationSystem;
    private UnitRenderBudgetCharacterPolicy _characterPolicySystem;
    private UnitRenderBudgetLodReferences _lodReferenceSystem;
    private UnitRenderBudgetAnimationReadiness _animationReadinessSystem;
    private UnitRenderBudgetRenderableState _renderableQuerySystem;
    private UnitRenderBudgetVisualState _visualStateSystem;
    private UnitRenderBudgetReadiness _readinessSystem;
    private UnitRenderBudgetRenderSafety _renderSafetySystem;
    private UnitRenderBudgetVisualPlan _visualPlanSystem;
    private UnitRenderBudgetDecision _decisionSystem;
    private UnitRenderBudgetVisibilityChange _visibilityChangeSystem;
    private UnitRenderBudgetImpostorTag _impostorTagSystem;
    private UnitRenderBudgetVisibilityApply _visibilityApplySystem;
    private UnitRenderBudgetDiagnosticState _diagnosticStateSystem;
    private UnitRenderBudgetDiagnosticLog _diagnosticLogSystem;
    private UnitRenderBudgetLightDiagnostic _lightDiagnosticSystem;
    private UnitRenderBudgetMismatchDiagnostic _mismatchDiagnosticSystem;
    private UnitRenderBudgetFreezeDiagnostic _freezeDiagnosticSystem;
    private EntityQuery _renderableEntityQuery;
    private EntityStorageInfoLookup _entityStorageInfoLookup;
    private UnitRenderBudgetLodReferences.Lookups _lodReferenceLookups;
    private UnitRenderBudgetRenderableState.Lookups _renderableQueryLookups;
    private UnitRenderBudgetAnimationReadiness.Lookups _animationReadinessLookups;
    private UnitRenderBudgetReadiness.Lookups _readinessLookups;
    private UnitRenderBudgetRenderSafety.Lookups _renderSafetyLookups;
    private UnitRenderBudgetVisibilityApply.Lookups _visibilityApplyLookups;
    private ComponentLookup<UnitRenderVisualComponent> _visualStateLookup;
    private EntityTypeHandle _unitEntityTypeHandle;
    private ComponentTypeHandle<LocalTransform> _unitLocalTransformTypeHandle;

    public void OnCreate(ref SystemState state)
    {
        _queryContext = _querySystem.Create(ref state);
        _renderableEntityQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            Any = new[]
            {
                ComponentType.ReadOnly<RenderFilterSettings>(),
                ComponentType.ReadOnly<RenderBounds>()
            }
        });
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
        _lodReferenceLookups = new UnitRenderBudgetLodReferences.Lookups
        {
            DetailedVisualReferenceLookup = state.GetComponentLookup<UnitDetailedVisualReference>(true),
            MidLodPrefabReferenceLookup = state.GetComponentLookup<UnitMidLodPrefabReference>(true),
            MidLodInstanceReferenceLookup = state.GetComponentLookup<UnitMidLodInstanceReference>(true),
            LowLodPrefabReferenceLookup = state.GetComponentLookup<UnitLowLodPrefabReference>(true),
            LowLodInstanceReferenceLookup = state.GetComponentLookup<UnitLowLodInstanceReference>(true)
        };
        _renderableQueryLookups = new UnitRenderBudgetRenderableState.Lookups
        {
            EntityStorageInfoLookup = state.GetEntityStorageInfoLookup(),
            RenderableEntityMask = _renderableEntityQuery.GetEntityQueryMask(),
            DisabledLookup = state.GetComponentLookup<Disabled>(true),
            DisableRenderingLookup = state.GetComponentLookup<DisableRendering>(true),
            CulledTagLookup = state.GetComponentLookup<UnitRenderBudgetCulledTag>(true),
            SafeVisibleCharacterLodLookup = state.GetComponentLookup<UnitSafeVisibleCharacterLodTag>(true)
        };
        _animationReadinessLookups = new UnitRenderBudgetAnimationReadiness.Lookups
        {
            MeshLodLookup = state.GetComponentLookup<MeshLODComponent>(true),
            MaterialAlphaCompleteLookup = state.GetComponentLookup<MaterialAlphaCompleteTag>(true),
            HasGpuAnimationMaterialLookups = 1
        };
        _readinessLookups = new UnitRenderBudgetReadiness.Lookups
        {
            EntityStorageInfoLookup = state.GetEntityStorageInfoLookup(),
            VisualReadyLookup = state.GetComponentLookup<UnitRenderVisualReadyTag>(true)
        };
        _renderSafetyLookups = new UnitRenderBudgetRenderSafety.Lookups
        {
            EntityStorageInfoLookup = state.GetEntityStorageInfoLookup(),
            SafetyPatchedLookup = state.GetComponentLookup<UnitRenderSafetyPatchedTag>(true),
            RenderBoundsLookup = state.GetComponentLookup<RenderBounds>(true),
            MeshLodLookup = state.GetComponentLookup<MeshLODComponent>(true),
            MeshLodGroupLookup = state.GetComponentLookup<MeshLODGroupComponent>(true)
        };
        _visibilityApplyLookups = new UnitRenderBudgetVisibilityApply.Lookups
        {
            EntityStorageInfoLookup = state.GetEntityStorageInfoLookup(),
            CulledUnitLookup = state.GetComponentLookup<UnitRenderBudgetCulledUnitTag>(true),
            DisabledLookup = state.GetComponentLookup<Disabled>(true),
            DisableRenderingLookup = state.GetComponentLookup<DisableRendering>(true),
            CulledTagLookup = state.GetComponentLookup<UnitRenderBudgetCulledTag>(true)
        };
        _visualStateLookup = state.GetComponentLookup<UnitRenderVisualComponent>(true);
        _unitEntityTypeHandle = state.GetEntityTypeHandle();
        _unitLocalTransformTypeHandle = state.GetComponentTypeHandle<LocalTransform>(true);
        state.RequireForUpdate<RuntimeGameplayStateComponent>();

    }

    public void OnUpdate(ref SystemState state)
    {
        RuntimeGameplayStateComponent runtimeGameplayState = SystemAPI.GetSingleton<RuntimeGameplayStateComponent>();
        if (runtimeGameplayState.PlayRequested == 0)
            return;

        if (!RuntimeCameraReferenceSystem.TryGetCameraSnapshot(state.World, out RuntimeCameraSnapshotComponent camera))
            return;

        bool cameraMotionActive = _cameraMotionSystem.IsCameraMotionActive(camera, ref _scheduleSystem, ref _diagnosticStateSystem, Time.frameCount);
        int currentUnitCount = _queryContext.UnitQuery.CalculateEntityCount();
        int currentSelectedUnitHash = CalculateSelectedUnitHash(_queryContext.SelectedUnitQuery, out int currentSelectedUnitCount);
        if (_scheduleSystem.ShouldSkipStableBudget(
                cameraMotionActive,
                currentUnitCount,
                currentSelectedUnitCount,
                currentSelectedUnitHash))
            return;

        if (_scheduleSystem.ShouldSkipUpdateFrame(
                cameraMotionActive,
                Time.frameCount,
                currentSelectedUnitCount,
                currentSelectedUnitHash))
            return;

        _scheduleSystem.ScheduleNextUpdate(cameraMotionActive, Time.frameCount, UpdateIntervalFrames);
        double startTime = Time.realtimeSinceStartupAsDouble;
        EntityManager em = state.EntityManager;
        var renderStateEcb = new EntityCommandBuffer(Allocator.Temp);
        var childLookup = SystemAPI.GetBufferLookup<Child>(true);
        var animationIndexLookup = SystemAPI.GetComponentLookup<MaterialAnimationIndex>(true);
        var moveVisualLookup = SystemAPI.GetComponentLookup<UnitMoveVisualComponent>(true);
        var movementBehaviorLookup = SystemAPI.GetComponentLookup<UnitMovementBehavior>(true);
        var sourcePrefabKeyLookup = SystemAPI.GetComponentLookup<UnitSourcePrefabKey>(true);
        var factionLookup = SystemAPI.GetComponentLookup<Faction>(true);
        var selectedLookup = SystemAPI.GetComponentLookup<SelectedUnitTag>(true);
        var culledUnitLookup = SystemAPI.GetComponentLookup<UnitRenderBudgetCulledUnitTag>(true);
        var disabledLookup = SystemAPI.GetComponentLookup<Disabled>(true);
        var disableRenderingLookup = SystemAPI.GetComponentLookup<DisableRendering>(true);
        var culledTagLookup = SystemAPI.GetComponentLookup<UnitRenderBudgetCulledTag>(true);
        var passengerLookup = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true);
        _entityStorageInfoLookup.Update(ref state);
        _lodReferenceLookups.Update(ref state);
        _renderableQueryLookups.Update(ref state);
        _animationReadinessLookups.Update(ref state);
        _readinessLookups.Update(ref state);
        _renderSafetyLookups.Update(ref state);
        _visibilityApplyLookups.Update(ref state);
        _visualStateLookup.Update(ref state);
        _unitEntityTypeHandle.Update(ref state);
        _unitLocalTransformTypeHandle.Update(ref state);

        using UnitRenderBudgetSnapshot.Snapshot snapshot = _snapshotSystem.Create(
            _queryContext.UnitQuery,
            _unitEntityTypeHandle,
            _unitLocalTransformTypeHandle,
            Allocator.TempJob);
        NativeArray<Entity> units = snapshot.Units.AsArray();
        NativeArray<LocalTransform> transforms = snapshot.Transforms.AsArray();
        using NativeHashSet<Entity> safetyTaggedThisFrame = new(math.max(1, units.Length * 3), Allocator.Temp);
        using NativeHashSet<Entity> readyTaggedThisFrame = new(math.max(1, units.Length * 3), Allocator.Temp);
        using NativeList<UnitDistance> distances = new(units.Length, Allocator.TempJob);
        using NativeList<Entity> entitiesToShow = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> entitiesToHide = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> unitsToShowDetailed = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> unitsToShowFarImpostor = new(MaxUpdatesPerFrame, Allocator.Temp);
        _distanceSystem.Collect(
            camera,
            units,
            transforms,
            distances,
            passengerLookup,
            _entityStorageInfoLookup,
            AlwaysDetailedDistanceSq,
            VisibleCharacterViewportPadding,
            VisibleCharacterEdgeSafetyMargin);

        if (distances.Length == 0)
        {
            renderStateEcb.Dispose();
            if (_diagnosticStateSystem.ShouldRunDiagnostics(Time.frameCount))
            {
                _diagnosticStateSystem.ScheduleNextDiagnostics(Time.frameCount);
                _diagnosticLogSystem.LogEmptyQueryState(
                    em,
                    Time.frameCount,
                    _queryContext,
                    units.Length,
                    runtimeGameplayState.PlayRequested != 0);
            }
            return;
        }

        _sortSystem.Sort(distances);
        using UnitRenderBudgetBand.Plan bandPlan = _bandSystem.Create(
            distances,
            MaxDetailedUnits,
            MaxMidLodUnits,
            MaxLowLodUnits,
            AlwaysDetailedDistanceSq,
            Allocator.TempJob);
        NativeHashSet<Entity> detailedUnits = bandPlan.DetailedUnits;
        NativeHashSet<Entity> midLodUnits = bandPlan.MidLodUnits;
        NativeHashSet<Entity> lowLodUnits = bandPlan.LowLodUnits;
        int detailedCount = bandPlan.DetailedCount;

        var decisionContext = new UnitRenderBudgetDecision.Context
        {
            RenderStateEcb = renderStateEcb,
            SafetyTaggedThisFrame = safetyTaggedThisFrame,
            ReadyTaggedThisFrame = readyTaggedThisFrame,
            ChildLookup = childLookup,
            AnimationIndexLookup = animationIndexLookup,
            MoveVisualLookup = moveVisualLookup,
            MovementBehaviorLookup = movementBehaviorLookup,
            SourcePrefabKeyLookup = sourcePrefabKeyLookup,
            FactionLookup = factionLookup,
            SelectedLookup = selectedLookup,
            VisualStateLookup = _visualStateLookup,
            CulledUnitLookup = culledUnitLookup,
            EntityStorageInfoLookup = _entityStorageInfoLookup,
            DisabledLookup = disabledLookup,
            DisableRenderingLookup = disableRenderingLookup,
            CulledTagLookup = culledTagLookup,
            Distances = distances,
            DetailedUnits = detailedUnits,
            MidLodUnits = midLodUnits,
            LowLodUnits = lowLodUnits,
            EntitiesToShow = entitiesToShow,
            EntitiesToHide = entitiesToHide,
            UnitsToShowDetailed = unitsToShowDetailed,
            UnitsToShowFarImpostor = unitsToShowFarImpostor,
            CameraMotionActive = cameraMotionActive,
            AlwaysDetailedDistanceSq = AlwaysDetailedDistanceSq,
            VisibleCharacterLowDistanceSq = VisibleCharacterLowDistanceSq,
            VisibleCharacterImpostorNearDistance = VisibleCharacterImpostorNearDistance,
            VisibleCharacterImpostorFarDistance = VisibleCharacterImpostorFarDistance,
            EnemyAlwaysDetailedDistanceSq = EnemyAlwaysDetailedDistanceSq,
            EnemyLowLodDistanceSq = EnemyLowLodDistanceSq,
            EnemyImpostorDistanceSq = EnemyImpostorDistanceSq,
            ClassificationSystem = _classificationSystem,
            CharacterPolicySystem = _characterPolicySystem,
            LodReferenceSystem = _lodReferenceSystem,
            LodReferenceLookups = _lodReferenceLookups,
            AnimationReadinessSystem = _animationReadinessSystem,
            AnimationReadinessLookups = _animationReadinessLookups,
            RenderableQuerySystem = _renderableQuerySystem,
            RenderableQueryLookups = _renderableQueryLookups,
            VisualStateSystem = _visualStateSystem,
            ReadinessSystem = _readinessSystem,
            ReadinessLookups = _readinessLookups,
            RenderSafetySystem = _renderSafetySystem,
            RenderSafetyLookups = _renderSafetyLookups,
            VisualPlanSystem = _visualPlanSystem,
            VisibilityChangeSystem = _visibilityChangeSystem,
            ImpostorTagSystem = _impostorTagSystem,
            CurrentFrame = Time.frameCount
        };
        UnitRenderBudgetDecision.Result decisionResult = _decisionSystem.Process(ref decisionContext);

        UnitRenderBudgetVisibilityApply.Result applyResult = _visibilityApplySystem.Apply(
            em,
            renderStateEcb,
            unitsToShowDetailed,
            unitsToShowFarImpostor,
            entitiesToShow,
            entitiesToHide,
            _visibilityApplyLookups);
        UnitRenderBudgetDiagnosticState.FrameCounters counters = _diagnosticStateSystem.CreateFrameCounters(decisionResult, applyResult);

        double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
        if (_diagnosticStateSystem.ShouldRunDiagnostics(Time.frameCount))
        {
            _diagnosticStateSystem.ScheduleNextDiagnostics(Time.frameCount);
            if (counters.Changed == 0 && counters.VisualStateChanges == 0 && counters.MissingMidInstance == 0 && counters.MissingLowInstance == 0)
            {
                _lightDiagnosticSystem.LogRenderBudgetStateLight(
                    em,
                    distances,
                    detailedCount,
                    cameraMotionActive,
                    AlwaysDetailedDistanceSq,
                    _classificationSystem,
                    ref _diagnosticLogSystem);
            }
            else
            {
                childLookup = SystemAPI.GetBufferLookup<Child>(true);
                _mismatchDiagnosticSystem.LogMidLodDiagnostics(
                    em,
                    distances,
                    detailedUnits,
                    midLodUnits,
                    lowLodUnits,
                    childLookup,
                    detailedCount,
                    cameraMotionActive,
                    _classificationSystem,
                    _lodReferenceSystem,
                    _animationReadinessSystem,
                    _renderableQuerySystem,
                    _readinessSystem,
                    _visualPlanSystem,
                    _diagnosticStateSystem,
                    VisibleCharacterLowDistanceSq,
                    VisibleCharacterImpostorNearDistance,
                    VisibleCharacterImpostorFarDistance,
                    ref _diagnosticLogSystem);
            }
        }

        _freezeDiagnosticSystem.LogFreezeIfNeeded(
            em,
            elapsed,
            distances,
            detailedCount,
            cameraMotionActive,
            counters,
            VisibleCharacterLowDistanceSq,
            VisibleCharacterImpostorNearDistance,
            VisibleCharacterImpostorFarDistance,
            ref _diagnosticLogSystem);

        bool budgetStable =
            !cameraMotionActive &&
            counters.Changed == 0 &&
            counters.Hidden == 0 &&
            counters.Shown == 0 &&
            counters.VisualStateChanges == 0 &&
            counters.VisualStatePending == 0 &&
            counters.VisualTransitionsCommitted == 0 &&
            counters.VisibleMidSafetyPatched == 0 &&
            counters.MissingMidInstance == 0 &&
            counters.MissingLowInstance == 0;
        _scheduleSystem.RecordBudgetStability(
            currentUnitCount,
            currentSelectedUnitCount,
            currentSelectedUnitHash,
            budgetStable);
    }

    private static int CalculateSelectedUnitHash(EntityQuery selectedUnitQuery, out int selectedUnitCount)
    {
        using NativeArray<Entity> selectedUnits = selectedUnitQuery.ToEntityArray(Allocator.Temp);
        selectedUnitCount = selectedUnits.Length;
        unchecked
        {
            int hash = selectedUnitCount;
            for (int i = 0; i < selectedUnits.Length; i++)
            {
                Entity entity = selectedUnits[i];
                hash = (hash * 397) ^ entity.Index;
                hash = (hash * 397) ^ entity.Version;
            }

            return hash;
        }
    }

}
