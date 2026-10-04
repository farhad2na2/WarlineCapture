#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Catalog.Contracts;
using Game.Composition;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class CampaignComicSpeakerPortraitBuilder
    {
        private const string Root = "Assets/Game/Art/UI/Portraits/Generated/";
        private const string Output = "Design/AgentReports/CampaignComicSpeakerPortraits";
        private static readonly NarrativeSpeakerId[] Targets = { NarrativeSpeakerId.Qassem, NarrativeSpeakerId.Karim, NarrativeSpeakerId.Yusuf };

        public static void InstallAndValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires Edit mode.");
            Directory.CreateDirectory(Output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath);
            var data = new SerializedObject(catalog);
            var entries = data.FindProperty("speakers");
            foreach (var id in Targets)
            {
                var path = Root + "Portrait_" + id + "_CampaignComic.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new InvalidOperationException(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.isReadable = false;
                importer.alphaIsTransparency = true; importer.maxTextureSize = 1024;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidOperationException(path);
                bool found = false;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("speakerId").intValue != (int)id) continue;
                    entry.FindPropertyRelative("identitySprite").objectReferenceValue = sprite;
                    found = true;
                }
                if (!found) throw new InvalidOperationException("Speaker missing: " + id);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);
            InstallPersianIdentities();
            ValidateAndCapture();
        }

        private static void InstallPersianIdentities()
        {
            var ui = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var serialized = new SerializedObject(ui);
            var locales = serialized.FindProperty("locales");
            for (int i = 0; i < locales.arraySize; i++)
            {
                var locale = locales.GetArrayElementAtIndex(i);
                var code = locale.FindPropertyRelative("localeCode").stringValue;
                if (code != "en" && code != "fa-IR") continue;
                var entries = locale.FindPropertyRelative("entries");
                foreach (var copy in CampaignComicSpeakerIdentityCopy.Entries)
                    Upsert(entries, copy.Key, code == "en" ? copy.English : copy.Persian);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(ui);
            var narrative = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(FirstLaunchNarrativeConfigBuilder.PersianLocalePath);
            var data = new SerializedObject(narrative);
            foreach (var copy in CampaignComicSpeakerIdentityCopy.Entries) Upsert(data.FindProperty("text"), copy.Key, copy.Persian);
            data.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(narrative);
            GameLocalization.Initialize(ui, GameLocalization.CurrentLocaleCode, persist: false);
        }

        private static void Upsert(SerializedProperty entries, string key, string value)
        {
            SerializedProperty target = null;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key) target = entries.GetArrayElementAtIndex(i);
            target ??= entries.GetArrayElementAtIndex(entries.arraySize++);
            target.FindPropertyRelative("key").stringValue = key;
            target.FindPropertyRelative("value").stringValue = value;
        }

        public static void ValidateAndCapture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires Edit mode.");
            Directory.CreateDirectory(Output);
            var catalog = AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath);
            var sequenceAssets = AssetDatabase.FindAssets("t:NarrativeSequenceConfig", new[] { "Assets/Game/Configs/Narrative" })
                .Select(AssetDatabase.GUIDToAssetPath).Distinct()
                .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<NarrativeSequenceConfig>()).ToArray();
            string locale = GameLocalization.CurrentLocaleCode;
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool dirty = active.isDirty;
            int lines = 0, captures = 0;
            try
            {
                foreach (var id in Targets)
                {
                    var speaker = catalog.Speakers.Single(s => s.SpeakerId == id);
                    var path = Root + "Portrait_" + id + "_CampaignComic.png";
                    if (AssetDatabase.GetAssetPath(speaker.IdentitySprite) != path) throw new InvalidOperationException("Fallback remains: " + id);
                    var matches = sequenceAssets.SelectMany(s => s.States.SelectMany(state => state.Lines.Where(l => l.Speaker == id).Select(line => (state, line)))).ToArray();
                    if (matches.Length == 0) throw new InvalidOperationException("No campaign dialogue: " + id);
                    lines += matches.Length;
                    foreach (var lang in new[] { "en", "fa-IR" })
                    {
                        GameLocalization.SetLocale(lang, false);
                        var resolver = new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(FallbackGameTextResolver.Instance);
                        string expected = CampaignComicSpeakerIdentityCopy.Entries.Single(e => e.Key == speaker.NameKey).Persian;
                        if (lang == "fa-IR" && resolver.Get(speaker.NameKey, speaker.NameFallback) != expected)
                            throw new InvalidOperationException("Persian name fallback: " + id);
                        if (lang == "fa-IR" && !resolver.Get(speaker.RoleKey, speaker.RoleFallback).Any(c => c >= '\u0600' && c <= '\u06ff'))
                            throw new InvalidOperationException("Persian role fallback: " + id);
                        Capture(speaker, matches[0].state, matches[0].line, lang);
                        captures++;
                    }
                }
                var handPanelPath = "Assets/Game/Resources/FutureMissionComics/CH04M03_SplitFront_Comms.png";
                var handPanelGuid = AssetDatabase.AssetPathToGUID(handPanelPath);
                AssetDatabase.ImportAsset(handPanelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var handState = sequenceAssets.SelectMany(sequence => sequence.States)
                    .First(state => state.Panel16x9Reference != null && state.Panel16x9Reference.AssetGUID == handPanelGuid);
                var handLine = handState.Lines.First();
                var handSpeaker = catalog.Speakers.Single(s => s.SpeakerId == handLine.Speaker);
                foreach (var lang in new[] { "en", "fa-IR" })
                {
                    GameLocalization.SetLocale(lang, false);
                    Capture(handSpeaker, handState, handLine, lang, "DaliaHand-" + lang);
                    captures++;
                }
            }
            finally { GameLocalization.SetLocale(locale, false); }
            if (active != UnityEngine.SceneManagement.SceneManager.GetActiveScene() || dirty != active.isDirty)
                throw new InvalidOperationException("Active scene changed.");
            Debug.Log($"[CampaignComicSpeakerPortraits] result=Passed speakers=3 campaignLines={lines} captures={captures} locales=en,fa-IR fallbackUnitPortraits=0 activeScene=Preserved fullMission=NotClaimed");
        }

        private static void Capture(NarrativeSpeakerRecord speaker, NarrativeStateRecord state, NarrativeDialogueLineRecord line, string locale, string captureName = null)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var cameraObject = new GameObject("ComicPortraitCamera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene; camera.orthographic = true; camera.orthographicSize = 1080;
            camera.aspect = 1920f / 1080; camera.transform.position = new Vector3(0, 0, -100);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var canvasObject = new GameObject("ComicPortraitCanvas", typeof(RectTransform), typeof(Canvas));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var canvasRect = (RectTransform)canvas.transform; canvasRect.sizeDelta = new Vector2(3840, 2160);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            var instance = Object.Instantiate(prefab, canvasRect);
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var target = new RenderTexture(1920, 1080, 24); var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var view = instance.GetComponent<NarrativeSequenceView>();
                view.ReviewerControlsView?.gameObject.SetActive(false); view.SkipConfirmationView?.gameObject.SetActive(false);
                view.SetVisible(true);
                var resolver = new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(FallbackGameTextResolver.Instance);
                view.ApplyLanguage(locale == "fa-IR", resolver);
                Sprite panel = state.Panel16x9;
                if (panel == null && state.Panel16x9Reference != null)
                {
                    var path = AssetDatabase.GUIDToAssetPath(state.Panel16x9Reference.AssetGUID);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                    panel = sprites.FirstOrDefault(s => s.name.EndsWith("16x9", StringComparison.Ordinal)) ?? sprites.FirstOrDefault();
                }
                if (panel == null) throw new InvalidOperationException("Panel missing: " + state.StateId);
                view.ApplyPanel(new NarrativePanelPresentationModel { StateId = state.StateId, PanelSprite = panel, Tint = Color.white });
                view.SetInteractiveState(NarrativeInteractiveStateKind.None); view.ApplyLocation(default);
                view.PlaybackControlsView.BindTransport(null, null); view.PlaybackControlsView.ApplyTransport(1, 1, false, true);
                view.DialogueView.ApplySpeaker(new NarrativeSpeakerPresentationModel { SpeakerId = speaker.SpeakerId,
                    DisplayName = resolver.Get(speaker.NameKey, speaker.NameFallback), Role = resolver.Get(speaker.RoleKey, speaker.RoleFallback),
                    IdentitySprite = speaker.IdentitySprite, AccentColor = speaker.AccentColor, Treatment = speaker.Treatment });
                var text = resolver.Get(line.TextKey, line.EnglishFallback);
                if (locale == "fa-IR" && !text.Any(c => c >= '\u0600' && c <= '\u06ff')) throw new InvalidOperationException("Persian fallback: " + line.LineId);
                view.DialogueView.PrepareLine(text, NarrativeSubtitleStyleUtilitySystemHelper.Resolve(Game.UI.Runtime.SettingsService.Defaults));
                view.DialogueView.CompleteLine(); Canvas.ForceUpdateCanvases();
                if (!view.DialogueView.GetComponentsInChildren<Image>().Any(i => i.sprite == speaker.IdentitySprite && i.gameObject.activeInHierarchy))
                    throw new InvalidOperationException("Dedicated portrait not visible.");
                foreach (var caption in view.DialogueView.GetComponentsInChildren<TMP_Text>())
                { caption.ForceMeshUpdate(); if (caption.isTextOverflowing || caption.isTextTruncated) throw new InvalidOperationException("Caption overflow: " + caption.name); }
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "/" + (captureName ?? speaker.SpeakerId + "-" + locale) + ".png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null; Object.DestroyImmediate(image);
                target.Release(); Object.DestroyImmediate(target); EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
#endif
