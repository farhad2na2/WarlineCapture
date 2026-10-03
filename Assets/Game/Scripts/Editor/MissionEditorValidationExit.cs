using UnityEditor;
using System;

namespace Game.Editor
{
    /// <summary>Only exits the wrapper-owned validation Editor after an explicitly completed probe.</summary>
    [InitializeOnLoad]
    internal static class MissionEditorValidationExit
    {
        internal static int? LastCompletion;
        private const string Pending = "Warline.MissionValidation.ExitPending";
        private const string Status = "Warline.MissionValidation.ExitStatus";
        private const string ResumeRefresh = "Warline.MissionValidation.ResumeRefresh";
        private static double readyAt;

        static MissionEditorValidationExit()
        {
            readyAt = EditorApplication.timeSinceStartup + 3;
            EditorApplication.update += Tick;
        }

        internal static void Complete(bool passed)
        {
            // Publish synchronous validation results before the wrapper method returns.
            LastCompletion = passed ? 0 : 1;
            SessionState.SetInt(Status, LastCompletion.Value);
            SessionState.SetBool(Pending, true);
            // Normal-input probes suspend refresh before entering Play mode;
            // synchronous checks never acquire that suspension.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                SessionState.SetBool(ResumeRefresh, true);
            readyAt = EditorApplication.timeSinceStartup + 3;
            EditorApplication.ExitPlaymode();
        }

        private static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!SessionState.GetBool(Pending, false))
            {
                if (SessionState.GetBool(ResumeRefresh, false) && !ExistingEditorValidation.IsRunning)
                { SessionState.SetBool(ResumeRefresh, false); AssetDatabase.AllowAutoRefresh(); }
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            int status = SessionState.GetInt(Status, 1);
            SessionState.SetBool(Pending, false);
            LastCompletion = status;
            if (ExistingEditorValidation.IsRunning) return;
            // A normal connected Editor must never be closed after domain reload.
            // Only a wrapper-launched executeMethod process owns its own exit.
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-executeMethod") < 0) return;
            EditorApplication.Exit(status);
        }
    }
}
