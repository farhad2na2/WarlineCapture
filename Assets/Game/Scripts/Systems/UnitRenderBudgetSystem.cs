using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using SnivelerCode.GpuAnimation.Scripts.Components;
using UnitDistance = UnitRenderBudgetDistanceSystem.UnitDistance;

[UpdateAfter(typeof(UnitMassRenderSettingsSystem))]
public partial struct UnitRenderBudgetSystem : ISystem
{
    private const int MaxDetailedUnits = 12;
    private const int MaxMidLodUnits = 36;
    private const int MaxLowLodUnits = 48;
    private const int MaxUpdatesPerFrame = 4096;
    private const int UpdateIntervalFrames = 10;
    private const float AlwaysDetailedDistanceSq = 18f * 18f;
    private const float VisibleCharacterLowDistanceSq = 32f * 32f;
    private const float VisibleCharacterImpostorNearDistance = 48f;
    private const float VisibleCharacterImpostorFarDistance = 48f;
    private const float EnemyAlwaysDetailedDistanceSq = 14f * 14f;
    private const float EnemyLowLodDistanceSq = 20f * 20f;
    private const float EnemyImpostorDistanceSq = 28f * 28f;
    private const float VisibleCharacterViewportPadding = 0.35f;
    private const float VisibleCharacterEdgeSafetyMargin = 0.18f;

    private UnitRenderBudgetQuerySystem _querySystem;
    private UnitRenderBudgetQuerySystem.Context _queryContext;
    private UnitRenderBudgetScheduleSystem _scheduleSystem;
    private UnitRenderBudgetCameraMotionSystem _cameraMotionSystem;
    private UnitRenderBudgetSnapshotSystem _snapshotSystem;
    private UnitRenderBudgetDistanceSystem _distanceSystem;
    private UnitRenderBudgetSortSystem _sortSystem;
    private UnitRenderBudgetBandSystem _bandSystem;
    private UnitRenderBudgetClassificationSystem _classificationSystem;
    private UnitRenderBudgetCharacterPolicySystem _characterPolicySystem;
    private UnitRenderBudgetLodReferenceSystem _lodReferenceSystem;
    private UnitRenderBudgetAnimationReadinessSystem _animationReadinessSystem;
    private UnitRenderBudgetRenderableQuerySystem _renderableQuerySystem;
    private UnitRenderBudgetVisualStateSystem _visualStateSystem;
    private UnitRenderBudgetReadinessSystem _readinessSystem;
    private UnitRenderBudgetRenderSafetySystem _renderSafetySystem;
    private UnitRenderBudgetVisualPlanSystem _visualPlanSystem;
    private UnitRenderBudgetDecisionSystem _decisionSystem;
    private UnitRenderBudgetVisibilityChangeSystem _visibilityChangeSystem;
    private UnitRenderBudgetImpostorTagSystem _impostorTagSystem;
    private UnitRenderBudgetVisibilityApplySystem _visibilityApplySystem;
    private UnitRenderBudgetDiagnosticStateSystem _diagnosticStateSystem;
    private UnitRenderBudgetDiagnosticLogSystem _diagnosticLogSystem;
    private UnitRenderBudgetLightDiagnosticSystem _lightDiagnosticSystem;
    private UnitRenderBudgetMismatchDiagnosticSystem _mismatchDiagnosticSystem;
    private UnitRenderBudgetFreezeDiagnosticSystem _freezeDiagnosticSystem;

    public void OnCreate(ref SystemState state)
    {
        _queryContext = _querySystem.Create(ref state);
        state.RequireForUpdate<RuntimeGameplayStateComponent>();

    }

    public void OnUpdate(ref SystemState state)
    {
        RuntimeGameplayStateComponent runtimeGameplayState = SystemAPI.GetSingleton<RuntimeGameplayStateComponent>();
        if (runtimeGameplayState.PlayRequested == 0)
            return;

        if (!RuntimeCameraReferenceSystem.TryGetWorldCamera(state.EntityManager, _queryContext.CameraReferenceQuery, out Camera camera))
            return;

        bool cameraMotionActive = _cameraMotionSystem.IsCameraMotionActive(camera, ref _scheduleSystem, ref _diagnosticStateSystem, Time.frameCount);
        int currentUnitCount = _queryContext.UnitQuery.CalculateEntityCount();
        if (_scheduleSystem.ShouldSkipStableBudget(cameraMotionActive, currentUnitCount))
            return;

        if (_scheduleSystem.ShouldSkipUpdateFrame(cameraMotionActive, Time.frameCount))
            return;

        _scheduleSystem.ScheduleNextUpdate(cameraMotionActive, Time.frameCount, UpdateIntervalFrames);
        double startTime = Time.realtimeSinceStartupAsDouble;
        EntityManager em = state.EntityManager;
        var renderStateEcb = new EntityCommandBuffer(Allocator.Temp);
        var childLookup = SystemAPI.GetBufferLookup<Child>(true);
        var animationIndexLookup = SystemAPI.GetComponentLookup<MaterialAnimationIndex>(true);
        var moveVisualLookup = SystemAPI.GetComponentLookup<UnitMoveVisualState>(true);

        using UnitRenderBudgetSnapshotSystem.Snapshot snapshot = _snapshotSystem.Create(_queryContext.UnitQuery, Allocator.Temp);
        NativeArray<Entity> units = snapshot.Units;
        NativeArray<LocalTransform> transforms = snapshot.Transforms;
        using NativeHashSet<Entity> safetyTaggedThisFrame = new(math.max(1, units.Length * 3), Allocator.Temp);
        using NativeHashSet<Entity> readyTaggedThisFrame = new(math.max(1, units.Length * 3), Allocator.Temp);
        using NativeList<UnitDistance> distances = new(units.Length, Allocator.Temp);
        using NativeList<Entity> entitiesToShow = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> entitiesToHide = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> unitsToShowDetailed = new(MaxUpdatesPerFrame, Allocator.Temp);
        using NativeList<Entity> unitsToShowFarImpostor = new(MaxUpdatesPerFrame, Allocator.Temp);
        _distanceSystem.Collect(
            em,
            camera,
            units,
            transforms,
            distances,
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
        using UnitRenderBudgetBandSystem.Plan bandPlan = _bandSystem.Create(
            distances,
            MaxDetailedUnits,
            MaxMidLodUnits,
            MaxLowLodUnits,
            AlwaysDetailedDistanceSq,
            Allocator.Temp);
        NativeHashSet<Entity> detailedUnits = bandPlan.DetailedUnits;
        NativeHashSet<Entity> midLodUnits = bandPlan.MidLodUnits;
        NativeHashSet<Entity> lowLodUnits = bandPlan.LowLodUnits;
        int detailedCount = bandPlan.DetailedCount;

        var decisionContext = new UnitRenderBudgetDecisionSystem.Context
        {
            Em = em,
            RenderStateEcb = renderStateEcb,
            SafetyTaggedThisFrame = safetyTaggedThisFrame,
            ReadyTaggedThisFrame = readyTaggedThisFrame,
            ChildLookup = childLookup,
            AnimationIndexLookup = animationIndexLookup,
            MoveVisualLookup = moveVisualLookup,
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
            AnimationReadinessSystem = _animationReadinessSystem,
            RenderableQuerySystem = _renderableQuerySystem,
            VisualStateSystem = _visualStateSystem,
            ReadinessSystem = _readinessSystem,
            RenderSafetySystem = _renderSafetySystem,
            VisualPlanSystem = _visualPlanSystem,
            VisibilityChangeSystem = _visibilityChangeSystem,
            ImpostorTagSystem = _impostorTagSystem
        };
        UnitRenderBudgetDecisionSystem.Result decisionResult = _decisionSystem.Process(ref decisionContext);

        UnitRenderBudgetVisibilityApplySystem.Result applyResult = _visibilityApplySystem.Apply(
            em,
            renderStateEcb,
            unitsToShowDetailed,
            unitsToShowFarImpostor,
            entitiesToShow,
            entitiesToHide);
        UnitRenderBudgetDiagnosticStateSystem.FrameCounters counters = _diagnosticStateSystem.CreateFrameCounters(decisionResult, applyResult);

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
        _scheduleSystem.RecordBudgetStability(currentUnitCount, budgetStable);
    }

}
