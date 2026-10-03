using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Game.Editor.MapVariants
{
    // Variant descriptors share the existing record/realization contracts without changing
    // the dense-city transaction or production selection. These are isolated candidates.
    public static class MapVariantPreparedCandidateBuilder
    {
        internal const string Version = "map-prepared-candidate-v6";
        internal const string AssetRoot = "Assets/Game/GeneratedOperationMaps/Variants";
        internal const string SceneRoot = "Assets/Game/Scenes/OperationMaps/Variants";
        internal static readonly string[] MediumMaps = { "RefineryDistrict", "CityEdgeAirfield", "AshLinePort" };

        [MenuItem("Game/Map Variants/Preparation/Build Medium Candidates")]
        public static void BuildMediumCandidates()
        {
            foreach (string map in MediumMaps) Build(map);
            Debug.Log("[MapPreparedCandidates] result=Passed maps=3 scope=AuthoringAndSurfaceData Frontier=Deferred");
        }

        internal static MapVariantBuilder BuildSource(string map) => map switch
        {
            "RefineryDistrict" => MapVariantRefineryDistrict.Build(),
            "CityEdgeAirfield" => MapVariantCityAirfield.Build(),
            "AshLinePort" => MapVariantLogisticsPort.Build(),
            "Frontier" => MapVariantFrontier.Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(map))
        };
        internal static string ScenePath(string map) => SceneRoot + "/" + map + "/PreparedEntities.unity";
        internal static string MapId(string map) => "opmap.skirmish." + map.ToLowerInvariant() + "_prepared";

        public static void BuildRefinery() => Build("RefineryDistrict");
        public static void BuildAirfield() => Build("CityEdgeAirfield");
        public static void BuildPort() => Build("AshLinePort");
        public static void BuildFrontier() { RequireMediumRuntimeProof(); Build("Frontier"); }

        internal static void RequireMediumRuntimeProof()
        {
            foreach (string map in MediumMaps)
            {
                string root = MapVariantPreparationInventory.ReportRoot + "/" + map + "/Candidate/";
                var current = JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText(root + "output-manifest.json"));
                var proof = JsonUtility.FromJson<MediumRuntimeProof>(File.ReadAllText(root + "packed-runtime-evidence.json"));
                if (proof.result != "Passed" || proof.contentHash != current.semanticHash)
                    throw new InvalidOperationException("Frontier requires current medium runtime proof: " + map);
            }
            var switching = JsonUtility.FromJson<MediumRuntimeProof>(File.ReadAllText(
                MapVariantPreparationInventory.ReportRoot + "/Evidence/existing-map-switch-result.json"));
            if (switching.result != "Passed")
                throw new InvalidOperationException("Frontier requires successful existing-map switching.");
        }

        [Serializable] private sealed class MediumRuntimeProof { public string result, contentHash; }

        internal static void Build(string map)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || MapVariantBuilder.InventoryOnly)
                throw new InvalidOperationException("Candidate generation requires idle edit mode.");
            var protectedHashes = MapVariantPreparationInventory.ProtectedHashes();
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            MapVariantBuilder.InventoryOnly = true;
            var timer = Stopwatch.StartNew();
            try
            {
                MapVariantBuilder b = BuildSource(map);
                MapPreparationManifest inventory = MapVariantPreparationInventory.CreateManifest(b);
                string folder = AssetRoot + "/" + map + "/Candidate";
                string report = MapVariantPreparationInventory.ReportRoot + "/" + map + "/Candidate";
                MapVariantBuilder.EnsureFolder(folder);
                Directory.CreateDirectory(report);
                var output = new PreparedCandidateOutput
                {
                    mapId = MapId(map), variant = map, generatorVersion = Version,
                    inputHash = MapVariantPreparationSchema.Hash(Version + JsonUtility.ToJson(inventory)),
                    sourceToRuntimeTranslation = MapVariantPreparationSchema.Offset(map),
                    runtimePlayableMin = inventory.runtimePlayableMin, playableSize = inventory.playableSize,
                    regenerationMethod = "Game.Editor.MapVariants.MapVariantPreparedCandidateBuilder.Build" +
                        (map == "RefineryDistrict" ? "Refinery" : map == "CityEdgeAirfield" ? "Airfield" : map == "Frontier" ? "Frontier" : "Port")
                };
                var rows = inventory.placements.ToDictionary(p => p.stableKey, StringComparer.Ordinal);
                var keys = b.Placements.ToDictionary(p => p, p => Key(map, p));
                var prefabRows = inventory.prefabs.ToDictionary(p => p.path, StringComparer.Ordinal);
                var hierarchy = MapVariantRefineryPreparationSlice.CreateHierarchy(scene, inventory, map + "-prepared-v1");
                var definitions = DenseCityBuildingDefinitionLibrary.LoadExisting();
                var staticMovement = new bool[2048 * 1024];
                var buildExcluded = new bool[staticMovement.Length];
                int ownerIndex = 0;
                foreach (MapVariantPlacement p in b.Placements.OrderBy(p => keys[p], StringComparer.Ordinal))
                {
                    string key = keys[p];
                    MapPreparationPlacement row = rows[key];
                    if (row.category == "Unresolved" || p.Attached && p.Support == null)
                        throw new InvalidOperationException("Unmapped source semantics: " + row.sourceRecipe);
                    bool gameplay = row.category == "GameplayBuilding" && !row.outsidePlayable && !row.backdrop && row.qualification == "Mapped";
                    if (row.category == "GameplayBuilding" && !gameplay)
                    {
                        Rect footprintBounds = MapVariantBuilder.FootprintAabb(p.Center, p.HalfSize, p.Yaw, 0f);
                        row.category = row.backdrop || !footprintBounds.Overlaps(b.Playable) ? "Backdrop" : "StaticObstacle";
                        row.damageEligible = false;
                        row.qualification = "Mapped: non-destructible scenery; compatible pair unavailable or full gameplay footprint outside playable bounds";
                        row.definitionRole = "None";
                        row.destroyedMapping = "None: static intact presentation";
                    }
                    row.movementBlocked = row.category is "StaticObstacle" or "SceneryVehicle" or "IntentionalRuin" or "GameplayBuilding";
                    row.movementBlocked &= !row.backdrop && row.category != "Backdrop";
                    row.buildExcluded = row.movementBlocked || row.category is "WalkableSurface" or "Attachment";
                    row.attachmentPolicy = p.Attached ? "Static scenery follows declared static support; no damage owner" :
                        row.attachmentPrefabGuids.Length > 0 ? "Composed descendants belong to one static presentation; preserve relative transforms" :
                        gameplay ? "All Model descendants follow intact; matching Destroyed descendants follow destroyed" : "None";
                    if (gameplay)
                    {
                        if (row.attachmentPrefabGuids.Length != 0 || p.PrefabPath.Contains("~"))
                            throw new InvalidOperationException("Gameplay material/composition requires explicit state mapping: " + key);
                        CreateBuilding(b, p, row, hierarchy, definitions, folder, ownerIndex++, output);
                    }
                    else if (!p.Surface)
                    {
                        NormalizeStatic(b, p, prefabRows, row.intentionalRuin, output);
                        StripPhysics(p.Instance);
                    }
                    if (row.movementBlocked && !gameplay) Rasterize(row.runtimeFootprint, staticMovement, inventory);
                    if (row.buildExcluded) Rasterize(row.runtimeFootprint, buildExcluded, inventory);
                    output.classifications.Add(row);
                }
                // Render-only art added after classification; it never reaches the semantic hash or grid.
                if (MapVariantBeautify.Supports(map))
                    output.presentation = MapVariantBeautify.Apply(b, output.mapId, staticMovement, output.sourceToRuntimeTranslation);
                PersistGround(b, folder);
                StripPhysics(b.Root.gameObject);
                ConfigureReadiness(b, hierarchy, output);
                // Translate all scene domains together exactly once; manifest runtime fields
                // have already been converted independently and must never be translated again.
                foreach (GameObject root in scene.GetRootGameObjects()) root.transform.position += output.sourceToRuntimeTranslation;
                UnityEngine.Object.DestroyImmediate(b.Layer(MapVariantLayer.Zones).gameObject);
                CreateSurfaceAndGrid(b, inventory, folder, staticMovement, buildExcluded, output, keys, rows);
                ResolveZones(b, inventory, output);
                foreach (var row in output.classifications.Where(p => p.attached))
                {
                    var owner = output.classifications.Single(p => p.stableKey == row.attachmentOwnerKey);
                    if (owner.damageEligible) throw new InvalidOperationException("Static attachment cannot remain on destructible owner.");
                }
                var owners = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).ToArray();
                if (owners.Length != output.owners.Count || owners.Select(o => o.StableId).Distinct().Count() != owners.Length)
                    throw new InvalidOperationException("Candidate independent-owner count/identity failure.");
                foreach (var owner in owners)
                    if (!owner.TryValidate(out string error) || !DenseCityBuildingIntactVisualPolicy.TryValidateNormalized(owner.IntactVisualRoot, out error))
                        throw new InvalidOperationException(error);
                output.duplicateOriginals = b.Placements.Count(p => rows[keys[p]].damageEligible && p.Instance != null);
                if (output.duplicateOriginals != 0) throw new InvalidOperationException("Original gameplay presentation survived conversion.");
                // Evidence references only. Save full state scales for correct runtime baking.
                foreach (var owner in owners) owner.DestroyedVisualRoot.transform.localScale = Vector3.zero;
                var view = MapVariantView.Battle("", b.Playable.center + new Vector2(output.sourceToRuntimeTranslation.x, output.sourceToRuntimeTranslation.z), 0f, 180f);
                MapVariantRefineryPreparationSlice.Capture(scene, view, report + "/battle-authoring-reference.png");
                var top = new MapVariantView("Top", new Vector3(b.Playable.center.x, map == "Frontier" ? 1200f : 600f, b.Playable.center.y) + output.sourceToRuntimeTranslation, new Vector3(90f, 0f, 0f), 60f);
                MapVariantRefineryPreparationSlice.Capture(scene, top, report + "/top-authoring-reference.png");
                foreach (var owner in owners) owner.DestroyedVisualRoot.transform.localScale = Vector3.one;
                MapVariantBuilder.EnsureFolder(Path.GetDirectoryName(ScenePath(map)).Replace('\\', '/'));
                b.Root.Find("Lighting").gameObject.SetActive(false);
                if (!EditorSceneManager.SaveScene(scene, ScenePath(map))) throw new IOException("Candidate save failed.");
                output.scenePath = ScenePath(map); output.sceneGuid = AssetDatabase.AssetPathToGUID(output.scenePath);
                output.sceneHash = MapVariantPreparationInventory.FileHash(output.scenePath);
                // Semantic identity excludes editor file IDs, elapsed time and capture bytes.
                output.semanticHash = MapVariantPreparationSchema.Hash(JsonUtility.ToJson(new PreparedSemanticOutput
                { inputHash = output.inputHash, owners = output.owners, classifications = output.classifications, zones = output.zones,
                    surfaceHash = output.surfaceHash, gridHash = output.gridHash }));
                string outputPath = report + "/output-manifest.json";
                if (File.Exists(outputPath))
                {
                    var old = JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText(outputPath));
                    if (old.inputHash == output.inputHash && old.semanticHash != output.semanticHash)
                        throw new InvalidOperationException("Identical candidate inputs changed semantic output.");
                    output.deterministicReplay = old.inputHash == output.inputHash ? "Passed" : "InputsChanged";
                }
                output.generationSeconds = (float)timer.Elapsed.TotalSeconds;
                output.rendererCount = scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<Renderer>(true).Length);
                AssetDatabase.SaveAssets();
                File.WriteAllText(outputPath, JsonUtility.ToJson(output, true));
                File.WriteAllText(report + "/REVIEW.md", Review(output));
                foreach (var pair in protectedHashes)
                    if (MapVariantPreparationInventory.FileHash(pair.Key) != pair.Value)
                        throw new InvalidOperationException("Protected source changed: " + pair.Key);
                Debug.Log($"[MapPreparedCandidate] result=Passed map={map} owners={owners.Length} staticBlockCells={output.staticBlockCells} " +
                    $"duplicateOriginals=0 semanticHash={output.semanticHash} replay={output.deterministicReplay} scope=AuthoringAndSurfaceData");
            }
            catch (Exception e) { Debug.LogError("[MapPreparedCandidate] result=Failed map=" + map + " " + e.Message); throw; }
            finally
            {
                MapVariantBuilder.InventoryOnly = false;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
                    if (f.sharedMesh != null && !EditorUtility.IsPersistent(f.sharedMesh)) UnityEngine.Object.DestroyImmediate(f.sharedMesh);
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static string Key(string map, MapVariantPlacement p) => MapVariantPreparationSchema.PlacementKey(map,
            AssetDatabase.AssetPathToGUID(MapVariantPreparationInventory.BasePath(p.PrefabPath)), p.Instance.transform.localToWorldMatrix);

        private static void CreateBuilding(MapVariantBuilder b, MapVariantPlacement p, MapPreparationPlacement row,
            DenseCityPresentationHierarchyContext hierarchy, DenseCityBuildingDefinitionLibrary definitions,
            string folder, int index, PreparedCandidateOutput output)
        {
            GameObject intact = b.Info(p.PrefabPath).Prefab;
            GameObject destroyed = MapVariantRefineryPreparationSlice.ExtractDestroyed(intact, folder);
            var im = DenseCityVisualAssetMetadataExtractor.Extract(intact, null, r => DenseCityBuildingIntactVisualPolicy.ShouldIncludeRenderer(intact, r));
            var dm = DenseCityVisualAssetMetadataExtractor.Extract(destroyed);
            MapVariantBuilder.TryGetRenderBounds(p.Instance, out Bounds bounds);
            bounds.center += output.sourceToRuntimeTranslation;
            Vector2Int origin = new(Mathf.FloorToInt(bounds.min.x), Mathf.FloorToInt(bounds.min.z));
            Vector2Int cells = new(Mathf.CeilToInt(bounds.max.x) - origin.x, Mathf.CeilToInt(bounds.max.z) - origin.y);
            var role = (GeneratedCityBuildingRole)Enum.Parse(typeof(GeneratedCityBuildingRole), row.definitionRole);
            Matrix4x4 runtimeMatrix = Matrix4x4.Translate(output.sourceToRuntimeTranslation) * p.Instance.transform.localToWorldMatrix;
            // Realization is source-space until the single scene-domain translation below.
            // Medium variants have identity conversion; Frontier uses inverse translation here.
            var group = DenseCityBuildingRecordFactory.Create(new DenseCityBuildingRecordInput(
                "map-prep-" + row.stableKey, b.Seed, 0, 0, im.PrefabAssetGuid, im.PrefabLocalId, dm.PrefabAssetGuid, dm.PrefabLocalId,
                im.MaterialAssetGuids, dm.MaterialAssetGuids, runtimeMatrix, origin, cells, p.HalfSize * 2f,
                p.Instance.transform.position.y, bounds, p.Instance.transform.forward, role, definitions.ResolveAssetGuid(role),
                0, definitions.ResolveAsset(role).MaxHealth, uint.MaxValue, 0, new Vector2Int(origin.x / 64, origin.y / 64), b.MapId.ToLowerInvariant()));
            var realized = DenseCityBuildingPresentationRealizer.Realize(output.mapId, group.Building, group.IntactPresentation,
                group.DestroyedPresentation, hierarchy, definitions, placementIndexOverride: index);
            realized.Authoring.transform.position -= output.sourceToRuntimeTranslation;
            output.owners.Add(new PreparedOwner
            {
                placementKey = row.stableKey, stableId = realized.Authoring.StableId, sourceGuid = im.PrefabAssetGuid,
                destroyedGuid = dm.PrefabAssetGuid, definitionGuid = definitions.ResolveAssetGuid(role),
                position = row.runtimePosition, rotation = row.rotation, scale = row.sourceScale, origin = origin, footprint = cells,
                intactRendererCount = realized.IntactVisualRoot.GetComponentsInChildren<Renderer>(true).Length,
                destroyedRendererCount = realized.DestroyedVisualRoot.GetComponentsInChildren<Renderer>(true).Length
            });
            row.damageEligible = true;
            UnityEngine.Object.DestroyImmediate(p.Instance);
        }

        private static void NormalizeStatic(MapVariantBuilder b, MapVariantPlacement p,
            Dictionary<string, MapPreparationPrefab> audit, bool ruin, PreparedCandidateOutput output)
        {
            var info = b.Info(p.PrefabPath);
            var sourceRoots = new List<(GameObject source, GameObject instance)> { (info.Prefab, p.Instance) };
            // Composed children are prefab instance roots with exact source GUIDs, never a name substring.
            foreach (var children in info.Attachments.GroupBy(a => a.prefab))
            {
                GameObject childPrefab = children.Key;
                string path = AssetDatabase.GetAssetPath(childPrefab);
                var roots = p.Instance.GetComponentsInChildren<Transform>(true).Where(t => t != p.Instance.transform &&
                    PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) == childPrefab).ToArray();
                if (roots.Length != children.Count()) throw new InvalidOperationException("Ambiguous composed child provenance: " + path);
                foreach (var root in roots) sourceRoots.Add((childPrefab, root.gameObject));
            }
            foreach (var pair in sourceRoots)
            {
                var row = audit[AssetDatabase.GetAssetPath(pair.source)];
                if (row.hierarchyStatus.StartsWith("Unresolved", StringComparison.Ordinal))
                    throw new InvalidOperationException("Static alternative requires explicit hierarchy mapping: " + row.path);
                if (row.hierarchyStatus != "DirectAlternativeValidated") continue;
                string branch = ruin ? row.intactBranch : row.destroyedBranch;
                Transform alternative = pair.instance.transform.Find(branch);
                if (alternative == null) throw new InvalidOperationException("Mapped static alternative missing: " + row.path);
                UnityEngine.Object.DestroyImmediate(alternative.gameObject);
                output.staticAlternativesRemoved++;
            }
        }

        private static void StripPhysics(GameObject instance)
        {
            foreach (var c in instance.GetComponentsInChildren<Component>(true))
                if (c is Collider || c is Rigidbody || c is Joint) UnityEngine.Object.DestroyImmediate(c);
        }

        private static void ConfigureReadiness(MapVariantBuilder b, DenseCityPresentationHierarchyContext hierarchy, PreparedCandidateOutput output)
        {
            // Generated civilian owners carry DenseCity identities. Parked vehicles remain
            // render-only and the exact readiness contract explicitly expects zero active vehicles.
            foreach (MapVariantLayer layer in Enum.GetValues(typeof(MapVariantLayer)))
            {
                if (layer == MapVariantLayer.Zones) continue;
                foreach (Transform child in b.Layer(layer))
                {
                    if (child.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                    string hash = MapVariantPreparationSchema.Hash(b.MapId + "/" + layer + "/" + child.name);
                    string key = MapVariantPreparationSchema.PlacementKey(b.MapId, hash.Substring(0, 32), child.localToWorldMatrix);
                    var identity = child.gameObject.AddComponent<DenseCityPresentationIdentityAuthoring>();
                    var category = layer == MapVariantLayer.Backdrop ? DenseCityPresentationSemanticCategory.Horizon :
                        layer == MapVariantLayer.Vegetation ? DenseCityPresentationSemanticCategory.Vegetation :
                        layer is MapVariantLayer.Ground or MapVariantLayer.Roads ? DenseCityPresentationSemanticCategory.Infrastructure : DenseCityPresentationSemanticCategory.Prop;
                    identity.ConfigureForEditor("densecity." + MapVariantPreparationSchema.Hash(key), OperationMapEntityPresentationRole.RenderOnly, category);
                    if (!identity.TryValidate(out string error)) throw new InvalidOperationException(error);
                    output.renderOnlyIdentityCount++;
                }
            }
            output.generatedIdentityCount = output.owners.Count + output.renderOnlyIdentityCount;
            GameObject buildingRoot = b.Root.gameObject.scene.GetRootGameObjects().Single(r => r.name == "PreparedEntityPresentation");
            var vehicleRoot = new GameObject("PreparedGameplayVehicles");
            foreach (var pair in new[] { (buildingRoot, OperationMapEntityPresentationRole.GameplayBuildings),
                (vehicleRoot, OperationMapEntityPresentationRole.GameplayVehicles), (b.Root.gameObject, OperationMapEntityPresentationRole.RenderOnly) })
            {
                var marker = pair.Item1.AddComponent<OperationMapEntityPresentationRootAuthoring>();
                var data = new SerializedObject(marker);
                data.FindProperty("operationMapId").stringValue = output.mapId;
                data.FindProperty("role").intValue = (int)pair.Item2;
                data.FindProperty("migrationRecordSetHash").stringValue = output.inputHash;
                data.FindProperty("expectedGameplayBuildingCount").intValue = output.owners.Count;
                data.FindProperty("expectedGameplayVehicleCount").intValue = 0;
                data.FindProperty("expectedRenderOnlyCount").intValue = output.renderOnlyIdentityCount;
                data.FindProperty("expectedGeneratedIdentityCount").intValue = output.generatedIdentityCount;
                data.ApplyModifiedPropertiesWithoutUndo();
                if (!marker.TryValidate(out string error)) throw new InvalidOperationException("Variant readiness root: " + error);
            }
        }

        private static void PersistGround(MapVariantBuilder b, string folder)
        {
            MapVariantBuilder.EnsureFolder(folder + "/Ground");
            foreach (var f in b.Layer(MapVariantLayer.Ground).GetComponentsInChildren<MeshFilter>(true))
            {
                if (EditorUtility.IsPersistent(f.sharedMesh)) continue;
                string path = folder + "/Ground/" + f.name + ".asset";
                Mesh old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (old == null) AssetDatabase.CreateAsset(f.sharedMesh, path);
                else { Mesh temporary = f.sharedMesh; EditorUtility.CopySerialized(temporary, old);
                    // Set the native vertex/index buffers too: a serialized copy can leave the old
                    // uploaded mesh in the Editor until its next import, hiding new paint in captures.
                    old.vertices = temporary.vertices;
                    old.normals = temporary.normals;
                    old.uv = temporary.uv;
                    old.subMeshCount = temporary.subMeshCount;
                    for (int sub = 0; sub < temporary.subMeshCount; sub++) old.SetTriangles(temporary.GetTriangles(sub), sub);
                    old.RecalculateBounds(); old.RecalculateTangents(); old.UploadMeshData(false);
                    f.sharedMesh = old; EditorUtility.SetDirty(old); UnityEngine.Object.DestroyImmediate(temporary); }
            }
        }

        // Cell square vs yaw polygon SAT. This deliberately includes partially overlapping
        // boundary scenery; art-reservation bits are never read as gameplay blockers.
        internal static void Rasterize(Vector2[] polygon, bool[] mask, MapPreparationManifest inventory)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(polygon.Min(p => p.x)));
            int minZ = Mathf.Max(0, Mathf.FloorToInt(polygon.Min(p => p.y)));
            int maxX = Mathf.Min(2048, Mathf.CeilToInt(polygon.Max(p => p.x)));
            int maxZ = Mathf.Min(1024, Mathf.CeilToInt(polygon.Max(p => p.y)));
            Rect playable = new(inventory.runtimePlayableMin, inventory.playableSize);
            for (int z = minZ; z < maxZ; z++) for (int x = minX; x < maxX; x++)
                if (playable.Contains(new Vector2(x + .5f, z + .5f)) && IntersectsCell(polygon, x, z)) mask[z * 2048 + x] = true;
        }

        internal static bool IntersectsCell(Vector2[] polygon, int x, int z)
        {
            Vector2[] square = { new(x, z), new(x + 1, z), new(x + 1, z + 1), new(x, z + 1) };
            var axes = new List<Vector2> { Vector2.right, Vector2.up };
            for (int i = 0; i < polygon.Length; i++) { Vector2 e = polygon[(i + 1) % polygon.Length] - polygon[i]; axes.Add(new Vector2(-e.y, e.x)); }
            foreach (Vector2 axis in axes)
            {
                float aMin = polygon.Min(p => Vector2.Dot(p, axis)), aMax = polygon.Max(p => Vector2.Dot(p, axis));
                float bMin = square.Min(p => Vector2.Dot(p, axis)), bMax = square.Max(p => Vector2.Dot(p, axis));
                if (aMax <= bMin || bMax <= aMin) return false;
            }
            return true;
        }

        private static void CreateSurfaceAndGrid(MapVariantBuilder b, MapPreparationManifest m, string folder,
            bool[] staticMovement, bool[] buildExcluded, PreparedCandidateOutput output, Dictionary<MapVariantPlacement, string> keys, Dictionary<string, MapPreparationPlacement> rows)
        {
            int count = 2048 * 1024;
            var heights = new float[count];
            var movement = new MapSurfaceMovementMask[count];
            var normals = new float3[count];
            var slopes = new float[count];
            var kinds = new MapSurfaceType[count];
            var overrideHeights = Enumerable.Repeat(float.NegativeInfinity, count).ToArray();
            var overrideNormals = new Vector3[count];
            foreach (var p in b.Placements.Where(p => p.Instance != null && (p.Surface || rows[keys[p]].category == "WalkableSurface")))
                RasterizeWalkableTriangles(p.Instance, Vector3.zero, m, overrideHeights, overrideNormals);
            var pads = b.Placements.Where(p => p.Surface).ToArray();
            // Sampling ignores decoration and samples only ground/deck/pads. Precompute the
            // few pad bounds so the 1m export is linear rather than scanning every placement.
            var padBounds = pads.Select(p => { MapVariantBuilder.TryGetRenderBounds(p.Instance, out Bounds bb); return (p, bb.max.y); }).ToArray();
            Vector3 off = output.sourceToRuntimeTranslation;
            var blockedCells = new List<Vector2Int>();
            for (int z = 0; z < 1024; z++) for (int x = 0; x < 2048; x++)
            {
                int i = z * 2048 + x;
                Vector2 source = new(x + .5f - off.x, z + .5f - off.z);
                if (!b.Playable.Contains(source)) { kinds[i] = MapSurfaceType.Blocked; continue; }
                float h = b.Height.Sample(source.x, source.y);
                bool bridge = b.BridgeCells.TryGetValue(b.RoadCell(source), out float deck);
                if (bridge) h = deck + .08f;
                else foreach (var (p, y) in padBounds)
                {
                    Vector3 local = Quaternion.Euler(0, -p.Yaw, 0) * new Vector3(source.x - p.Center.x, 0, source.y - p.Center.y);
                    if (Mathf.Abs(local.x) <= p.HalfSize.x && Mathf.Abs(local.z) <= p.HalfSize.y) h = Mathf.Max(h, y);
                }
                bool walkableOverride = !float.IsNegativeInfinity(overrideHeights[i]) && overrideHeights[i] >= h;
                if (!bridge && walkableOverride) h = overrideHeights[i];
                heights[i] = h;
                bool water = !bridge && b.WaterLevel.HasValue && h < b.WaterLevel.Value + .15f;
                kinds[i] = water ? MapSurfaceType.Blocked : bridge ? MapSurfaceType.BridgeDeck :
                    walkableOverride ? MapSurfaceType.Plaza : b.RoadCells.ContainsKey(b.RoadCell(source)) ? MapSurfaceType.Road : MapSurfaceType.Terrain;
                if (water || staticMovement[i]) { blockedCells.Add(new Vector2Int(x, z)); continue; }
                float dx = b.Height.Sample(source.x + .5f, source.y) - b.Height.Sample(source.x - .5f, source.y);
                float dz = b.Height.Sample(source.x, source.y + .5f) - b.Height.Sample(source.x, source.y - .5f);
                float slope = bridge ? 0f : walkableOverride ? Vector3.Angle(overrideNormals[i], Vector3.up) : Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
                normals[i] = bridge ? new float3(0, 1, 0) : walkableOverride ? (float3)overrideNormals[i] : math.normalize(new float3(-dx, 1, -dz));
                slopes[i] = slope;
                if (slope <= 35f) movement[i] |= MapSurfaceMovementMask.Infantry;
                if (slope <= 22f) movement[i] |= MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle;
                movement[i] |= MapSurfaceMovementMask.AirGrounded;
                if (!buildExcluded[i] && slope <= 8f && kinds[i] == MapSurfaceType.Terrain) movement[i] |= MapSurfaceMovementMask.BuildingPlacement;
                if (bridge) output.bridgeSurfaceCells++;
            }
            using var builder = new BlobBuilder(Allocator.Temp);
            ref MapSurfaceBlob blob = ref builder.ConstructRoot<MapSurfaceBlob>();
            blob.GridOrigin = float3.zero; blob.CellSize = 1f; blob.Dimensions = new int2(2048, 1024);
            var cells = builder.Allocate(ref blob.Cells, count);
            builder.Allocate(ref blob.Connections, 0);
            builder.Allocate(ref blob.CompactSamples, 0);
            for (int i = 0; i < count; i++) cells[i] = new MapSurfaceCell { FirstSurfaceIndex = i, SurfaceCount = 1, InlineSurfaceIndex = 0 };
            var samples = builder.Allocate(ref blob.Samples, count);
            for (int i = 0; i < count; i++) samples[i] = new MapSurfaceSample
            {
                Cell = new int2(i % 2048, i / 2048), SurfaceId = i, LayerId = 0, Height = heights[i], Normal = math.lengthsq(normals[i]) > 0 ? normals[i] : new float3(0, 1, 0), SlopeDegrees = slopes[i],
                SurfaceType = kinds[i], MovementMask = movement[i], Flags = kinds[i] == MapSurfaceType.BridgeDeck ? MapSurfaceFlags.Bridge | MapSurfaceFlags.Road :
                    kinds[i] == MapSurfaceType.Road ? MapSurfaceFlags.Road : MapSurfaceFlags.None
            };
            using var surfaceBlob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
            var surface = LoadOrCreate<MapSurfaceDataAsset>(folder + "/Surface.asset");
            surface.ConfigureBakedSurface(Vector3.zero, 1f, new Vector2Int(2048, 1024), surfaceBlob, false);
            EditorUtility.SetDirty(surface);
            var grid = LoadOrCreate<GridAuthoringSceneConfigAsset>(folder + "/Grid.asset");
            var gridData = new SerializedObject(grid);
            gridData.FindProperty("width").intValue = 2048; gridData.FindProperty("height").intValue = 1024;
            gridData.FindProperty("cellSize").floatValue = 1f; gridData.FindProperty("origin").vector3Value = Vector3.zero;
            // Typed assignment avoids millions of SerializedProperty traversals.
            gridData.ApplyModifiedPropertiesWithoutUndo();
            typeof(GridAuthoringConfig).GetField("blockedCells", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(grid, blockedCells.ToArray());
            EditorUtility.SetDirty(grid);
            var root = new GameObject("PreparedGridAndSurface");
            root.AddComponent<GridAuthoring>().Configure(grid);
            var authoring = root.AddComponent<MapSurfaceAuthoring>();
            var data = new SerializedObject(authoring); data.FindProperty("bakedSurfaceData").objectReferenceValue = surface;
            data.FindProperty("gridConfig").objectReferenceValue = grid; data.ApplyModifiedPropertiesWithoutUndo();
            Rect playable = new(m.runtimePlayableMin, m.playableSize);
            AddOutsideBlocker(root.transform, 0, 0, 2048, (int)playable.yMin);
            AddOutsideBlocker(root.transform, 0, (int)playable.yMax, 2048, 1024 - (int)playable.yMax);
            AddOutsideBlocker(root.transform, 0, (int)playable.yMin, (int)playable.xMin, (int)playable.height);
            AddOutsideBlocker(root.transform, (int)playable.xMax, (int)playable.yMin, 2048 - (int)playable.xMax, (int)playable.height);
            AssetDatabase.SaveAssets();
            output.surfacePath = AssetDatabase.GetAssetPath(surface); output.surfaceGuid = AssetDatabase.AssetPathToGUID(output.surfacePath);
            output.surfaceHash = MapVariantPreparationInventory.FileHash(output.surfacePath);
            output.gridPath = AssetDatabase.GetAssetPath(grid); output.gridGuid = AssetDatabase.AssetPathToGUID(output.gridPath);
            output.gridHash = MapVariantPreparationInventory.FileHash(output.gridPath);
            output.staticBlockCells = staticMovement.Count(v => v); output.waterAndStaticBlockedCells = blockedCells.Count;
        }

        private static void RasterizeWalkableTriangles(GameObject root, Vector3 offset, MapPreparationManifest m, float[] heights, Vector3[] normals)
        {
            Rect playable = new(m.runtimePlayableMin, m.playableSize);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh; if (mesh == null) continue;
                Vector3[] vertices = mesh.vertices; int[] triangles = mesh.triangles;
                Matrix4x4 matrix = Matrix4x4.Translate(offset) * filter.transform.localToWorldMatrix;
                for (int n = 0; n < triangles.Length; n += 3)
                {
                    Vector3 a = matrix.MultiplyPoint3x4(vertices[triangles[n]]), b = matrix.MultiplyPoint3x4(vertices[triangles[n + 1]]), c = matrix.MultiplyPoint3x4(vertices[triangles[n + 2]]);
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    if (normal.y < .65f) continue;
                    float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                    if (Mathf.Abs(denominator) < .00001f) continue;
                    int xMin = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x))), xMax = Mathf.Min(2048, Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)));
                    int zMin = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.z, b.z, c.z))), zMax = Mathf.Min(1024, Mathf.CeilToInt(Mathf.Max(a.z, b.z, c.z)));
                    for (int z = zMin; z < zMax; z++) for (int x = xMin; x < xMax; x++)
                    {
                        float px = x + .5f, pz = z + .5f; if (!playable.Contains(new Vector2(px,pz))) continue;
                        float u = ((b.z - c.z) * (px - c.x) + (c.x - b.x) * (pz - c.z)) / denominator;
                        float v = ((c.z - a.z) * (px - c.x) + (a.x - c.x) * (pz - c.z)) / denominator;
                        if (u < -.00001f || v < -.00001f || u + v > 1.00001f) continue;
                        float height = u * a.y + v * b.y + (1 - u - v) * c.y; int i = z * 2048 + x;
                        if (height > heights[i]) { heights[i] = height; normals[i] = normal; }
                    }
                }
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); }
            return value;
        }
        private static void AddOutsideBlocker(Transform parent, int x, int z, int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            var go = new GameObject("OutsidePlayable"); go.transform.SetParent(parent, false);
            var blocker = go.AddComponent<StaticGridBlockerAuthoring>(); var data = new SerializedObject(blocker);
            data.FindProperty("cell").vector2IntValue = new Vector2Int(x, z); data.FindProperty("size").vector2IntValue = new Vector2Int(w, h);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ResolveZones(MapVariantBuilder b, MapPreparationManifest m, PreparedCandidateOutput output)
        {
            // Explicit district/anchor decisions preserve original inventory for comparison.
            // Districts are reduced at the north edge, with no expansion of playable bounds.
            var decisions = new Dictionary<string, (Vector2 center, Vector2 size, string reason)>
            {
                ["RefineryDistrict/SplitFront_Ridge"] = (new(760, 550), new(480, 280), "Reduce ridge district north edge to 690m; maintain deployment margin"),
                ["CityEdgeAirfield/AirCorridor_Tower"] = (new(680, 590), new(520, 200), "Reduce north corridor district to 690m"),
                ["CityEdgeAirfield/Airlift_Helipad"] = (new(800, 620), new(260, 140), "Reduce north helipad district to 690m; boarding clearance still requires movement validation"),
                ["CityEdgeAirfield/Anchor_CityGate"] = (new(705, 690), new(14, 8), "Move boundary anchor 10m inward; preserve gate approach rather than out-of-map center"),
                ["AshLinePort/RouteReopened_Hub"] = (new(530, 595), new(176, 190), "Reduce north hub district to 690m"),
                ["AshLinePort/SupplyYard_East"] = (new(850, 595), new(236, 190), "Reduce north supply district to 690m"),
                ["AshLinePort/Anchor_EastCheckpoint"] = (new(980, 505), new(30, 30), "Move boundary anchor 20m inward to playable checkpoint approach")
            };
            foreach (var input in m.zones)
            {
                Vector2 center = new(input.sourceCenter.x, input.sourceCenter.z), size = new(input.size.x, input.size.z);
                string reason = "Preserve authored footprint";
                if (!input.entireFootprintInsidePlayable)
                {
                    if (!decisions.TryGetValue(b.MapId + "/" + input.id, out var d)) throw new InvalidOperationException("Missing bounds decision: " + input.id);
                    center = d.center; size = d.size; reason = d.reason;
                }
                bool inside = MapVariantPreparationSchema.Contains(b.Playable, new Rect(center - size * .5f, size));
                bool valid = MapVariantPreparationSchema.TrySurface(b, center, out float h, out string kind);
                if (!inside) throw new InvalidOperationException("Explicit bounds decision failed: " + input.id);
                output.zones.Add(new MapPreparationZone
                {
                    id = input.id, sourceCenter = input.sourceCenter, runtimeCenter = new Vector3(center.x, h, center.y) + output.sourceToRuntimeTranslation,
                    size = new Vector3(size.x, 0, size.y), entireFootprintInsidePlayable = inside, centerSurfaceValid = valid,
                    surfaceKind = kind, boundsDecision = reason + "; center surface " + (valid ? "valid" : "FAILED")
                });
            }
        }
        private static string Review(PreparedCandidateOutput o) =>
            "# " + o.variant + " prepared authoring candidate\n\nStatus: In progress. Authoring and surface artifacts only; runtime packing, actual routes, native captures and device measurements remain gates.\n\n" +
            $"- Independent neutral building owners: {o.owners.Count}; original duplicates: {o.duplicateOriginals}.\n" +
            $"- Static alternatives removed by mapped branch provenance: {o.staticAlternativesRemoved}.\n" +
            $"- Static movement cells: {o.staticBlockCells}; water/static exclusion cells: {o.waterAndStaticBlockedCells}; bridge deck cells: {o.bridgeSurfaceCells}.\n" +
            "- Unsupported industrial/civic destruction pairs explicitly become non-destructible scenery. No unrelated rubble substitution.\n" +
            "- Rotated static footprints include partial boundary intersections. Gameplay owners use the existing conservative AABB runtime blocker contract.\n" +
            "- Surface build exclusion is separate from movement exclusion. Dynamic building footprints are blocked by owners, not duplicated as grid static blockers.\n" +
            $"- Semantic hash: `{o.semanticHash}`; regeneration: `{o.regenerationMethod}`; replay: {o.deterministicReplay}.\n\n" +
            "## Bounds decisions\n\n| Zone | Decision | Center surface |\n|---|---|---|\n" +
            string.Join("\n", o.zones.Select(z => $"| {z.id} | {z.boundsDecision} | {z.surfaceKind} |")) + "\n";
    }
    [Serializable] internal sealed class PreparedCandidateOutput
    {
        public string mapId, variant, generatorVersion, inputHash, semanticHash, scenePath, sceneGuid, sceneHash, regenerationMethod;
        public string surfacePath, surfaceGuid, surfaceHash, gridPath, gridGuid, gridHash;
        public string deterministicReplay = "NotCompared";
        public Vector3 sourceToRuntimeTranslation;
        public Vector2 runtimePlayableMin, playableSize;
        public int generatedIdentityCount, renderOnlyIdentityCount;
        public int duplicateOriginals, staticAlternativesRemoved, staticBlockCells, waterAndStaticBlockedCells, bridgeSurfaceCells, rendererCount;
        public float generationSeconds;
        public MapVariantBeautify.Report presentation;
        public List<PreparedOwner> owners = new();
        public List<MapPreparationPlacement> classifications = new();
        public List<MapPreparationZone> zones = new();
    }
    [Serializable] internal sealed class PreparedOwner
    {
        public string placementKey, stableId, sourceGuid, destroyedGuid, definitionGuid;
        public Vector3 position, scale;
        public Quaternion rotation;
        public Vector2Int origin, footprint;
        public int intactRendererCount, destroyedRendererCount;
    }
    [Serializable] internal sealed class PreparedSemanticOutput
    {
        public string inputHash, surfaceHash, gridHash;
        public List<PreparedOwner> owners;
        public List<MapPreparationPlacement> classifications;
        public List<MapPreparationZone> zones;
    }
}
