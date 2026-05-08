using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

[UpdateAfter(typeof(MissionRuntimeSpritePresenterSystem))]
[UpdateAfter(typeof(UnitModelSpawnSystem))]
[UpdateAfter(typeof(UnitRenderBudgetSystem))]
public partial class MissionRuntimeSpriteRendererSystem : SystemBase
{
    private const float SpriteGroundLift = 0.03f;

    private Transform _root;

    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<MissionRuntimeSpritePresenter>(),
            ComponentType.ReadOnly<LocalTransform>());
        using Unity.Collections.NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            MissionRuntimeSpritePresenter presenter = em.GetComponentData<MissionRuntimeSpritePresenter>(entity);
            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            EnsureRenderer(em, entity, presenter);
            if (!em.HasComponent<MissionRuntimeSpriteRendererRuntime>(entity))
                continue;

            MissionRuntimeSpriteRendererRuntime runtime = em.GetComponentObject<MissionRuntimeSpriteRendererRuntime>(entity);
            UpdateRenderer(runtime, presenter, transform);

            if (em.HasComponent<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>(entity))
                SuppressLegacyModelRendering(em, entity);
        }
    }

    protected override void OnDestroy()
    {
        if (_root != null)
            Object.Destroy(_root.gameObject);
        _root = null;
        Chapter01M01SpriteAssetResolver.ClearCache();
    }

    public static bool TryResolveSprite(in MissionRuntimeSpritePresenter presenter, out Sprite sprite)
    {
        return Chapter01M01SpriteAssetResolver.TryGetSprite(presenter.CurrentSpriteId.ToString(), out sprite);
    }

    public static bool SuppressLegacyModelRendering(EntityManager em, Entity entity)
    {
        bool changed = false;
        if (em.HasComponent<UnitModelInstanceReference>(entity))
            changed |= DisableRenderingRecursive(em, em.GetComponentData<UnitModelInstanceReference>(entity).Instance);
        if (em.HasComponent<UnitMidLodInstanceReference>(entity))
            changed |= DisableRenderingRecursive(em, em.GetComponentData<UnitMidLodInstanceReference>(entity).Instance);
        if (em.HasComponent<UnitLowLodInstanceReference>(entity))
            changed |= DisableRenderingRecursive(em, em.GetComponentData<UnitLowLodInstanceReference>(entity).Instance);
        return changed;
    }

    private void EnsureRenderer(EntityManager em, Entity entity, in MissionRuntimeSpritePresenter presenter)
    {
        if (em.HasComponent<MissionRuntimeSpriteRendererRuntime>(entity))
            return;

        EnsureRoot();
        GameObject instance = new($"M01Sprite_{presenter.RuntimeEntityId.ToString()}");
        instance.transform.SetParent(_root, false);
        SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = ResolveSortingOrder(presenter);

        em.AddComponentObject(entity, new MissionRuntimeSpriteRendererRuntime
        {
            Instance = instance,
            Renderer = renderer,
            CurrentSpriteId = string.Empty
        });
    }

    private void EnsureRoot()
    {
        if (_root != null)
            return;

        GameObject root = new("M01RuntimeSpriteRenderers");
        Object.DontDestroyOnLoad(root);
        _root = root.transform;
    }

    private static void UpdateRenderer(MissionRuntimeSpriteRendererRuntime runtime, in MissionRuntimeSpritePresenter presenter, LocalTransform transform)
    {
        if (runtime == null || runtime.Instance == null || runtime.Renderer == null)
            return;

        string spriteId = presenter.CurrentSpriteId.ToString();
        if (runtime.CurrentSpriteId != spriteId)
        {
            runtime.CurrentSpriteId = spriteId;
            if (Chapter01M01SpriteAssetResolver.TryGetSprite(spriteId, out Sprite sprite))
                runtime.Renderer.sprite = sprite;
            else
                runtime.Renderer.sprite = null;
        }

        runtime.Instance.transform.position = transform.Position + new Unity.Mathematics.float3(0f, SpriteGroundLift, 0f);
        runtime.Instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        float scale = Chapter01M01SpriteAssetResolver.TryGetScale(presenter.ManifestAssetId.ToString(), out float resolvedScale)
            ? resolvedScale
            : 1f;
        runtime.Instance.transform.localScale = Vector3.one * scale;
        runtime.Renderer.color = ResolveTint(presenter);
        runtime.Renderer.enabled = runtime.Renderer.sprite != null;
    }

    private static Color ResolveTint(in MissionRuntimeSpritePresenter presenter)
    {
        string id = presenter.ManifestAssetId.ToString();
        if (id == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
            return new Color(1f, 0.58f, 0.48f, 1f);
        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState == MissionRuntimeSpriteVisualState.Damaged)
            return new Color(1f, 0.86f, 0.72f, 1f);
        return Color.white;
    }

    private static int ResolveSortingOrder(in MissionRuntimeSpritePresenter presenter)
    {
        string id = presenter.ManifestAssetId.ToString();
        if (id == Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId)
            return 22;
        return 24;
    }

    private static bool DisableRenderingRecursive(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return false;

        Entity[] childEntities = System.Array.Empty<Entity>();
        if (em.HasBuffer<Child>(entity))
        {
            DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
            childEntities = new Entity[children.Length];
            for (int i = 0; i < children.Length; i++)
                childEntities[i] = children[i].Value;
        }

        bool changed = false;
        if (!em.HasComponent<DisableRendering>(entity))
        {
            em.AddComponent<DisableRendering>(entity);
            changed = true;
        }

        for (int i = 0; i < childEntities.Length; i++)
            changed |= DisableRenderingRecursive(em, childEntities[i]);

        return changed;
    }
}
