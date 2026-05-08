using UnityEngine;

public static class AILog
{
    public static void Log(string message)
    {
        if (InitialUnitsRuntimeState.ShouldLogAI)
            Debug.Log(message);
    }

    public static void LogWarning(string message)
    {
        if (InitialUnitsRuntimeState.ShouldLogAI)
            Debug.LogWarning(message);
    }
}
