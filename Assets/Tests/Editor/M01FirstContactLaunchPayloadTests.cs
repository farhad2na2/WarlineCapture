using System;
using System.IO;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class M01FirstContactLaunchPayloadTests
{
    private const string PassMarker = "[M01FirstContactLaunchPayloadValidation] result=Passed tests=9";

    public static void RunFocusedValidation()
    {
        try
        {
            M01FirstContactLaunchPayloadTests tests = new();
            tests.EqualInputsProduceEqualPayloads();
            tests.FirstLaunchAndCampaignUseOneFactory();
            tests.RetryPreservesIdentityAndIncrementsOnlyAttempt();
            tests.ChangedCorrelationChangesEquality();
            tests.InvalidIdentityFailsClosed();
            tests.InvalidOriginFailsClosed();
            tests.InvalidGuidanceFailsClosed();
            tests.ZeroSeedFailsClosed();
            tests.FactoryHasNoUiOrRouteDependency();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactLaunchPayloadValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void EqualInputsProduceEqualPayloads()
    {
        MissionLaunchPayload left = Create(MissionLaunchOriginKind.FirstLaunch);
        MissionLaunchPayload right = Create(MissionLaunchOriginKind.FirstLaunch);
        Assert.AreEqual(left, right);
        Assert.IsTrue(left == right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    [Test]
    public void FirstLaunchAndCampaignUseOneFactory()
    {
        MissionLaunchPayload firstLaunch = Create(MissionLaunchOriginKind.FirstLaunch);
        MissionLaunchPayload campaign = Create(MissionLaunchOriginKind.CampaignOperations);
        Assert.AreEqual(firstLaunch.MissionId, campaign.MissionId);
        Assert.AreEqual(firstLaunch.ScenarioId, campaign.ScenarioId);
        Assert.AreEqual(firstLaunch.OperationMapId, campaign.OperationMapId);
        Assert.AreEqual(MissionLaunchOriginKind.FirstLaunch, firstLaunch.LaunchOrigin);
        Assert.AreEqual(MissionLaunchOriginKind.CampaignOperations, campaign.LaunchOrigin);
    }

    [Test]
    public void RetryPreservesIdentityAndIncrementsOnlyAttempt()
    {
        MissionLaunchPayload previous = Create(MissionLaunchOriginKind.FirstLaunch);
        MissionLaunchPayload retry = MissionLaunchPayloadFactory.CreateRetry(previous, 778UL);
        Assert.AreEqual(previous.MissionId, retry.MissionId);
        Assert.AreEqual(previous.ScenarioId, retry.ScenarioId);
        Assert.AreEqual(previous.OperationMapId, retry.OperationMapId);
        Assert.AreEqual(previous.SessionToken, retry.SessionToken);
        Assert.AreEqual(previous.DeterministicSeed, retry.DeterministicSeed);
        Assert.AreEqual(previous.LaunchOrigin, retry.LaunchOrigin);
        Assert.AreEqual(previous.AttemptOrdinal + 1, retry.AttemptOrdinal);
        Assert.AreEqual(MissionRunKind.Retry, retry.RunKind);
        Assert.AreEqual(778UL, retry.TransitionToken);
    }

    [Test]
    public void ChangedCorrelationChangesEquality()
    {
        MissionLaunchPayload previous = Create(MissionLaunchOriginKind.FirstLaunch);
        MissionLaunchPayload retry = MissionLaunchPayloadFactory.CreateRetry(previous, 778UL);
        Assert.AreNotEqual(previous, retry);
        Assert.IsTrue(previous != retry);
    }

    [Test]
    public void InvalidIdentityFailsClosed()
    {
        Assert.Throws<ArgumentException>(() => Create(MissionLaunchOriginKind.FirstLaunch, missionId: ""));
        Assert.Throws<ArgumentException>(() => Create(MissionLaunchOriginKind.FirstLaunch, sessionToken: null));
    }

    [Test]
    public void InvalidOriginFailsClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(MissionLaunchOriginKind.None));
    }

    [Test]
    public void InvalidGuidanceFailsClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MissionLaunchPayloadFactory.Create(
            "saga.ch01.m01.first_contact", "scenario.ch01.m01.first_contact",
            "opmap.ch01.district_edge_01", MissionLaunchOriginKind.FirstLaunch,
            MissionRunKind.FirstClear, (NarrativeGuidanceMode)99, false, 777UL,
            "session.m01.test", 0, 104729));
    }

    [Test]
    public void ZeroSeedFailsClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MissionLaunchPayloadFactory.Create(
            "saga.ch01.m01.first_contact", "scenario.ch01.m01.first_contact",
            "opmap.ch01.district_edge_01", MissionLaunchOriginKind.FirstLaunch,
            MissionRunKind.FirstClear, NarrativeGuidanceMode.Contextual, false, 777UL,
            "session.m01.test", 0, 0));
    }

    [Test]
    public void FactoryHasNoUiOrRouteDependency()
    {
        string source = File.ReadAllText(
            "Assets/Game/Scripts/Runtime/Missions/MissionLaunchPayloadFactory.cs");
        StringAssert.DoesNotContain("Game.UI", source);
        StringAssert.DoesNotContain("Route", source);
        StringAssert.DoesNotContain("MonoBehaviour", source);
        StringAssert.DoesNotContain("DateTime", source);
        StringAssert.DoesNotContain("Random", source);
    }

    private static MissionLaunchPayload Create(
        MissionLaunchOriginKind origin,
        string missionId = "saga.ch01.m01.first_contact",
        string sessionToken = "session.m01.test") => MissionLaunchPayloadFactory.Create(
            missionId,
            "scenario.ch01.m01.first_contact",
            "opmap.ch01.district_edge_01",
            origin,
            MissionRunKind.FirstClear,
            NarrativeGuidanceMode.Contextual,
            false,
            777UL,
            sessionToken,
            0,
            104729);
}
