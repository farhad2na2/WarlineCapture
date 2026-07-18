using System;
using System.Reflection;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using UnityEngine;

public sealed class UiDiagnosticsRuntimeLogBufferTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            new UiDiagnosticsRuntimeLogBufferTests().MultipleOwners_KeepSubscriptionUntilLastReleaseAndResetCleanly();
            Debug.Log("[UiDiagnosticsRuntimeLogBufferValidation] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UiDiagnosticsRuntimeLogBufferValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void MultipleOwners_KeepSubscriptionUntilLastReleaseAndResetCleanly()
    {
        MethodInfo reset = typeof(UiDiagnosticsRuntimeLogBuffer).GetMethod(
            "ResetRuntimeLogState",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(reset);
        reset.Invoke(null, null);

        UiDiagnosticsRuntimeLogBuffer.EnsureSubscribed();
        UiDiagnosticsRuntimeLogBuffer.EnsureSubscribed();
        UiDiagnosticsRuntimeLogBuffer.ReleaseSubscription();
        Debug.Log("diagnostics-owner-still-active");

        StringAssert.Contains(
            "diagnostics-owner-still-active",
            UiDiagnosticsRuntimeLogBuffer.BuildLogText().ToString());
        int activeVersion = UiDiagnosticsRuntimeLogBuffer.Version;

        UiDiagnosticsRuntimeLogBuffer.ReleaseSubscription();
        Debug.Log("diagnostics-owner-released");
        Assert.AreEqual(activeVersion, UiDiagnosticsRuntimeLogBuffer.Version);

        reset.Invoke(null, null);
        Assert.AreEqual(0, UiDiagnosticsRuntimeLogBuffer.Version);
        Assert.AreEqual("Runtime log ready.", UiDiagnosticsRuntimeLogBuffer.BuildLogText().ToString());
    }
}
