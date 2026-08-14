#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class M01FirstContactFirstLaunchHandoffTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            CanonicalPayloadAndRequest(); AcceptanceIsCorrelated(); RejectionRetriesAreBounded();
            RestartReusesCorrelation(); StartupEnumIsAppendOnly();
            Debug.Log("[M01FirstContactFirstLaunchHandoffValidation] result=Passed tests=5"); ValidationExit.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); Debug.LogError("[M01FirstContactFirstLaunchHandoffValidation] result=Failed"); ValidationExit.Exit(1); }
    }

    [Test] public static void CanonicalPayloadAndRequest()
    {
        PlayerProfileSaveData profile = new();
        MissionLaunchPayload payload = FirstLaunchMissionHandoffOperation.Prepare(profile, 17, NarrativeGuidanceMode.Contextual);
        CampaignMissionLaunchRequestElement request = FirstLaunchMissionHandoffOperation.ToRequest(payload);
        Assert.That(payload.MissionId, Is.EqualTo(FirstLaunchMissionHandoffOperation.MissionId));
        Assert.That(payload.ScenarioId, Is.EqualTo(FirstLaunchMissionHandoffOperation.ScenarioId));
        Assert.That(payload.OperationMapId, Is.EqualTo(FirstLaunchMissionHandoffOperation.OperationMapId));
        Assert.That(request.LaunchOrigin, Is.EqualTo(MissionLaunchOriginKind.FirstLaunch));
        Assert.That(request.TransitionToken, Is.EqualTo(17));
    }

    [Test] public static void AcceptanceIsCorrelated()
    {
        using World world = WorldWithRoot(out Entity root);
        PlayerProfileSaveData profile = new(); bool published = false; byte rejections = 0;
        MissionLaunchPayload payload = FirstLaunchMissionHandoffOperation.Prepare(profile, 23, NarrativeGuidanceMode.Full);
        Assert.That(FirstLaunchMissionHandoffOperation.Advance(world.EntityManager, payload, ref published, ref rejections), Is.EqualTo(FirstLaunchMissionHandoffState.Pending));
        world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(root).Add(new CampaignMissionLaunchResultElement
        { TransitionToken = 23, SessionToken = new FixedString64Bytes(payload.SessionToken), Accepted = 1 });
        Assert.That(FirstLaunchMissionHandoffOperation.Advance(world.EntityManager, payload, ref published, ref rejections), Is.EqualTo(FirstLaunchMissionHandoffState.Accepted));
    }

    [Test] public static void RejectionRetriesAreBounded()
    {
        using World world = WorldWithRoot(out Entity root); PlayerProfileSaveData profile = new(); bool published = false; byte rejections = 0;
        MissionLaunchPayload payload = FirstLaunchMissionHandoffOperation.Prepare(profile, 29, NarrativeGuidanceMode.Minimal);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            FirstLaunchMissionHandoffOperation.Advance(world.EntityManager, payload, ref published, ref rejections);
            world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(root).Add(new CampaignMissionLaunchResultElement
            { TransitionToken = 29, SessionToken = new FixedString64Bytes(payload.SessionToken), Accepted = 0 });
            Assert.That(FirstLaunchMissionHandoffOperation.Advance(world.EntityManager, payload, ref published, ref rejections), Is.EqualTo(FirstLaunchMissionHandoffState.Rejected));
        }
        FirstLaunchMissionHandoffOperation.Advance(world.EntityManager, payload, ref published, ref rejections);
        Assert.That(world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root).Length, Is.EqualTo(3));
    }

    [Test] public static void RestartReusesCorrelation()
    {
        PlayerProfileSaveData profile = new();
        MissionLaunchPayload first = FirstLaunchMissionHandoffOperation.Prepare(profile, 31, NarrativeGuidanceMode.Full);
        MissionLaunchPayload resumed = FirstLaunchMissionHandoffOperation.Prepare(profile, 99, NarrativeGuidanceMode.Full);
        Assert.That(resumed.TransitionToken, Is.EqualTo(first.TransitionToken)); Assert.That(resumed.SessionToken, Is.EqualTo(first.SessionToken));
    }

    [Test] public static void StartupEnumIsAppendOnly()
    {
        Assert.That((byte)UiShellStartupDisposition.Pending, Is.Zero); Assert.That((byte)UiShellStartupDisposition.FirstLaunch, Is.EqualTo(1));
        Assert.That((byte)UiShellStartupDisposition.EnterMenu, Is.EqualTo(2)); Assert.That((byte)UiShellStartupDisposition.EnterMission, Is.EqualTo(3));
        Assert.That(File.ReadAllText("Assets/Game/Scripts/Runtime/FirstLaunch/FirstLaunchMissionHandoffOperation.cs"), Does.Not.Contain("UiShellRouteRequestComponent"));
    }

    private static World WorldWithRoot(out Entity root)
    {
        World world = new("M01 FirstLaunch handoff"); root = world.EntityManager.CreateEntity(typeof(CampaignMissionRootComponent));
        world.EntityManager.AddBuffer<CampaignMissionLaunchRequestElement>(root); world.EntityManager.AddBuffer<CampaignMissionLaunchResultElement>(root); return world;
    }
}
#endif
