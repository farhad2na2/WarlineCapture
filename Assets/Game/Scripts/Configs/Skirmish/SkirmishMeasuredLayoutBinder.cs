using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// Draft for review: projects explicit logical data into an existing Campaign source.
    /// A successful binding proves configuration consistency only, never native clearance.
    /// Caller must preserve the fingerprint in candidate/setup identity before qualification.
    /// </summary>
    public static class SkirmishMeasuredLayoutBinder
    {
        // Compatibility adapter only for the existing packaged four bindings.
        // New DB-named entries or altered legacy layout/source contracts use generic
        // binding and its actual source bounds/fingerprint instead.
        public static string ComputeLayoutSemanticFingerprint(SkirmishMapLayoutConfig layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if(layout.BindingValueFormat==SkirmishBindingValueFormat.CanonicalBinaryV2)return SkirmishBindingCanonicalValues.LayoutFingerprint(layout);
            if(layout.BindingValueFormat!=SkirmishBindingValueFormat.NativeJsonV1)throw new NotSupportedException("Unsupported binding value format.");
            if (SkirmishLegacyLayoutValueContract.TryGetCompatibilityFingerprint(layout, out string compatibilityFingerprint)) return SkirmishStructureInstanceBinding.AppendLayoutFingerprint(compatibilityFingerprint,layout);
            using var hash = SHA256.Create();
            return SkirmishStructureInstanceBinding.AppendLayoutFingerprint(BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(LayoutValues.From(layout))))).Replace("-", "").ToLowerInvariant(),layout);
        }
        public static bool PreservesPackagedLegacyBinding(SkirmishResolvedSetup setup, SkirmishScenarioDefinitionConfig definition,
            string reviewedLayoutSemanticFingerprint)
        {
            if (setup == null || definition == null || setup.CatalogId != definition.CatalogId ||
                setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                (setup.CatalogId != "S002" && setup.CatalogId != "S003" && setup.CatalogId != "S004" && setup.CatalogId != "S005") ||
                setup.OperationMapId != SkirmishMapLayoutBuilder.DesertBaseMapId || setup.LayoutId != SkirmishMapLayoutBuilder.DesertBaseLayoutId ||
                definition.MapLayout == null || string.IsNullOrEmpty(reviewedLayoutSemanticFingerprint) ||
                reviewedLayoutSemanticFingerprint != ComputeLayoutSemanticFingerprint(definition.MapLayout)) return false;
            if (setup.CatalogId == "S002" && setup.ContentVersion != 2) return false;
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (catalog == null || !catalog.TryGetDefinition(setup.CatalogId, setup.ContentVersion, out var packaged) || packaged.MapLayout == null) return false;
            if (setup.CatalogId == "S005")
                return definition.CampaignMap != null && definition.CampaignMap == packaged.CampaignMap &&
                    definition.CampaignMap.OperationMapId == "opmap.skirmish.desert_base_01_s005" &&
                    definition.CampaignMap.SourceIdentityHash == "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" &&
                    definition.CampaignMap.ContentHash == "de882aa416c9e65c42fe6e33f383bd89ba1bb4985f0647f048da6630946a96aa" &&
                    definition.MapLayout.WorldBinding.SourceHash == definition.CampaignMap.ContentHash;
            if (definition.CampaignMap != null || packaged.CampaignMap != null) return false;
            if (setup.CatalogId == "S004")
            {
                var fallback = Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap");
                return fallback != null && fallback.OperationMapId == "opmap.skirmish.desert_base_01" &&
                    fallback.SourceIdentityHash == "3702e8f11717773ce79fe38461c50cfeaf0caf86e9a62b0670166d9461aea5db" &&
                    fallback.ContentHash == "e5a78e5b35d94ed9908a0969bbeca3e513d17fdea01265e566ee00916725d914";
            }
            return true; // S002/S003 retain their existing explicit no-CampaignMap path.
        }

        public static bool TryBind(SkirmishResolvedSetup setup, SkirmishScenarioDefinitionConfig definition,
            List<SkirmishCompileReason> reasons, out string fingerprint)
        {
            fingerprint = null;
            if (reasons == null) throw new ArgumentNullException(nameof(reasons));
            int start = reasons.Count;
            var layout = definition?.MapLayout;
            var source = definition?.CampaignMap;
            if (setup == null || layout == null || source == null)
            { Add(reasons, "definition/layout/source", "Explicit setup, logical layout and Campaign source required."); return false; }
            if (setup.CatalogId != definition.CatalogId || setup.OperationMapId != layout.OperationMapId ||
                setup.LayoutId != layout.LayoutId || definition.ObjectiveConfig == null || setup.ObjectiveKind != definition.ObjectiveConfig.Kind ||
                (setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault && setup.ObjectiveKind != SkirmishObjectiveKind.FrontlineControl &&
                 setup.ObjectiveKind != SkirmishObjectiveKind.Breakthrough && setup.ObjectiveKind != SkirmishObjectiveKind.ConvoyEscort))
                Add(reasons, "identity", "Setup must match the explicit supported definition and logical layout.");
            if (!source.TryValidateMetadata(out string error) || !source.TryValidateLocalContentReferences(out error))
                Add(reasons, "campaignSource", error);
            if (source.AdditionalBuildingPlacements != null && source.AdditionalBuildingPlacements.Placements == null)
                Add(reasons, "additionalBuildingPlacements", "Placement list is missing.");
            else if (source.AdditionalBuildingPlacements != null)
                foreach (var placement in source.AdditionalBuildingPlacements.Placements)
                    if (placement == null || (placement.BuildingPrefab != null && string.IsNullOrEmpty(placement.SourcePath)))
                        Add(reasons, "additionalBuildingPlacements", "Each prefab dependency needs its authored stable SourcePath.");
            var frame = layout.WorldBinding;
            bool hasDeclaration=layout.DeclaredConfiguration!=null;
            bool declaredContext=SkirmishStructureInstanceBinding.TryDeclaredContext(layout,setup,reasons,out var declaredPlayer,out var declaredEnemy);

            if (!layout.HasWorldBinding || (hasDeclaration ? !declaredContext : (!frame.HasMeasuredDeployments || !frame.BindAllConfigurations)) ||
                frame.SourceHash != source.ContentHash)
                Add(reasons, "worldBinding", "An all-configuration frame pinned to this exact source is required.");
            if (!Positive(layout.WorldWidthMetres) || !Positive(layout.WorldDepthMetres) || !Positive(layout.CellSize) ||
                !Finite(layout.OriginX) || !Finite(layout.OriginZ) || !Finite(frame.OriginX) || !Finite(frame.OriginZ) ||
                !Positive(frame.ForwardMetres) || !Positive(frame.AcrossMetres) ||
                !Finite(frame.ForwardX) || !Finite(frame.ForwardZ) || !Finite(frame.AcrossX) || !Finite(frame.AcrossZ) ||
                Mathf.Abs(frame.ForwardX * frame.ForwardX + frame.ForwardZ * frame.ForwardZ - 1f) > .01f ||
                Mathf.Abs(frame.AcrossX * frame.AcrossX + frame.AcrossZ * frame.AcrossZ - 1f) > .01f ||
                Mathf.Abs(frame.ForwardX * frame.AcrossX + frame.ForwardZ * frame.AcrossZ) > .01f)
                Add(reasons, "frame", "Finite positive envelope and orthonormal world frame required.");
            if (layout.Anchors == null || layout.Pads == null || layout.Routes == null ||
                setup.Forces == null || setup.Structures == null)
            { Add(reasons, "typedData", "Typed arrays must be present."); return false; }
            if (reasons.Count != start) return false;

            var anchorIds = new HashSet<string>(StringComparer.Ordinal);
            var anchorRoles = new HashSet<string>(StringComparer.Ordinal);
            foreach (var anchor in layout.Anchors)
            {
                if (string.IsNullOrEmpty(anchor.AnchorId) || !anchorIds.Add(anchor.AnchorId) ||
                    string.IsNullOrEmpty(anchor.RoleId) || !anchorRoles.Add(anchor.RoleId))
                    Add(reasons, "anchors", "Anchor IDs and roles must be unique and nonempty.");
                float expectedX = layout.OriginX + anchor.NormalizedU * layout.WorldWidthMetres;
                float expectedZ = layout.OriginZ + anchor.NormalizedV * layout.WorldDepthMetres;
                if (!Finite(anchor.NormalizedU) || !Finite(anchor.NormalizedV) || anchor.NormalizedU < 0 || anchor.NormalizedU > 1 ||
                    anchor.NormalizedV < 0 || anchor.NormalizedV > 1 ||
                    !Finite(anchor.WorldX) || !Finite(anchor.WorldZ) || Mathf.Abs(anchor.WorldX - expectedX) > .05f ||
                    Mathf.Abs(anchor.WorldZ - expectedZ) > .05f ||
                    !layout.TryProjectToMap(anchor.NormalizedU, anchor.NormalizedV, out float x, out float z) || !Inside(source, x, z))
                    Add(reasons, anchor.AnchorId, "Anchor local/normalized/world-frame declaration is inconsistent or out of source bounds.");
            }
            ValidateDeployment(layout, source, "base.player", frame.PlayerAnchorId, hasDeclaration?declaredPlayer:frame.PlayerDeployment, reasons);
            ValidateDeployment(layout, source, "base.enemy", frame.EnemyAnchorId, hasDeclaration?declaredEnemy:frame.EnemyDeployment, reasons);

            var padIds = new HashSet<string>(StringComparer.Ordinal);
            var padKinds = new HashSet<string>(StringComparer.Ordinal);
            var objectiveTruckRoles = new HashSet<string>(StringComparer.Ordinal);
            var defenseRoles = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pad in layout.Pads)
            {
                if (string.IsNullOrEmpty(pad.PadId) || !padIds.Add(pad.PadId) || !anchorIds.Contains(pad.AnchorId) ||
                    !Positive(pad.WidthMetres) || !Positive(pad.DepthMetres) ||
                    pad.FactionId > 2 || pad.Kind == SkirmishLegalPadKind.None)
                    Add(reasons, pad.PadId, "Typed pad declaration invalid.");
                if (pad.Kind == SkirmishLegalPadKind.ObjectiveTruckSpawn)
                {
                    // These three mode-owned bays are resolved by exact public
                    // role, never by the nonunique faction/kind convenience API.
                    if (pad.FactionId != 1 ||
                        (pad.RoleId != "convoy.origin.1" && pad.RoleId != "convoy.origin.2" && pad.RoleId != "convoy.origin.3") ||
                        !objectiveTruckRoles.Add(pad.RoleId) || !layout.TryGetAnchor(pad.RoleId, out var bayAnchor) || bayAnchor.AnchorId != pad.AnchorId)
                        Add(reasons, pad.PadId, "Objective truck bays require unique exact convoy origin roles and faction 1 anchor bindings.");
                }
                else if(pad.Kind==SkirmishLegalPadKind.DefenseApproach)
                {
                    if((definition.ObjectiveConfig.Kind!=SkirmishObjectiveKind.Breakthrough&&definition.ObjectiveConfig.Kind!=SkirmishObjectiveKind.ConvoyEscort)||
                        pad.FactionId!=2||(pad.RoleId!="defense.approach.a"&&pad.RoleId!="defense.approach.b")||
                        !defenseRoles.Add(pad.RoleId)||!layout.TryGetAnchor(pad.RoleId,out var defenseAnchor)||defenseAnchor.AnchorId!=pad.AnchorId)
                        Add(reasons,pad.PadId,"Defense approaches require unique exact faction2 mode-owned anchors/pads.");
                }
                else if (pad.FactionId != 0 && !padKinds.Add(pad.FactionId + "/" + pad.Kind) &&
                    !(declaredContext && SkirmishStructureInstanceBinding.IsExplicitInstanceOnlyPad(layout,setup,pad)))
                    Add(reasons, pad.PadId, "Ambiguous faction/kind pad lookup.");
                foreach (int dx in new[] { -1, 1 }) foreach (int dz in new[] { -1, 1 })
                    if (!layout.TryProjectLocalToMap(pad.CenterX + dx * pad.WidthMetres * .5f,
                        pad.CenterZ + dz * pad.DepthMetres * .5f, out float x, out float z) || !Inside(source, x, z))
                        Add(reasons, pad.PadId, "Declared pad corner leaves actual source bounds.");
            }

            if (objectiveTruckRoles.Count != 0 && objectiveTruckRoles.Count != 3)
                Add(reasons, "convoy.origin", "All three distinct objective truck bays are required.");
            var points = new Vector2[8];
            int point = 0;
            foreach (byte faction in new byte[] { 1, 2 })
                foreach (var kind in new[] { SkirmishLegalPadKind.BaseBarracks, SkirmishLegalPadKind.GroundStaging,
                    SkirmishLegalPadKind.VehicleSpawn, SkirmishLegalPadKind.Rally })
                {
                    if (!layout.TryGetPad(faction, kind, out var pad) || !Project(layout, pad, out points[point]))
                        Add(reasons, faction + "/" + kind, "Required typed pad absent.");
                    point++;
                }
            string defaultRoute = ValidateRoutes(layout, source, anchorIds, reasons);
            var forces = (SkirmishResolvedForceEntry[])setup.Forces.Clone();
            for (int i = 0; i < forces.Length; i++)
            {
                var force = forces[i];
                if (force.FactionId < 1 || force.FactionId > 2 || force.Quantity < 0 ||
                    !anchorIds.Contains(force.SpawnAnchorId) ||
                    !SkirmishMapLayoutBuilder.TryPadForForce(layout, force, out var pad) || !Project(layout, pad, out var projected))
                { Add(reasons, force.RoleId, "Force has no unambiguous typed spawn pad."); continue; }
                if (definition.RoleCatalog == null || !definition.RoleCatalog.TryGet(force.RoleId, out var role) ||
                    role.Kind != force.RoleKind || string.IsNullOrEmpty(role.RuntimePrefabKey) || role.RuntimePrefabKey != force.RuntimePrefabKey)
                    Add(reasons, force.RoleId, "Force role/prefab binding does not match the explicit role catalog.");
                force.SpawnWorldX = projected.x; force.SpawnWorldZ = projected.y; forces[i] = force;
            }
            var structures = (SkirmishResolvedStructureEntry[])setup.Structures.Clone();
            for (int i = 0; i < structures.Length; i++)
            {
                var structure = structures[i];
                if (structure.FactionId < 1 || structure.FactionId > 2 || !anchorIds.Contains(structure.SpawnAnchorId) ||
                    string.IsNullOrEmpty(SkirmishStructureIds.VisualKey(structure.StructureId)) ||
                    !Finite(structure.AcrossOffsetMetres) || !Finite(structure.ForwardOffsetMetres) ||
                    !SkirmishMapLayoutBuilder.TryPadForStructure(layout, structure, out var pad) || !Project(layout, pad, out var projected))
                { Add(reasons, structure.StructureId, "Structure has no recognized typed facility pad."); continue; }
                float x = projected.x, z = projected.y;
                SkirmishMapLayoutBuilder.ApplyFrameOffset(layout, structure.AcrossOffsetMetres, structure.ForwardOffsetMetres, ref x, ref z);
                if (!Inside(source, x, z)) Add(reasons, structure.StructureId, "Facility center after compiled frame offset leaves source bounds.");
                structure.SpawnWorldX = x; structure.SpawnWorldZ = z; structures[i] = structure;
            }
            var instanceScope=new SkirmishResolvedSetup{SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,Structures=structures};
            if(!SkirmishStructureInstanceBinding.TryApply(layout,instanceScope,reasons))return false;
            structures=instanceScope.Structures;
            if (reasons.Count != start) return false;

            // The actual native placement owner still validates real facility footprints,
            // grid blockers, relocation, exits and census. No arbitrary 8m margin is invented here.
            fingerprint = Fingerprint(source, layout, forces, structures);
            setup.Forces = forces; setup.Structures = structures;
            setup.PlayerBaseWorldX = points[0].x; setup.PlayerBaseWorldZ = points[0].y;
            setup.PlayerStagingWorldX = points[1].x; setup.PlayerStagingWorldZ = points[1].y;
            setup.PlayerSpawnPadX = points[2].x; setup.PlayerSpawnPadZ = points[2].y;
            setup.PlayerRallyPadX = points[3].x; setup.PlayerRallyPadZ = points[3].y;
            setup.EnemyBaseWorldX = points[4].x; setup.EnemyBaseWorldZ = points[4].y;
            setup.EnemyStagingWorldX = points[5].x; setup.EnemyStagingWorldZ = points[5].y;
            setup.EnemySpawnPadX = points[6].x; setup.EnemySpawnPadZ = points[6].y;
            setup.EnemyRallyPadX = points[7].x; setup.EnemyRallyPadZ = points[7].y;
            setup.WorldWidthMetres = source.Bounds.WorldMax.x - source.Bounds.WorldMin.x;
            setup.WorldDepthMetres = source.Bounds.WorldMax.z - source.Bounds.WorldMin.z;
            setup.GridOriginX = source.GridMetadata.Origin.x; setup.GridOriginZ = source.GridMetadata.Origin.z;
            setup.CellSize = source.GridMetadata.CellSize;
            setup.DefaultRouteId = defaultRoute;
            setup.MeasuredLayoutBound = true;
            return true;
        }

        private static void ValidateDeployment(SkirmishMapLayoutConfig layout, OperationMapDefinition source,
            string role, string expectedId, Vector2 measured, List<SkirmishCompileReason> reasons)
        {
            if (!layout.TryGetAnchor(role, out var anchor) || anchor.AnchorId != expectedId || !Inside(source, measured.x, measured.y) ||
                !layout.TryProjectToMap(anchor.NormalizedU, anchor.NormalizedV, out float x, out float z) ||
                Vector2.Distance(measured, new Vector2(x, z)) > .5f)
                Add(reasons, role, "Measured deployment does not match its typed source-frame anchor.");
        }

        private static string ValidateRoutes(SkirmishMapLayoutConfig layout, OperationMapDefinition source,
            HashSet<string> anchors, List<SkirmishCompileReason> reasons)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            string selected = null;
            foreach (var route in layout.Routes)
            {
                if (string.IsNullOrEmpty(route.RouteId) || !ids.Add(route.RouteId) || !Positive(route.WidthMetres) ||
                    route.Kind < SkirmishMeasuredRouteKind.MainHighway || route.Kind > SkirmishMeasuredRouteKind.FlankSouthSweep ||
                    !Positive(route.LengthMetres) || !Positive(route.InfantryTransitSeconds) ||
                    !Nonnegative(route.GroundTransitSeconds) || (route.AllowsVehicles && !Positive(route.GroundTransitSeconds)) ||
                    !Nonnegative(route.AirTransitSeconds) || (route.AllowsAircraft && !Positive(route.AirTransitSeconds)) ||
                    !Nonnegative(route.InfantryFirstContactSeconds) || !Nonnegative(route.GroundFirstContactSeconds) ||
                    route.WaypointAnchorIds == null || route.WaypointAnchorIds.Length < 2)
                { Add(reasons, "routes", "Unique typed routes with finite width and waypoints required."); continue; }
                var points = new List<Vector2>();
                bool valid = true;
                foreach (string id in route.WaypointAnchorIds)
                    if (!anchors.Contains(id) || !layout.TryGetAnchorById(id, out var anchor) ||
                        !layout.TryProjectToMap(anchor.NormalizedU, anchor.NormalizedV, out float x, out float z) || !Inside(source, x, z))
                    { Add(reasons, route.RouteId, "Route waypoint not bound inside source."); valid = false; }
                    else
                    {
                        var point = new Vector2(x, z);
                        if (points.Exists(previous => previous.x == point.x && previous.y == point.y))
                        { Add(reasons, route.RouteId, "Route repeats a projected position (including aliased anchor IDs)."); valid = false; }
                        points.Add(point);
                    }
                if (!valid) continue;
                for (int i = points.Count - 2; i > 0; i--)
                {
                    var a = points[i-1]; var b = points[i]; var c = points[i+1];
                    double cross = ((double)b.x-a.x)*((double)c.y-b.y)-((double)b.y-a.y)*((double)c.x-b.x);
                    if (cross != 0) continue;
                    double dot = ((double)b.x-a.x)*((double)c.x-b.x)+((double)b.y-a.y)*((double)c.y-b.y);
                    if (dot <= 0) { Add(reasons, route.RouteId, "Route backtracks over its preceding segment."); valid = false; }
                    else points.RemoveAt(i); // Same path with more collinear samples is not distinct.
                }
                if (!valid) continue;
                // Coordinate tokens, not anchor IDs, define the approach. Reverse
                // traversal and aliased anchor names do not create another route.
                var tokens = points.ConvertAll(point => point.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                    point.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                string forward = string.Join(">", tokens); tokens.Reverse(); string reverse = string.Join(">", tokens);
                string canonical = string.CompareOrdinal(forward, reverse) <= 0 ? forward : reverse;
                if (!paths.Add(canonical)) Add(reasons, route.RouteId, "Equivalent forward/reverse approach is duplicated.");
                if (route.Kind == SkirmishMeasuredRouteKind.MainHighway && route.AllowsVehicles)
                {
                    if (selected != null) Add(reasons, "routes", "Exactly one typed main ground route is required.");
                    selected = route.RouteId;
                }
            }
            if (paths.Count < 2 || selected == null) Add(reasons, "routes", "Two distinct declared approaches and a ground-vehicle route required.");
            return selected;
        }

        private static bool Project(SkirmishMapLayoutConfig layout, SkirmishLegalPadConfig pad, out Vector2 point)
        {
            bool projected = layout.TryProjectLocalToMap(pad.CenterX, pad.CenterZ, out float x, out float z);
            point = new Vector2(x, z); return projected && Finite(x) && Finite(z);
        }
        private static bool Inside(OperationMapDefinition source, float x, float z) => Finite(x) && Finite(z) &&
            x >= source.Bounds.WorldMin.x && x <= source.Bounds.WorldMax.x && z >= source.Bounds.WorldMin.z && z <= source.Bounds.WorldMax.z;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Positive(float value) => Finite(value) && value > 0;
        private static bool Nonnegative(float value) => Finite(value) && value >= 0;
        private static void Add(List<SkirmishCompileReason> reasons, string field, string detail) =>
            reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, field, detail));
        [Serializable]
        private sealed class ProjectedActors
        {
            public SkirmishResolvedForceEntry[] Forces;
            public SkirmishResolvedStructureEntry[] Structures;
        }
        [Serializable]
        internal sealed class StableReference
        {
            public string Guid;
            public string SubObjectName;
            public static StableReference From(UnityEngine.AddressableAssets.AssetReference value) => value == null ? null :
                new StableReference { Guid = value.AssetGUID, SubObjectName = value.SubObjectName };
        }
        [Serializable]
        internal sealed class PlacementValue
        {
            // SourcePath is the authored dependency identity. Never hash a prefab instance ID.
            public string SourcePath;
            public string Category;
            public bool PrefabPresent;
            public byte Faction;
            public Vector3 Center, Position, EulerAngles, Scale;
            public float Yaw;
            public bool RotateVertical, InstantiateVisualWhenSourceMissing, UseGeometricFootprintCenter;
            public static PlacementValue From(MapBuildingPlacementConfigEntry value) => new PlacementValue
            {
                SourcePath = value.SourcePath, Category = value.Category, PrefabPresent = value.BuildingPrefab != null,
                Faction = value.FactionId, Center = value.WorldCenter, Position = value.WorldPosition,
                EulerAngles = value.WorldEulerAngles, Scale = value.WorldScale, Yaw = value.YawDegrees,
                RotateVertical = value.RotateVertical, InstantiateVisualWhenSourceMissing = value.InstantiateVisualWhenSourceMissing,
                UseGeometricFootprintCenter = value.UseGeometricFootprintCenter
            };
        }
        [Serializable]
        internal sealed class PlacementValues
        {
            public bool SpawnOnMatchStart, HideAuthoringVisualsAfterSpawn, UseExistingStaticPresentationWhenAuthoringVisualMissing;
            public PlacementValue[] Placements;
            public static PlacementValues From(MapBuildingPlacementConfig source)
            {
                if (source == null) return null;
                var values = new PlacementValue[source.Placements.Count];
                for (int i = 0; i < values.Length; i++) values[i] = PlacementValue.From(source.Placements[i]);
                return new PlacementValues
                {
                    SpawnOnMatchStart = source.SpawnOnMatchStart, HideAuthoringVisualsAfterSpawn = source.HideAuthoringVisualsAfterSpawn,
                    UseExistingStaticPresentationWhenAuthoringVisualMissing = source.UseExistingStaticPresentationWhenAuthoringVisualMissing,
                    Placements = values
                };
            }
        }
        [Serializable]
        internal sealed class SourceValues
        {
            public string OperationMapId, SourceIdentityHash, ContentHash, GeneratedMetadataHash, PlanningCameraId, BattleCameraId;
            public int SchemaVersion, ContentVersion;
            public OperationMapPresentationKind PresentationKind;
            public OperationMapRenderResidencyMode RenderResidencyMode;
            public OperationMapSourceBindingConfig SourceBinding;
            public OperationMapBoundsConfig Bounds;
            public OperationMapGridMetadataConfig Grid;
            public OperationMapSurfaceMetadataConfig Surface;
            public OperationMapNavigationMetadataConfig Navigation;
            public OperationMapCameraConfig[] Cameras;
            public OperationMapAnchorConfig[] Anchors;
            public OperationMapMinimapConfig Minimap;
            public StableReference Scene, SurfaceData, MinimapRaster, StaticPresentation, Buildings, Vehicles, HeavyMetadata;
            public PlacementValues AdditionalBuildings;
            public static SourceValues From(OperationMapDefinition source) => new SourceValues
            {
                OperationMapId = source.OperationMapId, SourceIdentityHash = source.SourceIdentityHash, ContentHash = source.ContentHash,
                GeneratedMetadataHash = source.GeneratedMetadataHash, PlanningCameraId = source.PlanningCameraId,
                BattleCameraId = source.BattleCameraId, SchemaVersion = source.SchemaVersion, ContentVersion = source.ContentVersion,
                PresentationKind = source.PresentationKind, RenderResidencyMode = source.RenderResidencyMode, SourceBinding = source.SourceBinding,
                Bounds = source.Bounds, Grid = source.GridMetadata, Surface = source.SurfaceMetadata, Navigation = source.NavigationMetadata,
                Cameras = source.Cameras.ToArray(), Anchors = source.Anchors.ToArray(), Minimap = source.Minimap,
                Scene = StableReference.From(source.SourceSceneReference), SurfaceData = StableReference.From(source.MapSurfaceDataReference),
                MinimapRaster = StableReference.From(source.MinimapRasterReference), StaticPresentation = StableReference.From(source.StaticPresentationManifestReference),
                Buildings = StableReference.From(source.BuildingPlacementsReference), Vehicles = StableReference.From(source.VehiclePlacementsReference),
                HeavyMetadata = StableReference.From(source.OptionalHeavyMetadataReference), AdditionalBuildings = PlacementValues.From(source.AdditionalBuildingPlacements)
            };
        }
        [Serializable]
        internal sealed class LayoutValues
        {
            public string Id, OperationMapId, ContentHash;
            public int Version;
            public float Width, Depth, OriginX, OriginZ, CellSize;
            public SkirmishLayoutWorldBindingConfig Frame;
            public SkirmishLayoutAnchorConfig[] Anchors;
            public SkirmishLayoutRouteConfig[] Routes;
            public SkirmishLegalPadConfig[] Pads;
            public static LayoutValues From(SkirmishMapLayoutConfig source) => new LayoutValues
            {
                Id = source.LayoutId, OperationMapId = source.OperationMapId, ContentHash = source.ContentHash, Version = source.ContentVersion,
                Width = source.WorldWidthMetres, Depth = source.WorldDepthMetres, OriginX = source.OriginX, OriginZ = source.OriginZ,
                CellSize = source.CellSize, Frame = source.WorldBinding, Anchors = source.Anchors, Routes = source.Routes, Pads = source.Pads
            };
        }
        internal static SourceValues CanonicalSourceValueSnapshot(OperationMapDefinition source) => SourceValues.From(source);

        private static string Fingerprint(OperationMapDefinition source, SkirmishMapLayoutConfig layout,
            SkirmishResolvedForceEntry[] forces, SkirmishResolvedStructureEntry[] structures)
        {
            // Native provenance remains separate. These serialized config values are only
            // a deterministic layout/source dependency fingerprint for generic setup hashing.
            // The projections contain only strings, scalars, vectors, value structs and arrays.
            // Do not serialize ScriptableObjects, prefab refs or Addressables cached state.
            if(layout.BindingValueFormat==SkirmishBindingValueFormat.CanonicalBinaryV2)
                return SkirmishBindingCanonicalValues.LayoutBindingFingerprint(source,layout,forces,structures);
            if(layout.BindingValueFormat!=SkirmishBindingValueFormat.NativeJsonV1)throw new NotSupportedException("Unsupported binding value format.");
            string text = "generic-layout-v1|" + JsonUtility.ToJson(SourceValues.From(source)) + "|" + JsonUtility.ToJson(LayoutValues.From(layout)) + "|" +
                JsonUtility.ToJson(new ProjectedActors { Forces = forces, Structures = structures });
            using (var sha = SHA256.Create())
                return SkirmishStructureInstanceBinding.AppendLayoutFingerprint(BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant(),layout);
        }
    }
}
