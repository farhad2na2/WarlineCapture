using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class M01FirstContactResultRuleTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AllFiveStarCombinationsAreDeterministic()); passed++;
            RunCase(test => test.FourMinuteBoundaryNeverFailsMission()); passed++;
            RunCase(test => test.ResultCarriesAttemptIdentityAndReturnRoute()); passed++;
            RunCase(test => test.RepeatedUpdateIsImmutableAndQueuesSettlementOnce()); passed++;
            RunCase(test => test.NewAttemptProjectsWithoutMutatingPriorBestData()); passed++;
            RunCase(test => test.InvalidOrContradictoryFactsFailClosed()); passed++;
            AuthoredRulesRemainTheOnlyThresholdAuthority(); passed++;
            ResultUiIsReadOnlyAndProjectionHasOneWriter(); passed++;
            Debug.Log($"[M01FirstContactResultRuleValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01FirstContactResultRuleValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<M01FirstContactResultRuleTests> testCase) =>
        testCase(new M01FirstContactResultRuleTests());

    [Test]
    public void AllFiveStarCombinationsAreDeterministic()
    {
        AssertResult(MissionOutcomeKind.Victory, 239999, 0, 3);
        AssertResult(MissionOutcomeKind.Victory, 240000, 0, 2);
        AssertResult(MissionOutcomeKind.Victory, 239999, 1, 2);
        AssertResult(MissionOutcomeKind.Victory, 240001, 1, 1);
        AssertResult(MissionOutcomeKind.Defeat, 120000, 1, 0);
    }

    [Test]
    public void FourMinuteBoundaryNeverFailsMission()
    {
        using Fixture fixture = CreateFixture(MissionOutcomeKind.Victory, 240000, 0);
        CampaignMissionRuntimeComponent before = fixture.EntityManager.GetComponentData<
            CampaignMissionRuntimeComponent>(fixture.Root);
        fixture.Update();
        CampaignMissionResultComponent result = fixture.EntityManager.GetComponentData<
            CampaignMissionResultComponent>(fixture.Root);
        Assert.That(result.Outcome, Is.EqualTo(MissionOutcomeKind.Victory));
        Assert.That(result.Stars, Is.EqualTo(2));
        Assert.That(fixture.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(fixture.Root),
            Is.EqualTo(before));
    }

    [Test]
    public void ResultCarriesAttemptIdentityAndReturnRoute()
    {
        using Fixture fixture = CreateFixture(MissionOutcomeKind.Victory, 70000, 0);
        fixture.Update();
        CampaignMissionResultComponent result = fixture.EntityManager.GetComponentData<
            CampaignMissionResultComponent>(fixture.Root);
        Assert.That(result.MissionId.ToString(), Is.EqualTo("saga.ch01.m01.first_contact"));
        Assert.That(result.SessionToken.ToString(), Is.EqualTo("session-result-tests"));
        Assert.That(result.AttemptOrdinal, Is.EqualTo(1));
        Assert.That(result.SourceVersion, Is.EqualTo(8));
        Assert.That(result.ReturnDestination, Is.EqualTo(MissionReturnDestinationKind.CommandBase));
        Assert.That(result.ElapsedMilliseconds, Is.EqualTo(70000));
        Assert.That(result.SquadLossCount, Is.Zero);
    }

    [Test]
    public void RepeatedUpdateIsImmutableAndQueuesSettlementOnce()
    {
        using Fixture fixture = CreateFixture(MissionOutcomeKind.Victory, 90000, 0);
        fixture.Update();
        CampaignMissionResultComponent first = fixture.EntityManager.GetComponentData<
            CampaignMissionResultComponent>(fixture.Root);
        fixture.Update();
        Assert.That(fixture.EntityManager.GetComponentData<CampaignMissionResultComponent>(fixture.Root),
            Is.EqualTo(first));
        DynamicBuffer<CampaignMissionSettlementRequestElement> requests =
            fixture.EntityManager.GetBuffer<CampaignMissionSettlementRequestElement>(fixture.Root);
        Assert.That(requests.Length, Is.EqualTo(1));
        Assert.That(requests[0].SessionToken, Is.EqualTo(first.SessionToken));
        Assert.That(requests[0].AttemptOrdinal, Is.EqualTo(first.AttemptOrdinal));
    }

    [Test]
    public void NewAttemptProjectsWithoutMutatingPriorBestData()
    {
        using Fixture fixture = CreateFixture(MissionOutcomeKind.Victory, 90000, 0);
        Entity bestData = fixture.EntityManager.CreateEntity(typeof(CampaignMissionResultComponent));
        CampaignMissionResultComponent priorBest = new()
        {
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            SessionToken = new FixedString64Bytes("settled-prior-attempt"), AttemptOrdinal = 4,
            SourceVersion = 12, Outcome = MissionOutcomeKind.Victory, Stars = 3,
            ElapsedMilliseconds = 60000
        };
        fixture.EntityManager.SetComponentData(bestData, priorBest);
        fixture.Update();
        Assert.That(fixture.EntityManager.GetComponentData<CampaignMissionResultComponent>(bestData),
            Is.EqualTo(priorBest));
        Assert.That(fixture.EntityManager.GetComponentData<CampaignMissionResultComponent>(fixture.Root)
            .AttemptOrdinal, Is.EqualTo(1));
    }

    [Test]
    public void InvalidOrContradictoryFactsFailClosed()
    {
        using Fixture fixture = CreateFixture(MissionOutcomeKind.Victory, 1000, 0);
        CampaignMissionAttemptFactsComponent facts = fixture.EntityManager.GetComponentData<
            CampaignMissionAttemptFactsComponent>(fixture.Root);
        facts.CommandSquadAlive = 0;
        fixture.EntityManager.SetComponentData(fixture.Root, facts);
        fixture.Update();
        Assert.That(fixture.EntityManager.HasComponent<CampaignMissionResultComponent>(fixture.Root), Is.False);
        Assert.That(fixture.EntityManager.GetBuffer<CampaignMissionSettlementRequestElement>(fixture.Root).Length,
            Is.Zero);
    }

    [Test]
    public static void AuthoredRulesRemainTheOnlyThresholdAuthority()
    {
        string source = File.ReadAllText(
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionResultProjectionSystem.cs");
        StringAssert.DoesNotContain("240000", source);
        StringAssert.Contains("definition.StarRules", source);
        string projection = File.ReadAllText(
            "Assets/Game/Scripts/Composition/CampaignMissionCatalogProjection.cs");
        StringAssert.Contains("mission.Stars", projection);
    }

    [Test]
    public static void ResultUiIsReadOnlyAndProjectionHasOneWriter()
    {
        string[] uiWrites = Directory.GetFiles("Assets/Game/Scripts/UI", "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("SetComponentData<CampaignMissionResultComponent>",
                StringComparison.Ordinal) || File.ReadAllText(path).Contains(
                "AddComponentData(root, new CampaignMissionResultComponent", StringComparison.Ordinal))
            .ToArray();
        Assert.That(uiWrites, Is.Empty);
        string[] writers = Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("new CampaignMissionResultComponent", StringComparison.Ordinal))
            .Select(path => path.Replace('\\', '/')).ToArray();
        CollectionAssert.AreEqual(new[]
        {
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionResultProjectionSystem.cs"
        }, writers);
    }

    private static void AssertResult(
        MissionOutcomeKind outcome, int elapsedMilliseconds, int losses, byte expectedStars)
    {
        using Fixture fixture = CreateFixture(outcome, elapsedMilliseconds, losses);
        fixture.Update();
        CampaignMissionResultComponent result = fixture.EntityManager.GetComponentData<
            CampaignMissionResultComponent>(fixture.Root);
        Assert.That(result.Stars, Is.EqualTo(expectedStars));
        Assert.That(result.Outcome, Is.EqualTo(outcome));
    }

    private static Fixture CreateFixture(MissionOutcomeKind outcome, int elapsedMilliseconds, int losses) =>
        new(outcome, elapsedMilliseconds, losses);

    private sealed class Fixture : IDisposable
    {
        private readonly BlobAssetReference<CampaignMissionCatalogBlob> _blob;
        private readonly World _world;
        private readonly SystemHandle _system;
        public EntityManager EntityManager => _world.EntityManager;
        public Entity Root { get; }

        public Fixture(MissionOutcomeKind outcome, int elapsedMilliseconds, int losses)
        {
            _world = new(nameof(M01FirstContactResultRuleTests));
            _blob = CreateBlob();
            Root = EntityManager.CreateEntity(
                typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent),
                typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
            EntityManager.AddBuffer<CampaignMissionSettlementRequestElement>(Root);
            EntityManager.SetComponentData(Root, new CampaignMissionCatalogComponent
            {
                Blob = _blob, SourceVersion = 7, OwnsBlob = 0
            });
            MissionReturnDestinationKind destination = outcome == MissionOutcomeKind.Victory
                ? MissionReturnDestinationKind.CommandBase : MissionReturnDestinationKind.CampaignOperations;
            EntityManager.SetComponentData(Root, new CampaignMissionRuntimeComponent
            {
                MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
                ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
                OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
                SessionToken = new FixedString64Bytes("session-result-tests"),
                Phase = MissionPhaseKind.Result, Outcome = outcome, ReturnDestination = destination,
                LaunchOrigin = MissionLaunchOriginKind.FirstLaunch, RunKind = MissionRunKind.FirstClear,
                Version = 8, SourceVersion = 7, AttemptOrdinal = 1, DeterministicSeed = 7001
            });
            EntityManager.SetComponentData(Root, new CampaignMissionAttemptFactsComponent
            {
                ElapsedMilliseconds = elapsedMilliseconds, SquadLossCount = losses,
                HostileTotalCount = 3, HostileDefeatedCount = outcome == MissionOutcomeKind.Victory ? 3 : 1,
                CommandSquadSpawned = 1, CommandSquadAlive = outcome == MissionOutcomeKind.Victory ? (byte)1 : (byte)0
            });
            _system = _world.GetOrCreateSystem<CampaignMissionResultProjectionSystem>();
        }

        public void Update() => _world.Unmanaged.GetUnsafeSystemRef<CampaignMissionResultProjectionSystem>(_system)
            .OnUpdate(ref _world.Unmanaged.ResolveSystemStateRef(_system));

        public void Dispose()
        {
            _world.Dispose();
            if (_blob.IsCreated) _blob.Dispose();
        }

        private static BlobAssetReference<CampaignMissionCatalogBlob> CreateBlob()
        {
            BlobBuilder builder = new(Allocator.Temp);
            ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            BlobBuilderArray<CampaignMissionDefinitionBlob> definitions = builder.Allocate(ref catalog.Missions, 1);
            ref CampaignMissionDefinitionBlob definition = ref definitions[0];
            definition.MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact");
            definition.ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact");
            definition.OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01");
            BlobBuilderArray<CampaignMissionStarRuleBlob> stars = builder.Allocate(ref definition.StarRules, 3);
            stars[0] = new CampaignMissionStarRuleBlob { StarIndex = 1, Rule = MissionStarRuleKind.CompleteMission };
            stars[1] = new CampaignMissionStarRuleBlob { StarIndex = 2, Rule = MissionStarRuleKind.NoSquadLoss };
            stars[2] = new CampaignMissionStarRuleBlob
            {
                StarIndex = 3, Rule = MissionStarRuleKind.CompleteUnderMilliseconds, Threshold = 240000
            };
            BlobAssetReference<CampaignMissionCatalogBlob> blob =
                builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
    }
}
