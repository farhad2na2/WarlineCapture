using System;
using System.Collections.Generic;
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
        public const string SourceScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
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

        [MenuItem("Game/Operation Maps/Configure Local Addressables Groups")]
        public static void Run()
        {
            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
                throw new InvalidOperationException("Addressables settings are required.");

            AddressableAssetGroup catalog = EnsureGroup(settings, CatalogGroupName, false);
            EnsureGroup(settings, SharedGroupName, false);
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
            for (int index = 0; index < manifest.Chunks.Count; index++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[index];
                string partitionLabel = BuildPartitionLabel(chunk, manifest.ChunkSize);
                SetOperationMapLabels(
                    settings,
                    MoveEntry(
                        settings,
                        presentation,
                        chunk.ScenePath,
                        AddressPrefix + "presentation/" + chunk.ChunkId),
                    PresentationRoleLabel,
                    partitionLabel);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[OperationMapAddressablesLayoutBuilder] Configured one-map local group topology.");
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
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;
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
