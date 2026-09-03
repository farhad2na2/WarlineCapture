#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class MatchHudV3PrefabBuilder
    {
        internal const string PrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string PersianFontPath =
            "Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Narrative SDF.asset";
        private const string CaptureBackgroundPath =
            "Design/AgentReports/M02EstablishBase/M02EB-029/current_gameplay_zoom.png";
        private const string PassengerRiflePortraitPath =
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_01_Alt_02_AdvancedRifle_Card_512.png";
        private const string PassengerEngineerPortraitPath =
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_02_Alt_04_Rifleman_Card_512.png";
        private const string PassengerDaliaPortraitPath =
            "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_dalia.png";
        private const string PassengerRifleBPortraitPath =
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_02_Alt_02_Rifleman_Card_512.png";
        private const string TransportHelicopterPortraitPath =
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Helicopter_Transport_Action_512.png";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(25, 34, 38, 247);
        private static readonly Color DarkBottom = new Color32(3, 8, 10, 252);
        private static readonly Color RaisedTop = new Color32(38, 50, 55, 250);
        private static readonly Color Line = new Color32(151, 164, 167, 255);
        private static readonly Color CyanTop = new Color32(14, 139, 185, 255);
        private static readonly Color CyanBottom = new Color32(3, 57, 83, 255);
        private static readonly Color BlueTop = new Color32(27, 111, 180, 255);
        private static readonly Color BlueBottom = new Color32(4, 43, 82, 255);
        private static readonly Color GreenTop = new Color32(52, 153, 68, 255);
        private static readonly Color GreenBottom = new Color32(10, 66, 28, 255);
        private static readonly Color RedTop = new Color32(190, 54, 31, 255);
        private static readonly Color RedBottom = new Color32(78, 14, 12, 255);
        private static readonly Color AmberTop = new Color32(190, 130, 18, 255);
        private static readonly Color AmberBottom = new Color32(76, 42, 3, 255);
        private static readonly Color OliveTop = new Color32(115, 130, 43, 255);
        private static readonly Color OliveBottom = new Color32(40, 49, 13, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;

        [MenuItem("Game/UI/V3/Rebuild Match HUD V3 Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                RectTransform composition = EnsureComposition(root.transform);
                RectTransform header = RequireRect(root.transform, "HeaderContent");
                RectTransform left = RequireRect(root.transform, "LeftContent");
                RectTransform right = RequireRect(root.transform, "RightContent");
                RectTransform footer = RequireRect(root.transform, "FooterContent");
                RemoveResponsiveLayouts(header, left, right, footer);
                MountSection(header, composition);
                MountSection(left, composition);
                MountSection(right, composition);
                MountSection(footer, composition);

                StyleHeader(header, footer);
                StyleSelection(left, footer);
                StyleFooter(footer, right);
                InstallTacticalFeedbackPreview(header);
                InstallCommandWheel(root.transform, footer);
                ConfigureResponsiveLayouts(composition, header, left, right, footer);
                StyleTypography(root.transform);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[MatchHudV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 atlases=shared match=runtime-bound");
        }

        [MenuItem("Game/UI/V3/Capture Match HUD V3 Review")]
        public static void CaptureReview()
        {
            Build();
            // The approved SCN-08 lock shows the contextual tutorial state. Keep
            // the production prefab hidden-by-default, but stage that state for
            // review so the proof image validates the complete, single ARIA panel.
            Capture("/private/tmp/warline-match-hud-v3-16x9.png", 1920, 1080,
                showTutorialPresentation: true);
            Capture("/private/tmp/warline-match-hud-v3-20x9.png", 4800, 2160,
                showTutorialPresentation: true);
        }

        [MenuItem("Game/UI/V3/Capture M02 Restricted Controls Review")]
        public static void CaptureM02RestrictedControlsReview()
        {
            // This is a presentation-state proof only; do not rebuild the prefab as a side
            // effect. It stages the exact M02 unavailable categories against the current asset.
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();
            Capture("/private/tmp/warline-m02-restricted-controls-v3-16x9.png", 1920, 1080,
                showM02RestrictedControls: true);
            Capture("/private/tmp/warline-m02-restricted-controls-v3-20x9.png", 4800, 2160,
                showM02RestrictedControls: true);
        }

        [MenuItem("Game/UI/V3/Capture M02 Restricted Controls Persian Review")]
        public static void CaptureM02RestrictedControlsPersianReview()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();
            GameLocalizationCatalog localization =
                AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            if (localization == null)
                throw new FileNotFoundException($"Missing localization catalog: {V3UiLocalizationCatalogBuilder.CatalogPath}");

            GameLocalization.Initialize(localization, GameLocalization.PersianLocaleCode, persist: false);
            try
            {
                Capture("/private/tmp/warline-m02-restricted-controls-fa-v3-16x9.png", 1920, 1080,
                    showM02RestrictedControls: true,
                    applyLocalization: true);
                Capture("/private/tmp/warline-m02-restricted-controls-fa-v3-20x9.png", 4800, 2160,
                    showM02RestrictedControls: true,
                    applyLocalization: true);
            }
            finally
            {
                GameLocalization.Initialize(localization, GameLocalization.EnglishLocaleCode, persist: false);
            }
        }

        [MenuItem("Game/UI/V3/Capture Unit Command Wheel V3 Review")]
        public static void CaptureCommandWheelReview()
        {
            Build();
            Capture("/private/tmp/warline-command-wheel-v3-16x9.png", 1920, 1080, true, false);
            Capture("/private/tmp/warline-command-wheel-v3-20x9.png", 4800, 2160, true, false);
            Capture("/private/tmp/warline-command-wheel-targeting-v3-16x9.png", 1920, 1080, true, true);
            Capture("/private/tmp/warline-command-wheel-targeting-v3-20x9.png", 4800, 2160, true, true);
        }

        [MenuItem("Game/UI/V3/Capture Transport Passengers V3 Review")]
        public static void CaptureTransportPassengersReview()
        {
            Build();
            Capture("/private/tmp/warline-transport-passengers-v3-16x9.png", 1920, 1080, false, false, true);
            Capture("/private/tmp/warline-transport-passengers-v3-20x9.png", 4800, 2160, false, false, true);
        }

        [MenuItem("Game/UI/V3/Capture Tactical Feedback V3 Review")]
        public static void CaptureTacticalFeedbackReview()
        {
            Build();
            Capture("/private/tmp/warline-tactical-feedback-v3-16x9.png", 1920, 1080, false, false, false, true);
            Capture("/private/tmp/warline-tactical-feedback-v3-20x9.png", 4800, 2160, false, false, false, true);
        }

        [MenuItem("Game/UI/V3/Capture Build Placement Confirmation V3 Review")]
        public static void CaptureBuildPlacementConfirmationReview()
        {
            Build();
            BuildPlacementConfirmationBarPrefabSetupEditor.Setup();
            Capture("/private/tmp/warline-build-placement-v3-16x9.png", 1920, 1080,
                showBuildPlacement: true);
            Capture("/private/tmp/warline-build-placement-v3-20x9.png", 4800, 2160,
                showBuildPlacement: true);
            Capture("/private/tmp/warline-build-placement-validity-v3-16x9.png", 1920, 1080,
                showBuildPlacement: true, showBuildPlacementInvalid: true);
            Capture("/private/tmp/warline-build-placement-validity-v3-20x9.png", 4800, 2160,
                showBuildPlacement: true, showBuildPlacementInvalid: true);
        }

        [MenuItem("Game/UI/V3/Capture Tutorial Presentation V3 Review")]
        public static void CaptureTutorialPresentationReview()
        {
            Build();
            Capture("/private/tmp/warline-tutorial-presentation-v3-16x9.png", 1920, 1080,
                showTutorialPresentation: true);
            Capture("/private/tmp/warline-tutorial-presentation-v3-20x9.png", 4800, 2160,
                showTutorialPresentation: true);
        }

        [MenuItem("Game/UI/V3/Capture ARIA Command Assistant V3 Review")]
        public static void CaptureAriaCommandAssistantReview()
        {
            Build();
            AriaTutorialBriefingPrefabBuilder.Build();
            AriaCommandAssistantV3PrefabBuilder.Build();
            Capture("/private/tmp/warline-aria-command-assistant-v3-16x9.png", 1920, 1080,
                showCommandAssistant: true);
            Capture("/private/tmp/warline-aria-command-assistant-v3-20x9.png", 4800, 2160,
                showCommandAssistant: true);
        }

        [MenuItem("Game/UI/V3/Capture Assistant Takeover V3 Review")]
        public static void CaptureAssistantTakeoverReview()
        {
            Build();
            AriaTutorialBriefingPrefabBuilder.Build();
            AriaCommandAssistantV3PrefabBuilder.Build();
            Capture("/private/tmp/warline-assistant-takeover-v3-16x9.png", 1920, 1080,
                showAssistantTakeover: true);
            Capture("/private/tmp/warline-assistant-takeover-v3-20x9.png", 4800, 2160,
                showAssistantTakeover: true);
        }

        [MenuItem("Game/UI/V3/Capture POP-01 Threat Alert V3 Review")]
        public static void CaptureThreatAlertReview()
        {
            Build();
            ThreatAlertV3PrefabBuilder.Build();
            Capture("/private/tmp/warline-threat-alert-v3-16x9.png", 1920, 1080,
                showThreatAlert: true);
            Capture("/private/tmp/warline-threat-alert-v3-20x9.png", 4800, 2160,
                showThreatAlert: true);
            Capture("/private/tmp/warline-threat-route-preview-v3-16x9.png", 1920, 1080,
                showThreatRoutePreview: true);
            Capture("/private/tmp/warline-threat-route-preview-v3-20x9.png", 4800, 2160,
                showThreatRoutePreview: true);
        }

        [MenuItem("Game/UI/V3/Validate Match HUD V3 Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Match HUD prefab: {PrefabPath}");

            MainMenuV3SectionLayoutView[] layouts = prefab.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true);
            if (layouts.Length != 4)
                throw new InvalidOperationException("Match HUD V3 must serialize one responsive layout for each independently mounted shell section.");
            for (int i = 0; i < layouts.Length; i++)
            {
                if (layouts[i].ReferenceResolution != Reference || !layouts[i].ExpandToCanvasWidth)
                    throw new InvalidOperationException($"Match HUD section {layouts[i].name} must fill wide canvases from the 1672x941 reference.");
            }
            if (prefab.GetComponent<UIShellContentSectionsView>() == null ||
                prefab.GetComponentInChildren<MatchHudSelectionPanelView>(true) == null ||
                prefab.GetComponentInChildren<MatchHudSquadTrayView>(true) == null ||
                prefab.GetComponentInChildren<MatchHudMinimapView>(true) == null)
                throw new MissingReferenceException("Match HUD V3 runtime bindings are incomplete.");
            MatchOverlayCommandControlsView controls = prefab.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            CommandWheelPanelView wheel = prefab.GetComponentInChildren<CommandWheelPanelView>(true);
            if (controls == null || controls.CommandWheelPanel != wheel || controls.CommandWheelStopButton == null)
                throw new MissingReferenceException("Unit Command Wheel runtime bindings are incomplete.");
            if (wheel.GetComponentsInChildren<V3RadialWedgeGraphic>(true).Length != 6)
                throw new InvalidOperationException("Unit Command Wheel must contain exactly six procedural radial sectors.");

            Transform rail = FindDeepChild(prefab.transform, "CommandRail");
            Transform railFrame = rail != null ? FindDirectChild(rail, "Frame") : null;
            if (railFrame == null || CountDirectButtons(railFrame) != 8)
                throw new InvalidOperationException("Match HUD V3 command rail must contain eight functional command buttons.");
            RequireCommandRailPointerTargets(railFrame);
            Transform right = FindDeepChild(prefab.transform, "RightContent");
            if (right == null || right.gameObject.activeSelf)
                throw new InvalidOperationException("The obsolete right quick rail must be disabled after its commands are relocated.");
            Transform aria = FindDeepChild(prefab.transform, "AriaAssistantButton");
            if (aria == null || aria.GetComponentInChildren<MatchHudMinimapView>(true) == null)
                throw new InvalidOperationException("The minimap must remain runtime-bound and attached to the expanded ARIA panel.");
            AriaTutorialBriefingView embeddedTutorial =
                aria.GetComponent<AriaTutorialBriefingView>();
            if (embeddedTutorial == null || !embeddedTutorial.TryBindHierarchy() ||
                embeddedTutorial.CloseButton != null ||
                embeddedTutorial.BriefingLayout.gameObject.activeSelf)
            {
                throw new InvalidOperationException(
                    "Match HUD must contain one hidden-by-default embedded ARIA tutorial surface with DO IT/SHOW ME and no Skip.");
            }
            RectTransform ariaRect = aria as RectTransform;
            RectTransform resourceStrip = FindDeepChild(prefab.transform, "ResourceStrip") as RectTransform;
            RectTransform threatStrip = FindDeepChild(prefab.transform, "ThreatJumpPanel") as RectTransform;
            MainMenuV3SectionLayoutView headerLayout =
                FindDeepChild(prefab.transform, "HeaderContent")?.GetComponent<MainMenuV3SectionLayoutView>();
            Vector2 ariaBase = default;
            Vector2 resourceBase = default;
            Vector2 threatBase = default;
            bool hasAriaBase = headerLayout != null &&
                               headerLayout.TryGetAuthoredBasePosition(ariaRect, out ariaBase);
            bool hasResourceBase = headerLayout != null &&
                                   headerLayout.TryGetAuthoredBasePosition(resourceStrip, out resourceBase);
            bool hasThreatBase = headerLayout != null &&
                                  headerLayout.TryGetAuthoredBasePosition(threatStrip, out threatBase);
            if (!hasAriaBase || Mathf.Abs(ariaBase.x - 1257f) > .1f ||
                !hasResourceBase || Mathf.Abs(resourceBase.x - 414f) > .1f ||
                !hasThreatBase || Mathf.Abs(threatBase.x - 663f) > .1f ||
                threatStrip == null || Mathf.Abs(threatStrip.sizeDelta.x - 525f) > .1f)
            {
                throw new InvalidOperationException(
                    "Match HUD responsive targets lost their serialized authored X positions.");
            }
            Transform settingsButton = FindDeepChild(prefab.transform, "SettingsButton");
            Transform pauseButton = FindDeepChild(prefab.transform, "PauseButton");
            RectTransform settingsIcon = settingsButton != null
                ? FindDeepChild(settingsButton, "Icon") as RectTransform
                : null;
            RectTransform pauseIcon = pauseButton != null
                ? FindDeepChild(pauseButton, "Icon") as RectTransform
                : null;
            if (!IsCenteredIcon(settingsIcon) || !IsCenteredIcon(pauseIcon))
                throw new InvalidOperationException("Match HUD settings and pause icons must remain centered in their buttons.");
            Image ariaPortrait = FindDirectChild(aria, "PortraitClip")?.GetComponentInChildren<Image>(true);
            if (ariaPortrait == null || ariaPortrait.GetComponent<AspectRatioFitter>() == null)
                throw new InvalidOperationException("ARIA portrait must preserve its aspect ratio.");
            MatchHudTransportPassengerDrawerView passengerDrawer =
                prefab.GetComponentInChildren<MatchHudTransportPassengerDrawerView>(true);
            if (passengerDrawer == null)
                throw new MissingReferenceException("Match HUD V3 transport passenger drawer is missing.");
            SerializedObject passengerDrawerSerialized = new(passengerDrawer);
            RectTransform capacitySlots = passengerDrawerSerialized.FindProperty("capacitySlotsRoot")?.objectReferenceValue as RectTransform;
            Button ropeDrop = passengerDrawerSerialized.FindProperty("ropeDropButton")?.objectReferenceValue as Button;
            if (capacitySlots == null || capacitySlots.childCount != 10 || ropeDrop == null)
                throw new MissingReferenceException("Transport passenger V3 capacity and Rope Drop bindings are incomplete.");
            if (passengerDrawer.GetComponentsInChildren<V3GradientGraphic>(true).Length < 15)
                throw new InvalidOperationException("Transport passenger drawer must use sharp procedural V3 gradients and borders.");
            Transform tacticalPreview = FindDeepChild(prefab.transform, "V3TacticalFeedbackPreview");
            if (tacticalPreview == null || tacticalPreview.gameObject.activeSelf ||
                FindDeepChild(tacticalPreview, "RangeBanner") == null ||
                FindDeepChild(tacticalPreview, "HostileTargetMarker") == null ||
                FindDeepChild(tacticalPreview, "AttackRoute") == null ||
                FindDeepChild(tacticalPreview, "FriendlySourceRing")?.GetComponent<V3EllipseRingGraphic>() == null ||
                FindDeepChild(tacticalPreview, "HostileTargetRing")?.GetComponent<V3EllipseRingGraphic>() == null)
            {
                throw new MissingReferenceException("Tactical feedback V3 preview must serialize hidden with range, route, and hostile marker visuals.");
            }
            Transform attackSelection = FindDeepChild(controls.AttackButton.transform, "V3SelectedState");
            if (attackSelection == null || attackSelection.gameObject.activeSelf)
                throw new MissingReferenceException("Attack command must serialize a hidden procedural V3 selected state.");
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(V3UiFoundationBuilder.MatchIconAtlasPath);
            if (atlas == null)
                throw new FileNotFoundException("Missing shared Match HUD V3 icon atlas.");
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 24)
                throw new InvalidOperationException($"Match HUD V3 requires procedural gradients; found {gradients}.");
            int pointerTargets = RequireLiveButtonPointerTargets(prefab);
            Debug.Log($"[MatchHudV3PrefabBuilder] validation=Passed commands=8 squads=5 gradients={gradients} pointerTargets={pointerTargets} passengers=10-slots rope-drop=bound aria=minimap-attached art=aspect-preserved");
        }

        private static bool IsCenteredIcon(RectTransform icon)
        {
            if (icon == null)
                return false;
            Vector2 center = new(.5f, .5f);
            return Vector2.Distance(icon.anchorMin, center) < .001f &&
                   Vector2.Distance(icon.anchorMax, center) < .001f &&
                   Vector2.Distance(icon.pivot, center) < .001f &&
                   Vector2.Distance(icon.anchoredPosition, Vector2.zero) < .001f;
        }

        [MenuItem("Game/UI/V3/Inspect Match HUD V3 Hierarchy")]
        public static void InspectHierarchy()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var report = new StringBuilder(32768);
                report.AppendLine("[MatchHudV3PrefabBuilder] hierarchy-begin");
                AppendHierarchy(report, root.transform, root.transform, 0);
                report.AppendLine("[MatchHudV3PrefabBuilder] hierarchy-end");
                Debug.Log(report.ToString());
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
            if (boldFont == null || mediumFont == null || theme == null || catalog == null)
                throw new MissingReferenceException("Match HUD V3 fonts or shared foundation assets are missing.");
        }

        private static RectTransform EnsureComposition(Transform root)
        {
            RectTransform composition = FindDeepChild(root, "V3Composition") as RectTransform;
            if (composition == null)
                composition = CreateTopLeft("V3Composition", root, 0f, 0f, Reference.x, Reference.y);
            MainMenuV3SectionLayoutView existingLayout = composition.GetComponent<MainMenuV3SectionLayoutView>();
            if (existingLayout != null)
                UnityEngine.Object.DestroyImmediate(existingLayout);
            return composition;
        }

        private static void RemoveResponsiveLayouts(params RectTransform[] sections)
        {
            // ExecuteAlways layout components register driven RectTransform properties in
            // prefab-editing contexts too. Remove them before authoring reference positions,
            // otherwise Unity keeps the previous driven X values (commonly zero) when the
            // rebuilt prefab is saved. ConfigureResponsiveLayouts adds fresh drivers after
            // every reference-space position has been finalized.
            foreach (RectTransform section in sections)
            {
                MainMenuV3SectionLayoutView layout =
                    section != null ? section.GetComponent<MainMenuV3SectionLayoutView>() : null;
                if (layout != null)
                    UnityEngine.Object.DestroyImmediate(layout);
            }
        }

        private static void ConfigureResponsiveLayouts(
            RectTransform composition,
            RectTransform header,
            RectTransform left,
            RectTransform right,
            RectTransform footer)
        {
            ConfigureSectionLayout(
                header,
                new[]
                {
                    RequireRect(header, "AriaAssistantButton"),
                    RequireRect(header, "ThreatJumpPanel")
                },
                new[]
                {
                    RequireRect(header, "ResourceStrip"),
                    RequireRect(header, "SettingsButton"),
                    RequireRect(header, "PauseButton"),
                    RequireRect(header, "V3TacticalFeedbackPreview")
                });
            ConfigureSectionLayout(left, Array.Empty<RectTransform>(), Array.Empty<RectTransform>());
            ConfigureSectionLayout(right, Array.Empty<RectTransform>(), Array.Empty<RectTransform>());

            RectTransform controller = RequireRect(footer, "CommandWheelController");
            ConfigureSectionLayout(
                footer,
                new[] { RequireRect(controller, "TargetingRail") },
                new[]
                {
                    RequireRect(controller, "WheelUnitCard"),
                    RequireRect(controller, "Wheel"),
                    RequireRect(controller, "InstructionStrip"),
                    RequireRect(controller, "RangeBanner")
                },
                new[]
                {
                    RequireRect(footer, "FeedbackPanel"),
                    RequireRect(footer, "CommandRail")
                });

            SetTopLeft(composition, 0f, 0f, Reference.x, Reference.y);
        }

        private static void ConfigureSectionLayout(
            RectTransform section,
            RectTransform[] rightAnchored,
            RectTransform[] centerAnchored,
            RectTransform[] widthExpanded = null)
        {
            MainMenuV3SectionLayoutView layout = section.GetComponent<MainMenuV3SectionLayoutView>() ??
                section.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            layout.Configure(
                Reference,
                MainMenuV3SectionAlignment.Center,
                rightAnchored,
                true,
                centerAnchored,
                widthExpanded);
        }

        private static void MountSection(RectTransform section, RectTransform composition)
        {
            section.SetParent(composition, false);
            SetTopLeft(section, 0f, 0f, Reference.x, Reference.y);
            section.localScale = Vector3.one;
            section.localRotation = Quaternion.identity;
        }

        private static void StyleHeader(RectTransform header, RectTransform footer)
        {
            SetActive(FindDeepChild(header, "ObjectivesPanel"), false);
            SetActive(FindDeepChild(header, "CurrentOrderBanner"), false);
            SetActive(FindDeepChild(header, "HeaderBackPlate"), false);
            SetActive(FindDeepChild(header, "BattlefieldLayer"), true);

            RectTransform resource = RequireRect(header, "ResourceStrip");
            SetTopLeft(resource, 414f, 8f, 615f, 73f);
            SetImageTransparent(resource.GetComponent<Image>());
            EnsureGradient(resource, DarkTop, DarkBottom, Line, 3f, resource.GetComponent<Button>());
            HorizontalLayoutGroup resourceLayout = resource.GetComponent<HorizontalLayoutGroup>();
            resourceLayout.padding = new RectOffset(8, 8, 5, 5);
            resourceLayout.spacing = 5f;
            resourceLayout.childAlignment = TextAnchor.MiddleLeft;
            resourceLayout.childControlWidth = true;
            resourceLayout.childControlHeight = true;
            resourceLayout.childForceExpandWidth = true;
            resourceLayout.childForceExpandHeight = true;

            string[] slots = { "MaterialsSlot", "OilSlot", "FuelSlot", "CivilianRiskSlot" };
            string[] labels = { "MATERIALS", "OIL", "FUEL", "CIVILIAN RISK" };
            string[] values = { "92/120", "2,860", "2,860", "MED" };
            Sprite[] icons =
            {
                RequireSprite(V3UiFoundationBuilder.MatchMaterialsIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchOilIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchFuelIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchCiviliansIconPath)
            };
            Color[] accents = { theme.TextPrimary, theme.Amber, theme.OrangeRed, theme.Amber };
            for (int i = 0; i < slots.Length; i++)
            {
                RectTransform slot = RequireRect(resource, slots[i]);
                LayoutElement element = slot.GetComponent<LayoutElement>() ?? slot.gameObject.AddComponent<LayoutElement>();
                element.flexibleWidth = 1f;
                Image icon = FindDeepChild(slot, "Icon")?.GetComponent<Image>();
                SetSprite(icon, icons[i], accents[i]);
                SetTopLeft(icon.rectTransform, 4f, 10f, 40f, 40f);
                TMP_Text label = FindDeepChild(slot, "Label")?.GetComponent<TMP_Text>();
                ConfigureText(label, labels[i], 12f, boldFont, theme.TextMuted, TextAlignmentOptions.MidlineLeft);
                SetTopLeft(label.rectTransform, 49f, 6f, 98f, 25f);
                // The shared Persian font has taller line metrics than Oxanium. Auto-size keeps
                // the translated resource captions inside the compact header instead of letting
                // TMP ellipsis cull the whole line when the locale changes at runtime.
                label.enableAutoSizing = true;
                label.fontSizeMin = 9f;
                label.fontSizeMax = 12f;
                TMP_Text value = FindDeepChild(slot, "Value")?.GetComponent<TMP_Text>();
                ConfigureText(value, values[i], 20f, boldFont, accents[i], TextAlignmentOptions.MidlineLeft);
                SetTopLeft(value.rectTransform, 49f, 29f, 98f, 32f);
                value.enableAutoSizing = true;
                value.fontSizeMin = 12f;
                value.fontSizeMax = 20f;
            }

            StyleHeaderButton(RequireRect(header, "SettingsButton"), 1042f, RequireSprite(V3UiFoundationBuilder.MatchSettingsIconPath));
            StyleHeaderButton(RequireRect(header, "PauseButton"), 1120f, RequireSprite(V3UiFoundationBuilder.MatchPauseIconPath));

            RectTransform threat = RequireRect(header, "ThreatJumpPanel");
            // Keep the alert and ARIA in the same right-anchored group so their
            // 15 px gap stays constant instead of overlapping on ultrawide.
            // The target lock aligns the warning strip with the right edge of the
            // header controls, leaving a clean lane before ARIA. Keeping it out of
            // ARIA's column also prevents the warning from being occluded at 20:9.
            SetTopLeft(threat, 663f, 94f, 525f, 79f);
            RectTransform threatFrame = RequireRect(threat, "Frame");
            Stretch(threatFrame);
            SetImageTransparent(threatFrame.GetComponent<Image>());
            EnsureGradient(threatFrame, DarkTop, DarkBottom, theme.OrangeRed, 3f);
            Image warning = FindDeepChild(threat, "WarningIcon")?.GetComponent<Image>();
            SetSprite(warning, RequireSprite(V3UiFoundationBuilder.MatchInvalidIconPath), theme.OrangeRed);
            SetTopLeft(warning.rectTransform, 16f, 16f, 47f, 47f);
            TMP_Text title = FindDeepChild(threat, "Title")?.GetComponent<TMP_Text>();
            ConfigureText(title, "HOSTILE CELL SPOTTED\nMarket quarter, 140m", 18f, boldFont, theme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            title.textWrappingMode = TextWrappingModes.Normal;
            title.fontStyle = FontStyles.Normal;
            SetTopLeft(title.rectTransform, 76f, 6f, 350f, 67f);
            RectTransform jump = EnsureRect("V3ThreatJump", threatFrame);
            SetTopLeft(jump, 450f, 10f, 63f, 59f);
            EnsureGradient(jump, AmberTop, RedBottom, theme.OrangeRed, 2f);
            Image jumpIcon = EnsureImage(jump, "Icon");
            SetSprite(jumpIcon, RequireSprite(V3UiFoundationBuilder.MatchJumpIconPath), theme.TextPrimary);
            SetTopLeft(jumpIcon.rectTransform, 13f, 11f, 37f, 37f);

            RectTransform aria = RequireRect(header, "AriaAssistantButton");
            SetTopLeft(aria, 1257f, 8f, 400f, 683f);
            SetImageTransparent(aria.GetComponent<Image>());
            EnsureGradient(aria, new Color32(12, 42, 54, 250), DarkBottom, theme.Cyan, 3f, aria.GetComponent<Button>());
            TMP_Text ariaLabel = FindDeepChild(aria, "Label")?.GetComponent<TMP_Text>();
            ConfigureText(ariaLabel, "ARIA", 32f, boldFont, theme.Cyan, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(ariaLabel.rectTransform, 15f, 12f, 104f, 42f);
            TMP_Text ariaState = FindDeepChild(aria, "State")?.GetComponent<TMP_Text>();
            ConfigureText(ariaState, "TUTORIAL 1/3", 14f, boldFont, theme.Cyan, TextAlignmentOptions.MidlineRight);
            ariaState.enableAutoSizing = true;
            ariaState.fontSizeMin = 12f;
            ariaState.fontSizeMax = 14f;
            ariaState.overflowMode = TextOverflowModes.Overflow;
            SetTopLeft(ariaState.rectTransform, 260f, 10f, 124f, 30f);
            TMP_Text ariaCopy = FindDeepChild(aria, "AlertCue")?.GetComponent<TMP_Text>();
            ConfigureText(ariaCopy, string.Empty, 16f, mediumFont, theme.TextPrimary, TextAlignmentOptions.TopLeft);
            ariaCopy.textWrappingMode = TextWrappingModes.Normal;
            ariaCopy.overflowMode = TextOverflowModes.Ellipsis;
            SetTopLeft(ariaCopy.rectTransform, 20f, 242f, 360f, 62f);

            // The portrait source intentionally has a black hologram field. Give
            // the whole upper telemetry bay the same field so it blends cleanly,
            // then keep portrait art behind the ARIA/title readouts.
            RectTransform portraitStage = EnsureRect("PortraitStage", aria);
            SetTopLeft(portraitStage, 10f, 8f, 380f, 230f);
            EnsureGradient(
                portraitStage,
                new Color32(0, 0, 0, 255),
                new Color32(0, 2, 4, 255),
                new Color32(9, 87, 105, 255),
                1f);
            RectTransform ariaGradient = FindDirectChild(aria, "V3GradientLayer") as RectTransform;
            ariaGradient?.SetAsFirstSibling();
            portraitStage.SetSiblingIndex(1);

            Image portrait = FindDirectChild(aria, "Image")?.GetComponent<Image>();
            if (portrait == null)
                portrait = FindDeepChild(aria, "Image")?.GetComponent<Image>();
            RectTransform portraitClip = EnsureRect("PortraitClip", aria);
            // Match the lock's centered head-and-shoulders hologram instead of a
            // full-width face crop. This also leaves the ARIA and step readouts
            // their own clean columns on both sides.
            SetTopLeft(portraitClip, 104f, 20f, 192f, 210f);
            if (portraitClip.GetComponent<RectMask2D>() == null)
                portraitClip.gameObject.AddComponent<RectMask2D>();
            portraitClip.SetSiblingIndex(2);
            portrait.transform.SetParent(portraitClip, false);
            Stretch(portrait.rectTransform);
            SetSprite(portrait, RequireSprite(V3UiFoundationBuilder.SharedAriaPortraitPath), Color.white);
            portrait.preserveAspect = true;
            AspectRatioFitter ariaFitter = portrait.GetComponent<AspectRatioFitter>() ?? portrait.gameObject.AddComponent<AspectRatioFitter>();
            ariaFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ariaFitter.aspectRatio = portrait.sprite.rect.width / portrait.sprite.rect.height;

            InstallAriaTelemetry(aria);

            RectTransform guidance = EnsureRect("TutorialGuidance", aria);
            SetTopLeft(guidance, 20f, 242f, 360f, 126f);
            TMP_Text tutorialTitle = EnsureText(guidance, "TutorialTitle");
            ConfigureText(tutorialTitle, "SELECT THE RIFLE SQUAD", 18f, boldFont, theme.Cyan, TextAlignmentOptions.MidlineLeft);
            tutorialTitle.enableAutoSizing = true;
            tutorialTitle.fontSizeMin = 14f;
            tutorialTitle.fontSizeMax = 18f;
            SetTopLeft(tutorialTitle.rectTransform, 0f, 0f, 360f, 27f);
            TMP_Text tutorialBody = EnsureText(guidance, "TutorialBody");
            ConfigureText(tutorialBody, "Tap the Rifle Squad card to select it.", 15f, mediumFont, theme.TextPrimary, TextAlignmentOptions.TopLeft);
            tutorialBody.textWrappingMode = TextWrappingModes.Normal;
            tutorialBody.richText = true;
            tutorialBody.enableAutoSizing = true;
            tutorialBody.fontSizeMin = 12f;
            tutorialBody.fontSizeMax = 15f;
            SetTopLeft(tutorialBody.rectTransform, 0f, 27f, 360f, 41f);

            RectTransform actions = null;
            for (int i = guidance.childCount - 1; i >= 0; i--)
            {
                Transform child = guidance.GetChild(i);
                if (child.name != "GuidanceActions")
                    continue;
                if (actions == null)
                    actions = child as RectTransform;
                else
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            actions ??= FindDirectChild(aria, "GuidanceActions") as RectTransform;
            actions ??= EnsureRect("GuidanceActions", aria);
            actions.SetParent(guidance, false);
            SetTopLeft(actions, 0f, 68f, 360f, 57f);
            Button doIt = EnsureGeneratedButton(actions, "DoItButton", 0f, 0f, 170f, 57f, GreenTop, GreenBottom, theme.Green, "DO IT", 20f);
            Button showMe = EnsureGeneratedButton(actions, "ShowMeButton", 190f, 0f, 170f, 57f, BlueTop, BlueBottom, theme.Blue, "SHOW ME", 20f);

            AriaTutorialBriefingView tutorialView =
                aria.GetComponent<AriaTutorialBriefingView>() ??
                aria.gameObject.AddComponent<AriaTutorialBriefingView>();
            SerializedObject tutorialSerialized = new(tutorialView);
            tutorialSerialized.FindProperty("briefingLayout").objectReferenceValue = guidance;
            tutorialSerialized.FindProperty("portraitImage").objectReferenceValue = portrait;
            tutorialSerialized.FindProperty("titleText").objectReferenceValue = tutorialTitle;
            tutorialSerialized.FindProperty("bodyText").objectReferenceValue = tutorialBody;
            tutorialSerialized.FindProperty("progressText").objectReferenceValue = ariaState;
            tutorialSerialized.FindProperty("closeButton").objectReferenceValue = null;
            tutorialSerialized.FindProperty("showMeButton").objectReferenceValue = showMe;
            tutorialSerialized.FindProperty("doItButton").objectReferenceValue = doIt;
            tutorialSerialized.FindProperty("showMeButtonLabel").objectReferenceValue =
                showMe.GetComponentInChildren<TMP_Text>(true);
            tutorialSerialized.FindProperty("doItButtonLabel").objectReferenceValue =
                doIt.GetComponentInChildren<TMP_Text>(true);
            tutorialSerialized.FindProperty("firstStepGuideRoot").objectReferenceValue = null;
            tutorialSerialized.FindProperty("persianFont").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PersianFontPath);
            tutorialSerialized.ApplyModifiedPropertiesWithoutUndo();
            guidance.gameObject.SetActive(false);

            RectTransform minimap = RequireRect(header.parent, "MinimapPanel");
            minimap.SetParent(aria, false);
            SetTopLeft(minimap, 10f, 375f, 380f, 300f);
            StyleMinimap(minimap);
        }

        private static void InstallAriaTelemetry(RectTransform aria)
        {
            RectTransform telemetry = EnsureRect("V3Telemetry", aria);
            SetTopLeft(telemetry, 12f, 60f, 376f, 166f);

            float[] widths = { 38f, 56f, 46f, 62f, 32f };
            for (int i = 0; i < widths.Length; i++)
            {
                Image leftLine = EnsureImage(telemetry, $"LeftLine{i + 1}");
                leftLine.sprite = null;
                leftLine.color = new Color(theme.Cyan.r, theme.Cyan.g, theme.Cyan.b, .66f);
                leftLine.raycastTarget = false;
                SetTopLeft(leftLine.rectTransform, 4f, 12f + i * 12f, widths[i], 2f);

                Image rightLine = EnsureImage(telemetry, $"RightLine{i + 1}");
                rightLine.sprite = null;
                rightLine.color = new Color(theme.Cyan.r, theme.Cyan.g, theme.Cyan.b, .66f);
                rightLine.raycastTarget = false;
                SetTopLeft(rightLine.rectTransform, 368f - widths[widths.Length - 1 - i], 12f + i * 12f,
                    widths[widths.Length - 1 - i], 2f);
            }

            Image target = EnsureImage(telemetry, "TargetGlyph");
            SetSprite(target, RequireSprite(V3UiFoundationBuilder.MatchScanIconPath), theme.Cyan);
            target.raycastTarget = false;
            target.preserveAspect = true;
            SetTopLeft(target.rectTransform, 322f, 102f, 42f, 42f);
        }

        private static void InstallTacticalFeedbackPreview(RectTransform header)
        {
            RectTransform battlefield = RequireRect(header, "BattlefieldLayer");
            for (int i = battlefield.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(battlefield.GetChild(i).gameObject);

            RectTransform preview = CreateTopLeft(
                "V3TacticalFeedbackPreview",
                battlefield,
                0f,
                0f,
                Reference.x,
                Reference.y);
            preview.gameObject.SetActive(false);

            RectTransform rangeBanner = CreateTopLeft("RangeBanner", preview, 610f, 218f, 364f, 48f);
            EnsureGradient(rangeBanner, DarkTop, DarkBottom, theme.Cyan, 3f);
            Image rangeIcon = EnsureImage(rangeBanner, "Icon");
            SetSprite(rangeIcon, RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath), theme.Cyan);
            SetTopLeft(rangeIcon.rectTransform, 13f, 9f, 30f, 30f);
            TMP_Text rangeText = EnsureText(rangeBanner, "Label");
            ConfigureText(
                rangeText,
                "<color=#00C8EE>RANGE 140m</color>  /  WEAPON 90m",
                18f,
                boldFont,
                theme.TextPrimary,
                TextAlignmentOptions.Center);
            rangeText.richText = true;
            SetTopLeft(rangeText.rectTransform, 44f, 4f, 307f, 40f);

            RectTransform attackRoute = CreateTopLeft("AttackRoute", preview, 0f, 0f, Reference.x, Reference.y);
            BuildDashedRoute(attackRoute, new Vector2(835f, 626f), new Vector2(1119f, 322f), 13, theme.OrangeRed);

            RectTransform friendlyRing = CreateTopLeft("FriendlySourceRing", preview, 775f, 596f, 120f, 60f);
            V3EllipseRingGraphic friendlyOuter = friendlyRing.gameObject.AddComponent<V3EllipseRingGraphic>();
            friendlyOuter.Configure(theme.TextPrimary, 3f, 72);
            RectTransform friendlyInner = CreateTopLeft("InnerRing", friendlyRing, 6f, 6f, 108f, 48f);
            V3EllipseRingGraphic friendlyInnerGraphic = friendlyInner.gameObject.AddComponent<V3EllipseRingGraphic>();
            friendlyInnerGraphic.Configure(theme.Green, 4f, 72);

            RectTransform hostileRing = CreateTopLeft("HostileTargetRing", preview, 1059f, 288f, 120f, 54f);
            V3EllipseRingGraphic hostileOuter = hostileRing.gameObject.AddComponent<V3EllipseRingGraphic>();
            hostileOuter.Configure(theme.OrangeRed, 3f, 72);
            RectTransform hostileInner = CreateTopLeft("InnerRing", hostileRing, 7f, 6f, 106f, 42f);
            V3EllipseRingGraphic hostileInnerGraphic = hostileInner.gameObject.AddComponent<V3EllipseRingGraphic>();
            hostileInnerGraphic.Configure(
                new Color(theme.OrangeRed.r, theme.OrangeRed.g, theme.OrangeRed.b, .72f),
                2f,
                72);

            RectTransform targetHealth = CreateTopLeft("TargetHealth", preview, 1082f, 205f, 112f, 14f);
            Image targetHealthBackground = targetHealth.gameObject.AddComponent<Image>();
            targetHealthBackground.color = new Color32(5, 9, 10, 235);
            targetHealthBackground.raycastTarget = false;
            for (int i = 0; i < 7; i++)
            {
                RectTransform cell = CreateTopLeft("Cell" + (i + 1), targetHealth, 4f + i * 15f, 3f, 12f, 8f);
                Image fill = cell.gameObject.AddComponent<Image>();
                fill.color = i < 6 ? theme.OrangeRed : new Color32(92, 34, 24, 255);
                fill.raycastTarget = false;
            }

            Image hostileMarker = EnsureImage(preview, "HostileTargetMarker");
            SetSprite(hostileMarker, RequireSprite(V3UiFoundationBuilder.MatchHostileMarkerIconPath), theme.OrangeRed);
            SetTopLeft(hostileMarker.rectTransform, 1087f, 221f, 86f, 86f);
        }

        private static void BuildDashedRoute(
            RectTransform parent,
            Vector2 start,
            Vector2 end,
            int dashCount,
            Color color)
        {
            Vector2 delta = end - start;
            float angle = Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg;
            for (int i = 0; i < dashCount; i++)
            {
                float t = (i + .5f) / dashCount;
                Vector2 point = Vector2.Lerp(start, end, t);
                RectTransform dash = CreateRect(
                    "Dash" + (i + 1),
                    parent,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(25f, 6f),
                    new Vector2(point.x, -point.y));
                dash.pivot = new Vector2(.5f, .5f);
                dash.localRotation = Quaternion.Euler(0f, 0f, angle);
                Image dashImage = dash.gameObject.AddComponent<Image>();
                dashImage.color = color;
                dashImage.raycastTarget = false;
            }
        }

        private static void StyleHeaderButton(RectTransform buttonRect, float x, Sprite iconSprite)
        {
            SetTopLeft(buttonRect, x, 8f, 68f, 73f);
            SetImageTransparent(buttonRect.GetComponent<Image>());
            EnsureGradient(buttonRect, RaisedTop, DarkBottom, Line, 3f, buttonRect.GetComponent<Button>());
            Image icon = FindDeepChild(buttonRect, "Icon")?.GetComponent<Image>();
            SetSprite(icon, iconSprite, theme.TextPrimary);
            CenterRect(icon.rectTransform, 38f, 38f);
            icon.preserveAspect = true;
        }

        private static void StyleMinimap(RectTransform minimap)
        {
            EnsureGradient(minimap, DarkTop, DarkBottom, theme.Cyan, 3f);
            if (minimap.GetComponent<RectMask2D>() == null)
                minimap.gameObject.AddComponent<RectMask2D>();
            MatchHudMinimapView minimapView = minimap.GetComponentInChildren<MatchHudMinimapView>(true);
            minimapView?.ConfigureMarkerSprites(
                RequireSprite(V3UiFoundationBuilder.MatchFriendlyMarkerIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchHostileMarkerIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath));
            Image map = FindDirectChild(minimap, "Map")?.GetComponent<Image>();
            if (map != null)
            {
                SetTopLeft(map.rectTransform, 4f, 4f,
                    Mathf.Max(1f, minimap.sizeDelta.x - 8f),
                    Mathf.Max(1f, minimap.sizeDelta.y - 8f));
                map.preserveAspect = true;
                AspectRatioFitter fitter = map.GetComponent<AspectRatioFitter>() ?? map.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                if (map.sprite != null)
                    fitter.aspectRatio = map.sprite.rect.width / map.sprite.rect.height;
            }
            RectTransform overlay = FindDirectChild(minimap, "MapOverlay") as RectTransform;
            if (overlay != null)
            {
                SetTopLeft(overlay, 4f, 4f,
                    Mathf.Max(1f, minimap.sizeDelta.x - 8f),
                    Mathf.Max(1f, minimap.sizeDelta.y - 8f));
                SetImageTransparent(overlay.GetComponent<Image>());
            }
            RectTransform frame = FindDirectChild(minimap, "Frame") as RectTransform;
            if (frame != null)
            {
                Stretch(frame);
                SetImageTransparent(frame.GetComponent<Image>());
                SetActive(FindDeepChild(frame, "ZoomIn"), false);
                SetActive(FindDeepChild(frame, "ZoomOut"), false);
                Image north = FindDeepChild(frame, "North")?.GetComponent<Image>();
                if (north != null)
                    SetTopLeft(north.rectTransform, 14f, 14f, 37f, 37f);
            }
        }

        private static void StyleSelection(RectTransform left, RectTransform footer)
        {
            RectTransform panel = RequireRect(left, "SelectedSquadPanel");
            SetTopLeft(panel, 12f, 11f, 386f, 717f);
            RectTransform frame = RequireRect(panel, "Frame");
            Stretch(frame);
            SetImageTransparent(frame.GetComponent<Image>());
            EnsureGradient(frame, DarkTop, DarkBottom, Line, 3f);

            Image badge = FindDirectChild(frame, "Badge")?.GetComponent<Image>();
            SetSprite(badge, RequireSprite(V3UiFoundationBuilder.MatchRankBadgeIconPath), theme.Amber);
            SetTopLeft(badge.rectTransform, 12f, 11f, 44f, 44f);
            TMP_Text title = FindDirectChild(frame, "Title")?.GetComponent<TMP_Text>();
            ConfigureText(title, "RIFLE SQUAD", 29f, boldFont, theme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(title.rectTransform, 68f, 7f, 288f, 37f);
            TMP_Text subtitle = FindDirectChild(frame, "Subtitle")?.GetComponent<TMP_Text>();
            ConfigureText(subtitle, "SQUAD 1  |  ANTI-INFANTRY", 14f, mediumFont, theme.TextMuted, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(subtitle.rectTransform, 68f, 40f, 288f, 27f);

            RectTransform portraitFrame = RequireRect(frame, "PortraitFrame");
            SetTopLeft(portraitFrame, 14f, 77f, 358f, 165f);
            SetImageTransparent(portraitFrame.GetComponent<Image>());
            EnsureGradient(portraitFrame, new Color32(18, 29, 31, 255), new Color32(4, 9, 10, 255), Line, 3f);
            if (portraitFrame.GetComponent<RectMask2D>() == null)
                portraitFrame.gameObject.AddComponent<RectMask2D>();
            Image portrait = FindDeepChild(portraitFrame, "Portrait")?.GetComponent<Image>();
            AspectRatioFitter portraitFitter = portrait.GetComponent<AspectRatioFitter>();
            if (portraitFitter != null)
                UnityEngine.Object.DestroyImmediate(portraitFitter);
            // Unit card artwork is square. Crop from the top so faces and unit
            // silhouettes remain visible in the wide selection aperture instead
            // of centering on torsos.
            SetTopLeft(portrait.rectTransform, 0f, 0f, 358f, 358f);
            portrait.preserveAspect = false;

            RectTransform health = RequireRect(frame, "HealthPanel");
            SetTopLeft(health, 17f, 252f, 352f, 31f);
            SetTopLeft(RequireRect(health, "HealthFrame"), 0f, 7f, 263f, 18f);
            SetTopLeft(RequireRect(health, "HealthFill"), 4f, 11f, 250f, 10f);
            Image healthFrameImage = RequireRect(health, "HealthFrame").GetComponent<Image>();
            if (healthFrameImage != null)
            {
                healthFrameImage.sprite = null;
                healthFrameImage.color = new Color32(4, 11, 7, 255);
            }
            Image healthFillImage = RequireRect(health, "HealthFill").GetComponent<Image>();
            if (healthFillImage != null)
            {
                healthFillImage.sprite = null;
                healthFillImage.color = new Color32(75, 205, 45, 255);
            }
            TMP_Text healthText = FindDeepChild(health, "HealthText")?.GetComponent<TMP_Text>();
            ConfigureText(healthText, "120 / 120", 18f, boldFont, theme.TextPrimary, TextAlignmentOptions.MidlineRight);
            healthText.enableAutoSizing = true;
            healthText.fontSizeMin = 14f;
            healthText.fontSizeMax = 18f;
            SetTopLeft(healthText.rectTransform, 263f, 0f, 87f, 31f);

            RectTransform order = RequireRect(frame, "OrderLabel");
            SetTopLeft(order, 17f, 291f, 352f, 55f);
            ConfigureText(order.GetComponent<TMP_Text>(), "CURRENT ORDER", 14f, boldFont, theme.TextMuted, TextAlignmentOptions.TopLeft);
            TMP_Text orderValue = FindDeepChild(order, "OrderValue")?.GetComponent<TMP_Text>();
            ConfigureText(orderValue, "MOVING TO MARKER", 18f, boldFont, theme.Green, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(orderValue.rectTransform, 0f, 23f, 352f, 27f);

            RectTransform playerControl = EnsureRect("V3PlayerControl", frame);
            SetTopLeft(playerControl, 17f, 351f, 352f, 40f);
            EnsureGradient(playerControl, new Color32(12, 39, 29, 255), new Color32(4, 19, 12, 255), theme.Green, 2f);
            Image controlIcon = EnsureImage(playerControl, "Icon");
            SetSprite(controlIcon, RequireSprite(V3UiFoundationBuilder.MatchPlayerIconPath), theme.TextPrimary);
            SetTopLeft(controlIcon.rectTransform, 12f, 6f, 28f, 28f);
            TMP_Text controlText = EnsureText(playerControl, "Label");
            ConfigureText(controlText, "PLAYER CONTROL\nMoving", 14f, mediumFont, theme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            controlText.textWrappingMode = TextWrappingModes.Normal;
            controlText.fontStyle = FontStyles.Normal;
            SetTopLeft(controlText.rectTransform, 52f, 2f, 288f, 36f);

            RectTransform commands = RequireRect(frame, "CommandButtons");
            SetTopLeft(commands, 17f, 402f, 352f, 226f);
            HorizontalLayoutGroup horizontal = commands.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
                UnityEngine.Object.DestroyImmediate(horizontal);
            GridLayoutGroup grid = commands.GetComponent<GridLayoutGroup>() ?? commands.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.spacing = new Vector2(8f, 8f);
            grid.cellSize = new Vector2(172f, 109f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;

            RectTransform board = RequireRect(left.parent, "BoardButton");
            board.SetParent(commands, false);
            RectTransform returnButton = RequireRect(commands, "ReturnButton");
            RectTransform destroyButton = RequireRect(commands, "DestroyButton");
            RectTransform cameraButton = RequireRect(commands, "CameraButton");
            returnButton.SetSiblingIndex(0);
            destroyButton.SetSiblingIndex(1);
            board.SetSiblingIndex(2);
            cameraButton.SetSiblingIndex(3);
            StyleActionButton(returnButton, GreenTop, GreenBottom, theme.Green, RequireSprite(V3UiFoundationBuilder.MatchReturnIconPath), theme.TextPrimary, "RETURN");
            StyleActionButton(destroyButton, RedTop, RedBottom, theme.OrangeRed, RequireSprite(V3UiFoundationBuilder.MatchDestroyIconPath), theme.TextPrimary, "DESTROY");
            StyleActionButton(board, BlueTop, BlueBottom, theme.Blue, RequireSprite(V3UiFoundationBuilder.MatchBoardIconPath), theme.TextPrimary, "BOARD");
            StyleActionButton(cameraButton, RaisedTop, DarkBottom, Line, RequireSprite(V3UiFoundationBuilder.MatchCameraIconPath), theme.Cyan, "CAMERA");
            RectTransform ropeDropButton = EnsureRect("RopeDropButton", commands);
            Button ropeDrop = ropeDropButton.GetComponent<Button>() ?? ropeDropButton.gameObject.AddComponent<Button>();
            EnsureImage(ropeDropButton, "Icon");
            EnsureText(ropeDropButton, "Label");
            StyleActionButton(
                ropeDropButton,
                BlueTop,
                BlueBottom,
                theme.Cyan,
                RequireSprite(V3UiFoundationBuilder.MatchRopeDropIconPath),
                theme.TextPrimary,
                "ROPE DROP");
            ropeDropButton.gameObject.SetActive(false);

            RectTransform chip = RequireRect(frame, "PassengerChip");
            SetTopLeft(chip, 17f, 665f, 352f, 39f);
            SetImageTransparent(chip.GetComponent<Image>());
            EnsureGradient(chip, new Color32(17, 50, 32, 255), new Color32(4, 22, 12, 255), theme.Green, 2f, chip.GetComponent<Button>());
            TMP_Text chipText = FindDeepChild(chip, "Label")?.GetComponent<TMP_Text>();
            ConfigureText(chipText, "PASSENGERS 0/4", 17f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            SetTopLeft(chipText.rectTransform, 42f, 3f, 300f, 33f);
            Image chipIcon = EnsureImage(chip, "Icon");
            SetSprite(chipIcon, RequireSprite(V3UiFoundationBuilder.MatchCiviliansIconPath), theme.TextPrimary);
            SetTopLeft(chipIcon.rectTransform, 9f, 7f, 25f, 25f);
            StylePassengerDrawer(frame);
        }

        private static void StylePassengerDrawer(RectTransform selectionFrame)
        {
            RectTransform drawer = RequireRect(selectionFrame, "TransportPassengerDrawer");
            SetTopLeft(drawer, 316f, 75f, 449f, 618f);
            SetImageTransparent(drawer.GetComponent<Image>());
            EnsureGradient(drawer, new Color32(12, 41, 31, 252), new Color32(2, 14, 8, 253), theme.Green, 3f);
            drawer.SetAsLastSibling();

            TMP_Text header = FindDirectChild(drawer, "Header")?.GetComponent<TMP_Text>();
            ConfigureText(header, "PASSENGERS 4/10  |  SOLDIERS 4/10", 22f, boldFont, theme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(header.rectTransform, 17f, 6f, 415f, 42f);

            RectTransform capacitySlots = EnsureRect("CapacitySlots", drawer);
            SetTopLeft(capacitySlots, 17f, 54f, 415f, 35f);
            HorizontalLayoutGroup capacityLayout = capacitySlots.GetComponent<HorizontalLayoutGroup>() ??
                                                   capacitySlots.gameObject.AddComponent<HorizontalLayoutGroup>();
            capacityLayout.padding = new RectOffset(0, 0, 0, 0);
            capacityLayout.spacing = 6f;
            capacityLayout.childAlignment = TextAnchor.MiddleLeft;
            capacityLayout.childControlWidth = false;
            capacityLayout.childControlHeight = false;
            capacityLayout.childForceExpandWidth = false;
            capacityLayout.childForceExpandHeight = false;
            for (int i = 0; i < 10; i++)
            {
                RectTransform slot = EnsureRect($"CapacitySlot{i + 1}", capacitySlots);
                slot.sizeDelta = new Vector2(35f, 35f);
                LayoutElement slotLayout = slot.GetComponent<LayoutElement>() ?? slot.gameObject.AddComponent<LayoutElement>();
                slotLayout.preferredWidth = 35f;
                slotLayout.preferredHeight = 35f;
                bool occupied = i < 4;
                EnsureGradient(
                    slot,
                    occupied ? new Color32(102, 207, 57, 255) : new Color32(20, 32, 34, 255),
                    occupied ? new Color32(42, 130, 20, 255) : new Color32(5, 12, 14, 255),
                    occupied ? new Color32(129, 232, 73, 255) : new Color32(72, 94, 99, 255),
                    2f);
            }

            RectTransform scroll = RequireRect(drawer, "Scroll View");
            SetTopLeft(scroll, 12f, 101f, 425f, 425f);
            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
            RectTransform viewport = RequireRect(scroll, "Viewport");
            Stretch(viewport);
            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
                viewportImage.color = new Color32(2, 8, 9, 230);
            RectTransform content = RequireRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(2, 2, 2, 2);
            contentLayout.spacing = 6f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            RectTransform item = RequireRect(content, "PassengerItemView");
            LayoutElement itemLayout = item.GetComponent<LayoutElement>() ?? item.gameObject.AddComponent<LayoutElement>();
            itemLayout.minHeight = 96f;
            itemLayout.preferredHeight = 96f;
            SetImageTransparent(item.GetComponent<Image>());
            EnsureGradient(item, new Color32(22, 47, 43, 252), new Color32(4, 15, 14, 252), new Color32(71, 109, 106, 255), 2f);
            Image portrait = FindDirectChild(item, "Portrait")?.GetComponent<Image>();
            if (portrait != null)
            {
                SetTopLeft(portrait.rectTransform, 8f, 8f, 82f, 80f);
                portrait.preserveAspect = true;
            }
            TMP_Text name = FindDirectChild(item, "Name")?.GetComponent<TMP_Text>();
            ConfigureText(name, "RIFLE SQUAD A", 19f, boldFont, theme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(name.rectTransform, 103f, 7f, 218f, 27f);
            TMP_Text role = FindDirectChild(item, "Role")?.GetComponent<TMP_Text>();
            ConfigureText(role, "SOLDIER", 15f, boldFont, theme.Green, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(role.rectTransform, 103f, 33f, 218f, 23f);
            RectTransform healthFrame = RequireRect(item, "HealthFrame");
            SetTopLeft(healthFrame, 103f, 61f, 151f, 18f);
            Image healthFrameImage = healthFrame.GetComponent<Image>();
            if (healthFrameImage != null)
            {
                healthFrameImage.sprite = null;
                healthFrameImage.color = new Color32(5, 15, 15, 255);
            }
            Image healthFill = FindDirectChild(healthFrame, "HealthFill")?.GetComponent<Image>();
            if (healthFill != null)
            {
                Stretch(healthFill.rectTransform, 2f, 3f);
                healthFill.sprite = null;
                healthFill.color = new Color32(84, 202, 45, 255);
            }
            TMP_Text health = FindDirectChild(healthFrame, "Health")?.GetComponent<TMP_Text>();
            ConfigureText(health, "100 / 100", 13f, mediumFont, theme.TextPrimary, TextAlignmentOptions.MidlineRight);
            SetTopLeft(health.rectTransform, 152f, -2f, 74f, 22f);
            RectTransform exit = RequireRect(item, "ExitButton");
            SetTopLeft(exit, 329f, 8f, 86f, 80f);
            SetImageTransparent(exit.GetComponent<Image>());
            EnsureGradient(exit, BlueTop, BlueBottom, theme.Cyan, 3f, exit.GetComponent<Button>());
            TMP_Text exitLabel = FindDirectChild(exit, "Label")?.GetComponent<TMP_Text>();
            ConfigureText(exitLabel, "EXIT", 19f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            Stretch(exitLabel.rectTransform, 3f, 3f);

            RectTransform emptyState = RequireRect(drawer, "EmptyState");
            SetTopLeft(emptyState, 15f, 235f, 419f, 72f);
            TMP_Text emptyText = FindDeepChild(emptyState, "Label")?.GetComponent<TMP_Text>();
            ConfigureText(emptyText, "NO PASSENGERS ONBOARD", 17f, boldFont, theme.TextMuted, TextAlignmentOptions.Center);
            Stretch(emptyText.rectTransform);

            RectTransform footer = RequireRect(drawer, "Footer");
            SetTopLeft(footer, 12f, 536f, 425f, 70f);
            HorizontalLayoutGroup footerLayout = footer.GetComponent<HorizontalLayoutGroup>();
            footerLayout.padding = new RectOffset(0, 0, 0, 0);
            footerLayout.spacing = 10f;
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = true;
            RectTransform exitAll = RequireRect(footer, "ExitAllButton");
            SetImageTransparent(exitAll.GetComponent<Image>());
            EnsureGradient(exitAll, RedTop, RedBottom, theme.OrangeRed, 3f, exitAll.GetComponent<Button>());
            TMP_Text exitAllLabel = FindDirectChild(exitAll, "Label")?.GetComponent<TMP_Text>();
            ConfigureText(exitAllLabel, "EXIT ALL", 19f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            Stretch(exitAllLabel.rectTransform, 3f, 3f);

            RectTransform close = RequireRect(drawer, "CloseButton");
            if (close.parent != footer)
                close.SetParent(footer, false);
            SetImageTransparent(close.GetComponent<Image>());
            EnsureGradient(close, RaisedTop, DarkBottom, Line, 3f, close.GetComponent<Button>());
            TMP_Text closeLabel = FindDirectChild(close, "Label")?.GetComponent<TMP_Text>();
            ConfigureText(closeLabel, "CLOSE", 19f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            Stretch(closeLabel.rectTransform, 3f, 3f);

            SerializedObject serialized = new(drawer.GetComponent<MatchHudTransportPassengerDrawerView>());
            SerializedProperty capacitySlotsProperty = serialized.FindProperty("capacitySlotsRoot");
            if (capacitySlotsProperty != null)
                capacitySlotsProperty.objectReferenceValue = capacitySlots;
            SerializedProperty ropeDropProperty = serialized.FindProperty("ropeDropButton");
            if (ropeDropProperty != null)
                ropeDropProperty.objectReferenceValue = FindDeepChild(selectionFrame, "RopeDropButton")?.GetComponent<Button>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            drawer.gameObject.SetActive(false);
        }

        private static void StyleFooter(RectTransform footer, RectTransform right)
        {
            RectTransform tray = RequireRect(footer, "SquadTray");
            SetTopLeft(tray, 10f, 731f, 579f, 201f);
            RectTransform trayFrame = RequireRect(tray, "Frame");
            Stretch(trayFrame);
            SetImageTransparent(trayFrame.GetComponent<Image>());
            HorizontalLayoutGroup trayLayout = trayFrame.GetComponent<HorizontalLayoutGroup>();
            trayLayout.padding = new RectOffset(4, 4, 4, 4);
            trayLayout.spacing = 5f;
            trayLayout.childAlignment = TextAnchor.MiddleLeft;
            trayLayout.childControlWidth = true;
            trayLayout.childControlHeight = true;
            trayLayout.childForceExpandWidth = true;
            trayLayout.childForceExpandHeight = true;
            SerializedObject traySerialized = new(trayFrame.GetComponent<MatchHudSquadTrayView>());
            traySerialized.FindProperty("cardLabelFont").objectReferenceValue = boldFont;
            traySerialized.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 1; i <= 5; i++)
            {
                RectTransform card = RequireRect(trayFrame, "SquadCard" + i);
                LayoutElement layout = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
                layout.flexibleWidth = 1f;
                RectTransform cardFrame = RequireRect(card, "Frame");
                Stretch(cardFrame);
                SetImageTransparent(cardFrame.GetComponent<Image>());
                EnsureGradient(
                    cardFrame,
                    RaisedTop,
                    DarkBottom,
                    i == 1 ? theme.Cyan : theme.Green,
                    3f,
                    card.GetComponent<Button>());
                Image portrait = FindDeepChild(cardFrame, "Portrait")?.GetComponent<Image>();
                SetTopLeft(portrait.rectTransform, 7f, 9f, 99f, 126f);
                portrait.preserveAspect = true;
                Image overlay = FindDeepChild(portrait.transform, "Overlay")?.GetComponent<Image>();
                if (overlay != null)
                    SetImageTransparent(overlay);
                RectTransform badge = FindDeepChild(cardFrame, "NumberBadge") as RectTransform;
                if (badge != null)
                    SetTopLeft(badge, 5f, 5f, 29f, 35f);
                TMP_Text number = badge != null ? badge.GetComponentInChildren<TMP_Text>(true) : null;
                ConfigureText(number, i.ToString(), 17f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
                RectTransform health = FindDeepChild(cardFrame, "HealthFrame") as RectTransform;
                if (health != null)
                {
                    SetTopLeft(health, 9f, 174f, 95f, 10f);
                    RectTransform fill = FindDeepChild(health, "HealthFill") as RectTransform;
                    if (fill != null)
                        Stretch(fill, 2f, 2f);
                }
                EnsureSquadCardLabel(card, SquadLabel(i));
            }

            RectTransform feedback = RequireRect(footer, "FeedbackPanel");
            // Leave enough width for the longer Persian command rejection copy at 16:9.
            // The rail still ends before ARIA's right-side column, while the ultrawide
            // responsive layout can continue expanding it with the available safe width.
            SetTopLeft(feedback, 591f, 710f, 780f, 48f);
            RectTransform feedbackFrame = RequireRect(feedback, "Frame");
            Stretch(feedbackFrame);
            SetImageTransparent(feedbackFrame.GetComponent<Image>());
            EnsureGradient(feedbackFrame, DarkTop, DarkBottom, theme.OrangeRed, 3f);
            Image feedbackIcon = FindDeepChild(feedbackFrame, "Icon")?.GetComponent<Image>();
            SetSprite(feedbackIcon, RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath), theme.TextPrimary);
            SetTopLeft(feedbackIcon.rectTransform, 12f, 9f, 30f, 30f);
            BattleHudRuntimeFeedbackView feedbackView = footer.GetComponent<BattleHudRuntimeFeedbackView>();
            if (feedbackView != null)
            {
                SerializedObject feedbackSerialized = new(feedbackView);
                feedbackSerialized.FindProperty("neutralIcon").objectReferenceValue =
                    RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath);
                feedbackSerialized.FindProperty("readyIcon").objectReferenceValue =
                    RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath);
                feedbackSerialized.FindProperty("warningIcon").objectReferenceValue =
                    RequireSprite(V3UiFoundationBuilder.MatchInvalidIconPath);
                feedbackSerialized.FindProperty("errorIcon").objectReferenceValue =
                    RequireSprite(V3UiFoundationBuilder.MatchInvalidIconPath);
                feedbackSerialized.FindProperty("suppressCurrentOrderBanner").boolValue = true;
                feedbackSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
            TMP_Text feedbackText = FindDeepChild(feedbackFrame, "Feedback")?.GetComponent<TMP_Text>();
            ConfigureText(feedbackText, "ATTACK UNAVAILABLE — SELECT A UNIT OR VALID TARGET", 18f, boldFont, theme.OrangeRed, TextAlignmentOptions.MidlineLeft);
            // Keep an explicit right inset as well: shaped RTL glyph geometry can extend a few
            // pixels beyond TMP's measured advance even when autosizing reports a fit.
            SetTopLeft(feedbackText.rectTransform, 53f, 4f, 680f, 40f);
            feedbackText.enableAutoSizing = true;
            feedbackText.fontSizeMin = 8f;
            feedbackText.fontSizeMax = 18f;
            feedbackText.margin = new Vector4(0f, 0f, 80f, 0f);
            SetActive(FindDeepChild(feedbackFrame, "Actions"), false);

            RectTransform commandRail = RequireRect(footer, "CommandRail");
            SetTopLeft(commandRail, 591f, 765f, 1071f, 167f);
            RectTransform commandFrame = RequireRect(commandRail, "Frame");
            Stretch(commandFrame, 3f, 3f);
            SetImageTransparent(commandFrame.GetComponent<Image>());
            EnsureGradient(commandFrame, new Color32(7, 15, 17, 250), new Color32(2, 5, 6, 252), Line, 3f);
            HorizontalLayoutGroup railLayout = commandFrame.GetComponent<HorizontalLayoutGroup>();
            railLayout.padding = new RectOffset(5, 5, 5, 5);
            railLayout.spacing = 5f;
            railLayout.childAlignment = TextAnchor.MiddleLeft;
            railLayout.childControlWidth = true;
            railLayout.childControlHeight = true;
            railLayout.childForceExpandWidth = false;
            railLayout.childForceExpandHeight = true;

            RectTransform support = RequireRect(footer.parent, "SupportCommand");
            RectTransform build = RequireRect(footer.parent, "BuildCommand");
            support.SetParent(commandFrame, false);
            build.SetParent(commandFrame, false);
            string[] names = { "SelectCommand", "MoveCommand", "AttackCommand", "HoldCommand", "StopCommand", "ScanCommand", "SupportCommand", "BuildCommand" };
            Sprite[] icons =
            {
                RequireSprite(V3UiFoundationBuilder.MatchSelectIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchMoveIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchAttackIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchHoldIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchStopIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchScanIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchSupportIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchBuildIconPath)
            };
            Color[] tops = { RaisedTop, GreenTop, RedTop, OliveTop, RedTop, BlueTop, BlueTop, AmberTop };
            Color[] bottoms = { DarkBottom, GreenBottom, RedBottom, OliveBottom, RedBottom, BlueBottom, BlueBottom, AmberBottom };
            Color[] borders = { Line, theme.Cyan, theme.OrangeRed, theme.Amber, theme.OrangeRed, theme.Cyan, theme.Blue, theme.Amber };
            string[] labels = { "SELECT", "MOVE", "ATTACK", "HOLD", "STOP", "SCAN", "SUPPORT", "BUILD" };
            float[] widths = { 123f, 123f, 123f, 123f, 123f, 123f, 130f, 160f };
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform command = RequireRect(commandFrame, names[i]);
                command.SetSiblingIndex(i + 1);
                LayoutElement element = command.GetComponent<LayoutElement>() ?? command.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = widths[i];
                element.minWidth = widths[i];
                element.flexibleWidth = 1f;
                StyleCommandButton(command, tops[i], bottoms[i], borders[i], icons[i], labels[i]);
            }
            right.gameObject.SetActive(false);
        }

        private static void InstallCommandWheel(Transform root, RectTransform footer)
        {
            Transform existing = FindDeepChild(root, "CommandWheelController");
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            RectTransform controllerRoot = CreateRect(
                "CommandWheelController",
                footer,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Stretch(controllerRoot);
            CommandWheelPanelView controller = controllerRoot.gameObject.AddComponent<CommandWheelPanelView>();

            RectTransform overlay = CreateRect(
                "CommandWheelOverlay",
                controllerRoot,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Stretch(overlay);

            RectTransform scrim = CreateRect("ScrimButton", overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(scrim);
            Image scrimImage = scrim.gameObject.AddComponent<Image>();
            scrimImage.color = new Color(0f, 0f, 0f, .10f);
            scrimImage.raycastTarget = true;
            Button scrimButton = scrim.gameObject.AddComponent<Button>();
            scrimButton.targetGraphic = scrimImage;
            scrimButton.transition = Selectable.Transition.None;

            Transform helicopterCard = FindDeepChild(root, "SquadCard3");
            Image sourcePortrait = helicopterCard != null
                ? FindDeepChild(helicopterCard, "Portrait")?.GetComponent<Image>()
                : null;

            const float wheelSize = 470f;
            const float wheelCenterX = 838f;
            RectTransform wheel = CreateTopLeft("Wheel", overlay, wheelCenterX - wheelSize * .5f, 185f, wheelSize, wheelSize);
            Button[] sectors = new Button[6];
            sectors[0] = BuildWheelSector(wheel, "MoveSector", 62f, 56f, CyanTop, CyanBottom, theme.Cyan,
                RequireSprite(V3UiFoundationBuilder.MatchMoveIconPath), "MOVE", true);
            sectors[1] = BuildWheelSector(wheel, "StopSector", 122f, 56f, RaisedTop, DarkBottom, Line,
                RequireSprite(V3UiFoundationBuilder.MatchStopIconPath), "STOP", true);
            sectors[2] = BuildWheelSector(wheel, "ExtractSector", 182f, 56f, BlueTop, BlueBottom, theme.Blue,
                RequireSprite(V3UiFoundationBuilder.MatchExtractIconPath), "EXTRACT", false);
            sectors[3] = BuildWheelSector(wheel, "RopeDropSector", 242f, 56f, AmberTop, AmberBottom, theme.Amber,
                RequireSprite(V3UiFoundationBuilder.MatchRopeDropIconPath), "ROPE DROP", false);
            sectors[4] = BuildWheelSector(wheel, "PatrolSector", 302f, 56f, GreenTop, GreenBottom, theme.Green,
                RequireSprite(V3UiFoundationBuilder.MatchPatrolIconPath), "PATROL", false);
            sectors[5] = BuildWheelSector(wheel, "AttackSector", 2f, 56f, RedTop, RedBottom, theme.OrangeRed,
                RequireSprite(V3UiFoundationBuilder.MatchAttackIconPath), "ATTACK", true);

            const float centerSize = 165f;
            float centerInset = (wheelSize - centerSize) * .5f;
            RectTransform center = CreateTopLeft("CenterDisc", wheel, centerInset, centerInset, centerSize, centerSize);
            RectTransform centerFillLayer = CreateTopLeft("Fill", center, 0f, 0f, centerSize, centerSize);
            V3PolygonGraphic centerFill = centerFillLayer.gameObject.AddComponent<V3PolygonGraphic>();
            centerFill.Configure(BuildCirclePoints(centerSize * .5f, centerSize * .5f, centerSize * .5f - 2f, 48), new Color32(5, 15, 19, 255));
            Mask centerMask = centerFillLayer.gameObject.AddComponent<Mask>();
            centerMask.showMaskGraphic = true;
            RectTransform centerRingLayer = CreateTopLeft("Ring", center, 0f, 0f, centerSize, centerSize);
            V3RingGraphic centerRing = centerRingLayer.gameObject.AddComponent<V3RingGraphic>();
            centerRing.Configure(theme.Cyan, 5f, 64);
            centerRing.raycastTarget = false;

            Image centerPortrait = EnsureImage(centerFillLayer, "Portrait");
            SetSprite(centerPortrait, sourcePortrait != null ? sourcePortrait.sprite : null, Color.white);
            SetTopLeft(centerPortrait.rectTransform, 15f, 13f, 135f, 108f);
            centerPortrait.preserveAspect = true;
            AspectRatioFitter centerFitter = centerPortrait.gameObject.AddComponent<AspectRatioFitter>();
            centerFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (centerPortrait.sprite != null)
                centerFitter.aspectRatio = centerPortrait.sprite.rect.width / centerPortrait.sprite.rect.height;
            TMP_Text centerLabel = EnsureText(centerFillLayer, "Label");
            ConfigureText(centerLabel, "BLACK HAWK", 18f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            SetTopLeft(centerLabel.rectTransform, 11f, 118f, 143f, 32f);

            // The target card sits slightly in front of the wheel's left edge so
            // all three stat columns remain readable at both validated ratios.
            BuildWheelUnitCard(overlay, sourcePortrait != null ? sourcePortrait.sprite : null);

            RectTransform instruction = CreateTopLeft("InstructionStrip", overlay, 572f, 675f, 532f, 50f);
            EnsureGradient(instruction, DarkTop, DarkBottom, Line, 3f);
            Image instructionIcon = EnsureImage(instruction, "Icon");
            SetSprite(instructionIcon, RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath), theme.TextPrimary);
            SetTopLeft(instructionIcon.rectTransform, 13f, 7f, 36f, 36f);
            TMP_Text instructionText = EnsureText(instruction, "Label");
            ConfigureText(instructionText, "SELECT COMMAND, THEN TAP TARGET.", 19f, mediumFont, theme.TextPrimary, TextAlignmentOptions.Center);
            SetTopLeft(instructionText.rectTransform, 58f, 2f, 462f, 46f);

            RectTransform targetingRoot = CreateRect(
                "TargetingState",
                overlay,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Stretch(targetingRoot);
            RectTransform rangeBanner = CreateTopLeft("RangeBanner", targetingRoot, 600f, 111f, 360f, 48f);
            EnsureGradient(rangeBanner, DarkTop, DarkBottom, theme.Cyan, 3f);
            Image rangeIcon = EnsureImage(rangeBanner, "Icon");
            SetSprite(rangeIcon, RequireSprite(V3UiFoundationBuilder.MatchScanIconPath), theme.Cyan);
            SetTopLeft(rangeIcon.rectTransform, 12f, 9f, 30f, 30f);
            TMP_Text rangeText = EnsureText(rangeBanner, "Label");
            ConfigureText(rangeText, "RANGE 185m  /  WEAPON 90m", 18f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            SetTopLeft(rangeText.rectTransform, 44f, 4f, 304f, 40f);

            Button cancelTargetButton = BuildTargetingRail(targetingRoot);
            targetingRoot.gameObject.SetActive(false);

            RectTransform portraitFrame = RequireRect(root, "PortraitFrame");
            Button openButton = portraitFrame.GetComponent<Button>() ?? portraitFrame.gameObject.AddComponent<Button>();
            V3GradientGraphic portraitTarget = EnsureGradient(
                portraitFrame,
                new Color32(18, 29, 31, 255),
                new Color32(4, 9, 10, 255),
                Line,
                3f,
                openButton);
            openButton.targetGraphic = portraitTarget;
            RectTransform commandChip = EnsureRect("CommandWheelCue", portraitFrame);
            SetTopLeft(commandChip, 238f, 129f, 112f, 29f);
            EnsureGradient(commandChip, CyanTop, CyanBottom, theme.Cyan, 2f);
            TMP_Text commandChipText = EnsureText(commandChip, "Label");
            ConfigureText(commandChipText, "COMMANDS", 13f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            Stretch(commandChipText.rectTransform, 3f, 2f);

            MatchHudSelectionPanelView selectionPanel = root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            if (selectionPanel == null)
                throw new MissingReferenceException("Match HUD selection panel is missing while binding the command-wheel portrait cue.");
            SerializedObject selectionSerialized = new(selectionPanel);
            selectionSerialized.FindProperty("commandWheelOpenButton").objectReferenceValue = openButton;
            selectionSerialized.ApplyModifiedPropertiesWithoutUndo();

            BattleHudRuntimeFeedbackView feedback = root.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            MatchOverlayCommandControlsView controls = root.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            if (controls == null)
                throw new MissingReferenceException("Match HUD command controls are missing while installing the Unit Command Wheel.");

            SerializedObject controllerSerialized = new(controller);
            controllerSerialized.FindProperty("wheelRoot").objectReferenceValue = overlay.gameObject;
            // Header, left, and footer are instantiated as independent shell
            // sections at runtime. UIShellContentView stitches the live portrait
            // button and threat panel to this footer-owned wheel after install.
            controllerSerialized.FindProperty("openButton").objectReferenceValue = null;
            controllerSerialized.FindProperty("closeButton").objectReferenceValue = cancelTargetButton;
            controllerSerialized.FindProperty("scrimButton").objectReferenceValue = scrimButton;
            controllerSerialized.FindProperty("wheelMoveButton").objectReferenceValue = sectors[0];
            controllerSerialized.FindProperty("wheelAttackButton").objectReferenceValue = sectors[5];
            controllerSerialized.FindProperty("moveCommandButton").objectReferenceValue = controls.MoveButton;
            controllerSerialized.FindProperty("attackCommandButton").objectReferenceValue = controls.AttackButton;
            controllerSerialized.FindProperty("moveWedge").objectReferenceValue = sectors[0].targetGraphic as V3RadialWedgeGraphic;
            controllerSerialized.FindProperty("attackWedge").objectReferenceValue = sectors[5].targetGraphic as V3RadialWedgeGraphic;
            controllerSerialized.FindProperty("wheelTransform").objectReferenceValue = wheel;
            controllerSerialized.FindProperty("targetingRoot").objectReferenceValue = targetingRoot.gameObject;
            controllerSerialized.FindProperty("rangeBanner").objectReferenceValue = rangeBanner.gameObject;
            controllerSerialized.FindProperty("unitCard").objectReferenceValue = FindDirectChild(overlay, "WheelUnitCard") as RectTransform;
            controllerSerialized.FindProperty("instructionRoot").objectReferenceValue = instruction.gameObject;
            controllerSerialized.FindProperty("threatRoot").objectReferenceValue = null;
            controllerSerialized.FindProperty("feedbackRoot").objectReferenceValue = FindDeepChild(root, "FeedbackPanel")?.gameObject;
            controllerSerialized.FindProperty("runtimeFeedbackView").objectReferenceValue = feedback;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject controlsSerialized = new(controls);
            controlsSerialized.FindProperty("commandWheelStopButton").objectReferenceValue = sectors[1];
            controlsSerialized.FindProperty("commandWheelPanel").objectReferenceValue = controller;
            controlsSerialized.ApplyModifiedPropertiesWithoutUndo();

            overlay.gameObject.SetActive(false);
        }

        private static void BuildWheelUnitCard(RectTransform overlay, Sprite portraitSprite)
        {
            RectTransform card = CreateTopLeft("WheelUnitCard", overlay, 400f, 104f, 252f, 338f);
            EnsureGradient(card, new Color32(21, 34, 38, 252), DarkBottom, theme.Cyan, 3f);

            TMP_Text title = EnsureText(card, "Title");
            ConfigureText(title, "BLACK HAWK", 22f, boldFont, theme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(title.rectTransform, 14f, 7f, 192f, 31f);

            TMP_Text subtitle = EnsureText(card, "Subtitle");
            ConfigureText(subtitle, "HELICOPTER", 14f, mediumFont, theme.TextMuted, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(subtitle.rectTransform, 14f, 35f, 174f, 24f);

            Image badge = EnsureImage(card, "RankBadge");
            SetSprite(badge, RequireSprite(V3UiFoundationBuilder.MatchArmorIconPath), theme.Cyan);
            SetTopLeft(badge.rectTransform, 204f, 9f, 36f, 42f);

            RectTransform portraitFrame = CreateTopLeft("PortraitFrame", card, 14f, 63f, 224f, 147f);
            EnsureGradient(portraitFrame, new Color32(16, 29, 32, 255), new Color32(3, 8, 10, 255), Line, 2f);
            portraitFrame.gameObject.AddComponent<RectMask2D>();
            Image portrait = EnsureImage(portraitFrame, "Portrait");
            SetSprite(portrait, portraitSprite, Color.white);
            Stretch(portrait.rectTransform, 4f, 4f);
            portrait.preserveAspect = true;
            AspectRatioFitter portraitFitter = portrait.gameObject.AddComponent<AspectRatioFitter>();
            portraitFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (portraitSprite != null)
                portraitFitter.aspectRatio = portraitSprite.rect.width / portraitSprite.rect.height;

            RectTransform healthFrame = CreateTopLeft("HealthFrame", card, 14f, 217f, 224f, 17f);
            Image healthBackground = healthFrame.gameObject.AddComponent<Image>();
            healthBackground.sprite = null;
            healthBackground.color = new Color32(3, 9, 7, 255);
            healthBackground.raycastTarget = false;
            RectTransform healthFill = CreateTopLeft("HealthFill", healthFrame, 3f, 3f, 167f, 11f);
            Image healthFillImage = healthFill.gameObject.AddComponent<Image>();
            healthFillImage.sprite = null;
            healthFillImage.color = new Color32(75, 205, 45, 255);
            healthFillImage.raycastTarget = false;
            TMP_Text healthValue = EnsureText(healthFrame, "Value");
            ConfigureText(healthValue, "1,200 / 1,200", 11f, boldFont, theme.Green, TextAlignmentOptions.MidlineRight);
            SetTopLeft(healthValue.rectTransform, 123f, 0f, 99f, 17f);

            TMP_Text ready = EnsureText(card, "Ready");
            ConfigureText(ready, "READY", 17f, boldFont, theme.Cyan, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(ready.rectTransform, 14f, 239f, 224f, 28f);

            BuildWheelUnitStat(card, "AirTransport", 14f,
                RequireSprite(V3UiFoundationBuilder.MatchAirTransportIconPath), "AIR\nTRANSPORT", "8");
            BuildWheelUnitStat(card, "LightArmor", 88f,
                RequireSprite(V3UiFoundationBuilder.MatchArmorIconPath), "LIGHT\nARMOR", "LOW");
            BuildWheelUnitStat(card, "Speed", 162f,
                RequireSprite(V3UiFoundationBuilder.MatchSpeedIconPath), "SPEED", "FAST");
        }

        private static void BuildWheelUnitStat(
            RectTransform card,
            string name,
            float x,
            Sprite iconSprite,
            string label,
            string value)
        {
            RectTransform stat = CreateTopLeft(name, card, x, 270f, 70f, 58f);
            EnsureGradient(stat, new Color32(20, 35, 39, 245), DarkBottom, new Color32(76, 96, 103, 255), 2f);
            Image icon = EnsureImage(stat, "Icon");
            SetSprite(icon, iconSprite, theme.Cyan);
            SetTopLeft(icon.rectTransform, 4f, 6f, 27f, 27f);
            TMP_Text statLabel = EnsureText(stat, "Label");
            ConfigureText(statLabel, label, 9f, mediumFont, theme.TextPrimary, TextAlignmentOptions.Center);
            statLabel.textWrappingMode = TextWrappingModes.Normal;
            SetTopLeft(statLabel.rectTransform, 29f, 4f, 38f, 31f);
            TMP_Text statValue = EnsureText(stat, "Value");
            ConfigureText(statValue, value, 11f, boldFont, theme.Green, TextAlignmentOptions.Center);
            SetTopLeft(statValue.rectTransform, 4f, 37f, 62f, 17f);
        }

        private static Button BuildWheelSector(
            RectTransform wheel,
            string name,
            float startAngle,
            float sweepAngle,
            Color top,
            Color bottom,
            Color border,
            Sprite iconSprite,
            string label,
            bool interactable)
        {
            RectTransform sector = CreateRect(name, wheel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(sector);
            V3RadialWedgeGraphic wedge = sector.gameObject.AddComponent<V3RadialWedgeGraphic>();
            wedge.Configure(startAngle, sweepAngle, .38f, .98f, top, bottom, border, 3f, true);
            Button button = sector.gameObject.AddComponent<Button>();
            button.targetGraphic = wedge;
            button.transition = Selectable.Transition.None;
            button.interactable = interactable;

            float middle = (startAngle + sweepAngle * .5f) * Mathf.Deg2Rad;
            float half = wheel.sizeDelta.x * .5f;
            float labelRadius = half * .70f;
            float centerX = half + Mathf.Cos(middle) * labelRadius;
            float centerY = half - Mathf.Sin(middle) * labelRadius;
            if (iconSprite != null)
            {
                Image icon = EnsureImage(sector, "Icon");
                SetSprite(icon, iconSprite, theme.TextPrimary);
                SetTopLeft(icon.rectTransform, centerX - 36f, centerY - 57f, 72f, 72f);
            }
            else
            {
                RectTransform icon = EnsureRect("Icon", sector);
                SetTopLeft(icon, centerX - 36f, centerY - 57f, 72f, 72f);
                BuildProceduralX(icon, theme.TextPrimary, 7f, 40f);
            }

            TMP_Text text = EnsureText(sector, "Label");
            ConfigureText(text, label, label.Length > 8 ? 15f : 18f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            SetTopLeft(text.rectTransform, centerX - 68f, centerY + 14f, 136f, 34f);
            return button;
        }

        private static Button BuildTargetingRail(RectTransform targetingRoot)
        {
            const float railWidth = 310f;
            RectTransform rail = CreateTopLeft("TargetingRail", targetingRoot, 1346f, 380f, railWidth, 344f);
            Color opaqueRaised = new Color32(34, 45, 49, 255);
            Color opaqueDark = new Color32(4, 9, 11, 255);

            RectTransform hint = CreateTopLeft("TargetHint", rail, 0f, 0f, railWidth, 92f);
            EnsureGradient(hint, opaqueRaised, opaqueDark, theme.OrangeRed, 3f);
            TMP_Text hintTitle = EnsureText(hint, "Title");
            ConfigureText(hintTitle, "TARGET HINT", 19f, boldFont, theme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(hintTitle.rectTransform, 15f, 8f, railWidth - 30f, 30f);
            TMP_Text hintCopy = EnsureText(hint, "Copy");
            ConfigureText(hintCopy, "Tap enemy patrol to attack.", 17f, mediumFont, theme.TextMuted, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(hintCopy.rectTransform, 15f, 39f, railWidth - 30f, 40f);

            RectTransform reason = CreateTopLeft("DisabledReason", rail, 0f, 101f, railWidth, 102f);
            EnsureGradient(reason, opaqueRaised, opaqueDark, new Color32(215, 55, 55, 255), 3f);
            TMP_Text reasonTitle = EnsureText(reason, "Title");
            ConfigureText(reasonTitle, "DISABLED REASON", 18f, boldFont, theme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(reasonTitle.rectTransform, 15f, 8f, railWidth - 30f, 30f);
            TMP_Text reasonCopy = EnsureText(reason, "Copy");
            ConfigureText(reasonCopy, "Special unavailable for this selection.", 16f, mediumFont, theme.TextMuted, TextAlignmentOptions.MidlineLeft);
            reasonCopy.textWrappingMode = TextWrappingModes.Normal;
            SetTopLeft(reasonCopy.rectTransform, 15f, 40f, railWidth - 30f, 53f);

            Button confirm = EnsureGeneratedButton(
                rail,
                "ConfirmTargetButton",
                0f,
                214f,
                railWidth,
                59f,
                RedTop,
                RedBottom,
                theme.OrangeRed,
                "CONFIRM TARGET",
                21f);
            Image confirmIcon = EnsureImage(confirm.transform, "Icon");
            SetSprite(confirmIcon, RequireSprite(V3UiFoundationBuilder.MatchAttackIconPath), theme.TextPrimary);
            SetTopLeft(confirmIcon.rectTransform, 17f, 12f, 35f, 35f);

            Button cancel = EnsureGeneratedButton(
                rail,
                "CancelTargetButton",
                0f,
                283f,
                railWidth,
                59f,
                RaisedTop,
                DarkBottom,
                Line,
                "CANCEL",
                21f);
            RectTransform cancelIcon = EnsureRect("Icon", cancel.transform);
            SetTopLeft(cancelIcon, 18f, 9f, 40f, 40f);
            BuildProceduralX(cancelIcon, theme.TextPrimary, 5f, 27f);
            return cancel;
        }

        private static Vector2[] BuildCirclePoints(float centerX, float centerY, float radius, int segments)
        {
            int count = Mathf.Max(12, segments);
            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                points[i] = new Vector2(centerX + Mathf.Cos(angle) * radius, centerY - Mathf.Sin(angle) * radius);
            }
            return points;
        }

        private static void BuildProceduralX(RectTransform parent, Color color, float width, float height)
        {
            Image first = EnsureImage(parent, "StrokeA");
            first.sprite = null;
            first.color = color;
            first.raycastTarget = false;
            CenterRect(first.rectTransform, width, height);
            first.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Image second = EnsureImage(parent, "StrokeB");
            second.sprite = null;
            second.color = color;
            second.raycastTarget = false;
            CenterRect(second.rectTransform, width, height);
            second.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        private static void CenterRect(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void StyleActionButton(RectTransform buttonRect, Color top, Color bottom, Color border, Sprite iconSprite, Color iconColor, string labelValue)
        {
            EnableInvisibleRootHitTarget(buttonRect.GetComponent<Image>());
            Button button = buttonRect.GetComponent<Button>();
            EnsureGradient(buttonRect, top, bottom, border, 2f, button);
            ConfigureV3ButtonColorStates(button);
            SetImageTransparent(FindDirectChild(buttonRect, "Frame")?.GetComponent<Image>());
            Image icon = FindDeepChild(buttonRect, "Icon")?.GetComponent<Image>();
            SetSprite(icon, iconSprite, iconColor);
            SetTopLeft(icon.rectTransform, 51f, 11f, 70f, 70f);
            TMP_Text label = FindDeepChild(buttonRect, "Label")?.GetComponent<TMP_Text>();
            ConfigureText(label, labelValue, 17f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            SetTopLeft(label.rectTransform, 9f, 76f, 154f, 27f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 17f;
            SetActive(FindDeepChild(buttonRect, "Status"), false);
        }

        private static void StyleCommandButton(RectTransform buttonRect, Color top, Color bottom, Color border, Sprite iconSprite, string labelValue)
        {
            // Keep a full-rect invisible hit surface on the command root. The V3
            // gradient remains the Button target graphic, while this root Image
            // prevents narrow child/layout gaps from dropping pointer events.
            EnableInvisibleRootHitTarget(buttonRect.GetComponent<Image>());
            Button button = buttonRect.GetComponent<Button>();
            EnsureGradient(buttonRect, top, bottom, border, 3f, button);
            ConfigureV3ButtonColorStates(button);
            SetImageTransparent(FindDirectChild(buttonRect, "Frame")?.GetComponent<Image>());
            Image icon = FindDeepChild(buttonRect, "Icon")?.GetComponent<Image>();
            SetSprite(icon, iconSprite, theme.TextPrimary);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(.5f, 1f);
            icon.rectTransform.pivot = new Vector2(.5f, 1f);
            icon.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            icon.rectTransform.sizeDelta = new Vector2(70f, 70f);
            icon.rectTransform.localScale = Vector3.one;
            icon.preserveAspect = true;
            TMP_Text label = FindDeepChild(buttonRect, "Label")?.GetComponent<TMP_Text>();
            ConfigureText(label, labelValue, 19f, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.pivot = new Vector2(.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -101f);
            label.rectTransform.sizeDelta = new Vector2(-10f, 35f);
            label.rectTransform.localScale = Vector3.one;
            // Persian glyphs use a taller line box. Without auto-sizing, TMP's ellipsis mode
            // rejects the complete line even though the caption text and font are both valid.
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 19f;

            RectTransform selected = EnsureRect("V3SelectedState", buttonRect);
            selected.anchorMin = Vector2.zero;
            selected.anchorMax = Vector2.one;
            selected.pivot = new Vector2(.5f, .5f);
            selected.offsetMin = new Vector2(-5f, -5f);
            selected.offsetMax = new Vector2(5f, 5f);
            selected.localScale = Vector3.one;
            EnsureGradient(selected, Color.clear, Color.clear, theme.Cyan, 4f);
            RectTransform selectedInner = EnsureRect("V3SelectedInner", selected);
            Stretch(selectedInner, 6f, 6f);
            EnsureGradient(selectedInner, Color.clear, Color.clear, theme.TextPrimary, 2f);
            selected.SetAsLastSibling();
            selected.gameObject.SetActive(false);
        }

        private static void StyleTypography(Transform root)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == null)
                    text.font = mediumFont;
                text.raycastTarget = false;
            }
        }

        private static void Capture(
            string outputPath,
            int width,
            int height,
            bool showCommandWheel = false,
            bool showTargetingState = false,
            bool showTransportPassengers = false,
            bool showTacticalFeedback = false,
            bool showBuildPlacement = false,
            bool showBuildPlacementInvalid = false,
            bool showTutorialPresentation = false,
            bool showCommandAssistant = false,
            bool showAssistantTakeover = false,
            bool showThreatAlert = false,
            bool showThreatRoutePreview = false,
            bool showM02RestrictedControls = false,
            bool applyLocalization = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Match HUD prefab for capture: {PrefabPath}");

            SceneSetup[] previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("MatchHudV3CaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            GameObject canvasObject = new("MatchHudV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Texture2D backgroundTexture = LoadCaptureTexture(CaptureBackgroundPath);
            RawImage background = CreateRawImage("GameplayRuntimeBackground", canvasRect, backgroundTexture);
            Stretch(background.rectTransform);
            AspectRatioFitter backgroundFitter = background.gameObject.AddComponent<AspectRatioFitter>();
            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = (float)backgroundTexture.width / backgroundTexture.height;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
            instance.name = prefab.name;
            Stretch(instance.transform as RectTransform);
            ConfigureCaptureState(instance, showTransportPassengers);
            if (showM02RestrictedControls)
                ConfigureM02RestrictedControlsCaptureState(instance);
            if (showTacticalFeedback)
                ConfigureTacticalFeedbackCaptureState(instance);
            if (showBuildPlacement)
                ConfigureBuildPlacementCaptureState(instance, canvasRect, showBuildPlacementInvalid);
            if (showTutorialPresentation)
                ConfigureTutorialPresentationCaptureState(instance, canvasRect);
            if (showCommandAssistant)
                ConfigureAriaCommandAssistantCaptureState(instance, canvasRect);
            if (showAssistantTakeover)
                ConfigureAssistantTakeoverCaptureState(instance, canvasRect);
            if (showThreatAlert || showThreatRoutePreview)
                ConfigureThreatAlertCaptureState(instance, canvasRect, showThreatRoutePreview);
            if (applyLocalization)
            {
                foreach (V3LocalizedTextBinding binding in
                         instance.GetComponentsInChildren<V3LocalizedTextBinding>(true))
                {
                    binding.ApplyLocalization();
                }
            }
            if (showCommandWheel)
            {
                CommandWheelPanelView wheel = instance.GetComponentInChildren<CommandWheelPanelView>(true);
                if (wheel == null)
                    throw new MissingReferenceException("Missing Unit Command Wheel for capture.");
                wheel.Open();
                wheel.SetTargetingPreview(showTargetingState);
            }
            Canvas.ForceUpdateCanvases();
            foreach (MainMenuV3SectionLayoutView layout in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                layout.RefreshLayout();
            if (showTacticalFeedback)
                ConfigureTacticalFeedbackCaptureState(instance);
            if (showBuildPlacement)
            {
                BuildPlacementConfirmationBarView placementView =
                    canvasRect.GetComponentInChildren<BuildPlacementConfirmationBarView>(true);
                placementView?.GetComponent<MainMenuV3SectionLayoutView>()?.RefreshLayout();
                BuildPlacementConfirmationResponsiveLayoutView responsive =
                    canvasRect.GetComponentInChildren<BuildPlacementConfirmationResponsiveLayoutView>(true);
                responsive?.RefreshLayout();
                placementView?.ValidityPanel?.GetComponent<MainMenuV3SectionLayoutView>()?.RefreshLayout();
            }
            if (showTutorialPresentation)
            {
                foreach (MainMenuV3SectionLayoutView layout in
                         canvasRect.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                {
                    layout.RefreshLayout();
                }
            }
            if (showCommandAssistant)
            {
                foreach (MainMenuV3SectionLayoutView layout in
                         canvasRect.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                {
                    layout.RefreshLayout();
                }
            }
            if (showAssistantTakeover)
            {
                foreach (MainMenuV3SectionLayoutView layout in
                         canvasRect.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                {
                    layout.RefreshLayout();
                }
            }
            if (showThreatAlert || showThreatRoutePreview)
            {
                foreach (MainMenuV3SectionLayoutView layout in
                         canvasRect.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                {
                    layout.RefreshLayout();
                }
            }
            Canvas.ForceUpdateCanvases();

            RenderTexture previous = RenderTexture.active;
            Texture2D capture = new(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply(false);
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
                Debug.Log($"[MatchHudV3PrefabBuilder] capture=Passed size={width}x{height} path={outputPath} scene={scene.name}");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(capture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(backgroundTexture);
                if (previousSceneSetup != null && previousSceneSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void ConfigureCaptureState(GameObject instance, bool showTransportPassengers = false)
        {
            MatchHudSquadTrayView tray = instance.GetComponentInChildren<MatchHudSquadTrayView>(true);
            MatchHudSquadTraySlot selectedSlot = showTransportPassengers
                ? MatchHudSquadTraySlot.Transport
                : MatchHudSquadTraySlot.Soldiers;
            tray.SetSelectedSlot(selectedSlot);
            MatchHudSelectionPanelView selection = instance.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            tray.TryGetPortraitSprite(selectedSlot, out Sprite portrait);
            if (showTransportPassengers)
                portrait = RequireSprite(TransportHelicopterPortraitPath);
            else
                portrait = selection.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Soldiers);
            selection.Apply(new MatchHudSelectionPanelModel(
                true,
                showTransportPassengers ? "TRANSPORT HELICOPTER" : "RIFLE SQUAD",
                showTransportPassengers ? "AIR TRANSPORT" : "SQUAD 1  |  ANTI-INFANTRY",
                showTransportPassengers ? "HOLDING AT LZ" : "MOVING TO MARKER",
                showTransportPassengers ? "1,500 / 1,500" : "120 / 120",
                1f,
                portrait,
                showTransportPassengers ? SelectionSummaryPortraitKind.Transports : SelectionSummaryPortraitKind.Soldiers,
                !showTransportPassengers,
                RequireSprite(V3UiFoundationBuilder.MatchRankBadgeIconPath), true, true, true));
            MatchHudSelectionPanelPassengerItemModel[] passengers = showTransportPassengers
                ? CreateTransportPassengerCaptureItems()
                : Array.Empty<MatchHudSelectionPanelPassengerItemModel>();
            selection.ApplyTransportPassengers(new MatchHudTransportPassengersModel(
                true, false, new UiEntityHandle(1, 1), passengers.Length, showTransportPassengers ? 10 : 4,
                showTransportPassengers, passengers,
                soldierPassengerCount: passengers.Length,
                soldierCapacity: showTransportPassengers ? 10 : 4));
            if (showTransportPassengers)
            {
                selection.ToggleTransportPassengerDrawer();
                selection.ApplyTransportPassengers(new MatchHudTransportPassengersModel(
                    true, false, new UiEntityHandle(1, 1), passengers.Length, 10, true, passengers,
                    soldierPassengerCount: passengers.Length,
                    soldierCapacity: 10));

                TMP_Text ariaCopy = FindDeepChild(instance.transform, "AlertCue")?.GetComponent<TMP_Text>();
                if (ariaCopy != null)
                    ariaCopy.text = "Select Transport Helicopter.\nThen tap a passenger to exit.";
                TMP_Text doItLabel = FindDeepChild(instance.transform, "DoItButton")?.GetComponentInChildren<TMP_Text>(true);
                if (doItLabel != null)
                    doItLabel.text = "GOT IT";
            }
            BattleHudRuntimeFeedbackView feedback = instance.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            if (showTransportPassengers)
                feedback?.ShowFeedbackMessage("TRANSPORT READY — SELECT PASSENGER OR EXIT ALL", CommandFeedbackSeverity.Ready);
            else
            {
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(feedback, TacticalCommandMode.Move);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
                    feedback,
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetBlocked, "ATTACK UNAVAILABLE — SELECT A UNIT OR VALID TARGET"));
            }
            if (feedback != null)
            {
                feedback.FeedbackPanel.SetActive(true);
                feedback.FeedbackText.gameObject.SetActive(true);
                ConfigureText(
                    feedback.FeedbackText,
                    showTransportPassengers
                        ? "TRANSPORT READY — SELECT PASSENGER OR EXIT ALL"
                        : "ATTACK UNAVAILABLE — SELECT A UNIT OR VALID TARGET",
                    18f,
                    boldFont,
                    showTransportPassengers ? theme.Cyan : theme.OrangeRed,
                    TextAlignmentOptions.MidlineLeft);
                feedback.FeedbackText.enableAutoSizing = true;
                feedback.FeedbackText.fontSizeMin = 8f;
                feedback.FeedbackText.fontSizeMax = 18f;
                feedback.FeedbackText.margin = new Vector4(0f, 0f, 80f, 0f);
                RectTransform feedbackFrame = feedback.FeedbackPanel.transform.Find("Frame") as RectTransform;
                if (showTransportPassengers && feedbackFrame != null)
                    EnsureGradient(feedbackFrame, DarkTop, DarkBottom, theme.Cyan, 3f);
                if (showTransportPassengers && feedback.FeedbackIcon != null)
                    SetSprite(feedback.FeedbackIcon, RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath), theme.Cyan);
            }
        }

        private static void ConfigureM02RestrictedControlsCaptureState(GameObject instance)
        {
            MatchHudSquadTrayView tray = instance.GetComponentInChildren<MatchHudSquadTrayView>(true);
            if (tray == null)
                throw new MissingReferenceException("Missing squad tray for M02 restriction capture.");
            tray.ApplyMissionRestrictionVisibility(
                combatVehiclesDisabled: false,
                airDisabled: true,
                transportDisabled: true,
                hideUnrelatedControls: true);

            MatchOverlayCommandControlsView commands =
                instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            if (commands == null)
                throw new MissingReferenceException("Missing V3 command rail for M02 restriction capture.");
            commands.ApplyMissionRestrictionState(buildDisabled: false, supportDisabled: true);
        }

        private static void ConfigureTacticalFeedbackCaptureState(GameObject instance)
        {
            BattleHudRuntimeFeedbackView feedback = instance.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            if (feedback == null)
                throw new MissingReferenceException("Missing runtime feedback view for tactical feedback capture.");

            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(feedback, TacticalCommandMode.Attack);
            feedback.ApplyPersistentCommandFeedback(
                MatchHudCommandFeedbackModel.Show(
                    "ATTACK UNAVAILABLE — TARGET OUT OF RANGE",
                    CommandFeedbackSeverity.Error),
                MatchHudCommandFeedbackActionsModel.Hidden);

            Transform preview = FindDeepChild(instance.transform, "V3TacticalFeedbackPreview");
            if (preview == null)
                throw new MissingReferenceException("Missing V3 tactical feedback preview layer for capture.");
            preview.gameObject.SetActive(true);

            Transform aria = FindDeepChild(instance.transform, "AriaAssistantButton");
            AriaTutorialBriefingView tutorial = aria?.GetComponent<AriaTutorialBriefingView>();
            if (tutorial == null || !tutorial.TryBindHierarchy())
                throw new MissingReferenceException("The tactical feedback capture must use the one embedded ARIA tutorial panel.");
            tutorial.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel());
            tutorial.ApplyAccessibility(false, false);
            tutorial.SetPresentationVisible(true);
            tutorial.TitleText.gameObject.SetActive(false);
            tutorial.BodyText.richText = true;
            tutorial.BodyText.text =
                "Select <color=#00C8EE>Rifle Squad</color> first.\n" +
                "Then tap <color=#FF6A24>ATTACK</color> to engage the enemy.";
            SetTopLeft(tutorial.BodyText.rectTransform, 0f, 0f, 360f, 60f);
            tutorial.ProgressText.text = "TUTORIAL 1/5";
        }

        private static void ConfigureBuildPlacementCaptureState(
            GameObject matchHud,
            RectTransform canvasRoot,
            bool invalid)
        {
            TMP_Text ariaCopy = FindDeepChild(matchHud.transform, "AlertCue")?.GetComponent<TMP_Text>();
            if (ariaCopy != null)
            {
                ariaCopy.richText = true;
                ariaCopy.text = "Select <color=#00C8EE>Power Plant</color>.\nThen tap <color=#00C8EE>Place Building</color> to confirm.";
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildPlacementConfirmationBarPrefabSetupEditor.PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException("Missing Build Placement Confirmation V3 prefab for capture.");
            GameObject placement = UnityEngine.Object.Instantiate(prefab, canvasRoot, false);
            placement.name = prefab.name;
            RectTransform rect = placement.transform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one;
            CanvasGroup group = placement.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            placement.SetActive(true);
            placement.transform.SetAsLastSibling();
            BuildPlacementConfirmationBarView view = placement.GetComponent<BuildPlacementConfirmationBarView>();
            if (view == null)
                throw new MissingComponentException("Build Placement V3 capture prefab has no runtime view.");
            view.BindRuntimeCommands(new PreviewBuildingUiCommand(!invalid), null);
        }

        private sealed class PreviewBuildingUiCommand : IBuildingUiCommand
        {
            private readonly bool canConfirm;

            public PreviewBuildingUiCommand(bool canConfirmPlacement)
            {
                canConfirm = canConfirmPlacement;
            }

            public int CurrentDollars => 12500;
            public bool HasPendingBuildingPlacement => true;
            public bool CanConfirmBuildingPlacement => canConfirm;
            public string PlacementStatusText => canConfirm
                ? "Power Plant: Valid placement"
                : "Power Plant: Invalid placement";
            public int ActivePlacementCost => 1500;
            public int ActivePlacementCreditsCost => 250;
            public float ActivePlacementDurationSeconds => 45f;
            public int MaxQueuedUnitProductions => 25;

            public BuildingUiCommandFailure GetCampRequestFailure(
                GameObject prefab, int price, out string requiredBuildingDisplayName)
            {
                requiredBuildingDisplayName = string.Empty;
                return default;
            }

            public BuildingUiCommandFailure TryRequestCampItem(
                GameObject prefab,
                int price,
                out string requiredBuildingDisplayName,
                bool focusProducerOnSuccess)
            {
                requiredBuildingDisplayName = string.Empty;
                return default;
            }

            public bool CancelProduction(int buildingId, int pendingProductionIndex) => false;
            public bool ConfirmBuildingPlacement() => canConfirm;
            public void CancelBuildingPlacement() { }
            public bool RotateBuildingPlacement() => true;
        }

        private static void ConfigureTutorialPresentationCaptureState(
            GameObject matchHud,
            RectTransform canvasRoot)
        {
            BattleHudRuntimeFeedbackView feedback =
                matchHud.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            if (feedback != null && feedback.FeedbackPanel != null)
                feedback.FeedbackPanel.SetActive(true);

            AriaTutorialBriefingView tutorial =
                FindDeepChild(matchHud.transform, "AriaAssistantButton")?
                    .GetComponent<AriaTutorialBriefingView>();
            if (tutorial == null || !tutorial.TryBindHierarchy())
                throw new MissingReferenceException(
                    "Match HUD embedded ARIA tutorial hierarchy did not bind.");

            tutorial.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel());
            tutorial.ApplyAccessibility(false, false);
            tutorial.SetPresentationVisible(true);
        }

        private static void ConfigureAriaCommandAssistantCaptureState(
            GameObject matchHud,
            RectTransform canvasRoot)
        {
            BattleHudRuntimeFeedbackView feedback =
                matchHud.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            if (feedback != null && feedback.FeedbackPanel != null)
                feedback.FeedbackPanel.SetActive(true);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AriaTutorialBriefingPrefabBuilder.PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException("Missing POP-13 ARIA Command Assistant prefab for capture.");

            GameObject popupObject = UnityEngine.Object.Instantiate(prefab, canvasRoot, false);
            popupObject.name = "AriaCommandAssistantPreview";
            Stretch(popupObject.transform as RectTransform);
            popupObject.SetActive(true);
            AriaCommandAssistantPopupView popup = popupObject.GetComponent<AriaCommandAssistantPopupView>();
            if (popup == null || !popup.TryBindHierarchy())
                throw new MissingReferenceException("POP-13 ARIA Command Assistant hierarchy did not bind.");

            ApplyAriaCommandAssistantPreviewModel(
                popup,
                AriaTutorialBriefingPrefabBuilder.CreateCommandAssistantPreviewModel());
            popup.ApplyAccessibility(false, false);
            popup.Show();
            popup.LandscapeLayout.GetComponent<AriaTutorialHudVariantLayoutView>()?.RefreshLayout();
            popupObject.transform.SetAsLastSibling();
        }

        private static void ConfigureAssistantTakeoverCaptureState(
            GameObject matchHud,
            RectTransform canvasRoot)
        {
            BattleHudRuntimeFeedbackView feedback =
                matchHud.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            if (feedback != null && feedback.FeedbackPanel != null)
                feedback.FeedbackPanel.SetActive(false);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AriaCommandAssistantV3PrefabBuilder.PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException("Missing POP-10 Assistant Takeover V3 prefab state for capture.");

            GameObject popupObject = UnityEngine.Object.Instantiate(prefab, canvasRoot, false);
            popupObject.name = "AssistantTakeoverPreview";
            Stretch(popupObject.transform as RectTransform);
            popupObject.SetActive(true);
            AriaCommandAssistantPopupView popup = popupObject.GetComponent<AriaCommandAssistantPopupView>();
            if (popup == null || !popup.TryBindHierarchy())
                throw new MissingReferenceException("POP-10 Assistant Takeover V3 hierarchy did not bind.");

            ApplyAriaCommandAssistantPreviewModel(
                popup,
                AriaCommandAssistantV3PrefabBuilder.CreateAssistantTakeoverPreviewModel());
            popup.ApplyAccessibility(false, false);
            popup.Show();
            popup.LandscapeLayout.GetComponent<AriaTutorialHudVariantLayoutView>()?.RefreshLayout();
            popupObject.transform.SetAsLastSibling();
        }

        private static void ConfigureThreatAlertCaptureState(
            GameObject matchHud,
            RectTransform canvasRoot,
            bool routePreview)
        {
            BattleHudRuntimeFeedbackView feedback =
                matchHud.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            if (feedback != null && feedback.FeedbackPanel != null)
                feedback.FeedbackPanel.SetActive(false);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThreatAlertV3PrefabBuilder.PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException("Missing POP-01 Threat Alert V3 prefab for capture.");

            GameObject popupObject = UnityEngine.Object.Instantiate(prefab, canvasRoot, false);
            popupObject.name = routePreview ? "ThreatRoutePreview" : "ThreatAlertPreview";
            Stretch(popupObject.transform as RectTransform);
            popupObject.SetActive(true);
            ThreatAlertV3PopupView popup = popupObject.GetComponent<ThreatAlertV3PopupView>();
            if (popup == null)
                throw new MissingReferenceException("POP-01 Threat Alert V3 state view is missing.");

            if (routePreview)
                popup.ShowRoutePreview();
            else
                popup.ShowAlert();
            popupObject.transform.SetAsLastSibling();
        }

        internal static void ApplyAriaCommandAssistantPreviewModel(
            AriaCommandAssistantPopupView popup,
            UiAssistantPanelModel model)
        {
            popup.ApplyControlState(model.OwnershipText);
            popup.ApplyElapsed(model.ElapsedVisible, model.ElapsedWholeSeconds);
            popup.ApplyGoal(0, model.Goal0);
            popup.ApplyGoal(1, model.Goal1);
            popup.ApplyGoal(2, model.Goal2);
            popup.ApplyAlert(0, model.Alert0);
            popup.ApplyAlert(1, model.Alert1);
            popup.ApplyAlert(2, model.Alert2);
            popup.ApplyReport(0, model.Report0);
            popup.ApplyReport(1, model.Report1);
            popup.ApplyRecommendation(model);
            popup.ApplyTargetLock(model.TargetLock);
            popup.ApplyNarration(model.Narration, model.NarrationSubtitleText, model.NarrationSubtitlesVisible);
        }

        private static MatchHudSelectionPanelPassengerItemModel[] CreateTransportPassengerCaptureItems()
        {
            return new[]
            {
                new MatchHudSelectionPanelPassengerItemModel(
                    new UiEntityHandle(101, 1), "RIFLE SQUAD A", "SOLDIER", "100 / 100", 1f,
                    RequireSprite(PassengerRiflePortraitPath), true),
                new MatchHudSelectionPanelPassengerItemModel(
                    new UiEntityHandle(102, 1), "ENGINEER TEAM", "SOLDIER", "92 / 100", .92f,
                    RequireSprite(PassengerEngineerPortraitPath), true),
                new MatchHudSelectionPanelPassengerItemModel(
                    new UiEntityHandle(103, 1), "DALIA TEAM", "FIELD LEAD", "85 / 100", .85f,
                    RequireSprite(PassengerDaliaPortraitPath), true),
                new MatchHudSelectionPanelPassengerItemModel(
                    new UiEntityHandle(104, 1), "RIFLE SQUAD B", "SOLDIER", "78 / 100", .78f,
                    RequireSprite(PassengerRifleBPortraitPath), true)
            };
        }

        private static Texture2D LoadCaptureTexture(string relativePath)
        {
            string absolutePath = Path.GetFullPath(relativePath);
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException($"Missing runtime capture background: {absolutePath}");
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath), false))
                throw new InvalidOperationException($"Could not load runtime capture background: {absolutePath}");
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static V3GradientGraphic EnsureGradient(RectTransform target, Color top, Color bottom, Color border, float width, Button button = null)
        {
            RectTransform layer = FindDirectChild(target, "V3GradientLayer") as RectTransform;
            if (layer == null)
            {
                layer = CreateRect("V3GradientLayer", target, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Stretch(layer);
            }
            layer.SetAsFirstSibling();
            V3GradientGraphic gradient = layer.GetComponent<V3GradientGraphic>() ?? layer.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, border, width);
            gradient.raycastTarget = button != null;
            LayoutElement layout = layer.GetComponent<LayoutElement>() ?? layer.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            if (button != null)
                button.targetGraphic = gradient;
            return gradient;
        }

        private static void EnsureSquadCardLabel(RectTransform card, string value)
        {
            RectTransform strip = EnsureRect("NameStrip", card);
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.anchoredPosition = new Vector2(0f, 17f);
            strip.sizeDelta = new Vector2(-12f, 34f);
            Image stripImage = strip.GetComponent<Image>() ?? strip.gameObject.AddComponent<Image>();
            stripImage.color = new Color(0f, 0f, 0f, 0.72f);
            stripImage.raycastTarget = false;
            TMP_Text label = EnsureText(strip, "Label");
            ConfigureText(label, value, 17f, boldFont, new Color32(236, 238, 231, 255), TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 17f;
            Stretch(label.rectTransform, 3f, 1f);
            strip.SetAsLastSibling();
        }

        private static string SquadLabel(int index)
        {
            return index switch
            {
                1 => "RIFLE SQUAD",
                2 => "APC",
                3 => "TANK",
                4 => "HELICOPTER",
                _ => "TRANSPORT"
            };
        }

        private static Button EnsureGeneratedButton(Transform parent, string name, float x, float y, float width, float height, Color top, Color bottom, Color border, string label, float fontSize)
        {
            RectTransform rect = EnsureRect(name, parent);
            SetTopLeft(rect, x, y, width, height);
            Button button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
            EnsureGradient(rect, top, bottom, border, 3f, button);
            TMP_Text text = EnsureText(rect, "Label");
            ConfigureText(text, label, fontSize, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 5f, 3f);
            return button;
        }

        private static TMP_Text EnsureText(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null && existing.TryGetComponent(out TMP_Text text))
                return text;
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static Image EnsureImage(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null && existing.TryGetComponent(out Image image))
                return image;
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rect.gameObject.AddComponent<Image>();
        }

        private static RectTransform EnsureRect(string name, Transform parent)
        {
            return FindDirectChild(parent, name) as RectTransform ??
                   CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void ConfigureText(TMP_Text text, string value, float size, TMP_FontAsset font, Color color, TextAlignmentOptions alignment)
        {
            if (text == null)
                return;
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = Vector4.zero;
            text.raycastTarget = false;
        }

        private static void SetSprite(Image image, Sprite sprite, Color color)
        {
            if (image == null)
                return;
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
        }

        private static void SetImageTransparent(Image image)
        {
            if (image == null)
                return;
            image.sprite = null;
            image.color = Color.clear;
            image.raycastTarget = false;
            image.enabled = false;
        }

        private static void EnableInvisibleRootHitTarget(Image image)
        {
            SetImageTransparent(image);
            if (image == null)
                return;

            image.enabled = true;
            image.raycastTarget = true;
        }

        private static void ConfigureV3ButtonColorStates(Button button)
        {
            if (button == null)
                return;

            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.91f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.84f, 0.9f, 1f);
            colors.selectedColor = new Color(0.78f, 1f, 0.86f, 1f);
            colors.disabledColor = new Color(0.42f, 0.46f, 0.46f, 0.72f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f;
            button.colors = colors;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing Match HUD V3 sprite: {path}");
            return sprite;
        }

        private static RectTransform RequireRect(Transform root, string name)
        {
            RectTransform rect = FindDeepChild(root, name) as RectTransform;
            if (rect == null)
                throw new MissingReferenceException($"Missing Match HUD object '{name}' under '{root.name}'.");
            return rect;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Transform FindDirectChild(Transform root, string name)
        {
            if (root == null)
                return null;
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i).name == name)
                    return root.GetChild(i);
            return null;
        }

        private static int CountDirectButtons(Transform root)
        {
            int count = 0;
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i).GetComponent<Button>() != null)
                    count++;
            return count;
        }

        private static int RequireLiveButtonPointerTargets(GameObject root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button.targetGraphic == null || !button.targetGraphic.raycastTarget)
                {
                    throw new InvalidOperationException(
                        $"Match HUD button '{AnimationUtility.CalculateTransformPath(button.transform, root.transform)}' " +
                        "must expose a live raycast target.");
                }
            }
            return buttons.Length;
        }

        private static void RequireCommandRailPointerTargets(Transform railFrame)
        {
            for (int i = 0; i < railFrame.childCount; i++)
            {
                Transform child = railFrame.GetChild(i);
                Button button = child.GetComponent<Button>();
                if (button == null)
                    continue;

                Image rootHitTarget = child.GetComponent<Image>();
                if (rootHitTarget == null || !rootHitTarget.enabled || !rootHitTarget.raycastTarget)
                {
                    throw new InvalidOperationException(
                        $"Match HUD command '{child.name}' must keep its full-rect root pointer target enabled.");
                }
            }
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 position) =>
            V3UiPrefabFactory.CreateRect(name, parent, min, max, size, position);

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, float insetX = 0f, float insetY = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(insetX, insetY);
            rect.offsetMax = new Vector2(-insetX, -insetY);
            rect.localScale = Vector3.one;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null)
                target.gameObject.SetActive(active);
        }

        private static void AppendHierarchy(StringBuilder report, Transform root, Transform current, int depth)
        {
            RectTransform rect = current as RectTransform;
            string path = current == root ? current.name : AnimationUtility.CalculateTransformPath(current, root);
            report.Append(' ', depth * 2).Append(path).Append(" active=").Append(current.gameObject.activeSelf);
            if (rect != null)
            {
                report.Append(" anchors=").Append(rect.anchorMin).Append("..").Append(rect.anchorMax)
                    .Append(" pivot=").Append(rect.pivot).Append(" pos=").Append(rect.anchoredPosition)
                    .Append(" size=").Append(rect.sizeDelta);
            }
            report.Append(" components=");
            Component[] components = current.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (i > 0)
                    report.Append(',');
                report.Append(components[i] != null ? components[i].GetType().Name : "Missing");
            }
            report.AppendLine();
            for (int i = 0; i < current.childCount; i++)
                AppendHierarchy(report, root, current.GetChild(i), depth + 1);
        }
    }
}
#endif
