#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class StoreCommandExchangeV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN14_StoreCommandExchangeContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string DepotArtPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_art_ammunition_depot.png";
        private const string RangerArtPath = "Assets/Game/Art/UI/V3Shared/RewardUnlock/POP04_RangerSquad_V3.png";
        private const string AriaArtPath = "Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png";
        private const string HelicopterArtPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_art_attack_helicopter.png";
        private const string StoreIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_StoreCart_V3.png";
        private const string OperationsIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_OperationsCompass_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(65, 78, 83, 255);
        private static readonly Color DarkTop = new Color32(24, 35, 39, 252);
        private static readonly Color DarkBottom = new Color32(4, 10, 13, 254);
        private static readonly Color RaisedTop = new Color32(40, 50, 54, 255);
        private static readonly Color RaisedBottom = new Color32(10, 17, 20, 255);
        private static readonly Color Cyan = new Color32(0, 184, 235, 255);
        private static readonly Color BlueTop = new Color32(21, 126, 203, 255);
        private static readonly Color BlueBottom = new Color32(2, 65, 119, 255);
        private static readonly Color Amber = new Color32(250, 174, 0, 255);
        private static readonly Color AmberTop = new Color32(255, 194, 15, 255);
        private static readonly Color AmberBottom = new Color32(223, 125, 0, 255);
        private static readonly Color Violet = new Color32(143, 82, 190, 255);
        private static readonly Color TextPrimary = new Color32(244, 245, 242, 255);
        private static readonly Color TextMuted = new Color32(173, 181, 181, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiArtCatalog catalog;
        private static Sprite depotArt;
        private static Sprite rangerArt;
        private static Sprite ariaArt;
        private static Sprite helicopterArt;
        private static Sprite storeIcon;
        private static Sprite operationsIcon;

        [MenuItem("Game/UI/V3/Rebuild SCN-14 Store")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            RectTransform rootRect = CreateRect("SCN14_StoreCommandExchangeContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject root = rootRect.gameObject;
            StoreCommandExchangeV3View view = root.AddComponent<StoreCommandExchangeV3View>();
            Image black = CreateImage("CanvasBlack", root.transform, null, Color.black, false);
            Stretch(black.rectTransform);
            RectTransform composition = CreateTopLeft("StoreComposition", root.transform, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);

            var rightTargets = new List<RectTransform>();
            var widthTargets = new List<RectTransform>();
            BuildHeader(composition, rightTargets, widthTargets, out TMP_Text credits, out TMP_Text command);
            BuildCategories(composition, out Button[] categoryButtons, out V3GradientGraphic[] categoryGradients);
            BuildOffers(
                composition,
                widthTargets,
                out TMP_Text heading,
                out Button[] offerButtons,
                out V3GradientGraphic[] offerGradients,
                out TMP_Text[] offerTitles,
                out TMP_Text[] offerSubtitles,
                out TMP_Text[] offerPrices);
            BuildDetail(
                composition,
                rightTargets,
                out TMP_Text detailTitle,
                out TMP_Text detailTimer,
                out RawImage detailArt,
                out TMP_Text[] detailLines,
                out TMP_Text detailNote);
            BuildFooter(composition, rightTargets, widthTargets, out Button purchase, out TMP_Text purchaseLabel);

            MainMenuV3SectionLayoutView layout = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            layout.Configure(ReferenceResolution, MainMenuV3SectionAlignment.Center, rightTargets.ToArray(), true, null, widthTargets.ToArray());
            view.Configure(
                credits,
                command,
                heading,
                categoryButtons,
                categoryGradients,
                offerButtons,
                offerGradients,
                offerTitles,
                offerSubtitles,
                offerPrices,
                detailTitle,
                detailTimer,
                detailArt,
                new Texture[] { rangerArt.texture, depotArt.texture, ariaArt.texture, helicopterArt.texture },
                detailLines,
                detailNote,
                purchase,
                purchaseLabel);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Store V3 prefab: {PrefabPath}");
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[StoreCommandExchangeV3PrefabBuilder] result=Passed v3=True purchase=DesignedUnavailable");
        }

        [MenuItem("Game/UI/V3/Validate SCN-14 Store")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Store V3 prefab: {PrefabPath}");
            StoreCommandExchangeV3View view = prefab.GetComponent<StoreCommandExchangeV3View>();
            if (view == null || view.CategoryButtons == null || view.CategoryButtons.Length != 6 || view.OfferButtons == null || view.OfferButtons.Length != 4)
                throw new MissingReferenceException("Store V3 category/product bindings are incomplete.");
            if (view.PurchaseButton == null || view.PurchaseButton.interactable)
                throw new InvalidOperationException("Store purchase must remain disabled until the receipt/reward service chain exists.");
            Require(prefab.transform, "StoreComposition/Header/StoreBrand");
            Require(prefab.transform, "StoreComposition/CategoryRail/Category_0");
            Require(prefab.transform, "StoreComposition/OffersPanel/Offer_0");
            RawImage detailArtImage = Require(prefab.transform, "StoreComposition/DetailPanel/DetailArtClip/DetailArt").GetComponent<RawImage>();
            if (detailArtImage == null || detailArtImage.GetComponent<AspectRatioFitter>()?.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("Store detail art must crop without stretching.");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || !layout.ExpandToCanvasWidth || layout.ReferenceResolution != ReferenceResolution)
                throw new InvalidOperationException("Store V3 must fill 16:9 and 20:9 canvases.");
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 34)
                throw new InvalidOperationException($"Store V3 requires directional gradient chrome; found {gradients}.");
            Debug.Log($"[StoreCommandExchangeV3Validation] result=Passed gradients={gradients} categories=6 offers=4 purchase=Disabled");
        }

        private static void LoadAssets()
        {
            ConfigureSprite(DepotArtPath, 1024);
            ConfigureSprite(RangerArtPath, 2048);
            ConfigureSprite(AriaArtPath, 1024);
            ConfigureSprite(HelicopterArtPath, 1024);
            ConfigureSprite(StoreIconPath, 512);
            ConfigureSprite(OperationsIconPath, 512);
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            catalog = V3UiFoundationBuilder.RequireCatalog();
            depotArt = RequireSprite(DepotArtPath);
            rangerArt = RequireSprite(RangerArtPath);
            ariaArt = RequireSprite(AriaArtPath);
            helicopterArt = RequireSprite(HelicopterArtPath);
            storeIcon = RequireSprite(StoreIconPath);
            operationsIcon = RequireSprite(OperationsIconPath);
            if (boldFont == null || mediumFont == null)
                throw new MissingReferenceException("Store V3 fonts are missing.");
        }

        private static void BuildHeader(
            RectTransform root,
            ICollection<RectTransform> rightTargets,
            ICollection<RectTransform> widthTargets,
            out TMP_Text creditsValue,
            out TMP_Text commandValue)
        {
            RectTransform header = CreateTopLeft("Header", root, 9f, 10f, 1654f, 89f);
            RectTransform brand = CreateTopLeft("StoreBrand", header, 0f, 0f, 340f, 89f);
            CreateGradient(brand, DarkTop, DarkBottom, Border, 3f);
            RectTransform iconCell = CreateTopLeft("IconCell", brand, 0f, 0f, 102f, 89f);
            CreateGradient(iconCell, BlueTop, BlueBottom, Cyan, 3f);
            Image cart = CreateImage("CartIcon", iconCell, storeIcon, TextPrimary, false);
            SetCentered(cart.rectTransform, 64f, 64f);
            TMP_Text title = CreateText("Title", brand, "STORE", 50f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(title.rectTransform, 120f, 0f, 210f, 86f);

            RectTransform backdropClip = CreateTopLeft("HeaderBackdropClip", header, 340f, 0f, 630f, 89f);
            backdropClip.gameObject.AddComponent<RectMask2D>();
            RawImage backdrop = CreateRaw("HeaderBackdrop", backdropClip, depotArt.texture, new Color(.55f, .49f, .38f, 1f));
            AddCover(backdrop, depotArt.texture);
            CreateOverlay("HeaderShade", backdropClip, new Color(0f, 0f, 0f, .22f), new Color(0f, 0f, 0f, .72f));
            widthTargets.Add(backdropClip);

            RectTransform credits = BuildResourceChip(header, "Credits", 970f, 0f, 237f, 89f, catalog.CreditsIcon, "CREDITS", "24,750", Amber, out creditsValue);
            RectTransform command = BuildResourceChip(header, "Command", 1214f, 0f, 236f, 89f, catalog.CommandIcon, "COMMAND", "8,430", Cyan, out commandValue);
            Button settings = CreateButton("SettingsButton", header, 1457f, 0f, 97f, 89f, DarkTop, DarkBottom, Border, 3f);
            Image settingsIcon = CreateImage("Icon", settings.transform, catalog.SettingsIcon, TextPrimary, false);
            SetCentered(settingsIcon.rectTransform, 52f, 52f);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
            Button close = CreateButton("CloseButton", header, 1562f, 0f, 92f, 89f, DarkTop, DarkBottom, Border, 3f);
            CreateCloseIcon(close.transform, TextPrimary);
            close.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            rightTargets.Add(credits);
            rightTargets.Add(command);
            rightTargets.Add(settings.GetComponent<RectTransform>());
            rightTargets.Add(close.GetComponent<RectTransform>());
        }

        private static RectTransform BuildResourceChip(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Sprite icon,
            string label,
            string value,
            Color accent,
            out TMP_Text valueText)
        {
            RectTransform chip = CreateTopLeft(name, parent, x, y, width, height);
            CreateGradient(chip, DarkTop, DarkBottom, Border, 3f);
            Image iconImage = CreateImage("Icon", chip, icon, accent, false);
            SetTopLeft(iconImage.rectTransform, 15f, 15f, 57f, 57f);
            TMP_Text labelText = CreateText("Label", chip, label, 18f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(labelText.rectTransform, 83f, 6f, width - 90f, 31f);
            valueText = CreateText("Value", chip, value, 31f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(valueText.rectTransform, 83f, 34f, width - 90f, 44f);
            return chip;
        }

        private static void BuildCategories(RectTransform root, out Button[] buttons, out V3GradientGraphic[] gradients)
        {
            RectTransform rail = CreateTopLeft("CategoryRail", root, 9f, 110f, 264f, 679f);
            string[] labels = { "FEATURED", "STARTER PACKS", "RESOURCES", "ARMORY", "COSMETICS", "OPERATIONS" };
            Sprite[] sprites =
            {
                null,
                RequireSprite(V3UiFoundationBuilder.CommanderRewardIconPath),
                catalog.CommandIcon,
                RequireSprite(V3UiFoundationBuilder.CommanderBadgeIconPath),
                RequireSprite(V3UiFoundationBuilder.MatchPlayerIconPath),
                operationsIcon
            };
            buttons = new Button[labels.Length];
            gradients = new V3GradientGraphic[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float y = i * 110f;
                bool selected = i == 0;
                Button button = CreateButton(
                    $"Category_{i}", rail, 0f, y, 264f, 103f,
                    selected ? BlueTop : DarkTop,
                    selected ? BlueBottom : DarkBottom,
                    selected ? Cyan : Border,
                    3f);
                gradients[i] = button.targetGraphic as V3GradientGraphic;
                buttons[i] = button;
                RectTransform iconRoot = CreateTopLeft("IconRoot", button.transform, 16f, 15f, 65f, 65f);
                if (i == 0)
                {
                    V3StarGraphic star = iconRoot.gameObject.AddComponent<V3StarGraphic>();
                    star.color = TextPrimary;
                }
                else
                {
                    Image icon = CreateImage("Icon", iconRoot, sprites[i], TextPrimary, false);
                    Stretch(icon.rectTransform);
                }
                TMP_Text label = CreateText("Label", button.transform, labels[i], i == 1 ? 22f : 24f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
                SetTopLeft(label.rectTransform, 102f, 7f, 150f, 86f);
            }
        }

        private static void BuildOffers(
            RectTransform root,
            ICollection<RectTransform> widthTargets,
            out TMP_Text heading,
            out Button[] offerButtons,
            out V3GradientGraphic[] offerGradients,
            out TMP_Text[] offerTitles,
            out TMP_Text[] offerSubtitles,
            out TMP_Text[] offerPrices)
        {
            RectTransform panel = CreateTopLeft("OffersPanel", root, 283f, 110f, 868f, 679f);
            CreateGradient(panel, DarkTop, DarkBottom, Border, 3f);
            widthTargets.Add(panel);
            heading = CreateText("OffersHeading", panel, "FEATURED OFFERS", 31f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetHorizontalStretch(heading.rectTransform, 20f, 18f, 0f, 52f);

            Sprite[] art = { rangerArt, depotArt, ariaArt, helicopterArt };
            string[] titles = { "RECON STARTER PACK", "COMMAND READY BUNDLE", "BLUE VANGUARD FRAME", "AIRLIFT SUPPORT BUNDLE" };
            string[] subtitles = { "2,500 CREDITS + 120 COMMAND", "7,500 CREDITS + 200 COMMAND", "COMMANDER COSMETIC", "300 COMMAND + SUPPORT PARTS" };
            string[] prices = { "$4.99", "$9.99", "250", "$14.99" };
            Color[] accents = { Cyan, Amber, Cyan, Violet };
            offerButtons = new Button[4];
            offerGradients = new V3GradientGraphic[4];
            offerTitles = new TMP_Text[4];
            offerSubtitles = new TMP_Text[4];
            offerPrices = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                int column = i % 2;
                int row = i / 2;
                RectTransform slot = CreateAnchored(
                    $"OfferSlot_{i}", panel,
                    column == 0 ? 0f : .5f,
                    column == 0 ? .5f : 1f,
                    10f, 10f,
                    58f + row * 307f,
                    296f);
                Button card = slot.gameObject.AddComponent<Button>();
                V3GradientGraphic frame = CreateGradient(slot, DarkTop, DarkBottom, i == 0 ? new Color32(75, 211, 255, 255) : accents[i], 3f);
                card.targetGraphic = frame;
                card.transition = Selectable.Transition.ColorTint;
                offerButtons[i] = card;
                offerGradients[i] = frame;
                RectTransform artClip = CreateHorizontalStretch("ArtClip", slot, 4f, 4f, 4f, 226f);
                artClip.gameObject.AddComponent<RectMask2D>();
                RawImage image = CreateRaw("Art", artClip, art[i].texture, Color.white);
                AddCover(image, art[i].texture);
                CreateOverlay("Readability", artClip, new Color(0f, 0f, 0f, .04f), new Color(0f, 0f, 0f, .72f));
                offerTitles[i] = CreateText("Title", slot, titles[i], 25f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
                SetHorizontalStretch(offerTitles[i].rectTransform, 17f, 15f, 6f, 48f);
                offerSubtitles[i] = CreateText("Summary", slot, subtitles[i], 17f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
                SetHorizontalStretch(offerSubtitles[i].rectTransform, 17f, 145f, 237f, 47f);
                RectTransform price = CreateTopRight("PriceChip", slot, 10f, 237f, 118f, 49f);
                CreateGradient(price, accents[i], Color.Lerp(accents[i], Color.black, .45f), accents[i], 3f);
                offerPrices[i] = CreateText("Price", price, prices[i], 27f, boldFont, TextAlignmentOptions.Center, i == 0 || i == 2 ? DarkBottom : TextPrimary);
                Stretch(offerPrices[i].rectTransform);
            }
        }

        private static void BuildDetail(
            RectTransform root,
            ICollection<RectTransform> rightTargets,
            out TMP_Text title,
            out TMP_Text timer,
            out RawImage detailArt,
            out TMP_Text[] detailLines,
            out TMP_Text note)
        {
            RectTransform panel = CreateTopLeft("DetailPanel", root, 1161f, 110f, 447f, 679f);
            CreateGradient(panel, DarkTop, DarkBottom, Border, 3f);
            rightTargets.Add(panel);
            title = CreateText("DetailTitle", panel, "RECON STARTER PACK", 34f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetHorizontalStretch(title.rectTransform, 22f, 18f, 5f, 51f);
            timer = CreateText("DetailTimer", panel, "72H REMAINING", 21f, boldFont, TextAlignmentOptions.MidlineLeft, Amber);
            SetHorizontalStretch(timer.rectTransform, 22f, 18f, 55f, 38f);
            RectTransform artClip = CreateTopLeft("DetailArtClip", panel, 13f, 94f, 421f, 305f);
            artClip.gameObject.AddComponent<RectMask2D>();
            detailArt = CreateRaw("DetailArt", artClip, rangerArt.texture, Color.white);
            AddCover(detailArt, rangerArt.texture);
            CreateOverlay("DetailShade", artClip, new Color(0f, 0f, 0f, .02f), new Color(0f, 0f, 0f, .44f));

            TMP_Text includes = CreateText("IncludesTitle", panel, "INCLUDES:", 22f, boldFont, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(includes.rectTransform, 22f, 400f, 220f, 39f);
            string[] values = { "2,500 CREDITS", "120 COMMAND", "RANGER SQUAD UNLOCK", "BLUE VANGUARD FRAME" };
            Sprite[] icons = { catalog.CreditsIcon, catalog.CommandIcon, RequireSprite(V3UiFoundationBuilder.CampaignSquadIconPath), ariaArt };
            detailLines = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float y = 442f + i * 50f;
                RectTransform row = CreateTopLeft($"DetailLine_{i}", panel, 22f, y, 403f, 49f);
                CreateGradient(row, DarkTop, DarkBottom, new Color32(43, 57, 61, 255), 3f);
                Image icon = CreateImage("Icon", row, icons[i], i < 2 ? (i == 0 ? Amber : Cyan) : TextPrimary, false);
                SetTopLeft(icon.rectTransform, 7f, 5f, 39f, 39f);
                detailLines[i] = CreateText("Text", row, values[i], 20f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
                SetTopLeft(detailLines[i].rectTransform, 62f, 1f, 333f, 45f);
            }
            note = CreateText("DetailNote", panel, "Duplicates grant 40 Ranger Parts.", 16f, mediumFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetHorizontalStretch(note.rectTransform, 22f, 15f, 645f, 29f);
        }

        private static void BuildFooter(
            RectTransform root,
            ICollection<RectTransform> rightTargets,
            ICollection<RectTransform> widthTargets,
            out Button purchase,
            out TMP_Text purchaseLabel)
        {
            Button back = CreateButton("BackButton", root, 9f, 802f, 331f, 115f, RaisedTop, RaisedBottom, Border, 3f);
            back.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            CreateBackIcon(back.transform, TextPrimary);
            TMP_Text backText = CreateText("Label", back.transform, "BACK", 39f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(backText.rectTransform, 115f, 10f, 192f, 92f);

            RectTransform eligibility = CreateTopLeft("EligibilityPanel", root, 350f, 802f, 600f, 115f);
            CreateGradient(eligibility, DarkTop, DarkBottom, Border, 3f);
            widthTargets.Add(eligibility);
            RectTransform shield = CreateTopLeft("Shield", eligibility, 26f, 20f, 62f, 73f);
            CreateShieldIcon(shield, TextPrimary);
            TMP_Text level = CreateText("Eligibility", eligibility, "AVAILABLE FOR LEVELS 1 - 8", 27f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetHorizontalStretch(level.rectTransform, 111f, 15f, 9f, 45f);
            TMP_Text reason = CreateText("UnavailableReason", eligibility, "Purchases unavailable until secure receipt services are connected.", 15f, mediumFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetHorizontalStretch(reason.rectTransform, 111f, 15f, 53f, 44f);

            purchase = CreateButton("PurchaseButton", root, 951f, 802f, 712f, 115f, AmberTop, AmberBottom, Amber, 3f);
            rightTargets.Add(purchase.GetComponent<RectTransform>());
            purchaseLabel = CreateText("Label", purchase.transform, "PURCHASE $4.99", 51f, boldFont, TextAlignmentOptions.Center, new Color32(22, 16, 2, 255));
            Stretch(purchaseLabel.rectTransform);
            purchase.interactable = false;
            ColorBlock colors = purchase.colors;
            colors.disabledColor = Color.white;
            purchase.colors = colors;
        }

        private static Button CreateButton(string name, Transform parent, float x, float y, float width, float height, Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic gradient = CreateGradient(rect, top, bottom, border, borderWidth);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = gradient;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        private static V3GradientGraphic CreateGradient(RectTransform rect, Color top, Color bottom, Color border, float borderWidth)
        {
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.ConfigureCorners(Color.Lerp(top, Color.white, .04f), top, Color.Lerp(bottom, Color.black, .12f), bottom, border, borderWidth);
            return gradient;
        }

        private static void CreateOverlay(string name, Transform parent, Color top, Color bottom)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, Color.clear, 0f);
            gradient.raycastTarget = false;
        }

        private static void CreateCloseIcon(Transform parent, Color color)
        {
            Image first = CreateSolid("CloseA", parent, 29f, 42f, 36f, 6f, color);
            Image second = CreateSolid("CloseB", parent, 29f, 42f, 36f, 6f, color);
            first.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            second.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        private static void CreateBackIcon(Transform parent, Color color)
        {
            CreateSolid("Shaft", parent, 43f, 54f, 58f, 6f, color);
            Image upper = CreateSolid("Upper", parent, 37f, 41f, 34f, 6f, color);
            Image lower = CreateSolid("Lower", parent, 37f, 66f, 34f, 6f, color);
            upper.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -43f);
            lower.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 43f);
        }

        private static void CreateShieldIcon(RectTransform root, Color color)
        {
            Image left = CreateSolid("Left", root, 9f, 7f, 29f, 5f, color);
            Image right = CreateSolid("Right", root, 34f, 7f, 29f, 5f, color);
            left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -17f);
            right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 17f);
            CreateSolid("SideL", root, 6f, 13f, 5f, 37f, color);
            CreateSolid("SideR", root, 57f, 13f, 5f, 37f, color);
            Image tipL = CreateSolid("TipL", root, 16f, 50f, 29f, 5f, color);
            Image tipR = CreateSolid("TipR", root, 33f, 50f, 29f, 5f, color);
            tipL.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            tipR.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            RectTransform starRoot = CreateTopLeft("Star", root, 20f, 19f, 29f, 29f);
            V3StarGraphic star = starRoot.gameObject.AddComponent<V3StarGraphic>();
            star.color = color;
        }

        private static Image CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static RawImage CreateRaw(string name, Transform parent, Texture texture, Color color)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AddCover(RawImage image, Texture texture)
        {
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = texture.width / (float)texture.height;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(200f, 60f), Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 position) =>
            V3UiPrefabFactory.CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, position);

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static RectTransform CreateTopRight(string name, Transform parent, float right, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(width, height), new Vector2(-right, -y));
            rect.pivot = new Vector2(1f, 1f);
            return rect;
        }

        private static RectTransform CreateHorizontalStretch(string name, Transform parent, float left, float right, float y, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-(left + right), height), new Vector2(left, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static RectTransform CreateAnchored(string name, Transform parent, float minX, float maxX, float left, float right, float y, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(minX, 1f), new Vector2(maxX, 1f), new Vector2(-(left + right), height), new Vector2(left, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast) =>
            V3UiPrefabFactory.CreateImage(name, parent, sprite, color, raycast, false);

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetHorizontalStretch(RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(-(left + right), height);
            rect.anchoredPosition = new Vector2(left, -y);
        }

        private static void SetCentered(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Transform Require(Transform root, string path)
        {
            Transform result = root.Find(path);
            if (result == null)
                throw new MissingReferenceException($"Store V3 is missing {path}.");
            return result;
        }

        private static void AssignMenuScenePrefab(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                content = root.GetComponentInChildren<UIShellContentView>(true);
                if (content != null)
                    break;
            }
            if (content == null)
                throw new InvalidOperationException("Menu scene is missing UIShellContentView.");
            SerializedObject serialized = new(content);
            SerializedProperty property = serialized.FindProperty("storeContentPrefab");
            if (property == null)
                throw new MissingFieldException(nameof(UIShellContentView), "storeContentPrefab");
            property.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureSprite(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing Store V3 art: {path}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing Store V3 sprite: {path}");
            return sprite;
        }
    }
}
#endif
