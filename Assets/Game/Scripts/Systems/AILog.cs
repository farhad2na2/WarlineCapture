using UnityEngine;

public static class AILog
{
    private static readonly RuntimeDiagnosticsSystem RuntimeDiagnostics = new();

    public static bool IsEnabled => RuntimeDiagnostics.ShouldLogAI;

    public static void Log(string message)
    {
        if (IsEnabled)
            Debug.Log(message);
    }

    public static void LogWarning(string message)
    {
        if (IsEnabled)
            Debug.LogWarning(message);
    }
}
