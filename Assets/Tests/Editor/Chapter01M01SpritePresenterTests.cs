using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;

public sealed class Chapter01M01SpritePresenterTests
{
    private const string AtlasContractPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_atlas_contract.asset";
    private World _world;

    [SetUp]
    public void SetUp()
    {
        _world = new World("Chapter01M01SpritePresenterTests");
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
            _world.Dispose();
    }

    [Test]
    public void PresenterCatalog_ResolvesM01RuntimeEntityIdsToAtlasSpriteIds()
    {
        Chapter01TacticalAtlasContract contract = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAtlasContract>(AtlasContractPath);
        Assert.NotNull(contract);

        AssertPresenterResolves(contract, Chapter01M01PlayableRuntime.PlayerSquadEntityId);
        AssertPresenterResolves(contract, Chapter01M01PlayableRuntime.EnemyPatrolEntityId);
        AssertPresenterResolves(contract, Chapter01M01PlayableRuntime.DecorCommandPointEntityId);
    }

    [Test]
    public void PresenterCatalog_UsesV2DeathSpriteInsteadOfSeparateDestroyedChild()
    {
        Chapter01TacticalAtlasContract contract = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAtlasContract>(AtlasContractPath);
        Assert.NotNull(contract);

        Assert.IsTrue(Chapter01M01SpritePresenterCatalog.TryCreatePresenter(Chapter01M01PlayableRuntime.PlayerSquadEntityId, contract, out MissionRuntimeSpritePresenter presenter));
        Assert.AreEqual(
            Chapter01M01PlayableRuntime.PlayerSquadEntityId + Chapter01M01SpritePresenterCatalog.DeathStateSuffix,
            presenter.DestroyedSpriteId.ToString());
        Assert.AreEqual(Chapter01M01SpritePresenterCatalog.DestroyedSmallVfxSpriteId, presenter.DestructionVfxSpriteId.ToString());
        Assert.AreEqual(0, presenter.UsesSeparateDestroyedChild);
        Assert.AreEqual(1, presenter.RequiresFixedDirectionBakedContactShadow);
        Assert.AreEqual(1, presenter.FinalAtlasArtReady);
    }

    [Test]
    public void PresenterSystem_MapsUnitStateToMoveAttackDamagedAndDestroyedSpriteStates()
    {
        EntityManager em = _world.EntityManager;
        Entity entity = em.CreateEntity(
            typeof(MissionRuntimeSpritePresenter),
            typeof(UnitHealth),
            typeof(UnitMoveVisualState),
            typeof(UnitAttackAnimationState),
            typeof(LocalTransform));
        Assert.IsTrue(Chapter01M01SpritePresenterCatalog.TryCreatePresenter(Chapter01M01PlayableRuntime.PlayerSquadEntityId, out MissionRuntimeSpritePresenter presenter));
        em.SetComponentData(entity, presenter);
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitMoveVisualState { IsMoving = 1, StillSeconds = 0f });
        em.SetComponentData(entity, new UnitAttackAnimationState { TimeRemaining = 0f });
        em.SetComponentData(entity, LocalTransform.Identity);

        AssertResolvedState(em, entity, MissionRuntimeSpriteVisualState.Move, Chapter01M01PlayableRuntime.PlayerSquadEntityId + Chapter01M01SpritePresenterCatalog.MoveStateSuffix);

        em.SetComponentData(entity, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, new UnitAttackAnimationState { TimeRemaining = 0.25f });
        AssertResolvedState(em, entity, MissionRuntimeSpriteVisualState.Attack, Chapter01M01PlayableRuntime.PlayerSquadEntityId + Chapter01M01SpritePresenterCatalog.AttackStateSuffix);

        em.SetComponentData(entity, new UnitAttackAnimationState { TimeRemaining = 0f });
        em.SetComponentData(entity, new UnitHealth { Current = 25, Max = 100 });
        AssertResolvedState(em, entity, MissionRuntimeSpriteVisualState.Damaged, Chapter01M01PlayableRuntime.PlayerSquadEntityId + Chapter01M01SpritePresenterCatalog.DamagedStateSuffix);

        em.SetComponentData(entity, new UnitHealth { Current = 0, Max = 100 });
        AssertResolvedState(em, entity, MissionRuntimeSpriteVisualState.Destroyed, Chapter01M01PlayableRuntime.PlayerSquadEntityId + Chapter01M01SpritePresenterCatalog.DeathStateSuffix);
    }

    private static void AssertPresenterResolves(Chapter01TacticalAtlasContract contract, string runtimeEntityId)
    {
        Assert.IsTrue(Chapter01M01SpritePresenterCatalog.TryCreatePresenter(runtimeEntityId, contract, out MissionRuntimeSpritePresenter presenter), $"{runtimeEntityId} must resolve.");
        Assert.AreEqual(runtimeEntityId, presenter.RuntimeEntityId.ToString());
        Assert.AreEqual(runtimeEntityId, presenter.ManifestAssetId.ToString());
        Assert.AreEqual(runtimeEntityId + Chapter01M01SpritePresenterCatalog.IdleStateSuffix, presenter.IdleSpriteId.ToString());
        Assert.AreEqual(runtimeEntityId + Chapter01M01SpritePresenterCatalog.MoveStateSuffix, presenter.MoveSpriteId.ToString());
        Assert.AreEqual(runtimeEntityId + Chapter01M01SpritePresenterCatalog.AttackStateSuffix, presenter.AttackSpriteId.ToString());
        Assert.AreEqual(runtimeEntityId + Chapter01M01SpritePresenterCatalog.DamagedStateSuffix, presenter.DamagedSpriteId.ToString());
        Assert.AreEqual(0, presenter.UsesSeparateDestroyedChild);
        Assert.AreEqual(1, presenter.RequiresFixedDirectionBakedContactShadow);
        if (runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            Assert.AreEqual(1, presenter.FinalAtlasArtReady);
            Assert.AreEqual(runtimeEntityId + Chapter01M01SpritePresenterCatalog.DeathStateSuffix, presenter.DestroyedSpriteId.ToString());
        }
        else
        {
            Assert.IsTrue(contract.TryGetSprite(presenter.DestroyedSpriteId.ToString(), out _));
        }
    }

    private static void AssertResolvedState(EntityManager em, Entity entity, MissionRuntimeSpriteVisualState expectedState, string expectedSpriteId)
    {
        MissionRuntimeSpritePresenter presenter = em.GetComponentData<MissionRuntimeSpritePresenter>(entity);
        MissionRuntimeSpriteVisualState visualState = MissionRuntimeSpritePresenterSystem.ResolveVisualState(em, entity);
        Assert.AreEqual(expectedState, visualState);
        Assert.AreEqual(expectedSpriteId, Chapter01M01SpritePresenterCatalog.ResolveSpriteId(presenter, visualState).ToString());
    }
}
