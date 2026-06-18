using System;
using UnityEditor;
using UnityEngine;

internal static class ValidationExit
{
    public static void Exit(int code)
    {
        if (Application.isBatchMode ||
            Array.IndexOf(Environment.GetCommandLineArgs(), "-runTests") >= 0)
        {
            EditorApplication.Exit(code);
        }
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