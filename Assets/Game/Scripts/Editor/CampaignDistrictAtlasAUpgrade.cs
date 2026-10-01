using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    public static class CampaignDistrictAtlasAUpgrade
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";
        private const string ArtRoot = "Assets/Game/Art/UI/Generated/CampaignOperations/DistrictAtlasA";
        private static readonly Vector2[] CardPositions =
        {
            new(36, 31), new(505, 48), new(19, 248), new(547, 278), new(281, 455)
        };

        [MenuItem("Game/UI/Campaign/Apply District Atlas A")]
        public static void Apply()
        {
            Texture2D[] scenes = new Texture2D[5];
            for (int i = 0; i < scenes.Length; i++)
            {
                string path = $"{ArtRoot}/chapter-{i + 1:00}-district.png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing atlas art: " + path);
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                scenes[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (scenes[i] == null) throw new InvalidOperationException("Failed atlas import: " + path);
            }
            Texture2D firstContact = ImportArtwork(ArtRoot + "/chapter-01-m01-old-market.png");
            Texture2D establishBase = ImportArtwork(ArtRoot + "/chapter-01-m02-forward-post.png");

            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                CampaignOperationsScreenView view = prefab.GetComponentInChildren<CampaignOperationsScreenView>(true);
                if (view == null || view.MissionSelectRoot == null || view.MissionNodeButtons?.Length != 5)
                    throw new InvalidOperationException("Current Campaign screen and mission bindings are required.");

                Transform fullScreen = prefab.transform;
                RawImage backdrop = (view.MissionSelectRoot.Find("DistrictAtlasBackdrop") ??
                    fullScreen.Find("DistrictAtlasBackdrop"))?.GetComponent<RawImage>() ??
                    EnsureRaw("DistrictAtlasBackdrop", fullScreen);
                backdrop.transform.SetParent(fullScreen, false);
                Stretch(backdrop.rectTransform);
                backdrop.transform.SetSiblingIndex(2);
                backdrop.texture = scenes[0];
                backdrop.color = new Color(0.65f, 0.65f, 0.65f, 1f);
                Cover(backdrop, 16f / 9f);
                Image backdropShade = (view.MissionSelectRoot.Find("DistrictAtlasBackdropShade") ??
                    fullScreen.Find("DistrictAtlasBackdropShade"))?.GetComponent<Image>() ??
                    EnsureImage("DistrictAtlasBackdropShade", fullScreen);
                backdropShade.transform.SetParent(fullScreen, false);
                Stretch(backdropShade.rectTransform);
                backdropShade.transform.SetSiblingIndex(3);
                backdropShade.color = new Color(0.01f, 0.025f, 0.03f, 0.56f);
                backdropShade.raycastTarget = false;

                Image[] progressMarks = new Image[25];
                string[] chapterNames = { "FIRST RESPONSE", "BROKEN GRID", "HIDDEN NETWORK", "AIR AND ARMOR", "CITYWIDE COMMAND" };
                for (int i = 0; i < 5; i++)
                {
                    RectTransform chapter = view.ChapterCards[i];
                    Transform clip = chapter.Find("ArtClip");
                    if (clip == null) throw new InvalidOperationException("Chapter art clip missing: " + i);
                    Image oldArt = clip.Find("Art")?.GetComponent<Image>();
                    if (oldArt != null) oldArt.enabled = false;
                    RawImage art = EnsureRaw("AtlasChapterArt", clip);
                    Stretch(art.rectTransform);
                    art.transform.SetAsFirstSibling();
                    art.texture = scenes[i];
                    art.color = Color.white;
                    Cover(art, 16f / 9f);
                    art.raycastTarget = false;
                    Image shade = clip.Find("Shade")?.GetComponent<Image>();
                    if (shade != null) shade.color = i == 0 ? new Color32(64, 39, 5, 210) : new Color32(9, 37, 45, 220);
                    TMP_Text roman = EnsureText("AtlasRoman", chapter, chapter.Find("Title")?.GetComponent<TMP_Text>());
                    SetTopLeft(roman.rectTransform, 9, 12, 70, 82);
                    roman.text = new[] { "I", "II", "III", "IV", "V" }[i];
                    roman.fontSize = 52;
                    roman.alignment = TextAlignmentOptions.Center;
                    roman.color = Color.white;
                    roman.raycastTarget = false;
                    Transform icon = chapter.Find("Icon");
                    if (icon != null)
                    {
                        SetTopLeft((RectTransform)icon, 354, 37, 31, 38);
                        if (i == 0) icon.gameObject.SetActive(false);
                    }
                    TMP_Text title = chapter.Find("Title")?.GetComponent<TMP_Text>();
                    TMP_Text subtitle = chapter.Find("Subtitle")?.GetComponent<TMP_Text>();
                    if (title != null)
                    {
                        SetTopLeft(title.rectTransform, 88, 12, 275, 36);
                        title.text = chapterNames[i];
                        title.fontSize = 23;
                        title.enableAutoSizing = true;
                        title.fontSizeMin = 17;
                        title.fontSizeMax = 23;
                    }
                    if (subtitle != null)
                    {
                        SetTopLeft(subtitle.rectTransform, 88, 49, 275, 23);
                        subtitle.text = "CHAPTER " + new[] { "I", "II", "III", "IV", "V" }[i];
                        subtitle.fontSize = 15;
                    }
                    for (int mission = 0; mission < 5; mission++)
                    {
                        Image mark = EnsureImage("AtlasProgress" + mission, chapter);
                        SetTopLeft(mark.rectTransform, 89 + mission * 28, 85, 18, 9);
                        mark.color = new Color32(93, 113, 118, 230);
                        mark.raycastTarget = false;
                        progressMarks[i * 5 + mission] = mark;
                    }
                }

                Transform mapClip = view.StrategicMap.Find("MapClip");
                if (mapClip == null) throw new InvalidOperationException("District map clip missing.");
                foreach (Image route in view.StrategicMap.GetComponentsInChildren<Image>(true))
                    if (route.name == "Route") route.gameObject.SetActive(false);
                if (view.DistrictMapImage != null)
                {
                    view.DistrictMapImage.texture = scenes[0];
                    Cover(view.DistrictMapImage, 16f / 9f);
                }
                Image mapShade = mapClip.Find("MapShade")?.GetComponent<Image>();
                if (mapShade != null) mapShade.color = new Color(0.015f, 0.03f, 0.035f, .30f);

                SerializedObject serialized = new SerializedObject(view);
                SerializedProperty labelPanels = serialized.FindProperty("nodeLabelPanels");
                SerializedProperty cardArt = serialized.FindProperty("atlasMissionArtwork");
                cardArt.arraySize = 5;
                SerializedProperty chapterArt = serialized.FindProperty("atlasChapterArtwork");
                chapterArt.arraySize = 5;
                SerializedProperty sceneProp = serialized.FindProperty("districtAtlasScenes");
                sceneProp.arraySize = 5;
                serialized.FindProperty("atlasFirstContactArtwork").objectReferenceValue = firstContact;
                serialized.FindProperty("atlasEstablishBaseArtwork").objectReferenceValue = establishBase;
                SerializedProperty progressProp = serialized.FindProperty("atlasChapterProgress");
                progressProp.arraySize = 25;
                for (int i = 0; i < 25; i++)
                    progressProp.GetArrayElementAtIndex(i).objectReferenceValue = progressMarks[i];
                for (int i = 0; i < 5; i++)
                {
                    sceneProp.GetArrayElementAtIndex(i).objectReferenceValue = scenes[i];
                    chapterArt.GetArrayElementAtIndex(i).objectReferenceValue =
                        view.ChapterCards[i].Find("ArtClip/AtlasChapterArt")?.GetComponent<RawImage>();

                    Button button = view.MissionNodeButtons[i];
                    RectTransform rect = button.GetComponent<RectTransform>();
                    SetTopLeft(rect, CardPositions[i].x, CardPositions[i].y, 210, 156);
                    Image cardFace = button.GetComponent<Image>();
                    cardFace.sprite = null;
                    cardFace.color = new Color32(20, 42, 48, 255);
                    cardFace.type = Image.Type.Simple;
                    cardFace.raycastTarget = true;
                    button.targetGraphic = cardFace;
                    button.transition = Selectable.Transition.None;

                    RawImage missionArt = EnsureRaw("AtlasMissionArt", rect);
                    AspectRatioFitter oldFitter = missionArt.GetComponent<AspectRatioFitter>();
                    if (oldFitter != null) UnityEngine.Object.DestroyImmediate(oldFitter);
                    SetTopLeft(missionArt.rectTransform, 3, 42, 204, 110);
                    missionArt.raycastTarget = false;
                    cardArt.GetArrayElementAtIndex(i).objectReferenceValue = missionArt;

                    V3GradientGraphic panel = labelPanels.GetArrayElementAtIndex(i).objectReferenceValue as V3GradientGraphic;
                    if (panel == null) throw new InvalidOperationException("Mission label binding missing: " + i);
                    panel.transform.SetParent(rect, false);
                    SetTopLeft(panel.rectTransform, 3, 3, 204, 37);
                    panel.transform.SetAsLastSibling();
                    panel.raycastTarget = false;
                    TMP_Text id = panel.transform.Find("Id")?.GetComponent<TMP_Text>();
                    TMP_Text name = panel.transform.Find("Name")?.GetComponent<TMP_Text>();
                    if (id != null) { SetTopLeft(id.rectTransform, 7, 4, 42, 29); id.fontSize = 18; id.raycastTarget = false; }
                    if (name != null)
                    {
                        SetTopLeft(name.rectTransform, 49, 3, 151, 31);
                        name.fontSize = 16;
                        name.enableAutoSizing = true;
                        name.fontSizeMin = 12;
                        name.fontSizeMax = 16;
                        name.textWrappingMode = TextWrappingModes.Normal;
                        name.overflowMode = TextOverflowModes.Ellipsis;
                        name.raycastTarget = false;
                    }
                    Image lockIcon = rect.Find("LockIcon")?.GetComponent<Image>();
                    if (lockIcon != null) lockIcon.raycastTarget = false;
                    button.transform.SetAsLastSibling();
                }
                serialized.FindProperty("districtAtlasBackdrop").objectReferenceValue = backdrop;
                Button archive = view.MissionSelectRoot.Find("AtlasStoryArchiveButton")?.GetComponent<Button>();
                if (archive == null)
                {
                    Button source = serialized.FindProperty("footerStoryArchiveButton").objectReferenceValue as Button;
                    if (source == null) throw new InvalidOperationException("Existing Story Archive control missing.");
                    archive = UnityEngine.Object.Instantiate(source, view.MissionSelectRoot);
                    archive.name = "AtlasStoryArchiveButton";
                }
                SetTopLeft(archive.GetComponent<RectTransform>(), 1321, 803, 341, 123);
                Transform archiveIcon = archive.transform.Find("Icon");
                Transform archiveLabel = archive.transform.Find("Label");
                if (archiveIcon != null) SetTopLeft((RectTransform)archiveIcon, 22, 30, 58, 58);
                if (archiveLabel != null)
                {
                    SetTopLeft((RectTransform)archiveLabel, 82, 9, 245, 102);
                    TMP_Text copy = archiveLabel.GetComponent<TMP_Text>();
                    if (copy != null) copy.fontSize = 26;
                }
                archive.transform.SetAsLastSibling();
                serialized.FindProperty("atlasStoryArchiveButton").objectReferenceValue = archive;
                RectTransform launchRect = view.LaunchMissionButton.GetComponent<RectTransform>();
                SetTopLeft(launchRect, 688, 803, 627, 123);
                Transform launchIcon = launchRect.Find("Icon");
                Transform launchLabel = launchRect.Find("Label");
                if (launchIcon != null) SetTopLeft((RectTransform)launchIcon, 48, 33, 64, 58);
                if (launchLabel != null)
                {
                    SetTopLeft((RectTransform)launchLabel, 108, 8, 494, 107);
                    TMP_Text copy = launchLabel.GetComponent<TMP_Text>();
                    if (copy != null) copy.fontSize = 30;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                UpdateResponsiveLayout(view, archive);

                // The atlas spans the header; keep the existing account chrome above it.
                foreach (string name in new[] { "WarlineLogo", "CreditsChip", "CommandChip", "SettingsButton" })
                    view.transform.Find(name)?.SetAsLastSibling();

                // Mission objective copy comes from the selected mission and can be
                // longer than the original CH01 placeholder text.
                foreach (TMP_Text label in view.MissionBriefing.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.name != "Label" || label.transform.parent?.name != "Objective") continue;
                    label.fontSize = 13;
                    label.textWrappingMode = TextWrappingModes.Normal;
                    label.overflowMode = TextOverflowModes.Ellipsis;
                    label.lineSpacing = -2;
                    SetTopLeft(label.rectTransform, 5, 42, label.rectTransform.sizeDelta.x, 44);
                }

                TMP_Text reward = view.RewardSummaryText;
                if (reward != null)
                {
                    reward.fontSize = 17;
                    reward.enableAutoSizing = true;
                    reward.fontSizeMin = 14;
                    reward.fontSizeMax = 17;
                    reward.textWrappingMode = TextWrappingModes.Normal;
                    reward.overflowMode = TextOverflowModes.Ellipsis;
                }
                TMP_Text briefing = view.MissionBriefingText;
                if (briefing != null)
                {
                    briefing.fontSize = 16;
                    briefing.enableAutoSizing = true;
                    briefing.fontSizeMin = 13;
                    briefing.fontSizeMax = 16;
                    briefing.overflowMode = TextOverflowModes.Ellipsis;
                }
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            Validate();
            Debug.Log("[CampaignDistrictAtlasA] result=Passed chapters=5 missionCards=5 preservedBindings=True");
        }

        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            CampaignOperationsScreenView view = prefab?.GetComponentInChildren<CampaignOperationsScreenView>(true);
            if (view == null || view.MissionNodeButtons?.Length != 5 || view.ChapterCards?.Length != 5)
                throw new InvalidOperationException("Campaign mission bindings are incomplete.");
            SerializedObject serialized = new SerializedObject(view);
            if (serialized.FindProperty("districtAtlasScenes").arraySize != 5 ||
                serialized.FindProperty("atlasMissionArtwork").arraySize != 5 ||
                serialized.FindProperty("districtAtlasBackdrop").objectReferenceValue == null)
                throw new InvalidOperationException("District atlas art bindings are incomplete.");
            for (int i = 0; i < 5; i++)
                if (serialized.FindProperty("atlasMissionArtwork").GetArrayElementAtIndex(i).objectReferenceValue == null ||
                    view.MissionNodeButtons[i].GetComponent<RectTransform>().sizeDelta.x < 200f)
                    throw new InvalidOperationException("Mission location card missing: " + i);
            Debug.Log("[CampaignDistrictAtlasA] validation=Passed chapters=5 missionCards=5");
        }

        public static void CaptureQa()
        {
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            Camera camera = new GameObject("AtlasQaCamera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.01f, .025f, .03f);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.cullingMask = -1;
            Canvas canvas = new GameObject("AtlasQaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1672, 941);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(asset, canvas.transform, false);
            RectTransform root = instance.GetComponent<RectTransform>();
            Stretch(root);
            CampaignOperationsScreenView view = instance.GetComponentInChildren<CampaignOperationsScreenView>(true);
            view.ShowMissionSelect();
            MethodInfo apply = typeof(CampaignOperationsScreenView).GetMethod("ApplyDistrictAtlas", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo tab = typeof(CampaignOperationsScreenView).GetMethod("ApplyChapterTabAppearance", BindingFlags.NonPublic | BindingFlags.Static);
            if (apply == null || tab == null) throw new InvalidOperationException("Atlas QA binding missing.");
            string[][] names =
            {
                new[] { "FIRST CONTACT", "ESTABLISH THE BASE", "RADAR WARNING", "AIRLIFT", "BREACH ASSAULT" },
                new[] { "GRIDLOCK", "SUPPLY LINE", "MARKET LIFELINE", "POWER RELAY", "ROUTE REOPENED" },
                new[] { "SIGNAL TRACE", "SAFEHOUSE SWEEP", "FALSE FRONT", "EVIDENCE CHAIN", "NETWORK BREAK" },
                new[] { "AIR CORRIDOR", "STEEL PUSH", "SPLIT FRONT", "GROUNDED SIGNAL", "ARMOR BREAK" },
                new[] { "CITYWIDE ALERT", "TRUST UNDER FIRE", "NETWORK COLLAPSE", "LAST CORRIDOR", "COMMAND NODE" }
            };
            string folder = "/private/tmp/warline-campaign-atlas-a-qa";
            Directory.CreateDirectory(folder);
            for (int chapter = 1; chapter <= 5; chapter++)
            {
                apply.Invoke(view, new object[] { chapter });
                for (int i = 0; i < 5; i++)
                {
                    tab.Invoke(null, new object[] { view.ChapterCards[i].GetComponent<Button>(), i == chapter - 1, true });
                    Transform label = view.MissionNodeButtons[i].transform.Find("MissionLabel/Name");
                    if (label != null) label.GetComponent<TMP_Text>().text = names[chapter - 1][i];
                }
                TMP_Text number = view.MissionNumber;
                number.text = $"CH{chapter:00} · M01";
                SetTopLeft(number.rectTransform, 20, 8, 240, 38);
                view.MissionName.text = names[chapter - 1][0];
                foreach (int width in new[] { 1920, 2400 })
                {
                    const int height = 1080;
                    Game.Editor.MainMenuV3PrefabBuilder.SetGameViewResolution(width, height);
                    RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    RenderTexture prior = RenderTexture.active;
                    camera.targetTexture = target;
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply(false, false);
                    string path = $"{folder}/chapter-{chapter:00}-{width}.png";
                    File.WriteAllBytes(path, image.EncodeToPNG());
                    camera.targetTexture = null;
                    RenderTexture.active = prior;
                    UnityEngine.Object.DestroyImmediate(image);
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            Debug.Log("[CampaignDistrictAtlasA] capture=Passed chapters=5 widths=1920,2400 folder=" + folder);
        }

        private static string Bounds(RectTransform rect)
        {
            if (rect == null) return "missing";
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return $"{corners[0].x:F1},{corners[0].y:F1}..{corners[2].x:F1},{corners[2].y:F1}";
        }

        private static void UpdateResponsiveLayout(CampaignOperationsScreenView view, Button archive)
        {
            MainMenuV3SectionLayoutView layout = view.GetComponent<MainMenuV3SectionLayoutView>();
            if (layout == null) throw new InvalidOperationException("Campaign responsive layout is missing.");
            SerializedObject data = new SerializedObject(layout);
            SerializedProperty centers = data.FindProperty("centerAnchoredTargets");
            SerializedProperty centerBases = data.FindProperty("centerTargetBasePositions");
            var kept = new List<(RectTransform target, Vector2 position)>();
            for (int i = 0; i < centers.arraySize; i++)
            {
                RectTransform target = centers.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform;
                if (target == null) continue;
                if (target.name == "MissionLabel") continue; // The label now moves with its card.
                int cardIndex = Array.IndexOf(view.MissionNodes, target);
                Vector2 position = cardIndex >= 0 ? new Vector2(CardPositions[cardIndex].x, -CardPositions[cardIndex].y) :
                    target == view.LaunchMissionLabel.rectTransform ? new Vector2(108, -8) :
                    target == view.LaunchMissionButton.transform.Find("Icon") ? new Vector2(48, -33) :
                    centerBases.GetArrayElementAtIndex(i).vector2Value;
                kept.Add((target, position));
            }
            centers.arraySize = centerBases.arraySize = kept.Count;
            for (int i = 0; i < kept.Count; i++)
            {
                centers.GetArrayElementAtIndex(i).objectReferenceValue = kept[i].target;
                centerBases.GetArrayElementAtIndex(i).vector2Value = kept[i].position;
            }

            SerializedProperty widths = data.FindProperty("widthExpandedTargets");
            SerializedProperty widthBases = data.FindProperty("widthTargetBaseSizes");
            for (int i = 0; i < widths.arraySize; i++)
                if (widths.GetArrayElementAtIndex(i).objectReferenceValue == view.LaunchMissionButton.GetComponent<RectTransform>())
                    widthBases.GetArrayElementAtIndex(i).vector2Value = new Vector2(627, 123);

            SerializedProperty rights = data.FindProperty("rightAnchoredTargets");
            SerializedProperty rightBases = data.FindProperty("rightTargetBasePositions");
            RectTransform archiveRect = archive.GetComponent<RectTransform>();
            bool found = false;
            for (int i = 0; i < rights.arraySize; i++)
                if (rights.GetArrayElementAtIndex(i).objectReferenceValue == archiveRect) found = true;
            if (!found)
            {
                int last = rights.arraySize;
                rights.arraySize = rightBases.arraySize = last + 1;
                rights.GetArrayElementAtIndex(last).objectReferenceValue = archiveRect;
                rightBases.GetArrayElementAtIndex(last).vector2Value = new Vector2(1321, -803);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);
        }

        public static void ApplyAndCaptureQa()
        {
            Apply();
            CaptureQa();
        }

        private static RawImage EnsureRaw(string name, Transform parent)
        {
            Transform child = parent.Find(name);
            GameObject go = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform), typeof(RawImage));
            if (child == null) go.transform.SetParent(parent, false);
            return go.GetComponent<RawImage>();
        }
        private static Texture2D ImportArtwork(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing mission artwork: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Image EnsureImage(string name, Transform parent)
        {
            Transform child = parent.Find(name);
            GameObject go = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            if (child == null) go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }
        private static TMP_Text EnsureText(string name, Transform parent, TMP_Text template)
        {
            Transform child = parent.Find(name);
            if (child != null) return child.GetComponent<TMP_Text>();
            if (template == null) throw new InvalidOperationException("Chapter title font missing.");
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            return text;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }
        private static void Cover(RawImage image, float ratio)
        {
            AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = ratio;
        }
    }
}
