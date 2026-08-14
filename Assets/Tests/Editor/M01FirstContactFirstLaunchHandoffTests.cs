#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEditor;

public static class M01FirstContactFirstLaunchHandoffTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            CanonicalPayloadAndRequest(); AcceptanceIsCorrelated(); RejectionRetriesAreBounded();
            RestartReusesCorrelation(); StartupEnumIsAppendOnly(); ProductionSceneSerializesCampaignConfigs();
            SharedPhysicalMapReusesLogicalMissionRequest();
            Debug.Log("[M01FirstContactFirstLaunchHandoffValidation] result=Passed tests=7"); ValidationExit.Exit(0);
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

    [Test] public static void ProductionSceneSerializesCampaignConfigs()
    {
        string scene = File.ReadAllText("Assets/Game/Scenes/Menu.unity");
        Assert.That(scene, Does.Contain("campaignMissionDefinition: {fileID: 11400000, guid: 7284111cf4349bf4bb7bb0faa0b53619"));
        Assert.That(scene, Does.Contain("campaignScenarioSetup: {fileID: 11400000, guid: ccf43b60d0265424291475c15a79ef9a"));
        Assert.That(scene, Does.Contain("campaignOperationMapCatalog: {fileID: 11400000, guid: f5eb5c2d2e932c548a01876109d52b46"));
    }

    [Test] public static void SharedPhysicalMapReusesLogicalMissionRequest()
    {
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(
            "Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M01_FirstContact.asset");
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset");
        OperationMapCatalogConfig maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
            "Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset");
        OperationMapDefinition physical = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("M01 production bootstrap");
        GameObject menuObject = new("M01 production Menu bootstrap");
        menuObject.SetActive(false);
        CampaignMissionMenuBootstrapRuntime menuBootstrap = new();
        Entity missionRoot = Entity.Null;
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            MenuBootstrapView view = menuObject.AddComponent<MenuBootstrapView>();
            view.Configure(null, null, null, null, null, null,
                configuredCampaignMissionDefinition: mission,
                configuredCampaignScenarioSetup: scenario,
                configuredCampaignOperationMapCatalog: maps);
            menuBootstrap.Update(view);

            using EntityQuery missionQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>());
            Assert.That(missionQuery.CalculateEntityCount(), Is.EqualTo(1));
            missionRoot = missionQuery.GetSingletonEntity();
            MissionLaunchPayload payload = FirstLaunchMissionHandoffOperation.Prepare(
                new PlayerProfileSaveData(), 41, NarrativeGuidanceMode.Full);
            world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot).Add(
                FirstLaunchMissionHandoffOperation.ToRequest(payload));
            menuBootstrap.Update(view);

            using OperationMapRuntimeBootstrapSceneSystemHelper matchBootstrap = new(world);
            Assert.That(matchBootstrap.TryPublish(
                physical, new FixedString64Bytes(payload.ScenarioId), new FixedString64Bytes(payload.MissionId),
                1, OperationMapReadinessFlags.Metadata, OperationMapReadinessFlags.Metadata,
                out Entity mapRoot, out string error), Is.True, error);
            Assert.That(world.EntityManager.GetComponentData<ActiveOperationMapComponent>(mapRoot).OperationMapId,
                Is.EqualTo(new FixedString64Bytes(payload.OperationMapId)));
        }
        finally
        {
            menuBootstrap.Shutdown();
            if (missionRoot != Entity.Null && world.EntityManager.Exists(missionRoot))
            {
                CampaignMissionCatalogComponent catalog =
                    world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(missionRoot);
                CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
                world.EntityManager.SetComponentData(missionRoot, catalog);
            }
            UnityEngine.Object.DestroyImmediate(menuObject);
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private static World WorldWithRoot(out Entity root)
    {
        World world = new("M01 FirstLaunch handoff"); root = world.EntityManager.CreateEntity(typeof(CampaignMissionRootComponent));
        world.EntityManager.AddBuffer<CampaignMissionLaunchRequestElement>(root); world.EntityManager.AddBuffer<CampaignMissionLaunchResultElement>(root); return world;
    }
}
#endif
