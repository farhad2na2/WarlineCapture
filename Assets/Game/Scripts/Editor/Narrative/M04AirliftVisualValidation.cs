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
    // Art integration evidence: renders the actual passive UI prefab and authored dialogue.
    // This does not simulate campaign progression or replace the Play Mode QA lane.
    public static class M04AirliftVisualValidation
    {
        private const string Output = "Design/AgentReports/M04Airlift/ComicQA";
        private static int captures, lines;

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            M04AirliftMediaImporter.ValidateStableArtImports();
            M04AirliftPresentationBuilder.Build();
            var catalog = Require<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath);
            string previousLocale = GameLocalization.CurrentLocaleCode;
            var sequences = AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Configs/Narrative/Chapter01/M04_Airlift_Narrative.asset")
                .OfType<NarrativeSequenceConfig>().ToArray();
            if(sequences.Length!=3) throw new InvalidOperationException("Expected three M04 sequences.");
            captures = lines = 0;
            try
            {
                foreach (bool accessible in new[]{false,true})
                foreach (int width in new[] { 1920, 2400 })
                foreach (bool persian in new[] { false, true })
                {
                    GameLocalization.SetLocale(persian ? "fa-IR" : "en", false);
                    // NewScene releases unused Unity assets; reacquire the locale for each variant.
                    var resolver = new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(FallbackGameTextResolver.Instance);
                    foreach (var sequence in sequences)
                    foreach (var state in sequence.States.Where(s => s.HasPanelBinding))
                    {
                        var sprite = width == 1920 ? state.Panel16x9 : state.Panel20x9;
                        var reference = width == 1920 ? state.Panel16x9Reference : state.Panel20x9Reference;
                        if (sprite == null && reference != null)
                            sprite = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(reference.AssetGUID)).OfType<Sprite>().Single(s => s.name.EndsWith(width==1920?"16x9":"20x9",StringComparison.Ordinal));
                        if (sprite == null) throw new InvalidOperationException("Missing panel: " + state.StateId);
                        string id = sequence.SequenceId.Replace('.', '_') + "-" + state.StateId+(accessible?"-large-contrast":"-standard");
                        Render(id, width, persian, view =>
                        {
                            view.ApplyLanguage(persian, resolver);
                            view.ApplyPanel(new NarrativePanelPresentationModel { StateId = state.StateId, PanelSprite = sprite, Tint = Color.white });
                            view.SetInteractiveState(NarrativeInteractiveStateKind.None);
                            view.ApplyLocation(default);
                            view.PlaybackControlsView.BindTransport(null, null);
                            view.PlaybackControlsView.ApplyTransport(1, 1, false, true);
                            view.SetSkipState(true, true, resolver.Get("narrative.first_launch.control.skip", "SKIP"));
                            foreach (var line in state.Lines)
                            {
                                var speaker = catalog.Speakers.First(s => s.SpeakerId == line.Speaker);
                                view.DialogueView.ApplySpeaker(new NarrativeSpeakerPresentationModel
                                {
                                    SpeakerId = speaker.SpeakerId, DisplayName = resolver.Get(speaker.NameKey, speaker.NameFallback),
                                    Role = resolver.Get(speaker.RoleKey, speaker.RoleFallback), IdentitySprite = speaker.IdentitySprite,
                                    AccentColor = speaker.AccentColor, Treatment = speaker.Treatment
                                });
                                string text = resolver.Get(line.TextKey, line.EnglishFallback);
                                if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Missing caption " + line.LineId);
                                if (persian && !text.Any(c => c >= '\u0600' && c <= '\u06ff'))
                                    throw new InvalidOperationException("Persian capture contains fallback English: " + line.LineId);
                                var settings=Game.UI.Runtime.SettingsService.Defaults;
                                if(accessible){settings.Narrative.SubtitleSize=UISubtitleSize.ExtraLarge;settings.Accessibility.HighContrastUi=true;settings.Narrative.BackgroundOpacity=UISubtitleBackgroundOpacity.OneHundredPercent;}
                                view.DialogueView.PrepareLine(text, NarrativeSubtitleStyleUtilitySystemHelper.Resolve(settings));
                                view.DialogueView.CompleteLine();
                                Canvas.ForceUpdateCanvases();
                                foreach (var caption in view.DialogueView.GetComponentsInChildren<TMP_Text>())
                                {
                                    caption.ForceMeshUpdate();
                                    if (caption.isTextOverflowing || caption.isTextTruncated)
                                        throw new InvalidOperationException("Caption overflow " + line.LineId + " " + caption.name);
                                }
                                lines++;
                            }
                            if (state.Lines.Count == 0) view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
                        });
                    }
                }
                Debug.Log($"[M04AirliftVisualValidation] result=Passed captures={captures} captionChecks={lines} panels=7 locales=2 aspects=2 subtitleStyles=2");
            }
            finally { GameLocalization.SetLocale(previousLocale, false); }
        }

        private static void Render(string id, int width, bool persian, Action<NarrativeSequenceView> configure)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("ArtReviewCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 1080; camera.aspect = width / 1080f;
            camera.transform.position = new Vector3(0, 0, -100); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(width, 1080, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, 1080, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            var canvasObject = new GameObject("ArtReviewCanvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var canvasRect = (RectTransform)canvas.transform; canvasRect.sizeDelta = new Vector2(width * 2, 2160);
            var instance = Object.Instantiate(Require<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath), canvasRect);
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            try
            {
                var view = instance.GetComponent<NarrativeSequenceView>();
                if (view.ReviewerControlsView != null) view.ReviewerControlsView.gameObject.SetActive(false);
                if (view.SkipConfirmationView != null) view.SkipConfirmationView.gameObject.SetActive(false);
                view.SetVisible(true); Canvas.ForceUpdateCanvases(); configure(view); Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, 1080), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(Output, id + (persian ? "-fa-" : "-en-") + width + ".png"), image.EncodeToPNG());
                captures++;
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null;
                Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(canvasObject); Object.DestroyImmediate(cameraObject);
            }
        }

        private static T Require<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new FileNotFoundException("Missing art review asset: " + path);
    }
}
