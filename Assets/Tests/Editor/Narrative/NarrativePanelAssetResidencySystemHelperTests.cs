using System;
using Game.Configs;
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class NarrativePanelAssetResidencyTests
{
    [Test]
    public void Residency_KeepsOnlyCurrentAndNextHandles()
    {
        FirstLaunchNarrativeConfigBuilder.Build();
        NarrativePanelAssetResidency residency = new();
        NarrativeSequenceConfig config = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
        NarrativeStateRecord firstState = Find(config, "FL-P01");
        NarrativeStateRecord secondState = Find(config, "FL-P02");
        NarrativeStateRecord thirdState = Find(config, "FL-P03");

        Sprite first = residency.LoadCurrentAndPrepareNext(firstState.Panel16x9Reference, secondState.Panel16x9Reference);
        Assert.NotNull(first);
        Assert.AreEqual(2, residency.ResidentAssetCount);
        StringAssert.Contains(firstState.Panel16x9Reference.AssetGUID, residency.CurrentKey);
        StringAssert.Contains(secondState.Panel16x9Reference.AssetGUID, residency.NextKey);

        Sprite second = residency.LoadCurrentAndPrepareNext(secondState.Panel16x9Reference, thirdState.Panel16x9Reference);
        Assert.NotNull(second);
        Assert.AreNotSame(first, second);
        Assert.AreEqual(2, residency.ResidentAssetCount);
        StringAssert.Contains(secondState.Panel16x9Reference.AssetGUID, residency.CurrentKey);
        StringAssert.Contains(thirdState.Panel16x9Reference.AssetGUID, residency.NextKey);

        residency.ReleaseAll();
        Assert.AreEqual(0, residency.ResidentAssetCount);
        Assert.AreEqual(string.Empty, residency.CurrentKey);
        Assert.AreEqual(string.Empty, residency.NextKey);
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
            new NarrativePanelAssetResidencyTests().Residency_KeepsOnlyCurrentAndNextHandles();
            Debug.Log("[NarrativePanelAssetResidencyValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[NarrativePanelAssetResidencyValidation] result=Failed");
            ValidationExit.Failed();
        }
    }
}
