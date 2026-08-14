#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public static class M01FirstContactLaunchBootstrapTests
{
    private const string Marker = "[M01FirstContactLaunchBootstrapValidation] result=Passed tests=10";
    private const string MissionPath = "Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M01_FirstContact.asset";
    private const string ScenarioPath = "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset";
    private const string MapCatalogPath = "Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset";

    public static void RunFocusedValidation()
    {
        try
        {
            Projection_CreatesValidatedOwnedCatalog();
            Projection_SameVersionIsIdempotent();
            Projection_ReplacementTransfersOwnership();
            Projection_InvalidInputsFailClosed();
            EqualRequests_ProduceEqualRuntimeSetup();
            BothOrigins_UseOneLaunchSystem();
            ReadinessPending_PreservesRequest();
            ReadinessFailure_PublishesBoundedRejection();
            ActiveMapMismatch_RejectsSkirmishShortcut();
            ReloadWithNewGeneration_ResetsAttemptDeterministically();
            WriteReport();
            Debug.Log(Marker);
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M01FirstContactLaunchBootstrapValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void Projection_CreatesValidatedOwnedCatalog()
    {
        using World world = new("m01-projection");
        Require(Project(world.EntityManager, 1, out Entity root), "Canonical projection failed.");
        CampaignMissionCatalogComponent catalog = world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        Require(catalog.OwnsBlob == 1 && catalog.SourceVersion == 1 && catalog.Blob.IsCreated,
            "Projection did not publish owned versioned data.");
        Require(catalog.Blob.Value.Missions.Length == 1 &&
                catalog.Blob.Value.Missions[0].MissionId.Equals(new FixedString64Bytes(MissionDefinitionContractValidation.FirstContactMissionId)),
            "Projection did not publish canonical M01.");
        DisposeCatalog(world.EntityManager, root);
    }

    private static void Projection_SameVersionIsIdempotent()
    {
        using World world = new("m01-idempotent");
        Require(Project(world.EntityManager, 7, out Entity first), "Initial projection failed.");
        BlobAssetReference<CampaignMissionCatalogBlob> blob = world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(first).Blob;
        Require(Project(world.EntityManager, 7, out Entity second) && first == second &&
                world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(second).Blob == blob,
            "Same-version projection replaced its owned blob.");
        DisposeCatalog(world.EntityManager, first);
    }

    private static void Projection_ReplacementTransfersOwnership()
    {
        using World world = new("m01-replace");
        Require(Project(world.EntityManager, 1, out Entity root), "Initial projection failed.");
        BlobAssetReference<CampaignMissionCatalogBlob> prior = world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root).Blob;
        Require(Project(world.EntityManager, 2, out root), "Replacement projection failed.");
        CampaignMissionCatalogComponent current = world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        Require(current.Blob.IsCreated && current.Blob != prior && current.SourceVersion == 2,
            "Replacement did not transfer ownership exactly once.");
        DisposeCatalog(world.EntityManager, root);
    }

    private static void Projection_InvalidInputsFailClosed()
    {
        using World world = new("m01-invalid");
        Load(out MissionDefinitionConfig mission, out ScenarioSetupConfig scenario, out OperationMapCatalogConfig maps);
        Require(!CampaignMissionCatalogProjection.TryProject(
                world.EntityManager, mission, scenario, maps, 0, out Entity root, out _) && root == Entity.Null,
            "Zero source version was accepted.");
        Require(!CampaignMissionCatalogProjection.TryProject(
                world.EntityManager, null, scenario, maps, 1, out root, out _) && root == Entity.Null,
            "Missing mission was accepted.");
    }

    private static void EqualRequests_ProduceEqualRuntimeSetup()
    {
        CampaignMissionRuntimeComponent a = RunAccepted(Request(MissionLaunchOriginKind.FirstLaunch), 1);
        CampaignMissionRuntimeComponent b = RunAccepted(Request(MissionLaunchOriginKind.FirstLaunch), 1);
        Require(SameRuntime(in a, in b), "Equal launch requests produced unequal runtime setup.");
    }

    private static void BothOrigins_UseOneLaunchSystem()
    {
        CampaignMissionRuntimeComponent first = RunAccepted(Request(MissionLaunchOriginKind.FirstLaunch), 1);
        CampaignMissionRuntimeComponent campaign = RunAccepted(Request(MissionLaunchOriginKind.CampaignOperations), 1);
        Require(first.LaunchOrigin == MissionLaunchOriginKind.FirstLaunch &&
                campaign.LaunchOrigin == MissionLaunchOriginKind.CampaignOperations &&
                first.MissionId.Equals(campaign.MissionId) && first.ScenarioId.Equals(campaign.ScenarioId) &&
                first.OperationMapId.Equals(campaign.OperationMapId),
            "Entry origins did not converge on the same canonical setup.");
    }

    private static void ReadinessPending_PreservesRequest()
    {
        using World world = CreateLaunchWorld(Request(MissionLaunchOriginKind.FirstLaunch), 1,
            OperationMapReadinessFlags.Metadata, OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface,
            OperationMapReadinessFlags.None, out Entity missionRoot, out _);
        UpdateLaunch(world);
        Require(world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot).Length == 1 &&
                world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(missionRoot).Length == 0,
            "Pending readiness consumed or rejected the launch.");
        DisposeCatalog(world.EntityManager, missionRoot);
    }

    private static void ReadinessFailure_PublishesBoundedRejection()
    {
        using World world = CreateLaunchWorld(Request(MissionLaunchOriginKind.FirstLaunch), 1,
            OperationMapReadinessFlags.Metadata, OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface,
            OperationMapReadinessFlags.MapSurface, out Entity missionRoot, out _);
        UpdateLaunch(world);
        DynamicBuffer<CampaignMissionLaunchResultElement> results =
            world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(missionRoot);
        Require(results.Length == 1 && results[0].Accepted == 0 &&
                results[0].ReasonCode.Equals(new FixedString64Bytes("operation-map-readiness-failed")),
            "Readiness failure did not publish its bounded recovery reason.");
        DisposeCatalog(world.EntityManager, missionRoot);
    }

    private static void ActiveMapMismatch_RejectsSkirmishShortcut()
    {
        CampaignMissionLaunchRequestElement request = Request(MissionLaunchOriginKind.CampaignOperations);
        using World world = CreateLaunchWorld(request, 1, AllReadiness(), AllReadiness(),
            OperationMapReadinessFlags.None, out Entity missionRoot, out Entity mapRoot);
        ActiveOperationMapComponent active = world.EntityManager.GetComponentData<ActiveOperationMapComponent>(mapRoot);
        active.OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01");
        world.EntityManager.SetComponentData(mapRoot, active);
        UpdateLaunch(world);
        Require(world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(missionRoot)[0].Accepted == 0,
            "Campaign launch reused an inappropriate Skirmish identity shortcut.");
        DisposeCatalog(world.EntityManager, missionRoot);
    }

    private static void ReloadWithNewGeneration_ResetsAttemptDeterministically()
    {
        CampaignMissionLaunchRequestElement retry = Request(MissionLaunchOriginKind.CampaignOperations);
        retry.RunKind = MissionRunKind.Retry;
        retry.AttemptOrdinal = 2;
        CampaignMissionRuntimeComponent runtime = RunAccepted(retry, 2);
        Require(runtime.Version == 1 && runtime.AttemptOrdinal == 2 && runtime.RunKind == MissionRunKind.Retry &&
                runtime.Phase == MissionPhaseKind.Preparing,
            "Reload did not reset the new attempt deterministically.");
    }

    private static CampaignMissionRuntimeComponent RunAccepted(CampaignMissionLaunchRequestElement request, int generation)
    {
        using World world = CreateLaunchWorld(request, generation, AllReadiness(), AllReadiness(),
            OperationMapReadinessFlags.None, out Entity root, out _);
        UpdateLaunch(world);
        Require(world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(root)[0].Accepted == 1,
            "Valid launch was rejected.");
        CampaignMissionRuntimeComponent runtime = world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
        DisposeCatalog(world.EntityManager, root);
        return runtime;
    }

    private static World CreateLaunchWorld(
        CampaignMissionLaunchRequestElement request, int generation,
        OperationMapReadinessFlags ready, OperationMapReadinessFlags required,
        OperationMapReadinessFlags failed, out Entity missionRoot, out Entity mapRoot)
    {
        World world = new("m01-launch");
        Require(Project(world.EntityManager, 1, out missionRoot), "Catalog projection failed.");
        world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot).Add(request);
        mapRoot = world.EntityManager.CreateEntity(typeof(ActiveOperationMapComponent), typeof(OperationMapReadinessComponent));
        world.EntityManager.SetComponentData(mapRoot, new ActiveOperationMapComponent
        {
            OperationMapId = request.OperationMapId, ScenarioId = request.ScenarioId, MissionId = request.MissionId,
            SchemaVersion = 1, ContentVersion = 1, Generation = generation
        });
        world.EntityManager.SetComponentData(mapRoot, new OperationMapReadinessComponent
        {
            Generation = generation, ReadyFlags = ready, RequiredFlags = required, FailedFlags = failed
        });
        return world;
    }

    private static void UpdateLaunch(World world)
    {
        SystemHandle handle = world.CreateSystem<CampaignMissionLaunchSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionLaunchSystem>(handle).OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
        world.DestroySystem(handle);
    }

    private static bool Project(EntityManager manager, uint version, out Entity root)
    {
        Load(out MissionDefinitionConfig mission, out ScenarioSetupConfig scenario, out OperationMapCatalogConfig maps);
        return CampaignMissionCatalogProjection.TryProject(manager, mission, scenario, maps, version, out root, out _);
    }

    private static void Load(out MissionDefinitionConfig mission, out ScenarioSetupConfig scenario,
        out OperationMapCatalogConfig maps)
    {
        mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(MissionPath);
        scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(ScenarioPath);
        maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(MapCatalogPath);
    }

    private static CampaignMissionLaunchRequestElement Request(MissionLaunchOriginKind origin) => new()
    {
        SchemaVersion = 1, MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
        ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
        LaunchOrigin = origin, RunKind = MissionRunKind.FirstClear, Guidance = NarrativeGuidanceMode.Contextual,
        ReplayTutorialEnabled = 1, TransitionToken = 71, SessionToken = new FixedString64Bytes("m01-session"),
        AttemptOrdinal = 0, DeterministicSeed = 104729
    };

    private static OperationMapReadinessFlags AllReadiness() =>
        OperationMapReadinessFlags.SourceContent | OperationMapReadinessFlags.SubScene |
        OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface |
        OperationMapReadinessFlags.AuthoredConversion | OperationMapReadinessFlags.PresentationManifest |
        OperationMapReadinessFlags.RequiredPresentationPreload;

    private static bool SameRuntime(in CampaignMissionRuntimeComponent a, in CampaignMissionRuntimeComponent b) =>
        a.MissionId.Equals(b.MissionId) && a.ScenarioId.Equals(b.ScenarioId) &&
        a.OperationMapId.Equals(b.OperationMapId) && a.SessionToken.Equals(b.SessionToken) &&
        a.Phase == b.Phase && a.LaunchOrigin == b.LaunchOrigin && a.RunKind == b.RunKind &&
        a.Guidance == b.Guidance && a.TransitionToken == b.TransitionToken && a.Version == b.Version &&
        a.SourceVersion == b.SourceVersion && a.AttemptOrdinal == b.AttemptOrdinal &&
        a.DeterministicSeed == b.DeterministicSeed && a.RequiredReadiness == b.RequiredReadiness &&
        a.ReadyReadiness == b.ReadyReadiness;

    private static void DisposeCatalog(EntityManager manager, Entity root)
    {
        CampaignMissionCatalogComponent catalog = manager.GetComponentData<CampaignMissionCatalogComponent>(root);
        CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
        manager.SetComponentData(root, catalog);
    }

    private static void WriteReport()
    {
        const string path = "Design/AgentReports/M01FirstContact/m01dc_018_launch_bootstrap.json";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path,
            "{\n  \"artifactId\": \"m01dc-018-launch-bootstrap-v1\",\n  \"result\": \"Passed\",\n" +
            "  \"launchSystemCount\": 1,\n  \"entryOrigins\": 2,\n  \"boundedFailureReasons\": true,\n" +
            "  \"skirmishShortcutAllowed\": false,\n  \"validation\": \"" + Marker + "\"\n}\n");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
