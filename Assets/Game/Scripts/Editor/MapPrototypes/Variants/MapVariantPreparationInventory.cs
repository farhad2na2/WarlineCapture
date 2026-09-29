using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    public static class MapVariantPreparationInventory
    {
        internal const string ReportRoot = "Design/MapVariants/Preparation";
        private static readonly Dictionary<string, string> Classes = CreateClasses();

        [MenuItem("Game/Map Variants/Preparation/Export All Inventories")]
        public static void ExportAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || MapVariantBuilder.InventoryOnly)
                throw new InvalidOperationException("Map inventory requires idle edit mode.");
            Dictionary<string, string> protectedHashes = ProtectedHashes();
            Directory.CreateDirectory(ReportRoot);
            File.WriteAllText(ReportRoot + "/protected-source-hashes.tsv",
                string.Join("\n", protectedHashes.Select(p => p.Key + "\t" + p.Value)) + "\n");
            try
            {
                Export("RefineryDistrict", MapVariantRefineryDistrict.Build);
                Export("CityEdgeAirfield", MapVariantCityAirfield.Build);
                Export("AshLinePort", MapVariantLogisticsPort.Build);
                Export("Frontier", MapVariantFrontier.Build);
                RequireProtected(protectedHashes);
                Debug.Log("[MapPreparationInventory] result=Passed maps=4 protectedSources=Unchanged scope=InventoryOnly");
            }
            catch (Exception e)
            {
                Debug.LogError("[MapPreparationInventory] result=Failed " + e.Message);
                throw;
            }
        }

        private static void Export(string mapId, Func<MapVariantBuilder> build)
        {
            // Build twice from the same inputs. Transient meshes never touch reference assets.
            string first = BuildManifest(build);
            string second = BuildManifest(build);
            if (!string.Equals(first, second, StringComparison.Ordinal))
                throw new InvalidOperationException("Non-deterministic semantic export: " + mapId);
            MapPreparationManifest manifest = JsonUtility.FromJson<MapPreparationManifest>(first);
            manifest.semanticHash = MapVariantPreparationSchema.Hash(first);
            string folder = ReportRoot + "/" + mapId;
            Directory.CreateDirectory(folder);
            File.WriteAllText(folder + "/inventory.json", JsonUtility.ToJson(manifest, true));
            WriteReview(folder, manifest);
            Debug.Log($"[MapPreparationInventory] result=Passed map={mapId} placements={manifest.placementCount} " +
                $"prefabs={manifest.prefabs.Count} unresolvedPlacements={manifest.unresolvedPlacementCount} " +
                $"unresolvedPrefabs={manifest.unresolvedPrefabCount} semanticHash={manifest.semanticHash} deterministicRebuild=Passed");
        }

        internal static string BuildManifest(Func<MapVariantBuilder> build)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            MapVariantBuilder.InventoryOnly = true;
            try
            {
                MapVariantBuilder b = build();
                return JsonUtility.ToJson(CreateManifest(b));
            }
            finally
            {
                MapVariantBuilder.InventoryOnly = false;
                // Destroy only meshes created by this transaction, before closing its own scene.
                foreach (GameObject root in temporary.GetRootGameObjects())
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null && !EditorUtility.IsPersistent(filter.sharedMesh))
                        UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
                EditorSceneManager.CloseScene(temporary, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        internal static MapPreparationManifest CreateManifest(MapVariantBuilder b)
        {
            Vector3 offset = MapVariantPreparationSchema.Offset(b.MapId);
            var m = new MapPreparationManifest
            {
                mapId = b.MapId, seed = b.Seed, sourceScene = b.ScenePath,
                sourceSceneGuid = AssetDatabase.AssetPathToGUID(b.ScenePath),
                sourceSceneHash = FileHash(b.ScenePath), worldMin = b.World.min, worldSize = b.World.size,
                playableMin = b.Playable.min, playableSize = b.Playable.size,
                runtimePlayableMin = b.Playable.min + new Vector2(offset.x, offset.z),
                sourceToRuntimeTranslation = offset, placementCount = b.Placements.Count,
                waterPresent = b.WaterLevel.HasValue, waterLevel = b.WaterLevel ?? 0f,
                heightOrigin = b.Height.Origin, heightCellSize = b.Height.CellSize,
                heightCellsX = b.Height.CellsX, heightCellsZ = b.Height.CellsZ,
                groundVertexHeights = new float[(b.Height.CellsX + 1) * (b.Height.CellsZ + 1)]
            };
            for (int z = 0; z <= b.Height.CellsZ; z++)
            for (int x = 0; x <= b.Height.CellsX; x++)
                m.groundVertexHeights[z * (b.Height.CellsX + 1) + x] = b.Height.GetVertex(x, z);

            var prefabRows = new Dictionary<string, MapPreparationPrefab>(StringComparer.Ordinal);
            var keys = new Dictionary<MapVariantPlacement, string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (MapVariantPlacement p in b.Placements)
            {
                string sourcePath = BasePath(p.PrefabPath);
                string guid = AssetDatabase.AssetPathToGUID(sourcePath);
                string key = MapVariantPreparationSchema.PlacementKey(b.MapId, guid, p.Instance.transform.localToWorldMatrix);
                if (!unique.Add(key)) throw new InvalidOperationException("Duplicate placement provenance: " + key);
                keys.Add(p, key);
                if (!p.Surface)
                {
                    MapVariantPrefabInfo info = b.Info(p.PrefabPath);
                    foreach (GameObject prefab in new[] { info.Prefab }.Concat(info.Attachments.Select(a => a.prefab)))
                    {
                        string path = AssetDatabase.GetAssetPath(prefab);
                        if (!prefabRows.ContainsKey(path)) prefabRows.Add(path, InspectPrefab(prefab));
                    }
                }
            }
            m.prefabs = prefabRows.Values.OrderBy(p => p.guid, StringComparer.Ordinal).ToList();
            foreach (MapVariantPlacement p in b.Placements)
            {
                string sourcePath = BasePath(p.PrefabPath);
                string category = Classify(sourcePath, p);
                bool outside = !MapVariantPreparationSchema.Contains(b.Playable,
                    MapVariantBuilder.FootprintAabb(p.Center, p.HalfSize, p.Yaw, 0f));
                string definitionRole = DefinitionRole(sourcePath);
                string qualification = "Mapped";
                string destroyed = "None";
                if (category == "GameplayBuilding")
                {
                    MapPreparationPrefab row = prefabRows[sourcePath];
                    if (row.hierarchyStatus == "DirectAlternativeValidated")
                        destroyed = "Embedded:" + row.destroyedBranch;
                    else qualification = "Unresolved: compatible destruction model required";
                    if (definitionRole == "Unmapped")
                        qualification = "Unresolved: explicit definition and destruction mapping required";
                }
                if (category == "Unresolved") qualification = "Unresolved: explicit semantics required";
                if (p.Attached && p.Support == null) qualification = "Unresolved: attachment owner required";
                if (p.Backdrop) category = "Backdrop";
                bool blocks = category is "GameplayBuilding" or "StaticObstacle" or "SceneryVehicle" or "IntentionalRuin";
                var rowPlacement = new MapPreparationPlacement
                {
                    stableKey = keys[p], sourceRecipe = p.PrefabPath,
                    sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath), category = category,
                    renderingCategory = p.Layer.ToString(), qualification = qualification,
                    sourcePosition = p.Instance.transform.position,
                    runtimePosition = p.Instance.transform.position + offset,
                    sourceScale = p.Instance.transform.lossyScale, rotation = p.Instance.transform.rotation,
                    sourceFootprint = MapVariantPreparationSchema.Footprint(p.Center, p.HalfSize, p.Yaw, Vector3.zero),
                    runtimeFootprint = MapVariantPreparationSchema.Footprint(p.Center, p.HalfSize, p.Yaw, offset),
                    movementBlocked = blocks && !p.Backdrop && !outside,
                    buildExcluded = (blocks || category == "WalkableSurface") && !p.Backdrop && !outside,
                    damageEligible = category == "GameplayBuilding" && qualification == "Mapped" && !outside,
                    targetEligible = false, definitionRole = definitionRole, destroyedMapping = destroyed,
                    artReserved = p.Reserved, backdrop = p.Backdrop, outsidePlayable = outside,
                    attached = p.Attached, intentionalRuin = category == "IntentionalRuin",
                    foundationDepth = p.FoundationDepth,
                    attachmentOwnerKey = p.Support != null ? keys[p.Support] : "",
                    attachmentPolicy = p.Attached ? "UnqualifiedPendingOwnerStatePolicy" : "None",
                    materialGuids = MaterialGuids(p.Instance)
                };
                rowPlacement.attachmentPrefabGuids = p.Surface ? Array.Empty<string>() : b.Info(p.PrefabPath)
                    .Attachments.Select(a => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(a.prefab))).ToArray();
                // Inventory semantics do not authorize runtime conversion, including otherwise mapped rows.
                if (rowPlacement.attachmentPrefabGuids.Length > 0)
                    rowPlacement.attachmentPolicy = "ComposedChild: state policy required before conversion";
                m.placements.Add(rowPlacement);
            }
            m.placements = m.placements.OrderBy(p => p.stableKey, StringComparer.Ordinal).ToList();
            m.unresolvedPlacementCount = m.placements.Count(p => p.qualification.StartsWith("Unresolved", StringComparison.Ordinal));
            m.unresolvedPrefabCount = m.placements.Where(p => p.qualification.StartsWith("Unresolved", StringComparison.Ordinal))
                .Select(p => p.sourceGuid).Distinct().Count();

            foreach (var road in b.RoadCells.OrderBy(p => p.Key.y).ThenBy(p => p.Key.x))
            {
                bool bridge = b.BridgeCells.TryGetValue(road.Key, out float deck);
                Rect rect = b.RoadCellRect(road.Key);
                m.roads.Add(new MapPreparationRoad
                {
                    cell = road.Key, kind = road.Value.ToString(), sourceMin = rect.min,
                    runtimeMin = rect.min + new Vector2(offset.x, offset.z), bridge = bridge,
                    groundHeight = b.Height.Sample(rect.center.x, rect.center.y),
                    declaredDeckLevel = bridge ? deck : 0f, renderedDeckHeight = bridge ? deck + 0.08f : 0f
                });
            }
            foreach (MapVariantZone zone in b.Layout.zones.OrderBy(z => z.id, StringComparer.Ordinal))
            {
                var center = new Vector2(zone.center.x, zone.center.z);
                bool inside = MapVariantPreparationSchema.Contains(b.Playable,
                    new Rect(center - new Vector2(zone.size.x, zone.size.z) * 0.5f, new Vector2(zone.size.x, zone.size.z)));
                bool surfaceValid = MapVariantPreparationSchema.TrySurface(b, center, out float height, out string kind);
                m.zones.Add(new MapPreparationZone
                {
                    id = zone.id, sourceCenter = zone.center,
                    runtimeCenter = new Vector3(zone.center.x, surfaceValid ? height : zone.center.y, zone.center.z) + offset,
                    size = zone.size, entireFootprintInsidePlayable = inside, centerSurfaceValid = surfaceValid,
                    surfaceKind = kind, boundsDecision = inside ? "Preserve" : "Failed gate: footprint outside playable; retained for explicit sector decision"
                });
            }
            return m;
        }

        internal static MapPreparationPrefab InspectPrefab(GameObject prefab)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long localId);
            string path = AssetDatabase.GetAssetPath(prefab);
            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            var pairs = transforms.Where(t => Enumerable.Range(0, t.childCount).Any(i => t.GetChild(i).name == "Model") &&
                Enumerable.Range(0, t.childCount).Any(i => t.GetChild(i).name == "Destroyed")).ToArray();
            string status = "NoMappedAlternative";
            string intact = "", destroyed = "";
            if (pairs.Length == 1 && pairs[0] == prefab.transform)
            {
                Transform[] models = Enumerable.Range(0, prefab.transform.childCount).Select(i => prefab.transform.GetChild(i))
                    .Where(t => t.name == "Model").ToArray();
                Transform[] ruins = Enumerable.Range(0, prefab.transform.childCount).Select(i => prefab.transform.GetChild(i))
                    .Where(t => t.name == "Destroyed").ToArray();
                if (models.Length == 1 && ruins.Length == 1 && models[0].GetComponentsInChildren<Renderer>(true).Length > 0 &&
                    ruins[0].GetComponentsInChildren<Renderer>(true).Length > 0)
                {
                    status = "DirectAlternativeValidated"; intact = "Model"; destroyed = "Destroyed";
                }
                else status = "UnresolvedAmbiguousBranches";
            }
            else if (pairs.Length > 0) status = "UnresolvedNestedAlternatives";
            return new MapPreparationPrefab
            {
                path = path, guid = guid, localId = localId,
                dependencyHash = AssetDatabase.GetAssetDependencyHash(path).ToString(),
                hierarchyStatus = status, intactBranch = intact, destroyedBranch = destroyed,
                hierarchy = transforms.Select(t => HierarchyPath(prefab.transform, t) +
                    $" activeSelf={t.gameObject.activeSelf} renderers={t.GetComponents<Renderer>().Length} " +
                    $"localPosition={t.localPosition.ToString("F6")} localRotation={t.localRotation.ToString("F6")} localScale={t.localScale.ToString("F6")}").ToArray(),
                materials = MaterialGuids(prefab),
                shaders = prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials)
                    .Where(mat => mat != null).Select(mat => mat.shader != null ? mat.shader.name : "Missing")
                    .Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray(),
                physics = prefab.GetComponentsInChildren<Component>(true).Where(c => c is Collider || c is Rigidbody || c is Joint)
                    .Select(c => HierarchyPath(prefab.transform, c.transform) + ":" + c.GetType().FullName).ToArray()
            };
        }

        private static string HierarchyPath(Transform root, Transform t) => t == root ? "." :
            HierarchyPath(root, t.parent) + "/" + t.name + "[" + t.GetSiblingIndex() + "]";
        private static string[] MaterialGuids(GameObject go) => go.GetComponentsInChildren<Renderer>(true)
            .SelectMany(r => r.sharedMaterials).Where(m => m != null)
            .Select(m => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m)))
            .Distinct().OrderBy(g => g, StringComparer.Ordinal).ToArray();
        internal static string BasePath(string recipe) => recipe.Split('|')[0].Split('~')[0].Split('@')[0];

        private static string Classify(string path, MapVariantPlacement p)
        {
            if (p.Backdrop) return "Backdrop";
            if (p.Surface || p.Spanning && p.Layer == MapVariantLayer.Roads) return "WalkableSurface";
            if (p.Attached) return "Attachment";
            return Classes.TryGetValue(path, out string category) ? category : "Unresolved";
        }

        private static string DefinitionRole(string path)
        {
            if (MapVariantKits.Houses.Contains(path)) return "House";
            if (MapVariantKits.Shops.Contains(path)) return "Shop";
            if (MapVariantKits.Landmarks.Contains(path) || MapVariantKits.Minarets.Contains(path)) return "Civic";
            return "Unmapped";
        }

        private static Dictionary<string, string> CreateClasses()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            void Add(string category, params string[] paths)
            {
                foreach (string path in paths) result[BasePath(path)] = category;
            }
            Add("RenderOnlyProp", MapVariantKits.GrassClumps.Concat(MapVariantKits.Pebbles)
                .Concat(MapVariantKits.SandEdges).Concat(MapVariantKits.Shrubs).ToArray());
            Add("StaticObstacle", MapVariantKits.Palms.Concat(MapVariantKits.SmallPalms).Concat(MapVariantKits.Rocks)
                .Concat(MapVariantKits.SandMounds).Concat(MapVariantKits.SandDunes).Concat(MapVariantKits.Plazas)
                .Concat(MapVariantKits.YardCargo).Concat(MapVariantKits.MilitaryCargo).Concat(MapVariantKits.PortCargo)
                .Concat(MapVariantKits.Checkpoint).Concat(MapVariantKits.Launchers).ToArray());
            Add("IntentionalRuin", MapVariantKits.Ruins);
            Add("GameplayBuilding", MapVariantKits.Houses.Concat(MapVariantKits.Shops).Concat(MapVariantKits.Landmarks)
                .Concat(MapVariantKits.Minarets).ToArray());
            Add("GameplayBuilding", MapVariantKits.Warehouse, MapVariantKits.PortWarehouse, MapVariantKits.Hangar,
                MapVariantKits.HangarOpen, MapVariantKits.Barracks, MapVariantKits.GuardTower, MapVariantKits.GasStation,
                MapVariantKits.WaterTower, MapVariantKits.ControlTower, MapVariantKits.GasSphereTank,
                MapVariantKits.GasHolder, MapVariantKits.OilTower, MapVariantKits.Shed);
            Add("SceneryVehicle", MapVariantKits.Trucks.Concat(MapVariantKits.ArmorParked)
                .Concat(MapVariantKits.CivilianVehicles).Concat(new[] { MapVariantKits.RocketTruck, MapVariantKits.Boat }).ToArray());
            Add("StaticObstacle", MapVariantKits.VillageWall, MapVariantKits.VillageWallPillar,
                MapVariantKits.StorageTank, MapVariantKits.OilPump, MapVariantKits.LargePipe, MapVariantKits.SmallPipe,
                MapVariantKits.PipeRack, MapVariantKits.Generator, MapVariantKits.Transformer, MapVariantKits.MobileRadar,
                MapVariantKits.FuelBladder, MapVariantKits.StreetLight, MapVariantKits.PortWall, MapVariantKits.PortWallAlt,
                MapVariantKits.Spotlight, MapVariantKits.BoatTie, MapVariantKits.ConcreteBarrier);
            Add("StaticObstacle", MapVariantKits.StacksOnPlatforms);
            Add("StaticObstacle", MapVariantKits.Fence, MapVariantKits.TallBarrier, MapVariantKits.SandbagRow,
                MapVariantKits.SandbagRowLong, MapVariantKits.RazorWire, MapVariantKits.RoadBarrier,
                MapVariantKits.LightPole, MapVariantKits.Container);
            Add("StaticObstacle", MapVariantKits.Substation.Concat(MapVariantKits.PowerPoles)
                .Concat(MapVariantKits.CamoTents).ToArray());
            const string mil = "Assets/PolygonMilitary/Prefabs/";
            const string br = "Assets/Synty/PolygonBattleRoyale/Prefabs/";
            Add("StaticObstacle", br + "Props/SM_Prop_Container_01.prefab",
                mil + "Buildings/SM_Bld_Tent_01.prefab", mil + "Buildings/SM_Bld_Tent_Open_01.prefab",
                mil + "Props/Military/SM_Prop_Bed_Medical_01.prefab", mil + "Props/Military/SM_Prop_Bed_Stretcher_01.prefab",
                mil + "Props/Military/SM_Prop_MedicalBox_01.prefab", mil + "Props/SM_Prop_Runway_Barrier_01.prefab",
                mil + "Props/SM_Prop_Runway_Light_01.prefab");
            // These deck meshes need surface clearance validation before being runtime-active.
            Add("WalkableSurface", MapVariantKits.Pier,
                mil + "Environment/SM_Env_Helipad_01.prefab", mil + "Environment/SM_Env_Runway_01.prefab");
            Add("SceneryVehicle", mil + "Vehicles/SM_Veh_Jet_01.prefab", mil + "Vehicles/SM_Veh_TransportPlane_01.prefab");
            Add("IntentionalRuin", new[] { "SM_Veh_APC_01_Destroyed", "SM_Veh_Helicopter_Transport_01_Destroyed",
                "SM_Veh_Jet_Destroyed_01", "SM_Veh_TransportPlane_Destroyed_Cockpit_01",
                "SM_Veh_TransportPlane_Destroyed_Fuselage_01", "SM_Veh_TransportPlane_Destroyed_Tail_01",
                "SM_Veh_Truck_01_Destroyed" }.Select(name => mil + "Vehicles/Destroyed/" + name + ".prefab").ToArray());
            return result;
        }

        private static void WriteReview(string folder, MapPreparationManifest m)
        {
            var text = new StringBuilder($"# {m.mapId} inventory\n\nSemantic hash: `{m.semanticHash}`. " +
                $"{m.placementCount} placements; {m.prefabs.Count} unique prefabs.\n\n" +
                "This is a preparation input audit. No runtime source is certified.\n\n" +
                "| Source | Category | Instances | Qualification |\n|---|---|---:|---|\n");
            foreach (var group in m.placements.GroupBy(p => new { p.sourceRecipe, p.category, p.qualification })
                         .OrderBy(g => g.Key.sourceRecipe, StringComparer.Ordinal).ThenBy(g => g.Key.category, StringComparer.Ordinal))
                text.AppendLine($"| `{group.Key.sourceRecipe.Replace("|", "&#124;")}` | {group.Key.category} | {group.Count()} | {group.Key.qualification} |");
            text.AppendLine("\n## Bounds and anchor surfaces\n\n| Zone | Full footprint | Surface | Decision |\n|---|---|---|---|");
            foreach (var z in m.zones) text.AppendLine($"| {z.id} | {(z.entireFootprintInsidePlayable ? "Inside" : "FAIL")} | {z.surfaceKind}; y={z.runtimeCenter.y:F3} | {z.boundsDecision} |");
            File.WriteAllText(folder + "/REVIEW.md", text.ToString());
        }

        internal static string FileHash(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        internal static Dictionary<string, string> ProtectedHashes()
        {
            string[] roots = { "Assets/Game/Art/MapPrototypes/Variants", "Assets/Game/Scenes/MapPrototypes/Variants",
                "Assets/Game/Scenes/OperationMaps", "Assets/Game/Configs/Missions", "Assets/Game/Configs/Scenarios",
                "Assets/Game/Configs/OperationMaps", "Assets/AddressableAssetsData" };
            return roots.Where(Directory.Exists).SelectMany(root => Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                .Where(p => !p.StartsWith("Assets/Game/Scenes/OperationMaps/Variants/", StringComparison.Ordinal))
                .OrderBy(p => p, StringComparer.Ordinal).ToDictionary(p => p, FileHash, StringComparer.Ordinal);
        }

        internal static void RequireProtected(Dictionary<string, string> expected)
        {
            var actual = ProtectedHashes();
            if (actual.Count != expected.Count || expected.Any(p => !actual.TryGetValue(p.Key, out string hash) || hash != p.Value))
                throw new InvalidOperationException("Preparation modified a protected source or campaign asset.");
        }
    }
}
