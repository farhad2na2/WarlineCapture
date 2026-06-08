using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class UnitImpostorRenderSystem : System.IDisposable
{
    private static readonly bool EnableImpostorAtlasDiagnostics = false;
    private const int MaxBatchSize = 1023;
    private const float BaseCharacterWidth = 0.85f;
    private const float BaseCharacterHeight = 1.55f;
    private const float BaseVehicleWidth = 1.9f;
    private const float BaseVehicleHeight = 1.25f;
    private const float BaseAirWidth = 2.2f;
    private const float BaseAirHeight = 1.35f;
    private const int DirectionalImpostorCount = 8;
    private const float CharacterImpostorWidthScale = 1.65f;
    private const float CharacterImpostorHeightScale = 1.65f;
    private const float CharacterTacticalBillboardStartCameraY = 80f;
    private const float CharacterTacticalBillboardFullCameraY = 200f;
    private const float CharacterTacticalBillboardMaxScale = 16f;
    private const string ImpostorShaderName = "WarlineCapture/Unit Impostor Unlit";

    private sealed class ImpostorStyle
    {
        public Material[] DirectionMaterials;
        public float Width;
        public float Height;
        public float GroundAnchorOffset;
    }

    private sealed class BatchState
    {
        public readonly Matrix4x4[] Matrices = new Matrix4x4[MaxBatchSize];
        public int Count;
    }

    private Camera _camera;
    private Mesh _quadMesh;
    private Material _fallbackMaterial;
    private readonly Dictionary<FixedString64Bytes, GameObject> _prefabByKey = new();
    private readonly Dictionary<FixedString64Bytes, UnitImpostorAtlasEntry> _atlasByKey = new();
    private readonly Dictionary<FixedString64Bytes, ImpostorStyle> _styleByKey = new();
    private readonly Dictionary<Material, BatchState> _batchByMaterial = new();
    private World _cachedWorld;
    private EntityQuery _query;
    private EntityQuery _sourceKeyFallbackQuery;
    private int _renderLayer;
    private bool _initialized;
    private bool _hasQuery;

    public int LastDrawnCount { get; private set; }
    public int LastCulledCandidateCount { get; private set; }
    public int LastSourceKeyFallbackCandidateCount { get; private set; }

    public void Init(Camera camera, int renderLayer, UnitPrefabRegistryAuthoringConfig registryConfig)
    {
        _camera = camera;
        _renderLayer = ResolveRenderLayer(camera, renderLayer);
        _quadMesh = CreateBillboardQuad();
        _fallbackMaterial = CreateFallbackMaterial();
        if (_fallbackMaterial == null)
        {
            Debug.LogError($"Unit impostor rendering disabled because no compatible shader was found. Ensure '{ImpostorShaderName}' is included in the player build.");
            _initialized = false;
            return;
        }

        RebuildPrefabLookup(registryConfig);
        _initialized = true;
    }

    public void LateUpdate()
    {
        LastDrawnCount = 0;
        LastCulledCandidateCount = 0;
        LastSourceKeyFallbackCandidateCount = 0;
        if (!_initialized || _camera == null)
            return;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        ResetBatches();
        LastCulledCandidateCount = DrawQuery(GetOrCreateCulledQuery(world), skipVisibleRenderableUnits: true);
        LastSourceKeyFallbackCandidateCount = DrawQuery(GetOrCreateSourceKeyFallbackQuery(world), skipRenderableUnits: true);
        FlushBatches();
    }

    public void Dispose()
    {
        if (_hasQuery && _cachedWorld != null && _cachedWorld.IsCreated)
        {
            try
            {
                _query.Dispose();
                _sourceKeyFallbackQuery.Dispose();
            }
            catch (System.NullReferenceException)
            {
                // Unity can tear down EntityQuery internals before MonoBehaviour.OnDestroy during play-mode exit.
            }
            catch (System.ObjectDisposedException)
            {
            }
        }

        foreach (KeyValuePair<FixedString64Bytes, ImpostorStyle> pair in _styleByKey)
        {
            DestroyStyleMaterials(pair.Value);
        }

        DestroyRuntimeObject(_fallbackMaterial);
        DestroyRuntimeObject(_quadMesh);
        _prefabByKey.Clear();
        _atlasByKey.Clear();
        _styleByKey.Clear();
        _batchByMaterial.Clear();
        _quadMesh = null;
        _fallbackMaterial = null;
        _camera = null;
        _cachedWorld = null;
        _initialized = false;
        _hasQuery = false;
        LastDrawnCount = 0;
        LastCulledCandidateCount = 0;
        LastSourceKeyFallbackCandidateCount = 0;
    }

    private void RebuildPrefabLookup(UnitPrefabRegistryAuthoringConfig registryConfig)
    {
        _prefabByKey.Clear();
        _atlasByKey.Clear();
        if (registryConfig == null || registryConfig.UnitSpawnPrefabs == null)
            return;

        List<GameObject> prefabs = registryConfig.UnitSpawnPrefabs;
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
                continue;

            _prefabByKey[new FixedString64Bytes(prefab.name)] = prefab;
        }

        List<UnitImpostorAtlasEntry> atlases = registryConfig.ImpostorAtlases;
        if (atlases == null)
            return;

        for (int i = 0; i < atlases.Count; i++)
        {
            UnitImpostorAtlasEntry entry = atlases[i];
            if (entry?.Prefab == null || entry.Atlas == null)
                continue;

            _atlasByKey[new FixedString64Bytes(entry.Prefab.name)] = entry;
        }
    }

    private EntityQuery GetOrCreateCulledQuery(World world)
    {
        if (_cachedWorld == world && _hasQuery)
            return _query;

        if (_hasQuery)
        {
            _query.Dispose();
            _sourceKeyFallbackQuery.Dispose();
        }

        _cachedWorld = world;
        _query = world.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitRenderBudgetCulledUnitTag>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitTransportPassenger>(),
                ComponentType.ReadOnly<Disabled>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        _sourceKeyFallbackQuery = CreateSourceKeyFallbackQuery(world);
        _hasQuery = true;
        return _query;
    }

    private EntityQuery GetOrCreateSourceKeyFallbackQuery(World world)
    {
        if (_cachedWorld == world && _hasQuery)
            return _sourceKeyFallbackQuery;

        GetOrCreateCulledQuery(world);
        return _sourceKeyFallbackQuery;
    }

    private EntityQuery CreateSourceKeyFallbackQuery(World world)
    {
        return world.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitTransportPassenger>(),
                ComponentType.ReadOnly<Disabled>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<UnitModelInstanceReference>(),
                ComponentType.ReadOnly<UnitDetailedVisualReference>(),
                ComponentType.ReadOnly<UnitRenderBudgetCulledUnitTag>(),
            }
        });
    }

    private int DrawQuery(EntityQuery query, bool skipRenderableUnits = false, bool skipVisibleRenderableUnits = false)
    {
        if (query.IsEmptyIgnoreFilter)
            return 0;

        EntityManager em = _cachedWorld.EntityManager;
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        using NativeArray<LocalTransform> transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using NativeArray<UnitSourcePrefabKey> sourceKeys = query.ToComponentDataArray<UnitSourcePrefabKey>(Allocator.Temp);
        int candidateCount = 0;

        Vector3 cameraPosition = _camera.transform.position;
        for (int i = 0; i < transforms.Length; i++)
        {
            if (skipRenderableUnits && IsRenderableUnit(em, entities[i]))
                continue;
            if (skipVisibleRenderableUnits && IsRenderableVisibleUnit(em, entities[i]))
                continue;

            float3 unitPosition = transforms[i].Position;
            Vector3 position = new(unitPosition.x, unitPosition.y, unitPosition.z);
            FixedString64Bytes sourceKey = sourceKeys[i].Value;
            ImpostorStyle style = GetOrCreateStyle(sourceKey);
            Material material = ResolveDirectionalMaterial(style, transforms[i].Rotation, cameraPosition - position);
            if (style == null || material == null)
            {
                style = GetFallbackStyle();
                material = ResolveDirectionalMaterial(style, transforms[i].Rotation, cameraPosition - position);
            }

            Vector3 toCamera = cameraPosition - position;
            bool isCharacter = IsCharacterSourceKey(sourceKey);

            Quaternion rotation = ResolveBillboardRotation(
                isCharacter,
                position,
                cameraPosition,
                _camera.transform.rotation);
            float tacticalScale = isCharacter ? ResolveCharacterTacticalScale(cameraPosition.y) : 1f;
            Vector3 scale = new(style.Width * tacticalScale, style.Height * tacticalScale, 1f);
            Vector3 drawPosition = new(position.x, position.y - (style.GroundAnchorOffset * tacticalScale), position.z);
            Matrix4x4 matrix = Matrix4x4.TRS(drawPosition, rotation, scale);
            AddToBatch(material, matrix);
            candidateCount++;
        }

        return candidateCount;
    }

    private static bool IsRenderableUnit(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
               em.Exists(entity) &&
               IsRenderableUnitRecursive(em, entity, depth: 0);
    }

    private static bool IsRenderableUnitRecursive(EntityManager em, Entity entity, int depth)
    {
        if (entity == Entity.Null || !em.Exists(entity) || depth > 12)
            return false;

        if (em.HasComponent<MaterialMeshInfo>(entity) ||
            em.HasComponent<RenderFilterSettings>(entity) ||
            em.HasComponent<RenderBounds>(entity))
        {
            return true;
        }

        if (!em.HasBuffer<Child>(entity))
            return false;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
        for (int i = 0; i < children.Length; i++)
        {
            if (IsRenderableUnitRecursive(em, children[i].Value, depth + 1))
                return true;
        }

        return false;
    }

    private static bool IsRenderableVisibleUnit(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
               em.Exists(entity) &&
               IsRenderableVisibleUnitRecursive(em, entity, depth: 0);
    }

    private static bool IsRenderableVisibleUnitRecursive(EntityManager em, Entity entity, int depth)
    {
        if (entity == Entity.Null || !em.Exists(entity) || depth > 12)
            return false;

        if (IsRenderableEntity(entity, em) &&
            !em.HasComponent<Disabled>(entity) &&
            !em.HasComponent<DisableRendering>(entity) &&
            !em.HasComponent<UnitRenderBudgetCulledTag>(entity))
        {
            return true;
        }

        if (!em.HasBuffer<Child>(entity))
            return false;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
        for (int i = 0; i < children.Length; i++)
        {
            if (IsRenderableVisibleUnitRecursive(em, children[i].Value, depth + 1))
                return true;
        }

        return false;
    }

    private static bool IsRenderableEntity(Entity entity, EntityManager em)
    {
        return entity != Entity.Null &&
               em.Exists(entity) &&
               (em.HasComponent<MaterialMeshInfo>(entity) ||
                em.HasComponent<RenderFilterSettings>(entity) ||
                em.HasComponent<RenderBounds>(entity));
    }

    private void ResetBatches()
    {
        foreach (KeyValuePair<Material, BatchState> pair in _batchByMaterial)
            pair.Value.Count = 0;
    }

    private void AddToBatch(Material material, Matrix4x4 matrix)
    {
        if (!_batchByMaterial.TryGetValue(material, out BatchState batch))
        {
            batch = new BatchState();
            _batchByMaterial[material] = batch;
        }

        batch.Matrices[batch.Count++] = matrix;
        if (batch.Count >= MaxBatchSize)
            FlushBatch(material, batch);
    }

    private void FlushBatches()
    {
        foreach (KeyValuePair<Material, BatchState> pair in _batchByMaterial)
            FlushBatch(pair.Key, pair.Value);
    }

    private void FlushBatch(Material material, BatchState batch)
    {
        if (batch == null || batch.Count <= 0 || material == null || _quadMesh == null)
            return;

#pragma warning disable CS0618
        Graphics.DrawMeshInstanced(
            _quadMesh,
            0,
            material,
            batch.Matrices,
            batch.Count,
            null,
            ShadowCastingMode.Off,
            false,
            _renderLayer,
            _camera);
#pragma warning restore CS0618
        LastDrawnCount += batch.Count;
        batch.Count = 0;
    }

    private ImpostorStyle GetOrCreateStyle(FixedString64Bytes sourceKey)
    {
        if (sourceKey.Length == 0)
            return null;

        if (_styleByKey.TryGetValue(sourceKey, out ImpostorStyle style))
            return style;

        style = BuildStyle(sourceKey);
        _styleByKey[sourceKey] = style;
        return style;
    }

    private ImpostorStyle BuildStyle(FixedString64Bytes sourceKey)
    {
        if (!_prefabByKey.TryGetValue(sourceKey, out GameObject prefab) || prefab == null)
            return GetFallbackStyle();

        bool hasAtlas = _atlasByKey.TryGetValue(sourceKey, out UnitImpostorAtlasEntry atlasEntry) && atlasEntry != null;
        Material[] materials = hasAtlas ? CreateAtlasMaterials(prefab, atlasEntry) : CreateDirectionalTexturedMaterials(prefab);
        ResolveBaseSize(prefab, out float width, out float height);
        if (hasAtlas && atlasEntry.Size.sqrMagnitude > 0.0001f)
        {
            width = atlasEntry.Size.x;
            height = atlasEntry.Size.y;
        }
        if (EnableImpostorAtlasDiagnostics && hasAtlas)
        {
            Debug.Log($"[UnitImpostorAtlas] using prefab={sourceKey.ToString()} atlas={atlasEntry.Atlas.name} directions={atlasEntry.DirectionCount} columns={atlasEntry.Columns} rows={atlasEntry.Rows} size={width:F2}x{height:F2}");
        }
        return new ImpostorStyle
        {
            DirectionMaterials = materials,
            Width = hasAtlas ? width : (IsCharacterPrefab(prefab) ? width * CharacterImpostorWidthScale : width),
            Height = hasAtlas ? height : (IsCharacterPrefab(prefab) ? height * CharacterImpostorHeightScale : height),
            GroundAnchorOffset = hasAtlas && IsCharacterPrefab(prefab)
                ? height * atlasEntry.GroundAnchorNormalized
                : 0f
        };
    }

    private ImpostorStyle GetFallbackStyle()
    {
        return new ImpostorStyle
        {
            DirectionMaterials = new[] { _fallbackMaterial },
            Width = BaseCharacterWidth,
            Height = BaseCharacterHeight,
            GroundAnchorOffset = 0f
        };
    }

    private static Material ResolveDirectionalMaterial(ImpostorStyle style, quaternion unitRotation, Vector3 toCamera)
    {
        if (style == null || style.DirectionMaterials == null || style.DirectionMaterials.Length == 0)
            return null;

        if (style.DirectionMaterials.Length == 1)
            return style.DirectionMaterials[0];

        float3 forward = math.mul(unitRotation, new float3(0f, 0f, 1f));
        Vector3 unitForward = new(forward.x, forward.y, forward.z);
        unitForward.y = 0f;
        toCamera.y = 0f;
        if (unitForward.sqrMagnitude < 0.0001f || toCamera.sqrMagnitude < 0.0001f)
            return style.DirectionMaterials[0];

        unitForward.Normalize();
        toCamera.Normalize();
        float signedAngle = Vector3.SignedAngle(unitForward, toCamera, Vector3.up);
        float normalizedAngle = Mathf.Repeat(signedAngle, 360f);
        int index = Mathf.RoundToInt(normalizedAngle / 360f * style.DirectionMaterials.Length) % style.DirectionMaterials.Length;
        return style.DirectionMaterials[index] != null ? style.DirectionMaterials[index] : style.DirectionMaterials[0];
    }

    private static void ResolveBaseSize(GameObject prefab, out float width, out float height)
    {
        width = BaseCharacterWidth;
        height = BaseCharacterHeight;
        if (prefab == null)
            return;

        if (TryResolveMeshBoundsSize(prefab, out float meshWidth, out float meshHeight))
        {
            width = Mathf.Max(width, meshWidth * 1.1f);
            height = Mathf.Max(height, meshHeight * 1.1f);
        }

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        if (authoring == null)
            return;

        if (authoring.IsAirUnit)
        {
            width = Mathf.Max(width, BaseAirWidth);
            height = Mathf.Max(height, BaseAirHeight);
            return;
        }

        Vector2Int footprint = authoring.GetConfiguredFootprintCells();
        bool vehicleLike = footprint.x > 1 || footprint.y > 1;
        if (vehicleLike)
        {
            width = Mathf.Max(width, BaseVehicleWidth);
            height = Mathf.Max(height, BaseVehicleHeight);
        }
    }

    private static bool TryResolveMeshBoundsSize(GameObject prefab, out float width, out float height)
    {
        width = 0f;
        height = 0f;
        if (prefab == null)
            return false;

        MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
        bool hasBounds = false;
        Bounds combinedBounds = default;
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            Matrix4x4 localToRoot = prefab.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
            Bounds meshBounds = TransformBounds(meshFilter.sharedMesh.bounds, localToRoot);
            if (!hasBounds)
            {
                combinedBounds = meshBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(meshBounds.min);
                combinedBounds.Encapsulate(meshBounds.max);
            }
        }

        if (!hasBounds)
            return false;

        width = Mathf.Max(combinedBounds.size.x, combinedBounds.size.z);
        height = combinedBounds.size.y;
        return width > 0.01f && height > 0.01f;
    }

    private static Bounds TransformBounds(Bounds localBounds, Matrix4x4 matrix)
    {
        Vector3 center = matrix.MultiplyPoint3x4(localBounds.center);
        Vector3 extents = localBounds.extents;
        Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
        Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
        Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
        Vector3 worldExtents = new(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
        return new Bounds(center, worldExtents * 2f);
    }

    private Material[] CreateDirectionalTexturedMaterials(GameObject prefab)
    {
        if (prefab == null)
            return new[] { _fallbackMaterial };

        int directionCount = IsCharacterPrefab(prefab) ? DirectionalImpostorCount : 1;
        Material[] materials = new Material[directionCount];
        for (int i = 0; i < directionCount; i++)
        {
            if (!SharedPrefabPreviewCache.TryGetOrCreateDirectionalImpostor(prefab, i, directionCount, out RenderTexture texture) || texture == null)
            {
                materials[i] = _fallbackMaterial;
                continue;
            }

            materials[i] = CreateTexturedMaterial(prefab, texture, directionCount > 1 ? i : -1);
        }

        return materials;
    }

    private Material[] CreateAtlasMaterials(GameObject prefab, UnitImpostorAtlasEntry entry)
    {
        if (prefab == null || entry == null || entry.Atlas == null)
            return new[] { _fallbackMaterial };

        int directionCount = Mathf.Max(1, entry.DirectionCount);
        int columns = Mathf.Max(1, entry.Columns);
        int rows = Mathf.Max(1, entry.Rows);
        Material[] materials = new Material[directionCount];
        for (int i = 0; i < directionCount; i++)
        {
            Material material = CreateTexturedMaterial(prefab, entry.Atlas, i);
            const float safeInset = 24f / 512f;
            Vector2 tileScale = new(1f / columns, 1f / rows);
            Vector2 scale = tileScale * (1f - safeInset * 2f);
            int column = i % columns;
            int rowFromTop = i / columns;
            int row = rows - 1 - rowFromTop;
            Vector2 offset = new Vector2(column * tileScale.x, row * tileScale.y) + tileScale * safeInset;
            ApplyTextureScaleOffset(material, scale, offset);
            materials[i] = material;
        }

        return materials;
    }

    private Material CreateTexturedMaterial(GameObject prefab, RenderTexture texture, int directionIndex)
    {
        if (prefab == null || texture == null)
            return _fallbackMaterial;

        Shader shader = ResolveImpostorShader();
        if (shader == null)
            return _fallbackMaterial;

        Material material = new(shader)
        {
            name = directionIndex >= 0 ? $"{prefab.name}_ImpostorMaterial_{directionIndex:00}" : $"{prefab.name}_ImpostorMaterial",
            enableInstancing = true,
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = texture
        };
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);
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
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    private Material CreateTexturedMaterial(GameObject prefab, Texture texture, int directionIndex)
    {
        if (prefab == null || texture == null)
            return _fallbackMaterial;

        Shader shader = ResolveImpostorShader();
        if (shader == null)
            return _fallbackMaterial;

        Material material = new(shader)
        {
            name = directionIndex >= 0 ? $"{prefab.name}_ImpostorAtlasMaterial_{directionIndex:00}" : $"{prefab.name}_ImpostorAtlasMaterial",
            enableInstancing = true,
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = texture
        };
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);
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
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
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

    private static bool IsCharacterPrefab(GameObject prefab)
    {
        return prefab != null && prefab.name.StartsWith("Unit_Chr_", System.StringComparison.Ordinal);
    }

    private static bool IsCharacterSourceKey(FixedString64Bytes sourceKey)
    {
        return sourceKey.ToString().StartsWith("Unit_Chr_", System.StringComparison.Ordinal);
    }

    public static float ResolveCharacterTacticalScale(float cameraY)
    {
        float t = Mathf.InverseLerp(
            CharacterTacticalBillboardStartCameraY,
            CharacterTacticalBillboardFullCameraY,
            cameraY);
        return Mathf.Lerp(1f, CharacterTacticalBillboardMaxScale, t);
    }

    public static Quaternion ResolveBillboardRotation(
        bool isCharacter,
        Vector3 position,
        Vector3 cameraPosition,
        Quaternion cameraRotation)
    {
        if (isCharacter && cameraPosition.y >= CharacterTacticalBillboardStartCameraY)
            return Quaternion.LookRotation(-(cameraRotation * Vector3.forward), cameraRotation * Vector3.up);

        Vector3 toCamera = cameraPosition - position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.0001f)
            toCamera = Vector3.forward;

        return Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }

    private void DestroyStyleMaterials(ImpostorStyle style)
    {
        if (style?.DirectionMaterials == null)
            return;

        for (int i = 0; i < style.DirectionMaterials.Length; i++)
        {
            Material material = style.DirectionMaterials[i];
            if (material != null && material != _fallbackMaterial)
                DestroyRuntimeObject(material);
        }
    }

    private static Material CreateFallbackMaterial()
    {
        Shader shader = ResolveImpostorShader();
        if (shader == null)
            return null;

        Material material = new(shader)
        {
            name = "Unit Impostor Fallback Material",
            enableInstancing = true,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color color = new(0.18f, 0.2f, 0.22f, 1f);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);
        return material;
    }

    private static Shader ResolveImpostorShader()
    {
        return
            Shader.Find(ImpostorShaderName) ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Sprites/Default") ??
            Shader.Find("Unlit/Transparent") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");
    }

    private static Mesh CreateBillboardQuad()
    {
        Mesh mesh = new()
        {
            name = "Unit Impostor Billboard Quad",
            hideFlags = HideFlags.HideAndDontSave
        };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 1f, 0f),
            new Vector3(0.5f, 1f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[]
        {
            0, 1, 2,
            2, 1, 3
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static int ResolveRenderLayer(Camera camera, int preferredLayer)
    {
        if (camera == null)
            return preferredLayer;

        int cullingMask = camera.cullingMask;
        if (preferredLayer >= 0 && preferredLayer < 32 && (cullingMask & (1 << preferredLayer)) != 0)
            return preferredLayer;

        if ((cullingMask & 1) != 0)
            return 0;

        for (int layer = 0; layer < 32; layer++)
        {
            if ((cullingMask & (1 << layer)) != 0)
                return layer;
        }

        return preferredLayer;
    }

    private static void DestroyRuntimeObject(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }
}
