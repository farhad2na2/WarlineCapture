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
    private const float M01InfantryMetricScale = 0.17f;
    private const float M01CommandBuildingMetricScale = 0.80f;
    private const float M01PlayerSoldierScale = 1f;
    private const float M01MoveAnimationCyclesPerSecond = 3.2f;
    private const float M01MoveBobHeight = 0.035f;
    private const float M01MoveStrideScale = 0.035f;
    private const float M01SelectionMarkerFootYOffset = -0.42f;
    private const float TargetMarkerGroundLift = 0.018f;
    private const int AtlasQuadRenderQueueOffset = 100;
    private const string AtlasQuadShaderName = "Universal Render Pipeline/Unlit";
    private const string SelectionMarkerAssetId = Chapter01M01SpriteAssetResolver.M01ProductionSelectionMarkerAssetId;
    private const string MoveDestinationMarkerAssetId = Chapter01M01SpriteAssetResolver.M01ProductionMoveDestinationMarkerAssetId;
    private const string AttackTargetMarkerAssetId = Chapter01M01SpriteAssetResolver.M01ProductionAttackTargetMarkerAssetId;

    private static readonly Vector3[] RifleSquadSoldierOffsets =
    {
        new(-2.20f, 0f, 0.92f),
        new(-0.70f, 0f, -0.58f),
        new(0.90f, 0f, 0.68f),
        new(2.35f, 0f, -0.84f)
    };

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
        Vector3[] soldierLocalPositions = new Vector3[soldierCount];
        bool[] soldierVisible = new bool[soldierCount];

        for (int i = 0; i < soldierCount; i++)
        {
            Material material = CreateAtlasQuadMaterial(presenter);
            material.name = $"M01AtlasQuad_{presenter.RuntimeEntityId.ToString()}_Soldier_{i + 1:00}";
            soldierMaterials[i] = material;
            soldierEntities[i] = CreateRenderEntity(em, $"M01EcsAtlasQuad_{presenter.RuntimeEntityId.ToString()}_Soldier_{i + 1:00}", material);
        }

        CreateSelectionMarkers(em, presenter, soldierCount, out Entity[] selectionEntities, out Material[] selectionMaterials, out Vector3[] selectionLocalPositions, out Vector3[] selectionLocalScales, out bool[] selectionVisible);
        CreateTargetMarker(em, presenter, out Entity targetMarkerEntity, out Material targetMarkerMaterial);

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
            SelectionRenderer = null,
            SelectionMaterial = selectionMaterials.Length > 0 ? selectionMaterials[0] : null,
            SelectionEntities = selectionEntities,
            SelectionRenderers = System.Array.Empty<MeshRenderer>(),
            SelectionMaterials = selectionMaterials,
            TargetMarkerEntity = targetMarkerEntity,
            TargetMarkerMaterial = targetMarkerMaterial,
            SoldierLocalPositions = soldierLocalPositions,
            SelectionLocalPositions = selectionLocalPositions,
            SelectionLocalScales = selectionLocalScales,
            TargetMarkerWorldPosition = Vector3.zero,
            TargetMarkerWorldScale = Vector3.zero,
            SoldierVisible = soldierVisible,
            SelectionVisible = selectionVisible,
            TargetMarkerVisible = false,
            TargetMarkerKind = string.Empty,
            CurrentSpriteId = string.Empty,
            CurrentFacingId = string.Empty,
            CurrentAnimationFrameKey = string.Empty,
            SoldierCount = soldierCount,
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

    private static void UpdateRenderer(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter, LocalTransform transform)
    {
        if (runtime == null || runtime.SoldierEntities == null || runtime.SoldierEntities.Length == 0 || runtime.Material == null)
            return;

        string spriteId = presenter.CurrentSpriteId.ToString();
        MissionRuntimeSpriteVisualState visualState = (MissionRuntimeSpriteVisualState)presenter.CurrentState;
        string facingId = ResolveFacingId(em, entity, transform);
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
            runtime.AnimationElapsed += UnityEngine.Time.deltaTime;
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
                ApplyTextureToSoldiers(runtime, null);
        }

        if (visualState == MissionRuntimeSpriteVisualState.Move && presenter.FinalAtlasArtReady == 0)
            runtime.AnimationPhase += UnityEngine.Time.deltaTime * M01MoveAnimationCyclesPerSecond * Mathf.PI * 2f;
        else if (visualState != MissionRuntimeSpriteVisualState.Move)
            runtime.AnimationPhase = 0f;

        runtime.InstancePosition = transform.Position + new float3(0f, SpriteGroundLift, 0f);
        runtime.InstanceRotation = Quaternion.Euler(90f, 0f, 0f);
        runtime.InstanceScale = ResolveContractScale(presenter);

        ApplyColorToSoldiers(runtime, ResolveTint(presenter));
        LayoutSoldiers(em, runtime, presenter);
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
        ApplyTextureToSoldiers(runtime, frame.Texture);
        ApplyTextureScaleOffsetToSoldiers(runtime, frame.TextureScale, frame.TextureOffset);
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
            SetEntityLocalToWorld(em, visual, runtime, offset, Vector3.one * soldierScale);
        }
    }

    private static void UpdateSelectionMarker(EntityManager em, Entity entity, MissionRuntimeAtlasQuadRuntime runtime, in MissionRuntimeSpritePresenter presenter)
    {
        if (runtime.SelectionEntities == null || runtime.SelectionEntities.Length == 0)
            return;

        bool selected = em.HasComponent<SelectedUnitTag>(entity);
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
            SetEntityLocalToWorld(em, visual, runtime, localPosition, localScale);
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

    private static void DrawRuntimeQuads(EntityManager em, MissionRuntimeAtlasQuadRuntime runtime, Mesh mesh, Camera camera, Matrix4x4 root)
    {
        if (runtime == null || mesh == null || runtime.SoldierEntities == null)
            return;

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
            Vector3 localPosition = runtime.SoldierLocalPositions != null && i < runtime.SoldierLocalPositions.Length
                ? runtime.SoldierLocalPositions[i]
                : Vector3.zero;
            Matrix4x4 local = Matrix4x4.TRS(localPosition, Quaternion.identity, Vector3.one);
            Matrix4x4 drawMatrix = root * local;
            if (em.Exists(visualEntity) && em.HasComponent<LocalToWorld>(visualEntity))
                drawMatrix = ToMatrix4x4(em.GetComponentData<LocalToWorld>(visualEntity).Value);

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
        return RifleSquadSoldierOffsets[Mathf.Clamp(index, 0, RifleSquadSoldierOffsets.Length - 1)];
    }

    private static Vector3 ResolveSelectionMarkerScale(in MissionRuntimeSpritePresenter presenter)
    {
        if (presenter.FinalAtlasArtReady != 0)
            return ResolveSoldierCount(presenter) > 1
                ? new Vector3(0.95f, 0.26f, 1f)
                : new Vector3(0.62f, 0.18f, 1f);

        return ResolveSoldierCount(presenter) > 1
            ? new Vector3(0.30f, 0.085f, 1f)
            : new Vector3(0.18f, 0.055f, 1f);
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
        if (presenter.FinalAtlasArtReady != 0)
            return Color.white;

        string id = presenter.ManifestAssetId.ToString();
        if (id == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
            return new Color(1f, 0.58f, 0.48f, 1f);
        if ((MissionRuntimeSpriteVisualState)presenter.CurrentState == MissionRuntimeSpriteVisualState.Damaged)
            return new Color(1f, 0.86f, 0.72f, 1f);
        return Color.white;
    }

    private static string ResolveFacingId(EntityManager em, Entity entity, LocalTransform transform)
    {
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

    private static void DestroyRuntime(EntityManager em, MissionRuntimeAtlasQuadRuntime runtime)
    {
        DestroyEntities(em, runtime.SoldierEntities);
        DestroyEntities(em, runtime.SelectionEntities);
        if (runtime.TargetMarkerEntity != Entity.Null && em.Exists(runtime.TargetMarkerEntity))
            em.DestroyEntity(runtime.TargetMarkerEntity);
        DestroyMaterials(runtime.SoldierMaterials);
        DestroyMaterials(runtime.SelectionMaterials);
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
