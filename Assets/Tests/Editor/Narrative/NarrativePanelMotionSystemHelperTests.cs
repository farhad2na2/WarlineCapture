using System;
using Game.Configs;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class NarrativePanelMotionSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            NarrativePanelMotionSystemHelperTests tests = new();
            foreach (NarrativeMotionPreset preset in new[]
                     {
                         NarrativeMotionPreset.PushIn,
                         NarrativeMotionPreset.PullBack,
                         NarrativeMotionPreset.DriftLeft,
                         NarrativeMotionPreset.DriftRight,
                         NarrativeMotionPreset.StaticImpact
                     })
            {
                tests.MotionPresets_RemainFullBleedAndBounded(preset);
            }
            tests.ReducedMotion_IsStaticWithoutChangingTimeline();
            Debug.Log("[NarrativePanelMotionValidation] result=Passed tests=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[NarrativePanelMotionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [TestCase(NarrativeMotionPreset.PushIn)]
    [TestCase(NarrativeMotionPreset.PullBack)]
    [TestCase(NarrativeMotionPreset.DriftLeft)]
    [TestCase(NarrativeMotionPreset.DriftRight)]
    [TestCase(NarrativeMotionPreset.StaticImpact)]
    public void MotionPresets_RemainFullBleedAndBounded(NarrativeMotionPreset preset)
    {
        GameObject target = new("Panel", typeof(RectTransform));
        RectTransform rect = target.GetComponent<RectTransform>();
        NarrativePanelMotionSystemHelper motion = new(rect);

        motion.Start(preset, 4f, false);
        for (int i = 0; i < 8; i++)
        {
            motion.Tick(0.5f);
            Assert.GreaterOrEqual(rect.localScale.x, 1f);
            Assert.LessOrEqual(rect.localScale.x, 1.041f);
            Assert.LessOrEqual(Mathf.Abs(rect.anchoredPosition.x), 12.01f);
        }

        Assert.AreEqual(1f, motion.NormalizedTime, 0.0001f);
        UnityEngine.Object.DestroyImmediate(target);
    }

    [Test]
    public void ReducedMotion_IsStaticWithoutChangingTimeline()
    {
        GameObject target = new("Panel", typeof(RectTransform));
        RectTransform rect = target.GetComponent<RectTransform>();
        NarrativePanelMotionSystemHelper motion = new(rect);

        motion.Start(NarrativeMotionPreset.DriftRight, 4f, true);
        motion.Tick(2f);
        Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
        Assert.AreEqual(Vector3.one, rect.localScale);
        Assert.AreEqual(0.5f, motion.NormalizedTime, 0.0001f);

        motion.SetReducedMotion(false);
        Assert.AreEqual(0f, rect.anchoredPosition.x, 0.0001f);
        motion.Tick(2f);
        Assert.AreEqual(1f, motion.NormalizedTime, 0.0001f);
        motion.SetReducedMotion(true);
        Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
        Assert.AreEqual(Vector3.one, rect.localScale);

        motion.Cancel();
        Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
        Assert.AreEqual(Vector3.one, rect.localScale);
        UnityEngine.Object.DestroyImmediate(target);
    }
}
