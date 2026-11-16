#if UNITY_EDITOR
using System;
using System.IO;
using Game.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
        // ExecuteMethod coordinator for the existing synthetic S073 placement probe.
    // This is explicitly not a normal-input playthrough.
    [InitializeOnLoad]
    public static class S073SyntheticLoadingActivationProbeDriver
    {
        private const string ArmedKey = "Warline.S073SyntheticLoadingActivationProbe.Armed";
        private const string DeadlineKey = "Warline.S073SyntheticLoadingActivationProbe.Deadline";
        private const string OutputKey = "WARLINE_S073_NATIVE_LOADING_ACTIVATION_OUTPUT";
        private const string PredicateKey = "WARLINE_S073_PREDICATE_OUTPUT";
        private const string MaskKey = "WARLINE_S073_MASK_OUTPUT";
        private const string PlacementAuditKey = "WARLINE_S073_PLACEMENT_AUDIT_OUTPUT";
        private const double TimeoutSeconds = 480d;

        static S073SyntheticLoadingActivationProbeDriver()
        {
            if (SessionState.GetBool(ArmedKey, false))
                EditorApplication.update += Poll;
        }

        // Invoke through the checked macOS wrapper with no -quit. All four output
        // paths must be fresh and exported before Unity starts so the passive
        // observer subscribes during editor initialization.
        public static void RunS073SyntheticLoadingActivationProbe()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before starting the synthetic S073 probe.");

            string observerPath = RequireFreshPrivateTemporaryOutput(OutputKey);
            RequireFreshPrivateTemporaryOutput(PredicateKey);
            string maskPath = RequireFreshPrivateTemporaryOutput(MaskKey);
            RequireFreshPrivateTemporaryOutput(PlacementAuditKey);
            if (File.Exists(maskPath + ".json"))
                throw new InvalidOperationException("Fresh S073 authored-mask manifest path required: " + maskPath + ".json");
            if (SessionState.GetBool(ArmedKey, false))
                throw new InvalidOperationException("S073 synthetic loading probe is already armed.");

            SessionState.SetBool(ArmedKey, true);
            SessionState.SetFloat(DeadlineKey, (float)(EditorApplication.timeSinceStartup + TimeoutSeconds));
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            try
            {
                SkirmishIndustrialBasinPlacementProbe.Launch();
                Debug.Log("[S073SyntheticLoadingActivationProbe] result=Started scope=SyntheticPlacementProbe normalInput=0 output=" + observerPath);
            }
            catch
            {
                SessionState.SetBool(ArmedKey, false);
                EditorApplication.update -= Poll;
                throw;
            }
        }

        private static void Poll()
        {
            if (!SessionState.GetBool(ArmedKey, false))
            {
                EditorApplication.update -= Poll;
                return;
            }

            string outputPath;
            try
            {
                outputPath = RequireFreshPrivateTemporaryOutput(OutputKey, allowExisting: true);
                if (File.Exists(outputPath))
                {
                    JObject result = JObject.Parse(File.ReadAllText(outputPath));
                    bool passed = (string)result["kind"] == "S073NativeLoadingActivationObservation" &&
                        (int?)result["schema_version"] == 1 && (string)result["result"] == "Observed";
                    Finish(passed ? 0 : 1, passed ? "Observed" : "ObserverResultNotObserved:" + (string)result["result"], outputPath);
                    return;
                }

                if (EditorApplication.timeSinceStartup >= SessionState.GetFloat(DeadlineKey, 0f))
                    Finish(1, "TimedOutWithoutObserverOutput", outputPath);
            }
            catch (Exception error)
            {
                Finish(1, "DriverFailure:" + error.GetType().Name + ":" + error.Message, null);
            }
        }

        private static void Finish(int exitCode, string reason, string outputPath)
        {
            SessionState.SetBool(ArmedKey, false);
            EditorApplication.update -= Poll;
            Debug.Log("[S073SyntheticLoadingActivationProbe] result=" + (exitCode == 0 ? "Passed" : "Failed") +
                " scope=SyntheticPlacementProbe normalInput=0 reason=" + reason + " output=" + (outputPath ?? "Unavailable"));
            EditorApplication.Exit(exitCode);
        }

        private static string RequireFreshPrivateTemporaryOutput(string environmentKey, bool allowExisting = false)
        {
            string rawPath = Environment.GetEnvironmentVariable(environmentKey);
            if (string.IsNullOrWhiteSpace(rawPath))
                throw new InvalidOperationException("Required fresh output environment variable missing: " + environmentKey);
            string path = Path.GetFullPath(rawPath);
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal))
                throw new InvalidOperationException("Synthetic probe outputs must be under /private/tmp: " + environmentKey);
            if (!allowExisting && File.Exists(path))
                throw new InvalidOperationException("Fresh output path required for " + environmentKey + ": " + path);
            return path;
        }
    }
}
#endif
