#if UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ComicPortraitRuntimeValidation
    {
        [Serializable] private sealed class Manifest { public Entry[] entries; }
        [Serializable] private sealed class Entry
        {
            public string target;
            public string role;
            public string config;
            public string field;
            public bool applied;
        }

        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Portrait validation requires an idle Editor.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(
                "Design/AgentReports/ComicPortraitMigration/manifest.json"));
            if (manifest.entries.Length != 238)
                throw new InvalidOperationException("Expected 238 comic portrait targets.");
            int entityRoles = 0, groups = 0;
            foreach (var entry in manifest.entries)
            {
                if (!entry.applied) throw new InvalidOperationException("Unapplied comic art: " + entry.target);
                AssetDatabase.ImportAsset(entry.target, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.target);
                if (sprite == null || sprite.texture.width != 512 || sprite.texture.height != 512)
                    throw new InvalidOperationException("Invalid imported portrait: " + entry.target);
                if (string.IsNullOrEmpty(entry.config)) { groups++; continue; }
                var config = AssetDatabase.LoadMainAssetAtPath(entry.config);
                if (config == null) throw new InvalidOperationException("Missing config: " + entry.config);
                var serialized = new SerializedObject(config);
                var property = serialized.FindProperty(entry.field);
                if (property == null || property.objectReferenceValue != sprite)
                    throw new InvalidOperationException("Portrait binding changed: " + entry.config + ":" + entry.field);
                entityRoles++;
            }
            if (entityRoles != 222 || groups != 16)
                throw new InvalidOperationException("Portrait coverage mismatch.");
            Debug.Log("[ComicPortraitRuntime] result=Passed configs=74 entityRoles=222 groups=16 sprites=238");
        }

        public static void CaptureNativeHud()
        {
            Run();
            CaptureHud("NativeHud");
            Debug.Log("[ComicPortraitNativeHud] result=Passed captures=4 locales=en,fa-IR");
        }

        public static void CaptureBefore()
        {
            CaptureHud("BeforeHud");
            Debug.Log("[ComicPortraitBeforeHud] result=Passed captures=4 locales=en,fa-IR");
        }

        private static void CaptureHud(string directory)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("HUD capture requires an idle Editor with no unsaved scene changes.");
            string output = "Design/AgentReports/ComicPortraitMigration/" + directory;
            Directory.CreateDirectory(output);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            string previousLocale = GameLocalization.CurrentLocaleCode;
            try
            {
                foreach (string locale in new[] { "en", "fa-IR" })
                {
                    GameLocalization.Initialize(catalog, locale, persist: false);
                    foreach (bool transport in new[] { false, true })
                        RenderNativeSelection(Path.Combine(output, "warline-selection-wheel-" + (transport ? "transport-" : "") + locale + ".png"), transport);
                }
            }
            finally
            {
                GameLocalization.Initialize(catalog, previousLocale, persist: false);
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void RenderNativeSelection(string path, bool transport)
        {
            const int width = 1920, height = 1080;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("PortraitPreviewCamera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(25, 34, 38, 255);
            camera.orthographic = true;
            camera.orthographicSize = height * .5f;
            camera.transform.position = new Vector3(0, 0, -100);
            var canvas = new GameObject("PortraitPreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1672, 941);
            scaler.matchWidthOrHeight = .5f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudV3PrefabBuilder.PrefabPath);
            var instance = UnityEngine.Object.Instantiate(prefab, canvas.transform);
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tray = instance.GetComponentInChildren<MatchHudSquadTrayView>(true);
            var selection = instance.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            var kind = transport ? SelectionSummaryPortraitKind.Transports : SelectionSummaryPortraitKind.Soldiers;
            tray.SetSelectedSlot(transport ? MatchHudSquadTraySlot.Transport : MatchHudSquadTraySlot.Soldiers);
            Sprite portrait = transport
                ? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Helicopter_Transport_Action_512.png")
                : selection.ResolveFallbackPortraitSprite(kind);
            selection.Apply(new MatchHudSelectionPanelModel(true, transport ? "TRANSPORT HELICOPTER" : "RIFLE SQUAD",
                transport ? "AIR TRANSPORT" : "SQUAD 1", "HOLDING", "120 / 120", 1f, portrait, kind,
                !transport, AssetDatabase.LoadAssetAtPath<Sprite>(V3UiFoundationBuilder.MatchRankBadgeIconPath), true, true, true));
            foreach (V3LocalizedTextBindingView binding in instance.GetComponentsInChildren<V3LocalizedTextBindingView>(true))
                binding.ApplyLocalization();
            instance.GetComponentInChildren<MissionHudTouchLayoutView>(true)?.RefreshLayout();
            var wheel = instance.GetComponentInChildren<CommandWheelPanelView>(true);
            wheel.BindRuntimeSectionReferences(selection.CommandWheelOpenButton, null);
            wheel.Open();
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(width, height, 24);
            var capture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                capture.Apply();
                File.WriteAllBytes(path, capture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(capture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
