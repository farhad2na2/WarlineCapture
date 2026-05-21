using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[UpdateAfter(typeof(MissionRuntimeSpritePresenterSystem))]
[UpdateAfter(typeof(UnitModelSpawnSystem))]
[UpdateAfter(typeof(UnitRenderBudgetSystem))]
public partial class MissionRuntimeAtlasQuadPresentationSystem : SystemBase
{
    private const float SpriteGroundLift = 0.03f;
    private const float SelectionGroundLift = 0.012f;
    private const float M01InfantryMetricScale = 0.21f;
    private const float M01CommandBuildingMetricScale = 0.80f;
    private const float M01PlayerSoldierScale = 1.85f;
    private const float M01SoldierShadowScale = 1f;
    private const float M01MoveAnimationCyclesPerSecond = 3.2f;
    private const float M01MoveBobHeight = 0.035f;
    private const float M01MoveStrideScale = 0.035f;
    private const float M01SelectionMarkerFootYOffset = 0.10f;
    private const float TargetMarkerGroundLift = 0.018f;
    private const int AtlasQuadRenderQueueOffset = 100;
    private const string AtlasQuadShaderName = "Universal Render Pipeline/Unlit";
    private const string SelectionMarkerAssetId = Chapter01M01SpriteAssetResolver.M01ProductionSelectionMarkerAssetId;
    private const string MoveDestinationMarkerAssetId = Chapter01M01SpriteAssetResolver.M01ProductionMoveDestinationMarkerAssetId;
    private const string AttackTargetMarkerAssetId = Chapter01M01SpriteAssetResolver.M01ProductionAttackTargetMarkerAssetId;
    private const string EnemyReadabilityMarkerAssetId = Chapter01M01SpriteAssetResolver.M01ProductionEnemyReadabilityMarkerAssetId;
#if UNITY_EDITOR
    private const string M01PlayerFacingOverrideKey = "WarlineCapture.M01.PlayerFacingOverride";
    private const string M01EnemyFacingOverrideKey = "WarlineCapture.M01.EnemyFacingOverride";
#endif

    private static int _diagnosticAnimationSampleTick;
    private static readonly Color PlayerFactionMaskColor = new Color(0.015f, 0.035f, 0.150f, 0.70f);
    private static readonly Color EnemyFactionMaskColor = new Color(0.170f, 0.020f, 0.015f, 0.70f);
    private static readonly Color EnemyHealthBarColor = new Color(0.86f, 0.10f, 0.08f, 0.92f);

    private static readonly Vector3[] RifleSquadSoldierOffsets =
    {
        new(-0.62f, 0.36f, 0f),
        new(-0.20f, -0.24f, 0f),
        new(0.28f, 0.28f, 0f),
        new(0.72f, -0.34f, 0f)
    };

    private static readonly Vector3[] M01PlayerRifleSquadSoldierOffsets =
    {
        new(-0.42f, 0.34f, 0f),
        new(-0.08f, -0.24f, 0f),
        new(0.34f, 0.25f, 0f),
        new(0.76f, -0.34f, 0f)
    };

    private Mesh _quadMesh;

    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        float deltaTime = SystemAPI.Time.DeltaTime;
        if (deltaTime <= 0f)
            deltaTime = UnityEngine.Time.unscaledDeltaTime;
        if (deltaTime <= 0f && Application.isPlaying)
            deltaTime = 1f / 60f;
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
            UpdateRenderer(em, entity, runtime, presenter, transform, deltaTime);
            DrawRuntimeQuads(runtime);

            if (em.HasComponent<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>(entity))
                SuppressLegacyModelRendering(em, entity);
        }
    }

    protected override void OnDestroy()
    {
        EntityManager em = EntityManager;
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeAtlasQuadRuntime>());
        using Unity.Collections.NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (em.Exists(entities[i]))
                DestroyRuntime(em, em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entities[i]));
        }

        if (_quadMesh != null)
            Object.Destroy(_quadMesh);
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

        EnsureQuadMesh();
        int soldierCount = ResolveSoldierCount(presenter);
        Entity[] soldierEntities = new Entity[soldierCount];
        Material[] soldierMaterials = new Material[soldierCount];
        Entity[] soldierFactionMaskEntities = new Entity[soldierCount];
        Material[] soldierFactionMaskMaterials = new Material[soldierCount];
        Entity[] soldierShadowEntities = new Entity[soldierCount];
        Material[] soldierShadowMaterials = new Material[soldierCount];
        Vector3[] soldierLocalPositions = new Vector3[soldierCount];
        Vector3[] soldierFactionMaskLocalPositions = new Vector3[soldierCount];
        Vector3[] soldierShadowLocalPositions = new Vector3[soldierCount];
        bool[] soldierVisible = new bool[soldierCount];
        bool[] soldierFactionMaskVisible = new bool[soldierCount];
        bool[] soldierShadowVisible = new bool[soldierCount];

        for (int i = 0; i < soldierCount; i++)
        {
            Material material = CreateAtlasQuadMaterial(presenter);
            material.name = $"M01AtlasQuad_{presenter.RuntimeEntityId.ToString()}_Soldier_{i + 1:00}";
            soldierMaterials[i] = material;
            soldierEntities[i] = CreateRenderEntity(em, $"M01EcsAtlasQuad_{presenter.RuntimeEntityId.ToString()}_Soldier_{i + 1:00}", material);

            Material factionMaskMaterial = CreateAtlasQuadMaterial(presenter);
            factionMaskMaterial.name = $"M01AtlasQuad_{presenter.RuntimeEntityId.ToString()}_FactionMask_{i + 1:00}";
            factionMaskMaterial.renderQueue = (int)RenderQueue.Transparent + AtlasQuadRenderQueueOffset + 1;
            ApplyColor(factionMaskMaterial, ResolveFactionMaskColor(presenter));
            soldierFactionMaskMaterials[i] = factionMaskMaterial;
            soldierFactionMaskEntities[i] = CreateRenderEntity(em, $"M01EcsAtlasQuad_{presenter.RuntimeEntityId.ToString()}_FactionMask_{i + 1:00}", factionMaskMaterial);

            Material shadowMaterial = CreateAtlasQuadMaterial(presenter);
            shadowMaterial.name = $"M01AtlasQuad_{presenter.RuntimeEntityId.ToString()}_Shadow_{i + 1:00}";
            soldierShadowMaterials[i] = shadowMaterial;
            soldierShadowEntities[i] = CreateRenderEntity(em, $"M01EcsAtlasQuad_{presenter.RuntimeEntityId.ToString()}_Shadow_{i + 1:00}", shadowMaterial);
        }

        CreateSelectionMarkers(em, presenter, soldierCount, out Entity[] selectionEntities, out Material[] selectionMaterials, out Vector3[] selectionLocalPositions, out Vector3[] selectionLocalScales, out bool[] selectionVisible);
        CreateTargetMarker(em, presenter, out Entity targetMarkerEntity, out Material targetMarkerMaterial);
        CreateEnemyReadabilityOverlays(
            em,
            presenter,
            soldierCount,
            out Entity[] enemyReadabilityEntities,
            out Material[] enemyReadabilityMaterials,
            out Vector3[] enemyReadabilityLocalPositions,
            out Vector3[] enemyReadabilityLocalScales,
            out bool[] enemyReadabilityVisible,
            out Entity[] enemyHealthBarEntities,
            out Material[] enemyHealthBarMaterials,
            out Vector3[] enemyHealthBarLocalPositions,
            out Vector3[] enemyHealthBarLocalScales,
            out bool[] enemyHealthBarVisible);

        Material material0 = soldierMaterials.Length > 0 ? soldierMaterials[0] : null;
        em.AddComponentObject(entity, new MissionRuntimeAtlasQuadRuntime
        {
            Instance = null,
            Mesh = _quadMesh,
            MeshFilter = null,
            Renderer = null,
            Material = material0,
            SoldierEntities = soldierEntities,
            SoldierRenderers = System.Array.Empty<MeshRenderer>(),
            SoldierMaterials = soldierMaterials,
            SoldierFactionMaskEntities = soldierFactionMaskEntities,
            SoldierFactionMaskMaterials = soldierFactionMaskMaterials,
            SoldierShadowEntities = soldierShadowEntities,
            SoldierShadowMaterials = soldierShadowMaterials,
            SelectionRenderer = null,
            SelectionMaterial = selectionMaterials.Length > 0 ? selectionMaterials[0] : null,
            SelectionEntities = selectionEntities,
            SelectionRenderers = System.Array.Empty<MeshRenderer>(),
            SelectionMaterials = selectionMaterials,
            TargetMarkerEntity = targetMarkerEntity,
            TargetMarkerMaterial = targetMarkerMaterial,
            EnemyReadabilityEntities = enemyReadabilityEntities,
            EnemyReadabilityMaterials = enemyReadabilityMaterials,
            EnemyHealthBarEntities = enemyHealthBarEntities,
            EnemyHealthBarMaterials = enemyHealthBarMaterials,
            SoldierLocalPositions = soldierLocalPositions,
            SoldierFactionMaskLocalPositions = soldierFactionMaskLocalPositions,
            SoldierShadowLocalPositions = soldierShadowLocalPositions,
            SelectionLocalPositions = selectionLocalPositions,
            SelectionLocalScales = selectionLocalScales,
            EnemyReadabilityLocalPositions = enemyReadabilityLocalPositions,
            EnemyReadabilityLocalScales = enemyReadabilityLocalScales,
            EnemyHealthBarLocalPositions = enemyHealthBarLocalPositions,
            EnemyHealthBarLocalScales = enemyHealthBarLocalScales,
            TargetMarkerWorldPosition = Vector3.zero,
            TargetMarkerWorldScale = Vector3.zero,
            SoldierVisible = soldierVisible,
            SoldierFactionMaskVisible = soldierFactionMaskVisible,
            SoldierShadowVisible = soldierShadowVisible,
            SelectionVisible = selectionVisible,
            EnemyReadabilityVisible = enemyReadabilityVisible,
            EnemyHealthBarVisible = enemyHealthBarVisible,
            TargetMarkerVisible = false,
            TargetMarkerKind = string.Empty,
            CurrentSpriteId = string.Empty,
            CurrentFacingId = string.Empty,
            CurrentAnimationFrameKey = string.Empty,
            SoldierCount = soldierCount,
            SoldierPivotNormalized = new Vector2(0.5f, 0.5f),
            AnimationPhase = 0f,
            AnimationElapsed = 0f,
            InstancePosition = Vector3.zero,
            InstanceRotation = Quaternion.identity,
            InstanceScale = 1f
        });
    }

    private Entity CreateRenderEntity(EntityManager em, string debugName, Material material)
    {
        Entity entity = em.CreateEntity();
        em.SetName(entity, debugName);

        RenderMeshArray renderMeshArray = new(new[] { material }, new[] { _quadMesh });
        RenderMeshDescription description = new(
            ShadowCastingMode.Off,
            receiveShadows: false,
            motionVectorGenerationMode: MotionVectorGenerationMode.Camera,
            layer: 0,
            renderingLayerMask: uint.MaxValue,
            lightProbeUsage: LightProbeUsage.Off,
            staticShadowCaster: false);
        RenderMeshUtility.AddComponents(
            entity,
            em,
            description,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        em.SetComponentEnabled<MaterialMeshInfo>(entity, false);
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.identity });
        em.AddComponent<MissionRuntimeEcsVisualTag>(entity);
        return entity;
    }

    private void EnsureQuadMesh()
    {
        if (_quadMesh != null)
            return;

        _quadMesh = new Mesh
        {
            name = "M01 Runtime ECS Atlas Quad",
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

    private void CreateSelectionMarkers(
        EntityManager em,
        in MissionRuntimeSpritePresenter presenter,
        int soldierCount,
        out Entity[] entities,
        out Material[] materials,
        out Vector3[] localPositions,
        out Vector3[] localScales,
        out bool[] visible)
    {
        entities = new Entity[soldierCount];
        materials = new Material[soldierCount];
        localPositions = new Vector3[soldierCount];
        localScales = new Vector3[soldierCount];
        visible = new bool[soldierCount];

        for (int i = 0; i < soldierCount; i++)
        {
            Material material = CreateSelectionMaterial(presenter, i);
            materials[i] = material;
            entities[i] = CreateRenderEntity(em, $"M01EcsGroundedSelection_{presenter.RuntimeEntityId.ToString()}_{i + 1:00}", material);
            em.AddComponent<MissionRuntimeSelectionMarkerVisualTag>(entities[i]);
        }
    }

    private void CreateTargetMarker(EntityManager em, in MissionRuntimeSpritePresenter presenter, out Entity entity, out Material material)
    {
        material = CreateTargetMarkerMaterial(presenter);
        entity = CreateRenderEntity(em, $"M01EcsCommandTargetMarker_{presenter.RuntimeEntityId.ToString()}", material);
        em.AddComponent<MissionRuntimeTargetMarkerVisualTag>(entity);
    }

    private void CreateEnemyReadabilityOverlays(
        EntityManager em,
        in MissionRuntimeSpritePresenter presenter,
        int soldierCount,
        out Entity[] readabilityEntities,
        out Material[] readabilityMaterials,
        out Vector3[] readabilityLocalPositions,
        out Vector3[] readabilityLocalScales,
        out bool[] readabilityVisible,
        out Entity[] healthBarEntities,
        out Material[] healthBarMaterials,
        out Vector3[] healthBarLocalPositions,
        out Vector3[] healthBarLocalScales,
        out bool[] healthBarVisible)
    {
        int overlayCount = IsEnemyPatrolPresenter(presenter) ? soldierCount : 0;
        readabilityEntities = new Entity[overlayCount];
        readabilityMaterials = new Material[overlayCount];
        readabilityLocalPositions = new Vector3[overlayCount];
        readabilityLocalScales = new Vector3[overlayCount];
        readabilityVisible = new bool[overlayCount];
        healthBarEntities = new Entity[overlayCount];
        healthBarMaterials = new Material[overlayCount];
        healthBarLocalPositions = new Vector3[overlayCount];
        healthBarLocalScales = new Vector3[overlayCount];
        healthBarVisible = new bool[overlayCount];

        for (int i = 0; i < overlayCount; i++)
        {
            Material readabilityMaterial = CreateEnemyReadabilityMaterial(presenter, i);
            readabilityMaterials[i] = readabilityMaterial;
            readabilityEntities[i] = CreateRenderEntity(em, $"M01EnemyReadability_{presenter.RuntimeEntityId.ToString()}_{i + 1:00}", readabilityMaterial);

            Material healthMaterial = CreateEnemyHealthBarMaterial(presenter, i);
            healthBarMaterials[i] = healthMaterial;
            healthBarEntities[i] = CreateRenderEntity(em, $"M01EnemyHealthBar_{presenter.RuntimeEntityId.ToString()}_{i + 1:00}", healthMaterial);
        }
    }

    private static void UpdateRenderer(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter, LocalTransform transform, float deltaTime)
    {
        if (runtime == null || runtime.SoldierEntities == null || runtime.SoldierEntities.Length == 0 || runtime.Material == null)
            return;

        string spriteId = presenter.CurrentSpriteId.ToString();
        MissionRuntimeSpriteVisualState visualState = (MissionRuntimeSpriteVisualState)presenter.CurrentState;
        string facingId = ResolveFacingId(em, entity, presenter, transform);
        bool stateOrFacingChanged = runtime.CurrentSpriteId != spriteId || runtime.CurrentFacingId != facingId;
        if (stateOrFacingChanged)
        {
            runtime.CurrentSpriteId = spriteId;
            runtime.CurrentFacingId = facingId;
            runtime.CurrentAnimationFrameKey = string.Empty;
            runtime.AnimationElapsed = 0f;
            runtime.AnimationPhase = 0f;
        }
        else
        {
            runtime.AnimationElapsed += deltaTime;
        }

        if (TryApplyV2SoldierAnimationFrame(runtime, presenter, visualState, facingId))
        {
            // V2 atlas animation owns the visible motion; keep the old procedural stride off.
        }
        else if (stateOrFacingChanged)
        {
            if (Chapter01M01SpriteAssetResolver.TryGetSprite(spriteId, out Sprite sprite))
                ApplySprite(runtime, sprite);
            else
            {
                ApplyTextureToSoldiers(runtime, null);
                ApplyTextureToSoldierFactionMasks(runtime, null);
                ApplyTextureToSoldierShadows(runtime, null);
            }
        }

        if (visualState == MissionRuntimeSpriteVisualState.Move && presenter.FinalAtlasArtReady == 0)
            runtime.AnimationPhase += deltaTime * M01MoveAnimationCyclesPerSecond * Mathf.PI * 2f;
        else if (visualState != MissionRuntimeSpriteVisualState.Move)
            runtime.AnimationPhase = 0f;

        runtime.InstancePosition = transform.Position + new float3(0f, SpriteGroundLift, 0f);
        runtime.InstanceRotation = Quaternion.Euler(90f, 0f, 0f);
        runtime.InstanceScale = ResolveContractScale(presenter);

        ApplyColorToSoldiers(runtime, ResolveTint(presenter));
        ApplyColorToSoldierFactionMasks(runtime, ResolveFactionMaskColor(presenter));
        LayoutSoldiers(em, runtime, presenter);
        UpdateEnemyReadabilityOverlays(em, runtime, presenter);
        UpdateSelectionMarker(em, entity, runtime, presenter);
        UpdateTargetMarker(em, entity, runtime);
    }

    private static bool TryApplyV2SoldierAnimationFrame(
        MissionRuntimeAtlasQuadRuntime runtime,
        in MissionRuntimeSpritePresenter presenter,
        MissionRuntimeSpriteVisualState visualState,
        string facingId)
    {
        if (presenter.FinalAtlasArtReady == 0)
            return false;

        if (!Chapter01M01SpriteAssetResolver.TryGetM01SoldierAnimationFrame(
                presenter.ManifestAssetId.ToString(),
                visualState,
                facingId,
                runtime.AnimationElapsed,
                out Chapter01M01SpriteAssetResolver.M01SoldierAnimationFrame frame))
        {
            return false;
        }

        if (runtime.CurrentAnimationFrameKey == frame.FrameKey)
            return true;

        runtime.CurrentAnimationFrameKey = frame.FrameKey;
        runtime.SoldierPivotNormalized = frame.PivotNormalizedUnitySprite;
        ApplyTextureToSoldiers(runtime, frame.Texture);
        ApplyTextureScaleOffsetToSoldiers(runtime, frame.TextureScale, frame.TextureOffset);
        ApplyTextureToSoldierFactionMasks(runtime, frame.FactionMaskTexture);
        ApplyTextureScaleOffsetToSoldierFactionMasks(runtime, frame.FactionMaskTextureScale, frame.FactionMaskTextureOffset);
        ApplyTextureToSoldierShadows(runtime, frame.ShadowTexture);
        ApplyTextureScaleOffsetToSoldierShadows(runtime, frame.ShadowTextureScale, frame.ShadowTextureOffset);
        return true;
    }

    private static Material CreateAtlasQuadMaterial(in MissionRuntimeSpritePresenter presenter)
    {
        Shader shader = Shader.Find(AtlasQuadShaderName) ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
        Material material = new(shader)
        {
            name = $"M01AtlasQuad_{presenter.RuntimeEntityId.ToString()}",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.Transparent + AtlasQuadRenderQueueOffset
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
        ApplyColor(material, Color.white);
        ApplyTexture(material, LoadMarkerTexture(SelectionMarkerAssetId) ?? Texture2D.whiteTexture);
        return material;
    }

    private static Material CreateTargetMarkerMaterial(in MissionRuntimeSpritePresenter presenter)
    {
        Material material = CreateAtlasQuadMaterial(presenter);
        material.name = $"M01CommandTargetMarker_{presenter.RuntimeEntityId.ToString()}";
        ApplyColor(material, Color.white);
        ApplyTexture(material, LoadMarkerTexture(MoveDestinationMarkerAssetId) ?? Texture2D.whiteTexture);
        return material;
    }

    private static Material CreateEnemyReadabilityMaterial(in MissionRuntimeSpritePresenter presenter, int index)
    {
        Material material = CreateAtlasQuadMaterial(presenter);
        material.name = $"M01EnemyReadability_{presenter.RuntimeEntityId.ToString()}_{index + 1:00}";
        ApplyColor(material, Color.white);
        ApplyTexture(material, LoadMarkerTexture(EnemyReadabilityMarkerAssetId));
        return material;
    }

    private static Material CreateEnemyHealthBarMaterial(in MissionRuntimeSpritePresenter presenter, int index)
    {
        Material material = CreateAtlasQuadMaterial(presenter);
        material.name = $"M01EnemyHealthBar_{presenter.RuntimeEntityId.ToString()}_{index + 1:00}";
        Texture texture = LoadMarkerTexture(Chapter01M01SpriteAssetResolver.M01ProductionEnemyHealthBarAssetId);
        ApplyColor(material, texture != null ? Color.white : EnemyHealthBarColor);
        ApplyTexture(material, texture ?? Texture2D.whiteTexture);
        return material;
    }

    private static Texture LoadMarkerTexture(string markerAssetId)
    {
#if UNITY_EDITOR
        return Chapter01M01SpriteAssetResolver.TryGetM01ProductionMarkerTexture(markerAssetId, out Texture2D texture)
            ? texture
            : null;
#else
        return null;
#endif
    }

    private static void ApplySprite(MissionRuntimeAtlasQuadRuntime runtime, Sprite sprite)
    {
        if (sprite == null)
        {
            ApplyTextureToSoldiers(runtime, null);
            ApplyTextureToSoldierFactionMasks(runtime, null);
            ApplyTextureToSoldierShadows(runtime, null);
            return;
        }

        Rect rect = sprite.textureRect;
        Texture texture = sprite.texture;
        Vector2 scale = new(rect.width / texture.width, rect.height / texture.height);
        Vector2 offset = new(rect.x / texture.width, rect.y / texture.height);
        ApplyTextureToSoldiers(runtime, sprite.texture);
        ApplyTextureToSoldierFactionMasks(runtime, null);
        ApplyTextureToSoldierShadows(runtime, null);
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

    private static void ApplyTextureToSoldierShadows(MissionRuntimeAtlasQuadRuntime runtime, Texture texture)
    {
        if (runtime.SoldierShadowMaterials == null)
            return;

        for (int i = 0; i < runtime.SoldierShadowMaterials.Length; i++)
            ApplyTexture(runtime.SoldierShadowMaterials[i], texture);
    }

    private static void ApplyTextureToSoldierFactionMasks(MissionRuntimeAtlasQuadRuntime runtime, Texture texture)
    {
        if (runtime.SoldierFactionMaskMaterials == null)
            return;

        for (int i = 0; i < runtime.SoldierFactionMaskMaterials.Length; i++)
            ApplyTexture(runtime.SoldierFactionMaskMaterials[i], texture);
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

    private static void ApplyTextureScaleOffsetToSoldierShadows(MissionRuntimeAtlasQuadRuntime runtime, Vector2 scale, Vector2 offset)
    {
        if (runtime.SoldierShadowMaterials == null)
            return;

        for (int i = 0; i < runtime.SoldierShadowMaterials.Length; i++)
            ApplyTextureScaleOffset(runtime.SoldierShadowMaterials[i], scale, offset);
    }

    private static void ApplyTextureScaleOffsetToSoldierFactionMasks(MissionRuntimeAtlasQuadRuntime runtime, Vector2 scale, Vector2 offset)
    {
        if (runtime.SoldierFactionMaskMaterials == null)
            return;

        for (int i = 0; i < runtime.SoldierFactionMaskMaterials.Length; i++)
            ApplyTextureScaleOffset(runtime.SoldierFactionMaskMaterials[i], scale, offset);
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

    private static void ApplyColorToSoldierFactionMasks(MissionRuntimeAtlasQuadRuntime runtime, Color color)
    {
        if (runtime.SoldierFactionMaskMaterials == null)
            return;

        for (int i = 0; i < runtime.SoldierFactionMaskMaterials.Length; i++)
            ApplyColor(runtime.SoldierFactionMaskMaterials[i], color);
    }

    private static void LayoutSoldiers(EntityManager em, MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter)
    {
        if (runtime.SoldierEntities == null || runtime.SoldierEntities.Length == 0)
            return;

        bool hasTexture = runtime.SoldierMaterials != null && runtime.SoldierMaterials.Length > 0 && runtime.SoldierMaterials[0] != null && runtime.SoldierMaterials[0].mainTexture != null;
        for (int i = 0; i < runtime.SoldierEntities.Length; i++)
        {
            Entity visual = runtime.SoldierEntities[i];
            if (!em.Exists(visual))
                continue;

            Vector3 offset = ResolveSoldierOffset(presenter, i) + ResolveMoveAnimationOffset(runtime, presenter, i);
            float soldierScale = ResolveSoldierCount(presenter) > 1 ? M01PlayerSoldierScale : 1f;
            soldierScale += ResolveMoveAnimationScale(runtime, presenter, i);

            runtime.SoldierLocalPositions[i] = offset;
            runtime.SoldierVisible[i] = hasTexture;
            SetRenderableEnabled(em, visual, hasTexture);
            SetEntityLocalToWorld(em, visual, runtime, ResolvePivotedSoldierCenter(runtime, offset, soldierScale), Vector3.one * soldierScale);

            if (runtime.SoldierFactionMaskEntities != null && i < runtime.SoldierFactionMaskEntities.Length)
            {
                Entity mask = runtime.SoldierFactionMaskEntities[i];
                bool hasMask = em.Exists(mask) &&
                    runtime.SoldierFactionMaskMaterials != null &&
                    i < runtime.SoldierFactionMaskMaterials.Length &&
                    runtime.SoldierFactionMaskMaterials[i] != null &&
                    runtime.SoldierFactionMaskMaterials[i].mainTexture != null &&
                    hasTexture;
                if (runtime.SoldierFactionMaskLocalPositions != null && i < runtime.SoldierFactionMaskLocalPositions.Length)
                    runtime.SoldierFactionMaskLocalPositions[i] = offset;
                if (runtime.SoldierFactionMaskVisible != null && i < runtime.SoldierFactionMaskVisible.Length)
                    runtime.SoldierFactionMaskVisible[i] = hasMask;
                if (em.Exists(mask))
                {
                    SetRenderableEnabled(em, mask, hasMask);
                    SetEntityLocalToWorld(em, mask, runtime, ResolvePivotedSoldierCenter(runtime, offset, soldierScale), Vector3.one * soldierScale);
                }
            }

            if (runtime.SoldierShadowEntities != null && i < runtime.SoldierShadowEntities.Length)
            {
                Entity shadow = runtime.SoldierShadowEntities[i];
                bool hasShadow = em.Exists(shadow) &&
                    runtime.SoldierShadowMaterials != null &&
                    i < runtime.SoldierShadowMaterials.Length &&
                    runtime.SoldierShadowMaterials[i] != null &&
                    runtime.SoldierShadowMaterials[i].mainTexture != null &&
                    hasTexture;
                if (runtime.SoldierShadowLocalPositions != null && i < runtime.SoldierShadowLocalPositions.Length)
                    runtime.SoldierShadowLocalPositions[i] = offset;
                if (runtime.SoldierShadowVisible != null && i < runtime.SoldierShadowVisible.Length)
                    runtime.SoldierShadowVisible[i] = hasShadow;
                if (em.Exists(shadow))
                {
                    SetRenderableEnabled(em, shadow, hasShadow);
                    SetEntityLocalToWorld(em, shadow, runtime, ResolvePivotedSoldierCenter(runtime, offset, soldierScale * M01SoldierShadowScale), Vector3.one * (soldierScale * M01SoldierShadowScale));
                }
            }
        }
    }

    private static void UpdateSelectionMarker(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter)
    {
        if (runtime.SelectionEntities == null || runtime.SelectionEntities.Length == 0)
            return;

        bool selected = em.HasComponent<SelectedUnitTag>(entity) || IsPlayerCommandSquadPresenter(presenter);
        for (int i = 0; i < runtime.SelectionEntities.Length; i++)
        {
            Entity visual = runtime.SelectionEntities[i];
            if (!em.Exists(visual))
                continue;

            Vector3 localPosition = ResolveSoldierOffset(presenter, i) + new Vector3(0f, M01SelectionMarkerFootYOffset - SelectionGroundLift, 0f);
            Vector3 localScale = ResolveSelectionMarkerScale(presenter);

            runtime.SelectionLocalPositions[i] = localPosition;
            runtime.SelectionLocalScales[i] = localScale;
            runtime.SelectionVisible[i] = selected;
            SetRenderableEnabled(em, visual, selected);
            SetEntityLocalToWorldGroundQuad(em, visual, runtime, localPosition, localScale);
        }
    }

    private static void UpdateEnemyReadabilityOverlays(EntityManager em, MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter)
    {
        if (runtime.EnemyReadabilityEntities == null || runtime.EnemyReadabilityEntities.Length == 0)
            return;

        bool isEnemy = IsEnemyPatrolPresenter(presenter);
        for (int i = 0; i < runtime.EnemyReadabilityEntities.Length; i++)
        {
            bool visible = isEnemy &&
                runtime.SoldierVisible != null &&
                i < runtime.SoldierVisible.Length &&
                runtime.SoldierVisible[i];

            Vector3 soldierOffset = ResolveSoldierOffset(presenter, i) + ResolveMoveAnimationOffset(runtime, presenter, i);
            Vector3 readabilityPosition = soldierOffset + new Vector3(0f, M01SelectionMarkerFootYOffset - SelectionGroundLift, 0f);
            Vector3 readabilityScale = ResolveEnemyReadabilityScale(presenter);
            Vector3 healthBarPosition = soldierOffset + new Vector3(0f, 0.52f, 0f);
            Vector3 healthBarScale = ResolveEnemyHealthBarScale();
            bool readabilityHasTexture = runtime.EnemyReadabilityMaterials != null &&
                i < runtime.EnemyReadabilityMaterials.Length &&
                runtime.EnemyReadabilityMaterials[i] != null &&
                runtime.EnemyReadabilityMaterials[i].mainTexture != null;
            bool healthBarHasTexture = runtime.EnemyHealthBarMaterials != null &&
                i < runtime.EnemyHealthBarMaterials.Length &&
                runtime.EnemyHealthBarMaterials[i] != null &&
                runtime.EnemyHealthBarMaterials[i].mainTexture != null;

            runtime.EnemyReadabilityLocalPositions[i] = readabilityPosition;
            runtime.EnemyReadabilityLocalScales[i] = readabilityScale;
            runtime.EnemyReadabilityVisible[i] = visible && readabilityHasTexture;
            if (em.Exists(runtime.EnemyReadabilityEntities[i]))
            {
                SetRenderableEnabled(em, runtime.EnemyReadabilityEntities[i], visible && readabilityHasTexture);
                SetEntityLocalToWorldGroundQuad(em, runtime.EnemyReadabilityEntities[i], runtime, readabilityPosition, readabilityScale);
            }

            runtime.EnemyHealthBarLocalPositions[i] = healthBarPosition;
            runtime.EnemyHealthBarLocalScales[i] = healthBarScale;
            runtime.EnemyHealthBarVisible[i] = visible && healthBarHasTexture;
            if (em.Exists(runtime.EnemyHealthBarEntities[i]))
            {
                SetRenderableEnabled(em, runtime.EnemyHealthBarEntities[i], visible && healthBarHasTexture);
                SetEntityLocalToWorld(em, runtime.EnemyHealthBarEntities[i], runtime, healthBarPosition, healthBarScale);
            }
        }
    }

    private static void UpdateTargetMarker(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime)
    {
        if (runtime.TargetMarkerEntity == Entity.Null || !em.Exists(runtime.TargetMarkerEntity))
            return;

        bool selected = em.HasComponent<SelectedUnitTag>(entity);
        Vector3 worldPosition = Vector3.zero;
        string markerAssetId = MoveDestinationMarkerAssetId;
        Color color = Color.white;
        string kind = string.Empty;
        Vector3 worldScale = Vector3.zero;
        bool hasTarget = selected && TryResolveTargetMarker(em, entity, out worldPosition, out markerAssetId, out color, out kind, out worldScale);
        runtime.TargetMarkerVisible = hasTarget;
        if (!hasTarget)
        {
            SetRenderableEnabled(em, runtime.TargetMarkerEntity, false);
            return;
        }

        if (runtime.TargetMarkerKind != kind)
        {
            runtime.TargetMarkerKind = kind;
            ApplyTexture(runtime.TargetMarkerMaterial, LoadMarkerTexture(markerAssetId) ?? Texture2D.whiteTexture);
            ApplyColor(runtime.TargetMarkerMaterial, color);
        }

        runtime.TargetMarkerWorldPosition = worldPosition + new Vector3(0f, TargetMarkerGroundLift, 0f);
        runtime.TargetMarkerWorldScale = worldScale;
        SetRenderableEnabled(em, runtime.TargetMarkerEntity, true);
        SetEntityWorldToGroundQuad(em, runtime.TargetMarkerEntity, runtime.TargetMarkerWorldPosition, runtime.TargetMarkerWorldScale);
    }

    private void DrawRuntimeQuads(MissionRuntimeAtlasQuadRuntime runtime)
    {
        if (_quadMesh == null || runtime == null || runtime.SoldierEntities == null)
            return;

        Matrix4x4 root = Matrix4x4.TRS(runtime.InstancePosition, runtime.InstanceRotation, Vector3.one * runtime.InstanceScale);
        DrawRuntimeQuads(EntityManager, runtime, _quadMesh, Camera.main, root);
    }

    public static void DrawAllRuntimeQuadsForCamera(EntityManager em, Camera camera)
    {
        if (camera == null)
            return;

        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeAtlasQuadRuntime>());
        using Unity.Collections.NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (!em.Exists(entities[i]))
                continue;

            MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entities[i]);
            if (runtime == null || runtime.Mesh == null)
                continue;

            Matrix4x4 root = Matrix4x4.TRS(runtime.InstancePosition, runtime.InstanceRotation, Vector3.one * runtime.InstanceScale);
            DrawRuntimeQuads(em, runtime, runtime.Mesh, camera, root);
        }
    }

    public static int BuildRuntimeQuadCommandBuffer(EntityManager em, CommandBuffer commandBuffer, Camera camera)
    {
        if (commandBuffer == null || camera == null)
            return 0;

        int drawCount = 0;
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeAtlasQuadRuntime>());
        using Unity.Collections.NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (!em.Exists(entities[i]))
                continue;

            MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entities[i]);
            if (runtime == null || runtime.Mesh == null)
                continue;

            AdvanceDiagnosticAnimationSample(em, entities[i], runtime);
            Matrix4x4 root = Matrix4x4.TRS(runtime.InstancePosition, runtime.InstanceRotation, Vector3.one * runtime.InstanceScale);
            drawCount += BuildRuntimeQuadCommandBuffer(em, runtime, runtime.Mesh, commandBuffer, root);
        }

        return drawCount;
    }

    public static void LogRuntimeQuadDiagnostics(EntityManager em, Camera camera)
    {
        if (camera == null)
            return;

        int runtimeCount = 0;
        int visibleCount = 0;
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeAtlasQuadRuntime>());
        using Unity.Collections.NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (!em.Exists(entities[i]))
                continue;

            MissionRuntimeAtlasQuadRuntime runtime = em.GetComponentObject<MissionRuntimeAtlasQuadRuntime>(entities[i]);
            if (runtime == null)
                continue;

            AdvanceDiagnosticAnimationSample(em, entities[i], runtime);
            runtimeCount++;
            Matrix4x4 root = Matrix4x4.TRS(runtime.InstancePosition, runtime.InstanceRotation, Vector3.one * runtime.InstanceScale);
            int soldierCount = runtime.SoldierEntities != null ? runtime.SoldierEntities.Length : 0;
            for (int soldier = 0; soldier < soldierCount; soldier++)
            {
                bool visible = runtime.SoldierVisible != null &&
                    soldier < runtime.SoldierVisible.Length &&
                    runtime.SoldierVisible[soldier];
                Material material = runtime.SoldierMaterials != null && soldier < runtime.SoldierMaterials.Length
                    ? runtime.SoldierMaterials[soldier]
                    : null;
                Matrix4x4 matrix = ResolveSoldierDrawMatrix(em, runtime, root, soldier);
                Vector3 world = matrix.MultiplyPoint3x4(Vector3.zero);
                Vector3 viewport = camera.WorldToViewportPoint(world);
                if (visible)
                    visibleCount++;

                Debug.Log($"WARLINECAPTURE_M01_ECS_QUAD_DIAG runtime={runtimeCount} soldier={soldier + 1} visible={(visible ? 1 : 0)} world={world.x:F3},{world.y:F3},{world.z:F3} viewport={viewport.x:F3},{viewport.y:F3},{viewport.z:F3} frame={runtime.CurrentAnimationFrameKey} tex={(material != null && material.mainTexture != null ? material.mainTexture.name : "null")} color={(material != null ? material.color.ToString() : "null")} queue={(material != null ? material.renderQueue : -1)}");
            }
            LogOverlayDiagnostics(camera, runtime, root, runtime.SoldierShadowEntities, runtime.SoldierShadowMaterials, runtime.SoldierShadowVisible, runtime.SoldierShadowLocalPositions, "soldierShadow");
            LogOverlayDiagnostics(camera, runtime, root, runtime.SelectionEntities, runtime.SelectionMaterials, runtime.SelectionVisible, runtime.SelectionLocalPositions, "selection");
            LogOverlayDiagnostics(camera, runtime, root, runtime.SoldierFactionMaskEntities, runtime.SoldierFactionMaskMaterials, runtime.SoldierFactionMaskVisible, runtime.SoldierFactionMaskLocalPositions, "soldierFactionMask");
            LogOverlayDiagnostics(camera, runtime, root, runtime.EnemyReadabilityEntities, runtime.EnemyReadabilityMaterials, runtime.EnemyReadabilityVisible, runtime.EnemyReadabilityLocalPositions, "enemyReadability");
            LogOverlayDiagnostics(camera, runtime, root, runtime.EnemyHealthBarEntities, runtime.EnemyHealthBarMaterials, runtime.EnemyHealthBarVisible, runtime.EnemyHealthBarLocalPositions, "enemyHealthBar");
        }

        Debug.Log($"WARLINECAPTURE_M01_ECS_QUAD_DIAG_SUMMARY runtimes={runtimeCount} visibleSoldiers={visibleCount}");
    }

    private static void AdvanceDiagnosticAnimationSample(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime)
    {
        if (runtime == null ||
            !em.Exists(entity) ||
            !em.HasComponent<MissionRuntimeSpritePresenter>(entity) ||
            !em.HasComponent<LocalTransform>(entity))
        {
            return;
        }

        MissionRuntimeSpritePresenter presenter = em.GetComponentData<MissionRuntimeSpritePresenter>(entity);
        if (presenter.FinalAtlasArtReady == 0)
            return;

        float elapsedSeconds = (++_diagnosticAnimationSampleTick) * 0.22f;
        LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
        runtime.CurrentSpriteId = presenter.CurrentSpriteId.ToString();
        runtime.CurrentFacingId = ResolveFacingId(em, entity, presenter, transform);
        runtime.AnimationElapsed = elapsedSeconds;
        TryApplyV2SoldierAnimationFrame(
            runtime,
            presenter,
            (MissionRuntimeSpriteVisualState)presenter.CurrentState,
            runtime.CurrentFacingId);
    }

    private static void DrawRuntimeQuads(EntityManager em, MissionRuntimeAtlasQuadRuntime runtime, Mesh mesh, Camera camera, Matrix4x4 root)
    {
        if (runtime == null || mesh == null || runtime.SoldierEntities == null)
            return;

        DrawOverlayQuads(em, runtime.SoldierShadowEntities, runtime.SoldierShadowMaterials, runtime.SoldierShadowVisible, runtime.SoldierShadowLocalPositions, mesh, camera, root);
        DrawOverlayQuads(em, runtime.EnemyReadabilityEntities, runtime.EnemyReadabilityMaterials, runtime.EnemyReadabilityVisible, runtime.EnemyReadabilityLocalPositions, mesh, camera, root);
        DrawOverlayQuads(em, runtime.SelectionEntities, runtime.SelectionMaterials, runtime.SelectionVisible, runtime.SelectionLocalPositions, mesh, camera, root);

        for (int i = 0; i < runtime.SoldierEntities.Length; i++)
        {
            if (runtime.SoldierVisible == null ||
                runtime.SoldierMaterials == null ||
                i >= runtime.SoldierVisible.Length ||
                i >= runtime.SoldierMaterials.Length ||
                !runtime.SoldierVisible[i] ||
                runtime.SoldierMaterials[i] == null)
            {
                continue;
            }

            Entity visualEntity = runtime.SoldierEntities[i];
            Matrix4x4 drawMatrix = ResolveSoldierDrawMatrix(em, runtime, root, i);

            Graphics.DrawMesh(
                mesh,
                drawMatrix,
                runtime.SoldierMaterials[i],
                0,
                camera,
                0,
                null,
                ShadowCastingMode.Off,
                false);
        }

        DrawOverlayQuads(em, runtime.SoldierFactionMaskEntities, runtime.SoldierFactionMaskMaterials, runtime.SoldierFactionMaskVisible, runtime.SoldierFactionMaskLocalPositions, mesh, camera, root);
        DrawOverlayQuads(em, runtime.EnemyHealthBarEntities, runtime.EnemyHealthBarMaterials, runtime.EnemyHealthBarVisible, runtime.EnemyHealthBarLocalPositions, mesh, camera, root);
    }

    private static int BuildRuntimeQuadCommandBuffer(EntityManager em, MissionRuntimeAtlasQuadRuntime runtime, Mesh mesh, CommandBuffer commandBuffer, Matrix4x4 root)
    {
        if (runtime == null || mesh == null || commandBuffer == null || runtime.SoldierEntities == null)
            return 0;

        int drawCount = 0;
        drawCount += BuildOverlayCommandBuffer(em, runtime.SoldierShadowEntities, runtime.SoldierShadowMaterials, runtime.SoldierShadowVisible, runtime.SoldierShadowLocalPositions, mesh, commandBuffer, root);
        drawCount += BuildOverlayCommandBuffer(em, runtime.EnemyReadabilityEntities, runtime.EnemyReadabilityMaterials, runtime.EnemyReadabilityVisible, runtime.EnemyReadabilityLocalPositions, mesh, commandBuffer, root);
        drawCount += BuildOverlayCommandBuffer(em, runtime.SelectionEntities, runtime.SelectionMaterials, runtime.SelectionVisible, runtime.SelectionLocalPositions, mesh, commandBuffer, root);
        for (int i = 0; i < runtime.SoldierEntities.Length; i++)
        {
            if (runtime.SoldierVisible == null ||
                runtime.SoldierMaterials == null ||
                i >= runtime.SoldierVisible.Length ||
                i >= runtime.SoldierMaterials.Length ||
                !runtime.SoldierVisible[i] ||
                runtime.SoldierMaterials[i] == null)
            {
                continue;
            }

            Matrix4x4 drawMatrix = ResolveSoldierDrawMatrix(em, runtime, root, i);

            commandBuffer.DrawMesh(mesh, drawMatrix, runtime.SoldierMaterials[i], 0, 0);
            drawCount++;
        }

        drawCount += BuildOverlayCommandBuffer(em, runtime.SoldierFactionMaskEntities, runtime.SoldierFactionMaskMaterials, runtime.SoldierFactionMaskVisible, runtime.SoldierFactionMaskLocalPositions, mesh, commandBuffer, root);
        drawCount += BuildOverlayCommandBuffer(em, runtime.EnemyHealthBarEntities, runtime.EnemyHealthBarMaterials, runtime.EnemyHealthBarVisible, runtime.EnemyHealthBarLocalPositions, mesh, commandBuffer, root);
        return drawCount;
    }

    private static void DrawOverlayQuads(EntityManager em, Entity[] entities, Material[] materials, bool[] visible, Vector3[] localPositions, Mesh mesh, Camera camera, Matrix4x4 root)
    {
        if (entities == null || materials == null || visible == null || mesh == null)
            return;

        for (int i = 0; i < entities.Length; i++)
        {
            if (i >= materials.Length || i >= visible.Length || !visible[i] || materials[i] == null)
                continue;

            Matrix4x4 drawMatrix = ResolveOverlayDrawMatrix(em, entities[i], root, localPositions, i);
            Graphics.DrawMesh(mesh, drawMatrix, materials[i], 0, camera, 0, null, ShadowCastingMode.Off, false);
        }
    }

    private static int BuildOverlayCommandBuffer(EntityManager em, Entity[] entities, Material[] materials, bool[] visible, Vector3[] localPositions, Mesh mesh, CommandBuffer commandBuffer, Matrix4x4 root)
    {
        if (entities == null || materials == null || visible == null || mesh == null || commandBuffer == null)
            return 0;

        int drawCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            if (i >= materials.Length || i >= visible.Length || !visible[i] || materials[i] == null)
                continue;

            Matrix4x4 drawMatrix = ResolveOverlayDrawMatrix(em, entities[i], root, localPositions, i);
            commandBuffer.DrawMesh(mesh, drawMatrix, materials[i], 0, 0);
            drawCount++;
        }

        return drawCount;
    }

    private static Matrix4x4 ResolveOverlayDrawMatrix(EntityManager em, Entity entity, Matrix4x4 root, Vector3[] localPositions, int index)
    {
        Vector3 localPosition = localPositions != null && index < localPositions.Length
            ? localPositions[index]
            : Vector3.zero;
        Matrix4x4 drawMatrix = root * Matrix4x4.TRS(localPosition, Quaternion.identity, Vector3.one);
        if (em.Exists(entity) && em.HasComponent<LocalToWorld>(entity))
            drawMatrix = ToMatrix4x4(em.GetComponentData<LocalToWorld>(entity).Value);
        return drawMatrix;
    }

    private static void LogOverlayDiagnostics(Camera camera, MissionRuntimeAtlasQuadRuntime runtime, Matrix4x4 root, Entity[] entities, Material[] materials, bool[] visible, Vector3[] localPositions, string overlayKind)
    {
        if (entities == null || entities.Length == 0)
            return;

        int visibleCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            bool isVisible = visible != null && i < visible.Length && visible[i];
            if (isVisible)
                visibleCount++;

            Material material = materials != null && i < materials.Length ? materials[i] : null;
            Vector3 localPosition = localPositions != null && i < localPositions.Length ? localPositions[i] : Vector3.zero;
            Vector3 world = root.MultiplyPoint3x4(localPosition);
            Vector3 viewport = camera.WorldToViewportPoint(world);
            Debug.Log($"WARLINECAPTURE_M01_ECS_OVERLAY_DIAG kind={overlayKind} index={i + 1} visible={(isVisible ? 1 : 0)} world={world.x:F3},{world.y:F3},{world.z:F3} viewport={viewport.x:F3},{viewport.y:F3},{viewport.z:F3} tex={(material != null && material.mainTexture != null ? material.mainTexture.name : "null")} color={(material != null ? material.color.ToString() : "null")} queue={(material != null ? material.renderQueue : -1)}");
        }

        Debug.Log($"WARLINECAPTURE_M01_ECS_OVERLAY_SUMMARY kind={overlayKind} total={entities.Length} visible={visibleCount}");
    }

    private static Matrix4x4 ResolveSoldierDrawMatrix(EntityManager em, MissionRuntimeAtlasQuadRuntime runtime, Matrix4x4 root, int index)
    {
        Entity visualEntity = runtime.SoldierEntities[index];
        Vector3 localPosition = runtime.SoldierLocalPositions != null && index < runtime.SoldierLocalPositions.Length
            ? runtime.SoldierLocalPositions[index]
            : Vector3.zero;
        Matrix4x4 local = Matrix4x4.TRS(localPosition, Quaternion.identity, Vector3.one);
        Matrix4x4 drawMatrix = root * local;
        if (em.Exists(visualEntity) && em.HasComponent<LocalToWorld>(visualEntity))
            drawMatrix = ToMatrix4x4(em.GetComponentData<LocalToWorld>(visualEntity).Value);
        return drawMatrix;
    }

    private static Matrix4x4 ToMatrix4x4(float4x4 matrix)
    {
        return new Matrix4x4(
            new Vector4(matrix.c0.x, matrix.c0.y, matrix.c0.z, matrix.c0.w),
            new Vector4(matrix.c1.x, matrix.c1.y, matrix.c1.z, matrix.c1.w),
            new Vector4(matrix.c2.x, matrix.c2.y, matrix.c2.z, matrix.c2.w),
            new Vector4(matrix.c3.x, matrix.c3.y, matrix.c3.z, matrix.c3.w));
    }

    private static bool TryResolveTargetMarker(EntityManager em, Entity entity, out Vector3 worldPosition, out string markerAssetId, out Color color, out string kind, out Vector3 worldScale)
    {
        if (em.HasComponent<EngageTarget>(entity))
        {
            EngageTarget target = em.GetComponentData<EngageTarget>(entity);
            worldPosition = math.lengthsq(target.Position) > 0.0001f
                ? (Vector3)target.Position
                : ResolveCellWorldPosition(em, target.Cell);
            markerAssetId = AttackTargetMarkerAssetId;
            color = Color.white;
            kind = "attack";
            worldScale = new Vector3(0.30f, 0.105f, 1f);
            return true;
        }

        if (em.HasComponent<UnitTarget>(entity))
        {
            UnitTarget target = em.GetComponentData<UnitTarget>(entity);
            worldPosition = ResolveCellWorldPosition(em, target.Cell);
            markerAssetId = MoveDestinationMarkerAssetId;
            color = Color.white;
            kind = "move";
            worldScale = new Vector3(0.26f, 0.095f, 1f);
            return true;
        }

        worldPosition = Vector3.zero;
        markerAssetId = MoveDestinationMarkerAssetId;
        color = Color.white;
        kind = string.Empty;
        worldScale = Vector3.zero;
        return false;
    }

    private static Vector3 ResolveCellWorldPosition(EntityManager em, int2 cell)
    {
        return TryGetGridConfig(em, out GridConfig grid)
            ? (Vector3)GridUtils.CellToWorldCenter(grid, cell)
            : new Vector3(cell.x, 0f, cell.y);
    }

    private static bool TryGetGridConfig(EntityManager em, out GridConfig grid)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        if (query.IsEmpty)
        {
            grid = default;
            return false;
        }

        using Unity.Collections.NativeArray<GridConfig> grids = query.ToComponentDataArray<GridConfig>(Unity.Collections.Allocator.Temp);
        grid = grids.Length > 0 ? grids[0] : default;
        return grids.Length > 0;
    }

    private static void SetRenderableEnabled(EntityManager em, Entity entity, bool enabled)
    {
        if (em.HasComponent<MaterialMeshInfo>(entity))
            em.SetComponentEnabled<MaterialMeshInfo>(entity, enabled);
    }

    private static void SetEntityLocalToWorld(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime, Vector3 localPosition, Vector3 localScale)
    {
        Matrix4x4 root = Matrix4x4.TRS(runtime.InstancePosition, runtime.InstanceRotation, Vector3.one * runtime.InstanceScale);
        Matrix4x4 local = Matrix4x4.TRS(localPosition, Quaternion.identity, localScale);
        em.SetComponentData(entity, new LocalToWorld { Value = root * local });
    }

    private static void SetEntityLocalToWorldGroundQuad(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime, Vector3 localPosition, Vector3 localScale)
    {
        Matrix4x4 root = Matrix4x4.TRS(runtime.InstancePosition, runtime.InstanceRotation, Vector3.one * runtime.InstanceScale);
        Vector3 worldPosition = root.MultiplyPoint3x4(localPosition);
        SetEntityWorldToGroundQuad(em, entity, worldPosition, localScale);
    }

    private static void SetEntityWorldToGroundQuad(EntityManager em, Entity entity, Vector3 worldPosition, Vector3 worldScale)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(worldPosition, Quaternion.Euler(90f, 0f, 0f), worldScale);
        em.SetComponentData(entity, new LocalToWorld { Value = matrix });
    }

    private static int ResolveSoldierCount(in MissionRuntimeSpritePresenter presenter)
    {
        string id = presenter.ManifestAssetId.ToString();
        return id == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            id == Chapter01M01PlayableRuntime.EnemyPatrolEntityId
                ? 4
                : 1;
    }

    private static Vector3 ResolveSoldierOffset(in MissionRuntimeSpritePresenter presenter, int index)
    {
        if (ResolveSoldierCount(presenter) <= 1)
            return Vector3.zero;
        if (presenter.ManifestAssetId.ToString() == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            presenter.RuntimeEntityId.ToString() == Chapter01M01PlayableRuntime.PlayerSquadEntityId)
        {
            return M01PlayerRifleSquadSoldierOffsets[Mathf.Clamp(index, 0, M01PlayerRifleSquadSoldierOffsets.Length - 1)];
        }

        return RifleSquadSoldierOffsets[Mathf.Clamp(index, 0, RifleSquadSoldierOffsets.Length - 1)];
    }

    private static Vector3 ResolvePivotedSoldierCenter(MissionRuntimeAtlasQuadRuntime runtime, Vector3 bootAnchor, float soldierScale)
    {
        Vector2 pivot = runtime.SoldierPivotNormalized;
        if (pivot == Vector2.zero)
            pivot = new Vector2(0.5f, 0.5f);

        return bootAnchor + new Vector3(
            (0.5f - pivot.x) * soldierScale,
            (0.5f - pivot.y) * soldierScale,
            0f);
    }

    private static Vector3 ResolveSelectionMarkerScale(in MissionRuntimeSpritePresenter presenter)
    {
        if (presenter.FinalAtlasArtReady != 0)
            return ResolveSoldierCount(presenter) > 1
                ? new Vector3(0.28f, 0.082f, 1f)
                : new Vector3(0.24f, 0.075f, 1f);

        return ResolveSoldierCount(presenter) > 1
            ? new Vector3(0.30f, 0.085f, 1f)
            : new Vector3(0.18f, 0.055f, 1f);
    }

    private static Vector3 ResolveEnemyReadabilityScale(in MissionRuntimeSpritePresenter presenter)
    {
        return ResolveSoldierCount(presenter) > 1
            ? new Vector3(0.26f, 0.078f, 1f)
            : new Vector3(0.24f, 0.075f, 1f);
    }

    private static Vector3 ResolveEnemyHealthBarScale()
    {
        return new Vector3(0.78f, 0.15f, 1f);
    }

    private static bool IsEnemyPatrolPresenter(in MissionRuntimeSpritePresenter presenter)
    {
        return presenter.ManifestAssetId.ToString() == Chapter01M01PlayableRuntime.EnemyPatrolEntityId ||
            presenter.RuntimeEntityId.ToString() == Chapter01M01PlayableRuntime.EnemyPatrolEntityId;
    }

    private static bool IsPlayerCommandSquadPresenter(in MissionRuntimeSpritePresenter presenter)
    {
        return presenter.ManifestAssetId.ToString() == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            presenter.RuntimeEntityId.ToString() == Chapter01M01PlayableRuntime.PlayerSquadEntityId;
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
        if (presenter.FinalAtlasArtReady != 0)
            return Vector3.zero;

        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState != MissionRuntimeSpriteVisualState.Move)
            return Vector3.zero;

        float phase = runtime.AnimationPhase + (index * 0.42f);
        return new Vector3(Mathf.Sin(phase) * M01MoveStrideScale, Mathf.Abs(Mathf.Sin(phase)) * M01MoveBobHeight, 0f);
    }

    private static float ResolveMoveAnimationScale(MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter, int index)
    {
        if (presenter.FinalAtlasArtReady != 0)
            return 0f;

        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState != MissionRuntimeSpriteVisualState.Move)
            return 0f;

        return Mathf.Sin(runtime.AnimationPhase + (index * 0.42f)) * 0.025f;
    }

    private static Color ResolveTint(in MissionRuntimeSpritePresenter presenter)
    {
        string id = presenter.ManifestAssetId.ToString();
        if (presenter.FinalAtlasArtReady != 0)
            return Color.white;
        if (id == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
            return Color.white;
        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState == MissionRuntimeSpriteVisualState.Damaged)
            return new Color(1f, 0.86f, 0.72f, 1f);
        return Color.white;
    }

    private static Color ResolveFactionMaskColor(in MissionRuntimeSpritePresenter presenter)
    {
        string id = presenter.ManifestAssetId.ToString();
        if (id == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
            return EnemyFactionMaskColor;
        if (id == Chapter01M01PlayableRuntime.PlayerSquadEntityId)
            return PlayerFactionMaskColor;
        return Color.clear;
    }

    private static string ResolveFacingId(EntityManager em, Entity entity, in MissionRuntimeSpritePresenter presenter, LocalTransform transform)
    {
        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState == MissionRuntimeSpriteVisualState.Idle &&
            presenter.FinalAtlasArtReady != 0)
        {
            string id = presenter.ManifestAssetId.ToString();
#if UNITY_EDITOR
            if (TryResolveEditorFacingOverride(id, out string overrideFacing))
                return overrideFacing;
#endif
            if (id == Chapter01M01PlayableRuntime.PlayerSquadEntityId)
                return "NE";
            if (id == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
                return "SW";
        }

        Vector3 direction = Vector3.zero;
        if (em.HasComponent<EngageTarget>(entity))
        {
            EngageTarget target = em.GetComponentData<EngageTarget>(entity);
            direction = math.lengthsq(target.Position) > 0.0001f
                ? (Vector3)(target.Position - transform.Position)
                : ResolveCellWorldPosition(em, target.Cell) - (Vector3)transform.Position;
        }
        else if (em.HasComponent<UnitTarget>(entity))
        {
            UnitTarget target = em.GetComponentData<UnitTarget>(entity);
            direction = ResolveCellWorldPosition(em, target.Cell) - (Vector3)transform.Position;
        }
        else if (em.HasComponent<UnitPathRequest>(entity))
        {
            UnitPathRequest request = em.GetComponentData<UnitPathRequest>(entity);
            direction = ResolveCellWorldPosition(em, request.Goal) - (Vector3)transform.Position;
        }

        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.Forward();

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return "SE";

        return direction.x switch
        {
            >= 0f when direction.z >= 0f => "NE",
            >= 0f => "SE",
            < 0f when direction.z >= 0f => "NW",
            _ => "SW"
        };
    }

#if UNITY_EDITOR
    private static bool TryResolveEditorFacingOverride(string runtimeEntityId, out string facing)
    {
        facing = null;
        string key = runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId
            ? M01PlayerFacingOverrideKey
            : runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId
                ? M01EnemyFacingOverrideKey
                : null;
        if (string.IsNullOrEmpty(key))
            return false;

        string value = EditorPrefs.GetString(key, string.Empty).ToUpperInvariant();
        if (value != "NE" && value != "SE" && value != "SW" && value != "NW")
            return false;

        facing = value;
        return true;
    }
#endif

    private static void DestroyRuntime(EntityManager em, MissionRuntimeAtlasQuadRuntime runtime)
    {
        DestroyEntities(em, runtime.SoldierEntities);
        DestroyEntities(em, runtime.SoldierFactionMaskEntities);
        DestroyEntities(em, runtime.SoldierShadowEntities);
        DestroyEntities(em, runtime.SelectionEntities);
        DestroyEntities(em, runtime.EnemyReadabilityEntities);
        DestroyEntities(em, runtime.EnemyHealthBarEntities);
        if (runtime.TargetMarkerEntity != Entity.Null && em.Exists(runtime.TargetMarkerEntity))
            em.DestroyEntity(runtime.TargetMarkerEntity);
        DestroyMaterials(runtime.SoldierMaterials);
        DestroyMaterials(runtime.SoldierFactionMaskMaterials);
        DestroyMaterials(runtime.SoldierShadowMaterials);
        DestroyMaterials(runtime.SelectionMaterials);
        DestroyMaterials(runtime.EnemyReadabilityMaterials);
        DestroyMaterials(runtime.EnemyHealthBarMaterials);
        if (runtime.TargetMarkerMaterial != null)
            Object.Destroy(runtime.TargetMarkerMaterial);
    }


    private static void DestroyEntities(EntityManager em, Entity[] entities)
    {
        if (entities == null)
            return;
        for (int i = 0; i < entities.Length; i++)
        {
            if (em.Exists(entities[i]))
                em.DestroyEntity(entities[i]);
        }
    }

    private static void DestroyMaterials(Material[] materials)
    {
        if (materials == null)
            return;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
                Object.Destroy(materials[i]);
        }
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
