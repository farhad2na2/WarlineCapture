using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public sealed class SelectionRuntimeDiagnosticsSystemHelper
    {
        public static readonly bool EnableSelectionClickDiagnostics = false;
        public static readonly bool EnableMoveCommandTrace = false;
        public static readonly bool EnableScanCommandTrace = false;

        private const string SelectionClickPrefix = "[SelectionClick]";
        private const string MoveCommandTracePrefix = "[MoveCommandTrace]";
        private const string ScanCommandTracePrefix = "[ScanCommandTrace]";

#if UNITY_EDITOR
        public enum EditorSelectionAllocationProbePhase
        {
            Total = 0,
            CommandFlush = 1,
            Input = 2,
            FocusedReadModel = 3,
            Panel = 4,
            TacticalCamera = 5,
            MarkerPreview = 6,
            Camera = 7
        }

        public readonly struct EditorSelectionAllocationProbeSnapshot
        {
            public readonly long TotalBytes;
            public readonly int TotalAllocationSamples;
            public readonly int TotalUpdateSamples;
            public readonly long CommandFlushBytes;
            public readonly int CommandFlushAllocationSamples;
            public readonly int CommandFlushUpdateSamples;
            public readonly long InputBytes;
            public readonly int InputAllocationSamples;
            public readonly int InputUpdateSamples;
            public readonly long FocusedReadModelBytes;
            public readonly int FocusedReadModelAllocationSamples;
            public readonly int FocusedReadModelUpdateSamples;
            public readonly long PanelBytes;
            public readonly int PanelAllocationSamples;
            public readonly int PanelUpdateSamples;
            public readonly long TacticalCameraBytes;
            public readonly int TacticalCameraAllocationSamples;
            public readonly int TacticalCameraUpdateSamples;
            public readonly long MarkerPreviewBytes;
            public readonly int MarkerPreviewAllocationSamples;
            public readonly int MarkerPreviewUpdateSamples;
            public readonly long CameraBytes;
            public readonly int CameraAllocationSamples;
            public readonly int CameraUpdateSamples;

            private EditorSelectionAllocationProbeSnapshot(
                EditorSelectionAllocationProbeCounter total,
                EditorSelectionAllocationProbeCounter commandFlush,
                EditorSelectionAllocationProbeCounter input,
                EditorSelectionAllocationProbeCounter focusedReadModel,
                EditorSelectionAllocationProbeCounter panel,
                EditorSelectionAllocationProbeCounter tacticalCamera,
                EditorSelectionAllocationProbeCounter markerPreview,
                EditorSelectionAllocationProbeCounter camera)
            {
                TotalBytes = total.Bytes;
                TotalAllocationSamples = total.AllocationSamples;
                TotalUpdateSamples = total.UpdateSamples;
                CommandFlushBytes = commandFlush.Bytes;
                CommandFlushAllocationSamples = commandFlush.AllocationSamples;
                CommandFlushUpdateSamples = commandFlush.UpdateSamples;
                InputBytes = input.Bytes;
                InputAllocationSamples = input.AllocationSamples;
                InputUpdateSamples = input.UpdateSamples;
                FocusedReadModelBytes = focusedReadModel.Bytes;
                FocusedReadModelAllocationSamples = focusedReadModel.AllocationSamples;
                FocusedReadModelUpdateSamples = focusedReadModel.UpdateSamples;
                PanelBytes = panel.Bytes;
                PanelAllocationSamples = panel.AllocationSamples;
                PanelUpdateSamples = panel.UpdateSamples;
                TacticalCameraBytes = tacticalCamera.Bytes;
                TacticalCameraAllocationSamples = tacticalCamera.AllocationSamples;
                TacticalCameraUpdateSamples = tacticalCamera.UpdateSamples;
                MarkerPreviewBytes = markerPreview.Bytes;
                MarkerPreviewAllocationSamples = markerPreview.AllocationSamples;
                MarkerPreviewUpdateSamples = markerPreview.UpdateSamples;
                CameraBytes = camera.Bytes;
                CameraAllocationSamples = camera.AllocationSamples;
                CameraUpdateSamples = camera.UpdateSamples;
            }

            public static EditorSelectionAllocationProbeSnapshot Create(
                EditorSelectionAllocationProbeCounter total,
                EditorSelectionAllocationProbeCounter commandFlush,
                EditorSelectionAllocationProbeCounter input,
                EditorSelectionAllocationProbeCounter focusedReadModel,
                EditorSelectionAllocationProbeCounter panel,
                EditorSelectionAllocationProbeCounter tacticalCamera,
                EditorSelectionAllocationProbeCounter markerPreview,
                EditorSelectionAllocationProbeCounter camera)
            {
                return new EditorSelectionAllocationProbeSnapshot(
                    total,
                    commandFlush,
                    input,
                    focusedReadModel,
                    panel,
                    tacticalCamera,
                    markerPreview,
                    camera);
            }
        }

        public struct EditorSelectionAllocationProbeCounter
        {
            public long Bytes;
            public int AllocationSamples;
            public int UpdateSamples;

            public void Add(long allocatedBytes)
            {
                UpdateSamples++;
                if (allocatedBytes <= 0)
                    return;

                Bytes += allocatedBytes;
                AllocationSamples++;
            }
        }

        public readonly struct EditorSelectionAllocationProbeScope : System.IDisposable
        {
            private readonly EditorSelectionAllocationProbePhase phase;
            private readonly long startBytes;

            public EditorSelectionAllocationProbeScope(EditorSelectionAllocationProbePhase phase)
            {
                this.phase = phase;
                startBytes = System.GC.GetAllocatedBytesForCurrentThread();
            }

            public void Dispose()
            {
                RecordEditorSelectionAllocation(
                    phase,
                    System.GC.GetAllocatedBytesForCurrentThread() - startBytes);
            }
        }

        private static EditorSelectionAllocationProbeCounter editorSelectionTotalProbe;
        private static EditorSelectionAllocationProbeCounter editorSelectionCommandFlushProbe;
        private static EditorSelectionAllocationProbeCounter editorSelectionInputProbe;
        private static EditorSelectionAllocationProbeCounter editorSelectionFocusedReadModelProbe;
        private static EditorSelectionAllocationProbeCounter editorSelectionPanelProbe;
        private static EditorSelectionAllocationProbeCounter editorSelectionTacticalCameraProbe;
        private static EditorSelectionAllocationProbeCounter editorSelectionMarkerPreviewProbe;
        private static EditorSelectionAllocationProbeCounter editorSelectionCameraProbe;
#endif

        public void EnqueueSelectionDiagnostic(string message)
        {
            EnqueueSelectionDiagnosticMessage(message);
        }

#if UNITY_EDITOR
        public static void ResetEditorSelectionAllocationProbe()
        {
            editorSelectionTotalProbe = default;
            editorSelectionCommandFlushProbe = default;
            editorSelectionInputProbe = default;
            editorSelectionFocusedReadModelProbe = default;
            editorSelectionPanelProbe = default;
            editorSelectionTacticalCameraProbe = default;
            editorSelectionMarkerPreviewProbe = default;
            editorSelectionCameraProbe = default;
        }

        public static void RecordEditorSelectionAllocation(
            EditorSelectionAllocationProbePhase phase,
            long allocatedBytes)
        {
            switch (phase)
            {
                case EditorSelectionAllocationProbePhase.Total:
                    editorSelectionTotalProbe.Add(allocatedBytes);
                    break;
                case EditorSelectionAllocationProbePhase.CommandFlush:
                    editorSelectionCommandFlushProbe.Add(allocatedBytes);
                    break;
                case EditorSelectionAllocationProbePhase.Input:
                    editorSelectionInputProbe.Add(allocatedBytes);
                    break;
                case EditorSelectionAllocationProbePhase.FocusedReadModel:
                    editorSelectionFocusedReadModelProbe.Add(allocatedBytes);
                    break;
                case EditorSelectionAllocationProbePhase.Panel:
                    editorSelectionPanelProbe.Add(allocatedBytes);
                    break;
                case EditorSelectionAllocationProbePhase.TacticalCamera:
                    editorSelectionTacticalCameraProbe.Add(allocatedBytes);
                    break;
                case EditorSelectionAllocationProbePhase.MarkerPreview:
                    editorSelectionMarkerPreviewProbe.Add(allocatedBytes);
                    break;
                case EditorSelectionAllocationProbePhase.Camera:
                    editorSelectionCameraProbe.Add(allocatedBytes);
                    break;
            }
        }

        public static EditorSelectionAllocationProbeSnapshot GetEditorSelectionAllocationProbe()
        {
            return EditorSelectionAllocationProbeSnapshot.Create(
                editorSelectionTotalProbe,
                editorSelectionCommandFlushProbe,
                editorSelectionInputProbe,
                editorSelectionFocusedReadModelProbe,
                editorSelectionPanelProbe,
                editorSelectionTacticalCameraProbe,
                editorSelectionMarkerPreviewProbe,
                editorSelectionCameraProbe);
        }
#endif

        public static void EnqueueSelectionDiagnosticMessage(string message)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager em = world.EntityManager;
            if (ShouldQueueTransportBoardingDiagnostics(em))
                EnqueueTransportBoardingDiagnostic(em, $"[Selection] {message}");
        }

        public void LogSelectionClickDiagnostic(string message)
        {
            LogSelectionClickDiagnosticMessage(message);
        }

        public static void LogSelectionClickDiagnosticMessage(string message)
        {
            if (!EnableSelectionClickDiagnostics)
                return;

            Debug.Log($"{SelectionClickPrefix} {message}");
            EnqueueSelectionDiagnosticMessage(message);
        }

        [System.Diagnostics.Conditional("WARLINE_SELECTION_CLICK_DIAGNOSTICS")]
        public static void LogSelectionClickDebug(string message)
        {
            Debug.Log(message);
        }

        public static void LogMoveCommandTrace(string message)
        {
            if (!EnableMoveCommandTrace)
                return;

            Debug.Log($"{MoveCommandTracePrefix} {message}");
        }

        public static void LogScanCommandTrace(string message)
        {
            if (!EnableScanCommandTrace)
                return;

            Debug.Log($"{ScanCommandTracePrefix} {message}");
        }

        private static bool ShouldQueueTransportBoardingDiagnostics(EntityManager em)
        {
            if (Application.isBatchMode)
                return true;

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
            return !query.IsEmptyIgnoreFilter &&
                em.GetComponentData<RuntimeDiagnosticsStateComponent>(query.GetSingletonEntity()).TransportBoardingDiagnostics != 0;
        }

        private static Entity EnsureTransportBoardingDiagnosticQueue(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<TransportBoardingDiagnosticLogComponent>());
            if (!query.IsEmptyIgnoreFilter)
                return query.GetSingletonEntity();

            Entity queueEntity = em.CreateEntity(typeof(TransportBoardingDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "TransportBoardingDiagnosticLogQueue");
            em.AddBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
            return queueEntity;
        }

        private static void EnqueueTransportBoardingDiagnostic(EntityManager em, FixedString512Bytes message)
        {
            Entity queueEntity = EnsureTransportBoardingDiagnosticQueue(em);
            DynamicBuffer<TransportBoardingDiagnosticLogComponent> logs = em.GetBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
            logs.Add(new TransportBoardingDiagnosticLogComponent { Message = message });
        }
    }
}
