using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        [Serializable] private sealed class NativeRenderingDiagnostic
        {
            public string stage = "OpeningPackedRenderingFailureV3";
            public int frame, databaseCount, activeMapCount, cameraSnapshotCount;
            public bool openingPreviouslyAccepted;
            public NativeRenderingDatabaseDiagnostic[] databases;
            public NativeRenderingCameraDiagnostic[] cameraSnapshots;
        }

        [Serializable] private sealed class NativeRenderingDatabaseDiagnostic
        {
            public int entityIndex, entityVersion, schemaVersion, mapGeneration;
            public string contentHash;
            public bool blobCreated, hasReadiness, hasState, hasMetrics, hasSync;
            public bool initialized, firstViewApplied, rebuilt, positiveActiveSlots;
            public bool stateNoOverflow, metricsNoOverflow, enabledMatchesActive, sufficientCapacity;
            public int commandsLength = -1, canonicalStatesLength = -1;
            public uint publishedCommandVersion;
            public NativeRenderingSelectorDiagnostic selector;
            public NativeRenderingScalar[] readiness;
            public NativeRenderingScalar[] state;
            public NativeRenderingScalar[] metrics;
            public NativeRenderingScalar[] sync;
        }

        [Serializable] private sealed class NativeRenderingCameraDiagnostic { public NativeRenderingScalar[] scalars; }

        public static void RunNativeRenderingDiagnosticSerializationValidation()
        {
            var example = new NativeRenderingDiagnostic
            {
                databaseCount = 1,
                databases = new[] { new NativeRenderingDatabaseDiagnostic
                {
                    state = RenderingScalars(new OperationMapRenderVirtualizationStateComponent { Initialized = 1, InitialViewApplied = 1, RebuildCount = 2, ActiveSlotCount = 129, OverflowCount = 17 }),
                    metrics = RenderingScalars(new OperationMapRenderVirtualizationMetricsComponent { Capacity = 7403, EnabledSlotCount = 129, OverflowCount = 17 }),
                    readiness = RenderingScalars(new OperationMapRenderPackedReadinessComponent { ProxySlotCount = 7403 }),
                    selector = new NativeRenderingSelectorDiagnostic { captured = true, selectionFailure = "None", scheduledCameraVersion = 258,
                        guardEnvelope = RenderingScalars(new Game.Rendering.OperationMapRenderCellEnvelope { Min = new Unity.Mathematics.int2(4, 6), Max = new Unity.Mathematics.int2(39, 24) }),
                        assignmentResult = new[] { new NativeRenderingScalar { path = "OverflowCount", kind = "System.Int32", value = "17" } },
                        buckets = new[] { new NativeRenderingBucketDiagnostic { index = 4, capacity = 9, selectedRequiredRows = 26, deficit = 17 } } }
                } },
                cameraSnapshots = new[] { new NativeRenderingCameraDiagnostic { scalars = RenderingScalars(new RuntimeCameraSnapshotComponent
                { IsValid = 1, PublicationVersion = 5, Position = new Unity.Mathematics.float3(123, 61, 41), Projection = Unity.Mathematics.float4x4.identity }) } }
            };
            string json = JsonUtility.ToJson(example);
            var copy = JsonUtility.FromJson<NativeRenderingDiagnostic>(json);
            if (copy.databases == null || copy.databases.Length != 1 || copy.cameraSnapshots == null || copy.cameraSnapshots.Length != 1)
                throw new InvalidOperationException("Explicit diagnostic DTO arrays lost during JSON roundtrip");
            var row = copy.databases[0];
            if (RenderingScalarValue(row.state, "ActiveSlotCount") != "129" || RenderingScalarValue(row.state, "OverflowCount") != "17" ||
                RenderingScalarValue(row.metrics, "OverflowCount") != "17" || RenderingScalarValue(row.readiness, "ProxySlotCount") != "7403" ||
                RenderingScalarValue(copy.cameraSnapshots[0].scalars, "Position.y") != "61" ||
                RenderingScalarValue(copy.cameraSnapshots[0].scalars, "Projection.c0.x") != "1" ||
                row.selector == null || !row.selector.captured || row.selector.selectionFailure != "None" ||
                row.selector.buckets.Length != 1 || row.selector.buckets[0].deficit != 17 ||
                row.selector.scheduledCameraVersion != 258 || RenderingScalarValue(row.selector.guardEnvelope, "Min.x") != "4" ||
                RenderingScalarValue(row.selector.guardEnvelope, "Max.y") != "24" ||
                RenderingScalarValue(row.selector.assignmentResult, "OverflowCount") != "17")
                throw new InvalidOperationException("Actual scalar values lost from explicit diagnostic JSON");
            Debug.Log("[SkirmishNativeRenderingDiagnosticSerialization] result=Passed syntheticDtoRoundtrip=1 nativeGameplayCertified=0");
        }
        private static string RenderingScalarValue(NativeRenderingScalar[] rows, string path)
        {
            if (rows != null) foreach (var row in rows) if (row.path == path) return row.value;
            return null;
        }
        [Serializable] private sealed class NativeRenderingScalar
        {
            public string path, kind, value;
        }
        [Serializable] private sealed class NativeRenderingSelectorDiagnostic
        {
            public bool captured;
            public string error = "", selectionFailure = "";
            public uint scheduledCameraVersion, scheduledStateRevision, scheduledCommandVersion;
            public int assignmentGeneration;
            public byte hasGuardEnvelope;
            public bool currentEnvelopeRecomputed;
            public NativeRenderingScalar[] guardEnvelope, currentRequiredEnvelope, currentMaterializedEnvelope;
            public NativeRenderingScalar[] assignmentResult;
            public int selectedCells, selectedPlacements, selectedLogicalRows;
            public NativeRenderingBucketDiagnostic[] buckets;
        }
        [Serializable] private sealed class NativeRenderingBucketDiagnostic
        {
            public int index, firstSlot, capacity, peakRequiredCount, headroomCount, selectedRequiredRows, deficit;
            public string policy;
        }
        private static NativeRenderingScalar[] RenderingScalars(object value)
        {
            var rows = new System.Collections.Generic.List<NativeRenderingScalar>();
            FlattenRenderingScalars(value, "", rows);
            return rows.ToArray();
        }
        private static void FlattenRenderingScalars(object value, string path, System.Collections.Generic.List<NativeRenderingScalar> rows)
        {
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string)
            {
                string text = value is float f ? f.ToString("R", System.Globalization.CultureInfo.InvariantCulture) :
                    value is double d ? d.ToString("R", System.Globalization.CultureInfo.InvariantCulture) :
                    Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                rows.Add(new NativeRenderingScalar { path = path, kind = type.FullName, value = text });
                return;
            }
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            foreach (var field in fields)
                FlattenRenderingScalars(field.GetValue(value), path.Length == 0 ? field.Name : path + "." + field.Name, rows);
        }
        private static object RenderingField(object value, string name)
        {
            var field = value.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(value.GetType().FullName, name);
            return field.GetValue(value);
        }
        private static object RenderingProperty(object value, string name)
        {
            var property = value.GetType().GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (property == null) throw new MissingMemberException(value.GetType().FullName, name);
            return property.GetValue(value);
        }
        // Synchronize outstanding native jobs, then copy existing values only. No system update,
        // schedule, native allocation, command/buffer/component write, or Dispose is invoked.
        private static NativeRenderingSelectorDiagnostic CaptureRenderingSelector(EntityManager em, Entity databaseEntity)
        {
            var result = new NativeRenderingSelectorDiagnostic();
            try
            {
                em.CompleteAllTrackedJobs();
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated || !world.EntityManager.Equals(em))
                    throw new InvalidOperationException("Exact owning default world unavailable");
                var handle = world.Unmanaged.GetExistingUnmanagedSystem<Game.Rendering.OperationMapRenderVirtualizationInitializationSystem>();
                if (handle.Equals(default(SystemHandle))) throw new InvalidOperationException("Existing packed initialization system unavailable");
                object system = world.Unmanaged.GetUnsafeSystemRef<Game.Rendering.OperationMapRenderVirtualizationInitializationSystem>(handle);
                if (!((Entity)RenderingField(system, "_initializedDatabaseEntity")).Equals(databaseEntity))
                    throw new InvalidOperationException("Packed system diagnostic database ownership differs");
                result.scheduledCameraVersion = (uint)RenderingField(system, "_scheduledCameraVersion");
                result.scheduledStateRevision = (uint)RenderingField(system, "_scheduledStateRevision");
                result.scheduledCommandVersion = (uint)RenderingField(system, "_commandVersion");
                result.assignmentGeneration = (int)RenderingField(system, "_assignmentGeneration");
                result.hasGuardEnvelope = (byte)RenderingField(system, "_hasGuardEnvelope");
                result.guardEnvelope = RenderingScalars(RenderingField(system, "_guardEnvelope"));
                using (var cameraQuery = em.CreateEntityQuery(typeof(RuntimeCameraSnapshotComponent)))
                {
                    if (cameraQuery.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique actual camera required for envelope diagnostic");
                    var actualCamera = cameraQuery.GetSingleton<RuntimeCameraSnapshotComponent>();
                    var actualDatabase = em.GetComponentData<OperationMapRenderDatabaseComponent>(databaseEntity);
                    var method = typeof(Game.Rendering.OperationMapRenderVirtualizationInitializationSystem).GetMethod("TryBuildCameraEnvelopes", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (method == null) throw new MissingMethodException("Actual camera-envelope query unavailable");
                    object[] args = { actualDatabase.Blob, actualCamera, default(Game.Rendering.OperationMapRenderCellEnvelope), default(Game.Rendering.OperationMapRenderCellEnvelope) };
                    result.currentEnvelopeRecomputed = (bool)method.Invoke(null, args);
                    if (!result.currentEnvelopeRecomputed) throw new InvalidOperationException("Actual camera-envelope diagnostic failed");
                    result.currentRequiredEnvelope = RenderingScalars(args[2]);
                    result.currentMaterializedEnvelope = RenderingScalars(args[3]);
                }
                object native = RenderingField(system, "_nativeState");
                object selection = RenderingField(native, "_selectionFailure");
                object assignment = RenderingField(native, "_assignmentResult");
                if (!(bool)RenderingProperty(selection, "IsCreated") || !(bool)RenderingProperty(assignment, "IsCreated"))
                    throw new InvalidOperationException("Existing native diagnostic references not created");
                result.selectionFailure = RenderingProperty(selection, "Value").ToString();
                result.assignmentResult = RenderingScalars(RenderingProperty(assignment, "Value"));
                result.selectedCells = (int)RenderingProperty(RenderingField(native, "_selectedCells"), "Length");
                result.selectedPlacements = (int)RenderingProperty(RenderingField(native, "_selectedPlacements"), "Length");
                object logicalRows = RenderingField(native, "_selectedLogicalRows");
                result.selectedLogicalRows = (int)RenderingProperty(logicalRows, "Length");
                var database = em.GetComponentData<OperationMapRenderDatabaseComponent>(databaseEntity);
                if (!database.Blob.IsCreated) throw new InvalidOperationException("Exact database blob unavailable");
                ref var blob = ref database.Blob.Value;
                result.buckets = new NativeRenderingBucketDiagnostic[blob.PoolBuckets.Length];
                for (int i = 0; i < result.buckets.Length; i++)
                {
                    var bucket = blob.PoolBuckets[i];
                    result.buckets[i] = new NativeRenderingBucketDiagnostic { index = i, firstSlot = bucket.FirstSlot,
                        capacity = bucket.Capacity, peakRequiredCount = bucket.PeakRequiredCount,
                        headroomCount = bucket.HeadroomCount, policy = bucket.PolicyBucket.ToString() };
                }
                var item = logicalRows.GetType().GetProperty("Item");
                if (item == null) throw new MissingMemberException("Native selected-row indexer unavailable");
                for (int i = 0; i < result.selectedLogicalRows; i++)
                {
                    object row = item.GetValue(logicalRows, new object[] { i });
                    int bucket = (int)RenderingField(row, "PoolBucketIndex");
                    if (bucket < 0 || bucket >= result.buckets.Length) throw new InvalidOperationException("Selected native row has invalid bucket");
                    result.buckets[bucket].selectedRequiredRows++;
                }
                foreach (var bucket in result.buckets) bucket.deficit = Math.Max(0, bucket.selectedRequiredRows - bucket.capacity);
                result.captured = true;
            }
            catch (Exception error) { result.error = error.ToString(); }
            return result;
        }

        // Read-only diagnostic. No readiness predicate, ordering or retry policy changes.
        private static string CaptureNativeRenderingFailureDiagnostic(EntityManager em)
        {
            using var databases = em.CreateEntityQuery(typeof(OperationMapRenderDatabaseComponent));
            using var activeMaps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent));
            using var cameras = em.CreateEntityQuery(typeof(RuntimeCameraSnapshotComponent));
            using var entities = databases.ToEntityArray(Allocator.Temp);
            using var cameraStates = cameras.ToComponentDataArray<RuntimeCameraSnapshotComponent>(Allocator.Temp);
            var report = new NativeRenderingDiagnostic
            {
                frame = Time.frameCount,
                databaseCount = entities.Length,
                activeMapCount = activeMaps.CalculateEntityCount(),
                cameraSnapshotCount = cameraStates.Length,
                openingPreviouslyAccepted = nativeOpeningRenderViewAccepted,
                databases = new NativeRenderingDatabaseDiagnostic[entities.Length],
                cameraSnapshots = new NativeRenderingCameraDiagnostic[cameraStates.Length]
            };
            for (int i = 0; i < cameraStates.Length; i++) report.cameraSnapshots[i] = new NativeRenderingCameraDiagnostic { scalars = RenderingScalars(cameraStates[i]) };
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var database = em.GetComponentData<OperationMapRenderDatabaseComponent>(entity);
                var row = new NativeRenderingDatabaseDiagnostic
                {
                    entityIndex = entity.Index, entityVersion = entity.Version,
                    schemaVersion = database.SchemaVersion, mapGeneration = database.MapGeneration,
                    contentHash = database.ContentHash.ToString(), blobCreated = database.Blob.IsCreated,
                    hasReadiness = em.HasComponent<OperationMapRenderPackedReadinessComponent>(entity),
                    hasState = em.HasComponent<OperationMapRenderVirtualizationStateComponent>(entity),
                    hasMetrics = em.HasComponent<OperationMapRenderVirtualizationMetricsComponent>(entity),
                    hasSync = em.HasComponent<OperationMapRenderStateSyncStateComponent>(entity)
                };
                if (row.hasReadiness) row.readiness = RenderingScalars(em.GetComponentData<OperationMapRenderPackedReadinessComponent>(entity));
                if (row.hasState)
                {
                    var state = em.GetComponentData<OperationMapRenderVirtualizationStateComponent>(entity);
                    row.state = RenderingScalars(state);
                    row.initialized = state.Initialized != 0;
                    row.firstViewApplied = state.InitialViewApplied != 0;
                    row.rebuilt = state.RebuildCount > 0;
                    row.positiveActiveSlots = state.ActiveSlotCount > 0;
                    row.stateNoOverflow = state.OverflowCount == 0;
                }
                if (row.hasMetrics)
                {
                    var metrics = em.GetComponentData<OperationMapRenderVirtualizationMetricsComponent>(entity);
                    row.metrics = RenderingScalars(metrics);
                    row.metricsNoOverflow = metrics.OverflowCount == 0;
                    row.enabledMatchesActive = row.hasState && metrics.EnabledSlotCount == em.GetComponentData<OperationMapRenderVirtualizationStateComponent>(entity).ActiveSlotCount;
                    row.sufficientCapacity = row.hasState && metrics.Capacity >= em.GetComponentData<OperationMapRenderVirtualizationStateComponent>(entity).ActiveSlotCount;
                }
                if (row.hasSync) row.sync = RenderingScalars(em.GetComponentData<OperationMapRenderStateSyncStateComponent>(entity));
                if (em.HasBuffer<OperationMapRenderSlotCommandComponent>(entity)) row.commandsLength = em.GetBuffer<OperationMapRenderSlotCommandComponent>(entity, true).Length;
                if (em.HasBuffer<OperationMapRenderCanonicalStateComponent>(entity)) row.canonicalStatesLength = em.GetBuffer<OperationMapRenderCanonicalStateComponent>(entity, true).Length;
                if (em.HasComponent<OperationMapRenderSlotCommandStateComponent>(entity)) row.publishedCommandVersion = em.GetComponentData<OperationMapRenderSlotCommandStateComponent>(entity).Version;
                row.selector = CaptureRenderingSelector(em, entity);
                report.databases[i] = row;
            }
            return JsonUtility.ToJson(report);
        }
    }
}
