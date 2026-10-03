using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    // Rebuilds the Refinery prepared map with the beautify pass and captures the Supply Line mission
    // cameras before and after. The mission bindings pin the prepared semantic hash, so a rebuild that
    // moves it fails here rather than in a mission.
    public static class MapVariantBeautifyPipeline
    {
        private const string Map = "RefineryDistrict";
        private const string PinnedSemanticHash = "2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778";
        private const string SceneFolder = "Assets/Game/Scenes/OperationMaps/Variants/" + Map;
        private const string CaptureFolder = "Design/MapVariants/Beautify/SupplyLine";
        private const string HostScene = "Assets/Game/Scenes/Match.unity";

        private static readonly (string name, Vector3 focus, Vector3 offset)[] Views =
        {
            ("battle", new Vector3(734f, 0f, 505f), new Vector3(0f, 110f, -65f)),
            ("overview", new Vector3(745f, 0f, 510f), new Vector3(0f, 230f, -120f)),
            ("zoom-yard", new Vector3(690f, 0f, 474f), new Vector3(0f, 38f, -24f)),
            ("zoom-gate", new Vector3(716f, 0f, 510f), new Vector3(0f, 40f, -26f)),
            ("zoom-pumpgate", new Vector3(600f, 0f, 505f), new Vector3(0f, 55f, -34f)),
            ("zoom-refinery", new Vector3(770f, 0f, 560f), new Vector3(0f, 55f, -34f)),
        };

        public static void CaptureBefore() => Capture("before");
        public static void CaptureAfter() => Capture("after");

        public static void RebuildRefinery()
        {
            EditorSceneManager.OpenScene(HostScene, OpenSceneMode.Single);
            MapVariantPreparedCandidateBuilder.Build(Map);
            string manifest = File.ReadAllText($"Design/MapVariants/Preparation/{Map}/Candidate/output-manifest.json");
            string hash = JsonUtility.FromJson<PreparedCandidateOutput>(manifest).semanticHash;
            if (hash != PinnedSemanticHash)
                throw new InvalidOperationException($"[MapBeautify] semanticHash moved: {hash} (missions pin {PinnedSemanticHash})");
            MapVariantCandidateRuntimeBuilder.BuildRefineryContent();
            Capture("after");
            Debug.Log($"[MapBeautify] rebuild result=Passed semanticHash={hash}");
        }

        public static void CaptureDenseCityBefore() => CaptureMissionCameras("opmap.skirmish.desert_base_01", "DenseCity", "before");

        // Renders each mission's battle camera on a shared source map, using the mission's own scenes.
        public static void CaptureMissionCameras(string sourceMapId, string folderName, string label)
        {
            string folder = "Design/MapVariants/Beautify/" + folderName;
            Directory.CreateDirectory(folder);
            var missions = AssetDatabase.FindAssets("t:OperationMapDefinition", new[] { "Assets/Game/Configs/OperationMaps" })
                .Select(g => new SerializedObject(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(g))))
                .Where(s => s.FindProperty("sourceBinding.sourceOperationMapId")?.stringValue == sourceMapId)
                .OrderBy(s => s.targetObject.name).ToList();
            if (missions.Count == 0) throw new InvalidOperationException("[MapBeautify] No missions on " + sourceMapId);
            string authored = AssetDatabase.GUIDToAssetPath(missions[0].FindProperty("navigationMetadata.authoredSubSceneGuid").stringValue);
            string binding = AssetDatabase.GUIDToAssetPath(missions[0].FindProperty("sourceSceneReference.m_AssetGUID").stringValue);
            var prepared = EditorSceneManager.OpenScene(authored, OpenSceneMode.Single);
            var bind = string.IsNullOrEmpty(binding) ? prepared : EditorSceneManager.OpenScene(binding, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(bind);
            foreach (var owner in prepared.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)))
                if (owner.DestroyedVisualRoot != null) owner.DestroyedVisualRoot.SetActive(false);

            var go = new GameObject("BeautifyCaptureCamera");
            SceneManager.MoveGameObjectToScene(go, bind);
            var camera = go.AddComponent<Camera>();
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 3000f;
            EnablePostProcessing(go);
            const int width = 1600, height = 900;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                foreach (var mission in missions)
                {
                    string battleId = mission.FindProperty("battleCameraId").stringValue;
                    var cameras = mission.FindProperty("cameras");
                    for (int i = 0; i < cameras.arraySize; i++)
                    {
                        var c = cameras.GetArrayElementAtIndex(i);
                        if (c.FindPropertyRelative("cameraId").stringValue != battleId) continue;
                        go.transform.SetPositionAndRotation(c.FindPropertyRelative("position").vector3Value,
                            Quaternion.Euler(c.FindPropertyRelative("eulerAngles").vector3Value));
                        camera.fieldOfView = c.FindPropertyRelative("fieldOfView").floatValue;
                        camera.Render();
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        image.Apply();
                        RenderTexture.active = null;
                        string path = $"{folder}/{label}-{mission.targetObject.name.Replace("OperationMap_", "")}.png";
                        File.WriteAllBytes(path, image.EncodeToPNG());
                        Debug.Log("[MapBeautify] capture " + path);
                    }
                }
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
                EditorSceneManager.OpenScene(HostScene, OpenSceneMode.Single);
            }
            Debug.Log($"[MapBeautify] capture map={folderName} missions={missions.Count} label={label} result=Passed");
        }

        public static void Capture(string label)
        {
            Directory.CreateDirectory(CaptureFolder);
            var prepared = EditorSceneManager.OpenScene(SceneFolder + "/PreparedEntities.unity", OpenSceneMode.Single);
            var binding = EditorSceneManager.OpenScene(SceneFolder + "/RuntimeBinding.unity", OpenSceneMode.Additive);
            SceneManager.SetActiveScene(binding);
            foreach (var owner in prepared.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)))
                if (owner.DestroyedVisualRoot != null) owner.DestroyedVisualRoot.SetActive(false);
            foreach (var system in binding.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ParticleSystem>(true)))
                if (system.transform.parent == null || system.transform.parent.GetComponentInParent<ParticleSystem>() == null)
                    system.Simulate(6f, true, true, true);

            var go = new GameObject("BeautifyCaptureCamera");
            SceneManager.MoveGameObjectToScene(go, binding);
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 3000f;
            EnablePostProcessing(go);
            const int width = 1600, height = 900;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                foreach (var view in Views)
                {
                    go.transform.position = view.focus + view.offset;
                    go.transform.LookAt(view.focus);
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply();
                    RenderTexture.active = null;
                    string path = $"{CaptureFolder}/{label}-{view.name}.png";
                    File.WriteAllBytes(path, image.EncodeToPNG());
                    Debug.Log("[MapBeautify] capture " + path);
                }
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
                // The map builders add scenes beside the open one, which must be saved and titled.
                EditorSceneManager.OpenScene(HostScene, OpenSceneMode.Single);
            }
            Debug.Log($"[MapBeautify] capture label={label} result=Passed");
        }

        // Game.Editor does not reference the URP runtime assembly; the game cameras render post-processing.
        private static void EnablePostProcessing(GameObject go)
        {
            var type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) return;
            var data = go.GetComponent(type) ?? go.AddComponent(type);
            type.GetProperty("renderPostProcessing")?.SetValue(data, true);
        }
    }
}
