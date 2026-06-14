using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIShellEcsPresentationSystem : MonoBehaviour
{
    private static readonly ProfilerMarker TryGetBoundaryMarker = new("UIShellEcsPresentation.TryGetBoundary");
    private static readonly ProfilerMarker FlushCompletionMarker = new("UIShellEcsPresentation.FlushCompletion");
    private static readonly ProfilerMarker ReadCommandsMarker = new("UIShellEcsPresentation.ReadCommands");

    [SerializeField] private UIShellView shellView;

    private readonly List<UiShellPresentationCommandModel> commandScratch = new();
    private bool isExecuting;
    private int activeSequenceId = -1;
    private bool hasPendingCompletion;
    private UiShellTransitionCompleteModel pendingCompletion;

#if UNITY_EDITOR
    private static long editorAllocationBytes;
    private static int editorAllocationSamples;
    private static int editorUpdateSamples;

    public static void ResetEditorAllocationProbe()
    {
        editorAllocationBytes = 0;
        editorAllocationSamples = 0;
        editorUpdateSamples = 0;
    }

    public static void GetEditorAllocationProbe(out long bytes, out int allocationSamples, out int updateSamples)
    {
        bytes = editorAllocationBytes;
        allocationSamples = editorAllocationSamples;
        updateSamples = editorUpdateSamples;
    }
#endif

    private void Awake()
    {
        if (shellView == null)
            shellView = GetComponent<UIShellView>();
    }

    private void Update()
    {
#if UNITY_EDITOR
        long allocationStart = System.GC.GetAllocatedBytesForCurrentThread();
        try
        {
#endif
        using (TryGetBoundaryMarker.Auto())
        {
            if (!UiShellRuntimeGateway.TryReadShellState(out _))
                return;
        }

        using (FlushCompletionMarker.Auto())
        {
            FlushPendingCompletion();
        }

        if (isExecuting || shellView == null)
            return;

        using (ReadCommandsMarker.Auto())
        {
            if (!UiShellRuntimeGateway.TryConsumePresentationCommands(commandScratch))
                return;
        }

        UiShellPresentationCommandModel finalCommand = commandScratch[commandScratch.Count - 1];
        activeSequenceId = finalCommand.SequenceId;
        isExecuting = true;

        shellView.ExecuteCommandSequence(commandScratch, activeSequenceId, completedSequenceId =>
        {
            if (completedSequenceId != activeSequenceId)
                return;

            pendingCompletion = new UiShellTransitionCompleteModel(
                finalCommand.Kind,
                finalCommand.Region,
                completedSequenceId);
            hasPendingCompletion = true;
            isExecuting = false;
        });
#if UNITY_EDITOR
        }
        finally
        {
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            editorUpdateSamples++;
            if (allocated > 0)
            {
                editorAllocationBytes += allocated;
                editorAllocationSamples++;
            }
        }
#endif
    }

    public void Configure(UIShellView view)
    {
        shellView = view;
    }

    private void FlushPendingCompletion()
    {
        if (!hasPendingCompletion)
            return;

        if (UiShellRuntimeGateway.TryEnqueueTransitionComplete(pendingCompletion))
            hasPendingCompletion = false;
    }
}
