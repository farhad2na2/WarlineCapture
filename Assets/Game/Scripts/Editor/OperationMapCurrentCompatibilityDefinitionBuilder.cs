using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapCurrentCompatibilityDefinitionBuilder
    {
        public const string DefinitionPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";

        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string GridPath = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset";
        private const string SurfacePath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        private const string SubScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity";
        private const string ManifestPath =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";
        private const long WorldCameraLocalId = 1220593093;
        private const long StartTransformLocalId = 229045073;
        private const long EndTransformLocalId = 29742182;
        private const long Faction1TransformLocalId = 1851641272;
        private const long Faction2TransformLocalId = 684472870;
        private const long GridAuthoringLocalId = 146043441;
        private const string OperationMapId = "opmap.skirmish.desert_base_01";
        private const string CameraId = "camera.skirmish.desert_base_01.active";
        private const string MinimapId = "minimap.skirmish.desert_base_01.full";
        private const string SourceIdentityHash = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        private const string ContentHash = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        private const string BaseGeneratedMetadataHash = "574afec991fbc1a684531c9f727c20eb296271260e7a4e1c4a8c300a2b642e79";
        private const string GridContentHash = "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb";
        private const string SurfaceContentHash = "aa08cb9115e8727bfdbc671a4a2cfd9334ef48134c00d58d7d29e350c45b752c";
        private const string BuildingPlacementContentHash = "26973214f433c44ebca01f302ecbe05789c84e573dc48eb8b2c21f241823464d";
        private const string MatchSceneFileHash = "182f3b4cb50f48e1a573e1e90ee0c13baf9d62fce46e35b1850ef72097db5d75";
        private const string SubSceneFileHash = "eff3ce6992d234c7438a321f0f9f552c2abebcc0a4738445014bc8f86579965d";
        private const string ManifestFileHash = "a006ed18eab6523d9f9aeec82d6f21b5ff7089d9743a95e778117fe0fbb89c1b";

        [MenuItem("Game/Operation Maps/Rebuild Current Compatibility Definition")]
        public static void Run()
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Build();
                Debug.Log($"[OperationMapCompatibilityDefinition] Passed path={DefinitionPath}");
            }
            finally
            {
                if (setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void Build()
        {
            RequireFileHash(MatchScenePath, MatchSceneFileHash);
            RequireFileHash(SubScenePath, SubSceneFileHash);
            RequireFileHash(GridPath, GridContentHash);
            RequireFileHash(SurfacePath, SurfaceContentHash);
            RequireFileHash(
                OperationMapCurrentCompatibilityPlacementStager.SourceBuildingConfigPath,
                BuildingPlacementContentHash);
            RequireFileHash(ManifestPath, ManifestFileHash);

            GridAuthoringSceneConfigAsset grid =
                AssetDatabase.LoadAssetAtPath<GridAuthoringSceneConfigAsset>(GridPath);
            MapSurfaceDataAsset surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(SurfacePath);
            MapBuildingPlacementConfig buildingPlacements =
                AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(
                    OperationMapCurrentCompatibilityPlacementStager.SourceBuildingConfigPath);
            if (grid == null || surface == null || buildingPlacements == null)
                throw new InvalidOperationException("Current grid, map-surface, or building-placement asset is missing.");

            Scene scene = SceneManager.GetSceneByPath(MatchScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Additive);
            Scene subScene = SceneManager.GetSceneByPath(SubScenePath);
            if (!subScene.IsValid() || !subScene.isLoaded)
                subScene = EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);

            Camera camera = FindByLocalId<Camera>(scene, WorldCameraLocalId);
            Transform start = FindByLocalId<Transform>(scene, StartTransformLocalId);
            Transform end = FindByLocalId<Transform>(scene, EndTransformLocalId);
            Transform faction1 = FindByLocalId<Transform>(scene, Faction1TransformLocalId);
            Transform faction2 = FindByLocalId<Transform>(scene, Faction2TransformLocalId);
            GridAuthoring gridAuthoring = FindByLocalId<GridAuthoring>(subScene, GridAuthoringLocalId);
            if (camera == null || start == null || end == null || faction1 == null || faction2 == null || gridAuthoring == null)
                throw new InvalidOperationException("Current camera, grid authoring, faction volumes, or compatibility boundary anchors are missing.");
            int staticGridBlockerCount = CountComponents<StaticGridBlockerAuthoring>(subScene);

            GetSurfaceHeightRange(surface, out float minimumHeight, out float maximumHeight);
            Vector3 gridMin = grid.Origin;
            Vector3 gridMax = gridMin + new Vector3(grid.Width * grid.CellSize, 0f, grid.Height * grid.CellSize);
            float worldMinimumHeight = math.min(minimumHeight, 15f);
            float worldMaximumHeight = math.max(maximumHeight, 100f);
            OperationMapAnchorConfig[] infrastructureAnchors =
                OperationMapCurrentInfrastructureAnchorAuthoring.BuildInfrastructureAnchors(buildingPlacements);
            var anchors = new OperationMapAnchorConfig[4 + infrastructureAnchors.Length];
            anchors[0] = new OperationMapAnchorConfig(
                "anchor.skirmish.desert_base_01.compat_start",
                OperationMapAnchorKind.Debug,
                start.position,
                start.eulerAngles,
                0f);
            anchors[1] = new OperationMapAnchorConfig(
                "anchor.skirmish.desert_base_01.compat_end",
                OperationMapAnchorKind.Debug,
                end.position,
                end.eulerAngles,
                0f);
            anchors[2] = BuildFactionDeploymentAnchor(faction1, 1);
            anchors[3] = BuildFactionDeploymentAnchor(faction2, 2);
            Array.Copy(infrastructureAnchors, 0, anchors, 4, infrastructureAnchors.Length);
            string generatedMetadataHash =
                OperationMapCurrentInfrastructureAnchorAuthoring.ComputeGeneratedMetadataHash(
                    BaseGeneratedMetadataHash,
                    anchors);

            OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
            if (definition == null)
            {
                EnsureFolder();
                definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            Set(definition, "operationMapId", OperationMapId);
            Set(definition, "schemaVersion", 1);
            Set(definition, "contentVersion", 4);
            Set(definition, "sourceIdentityHash", SourceIdentityHash);
            Set(definition, "contentHash", ContentHash);
            Set(definition, "generatedMetadataHash", generatedMetadataHash);
            Set(definition, "bounds", new OperationMapBoundsConfig(
                new Vector3(gridMin.x, worldMinimumHeight, gridMin.z),
                new Vector3(gridMax.x, worldMaximumHeight, gridMax.z),
                new Vector3(gridMin.x, minimumHeight, gridMin.z),
                new Vector3(gridMax.x, maximumHeight, gridMax.z),
                new Vector3(gridMin.x, 15f, gridMin.z),
                new Vector3(gridMax.x, 100f, gridMax.z)));
            Set(definition, "gridMetadata", new OperationMapGridMetadataConfig(
                AssetDatabase.AssetPathToGUID(GridPath),
                GridContentHash,
                grid.Origin,
                new Vector2Int(grid.Width, grid.Height),
                grid.CellSize,
                grid.BlockedCells?.Length ?? 0));
            Set(definition, "surfaceMetadata", new OperationMapSurfaceMetadataConfig(
                AssetDatabase.AssetPathToGUID(SurfacePath),
                SurfaceContentHash,
                surface.ComputeRuntimeBlobHash().ToString(),
                surface.SurfaceCount,
                surface.PayloadVersion,
                surface.PayloadEncoding,
                minimumHeight,
                maximumHeight));
            Set(definition, "navigationMetadata", new OperationMapNavigationMetadataConfig(
                AssetDatabase.AssetPathToGUID(SubScenePath),
                GridAuthoringLocalId,
                staticGridBlockerCount,
                true,
                true,
                true));
            Set(definition, "cameras", new[]
            {
                new OperationMapCameraConfig(
                    CameraId,
                    camera.transform.position,
                    camera.transform.eulerAngles,
                    camera.orthographic,
                    camera.fieldOfView,
                    camera.orthographicSize,
                    true)
            });
            Set(definition, "planningCameraId", CameraId);
            Set(definition, "battleCameraId", CameraId);
            Set(definition, "minimap", new OperationMapMinimapConfig(
                MinimapId,
                grid.Origin,
                new Vector2(grid.Width * grid.CellSize, grid.Height * grid.CellSize),
                0f));
            Set(definition, "anchors", anchors);

            if (!definition.TryValidateMetadata(out string error))
                throw new InvalidOperationException(error);

            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
        }

        private static OperationMapAnchorConfig BuildFactionDeploymentAnchor(
            Transform factionVolume,
            int factionId)
        {
            Vector3 scale = factionVolume.lossyScale;
            float radius = math.min(math.abs(scale.x), math.abs(scale.z)) * 0.5f;
            return new OperationMapAnchorConfig(
                $"anchor.skirmish.desert_base_01.deployment.faction_{factionId}",
                OperationMapAnchorKind.Deployment,
                factionVolume.position,
                factionVolume.eulerAngles,
                radius,
                factionId,
                -1);
        }

        private static void GetSurfaceHeightRange(
            MapSurfaceDataAsset surface,
            out float minimumHeight,
            out float maximumHeight)
        {
            if (!surface.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob))
                throw new InvalidOperationException("Current map-surface blob could not be created.");

            try
            {
                minimumHeight = float.PositiveInfinity;
                maximumHeight = float.NegativeInfinity;
                ref MapSurfaceBlob value = ref blob.Value;
                if (value.RuntimeEncoding == MapSurfaceRuntimeEncoding.SingleLayerCompact)
                {
                    for (int index = 0; index < value.CompactSamples.Length; index++)
                    {
                        float height = value.CompactMinHeight +
                                       value.CompactSamples[index].PackedHeight * value.CompactHeightStep;
                        minimumHeight = math.min(minimumHeight, height);
                        maximumHeight = math.max(maximumHeight, height);
                    }
                }
                else
                {
                    for (int index = 0; index < value.Samples.Length; index++)
                    {
                        float height = value.Samples[index].Height;
                        minimumHeight = math.min(minimumHeight, height);
                        maximumHeight = math.max(maximumHeight, height);
                    }
                }

                if (!math.isfinite(minimumHeight) || !math.isfinite(maximumHeight))
                    throw new InvalidOperationException("Current map-surface blob contains no finite heights.");
            }
            finally
            {
                blob.Dispose();
            }
        }

        private static T FindByLocalId<T>(Scene scene, long localId) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T candidate in root.GetComponentsInChildren<T>(true))
                {
                    GlobalObjectId identity = GlobalObjectId.GetGlobalObjectIdSlow(candidate);
                    if ((long)identity.targetObjectId == localId)
                        return candidate;
                }
            }

            return null;
        }

        private static int CountComponents<T>(Scene scene) where T : Component
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<T>(true).Length;
            return count;
        }

        private static void EnsureFolder()
        {
            const string folder = "Assets/Game/Configs/OperationMaps";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Game/Configs", "OperationMaps");
        }

        private static void RequireFileHash(string assetPath, string expectedHash)
        {
            using SHA256 sha256 = SHA256.Create();
            string actualHash = ToLowerHex(sha256.ComputeHash(ReadCanonicalTextBytes(assetPath)));
            if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Compatibility input is stale: {assetPath}.");
        }

        private static byte[] ReadCanonicalTextBytes(string assetPath)
        {
            byte[] source = File.ReadAllBytes(Path.GetFullPath(assetPath));
            int crlfCount = 0;
            for (int index = 0; index + 1 < source.Length; index++)
            {
                if (source[index] == '\r' && source[index + 1] == '\n')
                    crlfCount++;
            }

            if (crlfCount == 0)
                return source;

            var canonical = new byte[source.Length - crlfCount];
            int outputIndex = 0;
            for (int inputIndex = 0; inputIndex < source.Length; inputIndex++)
            {
                if (source[inputIndex] == '\r' &&
                    inputIndex + 1 < source.Length &&
                    source[inputIndex + 1] == '\n')
                    continue;
                canonical[outputIndex++] = source[inputIndex];
            }
            return canonical;
        }

        private static string ToLowerHex(byte[] bytes)
        {
            const string digits = "0123456789abcdef";
            char[] result = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                result[index * 2] = digits[bytes[index] >> 4];
                result[index * 2 + 1] = digits[bytes[index] & 0x0f];
            }
            return new string(result);
        }

        private static void Set<T>(OperationMapDefinition definition, string fieldName, T value)
        {
            FieldInfo field = typeof(OperationMapDefinition).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new MissingFieldException(typeof(OperationMapDefinition).FullName, fieldName);
            field.SetValue(definition, value);
        }
    }

    public static class OperationMapCompatibilityRuntimeBindingBuilder
    {
        public const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        public const string CatalogPath =
            "Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset";
        public const string OperationMapId = "opmap.skirmish.desert_base_01";
        public const string ScenarioId = "scenario.skirmish.desert_base_standard";
        public const string MissionId = "skirmish";

        [MenuItem("Game/Operation Maps/Bind Current Compatibility Runtime")]
        public static void Run()
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
                MatchSceneView view = FindSingleView(scene);
                OperationMapCatalogConfig catalog =
                    AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(CatalogPath);
                if (catalog == null)
                    throw new InvalidOperationException("Compatibility operation-map catalog is missing.");
                if (!catalog.TryValidate(out string catalogError))
                    throw new InvalidOperationException(
                        $"Compatibility operation-map catalog is invalid: {catalogError}");

                var serializedView = new SerializedObject(view);
                SetObject(serializedView, "operationMapCatalog", catalog);
                SetString(serializedView, "operationMapId", OperationMapId);
                SetString(serializedView, "scenarioId", ScenarioId);
                SetString(serializedView, "missionId", MissionId);
                if (serializedView.ApplyModifiedPropertiesWithoutUndo())
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Failed to save compatibility runtime binding.");
                }

                Debug.Log($"[OperationMapCompatibilityBinding] Passed scene={MatchScenePath}");
            }
            finally
            {
                if (setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static MatchSceneView FindSingleView(Scene scene)
        {
            MatchSceneView result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MatchSceneView candidate in root.GetComponentsInChildren<MatchSceneView>(true))
                {
                    if (result != null)
                        throw new InvalidOperationException("Match scene contains multiple MatchSceneView components.");
                    result = candidate;
                }
            }

            return result != null
                ? result
                : throw new InvalidOperationException("Match scene is missing MatchSceneView.");
        }

        private static void SetObject(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject target, string propertyName, string value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.stringValue = value;
        }
    }
}
