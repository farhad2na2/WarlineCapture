using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// W34: builds building.airport_small as a prefab variant of Building_Airport. It reuses the
    /// Airport's meshes and materials through transform overrides only (no new binary asset):
    /// a 120x40-cell footprint, a runway shortened to about 100 m, parking spawns inside the
    /// footprint, a destroyed-visual variant and a config that produces Jet_01 and Jet_02 only.
    /// Idempotent: every value is derived from the unchanged base assets, and existing outputs are
    /// updated in place, so a second run writes the same bytes. Building_Airport, its destroyed
    /// visual and its config are never written.
    /// </summary>
    public static class SmallAirportPrefabBuilder
    {
        public const string Marker = "[SmallAirportPrefabBuilder]";
        public const string BasePrefabPath = "Assets/Game/Prefabs/Buildings/Building_Airport.prefab";
        public const string PrefabPath = "Assets/Game/Prefabs/Buildings/Building_Airport_Small.prefab";
        public const string BaseDestroyedPath = "Assets/Game/Prefabs/Buildings/Destroyed/Building_Airport_Destroyed.prefab";
        public const string DestroyedPath = "Assets/Game/Prefabs/Buildings/Destroyed/Building_Airport_Small_Destroyed.prefab";
        public const string BaseConfigPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Airport_Config.asset";
        public const string ConfigPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_AirportSmall_Config.asset";
        public const string DisplayName = "Small Airport";
        public const string Description = "Short airstrip for deploying fighter and strike jets.";
        public static readonly Vector2Int Footprint = new(120, 40);
        public static readonly string[] Productions = { "Unit_Veh_Jet_01", "Unit_Veh_Jet_02" };

        // Model extents sit just inside the footprint, so EnclosingCells rounds to exactly 120x40.
        private const float HalfWidth = 59.75f;
        private const float HalfDepth = 19.75f;
        private const float TowerMargin = 1f;
        private const float ParkingMargin = 3f;

        public static void Build()
        {
            try
            {
                string report = BuildAll();
                Debug.Log(Marker + " result=Passed " + report);
            }
            catch (Exception error)
            {
                Debug.LogError(Marker + " result=Failed\n" + error);
                throw;
            }
        }

        public static string BuildAll()
        {
            string[] protectedPaths = { BasePrefabPath, BaseDestroyedPath, BaseConfigPath };
            var before = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string path in protectedPaths) before[path] = Hash(path);

            var basePrefab = Require<GameObject>(BasePrefabPath);
            var baseDestroyed = Require<GameObject>(BaseDestroyedPath);
            var baseConfig = Require<BuildingDefinitionAuthoringConfig>(BaseConfigPath);
            bool prefabExisted = File.Exists(PrefabPath);
            bool destroyedExisted = File.Exists(DestroyedPath);
            bool configExisted = File.Exists(ConfigPath);

            GameObject root = prefabExisted
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            Layout layout;
            try
            {
                layout = ApplyLayout(root, basePrefab);
                GameObject destroyed = BuildDestroyedVariant(baseDestroyed, layout.ModelLocalPosition, destroyedExisted);
                BuildingDefinitionAuthoringConfig config = BuildConfig(baseConfig, destroyed);

                var authoring = root.GetComponent<BuildingDefinitionAuthoring>();
                if (authoring == null) throw new InvalidOperationException("Building_Airport has no BuildingDefinitionAuthoring.");
                authoring.ConfigureForEditor(config);
                var data = new SerializedObject(authoring);
                data.FindProperty("footprintCells").vector2IntValue = Footprint;
                data.ApplyModifiedPropertiesWithoutUndo();
                root.name = Path.GetFileNameWithoutExtension(PrefabPath);
                if (!prefabExisted) RecordInstanceOverrides(root, authoring);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save " + PrefabPath);
            }
            finally
            {
                if (prefabExisted) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (string path in protectedPaths)
                if (Hash(path) != before[path]) throw new InvalidOperationException("Protected asset changed: " + path);

            Verification check = Verify();
            return "footprint=" + Footprint.x + "x" + Footprint.y +
                   " runwayLengthM=" + F(check.RunwayLength) + " runwayScaleX=" + F(layout.RunwayScale) +
                   " modelBounds=" + F(check.Bounds.size.x) + "x" + F(check.Bounds.size.z) +
                   " runtimeFootprint=" + check.RuntimeFootprint.x + "x" + check.RuntimeFootprint.y +
                   " spawnsInside=" + check.SpawnsInside + "/" + check.Spawns +
                   " parkingRows=" + layout.ParkingRows +
                   " productions=" + string.Join(",", check.Productions) +
                   " variantOf=" + Path.GetFileNameWithoutExtension(BasePrefabPath) +
                   " destroyedVariantOf=" + Path.GetFileNameWithoutExtension(BaseDestroyedPath) +
                   " created=prefab:" + (prefabExisted ? 0 : 1) + ",destroyed:" + (destroyedExisted ? 0 : 1) +
                   ",config:" + (configExisted ? 0 : 1) +
                   " baseUnchanged=1 newBinaryDependencies=" + check.NewBinaryDependencies;
        }

        private struct Layout
        {
            public float RunwayScale;
            public Vector3 ModelLocalPosition;
            public int ParkingRows;
        }

        private static Layout ApplyLayout(GameObject root, GameObject basePrefab)
        {
            if (root.transform.localPosition != Vector3.zero || root.transform.localRotation != Quaternion.identity ||
                root.transform.localScale != Vector3.one)
                throw new InvalidOperationException("Airport root transform must be identity.");

            Transform runway = Child(root.transform, "Runway");
            Transform model = Child(root.transform, "Model");
            Transform tower = Child(model, "SM_Bld_ControlTower_01");
            Transform baseRunway = Child(basePrefab.transform, "Runway");
            Transform baseModel = Child(basePrefab.transform, "Model");

            // Reset every overridden transform to the base, so the result never depends on a prior run.
            runway.localPosition = baseRunway.localPosition;
            runway.localRotation = baseRunway.localRotation;
            runway.localScale = baseRunway.localScale;
            var barriers = new List<(Transform live, Vector3 baseScale)>();
            for (int i = 0; i < runway.childCount; i++)
            {
                Transform child = runway.GetChild(i);
                Transform baseChild = Child(baseRunway, child.name);
                child.localPosition = baseChild.localPosition;
                child.localRotation = baseChild.localRotation;
                child.localScale = baseChild.localScale;
                if (child.GetComponent<Renderer>() != null) barriers.Add((child, baseChild.localScale));
            }
            model.localPosition = baseModel.localPosition;
            model.localRotation = baseModel.localRotation;

            // Shorten the runway along its axis (x) until its model is just inside 120 cells. Barrier
            // props keep their world size; Runway_Start/End scale with the strip.
            float low = 0.05f, high = 1f;
            for (int i = 0; i < 60; i++)
            {
                float mid = 0.5f * (low + high);
                SetRunwayScale(runway, baseRunway.localScale, barriers, mid);
                if (Measure(root.transform, runway).size.x > 2f * HalfWidth) high = mid; else low = mid;
            }
            SetRunwayScale(runway, baseRunway.localScale, barriers, low);
            Bounds strip = Measure(root.transform, runway);
            runway.localPosition += new Vector3(-strip.center.x, 0f, HalfDepth - strip.max.z);
            strip = Measure(root.transform, runway);

            // Control tower at the west end of the apron band below the strip.
            Bounds towerBounds = Measure(root.transform, tower);
            model.localPosition += new Vector3(-HalfWidth + TowerMargin - towerBounds.min.x, 0f, -HalfDepth - towerBounds.min.z);
            towerBounds = Measure(root.transform, tower);
            if (towerBounds.max.z > strip.min.z - 0.5f)
                throw new InvalidOperationException("Tower (depth " + F(towerBounds.size.z) + ") and runway (width " +
                    F(strip.size.z) + ") do not fit a " + Footprint.y + "-cell footprint.");

            if (!BuildingModelBounds.TryMeasure(root, out Bounds all) || BuildingModelBounds.EnclosingCells(all) != Footprint)
                throw new InvalidOperationException("Model bounds " + F(all.size.x) + "x" + F(all.size.z) +
                    " do not enclose to " + Footprint.x + "x" + Footprint.y + ".");

            // Parking spawns on the apron, east of the tower, inside the footprint.
            var spawns = new List<(Transform live, Transform baseSpawn, int index)>();
            for (int i = 0; i < model.childCount; i++)
            {
                Transform child = model.GetChild(i);
                if (!child.name.StartsWith("Spawn_", StringComparison.Ordinal) ||
                    !int.TryParse(child.name.Substring(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    continue;
                spawns.Add((child, Child(baseModel, child.name), index));
            }
            spawns.Sort((a, b) => a.index.CompareTo(b.index));
            float xMin = towerBounds.max.x + ParkingMargin, xMax = all.max.x - ParkingMargin;
            float zMin = all.min.z + ParkingMargin, zMax = strip.min.z - ParkingMargin;
            if (spawns.Count == 0 || xMax <= xMin || zMax <= zMin)
                throw new InvalidOperationException("No apron left for parking spawns.");
            int rows = zMax - zMin >= 12f && spawns.Count > 1 ? 2 : 1;
            int columns = (spawns.Count + rows - 1) / rows;
            for (int i = 0; i < spawns.Count; i++)
            {
                int row = i / columns, column = i % columns;
                float x = xMin + (column + 0.5f) * (xMax - xMin) / columns;
                float z = rows == 1 ? 0.5f * (zMin + zMax) : zMax - (row * 0.5f + 0.25f) * (zMax - zMin);
                Vector3 local = model.InverseTransformPoint(root.transform.TransformPoint(new Vector3(x, 0f, z)));
                spawns[i].live.localPosition = new Vector3(local.x, spawns[i].baseSpawn.localPosition.y, local.z);
                spawns[i].live.localRotation = spawns[i].baseSpawn.localRotation;
                spawns[i].live.localScale = spawns[i].baseSpawn.localScale;
            }

            return new Layout { RunwayScale = low, ModelLocalPosition = model.localPosition, ParkingRows = rows };
        }

        private static void SetRunwayScale(Transform runway, Vector3 baseScale,
            List<(Transform live, Vector3 baseScale)> barriers, float scaleX)
        {
            runway.localScale = new Vector3(baseScale.x * scaleX, baseScale.y, baseScale.z);
            foreach (var barrier in barriers)
                barrier.live.localScale = new Vector3(barrier.baseScale.x / scaleX, barrier.baseScale.y, barrier.baseScale.z);
        }

        private static GameObject BuildDestroyedVariant(GameObject baseDestroyed, Vector3 modelLocalPosition, bool existed)
        {
            GameObject root = existed
                ? PrefabUtility.LoadPrefabContents(DestroyedPath)
                : (GameObject)PrefabUtility.InstantiatePrefab(baseDestroyed);
            try
            {
                // The destroyed tower replaces the live tower at the same prefab-local position.
                Transform tower = Child(root.transform, "Destroyed");
                Transform baseTower = Child(baseDestroyed.transform, "Destroyed");
                Transform baseModel = Child(Require<GameObject>(BasePrefabPath).transform, "Model");
                tower.localPosition = baseTower.localPosition + (modelLocalPosition - baseModel.localPosition);
                tower.localRotation = baseTower.localRotation;
                tower.localScale = baseTower.localScale;
                root.name = Path.GetFileNameWithoutExtension(DestroyedPath);
                if (!existed) PrefabUtility.RecordPrefabInstancePropertyModifications(tower);
                PrefabUtility.SaveAsPrefabAsset(root, DestroyedPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save " + DestroyedPath);
            }
            finally
            {
                if (existed) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
            return Require<GameObject>(DestroyedPath);
        }

        private static BuildingDefinitionAuthoringConfig BuildConfig(BuildingDefinitionAuthoringConfig baseConfig, GameObject destroyed)
        {
            var config = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(ConfigPath);
            bool created = config == null;
            if (created) config = (BuildingDefinitionAuthoringConfig)ScriptableObject.CreateInstance(baseConfig.GetType());
            // Same building values as the Airport (health, cost, portraits); only identity,
            // destroyed visual and the production list differ.
            EditorUtility.CopySerialized(baseConfig, config);
            config.name = Path.GetFileNameWithoutExtension(ConfigPath);
            var data = new SerializedObject(config);
            data.FindProperty("displayName").stringValue = DisplayName;
            data.FindProperty("description").stringValue = Description;
            data.FindProperty("destroyedVisualPrefab").objectReferenceValue = destroyed;
            SerializedProperty productions = data.FindProperty("productions");
            for (int i = productions.arraySize - 1; i >= 0; i--)
            {
                var unit = productions.GetArrayElementAtIndex(i).FindPropertyRelative("spawnUnitPrefab").objectReferenceValue;
                if (unit == null || Array.IndexOf(Productions, unit.name) < 0)
                    productions.DeleteArrayElementAtIndex(i);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            if (created) AssetDatabase.CreateAsset(config, ConfigPath);
            else EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(ConfigPath);
        }

        private static void RecordInstanceOverrides(GameObject root, Component authoring)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
        }

        public struct Verification
        {
            public Bounds Bounds;
            public Vector2Int RuntimeFootprint;
            public float RunwayLength;
            public int Spawns, SpawnsInside, NewBinaryDependencies;
            public string[] Productions;
        }

        /// <summary>Checks the saved assets; throws on any mismatch.</summary>
        public static Verification Verify()
        {
            var prefab = Require<GameObject>(PrefabPath);
            var destroyed = Require<GameObject>(DestroyedPath);
            var config = Require<BuildingDefinitionAuthoringConfig>(ConfigPath);
            RequireVariantOf(prefab, BasePrefabPath);
            RequireVariantOf(destroyed, BaseDestroyedPath);

            var result = new Verification();
            if (!BuildingModelBounds.TryMeasure(prefab, out result.Bounds) ||
                BuildingModelBounds.EnclosingCells(result.Bounds) != Footprint)
                throw new InvalidOperationException("Saved model does not enclose to the footprint.");
            var authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
            if (authoring.AuthoringConfig != config || authoring.ConfiguredFootprintCells != Footprint)
                throw new InvalidOperationException("Saved authoring config or footprint is wrong.");
            if (config.DisplayName != DisplayName || config.DestroyedVisualPrefab != destroyed)
                throw new InvalidOperationException("Small airport config identity or destroyed visual is wrong.");

            var names = new List<string>();
            foreach (var entry in config.Productions) names.Add(entry?.SpawnUnitPrefab != null ? entry.SpawnUnitPrefab.name : "<null>");
            if (names.Count != Productions.Length || !names.TrueForAll(n => Array.IndexOf(Productions, n) >= 0))
                throw new InvalidOperationException("Productions must be exactly " + string.Join(",", Productions) + ": " + string.Join(",", names));
            result.Productions = names.ToArray();

            var definitions = new BuildingDefinitionPrefabSystemHelper();
            definitions.ConfigureAuthoringMetadataResolvers(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var definition = definitions.CreateRuntimeBuildingDefinition(prefab, prefab.name, "", Vector2Int.one, 1, new BuildingRunwaySystem());
            result.RuntimeFootprint = definition.FootprintCells;
            if (definition.FootprintCells != Footprint || !definition.HasRunway)
                throw new InvalidOperationException("Runtime definition footprint " + definition.FootprintCells + " runway=" + definition.HasRunway);

            Transform runway = Child(prefab.transform, "Runway");
            Vector3 start = Child(runway, "Runway_Start").position, end = Child(runway, "Runway_End").position;
            result.RunwayLength = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
            if (result.RunwayLength < 80f || result.RunwayLength > 120f)
                throw new InvalidOperationException("Runway length " + F(result.RunwayLength) + " m is not about 100 m.");

            foreach (Transform child in Child(prefab.transform, "Model"))
            {
                if (!child.name.StartsWith("Spawn_", StringComparison.Ordinal)) continue;
                result.Spawns++;
                Vector3 p = prefab.transform.InverseTransformPoint(child.position);
                if (p.x > result.Bounds.min.x + 1f && p.x < result.Bounds.max.x - 1f &&
                    p.z > result.Bounds.min.z + 1f && p.z < result.Bounds.max.z - 1f) result.SpawnsInside++;
            }
            if (result.Spawns == 0 || result.SpawnsInside != result.Spawns)
                throw new InvalidOperationException("Parking spawns outside the footprint: " + result.SpawnsInside + "/" + result.Spawns);

            // No new binary: every dependency of the new assets is an existing base dependency or one of them.
            var allowed = new HashSet<string>(StringComparer.Ordinal) { PrefabPath, DestroyedPath, ConfigPath };
            foreach (string path in AssetDatabase.GetDependencies(new[] { BasePrefabPath, BaseDestroyedPath, BaseConfigPath }, true))
                allowed.Add(path);
            foreach (string path in AssetDatabase.GetDependencies(new[] { PrefabPath, DestroyedPath, ConfigPath }, true))
                if (!allowed.Contains(path)) result.NewBinaryDependencies++;
            if (result.NewBinaryDependencies != 0)
                throw new InvalidOperationException("The small airport depends on assets the Airport does not use.");
            return result;
        }

        private static void RequireVariantOf(GameObject asset, string basePath)
        {
            if (PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.Variant)
                throw new InvalidOperationException(asset.name + " is not a prefab variant.");
            var source = PrefabUtility.GetCorrespondingObjectFromSource(asset);
            if (source == null || AssetDatabase.GetAssetPath(source) != basePath)
                throw new InvalidOperationException(asset.name + " is not a variant of " + basePath);
        }

        private static Bounds Measure(Transform root, Transform subtree)
        {
            bool found = false;
            Bounds bounds = default;
            foreach (var renderer in subtree.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!(renderer is MeshRenderer) || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
                Bounds local = filter.sharedMesh.bounds;
                Matrix4x4 matrix = root.worldToLocalMatrix * renderer.localToWorldMatrix;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(new Vector3(x == 0 ? local.min.x : local.max.x,
                        y == 0 ? local.min.y : local.max.y, z == 0 ? local.min.z : local.max.z));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!found) throw new InvalidOperationException("No mesh renderer under " + subtree.name);
            return bounds;
        }

        private static Transform Child(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            throw new InvalidOperationException("Missing child '" + name + "' under " + parent.name);
        }

        private static T Require<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset " + path);

        private static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
        }

        private static string F(float value) => value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
