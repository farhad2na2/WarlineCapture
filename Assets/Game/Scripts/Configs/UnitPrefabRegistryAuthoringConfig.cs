using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class UnitImpostorAtlasEntry
{
    private const float DefaultGroundAnchorNormalized = 0.25f;

    [SerializeField] private GameObject prefab;
    [SerializeField] private Texture2D atlas;
    [SerializeField, Min(1)] private int directionCount = 8;
    [SerializeField, Min(1)] private int columns = 4;
    [SerializeField, Min(1)] private int rows = 2;
    [SerializeField] private Vector2 size = new(1f, 1.8f);
    [SerializeField, Range(0f, 0.45f)] private float groundAnchorNormalized = DefaultGroundAnchorNormalized;

    public GameObject Prefab => prefab;
    public Texture2D Atlas => atlas;
    public int DirectionCount => Mathf.Max(1, directionCount);
    public int Columns => Mathf.Max(1, columns);
    public int Rows => Mathf.Max(1, rows);
    public Vector2 Size => size;
    public float GroundAnchorNormalized => groundAnchorNormalized > 0f
        ? Mathf.Clamp(groundAnchorNormalized, 0f, 0.45f)
        : DefaultGroundAnchorNormalized;
}

[CreateAssetMenu(menuName = "Game/Config/Unit Prefab Registry")]
public class UnitPrefabRegistryAuthoringConfig : ScriptableObject, IUiCatalogPrefabSource
{
    [SerializeField] private List<GameObject> unitSpawnPrefabs = new();
    [SerializeField] private List<UnitImpostorAtlasEntry> impostorAtlases = new();
    [SerializeField] private GameObject unitSelectionMarkerPrefab;
    [SerializeField] private GameObject unitHealthBarPrefab;

    public List<GameObject> UnitSpawnPrefabs => unitSpawnPrefabs;
    public List<UnitImpostorAtlasEntry> ImpostorAtlases => impostorAtlases;
    public GameObject UnitSelectionMarkerPrefab => unitSelectionMarkerPrefab;
    public GameObject UnitHealthBarPrefab => unitHealthBarPrefab;

    IReadOnlyList<GameObject> IUiCatalogPrefabSource.UnitSpawnPrefabs => unitSpawnPrefabs;
    IReadOnlyList<GameObject> IUiCatalogPrefabSource.BuildingSpawnPrefabs => null;
}
