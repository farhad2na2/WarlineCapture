using System;
using UnityEditor;
using UnityEngine;

internal static class ValidationExit
{
    private static int _suppressedExitDepth;

    public static int? LastExitCode { get; private set; }

    public static IDisposable SuppressProcessExit()
    {
        _suppressedExitDepth++;
        return new SuppressedExitScope();
    }

    public static void ClearLastExitCode()
    {
        LastExitCode = null;
    }

    public static void Exit(int code)
    {
        LastExitCode = code;
        if (_suppressedExitDepth > 0)
        {
            return;
        }

        // Unity's command-line test runner owns process lifetime and writes the
        // requested NUnit XML only after the full run completes.  A focused
        // validation may still use this helper to terminate a batch-mode editor,
        // but never compete with the test runner or Unity's -quit shutdown path.
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        if (Application.isBatchMode &&
            Array.IndexOf(commandLineArgs, "-runTests") < 0 &&
            Array.IndexOf(commandLineArgs, "-quit") < 0)
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

    private readonly struct SuppressedExitScope : IDisposable
    {
        public void Dispose()
        {
            _suppressedExitDepth = Math.Max(0, _suppressedExitDepth - 1);
        }
    }
}
