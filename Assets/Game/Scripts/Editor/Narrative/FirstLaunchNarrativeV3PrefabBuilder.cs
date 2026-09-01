using System;
using System.Collections.Generic;
using System.IO;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using RTLTMPro;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeV3PrefabBuilder
    {
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string PersianFontPath = "Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Narrative SDF.asset";
        private const string SelectedFramePath = "Assets/Game/Art/UI/Panels/scn09_build_card_frame_selected_check.png";
        private const string LanguageBackgroundPath = "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P08.png";
        private const string IdentityBackgroundPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_background_21x9_no_ui.png";
        private const string GuidanceBackgroundPath = "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P09.png";
        private const string ComicBackground16Path = "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png";
        private const string ComicBackground20Path = "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P04.png";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color Border = new Color32(63, 78, 83, 255);
        private static readonly Color DarkTop = new Color32(20, 29, 32, 255);
        private static readonly Color DarkBottom = new Color32(2, 8, 10, 255);
        private static readonly Color Cyan = new Color32(18, 184, 231, 255);
        private static readonly Color Green = new Color32(52, 181, 69, 255);
        private static readonly Color Lime = new Color32(124, 192, 49, 255);
        private static readonly Color Amber = new Color32(255, 180, 0, 255);
        private static readonly Color Orange = new Color32(255, 100, 15, 255);
        private static readonly Color White = new Color32(242, 243, 238, 255);
        private static readonly Color Muted = new Color32(177, 185, 182, 255);

        private static TMP_FontAsset bold;
        private static TMP_FontAsset medium;
        private static TMP_FontAsset persian;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;

        [MenuItem("Game/UI/V3/Rebuild First Launch V3 Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            FirstLaunchNarrativePresentationPrefabBuilder.Build();
            LoadDependencies();
            RestyleLanguageChoice();
            RestyleNarrativeSequence();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[FirstLaunchNarrativeV3PrefabBuilder] result=Passed screens=4 layout=1672x941 gradients=procedural borders=3 atlases=shared");
        }

        [MenuItem("Game/UI/V3/Validate First Launch V3 Final")]
        public static void Validate()
        {
            GameObject language = RequireAsset<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath);
            GameObject narrative = RequireAsset<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            if (language.GetComponent<FirstLaunchLanguageChoiceView>() == null)
                throw new UnityException("First Launch V3 language prefab is missing its runtime view.");
            if (narrative.GetComponent<NarrativeSequenceView>() == null)
                throw new UnityException("First Launch V3 narrative prefab is missing its runtime view.");
            if (language.GetComponentsInChildren<V3GradientGraphic>(true).Length < 5)
                throw new UnityException("First Launch V3 language screen is missing procedural gradients.");
            if (narrative.GetComponentsInChildren<V3GradientGraphic>(true).Length < 24)
                throw new UnityException("First Launch V3 narrative screens are missing procedural gradients.");
            Transform identity = narrative.transform.Find("SafeArea/CommanderIdentitySurface");
            if (identity == null || identity.GetComponentsInChildren<Button>(true).Length < 8)
                throw new UnityException("First Launch V3 commander identity surface is incomplete.");
            RequireAsset<Sprite>(IdentityBackgroundPath);
            if (identity.Find("Background") != null)
                throw new UnityException("First Launch V3 identity background must use the full-canvas narrative panel, not a composition-limited duplicate.");
            if (narrative.transform.Find("SafeArea/GuidanceChoiceSurface/AriaPortrait") == null)
                throw new UnityException("First Launch V3 ARIA guidance portrait is missing.");
            if (narrative.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true).Length != 1)
                throw new UnityException("First Launch V3 narrative prefab needs exactly one reference-layout controller.");
            ValidateSelectableRaycasts(language, "language choice");
            ValidateSelectableRaycasts(narrative, "narrative sequence");
            Debug.Log("[FirstLaunchNarrativeV3PrefabBuilder] validation=Passed language=select-then-continue identity=6 comic=complete guidance=complete");
        }

        [MenuItem("Game/UI/V3/Capture First Launch Review 1920x1080")]
        public static void CaptureReview1920() => CaptureReview(1920, 1080, "16x9");

        [MenuItem("Game/UI/V3/Capture First Launch Review 4800x2160")]
        public static void CaptureReview4800() => CaptureReview(4800, 2160, "20x9");

        private static void LoadDependencies()
        {
            bold = RequireAsset<TMP_FontAsset>(BoldFontPath);
            medium = RequireAsset<TMP_FontAsset>(MediumFontPath);
            persian = RequireAsset<TMP_FontAsset>(PersianFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
        }

        private static void RestyleLanguageChoice()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

                FirstLaunchLanguageChoiceView view = root.GetComponent<FirstLaunchLanguageChoiceView>();
                CanvasGroup group = root.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                // Keep the authored UI inside the centered 1672x941 composition, but let
                // the non-stretched background cover the physical canvas at ultrawide ratios.
                // Otherwise the composition leaves black gutters at 20:9.
                Image background = CreateImage("Background", root.transform, RequireAsset<Sprite>(LanguageBackgroundPath), Color.white, false);
                Stretch(background.rectTransform);
                AddCover(background, 16f / 9f);
                V3GradientGraphic shade = CreateGradient("Shade", root.transform, new Color(0f, 0f, 0f, .18f), new Color(0f, 0f, 0f, .70f), Color.clear, 0f);
                Stretch(shade.rectTransform);

                RectTransform composition = CreateComposition(root.transform);
                BuildBrandLogo(composition, 20f, 18f, 395f, 116f);

                TMP_Text title = CreateText("Title", composition, "SELECT STORY LANGUAGE", 58f, bold, TextAlignmentOptions.Center, White);
                SetTopLeft(title.rectTransform, 390f, 142f, 900f, 86f);
                List<Sprite> portraits = LoadCommanderPortraits();
                Button englishButton = BuildLanguageCard(composition, "EnglishButton", 70f, 295f, 745f, 365f, portraits[2], "ENGLISH", "People are counting on\nus. Keep them safe.", Cyan, false, out Behaviour englishSelection);
                Button persianButton = BuildLanguageCard(composition, "PersianButton", 850f, 295f, 745f, 365f, portraits[1], "فارسی", "مردم به ما امید دارند.\nآن‌ها را ایمن نگه دارید.", Orange, true, out Behaviour persianSelection);

                RectTransform info = CreateTopLeft("InfoPanel", composition, 10f, 763f, 518f, 162f);
                CreateGradientOn(info, DarkTop, DarkBottom, Border, 3f);
                TMP_Text infoIcon = CreateText("InfoIcon", info, "i", 52f, bold, TextAlignmentOptions.Center, Cyan);
                SetTopLeft(infoIcon.rectTransform, 22f, 35f, 78f, 78f);
                TMP_Text infoText = CreateText("InfoText", info, "This can be changed later\nin Command Settings.", 26f, medium, TextAlignmentOptions.MidlineLeft, White);
                SetTopLeft(infoText.rectTransform, 120f, 22f, 370f, 112f);

                Button continueButton = CreateGradientButton("ContinueButton", composition, 536f, 771f, 1106f, 145f,
                    new Color32(24, 132, 69, 255), new Color32(2, 70, 40, 255), Green, 3f);
                TMP_Text continueLabel = CreateText("Label", continueButton.transform, "CONTINUE   ›", 63f, bold, TextAlignmentOptions.Center, White);
                Stretch(continueLabel.rectTransform);

                SetObject(view, "group", group);
                SetObject(view, "englishButton", englishButton);
                SetObject(view, "persianButton", persianButton);
                SetObject(view, "continueButton", continueButton);
                SetObject(view, "englishSelectionImage", englishSelection);
                SetObject(view, "persianSelectionImage", persianSelection);
                PrefabUtility.SaveAsPrefabAsset(root, FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Button BuildLanguageCard(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Sprite portrait,
            string language,
            string sample,
            Color accent,
            bool rtl,
            out Behaviour selection)
        {
            Button button = CreateGradientButton(name, parent, x, y, width, height, new Color32(13, 25, 29, 245), new Color32(1, 10, 12, 250), accent, 3f);
            Image portraitImage = CreateImage("Portrait", button.transform, portrait, Color.white, false);
            SetTopLeft(portraitImage.rectTransform, 10f, 12f, 265f, height - 18f);
            portraitImage.preserveAspect = true;
            BuildGlobe(button.transform, 325f, 73f, 132f, accent);
            TMP_FontAsset font = rtl ? persian : bold;
            TMP_Text label = CreateText("Language", button.transform, language, rtl ? 53f : 51f, font, TextAlignmentOptions.Center, accent);
            SetTopLeft(label.rectTransform, 455f, 65f, 260f, 105f);
            CreateSolid("Rule", button.transform, 318f, 201f, 392f, 2f, accent);
            TMP_Text sampleText = CreateText("Sample", button.transform, sample, rtl ? 29f : 27f, rtl ? persian : medium, TextAlignmentOptions.Center, White);
            SetTopLeft(sampleText.rectTransform, 305f, 220f, 420f, 120f);
            selection = BuildSelectionFrame(button.transform, accent);
            ((V3SelectionFrameView)selection).SetVisible(name == "EnglishButton");
            return button;
        }

        private static void RestyleNarrativeSequence()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            try
            {
                NarrativeSequenceView sequence = root.GetComponent<NarrativeSequenceView>();
                RectTransform safeArea = root.transform.Find("SafeArea") as RectTransform;
                if (safeArea == null)
                    throw new UnityException("First Launch narrative prefab is missing SafeArea.");
                string[] replaced = { "Dialogue", "LocationIntroduction", "PlaybackControls", "CommanderIdentitySurface", "GuidanceChoiceSurface" };
                foreach (string childName in replaced)
                {
                    Transform child = safeArea.Find(childName);
                    if (child != null)
                        Object.DestroyImmediate(child.gameObject);
                }
                Stretch(safeArea);
                safeArea.localScale = Vector3.one;
                MainMenuV3SectionLayoutView layout = safeArea.gameObject.GetComponent<MainMenuV3SectionLayoutView>() ?? safeArea.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
                layout.Configure(Reference, MainMenuV3SectionAlignment.Center);

                NarrativeLocationIntroView location = BuildComicHeader(safeArea, out NarrativePlaybackControlsView controls);
                NarrativeDialogueView dialogue = BuildComicDialogue(safeArea);
                NarrativeCommanderIdentityView identity = BuildIdentitySurface(safeArea);
                NarrativeGuidanceChoiceView guidance = BuildGuidanceSurface(safeArea);
                Transform skip = safeArea.Find("SkipConfirmationSurface");
                Transform reviewer = safeArea.Find("DevelopmentReviewerControls");
                if (skip != null) skip.SetAsLastSibling();
                if (reviewer != null) reviewer.SetAsLastSibling();

                SetObject(sequence, "dialogueView", dialogue);
                SetObject(sequence, "locationIntroView", location);
                SetObject(sequence, "playbackControls", controls);
                SetObject(sequence, "commanderIdentityView", identity);
                SetObject(sequence, "guidanceChoiceView", guidance);
                AssignLocalizedBindings(sequence, safeArea);
                PrefabUtility.SaveAsPrefabAsset(root, FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static NarrativeLocationIntroView BuildComicHeader(Transform parent, out NarrativePlaybackControlsView controlsView)
        {
            RectTransform location = CreateTopLeft("LocationIntroduction", parent, 9f, 9f, 410f, 102f);
            CreateGradientOn(location, DarkTop, DarkBottom, Border, 3f);
            CanvasGroup locationGroup = location.gameObject.AddComponent<CanvasGroup>();
            NarrativeLocationIntroView locationView = location.gameObject.AddComponent<NarrativeLocationIntroView>();
            Image targetIcon = CreateImage("TargetIcon", location, RequireAsset<Sprite>(V3UiFoundationBuilder.FirstLaunchTargetIconPath), Cyan, false);
            SetTopLeft(targetIcon.rectTransform, 19f, 20f, 58f, 58f);
            targetIcon.preserveAspect = true;
            TMP_Text locationName = CreateText("LocationName", location, "SAHRIN", 30f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(locationName.rectTransform, 100f, 8f, 270f, 44f);
            TMP_Text district = CreateText("DistrictAndTime", location, "OLD MARKET / 10:00 LOCAL", 20f, medium, TextAlignmentOptions.MidlineLeft, Muted);
            SetTopLeft(district.rectTransform, 100f, 53f, 290f, 36f);
            SetObject(locationView, "group", locationGroup);
            SetObject(locationView, "titleText", locationName);
            SetObject(locationView, "subtitleText", district);

            RectTransform timeline = CreateTopLeft("ComicTimeline", parent, 422f, 9f, 650f, 102f);
            CreateGradientOn(timeline, DarkTop, DarkBottom, Border, 3f);
            TMP_Text state = CreateText("State", timeline, "FL-P04", 22f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(state.rectTransform, 42f, 24f, 120f, 52f);
            TMP_Text page = CreateText("Page", timeline, "4 / 26", 20f, bold, TextAlignmentOptions.Center, White);
            SetTopLeft(page.rectTransform, 185f, 24f, 90f, 52f);
            CreateSolid("Track", timeline, 305f, 43f, 290f, 16f, new Color32(45, 53, 56, 255));
            CreateSolid("Fill", timeline, 305f, 43f, 72f, 16f, Cyan);
            CreateSolid("Handle", timeline, 370f, 31f, 14f, 40f, White);

            RectTransform controls = CreateTopLeft("PlaybackControls", parent, 1083f, 9f, 580f, 102f);
            controlsView = controls.gameObject.AddComponent<NarrativePlaybackControlsView>();
            BuildHeaderControl(controls, "PauseButton", 0f, 166f, "PAUSE", V3UiFoundationBuilder.FirstLaunchPauseIconPath);
            BuildHeaderControl(controls, "SubtitlesButton", 171f, 230f, "SUBTITLES", null);
            Button skip = CreateGradientButton("SkipButton", controls, 406f, 0f, 174f, 102f, DarkTop, DarkBottom, Border, 3f);
            CanvasGroup skipGroup = skip.gameObject.AddComponent<CanvasGroup>();
            BuildChevronIcon(skip.transform, 18f, 29f, 44f, 44f, White, true);
            TMP_Text skipLabel = CreateText("Label", skip.transform, "SKIP", 23f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(skipLabel.rectTransform, 74f, 19f, 88f, 64f);
            SetObject(controlsView, "skipGroup", skipGroup);
            SetObject(controlsView, "skipButton", skip);
            SetObject(controlsView, "skipLabel", skipLabel);
            return locationView;
        }

        private static void BuildHeaderControl(Transform parent, string name, float x, float width, string label, string iconPath)
        {
            Button button = CreateGradientButton(name, parent, x, 0f, width, 102f, DarkTop, DarkBottom, Border, 3f);
            if (name == "PauseButton")
                BuildPauseIcon(button.transform, 18f, 29f, 42f, White);
            else
                BuildSpeechBubbleIcon(button.transform, 18f, 31f, 43f, Cyan);
            TMP_Text text = CreateText("Label", button.transform, label, 22f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(text.rectTransform, 69f, 18f, width - 78f, 66f);
        }

        private static NarrativeDialogueView BuildComicDialogue(Transform parent)
        {
            RectTransform root = CreateTopLeft("Dialogue", parent, 10f, 682f, 1652f, 249f);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            NarrativeDialogueView view = root.gameObject.AddComponent<NarrativeDialogueView>();

            Image frame = CreateImage("Frame", root, null, Color.clear, false);
            SetTopLeft(frame.rectTransform, 0f, 0f, 1438f, 249f);
            RectTransform body = CreateTopLeft("DialogueBody", root, 0f, 0f, 1438f, 249f);
            CreateGradientOn(body, new Color32(20, 30, 33, 250), new Color32(2, 8, 10, 252), Border, 3f);
            RectTransform next = CreateTopLeft("NextPanel", root, 1445f, 0f, 207f, 249f);
            CreateGradientOn(next, new Color32(37, 113, 45, 255), new Color32(8, 50, 20, 255), Green, 3f);

            Image portrait = CreateImage("Portrait", root, null, Color.white, false);
            SetTopLeft(portrait.rectTransform, 11f, 11f, 228f, 228f);
            portrait.preserveAspect = true;
            RectTransform portraitBorder = CreateTopLeft("PortraitBorder", root, 11f, 11f, 228f, 228f);
            CreateGradientOn(portraitBorder, Color.clear, Color.clear, Cyan, 3f);
            Image aria = CreateImage("AriaIcon", root, null, Color.white, false);
            SetTopLeft(aria.rectTransform, 11f, 11f, 228f, 228f);
            aria.preserveAspect = true;
            aria.gameObject.SetActive(false);
            TMP_Text name = CreateText("SpeakerName", root, "DALIA RAHIM", 42f, bold, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(name.rectTransform, 270f, 12f, 330f, 58f);
            TMP_Text role = CreateText("SpeakerRole", root, "JRC FIELD COMMAND", 22f, medium, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(role.rectTransform, 550f, 25f, 390f, 39f);
            TMP_Text line = CreateText("DialogueText", root, string.Empty, 30f, medium, TextAlignmentOptions.TopLeft, White);
            SetTopLeft(line.rectTransform, 270f, 78f, 1130f, 106f);
            line.textWrappingMode = TextWrappingModes.Normal;
            BuildChevronIcon(root, 270f, 193f, 32f, 30f, Cyan, false);
            CreateSolid("VoiceTrack", root, 318f, 207f, 1050f, 8f, new Color32(45, 53, 56, 255));
            CreateSolid("VoiceFill", root, 318f, 207f, 745f, 8f, Cyan);
            TMP_Text accessibility = CreateText("AccessibilityText", root, string.Empty, 1f, medium, TextAlignmentOptions.Left, Color.clear);
            Stretch(accessibility.rectTransform);
            TMP_Text advance = CreateText("AdvanceIndicator", root, string.Empty, 1f, bold, TextAlignmentOptions.Center, Color.clear);
            SetTopLeft(advance.rectTransform, 0f, 0f, 1f, 1f);

            Image pointer = CreateImage("Pointer", root, null, Color.clear, false);
            SetTopLeft(pointer.rectTransform, 1479f, 38f, 138f, 108f);
            CreateLine("Upper", pointer.transform, 31f, 16f, 76f, 54f, 14f, White);
            CreateLine("Lower", pointer.transform, 76f, 54f, 31f, 92f, 14f, White);
            TMP_Text nextLabel = CreateText("NextLabel", root, "NEXT", 29f, bold, TextAlignmentOptions.Center, White);
            SetTopLeft(nextLabel.rectTransform, 1465f, 163f, 165f, 52f);
            Image input = CreateImage("InputSurface", root, null, Color.clear, true);
            Stretch(input.rectTransform);
            Button inputButton = input.gameObject.AddComponent<Button>();
            inputButton.transition = Selectable.Transition.None;

            SetObject(view, "dialogueGroup", group);
            SetObject(view, "dialogueRect", root);
            SetObject(view, "frameImage", frame);
            SetObject(view, "pointerImage", pointer);
            SetObject(view, "portraitImage", portrait);
            SetObject(view, "ariaIconImage", aria);
            SetObject(view, "speakerNameText", name);
            SetObject(view, "speakerRoleText", role);
            SetObject(view, "dialogueText", line);
            SetObject(view, "accessibilityText", accessibility);
            SetObject(view, "advanceIndicator", advance.gameObject);
            SetObject(view, "inputButton", inputButton);
            SerializedObject serializedView = new(view);
            serializedView.FindProperty("useAuthoredHeight").boolValue = true;
            serializedView.FindProperty("authoredFontScale").floatValue = 0.6f;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static NarrativeCommanderIdentityView BuildIdentitySurface(Transform parent)
        {
            RectTransform surface = CreateTopLeft("CommanderIdentitySurface", parent, 0f, 0f, Reference.x, Reference.y);
            V3GradientGraphic shade = CreateGradient("Shade", surface, new Color(0f, 0f, 0f, .66f), new Color(0f, 0f, 0f, .91f), Color.clear, 0f);
            Stretch(shade.rectTransform);
            NarrativeCommanderIdentityView view = surface.gameObject.AddComponent<NarrativeCommanderIdentityView>();
            BuildBrandLogo(surface, 13f, 13f, 300f, 78f);
            RectTransform auth = CreateTopLeft("AuthenticationHeader", surface, 327f, 13f, 1332f, 78f);
            CreateGradientOn(auth, DarkTop, DarkBottom, Border, 3f);
            BuildWarningTriangle(auth, 18f, 23f, 30f, Orange);
            TMP_Text authText = CreateText("AuthText", auth, "EMERGENCY CONTINUITY AUTHENTICATION", 24f, bold, TextAlignmentOptions.MidlineLeft, Orange);
            SetTopLeft(authText.rectTransform, 58f, 8f, 930f, 60f);
            TMP_Text step = CreateText("Step", auth, "2 / 3", 25f, bold, TextAlignmentOptions.MidlineRight, Orange);
            SetTopLeft(step.rectTransform, 1130f, 8f, 170f, 60f);
            TMP_Text title = CreateText("Title", surface, "CHOOSE YOUR COMMANDER IDENTITY", 44f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(title.rectTransform, 53f, 119f, 1080f, 62f);
            TMP_Text instruction = CreateText("Instruction", surface, "Your commander leads every operation. Choose your identity.", 24f, medium, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(instruction.rectTransform, 53f, 178f, 1040f, 46f);

            List<Sprite> allPortraits = LoadCommanderPortraits();
            string[] names = { "MIRA ALAVI", "JALIL OKAFOR", "SAMIRA KHALID", "DALIA RAHIM", "KENJI SATO", "IRINA PETROVA" };
            string[] roles = { "RECON SPECIALIST", "ASSAULT LEADER", "INTEL OFFICER", "FIELD COMMANDER", "OPERATIONS LEAD", "SUPPORT COMMANDER" };
            Color[] accents = { Lime, Orange, Cyan, Green, Lime, Cyan };
            var portraitButtons = new Button[6];
            var portraitImages = new Image[6];
            var selections = new Behaviour[6];
            var access = new TMP_Text[6];
            for (int i = 0; i < 6; i++)
            {
                float x = 54f + i * 262f;
                Button card = CreateGradientButton($"PortraitButton_{i + 1:00}", surface, x, 238f, 246f, 425f, new Color32(35, 41, 42, 250), new Color32(7, 13, 15, 252), Border, 3f);
                Image portrait = CreateImage("Portrait", card.transform, allPortraits[i], Color.white, false);
                SetTopLeft(portrait.rectTransform, 3f, 3f, 240f, 275f);
                portrait.preserveAspect = true;
                TMP_Text cardName = CreateText("Name", card.transform, names[i], 21f, bold, TextAlignmentOptions.Center, White);
                SetTopLeft(cardName.rectTransform, 8f, 284f, 230f, 38f);
                TMP_Text role = CreateText("Role", card.transform, roles[i], i == 3 ? 18f : 16f, medium, TextAlignmentOptions.Center,
                    i == 3 ? new Color32(145, 255, 118, 255) : Muted);
                SetTopLeft(role.rectTransform, 6f, 324f, 234f, 36f);
                BuildCommanderRoleIcon(card.transform, i, accents[i]);
                Behaviour selected = BuildSelectionFrame(card.transform, Green);
                TMP_Text hidden = CreateText("AccessibilityLabel", card.transform, string.Empty, 1f, medium, TextAlignmentOptions.Left, Color.clear);
                hidden.gameObject.SetActive(false);
                portraitButtons[i] = card;
                portraitImages[i] = portrait;
                selections[i] = selected;
                access[i] = hidden;
            }

            RectTransform callsignPanel = CreateTopLeft("CallsignPanel", surface, 54f, 677f, 540f, 150f);
            CreateGradientOn(callsignPanel, DarkTop, DarkBottom, Border, 3f);
            TMP_Text callsignLabel = CreateText("CallsignLabel", callsignPanel, "CALLSIGN", 19f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(callsignLabel.rectTransform, 22f, 9f, 200f, 35f);
            TMP_InputField callsign = CreateInputField("CallsignInput", callsignPanel, "ECHO-7");
            SetTopLeft(callsign.GetComponent<RectTransform>(), 24f, 48f, 415f, 54f);
            TMP_Text hint = CreateText("Hint", callsignPanel, "3 - 12 characters, letters, numbers, hyphens only", 14f, medium, TextAlignmentOptions.MidlineLeft, Muted);
            SetTopLeft(hint.rectTransform, 23f, 108f, 490f, 28f);
            Image edit = CreateImage("Edit", callsignPanel, RequireAsset<Sprite>(V3UiFoundationBuilder.CommanderEditIconPath), White, false);
            SetTopLeft(edit.rectTransform, 458f, 53f, 47f, 47f);

            RectTransform selectedProfile = CreateTopLeft("SelectedProfile", surface, 610f, 677f, 1003f, 150f);
            CreateGradientOn(selectedProfile, new Color32(18, 54, 35, 245), DarkBottom, Border, 3f);
            TMP_Text selectedTitle = CreateText("SelectedTitle", selectedProfile, "SELECTED PROFILE", 18f, bold, TextAlignmentOptions.MidlineLeft, Green);
            SetTopLeft(selectedTitle.rectTransform, 30f, 9f, 260f, 34f);
            Image rank = CreateImage("Rank", selectedProfile, RequireAsset<Sprite>(V3UiFoundationBuilder.CommanderUpgradesIconPath), Amber, false);
            SetTopLeft(rank.rectTransform, 31f, 50f, 75f, 75f);
            TMP_Text selectedName = CreateText("SelectedName", selectedProfile, "DALIA RAHIM", 24f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(selectedName.rectTransform, 140f, 48f, 330f, 38f);
            TMP_Text selectedRole = CreateText("SelectedRole", selectedProfile, "FIELD COMMANDER", 17f, medium, TextAlignmentOptions.MidlineLeft, Green);
            SetTopLeft(selectedRole.rectTransform, 140f, 81f, 330f, 34f);
            TMP_Text selectedDesc = CreateText("SelectedDescription", selectedProfile, "Balanced leader with strong operational control.", 15f, medium, TextAlignmentOptions.MidlineLeft, Muted);
            SetTopLeft(selectedDesc.rectTransform, 140f, 111f, 720f, 28f);

            Button previous = CreateGradientButton("PreviousButton", surface, 13f, 838f, 824f, 90f, new Color32(24, 72, 128, 255), new Color32(5, 35, 71, 255), new Color32(55, 115, 177, 255), 3f);
            TMP_Text prevLabel = CreateText("Label", previous.transform, "‹   PREV", 37f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(prevLabel.rectTransform, 36f, 8f, 730f, 70f);
            Button continueButton = CreateGradientButton("ContinueButton", surface, 845f, 838f, 814f, 90f, new Color32(58, 130, 53, 255), new Color32(15, 70, 27, 255), Green, 3f);
            TMP_Text continueLabel = CreateText("Label", continueButton.transform, "CONTINUE   ›", 40f, bold, TextAlignmentOptions.Center, White);
            Stretch(continueLabel.rectTransform);

            Image selectedPortrait = CreateImage("SelectedPortrait", surface, allPortraits[3], Color.white, false);
            SetTopLeft(selectedPortrait.rectTransform, 0f, 0f, 1f, 1f);
            selectedPortrait.gameObject.SetActive(false);
            TMP_InputField displayName = CreateInputField("DisplayNameInput", surface, "Commander");
            displayName.gameObject.SetActive(false);
            TMP_Text callsignAccess = HiddenText("CallsignAccessibilityLabel", callsign.transform);
            TMP_Text displayAccess = HiddenText("DisplayNameAccessibilityLabel", displayName.transform);
            TMP_Text continueAccess = HiddenText("ContinueAccessibilityLabel", continueButton.transform);
            SetObject(view, "callsignInput", callsign);
            SetObject(view, "displayNameInput", displayName);
            SetObject(view, "selectedPortraitImage", selectedPortrait);
            SetObject(view, "defaultPortrait", allPortraits[3]);
            SetArray(view, "portraitButtons", portraitButtons);
            SetArray(view, "portraitImages", portraitImages);
            SetArray(view, "portraitSelectionImages", selections);
            SetArray(view, "portraitAccessibilityLabels", access);
            SetObject(view, "continueButton", continueButton);
            SetObject(view, "callsignAccessibilityLabel", callsignAccess);
            SetObject(view, "displayNameAccessibilityLabel", displayAccess);
            SetObject(view, "continueAccessibilityLabel", continueAccess);
            surface.gameObject.SetActive(false);
            return view;
        }

        private static NarrativeGuidanceChoiceView BuildGuidanceSurface(Transform parent)
        {
            RectTransform surface = CreateTopLeft("GuidanceChoiceSurface", parent, 0f, 0f, Reference.x, Reference.y);
            CreateGradientOn(surface, new Color32(12, 20, 23, 255), new Color32(1, 5, 7, 255), Color.clear, 0f);
            NarrativeGuidanceChoiceView view = surface.gameObject.AddComponent<NarrativeGuidanceChoiceView>();
            BuildBrandLogo(surface, 20f, 14f, 450f, 150f);
            TMP_Text eyebrow = CreateText("Eyebrow", surface, "FIRST-LAUNCH SETUP", 25f, bold, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(eyebrow.rectTransform, 510f, 23f, 560f, 38f);
            TMP_Text title = CreateText("Title", surface, "CHOOSE ARIA'S GUIDANCE LEVEL", 43f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(title.rectTransform, 510f, 61f, 900f, 56f);
            TMP_Text instruction = CreateText("Instruction", surface, "Aria will support you based on the level you choose.", 23f, medium, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(instruction.rectTransform, 510f, 118f, 900f, 38f);
            TMP_Text step = CreateText("Step", surface, "3 / 3", 25f, bold, TextAlignmentOptions.MidlineRight, Cyan);
            SetTopLeft(step.rectTransform, 1475f, 30f, 150f, 42f);
            for (int i = 0; i < 3; i++)
                CreateSolid("StepSegment" + i, surface, 1417f + i * 68f, 98f, 62f, 22f, i < 2 ? Cyan : new Color32(38, 48, 52, 255));

            Button full = BuildGuidanceCard(surface, "FullGuidanceButton", 25f, Cyan, "FULL GUIDANCE", "Complete support for\nnew commanders.", "HIGH", 3, V3UiFoundationBuilder.FirstLaunchTargetIconPath, out Behaviour fullSelection);
            Button contextual = BuildGuidanceCard(surface, "ContextualGuidanceButton", 414f, Lime, "TACTICAL HINTS", "Helpful tips in key\nsituations.", "MEDIUM", 2, V3UiFoundationBuilder.FirstLaunchMapIconPath, out Behaviour contextualSelection);
            Button minimal = BuildGuidanceCard(surface, "MinimalGuidanceButton", 803f, Orange, "MINIMAL GUIDANCE", "Only essential alerts.\nMaximum challenge.", "LOW", 1, V3UiFoundationBuilder.CommanderUpgradesIconPath, out Behaviour minimalSelection);

            Sprite ariaSprite = RequireAsset<Sprite>(V3UiFoundationBuilder.FirstLaunchAriaPortraitPath);
            RectTransform ariaPanel = CreateTopLeft("AriaPortrait", surface, 1184f, 164f, 468f, 625f);
            CreateGradientOn(ariaPanel, new Color32(2, 15, 23, 255), new Color32(0, 4, 8, 255), Color.clear, 0f);
            ariaPanel.gameObject.AddComponent<RectMask2D>();
            Image aria = CreateImage("PortraitArt", ariaPanel, ariaSprite, Color.white, false);
            SetTopLeft(aria.rectTransform, 34f, 13f, 400f, 600f);
            aria.preserveAspect = true;
            CreateSolid("PortraitCyanWash", ariaPanel, 34f, 13f, 400f, 600f, new Color(0f, .32f, .48f, .075f));
            BuildAriaTelemetry(ariaPanel);

            RectTransform accessibility = CreateTopLeft("AccessibilityStrip", surface, 20f, 706f, 1112f, 83f);
            CreateGradientOn(accessibility, DarkTop, DarkBottom, Border, 3f);
            RectTransform ccFrame = CreateTopLeft("CCIcon", accessibility, 24f, 16f, 64f, 50f);
            CreateGradientOn(ccFrame, new Color32(12, 28, 34, 255), new Color32(2, 10, 13, 255), Cyan, 3f);
            TMP_Text cc = CreateText("Label", ccFrame, "CC", 25f, bold, TextAlignmentOptions.Center, Cyan);
            Stretch(cc.rectTransform);
            TMP_Text subtitles = CreateText("SubtitlesLabel", accessibility, "SUBTITLES", 22f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(subtitles.rectTransform, 112f, 8f, 210f, 36f);
            TMP_Text subHint = CreateText("SubtitlesHint", accessibility, "Show dialogue subtitles.", 16f, medium, TextAlignmentOptions.MidlineLeft, Muted);
            SetTopLeft(subHint.rectTransform, 112f, 40f, 260f, 31f);
            BuildToggle("SubtitlesOn", accessibility, 390f, 15f, Cyan);
            Image motionIcon = CreateImage("MotionIcon", accessibility, RequireAsset<Sprite>(V3UiFoundationBuilder.FirstLaunchMotionIconPath), Cyan, false);
            SetTopLeft(motionIcon.rectTransform, 585f, 15f, 61f, 52f);
            motionIcon.preserveAspect = true;
            TMP_Text motion = CreateText("ReducedMotionLabel", accessibility, "REDUCED MOTION", 21f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(motion.rectTransform, 675f, 8f, 250f, 36f);
            TMP_Text motionHint = CreateText("ReducedMotionHint", accessibility, "Minimize camera movement.", 16f, medium, TextAlignmentOptions.MidlineLeft, Muted);
            SetTopLeft(motionHint.rectTransform, 675f, 40f, 270f, 31f);
            BuildToggle("ReducedMotionOn", accessibility, 950f, 15f, Cyan);

            Button previous = CreateGradientButton("PreviousButton", surface, 20f, 803f, 812f, 114f, DarkTop, DarkBottom, Border, 3f);
            TMP_Text prev = CreateText("Label", previous.transform, "‹       PREV", 50f, bold, TextAlignmentOptions.Center, White);
            Stretch(prev.rectTransform);
            Button continueButton = CreateGradientButton("ContinueButton", surface, 840f, 803f, 812f, 114f, new Color32(43, 157, 62, 255), new Color32(8, 74, 25, 255), Green, 3f);
            TMP_Text continueText = CreateText("Label", continueButton.transform, "CONTINUE       ›", 52f, bold, TextAlignmentOptions.Center, White);
            Stretch(continueText.rectTransform);

            SetObject(view, "fullButton", full);
            SetObject(view, "contextualButton", contextual);
            SetObject(view, "minimalButton", minimal);
            SetObject(view, "fullSelectionImage", fullSelection);
            SetObject(view, "contextualSelectionImage", contextualSelection);
            SetObject(view, "minimalSelectionImage", minimalSelection);
            SetObject(view, "continueButton", continueButton);
            SetObject(view, "fullAccessibilityLabel", HiddenText("FullAccessibilityLabel", full.transform));
            SetObject(view, "contextualAccessibilityLabel", HiddenText("ContextualAccessibilityLabel", contextual.transform));
            SetObject(view, "minimalAccessibilityLabel", HiddenText("MinimalAccessibilityLabel", minimal.transform));
            SetObject(view, "continueAccessibilityLabel", HiddenText("ContinueAccessibilityLabel", continueButton.transform));
            surface.gameObject.SetActive(false);
            return view;
        }

        private static Button BuildGuidanceCard(Transform parent, string name, float x, Color accent, string title, string description, string level, int bars, string iconPath, out Behaviour selection)
        {
            Button card = CreateGradientButton(name, parent, x, 183f, 374f, 514f, new Color32(32, 39, 41, 255), new Color32(5, 12, 14, 255), Border, 3f);
            if (name == "ContextualGuidanceButton")
            {
                BuildTacticalMapIcon(card.transform, 103f, 29f, 168f, 155f, accent);
            }
            else
            {
                Image icon = CreateImage("Icon", card.transform, RequireAsset<Sprite>(iconPath), accent, false);
                SetTopLeft(icon.rectTransform, 103f, 29f, 168f, 155f);
                icon.preserveAspect = true;
            }
            TMP_Text label = CreateText("Label", card.transform, title, 31f, bold, TextAlignmentOptions.Center, accent);
            SetTopLeft(label.rectTransform, 20f, 198f, 334f, 53f);
            CreateSolid("Rule", card.transform, 24f, 264f, 326f, 2f, accent);
            TMP_Text desc = CreateText("Description", card.transform, description, 23f, medium, TextAlignmentOptions.Center, White);
            SetTopLeft(desc.rectTransform, 28f, 281f, 318f, 88f);
            RectTransform levelPanel = CreateTopLeft("LevelPanel", card.transform, 45f, 386f, 284f, 104f);
            CreateGradientOn(levelPanel, new Color32(8, 22, 27, 255), new Color32(2, 10, 13, 255), accent, 2f);
            TMP_Text support = CreateText("Support", levelPanel, "SUPPORT LEVEL", 17f, bold, TextAlignmentOptions.Center, accent);
            SetTopLeft(support.rectTransform, 8f, 7f, 268f, 30f);
            TMP_Text levelText = CreateText("Level", levelPanel, level, 31f, bold, TextAlignmentOptions.Center, accent);
            SetTopLeft(levelText.rectTransform, 8f, 38f, 268f, 38f);
            for (int i = 0; i < 3; i++)
                CreateSolid("Bar" + i, levelPanel, 13f + i * 87f, 79f, 79f, 17f, i < bars ? accent : new Color32(57, 64, 66, 255));
            selection = BuildSelectionFrame(card.transform, accent);
            return card;
        }

        private static void AssignLocalizedBindings(NarrativeSequenceView sequence, Transform safeArea)
        {
            TMP_Text[] targets =
            {
                FindText(safeArea, "PlaybackControls/SkipButton/Label"),
                FindText(safeArea, "CommanderIdentitySurface/AuthenticationHeader/AuthText"),
                FindText(safeArea, "CommanderIdentitySurface/Title"),
                FindText(safeArea, "CommanderIdentitySurface/CallsignPanel/CallsignLabel"),
                FindText(safeArea, "CommanderIdentitySurface/ContinueButton/Label"),
                FindText(safeArea, "GuidanceChoiceSurface/Title"),
                FindText(safeArea, "GuidanceChoiceSurface/Instruction"),
                FindText(safeArea, "GuidanceChoiceSurface/FullGuidanceButton/Label"),
                FindText(safeArea, "GuidanceChoiceSurface/ContextualGuidanceButton/Label"),
                FindText(safeArea, "GuidanceChoiceSurface/MinimalGuidanceButton/Label"),
                FindText(safeArea, "GuidanceChoiceSurface/ContinueButton/Label")
            };
            string[] keys =
            {
                "narrative.first_launch.control.skip", "narrative.first_launch.identity.title", "narrative.first_launch.identity.instruction",
                "narrative.first_launch.identity.callsign", "narrative.first_launch.control.continue", "narrative.first_launch.guidance.title",
                "narrative.first_launch.guidance.instruction", "narrative.first_launch.guidance.full", "narrative.first_launch.guidance.contextual",
                "narrative.first_launch.guidance.minimal", "narrative.first_launch.control.continue"
            };
            string[] fallbacks =
            {
                "SKIP", "EMERGENCY CONTINUITY AUTHENTICATION", "CHOOSE YOUR COMMANDER IDENTITY", "CALLSIGN", "CONTINUE   ›",
                "CHOOSE ARIA'S GUIDANCE LEVEL", "Aria will support you based on the level you choose.", "FULL GUIDANCE", "TACTICAL HINTS",
                "MINIMAL GUIDANCE", "CONTINUE       ›"
            };
            SetArray(sequence, "localizedTextTargets", targets);
            SetStringArray(sequence, "localizedTextKeys", keys);
            SetStringArray(sequence, "localizedTextEnglishFallbacks", fallbacks);
        }

        private static void CaptureReview(int width, int height, string suffix)
        {
            Build();
            MainMenuV3PrefabBuilder.SetGameViewResolution(width, height);
            CaptureLanguage(width, height, $"/private/tmp/warline-first-launch-language-v3-{suffix}.png");
            CaptureInteractive(width, height, NarrativeInteractiveStateKind.CommanderIdentity, $"/private/tmp/warline-first-launch-identity-v3-{suffix}.png");
            CaptureComic(width, height, suffix == "20x9" ? ComicBackground20Path : ComicBackground16Path, $"/private/tmp/warline-first-launch-comic-v3-{suffix}.png");
            CaptureInteractive(width, height, NarrativeInteractiveStateKind.GuidanceChoice, $"/private/tmp/warline-first-launch-guidance-v3-{suffix}.png");
            Debug.Log($"[FirstLaunchNarrativeV3PrefabBuilder] capture=Passed size={width}x{height} suffix={suffix}");
        }

        private static void CaptureLanguage(int width, int height, string path)
        {
            CapturePrefab(width, height, FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath, instance =>
            {
                instance.GetComponent<FirstLaunchLanguageChoiceView>().SetVisible(true);
            }, path);
        }

        private static void CaptureInteractive(int width, int height, NarrativeInteractiveStateKind kind, string path)
        {
            CapturePrefab(width, height, FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath, instance =>
            {
                NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
                view.SetVisible(true);
                view.ApplyPanel(new NarrativePanelPresentationModel
                {
                    StateId = "review",
                    PanelSprite = RequireAsset<Sprite>(kind == NarrativeInteractiveStateKind.CommanderIdentity ? IdentityBackgroundPath : GuidanceBackgroundPath),
                    Tint = Color.white
                });
                view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
                view.SetSkipState(false, false, "SKIP");
                view.SetInteractiveState(kind);
                if (kind == NarrativeInteractiveStateKind.CommanderIdentity)
                {
                    view.CommanderIdentityView.SetIdentity("ECHO-7", "Commander", 3);
                    view.CommanderIdentityView.SetControlsInteractable(true);
                }
                else
                {
                    view.GuidanceChoiceView.SetSelectedGuidance(NarrativeGuidanceMode.Full);
                    view.GuidanceChoiceView.SetControlsInteractable(true);
                }
            }, path);
        }

        private static void CaptureComic(int width, int height, string backgroundPath, string path)
        {
            CapturePrefab(width, height, FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath, instance =>
            {
                NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
                view.SetVisible(true);
                view.ApplyPanel(new NarrativePanelPresentationModel { StateId = "FL-P04", PanelSprite = RequireAsset<Sprite>(backgroundPath), Tint = Color.white });
                view.ApplyLocation(new NarrativeLocationPresentationModel { Visible = true, Title = "SAHRIN", Subtitle = "OLD MARKET / 10:00 LOCAL" });
                view.SetInteractiveState(NarrativeInteractiveStateKind.None);
                view.SetSkipState(true, true, "SKIP");
                view.DialogueView.ApplySpeaker(new NarrativeSpeakerPresentationModel
                {
                    SpeakerId = NarrativeSpeakerId.Dalia,
                    DisplayName = "DALIA RAHIM",
                    Role = "JRC FIELD COMMAND",
                    AccessibleLabel = "Major Dalia Rahim, JRC Field Command",
                    IdentitySprite = RequireAsset<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.DaliaPortraitPath),
                    AccentColor = Cyan,
                    Treatment = NarrativeSpeakerTreatment.HumanPortrait
                });
                UISettingsModel settings = Game.UI.Runtime.SettingsService.Defaults;
                view.DialogueView.PrepareLine("District Dispatch, Major Dalia Rahim, JRC Field Command.\nWe found the convoy survivors. Extraction is underway.", NarrativeSubtitleStyleUtilitySystemHelper.Resolve(settings));
                view.DialogueView.CompleteLine();
            }, path);
        }

        private static void CapturePrefab(int width, int height, string prefabPath, Action<GameObject> configure, string outputPath)
        {
            GameObject cameraObject = new("FirstLaunchV3CaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            GameObject canvasObject = new("FirstLaunchV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 0.5f;
            GameObject instance = PrefabUtility.InstantiatePrefab(RequireAsset<GameObject>(prefabPath), canvas.transform) as GameObject;
            configure(instance);
            Canvas.ForceUpdateCanvases();
            foreach (MainMenuV3SectionLayoutView layout in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                layout.RefreshLayout();
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

        private static RectTransform CreateComposition(Transform parent)
        {
            RectTransform composition = CreateRect("Composition", parent);
            Stretch(composition);
            composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(Reference, MainMenuV3SectionAlignment.Center);
            return composition;
        }

        private static Behaviour BuildSelectionFrame(Transform parent, Color accent)
        {
            RectTransform frame = CreateRect("Selection", parent);
            Stretch(frame);
            CanvasGroup group = frame.gameObject.AddComponent<CanvasGroup>();
            Color selectedTop = new(accent.r, accent.g, accent.b, 0.025f);
            Color selectedBottom = new(accent.r, accent.g, accent.b, 0.18f);
            V3GradientGraphic border = CreateGradientOn(frame, selectedTop, selectedBottom, accent, 3f);
            border.raycastTarget = false;

            RectTransform corner = CreateRect("CheckCorner", frame);
            corner.anchorMin = new Vector2(1f, 1f);
            corner.anchorMax = new Vector2(1f, 1f);
            corner.pivot = new Vector2(1f, 1f);
            corner.sizeDelta = new Vector2(62f, 52f);
            corner.anchoredPosition = Vector2.zero;
            CreateGradientOn(corner, accent * 1.1f, accent * 0.62f, accent, 2f);
            CreateLine("CheckShort", corner, 12f, 26f, 25f, 38f, 6f, White);
            CreateLine("CheckLong", corner, 25f, 38f, 50f, 12f, 6f, White);

            V3SelectionFrameView selection = frame.gameObject.AddComponent<V3SelectionFrameView>();
            selection.Configure(group);
            return selection;
        }

        private static void BuildCommanderRoleIcon(Transform parent, int index, Color accent)
        {
            if (index == 2)
            {
                BuildAntennaIcon(parent, 91f, 360f, 64f, 56f, Cyan);
                return;
            }

            if (index == 5)
            {
                RectTransform support = CreateTopLeft("RoleIcon", parent, 91f, 360f, 64f, 56f);
                Image shield = CreateImage("Shield", support, RequireAsset<Sprite>(V3UiFoundationBuilder.CommanderBadgeIconPath), Cyan, false);
                Stretch(shield.rectTransform);
                shield.preserveAspect = true;
                Image plus = CreateImage("Plus", support, RequireAsset<Sprite>(V3UiFoundationBuilder.CommanderSupportIconPath), White, false);
                SetTopLeft(plus.rectTransform, 20f, 16f, 24f, 24f);
                plus.preserveAspect = true;
                return;
            }

            string path = index switch
            {
                0 => V3UiFoundationBuilder.FirstLaunchTargetIconPath,
                1 => V3UiFoundationBuilder.OperationsRaidIconPath,
                3 => V3UiFoundationBuilder.CommanderUpgradesIconPath,
                4 => V3UiFoundationBuilder.FirstLaunchTargetIconPath,
                _ => V3UiFoundationBuilder.CommanderBadgeIconPath
            };
            Color color = index == 3 ? Amber : accent;
            Image icon = CreateImage("RoleIcon", parent, RequireAsset<Sprite>(path), color, false);
            SetTopLeft(icon.rectTransform, 91f, 360f, 64f, 56f);
            icon.preserveAspect = true;
        }

        private static void BuildAntennaIcon(Transform parent, float x, float y, float width, float height, Color accent)
        {
            RectTransform icon = CreateTopLeft("RoleIcon", parent, x, y, width, height);
            Image ring = CreateImage("SignalRing", icon, RequireAsset<Sprite>(V3UiFoundationBuilder.FirstLaunchGlobeRingPath), accent, false);
            SetTopLeft(ring.rectTransform, 7f, 1f, 50f, 44f);
            CreateSolid("Mast", icon, 29f, 9f, 6f, 38f, accent);
            CreateSolid("Base", icon, 16f, 46f, 32f, 5f, accent);
            CreateSolid("Beacon", icon, 26f, 2f, 12f, 12f, accent);
        }

        private static void BuildSpeechBubbleIcon(Transform parent, float x, float y, float size, Color accent)
        {
            RectTransform bubble = CreateTopLeft("Icon", parent, x, y, size, size * 0.72f);
            CreateGradientOn(bubble, new Color32(14, 30, 35, 255), new Color32(3, 12, 15, 255), accent, 3f);
            CreateSolid("Tail", bubble, 7f, size * 0.64f, 12f, 10f, accent);
            for (int i = 0; i < 3; i++)
                CreateSolid("Dot" + i, bubble, 9f + i * 10f, 12f, 5f, 5f, White);
        }

        private static void BuildPauseIcon(Transform parent, float x, float y, float size, Color color)
        {
            RectTransform icon = CreateTopLeft("Icon", parent, x, y, size, size);
            CreateSolid("LeftBar", icon, 7f, 2f, 9f, size - 4f, color);
            CreateSolid("RightBar", icon, 26f, 2f, 9f, size - 4f, color);
        }

        private static void BuildChevronIcon(Transform parent, float x, float y, float width, float height, Color color, bool doubled)
        {
            RectTransform icon = CreateTopLeft("Icon", parent, x, y, width, height);
            float firstX = doubled ? 5f : 2f;
            CreateLine("UpperA", icon, firstX, 5f, firstX + 13f, height * 0.5f, 5f, color);
            CreateLine("LowerA", icon, firstX + 13f, height * 0.5f, firstX, height - 5f, 5f, color);
            if (!doubled)
                return;
            CreateLine("UpperB", icon, firstX + 15f, 5f, firstX + 28f, height * 0.5f, 5f, color);
            CreateLine("LowerB", icon, firstX + 28f, height * 0.5f, firstX + 15f, height - 5f, 5f, color);
        }

        private static void BuildToggle(string name, Transform parent, float x, float y, Color accent)
        {
            RectTransform root = CreateTopLeft(name, parent, x, y, 150f, 52f);
            TMP_Text label = CreateText("Label", root, "ON", 22f, bold, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(label.rectTransform, 0f, 4f, 48f, 40f);
            RectTransform track = CreateTopLeft("Track", root, 54f, 7f, 78f, 36f);
            CreateGradientOn(track, new Color32(24, 40, 46, 255), new Color32(5, 15, 19, 255), Border, 2f);
            CreateSolid("Knob", track, 44f, 4f, 29f, 28f, accent);
        }

        private static void BuildTacticalMapIcon(Transform parent, float x, float y, float width, float height, Color accent)
        {
            RectTransform icon = CreateTopLeft("Icon", parent, x, y, width, height);
            CreateLine("FoldLeft", icon, 22f, 18f, 22f, 134f, 6f, accent);
            CreateLine("FoldMiddle", icon, 77f, 8f, 77f, 122f, 6f, accent);
            CreateLine("FoldRight", icon, 135f, 18f, 135f, 134f, 6f, accent);
            CreateLine("TopLeft", icon, 22f, 18f, 77f, 8f, 6f, accent);
            CreateLine("TopRight", icon, 77f, 8f, 135f, 18f, 6f, accent);
            CreateLine("BottomLeft", icon, 22f, 134f, 77f, 122f, 6f, accent);
            CreateLine("BottomRight", icon, 77f, 122f, 135f, 134f, 6f, accent);
            CreateLine("RouteA", icon, 38f, 96f, 71f, 69f, 5f, accent);
            CreateLine("RouteB", icon, 71f, 69f, 105f, 89f, 5f, accent);
            CreateLine("RouteC", icon, 105f, 89f, 129f, 50f, 5f, accent);
            Image marker = CreateImage("RouteMarker", icon, RequireAsset<Sprite>(V3UiFoundationBuilder.FirstLaunchGlobeRingPath), accent, false);
            SetTopLeft(marker.rectTransform, 120f, 39f, 24f, 24f);
        }

        private static void CreateLine(string name, Transform parent, float x1, float y1, float x2, float y2, float thickness, Color color)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            RectTransform line = CreateRect(name, parent);
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(0f, 1f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.sizeDelta = new Vector2(length, thickness);
            line.anchoredPosition = new Vector2((x1 + x2) * 0.5f, -(y1 + y2) * 0.5f);
            line.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
            Image image = line.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void BuildBrandLogo(Transform parent, float x, float y, float width, float height)
        {
            RectTransform logo = CreateTopLeft("BrandLogo", parent, x, y, width, height);
            V3UiFoundationBuilder.AddMainMenuLogo(logo, left: 0f, top: 0f, right: 0f, bottom: 0f);
        }

        private static void BuildGlobe(Transform parent, float x, float y, float size, Color color)
        {
            RectTransform globe = CreateTopLeft("Globe", parent, x, y, size, size);
            AddScaledRing("Outer", globe, Vector3.one, color, 5f);
            AddScaledRing("Longitude", globe, new Vector3(.43f, 1f, 1f), color, 5f);
            AddScaledRing("Latitude", globe, new Vector3(1f, .43f, 1f), color, 5f);
            CreateSolid("Equator", globe, 4f, size * .5f - 2f, size - 8f, 4f, color);
        }

        private static void AddScaledRing(string name, Transform parent, Vector3 scale, Color color, float thickness)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            rect.localScale = scale;
            V3RingGraphic ring = rect.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, thickness, 64);
        }

        private static void BuildWarningTriangle(Transform parent, float x, float y, float size, Color color)
        {
            RectTransform icon = CreateTopLeft("WarningIcon", parent, x, y, size, size);
            CreateLine("Left", icon, size * .5f, 1f, 1f, size - 2f, 3f, color);
            CreateLine("Right", icon, size * .5f, 1f, size - 1f, size - 2f, 3f, color);
            CreateLine("Bottom", icon, 1f, size - 2f, size - 1f, size - 2f, 3f, color);
            CreateSolid("Stem", icon, size * .5f - 1.5f, 8f, 3f, 10f, color);
            CreateSolid("Dot", icon, size * .5f - 2f, 21f, 4f, 4f, color);
        }

        private static void BuildAriaTelemetry(Transform parent)
        {
            Color faint = new Color(Cyan.r, Cyan.g, Cyan.b, .48f);
            Color dim = new Color(Cyan.r, Cyan.g, Cyan.b, .24f);
            CreateSolid("LeftRail", parent, 12f, 48f, 3f, 500f, dim);
            CreateSolid("RightRail", parent, 453f, 54f, 3f, 492f, dim);
            for (int i = 0; i < 7; i++)
            {
                float y = 70f + i * 38f;
                CreateSolid("LeftTick" + i, parent, 12f, y, 19f + (i % 3) * 8f, 3f, i % 2 == 0 ? faint : dim);
            }
            for (int i = 0; i < 8; i++)
            {
                float y = 60f + i * 34f;
                float width = 14f + (i % 4) * 7f;
                CreateSolid("RightTick" + i, parent, 442f - width, y, width, 3f, i % 3 == 0 ? faint : dim);
            }

            RectTransform reticle = CreateTopLeft("TelemetryReticle", parent, 8f, 421f, 58f, 58f);
            V3RingGraphic outer = reticle.gameObject.AddComponent<V3RingGraphic>();
            outer.Configure(faint, 3f, 48);
            CreateSolid("Horizontal", reticle, 2f, 27f, 54f, 3f, faint);
            CreateSolid("Vertical", reticle, 27f, 2f, 3f, 54f, faint);
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string value)
        {
            RectTransform rect = CreateRect(name, parent);
            CreateGradientOn(rect, new Color32(22, 30, 32, 255), new Color32(4, 10, 12, 255), Border, 2f);
            TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
            TMP_Text text = CreateText("Text", rect, value, 26f, bold, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(text.rectTransform, 14f, 2f, 385f, 48f);
            TMP_Text placeholder = CreateText("Placeholder", rect, string.Empty, 18f, medium, TextAlignmentOptions.MidlineLeft, Muted);
            Stretch(placeholder.rectTransform);
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value;
            input.characterLimit = 32;
            input.targetGraphic = rect.GetComponent<V3GradientGraphic>();
            input.targetGraphic.raycastTarget = true;
            return input;
        }

        private static Button CreateGradientButton(string name, Transform parent, float x, float y, float width, float height, Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = CreateGradientOn(rect, top, bottom, border, borderWidth);
            graphic.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(.82f, .87f, .90f, 1f);
            colors.disabledColor = new Color(.42f, .45f, .46f, .75f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            return button;
        }

        private static void ValidateSelectableRaycasts(GameObject prefab, string screenName)
        {
            Selectable[] selectables = prefab.GetComponentsInChildren<Selectable>(true);
            if (selectables.Length == 0)
                throw new MissingComponentException($"First Launch V3 {screenName} has no interactive controls.");

            for (int i = 0; i < selectables.Length; i++)
            {
                Graphic target = selectables[i].targetGraphic;
                if (target == null || !target.raycastTarget)
                {
                    throw new MissingReferenceException(
                        $"First Launch V3 {screenName} control '{selectables[i].name}' cannot receive real pointer input.");
                }
            }
        }

        private static V3GradientGraphic CreateGradient(string name, Transform parent, Color top, Color bottom, Color border, float width)
        {
            RectTransform rect = CreateRect(name, parent);
            return CreateGradientOn(rect, top, bottom, border, width);
        }

        private static V3GradientGraphic CreateGradientOn(RectTransform rect, Color top, Color bottom, Color border, float width)
        {
            V3GradientGraphic graphic = rect.gameObject.GetComponent<V3GradientGraphic>() ?? rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.raycastTarget = false;
            graphic.Configure(top, bottom, border, width);
            return graphic;
        }

        private static Image CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
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

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            RTLTextMeshPro text = rect.gameObject.AddComponent<RTLTextMeshPro>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static TMP_Text HiddenText(string name, Transform parent)
        {
            TMP_Text text = CreateText(name, parent, string.Empty, 1f, medium, TextAlignmentOptions.Left, Color.clear);
            text.gameObject.SetActive(false);
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject value = new(name, typeof(RectTransform));
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent);
            SetTopLeft(rect, x, y, width, height);
            return rect;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddCover(Image image, float aspect)
        {
            image.preserveAspect = true;
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = aspect;
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target == null || !target.TryGetComponent(out TMP_Text text))
                throw new UnityException($"Missing First Launch V3 localized text target: {path}");
            return text;
        }

        private static List<Sprite> LoadCommanderPortraits()
        {
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(FirstLaunchNarrativeDialogueAssetImporter.CommanderPortraitSheetPath);
            List<Sprite> portraits = new();
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name.StartsWith("commander_", StringComparison.Ordinal))
                    portraits.Add(sprite);
            }
            portraits.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            if (portraits.Count < 6)
                throw new UnityException($"First Launch V3 requires six commander portraits; found {portraits.Count}.");
            return portraits;
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new UnityException($"Required First Launch V3 asset missing: {path}");
            return asset;
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
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStringArray(Object target, string propertyName, string[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
                throw new UnityException($"Missing serialized string array {target.GetType().Name}.{propertyName}");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
