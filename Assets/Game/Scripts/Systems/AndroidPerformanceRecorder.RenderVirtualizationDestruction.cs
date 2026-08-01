using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class AndroidPerformanceRecorder
    {
        private const string Vrp067StateOwnerArgument =
            "-warlineVrp067StateOwner";
        private const string Vrp067FamilyArgument =
            "-warlineVrp067Family";
        private const int Vrp067CenteringFrames = 60;
        private const int Vrp067BaselineFrames = 120;
        private const int Vrp067PostTransitionFrames = 120;
        private const double Vrp067IntactHoldSeconds = 5d;

        private enum Vrp067Phase : byte
        {
            Locate = 0,
            Center = 1,
            HoldIntact = 2,
            Baseline = 3,
            AwaitDestroyed = 4,
            PostTransition = 5,
            Complete = 6,
            Failed = 7
        }

        private bool _vrp067DestructionMatrixEnabled;
        private int _vrp067StateOwnerIndex;
        private string _vrp067Family;
        private Vrp067Phase _vrp067Phase;
        private Entity _vrp067TargetEntity;
        private string _vrp067StableId;
        private float3 _vrp067TargetPosition;
        private int _vrp067PhaseFrameCount;
        private double _vrp067IntactReadySeconds;
        private uint _vrp067InitialStateChangeVersion;
        private float _vrp067TransitionFrameMs;
        private float[] _vrp067BaselineFrameTimes;
        private float[] _vrp067PostTransitionFrameTimes;

        internal void SampleVrp067DestructionMatrix(
            bool gameplayActive,
            Camera camera)
        {
            if (!_vrp067DestructionMatrixEnabled || !_matchReady ||
                !gameplayActive || !Application.isFocused ||
                _vrp067Phase == Vrp067Phase.Complete ||
                _vrp067Phase == Vrp067Phase.Failed)
            {
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            switch (_vrp067Phase)
            {
                case Vrp067Phase.Locate:
                    LocateVrp067Target(entityManager);
                    break;
                case Vrp067Phase.Center:
                    CenterVrp067Camera(camera);
                    break;
                case Vrp067Phase.HoldIntact:
                    HoldVrp067Intact();
                    break;
                case Vrp067Phase.Baseline:
                    SampleVrp067Baseline(entityManager);
                    break;
                case Vrp067Phase.AwaitDestroyed:
                    AwaitVrp067Destroyed(entityManager);
                    break;
                case Vrp067Phase.PostTransition:
                    SampleVrp067PostTransition(entityManager, camera);
                    break;
            }
        }

        private void InitializeVrp067DestructionMatrix(
            IReadOnlyList<string> commandLineArguments)
        {
            _vrp067DestructionMatrixEnabled =
                TryResolveVrp067Configuration(
                    commandLineArguments,
                    out _vrp067StateOwnerIndex,
                    out _vrp067Family);
            _vrp067Phase = Vrp067Phase.Locate;
            _vrp067TargetEntity = Entity.Null;
            _vrp067StableId = string.Empty;
            _vrp067TargetPosition = float3.zero;
            _vrp067PhaseFrameCount = 0;
            _vrp067IntactReadySeconds = 0d;
            _vrp067InitialStateChangeVersion = 0u;
            _vrp067TransitionFrameMs = 0f;
            _vrp067BaselineFrameTimes = _vrp067DestructionMatrixEnabled
                ? new float[Vrp067BaselineFrames]
                : null;
            _vrp067PostTransitionFrameTimes =
                _vrp067DestructionMatrixEnabled
                    ? new float[Vrp067PostTransitionFrames]
                    : null;
        }

        private static bool TryResolveVrp067Configuration(
            IReadOnlyList<string> arguments,
            out int stateOwnerIndex,
            out string family)
        {
            stateOwnerIndex = -1;
            family = string.Empty;
            if (!TryGetArgumentValue(
                    arguments,
                    Vrp067StateOwnerArgument,
                    out string stateOwnerText) ||
                !int.TryParse(stateOwnerText, out stateOwnerIndex) ||
                stateOwnerIndex < 0 ||
                !TryGetArgumentValue(
                    arguments,
                    Vrp067FamilyArgument,
                    out string familyText))
            {
                stateOwnerIndex = -1;
                return false;
            }

            if (string.Equals(
                    familyText,
                    "House",
                    StringComparison.OrdinalIgnoreCase))
            {
                family = "House";
                return true;
            }

            if (string.Equals(
                    familyText,
                    "Shop",
                    StringComparison.OrdinalIgnoreCase))
            {
                family = "Shop";
                return true;
            }

            stateOwnerIndex = -1;
            return false;
        }

        private void LocateVrp067Target(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<
                            OperationMapVirtualizedBuildingPresentationComponent>(),
                        ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                        ComponentType.ReadWrite<UnitHealth>(),
                        ComponentType.ReadOnly<LocalTransform>(),
                        ComponentType.ReadOnly<
                            OperationMapBuildingDestroyedComponent>()
                    },
                    Options = EntityQueryOptions.IgnoreComponentEnabledState
                });
            using NativeArray<Entity> entities =
                query.ToEntityArray(Allocator.Temp);
            using NativeArray<OperationMapVirtualizedBuildingPresentationComponent>
                presentations = query.ToComponentDataArray<
                    OperationMapVirtualizedBuildingPresentationComponent>(
                    Allocator.Temp);

            int matchIndex = -1;
            for (int i = 0; i < presentations.Length; i++)
            {
                if (presentations[i].StateOwnerIndex !=
                    _vrp067StateOwnerIndex)
                {
                    continue;
                }

                if (matchIndex >= 0)
                {
                    FailVrp067("state owner resolves to multiple buildings");
                    return;
                }

                matchIndex = i;
            }

            if (matchIndex < 0)
                return;

            Entity target = entities[matchIndex];
            UnitHealth health = entityManager.GetComponentData<UnitHealth>(target);
            OperationMapBuildingComponent building =
                entityManager.GetComponentData<OperationMapBuildingComponent>(
                    target);
            if (entityManager.IsComponentEnabled<
                    OperationMapBuildingDestroyedComponent>(target) ||
                health.Current <= 0 ||
                building.BlockerPolicy !=
                OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked)
            {
                FailVrp067("target building is not intact and destructible");
                return;
            }

            _vrp067TargetEntity = target;
            _vrp067StableId = building.StableId.ToString();
            _vrp067TargetPosition = entityManager
                .GetComponentData<LocalTransform>(target).Position;
            _vrp067InitialStateChangeVersion =
                ReadVrp067StateChangeVersion(entityManager);
            _vrp067Phase = Vrp067Phase.Center;
            _vrp067PhaseFrameCount = 0;
            LogNoStackTrace(
                "[VRP-067 DestructionMatrix] phase=Located " +
                $"family={_vrp067Family} " +
                $"stateOwner={_vrp067StateOwnerIndex} " +
                $"stableId={_vrp067StableId} " +
                $"position={_vrp067TargetPosition.x:F3}," +
                $"{_vrp067TargetPosition.y:F3}," +
                $"{_vrp067TargetPosition.z:F3} " +
                $"initialSequence={_vrp067InitialStateChangeVersion}");
        }

        private void CenterVrp067Camera(Camera camera)
        {
            if (camera == null)
            {
                FailVrp067("world camera is unavailable");
                return;
            }

            Ray centerRay = camera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            if (Mathf.Abs(centerRay.direction.y) < 0.0001f)
            {
                FailVrp067("world camera does not intersect the ground");
                return;
            }

            float distance = -centerRay.origin.y / centerRay.direction.y;
            Vector3 groundCenter = centerRay.origin +
                                   centerRay.direction * distance;
            Vector3 target = new(
                _vrp067TargetPosition.x,
                0f,
                _vrp067TargetPosition.z);
            Vector3 delta = target - groundCenter;
            camera.transform.position += new Vector3(delta.x, 0f, delta.z);

            _vrp067PhaseFrameCount++;
            if (_vrp067PhaseFrameCount < Vrp067CenteringFrames)
                return;

            Vector3 viewport = camera.WorldToViewportPoint(
                new Vector3(
                    _vrp067TargetPosition.x,
                    _vrp067TargetPosition.y,
                    _vrp067TargetPosition.z));
            if (viewport.z <= 0f || viewport.x < 0.2f ||
                viewport.x > 0.8f || viewport.y < 0.2f ||
                viewport.y > 0.8f)
            {
                FailVrp067(
                    "target building is outside the central capture viewport");
                return;
            }

            _vrp067Phase = Vrp067Phase.HoldIntact;
            _vrp067PhaseFrameCount = 0;
            _vrp067IntactReadySeconds =
                Time.realtimeSinceStartupAsDouble;
            LogNoStackTrace(
                "[VRP-067 DestructionMatrix] phase=IntactReady " +
                $"family={_vrp067Family} " +
                $"stateOwner={_vrp067StateOwnerIndex} " +
                $"viewport={viewport.x:F3},{viewport.y:F3}," +
                $"{viewport.z:F3}");
        }

        private void HoldVrp067Intact()
        {
            if (Time.realtimeSinceStartupAsDouble -
                _vrp067IntactReadySeconds < Vrp067IntactHoldSeconds)
            {
                return;
            }

            _vrp067Phase = Vrp067Phase.Baseline;
            _vrp067PhaseFrameCount = 0;
        }

        private void SampleVrp067Baseline(EntityManager entityManager)
        {
            if (!RequireVrp067Target(entityManager))
                return;

            _vrp067BaselineFrameTimes[_vrp067PhaseFrameCount++] =
                Time.unscaledDeltaTime * 1000f;
            if (_vrp067PhaseFrameCount < Vrp067BaselineFrames)
                return;

            UnitHealth health = entityManager.GetComponentData<UnitHealth>(
                _vrp067TargetEntity);
            health.Current = 0;
            entityManager.SetComponentData(_vrp067TargetEntity, health);
            _vrp067Phase = Vrp067Phase.AwaitDestroyed;
            _vrp067PhaseFrameCount = 0;
            LogNoStackTrace(
                "[VRP-067 DestructionMatrix] phase=Triggered " +
                $"family={_vrp067Family} " +
                $"stateOwner={_vrp067StateOwnerIndex} " +
                $"baselineP95Ms={Percentile(_vrp067BaselineFrameTimes, Vrp067BaselineFrames, 95d):F3} " +
                $"baselineMaxMs={Vrp067Maximum(_vrp067BaselineFrameTimes, Vrp067BaselineFrames):F3}");
        }

        private void AwaitVrp067Destroyed(EntityManager entityManager)
        {
            if (!RequireVrp067Target(entityManager))
                return;

            _vrp067PhaseFrameCount++;
            if (!entityManager.IsComponentEnabled<
                    OperationMapBuildingDestroyedComponent>(
                    _vrp067TargetEntity))
            {
                if (_vrp067PhaseFrameCount > 120)
                    FailVrp067("canonical destroyed state did not arrive");
                return;
            }

            _vrp067TransitionFrameMs =
                Time.unscaledDeltaTime * 1000f;
            _vrp067Phase = Vrp067Phase.PostTransition;
            _vrp067PhaseFrameCount = 0;
            uint sequence = ReadVrp067StateChangeVersion(entityManager);
            if (sequence <= _vrp067InitialStateChangeVersion)
            {
                FailVrp067("state-change sequence did not advance");
                return;
            }

            LogNoStackTrace(
                "[VRP-067 DestructionMatrix] phase=DestroyedReady " +
                $"family={_vrp067Family} " +
                $"stateOwner={_vrp067StateOwnerIndex} " +
                $"transitionFrameMs={_vrp067TransitionFrameMs:F3} " +
                $"sequence={sequence}");
        }

        private void SampleVrp067PostTransition(
            EntityManager entityManager,
            Camera camera)
        {
            if (!RequireVrp067Target(entityManager))
                return;

            if (!entityManager.IsComponentEnabled<
                    OperationMapBuildingDestroyedComponent>(
                    _vrp067TargetEntity))
            {
                FailVrp067("destroyed state did not remain enabled");
                return;
            }

            _vrp067PostTransitionFrameTimes[_vrp067PhaseFrameCount++] =
                Time.unscaledDeltaTime * 1000f;
            if (_vrp067PhaseFrameCount < Vrp067PostTransitionFrames)
                return;

            ReadVrp067VirtualizationMetrics(
                entityManager,
                out int enabledSlots,
                out int activeCells,
                out int activePlacements,
                out int overflow,
                out int deficit);
            Vector3 viewport = camera != null
                ? camera.WorldToViewportPoint(
                    new Vector3(
                        _vrp067TargetPosition.x,
                        _vrp067TargetPosition.y,
                        _vrp067TargetPosition.z))
                : Vector3.zero;
            uint finalSequence =
                ReadVrp067StateChangeVersion(entityManager);
            bool passed = enabledSlots > 0 && activeCells > 0 &&
                          activePlacements > 0 && overflow == 0 &&
                          deficit == 0 &&
                          finalSequence > _vrp067InitialStateChangeVersion &&
                          viewport.z > 0f && viewport.x >= 0f &&
                          viewport.x <= 1f && viewport.y >= 0f &&
                          viewport.y <= 1f;
            _vrp067Phase = passed
                ? Vrp067Phase.Complete
                : Vrp067Phase.Failed;
            LogNoStackTrace(
                "[VRP-067 DestructionMatrix] " +
                $"result={(passed ? "Passed" : "Failed")} " +
                $"family={_vrp067Family} " +
                $"stateOwner={_vrp067StateOwnerIndex} " +
                $"stableId={_vrp067StableId} " +
                $"baselineP95Ms={Percentile(_vrp067BaselineFrameTimes, Vrp067BaselineFrames, 95d):F3} " +
                $"baselineMaxMs={Vrp067Maximum(_vrp067BaselineFrameTimes, Vrp067BaselineFrames):F3} " +
                $"transitionFrameMs={_vrp067TransitionFrameMs:F3} " +
                $"postP95Ms={Percentile(_vrp067PostTransitionFrameTimes, Vrp067PostTransitionFrames, 95d):F3} " +
                $"postMaxMs={Vrp067Maximum(_vrp067PostTransitionFrameTimes, Vrp067PostTransitionFrames):F3} " +
                $"slots={enabledSlots} activeCells={activeCells} " +
                $"activePlacements={activePlacements} overflow={overflow} " +
                $"deficit={deficit} sequence={finalSequence} " +
                $"viewport={viewport.x:F3},{viewport.y:F3},{viewport.z:F3}");
        }

        private bool RequireVrp067Target(EntityManager entityManager)
        {
            if (_vrp067TargetEntity != Entity.Null &&
                entityManager.Exists(_vrp067TargetEntity))
            {
                return true;
            }

            FailVrp067("target building no longer exists");
            return false;
        }

        private static uint ReadVrp067StateChangeVersion(
            EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<
                    OperationMapRenderStateChangeSequenceComponent>());
            return query.CalculateEntityCount() == 1
                ? query.GetSingleton<
                    OperationMapRenderStateChangeSequenceComponent>()
                    .LastPublishedVersion
                : 0u;
        }

        private static void ReadVrp067VirtualizationMetrics(
            EntityManager entityManager,
            out int enabledSlots,
            out int activeCells,
            out int activePlacements,
            out int overflow,
            out int deficit)
        {
            enabledSlots = 0;
            activeCells = 0;
            activePlacements = 0;
            overflow = int.MaxValue;
            deficit = int.MaxValue;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<
                    OperationMapRenderVirtualizationMetricsComponent>());
            if (query.CalculateEntityCount() != 1)
                return;

            OperationMapRenderVirtualizationMetricsComponent metrics =
                query.GetSingleton<
                    OperationMapRenderVirtualizationMetricsComponent>();
            enabledSlots = metrics.EnabledSlotCount;
            activeCells = metrics.ActiveCellCount;
            activePlacements = metrics.ActivePlacementCount;
            overflow = metrics.OverflowCount;
            deficit = metrics.HighestDeficit;
        }

        private static float Vrp067Maximum(float[] values, int count)
        {
            float maximum = 0f;
            for (int i = 0; i < count; i++)
                maximum = Mathf.Max(maximum, values[i]);
            return maximum;
        }

        private void FailVrp067(string reason)
        {
            _vrp067Phase = Vrp067Phase.Failed;
            LogNoStackTrace(
                "[VRP-067 DestructionMatrix] result=Failed " +
                $"family={_vrp067Family} " +
                $"stateOwner={_vrp067StateOwnerIndex} " +
                $"reason={reason}");
        }
    }
}
