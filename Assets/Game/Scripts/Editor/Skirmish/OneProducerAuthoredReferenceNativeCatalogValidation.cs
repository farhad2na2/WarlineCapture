using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Configs;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Skirmish
{
    public static class OneProducerAuthoredReferenceNativeCatalogValidation
    {
        const string PackageAssetPath = "Assets/Game/Resources/OneProducerAuthoredReferencePackage.asset";
        const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        const string MatchBuildingCatalogGuid = "b2010000000000000000000000000004";
        const string MatchBuildingCatalogPath = "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset";
        const string SatelliteDishPrefabPath = "Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab";

        public static void RunFocusedValidation()
        {
            var expansion = SkirmishExpansionCatalogConfig.Load();
            if (expansion == null || !expansion.TryGetDefinition("S002", out var definition))
                throw new InvalidOperationException("Current authored S002 definition is unavailable");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
                throw new InvalidOperationException("Current authored S002 setup matrix is unavailable: " + matrixError);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard, 104731, manifest, matrix, out var setup, out var reasons) ||
                setup.SetupHash != 3657809016u)
                throw new InvalidOperationException("Original current S002 setup did not compile: " + string.Join(";", reasons.Select(x => x.Code + ":" + x.Field)));

            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            var baseline = preset != null ? preset.buildingPlacement : null;
            string sceneDiskPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                MatchScenePath.Substring("Assets/".Length)));
            string sceneText = File.ReadAllText(sceneDiskPath);
            string sceneBinding = @"(?m)^\s*buildingPlacementConfig:\s*\{fileID: 11400000, guid: " +
                MatchBuildingCatalogGuid + @", type: 2\}\s*$";
            if (Regex.Matches(sceneText, sceneBinding).Count != 1 ||
                AssetDatabase.GUIDToAssetPath(MatchBuildingCatalogGuid) != MatchBuildingCatalogPath)
                throw new InvalidOperationException("Exact existing Match.unity building catalog binding is required");
            var sceneCatalog = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(MatchBuildingCatalogPath);
            var satelliteDish = AssetDatabase.LoadAssetAtPath<GameObject>(SatelliteDishPrefabPath);
            if (sceneCatalog == null || sceneCatalog.UnitPrefabRegistryConfig == null || satelliteDish == null ||
                sceneCatalog.Spawnables == null || !sceneCatalog.Spawnables.Any(x => ReferenceEquals(x, satelliteDish)))
                throw new InvalidOperationException("Existing Match scene catalog must contain its serialized Satellite Dish prefab");
            var package = AssetDatabase.LoadAssetAtPath<OneProducerAuthoredReferencePackageConfig>(PackageAssetPath);
            if (baseline == null || package == null)
                throw new InvalidOperationException("Exact current baseline, scene catalog and opt-in package assets are required");
            bool packageDirtyBefore = EditorUtility.IsDirty(package);
            var packageData = package.ReadAndValidatePackage();
            using (var catalog = SkirmishProductionNativeReferenceBinding.Create(setup, baseline, sceneCatalog, null, package))
            {
                if (!catalog.TryRole(packageData.unit.key, out var overlay) ||
                    overlay.RoleKind != SkirmishRoleKind.Rifle || overlay.Producer != SkirmishProducerKind.Barracks ||
                    overlay.SquadMembers != packageData.quantity)
                    throw new InvalidOperationException("Native catalog did not retain the exact packaged compiled role");
            }

            var originalOverlays = setup.RoleOverlays;
            try
            {
                var rifleOverlays = originalOverlays.Where(x =>
                    x.RoleKind == SkirmishRoleKind.Rifle && x.Producer == SkirmishProducerKind.Barracks).ToArray();
                if (rifleOverlays.Length != 1)
                    throw new InvalidOperationException("Native control requires one original Rifle/Barracks setup overlay");
                var withoutRifle = originalOverlays.Where(x =>
                    x.RoleKind != SkirmishRoleKind.Rifle || x.Producer != SkirmishProducerKind.Barracks).ToArray();
                setup.RoleOverlays = withoutRifle;
                if (!RejectsPackageCreate(setup, baseline, sceneCatalog, package))
                    throw new InvalidOperationException("Native package opt-in accepted an absent Rifle/Barracks overlay");

                setup.RoleOverlays = originalOverlays.Concat(rifleOverlays).ToArray();
                if (!RejectsPackageCreate(setup, baseline, sceneCatalog, package))
                    throw new InvalidOperationException("Native package opt-in accepted duplicate Rifle/Barracks overlays");

                var filtered = (SkirmishRoleOverlay[])originalOverlays.Clone();
                int rifleIndex = Array.FindIndex(filtered, x =>
                    x.RoleKind == SkirmishRoleKind.Rifle && x.Producer == SkirmishProducerKind.Barracks);
                var filteredRifle = filtered[rifleIndex];
                filteredRifle.RoleId = "__unavailable_native_control_role__";
                filtered[rifleIndex] = filteredRifle;
                setup.RoleOverlays = filtered;
                if (!RejectsPackageCreate(setup, baseline, sceneCatalog, package))
                    throw new InvalidOperationException("Native package opt-in accepted a profile-filtered Rifle role");
            }
            finally { setup.RoleOverlays = originalOverlays; }

            var mismatch = UnityEngine.Object.Instantiate(package);
            try
            {
                var serialized = new SerializedObject(mismatch);
                var unitReference = serialized.FindProperty("unitPrefabReference");
                if (unitReference == null) throw new InvalidOperationException("Serialized native unit reference field missing");
                unitReference.objectReferenceValue = packageData.transport == null
                    ? null
                    : AssetDatabase.LoadAssetAtPath<GameObject>(packageData.transport.path);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                bool rejected = false;
                try { using var ignored = SkirmishProductionNativeReferenceBinding.Create(setup, baseline,
                    sceneCatalog, null, mismatch); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new InvalidOperationException("Native catalog accepted a mismatched serialized unit reference");
            }
            finally { UnityEngine.Object.DestroyImmediate(mismatch); }

            if (EditorUtility.IsDirty(package) != packageDirtyBefore)
                throw new InvalidOperationException("Native package reference check dirtied its authored package asset");

            Debug.Log("[OneProducerAuthoredReferenceNativeCatalog] result=Passed cases=5 scope=NativeCatalogReferenceBindingOnly match_scene_catalog_binding_verified=true match_scene_edited=false package_scope=GenericAuthoredReferenceOnly source_pins_verified=false S002_membership_unclaimed=true source_fit_verified=false mission_ready=false");
        }

        static bool RejectsPackageCreate(SkirmishResolvedSetup setup,
            BuildingPlacementSystemConfig baseline, BuildingPlacementSystemConfig scene,
            OneProducerAuthoredReferencePackageConfig package)
        {
            try
            {
                using var ignored = SkirmishProductionNativeReferenceBinding.Create(setup, baseline, scene, null, package);
                return false;
            }
            catch (InvalidOperationException) { return true; }
            catch (ArgumentException) { return true; }
        }
    }
}
