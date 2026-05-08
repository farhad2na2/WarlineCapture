using System;
using UnityEngine;

[CreateAssetMenu(menuName = "WarlineCapture/Chapter 1 Tactical Asset Manifest")]
public sealed class Chapter01TacticalAssetManifest : ScriptableObject
{
    [SerializeField] private string manifestId = "chapter01.tactical.assets.v1";
    [SerializeField] private TacticalAssetManifestEntry[] entries = Array.Empty<TacticalAssetManifestEntry>();

    public string ManifestId => manifestId;
    public TacticalAssetManifestEntry[] Entries => entries;

    public bool TryGetEntry(string assetId, out TacticalAssetManifestEntry entry)
    {
        foreach (TacticalAssetManifestEntry candidate in entries)
        {
            if (candidate.AssetId == assetId)
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

    public void ConfigureDefaults()
    {
        manifestId = "chapter01.tactical.assets.v1";
        entries = new[]
        {
            Entry("iso.ch01.district_edge_01.ground", TacticalAssetCategory.GroundPlate, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Art/Generated/IsometricMaps/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a.png", "saga.ch01.m01.first_contact", "Accepted close tactical proxy until final M01 ground plate is approved."),
            Entry("iso.ch01.district_edge_01.metadata", TacticalAssetCategory.Metadata, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset", "saga.ch01.m01.first_contact", "Current M01 tactical map definition asset."),
            Entry("preview.ch01.first_contact", TacticalAssetCategory.MapPreview, TacticalAssetStatus.Planned, "Assets/Game/Art/UI/Generated/MissionArt/saga_ch01_m01_first_contact_MapPreview.png", "saga.ch01.m01.first_contact", "Strategic planning preview for briefing/Saga map."),
            Entry("minimap.ch01.first_contact", TacticalAssetCategory.Minimap, TacticalAssetStatus.Planned, "Assets/Game/Art/UI/Generated/MissionArt/saga_ch01_m01_first_contact_Minimap.png", "saga.ch01.m01.first_contact", "Battle HUD minimap art aligned to tactical bounds."),
            Entry("unit.player.rifle_squad_01", TacticalAssetCategory.Unit, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites/infantry_squad.png", "saga.ch01.m01.first_contact", "Uses InfantrySquad scale role; production atlas frames must include fixed-direction baked/contact shadows matched to the M01 tactical ground light.", true, TacticalVisualScaleRole.InfantrySquad),
            Entry("unit.enemy.patrol_01", TacticalAssetCategory.Unit, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites/infantry_squad.png", "saga.ch01.m01.first_contact", "Temporary enemy-tinted infantry until hostile variant is approved; production atlas frames must include fixed-direction baked/contact shadows matched to the M01 tactical ground light.", true, TacticalVisualScaleRole.InfantrySquad),
            Entry("decor.command_point", TacticalAssetCategory.Building, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites/command_building.png", "saga.ch01.m01.first_contact", "Non-attackable command/decor proxy; production atlas frame must include fixed-direction baked/contact shadow matched to the M01 tactical ground light.", true, TacticalVisualScaleRole.CommandBuilding),
            Entry("building.ch01.forward_barracks", TacticalAssetCategory.Building, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Buildings/building_ch01_forward_barracks.png", "saga.ch01.m02.establish_base", "Required build target for M02.", true, TacticalVisualScaleRole.TentCluster),
            Entry("building.ch01.guard_tower", TacticalAssetCategory.Building, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Buildings/building_ch01_guard_tower.png", "saga.ch01.m03.radar_warning", "M03 warning/defense unlock."),
            Entry("building.ch01.radar_dish", TacticalAssetCategory.Building, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Buildings/building_ch01_radar_dish.png", "saga.ch01.m03.radar_warning", "Radar ping support anchor."),
            Entry("vehicle.ch01.apc", TacticalAssetCategory.Vehicle, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites/apc.png", "saga.ch01.m05.breach_assault", "APC visual scale anchor.", true, TacticalVisualScaleRole.Apc),
            Entry("vehicle.ch01.battle_tank", TacticalAssetCategory.Vehicle, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites/battle_tank.png", "saga.ch01.m05.breach_assault", "Tank visual scale anchor.", true, TacticalVisualScaleRole.BattleTank),
            Entry("air.ch01.transport_helicopter", TacticalAssetCategory.AirUnit, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Art/Generated/IsometricMaps/TehranStrategicMap_A/ScaleMatchTest/Sprites/helicopter.png", "saga.ch01.m04.airlift", "Temporary helicopter scale target.", true, TacticalVisualScaleRole.Helicopter),
            Entry("building.ch01.fuel_refinery_module", TacticalAssetCategory.IndustrialBuilding, TacticalAssetStatus.ExistsNeedsReview, "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites/fuel_refinery_module.png", "saga.ch01.m04.airlift", "Large industrial building scale anchor.", true, TacticalVisualScaleRole.FuelRefineryModule),
            Entry("building.enemy.fortified_core_01", TacticalAssetCategory.AttackTarget, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Buildings/building_enemy_fortified_core_01.png", "saga.ch01.m05.breach_assault", "M05 primary attack target."),
            Entry("building.enemy.wall_gate_01", TacticalAssetCategory.AttackTarget, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Buildings/building_enemy_wall_gate_01.png", "saga.ch01.m05.breach_assault", "M05 breach target."),
            Entry("marker.selection.ring", TacticalAssetCategory.Marker, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Markers/marker_selection_ring.png", "saga.ch01.m01.first_contact", "Selection feedback marker."),
            Entry("marker.move.destination", TacticalAssetCategory.Marker, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Markers/marker_move_destination.png", "saga.ch01.m01.first_contact", "Accepted move-command marker."),
            Entry("marker.attack.target", TacticalAssetCategory.Marker, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Markers/marker_attack_target.png", "saga.ch01.m01.first_contact", "Accepted attack-command marker."),
            Entry("marker.objective.focus", TacticalAssetCategory.Marker, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/Markers/marker_objective_focus.png", "saga.ch01.m01.first_contact", "Objective row and ARIA focus marker."),
            Entry("vfx.impact.light", TacticalAssetCategory.Vfx, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/VFX/vfx_impact_light.png", "saga.ch01.m01.first_contact", "Small readable hit effect."),
            Entry("vfx.unit.destroyed.small", TacticalAssetCategory.Vfx, TacticalAssetStatus.Planned, "Assets/Game/Art/Generated/2DISO/Chapter01/VFX/vfx_unit_destroyed_small.png", "saga.ch01.m01.first_contact", "Objective completion destruction feedback."),
        };
    }

    private static TacticalAssetManifestEntry Entry(
        string assetId,
        TacticalAssetCategory category,
        TacticalAssetStatus status,
        string plannedPath,
        string usedByMissionId,
        string notes,
        bool usesScaleRole = false,
        TacticalVisualScaleRole scaleRole = TacticalVisualScaleRole.InfantrySquad)
    {
        return new TacticalAssetManifestEntry(
            assetId,
            category,
            status,
            plannedPath,
            new[] { usedByMissionId },
            usesScaleRole,
            scaleRole,
            notes);
    }
}

[Serializable]
public struct TacticalAssetManifestEntry
{
    [SerializeField] private string assetId;
    [SerializeField] private TacticalAssetCategory category;
    [SerializeField] private TacticalAssetStatus status;
    [SerializeField] private string plannedPath;
    [SerializeField] private string[] usedByMissionIds;
    [SerializeField] private bool usesScaleRole;
    [SerializeField] private TacticalVisualScaleRole scaleRole;
    [SerializeField] private string notes;

    public string AssetId => assetId;
    public TacticalAssetCategory Category => category;
    public TacticalAssetStatus Status => status;
    public string PlannedPath => plannedPath;
    public string[] UsedByMissionIds => usedByMissionIds;
    public bool UsesScaleRole => usesScaleRole;
    public TacticalVisualScaleRole ScaleRole => scaleRole;
    public string Notes => notes;

    public TacticalAssetManifestEntry(
        string assetId,
        TacticalAssetCategory category,
        TacticalAssetStatus status,
        string plannedPath,
        string[] usedByMissionIds,
        bool usesScaleRole,
        TacticalVisualScaleRole scaleRole,
        string notes)
    {
        this.assetId = assetId;
        this.category = category;
        this.status = status;
        this.plannedPath = plannedPath;
        this.usedByMissionIds = usedByMissionIds ?? Array.Empty<string>();
        this.usesScaleRole = usesScaleRole;
        this.scaleRole = scaleRole;
        this.notes = notes;
    }
}

public enum TacticalAssetCategory
{
    GroundPlate,
    Metadata,
    MapPreview,
    Minimap,
    Unit,
    Vehicle,
    AirUnit,
    Building,
    IndustrialBuilding,
    AttackTarget,
    Marker,
    Vfx
}

public enum TacticalAssetStatus
{
    Planned,
    ExistsNeedsReview,
    Approved,
    Rejected
}
