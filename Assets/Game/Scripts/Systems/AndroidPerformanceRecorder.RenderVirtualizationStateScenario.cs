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
        private const string Vrp095StateScenarioArgument =
            "-warlineVrp095StateScenario";
        private const int Vrp095CenteringFrames = 60;
        private const int Vrp095WaitFrames = 240;

        private enum Vrp095Phase : byte
        {
            Locate = 0,
            CenterVisible = 1,
            VerifyVisibleIntact = 2,
            AwaitVisibleDestroyed = 3,
            CenterRecycle = 4,
            VerifyRecycle = 5,
            AwaitOffCameraDestroyed = 6,
            CenterOffCamera = 7,
            VerifyOffCameraDestroyed = 8,
            ReturnVisible = 9,
            VerifyReturn = 10,
            Complete = 11,
            Failed = 12
        }

        private readonly struct Vrp095Candidate
        {
            internal readonly Entity Entity;
            internal readonly int StateOwnerIndex;
            internal readonly float3 Position;
            internal readonly uint IntactBucketMask;
            internal readonly uint DestroyedBucketMask;

            internal Vrp095Candidate(
                Entity entity,
                int stateOwnerIndex,
                float3 position,
                uint intactBucketMask,
                uint destroyedBucketMask)
            {
                Entity = entity;
                StateOwnerIndex = stateOwnerIndex;
                Position = position;
                IntactBucketMask = intactBucketMask;
                DestroyedBucketMask = destroyedBucketMask;
            }
        }

        private readonly struct Vrp095Snapshot
        {
            internal readonly int Count;
            internal readonly int IntactCount;
            internal readonly int DestroyedCount;
            internal readonly HashSet<int> Slots;

            internal Vrp095Snapshot(
                int count,
                int intactCount,
                int destroyedCount,
                HashSet<int> slots)
            {
                Count = count;
                IntactCount = intactCount;
                DestroyedCount = destroyedCount;
                Slots = slots;
            }
        }

        private bool _vrp095StateScenarioEnabled;
        private Vrp095Phase _vrp095Phase;
        private int _vrp095PhaseFrameCount;
        private Vrp095Candidate _vrp095Visible;
        private Vrp095Candidate _vrp095Recycle;
        private Vrp095Candidate _vrp095OffCamera;
        private HashSet<int> _vrp095VisibleIntactSlots;
        private HashSet<int> _vrp095VisibleDestroyedSlots;
        private HashSet<int> _vrp095RecycleSlots;
        private bool _vrp095OffCameraDestructionTriggered;
        private uint _vrp095InitialSequence;

        private void InitializeVrp095StateScenario(
            IReadOnlyList<string> commandLineArguments)
        {
            _vrp095StateScenarioEnabled = ContainsExactArgument(
                commandLineArguments,
                Vrp095StateScenarioArgument);
            _vrp095Phase = Vrp095Phase.Locate;
            _vrp095PhaseFrameCount = 0;
            _vrp095Visible = default;
            _vrp095Recycle = default;
            _vrp095OffCamera = default;
            _vrp095VisibleIntactSlots = null;
            _vrp095VisibleDestroyedSlots = null;
            _vrp095RecycleSlots = null;
            _vrp095OffCameraDestructionTriggered = false;
            _vrp095InitialSequence = 0u;
        }

        internal void SampleVrp095StateScenario(
            bool gameplayActive,
            Camera camera)
        {
            if (!_vrp095StateScenarioEnabled || !_matchReady ||
                !gameplayActive || !Application.isFocused ||
                _vrp095Phase == Vrp095Phase.Complete ||
                _vrp095Phase == Vrp095Phase.Failed)
            {
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            switch (_vrp095Phase)
            {
                case Vrp095Phase.Locate:
                    LocateVrp095Targets(entityManager);
                    break;
                case Vrp095Phase.CenterVisible:
                    CenterVrp095Camera(
                        camera,
                        _vrp095Visible,
                        Vrp095Phase.VerifyVisibleIntact);
                    break;
                case Vrp095Phase.VerifyVisibleIntact:
                    VerifyVrp095VisibleIntact(entityManager);
                    break;
                case Vrp095Phase.AwaitVisibleDestroyed:
                    AwaitVrp095VisibleDestroyed(entityManager);
                    break;
                case Vrp095Phase.CenterRecycle:
                    CenterVrp095Camera(
                        camera,
                        _vrp095Recycle,
                        Vrp095Phase.VerifyRecycle);
                    break;
                case Vrp095Phase.VerifyRecycle:
                    VerifyVrp095Recycle(entityManager);
                    break;
                case Vrp095Phase.AwaitOffCameraDestroyed:
                    AwaitVrp095OffCameraDestroyed(entityManager);
                    break;
                case Vrp095Phase.CenterOffCamera:
                    CenterVrp095Camera(
                        camera,
                        _vrp095OffCamera,
                        Vrp095Phase.VerifyOffCameraDestroyed);
                    break;
                case Vrp095Phase.VerifyOffCameraDestroyed:
                    VerifyVrp095OffCameraDestroyed(entityManager);
                    break;
                case Vrp095Phase.ReturnVisible:
                    CenterVrp095Camera(
                        camera,
                        _vrp095Visible,
                        Vrp095Phase.VerifyReturn);
                    break;
                case Vrp095Phase.VerifyReturn:
                    VerifyVrp095Return(entityManager);
                    break;
            }
        }

        private void LocateVrp095Targets(EntityManager entityManager)
        {
            if (!TryGetVrp095Database(
                    entityManager,
                    out OperationMapRenderDatabaseComponent database))
            {
                return;
            }

            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
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
            using NativeArray<
                OperationMapVirtualizedBuildingPresentationComponent>
                presentations = query.ToComponentDataArray<
                    OperationMapVirtualizedBuildingPresentationComponent>(
                    Allocator.Temp);
            using NativeArray<UnitHealth> health =
                query.ToComponentDataArray<UnitHealth>(Allocator.Temp);
            using NativeArray<LocalTransform> transforms =
                query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            var candidates = new List<Vrp095Candidate>(entities.Length);
            for (int i = 0; i < entities.Length; i++)
            {
                if (health[i].Current <= 0 ||
                    entityManager.IsComponentEnabled<
                        OperationMapBuildingDestroyedComponent>(entities[i]))
                {
                    continue;
                }

                int stateOwner = presentations[i].StateOwnerIndex;
                ResolveVrp095BucketMasks(
                    ref blob,
                    stateOwner,
                    out uint intactMask,
                    out uint destroyedMask);
                if (intactMask == 0u || destroyedMask == 0u)
                    continue;

                candidates.Add(new Vrp095Candidate(
                    entities[i],
                    stateOwner,
                    transforms[i].Position,
                    intactMask,
                    destroyedMask));
            }

            candidates.Sort((left, right) =>
                left.StateOwnerIndex.CompareTo(right.StateOwnerIndex));
            if (!TrySelectVrp095Targets(
                    candidates,
                    out _vrp095Visible,
                    out _vrp095Recycle,
                    out _vrp095OffCamera))
            {
                FailVrp095("three distant compatible intact buildings unavailable");
                return;
            }

            _vrp095InitialSequence =
                ReadVrp067StateChangeVersion(entityManager);
            _vrp095Phase = Vrp095Phase.CenterVisible;
            _vrp095PhaseFrameCount = 0;
            LogNoStackTrace(
                "[VRP-095 StateScenario] phase=Located " +
                $"visible={_vrp095Visible.StateOwnerIndex} " +
                $"recycle={_vrp095Recycle.StateOwnerIndex} " +
                $"offCamera={_vrp095OffCamera.StateOwnerIndex} " +
                $"initialSequence={_vrp095InitialSequence}");
        }

        private static bool TrySelectVrp095Targets(
            IReadOnlyList<Vrp095Candidate> candidates,
            out Vrp095Candidate visible,
            out Vrp095Candidate recycle,
            out Vrp095Candidate offCamera)
        {
            visible = default;
            recycle = default;
            offCamera = default;
            if (candidates == null || candidates.Count < 3)
                return false;

            visible = candidates[0];
            float farthest = 0f;
            for (int i = 1; i < candidates.Count; i++)
            {
                Vrp095Candidate candidate = candidates[i];
                if (!Vrp095BucketsMatch(visible, candidate))
                    continue;

                float distance = math.distancesq(
                    visible.Position.xz,
                    candidate.Position.xz);
                if (distance > farthest)
                {
                    farthest = distance;
                    recycle = candidate;
                }
            }

            if (recycle.Entity == Entity.Null)
                return false;

            float greatestMinimum = 0f;
            for (int i = 1; i < candidates.Count; i++)
            {
                Vrp095Candidate candidate = candidates[i];
                if (candidate.Entity == recycle.Entity ||
                    !Vrp095BucketsMatch(visible, candidate))
                {
                    continue;
                }

                float minimum = math.min(
                    math.distancesq(visible.Position.xz, candidate.Position.xz),
                    math.distancesq(recycle.Position.xz, candidate.Position.xz));
                if (minimum > greatestMinimum)
                {
                    greatestMinimum = minimum;
                    offCamera = candidate;
                }
            }

            const float minimumDistanceSquared = 150f * 150f;
            return offCamera.Entity != Entity.Null &&
                   farthest >= minimumDistanceSquared &&
                   greatestMinimum >= minimumDistanceSquared;
        }

        private static bool Vrp095BucketsMatch(
            in Vrp095Candidate left,
            in Vrp095Candidate right)
        {
            return left.IntactBucketMask == right.IntactBucketMask &&
                   left.DestroyedBucketMask == right.DestroyedBucketMask;
        }

        private static void ResolveVrp095BucketMasks(
            ref OperationMapRenderDatabaseBlob blob,
            int stateOwnerIndex,
            out uint intactMask,
            out uint destroyedMask)
        {
            intactMask = 0u;
            destroyedMask = 0u;
            for (int placementIndex = 0;
                 placementIndex < blob.Placements.Length;
                 placementIndex++)
            {
                ref OperationMapRenderPlacementBlob placement =
                    ref blob.Placements[placementIndex];
                if (placement.StateOwnerIndex != stateOwnerIndex ||
                    placement.PrototypeIndex < 0 ||
                    placement.PrototypeIndex >= blob.Prototypes.Length)
                {
                    continue;
                }

                ref OperationMapRenderPrototypeBlob prototype =
                    ref blob.Prototypes[placement.PrototypeIndex];
                for (int partOffset = 0;
                     partOffset < prototype.PartCount;
                     partOffset++)
                {
                    int partIndex = prototype.FirstPart + partOffset;
                    int bucket = blob.Parts[partIndex].PoolBucketIndex;
                    if (bucket < 0 || bucket >= 32)
                        continue;
                    if (placement.RequiredVisualState ==
                        OperationMapRenderVisualState.Intact)
                    {
                        intactMask |= 1u << bucket;
                    }
                    else if (placement.RequiredVisualState ==
                             OperationMapRenderVisualState.Destroyed)
                    {
                        destroyedMask |= 1u << bucket;
                    }
                }
            }
        }

        private void CenterVrp095Camera(
            Camera camera,
            in Vrp095Candidate candidate,
            Vrp095Phase nextPhase)
        {
            if (camera == null)
            {
                FailVrp095("world camera is unavailable");
                return;
            }

            Ray centerRay = camera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            if (Mathf.Abs(centerRay.direction.y) < 0.0001f)
            {
                FailVrp095("world camera does not intersect the ground");
                return;
            }

            float distance = -centerRay.origin.y / centerRay.direction.y;
            Vector3 groundCenter = centerRay.origin +
                                   centerRay.direction * distance;
            Vector3 target = new(candidate.Position.x, 0f, candidate.Position.z);
            Vector3 delta = target - groundCenter;
            camera.transform.position += new Vector3(delta.x, 0f, delta.z);

            _vrp095PhaseFrameCount++;
            if (_vrp095PhaseFrameCount < Vrp095CenteringFrames)
                return;

            Vector3 viewport = camera.WorldToViewportPoint(candidate.Position);
            if (viewport.z <= 0f || viewport.x < 0.2f || viewport.x > 0.8f ||
                viewport.y < 0.2f || viewport.y > 0.8f)
            {
                FailVrp095("target is outside the central viewport");
                return;
            }

            _vrp095Phase = nextPhase;
            _vrp095PhaseFrameCount = 0;
        }

        private void VerifyVrp095VisibleIntact(EntityManager entityManager)
        {
            if (!TryAwaitVrp095Snapshot(
                    entityManager,
                    _vrp095Visible.StateOwnerIndex,
                    OperationMapRenderVisualState.Intact,
                    out Vrp095Snapshot snapshot))
            {
                return;
            }

            _vrp095VisibleIntactSlots = snapshot.Slots;
            TriggerVrp095Destruction(entityManager, _vrp095Visible.Entity);
            _vrp095Phase = Vrp095Phase.AwaitVisibleDestroyed;
            _vrp095PhaseFrameCount = 0;
            LogVrp095Phase("VisibleIntact", _vrp095Visible, snapshot);
        }

        private void AwaitVrp095VisibleDestroyed(EntityManager entityManager)
        {
            if (!RequireVrp095Destroyed(
                    entityManager,
                    _vrp095Visible,
                    _vrp095InitialSequence + 1u))
            {
                return;
            }

            if (!TryAwaitVrp095Snapshot(
                    entityManager,
                    _vrp095Visible.StateOwnerIndex,
                    OperationMapRenderVisualState.Destroyed,
                    out Vrp095Snapshot snapshot))
            {
                return;
            }

            _vrp095VisibleDestroyedSlots = snapshot.Slots;
            _vrp095Phase = Vrp095Phase.CenterRecycle;
            _vrp095PhaseFrameCount = 0;
            LogVrp095Phase("VisibleDestroyed", _vrp095Visible, snapshot);
        }

        private void VerifyVrp095Recycle(EntityManager entityManager)
        {
            int travelAnchorStateOwner = _vrp095Recycle.StateOwnerIndex;
            bool travelAnchorIntact = TryReadVrp095Snapshot(
                                          entityManager,
                                          _vrp095Recycle.StateOwnerIndex,
                                          out Vrp095Snapshot snapshot) &&
                                      IsVrp095SnapshotState(
                                          snapshot,
                                          OperationMapRenderVisualState.Intact);

            int releasedSlotOverlap = travelAnchorIntact
                ? CountVrp095Overlap(
                    _vrp095VisibleIntactSlots,
                    snapshot.Slots)
                : 0;
            if (!travelAnchorIntact)
            {
                if (!TryResolveVrp095RecycledBuilding(
                        entityManager,
                        _vrp095VisibleIntactSlots,
                        _vrp095Visible.StateOwnerIndex,
                        out _vrp095Recycle,
                        out snapshot,
                        out releasedSlotOverlap))
                {
                    _vrp095PhaseFrameCount++;
                    if (_vrp095PhaseFrameCount > Vrp095WaitFrames)
                    {
                        FailVrp095(
                            "active intact replacement building did not materialize");
                    }

                    return;
                }
            }

            _vrp095PhaseFrameCount = 0;

            if (!TryReadVrp095Snapshot(
                    entityManager,
                    _vrp095OffCamera.StateOwnerIndex,
                    out Vrp095Snapshot offCamera) ||
                offCamera.Count != 0)
            {
                FailVrp095("off-camera target retained proxy slots");
                return;
            }

            _vrp095RecycleSlots = snapshot.Slots;
            _vrp095OffCameraDestructionTriggered = false;
            _vrp095Phase = Vrp095Phase.AwaitOffCameraDestroyed;
            _vrp095PhaseFrameCount = 0;
            LogNoStackTrace(
                "[VRP-095 StateScenario] phase=RecycleIntact " +
                $"stateOwner={_vrp095Recycle.StateOwnerIndex} " +
                $"travelAnchor={travelAnchorStateOwner} " +
                $"releasedSlotOverlap={releasedSlotOverlap} " +
                $"slots={snapshot.Count} intact={snapshot.IntactCount} " +
                $"destroyed={snapshot.DestroyedCount}");
        }

        private static bool TryResolveVrp095RecycledBuilding(
            EntityManager entityManager,
            IReadOnlyCollection<int> releasedSlots,
            int excludedStateOwnerIndex,
            out Vrp095Candidate candidate,
            out Vrp095Snapshot snapshot,
            out int recycledSlotCount)
        {
            candidate = default;
            snapshot = default;
            recycledSlotCount = 0;
            if (releasedSlots == null || releasedSlots.Count == 0 ||
                !TryGetVrp095Database(
                    entityManager,
                    out OperationMapRenderDatabaseComponent database))
            {
                return false;
            }

            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            HashSet<int> releasedSlotSet = releasedSlots as HashSet<int> ??
                                            new HashSet<int>(releasedSlots);
            var overlapByStateOwner = new Dictionary<int, int>();
            using (EntityQuery slotQuery = entityManager.CreateEntityQuery(
                       ComponentType.ReadOnly<
                           OperationMapRenderProxySlotComponent>()))
            using (NativeArray<OperationMapRenderProxySlotComponent> slots =
                   slotQuery.ToComponentDataArray<
                       OperationMapRenderProxySlotComponent>(Allocator.Temp))
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    OperationMapRenderProxySlotComponent slot = slots[i];
                    if (!releasedSlotSet.Contains(slot.SlotIndex) ||
                        slot.PlacementIndex < 0 ||
                        slot.PlacementIndex >= blob.Placements.Length)
                    {
                        continue;
                    }

                    ref OperationMapRenderPlacementBlob placement =
                        ref blob.Placements[slot.PlacementIndex];
                    int stateOwner = placement.StateOwnerIndex;
                    if (stateOwner < 0 ||
                        stateOwner == excludedStateOwnerIndex ||
                        placement.RequiredVisualState !=
                        OperationMapRenderVisualState.Intact)
                    {
                        continue;
                    }

                    overlapByStateOwner.TryGetValue(
                        stateOwner,
                        out int count);
                    overlapByStateOwner[stateOwner] = count + 1;
                }
            }

            if (overlapByStateOwner.Count == 0)
                return false;

            using EntityQuery buildingQuery = entityManager.CreateEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<
                            OperationMapVirtualizedBuildingPresentationComponent>(),
                        ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                        ComponentType.ReadOnly<UnitHealth>(),
                        ComponentType.ReadOnly<LocalTransform>(),
                        ComponentType.ReadOnly<
                            OperationMapBuildingDestroyedComponent>()
                    },
                    Options = EntityQueryOptions.IgnoreComponentEnabledState
                });
            using NativeArray<Entity> entities =
                buildingQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<
                OperationMapVirtualizedBuildingPresentationComponent>
                presentations = buildingQuery.ToComponentDataArray<
                    OperationMapVirtualizedBuildingPresentationComponent>(
                    Allocator.Temp);
            using NativeArray<UnitHealth> health =
                buildingQuery.ToComponentDataArray<UnitHealth>(Allocator.Temp);
            using NativeArray<LocalTransform> transforms =
                buildingQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                int stateOwner = presentations[i].StateOwnerIndex;
                if (!overlapByStateOwner.TryGetValue(
                        stateOwner,
                        out int overlap) ||
                    health[i].Current <= 0 ||
                    entityManager.IsComponentEnabled<
                        OperationMapBuildingDestroyedComponent>(entities[i]))
                {
                    continue;
                }

                if (overlap < recycledSlotCount ||
                    (overlap == recycledSlotCount &&
                     candidate.Entity != Entity.Null &&
                     stateOwner >= candidate.StateOwnerIndex))
                {
                    continue;
                }

                ResolveVrp095BucketMasks(
                    ref blob,
                    stateOwner,
                    out uint intactMask,
                    out uint destroyedMask);
                if (intactMask == 0u || destroyedMask == 0u)
                    continue;

                candidate = new Vrp095Candidate(
                    entities[i],
                    stateOwner,
                    transforms[i].Position,
                    intactMask,
                    destroyedMask);
                recycledSlotCount = overlap;
            }

            return candidate.Entity != Entity.Null &&
                   TryReadVrp095Snapshot(
                       entityManager,
                       candidate.StateOwnerIndex,
                       out snapshot) &&
                   IsVrp095SnapshotState(
                       snapshot,
                       OperationMapRenderVisualState.Intact) &&
                   CountVrp095Overlap(releasedSlots, snapshot.Slots) ==
                   recycledSlotCount;
        }

        private void AwaitVrp095OffCameraDestroyed(EntityManager entityManager)
        {
            if (!_vrp095OffCameraDestructionTriggered)
            {
                if (!TryReadVrp095Snapshot(
                        entityManager,
                        _vrp095Recycle.StateOwnerIndex,
                        out Vrp095Snapshot stableRecycle) ||
                    !IsVrp095SnapshotState(
                        stableRecycle,
                        OperationMapRenderVisualState.Intact))
                {
                    _vrp095Phase = Vrp095Phase.VerifyRecycle;
                    _vrp095PhaseFrameCount = 0;
                    return;
                }

                if (!_vrp095RecycleSlots.SetEquals(stableRecycle.Slots))
                {
                    _vrp095RecycleSlots = stableRecycle.Slots;
                    _vrp095PhaseFrameCount = 0;
                    return;
                }

                if (!TryReadVrp095Snapshot(
                        entityManager,
                        _vrp095OffCamera.StateOwnerIndex,
                        out Vrp095Snapshot offCameraBeforeDestruction) ||
                    offCameraBeforeDestruction.Count != 0)
                {
                    FailVrp095("off-camera target retained proxy slots");
                    return;
                }

                _vrp095PhaseFrameCount++;
                if (_vrp095PhaseFrameCount < Vrp095CenteringFrames)
                    return;

                TriggerVrp095Destruction(
                    entityManager,
                    _vrp095OffCamera.Entity);
                _vrp095OffCameraDestructionTriggered = true;
                _vrp095PhaseFrameCount = 0;
                LogNoStackTrace(
                    "[VRP-095 StateScenario] phase=RecycleStable " +
                    $"stateOwner={_vrp095Recycle.StateOwnerIndex} " +
                    $"slots={_vrp095RecycleSlots.Count}");
                return;
            }

            if (!RequireVrp095Destroyed(
                    entityManager,
                    _vrp095OffCamera,
                    _vrp095InitialSequence + 2u))
            {
                return;
            }

            if (!TryReadVrp095Snapshot(
                    entityManager,
                    _vrp095OffCamera.StateOwnerIndex,
                    out Vrp095Snapshot offCamera) ||
                offCamera.Count != 0)
            {
                FailVrp095("off-camera destruction materialized proxy slots");
                return;
            }

            if (!TryReadVrp095Snapshot(
                    entityManager,
                    _vrp095Recycle.StateOwnerIndex,
                    out Vrp095Snapshot recycle) ||
                !IsVrp095SnapshotState(
                    recycle,
                    OperationMapRenderVisualState.Intact))
            {
                FailVrp095("off-camera destruction disturbed visible owner");
                return;
            }

            _vrp095Phase = Vrp095Phase.CenterOffCamera;
            _vrp095PhaseFrameCount = 0;
            LogNoStackTrace(
                "[VRP-095 StateScenario] phase=OffCameraCanonical " +
                $"stateOwner={_vrp095OffCamera.StateOwnerIndex} " +
                $"sequence={ReadVrp067StateChangeVersion(entityManager)}");
        }

        private void VerifyVrp095OffCameraDestroyed(EntityManager entityManager)
        {
            if (!TryAwaitVrp095Snapshot(
                    entityManager,
                    _vrp095OffCamera.StateOwnerIndex,
                    OperationMapRenderVisualState.Destroyed,
                    out Vrp095Snapshot snapshot))
            {
                return;
            }

            _vrp095Phase = Vrp095Phase.ReturnVisible;
            _vrp095PhaseFrameCount = 0;
            LogVrp095Phase("OffCameraMaterialized", _vrp095OffCamera, snapshot);
        }

        private void VerifyVrp095Return(EntityManager entityManager)
        {
            if (!TryAwaitVrp095Snapshot(
                    entityManager,
                    _vrp095Visible.StateOwnerIndex,
                    OperationMapRenderVisualState.Destroyed,
                    out Vrp095Snapshot snapshot))
            {
                return;
            }

            if (snapshot.Count != _vrp095VisibleDestroyedSlots.Count)
            {
                FailVrp095("destroyed owner returned with an incomplete recipe");
                return;
            }

            if (!TryReadVrp095Metrics(entityManager, out string metrics))
            {
                _vrp095PhaseFrameCount++;
                if (_vrp095PhaseFrameCount > Vrp095WaitFrames)
                    FailVrp095("zero-overflow terminal metrics did not arrive");
                return;
            }

            _vrp095Phase = Vrp095Phase.Complete;
            LogNoStackTrace(
                "[VRP-095 StateScenario] result=Passed " +
                $"visible={_vrp095Visible.StateOwnerIndex} " +
                $"recycle={_vrp095Recycle.StateOwnerIndex} " +
                $"offCamera={_vrp095OffCamera.StateOwnerIndex} " +
                $"visibleIntactSlots={_vrp095VisibleIntactSlots.Count} " +
                $"visibleDestroyedSlots={_vrp095VisibleDestroyedSlots.Count} " +
                $"recycleSlots={_vrp095RecycleSlots.Count} " + metrics);
        }

        private bool TryAwaitVrp095Snapshot(
            EntityManager entityManager,
            int stateOwnerIndex,
            OperationMapRenderVisualState requiredState,
            out Vrp095Snapshot snapshot)
        {
            if (TryReadVrp095Snapshot(entityManager, stateOwnerIndex, out snapshot) &&
                IsVrp095SnapshotState(snapshot, requiredState))
            {
                _vrp095PhaseFrameCount = 0;
                return true;
            }

            _vrp095PhaseFrameCount++;
            if (_vrp095PhaseFrameCount > Vrp095WaitFrames)
                FailVrp095("expected materialized state did not arrive");
            return false;
        }

        private static bool IsVrp095SnapshotState(
            in Vrp095Snapshot snapshot,
            OperationMapRenderVisualState state)
        {
            return snapshot.Count > 0 &&
                   (state == OperationMapRenderVisualState.Intact
                       ? snapshot.IntactCount == snapshot.Count &&
                         snapshot.DestroyedCount == 0
                       : snapshot.DestroyedCount == snapshot.Count &&
                         snapshot.IntactCount == 0);
        }

        private static bool TryReadVrp095Snapshot(
            EntityManager entityManager,
            int stateOwnerIndex,
            out Vrp095Snapshot snapshot)
        {
            snapshot = default;
            if (!TryGetVrp095Database(
                    entityManager,
                    out OperationMapRenderDatabaseComponent database))
            {
                return false;
            }

            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>());
            using NativeArray<OperationMapRenderProxySlotComponent> slots =
                query.ToComponentDataArray<OperationMapRenderProxySlotComponent>(
                    Allocator.Temp);
            var assignedSlots = new HashSet<int>();
            int intactCount = 0;
            int destroyedCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                OperationMapRenderProxySlotComponent slot = slots[i];
                if (slot.PlacementIndex < 0 ||
                    slot.PlacementIndex >= blob.Placements.Length)
                {
                    continue;
                }

                ref OperationMapRenderPlacementBlob placement =
                    ref blob.Placements[slot.PlacementIndex];
                if (placement.StateOwnerIndex != stateOwnerIndex)
                    continue;

                assignedSlots.Add(slot.SlotIndex);
                if (placement.RequiredVisualState ==
                    OperationMapRenderVisualState.Intact)
                {
                    intactCount++;
                }
                else if (placement.RequiredVisualState ==
                         OperationMapRenderVisualState.Destroyed)
                {
                    destroyedCount++;
                }
            }

            snapshot = new Vrp095Snapshot(
                assignedSlots.Count,
                intactCount,
                destroyedCount,
                assignedSlots);
            return true;
        }

        private static bool TryGetVrp095Database(
            EntityManager entityManager,
            out OperationMapRenderDatabaseComponent database)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>());
            if (query.CalculateEntityCount() != 1)
            {
                database = default;
                return false;
            }

            database = query.GetSingleton<OperationMapRenderDatabaseComponent>();
            return database.Blob.IsCreated;
        }

        private bool RequireVrp095Destroyed(
            EntityManager entityManager,
            in Vrp095Candidate candidate,
            uint requiredSequence)
        {
            if (!entityManager.Exists(candidate.Entity))
            {
                FailVrp095("target building no longer exists");
                return false;
            }

            if (entityManager.IsComponentEnabled<
                    OperationMapBuildingDestroyedComponent>(candidate.Entity) &&
                ReadVrp095CanonicalState(entityManager, candidate.StateOwnerIndex) ==
                OperationMapRenderVisualState.Destroyed &&
                ReadVrp067StateChangeVersion(entityManager) >= requiredSequence)
            {
                _vrp095PhaseFrameCount = 0;
                return true;
            }

            _vrp095PhaseFrameCount++;
            if (_vrp095PhaseFrameCount > Vrp095WaitFrames)
                FailVrp095("canonical destroyed state did not arrive");
            return false;
        }

        private static OperationMapRenderVisualState ReadVrp095CanonicalState(
            EntityManager entityManager,
            int stateOwnerIndex)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderCanonicalStateComponent>());
            if (query.CalculateEntityCount() != 1)
                return OperationMapRenderVisualState.Intact;
            DynamicBuffer<OperationMapRenderCanonicalStateComponent> states =
                query.GetSingletonBuffer<
                    OperationMapRenderCanonicalStateComponent>(true);
            return stateOwnerIndex >= 0 && stateOwnerIndex < states.Length
                ? states[stateOwnerIndex].VisualState
                : OperationMapRenderVisualState.Intact;
        }

        private static void TriggerVrp095Destruction(
            EntityManager entityManager,
            Entity entity)
        {
            UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
            health.Current = 0;
            entityManager.SetComponentData(entity, health);
        }

        private static int CountVrp095Overlap(
            IReadOnlyCollection<int> left,
            IReadOnlyCollection<int> right)
        {
            if (left == null || right == null)
                return 0;
            IReadOnlyCollection<int> smaller = left.Count <= right.Count
                ? left
                : right;
            IReadOnlyCollection<int> larger = left.Count <= right.Count
                ? right
                : left;
            HashSet<int> largerSet = larger as HashSet<int> ??
                                     new HashSet<int>(larger);
            int overlap = 0;
            foreach (int value in smaller)
            {
                if (largerSet.Contains(value))
                    overlap++;
            }

            return overlap;
        }

        private static bool TryReadVrp095Metrics(
            EntityManager entityManager,
            out string text)
        {
            ReadVrp067VirtualizationMetrics(
                entityManager,
                out int enabledSlots,
                out int activeCells,
                out int activePlacements,
                out int overflow,
                out int deficit,
                out int2 envelopeMin,
                out int2 envelopeMax);
            text = $"slots={enabledSlots} activeCells={activeCells} " +
                   $"activePlacements={activePlacements} overflow={overflow} " +
                   $"deficit={deficit} envelope={envelopeMin.x},{envelopeMin.y}:" +
                   $"{envelopeMax.x},{envelopeMax.y}";
            return enabledSlots > 0 && activeCells > 0 &&
                   activePlacements > 0 && overflow == 0 && deficit == 0;
        }

        private static void LogVrp095Phase(
            string phase,
            in Vrp095Candidate candidate,
            in Vrp095Snapshot snapshot)
        {
            LogNoStackTrace(
                $"[VRP-095 StateScenario] phase={phase} " +
                $"stateOwner={candidate.StateOwnerIndex} " +
                $"slots={snapshot.Count} intact={snapshot.IntactCount} " +
                $"destroyed={snapshot.DestroyedCount}");
        }

        private void FailVrp095(string reason)
        {
            Vrp095Phase failedPhase = _vrp095Phase;
            _vrp095Phase = Vrp095Phase.Failed;
            LogNoStackTrace(
                "[VRP-095 StateScenario] result=Failed " +
                $"phase={failedPhase} reason={reason}");
        }
    }
}
