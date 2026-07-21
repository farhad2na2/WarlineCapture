using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Rendering;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    public static class OperationMapAddressablesLayoutBuilder
    {
        public const string CatalogGroupName = "Operation Maps - Catalog";
        public const string SharedGroupName = "Operation Maps - Shared";
        public const string CoreGroupName =
            "Operation Map - Local - skirmish-desert-base-01 - Core";
        public const string PresentationGroupName =
            "Operation Map - Local - skirmish-desert-base-01 - Presentation";
        public const string CatalogPath =
            "Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset";
        public const string DefinitionPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";
        public const string AuthoringScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        public const string SourceScenePath = OperationMapRuntimeBindingSceneBuilder.OutputPath;
        public const string SourceSubScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity";
        public const string MapSurfacePath =
            "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        public const string ManifestPath =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";
        public const string BuildingPlacementsPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset";
        public const string VehiclePlacementsPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_VehiclePlacements.asset";
        public const string MinimapRasterPath = OperationMapMinimapRasterBaker.OutputPath;
        public const string AddressPrefix = "operation-map/opmap.skirmish.desert_base_01/";
        public const string PackLabel = "operation-map-pack-skirmish-desert-base-01";
        public const string OperationMapLabel = "operation-map";
        public const string LocalLabel = "operation-map-local";
        public const string DefinitionRoleLabel = "operation-map-role-definition";
        public const string SourceSceneRoleLabel = "operation-map-role-source-scene";
        public const string MetadataRoleLabel = "operation-map-role-metadata";
        public const string PresentationRoleLabel = "operation-map-role-presentation";
        public const string MinimapRasterRoleLabel = "operation-map-role-minimap-raster";
        public const string SharedDependencyRoleLabel = "operation-map-role-shared-dependency";
        public const string SharedShardLabelPrefix = "operation-map-shared-shard-";
        public const int SharedDependencyPartitionThreshold = 2;
        public const int SharedDependencyShardCount = 8;

        [MenuItem("Game/Operation Maps/Configure Local Addressables Groups")]
        public static void Run()
        {
            PrepareRuntimeDefinition();
            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
                throw new InvalidOperationException("Addressables settings are required.");

            settings.BuildRemoteCatalog = false;
            settings.DisableCatalogUpdateOnStartup = true;
            settings.UniqueBundleIds = false;

            AddressableAssetGroup catalog = EnsureGroup(settings, CatalogGroupName, false);
            AddressableAssetGroup shared = EnsureGroup(settings, SharedGroupName, true);
            AddressableAssetGroup core = EnsureGroup(settings, CoreGroupName, false);
            AddressableAssetGroup presentation = EnsureGroup(settings, PresentationGroupName, true);

            AddressableAssetEntry catalogEntry = MoveEntry(settings, catalog, CatalogPath, "operation-map/catalog");
            SetOperationMapLabels(settings, catalogEntry, MetadataRoleLabel, null);
            AddressableAssetEntry definitionEntry = MoveEntry(
                settings,
                catalog,
                DefinitionPath,
                AddressPrefix + "definition");
            SetOperationMapLabels(settings, definitionEntry, DefinitionRoleLabel, null);

            AddressableAssetEntry sourceSceneEntry = MoveEntry(
                settings,
                core,
                SourceScenePath,
                AddressPrefix + "source-scene");
            SetOperationMapLabels(
                settings,
                sourceSceneEntry,
                SourceSceneRoleLabel,
                null);
            string authoringSceneGuid = AssetDatabase.AssetPathToGUID(AuthoringScenePath);
            if (!string.IsNullOrEmpty(authoringSceneGuid))
                settings.RemoveAssetEntry(authoringSceneGuid, false);
            AddressableAssetEntry mapSurfaceEntry = MoveEntry(
                settings,
                core,
                MapSurfacePath,
                AddressPrefix + "map-surface");
            SetOperationMapLabels(
                settings,
                mapSurfaceEntry,
                MetadataRoleLabel,
                null);
            AddressableAssetEntry manifestEntry = MoveEntry(
                settings,
                core,
                ManifestPath,
                AddressPrefix + "static-manifest");
            SetOperationMapLabels(
                settings,
                manifestEntry,
                MetadataRoleLabel,
                null);
            AddressableAssetEntry buildingPlacementsEntry = MoveEntry(
                settings,
                core,
                BuildingPlacementsPath,
                AddressPrefix + "building-placements");
            SetOperationMapLabels(
                settings,
                buildingPlacementsEntry,
                MetadataRoleLabel,
                null);
            AddressableAssetEntry vehiclePlacementsEntry = MoveEntry(
                settings,
                core,
                VehiclePlacementsPath,
                AddressPrefix + "vehicle-placements");
            SetOperationMapLabels(
                settings,
                vehiclePlacementsEntry,
                MetadataRoleLabel,
                null);
            AddressableAssetEntry minimapRasterEntry = MoveEntry(
                settings,
                core,
                MinimapRasterPath,
                AddressPrefix + "minimap-raster");
            SetOperationMapLabels(
                settings,
                minimapRasterEntry,
                MinimapRasterRoleLabel,
                null);

            AssignDefinitionReferences(
                sourceSceneEntry,
                mapSurfaceEntry,
                manifestEntry,
                buildingPlacementsEntry,
                vehiclePlacementsEntry,
                minimapRasterEntry);

            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
            if (manifest == null || manifest.Chunks.Count == 0)
                throw new InvalidOperationException("Static presentation manifest is missing or empty.");
            var acceptedPresentationGuids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < manifest.Chunks.Count; index++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[index];
                acceptedPresentationGuids.Add(AssetDatabase.AssetPathToGUID(chunk.ScenePath));
                string partitionLabel = BuildPartitionLabel(chunk, manifest.ChunkSize);
                if (!OperationMapContentAddressContract.TryBuildPresentationChunkAddress(
                        manifest.OperationMapId,
                        chunk.ChunkId,
                        out string chunkAddress,
                        out string addressError))
                {
                    throw new InvalidOperationException(addressError);
                }
                SetOperationMapLabels(
                    settings,
                    MoveEntry(
                        settings,
                        presentation,
                        chunk.ScenePath,
                        chunkAddress),
                    PresentationRoleLabel,
                    partitionLabel);
            }

            AddressableAssetEntry[] stalePresentationEntries = presentation.entries
                .Where(entry => !acceptedPresentationGuids.Contains(entry.guid))
                .ToArray();
            for (int index = 0; index < stalePresentationEntries.Length; index++)
                settings.RemoveAssetEntry(stalePresentationEntries[index].guid, false);

            ConfigureSharedDependencies(settings, shared, manifest);

            AssetDatabase.SaveAssets();
            Debug.Log("[OperationMapAddressablesLayoutBuilder] Configured one-map local group topology.");
        }

        internal static void PrepareRuntimeDefinition()
        {
            OperationMapDefinition staged = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapCurrentStagedDefinitionBuilder.DefinitionPath);
            OperationMapDefinition runtime = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
            if (staged == null || runtime == null)
                throw new InvalidOperationException("Staged and catalog operation-map definitions are required.");

            string runtimeSceneGuid = AssetDatabase.AssetPathToGUID(SourceScenePath);
            if (string.IsNullOrEmpty(runtimeSceneGuid))
                throw new InvalidOperationException($"Runtime binding scene is missing: {SourceScenePath}");

            OperationMapNavigationMetadataConfig navigation = staged.NavigationMetadata;
            var serialized = new SerializedObject(runtime);
            SerializedProperty serializedNavigation = serialized.FindProperty("navigationMetadata");
            serializedNavigation.FindPropertyRelative("authoredSubSceneGuid").stringValue =
                navigation.AuthoredSubSceneGuid;
            serializedNavigation.FindPropertyRelative("gridAuthoringLocalId").longValue =
                navigation.GridAuthoringLocalId;
            serializedNavigation.FindPropertyRelative("staticGridBlockerCount").intValue =
                navigation.StaticGridBlockerCount;
            serializedNavigation.FindPropertyRelative("usesSurfaceMovementMetadata").boolValue =
                navigation.UsesSurfaceMovementMetadata;
            serializedNavigation.FindPropertyRelative("supportsDynamicBlockers").boolValue =
                navigation.SupportsDynamicBlockers;
            serializedNavigation.FindPropertyRelative("supportsDynamicOccupancy").boolValue =
                navigation.SupportsDynamicOccupancy;
            serialized.FindProperty("generatedMetadataHash").stringValue = staged.GeneratedMetadataHash;
            SetAssetReferenceGuid(serialized, "sourceSceneReference", runtimeSceneGuid);
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
                EditorUtility.SetDirty(runtime);
            AssetDatabase.SaveAssets();
        }

        internal static string[] CollectSharedDependencyPaths(
            AddressableAssetSettings settings,
            StaticMapPresentationManifest manifest)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            var usage = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            const string corePartition = "operation-map-core";
            string[] coreOwnerPaths =
            {
                SourceScenePath,
                MapSurfacePath,
                ManifestPath,
                BuildingPlacementsPath,
                VehiclePlacementsPath,
                MinimapRasterPath
            };
            for (int index = 0; index < coreOwnerPaths.Length; index++)
                AddDependencyUsage(usage, coreOwnerPaths[index], corePartition);
            for (int index = 0; index < manifest.Chunks.Count; index++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[index];
                string partition = BuildPartitionLabel(chunk, manifest.ChunkSize);
                AddDependencyUsage(usage, chunk.ScenePath, partition);
            }

            return usage
                .Where(pair => pair.Value.Count >= SharedDependencyPartitionThreshold)
                .Select(pair => pair.Key)
                .Where(path =>
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                    return !string.IsNullOrEmpty(guid) &&
                           (entry == null || string.Equals(
                               entry.parentGroup?.Name,
                               SharedGroupName,
                               StringComparison.Ordinal));
                })
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static void AddDependencyUsage(
            Dictionary<string, HashSet<string>> usage,
            string ownerPath,
            string partition)
        {
            string[] dependencies = AssetDatabase.GetDependencies(ownerPath, true);
            for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
            {
                string path = dependencies[dependencyIndex];
                if (!IsShareableDependencyPath(path))
                    continue;

                if (!usage.TryGetValue(path, out HashSet<string> partitions))
                {
                    partitions = new HashSet<string>(StringComparer.Ordinal);
                    usage.Add(path, partitions);
                }
                partitions.Add(partition);
            }
        }

        internal static string BuildSharedShardLabel(string assetPath, string guid)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(guid))
                throw new ArgumentException("Shared dependency path and GUID are required.");

            string extension = Path.GetExtension(assetPath).ToLowerInvariant();
            string kind = extension switch
            {
                ".png" or ".jpg" or ".jpeg" or ".tga" or ".psd" or ".exr" or ".tif" or ".tiff" => "texture",
                ".mat" => "material",
                ".shader" or ".compute" or ".shadervariants" => "shader",
                ".fbx" or ".obj" or ".blend" => "mesh",
                ".prefab" => "prefab",
                _ => "other"
            };
            if (string.Equals(kind, "shader", StringComparison.Ordinal))
                return $"{SharedShardLabelPrefix}shader-00";

            int prefixLength = Math.Min(2, guid.Length);
            if (!int.TryParse(
                    guid.Substring(0, prefixLength),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out int hashPrefix))
                throw new InvalidOperationException($"Shared dependency GUID is invalid: {guid}");

            int shard = hashPrefix % SharedDependencyShardCount;
            return $"{SharedShardLabelPrefix}{kind}-{shard:D2}";
        }

        private static void ConfigureSharedDependencies(
            AddressableAssetSettings settings,
            AddressableAssetGroup shared,
            StaticMapPresentationManifest manifest)
        {
            string[] paths = CollectSharedDependencyPaths(settings, manifest);
            var acceptedGuids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < paths.Length; index++)
            {
                string path = paths[index];
                string guid = AssetDatabase.AssetPathToGUID(path);
                acceptedGuids.Add(guid);
                AddressableAssetEntry entry = MoveEntry(
                    settings,
                    shared,
                    path,
                    "operation-map/shared/" + guid);
                SetOperationMapLabels(
                    settings,
                    entry,
                    SharedDependencyRoleLabel,
                    BuildSharedShardLabel(path, guid));
            }

            AddressableAssetEntry[] stale = shared.entries
                .Where(entry =>
                    entry.labels.Contains(SharedDependencyRoleLabel) &&
                    !acceptedGuids.Contains(entry.guid))
                .ToArray();
            for (int index = 0; index < stale.Length; index++)
                settings.RemoveAssetEntry(stale[index].guid, false);

            HashSet<string> activeShardLabels = shared.entries
                .SelectMany(entry => entry.labels)
                .Where(label => label.StartsWith(SharedShardLabelPrefix, StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);
            string[] staleShardLabels = settings.GetLabels()
                .Where(label =>
                    label.StartsWith(SharedShardLabelPrefix, StringComparison.Ordinal) &&
                    !activeShardLabels.Contains(label))
                .ToArray();
            for (int index = 0; index < staleShardLabels.Length; index++)
                settings.RemoveLabel(staleShardLabels[index], false);
        }

        internal static bool IsShareableDependencyPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            bool isProjectAsset = path.StartsWith("Assets/", StringComparison.Ordinal);
            bool isRuntimePackageAsset = path.StartsWith("Packages/", StringComparison.Ordinal) &&
                                         path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) < 0;
            if (!isProjectAsset && !isRuntimePackageAsset)
                return false;
            if (isProjectAsset && StaticMapPresentationCanonicalSourceHash.IsGeneratedOutputPath(path))
                return false;

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (isRuntimePackageAsset &&
                extension is not (".mat" or ".shader" or ".compute" or ".shadervariants"))
                return false;

            return !string.Equals(extension, ".unity", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(extension, ".asmdef", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase);
        }

        private static void AssignDefinitionReferences(
            AddressableAssetEntry sourceScene,
            AddressableAssetEntry mapSurface,
            AddressableAssetEntry manifest,
            AddressableAssetEntry buildingPlacements,
            AddressableAssetEntry vehiclePlacements,
            AddressableAssetEntry minimapRaster)
        {
            OperationMapDefinition definition =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
            if (definition == null)
                throw new InvalidOperationException($"Operation-map definition is missing: {DefinitionPath}");

            SerializedObject serializedDefinition = new(definition);
            SetAssetReferenceGuid(serializedDefinition, "sourceSceneReference", sourceScene.guid);
            SetAssetReferenceGuid(serializedDefinition, "mapSurfaceDataReference", mapSurface.guid);
            SetAssetReferenceGuid(serializedDefinition, "staticPresentationManifestReference", manifest.guid);
            SetAssetReferenceGuid(serializedDefinition, "buildingPlacementsReference", buildingPlacements.guid);
            SetAssetReferenceGuid(serializedDefinition, "vehiclePlacementsReference", vehiclePlacements.guid);
            SetAssetReferenceGuid(serializedDefinition, "minimapRasterReference", minimapRaster.guid);
            if (serializedDefinition.ApplyModifiedPropertiesWithoutUndo())
                EditorUtility.SetDirty(definition);
        }

        private static void SetAssetReferenceGuid(
            SerializedObject serializedDefinition,
            string fieldName,
            string guid)
        {
            SerializedProperty reference = serializedDefinition.FindProperty(fieldName);
            SerializedProperty assetGuid = reference?.FindPropertyRelative("m_AssetGUID");
            if (assetGuid == null)
                throw new InvalidOperationException($"Operation-map definition field is missing: {fieldName}");

            assetGuid.stringValue = guid;
        }

        private static AddressableAssetGroup EnsureGroup(
            AddressableAssetSettings settings,
            string groupName,
            bool packTogetherByLabel)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName) ?? settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
            BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
                throw new InvalidOperationException($"Addressables group '{groupName}' requires a bundled schema.");

            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.UseDefaultSchemaSettings = false;
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            schema.UseAssetBundleCrc = true;
            schema.UseAssetBundleCrcForCachedBundles = true;
            // Pack-by-label identities can be long, while content-only hashes collide for equal empty bundles.
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;
            schema.BundleMode = packTogetherByLabel
                ? BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel
                : BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            return group;
        }

        private static AddressableAssetEntry MoveEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Addressable asset is missing: {assetPath}");

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null || entry.parentGroup != group)
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (!string.Equals(entry.address, address, StringComparison.Ordinal))
                entry.SetAddress(address, false);
            return entry;
        }

        private static void SetOperationMapLabels(
            AddressableAssetSettings settings,
            AddressableAssetEntry entry,
            string roleLabel,
            string partitionLabel)
        {
            List<string> existing = new(entry.labels);
            for (int index = 0; index < existing.Count; index++)
            {
                if (existing[index].StartsWith("operation-map", StringComparison.Ordinal))
                    entry.SetLabel(existing[index], false, false, false);
            }

            SetLabel(settings, entry, OperationMapLabel);
            SetLabel(settings, entry, LocalLabel);
            SetLabel(settings, entry, PackLabel);
            SetLabel(settings, entry, roleLabel);
            if (!string.IsNullOrEmpty(partitionLabel))
                SetLabel(settings, entry, partitionLabel);
        }

        private static void SetLabel(
            AddressableAssetSettings settings,
            AddressableAssetEntry entry,
            string label)
        {
            settings.AddLabel(label, false);
            entry.SetLabel(label, true, false, false);
        }

        private static string BuildPartitionLabel(
            StaticMapPresentationChunkEntry chunk,
            float chunkSize)
        {
            const int chunksPerAxis = 5;
            float regionSize = chunkSize * chunksPerAxis;
            int regionX = Mathf.FloorToInt(chunk.WorldBounds.center.x / regionSize);
            int regionZ = Mathf.FloorToInt(chunk.WorldBounds.center.z / regionSize);
            return $"operation-map-partition-skirmish-desert-base-01-region-{FormatCoordinate(regionX)}-{FormatCoordinate(regionZ)}";
        }

        private static string FormatCoordinate(int value)
        {
            return value >= 0 ? $"p{value:D3}" : $"n{Math.Abs(value):D3}";
        }
    }
}
