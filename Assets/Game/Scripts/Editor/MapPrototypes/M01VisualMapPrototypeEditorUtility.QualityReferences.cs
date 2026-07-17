namespace Game.Editor
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public static partial class M01VisualMapPrototypeEditorUtility
    {
        private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
        private const string Demo2ScenePath = "Assets/Game/Scenes/Demo2.unity";
        private const string DemoReferencePath = "Logs/M01_QualityReference_Demo.png";
        private const string Demo2ReferencePath = "Logs/M01_QualityReference_Demo2.png";

        [MenuItem("Game/Map Prototypes/M01/Capture Demo Quality References")]
        public static void CaptureDemoQualityReferences()
        {
            CaptureQualityReference(DemoScenePath, DemoReferencePath);
            CaptureQualityReference(Demo2ScenePath, Demo2ReferencePath);
            AssetDatabase.Refresh();
            Debug.Log("[M01QualityReferences] result=Passed references=2");
        }

        public static void CaptureDemoQualityReferencesAndExit()
        {
            try
            {
                CaptureDemoQualityReferences();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M01QualityReferences] result=Failed");
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureQualityReference(string scenePath, string outputPath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Camera camera = SelectReferenceCamera(cameras);
            if (camera == null)
                throw new InvalidOperationException($"Quality-reference scene has no camera: {scenePath}");

            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? throw new InvalidOperationException($"Invalid output path: {outputPath}"));
            CaptureCamera(camera, absolutePath);
            Debug.Log($"[M01QualityReferences] scene={scene.path} camera={camera.name} output={outputPath}");
        }

        private static Camera SelectReferenceCamera(Camera[] cameras)
        {
            Camera selected = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate == null || !candidate.gameObject.scene.IsValid())
                    continue;

                if (selected == null || (candidate.isActiveAndEnabled && !selected.isActiveAndEnabled) || candidate.depth > selected.depth)
                    selected = candidate;
            }

            return selected;
        }
    }
#endif
}
