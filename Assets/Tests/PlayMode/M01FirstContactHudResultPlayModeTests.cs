using System.Collections;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class M01FirstContactHudResultPlayModeTests
{
    [UnityTest]
    public IEnumerator ResultViewAppliesWithoutPerFramePolicy()
    {
        GameObject root = new("ResultView");
        root.AddComponent<RectTransform>();
        MissionResultPopupView view = root.AddComponent<MissionResultPopupView>();
        UiMissionResultPopupModel model = new(4, "saga.ch01.m01.first_contact",
            UiMissionResultOutcome.Loss, "MISSION FAILED", "FIRST CONTACT", "Regroup.", 0,
            "01:05", "1", "1/3", "No reward", "RETRY", true, true);
        view.Apply(in model);
        yield return null;
        Assert.That(root.activeInHierarchy, Is.True);
        Object.Destroy(root);
    }

    [UnityTest]
    public IEnumerator ResultButtonsAreLifecycleSafeWhenUnconfigured()
    {
        GameObject root = new("ResultViewLifecycle");
        root.AddComponent<RectTransform>();
        MissionResultPopupView view = root.AddComponent<MissionResultPopupView>();
        view.enabled = false;
        view.enabled = true;
        yield return null;
        Assert.That(view.isActiveAndEnabled, Is.True);
        Object.Destroy(root);
    }
}
