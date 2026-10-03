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

        public static void RebuildRefineryVisuals()
        {
            EditorSceneManager.OpenScene(HostScene, OpenSceneMode.Single);
            MapVariantPreparedCandidateBuilder.Build(Map);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var output = JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText($"Design/MapVariants/Preparation/{Map}/Candidate/output-manifest.json"));
            if (output.semanticHash != PinnedSemanticHash) throw new InvalidOperationException("[MapBeautify] semanticHash moved");
            Capture("after", true);
            Debug.Log($"[MapBeautify] visuals result=Passed semanticHash={output.semanticHash}");
        }

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

        public static void RebuildFrontierVisuals()
        {
            // The user approved regrouping on this currently unbound map.
            if (AssetDatabase.FindAssets("t:OperationMapDefinition", new[] { "Assets/Game/Configs/OperationMaps" })
                .Select(g => new SerializedObject(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(g))))
                .Any(o => o.FindProperty("sourceBinding.sourceOperationMapId")?.stringValue == "opmap.skirmish.frontier_prepared"))
                throw new InvalidOperationException("Frontier now has bound missions; regrouping needs a new pin audit.");
            EditorSceneManager.OpenScene(HostScene, OpenSceneMode.Single);
            MapVariantPreparedCandidateBuilder.Build("Frontier");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Capture("after", true, "Frontier", "Design/MapVariants/Beautify/Frontier", new[]
            {
                ("port-blocks", new Vector3(260f, 0f, 655f), new Vector3(0f, 130f, -75f)),
                ("port-close", new Vector3(260f, 0f, 655f), new Vector3(0f, 55f, -32f)),
                ("canal", new Vector3(365f, 0f, 560f), new Vector3(0f, 115f, -70f)),
                ("overview", new Vector3(1024f, 0f, 512f), new Vector3(0f, 1000f, -550f)),
            });
            Debug.Log("[MapBeautify] visuals map=Frontier result=Passed scope=ApprovedLayoutPreview");
        }

        public static void SaveVisualBindingsAndCapture()
        {
            var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/MapBeautify/Concrete.mat");
            concrete.SetColor("_BaseColor",new Color(.85f,.82f,.75f,1f));EditorUtility.SetDirty(concrete);
            foreach(string map in new[]{"RefineryDistrict","CityEdgeAirfield","AshLinePort","Frontier"})
            {
                string folder=$"Assets/Game/Scenes/OperationMaps/Variants/{map}/";
                var source=EditorSceneManager.OpenScene(folder+"PreparedEntities.unity",OpenSceneMode.Single);
                var binding=EditorSceneManager.OpenScene(folder+"RuntimeBinding.unity",OpenSceneMode.Additive);
                SceneManager.SetActiveScene(binding);
                foreach(var root in binding.GetRootGameObjects().Where(r=>r.name.Contains("Atmosphere"))) UnityEngine.Object.DestroyImmediate(root);
                var output=JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText($"Design/MapVariants/Preparation/{map}/Candidate/output-manifest.json"));
                MapVariantAtmosphere.Build(binding,source,map,new Rect(output.runtimePlayableMin,output.playableSize));
                foreach(var light in binding.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Directional))
                {
                    light.color=new Color(1f,.88f,.72f);light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(36f,-32f,0f);RenderSettings.sun=light;
                }
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=new Color(.54f,.57f,.61f);RenderSettings.ambientEquatorColor=new Color(.4f,.36f,.31f);RenderSettings.ambientGroundColor=new Color(.21f,.18f,.14f);
                if(!EditorSceneManager.SaveScene(binding,folder+"RuntimeBinding.unity")) throw new IOException("Visual binding save failed: "+map);
                EditorSceneManager.OpenScene(HostScene,OpenSceneMode.Single);
                Debug.Log("[MapBeautify] saved visual binding map="+map);
            }
            AssetDatabase.SaveAssets();
            Capture("after");
            CaptureMissionCameras("opmap.skirmish.cityedgeairfield_prepared","CityEdgeAirfield","after");
            CaptureMissionCameras("opmap.skirmish.ashlineport_prepared","AshLinePort","after");
            Capture("after",false,"Frontier","Design/MapVariants/Beautify/Frontier",new[]{
                ("port-blocks",new Vector3(260f,0f,655f),new Vector3(0f,130f,-75f)),
                ("port-close",new Vector3(260f,0f,655f),new Vector3(0f,55f,-32f)),
                ("canal",new Vector3(365f,0f,560f),new Vector3(0f,115f,-70f)),
                ("overview",new Vector3(1024f,0f,512f),new Vector3(0f,1000f,-550f))});
            CaptureMediumCloseViews();
            Debug.Log("[MapBeautify] savedBindingsAndCapture result=Passed maps=4");
        }

        public static void CaptureMediumCloseViews()
        {
            Capture("after", true, "CityEdgeAirfield", "Design/MapVariants/Beautify/CityEdgeAirfield", new[]
            {
                ("zoom-apron", new Vector3(780f, 0f, 648f), new Vector3(0f, 55f, -34f)),
                ("zoom-edge", new Vector3(705f, 0f, 588f), new Vector3(0f, 45f, -30f)),
            });
            Capture("after", true, "AshLinePort", "Design/MapVariants/Beautify/AshLinePort", new[]
            {
                ("zoom-quay", new Vector3(665f, 0f, 535f), new Vector3(0f, 70f, -42f)),
                ("zoom-containers", new Vector3(600f, 0f, 590f), new Vector3(0f, 55f, -34f)),
            });
        }

        public static void RebuildAirfieldVisuals() => RebuildVisuals("CityEdgeAirfield", "e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22");
        public static void RebuildPortVisuals() => RebuildVisuals("AshLinePort", "3f8bceea706372ba2a6b0584d6e7820c73ce3e40c30c53acdd937d9998cae8d5");

        private static void RebuildVisuals(string map, string pinned)
        {
            EditorSceneManager.OpenScene(HostScene, OpenSceneMode.Single);
            MapVariantPreparedCandidateBuilder.Build(map);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var output = JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText($"Design/MapVariants/Preparation/{map}/Candidate/output-manifest.json"));
            if (output.semanticHash != pinned) throw new InvalidOperationException("[MapBeautify] semanticHash moved: " + output.semanticHash);
            CaptureMissionCameras(output.mapId, map, "after", map);
            Debug.Log($"[MapBeautify] visuals map={map} result=Passed semanticHash={output.semanticHash}");
        }

        public static void CaptureDenseCityBefore() => CaptureMissionCameras("opmap.skirmish.desert_base_01", "DenseCity", "before");

        // Renders each mission's battle camera on a shared source map, using the mission's own scenes.
        public static void CaptureMissionCameras(string sourceMapId, string folderName, string label, string refreshMap = null)
        {
            using var profile = new VisualPreviewProfile();
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
            if (refreshMap != null)
            {
                foreach (var root in bind.GetRootGameObjects().Where(r => r.name.Contains("Atmosphere"))) UnityEngine.Object.DestroyImmediate(root);
                var output = JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText($"Design/MapVariants/Preparation/{refreshMap}/Candidate/output-manifest.json"));
                MapVariantAtmosphere.Build(bind, prepared, refreshMap, new Rect(output.runtimePlayableMin, output.playableSize));
            }
            foreach (var system in bind.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ParticleSystem>(true)))
                if (system.transform.parent == null || system.transform.parent.GetComponentInParent<ParticleSystem>() == null) system.Simulate(6f, true, true, true);
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
                        string path = $"{folder}/{label}-{Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mission.targetObject))}.png";
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

        public static void Capture(string label, bool freshAtmosphere = false, string previewMap = Map, string previewFolder = CaptureFolder, (string name, Vector3 focus, Vector3 offset)[] previewViews = null)
        {
            using var profile = new VisualPreviewProfile();
            Directory.CreateDirectory(previewFolder);
            var prepared = EditorSceneManager.OpenScene($"Assets/Game/Scenes/OperationMaps/Variants/{previewMap}/PreparedEntities.unity", OpenSceneMode.Single);
            var binding = EditorSceneManager.OpenScene($"Assets/Game/Scenes/OperationMaps/Variants/{previewMap}/RuntimeBinding.unity", OpenSceneMode.Additive);
            SceneManager.SetActiveScene(binding);
            if (freshAtmosphere)
            {
                foreach (var root in binding.GetRootGameObjects().Where(r => r.name == "MapAtmosphere" || r.name.Contains("Atmosphere"))) UnityEngine.Object.DestroyImmediate(root);
                var output = JsonUtility.FromJson<PreparedCandidateOutput>(File.ReadAllText($"Design/MapVariants/Preparation/{previewMap}/Candidate/output-manifest.json"));
                MapVariantAtmosphere.Build(binding, prepared, previewMap, new Rect(output.runtimePlayableMin, output.playableSize));
            }
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
                foreach (var view in previewViews ?? Views)
                {
                    go.transform.position = view.focus + view.offset;
                    go.transform.LookAt(view.focus);
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply();
                    RenderTexture.active = null;
                    string path = $"{previewFolder}/{label}-{view.name}.png";
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

        // The native premium profile supplies mission-distance shadows for art review.
        // This preview does not change the project's mobile quality selection.
        private sealed class VisualPreviewProfile : IDisposable
        {
            private readonly UnityEngine.Rendering.RenderPipelineAsset previous = QualitySettings.renderPipeline;
            public VisualPreviewProfile()
            {
                QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Game/Rendering/PC_Premium_RPAsset.asset");
                Debug.Log("[MapBeautify] captureProfile=PC_Premium_RPAsset scope=VisualReview");
            }
            public void Dispose() => QualitySettings.renderPipeline = previous;
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
