using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Tactical Map Definition")]
public sealed class TacticalMapDefinition : ScriptableObject
{
    [SerializeField] private string mapId;
    [SerializeField] private string levelId;
    [SerializeField] private string missionId;
    [SerializeField] private string scenarioSetupId;
    [SerializeField] private Sprite groundSprite;
    [SerializeField] private string mapPreviewArtId;
    [SerializeField] private string minimapArtId;
    [SerializeField, Min(1)] private int gridWidth = 64;
    [SerializeField, Min(1)] private int gridHeight = 36;
    [SerializeField, Min(0.001f)] private float cellSize = 0.05f;
    [SerializeField] private Vector2 worldOrigin;
    [SerializeField] private Vector2 visibleWorldSize = new(3.4f, 1.92f);
    [SerializeField] private Vector2 cameraDefaultCenter;
    [SerializeField, Min(0.01f)] private float defaultOrthographicSize = 0.597f;
    [SerializeField] private Rect cameraBounds = new(-1.5f, -0.85f, 3f, 1.7f);
    [SerializeField] private TacticalMapAnchor[] anchors = Array.Empty<TacticalMapAnchor>();
    [SerializeField] private TacticalMapSurface[] surfaces = Array.Empty<TacticalMapSurface>();
    [SerializeField] private TacticalMapRoute[] routes = Array.Empty<TacticalMapRoute>();
    [SerializeField] private TacticalMapEntityFootprint[] entityFootprints = Array.Empty<TacticalMapEntityFootprint>();
    [SerializeField] private string[] commandReasonCodes = Array.Empty<string>();

    public string MapId => mapId;
    public string LevelId => levelId;
    public string MissionId => missionId;
    public string ScenarioSetupId => scenarioSetupId;
    public Sprite GroundSprite => groundSprite;
    public string MapPreviewArtId => mapPreviewArtId;
    public string MinimapArtId => minimapArtId;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float CellSize => cellSize;
    public Vector2 WorldOrigin => worldOrigin;
    public Vector2 VisibleWorldSize => visibleWorldSize;
    public Vector2 CameraDefaultCenter => cameraDefaultCenter;
    public float DefaultOrthographicSize => defaultOrthographicSize;
    public Rect CameraBounds => cameraBounds;
    public TacticalMapAnchor[] Anchors => anchors;
    public TacticalMapSurface[] Surfaces => surfaces;
    public TacticalMapRoute[] Routes => routes;
    public TacticalMapEntityFootprint[] EntityFootprints => entityFootprints;
    public string[] CommandReasonCodes => commandReasonCodes;

    public Vector2 NormalizedToWorld(Vector2 normalizedPosition)
    {
        float x = worldOrigin.x + normalizedPosition.x * visibleWorldSize.x;
        float y = worldOrigin.y + normalizedPosition.y * visibleWorldSize.y;
        return new Vector2(x, y);
    }

    public Vector2Int NormalizedToCell(Vector2 normalizedPosition)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(normalizedPosition.x * (gridWidth - 1)), 0, gridWidth - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(normalizedPosition.y * (gridHeight - 1)), 0, gridHeight - 1);
        return new Vector2Int(x, y);
    }

    public bool TryGetAnchor(string anchorId, out TacticalMapAnchor anchor)
    {
        foreach (TacticalMapAnchor candidate in anchors)
        {
            if (candidate.Id == anchorId)
            {
                anchor = candidate;
                return true;
            }
        }

        anchor = default;
        return false;
    }

    public void Configure(
        string mapId,
        string levelId,
        string missionId,
        string scenarioSetupId,
        Sprite groundSprite,
        string mapPreviewArtId,
        string minimapArtId,
        int gridWidth,
        int gridHeight,
        float cellSize,
        Vector2 worldOrigin,
        Vector2 visibleWorldSize,
        Vector2 cameraDefaultCenter,
        float defaultOrthographicSize,
        Rect cameraBounds,
        TacticalMapAnchor[] anchors,
        TacticalMapSurface[] surfaces,
        TacticalMapRoute[] routes,
        TacticalMapEntityFootprint[] entityFootprints,
        string[] commandReasonCodes)
    {
        this.mapId = mapId;
        this.levelId = levelId;
        this.missionId = missionId;
        this.scenarioSetupId = scenarioSetupId;
        this.groundSprite = groundSprite;
        this.mapPreviewArtId = mapPreviewArtId;
        this.minimapArtId = minimapArtId;
        this.gridWidth = Mathf.Max(1, gridWidth);
        this.gridHeight = Mathf.Max(1, gridHeight);
        this.cellSize = Mathf.Max(0.001f, cellSize);
        this.worldOrigin = worldOrigin;
        this.visibleWorldSize = visibleWorldSize;
        this.cameraDefaultCenter = cameraDefaultCenter;
        this.defaultOrthographicSize = Mathf.Max(0.01f, defaultOrthographicSize);
        this.cameraBounds = cameraBounds;
        this.anchors = anchors ?? Array.Empty<TacticalMapAnchor>();
        this.surfaces = surfaces ?? Array.Empty<TacticalMapSurface>();
        this.routes = routes ?? Array.Empty<TacticalMapRoute>();
        this.entityFootprints = entityFootprints ?? Array.Empty<TacticalMapEntityFootprint>();
        this.commandReasonCodes = commandReasonCodes ?? Array.Empty<string>();
    }
}

[Serializable]
public struct TacticalMapAnchor
{
    [SerializeField] private string id;
    [SerializeField] private TacticalMapAnchorType type;
    [SerializeField] private Vector2 normalizedPosition;
    [SerializeField] private string notes;

    public string Id => id;
    public TacticalMapAnchorType Type => type;
    public Vector2 NormalizedPosition => normalizedPosition;
    public string Notes => notes;

    public TacticalMapAnchor(string id, TacticalMapAnchorType type, Vector2 normalizedPosition, string notes = "")
    {
        this.id = id;
        this.type = type;
        this.normalizedPosition = normalizedPosition;
        this.notes = notes;
    }
}

[Serializable]
public struct TacticalMapSurface
{
    [SerializeField] private string id;
    [SerializeField] private TacticalMapSurfaceType type;
    [SerializeField] private Rect normalizedBounds;
    [SerializeField] private string notes;

    public string Id => id;
    public TacticalMapSurfaceType Type => type;
    public Rect NormalizedBounds => normalizedBounds;
    public string Notes => notes;

    public TacticalMapSurface(string id, TacticalMapSurfaceType type, Rect normalizedBounds, string notes = "")
    {
        this.id = id;
        this.type = type;
        this.normalizedBounds = normalizedBounds;
        this.notes = notes;
    }
}

[Serializable]
public struct TacticalMapRoute
{
    [SerializeField] private string id;
    [SerializeField] private Vector2[] normalizedWaypoints;
    [SerializeField] private string notes;

    public string Id => id;
    public Vector2[] NormalizedWaypoints => normalizedWaypoints;
    public string Notes => notes;

    public TacticalMapRoute(string id, Vector2[] normalizedWaypoints, string notes = "")
    {
        this.id = id;
        this.normalizedWaypoints = normalizedWaypoints ?? Array.Empty<Vector2>();
        this.notes = notes;
    }
}

[Serializable]
public struct TacticalMapEntityFootprint
{
    [SerializeField] private string entityId;
    [SerializeField] private Vector2Int footprintCells;
    [SerializeField] private string notes;

    public string EntityId => entityId;
    public Vector2Int FootprintCells => footprintCells;
    public string Notes => notes;

    public TacticalMapEntityFootprint(string entityId, Vector2Int footprintCells, string notes = "")
    {
        this.entityId = entityId;
        this.footprintCells = footprintCells;
        this.notes = notes;
    }
}

public enum TacticalMapAnchorType
{
    Spawn,
    Camera,
    MoveTarget,
    RouteWaypoint,
    Objective,
    Threat,
    Minimap
}

public enum TacticalMapSurfaceType
{
    Walkable,
    MainRoad,
    RoadShoulder,
    CommandPointPad,
    Cover,
    Blocked,
    CivilianZone
}

public sealed class TacticalMapDefinitionReference : MonoBehaviour
{
    [SerializeField] private TacticalMapDefinition definition;

    public TacticalMapDefinition Definition => definition;

    public void Configure(TacticalMapDefinition definition)
    {
        this.definition = definition;
    }
}
