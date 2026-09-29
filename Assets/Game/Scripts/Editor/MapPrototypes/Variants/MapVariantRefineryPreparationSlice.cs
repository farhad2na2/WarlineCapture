using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    // Small, isolated architecture fixture. It is not registered in a runtime catalog.
    public static class MapVariantRefineryPreparationSlice
    {
        internal const string AssetRoot = "Assets/Game/GeneratedOperationMaps/Variants/RefineryDistrict/Slice";
        internal const string ScenePath = "Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/PreparationSlice.unity";
        internal const string MapId = "opmap.skirmish.refinery_preparation_slice";
        internal const string ReportFolder = MapVariantPreparationInventory.ReportRoot + "/RefineryDistrict/Slice";
        internal static readonly Rect Area = new(550f, 300f, 110f, 80f);

        [MenuItem("Game/Map Variants/Preparation/Build Refinery Building Slice")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || MapVariantBuilder.InventoryOnly)
                throw new InvalidOperationException("Preparation slice requires idle edit mode.");
            var protectedHashes = MapVariantPreparationInventory.ProtectedHashes();
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            MapVariantBuilder.InventoryOnly = true;
            try
            {
                MapVariantBuilder b = MapVariantRefineryDistrict.Build();
                MapPreparationManifest manifest = MapVariantPreparationInventory.CreateManifest(b);
                // A small cluster includes House_06's combined roof/details, House_01 and
                // their actual neighboring buildings. No synthesized attachment is needed.
                var selected = b.Placements.Where(p => p.Layer == MapVariantLayer.City &&
                    MapVariantKits.Houses.Contains(p.PrefabPath) &&
                    MapVariantPreparationSchema.Contains(Area,
                        MapVariantBuilder.FootprintAabb(p.Center, p.HalfSize, p.Yaw, 0f)))
                    .OrderBy(p => manifest.placements.Single(r => r.stableKey == Key(p)).stableKey, StringComparer.Ordinal).ToList();
                if (!selected.Any(p => p.PrefabPath == MapVariantKits.Houses[5]) || selected.Count < 2)
                    throw new InvalidOperationException("Refinery slice must contain combined roof geometry and independent neighbors.");
                var keep = new HashSet<GameObject>(selected.Select(p => p.Instance));
                foreach (MapVariantPlacement p in b.Placements)
                    if (!keep.Contains(p.Instance)) UnityEngine.Object.DestroyImmediate(p.Instance);
                // Ground/roads have independent generator objects. Replace with a cropped,
                // persistent derivative of the identical triangulation, never the original mesh.
                foreach (Transform child in b.Layer(MapVariantLayer.Ground).Cast<Transform>().ToArray())
                {
                    MeshFilter filter = child.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null && !EditorUtility.IsPersistent(filter.sharedMesh))
                        UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(b.Layer(MapVariantLayer.Roads).gameObject);
                UnityEngine.Object.DestroyImmediate(b.Layer(MapVariantLayer.Zones).gameObject);
                CreateGround(b);
                Directory.CreateDirectory(ReportFolder);
                var view = MapVariantView.Battle("", Area.center, 0f, 64f);
                Capture(scene, view, ReportFolder + "/before.png");

                DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(scene, manifest);
                DenseCityBuildingDefinitionLibrary definitions = DenseCityBuildingDefinitionLibrary.LoadExisting();
                var output = new SliceOutput { mapId = MapId, sourceInventoryHash = MapVariantPreparationSchema.Hash(JsonUtility.ToJson(manifest)) };
                string outputPath = ReportFolder + "/output-manifest.json";
                SliceOutput previousOutput = File.Exists(outputPath)
                    ? JsonUtility.FromJson<SliceOutput>(File.ReadAllText(outputPath)) : null;
                int index = 0;
                foreach (MapVariantPlacement p in selected)
                {
                    string key = Key(p);
                    MapPreparationPlacement input = manifest.placements.Single(r => r.stableKey == key);
                    if (input.qualification != "Mapped" || input.category != "GameplayBuilding" || input.outsidePlayable)
                        throw new InvalidOperationException("Unqualified class cannot become runtime-active: " + key);
                    GameObject intact = b.Info(p.PrefabPath).Prefab;
                    GameObject destroyed = ExtractDestroyed(intact);
                    DenseCityVisualAssetMetadata intactMetadata = DenseCityVisualAssetMetadataExtractor.Extract(
                        intact, null, r => DenseCityBuildingIntactVisualPolicy.ShouldIncludeRenderer(intact, r));
                    DenseCityVisualAssetMetadata destroyedMetadata = DenseCityVisualAssetMetadataExtractor.Extract(destroyed);
                    MapVariantBuilder.TryGetRenderBounds(p.Instance, out Bounds bounds);
                    // The current runtime blocker contract is axis-aligned. Keep the exact yaw
                    // polygon in inventory and use a conservative cell enclosure here.
                    Vector2Int origin = new(Mathf.FloorToInt(bounds.min.x), Mathf.FloorToInt(bounds.min.z));
                    Vector2Int cells = new(Mathf.CeilToInt(bounds.max.x) - origin.x, Mathf.CeilToInt(bounds.max.z) - origin.y);
                    var group = DenseCityBuildingRecordFactory.Create(new DenseCityBuildingRecordInput(
                        "map-prep-" + key, b.Seed, 0, 0,
                        intactMetadata.PrefabAssetGuid, intactMetadata.PrefabLocalId,
                        destroyedMetadata.PrefabAssetGuid, destroyedMetadata.PrefabLocalId,
                        intactMetadata.MaterialAssetGuids, destroyedMetadata.MaterialAssetGuids,
                        p.Instance.transform.localToWorldMatrix, origin, cells, p.HalfSize * 2f,
                        p.Instance.transform.position.y, bounds, p.Instance.transform.forward,
                        GeneratedCityBuildingRole.House, definitions.ResolveAssetGuid(GeneratedCityBuildingRole.House),
                        0, 350, uint.MaxValue, 0, new Vector2Int(origin.x / 64, origin.y / 64), "refinery"));
                    DenseCityRealizedBuildingPresentation realized = DenseCityBuildingPresentationRealizer.Realize(
                        MapId, group.Building, group.IntactPresentation, group.DestroyedPresentation,
                        hierarchy, definitions, placementIndexOverride: index++);
                    realized.Authoring.name = "RefineryBuilding_" + key.Substring(key.Length - 12);
                    // Native authoring view also begins with only intact geometry visible.
                    // Baking still includes the destroyed hierarchy for runtime transitions.
                    realized.DestroyedVisualRoot.localScale = Vector3.zero;
                    output.owners.Add(new SliceOwner
                    {
                        placementKey = key, stableId = realized.Authoring.StableId,
                        sourcePrefab = p.PrefabPath, sourcePrefabGuid = intactMetadata.PrefabAssetGuid,
                        destroyedPrefab = AssetDatabase.GetAssetPath(destroyed), destroyedPrefabGuid = destroyedMetadata.PrefabAssetGuid,
                        ownerPosition = realized.Authoring.transform.position, originalRemoved = true,
                        intactRenderers = realized.IntactVisualRoot.GetComponentsInChildren<Renderer>(true).Length,
                        destroyedRenderers = realized.DestroyedVisualRoot.GetComponentsInChildren<Renderer>(true).Length,
                        attachmentPolicy = "All source Model descendants follow intact state; matching source Destroyed descendants follow destroyed state"
                    });
                    // Provenance identifies exactly this original, regardless of overlap.
                    UnityEngine.Object.DestroyImmediate(p.Instance);
                }
                Capture(scene, view, ReportFolder + "/intact.png");
                if (previousOutput != null && previousOutput.sourceInventoryHash == output.sourceInventoryHash &&
                    JsonUtility.ToJson(new OwnerList { owners = previousOutput.owners }) !=
                    JsonUtility.ToJson(new OwnerList { owners = output.owners }))
                    throw new InvalidOperationException("Identical slice inputs changed owner identities, transforms or presentation provenance.");
                var owners = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).ToArray();
                if (owners.Length != output.owners.Count || owners.Select(o => o.StableId).Distinct().Count() != owners.Length)
                    throw new InvalidOperationException("Slice owner count or identity uniqueness failed.");
                foreach (var owner in owners)
                    if (!owner.TryValidate(out string error) || !DenseCityBuildingIntactVisualPolicy.TryValidateNormalized(owner.IntactVisualRoot, out error))
                        throw new InvalidOperationException(error);
                // State reference image only; runtime destruction is validated separately from baking.
                var damaged = owners.First(o => output.owners.Single(r => r.stableId == o.StableId).sourcePrefab == MapVariantKits.Houses[5]);
                damaged.IntactVisualRoot.transform.localScale = Vector3.zero;
                damaged.DestroyedVisualRoot.transform.localScale = Vector3.one;
                Capture(scene, view, ReportFolder + "/destroyed-state-reference.png");
                damaged.IntactVisualRoot.transform.localScale = Vector3.one;
                damaged.DestroyedVisualRoot.transform.localScale = Vector3.zero;
                // Authoring encodes each state's full visible scale. The runtime destruction
                // system hides the inactive state before presentation; zero here would bake
                // a 0.0001 destroyed-visible scale and break the eventual damage transition.
                foreach (var owner in owners) owner.DestroyedVisualRoot.transform.localScale = Vector3.one;
                MapVariantBuilder.EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Slice scene save failed.");
                output.scenePath = ScenePath;
                output.sceneGuid = AssetDatabase.AssetPathToGUID(ScenePath);
                output.sceneHash = MapVariantPreparationInventory.FileHash(ScenePath);
                output.contentHash = MapVariantPreparationSchema.Hash(JsonUtility.ToJson(output));
                File.WriteAllText(outputPath, JsonUtility.ToJson(output, true));
                AssetDatabase.SaveAssets();
                // Additive candidate files are allowed; every pre-existing protected file must retain its bytes.
                foreach (var pair in protectedHashes)
                    if (MapVariantPreparationInventory.FileHash(pair.Key) != pair.Value)
                        throw new InvalidOperationException("Protected source changed: " + pair.Key);
                Debug.Log($"[MapPreparationSlice] result=Passed map=RefineryDistrict owners={owners.Length} " +
                    $"duplicateOriginals=0 contentHash={output.contentHash} " +
                    $"deterministicOwnerReplay={(previousOutput != null && previousOutput.sourceInventoryHash == output.sourceInventoryHash ? "Passed" : "NotCompared")} scope=AuthoringFixture");
            }
            catch (Exception e)
            {
                Debug.LogError("[MapPreparationSlice] result=Failed " + e.Message); throw;
            }
            finally
            {
                MapVariantBuilder.InventoryOnly = false;
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static string Key(MapVariantPlacement p) => MapVariantPreparationSchema.PlacementKey(
            "RefineryDistrict", AssetDatabase.AssetPathToGUID(MapVariantPreparationInventory.BasePath(p.PrefabPath)), p.Instance.transform.localToWorldMatrix);

        internal static GameObject ExtractDestroyed(GameObject source, string assetRoot = AssetRoot)
        {
            var row = MapVariantPreparationInventory.InspectPrefab(source);
            if (row.hierarchyStatus != "DirectAlternativeValidated") throw new InvalidOperationException("Ambiguous alternative: " + row.path);
            string path = assetRoot + "/Destroyed/" + row.guid + ".prefab";
            MapVariantBuilder.EnsureFolder(assetRoot + "/Destroyed");
            var root = new GameObject("DestroyedPresentation");
            try
            {
                // Preserve the exact branch's authored local transform, materials and meshes.
                var branch = UnityEngine.Object.Instantiate(source.transform.Find("Destroyed").gameObject, root.transform, false);
                branch.name = "Destroyed";
                foreach (Component c in branch.GetComponentsInChildren<Component>(true))
                    if (c is Collider || c is Rigidbody || c is Joint) UnityEngine.Object.DestroyImmediate(c);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        internal static DenseCityPresentationHierarchyContext CreateHierarchy(Scene scene, MapPreparationManifest m, string generationId = "refinery-preparation-slice-v1")
        {
            var root = new GameObject("PreparedEntityPresentation");
            SceneManager.MoveGameObjectToScene(root, scene);
            var authoring = root.AddComponent<DenseCityGeneratedRootAuthoring>();
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("role").intValue = (int)DenseCityGeneratedRootRole.EntityPresentationSource;
            serialized.FindProperty("generationId").stringValue = generationId;
            serialized.FindProperty("generatorSchema").stringValue = MapVariantPreparationSchema.Version;
            serialized.FindProperty("generatorSchemaVersion").intValue = 1;
            serialized.FindProperty("deterministicSeed").intValue = m.seed;
            serialized.FindProperty("deterministicGenerationHash").stringValue = MapVariantPreparationSchema.Hash(JsonUtility.ToJson(m));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (string path in new[] { "GameplayBuildings/Buildings", "GameplayBuildings/CivicAndMarket",
                "RenderOnly/Infrastructure", "RenderOnly/Vegetation", "RenderOnly/Props", "RenderOnly/Horizon" })
            {
                Transform parent = root.transform;
                foreach (string segment in path.Split('/'))
                {
                    Transform child = parent.Find(segment);
                    if (child == null) { child = new GameObject(segment).transform; child.SetParent(parent, false); }
                    parent = child;
                }
            }
            return DenseCityPresentationHierarchyContext.Create(authoring);
        }

        private static void CreateGround(MapVariantBuilder b)
        {
            MapVariantBuilder.EnsureFolder(AssetRoot);
            Mesh mesh = b.Height.BuildChunkMesh(110, 60, 22, 16, MapVariantBuilder.ResolveGroundUv(), "SliceGround");
            string path = AssetRoot + "/Ground.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
            var go = new GameObject("PreparedSliceGround", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(b.Layer(MapVariantLayer.Ground), false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_A.mat");
        }

        internal static void Capture(Scene scene, MapVariantView view, string path)
        {
            var go = new GameObject("PreparationEvidenceCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.GetComponent<Camera>();
            var target = new RenderTexture(1920, 1080, 24);
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.transform.SetPositionAndRotation(view.Position, Quaternion.Euler(view.Euler));
                camera.fieldOfView = view.FieldOfView; camera.farClipPlane = 4000f; camera.targetTexture = target;
                camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply(); File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        [Serializable] internal sealed class SliceOutput
        {
            public string mapId, sourceInventoryHash, scenePath, sceneGuid, sceneHash, contentHash;
            public string status = "In progress; authoring fixture only; warehouse/industrial mapping and packed runtime pending";
            public List<SliceOwner> owners = new();
        }
        [Serializable] internal sealed class SliceOwner
        {
            public string placementKey, stableId, sourcePrefab, sourcePrefabGuid, destroyedPrefab, destroyedPrefabGuid, attachmentPolicy;
            public Vector3 ownerPosition;
            public bool originalRemoved;
            public int intactRenderers, destroyedRenderers;
        }
        [Serializable] private sealed class OwnerList { public List<SliceOwner> owners; }
    }
}
