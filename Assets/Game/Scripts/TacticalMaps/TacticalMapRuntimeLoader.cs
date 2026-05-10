using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum TacticalMapRuntimePlane
{
    GameplayXZ,
    ScreenXY
}

[DisallowMultipleComponent]
public sealed class TacticalMapRuntimeLoader : MonoBehaviour
{
    [SerializeField] private TacticalMapDefinition definition;
    [SerializeField] private GridAuthoringConfig gridConfig;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private TacticalMapRuntimePlane runtimePlane = TacticalMapRuntimePlane.GameplayXZ;
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool configureCamera = true;
    [SerializeField] private Transform generatedRoot;

    private SpriteRenderer groundRenderer;
    private GridAuthoring gridAuthoring;
    private Entity terrainSurfaceEntity;

    public TacticalMapDefinition Definition => definition;
    public GridAuthoringConfig GridConfig => gridConfig;
    public Camera GameplayCamera => gameplayCamera;
    public TacticalMapRuntimePlane RuntimePlane => runtimePlane;
    public SpriteRenderer GroundRenderer => groundRenderer;
    public GridAuthoring GridAuthoring => gridAuthoring;
    public Sprite RuntimeGroundSprite => ResolveRuntimeGroundSprite();

    private void Awake()
    {
        if (loadOnAwake && Application.isPlaying)
            Load();
    }

    public void Configure(
        TacticalMapDefinition definition,
        GridAuthoringConfig gridConfig,
        Camera gameplayCamera,
        TacticalMapRuntimePlane runtimePlane = TacticalMapRuntimePlane.GameplayXZ)
    {
        this.definition = definition;
        this.gridConfig = gridConfig;
        this.gameplayCamera = gameplayCamera;
        this.runtimePlane = runtimePlane;
    }

    public void Load()
    {
        if (definition == null)
        {
            Debug.LogError("TACTICAL_MAP_RUNTIME_LOADER_MISSING_DEFINITION");
            return;
        }

        EnsureRoot();
        EnsureGround();
        EnsureTerrainSurfaceEntity();
        EnsureGrid();

        if (configureCamera)
            ApplyCamera();

        Debug.Log($"TACTICAL_MAP_RUNTIME_LOADER_LOADED mapId={definition.MapId} children={(generatedRoot != null ? generatedRoot.childCount : 0)}");
    }

    public bool TryGetAnchorWorld(string anchorId, out Vector2 worldPosition)
    {
        if (definition != null && definition.TryGetAnchor(anchorId, out TacticalMapAnchor anchor))
        {
            worldPosition = definition.NormalizedToWorld(anchor.NormalizedPosition);
            return true;
        }

        worldPosition = default;
        return false;
    }

    public bool TryGetAnchorWorldPosition(string anchorId, out Vector3 worldPosition)
    {
        if (TryGetAnchorWorld(anchorId, out Vector2 mapWorldPosition))
        {
            worldPosition = MapWorldToRuntimeWorld(mapWorldPosition);
            return true;
        }

        worldPosition = default;
        return false;
    }

    public Vector3 MapWorldToRuntimeWorld(Vector2 mapWorldPosition)
    {
        return runtimePlane == TacticalMapRuntimePlane.GameplayXZ
            ? new Vector3(mapWorldPosition.x, 0f, mapWorldPosition.y)
            : new Vector3(mapWorldPosition.x, mapWorldPosition.y, 0f);
    }

    public bool TryGetAnchorCell(string anchorId, out Vector2Int cell)
    {
        if (definition != null && definition.TryGetAnchor(anchorId, out TacticalMapAnchor anchor))
        {
            cell = definition.NormalizedToCell(anchor.NormalizedPosition);
            return true;
        }

        cell = default;
        return false;
    }

    public Vector2 ClampWorldToCameraBounds(Vector2 worldPosition)
    {
        if (definition == null)
            return worldPosition;

        Rect bounds = definition.CameraBounds;
        return new Vector2(
            Mathf.Clamp(worldPosition.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(worldPosition.y, bounds.yMin, bounds.yMax));
    }

    private void EnsureRoot()
    {
        if (generatedRoot != null)
            return;

        GameObject root = new("GeneratedTacticalMap");
        root.transform.SetParent(transform, false);
        generatedRoot = root.transform;
    }

    private void EnsureGround()
    {
        GameObject ground;
        if (groundRenderer != null)
        {
            ground = groundRenderer.gameObject;
        }
        else
        {
            ground = new GameObject("Ground");
            groundRenderer = ground.AddComponent<SpriteRenderer>();
        }

        ground.transform.SetParent(generatedRoot, false);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localRotation = runtimePlane == TacticalMapRuntimePlane.GameplayXZ
            ? Quaternion.Euler(90f, 0f, 0f)
            : Quaternion.identity;
        groundRenderer.sprite = ResolveRuntimeGroundSprite();
        ground.transform.localScale = ResolveRuntimeGroundScale(groundRenderer.sprite);
        groundRenderer.sortingOrder = 0;
    }

    private void EnsureTerrainSurfaceEntity()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || definition == null || groundRenderer == null)
            return;

        EntityManager em = world.EntityManager;
        if (terrainSurfaceEntity == Entity.Null || !em.Exists(terrainSurfaceEntity))
            terrainSurfaceEntity = em.CreateEntity();

        string runtimeEntityId = $"terrain.{definition.MapId}";
        SetComponent(em, terrainSurfaceEntity, new MissionRuntimeEntityId { Value = new FixedString64Bytes(runtimeEntityId) });
        SetComponent(em, terrainSurfaceEntity, new MissionRuntimeTerrainSurface
        {
            RuntimeEntityId = new FixedString64Bytes(runtimeEntityId),
            MapId = new FixedString64Bytes(definition.MapId),
            MissionId = new FixedString64Bytes(definition.MissionId),
            WorldOrigin = definition.WorldOrigin,
            VisibleWorldSize = definition.VisibleWorldSize,
            GridSize = new int2(definition.GridWidth, definition.GridHeight),
            RuntimePlane = (byte)runtimePlane,
            SpriteUpAlignsPositiveWorldZ = runtimePlane == TacticalMapRuntimePlane.GameplayXZ ? (byte)1 : (byte)0
        });

        if (em.HasComponent<MissionRuntimeTerrainSurfaceRendererRuntime>(terrainSurfaceEntity))
        {
            MissionRuntimeTerrainSurfaceRendererRuntime runtime = em.GetComponentObject<MissionRuntimeTerrainSurfaceRendererRuntime>(terrainSurfaceEntity);
            runtime.Instance = groundRenderer.gameObject;
            runtime.Renderer = groundRenderer;
            runtime.GroundSprite = ResolveRuntimeGroundSprite();
            runtime.GroundScale = ResolveRuntimeGroundScale(runtime.GroundSprite);
            runtime.ProductionTacticalPlateSprites = ResolveProductionTacticalPlateSprites();
        }
        else
        {
            Sprite groundSprite = ResolveRuntimeGroundSprite();
            em.AddComponentObject(terrainSurfaceEntity, new MissionRuntimeTerrainSurfaceRendererRuntime
            {
                Instance = groundRenderer.gameObject,
                Renderer = groundRenderer,
                GroundSprite = groundSprite,
                GroundScale = ResolveRuntimeGroundScale(groundSprite),
                ProductionTacticalPlateSprites = ResolveProductionTacticalPlateSprites()
            });
        }
    }

    private Sprite ResolveRuntimeGroundSprite()
    {
        return Chapter01M01SpriteAssetResolver.TryGetM01ProductionTacticalGroundSprite(out Sprite productionSprite)
            ? productionSprite
            : definition != null ? definition.GroundSprite : null;
    }

    private Vector3 ResolveRuntimeGroundScale(Sprite groundSprite)
    {
        if (definition == null || groundSprite == null)
            return Vector3.one;

        Vector2 spriteWorldSize = groundSprite.bounds.size;
        if (spriteWorldSize.x <= 0.0001f || spriteWorldSize.y <= 0.0001f)
            return Vector3.one;

        return new Vector3(
            definition.VisibleWorldSize.x / spriteWorldSize.x,
            definition.VisibleWorldSize.y / spriteWorldSize.y,
            1f);
    }

    private static Sprite[] ResolveProductionTacticalPlateSprites()
    {
        return Chapter01M01SpriteAssetResolver.TryGetM01ProductionTacticalGroundSprites(out Sprite[] productionSprites)
            ? productionSprites
            : System.Array.Empty<Sprite>();
    }

    private void EnsureGrid()
    {
        GameObject grid;
        if (gridAuthoring != null)
        {
            grid = gridAuthoring.gameObject;
        }
        else
        {
            grid = new GameObject("GridAuthoring");
            gridAuthoring = grid.AddComponent<GridAuthoring>();
        }

        grid.transform.SetParent(generatedRoot, false);
        grid.transform.localPosition = MapWorldToRuntimeWorld(definition.WorldOrigin);
        grid.transform.localRotation = Quaternion.identity;
        grid.transform.localScale = Vector3.one;

        if (gridConfig != null)
            gridAuthoring.Configure(gridConfig);
    }

    private static void SetComponent<T>(EntityManager em, Entity entity, T component) where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.SetComponentData(entity, component);
        else
            em.AddComponentData(entity, component);
    }

    private void ApplyCamera()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
        if (gameplayCamera == null)
            return;

        gameplayCamera.orthographic = true;
        gameplayCamera.orthographicSize = definition.DefaultOrthographicSize;
        Vector2 center = ClampWorldToCameraBounds(definition.CameraDefaultCenter);
        Vector3 currentPosition = gameplayCamera.transform.position;
        gameplayCamera.transform.position = runtimePlane == TacticalMapRuntimePlane.GameplayXZ
            ? new Vector3(center.x, currentPosition.y, center.y)
            : new Vector3(center.x, center.y, currentPosition.z);
    }
}
