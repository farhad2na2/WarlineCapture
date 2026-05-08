using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

[UpdateAfter(typeof(MissionRuntimeSpritePresenterSystem))]
[UpdateAfter(typeof(UnitModelSpawnSystem))]
[UpdateAfter(typeof(UnitRenderBudgetSystem))]
public partial class MissionRuntimeAtlasQuadPresentationSystem : SystemBase
{
    private const float SpriteGroundLift = 0.03f;
    private const float SelectionGroundLift = 0.012f;
    private const float M01InfantryMetricScale = 0.20f;
    private const float M01CommandBuildingMetricScale = 0.80f;
    private const float M01PlayerSoldierScale = 1f;
    private const float M01MoveAnimationCyclesPerSecond = 3.2f;
    private const float M01MoveBobHeight = 0.035f;
    private const float M01MoveStrideScale = 0.035f;
    private const string AtlasQuadShaderName = "Universal Render Pipeline/Unlit";
    private static readonly Vector3[] RifleSquadSoldierOffsets =
    {
        new(0f, 0f, 0f),
        new(0.30f, 0f, 0.15f),
        new(-0.28f, 0f, -0.11f),
        new(0.18f, 0f, -0.28f)
    };

    private Transform _root;
    private Mesh _quadMesh;

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
            if (!em.HasComponent<MissionRuntimeAtlasQuadRuntime>(entity))
                continue;

            MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entity);
            UpdateRenderer(em, entity, runtime, presenter, transform);

            if (em.HasComponent<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>(entity))
                SuppressLegacyModelRendering(em, entity);
        }
    }

    protected override void OnDestroy()
    {
        if (_root != null)
            Object.Destroy(_root.gameObject);
        if (_quadMesh != null)
            Object.Destroy(_quadMesh);
        _root = null;
        _quadMesh = null;
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
        if (em.HasComponent<MissionRuntimeAtlasQuadRuntime>(entity))
            return;
        if (em.HasComponent<MissionRuntimeSpriteRendererRuntime>(entity))
            em.RemoveComponent<MissionRuntimeSpriteRendererRuntime>(entity);

        EnsureRoot();
        EnsureQuadMesh();
        GameObject instance = new($"M01AtlasQuad_{presenter.RuntimeEntityId.ToString()}");
        instance.transform.SetParent(_root, false);
        MeshFilter meshFilter = instance.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = _quadMesh;
        MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = ResolveSortingOrder(presenter);
        Material material = CreateAtlasQuadMaterial(presenter);
        renderer.sharedMaterial = material;
        int soldierCount = ResolveSoldierCount(presenter);
        MeshRenderer[] soldierRenderers = new MeshRenderer[soldierCount];
        Material[] soldierMaterials = new Material[soldierCount];
        soldierRenderers[0] = renderer;
        soldierMaterials[0] = material;
        for (int i = 1; i < soldierCount; i++)
            CreateSoldierChild(instance.transform, presenter, i, out soldierRenderers[i], out soldierMaterials[i]);

        CreateSelectionMarkers(instance.transform, presenter, soldierCount, out MeshRenderer[] selectionRenderers, out Material[] selectionMaterials);

        em.AddComponentObject(entity, new MissionRuntimeAtlasQuadRuntime
        {
            Instance = instance,
            MeshFilter = meshFilter,
            Renderer = renderer,
            Material = material,
            SoldierRenderers = soldierRenderers,
            SoldierMaterials = soldierMaterials,
            SelectionRenderer = selectionRenderers.Length > 0 ? selectionRenderers[0] : null,
            SelectionMaterial = selectionMaterials.Length > 0 ? selectionMaterials[0] : null,
            SelectionRenderers = selectionRenderers,
            SelectionMaterials = selectionMaterials,
            CurrentSpriteId = string.Empty,
            SoldierCount = soldierCount,
            AnimationPhase = 0f
        });
    }

    private void EnsureRoot()
    {
        if (_root != null)
            return;

        GameObject root = new("M01RuntimeEcsAtlasQuads");
        Object.DontDestroyOnLoad(root);
        _root = root.transform;
    }

    private void EnsureQuadMesh()
    {
        if (_quadMesh != null)
            return;

        _quadMesh = new Mesh
        {
            name = "M01 Runtime Atlas Quad",
            hideFlags = HideFlags.HideAndDontSave
        };
        _quadMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        _quadMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        _quadMesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
        _quadMesh.RecalculateBounds();
        _quadMesh.RecalculateNormals();
    }

    private void CreateSoldierChild(Transform parent, in MissionRuntimeSpritePresenter presenter, int index, out MeshRenderer renderer, out Material material)
    {
        GameObject soldier = new($"Soldier_{index + 1:00}");
        soldier.transform.SetParent(parent, false);
        MeshFilter meshFilter = soldier.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = _quadMesh;
        renderer = soldier.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = ResolveSortingOrder(presenter) + index;
        material = CreateAtlasQuadMaterial(presenter);
        material.name = $"M01AtlasQuad_{presenter.RuntimeEntityId.ToString()}_Soldier_{index + 1:00}";
        renderer.sharedMaterial = material;
    }

    private void CreateSelectionMarkers(Transform parent, in MissionRuntimeSpritePresenter presenter, int soldierCount, out MeshRenderer[] renderers, out Material[] materials)
    {
        renderers = new MeshRenderer[soldierCount];
        materials = new Material[soldierCount];
        for (int i = 0; i < soldierCount; i++)
        {
            GameObject marker = new($"GroundedSelection_{i + 1:00}");
            marker.transform.SetParent(parent, false);
            MeshFilter meshFilter = marker.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = _quadMesh;
            MeshRenderer renderer = marker.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = ResolveSortingOrder(presenter) - 1;
            Material material = CreateSelectionMaterial(presenter, i);
            renderer.sharedMaterial = material;
            renderer.enabled = false;
            renderers[i] = renderer;
            materials[i] = material;
        }
    }

    private static void UpdateRenderer(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter, LocalTransform transform)
    {
        if (runtime == null || runtime.Instance == null || runtime.Renderer == null || runtime.Material == null)
            return;

        string spriteId = presenter.CurrentSpriteId.ToString();
        if (runtime.CurrentSpriteId != spriteId)
        {
            runtime.CurrentSpriteId = spriteId;
            if (Chapter01M01SpriteAssetResolver.TryGetSprite(spriteId, out Sprite sprite))
                ApplySprite(runtime, sprite);
            else
                ApplyTextureToSoldiers(runtime, null);
        }

        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState == MissionRuntimeSpriteVisualState.Move)
            runtime.AnimationPhase += UnityEngine.Time.deltaTime * M01MoveAnimationCyclesPerSecond * Mathf.PI * 2f;
        else
            runtime.AnimationPhase = 0f;

        runtime.Instance.transform.position = transform.Position + new Unity.Mathematics.float3(0f, SpriteGroundLift, 0f);
        runtime.Instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        float scale = ResolveContractScale(presenter);
        runtime.Instance.transform.localScale = Vector3.one * scale;
        ApplyColorToSoldiers(runtime, ResolveTint(presenter));
        LayoutSoldiers(runtime, presenter);
        UpdateSelectionMarker(em, entity, runtime, presenter);
    }

    private static Material CreateAtlasQuadMaterial(in MissionRuntimeSpritePresenter presenter)
    {
        Shader shader = Shader.Find(AtlasQuadShaderName) ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
        Material material = new(shader)
        {
            name = $"M01AtlasQuad_{presenter.RuntimeEntityId.ToString()}",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.Transparent
        };
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);
        return material;
    }

    private static Material CreateSelectionMaterial(in MissionRuntimeSpritePresenter presenter, int index)
    {
        Material material = CreateAtlasQuadMaterial(presenter);
        material.name = $"M01GroundedSelection_{presenter.RuntimeEntityId.ToString()}_{index + 1:00}";
        ApplyColor(material, new Color(1f, 0.92f, 0.45f, 0.46f));
        ApplyTexture(material, Texture2D.whiteTexture);
        return material;
    }

    private static void ApplySprite(MissionRuntimeAtlasQuadRuntime runtime, Sprite sprite)
    {
        if (sprite == null)
        {
            ApplyTextureToSoldiers(runtime, null);
            return;
        }

        Rect rect = sprite.textureRect;
        Texture texture = sprite.texture;
        Vector2 scale = new(rect.width / texture.width, rect.height / texture.height);
        Vector2 offset = new(rect.x / texture.width, rect.y / texture.height);
        ApplyTextureToSoldiers(runtime, sprite.texture);
        ApplyTextureScaleOffsetToSoldiers(runtime, scale, offset);
    }

    private static void ApplyTextureToSoldiers(MissionRuntimeAtlasQuadRuntime runtime, Texture texture)
    {
        if (runtime.SoldierMaterials == null || runtime.SoldierMaterials.Length == 0)
        {
            ApplyTexture(runtime.Material, texture);
            return;
        }

        for (int i = 0; i < runtime.SoldierMaterials.Length; i++)
            ApplyTexture(runtime.SoldierMaterials[i], texture);
    }

    private static void ApplyTextureScaleOffsetToSoldiers(MissionRuntimeAtlasQuadRuntime runtime, Vector2 scale, Vector2 offset)
    {
        if (runtime.SoldierMaterials == null || runtime.SoldierMaterials.Length == 0)
        {
            ApplyTextureScaleOffset(runtime.Material, scale, offset);
            return;
        }

        for (int i = 0; i < runtime.SoldierMaterials.Length; i++)
            ApplyTextureScaleOffset(runtime.SoldierMaterials[i], scale, offset);
    }

    private static void ApplyTexture(Material material, Texture texture)
    {
        if (material == null)
            return;
        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
    }

    private static void ApplyTextureScaleOffset(Material material, Vector2 scale, Vector2 offset)
    {
        if (material == null)
            return;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTextureScale("_MainTex", scale);
            material.SetTextureOffset("_MainTex", offset);
        }
    }

    private static void ApplyColor(Material material, Color color)
    {
        if (material == null)
            return;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static void ApplyColorToSoldiers(MissionRuntimeAtlasQuadRuntime runtime, Color color)
    {
        if (runtime.SoldierMaterials == null || runtime.SoldierMaterials.Length == 0)
        {
            ApplyColor(runtime.Material, color);
            return;
        }

        for (int i = 0; i < runtime.SoldierMaterials.Length; i++)
            ApplyColor(runtime.SoldierMaterials[i], color);
    }

    private static void LayoutSoldiers(MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter)
    {
        MeshRenderer[] renderers = runtime.SoldierRenderers;
        Material[] materials = runtime.SoldierMaterials;
        if (renderers == null || renderers.Length == 0)
            return;

        bool hasTexture = materials != null && materials.Length > 0 && materials[0] != null && materials[0].mainTexture != null;
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Transform t = renderer.transform;
            if (runtime.Instance != null && t != runtime.Instance.transform)
            {
                Vector3 offset = ResolveSoldierOffset(presenter, i);
                offset += ResolveMoveAnimationOffset(runtime, presenter, i);
                t.localPosition = offset;
                t.localRotation = Quaternion.identity;
                float soldierScale = ResolveSoldierCount(presenter) > 1 ? M01PlayerSoldierScale : 1f;
                soldierScale += ResolveMoveAnimationScale(runtime, presenter, i);
                t.localScale = Vector3.one * soldierScale;
            }
            renderer.enabled = hasTexture;
        }
    }

    private static void UpdateSelectionMarker(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter)
    {
        if (runtime.SelectionRenderers == null || runtime.SelectionRenderers.Length == 0)
            return;

        bool selected = em.HasComponent<SelectedUnitTag>(entity);
        for (int i = 0; i < runtime.SelectionRenderers.Length; i++)
        {
            MeshRenderer renderer = runtime.SelectionRenderers[i];
            if (renderer == null)
                continue;

            renderer.enabled = selected;
            Transform t = renderer.transform;
            t.localPosition = ResolveSoldierOffset(presenter, i) + new Vector3(0f, -SelectionGroundLift, 0f);
            t.localRotation = Quaternion.identity;
            t.localScale = ResolveSoldierCount(presenter) > 1
                ? new Vector3(0.28f, 0.08f, 1f)
                : new Vector3(0.24f, 0.07f, 1f);
        }
    }

    private static int ResolveSoldierCount(in MissionRuntimeSpritePresenter presenter)
    {
        return presenter.ManifestAssetId.ToString() == Chapter01M01PlayableRuntime.PlayerSquadEntityId ? 4 : 1;
    }

    private static Vector3 ResolveSoldierOffset(in MissionRuntimeSpritePresenter presenter, int index)
    {
        if (ResolveSoldierCount(presenter) <= 1)
            return Vector3.zero;
        return RifleSquadSoldierOffsets[Mathf.Clamp(index, 0, RifleSquadSoldierOffsets.Length - 1)];
    }

    private static float ResolveContractScale(in MissionRuntimeSpritePresenter presenter)
    {
        string id = presenter.ManifestAssetId.ToString();
        if (id == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            id == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            return M01InfantryMetricScale;
        }

        if (id == Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId)
            return M01CommandBuildingMetricScale;

        return Chapter01M01SpriteAssetResolver.TryGetScale(id, out float resolvedScale)
            ? resolvedScale
            : 1f;
    }

    private static Vector3 ResolveMoveAnimationOffset(MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter, int index)
    {
        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState != MissionRuntimeSpriteVisualState.Move)
            return Vector3.zero;

        float phase = runtime.AnimationPhase + (index * 0.42f);
        return new Vector3(Mathf.Sin(phase) * M01MoveStrideScale, Mathf.Abs(Mathf.Sin(phase)) * M01MoveBobHeight, 0f);
    }

    private static float ResolveMoveAnimationScale(MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter, int index)
    {
        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState != MissionRuntimeSpriteVisualState.Move)
            return 0f;

        return Mathf.Sin(runtime.AnimationPhase + (index * 0.42f)) * 0.025f;
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
