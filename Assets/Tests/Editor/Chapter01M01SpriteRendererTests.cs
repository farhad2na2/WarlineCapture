using NUnit.Framework;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public sealed class Chapter01M01AtlasQuadPresentationTests
{
    private World _world;

    [SetUp]
    public void SetUp()
    {
        _world = new World("Chapter01M01AtlasQuadPresentationTests");
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        Chapter01M01SpriteAssetResolver.ClearCache();
    }

    [Test]
    public void Renderer_ResolvesCoveredM01PresenterSpritesFromManifestPaths()
    {
        AssertRendererResolves(Chapter01M01PlayableRuntime.PlayerSquadEntityId);
        AssertRendererResolves(Chapter01M01PlayableRuntime.EnemyPatrolEntityId);
        AssertRendererResolves(Chapter01M01PlayableRuntime.DecorCommandPointEntityId);
    }

    [Test]
    public void Renderer_UsesPresenterStateForCurrentSpriteSelection()
    {
        Assert.IsTrue(Chapter01M01SpritePresenterCatalog.TryCreatePresenter(Chapter01M01PlayableRuntime.PlayerSquadEntityId, out MissionRuntimeSpritePresenter presenter));
        presenter.CurrentState = (byte)MissionRuntimeSpriteVisualState.Move;
        presenter.CurrentSpriteId = Chapter01M01SpritePresenterCatalog.ResolveSpriteId(presenter, MissionRuntimeSpriteVisualState.Move);

        Assert.IsTrue(MissionRuntimeAtlasQuadPresentationSystem.TryResolveSprite(presenter, out UnityEngine.Sprite sprite));
        Assert.NotNull(sprite);
        StringAssert.Contains("player_rifle_squad_animation_atlas_v2_run_SE", sprite.name);
        Assert.AreEqual(4096, sprite.texture.width);
        Assert.AreEqual(1792, sprite.texture.height);
        Assert.AreEqual(256, sprite.textureRect.width);
        Assert.AreEqual(256, sprite.textureRect.height);
    }

    [Test]
    public void Renderer_SuppressesVisibleLegacyModelInstancesForCoveredEntities()
    {
        EntityManager em = _world.EntityManager;
        Entity unit = em.CreateEntity(typeof(MissionRuntimeSpritePresenterSuppressesLegacyModelTag));
        Entity model = em.CreateEntity();
        Entity child = em.CreateEntity();
        em.AddComponentData(unit, new UnitModelInstanceReference { Instance = model });
        DynamicBuffer<Child> children = em.AddBuffer<Child>(model);
        children.Add(new Child { Value = child });

        Assert.IsFalse(em.HasComponent<DisableRendering>(model));
        Assert.IsFalse(em.HasComponent<DisableRendering>(child));

        Assert.IsTrue(MissionRuntimeAtlasQuadPresentationSystem.SuppressLegacyModelRendering(em, unit));

        Assert.IsTrue(em.HasComponent<DisableRendering>(model));
        Assert.IsTrue(em.HasComponent<DisableRendering>(child));
    }

    [Test]
    public void Renderer_UsesV2DeathAtlasSpriteWithoutDestroyedChild()
    {
        Assert.IsTrue(Chapter01M01SpritePresenterCatalog.TryCreatePresenter(Chapter01M01PlayableRuntime.PlayerSquadEntityId, out MissionRuntimeSpritePresenter presenter));
        Assert.AreEqual(0, presenter.UsesSeparateDestroyedChild);
        Assert.AreEqual(
            Chapter01M01PlayableRuntime.PlayerSquadEntityId + Chapter01M01SpritePresenterCatalog.DeathStateSuffix,
            presenter.DestroyedSpriteId.ToString());
        Assert.IsTrue(
            Chapter01M01SpriteAssetResolver.TryGetSprite(presenter.DestroyedSpriteId.ToString(), out UnityEngine.Sprite sprite),
            "The v2 soldier atlas must provide the death sequence frame instead of falling back to the planned VFX blocker.");
        StringAssert.Contains("player_rifle_squad_animation_atlas_v2_death_SE", sprite.name);
    }

    private static void AssertRendererResolves(string runtimeEntityId)
    {
        Assert.IsTrue(Chapter01M01SpritePresenterCatalog.TryCreatePresenter(runtimeEntityId, out MissionRuntimeSpritePresenter presenter), $"{runtimeEntityId} presenter must resolve.");
        Assert.IsTrue(MissionRuntimeAtlasQuadPresentationSystem.TryResolveSprite(presenter, out UnityEngine.Sprite sprite), $"{runtimeEntityId} sprite must resolve from manifest path.");
        Assert.NotNull(sprite);
        if (runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            StringAssert.Contains("_animation_atlas_v2_", sprite.name);
            Assert.IsFalse(sprite.name.Contains("Unit_Chr_Soldier_Male_02"), $"{runtimeEntityId} must not resolve to the old individual soldier sheet.");
            Assert.IsFalse(sprite.name.Contains("infantry_squad"), $"{runtimeEntityId} must not resolve to the rejected mini-squad source.");
            Assert.AreEqual(1, presenter.FinalAtlasArtReady);
        }
        Assert.AreEqual(1, presenter.RequiresFixedDirectionBakedContactShadow);
        Assert.AreEqual(0, presenter.UsesSeparateDestroyedChild);
    }
}
