using System.IO;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ResourceExchangePopupPrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab";
        private const string SpriteRoot = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string LightFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset";

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static TMP_FontAsset lightFont;

        [MenuItem("Game/UI/Rebuild Resource Exchange Popup")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            EnsureSpritesImported();
            AssetDatabase.Refresh();
            LoadFonts();
            GameObject prefab = BuildPrefab();
            if (prefab == null)
                throw new System.InvalidOperationException($"Failed to build {PrefabPath}.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ResourceExchangePopupPrefabBuilder] Resource Exchange popup prefab rebuilt.");
        }

        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new System.InvalidOperationException($"Missing Resource Exchange popup prefab at {PrefabPath}.");

            ResourceExchangePopupView view = prefab.GetComponent<ResourceExchangePopupView>();
            if (view == null)
                throw new System.InvalidOperationException("Resource Exchange popup is missing ResourceExchangePopupView.");
            ResourceExchangePopupRuntimeView runtimeView = prefab.GetComponent<ResourceExchangePopupRuntimeView>();
            if (runtimeView == null || runtimeView.View == null)
                throw new System.InvalidOperationException("Resource Exchange popup is missing ResourceExchangePopupRuntimeView.");
            if (view.CloseButton == null || view.ExportTabButton == null || view.ImportTabButton == null)
                throw new System.InvalidOperationException("Resource Exchange popup is missing required header/tab buttons.");
            if (view.ConfirmButton == null || view.AmountDecreaseButton == null || view.AmountIncreaseButton == null)
                throw new System.InvalidOperationException("Resource Exchange popup is missing amount or confirm controls.");
            if (view.RushAllButton == null || view.ClearCompletedButton == null)
                throw new System.InvalidOperationException("Resource Exchange popup is missing queue action controls.");
            if (view.RecipeCardTemplate == null || view.StaticRecipeCards == null || view.StaticRecipeCards.Length < 6)
                throw new System.InvalidOperationException("Resource Exchange popup must expose at least six recipe card views.");
            if (view.QueueRowTemplate == null || view.StaticQueueRows == null || view.StaticQueueRows.Length < 4)
                throw new System.InvalidOperationException("Resource Exchange popup must expose at least four queue rows.");

            Image[] images = prefab.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Sprite sprite = images[i].sprite;
                if (sprite == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(sprite);
                if (!path.StartsWith(SpriteRoot, System.StringComparison.Ordinal))
                    throw new System.InvalidOperationException($"Resource Exchange image {images[i].name} uses non-POP12 sprite {path}.");
            }

            Debug.Log("[ResourceExchangePopupPrefabBuilder] Validation passed.");
        }

        private static GameObject BuildPrefab()
        {
            Sprites sprites = LoadSprites();
            GameObject root = CreateRect("POP12_ResourceExchangePopup", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            try
            {
                root.AddComponent<CanvasGroup>();
                UIPopupMotionView.Ensure(root);
                ResourceExchangePopupView popupView = root.AddComponent<ResourceExchangePopupView>();
                ResourceExchangePopupRuntimeView runtimeView = root.AddComponent<ResourceExchangePopupRuntimeView>();

                Image blocker = root.AddComponent<Image>();
                blocker.color = new Color(0f, 0f, 0f, 0.50f);
                blocker.raycastTarget = true;

                RectTransform panel = CreateRect("ResourceExchangeRoot", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1640f, 916f), Vector2.zero);
                Image backplate = CreateImage("ModalBackplate", panel, sprites.ModalBackplate, Color.white, true, Image.Type.Sliced);
                Stretch(backplate.rectTransform);
                Image frame = CreateImage("PopupOuterFrame", panel, sprites.PopupOuterFrame, Color.white, false, Image.Type.Sliced);
                Stretch(frame.rectTransform);

                HeaderRefs header = CreateHeader(panel, sprites);
                TabRefs tabs = CreateTabs(panel, sprites);
                ResourceExchangeRecipeCardView[] cards = CreateRecipeCards(panel, sprites);
                DetailRefs detail = CreateDetailPanel(panel, sprites);
                QueueRefs queue = CreateQueuePanel(panel, sprites);
                TMP_Text instruction = CreateInstruction(panel, sprites);

                var serialized = new SerializedObject(popupView);
                SetObject(serialized, "popupRoot", root);
                SetObject(serialized, "closeButton", header.CloseButton);
                SetObject(serialized, "titleText", header.TitleText);
                SetObject(serialized, "queueCapacityText", header.QueueCapacityText);
                SetObject(serialized, "creditsText", header.CreditsText);
                SetObject(serialized, "materialsText", header.MaterialsText);
                SetObject(serialized, "oilText", header.OilText);
                SetObject(serialized, "fuelText", header.FuelText);
                SetObject(serialized, "rushTicketsText", header.RushTicketsText);
                SetObject(serialized, "exportTabButton", tabs.ExportButton);
                SetObject(serialized, "importTabButton", tabs.ImportButton);
                SetObject(serialized, "exportTabFrameImage", tabs.ExportFrame);
                SetObject(serialized, "importTabFrameImage", tabs.ImportFrame);
                SetObject(serialized, "exportCountText", tabs.ExportCountText);
                SetObject(serialized, "importCountText", tabs.ImportCountText);
                SetObject(serialized, "selectedTabFrameSprite", sprites.TabSelected);
                SetObject(serialized, "defaultTabFrameSprite", sprites.TabDefault);
                SetObject(serialized, "recipeContentRoot", cards[0].transform.parent as RectTransform);
                SetObject(serialized, "recipeCardTemplate", cards[0]);
                SetObjectArray(serialized, "staticRecipeCards", cards);
                SetObject(serialized, "defaultRecipeCardFrameSprite", sprites.CardDefault);
                SetObject(serialized, "selectedRecipeCardFrameSprite", sprites.CardSelected);
                SetObject(serialized, "lockedRecipeCardFrameSprite", sprites.CardLocked);
                SetObjectArray(serialized, "recipeThumbnailSprites", sprites.Thumbnails);
                SetObject(serialized, "detailThumbnailImage", detail.ThumbnailImage);
                SetObject(serialized, "detailNameText", detail.NameText);
                SetObject(serialized, "detailRouteText", detail.RouteText);
                SetObject(serialized, "detailRateText", detail.RateText);
                SetObject(serialized, "detailAmountText", detail.AmountText);
                SetObject(serialized, "detailInputText", detail.InputText);
                SetObject(serialized, "detailOutputText", detail.OutputText);
                SetObject(serialized, "detailDurationText", detail.DurationText);
                SetObject(serialized, "detailRequirementsText", detail.RequirementsText);
                SetObject(serialized, "detailInstructionText", detail.InstructionText);
                SetObject(serialized, "detailWarningImage", detail.WarningImage);
                SetObject(serialized, "amountDecreaseButton", detail.AmountDecreaseButton);
                SetObject(serialized, "amountIncreaseButton", detail.AmountIncreaseButton);
                SetObject(serialized, "confirmButton", detail.ConfirmButton);
                SetObject(serialized, "confirmButtonText", detail.ConfirmButtonText);
                SetObject(serialized, "queueContentRoot", queue.ContentRoot);
                SetObject(serialized, "queueRowTemplate", queue.Rows[0]);
                SetObjectArray(serialized, "staticQueueRows", queue.Rows);
                SetObject(serialized, "rushAllButton", queue.RushAllButton);
                SetObject(serialized, "clearCompletedButton", queue.ClearCompletedButton);
                SetObject(serialized, "instructionText", instruction);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var runtimeSerialized = new SerializedObject(runtimeView);
                SetObject(runtimeSerialized, "view", popupView);
                runtimeSerialized.ApplyModifiedPropertiesWithoutUndo();

                cards[0].Bind("EXPORT OIL", "100 OIL", "46 CREDITS", "00:30", string.Empty, sprites.Thumbnails[0], true, true, false, false, sprites.CardDefault, sprites.CardSelected, sprites.CardLocked);
                cards[1].Bind("EXPORT MATERIALS", "75 MATERIALS", "90 CREDITS", "00:40", string.Empty, sprites.Thumbnails[1], false, true, false, false, sprites.CardDefault, sprites.CardSelected, sprites.CardLocked);
                cards[2].Bind("EXPORT FUEL", "50 FUEL", "80 CREDITS", "00:35", string.Empty, sprites.Thumbnails[2], false, true, false, false, sprites.CardDefault, sprites.CardSelected, sprites.CardLocked);
                cards[3].Bind("IMPORT MATERIALS", "150 CREDITS", "60 MATERIALS", "00:45", string.Empty, sprites.Thumbnails[3], false, true, false, false, sprites.CardDefault, sprites.CardSelected, sprites.CardLocked);
                cards[4].Bind("IMPORT FUEL", "120 CREDITS", "50 FUEL", "00:30", string.Empty, sprites.Thumbnails[4], false, true, false, false, sprites.CardDefault, sprites.CardSelected, sprites.CardLocked);
                cards[5].Bind("IMPORT OIL", "LOCKED", "SCENARIO GATED", "--:--", "LOCKED", sprites.Thumbnails[5], false, false, true, true, sprites.CardDefault, sprites.CardSelected, sprites.CardLocked);
                queue.Rows[0].Bind("1", "Export Oil", "100 OIL", "46 CREDITS", "00:11", "65%", "IN PROGRESS", 0.65f, sprites.Thumbnails[0], true, true, false, false);
                queue.Rows[1].Bind("2", "Import Fuel", "120 CREDITS", "50 FUEL", "00:30", "0%", "QUEUED", 0f, sprites.Thumbnails[4], false, true, false, false);
                queue.Rows[2].Bind("3", "Export Materials", "75 MATERIALS", "90 CREDITS", "00:40", "0%", "QUEUED", 0f, sprites.Thumbnails[1], false, true, false, false);
                queue.Rows[3].Bind("4", "Import Materials", "150 CREDITS", "60 MATERIALS", "DONE", "100%", "COMPLETE", 1f, sprites.Thumbnails[3], false, false, true, false);
                popupView.ApplyHeader("4/6", "2,400", "180", "620", "310", "7");
                popupView.ApplyTabs(true, 3, 3);
                popupView.ApplyDetail("Export Oil", "EXPORT", "1 OIL -> 0.47 CREDITS", "100", "100 OIL", "46 CREDITS", "00:30", "Requires Oil Pump", "Confirm to start a timed logistics exchange.", true, false, sprites.Thumbnails[0]);
                popupView.ApplyQueueControls(true, true);

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static HeaderRefs CreateHeader(RectTransform panel, Sprites sprites)
        {
            RectTransform header = CreateRect("Header", panel, new Vector2(0.018f, 0.895f), new Vector2(0.982f, 0.985f), Vector2.zero, Vector2.zero);
            Image strip = header.gameObject.AddComponent<Image>();
            strip.sprite = sprites.HeaderStrip;
            ApplySliced(strip);
            strip.raycastTarget = false;

            Image icon = CreateImage("LogisticsIcon", header, sprites.LogisticsTruckIcon, Color.white, false, Image.Type.Simple);
            SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 58f), new Vector2(60f, 0f));
            icon.preserveAspect = true;
            TMP_Text title = CreateText("Title", header, "RESOURCE EXCHANGE", 42f, boldFont, TextAlignmentOptions.Left, GoldText);
            SetRect(title.rectTransform, new Vector2(0f, 0f), new Vector2(0.45f, 1f), Vector2.zero, new Vector2(112f, 0f), new Vector2(0f, 0f));

            TMP_Text queue = CreateChip(header, "QueueCapacity", "4/6", sprites.CounterChip, new Vector2(0.46f, 0.18f), new Vector2(0.535f, 0.82f));
            TMP_Text credits = CreateResourceChip(header, "Credits", sprites.CreditsIcon, "2,400", new Vector2(0.55f, 0.18f), new Vector2(0.64f, 0.82f));
            TMP_Text materials = CreateResourceChip(header, "Materials", sprites.MaterialsIcon, "180", new Vector2(0.645f, 0.18f), new Vector2(0.735f, 0.82f));
            TMP_Text oil = CreateResourceChip(header, "Oil", sprites.OilIcon, "620", new Vector2(0.74f, 0.18f), new Vector2(0.81f, 0.82f));
            TMP_Text fuel = CreateResourceChip(header, "Fuel", sprites.FuelIcon, "310", new Vector2(0.815f, 0.18f), new Vector2(0.885f, 0.82f));
            TMP_Text rush = CreateResourceChip(header, "RushTickets", sprites.RushTicketIcon, "7", new Vector2(0.89f, 0.18f), new Vector2(0.945f, 0.82f));
            Button close = CreateIconButton("CloseButton", header, sprites.CloseButtonFrame, sprites.CloseIcon, new Vector2(0.955f, 0.06f), new Vector2(0.995f, 0.94f));
            return new HeaderRefs(title, queue, credits, materials, oil, fuel, rush, close);
        }

        private static TabRefs CreateTabs(RectTransform panel, Sprites sprites)
        {
            Button export = CreateTextButton("ExportTab", panel, "EXPORT", sprites.TabSelected, null, new Vector2(0.025f, 0.825f), new Vector2(0.235f, 0.885f), 26f, out TMP_Text exportLabel);
            Button import = CreateTextButton("ImportTab", panel, "IMPORT", sprites.TabDefault, null, new Vector2(0.25f, 0.825f), new Vector2(0.46f, 0.885f), 26f, out TMP_Text importLabel);
            TMP_Text exportCount = CreateText("Count", export.transform, "3", 16f, mediumFont, TextAlignmentOptions.Center, PaleText);
            SetRect(exportCount.rectTransform, new Vector2(0.82f, 0.15f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);
            TMP_Text importCount = CreateText("Count", import.transform, "3", 16f, mediumFont, TextAlignmentOptions.Center, PaleText);
            SetRect(importCount.rectTransform, new Vector2(0.82f, 0.15f), new Vector2(0.96f, 0.85f), Vector2.zero, Vector2.zero);
            return new TabRefs(export, import, export.GetComponent<Image>(), import.GetComponent<Image>(), exportCount, importCount);
        }

        private static ResourceExchangeRecipeCardView[] CreateRecipeCards(RectTransform panel, Sprites sprites)
        {
            RectTransform content = CreateRect("RecipeCards", panel, new Vector2(0.025f, 0.135f), new Vector2(0.60f, 0.805f), Vector2.zero, Vector2.zero);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(290f, 205f);
            grid.spacing = new Vector2(18f, 18f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(0, 0, 0, 0);

            ResourceExchangeRecipeCardView[] cards = new ResourceExchangeRecipeCardView[6];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = CreateRecipeCard($"RecipeCard{i + 1}", content, sprites, i);
            return cards;
        }

        private static ResourceExchangeRecipeCardView CreateRecipeCard(string name, Transform parent, Sprites sprites, int thumbnailIndex)
        {
            GameObject root = CreateUiObject(name, parent);
            Image frame = root.AddComponent<Image>();
            frame.sprite = thumbnailIndex == 0 ? sprites.CardSelected : thumbnailIndex == 5 ? sprites.CardLocked : sprites.CardDefault;
            ApplySliced(frame);
            frame.raycastTarget = true;
            Button button = root.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;

            Image thumbnail = CreateImage("Thumbnail", root.transform, sprites.Thumbnails[Mathf.Clamp(thumbnailIndex, 0, sprites.Thumbnails.Length - 1)], Color.white, false, Image.Type.Simple);
            SetRect(thumbnail.rectTransform, new Vector2(0.06f, 0.37f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);
            thumbnail.preserveAspect = false;

            TMP_Text title = CreateText("Title", root.transform, "EXPORT OIL", 22f, boldFont, TextAlignmentOptions.Center, PaleText);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
            TMP_Text input = CreateText("Input", root.transform, "100 OIL", 16f, mediumFont, TextAlignmentOptions.Left, PaleText);
            SetRect(input.rectTransform, new Vector2(0.08f, 0.19f), new Vector2(0.48f, 0.35f), Vector2.zero, Vector2.zero);
            TMP_Text output = CreateText("Output", root.transform, "46 CREDITS", 16f, mediumFont, TextAlignmentOptions.Left, GoldText);
            SetRect(output.rectTransform, new Vector2(0.50f, 0.19f), new Vector2(0.92f, 0.35f), Vector2.zero, Vector2.zero);
            TMP_Text duration = CreateText("Duration", root.transform, "00:30", 15f, mediumFont, TextAlignmentOptions.Left, PaleText);
            SetRect(duration.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.44f, 0.18f), Vector2.zero, Vector2.zero);
            TMP_Text reason = CreateText("Reason", root.transform, string.Empty, 13f, boldFont, TextAlignmentOptions.Right, WarningText);
            SetRect(reason.rectTransform, new Vector2(0.39f, 0.05f), new Vector2(0.90f, 0.18f), Vector2.zero, Vector2.zero);

            Image selected = CreateImage("SelectedCheck", root.transform, sprites.CheckBadgeIcon, Color.white, false, Image.Type.Simple);
            SetRect(selected.rectTransform, new Vector2(0.82f, 0.78f), new Vector2(0.99f, 1.02f), Vector2.zero, Vector2.zero);
            selected.preserveAspect = true;
            selected.gameObject.SetActive(thumbnailIndex == 0);
            Image locked = CreateImage("Lock", root.transform, sprites.LockBadgeIcon, Color.white, false, Image.Type.Simple);
            SetRect(locked.rectTransform, new Vector2(0.82f, 0.76f), new Vector2(0.99f, 1.00f), Vector2.zero, Vector2.zero);
            locked.preserveAspect = true;
            locked.gameObject.SetActive(thumbnailIndex == 5);
            Image warning = CreateImage("Warning", root.transform, sprites.WarningIcon, Color.white, false, Image.Type.Simple);
            SetRect(warning.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.18f, 0.20f), Vector2.zero, Vector2.zero);
            warning.preserveAspect = true;
            warning.gameObject.SetActive(thumbnailIndex == 5);

            GameObject disabled = CreateImage("DisabledOverlay", root.transform, null, new Color(0f, 0f, 0f, 0.48f), false, Image.Type.Simple).gameObject;
            Stretch(disabled.GetComponent<RectTransform>());
            disabled.transform.SetSiblingIndex(1);
            disabled.SetActive(thumbnailIndex == 5);

            ResourceExchangeRecipeCardView view = root.AddComponent<ResourceExchangeRecipeCardView>();
            var serialized = new SerializedObject(view);
            SetObject(serialized, "selectionButton", button);
            SetObject(serialized, "frameImage", frame);
            SetObject(serialized, "thumbnailImage", thumbnail);
            SetObject(serialized, "selectedCheckImage", selected);
            SetObject(serialized, "lockImage", locked);
            SetObject(serialized, "warningImage", warning);
            SetObject(serialized, "disabledOverlay", disabled);
            SetObject(serialized, "titleText", title);
            SetObject(serialized, "inputText", input);
            SetObject(serialized, "outputText", output);
            SetObject(serialized, "durationText", duration);
            SetObject(serialized, "reasonText", reason);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static DetailRefs CreateDetailPanel(RectTransform panel, Sprites sprites)
        {
            RectTransform detail = CreateRect("DetailPanel", panel, new Vector2(0.625f, 0.40f), new Vector2(0.975f, 0.875f), Vector2.zero, Vector2.zero);
            Image frame = detail.gameObject.AddComponent<Image>();
            frame.sprite = sprites.DetailPanelFrame;
            ApplySliced(frame);
            frame.raycastTarget = true;

            Image thumbnail = CreateImage("Thumbnail", detail, sprites.Thumbnails[0], Color.white, false, Image.Type.Simple);
            SetRect(thumbnail.rectTransform, new Vector2(0.035f, 0.50f), new Vector2(0.47f, 0.87f), Vector2.zero, Vector2.zero);
            TMP_Text name = CreateText("Name", detail, "EXPORT OIL", 28f, boldFont, TextAlignmentOptions.Left, GoldText);
            SetRect(name.rectTransform, new Vector2(0.50f, 0.82f), new Vector2(0.96f, 0.93f), Vector2.zero, Vector2.zero);
            TMP_Text route = DetailLine("Route", detail, "ROUTE", "EXPORT", 0.73f);
            TMP_Text rate = DetailLine("Rate", detail, "RATE", "1 OIL -> 0.47 CREDITS", 0.64f);
            TMP_Text input = DetailLine("Input", detail, "INPUT", "100 OIL", 0.55f);
            TMP_Text output = DetailLine("Output", detail, "OUTPUT", "46 CREDITS", 0.46f);
            TMP_Text duration = DetailLine("Duration", detail, "TIME", "00:30", 0.37f);
            TMP_Text requirements = DetailLine("Requirements", detail, "REQUIRES", "Oil Pump", 0.28f);

            RectTransform amountFrame = CreateRect("AmountStepper", detail, new Vector2(0.05f, 0.18f), new Vector2(0.50f, 0.29f), Vector2.zero, Vector2.zero);
            Image amountImage = amountFrame.gameObject.AddComponent<Image>();
            amountImage.sprite = sprites.AmountValueFrame;
            ApplySliced(amountImage);
            Button minus = CreateIconButton("AmountMinus", amountFrame, sprites.SmallMinusFrame, sprites.MinusIcon, new Vector2(0.02f, 0.10f), new Vector2(0.20f, 0.90f));
            Button plus = CreateIconButton("AmountPlus", amountFrame, sprites.SmallPlusFrame, sprites.PlusIcon, new Vector2(0.80f, 0.10f), new Vector2(0.98f, 0.90f));
            TMP_Text amount = CreateText("Amount", amountFrame, "100", 24f, boldFont, TextAlignmentOptions.Center, PaleText);
            SetRect(amount.rectTransform, new Vector2(0.22f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero);

            Button confirm = CreateTextButton("ConfirmButton", detail, "CONFIRM", sprites.PrimaryButtonFrame, sprites.CheckBadgeIcon, new Vector2(0.52f, 0.16f), new Vector2(0.96f, 0.31f), 25f, out TMP_Text confirmLabel);
            TMP_Text instruction = CreateText("Instruction", detail, "Confirm to start a timed logistics exchange.", 16f, mediumFont, TextAlignmentOptions.Left, PaleText);
            SetRect(instruction.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.94f, 0.14f), Vector2.zero, Vector2.zero);
            Image warning = CreateImage("Warning", detail, sprites.WarningIcon, Color.white, false, Image.Type.Simple);
            SetRect(warning.rectTransform, new Vector2(0.90f, 0.04f), new Vector2(0.97f, 0.14f), Vector2.zero, Vector2.zero);
            warning.preserveAspect = true;
            warning.gameObject.SetActive(false);

            return new DetailRefs(thumbnail, name, route, rate, amount, input, output, duration, requirements, instruction, warning, minus, plus, confirm, confirmLabel);
        }

        private static TMP_Text DetailLine(string name, Transform parent, string label, string value, float top)
        {
            TMP_Text labelText = CreateText($"{name}Label", parent, label, 14f, mediumFont, TextAlignmentOptions.Left, MutedText);
            SetRect(labelText.rectTransform, new Vector2(0.50f, top), new Vector2(0.70f, top + 0.08f), Vector2.zero, Vector2.zero);
            TMP_Text valueText = CreateText($"{name}Value", parent, value, 18f, mediumFont, TextAlignmentOptions.Left, PaleText);
            SetRect(valueText.rectTransform, new Vector2(0.70f, top), new Vector2(0.96f, top + 0.08f), Vector2.zero, Vector2.zero);
            return valueText;
        }

        private static QueueRefs CreateQueuePanel(RectTransform panel, Sprites sprites)
        {
            RectTransform queue = CreateRect("ExchangeQueuePanel", panel, new Vector2(0.625f, 0.105f), new Vector2(0.975f, 0.38f), Vector2.zero, Vector2.zero);
            Image frame = queue.gameObject.AddComponent<Image>();
            frame.sprite = sprites.QueuePanelFrame;
            ApplySliced(frame);
            frame.raycastTarget = true;
            TMP_Text title = CreateText("Title", queue, "EXCHANGE QUEUE", 24f, boldFont, TextAlignmentOptions.Left, PaleText);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.82f), new Vector2(0.54f, 0.97f), Vector2.zero, Vector2.zero);
            Image info = CreateImage("InfoIcon", queue, sprites.InfoIcon, Color.white, false, Image.Type.Simple);
            SetRect(info.rectTransform, new Vector2(0.91f, 0.83f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            info.preserveAspect = true;

            RectTransform content = CreateRect("Rows", queue, new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 4f;
            ResourceExchangeQueueItemView[] rows = new ResourceExchangeQueueItemView[4];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = CreateQueueRow($"QueueRow{i + 1}", content, sprites, i);

            Button rush = CreateTextButton("RushAllButton", queue, "RUSH ALL", sprites.SecondaryButtonFrame, sprites.RushLightningIcon, new Vector2(0.04f, 0.04f), new Vector2(0.47f, 0.20f), 18f, out _);
            Button clear = CreateTextButton("ClearCompletedButton", queue, "CLEAR DONE", sprites.SecondaryButtonFrame, sprites.CancelIcon, new Vector2(0.53f, 0.04f), new Vector2(0.96f, 0.20f), 18f, out _);
            return new QueueRefs(content, rows, rush, clear);
        }

        private static ResourceExchangeQueueItemView CreateQueueRow(string name, Transform parent, Sprites sprites, int rowIndex)
        {
            GameObject root = CreateImage(name, parent, sprites.QueueRowFrame, Color.white, true, Image.Type.Sliced).gameObject;
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 40f;
            Image thumbnail = CreateImage("Thumb", root.transform, sprites.Thumbnails[Mathf.Clamp(rowIndex, 0, sprites.Thumbnails.Length - 1)], Color.white, false, Image.Type.Simple);
            SetRect(thumbnail.rectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.14f, 0.88f), Vector2.zero, Vector2.zero);
            TMP_Text number = CreateText("Number", root.transform, (rowIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), 16f, boldFont, TextAlignmentOptions.Center, GoldText);
            SetRect(number.rectTransform, new Vector2(0.005f, 0.05f), new Vector2(0.055f, 0.95f), Vector2.zero, Vector2.zero);
            TMP_Text displayName = CreateText("Name", root.transform, "Export Oil", 16f, mediumFont, TextAlignmentOptions.Left, PaleText);
            SetRect(displayName.rectTransform, new Vector2(0.15f, 0.42f), new Vector2(0.42f, 0.95f), Vector2.zero, Vector2.zero);
            TMP_Text input = CreateText("Input", root.transform, "100 OIL", 12f, lightFont, TextAlignmentOptions.Left, MutedText);
            SetRect(input.rectTransform, new Vector2(0.15f, 0.05f), new Vector2(0.31f, 0.45f), Vector2.zero, Vector2.zero);
            TMP_Text output = CreateText("Output", root.transform, "46 CREDITS", 12f, lightFont, TextAlignmentOptions.Left, GoldText);
            SetRect(output.rectTransform, new Vector2(0.31f, 0.05f), new Vector2(0.50f, 0.45f), Vector2.zero, Vector2.zero);
            Image track = CreateImage("ProgressTrack", root.transform, sprites.ProgressTrackFrame, Color.white, false, Image.Type.Sliced);
            SetRect(track.rectTransform, new Vector2(0.45f, 0.20f), new Vector2(0.70f, 0.52f), Vector2.zero, Vector2.zero);
            Image fill = CreateImage("ProgressFill", track.transform, sprites.ProgressFill, Color.white, false, Image.Type.Filled);
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = rowIndex == 0 ? 0.65f : rowIndex == 3 ? 1f : 0f;
            TMP_Text percent = CreateText("Percent", root.transform, rowIndex == 0 ? "65%" : rowIndex == 3 ? "100%" : "0%", 12f, mediumFont, TextAlignmentOptions.Left, PaleText);
            SetRect(percent.rectTransform, new Vector2(0.71f, 0.18f), new Vector2(0.78f, 0.55f), Vector2.zero, Vector2.zero);
            TMP_Text time = CreateText("Time", root.transform, "00:11", 15f, mediumFont, TextAlignmentOptions.Right, PaleText);
            SetRect(time.rectTransform, new Vector2(0.76f, 0.44f), new Vector2(0.88f, 0.92f), Vector2.zero, Vector2.zero);
            TMP_Text state = CreateText("State", root.transform, "IN PROGRESS", 11f, mediumFont, TextAlignmentOptions.Right, MutedText);
            SetRect(state.rectTransform, new Vector2(0.70f, 0.04f), new Vector2(0.88f, 0.42f), Vector2.zero, Vector2.zero);
            Button rush = CreateIconButton("RushButton", root.transform, sprites.SmallCounterChip, sprites.RushLightningIcon, new Vector2(0.89f, 0.11f), new Vector2(0.94f, 0.89f));
            Button cancel = CreateIconButton("CancelButton", root.transform, sprites.SmallCounterChip, sprites.CancelIcon, new Vector2(0.945f, 0.11f), new Vector2(0.995f, 0.89f));
            Image completed = CreateImage("Completed", root.transform, sprites.CompletedIcon, Color.white, false, Image.Type.Simple);
            SetRect(completed.rectTransform, new Vector2(0.89f, 0.11f), new Vector2(0.94f, 0.89f), Vector2.zero, Vector2.zero);
            completed.preserveAspect = true;
            completed.gameObject.SetActive(rowIndex == 3);
            Image warning = CreateImage("Warning", root.transform, sprites.WarningIcon, Color.white, false, Image.Type.Simple);
            SetRect(warning.rectTransform, new Vector2(0.89f, 0.11f), new Vector2(0.94f, 0.89f), Vector2.zero, Vector2.zero);
            warning.preserveAspect = true;
            warning.gameObject.SetActive(false);

            ResourceExchangeQueueItemView view = root.AddComponent<ResourceExchangeQueueItemView>();
            var serialized = new SerializedObject(view);
            SetObject(serialized, "rushButton", rush);
            SetObject(serialized, "cancelButton", cancel);
            SetObject(serialized, "thumbnailImage", thumbnail);
            SetObject(serialized, "progressFillImage", fill);
            SetObject(serialized, "completedImage", completed);
            SetObject(serialized, "warningImage", warning);
            SetObject(serialized, "numberText", number);
            SetObject(serialized, "nameText", displayName);
            SetObject(serialized, "inputText", input);
            SetObject(serialized, "outputText", output);
            SetObject(serialized, "timeText", time);
            SetObject(serialized, "percentText", percent);
            SetObject(serialized, "stateText", state);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static TMP_Text CreateInstruction(RectTransform panel, Sprites sprites)
        {
            Image rail = CreateImage("InstructionRail", panel, sprites.InstructionRailFrame, Color.white, false, Image.Type.Sliced);
            SetRect(rail.rectTransform, new Vector2(0.025f, 0.035f), new Vector2(0.60f, 0.105f), Vector2.zero, Vector2.zero);
            Image info = CreateImage("InfoIcon", rail.transform, sprites.InfoIcon, Color.white, false, Image.Type.Simple);
            SetRect(info.rectTransform, new Vector2(0.025f, 0.18f), new Vector2(0.08f, 0.82f), Vector2.zero, Vector2.zero);
            TMP_Text instruction = CreateText("Instruction", rail.transform, "Select route, adjust amount, then confirm exchange.", 19f, mediumFont, TextAlignmentOptions.Left, PaleText);
            SetRect(instruction.rectTransform, new Vector2(0.09f, 0.10f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero);
            return instruction;
        }

        private static TMP_Text CreateChip(Transform parent, string name, string value, Sprite frameSprite, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image chip = CreateImage(name, parent, frameSprite, Color.white, false, Image.Type.Sliced);
            SetRect(chip.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            TMP_Text text = CreateText("Value", chip.transform, value, 16f, mediumFont, TextAlignmentOptions.Center, PaleText);
            Stretch(text.rectTransform);
            return text;
        }

        private static TMP_Text CreateResourceChip(Transform parent, string name, Sprite iconSprite, string value, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image chip = CreateImage(name, parent, null, new Color(0f, 0f, 0f, 0f), false, Image.Type.Simple);
            SetRect(chip.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image icon = CreateImage("Icon", chip.transform, iconSprite, Color.white, false, Image.Type.Simple);
            SetRect(icon.rectTransform, new Vector2(0f, 0.16f), new Vector2(0.34f, 0.84f), Vector2.zero, Vector2.zero);
            icon.preserveAspect = true;
            TMP_Text text = CreateText("Value", chip.transform, value, 16f, mediumFont, TextAlignmentOptions.Left, PaleText);
            SetRect(text.rectTransform, new Vector2(0.36f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            return text;
        }

        private static Button CreateTextButton(string name, Transform parent, string label, Sprite frameSprite, Sprite iconSprite, Vector2 anchorMin, Vector2 anchorMax, float fontSize, out TMP_Text labelText)
        {
            Image image = CreateImage(name, parent, frameSprite, Color.white, true, Image.Type.Sliced);
            SetRect(image.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (iconSprite != null)
            {
                Image icon = CreateImage("Icon", image.transform, iconSprite, Color.white, false, Image.Type.Simple);
                SetRect(icon.rectTransform, new Vector2(0.06f, 0.20f), new Vector2(0.22f, 0.80f), Vector2.zero, Vector2.zero);
                icon.preserveAspect = true;
            }

            labelText = CreateText("Label", image.transform, label, fontSize, boldFont, TextAlignmentOptions.Center, PaleText);
            SetRect(labelText.rectTransform, iconSprite != null ? new Vector2(0.22f, 0f) : Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Button CreateIconButton(string name, Transform parent, Sprite frameSprite, Sprite iconSprite, Vector2 anchorMin, Vector2 anchorMax)
        {
            Button button = CreateTextButton(name, parent, string.Empty, frameSprite, null, anchorMin, anchorMax, 1f, out TMP_Text label);
            Object.DestroyImmediate(label.gameObject);
            if (iconSprite != null)
            {
                Image icon = CreateImage("Icon", button.transform, iconSprite, Color.white, false, Image.Type.Simple);
                SetRect(icon.rectTransform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
                icon.preserveAspect = true;
            }

            return button;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float size, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 32f), Vector2.zero);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.fontSize = size;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(9f, size * 0.60f);
            tmp.fontSizeMax = size;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget, Image.Type type)
        {
            GameObject obj = CreateUiObject(name, parent);
            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.type = sprite != null ? type : Image.Type.Simple;
            image.preserveAspect = false;
            if (sprite != null && type == Image.Type.Sliced)
                ApplySliced(image);
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject obj = CreateUiObject(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, sizeDelta, anchoredPosition);
            return rect;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new(name, typeof(RectTransform));
            if (parent != null)
                obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return obj;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.offsetMin = sizeDelta == Vector2.zero && anchoredPosition == Vector2.zero ? Vector2.zero : rect.offsetMin;
            rect.offsetMax = sizeDelta == Vector2.zero && anchoredPosition == Vector2.zero ? Vector2.zero : rect.offsetMax;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Vector2 pivot)
        {
            SetRect(rect, anchorMin, anchorMax, sizeDelta, anchoredPosition);
            rect.pivot = pivot;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void ApplySliced(Image image)
        {
            if (image == null || image.sprite == null)
                return;

            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 2f;
        }

        private static void LoadFonts()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            lightFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LightFontPath);
        }

        private static Sprite LoadSprite(string fileName)
        {
            string path = SpriteRoot + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[ResourceExchangePopupPrefabBuilder] Missing sprite {path}");
            return sprite;
        }

        private static Sprites LoadSprites()
        {
            return new Sprites
            {
                PopupOuterFrame = LoadSprite("pop12_chrome_01_popup_outer_frame.png"),
                ModalBackplate = LoadSprite("pop12_chrome_02_modal_backplate_fill.png"),
                DetailPanelFrame = LoadSprite("pop12_chrome_03_detail_panel_frame.png"),
                HeaderStrip = LoadSprite("pop12_chrome_04_header_title_strip_frame.png"),
                CloseButtonFrame = LoadSprite("pop12_chrome_05_close_square_button_frame.png"),
                TabSelected = LoadSprite("pop12_chrome_06_tab_selected_gold_frame.png"),
                TabDefault = LoadSprite("pop12_chrome_07_tab_default_dark_frame.png"),
                CardSelected = LoadSprite("pop12_chrome_08_recipe_card_selected_frame.png"),
                CardDefault = LoadSprite("pop12_chrome_09_recipe_card_default_frame.png"),
                CardLocked = LoadSprite("pop12_chrome_10_recipe_card_locked_frame.png"),
                QueuePanelFrame = LoadSprite("pop12_chrome_11_queue_panel_frame.png"),
                InstructionRailFrame = LoadSprite("pop12_chrome_12_bottom_instruction_rail_frame.png"),
                PrimaryButtonFrame = LoadSprite("pop12_chrome_13_primary_gold_button_frame.png"),
                SecondaryButtonFrame = LoadSprite("pop12_chrome_14_secondary_dark_button_frame.png"),
                SmallPlusFrame = LoadSprite("pop12_chrome_15_small_plus_button_frame.png"),
                SmallMinusFrame = LoadSprite("pop12_chrome_16_small_minus_button_frame.png"),
                QueueRowFrame = LoadSprite("pop12_chrome_17_queue_row_frame.png"),
                ProgressTrackFrame = LoadSprite("pop12_chrome_18_progress_track_frame.png"),
                AmountValueFrame = LoadSprite("pop12_chrome_19_amount_value_frame.png"),
                ProgressFill = LoadSprite("pop12_chrome_20_progress_fill_blue_segment.png"),
                SmallCounterChip = LoadSprite("pop12_chrome_22_small_counter_chip_frame.png"),
                LogisticsTruckIcon = LoadSprite("pop12_icon_01_logistics_exchange_truck.png"),
                CloseIcon = LoadSprite("pop12_icon_02_close_x.png"),
                CreditsIcon = LoadSprite("pop12_icon_03_credits_star_coin.png"),
                MaterialsIcon = LoadSprite("pop12_icon_04_materials_crate.png"),
                OilIcon = LoadSprite("pop12_icon_05_oil_droplet.png"),
                FuelIcon = LoadSprite("pop12_icon_06_fuel_jerrycan.png"),
                RushTicketIcon = LoadSprite("pop12_icon_07_rush_ticket.png"),
                RushLightningIcon = LoadSprite("pop12_icon_08_rush_lightning.png"),
                TimerIcon = LoadSprite("pop12_icon_09_timer_clock.png"),
                InfoIcon = LoadSprite("pop12_icon_10_info_circle.png"),
                CheckBadgeIcon = LoadSprite("pop12_icon_11_checkmark_badge.png"),
                LockBadgeIcon = LoadSprite("pop12_icon_12_lock_badge.png"),
                WarningIcon = LoadSprite("pop12_icon_13_warning_triangle.png"),
                CancelIcon = LoadSprite("pop12_icon_14_cancel_x_small.png"),
                PlusIcon = LoadSprite("pop12_icon_16_plus.png"),
                MinusIcon = LoadSprite("pop12_icon_17_minus.png"),
                CompletedIcon = LoadSprite("pop12_icon_23_completed_check_square.png"),
                CounterChip = LoadSprite("pop12_icon_24_queued_number_chip_outline.png"),
                Thumbnails = new[]
                {
                    LoadSprite("pop12_content_01_export_oil_thumbnail.png"),
                    LoadSprite("pop12_content_02_export_materials_thumbnail.png"),
                    LoadSprite("pop12_content_03_export_fuel_thumbnail.png"),
                    LoadSprite("pop12_content_04_import_materials_thumbnail.png"),
                    LoadSprite("pop12_content_05_import_fuel_thumbnail.png"),
                    LoadSprite("pop12_content_06_import_oil_locked_thumbnail.png")
                }
            };
        }

        private static void EnsureSpritesImported()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteRoot.TrimEnd('/') });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               importer.mipmapEnabled ||
                               !importer.alphaIsTransparency;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                if (changed)
                    importer.SaveAndReimport();
            }
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new System.InvalidOperationException($"Missing serialized property {propertyName} on {serializedObject.targetObject}.");

            property.objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedObject serializedObject, string propertyName, Object[] values)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new System.InvalidOperationException($"Missing serialized property {propertyName} on {serializedObject.targetObject}.");

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Color GoldText => new(0.96f, 0.76f, 0.30f, 1f);
        private static Color PaleText => new(0.88f, 0.84f, 0.76f, 1f);
        private static Color MutedText => new(0.62f, 0.58f, 0.50f, 1f);
        private static Color WarningText => new(1f, 0.28f, 0.18f, 1f);

        private readonly struct HeaderRefs
        {
            public readonly TMP_Text TitleText;
            public readonly TMP_Text QueueCapacityText;
            public readonly TMP_Text CreditsText;
            public readonly TMP_Text MaterialsText;
            public readonly TMP_Text OilText;
            public readonly TMP_Text FuelText;
            public readonly TMP_Text RushTicketsText;
            public readonly Button CloseButton;

            public HeaderRefs(TMP_Text titleText, TMP_Text queueCapacityText, TMP_Text creditsText, TMP_Text materialsText, TMP_Text oilText, TMP_Text fuelText, TMP_Text rushTicketsText, Button closeButton)
            {
                TitleText = titleText;
                QueueCapacityText = queueCapacityText;
                CreditsText = creditsText;
                MaterialsText = materialsText;
                OilText = oilText;
                FuelText = fuelText;
                RushTicketsText = rushTicketsText;
                CloseButton = closeButton;
            }
        }

        private readonly struct TabRefs
        {
            public readonly Button ExportButton;
            public readonly Button ImportButton;
            public readonly Image ExportFrame;
            public readonly Image ImportFrame;
            public readonly TMP_Text ExportCountText;
            public readonly TMP_Text ImportCountText;

            public TabRefs(Button exportButton, Button importButton, Image exportFrame, Image importFrame, TMP_Text exportCountText, TMP_Text importCountText)
            {
                ExportButton = exportButton;
                ImportButton = importButton;
                ExportFrame = exportFrame;
                ImportFrame = importFrame;
                ExportCountText = exportCountText;
                ImportCountText = importCountText;
            }
        }

        private readonly struct DetailRefs
        {
            public readonly Image ThumbnailImage;
            public readonly TMP_Text NameText;
            public readonly TMP_Text RouteText;
            public readonly TMP_Text RateText;
            public readonly TMP_Text AmountText;
            public readonly TMP_Text InputText;
            public readonly TMP_Text OutputText;
            public readonly TMP_Text DurationText;
            public readonly TMP_Text RequirementsText;
            public readonly TMP_Text InstructionText;
            public readonly Image WarningImage;
            public readonly Button AmountDecreaseButton;
            public readonly Button AmountIncreaseButton;
            public readonly Button ConfirmButton;
            public readonly TMP_Text ConfirmButtonText;

            public DetailRefs(Image thumbnailImage, TMP_Text nameText, TMP_Text routeText, TMP_Text rateText, TMP_Text amountText, TMP_Text inputText, TMP_Text outputText, TMP_Text durationText, TMP_Text requirementsText, TMP_Text instructionText, Image warningImage, Button amountDecreaseButton, Button amountIncreaseButton, Button confirmButton, TMP_Text confirmButtonText)
            {
                ThumbnailImage = thumbnailImage;
                NameText = nameText;
                RouteText = routeText;
                RateText = rateText;
                AmountText = amountText;
                InputText = inputText;
                OutputText = outputText;
                DurationText = durationText;
                RequirementsText = requirementsText;
                InstructionText = instructionText;
                WarningImage = warningImage;
                AmountDecreaseButton = amountDecreaseButton;
                AmountIncreaseButton = amountIncreaseButton;
                ConfirmButton = confirmButton;
                ConfirmButtonText = confirmButtonText;
            }
        }

        private readonly struct QueueRefs
        {
            public readonly RectTransform ContentRoot;
            public readonly ResourceExchangeQueueItemView[] Rows;
            public readonly Button RushAllButton;
            public readonly Button ClearCompletedButton;

            public QueueRefs(RectTransform contentRoot, ResourceExchangeQueueItemView[] rows, Button rushAllButton, Button clearCompletedButton)
            {
                ContentRoot = contentRoot;
                Rows = rows;
                RushAllButton = rushAllButton;
                ClearCompletedButton = clearCompletedButton;
            }
        }

        private sealed class Sprites
        {
            public Sprite PopupOuterFrame;
            public Sprite ModalBackplate;
            public Sprite DetailPanelFrame;
            public Sprite HeaderStrip;
            public Sprite CloseButtonFrame;
            public Sprite TabSelected;
            public Sprite TabDefault;
            public Sprite CardSelected;
            public Sprite CardDefault;
            public Sprite CardLocked;
            public Sprite QueuePanelFrame;
            public Sprite InstructionRailFrame;
            public Sprite PrimaryButtonFrame;
            public Sprite SecondaryButtonFrame;
            public Sprite SmallPlusFrame;
            public Sprite SmallMinusFrame;
            public Sprite QueueRowFrame;
            public Sprite ProgressTrackFrame;
            public Sprite AmountValueFrame;
            public Sprite ProgressFill;
            public Sprite SmallCounterChip;
            public Sprite LogisticsTruckIcon;
            public Sprite CloseIcon;
            public Sprite CreditsIcon;
            public Sprite MaterialsIcon;
            public Sprite OilIcon;
            public Sprite FuelIcon;
            public Sprite RushTicketIcon;
            public Sprite RushLightningIcon;
            public Sprite TimerIcon;
            public Sprite InfoIcon;
            public Sprite CheckBadgeIcon;
            public Sprite LockBadgeIcon;
            public Sprite WarningIcon;
            public Sprite CancelIcon;
            public Sprite PlusIcon;
            public Sprite MinusIcon;
            public Sprite CompletedIcon;
            public Sprite CounterChip;
            public Sprite[] Thumbnails;
        }
    }
}
