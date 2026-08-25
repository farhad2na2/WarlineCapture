#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseObjectiveWriterTests
{
    private const string Marker = "[M02EstablishBaseObjectiveWriterValidation] result=Passed tests=8";

    [MenuItem("Game/Validation/Run M02 Establish Base Objective Writer Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseObjectiveWriterTests tests = new();
            tests.CanonicalDefinitionProjectsThreeObjectivesInAuthoredOrder();
            tests.UnboundForwardPostProjectsBlockedThenBecomesActive();
            tests.BuildAndProductionFactsCompleteOnlyTheirObjectives();
            tests.ForwardPostDamageAndDestructionProjectWarningAndFailure();
            tests.DuplicateUpdatesAndRegressiveFactsDoNotAdvanceVersion();
            tests.NewAttemptResetsProgressWithoutRegressingBoundaryVersion();
            tests.MissionTransitionReplacesM01RowsWithM02Rows();
            tests.SourceMismatchAndDuplicateDefinitionsFailClosed();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseObjectiveWriterValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void CanonicalDefinitionProjectsThreeObjectivesInAuthoredOrder()
    {
        using ObjectiveFixture fixture = new();
        fixture.Run();

        MatchObjectiveRuntimeStateComponent state = fixture.State;
        Assert.AreEqual(1u, state.Version);
        Assert.AreEqual(17u, state.MissionCatalogSourceVersion);
        Assert.AreEqual(1u, state.MissionSourceVersion);
        Assert.AreEqual(3, fixture.Objectives.Length);
        AssertObjective(
            fixture.Objectives[0],
            "obj.ch01.m02.build_forward_barracks",
            "anchor.ch01.m02.build_lot",
            "Build the forward Barracks",
            "Barracks completed 0/1",
            MatchObjectiveState.Active,
            2,
            1);
        AssertObjective(
            fixture.Objectives[1],
            "obj.ch01.m02.produce_rifle_squad",
            "anchor.ch01.m02.forward_post",
            "Produce a rifle squad",
            "Rifle squads ready 0/1",
            MatchObjectiveState.Active,
            3,
            0);
        AssertObjective(
            fixture.Objectives[2],
            "obj.ch01.m02.defend_forward_post",
            "anchor.ch01.m02.forward_post",
            "Defend the forward post",
            "Forward post operational",
            MatchObjectiveState.Active,
            4,
            0);
        Assert.AreEqual(1, fixture.Objectives[2].ProtectsTarget);
    }

    [Test]
    public void UnboundForwardPostProjectsBlockedThenBecomesActive()
    {
        using ObjectiveFixture fixture = new(forwardPostBound: false);
        fixture.Run();
        Assert.AreEqual(MatchObjectiveState.Blocked, fixture.Objectives[2].State);
        Assert.AreEqual("Forward post unavailable", fixture.Objectives[2].Body.ToString());

        CampaignMissionAttemptFactsComponent facts = fixture.Facts;
        facts.ForwardPostBound = 1;
        fixture.Facts = facts;
        fixture.Run();
        Assert.AreEqual(2u, fixture.State.Version);
        Assert.AreEqual(MatchObjectiveState.Active, fixture.Objectives[2].State);
    }

    [Test]
    public void BuildAndProductionFactsCompleteOnlyTheirObjectives()
    {
        using ObjectiveFixture fixture = new();
        fixture.Run();

        CampaignMissionAttemptFactsComponent facts = fixture.Facts;
        facts.RequiredBuildingCompletedCount = 1;
        fixture.Facts = facts;
        fixture.Run();
        Assert.AreEqual(MatchObjectiveState.Complete, fixture.Objectives[0].State);
        Assert.AreEqual(MatchObjectiveState.Active, fixture.Objectives[1].State);
        Assert.AreEqual(MatchObjectiveState.Active, fixture.Objectives[2].State);

        facts.RequiredUnitProducedCount = 1;
        fixture.Facts = facts;
        fixture.Run();
        Assert.AreEqual(3u, fixture.State.Version);
        Assert.AreEqual(MatchObjectiveState.Complete, fixture.Objectives[0].State);
        Assert.AreEqual(MatchObjectiveState.Complete, fixture.Objectives[1].State);
        Assert.AreEqual(MatchObjectiveState.Active, fixture.Objectives[2].State);
    }

    [Test]
    public void ForwardPostDamageAndDestructionProjectWarningAndFailure()
    {
        using ObjectiveFixture fixture = new();
        fixture.Run();

        CampaignMissionAttemptFactsComponent facts = fixture.Facts;
        facts.ForwardPostDamaged = 1;
        facts.DefenseWaveActivated = 1;
        fixture.Facts = facts;
        fixture.Run();
        Assert.AreEqual(MatchObjectiveState.Warning, fixture.Objectives[2].State);
        Assert.AreEqual("Forward post under attack", fixture.Objectives[2].Body.ToString());

        facts.ForwardPostDestroyed = 1;
        fixture.Facts = facts;
        fixture.Run();
        Assert.AreEqual(MatchObjectiveState.Failed, fixture.Objectives[2].State);
        Assert.AreEqual("Forward post destroyed", fixture.Objectives[2].Body.ToString());
    }

    [Test]
    public void DuplicateUpdatesAndRegressiveFactsDoNotAdvanceVersion()
    {
        using ObjectiveFixture fixture = new();
        CampaignMissionAttemptFactsComponent facts = fixture.Facts;
        facts.RequiredBuildingCompletedCount = 1;
        facts.RequiredUnitProducedCount = 1;
        facts.ForwardPostDamaged = 1;
        fixture.Facts = facts;
        fixture.Run();
        uint publishedVersion = fixture.State.Version;

        fixture.Run();
        Assert.AreEqual(publishedVersion, fixture.State.Version);

        facts.RequiredBuildingCompletedCount = 0;
        facts.RequiredUnitProducedCount = 0;
        facts.ForwardPostDamaged = 0;
        fixture.Facts = facts;
        fixture.Run();
        Assert.AreEqual(publishedVersion, fixture.State.Version);
        Assert.AreEqual(MatchObjectiveState.Complete, fixture.Objectives[0].State);
        Assert.AreEqual(MatchObjectiveState.Complete, fixture.Objectives[1].State);
        Assert.AreEqual(MatchObjectiveState.Warning, fixture.Objectives[2].State);
    }

    [Test]
    public void NewAttemptResetsProgressWithoutRegressingBoundaryVersion()
    {
        using ObjectiveFixture fixture = new();
        CampaignMissionAttemptFactsComponent facts = fixture.Facts;
        facts.RequiredBuildingCompletedCount = 1;
        facts.RequiredUnitProducedCount = 1;
        fixture.Facts = facts;
        fixture.Run();

        CampaignMissionRuntimeComponent runtime = fixture.Runtime;
        runtime.SessionToken = "m02-objective-attempt-2";
        runtime.AttemptOrdinal = 2;
        runtime.Version = 2;
        fixture.Runtime = runtime;
        facts = fixture.Facts;
        facts.RequiredBuildingCompletedCount = 0;
        facts.RequiredUnitProducedCount = 0;
        fixture.Facts = facts;
        fixture.Run();

        Assert.AreEqual(2u, fixture.State.Version);
        Assert.AreEqual(2, fixture.State.AttemptOrdinal);
        Assert.AreEqual(MatchObjectiveState.Active, fixture.Objectives[0].State);
        Assert.AreEqual(MatchObjectiveState.Active, fixture.Objectives[1].State);
    }

    [Test]
    public void MissionTransitionReplacesM01RowsWithM02Rows()
    {
        using ObjectiveFixture fixture = new(startWithM01: true);
        fixture.Run();
        Assert.AreEqual(2, fixture.Objectives.Length);
        Assert.AreEqual("obj.ch01.m01.destroy_patrol", fixture.Objectives[0].ObjectiveId.ToString());

        fixture.SwitchToM02();
        fixture.Run();
        Assert.AreEqual(2u, fixture.State.Version);
        Assert.AreEqual(3, fixture.Objectives.Length);
        Assert.AreEqual("obj.ch01.m02.build_forward_barracks", fixture.Objectives[0].ObjectiveId.ToString());
        Assert.AreEqual("obj.ch01.m02.defend_forward_post", fixture.Objectives[2].ObjectiveId.ToString());
    }

    [Test]
    public void SourceMismatchAndDuplicateDefinitionsFailClosed()
    {
        using (ObjectiveFixture sourceMismatch = new(sourceMismatch: true))
        {
            sourceMismatch.Run();
            Assert.IsFalse(sourceMismatch.HasProjection);
        }

        using ObjectiveFixture duplicateDefinition = new(duplicateM02ObjectiveIds: true);
        duplicateDefinition.Run();
        Assert.IsFalse(duplicateDefinition.HasProjection);
    }

    private static void AssertObjective(
        MatchObjectiveRuntimeElement objective,
        string objectiveId,
        string anchorId,
        string title,
        string body,
        MatchObjectiveState state,
        byte priority,
        byte primary)
    {
        Assert.AreEqual(objectiveId, objective.ObjectiveId.ToString());
        Assert.AreEqual(anchorId, objective.OperationMapAnchorId.ToString());
        Assert.AreEqual(title, objective.Title.ToString());
        Assert.AreEqual(body, objective.Body.ToString());
        Assert.AreEqual(state, objective.State);
        Assert.AreEqual(priority, objective.Priority);
        Assert.AreEqual(primary, objective.IsPrimary);
    }

    private sealed class ObjectiveFixture : IDisposable
    {
        private const uint CatalogSourceVersion = 17;
        private readonly World _world;
        private readonly SystemHandle _writer;

        internal ObjectiveFixture(
            bool forwardPostBound = true,
            bool startWithM01 = false,
            bool sourceMismatch = false,
            bool duplicateM02ObjectiveIds = false)
        {
            _world = new World(nameof(M02EstablishBaseObjectiveWriterTests));
            EntityManager = _world.EntityManager;
            Root = EntityManager.CreateEntity(
                typeof(CampaignMissionRootComponent),
                typeof(CampaignMissionCatalogComponent),
                typeof(CampaignMissionRuntimeComponent),
                typeof(CampaignMissionAttemptFactsComponent));
            EntityManager.SetComponentData(Root, CreateCatalog(duplicateM02ObjectiveIds));
            EntityManager.SetComponentData(
                Root,
                startWithM01 ? CreateM01Runtime() : CreateM02Runtime(sourceMismatch));
            EntityManager.SetComponentData(
                Root,
                startWithM01 ? CreateM01Facts() : CreateM02Facts(forwardPostBound));
            Boundary = EntityManager.CreateEntity(typeof(MatchObjectiveProjectionBoundaryComponent));
            _world.GetOrCreateSystem<CampaignMissionCatalogDisposalSystem>();
            _writer = _world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
        }

        internal EntityManager EntityManager { get; }
        internal Entity Root { get; }
        internal Entity Boundary { get; }
        internal bool HasProjection =>
            EntityManager.HasComponent<MatchObjectiveRuntimeStateComponent>(Boundary) &&
            EntityManager.HasBuffer<MatchObjectiveRuntimeElement>(Boundary);
        internal MatchObjectiveRuntimeStateComponent State =>
            EntityManager.GetComponentData<MatchObjectiveRuntimeStateComponent>(Boundary);
        internal DynamicBuffer<MatchObjectiveRuntimeElement> Objectives =>
            EntityManager.GetBuffer<MatchObjectiveRuntimeElement>(Boundary);
        internal CampaignMissionRuntimeComponent Runtime
        {
            get => EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(Root);
            set => EntityManager.SetComponentData(Root, value);
        }
        internal CampaignMissionAttemptFactsComponent Facts
        {
            get => EntityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(Root);
            set => EntityManager.SetComponentData(Root, value);
        }

        internal void Run()
        {
            _world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(_writer)
                .OnUpdate(ref _world.Unmanaged.ResolveSystemStateRef(_writer));
        }

        internal void SwitchToM02()
        {
            CampaignMissionRuntimeComponent runtime = CreateM02Runtime(sourceMismatch: false);
            runtime.Version = 2;
            runtime.AttemptOrdinal = 2;
            runtime.SessionToken = "m02-objective-transition";
            Runtime = runtime;
            Facts = CreateM02Facts(forwardPostBound: true);
        }

        public void Dispose() => _world.Dispose();

        private static CampaignMissionCatalogComponent CreateCatalog(bool duplicateM02ObjectiveIds)
        {
            using BlobBuilder builder = new(Allocator.Temp);
            ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            catalog.SchemaVersion = 1;
            BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 2);
            BuildM01Definition(builder, ref missions[0]);
            BuildM02Definition(builder, ref missions[1], duplicateM02ObjectiveIds);
            return new CampaignMissionCatalogComponent
            {
                Blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent),
                SourceVersion = CatalogSourceVersion,
                OwnsBlob = 1
            };
        }

        private static void BuildM01Definition(
            BlobBuilder builder,
            ref CampaignMissionDefinitionBlob definition)
        {
            definition.MissionId = "saga.ch01.m01.first_contact";
            definition.ScenarioId = "scenario.ch01.m01.first_contact";
            definition.OperationMapId = "opmap.ch01.district_edge_01";
            BlobBuilderArray<CampaignMissionObjectiveBlob> objectives =
                builder.Allocate(ref definition.Objectives, 2);
            objectives[0] = new CampaignMissionObjectiveBlob
            {
                ObjectiveId = "obj.ch01.m01.destroy_patrol",
                DisplayTextKey = "mission.m01.objective.secure_corridor",
                MissionRoleId = "role.hostile.patrol",
                Rule = MissionObjectiveRuleKind.DestroyMissionRole,
                RequiredCount = 3
            };
            objectives[1] = new CampaignMissionObjectiveBlob
            {
                ObjectiveId = "obj.ch01.m01.keep_command_squad_alive",
                DisplayTextKey = "mission.m01.failure.command_squad_destroyed",
                MissionRoleId = "role.friendly.command_squad",
                Rule = MissionObjectiveRuleKind.ProtectMissionRole,
                RequiredCount = 1,
                FailureOnRuleBreak = 1
            };
        }

        private static void BuildM02Definition(
            BlobBuilder builder,
            ref CampaignMissionDefinitionBlob definition,
            bool duplicateObjectiveIds)
        {
            definition.MissionId = "saga.ch01.m02.establish_base";
            definition.ScenarioId = "scenario.ch01.m02.establish_base";
            definition.OperationMapId = "opmap.ch01.forward_post_01";
            definition.BaseMissionRoleId = "role.friendly.forward_post";
            definition.BaseAnchorId = "anchor.ch01.m02.forward_post";
            definition.BuildZone = new CampaignMissionBuildZoneBlob
            {
                AnchorId = "anchor.ch01.m02.build_lot",
                HalfWidthCells = 12,
                HalfHeightCells = 7
            };
            BlobBuilderArray<CampaignMissionObjectiveBlob> objectives =
                builder.Allocate(ref definition.Objectives, 3);
            objectives[0] = new CampaignMissionObjectiveBlob
            {
                ObjectiveId = "obj.ch01.m02.build_forward_barracks",
                DisplayTextKey = "mission.m02.objective.build_forward_barracks",
                TargetConfigId = "Building_Barrack",
                Rule = MissionObjectiveRuleKind.BuildStructure,
                RequiredCount = 1
            };
            objectives[1] = new CampaignMissionObjectiveBlob
            {
                ObjectiveId = duplicateObjectiveIds
                    ? "obj.ch01.m02.build_forward_barracks"
                    : "obj.ch01.m02.produce_rifle_squad",
                DisplayTextKey = "mission.m02.objective.produce_rifle_squad",
                TargetConfigId = "Unit_Chr_Soldier_Male_02_Alt_04",
                Rule = MissionObjectiveRuleKind.ProduceUnit,
                RequiredCount = 1
            };
            objectives[2] = new CampaignMissionObjectiveBlob
            {
                ObjectiveId = "obj.ch01.m02.defend_forward_post",
                DisplayTextKey = "mission.m02.objective.defend_forward_post",
                MissionRoleId = "role.friendly.forward_post",
                Rule = MissionObjectiveRuleKind.DefendMissionRole,
                RequiredCount = 1,
                FailureOnRuleBreak = 1
            };
        }

        private static CampaignMissionRuntimeComponent CreateM01Runtime() => new()
        {
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
            OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
            SessionToken = new FixedString64Bytes("m01-objective-transition"),
            Phase = MissionPhaseKind.Engage,
            LaunchOrigin = MissionLaunchOriginKind.FirstLaunch,
            RunKind = MissionRunKind.FirstClear,
            Version = 1,
            SourceVersion = CatalogSourceVersion,
            AttemptOrdinal = 1,
            DeterministicSeed = 1701
        };

        private static CampaignMissionRuntimeComponent CreateM02Runtime(bool sourceMismatch) => new()
        {
            MissionId = new FixedString64Bytes("saga.ch01.m02.establish_base"),
            ScenarioId = new FixedString64Bytes("scenario.ch01.m02.establish_base"),
            OperationMapId = new FixedString64Bytes("opmap.ch01.forward_post_01"),
            SessionToken = new FixedString64Bytes("m02-objective-attempt-1"),
            Phase = MissionPhaseKind.Engage,
            LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
            RunKind = MissionRunKind.FirstClear,
            Version = 1,
            SourceVersion = sourceMismatch ? 99u : CatalogSourceVersion,
            AttemptOrdinal = 1,
            DeterministicSeed = 1702
        };

        private static CampaignMissionAttemptFactsComponent CreateM01Facts() => new()
        {
            ElapsedMilliseconds = 45000,
            HostileTotalCount = 3,
            HostileDefeatedCount = 1,
            CommandSquadSpawned = 1,
            CommandSquadAlive = 1
        };

        private static CampaignMissionAttemptFactsComponent CreateM02Facts(bool forwardPostBound) => new()
        {
            ElapsedMilliseconds = 30000,
            HostileTotalCount = 3,
            HostileDefeatedCount = 0,
            ForwardPostBound = forwardPostBound ? (byte)1 : (byte)0
        };
    }
}
#endif
