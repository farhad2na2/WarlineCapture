using System.IO;
using Game.UI.Runtime;
using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class FirstLaunchNarrativePresentationPrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        [MenuItem("Game/Narrative/First Launch/Build Presentation Prefab")]
        public static void Build()
        {
            FirstLaunchNarrativeDialogueAssetImporter.Configure();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            TMP_FontAsset bold = RequireAsset<TMP_FontAsset>(BoldFontPath);
            TMP_FontAsset medium = RequireAsset<TMP_FontAsset>(MediumFontPath);
            Sprite frame = RequireAsset<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.FramePath);
            Sprite pointer = RequireAsset<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.PointerPath);

            RectTransform root = CreateRect("FirstLaunchNarrativeSequence", null);
            Stretch(root);
            CanvasGroup rootGroup = root.gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
            NarrativeSequenceView sequenceView = root.gameObject.AddComponent<NarrativeSequenceView>();
            AudioSource voiceSource = root.gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;

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

            RectTransform dialogue = CreateRect("Dialogue", safeArea);
            dialogue.anchorMin = new Vector2(0.5f, 0f);
            dialogue.anchorMax = new Vector2(0.5f, 0f);
            dialogue.pivot = new Vector2(0.5f, 0f);
            dialogue.sizeDelta = new Vector2(1540f, 350f);
            dialogue.anchoredPosition = new Vector2(0f, 28f);
            CanvasGroup dialogueGroup = dialogue.gameObject.AddComponent<CanvasGroup>();
            NarrativeDialogueView dialogueView = dialogue.gameObject.AddComponent<NarrativeDialogueView>();

            Image inputSurface = CreateImage("InputSurface", dialogue, null, new Color(0f, 0f, 0f, 0f), true);
            Stretch(inputSurface.rectTransform);
            Button inputButton = inputSurface.gameObject.AddComponent<Button>();
            inputButton.transition = Selectable.Transition.None;

            Image frameImage = CreateImage("Frame", dialogue, frame, Color.white, false);
            Stretch(frameImage.rectTransform);
            frameImage.type = Image.Type.Sliced;

            Image pointerImage = CreateImage("Pointer", dialogue, pointer, Color.white, false);
            SetRect(pointerImage.rectTransform, new Vector2(1f, 0.47f), new Vector2(1f, 0.47f), new Vector2(174f, 138f), new Vector2(76f, 0f));
            pointerImage.preserveAspect = true;

            Image portrait = CreateImage("Portrait", dialogue, null, Color.white, false);
            SetRect(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(218f, 218f), new Vector2(154f, 0f));
            portrait.preserveAspect = true;

            Image ariaIcon = CreateImage("AriaIcon", dialogue, null, new Color(0.2f, 0.92f, 1f, 1f), false);
            SetRect(ariaIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 150f), new Vector2(154f, 0f));
            ariaIcon.preserveAspect = true;
            ariaIcon.gameObject.SetActive(false);

            TMP_Text speakerName = CreateText("SpeakerName", dialogue, "DALIA RAHIM", bold, 31f, TextAlignmentOptions.Left, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetStretchOffsets(speakerName.rectTransform, new Vector2(296f, 250f), new Vector2(-210f, -48f));
            TMP_Text speakerRole = CreateText("SpeakerRole", dialogue, "JRC FIELD COMMAND", medium, 20f, TextAlignmentOptions.Left, new Color(0.25f, 0.23f, 0.18f, 1f));
            SetStretchOffsets(speakerRole.rectTransform, new Vector2(296f, 210f), new Vector2(-210f, -102f));
            TMP_Text line = CreateText("DialogueText", dialogue, "", medium, 30f, TextAlignmentOptions.TopLeft, new Color(0.07f, 0.065f, 0.055f, 1f));
            SetStretchOffsets(line.rectTransform, new Vector2(296f, 70f), new Vector2(-210f, -145f));
            line.textWrappingMode = TextWrappingModes.Normal;
            line.enableAutoSizing = true;
            line.fontSizeMin = 18f;
            line.fontSizeMax = 44f;
            line.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text accessibility = CreateText("AccessibilityText", dialogue, "", medium, 1f, TextAlignmentOptions.Left, Color.clear);
            accessibility.raycastTarget = false;
            Stretch(accessibility.rectTransform);

            TMP_Text advance = CreateText("AdvanceIndicator", dialogue, ">", bold, 32f, TextAlignmentOptions.Center, new Color(0.11f, 0.1f, 0.08f, 1f));
            SetRect(advance.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(52f, 52f), new Vector2(-75f, 54f));
            advance.gameObject.SetActive(false);

            NarrativeCommanderIdentityView identityView = BuildIdentitySurface(safeArea, frame, bold, medium);
            NarrativeGuidanceChoiceView guidanceView = BuildGuidanceSurface(safeArea, frame, bold, medium);
            NarrativeSkipConfirmationView skipConfirmationView = BuildSkipConfirmationSurface(safeArea, frame, bold, medium);

            RectTransform controls = CreateRect("PlaybackControls", safeArea);
            controls.anchorMin = new Vector2(1f, 1f);
            controls.anchorMax = new Vector2(1f, 1f);
            controls.pivot = new Vector2(1f, 1f);
            controls.sizeDelta = new Vector2(190f, 68f);
            controls.anchoredPosition = new Vector2(-8f, -8f);
            CanvasGroup skipGroup = controls.gameObject.AddComponent<CanvasGroup>();
            NarrativePlaybackControlsView controlsView = controls.gameObject.AddComponent<NarrativePlaybackControlsView>();
            Image skipBacking = CreateImage("SkipButton", controls, frame, new Color(1f, 1f, 1f, 0.94f), true);
            Stretch(skipBacking.rectTransform);
            skipBacking.type = Image.Type.Sliced;
            Button skipButton = skipBacking.gameObject.AddComponent<Button>();
            TMP_Text skipLabel = CreateText("Label", skipBacking.transform, "SKIP", bold, 23f, TextAlignmentOptions.Center, new Color(0.07f, 0.065f, 0.055f, 1f));
            Stretch(skipLabel.rectTransform);
            NarrativeReviewerControlsView reviewerView = BuildReviewerSurface(safeArea, frame, bold, medium);

            SetObject(dialogueView, "dialogueGroup", dialogueGroup);
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
            SetObject(sequenceView, "playbackControls", controlsView);
            SetObject(sequenceView, "commanderIdentityView", identityView);
            SetObject(sequenceView, "guidanceChoiceView", guidanceView);
            SetObject(sequenceView, "skipConfirmationView", skipConfirmationView);
            SetObject(sequenceView, "reviewerControlsView", reviewerView);
            SetObject(sequenceView, "safeAreaPreview", safeAreaPreview);
            SetObject(sequenceView, "voiceSource", voiceSource);
            skipConfirmationView.transform.SetAsLastSibling();

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FirstLaunchNarrativePresentationPrefabBuilder] Built {PrefabPath}");
        }

        private static NarrativeReviewerControlsView BuildReviewerSurface(Transform parent, Sprite frame, TMP_FontAsset bold, TMP_FontAsset medium)
        {
            RectTransform surface = CreateRect("DevelopmentReviewerControls", parent);
            SetRect(surface, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1420f, 142f), new Vector2(-130f, -4f));
            CanvasGroup group = surface.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            Image backing = surface.gameObject.AddComponent<Image>();
            backing.sprite = frame;
            backing.type = Image.Type.Sliced;
            backing.color = new Color(1f, 1f, 1f, 0.96f);
            NarrativeReviewerControlsView view = surface.gameObject.AddComponent<NarrativeReviewerControlsView>();

            Button previous = CreateFramedButton("PreviousButton", surface, frame, bold, "PREV", new Vector2(116f, 50f));
            Button playPause = CreateFramedButton("PlayPauseButton", surface, frame, bold, "PAUSE", new Vector2(116f, 50f));
            Button next = CreateFramedButton("NextButton", surface, frame, bold, "NEXT", new Vector2(116f, 50f));
            Button restart = CreateFramedButton("RestartButton", surface, frame, bold, "RESTART", new Vector2(132f, 50f));
            Button skipToGame = CreateFramedButton("SkipToGameButton", surface, frame, bold, "GAME", new Vector2(132f, 50f));
            Button jumpDebrief = CreateFramedButton("JumpToDebriefButton", surface, frame, bold, "DEBRIEF", new Vector2(160f, 50f));
            Button capture = CreateFramedButton("CaptureButton", surface, frame, bold, "CAPTURE", new Vector2(148f, 50f));
            Button[] buttons = { previous, playPause, next, restart, skipToGame, jumpDebrief, capture };
            float[] x = { -627f, -499f, -371f, -232f, -88f, 70f, 236f };
            for (int i = 0; i < buttons.Length; i++)
                SetRect(buttons[i].GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), buttons[i].GetComponent<RectTransform>().sizeDelta, new Vector2(x[i], -36f));

            TMP_Text playPauseLabel = playPause.GetComponentInChildren<TMP_Text>(true);
            TMP_Text stateLabel = CreateText("StateIdLabel", surface, "FL-P01", medium, 17f, TextAlignmentOptions.Left, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetRect(stateLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(350f, 38f), new Vector2(-512f, 32f));
            TMP_Text positionLabel = CreateText("PositionLabel", surface, "1 / 26", bold, 17f, TextAlignmentOptions.Center, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetRect(positionLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(100f, 38f), new Vector2(-278f, 32f));

            Image sliderTrack = CreateImage("Timeline", surface, null, new Color(0.14f, 0.13f, 0.1f, 0.8f), true);
            SetRect(sliderTrack.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(500f, 24f), new Vector2(35f, 34f));
            Slider slider = sliderTrack.gameObject.AddComponent<Slider>();
            Image fill = CreateImage("Fill", sliderTrack.transform, null, new Color(0.15f, 0.73f, 0.86f, 1f), false);
            Stretch(fill.rectTransform);
            Image handle = CreateImage("Handle", sliderTrack.transform, null, new Color(0.96f, 0.95f, 0.88f, 1f), true);
            SetRect(handle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 38f), Vector2.zero);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            Image toggleBox = CreateImage("ReducedMotionToggle", surface, null, new Color(0.14f, 0.13f, 0.1f, 0.9f), true);
            SetRect(toggleBox.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(38f, 38f), new Vector2(430f, -37f));
            Toggle reducedMotion = toggleBox.gameObject.AddComponent<Toggle>();
            Image checkmark = CreateImage("Checkmark", toggleBox.transform, null, new Color(0.15f, 0.73f, 0.86f, 1f), false);
            checkmark.rectTransform.anchorMin = new Vector2(0.22f, 0.22f);
            checkmark.rectTransform.anchorMax = new Vector2(0.78f, 0.78f);
            checkmark.rectTransform.offsetMin = Vector2.zero;
            checkmark.rectTransform.offsetMax = Vector2.zero;
            reducedMotion.targetGraphic = toggleBox;
            reducedMotion.graphic = checkmark;
            TMP_Text reducedLabel = CreateText("ReducedMotionLabel", surface, "REDUCED MOTION", medium, 17f, TextAlignmentOptions.Left, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetRect(reducedLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(220f, 38f), new Vector2(576f, -37f));

            Toggle subtitles = CreateReviewerToggle(surface, "SubtitlesToggle", "SUBTITLES", medium, new Vector2(345f, 32f));
            Toggle safeAreaToggle = CreateReviewerToggle(surface, "SafeAreaToggle", "SAFE AREA", medium, new Vector2(535f, 32f));
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
            SetRect(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(30f, 30f), position);
            Toggle toggle = box.gameObject.AddComponent<Toggle>();
            Image checkmark = CreateImage("Checkmark", box.transform, null, new Color(0.15f, 0.73f, 0.86f, 1f), false);
            checkmark.rectTransform.anchorMin = new Vector2(0.22f, 0.22f);
            checkmark.rectTransform.anchorMax = new Vector2(0.78f, 0.78f);
            checkmark.rectTransform.offsetMin = Vector2.zero;
            checkmark.rectTransform.offsetMax = Vector2.zero;
            toggle.targetGraphic = box;
            toggle.graphic = checkmark;
            TMP_Text text = CreateText("Label", parent, label, font, 15f, TextAlignmentOptions.Left, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetRect(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(145f, 30f), position + new Vector2(91f, 0f));
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

        private static NarrativeCommanderIdentityView BuildIdentitySurface(Transform parent, Sprite frame, TMP_FontAsset bold, TMP_FontAsset medium)
        {
            RectTransform surface = CreateRect("CommanderIdentitySurface", parent);
            SetRect(surface, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1480f, 760f), Vector2.zero);
            Image backing = surface.gameObject.AddComponent<Image>();
            backing.sprite = frame;
            backing.type = Image.Type.Sliced;
            NarrativeCommanderIdentityView view = surface.gameObject.AddComponent<NarrativeCommanderIdentityView>();

            TMP_Text title = CreateText("Title", surface, "ESTABLISH COMMAND AUTHORITY", bold, 38f, TextAlignmentOptions.Center, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 66f), new Vector2(0f, -96f));
            TMP_Text instruction = CreateText("Instruction", surface, "Confirm the identity ARIA will use for command communications.", medium, 23f, TextAlignmentOptions.Center, new Color(0.2f, 0.18f, 0.14f, 1f));
            SetRect(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1040f, 54f), new Vector2(0f, -150f));

            Sprite neutralPortrait = RequireAsset<Sprite>("Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_portrait_01_commander_portrait_shadowed.png");
            Image selectedPortrait = CreateImage("SelectedPortrait", surface, neutralPortrait, Color.white, false);
            SetRect(selectedPortrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(320f, 320f), new Vector2(250f, -20f));
            selectedPortrait.preserveAspect = true;

            TMP_Text callsignLabel = CreateText("CallsignLabel", surface, "CALLSIGN", medium, 18f, TextAlignmentOptions.Left, new Color(0.28f, 0.25f, 0.2f, 1f));
            SetRect(callsignLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(650f, 30f), new Vector2(820f, 152f));
            TMP_InputField callsign = CreateInputField("CallsignInput", surface, frame, bold, medium, "COMMANDER");
            SetRect(callsign.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(650f, 112f), new Vector2(820f, 75f));
            TMP_Text displayLabel = CreateText("DisplayNameLabel", surface, "DISPLAY NAME", medium, 18f, TextAlignmentOptions.Left, new Color(0.28f, 0.25f, 0.2f, 1f));
            SetRect(displayLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(650f, 30f), new Vector2(820f, 2f));
            TMP_InputField displayName = CreateInputField("DisplayNameInput", surface, frame, bold, medium, "Commander");
            SetRect(displayName.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(650f, 112f), new Vector2(820f, -75f));

            Button continueButton = CreateFramedButton("ContinueButton", surface, frame, bold, "CONTINUE", new Vector2(430f, 96f));
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(430f, 96f), new Vector2(0f, 95f));

            TMP_Text callsignAccess = CreateHiddenAccessibilityText("CallsignAccessibilityLabel", callsign.transform, medium);
            TMP_Text nameAccess = CreateHiddenAccessibilityText("DisplayNameAccessibilityLabel", displayName.transform, medium);
            TMP_Text continueAccess = CreateHiddenAccessibilityText("ContinueAccessibilityLabel", continueButton.transform, medium);
            SetObject(view, "callsignInput", callsign);
            SetObject(view, "displayNameInput", displayName);
            SetObject(view, "selectedPortraitImage", selectedPortrait);
            SetObject(view, "defaultPortrait", neutralPortrait);
            SetObject(view, "continueButton", continueButton);
            SetObject(view, "callsignAccessibilityLabel", callsignAccess);
            SetObject(view, "displayNameAccessibilityLabel", nameAccess);
            SetObject(view, "continueAccessibilityLabel", continueAccess);
            surface.gameObject.SetActive(false);
            return view;
        }

        private static NarrativeGuidanceChoiceView BuildGuidanceSurface(Transform parent, Sprite frame, TMP_FontAsset bold, TMP_FontAsset medium)
        {
            RectTransform surface = CreateRect("GuidanceChoiceSurface", parent);
            SetRect(surface, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1480f, 760f), Vector2.zero);
            Image backing = surface.gameObject.AddComponent<Image>();
            backing.sprite = frame;
            backing.type = Image.Type.Sliced;
            NarrativeGuidanceChoiceView view = surface.gameObject.AddComponent<NarrativeGuidanceChoiceView>();

            TMP_Text title = CreateText("Title", surface, "CHOOSE ARIA GUIDANCE", bold, 38f, TextAlignmentOptions.Center, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 66f), new Vector2(0f, -100f));
            TMP_Text instruction = CreateText("Instruction", surface, "This can be changed later in Command Settings.", medium, 23f, TextAlignmentOptions.Center, new Color(0.2f, 0.18f, 0.14f, 1f));
            SetRect(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 50f), new Vector2(0f, -154f));

            Button full = CreateFramedButton("FullGuidanceButton", surface, frame, bold, "FULL GUIDANCE", new Vector2(360f, 180f));
            Button contextual = CreateFramedButton("ContextualGuidanceButton", surface, frame, bold, "TACTICAL HINTS", new Vector2(360f, 180f));
            Button minimal = CreateFramedButton("MinimalGuidanceButton", surface, frame, bold, "VETERAN", new Vector2(360f, 180f));
            SetRect(full.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 180f), new Vector2(-410f, 10f));
            SetRect(contextual.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 180f), new Vector2(0f, 10f));
            SetRect(minimal.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 180f), new Vector2(410f, 10f));
            Image fullSelection = CreateSelectionRail(full.transform);
            Image contextualSelection = CreateSelectionRail(contextual.transform);
            Image minimalSelection = CreateSelectionRail(minimal.transform);

            Button continueButton = CreateFramedButton("ContinueButton", surface, frame, bold, "CONTINUE", new Vector2(430f, 96f));
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(430f, 96f), new Vector2(0f, 95f));
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

        private static NarrativeSkipConfirmationView BuildSkipConfirmationSurface(Transform parent, Sprite frame, TMP_FontAsset bold, TMP_FontAsset medium)
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
            SetRect(modal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 430f), Vector2.zero);
            Image backing = modal.gameObject.AddComponent<Image>();
            backing.sprite = frame;
            backing.type = Image.Type.Sliced;
            TMP_Text title = CreateText("Title", modal, "SKIP TO TACTICAL COMMAND?", bold, 34f, TextAlignmentOptions.Center, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(720f, 60f), new Vector2(0f, -92f));
            TMP_Text body = CreateText("Body", modal, "Default Commander identity and Full Guidance will be used. You can change both later.", medium, 23f, TextAlignmentOptions.Center, new Color(0.18f, 0.16f, 0.12f, 1f));
            SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(690f, 100f), new Vector2(0f, 28f));
            Button cancel = CreateFramedButton("CancelButton", modal, frame, bold, "KEEP WATCHING", new Vector2(320f, 82f));
            Button confirm = CreateFramedButton("ConfirmButton", modal, frame, bold, "SKIP INTRO", new Vector2(320f, 82f));
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 82f), new Vector2(-185f, 76f));
            SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 82f), new Vector2(185f, 76f));
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
            TMP_Text text = CreateText("Text", backing.transform, value, bold, 30f, TextAlignmentOptions.Left, new Color(0.08f, 0.075f, 0.06f, 1f));
            SetStretchOffsets(text.rectTransform, new Vector2(90f, 16f), new Vector2(-70f, -40f));
            TMP_Text placeholder = CreateText("Placeholder", backing.transform, string.Empty, medium, 18f, TextAlignmentOptions.Left, new Color(0.35f, 0.32f, 0.26f, 0.75f));
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
            TMP_Text text = CreateText("Label", image.transform, label, font, 26f, TextAlignmentOptions.Center, new Color(0.08f, 0.075f, 0.06f, 1f));
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

        private static void Capture(
            int width,
            int height,
            string panelPath,
            UISubtitleSize subtitleSize,
            UISubtitleBackgroundOpacity opacity,
            string text,
            string outputPath)
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
            scaler.referenceResolution = new Vector2(width, height);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject prefab = RequireAsset<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
            NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
            view.SetVisible(true);
            view.ApplyPanel(new NarrativePanelPresentationModel
            {
                StateId = "FL-P04",
                PanelSprite = RequireAsset<Sprite>(panelPath),
                Tint = Color.white
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
            view.DialogueView.PrepareLine(text, NarrativeSubtitleStyleResolver.Resolve(settings));
            view.DialogueView.CompleteLine();
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
            scaler.referenceResolution = new Vector2(width, height);
            scaler.matchWidthOrHeight = 0.5f;
            GameObject instance = PrefabUtility.InstantiatePrefab(RequireAsset<GameObject>(PrefabPath), canvas.transform) as GameObject;
            NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
            view.SetVisible(true);
            view.ApplyPanel(new NarrativePanelPresentationModel { StateId = "review", PanelSprite = RequireAsset<Sprite>(panelPath), Tint = Color.white });
            view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
            view.SetSkipState(true, true, "SKIP");
            view.SetInteractiveState(kind);
            if (kind == NarrativeInteractiveStateKind.CommanderIdentity)
                view.CommanderIdentityView.ResetToDefaults();
            else if (kind == NarrativeInteractiveStateKind.GuidanceChoice)
                view.GuidanceChoiceView.ResetToDefault();
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
                view.DialogueView.PrepareLine("Families and road crews are trapped beyond the clinic route.", NarrativeSubtitleStyleResolver.Resolve(settings));
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
            scaler.referenceResolution = new Vector2(width, height);
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
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
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

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new UnityException($"Missing serialized property {target.GetType().Name}.{propertyName}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
