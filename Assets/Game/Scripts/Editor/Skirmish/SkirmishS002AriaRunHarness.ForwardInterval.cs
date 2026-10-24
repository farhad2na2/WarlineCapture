#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        [InitializeOnLoadMethod]
        static void RegisterForwardIntervalDiagnostic()
        {
            if (Environment.GetEnvironmentVariable("WARLINE_S002_OFFLINE_FORWARD_TRACE") == "1")
                EditorApplication.update += FinishForwardIntervalDiagnostic;
        }
        static void FinishForwardIntervalDiagnostic()
        {
            if (!S002ForwardIntervalCapture.Completed || finishingEvidence || !EditorApplication.isPlaying) return;
            EditorApplication.update -= FinishForwardIntervalDiagnostic;
            bool failed = S002ForwardIntervalCapture.Failure != null;
            Debug.Log("[S002ForwardInterval] result=" + (failed ? "Failed" : "Captured") +
                " acceptance=Unmeasured reason=" + (S002ForwardIntervalCapture.Failure ?? "normalTouchMoveNonIdleUtility"));
            // Reuse the harness's existing diagnostic teardown. Set only at the
            // end, after ordinary UI input; never publish a complete mission row.
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
            Finish(failed, "offlineForwardIntervalDiagnostic");
        }
    }
}
#endif
