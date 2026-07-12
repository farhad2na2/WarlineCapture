using System;
using System.Collections;
using System.Text.RegularExpressions;
using Game.Configs;
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;

public sealed class NarrativePanelAssetResidencyPresentationSystemHelperTests
{
    [Test]
    public void Residency_KeepsOnlyCurrentAndNextHandles()
    {
        NarrativeSequenceConfig config = LoadConfig();
        NarrativeStateRecord first = Find(config, "FL-P01");
        NarrativeStateRecord second = Find(config, "FL-P02");
        NarrativeStateRecord third = Find(config, "FL-P03");
        NarrativePanelAssetResidencyPresentationSystemHelper residency = new();

        residency.RequestCurrentAndPrepareNext(
            first.Panel16x9Reference,
            second.Panel16x9Reference,
            transitionToken: 1);
        Assert.AreEqual(2, residency.ResidentAssetCount);
        StringAssert.Contains(first.Panel16x9Reference.AssetGUID, residency.CurrentKey);
        StringAssert.Contains(second.Panel16x9Reference.AssetGUID, residency.NextKey);

        residency.RequestCurrentAndPrepareNext(
            second.Panel16x9Reference,
            third.Panel16x9Reference,
            transitionToken: 2);
        Assert.AreEqual(2, residency.ResidentAssetCount);
        StringAssert.Contains(second.Panel16x9Reference.AssetGUID, residency.CurrentKey);
        StringAssert.Contains(third.Panel16x9Reference.AssetGUID, residency.NextKey);
        residency.ReleaseAll();
    }

    [Test]
    public void Residency_InvalidReferencesReturnDirectFallbackWithoutHandles()
    {
        Texture2D texture = new(2, 2);
        Sprite fallback = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
        NarrativePanelAssetResidencyPresentationSystemHelper residency = new();
        Sprite result = residency.RequestCurrentAndPrepareNext(null, null, 1, fallback);
        Assert.AreSame(fallback, result);
        Assert.AreEqual(0, residency.ResidentAssetCount);
        UnityEngine.Object.DestroyImmediate(fallback);
        UnityEngine.Object.DestroyImmediate(texture);
    }

    [Test]
    public void Residency_RapidSeekReleasesSupersededRequests()
    {
        NarrativeSequenceConfig config = LoadConfig();
        NarrativeStateRecord first = Find(config, "FL-P01");
        NarrativeStateRecord second = Find(config, "FL-P02");
        NarrativeStateRecord third = Find(config, "FL-P03");
        NarrativePanelAssetResidencyPresentationSystemHelper residency = new();
        residency.RequestCurrentAndPrepareNext(first.Panel16x9Reference, second.Panel16x9Reference, 10);
        residency.RequestCurrentAndPrepareNext(third.Panel16x9Reference, first.Panel16x9Reference, 11);
        Assert.AreEqual(2, residency.ResidentAssetCount);
        StringAssert.Contains(third.Panel16x9Reference.AssetGUID, residency.CurrentKey);
        StringAssert.Contains(first.Panel16x9Reference.AssetGUID, residency.NextKey);
        StringAssert.DoesNotContain(second.Panel16x9Reference.AssetGUID, residency.CurrentKey + residency.NextKey);
        residency.ReleaseAll();
    }

    [Test]
    public void Residency_ReleaseAllClearsPendingRequests()
    {
        NarrativeSequenceConfig config = LoadConfig();
        NarrativeStateRecord first = Find(config, "FL-P01");
        NarrativeStateRecord second = Find(config, "FL-P02");
        NarrativePanelAssetResidencyPresentationSystemHelper residency = new();
        residency.RequestCurrentAndPrepareNext(first.Panel16x9Reference, second.Panel16x9Reference, 20);
        residency.ReleaseAll();
        Assert.AreEqual(0, residency.ResidentAssetCount);
        Assert.AreEqual(string.Empty, residency.CurrentKey);
        Assert.AreEqual(string.Empty, residency.NextKey);
    }

    [UnityTest]
    public IEnumerator Residency_CompletesCurrentWithoutBlocking()
    {
        NarrativeStateRecord first = Find(LoadConfig(), "FL-P01");
        NarrativePanelAssetResidencyPresentationSystemHelper residency = new();
        Sprite completed = null;
        residency.CurrentReady += (token, sprite) =>
        {
            if (token == 30)
                completed = sprite;
        };
        residency.RequestCurrentAndPrepareNext(first.Panel16x9Reference, null, 30);
        for (int frame = 0; frame < 120 && completed == null; frame++)
            yield return null;
        Assert.NotNull(completed);
        Assert.IsTrue(residency.IsCurrentReady);
        residency.ReleaseAll();
    }

    [UnityTest]
    public IEnumerator Residency_FailedLoadReportsFailureAndReleasesHandle()
    {
        NarrativePanelAssetResidencyPresentationSystemHelper residency = new();
        bool failed = false;
        residency.CurrentFailed += token => failed = token == 40;
        AssetReferenceSprite missing = new("00000000000000000000000000000000");
        LogAssert.Expect(LogType.Error, new Regex(".*InvalidKeyException.*No Location found for Key=.*"));
        residency.RequestCurrentAndPrepareNext(missing, null, 40);
        for (int frame = 0; frame < 120 && !failed; frame++)
            yield return null;
        Assert.IsTrue(failed);
        Assert.AreEqual(0, residency.ResidentAssetCount);
    }

    [UnityTest]
    public IEnumerator Residency_ReleasedRequestDoesNotPublishStaleCompletion()
    {
        NarrativeStateRecord first = Find(LoadConfig(), "FL-P01");
        NarrativePanelAssetResidencyPresentationSystemHelper residency = new();
        bool published = false;
        residency.CurrentReady += (_, _) => published = true;
        residency.RequestCurrentAndPrepareNext(first.Panel16x9Reference, null, 50);
        residency.ReleaseAll();
        for (int frame = 0; frame < 10; frame++)
            yield return null;
        Assert.IsFalse(published);
        Assert.AreEqual(0, residency.ResidentAssetCount);
    }

    private static NarrativeSequenceConfig LoadConfig()
    {
        FirstLaunchNarrativeConfigBuilder.Build();
        return AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(
            FirstLaunchNarrativeConfigBuilder.SequencePath);
    }

    private static NarrativeStateRecord Find(NarrativeSequenceConfig config, string stateId)
    {
        foreach (NarrativeStateRecord state in config.States)
        {
            if (state.StateId == stateId)
                return state;
        }
        Assert.Fail($"Missing state {stateId}");
        return null;
    }

    public static void RunFocusedValidation()
    {
        try
        {
            NarrativePanelAssetResidencyPresentationSystemHelperTests tests = new();
            tests.Residency_KeepsOnlyCurrentAndNextHandles();
            tests.Residency_InvalidReferencesReturnDirectFallbackWithoutHandles();
            tests.Residency_RapidSeekReleasesSupersededRequests();
            tests.Residency_ReleaseAllClearsPendingRequests();
            Debug.Log("[NarrativePanelAssetResidencyPresentationSystemHelperValidation] result=Passed tests=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[NarrativePanelAssetResidencyPresentationSystemHelperValidation] result=Failed");
            ValidationExit.Failed();
        }
    }
}
