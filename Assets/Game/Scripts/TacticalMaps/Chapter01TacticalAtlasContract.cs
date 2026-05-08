using System;
using UnityEngine;

[CreateAssetMenu(menuName = "WarlineCapture/Chapter 1 Tactical Atlas Contract")]
public sealed class Chapter01TacticalAtlasContract : ScriptableObject
{
    [SerializeField] private string contractId = "chapter01.tactical.atlases.v1";
    [SerializeField] private TacticalAtlasDefinition[] atlases = Array.Empty<TacticalAtlasDefinition>();

    public string ContractId => contractId;
    public TacticalAtlasDefinition[] Atlases => atlases;

    public bool TryGetSprite(string spriteId, out TacticalAtlasSpriteEntry sprite)
    {
        foreach (TacticalAtlasDefinition atlas in atlases)
        {
            foreach (TacticalAtlasSpriteEntry candidate in atlas.Sprites)
            {
                if (candidate.SpriteId == spriteId)
                {
                    sprite = candidate;
                    return true;
                }
            }
        }

        sprite = default;
        return false;
    }

    public void ConfigureDefaults()
    {
        contractId = "chapter01.tactical.atlases.v1";
        atlases = new[]
        {
            new TacticalAtlasDefinition(
                "chapter01_units_atlas",
                TacticalAtlasCategory.Units,
                "Assets/Game/Art/Generated/2DISO/Chapter01/Atlases/chapter01_units_atlas.spriteatlas",
                2048,
                new[]
                {
                    Sprite("unit.player.rifle_squad_01", "unit.player.rifle_squad_01", TacticalSortingClass.Unit, new Vector2(0.5f, 0.15f), new Rect(0.22f, 0.10f, 0.56f, 0.72f), new Vector2Int(1, 1), "FriendlyInfantry", "saga.ch01.m01.first_contact", true, TacticalVisualScaleRole.InfantrySquad),
                    Sprite("unit.enemy.patrol_01", "unit.enemy.patrol_01", TacticalSortingClass.Unit, new Vector2(0.5f, 0.15f), new Rect(0.22f, 0.10f, 0.56f, 0.72f), new Vector2Int(1, 1), "EnemyInfantry", "saga.ch01.m01.first_contact", true, TacticalVisualScaleRole.InfantrySquad),
                }),
            new TacticalAtlasDefinition(
                "chapter01_vehicles_atlas",
                TacticalAtlasCategory.Vehicles,
                "Assets/Game/Art/Generated/2DISO/Chapter01/Atlases/chapter01_vehicles_atlas.spriteatlas",
                2048,
                new[]
                {
                    Sprite("vehicle.ch01.apc", "vehicle.ch01.apc", TacticalSortingClass.Vehicle, new Vector2(0.5f, 0.18f), new Rect(0.10f, 0.12f, 0.80f, 0.62f), new Vector2Int(2, 2), "Apc", "saga.ch01.m05.breach_assault", true, TacticalVisualScaleRole.Apc),
                    Sprite("vehicle.ch01.battle_tank", "vehicle.ch01.battle_tank", TacticalSortingClass.Vehicle, new Vector2(0.5f, 0.18f), new Rect(0.08f, 0.12f, 0.84f, 0.62f), new Vector2Int(2, 2), "BattleTank", "saga.ch01.m05.breach_assault", true, TacticalVisualScaleRole.BattleTank),
                    Sprite("air.ch01.transport_helicopter", "air.ch01.transport_helicopter", TacticalSortingClass.AirUnit, new Vector2(0.5f, 0.35f), new Rect(0.10f, 0.16f, 0.80f, 0.68f), new Vector2Int(3, 3), "TransportHelicopter", "saga.ch01.m04.airlift", true, TacticalVisualScaleRole.Helicopter),
                }),
            new TacticalAtlasDefinition(
                "chapter01_buildings_atlas",
                TacticalAtlasCategory.Buildings,
                "Assets/Game/Art/Generated/2DISO/Chapter01/Atlases/chapter01_buildings_atlas.spriteatlas",
                4096,
                new[]
                {
                    Sprite("decor.command_point", "decor.command_point", TacticalSortingClass.Building, new Vector2(0.5f, 0.12f), new Rect(0.12f, 0.08f, 0.76f, 0.76f), new Vector2Int(4, 3), "DecorCommandPoint", "saga.ch01.m01.first_contact", true, TacticalVisualScaleRole.CommandBuilding),
                    Sprite("building.ch01.forward_barracks", "building.ch01.forward_barracks", TacticalSortingClass.Building, new Vector2(0.5f, 0.12f), new Rect(0.10f, 0.10f, 0.80f, 0.72f), new Vector2Int(4, 3), "ForwardBarracks", "saga.ch01.m02.establish_base", true, TacticalVisualScaleRole.TentCluster),
                    Sprite("building.ch01.guard_tower", "building.ch01.guard_tower", TacticalSortingClass.Building, new Vector2(0.5f, 0.10f), new Rect(0.20f, 0.08f, 0.60f, 0.82f), new Vector2Int(2, 2), "GuardTower", "saga.ch01.m03.radar_warning"),
                    Sprite("building.ch01.radar_dish", "building.ch01.radar_dish", TacticalSortingClass.Building, new Vector2(0.5f, 0.12f), new Rect(0.18f, 0.10f, 0.64f, 0.70f), new Vector2Int(2, 2), "RadarDish", "saga.ch01.m03.radar_warning"),
                    Sprite("building.ch01.fuel_refinery_module", "building.ch01.fuel_refinery_module", TacticalSortingClass.Building, new Vector2(0.5f, 0.12f), new Rect(0.08f, 0.08f, 0.84f, 0.78f), new Vector2Int(6, 4), "FuelRefinery", "saga.ch01.m04.airlift", true, TacticalVisualScaleRole.FuelRefineryModule),
                    Sprite("building.enemy.fortified_core_01", "building.enemy.fortified_core_01", TacticalSortingClass.Building, new Vector2(0.5f, 0.12f), new Rect(0.08f, 0.08f, 0.84f, 0.80f), new Vector2Int(6, 5), "EnemyCore", "saga.ch01.m05.breach_assault"),
                    Sprite("building.enemy.wall_gate_01", "building.enemy.wall_gate_01", TacticalSortingClass.Building, new Vector2(0.5f, 0.16f), new Rect(0.05f, 0.10f, 0.90f, 0.70f), new Vector2Int(5, 2), "BreachGate", "saga.ch01.m05.breach_assault"),
                }),
            new TacticalAtlasDefinition(
                "chapter01_vfx_decals_atlas",
                TacticalAtlasCategory.VfxDecals,
                "Assets/Game/Art/Generated/2DISO/Chapter01/Atlases/chapter01_vfx_decals_atlas.spriteatlas",
                2048,
                new[]
                {
                    Sprite("marker.selection.ring", "marker.selection.ring", TacticalSortingClass.Overlay, new Vector2(0.5f, 0.5f), new Rect(0f, 0f, 1f, 1f), new Vector2Int(1, 1), "SelectionMarker", "saga.ch01.m01.first_contact"),
                    Sprite("marker.move.destination", "marker.move.destination", TacticalSortingClass.Overlay, new Vector2(0.5f, 0.5f), new Rect(0f, 0f, 1f, 1f), new Vector2Int(1, 1), "MoveMarker", "saga.ch01.m01.first_contact"),
                    Sprite("marker.attack.target", "marker.attack.target", TacticalSortingClass.Overlay, new Vector2(0.5f, 0.5f), new Rect(0f, 0f, 1f, 1f), new Vector2Int(1, 1), "AttackMarker", "saga.ch01.m01.first_contact"),
                    Sprite("marker.objective.focus", "marker.objective.focus", TacticalSortingClass.Overlay, new Vector2(0.5f, 0.5f), new Rect(0f, 0f, 1f, 1f), new Vector2Int(1, 1), "ObjectiveMarker", "saga.ch01.m01.first_contact"),
                    Sprite("vfx.impact.light", "vfx.impact.light", TacticalSortingClass.Vfx, new Vector2(0.5f, 0.5f), new Rect(0f, 0f, 1f, 1f), new Vector2Int(1, 1), "ImpactLight", "saga.ch01.m01.first_contact"),
                    Sprite("vfx.unit.destroyed.small", "vfx.unit.destroyed.small", TacticalSortingClass.Vfx, new Vector2(0.5f, 0.5f), new Rect(0f, 0f, 1f, 1f), new Vector2Int(1, 1), "DestroyedSmall", "saga.ch01.m01.first_contact"),
                }),
        };
    }

    private static TacticalAtlasSpriteEntry Sprite(
        string spriteId,
        string manifestAssetId,
        TacticalSortingClass sortingClass,
        Vector2 pivot,
        Rect selectionBounds,
        Vector2Int colliderFootprintCells,
        string gameplayClass,
        string usedByMissionId,
        bool usesScaleRole = false,
        TacticalVisualScaleRole scaleRole = TacticalVisualScaleRole.InfantrySquad)
    {
        return new TacticalAtlasSpriteEntry(spriteId, manifestAssetId, sortingClass, pivot, selectionBounds, colliderFootprintCells, gameplayClass, new[] { usedByMissionId }, usesScaleRole, scaleRole);
    }
}

[Serializable]
public struct TacticalAtlasDefinition
{
    [SerializeField] private string atlasId;
    [SerializeField] private TacticalAtlasCategory category;
    [SerializeField] private string outputPath;
    [SerializeField, Min(1)] private int maxTextureSize;
    [SerializeField] private TacticalAtlasSpriteEntry[] sprites;

    public string AtlasId => atlasId;
    public TacticalAtlasCategory Category => category;
    public string OutputPath => outputPath;
    public int MaxTextureSize => maxTextureSize;
    public TacticalAtlasSpriteEntry[] Sprites => sprites;

    public TacticalAtlasDefinition(string atlasId, TacticalAtlasCategory category, string outputPath, int maxTextureSize, TacticalAtlasSpriteEntry[] sprites)
    {
        this.atlasId = atlasId;
        this.category = category;
        this.outputPath = outputPath;
        this.maxTextureSize = maxTextureSize;
        this.sprites = sprites ?? Array.Empty<TacticalAtlasSpriteEntry>();
    }
}

[Serializable]
public struct TacticalAtlasSpriteEntry
{
    [SerializeField] private string spriteId;
    [SerializeField] private string manifestAssetId;
    [SerializeField] private TacticalSortingClass sortingClass;
    [SerializeField] private Vector2 pivot;
    [SerializeField] private Rect selectionBounds;
    [SerializeField] private Vector2Int colliderFootprintCells;
    [SerializeField] private string gameplayClass;
    [SerializeField] private string[] usedByMissionIds;
    [SerializeField] private bool usesScaleRole;
    [SerializeField] private TacticalVisualScaleRole scaleRole;

    public string SpriteId => spriteId;
    public string ManifestAssetId => manifestAssetId;
    public TacticalSortingClass SortingClass => sortingClass;
    public Vector2 Pivot => pivot;
    public Rect SelectionBounds => selectionBounds;
    public Vector2Int ColliderFootprintCells => colliderFootprintCells;
    public string GameplayClass => gameplayClass;
    public string[] UsedByMissionIds => usedByMissionIds;
    public bool UsesScaleRole => usesScaleRole;
    public TacticalVisualScaleRole ScaleRole => scaleRole;

    public TacticalAtlasSpriteEntry(
        string spriteId,
        string manifestAssetId,
        TacticalSortingClass sortingClass,
        Vector2 pivot,
        Rect selectionBounds,
        Vector2Int colliderFootprintCells,
        string gameplayClass,
        string[] usedByMissionIds,
        bool usesScaleRole,
        TacticalVisualScaleRole scaleRole)
    {
        this.spriteId = spriteId;
        this.manifestAssetId = manifestAssetId;
        this.sortingClass = sortingClass;
        this.pivot = pivot;
        this.selectionBounds = selectionBounds;
        this.colliderFootprintCells = colliderFootprintCells;
        this.gameplayClass = gameplayClass;
        this.usedByMissionIds = usedByMissionIds ?? Array.Empty<string>();
        this.usesScaleRole = usesScaleRole;
        this.scaleRole = scaleRole;
    }
}

public enum TacticalAtlasCategory
{
    Units,
    Vehicles,
    Buildings,
    VfxDecals
}

public enum TacticalSortingClass
{
    Unit,
    Vehicle,
    AirUnit,
    Building,
    Overlay,
    Vfx
}
