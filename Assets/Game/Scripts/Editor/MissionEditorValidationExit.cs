using UnityEditor;

namespace Game.Editor
{
    /// <summary>Only exits the wrapper-owned validation Editor after an explicitly completed probe.</summary>
    [InitializeOnLoad]
    internal static class MissionEditorValidationExit
    {
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
            SessionState.SetInt(Status, passed ? 0 : 1);
            SessionState.SetBool(Pending, true);
            SessionState.SetBool(ResumeRefresh, true);
            readyAt = EditorApplication.timeSinceStartup + 3;
            EditorApplication.ExitPlaymode();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool(ResumeRefresh, false))
            {
                SessionState.SetBool(ResumeRefresh, false);
                AssetDatabase.AllowAutoRefresh();
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            int status = SessionState.GetInt(Status, 1);
            SessionState.SetBool(Pending, false);
            EditorApplication.Exit(status);
        }
    }
}
