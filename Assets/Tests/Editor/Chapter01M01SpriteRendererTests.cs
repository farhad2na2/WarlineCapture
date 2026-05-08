using NUnit.Framework;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public sealed class Chapter01M01SpriteRendererTests
{
    private World _world;

    [SetUp]
    public void SetUp()
    {
        _world = new World("Chapter01M01SpriteRendererTests");
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

        Assert.IsTrue(MissionRuntimeSpriteRendererSystem.TryResolveSprite(presenter, out UnityEngine.Sprite sprite));
        Assert.NotNull(sprite);
        StringAssert.Contains("infantry_squad", sprite.name);
        Assert.AreEqual(299, sprite.texture.width);
        Assert.AreEqual(255, sprite.texture.height);
        Assert.AreEqual(0f, sprite.texture.GetPixel(0, 0).a, 0.01f);
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

        Assert.IsTrue(MissionRuntimeSpriteRendererSystem.SuppressLegacyModelRendering(em, unit));

        Assert.IsTrue(em.HasComponent<DisableRendering>(model));
        Assert.IsTrue(em.HasComponent<DisableRendering>(child));
    }

    [Test]
    public void Renderer_DocumentsDestroyedVfxAssetBlockerWithoutDestroyedChild()
    {
        Assert.IsTrue(Chapter01M01SpritePresenterCatalog.TryCreatePresenter(Chapter01M01PlayableRuntime.PlayerSquadEntityId, out MissionRuntimeSpritePresenter presenter));
        Assert.AreEqual(0, presenter.UsesSeparateDestroyedChild);
        Assert.AreEqual(Chapter01M01SpritePresenterCatalog.DestroyedSmallVfxSpriteId, presenter.DestroyedSpriteId.ToString());
        Assert.IsFalse(
            Chapter01M01SpriteAssetResolver.TryGetSprite(Chapter01M01SpritePresenterCatalog.DestroyedSmallVfxSpriteId, out _),
            "The final destroyed VFX sprite is still planned; this test records the visual blocker without falling back to a Destroyed child.");
    }

    private static void AssertRendererResolves(string runtimeEntityId)
    {
        Assert.IsTrue(Chapter01M01SpritePresenterCatalog.TryCreatePresenter(runtimeEntityId, out MissionRuntimeSpritePresenter presenter), $"{runtimeEntityId} presenter must resolve.");
        Assert.IsTrue(MissionRuntimeSpriteRendererSystem.TryResolveSprite(presenter, out UnityEngine.Sprite sprite), $"{runtimeEntityId} sprite must resolve from manifest path.");
        Assert.NotNull(sprite);
        Assert.AreEqual(1, presenter.RequiresFixedDirectionBakedContactShadow);
        Assert.AreEqual(0, presenter.UsesSeparateDestroyedChild);
    }
}
