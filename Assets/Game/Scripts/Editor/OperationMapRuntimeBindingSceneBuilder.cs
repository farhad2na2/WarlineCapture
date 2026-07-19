using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
using Game.Runtime;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapRuntimeBindingSceneBuilder
    {
        public const string OutputFolder =
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01";
        public const string OutputPath = OutputFolder +
            "/opmap_skirmish_desert_base_01_runtime.unity";
        public const string LedgerPath = OutputFolder +
            "/opmap_skirmish_desert_base_01_runtime_ledger.json";
        internal const string CandidatePath = OutputFolder +
            "/opmap_skirmish_desert_base_01_runtime_candidate.unity";

        [MenuItem("Game/Operation Maps/Build Current Runtime Binding Scene")]
        public static void Run()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene authoringScene = default;
            Scene candidateScene = default;
            try
            {
                authoringScene = EditorSceneManager.OpenScene(
                    StaticMapPresentationBaker.CurrentStagedOperationMapScenePath,
                    OpenSceneMode.Single);
                OperationMapSceneView source = RequireSourceView(authoringScene);
                BindingInput input = BindingInput.Capture(source);
                CloseSceneKeepingEditorValid(authoringScene);
                authoringScene = default;

                EnsureOutputFolder();
                RuntimeBindingLedger ledger = CreateLedger(input);
                if (TryReuse(ledger))
                {
                    Debug.Log($"[OperationMapRuntimeBindingSceneBuilder] result=Passed reused=true path={OutputPath}");
                    return;
                }
                AssetDatabase.DeleteAsset(CandidatePath);
                candidateScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
                CreateBindingScene(candidateScene, input);
                if (!OperationMapRuntimeBindingSceneValidator.TryValidateLoadedScene(
                        candidateScene,
                        StaticMapPresentationBaker.CurrentOperationMapId,
                        out string error))
                {
                    throw new InvalidOperationException(error);
                }
                if (!EditorSceneManager.SaveScene(candidateScene, CandidatePath, false))
                    throw new InvalidOperationException("Unity failed to save the runtime binding candidate scene.");
                CloseSceneKeepingEditorValid(candidateScene);
                candidateScene = default;
                NormalizeCandidateText();

                PublishCandidate();
                ValidatePublishedScene();
                ledger.outputHash = ComputeFileHash(OutputPath);
                WriteLedger(ledger);
                Debug.Log($"[OperationMapRuntimeBindingSceneBuilder] result=Passed path={OutputPath}");
            }
            finally
            {
                if (authoringScene.IsValid() && authoringScene.isLoaded)
                    CloseSceneKeepingEditorValid(authoringScene);
                if (candidateScene.IsValid() && candidateScene.isLoaded)
                    CloseSceneKeepingEditorValid(candidateScene);
                AssetDatabase.DeleteAsset(CandidatePath);
                if (previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static OperationMapSceneView RequireSourceView(Scene scene)
        {
            OperationMapSceneView found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                OperationMapSceneView[] views = root.GetComponentsInChildren<OperationMapSceneView>(true);
                for (int index = 0; index < views.Length; index++)
                {
                    if (found != null)
                        throw new InvalidOperationException("Authoring scene contains multiple operation-map views.");
                    found = views[index];
                }
            }

            if (found == null)
                throw new InvalidOperationException("Authoring scene view is missing.");
            if (!found.TryValidate(out string error))
                throw new InvalidOperationException(error);
            return found;
        }

        private static void CreateBindingScene(Scene scene, BindingInput input)
        {
            OperationMapDefinition definition = LoadRequired<OperationMapDefinition>(input.DefinitionPath);
            GridAuthoringConfig gridConfig = LoadRequired<GridAuthoringConfig>(input.GridConfigPath);
            MapBuildingPlacementConfig buildingPlacements =
                LoadRequired<MapBuildingPlacementConfig>(input.BuildingPlacementsPath);
            MapVehiclePlacementConfig vehiclePlacements =
                LoadRequired<MapVehiclePlacementConfig>(input.VehiclePlacementsPath);
            MapSurfaceDataAsset surfaceAsset = LoadRequired<MapSurfaceDataAsset>(input.SurfaceDataPath);
            SceneAsset subSceneAsset = LoadRequired<SceneAsset>(input.SubScenePath);
            GameObject viewRoot = CreateRoot(scene, "OperationMapSceneView");
            OperationMapSceneView view = viewRoot.AddComponent<OperationMapSceneView>();
            GameObject mapRoot = CreateRoot(scene, "RuntimeMapBindings");

            Transform decorations = CreateChild(mapRoot.transform, "Decorations");
            CombinedMeshBaker combinedMeshBaker = decorations.gameObject.AddComponent<CombinedMeshBaker>();
            Transform buildings = CreateChild(mapRoot.transform, "Buildings");
            Transform vehicles = CreateChild(mapRoot.transform, "Vehicles");
            Transform surfaceRoot = CreateChild(mapRoot.transform, "Surface");
            MapSurfaceAuthoring surface = surfaceRoot.gameObject.AddComponent<MapSurfaceAuthoring>();
            Transform subSceneRoot = CreateChild(mapRoot.transform, "SubScene");
            SubScene subScene = subSceneRoot.gameObject.AddComponent<SubScene>();
            subScene.SceneAsset = subSceneAsset;
            subScene.AutoLoadScene = true;

            var surfaceData = new SerializedObject(surface);
            surfaceData.FindProperty("bakedSurfaceData").objectReferenceValue = surfaceAsset;
            surfaceData.FindProperty("gridConfig").objectReferenceValue = gridConfig;
            surfaceData.FindProperty("samplesPerCellAxis").intValue = input.SamplesPerCellAxis;
            surfaceData.FindProperty("maxSampleHeightDelta").floatValue = input.MaxSampleHeightDelta;
            surfaceData.FindProperty("maxBuildingSlopeDegrees").floatValue = input.MaxBuildingSlopeDegrees;
            surfaceData.FindProperty("maxInfantrySlopeDegrees").floatValue = input.MaxInfantrySlopeDegrees;
            surfaceData.FindProperty("maxVehicleSlopeDegrees").floatValue = input.MaxVehicleSlopeDegrees;
            surfaceData.ApplyModifiedPropertiesWithoutUndo();

            var viewData = new SerializedObject(view);
            viewData.FindProperty("operationMapId").stringValue = input.OperationMapId;
            viewData.FindProperty("definition").objectReferenceValue = definition;
            viewData.FindProperty("mapRoot").objectReferenceValue = mapRoot.transform;
            viewData.FindProperty("decorationCombinedMeshBaker").objectReferenceValue = combinedMeshBaker;
            viewData.FindProperty("decorationRoot").objectReferenceValue = decorations;
            viewData.FindProperty("buildingAuthoringRoot").objectReferenceValue = buildings;
            viewData.FindProperty("vehicleAuthoringRoot").objectReferenceValue = vehicles;
            viewData.FindProperty("mapSurfaceAuthoring").objectReferenceValue = surface;
            viewData.FindProperty("gridAuthoringConfig").objectReferenceValue = gridConfig;
            viewData.FindProperty("buildingPlacements").objectReferenceValue = buildingPlacements;
            viewData.FindProperty("vehiclePlacements").objectReferenceValue = vehiclePlacements;
            viewData.FindProperty("mapSubScene").objectReferenceValue = subScene;
            viewData.FindProperty("canonicalPresentationMode").enumValueIndex =
                (int)OperationMapCanonicalPresentationMode.PresentationOnly;
            viewData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PublishCandidate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string candidatePhysical = PhysicalPath(CandidatePath);
            string outputPhysical = PhysicalPath(OutputPath);
            byte[] candidateBytes = File.ReadAllBytes(candidatePhysical);
            if (File.Exists(outputPhysical) &&
                candidateBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPhysical)))
                return;

            if (!File.Exists(outputPhysical))
            {
                string moveError = AssetDatabase.MoveAsset(CandidatePath, OutputPath);
                if (!string.IsNullOrEmpty(moveError))
                    throw new InvalidOperationException(moveError);
                return;
            }

            File.Copy(candidatePhysical, outputPhysical, true);
            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ValidatePublishedScene()
        {
            Scene scene = EditorSceneManager.OpenScene(OutputPath, OpenSceneMode.Single);
            try
            {
                if (!OperationMapRuntimeBindingSceneValidator.TryValidateLoadedScene(
                        scene,
                        StaticMapPresentationBaker.CurrentOperationMapId,
                        out string error))
                    throw new InvalidOperationException(error);
            }
            finally
            {
                CloseSceneKeepingEditorValid(scene);
            }
        }

        private static void CloseSceneKeepingEditorValid(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            if (SceneManager.sceneCount == 1)
            {
                Scene transitionScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                SceneManager.SetActiveScene(transitionScene);
            }

            if (!EditorSceneManager.CloseScene(scene, true))
                throw new InvalidOperationException($"Unity failed to close scene '{scene.path}'.");
        }

        private static void NormalizeCandidateText()
        {
            string physicalPath = Path.GetFullPath(CandidatePath);
            string[] lines = File.ReadAllLines(physicalPath);
            var text = new StringBuilder();
            for (int index = 0; index < lines.Length; index++)
                text.Append(lines[index].TrimEnd(' ', '\t')).Append('\n');
            File.WriteAllText(physicalPath, text.ToString(), new UTF8Encoding(false));
        }

        private static GameObject CreateRoot(Scene scene, string name)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void EnsureOutputFolder()
        {
            string current = "Assets";
            string[] segments = OutputFolder.Substring("Assets/".Length).Split('/');
            for (int index = 0; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static string PhysicalPath(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        private static RuntimeBindingLedger CreateLedger(BindingInput input)
        {
            string sourceScenePath = StaticMapPresentationBaker.CurrentStagedOperationMapScenePath;
            string[] inputPaths =
            {
                sourceScenePath,
                input.DefinitionPath,
                input.GridConfigPath,
                input.BuildingPlacementsPath,
                input.VehiclePlacementsPath,
                input.SurfaceDataPath,
                input.SubScenePath
            };
            var fingerprint = new StringBuilder(1024);
            for (int index = 0; index < inputPaths.Length; index++)
                fingerprint.Append(inputPaths[index]).Append('|').Append(ComputeFileHash(inputPaths[index])).Append('\n');

            return new RuntimeBindingLedger
            {
                schemaVersion = 2,
                operationMapId = input.OperationMapId,
                sourceSceneGuid = AssetDatabase.AssetPathToGUID(sourceScenePath),
                sourceSceneHash = ComputeFileHash(sourceScenePath),
                inputHash = ComputeTextHash(fingerprint.ToString()),
                strippedRendererCount = input.SourceRendererCount,
                strippedColliderCount = input.SourceColliderCount,
                copiedPhysicsIdentities = Array.Empty<string>(),
                outputGameObjectCount = 7,
                outputHash = string.Empty
            };
        }

        private static bool TryReuse(RuntimeBindingLedger expected)
        {
            string ledgerPhysical = PhysicalPath(LedgerPath);
            string outputPhysical = PhysicalPath(OutputPath);
            if (!File.Exists(ledgerPhysical) || !File.Exists(outputPhysical))
                return false;

            RuntimeBindingLedger current;
            try
            {
                current = JsonUtility.FromJson<RuntimeBindingLedger>(File.ReadAllText(ledgerPhysical));
            }
            catch
            {
                return false;
            }

            if (current == null || current.schemaVersion != expected.schemaVersion ||
                !string.Equals(current.operationMapId, expected.operationMapId, StringComparison.Ordinal) ||
                !string.Equals(current.sourceSceneGuid, expected.sourceSceneGuid, StringComparison.Ordinal) ||
                !string.Equals(current.sourceSceneHash, expected.sourceSceneHash, StringComparison.Ordinal) ||
                !string.Equals(current.inputHash, expected.inputHash, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(current.outputHash) ||
                !string.Equals(current.outputHash, ComputeFileHash(OutputPath), StringComparison.Ordinal))
                return false;

            ValidatePublishedScene();
            return true;
        }

        private static void WriteLedger(RuntimeBindingLedger ledger)
        {
            string physicalPath = PhysicalPath(LedgerPath);
            string temporaryPath = physicalPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(ledger, true) + "\n", new UTF8Encoding(false));
                if (File.Exists(physicalPath))
                    File.Replace(temporaryPath, physicalPath, null);
                else
                    File.Move(temporaryPath, physicalPath);
                AssetDatabase.ImportAsset(LedgerPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static string ComputeFileHash(string assetPath)
        {
            using SHA256 algorithm = SHA256.Create();
            return ToLowerHex(algorithm.ComputeHash(File.ReadAllBytes(PhysicalPath(assetPath))));
        }

        private static string ComputeTextHash(string value)
        {
            using SHA256 algorithm = SHA256.Create();
            return ToLowerHex(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        private static string ToLowerHex(byte[] bytes) =>
            BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null
                ? asset
                : throw new InvalidOperationException($"Runtime binding input is missing: {path}");
        }

        private readonly struct BindingInput
        {
            internal readonly string OperationMapId;
            internal readonly string DefinitionPath;
            internal readonly string GridConfigPath;
            internal readonly string BuildingPlacementsPath;
            internal readonly string VehiclePlacementsPath;
            internal readonly string SurfaceDataPath;
            internal readonly string SubScenePath;
            internal readonly int SamplesPerCellAxis;
            internal readonly float MaxSampleHeightDelta;
            internal readonly float MaxBuildingSlopeDegrees;
            internal readonly float MaxInfantrySlopeDegrees;
            internal readonly float MaxVehicleSlopeDegrees;
            internal readonly int SourceRendererCount;
            internal readonly int SourceColliderCount;

            private BindingInput(OperationMapSceneView source)
            {
                OperationMapId = source.OperationMapId;
                DefinitionPath = AssetDatabase.GetAssetPath(source.Definition);
                GridConfigPath = AssetDatabase.GetAssetPath(source.GridAuthoringConfig);
                BuildingPlacementsPath = AssetDatabase.GetAssetPath(source.BuildingPlacements);
                VehiclePlacementsPath = AssetDatabase.GetAssetPath(source.VehiclePlacements);
                SurfaceDataPath = AssetDatabase.GetAssetPath(source.MapSurfaceAuthoring.BakedSurfaceData);
                SubScenePath = AssetDatabase.GetAssetPath(source.MapSubScene.SceneAsset);
                SamplesPerCellAxis = source.MapSurfaceAuthoring.SamplesPerCellAxis;
                MaxSampleHeightDelta = source.MapSurfaceAuthoring.MaxSampleHeightDelta;
                MaxBuildingSlopeDegrees = source.MapSurfaceAuthoring.MaxBuildingSlopeDegrees;
                MaxInfantrySlopeDegrees = source.MapSurfaceAuthoring.MaxInfantrySlopeDegrees;
                MaxVehicleSlopeDegrees = source.MapSurfaceAuthoring.MaxVehicleSlopeDegrees;
                SourceRendererCount = source.MapRoot.GetComponentsInChildren<Renderer>(true).Length;
                SourceColliderCount = source.MapRoot.GetComponentsInChildren<Collider>(true).Length;
            }

            internal static BindingInput Capture(OperationMapSceneView source) => new(source);
        }

        [Serializable]
        private sealed class RuntimeBindingLedger
        {
            public int schemaVersion;
            public string operationMapId;
            public string sourceSceneGuid;
            public string sourceSceneHash;
            public string inputHash;
            public int strippedRendererCount;
            public int strippedColliderCount;
            public string[] copiedPhysicsIdentities;
            public int outputGameObjectCount;
            public string outputHash;
        }
    }
}
