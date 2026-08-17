using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class AssistantTargetLockReadModelSystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private SystemHandle _system;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.PublishesExecutableTargetTelemetryFromRecommendation());
            passed++;
            RunCase(test => test.PreviewChangesLockStateWithoutChangingTarget());
            passed++;
            UnityEngine.Debug.Log($"[AssistantTargetLockReadModelValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError($"[AssistantTargetLockReadModelValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(System.Action<AssistantTargetLockReadModelSystemTests> testCase)
    {
        AssistantTargetLockReadModelSystemTests tests = new();
        tests.SetUp();
        try { testCase(tests); }
        finally { tests.TearDown(); }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World(nameof(AssistantTargetLockReadModelSystemTests));
        _entityManager = _world.EntityManager;
        _system = _world.CreateSystem<AssistantTargetLockReadModelSystem>();
    }

    [TearDown]
    public void TearDown() => _world?.Dispose();

    [Test]
    public void PublishesExecutableTargetTelemetryFromRecommendation()
    {
        Entity source = CreateUnit("RIFLE SQUAD", FactionIdentity.PlayerFactionId, 100, new float3(0f, 0f, 0f));
        Entity target = CreateUnit("HOSTILE CAR", FactionIdentity.EnemyFactionId, 70, new float3(3f, 0f, 4f));
        Entity boundary = CreateBoundary();
        _entityManager.AddBuffer<AssistantRecommendationElement>(boundary).Add(new AssistantRecommendationElement
        {
            RecommendationId = 7,
            SourceVersion = 2,
            Kind = AssistantRecommendationKind.Attack,
            TargetKind = AssistantTargetKind.Entity,
            SourceEntity = source,
            TargetEntity = target,
            CanExecute = 1,
            CanShow = 1,
            Reason = new FixedString128Bytes("Verified hostile target")
        });

        _system.Update(_world.Unmanaged);

        AssistantTargetLockReadModelComponent model =
            _entityManager.GetComponentData<AssistantTargetLockReadModelComponent>(boundary);
        Assert.AreEqual(1, model.Visible);
        Assert.AreEqual(AssistantTargetLockState.Executable, model.State);
        Assert.AreEqual("RIFLE SQUAD", model.SourceName.ToString());
        Assert.AreEqual("HOSTILE CAR", model.TargetName.ToString());
        Assert.AreEqual(AssistantFactionRelation.Hostile, model.FactionRelation);
        Assert.AreEqual(1, model.HasDistance);
        Assert.AreEqual(5f, model.Distance, 0.001f);
        Assert.AreEqual(70, model.HealthCurrent);
        Assert.AreEqual(100, model.HealthMax);
    }

    [Test]
    public void PreviewChangesLockStateWithoutChangingTarget()
    {
        Entity target = CreateUnit("HOSTILE CAR", FactionIdentity.EnemyFactionId, 70, new float3(2f, 0f, 1f));
        Entity boundary = CreateBoundary();
        _entityManager.AddBuffer<AssistantRecommendationElement>(boundary).Add(new AssistantRecommendationElement
        {
            RecommendationId = 8,
            SourceVersion = 2,
            Kind = AssistantRecommendationKind.CameraFocus,
            TargetKind = AssistantTargetKind.Entity,
            TargetEntity = target,
            CanShow = 1
        });
        _entityManager.AddBuffer<AssistantPreviewHighlightElement>(boundary).Add(new AssistantPreviewHighlightElement
        {
            RequestId = 10,
            RecommendationId = 8,
            TargetKind = AssistantTargetKind.Entity,
            TargetEntity = target,
            WorldPosition = new float3(2f, 0f, 1f),
            Active = 1
        });

        _system.Update(_world.Unmanaged);

        Assert.AreEqual(
            AssistantTargetLockState.Preview,
            _entityManager.GetComponentData<AssistantTargetLockReadModelComponent>(boundary).State);
        Assert.AreEqual(
            string.Empty,
            _entityManager.GetComponentData<AssistantTargetLockReadModelComponent>(boundary).SourceName.ToString(),
            "A target-only hostile preview must not be mislabeled as a friendly source.");
    }

    private Entity CreateBoundary()
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(UiMatchHudHeaderComponent));
        _entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            ActiveRoute = UIRoute.Match,
            CurrentMode = UiShellMode.MatchHud,
            Phase = UiShellTransitionPhase.MatchHudReady
        });
        Entity start = _entityManager.CreateEntity(typeof(MatchStartQueueComponent));
        _entityManager.SetComponentData(start, new MatchStartQueueComponent
        {
            HasStarted = 1,
            LastStatus = MatchStartStatusKind.Started
        });
        return boundary;
    }

    private Entity CreateUnit(string name, byte factionId, int health, float3 position)
    {
        Entity entity = _entityManager.CreateEntity(
            typeof(UnitDisplayInfo),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform));
        _entityManager.SetComponentData(entity, new UnitDisplayInfo { Name = new FixedString64Bytes(name) });
        _entityManager.SetComponentData(entity, new Faction { Id = factionId });
        _entityManager.SetComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        _entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
