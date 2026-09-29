using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
using Game.Runtime;
using Unity.Entities.Build;
using Unity.Collections;
using Unity.Entities.Content;
using Unity.Scenes;
using Unity.Scenes.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Hash128 = Unity.Entities.Hash128;

namespace Game.Editor.MapVariants
{
    public static class MapVariantCandidateRuntimeBuilder
    {
        private const BuildTarget ValidationTarget = BuildTarget.StandaloneOSX;
        public static void BuildMediumContent()
        {
            foreach (string map in MapVariantPreparedCandidateBuilder.MediumMaps) Build(map);
            Debug.Log("[MapVariantPackedContent] result=Passed maps=3 productionSettings=Unchanged scope=ContentBuildOnly");
        }
        public static void BuildIntegrationContent() { BuildMediumContent(); BuildComparisonContentAndReferences(); }
        public static void BuildExistingMapComparison()
        {
            const string map = "ExistingDenseCity";
            using var outputTransaction = new PreparedContentOutputTransaction(map);
            const string definitionPath = "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
            var protectedHashes = MapVariantPreparationInventory.ProtectedHashes();
            var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(definitionPath);
            if (definition == null || !definition.TryValidateMetadata(out string error)) throw new InvalidOperationException("Existing definition invalid.");
            string bindingPath = AssetDatabase.GUIDToAssetPath(definition.SourceSceneReference.AssetGUID);
            string catalog = BuildAddressables(map, definitionPath, bindingPath,
                AssetDatabase.GUIDToAssetPath(definition.MapSurfaceDataReference.AssetGUID),
                AssetDatabase.GUIDToAssetPath(definition.MinimapRasterReference.AssetGUID));
            string content = "Library/MapVariantPreparedContent/" + map + "/Entities";
            Directory.CreateDirectory(content);
            RemoteContentCatalogBuildUtility.BuildContent(new HashSet<Hash128>{new(definition.NavigationMetadata.AuthoredSubSceneGuid)},
                DotsGlobalSettings.Instance.GetClientGUID(), ValidationTarget, Path.GetFullPath(content));
            string entityCatalog = content + "/" + RuntimeContentManager.RelativeCatalogPath;
            if (!File.Exists(entityCatalog)) throw new InvalidOperationException("Existing comparison entity catalog missing.");
            var report = new VariantRuntimeContentReport
            {
                variant=map, mapId=definition.OperationMapId, semanticHash=definition.ContentHash, target=ValidationTarget.ToString(),
                entitySceneGuid=definition.NavigationMetadata.AuthoredSubSceneGuid, entityContentPath=content,
                entityCatalogPath=entityCatalog, entityCatalogHash=MapVariantPreparationInventory.FileHash(entityCatalog),
                addressablesCatalogPath=catalog, addressablesCatalogHash=MapVariantPreparationInventory.FileHash(catalog),
                definitionAddress="map-variant-candidate/" + map + "/definition", definitionPath=definitionPath,
                definitionGuid=AssetDatabase.AssetPathToGUID(definitionPath), definitionHash=MapVariantPreparationInventory.FileHash(definitionPath),
                bindingPath=bindingPath, bindingGuid=AssetDatabase.AssetPathToGUID(bindingPath), bindingHash=MapVariantPreparationInventory.FileHash(bindingPath),
                status="Read-only repack of existing map; switching acceptance pending"
            };
            string output = MapVariantPreparationInventory.ReportRoot + "/ExistingDenseCity"; Directory.CreateDirectory(output);
            File.WriteAllText(output + "/runtime-content.json", JsonUtility.ToJson(report,true));
            MapVariantPreparationInventory.RequireProtected(protectedHashes);
            outputTransaction.Commit();
            Debug.Log("[MapVariantExistingContent] result=Passed protectedSources=Unchanged scope=ReadOnlyComparisonContent");
        }
        public static void BuildComparisonContentAndReferences()
        {
            BuildMatchingPrototypeReferences(); BuildExistingMapComparison();
        }
        public static void BuildMatchingPrototypeReferences()
        {
            var protectedHashes = MapVariantPreparationInventory.ProtectedHashes();
            Scene previous = SceneManager.GetActiveScene();
            foreach(string map in MapVariantPreparedCandidateBuilder.MediumMaps)
            {
                Scene source = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(source); MapVariantBuilder.InventoryOnly=true;
                    var b = MapVariantPreparedCandidateBuilder.BuildSource(map);
                    string report = MapVariantPreparationInventory.ReportRoot + "/" + map + "/Candidate";
                    MapVariantRefineryPreparationSlice.Capture(source,MapVariantView.Battle("",b.Playable.center,0,180), report+"/battle-prototype-reference.png");
                    MapVariantRefineryPreparationSlice.Capture(source,new MapVariantView("Top",new Vector3(b.Playable.center.x,600,b.Playable.center.y),new Vector3(90,0,0),60),report+"/top-prototype-reference.png");
                }
                finally
                {
                    MapVariantBuilder.InventoryOnly=false;
                    foreach(var root in source.GetRootGameObjects()) foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
                        if(f.sharedMesh!=null && !EditorUtility.IsPersistent(f.sharedMesh)) UnityEngine.Object.DestroyImmediate(f.sharedMesh);
                    EditorSceneManager.CloseScene(source,true);
                    if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
            MapVariantPreparationInventory.RequireProtected(protectedHashes);
            Debug.Log("[MapVariantPrototypeReferences] result=Passed maps=3 protectedSources=Unchanged camera=Matched");
        }

        public static void RefreshBindingEnvironmentsAndAddressables()
        {
            var protectedHashes=MapVariantPreparationInventory.ProtectedHashes();
            Scene previous=SceneManager.GetActiveScene();
            foreach(string map in MapVariantPreparedCandidateBuilder.MediumMaps)
            {
                string report=MapVariantPreparationInventory.ReportRoot+"/"+map+"/Candidate/runtime-content.json";
                var content=JsonUtility.FromJson<VariantRuntimeContentReport>(File.ReadAllText(report));
                var output=JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText(MapVariantPreparationInventory.ReportRoot+"/"+map+"/Candidate/output-manifest.json"));
                Scene source=default,binding=default;
                try
                {
                    source=EditorSceneManager.OpenScene(output.scenePath,OpenSceneMode.Additive); SceneManager.SetActiveScene(source);
                    var environment=new PreparedLightingEnvironment();
                    binding=EditorSceneManager.OpenScene(content.bindingPath,OpenSceneMode.Additive); SceneManager.SetActiveScene(binding);
                    environment.Apply();
                    var view=binding.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapSceneView>(true)).Single();
                    // The placeholder is metadata for the loader. A native player calls
                    // SubScene.OnEnable even when AutoLoadScene is false, so it must stay disabled.
                    view.MapSubScene.enabled=false;
                    if(view.MapSubScene.SceneGUID.IsValid || !view.TryValidate(out string error)) throw new InvalidOperationException("Binding must use explicit packed ownership.");
                    if(!EditorSceneManager.SaveScene(binding,content.bindingPath)) throw new IOException("Binding environment save failed.");
                }
                finally
                {
                    if(binding.IsValid() && binding.isLoaded) EditorSceneManager.CloseScene(binding,true);
                    if(source.IsValid() && source.isLoaded) EditorSceneManager.CloseScene(source,true);
                    if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
                var definition=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(content.definitionPath);
                Set(definition,"sourceSceneReference",new AssetReference(content.bindingGuid));
                EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
                if(!definition.TryValidateMetadata(out string definitionError) || !definition.TryValidateLocalContentReferences(out definitionError))
                    throw new InvalidOperationException("Refreshed definition invalid: "+definitionError);
                content.addressablesCatalogPath=BuildAddressables(map,content.definitionPath,content.bindingPath,output.surfacePath,
                    MapVariantPreparedCandidateBuilder.AssetRoot+"/"+map+"/Candidate/Minimap.png");
                content.addressablesCatalogHash=MapVariantPreparationInventory.FileHash(content.addressablesCatalogPath);
                content.bindingHash=MapVariantPreparationInventory.FileHash(content.bindingPath);
                content.definitionHash=MapVariantPreparationInventory.FileHash(content.definitionPath);
                File.WriteAllText(report,JsonUtility.ToJson(content,true));
                MapVariantPreparationInventory.RequireProtected(protectedHashes);
                Debug.Log("[MapVariantBindingEnvironment] result=Passed map="+map+" sourceAmbientFog=Matched packedOwnership=Explicit");
            }
            Debug.Log("[MapVariantBindingEnvironment] result=Passed maps=3 sourceAmbientFog=Matched packedOwnership=Explicit");
        }
        public static void BuildRefineryContent() => Build("RefineryDistrict");
        public static void BuildAirfieldContent() => Build("CityEdgeAirfield");
        public static void BuildPortContent() => Build("AshLinePort");
        private static void Build(string map)
        {
            using var outputTransaction = new PreparedContentOutputTransaction(map);
            var protectedHashes = MapVariantPreparationInventory.ProtectedHashes();
            string report = MapVariantPreparationInventory.ReportRoot + "/" + map + "/Candidate";
            var o = JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText(report + "/output-manifest.json"));
            string folder = MapVariantPreparedCandidateBuilder.AssetRoot + "/" + map + "/Candidate";
            Scene previous = SceneManager.GetActiveScene();
            Scene source = EditorSceneManager.OpenScene(o.scenePath, OpenSceneMode.Additive);
            Scene binding = default;
            try
            {
                SceneManager.SetActiveScene(source);
                var surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(o.surfacePath);
                var grid = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(o.gridPath);
                GridAuthoring gridAuthoring = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GridAuthoring>(true)).Single();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(gridAuthoring, out _, out long localId);
                if (localId == 0) localId = checked((long)GlobalObjectId.GetGlobalObjectIdSlow(gridAuthoring).targetObjectId);
                if (localId <= 0) throw new InvalidOperationException("Saved grid authoring has no positive local ID.");
                var owners = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).ToArray();
                foreach (var owner in owners) owner.DestroyedVisualRoot.transform.localScale = Vector3.zero;
                Transform sourceLighting = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Lighting");
                sourceLighting.gameObject.SetActive(true);
                CreateMinimap(source, o, folder + "/Minimap.png");
                sourceLighting.gameObject.SetActive(false);
                foreach (var owner in owners) owner.DestroyedVisualRoot.transform.localScale = Vector3.one;
                AssetDatabase.ImportAsset(folder + "/Minimap.png", ImportAssetOptions.ForceSynchronousImport);
                var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(folder + "/Definition.asset");
                if (definition == null) { definition = ScriptableObject.CreateInstance<OperationMapDefinition>(); AssetDatabase.CreateAsset(definition, folder + "/Definition.asset"); }
                Set(definition, "operationMapId", o.mapId);
                Set(definition, "sourceIdentityHash", o.inputHash); Set(definition, "contentHash", o.semanticHash); Set(definition, "generatedMetadataHash", o.semanticHash);
                Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
                Set(definition, "renderResidencyMode", OperationMapRenderResidencyMode.ResidentEntities);
                Vector3 min = new(o.runtimePlayableMin.x, -20, o.runtimePlayableMin.y);
                Vector3 max = min + new Vector3(o.playableSize.x, 1000, o.playableSize.y);
                Set(definition, "bounds", new OperationMapBoundsConfig(new Vector3(0, -20, 0), new Vector3(2048, 1000, 1024), min, max,
                    new Vector3(0, -20, 0), new Vector3(2048, 1000, 1024)));
                Set(definition, "gridMetadata", new OperationMapGridMetadataConfig(o.gridGuid, o.gridHash, Vector3.zero, new Vector2Int(2048, 1024), 1, grid.BlockedCells.Length));
                Set(definition, "surfaceMetadata", new OperationMapSurfaceMetadataConfig(o.surfaceGuid, o.surfaceHash,
                    surface.ComputeRuntimeBlobHash().ToString(), surface.SurfaceCount, surface.PayloadVersion, surface.PayloadEncoding, SurfaceRange(surface).Item1, SurfaceRange(surface).Item2));
                Set(definition, "navigationMetadata", new OperationMapNavigationMetadataConfig(o.sceneGuid, localId, source.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<StaticGridBlockerAuthoring>(true).Length) + owners.Length, true, true, true));
                Vector2 center = o.runtimePlayableMin + o.playableSize * .5f;
                var view = MapVariantView.Battle("", center, 0, 180);
                Set(definition, "cameras", new[]
                {
                    new OperationMapCameraConfig("camera.skirmish.battle", view.Position, view.Euler, false, view.FieldOfView, 0, true),
                    new OperationMapCameraConfig("camera.skirmish.planning", new Vector3(center.x, 600, center.y), new Vector3(90,0,0), true, 60, 220, true)
                });
                Set(definition, "planningCameraId", "camera.skirmish.planning"); Set(definition, "battleCameraId", "camera.skirmish.battle");
                Set(definition, "minimap", new OperationMapMinimapConfig("minimap.skirmish.main", new Vector3(min.x,0,min.z), o.playableSize, 0));
                // Preparation zones remain metadata proposals. Mission-specific anchors and
                // objectives require later authoring and complete footprint/movement acceptance.
                Set(definition, "anchors", new[] { new OperationMapAnchorConfig("anchor.preparation.overview", OperationMapAnchorKind.Camera, view.Position, view.Euler, 0) });
                Set(definition, "mapSurfaceDataReference", new AssetReference(o.surfaceGuid));
                Set(definition, "minimapRasterReference", new AssetReference(AssetDatabase.AssetPathToGUID(folder + "/Minimap.png")));
                EditorUtility.SetDirty(definition);
                var environment = new PreparedLightingEnvironment();
                binding = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(binding);
                string bindingPath = MapVariantPreparedCandidateBuilder.SceneRoot + "/" + map + "/RuntimeBinding.unity";
                CreateBinding(binding, source, o, definition, grid, surface);
                environment.Apply();
                if (!EditorSceneManager.SaveScene(binding, bindingPath)) throw new IOException("Runtime binding save failed.");
                Set(definition, "sourceSceneReference", new AssetReference(AssetDatabase.AssetPathToGUID(bindingPath)));
                EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
                if (!definition.TryValidateMetadata(out string error) || !definition.TryValidateLocalContentReferences(out error))
                    throw new InvalidOperationException("Variant definition failed: " + error);
                var bindingView = binding.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OperationMapSceneView>(true)).Single();
                if (!bindingView.TryValidate(out error)) throw new InvalidOperationException("Variant binding failed: " + error);
                AssetDatabase.SaveAssets();
                EditorSceneManager.CloseScene(source, true); source = default;
                EditorSceneManager.CloseScene(binding, true); binding = default;
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                string addressablesCatalog = BuildAddressables(map, folder + "/Definition.asset", bindingPath, o.surfacePath, folder + "/Minimap.png");
                string content = "Library/MapVariantPreparedContent/" + map + "/Entities";
                Directory.CreateDirectory(content);
                Hash128 player = DotsGlobalSettings.Instance.GetClientGUID();
                if (!player.IsValid) throw new InvalidOperationException("Entities client player GUID unavailable.");
                RemoteContentCatalogBuildUtility.BuildContent(new HashSet<Hash128> { new(o.sceneGuid), new(BuildUnitFixture()) }, player,
                    ValidationTarget, Path.GetFullPath(content));
                string catalog = content + "/" + RuntimeContentManager.RelativeCatalogPath;
                if (!File.Exists(catalog)) throw new InvalidOperationException("Packed entity catalog missing.");
                var result = new VariantRuntimeContentReport
                {
                    variant = map, mapId = o.mapId, semanticHash = o.semanticHash, entitySceneGuid = o.sceneGuid,
                    target = ValidationTarget.ToString(), entityContentPath = content,
                    unitFixtureSceneGuid = AssetDatabase.AssetPathToGUID(UnitFixturePath),
                    entityCatalogPath = catalog, entityCatalogHash = MapVariantPreparationInventory.FileHash(catalog),
                    addressablesCatalogPath = addressablesCatalog, addressablesCatalogHash = MapVariantPreparationInventory.FileHash(addressablesCatalog),
                    definitionAddress = "map-variant-candidate/" + map + "/definition",
                    definitionPath = folder + "/Definition.asset", definitionGuid = AssetDatabase.AssetPathToGUID(folder + "/Definition.asset"),
                    bindingPath = bindingPath, bindingGuid = AssetDatabase.AssetPathToGUID(bindingPath),
                    entityContentBytes = Directory.GetFiles(content, "*", SearchOption.AllDirectories).Sum(p => new FileInfo(p).Length),
                    entityFileSetHash = MapVariantPreparationSchema.Hash(string.Join("\n", Directory.GetFiles(content, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal).Select(p => Path.GetRelativePath(content, p) + " " + MapVariantPreparationInventory.FileHash(p)))),
                    definitionHash = MapVariantPreparationInventory.FileHash(folder + "/Definition.asset"), bindingHash = MapVariantPreparationInventory.FileHash(bindingPath),
                    status = "Content built; packed load/unload, graphics, actual movement and device performance pending"
                };
                File.WriteAllText(report + "/runtime-content.json", JsonUtility.ToJson(result, true));
                foreach (var pair in protectedHashes)
                    if (MapVariantPreparationInventory.FileHash(pair.Key) != pair.Value) throw new InvalidOperationException("Protected source changed: " + pair.Key);
                outputTransaction.Commit();
                Debug.Log($"[MapVariantPackedContent] result=Passed map={map} bytes={result.entityContentBytes} semanticHash={o.semanticHash} " +
                    $"productionSettings=Unchanged scope=ContentBuildOnly");
            }
            catch (Exception e) { Debug.LogError("[MapVariantPackedContent] result=Failed map=" + map + " " + e); throw; }
            finally
            {
                if (source.IsValid() && source.isLoaded) EditorSceneManager.CloseScene(source, true);
                if (binding.IsValid() && binding.isLoaded) EditorSceneManager.CloseScene(binding, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        private static (float, float) SurfaceRange(MapSurfaceDataAsset surface)
        {
            if (!surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out var blob)) throw new InvalidOperationException("Surface payload could not reload for metadata.");
            using (blob)
            {
                float min = float.PositiveInfinity, max = float.NegativeInfinity;
                for (int i = 0; i < MapSurfaceBlobAccess.SurfaceCount(ref blob.Value); i++)
                    if (MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref blob.Value, i, out var sample)) { min = Mathf.Min(min,sample.Height); max = Mathf.Max(max,sample.Height); }
                return (min,max);
            }
        }

        internal const string UnitFixturePath = "Assets/Game/Scenes/OperationMaps/Variants/PreparationUnitFixture.unity";
        private static string BuildUnitFixture()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var fixture = new GameObject("PreparationUnitPrefabLibrary").AddComponent<MapPreparationUnitFixtureAuthoring>();
                fixture.InfantryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab");
                fixture.HaulerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tanker.prefab");
                fixture.ArmorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab");
                if (fixture.InfantryPrefab == null || fixture.HaulerPrefab == null || fixture.ArmorPrefab == null) throw new InvalidOperationException("Actual unit fixture prefab missing.");
                if (!EditorSceneManager.SaveScene(scene, UnitFixturePath)) throw new IOException("Unit fixture save failed.");
                return AssetDatabase.AssetPathToGUID(UnitFixturePath);
            }
            finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }

        private static string BuildAddressables(string map, string definition, string binding, string surface, string minimap)
        {
            using var outputTransaction=new PreparedContentOutputTransaction(map,"Addressables");
            const string temporary = "Assets/Game/GeneratedOperationMaps/Variants/PackagingTemp";
            if (AssetDatabase.IsValidFolder(temporary)) throw new InvalidOperationException("Packaging temp exists; inspect interrupted transaction before reuse.");
            string output = Path.GetFullPath("Library/MapVariantPreparedContent/" + map + "/Addressables");
            // BuildPath follows the active Editor platform, which may be Android.
            // The explicit validation target maps to AddressablesPlatform.OSX.
            string shared = Path.GetFullPath("Library/com.unity.addressables/aa");
            string builtRuntimePath = Path.Combine(shared, "OSX");
            string backup = Path.GetFullPath("Library/MapVariantPreparedContentTransactions/" + Guid.NewGuid().ToString("N"));
            var productionSettings = OperationMapDenseCityCandidateRuntimeContentBuilder.CandidateDirectoryTransaction.Capture(
                Path.GetFullPath("."), "Assets/AddressableAssetsData");
            var renderingSettings = OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                Path.GetFullPath("."), new[]{"Assets/Settings/PC_RPAsset.asset"});
            bool hadShared = Directory.Exists(shared);
            string sharedHash = DirectoryHash(shared);
            bool layoutEnabled = ProjectConfigData.GenerateBuildLayout;
            var layoutFormat = ProjectConfigData.BuildLayoutReportFileFormat;
            string[] layoutPaths = ProjectConfigData.BuildReportFilePaths.ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (hadShared) Directory.Move(shared, backup);
            try
            {
                var settings = AddressableAssetSettings.Create(temporary, "VariantValidationSettings", false, true);
                settings.BuildRemoteCatalog = false; settings.DisableCatalogUpdateOnStartup = true;
                settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, output + "/Bundles");
                settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, output + "/Bundles");
                var group = settings.CreateGroup("Variant " + map, true, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                schema.UseDefaultSchemaSettings = false; schema.IncludeInBuild = true;
                schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;
                foreach (var entry in new[] { (definition, "definition"), (binding, "source-scene"), (surface, "surface"), (minimap, "minimap") })
                    settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(entry.Item1), group).address = "map-variant-candidate/" + map + "/" + entry.Item2;
                var builder = ScriptableObject.CreateInstance<BuildScriptPackedMode>();
                AssetDatabase.CreateAsset(builder, temporary + "/PackedBuilder.asset");
                if (!settings.AddDataBuilder(builder, false)) throw new InvalidOperationException("Packed builder registration failed.");
                settings.ActivePlayerDataBuilderIndex = 0; AssetDatabase.SaveAssets();
                var input = new AddressablesDataBuilderInput(settings, new BuildPlayerOptions { target = ValidationTarget }) { RuntimeCatalogFilename = "catalog", RuntimeSettingsFilename = "settings.json" };
                ProjectConfigData.GenerateBuildLayout = true;
                ProjectConfigData.BuildLayoutReportFileFormat = ProjectConfigData.ReportFileFormat.JSON;
                var result = builder.BuildData<AddressablesPlayerBuildResult>(input);
                if (result == null || !string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result?.Error ?? "Addressables build returned no result.");
                string layoutPath = OperationMapDenseCityCandidateRuntimeContentBuilder.SelectSingleGeneratedBuildLayoutPath(layoutPaths, ProjectConfigData.BuildReportFilePaths, File.Exists);
                var layout = BuildLayout.Open(layoutPath, readFullFile: true);
                string sourcePath = AssetDatabase.GUIDToAssetPath(AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(definition).NavigationMetadata.AuthoredSubSceneGuid);
                var assets = BuildLayoutHelpers.EnumerateAssets(layout).Select(a => a.AssetPath)
                    .Concat(BuildLayoutHelpers.EnumerateBundles(layout).SelectMany(b => b.Files).SelectMany(f => f.OtherAssets.Concat(f.Assets.SelectMany(a => a.InternalReferencedOtherAssets))).Select(a => a.AssetPath));
                if (assets.Any(p => p == sourcePath)) throw new InvalidOperationException("Packed binding includes source GameObject hierarchy: " + sourcePath);
                Directory.CreateDirectory(output); File.Copy(layoutPath, output + "/buildlayout.json", true);
                CopyTree(builtRuntimePath, output);
                string[] catalogs = Directory.GetFiles(output, "catalog.bin", SearchOption.AllDirectories);
                if (catalogs.Length != 1) throw new InvalidOperationException("Expected exactly one packed variant catalog.");
                outputTransaction.Commit();
                return catalogs[0];
            }
            finally
            {
                if (Directory.Exists(shared)) Directory.Delete(shared, true);
                if (hadShared) Directory.Move(backup, shared);
                if (AssetDatabase.IsValidFolder(temporary)) AssetDatabase.DeleteAsset(temporary);
                productionSettings.Rollback(); renderingSettings.Rollback();
                ProjectConfigData.GenerateBuildLayout = layoutEnabled; ProjectConfigData.BuildLayoutReportFileFormat = layoutFormat;
                ProjectConfigData.ClearBuildReportFilePaths(); foreach (string path in layoutPaths) ProjectConfigData.AddBuildReportFilePath(path);
                if (DirectoryHash(shared) != sharedHash) throw new InvalidOperationException("Shared production Addressables output changed.");
            }
        }
        private sealed class PreparedLightingEnvironment
        {
            private readonly bool fog=RenderSettings.fog;
            private readonly FogMode fogMode=RenderSettings.fogMode;
            private readonly Color fogColor=RenderSettings.fogColor, sky=RenderSettings.ambientSkyColor,
                equator=RenderSettings.ambientEquatorColor, ground=RenderSettings.ambientGroundColor, ambient=RenderSettings.ambientLight;
            private readonly float density=RenderSettings.fogDensity, start=RenderSettings.fogStartDistance,
                end=RenderSettings.fogEndDistance, intensity=RenderSettings.ambientIntensity,
                reflectionIntensity=RenderSettings.reflectionIntensity, haloStrength=RenderSettings.haloStrength,
                flareStrength=RenderSettings.flareStrength, flareFadeSpeed=RenderSettings.flareFadeSpeed;
            private readonly UnityEngine.Rendering.AmbientMode ambientMode=RenderSettings.ambientMode;
            private readonly UnityEngine.Rendering.DefaultReflectionMode reflectionMode=RenderSettings.defaultReflectionMode;
            private readonly UnityEngine.Rendering.SphericalHarmonicsL2 probe=RenderSettings.ambientProbe;
            private readonly Material skybox=RenderSettings.skybox;
            private readonly Texture reflection=RenderSettings.customReflectionTexture;
            private readonly int reflectionBounces=RenderSettings.reflectionBounces, reflectionResolution=RenderSettings.defaultReflectionResolution;
            public void Apply()
            {
                RenderSettings.fog=fog; RenderSettings.fogMode=fogMode; RenderSettings.fogColor=fogColor;
                RenderSettings.fogDensity=density; RenderSettings.fogStartDistance=start; RenderSettings.fogEndDistance=end;
                RenderSettings.ambientMode=ambientMode; RenderSettings.ambientSkyColor=sky; RenderSettings.ambientEquatorColor=equator;
                RenderSettings.ambientGroundColor=ground; RenderSettings.ambientLight=ambient; RenderSettings.ambientIntensity=intensity;
                RenderSettings.skybox=skybox; RenderSettings.ambientProbe=probe;
                RenderSettings.defaultReflectionMode=reflectionMode; RenderSettings.customReflectionTexture=reflection;
                RenderSettings.defaultReflectionResolution=reflectionResolution; RenderSettings.reflectionIntensity=reflectionIntensity;
                RenderSettings.reflectionBounces=reflectionBounces; RenderSettings.haloStrength=haloStrength;
                RenderSettings.flareStrength=flareStrength; RenderSettings.flareFadeSpeed=flareFadeSpeed;
            }
        }
        private sealed class PreparedContentOutputTransaction : IDisposable
        {
            private readonly string output, backup;
            private readonly bool hadOutput;
            private bool committed;
            public PreparedContentOutputTransaction(string map, string part=null)
            {
                if(!MapVariantPreparedCandidateBuilder.MediumMaps.Contains(map) && map!="Frontier" && map!="ExistingDenseCity")
                    throw new ArgumentOutOfRangeException(nameof(map));
                if(part!=null && part!="Addressables") throw new ArgumentOutOfRangeException(nameof(part));
                output=Path.GetFullPath("Library/MapVariantPreparedContent/"+map+(part==null ? "" : "/"+part));
                backup=Path.GetFullPath("Library/MapVariantPreparedContentTransactions/Content-"+Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                hadOutput=Directory.Exists(output);
                if(hadOutput) Directory.Move(output,backup);
                Directory.CreateDirectory(output);
            }
            public void Commit() => committed=true;
            public void Dispose()
            {
                if(committed) { if(hadOutput && Directory.Exists(backup)) Directory.Delete(backup,true); }
                else
                {
                    if(Directory.Exists(output)) Directory.Delete(output,true);
                    if(hadOutput) Directory.Move(backup,output);
                }
            }
        }
        private static string DirectoryHash(string path) => !Directory.Exists(path) ? "Absent" : MapVariantPreparationSchema.Hash(string.Join("\n", Directory.GetFiles(path, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal).Select(p => Path.GetRelativePath(path,p) + " " + MapVariantPreparationInventory.FileHash(p))));
        private static void CopyTree(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, dir)));
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(file, target, true);
            }
        }
        private static void Set(object obj, string field, object value)
        {
            if (obj == null) throw new InvalidOperationException("Missing target for serialized field " + field);
            var info = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null) throw new InvalidOperationException("Missing serialized field " + obj.GetType().FullName + "." + field);
            info.SetValue(obj,value);
        }
        private static void CreateBinding(Scene binding, Scene source, PreparedCandidateOutput o,
            OperationMapDefinition definition, GridAuthoringConfig grid, MapSurfaceDataAsset surface)
        {
            var root = new GameObject("RuntimeMapBindings");
            var viewRoot = new GameObject("OperationMapSceneView"); var view = viewRoot.AddComponent<OperationMapSceneView>();
            Transform Child(string name) { var child = new GameObject(name); child.transform.SetParent(root.transform, false); return child.transform; }
            var decorations = Child("Decorations"); var meshBaker = decorations.gameObject.AddComponent<CombinedMeshBaker>();
            var buildings = Child("Buildings"); var vehicles = Child("Vehicles");
            var surfaceAuthoring = Child("Surface").gameObject.AddComponent<MapSurfaceAuthoring>();
            Set(surfaceAuthoring, "bakedSurfaceData", surface); Set(surfaceAuthoring, "gridConfig", grid);
            var sub = Child("SubScene").gameObject.AddComponent<SubScene>();
            // A valid SceneGUID selects Editor SubScene ownership in the existing loader.
            // Keep it empty so the loader owns the packed scene from navigation metadata.
            sub.SceneAsset = null; sub.AutoLoadScene = false; sub.enabled = false;
            Transform lighting = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Lighting");
            var copy = UnityEngine.Object.Instantiate(lighting.gameObject, root.transform, false); copy.name = "Lighting"; copy.SetActive(true);
            RenderSettings.sun = copy.GetComponentInChildren<Light>();
            Set(view, "operationMapId", o.mapId); Set(view, "definition", definition); Set(view, "mapRoot", root.transform);
            Set(view, "decorationCombinedMeshBaker", meshBaker); Set(view, "decorationRoot", decorations);
            Set(view, "buildingAuthoringRoot", buildings); Set(view, "vehicleAuthoringRoot", vehicles);
            Set(view, "mapSurfaceAuthoring", surfaceAuthoring); Set(view, "gridAuthoringConfig", grid); Set(view, "mapSubScene", sub);
            Set(view, "canonicalPresentationMode", OperationMapCanonicalPresentationMode.EntityScene);
            Set(view, "presentationSourceSceneGuid", o.sceneGuid); Set(view, "presentationSourceScenePath", o.scenePath);
        }
        private static void CreateMinimap(Scene source, PreparedCandidateOutput o, string path)
        {
            var go = new GameObject("MinimapCapture", typeof(Camera)); SceneManager.MoveGameObjectToScene(go, source);
            var cam = go.GetComponent<Camera>(); Vector2 center = o.runtimePlayableMin + o.playableSize * .5f;
            cam.transform.SetPositionAndRotation(new Vector3(center.x, 700, center.y), Quaternion.Euler(90,0,0));
            cam.orthographic = true; cam.orthographicSize = o.playableSize.y * .5f; cam.farClipPlane = 4000;
            int width = Mathf.RoundToInt(800 * o.playableSize.x / o.playableSize.y);
            var target = new RenderTexture(width, 800, 24); var pixels = new Texture2D(width,800,TextureFormat.RGB24,false);
            var old = RenderTexture.active;
            try { cam.targetTexture = target; cam.Render(); RenderTexture.active = target; pixels.ReadPixels(new Rect(0,0,width,800),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG()); }
            finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels); }
        }
        [Serializable] private sealed class VariantRuntimeContentReport
        {
            public string unitFixtureSceneGuid;
            public string variant, mapId, semanticHash, target, entitySceneGuid, entityContentPath, entityCatalogPath, entityCatalogHash;
            public string definitionPath, definitionGuid, bindingPath, bindingGuid, status;
            public string addressablesCatalogPath, addressablesCatalogHash, definitionAddress, entityFileSetHash, definitionHash, bindingHash;
            public long entityContentBytes;
        }
    }
}
