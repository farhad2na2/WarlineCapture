using Game.Components;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;

public sealed class AssistantCommandResultBridgeSystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private SystemHandle _system;

    [SetUp]
    public void SetUp()
    {
        _world = new World(nameof(AssistantCommandResultBridgeSystemTests));
        _entityManager = _world.EntityManager;
        _system = _world.CreateSystem<AssistantCommandResultBridgeSystem>();
    }

    [TearDown]
    public void TearDown() => _world?.Dispose();

    [Test]
    public void CompletesCorrelatedMoveDispatch()
    {
        Entity boundary = CreateBoundary(new AssistantCommandDispatchElement
        {
            AssistantRequestId = 4,
            RecommendationId = 40,
            IntentKind = AssistantCommandIntentKind.MoveToWorldPosition,
            DownstreamKind = AssistantDownstreamCommandKind.MoveOrder,
            DownstreamRequestId = 9,
            Status = AssistantCommandIntentStatus.Accepted
        });
        Entity queue = _entityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
        _entityManager.AddBuffer<UnitMoveOrderResultElement>(queue).Add(new UnitMoveOrderResultElement
        {
            RequestId = 9,
            Issued = 1
        });

        _system.Update(_world.Unmanaged);

        Assert.AreEqual(
            AssistantCommandIntentStatus.Completed,
            _entityManager.GetBuffer<AssistantCommandDispatchElement>(boundary)[0].Status);
        AssistantCommandIntentResultElement result =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary)[0];
        Assert.AreEqual(4, result.RequestId);
        Assert.AreEqual(AssistantCommandIntentStatus.Completed, result.Status);
    }

    [Test]
    public void TimesOutUnresolvedDispatch()
    {
        Entity boundary = CreateBoundary(new AssistantCommandDispatchElement
        {
            AssistantRequestId = 5,
            RecommendationId = 50,
            IntentKind = AssistantCommandIntentKind.AttackEntity,
            DownstreamKind = AssistantDownstreamCommandKind.AttackOrder,
            DownstreamRequestId = 11,
            Status = AssistantCommandIntentStatus.Accepted,
            RequestedAt = 1f
        });
        _world.SetTime(new TimeData(7d, 0.1f));

        _system.Update(_world.Unmanaged);

        Assert.AreEqual(
            AssistantCommandIntentStatus.TimedOut,
            _entityManager.GetBuffer<AssistantCommandDispatchElement>(boundary)[0].Status);
        Assert.AreEqual(
            AssistantCommandIntentStatus.TimedOut,
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary)[0].Status);
    }

    private Entity CreateBoundary(AssistantCommandDispatchElement dispatch)
    {
        Entity boundary = _entityManager.CreateEntity(typeof(AssistantStateComponent));
        _entityManager.AddBuffer<AssistantCommandDispatchElement>(boundary).Add(dispatch);
        _entityManager.AddBuffer<AssistantCommandIntentResultElement>(boundary);
        return boundary;
    }
}
