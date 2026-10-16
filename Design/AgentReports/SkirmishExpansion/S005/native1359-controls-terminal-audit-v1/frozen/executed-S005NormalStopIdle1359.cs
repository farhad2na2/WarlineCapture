public static class S005NormalStopIdle1359
{
    public static string Run() => UnityEditor.EditorApplication.isPlaying.ToString()+"|"+UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode+"|"+UnityEditor.EditorApplication.isCompiling+"|"+UnityEditor.EditorApplication.isUpdating+"|"+Game.Editor.ExistingEditorValidation.IsRunning+"|"+UnityEngine.Application.dataPath;
}
