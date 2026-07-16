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
        private const string SubScenePath = "Assets/Game/Scenes/Match/MatchSubScene.unity";
        private const string ManifestPath =
            "Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset";
        private const long WorldCameraLocalId = 1220593093;
        private const long StartTransformLocalId = 229045073;
        private const long EndTransformLocalId = 29742182;
        private const long GridAuthoringLocalId = 146043441;
        private const string OperationMapId = "opmap.skirmish.desert_base_01";
        private const string CameraId = "camera.skirmish.desert_base_01.active";
        private const string MinimapId = "minimap.skirmish.desert_base_01.full";
        private const string SourceIdentityHash = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        private const string ContentHash = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        private const string GeneratedMetadataHash = "574afec991fbc1a684531c9f727c20eb296271260e7a4e1c4a8c300a2b642e79";
        private const string GridContentHash = "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb";
        private const string SurfaceContentHash = "aa08cb9115e8727bfdbc671a4a2cfd9334ef48134c00d58d7d29e350c45b752c";
        private const string MatchSceneFileHash = "182f3b4cb50f48e1a573e1e90ee0c13baf9d62fce46e35b1850ef72097db5d75";
        private const string SubSceneFileHash = "bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8";
        private const string ManifestFileHash = "494e0052e1c55578238fd1200517999a437fb35465aac3eb295ec0c79e0cc715";

        [MenuItem("Tools/Warline Capture/Operation Maps/Rebuild Current Compatibility Definition")]
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
            RequireFileHash(ManifestPath, ManifestFileHash);

            GridAuthoringSceneConfigAsset grid =
                AssetDatabase.LoadAssetAtPath<GridAuthoringSceneConfigAsset>(GridPath);
            MapSurfaceDataAsset surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(SurfacePath);
            if (grid == null || surface == null)
                throw new InvalidOperationException("Current grid or map-surface asset is missing.");

            Scene scene = SceneManager.GetSceneByPath(MatchScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Additive);
            Scene subScene = SceneManager.GetSceneByPath(SubScenePath);
            if (!subScene.IsValid() || !subScene.isLoaded)
                subScene = EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);

            Camera camera = FindByLocalId<Camera>(scene, WorldCameraLocalId);
            Transform start = FindByLocalId<Transform>(scene, StartTransformLocalId);
            Transform end = FindByLocalId<Transform>(scene, EndTransformLocalId);
            GridAuthoring gridAuthoring = FindByLocalId<GridAuthoring>(subScene, GridAuthoringLocalId);
            if (camera == null || start == null || end == null || gridAuthoring == null)
                throw new InvalidOperationException("Current camera, grid authoring, or compatibility boundary anchors are missing.");
            int staticGridBlockerCount = CountComponents<StaticGridBlockerAuthoring>(subScene);

            GetSurfaceHeightRange(surface, out float minimumHeight, out float maximumHeight);
            Vector3 gridMin = grid.Origin;
            Vector3 gridMax = gridMin + new Vector3(grid.Width * grid.CellSize, 0f, grid.Height * grid.CellSize);
            float worldMinimumHeight = math.min(minimumHeight, 15f);
            float worldMaximumHeight = math.max(maximumHeight, 100f);

            OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
            if (definition == null)
            {
                EnsureFolder();
                definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            Set(definition, "operationMapId", OperationMapId);
            Set(definition, "schemaVersion", 1);
            Set(definition, "contentVersion", 2);
            Set(definition, "sourceIdentityHash", SourceIdentityHash);
            Set(definition, "contentHash", ContentHash);
            Set(definition, "generatedMetadataHash", GeneratedMetadataHash);
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
            Set(definition, "anchors", new[]
            {
                new OperationMapAnchorConfig(
                    "anchor.skirmish.desert_base_01.compat_start",
                    OperationMapAnchorKind.Debug,
                    start.position,
                    start.eulerAngles,
                    0f),
                new OperationMapAnchorConfig(
                    "anchor.skirmish.desert_base_01.compat_end",
                    OperationMapAnchorKind.Debug,
                    end.position,
                    end.eulerAngles,
                    0f)
            });

            if (!definition.TryValidateMetadata(out string error))
                throw new InvalidOperationException(error);

            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
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
            using FileStream stream = File.OpenRead(Path.GetFullPath(assetPath));
            using SHA256 sha256 = SHA256.Create();
            string actualHash = ToLowerHex(sha256.ComputeHash(stream));
            if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Compatibility input is stale: {assetPath}.");
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

        [MenuItem("Tools/Warline Capture/Operation Maps/Bind Current Compatibility Runtime")]
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
