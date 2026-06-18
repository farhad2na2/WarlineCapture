using UnityEditor;
using UnityEngine;

internal static class ValidationExit
{
    public static void Exit(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    public static void Passed()
    {
        Exit(0);
    }

    public static void Failed()
    {
        Exit(1);
    }
}
