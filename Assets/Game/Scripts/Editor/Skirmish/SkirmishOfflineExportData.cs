#if UNITY_EDITOR
using System;
using Game.Configs;
namespace Game.Editor
{
    [Serializable] public sealed class OfflineSourcePin { public string path, guid, sha256, status; public long bytes; }
    [Serializable] public sealed class OfflineArtifact { public string path, sha256, encoding; public long bytes; public int count, stride; }
    [Serializable] public sealed class OfflinePoint { public string id, role, coordinate_source; public float world_x, world_z; public int cell_x, cell_z; public bool has_cell, in_bounds; }
    [Serializable] public sealed class OfflineAnchor { public SkirmishLayoutAnchorConfig authored; public OfflinePoint projected; }
    [Serializable] public sealed class OfflinePad { public SkirmishLegalPadConfig authored; public OfflinePoint projected; }
    [Serializable] public sealed class OfflineReason { public string code, field, detail; }
    [Serializable] public sealed class OfflineVariant
    {
        public string size, difficulty; public int seed; public bool compile_success, static_budget_available, static_budget_exact;
        public OfflineReason[] compile_reasons = Array.Empty<OfflineReason>();
        public string[] static_budget_errors = Array.Empty<string>();
        public SkirmishResolvedSetup setup;
        public SkirmishResolvedForceEntry[] forces = Array.Empty<SkirmishResolvedForceEntry>();
        public SkirmishResolvedStructureEntry[] structures = Array.Empty<SkirmishResolvedStructureEntry>();
        public OfflinePoint[] objective_area_points = Array.Empty<OfflinePoint>();
        public OfflinePoint[] desired_force_positions = Array.Empty<OfflinePoint>(), desired_structure_positions = Array.Empty<OfflinePoint>();
        public SkirmishObjectiveAreaConfig[] objective_area_configs = Array.Empty<SkirmishObjectiveAreaConfig>();
        public string position_scope = "Desired authored anchors only; runtime spawn search/footprints/reservations/grounding not executed.";
    }
    [Serializable] public sealed class OfflineMission
    {
        public string canonical_id, map_key, definition_id, objective_json, logical_binding_json;
        public SkirmishBattleCatalogEntry[] catalog_rows = Array.Empty<SkirmishBattleCatalogEntry>();
        public string[] definition_refs = Array.Empty<string>(), reasons = Array.Empty<string>();
        public OfflineAnchor[] anchors = Array.Empty<OfflineAnchor>();
        public SkirmishLayoutRouteConfig[] routes = Array.Empty<SkirmishLayoutRouteConfig>();
        public OfflinePad[] pads = Array.Empty<OfflinePad>();
        public SkirmishLayoutWorldBindingConfig world_binding;
        public OfflineVariant[] variants = Array.Empty<OfflineVariant>();
    }
    [Serializable] public sealed class OfflineMapMetadata
    {
        public OperationMapBoundsConfig bounds; public OperationMapSourceBindingConfig source_binding;
        public OperationMapGridMetadataConfig grid; public OperationMapSurfaceMetadataConfig surface; public OperationMapNavigationMetadataConfig navigation;
    }
    [Serializable] public sealed class OfflineMap
    {
        public string key, definition_path, operation_map_id, source_scene_path, source_scene_guid, grid_path, surface_path, runtime_blob_hash;
        public string[] campaign_mission_ids = Array.Empty<string>(), campaign_definition_paths = Array.Empty<string>(), reasons = Array.Empty<string>();
        public int width, height; public float origin_x, origin_y, origin_z, cell_size;
        public bool authored_grid_available, decoded_surface_available, surface_grid_matches, campaign_physical_source_member;
        public string walkable_artifact, infantry_surface_artifact, cells_artifact, samples_artifact, connections_artifact, compact_samples_artifact;
        public int surface_width, surface_height; public float surface_origin_x, surface_origin_y, surface_origin_z, surface_cell_size;
        public int runtime_encoding; public float compact_min_height, compact_height_step;
        public OfflineMapMetadata metadata;
        public OfflinePoint[] map_anchors = Array.Empty<OfflinePoint>();
        public string source_identity_hash, content_hash, generated_metadata_hash, grid_content_hash, surface_content_hash, surface_metadata_runtime_hash;
        public int surface_payload_version, surface_payload_encoding; public bool surface_generated_flat_equivalent;
        public string[] runtime_navigation_gaps = { "Runtime static building blockers/openings and scene overlays not captured", "Dynamic blockers, occupancy and road projections not captured", "Surface cell admission alone does not certify layered edge connectivity", "No runtime actors, spawn placement or full mission readiness certified" };
    }
    [Serializable] public sealed class OfflineCampaignRow
    { public string mission_id, definition_path, scenario_path, operation_map_id, map_path, scene_guid; public string[] reasons = Array.Empty<string>(); }
    [Serializable] public sealed class OfflineExportManifest
    {
        public int schema_version = 1;
        public string exporter_scope = "Read-only actual authored asset snapshot; static prevalidation only";
        public string producer = "Game.Editor.SkirmishOfflineDataExporter.Export";
        public string required_pass_marker = "[SkirmishOfflineDataExporter] result=Passed";
        public string catalog_selection, compiler_catalog_scope, selected_catalog_guid; public long selected_catalog_local_id;
        public string menu_path, campaign_catalog_path, campaign_map_catalog_path, battle_catalog_path, expansion_catalog_path, matrix_path;
        public string[] reasons = Array.Empty<string>();
        public OfflineCampaignRow[] campaign_rows = Array.Empty<OfflineCampaignRow>();
        public OfflineMission[] missions = Array.Empty<OfflineMission>();
        public OfflineMap[] maps = Array.Empty<OfflineMap>();
        public OfflineArtifact[] artifacts = Array.Empty<OfflineArtifact>();
        public OfflineSourcePin[] source_pins = Array.Empty<OfflineSourcePin>();
    }
    [Serializable] public sealed class OfflineExportReceipt
    { public int schema_version = 1; public string manifest_sha256, required_pass_marker = "[SkirmishOfflineDataExporter] result=Passed"; public bool source_pins_unchanged; }
}
#endif
