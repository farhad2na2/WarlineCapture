#if UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Queue request file support for an already open Editor (invoke_unity_macos.sh --reuse). Environment variables
    /// cannot reach an open Editor, so run_aria_queue.py --reuse writes Temp/aria-queue/request.json and the wrapper
    /// then runs RunAriaQueueAndExit unchanged. WARLINE_ARIA_QUEUE (the environment path) still wins when it is set.
    /// A reused Editor also needs Play Mode entered without a domain reload, because ExistingEditorValidation aborts a
    /// run whose assemblies reload, and must never be closed by the queue.
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        private const string AriaQueueRequestFlagsKey = "Warline.AriaQueue.RequestFlags";
        private const string AriaQueueReuseOptionsKey = "Warline.AriaQueue.ReuseOptions";

        /// <summary>
        /// The queue file path: WARLINE_ARIA_QUEUE when set, otherwise the request file's queue (the request is validated,
        /// consumed, and its menuJourney / validationLibrary flags are applied to this process). Null when neither exists.
        /// Throws InvalidDataException for a request that is present but refused.
        /// </summary>
        private static string ResolveAriaQueuePath()
        {
            string environmentPath = Environment.GetEnvironmentVariable("WARLINE_ARIA_QUEUE");
            if (!string.IsNullOrWhiteSpace(environmentPath)) return environmentPath;

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string requestPath = SkirmishAriaQueueRequest.PathFor(root);
            if (!File.Exists(requestPath)) return null;

            string error = SkirmishAriaQueueRequest.Parse(File.ReadAllText(requestPath), out var request);
            if (error == null) error = SkirmishAriaQueueRequest.Validate(request, root, DateTime.UtcNow, File.Exists);
            if (error != null)
            {
                Debug.LogError(SkirmishAriaQueueRequest.FormatRefused(error));
                MoveAside(requestPath, requestPath + ".refused");
                throw new InvalidDataException("ARIA queue request refused: " + error);
            }

            MoveAside(requestPath, SkirmishAriaQueueRequest.ConsumedPath(requestPath, request.requestId));
            // Explicit values, never inherited ones: a reused Editor keeps the process environment of earlier runs.
            Environment.SetEnvironmentVariable("WARLINE_MENU_JOURNEY", request.menuJourney ? "1" : null);
            Environment.SetEnvironmentVariable(SkirmishValidationLibraryPlan.EnvironmentVariable, request.validationLibrary ? "1" : null);
            SessionState.SetBool(AriaQueueRequestFlagsKey, true);
            Debug.Log(SkirmishAriaQueueRequest.FormatAccepted(request));
            return request.queue;
        }

        private static void MoveAside(string from, string to)
        {
            try
            {
                if (File.Exists(to)) File.Delete(to);
                File.Move(from, to);
            }
            catch (Exception error)
            {
                // A request that cannot be consumed could be replayed, so make that loud; the run itself is still valid.
                Debug.LogWarning(SkirmishAriaQueueRequest.Marker + " request=NotConsumed path=" + from + " error=" + error.Message);
            }
        }

        // Flags taken from a request file must not leak into later runs of a reused Editor.
        private static void ClearAriaQueueRequestEnvironment()
        {
            if (!SessionState.GetBool(AriaQueueRequestFlagsKey, false)) return;
            Environment.SetEnvironmentVariable("WARLINE_MENU_JOURNEY", null);
            Environment.SetEnvironmentVariable(SkirmishValidationLibraryPlan.EnvironmentVariable, null);
            SessionState.EraseBool(AriaQueueRequestFlagsKey);
        }

        // ExistingEditorValidation records a domain reload as a failed validation, so a queue run through --reuse enters
        // every job's Play Mode with DisableDomainReload. FinishQuitAfterPlayMode restores the options after each job
        // (RestoreS004EditorJourneyOptions), so this is applied again at every dispatch. A normally launched Editor
        // (no ExistingEditorValidation run) is untouched.
        private static bool EnsureReusedQueueEditorOptions()
        {
            if (!ExistingEditorValidation.IsRunning) return false;
            if (!SessionState.GetBool("S004EditorJourney.OptionsSaved", false))
            {
                SessionState.SetBool("S004EditorJourney.OptionsSaved", true);
                SessionState.SetBool("S004EditorJourney.OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
                SessionState.SetInt("S004EditorJourney.Options", (int)EditorSettings.enterPlayModeOptions);
            }
            SessionState.SetBool(AriaQueueReuseOptionsKey, true);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            return true;
        }

        // A job that never reached Play Mode (launch failure) has no FinishQuitAfterPlayMode to restore the options, so
        // the queue's own end restores them. Only when this queue applied them.
        private static void RestoreReusedQueueEditorOptions()
        {
            if (!SessionState.GetBool(AriaQueueReuseOptionsKey, false)) return;
            SessionState.EraseBool(AriaQueueReuseOptionsKey);
            if (!SessionState.GetBool("S004EditorJourney.OptionsSaved", false)) return;
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("S004EditorJourney.Options", 0);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("S004EditorJourney.OptionsEnabled", false);
            SessionState.EraseBool("S004EditorJourney.OptionsSaved");
        }

        // Same refresh suspension the S004 editor journey takes (released by MissionEditorValidationExit once the run ends).
        private static void SuspendRefreshForReusedQueue()
        {
            if (!ExistingEditorValidation.IsRunning) return;
            AssetDatabase.DisallowAutoRefresh();
            SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", true);
        }
    }
}
#endif
