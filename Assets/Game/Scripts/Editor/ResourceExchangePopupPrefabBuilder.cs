#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Builds POP-12 from the shared V3 visual language. All chrome and button fills are
    /// procedural, so the popup does not own duplicate raster gradients or borders.
    /// </summary>
    public static class ResourceExchangePopupPrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab";
        private const string BackgroundPath = "Assets/Game/Art/UI/V3Shared/Backgrounds/SCN01_LoadingEnvironment_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 ReferenceResolution = new(1280f, 720f);
        private static readonly Color Border = new Color32(64, 79, 84, 255);
        private static readonly Color DarkTop = new Color32(26, 38, 42, 255);
        private static readonly Color DarkBottom = new Color32(3, 11, 14, 255);
        private static readonly Color RaisedTop = new Color32(43, 54, 58, 255);
        private static readonly Color RaisedBottom = new Color32(10, 20, 23, 255);
        private static readonly Color TextPrimary = new Color32(244, 246, 242, 255);
        private static readonly Color TextMuted = new Color32(168, 180, 181, 255);
        private static readonly Color Cyan = new Color32(0, 190, 230, 255);
        private static readonly Color Amber = new Color32(247, 173, 0, 255);
        private static readonly Color Green = new Color32(61, 190, 67, 255);
        private static readonly Color Red = new Color32(232, 65, 35, 255);
        private static readonly Color Purple = new Color32(137, 79, 206, 255);
        private static readonly Color BlueTop = new Color32(27, 132, 207, 255);
        private static readonly Color BlueBottom = new Color32(3, 61, 112, 255);
        private static readonly Color GreenTop = new Color32(69, 173, 59, 255);
        private static readonly Color GreenBottom = new Color32(8, 78, 28, 255);

        private static TMP_FontAsset bold;
        private static TMP_FontAsset medium;
        private static V3UiArtCatalog catalog;
        private static Texture2D background;

        [MenuItem("Game/UI/V3/Rebuild POP-12 Resource Logistics Exchange")]
        [MenuItem("Game/UI/Rebuild Resource Exchange Popup")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/Game/Prefabs/UI/Shell/Popups");

            RectTransform root = Rect("POP12_ResourceExchangePopup", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            try
            {
                root.gameObject.AddComponent<CanvasGroup>();
                UIPopupMotionView.Ensure(root.gameObject);
                ResourceExchangePopupView popupView = root.gameObject.AddComponent<ResourceExchangePopupView>();
                ResourceExchangePopupRuntimeView runtimeView = root.gameObject.AddComponent<ResourceExchangePopupRuntimeView>();

                UnityEngine.UI.Image blocker = root.gameObject.AddComponent<UnityEngine.UI.Image>();
                blocker.color = Color.black;
                blocker.raycastTarget = true;

                RectTransform composition = TopLeft("ResourceExchangeRoot", root, 0, 0, ReferenceResolution.x, ReferenceResolution.y);
                composition.gameObject.AddComponent<RectMask2D>();
                BuildBackground(composition);

                var rightTargets = new List<RectTransform>();
                var widthTargets = new List<RectTransform>();
                HeaderRefs header = BuildHeader(composition, rightTargets, widthTargets);
                TabRefs tabs = BuildRecipeHeader(composition);
                ResourceExchangeRecipeCardView[] cards = BuildRecipeCards(composition);
                DetailRefs detail = BuildDetailPanel(composition, widthTargets);
                QueueRefs queue = BuildQueuePanel(composition, rightTargets);
                FooterRefs footer = BuildFooter(composition, widthTargets);

                ApplyMobileLayout(composition, header, detail, queue, footer, cards, rightTargets, widthTargets);

                MainMenuV3SectionLayoutView layout = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
                layout.Configure(ReferenceResolution, MainMenuV3SectionAlignment.Center, rightTargets.ToArray(), true, null, widthTargets.ToArray());

                SerializedObject serialized = new(popupView);
                SetObject(serialized, "popupRoot", root.gameObject);
                SetObject(serialized, "closeButton", header.CloseButton);
                SetObject(serialized, "footerCancelButton", footer.CancelButton);
                SetObject(serialized, "footerConfirmButton", footer.ConfirmButton);
                SetObject(serialized, "titleText", header.TitleText);
                SetObject(serialized, "queueCapacityText", queue.CapacityText);
                SetObject(serialized, "materialsText", header.MaterialsText);
                SetObject(serialized, "oilText", header.OilText);
                SetObject(serialized, "fuelText", header.FuelText);
                SetObject(serialized, "rushTicketsText", header.RushText);
                SetObject(serialized, "exportTabButton", tabs.ExportButton);
                SetObject(serialized, "importTabButton", tabs.ImportButton);
                SetObject(serialized, "exportTabFrameImage", tabs.ExportFrame);
                SetObject(serialized, "importTabFrameImage", tabs.ImportFrame);
                SetObject(serialized, "exportCountText", tabs.ExportCount);
                SetObject(serialized, "importCountText", tabs.ImportCount);
                SetObject(serialized, "selectedTabFrameSprite", null);
                SetObject(serialized, "defaultTabFrameSprite", null);
                SetObject(serialized, "recipeContentRoot", cards[0].transform.parent as RectTransform);
                SetObject(serialized, "recipeCardTemplate", cards[0]);
                SetObjectArray(serialized, "staticRecipeCards", cards);
                SetObject(serialized, "defaultRecipeCardFrameSprite", null);
                SetObject(serialized, "selectedRecipeCardFrameSprite", null);
                SetObject(serialized, "lockedRecipeCardFrameSprite", null);
                SetObjectArray(serialized, "recipeThumbnailSprites", RecipeIcons());
                SetObject(serialized, "detailThumbnailImage", detail.Thumbnail);
                SetObject(serialized, "detailNameText", detail.Name);
                SetObject(serialized, "detailRouteText", detail.Route);
                SetObject(serialized, "detailRateText", detail.Rate);
                SetObject(serialized, "detailAmountText", detail.Amount);
                SetObject(serialized, "detailInputText", detail.Input);
                SetObject(serialized, "detailOutputText", detail.Output);
                SetObject(serialized, "detailDurationText", detail.Duration);
                SetObject(serialized, "detailRequirementsText", detail.Requirements);
                SetObject(serialized, "detailInstructionText", detail.Instruction);
                SetObject(serialized, "detailWarningImage", detail.Warning);
                SetObject(serialized, "amountDecreaseButton", detail.Minus);
                SetObject(serialized, "amountIncreaseButton", detail.Plus);
                SetObject(serialized, "confirmButton", detail.Confirm);
                SetObject(serialized, "confirmButtonText", detail.ConfirmLabel);
                SetObject(serialized, "queueContentRoot", queue.Content);
                SetObject(serialized, "queueRowTemplate", queue.Rows[0]);
                SetObjectArray(serialized, "staticQueueRows", queue.Rows);
                SetObject(serialized, "rushAllButton", queue.RushAll);
                SetObject(serialized, "clearCompletedButton", queue.ClearDone);
                SetObject(serialized, "instructionText", detail.Instruction);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject runtimeSerialized = new(runtimeView);
                SetObject(runtimeSerialized, "view", popupView);
                runtimeSerialized.ApplyModifiedPropertiesWithoutUndo();


                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException($"Failed to save POP-12 V3 prefab: {PrefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root.gameObject);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[ResourceExchangePopupPrefabBuilder] result=Passed v3=True fullSafeWidth=True sharedLogo=True gradients=procedural");
        }

        [MenuItem("Game/UI/V3/Validate POP-12 Resource Logistics Exchange")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing POP-12 V3 prefab: {PrefabPath}");

            ResourceExchangePopupView view = prefab.GetComponent<ResourceExchangePopupView>();
            ResourceExchangePopupRuntimeView runtimeView = prefab.GetComponent<ResourceExchangePopupRuntimeView>();
            if (view == null || runtimeView == null || runtimeView.View != view)
                throw new MissingReferenceException("POP-12 runtime presenter binding is incomplete.");
            if (view.CloseButton == null || view.FooterCancelButton == null || view.FooterConfirmButton == null ||
                view.ExportTabButton == null || view.ImportTabButton == null || view.ConfirmButton == null ||
                view.AmountDecreaseButton == null || view.AmountIncreaseButton == null ||
                view.RushAllButton == null || view.ClearCompletedButton == null)
                throw new MissingReferenceException("POP-12 V3 buttons are not fully bound.");
            if (view.StaticRecipeCards == null || view.StaticRecipeCards.Length != 7 ||
                view.StaticQueueRows == null || view.StaticQueueRows.Length != 4)
                throw new MissingReferenceException("POP-12 must retain seven recipe and four queue runtime slots.");

            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || !layout.ExpandToCanvasWidth || layout.ReferenceResolution != ReferenceResolution)
                throw new InvalidOperationException("POP-12 V3 must fill both reference and wide canvases.");
            if (prefab.transform.Find("ResourceExchangeRoot/Header/LogoPanel/SharedMainMenuLogo") == null)
                throw new MissingReferenceException("POP-12 does not use the shared V3 Main Menu logo prefab.");

            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 35)
                throw new InvalidOperationException($"POP-12 requires layered procedural V3 chrome; found {gradients.Length} gradients.");
            foreach (V3GradientGraphic gradient in gradients)
            {
                SerializedObject serialized = new(gradient);
                Color border = serialized.FindProperty("borderColor").colorValue;
                float width = serialized.FindProperty("borderWidth").floatValue;
                if (border.a > .01f && Mathf.Abs(width - 3f) > .001f)
                    throw new InvalidOperationException($"POP-12 border must be exactly 3 px: {GetPath(gradient.transform)} width={width}");
            }

            foreach (UnityEngine.UI.Image image in prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.sprite == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(image.sprite);
                if (!path.StartsWith("Assets/Game/Art/UI/V3Shared/", StringComparison.Ordinal) &&
                    !path.StartsWith(CanonicalUiResourceIconPaths.Root, StringComparison.Ordinal))
                    throw new InvalidOperationException($"POP-12 owns an unshared raster asset: {GetPath(image.transform)} -> {path}");
            }

            Debug.Log($"[ResourceExchangePopupV3Validation] result=Passed gradients={gradients.Length} borders=3 sharedLogo=True wide=True");
        }

        private static void LoadAssets()
        {
            bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            medium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            catalog = V3UiFoundationBuilder.RequireCatalog();
            background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            if (bold == null || medium == null || catalog == null || background == null)
                throw new MissingReferenceException("POP-12 V3 shared fonts, catalog, or background are missing.");
        }

        private static void BuildBackground(RectTransform root)
        {
            RectTransform artRoot = Rect("SharedEnvironment", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RawImage art = artRoot.gameObject.AddComponent<RawImage>();
            art.texture = background;
            art.color = new Color(.55f, .58f, .58f, 1f);
            art.raycastTarget = false;
            AspectRatioFitter fitter = artRoot.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.width / (float)background.height;
            UnityEngine.UI.Image shade = Img("BackdropShade", root, null, new Color(0f, .025f, .035f, .79f), false);
            Stretch(shade.rectTransform);
        }

        private static HeaderRefs BuildHeader(RectTransform root, ICollection<RectTransform> right, ICollection<RectTransform> widths)
        {
            RectTransform header = TopLeft("Header", root, 15, 12, 1642, 94);
            RectTransform logo = TopLeft("LogoPanel", header, 0, 0, 345, 94);
            Gradient(logo, DarkTop, DarkBottom, Border);
            V3UiFoundationBuilder.AddMainMenuLogo(logo, left: 13, top: 12, right: 13, bottom: 12);

            RectTransform titlePanel = TopLeft("TitlePanel", header, 355, 0, 670, 94);
            Gradient(titlePanel, DarkTop, DarkBottom, Border);
            widths.Add(titlePanel);
            TMP_Text title = Text("Title", titlePanel, "RESOURCE LOGISTICS EXCHANGE", 35, bold, TextAlignmentOptions.MidlineLeft, TextPrimary);
            Horizontal(title.rectTransform, 25, 20, 5, 48);
            TMP_Text subtitle = Text("Subtitle", titlePanel, "CONVERT SURPLUS STOCK INTO OPERATIONAL SUPPLY", 16, medium, TextAlignmentOptions.MidlineLeft, Cyan);
            Horizontal(subtitle.rectTransform, 26, 20, 52, 30);

            RectTransform materials = ResourceChip(header, "Materials", 1035, catalog.MaterialsIcon, "MATERIALS", "180", Cyan, out TMP_Text materialsText);
            RectTransform oil = ResourceChip(header, "Oil", 1210, catalog.OilIcon, "OIL", "620", Amber, out TMP_Text oilText);
            RectTransform fuel = ResourceChip(header, "Fuel", 1385, catalog.FuelIcon, "FUEL", "310", Red, out TMP_Text fuelText);
            Button close = GradientButton("CloseButton", header, 1560, 0, 82, 94, RaisedTop, DarkBottom, Border);
            TMP_Text closeText = Text("Icon", close.transform, "X", 35, bold, TextAlignmentOptions.Center, TextPrimary);
            Stretch(closeText.rectTransform);
            right.Add(materials); right.Add(oil); right.Add(fuel); right.Add(close.GetComponent<RectTransform>());
            return new HeaderRefs(title, materialsText, oilText, fuelText, null, close);
        }

        private static TabRefs BuildRecipeHeader(RectTransform root)
        {
            RectTransform left = TopLeft("RecipeColumn", root, 15, 116, 550, 735);
            Gradient(left, DarkTop, DarkBottom, Border);
            TMP_Text heading = Text("Heading", left, "SELECT EXCHANGE TYPE", 27, bold, TextAlignmentOptions.MidlineLeft, TextPrimary);
            TopLeft(heading.rectTransform, 20, 12, 510, 44);
            TMP_Text hint = Text("Hint", left, "Choose a logistics conversion route", 15, medium, TextAlignmentOptions.MidlineLeft, TextMuted);
            TopLeft(hint.rectTransform, 20, 52, 510, 28);

            Button export = GradientButton("ExportTab", left, 18, 656, 245, 58, RaisedTop, DarkBottom, Cyan);
            TMP_Text exportLabel = Text("Label", export.transform, "VIEW RATES", 19, bold, TextAlignmentOptions.Center, TextPrimary); Stretch(exportLabel.rectTransform);
            Button import = GradientButton("ImportTab", left, 272, 656, 260, 58, RaisedTop, DarkBottom, Border);
            TMP_Text importLabel = Text("Label", import.transform, "EXCHANGE INFO", 19, bold, TextAlignmentOptions.Center, TextPrimary); Stretch(importLabel.rectTransform);
            UnityEngine.UI.Image exportFrame = LegacyBindingImage("LegacyFrame", export.transform);
            UnityEngine.UI.Image importFrame = LegacyBindingImage("LegacyFrame", import.transform);
            TMP_Text exportCount = Text("Count", export.transform, "3", 12, medium, TextAlignmentOptions.BottomRight, Cyan); TopLeft(exportCount.rectTransform, 216, 31, 20, 20);
            TMP_Text importCount = Text("Count", import.transform, "3", 12, medium, TextAlignmentOptions.BottomRight, TextMuted); TopLeft(importCount.rectTransform, 231, 31, 20, 20);
            return new TabRefs(export, import, exportFrame, importFrame, exportCount, importCount);
        }

        private static ResourceExchangeRecipeCardView[] BuildRecipeCards(RectTransform root)
        {
            RectTransform content = root.Find("RecipeColumn") as RectTransform;
            RectTransform recipes = TopLeft("RecipeCards", content, 18, 90, 514, 550);
            Color[] accents = { Cyan, Amber, Green, Purple, Red, Cyan, Amber };
            Color[] tops =
            {
                new Color32(25, 109, 157, 255), new Color32(129, 88, 11, 255),
                new Color32(34, 116, 46, 255), new Color32(83, 47, 131, 255),
                new Color32(119, 39, 25, 255), new Color32(22, 91, 118, 255),
                new Color32(110, 74, 12, 255)
            };
            Sprite[] icons = RecipeIcons();
            ResourceExchangeRecipeCardView[] cards = new ResourceExchangeRecipeCardView[7];
            for (int i = 0; i < cards.Length; i++)
            {
                float y = i * 132f;
                cards[i] = BuildRecipeCard($"RecipeCard{i + 1}", recipes, y, tops[i], Color.Lerp(tops[i], Color.black, .68f), accents[i], icons[i], i);
                if (i >= 4) cards[i].gameObject.SetActive(false);
            }
            return cards;
        }

        private static ResourceExchangeRecipeCardView BuildRecipeCard(string name, Transform parent, float y, Color top, Color bottom, Color accent, Sprite iconSprite, int index)
        {
            RectTransform rect = TopLeft(name, parent, 0, y, 514, 120);
            V3GradientGraphic chrome = Gradient(rect, top, bottom, accent);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = chrome; button.transition = Selectable.Transition.None;
            UnityEngine.UI.Image frameBinding = LegacyBindingImage("FrameBinding", rect);
            RectTransform iconPanel = TopLeft("IconPanel", rect, 8, 8, 104, 104); Gradient(iconPanel, DarkTop, DarkBottom, accent);
            UnityEngine.UI.Image icon = Img("Thumbnail", iconPanel, iconSprite, Color.white, false); TopLeft(icon.rectTransform, 20, 20, 64, 64); icon.preserveAspect = true;
            TMP_Text title = Text("Title", rect, "FUEL TO MATERIALS", 23, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(title.rectTransform, 127, 12, 310, 36);
            TMP_Text input = Text("Input", rect, "SPEND 100 FUEL", 15, medium, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(input.rectTransform, 127, 50, 178, 27);
            TMP_Text output = Text("Output", rect, "RECEIVE 180 MATERIALS", 15, bold, TextAlignmentOptions.MidlineLeft, accent); TopLeft(output.rectTransform, 127, 78, 270, 27);
            TMP_Text duration = Text("Duration", rect, "01:30", 15, bold, TextAlignmentOptions.Center, TextPrimary); TopLeft(duration.rectTransform, 426, 67, 75, 31);
            TMP_Text reason = Text("Reason", rect, string.Empty, 12, bold, TextAlignmentOptions.BottomRight, Red); TopLeft(reason.rectTransform, 309, 94, 190, 18);
            UnityEngine.UI.Image selected = StatusSquare("SelectedCheck", rect, 462, 9, Green, "V");
            UnityEngine.UI.Image locked = StatusSquare("Lock", rect, 462, 9, TextMuted, "L");
            UnityEngine.UI.Image warning = StatusSquare("Warning", rect, 427, 9, Red, "!");
            selected.gameObject.SetActive(index == 0); locked.gameObject.SetActive(index >= 4); warning.gameObject.SetActive(index >= 4);
            GameObject disabled = Img("DisabledOverlay", rect, null, new Color(0, 0, 0, .55f), false).gameObject;
            Stretch(disabled.GetComponent<RectTransform>()); disabled.transform.SetSiblingIndex(1); disabled.SetActive(index >= 4);

            ResourceExchangeRecipeCardView view = rect.gameObject.AddComponent<ResourceExchangeRecipeCardView>();
            SerializedObject serialized = new(view);
            SetObject(serialized, "selectionButton", button); SetObject(serialized, "frameImage", frameBinding); SetObject(serialized, "thumbnailImage", icon);
            SetObject(serialized, "selectedCheckImage", selected); SetObject(serialized, "lockImage", locked); SetObject(serialized, "warningImage", warning);
            SetObject(serialized, "disabledOverlay", disabled); SetObject(serialized, "titleText", title); SetObject(serialized, "inputText", input);
            SetObject(serialized, "outputText", output); SetObject(serialized, "durationText", duration); SetObject(serialized, "reasonText", reason);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static DetailRefs BuildDetailPanel(RectTransform root, ICollection<RectTransform> widths)
        {
            RectTransform detail = TopLeft("DetailPanel", root, 576, 116, 565, 735); Gradient(detail, DarkTop, DarkBottom, Border); widths.Add(detail);
            TMP_Text section = Text("SectionTitle", detail, "SELECTED EXCHANGE", 20, bold, TextAlignmentOptions.MidlineLeft, Cyan); Horizontal(section.rectTransform, 23, 20, 13, 31);
            TMP_Text name = Text("Name", detail, "FUEL TO MATERIALS", 36, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); Horizontal(name.rectTransform, 23, 20, 44, 54);
            TMP_Text route = Text("RouteValue", detail, "CONVERSION ROUTE", 16, medium, TextAlignmentOptions.MidlineLeft, TextMuted); Horizontal(route.rectTransform, 24, 20, 96, 28);
            RectTransform wallet = Horizontal("YourResources", detail, 20, 20, 137, 103); Gradient(wallet, RaisedTop, DarkBottom, Border);
            TMP_Text walletTitle = Text("Title", wallet, "YOUR RESOURCES", 17, bold, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(walletTitle.rectTransform, 17, 5, 170, 28);
            ResourceMini(wallet, "Materials", 15, catalog.MaterialsIcon, "180", Cyan); ResourceMini(wallet, "Oil", 185, catalog.OilIcon, "620", Amber); ResourceMini(wallet, "Fuel", 355, catalog.FuelIcon, "310", Red);
            TMP_Text detailsTitle = Text("DetailsTitle", detail, "EXCHANGE DETAILS", 19, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); Horizontal(detailsTitle.rectTransform, 23, 20, 255, 34);
            RectTransform spend = Horizontal("Spend", detail, 20, 285, 292, 94); Gradient(spend, RaisedTop, DarkBottom, Border);
            TMP_Text spendLabel = Text("Label", spend, "SPEND", 14, bold, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(spendLabel.rectTransform, 17, 8, 100, 25);
            TMP_Text input = Text("Value", spend, "100 FUEL", 27, bold, TextAlignmentOptions.MidlineLeft, Red); TopLeft(input.rectTransform, 17, 34, 230, 45);
            RectTransform receive = Rect("Receive", detail, new Vector2(.5f, 1), new Vector2(1, 1), new Vector2(-30, 94), new Vector2(5, -292)); receive.pivot = new Vector2(.5f, 1); Gradient(receive, RaisedTop, DarkBottom, Border);
            TMP_Text receiveLabel = Text("Label", receive, "RECEIVE", 14, bold, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(receiveLabel.rectTransform, 17, 8, 100, 25);
            TMP_Text output = Text("Value", receive, "180 MATERIALS", 27, bold, TextAlignmentOptions.MidlineLeft, Cyan); Horizontal(output.rectTransform, 17, 12, 34, 45);
            TMP_Text amountTitle = Text("AmountTitle", detail, "AMOUNT", 18, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(amountTitle.rectTransform, 23, 402, 180, 31);
            RectTransform stepper = Horizontal("AmountStepper", detail, 20, 20, 438, 72); Gradient(stepper, RaisedTop, DarkBottom, Border);
            Button minus = GradientButton("AmountMinus", stepper, 3, 3, 72, 66, RaisedTop, DarkBottom, Border); ButtonGlyph(minus, "-", 31);
            Button plus = GradientButtonRight("AmountPlus", stepper, 3, 3, 72, 66, RaisedTop, DarkBottom, Border); ButtonGlyph(plus, "+", 31);
            TMP_Text amount = Text("Amount", stepper, "100", 30, bold, TextAlignmentOptions.Center, TextPrimary); Horizontal(amount.rectTransform, 80, 80, 4, 61);
            RectTransform track = Horizontal("AmountTrack", detail, 20, 20, 523, 18); Gradient(track, new Color32(14, 31, 36, 255), new Color32(5, 15, 18, 255), Border);
            RectTransform fill = Rect("Fill", track, Vector2.zero, new Vector2(.52f, 1), Vector2.zero, Vector2.zero); Gradient(fill, new Color32(39, 195, 229, 255), new Color32(0, 119, 170, 255), Color.clear);
            RectTransform knob = Rect("Knob", track, new Vector2(.52f, .5f), new Vector2(.52f, .5f), new Vector2(18, 31), Vector2.zero); Gradient(knob, TextPrimary, TextMuted, Cyan);
            TMP_Text rate = DetailMetric(detail, "Rate", "RATE", "1 FUEL = 1.8 MATERIALS", 570, Cyan);
            TMP_Text duration = DetailMetric(detail, "Duration", "DURATION", "01:30", 614, Amber);
            TMP_Text requirements = DetailMetric(detail, "Requirements", "QUEUE", "1 SLOT REQUIRED", 658, TextPrimary);
            TMP_Text instruction = Text("Instruction", detail, "Adjust the amount, then convert to add this exchange to the logistics queue.", 14, medium, TextAlignmentOptions.MidlineLeft, TextMuted); Horizontal(instruction.rectTransform, 23, 205, 688, 38); instruction.textWrappingMode = TextWrappingModes.Normal;
            UnityEngine.UI.Image warning = StatusSquare("Warning", detail, 532, 690, Red, "!"); warning.gameObject.SetActive(false);
            UnityEngine.UI.Image thumbnail = Img("Thumbnail", detail, catalog.FuelIcon, Color.white, false); TopLeft(thumbnail.rectTransform, 485, 48, 58, 58); thumbnail.preserveAspect = true;
            Button confirm = GradientButtonRight("ConfirmButton", detail, 18, 681, 180, 43, GreenTop, GreenBottom, Green);
            TMP_Text confirmLabel = Text("Label", confirm.transform, "CONVERT", 20, bold, TextAlignmentOptions.Center, TextPrimary); Stretch(confirmLabel.rectTransform);
            return new DetailRefs(thumbnail, name, route, rate, amount, input, output, duration, requirements, instruction, warning, minus, plus, confirm, confirmLabel);
        }

        private static QueueRefs BuildQueuePanel(RectTransform root, ICollection<RectTransform> right)
        {
            RectTransform queue = TopLeft("ExchangeQueuePanel", root, 1152, 116, 505, 735); Gradient(queue, DarkTop, DarkBottom, Border); right.Add(queue);
            TMP_Text title = Text("Title", queue, "EXCHANGE QUEUE", 28, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(title.rectTransform, 20, 14, 330, 42);
            TMP_Text capacity = Text("Capacity", queue, "3/3", 24, bold, TextAlignmentOptions.Center, Amber); TopLeft(capacity.rectTransform, 409, 14, 74, 42);
            TMP_Text sub = Text("Subtitle", queue, "ACTIVE LOGISTICS ORDERS", 14, medium, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(sub.rectTransform, 21, 54, 330, 25);
            RectTransform rowsRoot = TopLeft("Rows", queue, 16, 92, 473, 472);
            ResourceExchangeQueueItemView[] rows = new ResourceExchangeQueueItemView[4];
            for (int i = 0; i < rows.Length; i++) { rows[i] = BuildQueueRow($"QueueRow{i + 1}", rowsRoot, i * 153f, i); if (i == 3) rows[i].gameObject.SetActive(false); }
            RectTransform capacityPanel = TopLeft("CapacityPanel", queue, 16, 581, 473, 62); Gradient(capacityPanel, RaisedTop, DarkBottom, Border);
            TMP_Text capacityLabel = Text("Label", capacityPanel, "QUEUE CAPACITY", 16, bold, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(capacityLabel.rectTransform, 17, 6, 240, 24);
            TMP_Text capacityValue = Text("Value", capacityPanel, "3 / 3 SLOTS USED", 20, bold, TextAlignmentOptions.MidlineLeft, Amber); TopLeft(capacityValue.rectTransform, 17, 28, 300, 28);
            Button clear = GradientButton("ClearCompletedButton", queue, 16, 657, 224, 58, RaisedTop, DarkBottom, Border); ButtonLabel(clear, "CLEAR DONE", 17);
            Button rush = GradientButton("RushAllButton", queue, 249, 657, 240, 58, BlueTop, BlueBottom, Cyan); ButtonLabel(rush, "RUSH ALL", 17);
            return new QueueRefs(rowsRoot, rows, rush, clear, capacity);
        }

        private static ResourceExchangeQueueItemView BuildQueueRow(string name, Transform parent, float y, int index)
        {
            RectTransform row = TopLeft(name, parent, 0, y, 473, 140); Gradient(row, RaisedTop, DarkBottom, index == 2 ? Green : Border);
            UnityEngine.UI.Image thumb = Img("Thumb", row, RecipeIcons()[Mathf.Min(index, 3)], Color.white, false); TopLeft(thumb.rectTransform, 17, 20, 58, 58); thumb.preserveAspect = true;
            TMP_Text number = Text("Number", row, (index + 1).ToString(), 17, bold, TextAlignmentOptions.Center, TextPrimary); TopLeft(number.rectTransform, 8, 5, 28, 28);
            TMP_Text displayName = Text("Name", row, index == 0 ? "FUEL TO MATERIALS" : index == 1 ? "OIL TO MATERIALS" : "MATERIALS TO OIL", 18, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(displayName.rectTransform, 86, 12, 265, 30);
            TMP_Text input = Text("Input", row, "100 FUEL", 13, medium, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(input.rectTransform, 86, 44, 120, 24);
            TMP_Text output = Text("Output", row, "180 MATERIALS", 13, bold, TextAlignmentOptions.MidlineLeft, Cyan); TopLeft(output.rectTransform, 205, 44, 170, 24);
            RectTransform track = TopLeft("ProgressTrack", row, 86, 81, 252, 13); Gradient(track, new Color32(11, 29, 33, 255), new Color32(4, 14, 17, 255), Border);
            UnityEngine.UI.Image fill = Img("ProgressFill", track, null, index == 2 ? Green : Cyan, false); Stretch(fill.rectTransform); fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillAmount = index == 0 ? .65f : index == 2 ? 1f : 0f;
            TMP_Text percent = Text("Percent", row, index == 0 ? "65%" : index == 2 ? "100%" : "0%", 13, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(percent.rectTransform, 348, 73, 58, 28);
            TMP_Text time = Text("Time", row, index == 2 ? "DONE" : index == 0 ? "00:11" : "01:30", 16, bold, TextAlignmentOptions.Right, index == 2 ? Green : Amber); TopLeft(time.rectTransform, 354, 13, 98, 28);
            TMP_Text state = Text("State", row, index == 2 ? "COMPLETE" : index == 0 ? "IN PROGRESS" : "QUEUED", 12, bold, TextAlignmentOptions.MidlineLeft, index == 2 ? Green : TextMuted); TopLeft(state.rectTransform, 86, 103, 178, 24);
            Button rush = GradientButton("RushButton", row, 350, 101, 48, 31, BlueTop, BlueBottom, Cyan); ButtonGlyph(rush, ">", 18);
            Button cancel = GradientButton("CancelButton", row, 405, 101, 48, 31, new Color32(125, 38, 28, 255), new Color32(55, 12, 10, 255), Red); ButtonGlyph(cancel, "X", 15);
            UnityEngine.UI.Image completed = StatusSquare("Completed", row, 419, 99, Green, "V"); completed.gameObject.SetActive(index == 2);
            UnityEngine.UI.Image warning = StatusSquare("Warning", row, 365, 99, Red, "!"); warning.gameObject.SetActive(false);
            ResourceExchangeQueueItemView view = row.gameObject.AddComponent<ResourceExchangeQueueItemView>();
            SerializedObject serialized = new(view);
            SetObject(serialized, "rushButton", rush); SetObject(serialized, "cancelButton", cancel); SetObject(serialized, "thumbnailImage", thumb); SetObject(serialized, "progressFillImage", fill);
            SetObject(serialized, "completedImage", completed); SetObject(serialized, "warningImage", warning); SetObject(serialized, "numberText", number); SetObject(serialized, "nameText", displayName);
            SetObject(serialized, "inputText", input); SetObject(serialized, "outputText", output); SetObject(serialized, "timeText", time); SetObject(serialized, "percentText", percent); SetObject(serialized, "stateText", state);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // Two reading columns: choose a route, then review and start. The queue stays
        // visible below the form; long lists scroll instead of shrinking touch targets.
        private static void ApplyMobileLayout(RectTransform root,
            HeaderRefs header, DetailRefs detail, QueueRefs queue, FooterRefs footer,
            ResourceExchangeRecipeCardView[] cards, List<RectTransform> right, List<RectTransform> widths)
        {
            right.Clear(); widths.Clear();
            void Place(string path, float x, float y, float w, float h) => TopLeft((RectTransform)root.Find(path), x, y, w, h);
            void Hide(string path) => root.Find(path).gameObject.SetActive(false);
            void Label(string path, string value, float size) { var t=root.Find(path).GetComponent<TMP_Text>(); t.text=value; t.fontSize=size; }
            Place("Header",12,12,1256,78);
            Hide("Header/LogoPanel");
            Place("Header/TitlePanel",0,0,632,78); widths.Add((RectTransform)root.Find("Header/TitlePanel"));
            Label("Header/TitlePanel/Title","RESOURCE EXCHANGE",30);
            Horizontal(header.TitleText.rectTransform,20,16,4,40);
            Label("Header/TitlePanel/Subtitle","Available resources — choose an exchange",22);
            Horizontal(root.Find("Header/TitlePanel/Subtitle") as RectTransform,20,16,43,29);
            string[] resources={"Materials","Oil","Fuel"};
            for(int i=0;i<3;i++) {
                string p="Header/"+resources[i]; Place(p,642+i*174,0,164,78); right.Add((RectTransform)root.Find(p));
                Place(p+"/Icon",8,18,40,40); Place(p+"/Label",52,7,104,25); Label(p+"/Label",resources[i].ToUpperInvariant(),18);
                Place(p+"/Value",52,31,104,40); Label(p+"/Value","0",26);
            }
            Place("Header/CloseButton",1172,0,84,84); right.Add((RectTransform)header.CloseButton.transform);
            Place("RecipeColumn",12,102,400,606);
            Place("RecipeColumn/Heading",16,10,368,40); Label("RecipeColumn/Heading","CHOOSE AN EXCHANGE",26);
            Hide("RecipeColumn/Hint"); Hide("RecipeColumn/ExportTab"); Hide("RecipeColumn/ImportTab");
            var recipeViewport=TopLeft("RecipeViewport",root.Find("RecipeColumn"),12,60,376,534);
            recipeViewport.gameObject.AddComponent<RectMask2D>();
            var content=(RectTransform)cards[0].transform.parent; content.SetParent(recipeViewport,false); TopLeft(content,0,0,376,7*108);
            AddScroll(recipeViewport,content);
            for(int i=0;i<cards.Length;i++) {
                var c=cards[i].transform; TopLeft((RectTransform)c,0,i*108,376,100);
                c.Find("IconPanel").gameObject.SetActive(false);
                TopLeft((RectTransform)c.Find("Title"),14,9,316,38); c.Find("Title").GetComponent<TMP_Text>().fontSize=24;
                TopLeft((RectTransform)c.Find("Input"),14,48,180,38); c.Find("Input").GetComponent<TMP_Text>().fontSize=20;
                TopLeft((RectTransform)c.Find("Output"),198,48,166,38); c.Find("Output").GetComponent<TMP_Text>().fontSize=20;
                c.Find("Duration").gameObject.SetActive(false); c.Find("Reason").gameObject.SetActive(false);
                TopLeft((RectTransform)c.Find("SelectedCheck"),336,12,28,28);
                TopLeft((RectTransform)c.Find("Lock"),336,12,28,28); TopLeft((RectTransform)c.Find("Warning"),302,12,28,28);
                cards[i].gameObject.SetActive(false);
            }
            Place("DetailPanel",424,102,844,362); widths.Add((RectTransform)root.Find("DetailPanel"));
            foreach(string n in new[]{"Thumbnail","SectionTitle","Name","RouteValue","YourResources","DetailsTitle","AmountTrack","RateLabel","RateValue","RequirementsLabel","RequirementsValue"}) {
                var t=root.Find("DetailPanel/"+n); if(t!=null)t.gameObject.SetActive(false);
            }
            var panel=(RectTransform)root.Find("DetailPanel");
            var spend=(RectTransform)panel.Find("Spend"); spend.anchorMin=new Vector2(0,1); spend.anchorMax=new Vector2(.5f,1); spend.pivot=new Vector2(0,1); spend.anchoredPosition=new Vector2(16,-12); spend.sizeDelta=new Vector2(-24,94);
            var receive=(RectTransform)panel.Find("Receive"); receive.anchorMin=new Vector2(.5f,1); receive.anchorMax=new Vector2(1,1); receive.pivot=new Vector2(0,1); receive.anchoredPosition=new Vector2(8,-12); receive.sizeDelta=new Vector2(-24,94);
            Label("DetailPanel/Spend/Label","YOU GIVE",24); Horizontal((RectTransform)spend.Find("Label"),12,12,6,32);
            Label("DetailPanel/Receive/Label","YOU RECEIVE",24); Horizontal((RectTransform)receive.Find("Label"),12,12,6,32);
            Horizontal(detail.Input.rectTransform,12,12,42,44); detail.Input.fontSize=32;
            Horizontal(detail.Output.rectTransform,12,12,42,44); detail.Output.fontSize=32;
            // Existing serialized fields keep their runtime bindings, with just one visible submit control.
            foreach(Transform child in panel) if(child.name.StartsWith("Amount") || child.name.StartsWith("Duration")) child.gameObject.SetActive(false);
            var minus=(RectTransform)detail.Minus.transform; minus.gameObject.SetActive(true); TopLeft(minus,16,118,94,88);
            var plus=(RectTransform)detail.Plus.transform; plus.gameObject.SetActive(true); plus.anchorMin=plus.anchorMax=new Vector2(1,1); plus.pivot=new Vector2(1,1); plus.anchoredPosition=new Vector2(-16,-118); plus.sizeDelta=new Vector2(94,88);
            detail.Amount.transform.parent.gameObject.SetActive(true);
            // Amount is authored inside AmountStepper; move the three live controls out of it.
            minus.SetParent(panel,false); TopLeft(minus,16,118,94,88);
            plus.SetParent(panel,false); plus.anchorMin=plus.anchorMax=new Vector2(1,1); plus.pivot=new Vector2(1,1); plus.anchoredPosition=new Vector2(-16,-118); plus.sizeDelta=new Vector2(94,88);
            detail.Amount.transform.SetParent(panel,false); Horizontal(detail.Amount.rectTransform,126,126,118,80); detail.Amount.fontSize=36;
            var stepper=panel.Find("AmountStepper"); if(stepper!=null)stepper.gameObject.SetActive(false);
            detail.Instruction.gameObject.SetActive(true); Horizontal(detail.Instruction.rectTransform,16,174,207,60); detail.Instruction.fontSize=22; detail.Instruction.textWrappingMode=TextWrappingModes.Normal; detail.Instruction.overflowMode=TextOverflowModes.Overflow;
            detail.Duration.gameObject.SetActive(true); detail.Duration.rectTransform.anchorMin=detail.Duration.rectTransform.anchorMax=new Vector2(1,1); detail.Duration.rectTransform.pivot=new Vector2(1,1); detail.Duration.rectTransform.anchoredPosition=new Vector2(-16,-210); detail.Duration.rectTransform.sizeDelta=new Vector2(144,50); detail.Duration.fontSize=30;
            if(detail.Warning!=null) { detail.Warning.transform.SetParent(detail.Confirm.transform,false); detail.Warning.gameObject.SetActive(false); }
            detail.Confirm.gameObject.SetActive(false);
            footer.CancelButton.gameObject.SetActive(false);
            footer.ConfirmButton.transform.SetParent(panel,false); Horizontal((RectTransform)footer.ConfirmButton.transform,16,16,272,84);
            footer.ConfirmButton.GetComponentInChildren<TMP_Text>().text="START EXCHANGE";
            footer.ConfirmButton.transition=Selectable.Transition.ColorTint;
            var colors=footer.ConfirmButton.colors; colors.disabledColor=new Color(.32f,.32f,.32f,1); footer.ConfirmButton.colors=colors;
            Hide("Footer");
            Place("ExchangeQueuePanel",424,476,844,232); widths.Add((RectTransform)root.Find("ExchangeQueuePanel"));
            foreach(string n in new[]{"Subtitle","CapacityPanel","RushAllButton"}) Hide("ExchangeQueuePanel/"+n);
            Place("ExchangeQueuePanel/Title",16,6,256,40); Label("ExchangeQueuePanel/Title","EXCHANGE PROGRESS",24);
            queue.CapacityText.rectTransform.SetParent(root.Find("ExchangeQueuePanel"),false); TopLeft(queue.CapacityText.rectTransform,280,6,76,40); queue.CapacityText.fontSize=24;
            var clear=(RectTransform)queue.ClearDone.transform; clear.anchorMin=clear.anchorMax=new Vector2(1,1); clear.pivot=new Vector2(1,1); clear.anchoredPosition=new Vector2(-12,-6); clear.sizeDelta=new Vector2(210,84);
            queue.ClearDone.GetComponentInChildren<TMP_Text>().fontSize=22;
            var qViewport=Horizontal("QueueViewport",root.Find("ExchangeQueuePanel"),12,12,98,122); qViewport.gameObject.AddComponent<RectMask2D>();
            queue.Content.SetParent(qViewport,false); Horizontal(queue.Content,0,0,0,4*140); AddScroll(qViewport,queue.Content);
            var empty=Text("EmptyQueue",qViewport,"No exchanges yet. Choose a route above.\nExchanges keep running when you close this screen.",24,medium,TextAlignmentOptions.Center,TextMuted); Stretch(empty.rectTransform); empty.textWrappingMode=TextWrappingModes.Normal;
            for(int i=0;i<queue.Rows.Length;i++) {
                var row=(RectTransform)queue.Rows[i].transform; Horizontal(row,0,0,i*140,130);
                foreach(string n in new[]{"Thumb","Number","Input","Output","Percent","Completed","Warning","RushButton"})row.Find(n).gameObject.SetActive(false);
                Horizontal((RectTransform)row.Find("Name"),14,180,6,40); row.Find("Name").GetComponent<TMP_Text>().fontSize=24;
                Horizontal((RectTransform)row.Find("State"),14,300,46,38); row.Find("State").GetComponent<TMP_Text>().fontSize=22;
                Horizontal((RectTransform)row.Find("ProgressTrack"),14,180,100,16);
                var time=(RectTransform)row.Find("Time"); time.anchorMin=time.anchorMax=new Vector2(1,1); time.pivot=new Vector2(1,1); time.anchoredPosition=new Vector2(-188,-47); time.sizeDelta=new Vector2(100,36); time.GetComponent<TMP_Text>().fontSize=26;
                var cancel=(RectTransform)row.Find("CancelButton"); cancel.anchorMin=cancel.anchorMax=new Vector2(1,1); cancel.pivot=new Vector2(1,1); cancel.anchoredPosition=new Vector2(-12,-24); cancel.sizeDelta=new Vector2(150,82); cancel.GetComponentInChildren<TMP_Text>().text="CANCEL"; cancel.GetComponentInChildren<TMP_Text>().fontSize=24;
                row.gameObject.SetActive(false);
            }
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)) {
                string source=text.text;
                var font=text.font; var alignment=text.alignment; float size=text.fontSize;
                // Keep Arabic's taller line metrics from shrinking phone text to half size.
                // Glyphs fit these spaced rows; their font line boxes may extend beyond them.
                text.enableAutoSizing=true; text.fontSizeMin=size; text.fontSizeMax=size;
                text.overflowMode=TextOverflowModes.Overflow;
                var binding=text.GetComponent<V3LocalizedTextBindingView>() ?? text.gameObject.AddComponent<V3LocalizedTextBindingView>();
                Game.Configs.GameLocalization.TryGetBySource(source, out string key, out _);
                binding.Configure(key,source);
                // OnEnable can localize in the Editor. Save stable English authoring defaults.
                text.font=font; text.fontSharedMaterial=font.material; text.alignment=alignment;
                text.fontSize=size; text.isRightToLeftText=false; text.text=source;
            }
        }

        private static void AddScroll(RectTransform viewport, RectTransform content)
        {
            var hit=viewport.gameObject.AddComponent<UnityEngine.UI.Image>(); hit.color=Color.clear;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content;
            scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=40;
        }

        private static FooterRefs BuildFooter(RectTransform root, ICollection<RectTransform> widths)
        {
            RectTransform rail = TopLeft("Footer", root, 15, 863, 1642, 66);
            Button cancel = GradientButton("CancelButton", rail, 0, 0, 340, 66, RaisedTop, DarkBottom, Border); ButtonLabel(cancel, "CANCEL", 24);
            Button confirm = GradientButton("ConfirmExchangeButton", rail, 350, 0, 1292, 66, GreenTop, GreenBottom, Green); ButtonLabel(confirm, "CONFIRM EXCHANGE", 26);
            widths.Add(confirm.GetComponent<RectTransform>());
            return new FooterRefs(cancel, confirm);
        }

        private static void SeedPreview(ResourceExchangePopupView popup, ResourceExchangeRecipeCardView[] cards, ResourceExchangeQueueItemView[] rows)
        {
            Sprite[] icons = RecipeIcons();
            string[] names = { "FUEL TO MATERIALS", "OIL TO MATERIALS", "FUEL TO OIL", "MATERIALS TO OIL", "OIL TO FUEL", "RECOVERY ROUTE", "SCENARIO ROUTE" };
            string[] inputs = { "100 FUEL", "100 OIL", "100 FUEL", "100 MATERIALS", "100 OIL", "LOCKED", "LOCKED" };
            string[] outputs = { "180 MATERIALS", "300 MATERIALS", "120 OIL", "15 OIL", "33 FUEL", "SCENARIO GATED", "SCENARIO GATED" };
            for (int i = 0; i < cards.Length; i++) cards[i].Bind(names[i], inputs[i], outputs[i], i < 2 ? "00:45" : "01:30", i >= 4 ? "LOCKED" : string.Empty, icons[i], i == 0, i < 4, i >= 4, i >= 4, null, null, null);
            rows[0].Bind("1", "Fuel to Materials", "100 FUEL", "180 MATERIALS", "00:11", "65%", "IN PROGRESS", .65f, icons[0], true, true, false, false);
            rows[1].Bind("2", "Oil to Materials", "100 OIL", "300 MATERIALS", "00:45", "0%", "QUEUED", 0f, icons[1], false, true, false, false);
            rows[2].Bind("3", "Materials to Oil", "100 MATERIALS", "15 OIL", "DONE", "100%", "COMPLETE", 1f, icons[3], false, false, true, false);
            rows[3].Bind("4", "Fuel to Oil", "100 FUEL", "120 OIL", "01:30", "0%", "QUEUED", 0f, icons[2], false, true, false, false);
            popup.ApplyHeader("3/3", "180", "620", "310", "7"); popup.ApplyTabs(true, 3, 3);
            popup.ApplyDetail("Fuel to Materials", "CONVERSION ROUTE", "1 FUEL = 1.8 MATERIALS", "100", "100 FUEL", "180 MATERIALS", "01:30", "1 SLOT REQUIRED", "Adjust the amount, then convert to add this exchange to the logistics queue.", true, false, icons[0]); popup.ApplyQueueControls(true, true);
        }

        private static RectTransform ResourceChip(Transform parent, string name, float x, Sprite icon, string label, string value, Color accent, out TMP_Text valueText)
        {
            RectTransform panel = TopLeft(name, parent, x, 0, 165, 94); Gradient(panel, DarkTop, DarkBottom, Border);
            UnityEngine.UI.Image image = Img("Icon", panel, icon, accent, false); TopLeft(image.rectTransform, 10, 19, 48, 48); image.preserveAspect = true;
            TMP_Text labelText = Text("Label", panel, label, 13, bold, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(labelText.rectTransform, 65, 12, 94, 23);
            valueText = Text("Value", panel, value, 26, bold, TextAlignmentOptions.MidlineLeft, accent); TopLeft(valueText.rectTransform, 65, 34, 94, 39);
            return panel;
        }

        private static void ResourceMini(Transform parent, string name, float x, Sprite sprite, string value, Color accent)
        {
            UnityEngine.UI.Image icon = Img(name + "Icon", parent, sprite, accent, false); TopLeft(icon.rectTransform, x, 40, 40, 40); icon.preserveAspect = true;
            TMP_Text text = Text(name + "Value", parent, value, 22, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(text.rectTransform, x + 48, 39, 105, 42);
        }

        private static TMP_Text DetailMetric(Transform parent, string name, string label, string value, float y, Color accent)
        {
            TMP_Text labelText = Text(name + "Label", parent, label, 13, bold, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(labelText.rectTransform, 23, y, 105, 31);
            TMP_Text valueText = Text(name + "Value", parent, value, 16, bold, TextAlignmentOptions.MidlineLeft, accent); Horizontal(valueText.rectTransform, 135, 20, y, 31); return valueText;
        }

        private static Sprite[] RecipeIcons() => new[] { catalog.FuelIcon, catalog.OilIcon, catalog.FuelIcon, catalog.MaterialsIcon, catalog.OilIcon, catalog.MaterialsIcon, catalog.RushIcon };

        private static UnityEngine.UI.Image StatusSquare(string name, Transform parent, float x, float y, Color color, string glyph)
        {
            UnityEngine.UI.Image image = Img(name, parent, null, color, false); TopLeft(image.rectTransform, x, y, 30, 30);
            if (glyph == "V")
            {
                UnityEngine.UI.Image shortStroke = Img("CheckShort", image.transform, null, TextPrimary, false);
                shortStroke.rectTransform.anchorMin = shortStroke.rectTransform.anchorMax = new Vector2(.5f, .5f);
                shortStroke.rectTransform.sizeDelta = new Vector2(5f, 13f);
                shortStroke.rectTransform.anchoredPosition = new Vector2(-5f, 2f);
                shortStroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 43f);
                UnityEngine.UI.Image longStroke = Img("CheckLong", image.transform, null, TextPrimary, false);
                longStroke.rectTransform.anchorMin = longStroke.rectTransform.anchorMax = new Vector2(.5f, .5f);
                longStroke.rectTransform.sizeDelta = new Vector2(5f, 21f);
                longStroke.rectTransform.anchoredPosition = new Vector2(4f, 0f);
                longStroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -42f);
            }
            else
            {
                TMP_Text text = Text("Glyph", image.transform, glyph, 16, bold, TextAlignmentOptions.Center, TextPrimary);
                Stretch(text.rectTransform);
            }
            return image;
        }

        private static UnityEngine.UI.Image LegacyBindingImage(string name, Transform parent)
        { UnityEngine.UI.Image image = Img(name, parent, null, Color.clear, false); Stretch(image.rectTransform); return image; }

        private static Button GradientButton(string name, Transform parent, float x, float y, float w, float h, Color top, Color bottom, Color border)
        {
            RectTransform rect = TopLeft(name, parent, x, y, w, h); V3GradientGraphic chrome = Gradient(rect, top, bottom, border);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = chrome; button.transition = Selectable.Transition.None; return button;
        }

        private static Button GradientButtonRight(string name, Transform parent, float right, float y, float w, float h, Color top, Color bottom, Color border)
        {
            RectTransform rect = Rect(name, parent, new Vector2(1, 1), new Vector2(1, 1), new Vector2(w, h), new Vector2(-right, -y)); rect.pivot = new Vector2(1, 1);
            V3GradientGraphic chrome = Gradient(rect, top, bottom, border); Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = chrome; button.transition = Selectable.Transition.None; return button;
        }

        private static void ButtonLabel(Button button, string value, float size) { TMP_Text text = Text("Label", button.transform, value, size, bold, TextAlignmentOptions.Center, TextPrimary); Stretch(text.rectTransform); }
        private static void ButtonGlyph(Button button, string glyph, float size) { TMP_Text text = Text("Glyph", button.transform, glyph, size, bold, TextAlignmentOptions.Center, TextPrimary); Stretch(text.rectTransform); }

        private static V3GradientGraphic Gradient(RectTransform rect, Color top, Color bottom, Color border)
        {
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.ConfigureCorners(Color.Lerp(top, Color.white, .055f), top, Color.Lerp(bottom, Color.black, .12f), bottom, border, border.a > .01f ? 3f : 0f); return graphic;
        }

        private static UnityEngine.UI.Image Img(string name, Transform parent, Sprite sprite, Color color, bool raycast)
            => V3UiPrefabFactory.CreateImage(name, parent, sprite, color, raycast, false);

        private static TMP_Text Text(string name, Transform parent, string value, float size, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(200, 40), Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.text = value; text.font = font; text.fontSize = size; text.alignment = alignment; text.color = color;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; return text;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 position) => V3UiPrefabFactory.CreateRect(name, parent, min, max, size, position);
        private static RectTransform TopLeft(string name, Transform parent, float x, float y, float w, float h) { RectTransform rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(w, h), new Vector2(x, -y)); rect.pivot = new Vector2(0, 1); return rect; }
        private static RectTransform Horizontal(string name, Transform parent, float left, float right, float y, float h) { RectTransform rect = Rect(name, parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-(left + right), h), new Vector2(left, -y)); rect.pivot = new Vector2(0, 1); return rect; }
        private static void TopLeft(RectTransform rect, float x, float y, float w, float h) { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, -y); }
        private static void Horizontal(RectTransform rect, float left, float right, float y, float h) { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(-(left + right), h); rect.anchoredPosition = new Vector2(left, -y); }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        { SerializedProperty property = serialized.FindProperty(propertyName) ?? throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName); property.objectReferenceValue = value; }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, UnityEngine.Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ?? throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName); property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static string GetPath(Transform transform) { string path = transform.name; while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; } return path; }

        private readonly struct HeaderRefs
        {
            public readonly TMP_Text TitleText, MaterialsText, OilText, FuelText, RushText; public readonly Button CloseButton;
            public HeaderRefs(TMP_Text title, TMP_Text materials, TMP_Text oil, TMP_Text fuel, TMP_Text rush, Button close) { TitleText = title; MaterialsText = materials; OilText = oil; FuelText = fuel; RushText = rush; CloseButton = close; }
        }

        private readonly struct TabRefs
        {
            public readonly Button ExportButton, ImportButton; public readonly UnityEngine.UI.Image ExportFrame, ImportFrame; public readonly TMP_Text ExportCount, ImportCount;
            public TabRefs(Button export, Button import, UnityEngine.UI.Image exportFrame, UnityEngine.UI.Image importFrame, TMP_Text exportCount, TMP_Text importCount) { ExportButton = export; ImportButton = import; ExportFrame = exportFrame; ImportFrame = importFrame; ExportCount = exportCount; ImportCount = importCount; }
        }

        private readonly struct DetailRefs
        {
            public readonly UnityEngine.UI.Image Thumbnail, Warning; public readonly TMP_Text Name, Route, Rate, Amount, Input, Output, Duration, Requirements, Instruction, ConfirmLabel; public readonly Button Minus, Plus, Confirm;
            public DetailRefs(UnityEngine.UI.Image thumbnail, TMP_Text name, TMP_Text route, TMP_Text rate, TMP_Text amount, TMP_Text input, TMP_Text output, TMP_Text duration, TMP_Text requirements, TMP_Text instruction, UnityEngine.UI.Image warning, Button minus, Button plus, Button confirm, TMP_Text confirmLabel) { Thumbnail = thumbnail; Name = name; Route = route; Rate = rate; Amount = amount; Input = input; Output = output; Duration = duration; Requirements = requirements; Instruction = instruction; Warning = warning; Minus = minus; Plus = plus; Confirm = confirm; ConfirmLabel = confirmLabel; }
        }

        private readonly struct QueueRefs
        {
            public readonly RectTransform Content; public readonly ResourceExchangeQueueItemView[] Rows; public readonly Button RushAll, ClearDone; public readonly TMP_Text CapacityText;
            public QueueRefs(RectTransform content, ResourceExchangeQueueItemView[] rows, Button rushAll, Button clearDone, TMP_Text capacity) { Content = content; Rows = rows; RushAll = rushAll; ClearDone = clearDone; CapacityText = capacity; }
        }

        private readonly struct FooterRefs { public readonly Button CancelButton, ConfirmButton; public FooterRefs(Button cancel, Button confirm) { CancelButton = cancel; ConfirmButton = confirm; } }
    }
}
#endif
