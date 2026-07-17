using System;
using System.Collections.Generic;
using System.IO;
using Game.Catalog.Contracts;
using Game.Narrative.Contracts;
using Game.UI.Runtime;
using Game.Configs;
using Game.UI.Contracts;
using RTLTMPro;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class FirstLaunchNarrativePresentationPrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab";
        public const string LanguageChoicePrefabPath = "Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchLanguageChoice.prefab";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string PersianFontSourcePath = "Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Regular.ttf";
        private const string PersianFontAssetPath = "Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Narrative SDF.asset";
        private const string HudPanelPath = "Assets/Game/Art/UI/Panels/scn09_panel_detail_tall_frame.png";
        private const string HudButtonPath = "Assets/Game/Art/UI/Panels/scn09_panel_secondary_button_bg.png";
        private const string HudPrimaryButtonPath = "Assets/Game/Art/UI/Panels/scn09_panel_gold_action_button_bg.png";
        private const float LiveMenuPresentationScale = 2.2f;
        private static readonly Vector2 LiveMenuReferenceResolution = new(4800f, 2160f);

        [MenuItem("Game/Narrative/First Launch/Build Presentation Prefab")]
        public static void Build()
        {
            FirstLaunchNarrativeDialogueAssetImporter.Configure();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            TMP_FontAsset bold = RequireAsset<TMP_FontAsset>(BoldFontPath);
            TMP_FontAsset medium = RequireAsset<TMP_FontAsset>(MediumFontPath);
            TMP_FontAsset persian = GetOrCreatePersianFontAsset();
            Sprite frame = RequireAsset<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.FramePath);
            Sprite pointer = RequireAsset<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.PointerPath);
            Sprite hudPanel = RequireAsset<Sprite>(HudPanelPath);
            Sprite hudButton = RequireAsset<Sprite>(HudButtonPath);
            Sprite hudPrimaryButton = RequireAsset<Sprite>(HudPrimaryButtonPath);
            Sprite locationFrame = RequireAsset<Sprite>("Assets/Game/Art/UI/Panels/scn08_title_banner_frame.png");

            RectTransform root = CreateRect("FirstLaunchNarrativeSequence", null);
            Stretch(root);
            CanvasGroup rootGroup = root.gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
            NarrativeSequenceView sequenceView = root.gameObject.AddComponent<NarrativeSequenceView>();
            AudioSource voiceSource = root.gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;
            NarrativeSequenceAudioView sequenceAudioView = root.gameObject.AddComponent<NarrativeSequenceAudioView>();
            AudioSource musicSource = CreateAudioSource("NarrativeMusic", root, true);
            AudioSource ambienceSource = CreateAudioSource("NarrativeAmbience", root, true);
            AudioSource vehicleSource = CreateAudioSource("NarrativeVehicle", root, true);
            AudioSource eventSource = CreateAudioSource("NarrativeEvents", root, false);
            SetObject(sequenceAudioView, "musicSource", musicSource);
            SetObject(sequenceAudioView, "ambienceSource", ambienceSource);
            SetObject(sequenceAudioView, "vehicleSource", vehicleSource);
            SetObject(sequenceAudioView, "eventSource", eventSource);
            const string environment = "Assets/Game/Audio/Narrative/FirstLaunch/Environment/";
            SetObject(sequenceAudioView, "briefingMusic", RequireAsset<AudioClip>(environment + "first_launch_story_calm_loop_01.wav"));
            SetObject(sequenceAudioView, "conflictMusic", RequireAsset<AudioClip>(environment + "first_launch_story_crisis_loop_01.wav"));
            SetObject(sequenceAudioView, "cityDayAmbience", RequireAsset<AudioClip>(environment + "first_launch_city_market_loop_01.wav"));
            SetObject(sequenceAudioView, "cityConflictAmbience", RequireAsset<AudioClip>(environment + "first_launch_command_room_loop_01.wav"));
            SetObject(sequenceAudioView, "battlefieldAmbience", RequireAsset<AudioClip>(environment + "first_launch_city_attack_loop_01.wav"));
            SetObject(sequenceAudioView, "vehicleEngine", RequireAsset<AudioClip>(environment + "first_launch_convoy_interior_loop_01.wav"));
            SetObject(sequenceAudioView, "attackCue", RequireAsset<AudioClip>(environment + "first_launch_distant_attack_event_01.wav"));
            SetObject(sequenceAudioView, "smallArmsCue", null);
            SetObject(sequenceAudioView, "radioCue", RequireAsset<AudioClip>(environment + "first_launch_radio_emergency_event_01.wav"));
            SetObject(sequenceAudioView, "blackoutCue", null);
            SetObject(sequenceAudioView, "ariaBootCue", RequireAsset<AudioClip>("Assets/Game/Audio/Gameplay/game_command_scan_targeting_01.wav"));
            SetObject(sequenceAudioView, "transitionCue", null);

            Image panel = CreateImage("Panel", root, null, Color.white, false);
            Stretch(panel.rectTransform);
            panel.preserveAspect = false;
            AspectRatioFitter panelAspectFitter = panel.gameObject.AddComponent<AspectRatioFitter>();
            panelAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            panelAspectFitter.aspectRatio = 16f / 9f;

            RectTransform safeArea = CreateRect("SafeArea", root);
            safeArea.anchorMin = new Vector2(0.04f, 0.055f);
            safeArea.anchorMax = new Vector2(0.96f, 0.945f);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            GameObject safeAreaPreview = BuildSafeAreaPreview(safeArea);
            NarrativeLocationIntroView locationIntroView = BuildLocationIntroSurface(safeArea, locationFrame, bold, medium);

            RectTransform dialogue = CreateRect("Dialogue", safeArea);
            dialogue.anchorMin = new Vector2(0.5f, 0f);
            dialogue.anchorMax = new Vector2(0.5f, 0f);
            dialogue.pivot = new Vector2(0.5f, 0f);
            dialogue.sizeDelta = new Vector2(1540f, 292f);
            dialogue.anchoredPosition = new Vector2(0f, 28f);
            ApplyLiveMenuScale(dialogue);
            CanvasGroup dialogueGroup = dialogue.gameObject.AddComponent<CanvasGroup>();
            NarrativeDialogueView dialogueView = dialogue.gameObject.AddComponent<NarrativeDialogueView>();

            Image inputSurface = CreateImage("InputSurface", dialogue, null, new Color(0f, 0f, 0f, 0f), true);
            Stretch(inputSurface.rectTransform);
            Button inputButton = inputSurface.gameObject.AddComponent<Button>();
            inputButton.transition = Selectable.Transition.None;

            Image pointerImage = CreateImage("Pointer", dialogue, pointer, Color.white, false);
            SetRect(pointerImage.rectTransform, new Vector2(1f, 0.45f), new Vector2(1f, 0.45f), new Vector2(145f, 116f), new Vector2(58f, 0f));
            pointerImage.preserveAspect = true;
            pointerImage.gameObject.SetActive(false);

            // Keep the pointer below the body so transparent edge pixels cannot overpaint the frame seam.
            Image frameImage = CreateImage("Frame", dialogue, frame, Color.white, false);
            Stretch(frameImage.rectTransform);
            frameImage.type = Image.Type.Sliced;

            Image portrait = CreateImage("Portrait", dialogue, null, Color.white, false);
            SetRect(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, 220f), new Vector2(160f, 0f));
            portrait.preserveAspect = true;

            Image ariaIcon = CreateImage("AriaIcon", dialogue, null, new Color(0.2f, 0.92f, 1f, 1f), false);
            SetRect(ariaIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, 220f), new Vector2(160f, 0f));
            ariaIcon.preserveAspect = true;
            ariaIcon.gameObject.SetActive(false);

            TMP_Text speakerName = CreateText("SpeakerName", dialogue, "DALIA RAHIM", bold, 54f, TextAlignmentOptions.Left, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetTopStretchOffsets(speakerName.rectTransform, 290f, 176f, 63f, 55f);
            TMP_Text speakerRole = CreateText("SpeakerRole", dialogue, "JRC FIELD COMMAND", medium, 30f, TextAlignmentOptions.Left, new Color(0.1f, 0.48f, 0.52f, 1f));
            SetTopStretchOffsets(speakerRole.rectTransform, 290f, 176f, 110f, 42f);
            TMP_Text line = CreateText("DialogueText", dialogue, "", medium, 50f, TextAlignmentOptions.TopLeft, new Color(0.07f, 0.065f, 0.055f, 1f));
            SetStretchOffsets(line.rectTransform, new Vector2(290f, 78f), new Vector2(-176f, -155f));
            line.textWrappingMode = TextWrappingModes.Normal;
            line.enableAutoSizing = false;
            line.overflowMode = TextOverflowModes.Overflow;

            TMP_Text accessibility = CreateText("AccessibilityText", dialogue, "", medium, 1f, TextAlignmentOptions.Left, Color.clear);
            accessibility.raycastTarget = false;
            Stretch(accessibility.rectTransform);

            TMP_Text advance = CreateText("AdvanceIndicator", dialogue, ">", bold, 32f, TextAlignmentOptions.Center, new Color(0.11f, 0.1f, 0.08f, 1f));
            SetRect(advance.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(52f, 52f), new Vector2(-75f, 54f));
            advance.gameObject.SetActive(false);

            NarrativeCommanderIdentityView identityView = BuildIdentitySurface(safeArea, hudPanel, hudButton, hudPrimaryButton, bold, medium);
            NarrativeGuidanceChoiceView guidanceView = BuildGuidanceSurface(safeArea, hudPanel, hudButton, hudPrimaryButton, bold, medium);
            NarrativeSkipConfirmationView skipConfirmationView = BuildSkipConfirmationSurface(safeArea, hudPanel, hudButton, hudPrimaryButton, bold, medium);

            RectTransform controls = CreateRect("PlaybackControls", safeArea);
            controls.anchorMin = new Vector2(1f, 1f);
            controls.anchorMax = new Vector2(1f, 1f);
            controls.pivot = new Vector2(1f, 1f);
            controls.sizeDelta = new Vector2(224f, 92f);
            controls.anchoredPosition = new Vector2(-8f, -8f);
            ApplyLiveMenuScale(controls);
            CanvasGroup skipGroup = controls.gameObject.AddComponent<CanvasGroup>();
            NarrativePlaybackControlsView controlsView = controls.gameObject.AddComponent<NarrativePlaybackControlsView>();
            Image skipBacking = CreateImage("SkipButton", controls, hudButton, Color.white, true);
            Stretch(skipBacking.rectTransform);
            skipBacking.type = Image.Type.Sliced;
            Button skipButton = skipBacking.gameObject.AddComponent<Button>();
            TMP_Text skipLabel = CreateText("Label", skipBacking.transform, "SKIP  >", bold, 38f, TextAlignmentOptions.Center, new Color(0.94f, 0.91f, 0.78f, 1f));
            Stretch(skipLabel.rectTransform);
            NarrativeReviewerControlsView reviewerView = BuildReviewerSurface(safeArea, hudPanel, hudButton, bold, medium);

            SetObject(dialogueView, "dialogueGroup", dialogueGroup);
            SetObject(dialogueView, "dialogueRect", dialogue);
            SetObject(dialogueView, "frameImage", frameImage);
            SetObject(dialogueView, "pointerImage", pointerImage);
            SetObject(dialogueView, "portraitImage", portrait);
            SetObject(dialogueView, "ariaIconImage", ariaIcon);
            SetObject(dialogueView, "speakerNameText", speakerName);
            SetObject(dialogueView, "speakerRoleText", speakerRole);
            SetObject(dialogueView, "dialogueText", line);
            SetObject(dialogueView, "accessibilityText", accessibility);
            SetObject(dialogueView, "advanceIndicator", advance.gameObject);
            SetObject(dialogueView, "inputButton", inputButton);

            SetObject(controlsView, "skipGroup", skipGroup);
            SetObject(controlsView, "skipButton", skipButton);
            SetObject(controlsView, "skipLabel", skipLabel);

            SetObject(sequenceView, "rootGroup", rootGroup);
            SetObject(sequenceView, "panelImage", panel);
            SetObject(sequenceView, "panelAspectFitter", panelAspectFitter);
            SetObject(sequenceView, "panelMotionRoot", panel.rectTransform);
            SetObject(sequenceView, "dialogueView", dialogueView);
            SetObject(sequenceView, "locationIntroView", locationIntroView);
            SetObject(sequenceView, "playbackControls", controlsView);
            SetObject(sequenceView, "commanderIdentityView", identityView);
            SetObject(sequenceView, "guidanceChoiceView", guidanceView);
            SetObject(sequenceView, "skipConfirmationView", skipConfirmationView);
            SetObject(sequenceView, "reviewerControlsView", reviewerView);
            SetObject(sequenceView, "safeAreaPreview", safeAreaPreview);
            SetObject(sequenceView, "voiceSource", voiceSource);
            SetObject(sequenceView, "sequenceAudioView", sequenceAudioView);
            SetObject(sequenceView, "persianFont", persian);
            SetLocalizedInterfaceBindings(sequenceView, root);
            skipConfirmationView.transform.SetAsLastSibling();

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);
            AssetDatabase.SaveAssets();
            BuildLanguageChoicePrefab(bold, persian, hudPanel, hudButton);
            Debug.Log($"[FirstLaunchNarrativePresentationPrefabBuilder] Built {PrefabPath}");
        }

        private static void BuildLanguageChoicePrefab(
            TMP_FontAsset englishFont,
            TMP_FontAsset persianFont,
            Sprite panel,
            Sprite buttonFrame)
        {
            RectTransform root = CreateRect("FirstLaunchLanguageChoice", null);
            Stretch(root);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            FirstLaunchLanguageChoiceView view = root.gameObject.AddComponent<FirstLaunchLanguageChoiceView>();

            Image dim = CreateImage("Dim", root, null, new Color(0.015f, 0.025f, 0.03f, 0.94f), true);
            Stretch(dim.rectTransform);
            RectTransform surface = CreateRect("LanguageSurface", root);
            SetRect(surface, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1120f, 560f), Vector2.zero);
            ApplyLiveMenuScale(surface);
            Image backing = surface.gameObject.AddComponent<Image>();
            backing.sprite = panel;
            backing.type = Image.Type.Sliced;

            TMP_Text title = CreateText("Title", surface, "SELECT STORY LANGUAGE", englishFont, 46f, TextAlignmentOptions.Center, new Color(0.96f, 0.78f, 0.3f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(880f, 70f), new Vector2(0f, -118f));
            TMP_Text persianTitle = CreateText("PersianTitle", surface, "زبان داستان را انتخاب کنید", persianFont, 38f, TextAlignmentOptions.Center, new Color(0.9f, 0.88f, 0.8f, 1f));
            SetRect(persianTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(880f, 62f), new Vector2(0f, -180f));

            Button englishButton = CreateFramedButton("EnglishButton", surface, buttonFrame, englishFont, "ENGLISH", new Vector2(390f, 136f));
            SetRect(englishButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(390f, 136f), new Vector2(-225f, 120f));
            Button persianButton = CreateFramedButton("PersianButton", surface, buttonFrame, persianFont, "فارسی", new Vector2(390f, 136f));
            SetRect(persianButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(390f, 136f), new Vector2(225f, 120f));

            SetObject(view, "group", group);
            SetObject(view, "englishButton", englishButton);
            SetObject(view, "persianButton", persianButton);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, LanguageChoicePrefabPath);
            Object.DestroyImmediate(root.gameObject);
            AssetDatabase.SaveAssets();
        }

        private static TMP_FontAsset GetOrCreatePersianFontAsset()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PersianFontAssetPath);
            if (existing != null)
            {
                existing.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                existing.ClearFontAssetData();
                ConfigurePersianFontFallback(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Font source = RequireAsset<Font>(PersianFontSourcePath);
            TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(source);
            created.name = "NotoSansArabic-Narrative SDF";
            created.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            created.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(created, PersianFontAssetPath);
            if (created.material != null)
                AssetDatabase.AddObjectToAsset(created.material, created);
            Texture2D[] atlases = created.atlasTextures;
            for (int i = 0; i < atlases.Length; i++)
            {
                if (atlases[i] != null)
                    AssetDatabase.AddObjectToAsset(atlases[i], created);
            }
            ConfigurePersianFontFallback(created);
            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static void ConfigurePersianFontFallback(TMP_FontAsset font)
        {
            TMP_FontAsset latinFallback = RequireAsset<TMP_FontAsset>(MediumFontPath);
            if (!font.fallbackFontAssetTable.Contains(latinFallback))
                font.fallbackFontAssetTable.Add(latinFallback);
        }

        private static void SetLocalizedInterfaceBindings(NarrativeSequenceView view, RectTransform root)
        {
            TMP_Text[] targets =
            {
                FindText(root, "SafeArea/PlaybackControls/SkipButton/Label"),
                FindText(root, "SafeArea/CommanderIdentitySurface/Title"),
                FindText(root, "SafeArea/CommanderIdentitySurface/Instruction"),
                FindText(root, "SafeArea/CommanderIdentitySurface/CallsignLabel"),
                FindText(root, "SafeArea/CommanderIdentitySurface/ContinueButton/Label"),
                FindText(root, "SafeArea/GuidanceChoiceSurface/Title"),
                FindText(root, "SafeArea/GuidanceChoiceSurface/Instruction"),
                FindText(root, "SafeArea/GuidanceChoiceSurface/FullGuidanceButton/Label"),
                FindText(root, "SafeArea/GuidanceChoiceSurface/ContextualGuidanceButton/Label"),
                FindText(root, "SafeArea/GuidanceChoiceSurface/MinimalGuidanceButton/Label"),
                FindText(root, "SafeArea/GuidanceChoiceSurface/ContinueButton/Label"),
                FindText(root, "SafeArea/SkipConfirmationSurface/Confirmation/Title"),
                FindText(root, "SafeArea/SkipConfirmationSurface/Confirmation/Body"),
                FindText(root, "SafeArea/SkipConfirmationSurface/Confirmation/CancelButton/Label"),
                FindText(root, "SafeArea/SkipConfirmationSurface/Confirmation/ConfirmButton/Label")
            };
            string[] keys =
            {
                "narrative.first_launch.control.skip",
                "narrative.first_launch.identity.title",
                "narrative.first_launch.identity.instruction",
                "narrative.first_launch.identity.callsign",
                "narrative.first_launch.control.continue",
                "narrative.first_launch.guidance.title",
                "narrative.first_launch.guidance.instruction",
                "narrative.first_launch.guidance.full",
                "narrative.first_launch.guidance.contextual",
                "narrative.first_launch.guidance.minimal",
                "narrative.first_launch.control.continue",
                "narrative.first_launch.skip.title",
                "narrative.first_launch.skip.body",
                "narrative.first_launch.control.cancel_skip",
                "narrative.first_launch.control.confirm_skip"
            };
            string[] fallbacks =
            {
                "SKIP", "EMERGENCY CONTINUITY AUTHENTICATION", "CHOOSE YOUR COMMANDER IDENTITY", "COMMANDER", "CONTINUE  >",
                "CHOOSE ARIA'S GUIDANCE LEVEL", "This can be changed later in Command Settings.", "FULL GUIDANCE", "TACTICAL HINTS",
                "MINIMAL GUIDANCE", "CONTINUE", "SKIP TO TACTICAL COMMAND?",
                "The default commander identity and Full Guidance setting will be used. You can change both later.", "KEEP WATCHING", "SKIP INTRO"
            };
            SetArray(view, "localizedTextTargets", targets);
            SetStringArray(view, "localizedTextKeys", keys);
            SetStringArray(view, "localizedTextEnglishFallbacks", fallbacks);
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target == null || !target.TryGetComponent(out TMP_Text text))
                throw new UnityException($"Missing localized narrative text target: {path}");
            return text;
        }

        private static AudioSource CreateAudioSource(string name, Transform parent, bool loop)
        {
            RectTransform rect = CreateRect(name, parent);
            AudioSource source = rect.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private static NarrativeLocationIntroView BuildLocationIntroSurface(Transform parent, Sprite frame, TMP_FontAsset bold, TMP_FontAsset medium)
        {
            Image surface = CreateImage("LocationIntroduction", parent, frame, Color.white, false);
            surface.rectTransform.pivot = new Vector2(0f, 1f);
            SetRect(surface.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(650f, 156f), new Vector2(0f, -18f));
            ApplyLiveMenuScale(surface.rectTransform);
            surface.preserveAspect = true;
            CanvasGroup group = surface.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            NarrativeLocationIntroView view = surface.gameObject.AddComponent<NarrativeLocationIntroView>();

            TMP_Text title = CreateText("LocationName", surface.transform, "SAHRIN", bold, 56f, TextAlignmentOptions.Left, new Color(0.96f, 0.78f, 0.3f, 1f));
            SetStretchOffsets(title.rectTransform, new Vector2(74f, 66f), new Vector2(-54f, -24f));
            TMP_Text subtitle = CreateText("DistrictAndTime", surface.transform, "OLD MARKET / 06:42 LOCAL", medium, 34f, TextAlignmentOptions.Left, new Color(0.9f, 0.88f, 0.8f, 1f));
            SetStretchOffsets(subtitle.rectTransform, new Vector2(74f, 24f), new Vector2(-54f, -78f));
            SetObject(view, "group", group);
            SetObject(view, "titleText", title);
            SetObject(view, "subtitleText", subtitle);
            return view;
        }

        private static NarrativeReviewerControlsView BuildReviewerSurface(Transform parent, Sprite hudPanel, Sprite hudButton, TMP_FontAsset bold, TMP_FontAsset medium)
        {
            RectTransform surface = CreateRect("DevelopmentReviewerControls", parent);
            SetRect(surface, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1450f, 188f), new Vector2(-132f, -4f));
            surface.pivot = new Vector2(0.5f, 1f);
            ApplyLiveMenuScale(surface);
            CanvasGroup group = surface.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            Image backing = surface.gameObject.AddComponent<Image>();
            backing.sprite = hudPanel;
            backing.type = Image.Type.Sliced;
            backing.color = new Color(1f, 1f, 1f, 0.97f);
            NarrativeReviewerControlsView view = surface.gameObject.AddComponent<NarrativeReviewerControlsView>();

            Button previous = CreateFramedButton("PreviousButton", surface, hudButton, bold, "PREV", new Vector2(132f, 68f));
            Button playPause = CreateFramedButton("PlayPauseButton", surface, hudButton, bold, "PAUSE", new Vector2(132f, 68f));
            Button next = CreateFramedButton("NextButton", surface, hudButton, bold, "NEXT", new Vector2(132f, 68f));
            Button restart = CreateFramedButton("RestartButton", surface, hudButton, bold, "RESTART", new Vector2(146f, 68f));
            Button skipToGame = CreateFramedButton("SkipToGameButton", surface, hudButton, bold, "GAME", new Vector2(136f, 68f));
            Button jumpDebrief = CreateFramedButton("JumpToDebriefButton", surface, hudButton, bold, "DEBRIEF", new Vector2(168f, 68f));
            Button capture = CreateFramedButton("CaptureButton", surface, hudButton, bold, "CAPTURE", new Vector2(156f, 68f));
            Button[] buttons = { previous, playPause, next, restart, skipToGame, jumpDebrief, capture };
            float[] x = { -634f, -492f, -350f, -202f, -51f, 111f, 283f };
            for (int i = 0; i < buttons.Length; i++)
                SetRect(buttons[i].GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), buttons[i].GetComponent<RectTransform>().sizeDelta, new Vector2(x[i], -47f));

            TMP_Text playPauseLabel = playPause.GetComponentInChildren<TMP_Text>(true);
            TMP_Text stateLabel = CreateText("StateIdLabel", surface, "FL-P01", medium, 22f, TextAlignmentOptions.Left, new Color(0.94f, 0.91f, 0.78f, 1f));
            SetRect(stateLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(350f, 48f), new Vector2(-510f, 44f));
            TMP_Text positionLabel = CreateText("PositionLabel", surface, "1 / 26", bold, 22f, TextAlignmentOptions.Center, new Color(0.94f, 0.91f, 0.78f, 1f));
            SetRect(positionLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(110f, 48f), new Vector2(-274f, 44f));

            Image sliderTrack = CreateImage("Timeline", surface, null, new Color(0.14f, 0.13f, 0.1f, 0.8f), true);
            SetRect(sliderTrack.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(510f, 34f), new Vector2(45f, 46f));
            Slider slider = sliderTrack.gameObject.AddComponent<Slider>();
            Image fill = CreateImage("Fill", sliderTrack.transform, null, new Color(0.15f, 0.73f, 0.86f, 1f), false);
            Stretch(fill.rectTransform);
            Image handle = CreateImage("Handle", sliderTrack.transform, null, new Color(0.96f, 0.95f, 0.88f, 1f), true);
            SetRect(handle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 52f), Vector2.zero);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            Image toggleBox = CreateImage("ReducedMotionToggle", surface, null, new Color(0.14f, 0.13f, 0.1f, 0.9f), true);
            SetRect(toggleBox.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(48f, 48f), new Vector2(454f, -48f));
            Toggle reducedMotion = toggleBox.gameObject.AddComponent<Toggle>();
            Image checkmark = CreateImage("Checkmark", toggleBox.transform, null, new Color(0.15f, 0.73f, 0.86f, 1f), false);
            checkmark.rectTransform.anchorMin = new Vector2(0.22f, 0.22f);
            checkmark.rectTransform.anchorMax = new Vector2(0.78f, 0.78f);
            checkmark.rectTransform.offsetMin = Vector2.zero;
            checkmark.rectTransform.offsetMax = Vector2.zero;
            reducedMotion.targetGraphic = toggleBox;
            reducedMotion.graphic = checkmark;
            TMP_Text reducedLabel = CreateText("ReducedMotionLabel", surface, "REDUCED MOTION", medium, 21f, TextAlignmentOptions.Left, new Color(0.94f, 0.91f, 0.78f, 1f));
            SetRect(reducedLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230f, 48f), new Vector2(604f, -48f));

            Toggle subtitles = CreateReviewerToggle(surface, "SubtitlesToggle", "SUBTITLES", medium, new Vector2(354f, 44f));
            Toggle safeAreaToggle = CreateReviewerToggle(surface, "SafeAreaToggle", "SAFE AREA", medium, new Vector2(566f, 44f));
            subtitles.SetIsOnWithoutNotify(true);

            SetObject(view, "playPauseButton", playPause);
            SetObject(view, "restartButton", restart);
            SetObject(view, "previousButton", previous);
            SetObject(view, "nextButton", next);
            SetObject(view, "skipToGameButton", skipToGame);
            SetObject(view, "jumpToDebriefButton", jumpDebrief);
            SetObject(view, "captureButton", capture);
            SetObject(view, "timelineSlider", slider);
            SetObject(view, "playPauseLabel", playPauseLabel);
            SetObject(view, "stateIdLabel", stateLabel);
            SetObject(view, "positionLabel", positionLabel);
            SetObject(view, "reducedMotionToggle", reducedMotion);
            SetObject(view, "subtitlesToggle", subtitles);
            SetObject(view, "safeAreaToggle", safeAreaToggle);
            SetObject(view, "visibilityGroup", group);
            return view;
        }

        private static Toggle CreateReviewerToggle(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position)
        {
            Image box = CreateImage(name, parent, null, new Color(0.14f, 0.13f, 0.1f, 0.9f), true);
            SetRect(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(42f, 42f), position);
            Toggle toggle = box.gameObject.AddComponent<Toggle>();
            Image checkmark = CreateImage("Checkmark", box.transform, null, new Color(0.15f, 0.73f, 0.86f, 1f), false);
            checkmark.rectTransform.anchorMin = new Vector2(0.22f, 0.22f);
            checkmark.rectTransform.anchorMax = new Vector2(0.78f, 0.78f);
            checkmark.rectTransform.offsetMin = Vector2.zero;
            checkmark.rectTransform.offsetMax = Vector2.zero;
            toggle.targetGraphic = box;
            toggle.graphic = checkmark;
            TMP_Text text = CreateText("Label", parent, label, font, 20f, TextAlignmentOptions.Left, new Color(0.94f, 0.91f, 0.78f, 1f));
            SetRect(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 42f), position + new Vector2(106f, 0f));
            return toggle;
        }

        private static GameObject BuildSafeAreaPreview(RectTransform safeArea)
        {
            RectTransform preview = CreateRect("SafeAreaPreview", safeArea);
            Stretch(preview);
            Color color = new(0.15f, 0.85f, 0.95f, 0.88f);
            Image top = CreateImage("Top", preview, null, color, false);
            SetRect(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f), new Vector2(0f, -1f));
            Image bottom = CreateImage("Bottom", preview, null, color, false);
            SetRect(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), new Vector2(0f, 1f));
            Image left = CreateImage("Left", preview, null, color, false);
            SetRect(left.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(2f, 0f), new Vector2(1f, 0f));
            Image right = CreateImage("Right", preview, null, color, false);
            SetRect(right.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(2f, 0f), new Vector2(-1f, 0f));
            preview.gameObject.SetActive(false);
            return preview.gameObject;
        }

        private static NarrativeCommanderIdentityView BuildIdentitySurface(Transform parent, Sprite hudPanel, Sprite hudButton, Sprite hudPrimaryButton, TMP_FontAsset bold, TMP_FontAsset medium)
        {
            RectTransform surface = CreateRect("CommanderIdentitySurface", parent);
            SetRect(surface, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1480f, 760f), Vector2.zero);
            ApplyLiveMenuScale(surface);
            Image backing = surface.gameObject.AddComponent<Image>();
            backing.sprite = hudPanel;
            backing.type = Image.Type.Sliced;
            NarrativeCommanderIdentityView view = surface.gameObject.AddComponent<NarrativeCommanderIdentityView>();

            TMP_Text title = CreateText("Title", surface, "EMERGENCY CONTINUITY AUTHENTICATION", bold, 46f, TextAlignmentOptions.Left, new Color(0.96f, 0.93f, 0.82f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1120f, 64f), new Vector2(45f, -64f));
            TMP_Text instruction = CreateText("Instruction", surface, "CHOOSE YOUR COMMANDER IDENTITY", medium, 25f, TextAlignmentOptions.Left, new Color(0.15f, 0.78f, 0.84f, 1f));
            SetRect(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1120f, 40f), new Vector2(45f, -112f));

            List<Sprite> portraits = LoadCommanderPortraits();
            Sprite selectedFrame = RequireAsset<Sprite>("Assets/Game/Art/UI/Panels/scn09_build_card_frame_selected_check.png");
            Button[] portraitButtons = new Button[portraits.Count];
            Image[] portraitImages = new Image[portraits.Count];
            Image[] selectionImages = new Image[portraits.Count];
            TMP_Text[] portraitAccess = new TMP_Text[portraits.Count];
            Vector2[] portraitPositions =
            {
                new(-450f, 125f), new(-150f, 125f), new(150f, 125f), new(450f, 125f),
                new(-300f, -70f), new(0f, -70f), new(300f, -70f)
            };
            for (int i = 0; i < portraits.Count; i++)
            {
                Image card = CreateImage($"PortraitButton_{i + 1:00}", surface, hudButton, Color.white, true);
                card.type = Image.Type.Sliced;
                SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250f, 175f), portraitPositions[i]);
                portraitButtons[i] = card.gameObject.AddComponent<Button>();
                Image portrait = CreateImage("Portrait", card.transform, portraits[i], Color.white, false);
                portrait.rectTransform.anchorMin = new Vector2(0.04f, 0.06f);
                portrait.rectTransform.anchorMax = new Vector2(0.96f, 0.94f);
                portrait.rectTransform.offsetMin = Vector2.zero;
                portrait.rectTransform.offsetMax = Vector2.zero;
                portrait.preserveAspect = true;
                portraitImages[i] = portrait;
                Image selected = CreateImage("Selection", card.transform, selectedFrame, Color.white, false);
                Stretch(selected.rectTransform);
                selected.type = Image.Type.Sliced;
                selectionImages[i] = selected;
                portraitAccess[i] = CreateHiddenAccessibilityText("AccessibilityLabel", card.transform, medium);
            }

            Image selectedPortrait = CreateImage("SelectedPortrait", surface, portraits[^1], Color.white, false);
            SetRect(selectedPortrait.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.one, Vector2.zero);
            selectedPortrait.gameObject.SetActive(false);

            TMP_Text callsignLabel = CreateText("CallsignLabel", surface, "COMMANDER", medium, 22f, TextAlignmentOptions.Left, new Color(0.15f, 0.78f, 0.84f, 1f));
            SetRect(callsignLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(650f, 30f), new Vector2(0f, 196f));
            TMP_InputField callsign = CreateInputField("CallsignInput", surface, hudButton, bold, medium, "COMMANDER");
            SetRect(callsign.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(650f, 86f), new Vector2(0f, 136f));
            TMP_InputField displayName = CreateInputField("DisplayNameInput", surface, hudButton, bold, medium, "Commander");
            displayName.gameObject.SetActive(false);

            Button continueButton = CreateFramedButton("ContinueButton", surface, hudPrimaryButton, bold, "CONTINUE  >", new Vector2(650f, 88f));
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(650f, 88f), new Vector2(0f, 56f));

            TMP_Text callsignAccess = CreateHiddenAccessibilityText("CallsignAccessibilityLabel", callsign.transform, medium);
            TMP_Text nameAccess = CreateHiddenAccessibilityText("DisplayNameAccessibilityLabel", displayName.transform, medium);
            TMP_Text continueAccess = CreateHiddenAccessibilityText("ContinueAccessibilityLabel", continueButton.transform, medium);
            SetObject(view, "callsignInput", callsign);
            SetObject(view, "displayNameInput", displayName);
            SetObject(view, "selectedPortraitImage", selectedPortrait);
            SetObject(view, "defaultPortrait", portraits[^1]);
            SetArray(view, "portraitButtons", portraitButtons);
            SetArray(view, "portraitImages", portraitImages);
            SetArray(view, "portraitSelectionImages", selectionImages);
            SetArray(view, "portraitAccessibilityLabels", portraitAccess);
            SetObject(view, "continueButton", continueButton);
            SetObject(view, "callsignAccessibilityLabel", callsignAccess);
            SetObject(view, "displayNameAccessibilityLabel", nameAccess);
            SetObject(view, "continueAccessibilityLabel", continueAccess);
            surface.gameObject.SetActive(false);
            return view;
        }

        private static NarrativeGuidanceChoiceView BuildGuidanceSurface(Transform parent, Sprite hudPanel, Sprite hudButton, Sprite hudPrimaryButton, TMP_FontAsset bold, TMP_FontAsset medium)
        {
            RectTransform surface = CreateRect("GuidanceChoiceSurface", parent);
            SetRect(surface, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1480f, 760f), Vector2.zero);
            ApplyLiveMenuScale(surface);
            Image backing = surface.gameObject.AddComponent<Image>();
            backing.sprite = hudPanel;
            backing.type = Image.Type.Sliced;
            NarrativeGuidanceChoiceView view = surface.gameObject.AddComponent<NarrativeGuidanceChoiceView>();

            TMP_Text title = CreateText("Title", surface, "CHOOSE ARIA'S GUIDANCE LEVEL", bold, 44f, TextAlignmentOptions.Center, new Color(0.96f, 0.78f, 0.3f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 66f), new Vector2(0f, -100f));
            TMP_Text instruction = CreateText("Instruction", surface, "This can be changed later in Command Settings.", medium, 30f, TextAlignmentOptions.Center, new Color(0.9f, 0.88f, 0.8f, 1f));
            SetRect(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 50f), new Vector2(0f, -154f));

            Button full = CreateFramedButton("FullGuidanceButton", surface, hudButton, bold, "FULL GUIDANCE", new Vector2(360f, 200f));
            Button contextual = CreateFramedButton("ContextualGuidanceButton", surface, hudButton, bold, "TACTICAL HINTS", new Vector2(360f, 200f));
            Button minimal = CreateFramedButton("MinimalGuidanceButton", surface, hudButton, bold, "MINIMAL GUIDANCE", new Vector2(360f, 200f));
            SetRect(full.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 200f), new Vector2(-410f, 10f));
            SetRect(contextual.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 200f), new Vector2(0f, 10f));
            SetRect(minimal.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 200f), new Vector2(410f, 10f));
            Image fullSelection = CreateSelectionRail(full.transform);
            Image contextualSelection = CreateSelectionRail(contextual.transform);
            Image minimalSelection = CreateSelectionRail(minimal.transform);

            Button continueButton = CreateFramedButton("ContinueButton", surface, hudPrimaryButton, bold, "CONTINUE", new Vector2(430f, 112f));
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(430f, 112f), new Vector2(0f, 104f));
            TMP_Text fullAccess = CreateHiddenAccessibilityText("FullAccessibilityLabel", full.transform, medium);
            TMP_Text contextualAccess = CreateHiddenAccessibilityText("ContextualAccessibilityLabel", contextual.transform, medium);
            TMP_Text minimalAccess = CreateHiddenAccessibilityText("MinimalAccessibilityLabel", minimal.transform, medium);
            TMP_Text continueAccess = CreateHiddenAccessibilityText("ContinueAccessibilityLabel", continueButton.transform, medium);
            SetObject(view, "fullButton", full);
            SetObject(view, "contextualButton", contextual);
            SetObject(view, "minimalButton", minimal);
            SetObject(view, "fullSelectionImage", fullSelection);
            SetObject(view, "contextualSelectionImage", contextualSelection);
            SetObject(view, "minimalSelectionImage", minimalSelection);
            SetObject(view, "continueButton", continueButton);
            SetObject(view, "fullAccessibilityLabel", fullAccess);
            SetObject(view, "contextualAccessibilityLabel", contextualAccess);
            SetObject(view, "minimalAccessibilityLabel", minimalAccess);
            SetObject(view, "continueAccessibilityLabel", continueAccess);
            surface.gameObject.SetActive(false);
            return view;
        }

        private static NarrativeSkipConfirmationView BuildSkipConfirmationSurface(Transform parent, Sprite hudPanel, Sprite hudButton, Sprite hudPrimaryButton, TMP_FontAsset bold, TMP_FontAsset medium)
        {
            RectTransform surface = CreateRect("SkipConfirmationSurface", parent);
            Stretch(surface);
            CanvasGroup group = surface.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            NarrativeSkipConfirmationView view = surface.gameObject.AddComponent<NarrativeSkipConfirmationView>();
            Image dim = CreateImage("Dim", surface, null, new Color(0f, 0f, 0f, 0.72f), true);
            Stretch(dim.rectTransform);
            RectTransform modal = CreateRect("Confirmation", surface);
            SetRect(modal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(980f, 520f), Vector2.zero);
            ApplyLiveMenuScale(modal);
            Image backing = modal.gameObject.AddComponent<Image>();
            backing.sprite = hudPanel;
            backing.type = Image.Type.Sliced;
            TMP_Text title = CreateText("Title", modal, "SKIP TO TACTICAL COMMAND?", bold, 42f, TextAlignmentOptions.Center, new Color(0.96f, 0.78f, 0.3f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(800f, 70f), new Vector2(0f, -110f));
            TMP_Text body = CreateText("Body", modal, "The default commander identity and Full Guidance setting will be used. You can change both later.", medium, 30f, TextAlignmentOptions.Center, new Color(0.9f, 0.88f, 0.8f, 1f));
            SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760f, 126f), new Vector2(0f, 32f));
            Button cancel = CreateFramedButton("CancelButton", modal, hudButton, bold, "KEEP WATCHING", new Vector2(350f, 100f));
            Button confirm = CreateFramedButton("ConfirmButton", modal, hudPrimaryButton, bold, "SKIP INTRO", new Vector2(350f, 100f));
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(350f, 100f), new Vector2(-202f, 92f));
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(350f, 100f), new Vector2(202f, 92f));
            TMP_Text accessible = CreateHiddenAccessibilityText("AccessibilityLabel", modal, medium);
            SetObject(view, "group", group);
            SetObject(view, "confirmButton", confirm);
            SetObject(view, "cancelButton", cancel);
            SetObject(view, "accessibleLabel", accessible);
            return view;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, Sprite frame, TMP_FontAsset bold, TMP_FontAsset medium, string value)
        {
            Image backing = CreateImage(name, parent, frame, Color.white, true);
            backing.type = Image.Type.Sliced;
            TMP_InputField input = backing.gameObject.AddComponent<TMP_InputField>();
            TMP_Text text = CreateText("Text", backing.transform, value, bold, 36f, TextAlignmentOptions.Left, new Color(0.94f, 0.91f, 0.78f, 1f));
            SetStretchOffsets(text.rectTransform, new Vector2(90f, 16f), new Vector2(-70f, -40f));
            TMP_Text placeholder = CreateText("Placeholder", backing.transform, string.Empty, medium, 24f, TextAlignmentOptions.Left, new Color(0.62f, 0.6f, 0.54f, 0.8f));
            SetStretchOffsets(placeholder.rectTransform, new Vector2(42f, 54f), new Vector2(-42f, -12f));
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value;
            input.characterLimit = 32;
            return input;
        }

        private static Button CreateFramedButton(string name, Transform parent, Sprite frame, TMP_FontAsset font, string label, Vector2 size)
        {
            Image image = CreateImage(name, parent, frame, Color.white, true);
            image.type = Image.Type.Sliced;
            Button button = image.gameObject.AddComponent<Button>();
            TMP_Text text = CreateText("Label", image.transform, label, font, 30f, TextAlignmentOptions.Center, new Color(0.96f, 0.93f, 0.82f, 1f));
            Stretch(text.rectTransform);
            image.rectTransform.sizeDelta = size;
            return button;
        }

        private static Image CreateSelectionRail(Transform parent)
        {
            Image rail = CreateImage("SelectionRail", parent, null, new Color(0.12f, 0.68f, 0.76f, 1f), false);
            SetRect(rail.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(240f, 8f), new Vector2(0f, 24f));
            return rail;
        }

        private static TMP_Text CreateHiddenAccessibilityText(string name, Transform parent, TMP_FontAsset font)
        {
            TMP_Text text = CreateText(name, parent, string.Empty, font, 1f, TextAlignmentOptions.Left, Color.clear);
            text.gameObject.SetActive(false);
            return text;
        }

        [MenuItem("Game/Narrative/First Launch/Capture Dialogue Accessibility Evidence")]
        public static void CaptureAccessibilityEvidence()
        {
            Build();
            const string evidenceRoot = "Design/NarrativeVision/FirstLaunch/evidence/runtime/phase8";
            Directory.CreateDirectory(evidenceRoot);
            Capture(
                1920,
                1080,
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png",
                UISubtitleSize.Standard,
                UISubtitleBackgroundOpacity.SeventyFivePercent,
                "Families and road crews are trapped beyond the clinic route.",
                evidenceRoot + "/dialogue_standard_1920x1080.png");
            Capture(
                2400,
                1080,
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P04.png",
                UISubtitleSize.ExtraLarge,
                UISubtitleBackgroundOpacity.OneHundredPercent,
                "Families, clinic staff, municipal crews, and road-repair teams remain trapped beyond the blocked clinic route. Civilian evacuation access must remain visible and unobstructed throughout the response.",
                evidenceRoot + "/dialogue_max_expansion_2400x1080.png");
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P08.png", NarrativeInteractiveStateKind.CommanderIdentity, false, evidenceRoot + "/live_identity_1920x1080.png");
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P09.png", NarrativeInteractiveStateKind.GuidanceChoice, false, evidenceRoot + "/live_guidance_1920x1080.png");
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png", NarrativeInteractiveStateKind.None, true, evidenceRoot + "/live_skip_confirmation_1920x1080.png");
            Debug.Log($"[FirstLaunchNarrativePresentationPrefabBuilder] Accessibility evidence written to {evidenceRoot}.");
        }

        [MenuItem("Game/Narrative/First Launch/Capture Reviewer Evidence")]
        public static void CaptureReviewerEvidence()
        {
            Build();
            const string evidenceRoot = "Design/NarrativeVision/FirstLaunch/evidence/runtime/phase9";
            Directory.CreateDirectory(evidenceRoot);
            CaptureReviewer(1920, 1080,
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P16.png",
                "FL-P16", 17, false, evidenceRoot + "/reviewer_controls_normal_1920x1080.png");
            CaptureReviewer(2400, 1080,
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P19.png",
                "FL-P19", 22, true, evidenceRoot + "/reviewer_controls_reduced_motion_2400x1080.png");
            Debug.Log($"[FirstLaunchNarrativePresentationPrefabBuilder] Reviewer evidence written to {evidenceRoot}.");
        }

        [MenuItem("Game/Narrative/First Launch/Capture Skip Checkpoint Evidence")]
        public static void CaptureSkipCheckpointEvidence()
        {
            Build();
            const string evidenceRoot = "Design/NarrativeVision/FirstLaunch/evidence/runtime/phase9";
            Directory.CreateDirectory(evidenceRoot);
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P02.png", NarrativeInteractiveStateKind.None, true, evidenceRoot + "/skip_early_fl-p02_1920x1080.png");
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P10.png", NarrativeInteractiveStateKind.None, true, evidenceRoot + "/skip_middle_fl-p10_1920x1080.png");
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P08.png", NarrativeInteractiveStateKind.CommanderIdentity, true, evidenceRoot + "/skip_identity_fl-p08_1920x1080.png");
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P18.png", NarrativeInteractiveStateKind.None, true, evidenceRoot + "/skip_final_opening_fl-p18_1920x1080.png");
            Debug.Log($"[FirstLaunchNarrativePresentationPrefabBuilder] Skip checkpoint evidence written to {evidenceRoot}.");
        }

        [MenuItem("Game/Narrative/First Launch/Capture Device Aspect Evidence")]
        public static void CaptureDeviceAspectEvidence()
        {
            Build();
            const string evidenceRoot = "Design/NarrativeVision/FirstLaunch/evidence/runtime/phase10";
            Directory.CreateDirectory(evidenceRoot);
            const string text = "Civilians, clinic staff, and municipal crews remain behind the closure.";
            Capture(
                2400,
                1080,
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P16.png",
                UISubtitleSize.Standard,
                UISubtitleBackgroundOpacity.SeventyFivePercent,
                text,
                evidenceRoot + "/normal_playback_2400x1080.png");
            Capture(
                1920,
                1200,
                "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P16.png",
                UISubtitleSize.Standard,
                UISubtitleBackgroundOpacity.SeventyFivePercent,
                text,
                evidenceRoot + "/tablet_landscape_1920x1200.png");
            Debug.Log($"[FirstLaunchNarrativePresentationPrefabBuilder] Device aspect evidence written to {evidenceRoot}.");
        }

        [MenuItem("Game/Narrative/First Launch/Capture Phase 10R Revision Evidence")]
        public static void CapturePhase10RRevisionEvidence()
        {
            Build();
            const string evidenceRoot = "Design/NarrativeVision/FirstLaunch/evidence/runtime/phase10r";
            Directory.CreateDirectory(evidenceRoot);
            const string text = "Families and road crews are trapped beyond the clinic route.";
            const string longText = "Commander, the eastern clinic corridor remains blocked while civil crews, families, and the surviving response convoy wait beyond the damaged relay junction.";
            Capture(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png", UISubtitleSize.Standard, UISubtitleBackgroundOpacity.SeventyFivePercent, text, evidenceRoot + "/dialogue_standard_1920x1080.png");
            Capture(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png", UISubtitleSize.Standard, UISubtitleBackgroundOpacity.SeventyFivePercent, longText, evidenceRoot + "/dialogue_long_1920x1080.png");
            Capture(2400, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P04.png", UISubtitleSize.Standard, UISubtitleBackgroundOpacity.SeventyFivePercent, text, evidenceRoot + "/dialogue_standard_2400x1080.png");
            Capture(1920, 1200, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png", UISubtitleSize.Standard, UISubtitleBackgroundOpacity.SeventyFivePercent, text, evidenceRoot + "/dialogue_tablet_1920x1200.png");
            Capture(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P01.png", UISubtitleSize.Standard, UISubtitleBackgroundOpacity.SeventyFivePercent, string.Empty, evidenceRoot + "/location_intro_1920x1080.png", true, false);
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P08.png", NarrativeInteractiveStateKind.CommanderIdentity, false, evidenceRoot + "/identity_1920x1080.png");
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P09.png", NarrativeInteractiveStateKind.GuidanceChoice, false, evidenceRoot + "/guidance_1920x1080.png");
            CaptureInteractive(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png", NarrativeInteractiveStateKind.None, true, evidenceRoot + "/skip_confirmation_1920x1080.png");
            CaptureReviewer(1920, 1080, "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P16.png", "FL-P16", 17, false, evidenceRoot + "/reviewer_controls_1920x1080.png");
            Debug.Log($"[FirstLaunchNarrativePresentationPrefabBuilder] Phase 10R revision evidence written to {evidenceRoot}.");
        }

        private static void Capture(
            int width,
            int height,
            string panelPath,
            UISubtitleSize subtitleSize,
            UISubtitleBackgroundOpacity opacity,
            string text,
            string outputPath,
            bool showLocation = false,
            bool showDialogue = true)
        {
            GameObject cameraObject = new("NarrativeCaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasObject = new("NarrativeCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = LiveMenuReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject prefab = RequireAsset<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
            NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
            view.SetVisible(true);
            view.ApplyPanel(new NarrativePanelPresentationModel
            {
                StateId = showLocation ? "FL-P01" : "FL-P04",
                PanelSprite = RequireAsset<Sprite>(panelPath),
                Tint = Color.white
            });
            view.ApplyLocation(new NarrativeLocationPresentationModel
            {
                Visible = showLocation,
                Title = "SAHRIN",
                Subtitle = "OLD MARKET / 06:42 LOCAL"
            });
            view.DialogueView.ApplySpeaker(new NarrativeSpeakerPresentationModel
            {
                SpeakerId = NarrativeSpeakerId.Samira,
                DisplayName = "SAMIRA HADDAD",
                Role = "CIVIL INFRASTRUCTURE",
                AccessibleLabel = "Engineer Samira Haddad, civil infrastructure",
                IdentitySprite = RequireAsset<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.SamiraPortraitPath),
                AccentColor = Color.white,
                Treatment = NarrativeSpeakerTreatment.HumanPortrait
            });
            UISettingsModel settings = Game.UI.Runtime.SettingsService.Defaults;
            settings.Narrative.SubtitleSize = subtitleSize;
            settings.Narrative.BackgroundOpacity = opacity;
            if (showDialogue)
            {
                view.DialogueView.PrepareLine(text, NarrativeSubtitleStyleUtilitySystemHelper.Resolve(settings));
                view.DialogueView.CompleteLine();
            }
            else
            {
                view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
            }
            view.SetSkipState(true, true, "SKIP");

            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D capture = new(width, height, TextureFormat.RGBA32, false);
            capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            capture.Apply(false);
            File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureInteractive(int width, int height, string panelPath, NarrativeInteractiveStateKind kind, bool showSkipConfirmation, string outputPath)
        {
            GameObject cameraObject = new("NarrativeCaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            GameObject canvasObject = new("NarrativeCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = LiveMenuReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            GameObject instance = PrefabUtility.InstantiatePrefab(RequireAsset<GameObject>(PrefabPath), canvas.transform) as GameObject;
            NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
            view.SetVisible(true);
            view.ApplyPanel(new NarrativePanelPresentationModel { StateId = "review", PanelSprite = RequireAsset<Sprite>(panelPath), Tint = Color.white });
            view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
            view.SetSkipState(true, true, "SKIP");
            view.SetInteractiveState(kind);
            if (kind == NarrativeInteractiveStateKind.CommanderIdentity)
            {
                view.CommanderIdentityView.SetIdentity("COMMANDER", "Commander", 6);
                view.CommanderIdentityView.SetControlsInteractable(true);
            }
            else if (kind == NarrativeInteractiveStateKind.GuidanceChoice)
            {
                view.GuidanceChoiceView.SetSelectedGuidance(NarrativeGuidanceMode.Full);
                view.GuidanceChoiceView.SetControlsInteractable(true);
            }
            view.SkipConfirmationView.SetVisible(showSkipConfirmation);
            if (showSkipConfirmation)
            {
                UISettingsModel settings = Game.UI.Runtime.SettingsService.Defaults;
                view.DialogueView.ApplySpeaker(new NarrativeSpeakerPresentationModel
                {
                    SpeakerId = NarrativeSpeakerId.Dalia,
                    DisplayName = "DALIA RAHIM",
                    Role = "JRC FIELD COMMAND",
                    IdentitySprite = RequireAsset<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.DaliaPortraitPath),
                    Treatment = NarrativeSpeakerTreatment.HumanPortrait,
                    AccentColor = Color.white
                });
                view.DialogueView.PrepareLine("Families and road crews are trapped beyond the clinic route.", NarrativeSubtitleStyleUtilitySystemHelper.Resolve(settings));
                view.DialogueView.CompleteLine();
            }
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D capture = new(width, height, TextureFormat.RGBA32, false);
            capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            capture.Apply(false);
            File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureReviewer(int width, int height, string panelPath, string stateId, int stateNumber, bool reducedMotion, string outputPath)
        {
            GameObject cameraObject = new("NarrativeCaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            GameObject canvasObject = new("NarrativeCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = LiveMenuReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            GameObject instance = PrefabUtility.InstantiatePrefab(RequireAsset<GameObject>(PrefabPath), canvas.transform) as GameObject;
            NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
            view.SetVisible(true);
            view.ApplyPanel(new NarrativePanelPresentationModel { StateId = stateId, PanelSprite = RequireAsset<Sprite>(panelPath), Tint = Color.white });
            view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
            view.SetSkipState(true, true, "SKIP");
            view.ReviewerControlsView.SetDevelopmentVisibility(true);
            view.ReviewerControlsView.SetPlayingState(true);
            view.ReviewerControlsView.SetState(stateId, stateNumber, 26);
            view.ReviewerControlsView.SetProgress((stateNumber - 1f) / 25f);
            view.ReviewerControlsView.SetReducedMotion(reducedMotion);
            view.ReviewerControlsView.SetNavigationState(stateNumber > 1, stateNumber < 26);

            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D capture = new(width, height, TextureFormat.RGBA32, false);
            capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            capture.Apply(false);
            File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new UnityException($"Required asset missing: {path}");
            return asset;
        }

        private static List<Sprite> LoadCommanderPortraits()
        {
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(FirstLaunchNarrativeDialogueAssetImporter.CommanderPortraitSheetPath);
            List<Sprite> portraits = new();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name.StartsWith("commander_", StringComparison.Ordinal))
                    portraits.Add(sprite);
            }
            portraits.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            if (portraits.Count != 7)
                throw new UnityException($"Commander portrait sheet must provide 7 sprites; found {portraits.Count}.");
            return portraits;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject value = new(name, typeof(RectTransform));
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            RTLTextMeshPro text = rect.gameObject.AddComponent<RTLTextMeshPro>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.characterSpacing = 0f;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetStretchOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetTopStretchOffsets(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void ApplyLiveMenuScale(RectTransform rect)
        {
            rect.localScale = Vector3.one * LiveMenuPresentationScale;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new UnityException($"Missing serialized property {target.GetType().Name}.{propertyName}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(Object target, string propertyName, T[] values) where T : Object
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
                throw new UnityException($"Missing serialized array {target.GetType().Name}.{propertyName}");
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new UnityException($"Missing serialized integer {target.GetType().Name}.{propertyName}");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStringArray(Object target, string propertyName, string[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
                throw new UnityException($"Missing serialized string array {target.GetType().Name}.{propertyName}");
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i] ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
