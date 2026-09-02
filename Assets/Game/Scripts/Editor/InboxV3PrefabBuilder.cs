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
    public static class InboxV3PrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN15_InboxContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string EnvelopePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_06_inbox_envelope.png";
        private const string OperationsPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_OperationsCompass_V3.png";
        private const string SkirmishPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_SkirmishBlades_V3.png";
        private const string AriaPath = "Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png";
        private const string RangerPath = "Assets/Game/Art/UI/V3Shared/RewardUnlock/POP04_RangerSquad_V3.png";
        private const string NorthBridgePath = "Assets/Game/Art/UI/V3Shared/Inbox/SCN15_NorthBridgeIntel_V3.png";
        private const string DistrictMapPath = "Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png";
        private const string NetworkPath = "Assets/Game/Art/UI/V3Shared/Backgrounds/SCN01_LoadingEnvironment_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(56, 70, 74, 255);
        private static readonly Color DarkTop = new Color32(27, 38, 42, 255);
        private static readonly Color DarkBottom = new Color32(4, 10, 13, 255);
        private static readonly Color RaisedTop = new Color32(44, 54, 57, 255);
        private static readonly Color RaisedBottom = new Color32(12, 18, 20, 255);
        private static readonly Color Cyan = new Color32(0, 190, 238, 255);
        private static readonly Color BlueTop = new Color32(21, 132, 208, 255);
        private static readonly Color BlueBottom = new Color32(1, 65, 116, 255);
        private static readonly Color Green = new Color32(111, 181, 47, 255);
        private static readonly Color GreenTop = new Color32(101, 163, 48, 255);
        private static readonly Color GreenBottom = new Color32(34, 96, 25, 255);
        private static readonly Color Amber = new Color32(250, 177, 0, 255);
        private static readonly Color Orange = new Color32(232, 99, 18, 255);
        private static readonly Color TextPrimary = new Color32(244, 245, 242, 255);
        private static readonly Color TextMuted = new Color32(185, 193, 193, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiArtCatalog catalog;
        private static Sprite envelope;
        private static Sprite operations;
        private static Sprite skirmish;
        private static Sprite aria;
        private static Sprite ranger;
        private static Sprite northBridge;
        private static Sprite districtMap;
        private static Sprite network;

        [MenuItem("Game/UI/V3/Rebuild SCN-15 Inbox")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();
            RectTransform rootRect = CreateRect("SCN15_InboxContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject root = rootRect.gameObject;
            InboxV3View view = root.AddComponent<InboxV3View>();
            Image black = CreateImage("CanvasBlack", root.transform, null, Color.black, false);
            Stretch(black.rectTransform);
            RectTransform composition = CreateTopLeft("InboxComposition", root.transform, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);

            var rightTargets = new List<RectTransform>();
            var widthTargets = new List<RectTransform>();
            BuildHeader(composition, rightTargets, widthTargets, out TMP_Text credits, out TMP_Text command);
            BuildCategoryRail(composition,
                out Button[] categories, out V3GradientGraphic[] categoryGradients, out TMP_Text[] categoryBadges,
                out Button filter, out TMP_Text filterLabel);
            BuildMessageList(composition, widthTargets,
                out TMP_InputField search, out Button sort, out TMP_Text sortLabel, out Button markAllRead,
                out Button[] messages, out V3GradientGraphic[] messageGradients, out TMP_Text[] messageTitles,
                out TMP_Text[] messageSenders, out TMP_Text[] messageTimes, out GameObject[] unreadBars, out GameObject emptyState);
            BuildDetail(composition, rightTargets,
                out TMP_Text detailTitle, out TMP_Text detailFrom, out TMP_Text detailDate, out RawImage detailArt,
                out TMP_Text detailBody, out Button favorite, out Graphic favoriteStar, out Button markRead,
                out TMP_Text markReadLabel, out Button[] attachments, out TMP_Text[] attachmentTitles,
                out TMP_Text[] attachmentFiles, out TMP_Text[] attachmentStates);

            MainMenuV3SectionLayoutView layout = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            layout.Configure(ReferenceResolution, MainMenuV3SectionAlignment.Center, rightTargets.ToArray(), true, null, widthTargets.ToArray());
            view.Configure(
                credits, command, categories, categoryGradients, categoryBadges, search, sort, sortLabel,
                filter, filterLabel, markAllRead, messages, messageGradients, messageTitles, messageSenders,
                messageTimes, unreadBars, emptyState, detailTitle, detailFrom, detailDate, detailArt,
                new Texture[] { northBridge.texture, districtMap.texture, ranger.texture, aria.texture, network.texture },
                detailBody, favorite, favoriteStar, markRead, markReadLabel, attachments, attachmentTitles,
                attachmentFiles, attachmentStates);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Inbox V3 prefab: {PrefabPath}");
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[InboxV3PrefabBuilder] result=Passed categories=5 messages=5 gradients=directional borders=3");
        }

        [MenuItem("Game/UI/V3/Validate SCN-15 Inbox")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException(PrefabPath);
            InboxV3View view = prefab.GetComponent<InboxV3View>();
            if (view == null || view.CategoryButtons?.Length != 5 || view.MessageButtons?.Length != 5)
                throw new MissingReferenceException("Inbox category/message bindings are incomplete.");
            Require(prefab.transform, "InboxComposition/Header/TitlePanel");
            Require(prefab.transform, "InboxComposition/CategoryRail/Category_0");
            Require(prefab.transform, "InboxComposition/MessagePanel/Message_0");
            Require(prefab.transform, "InboxComposition/DetailPanel/Attachment_0");
            RawImage art = Require(prefab.transform, "InboxComposition/DetailPanel/DetailArtClip/DetailArt").GetComponent<RawImage>();
            if (art == null || art.GetComponent<AspectRatioFitter>()?.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("Inbox detail art must crop without stretching.");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || !layout.ExpandToCanvasWidth || layout.ReferenceResolution != ReferenceResolution)
                throw new InvalidOperationException("Inbox must fill 16:9 and 20:9 canvases.");
            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 29)
                throw new InvalidOperationException($"Inbox requires directional V3 chrome; found {gradients.Length} gradients.");
            for (int i = 0; i < gradients.Length; i++)
            {
                SerializedObject serialized = new(gradients[i]);
                if (serialized.FindProperty("borderColor").colorValue.a > .01f &&
                    Mathf.Abs(serialized.FindProperty("borderWidth").floatValue - 3f) > .001f)
                    throw new InvalidOperationException($"Inbox visible border mismatch: {gradients[i].name}");
            }
            Debug.Log($"[InboxV3Validation] result=Passed gradients={gradients.Length} categories=5 messages=5");
        }

        private static void LoadAssets()
        {
            string[] paths = { EnvelopePath, OperationsPath, SkirmishPath, AriaPath, RangerPath, NorthBridgePath, DistrictMapPath, NetworkPath };
            int[] sizes = { 512, 512, 512, 2048, 2048, 2048, 2048, 2048 };
            for (int i = 0; i < paths.Length; i++) ConfigureSprite(paths[i], sizes[i]);
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            catalog = V3UiFoundationBuilder.RequireCatalog();
            envelope = RequireSprite(EnvelopePath);
            operations = RequireSprite(OperationsPath);
            skirmish = RequireSprite(SkirmishPath);
            aria = RequireSprite(AriaPath);
            ranger = RequireSprite(RangerPath);
            northBridge = RequireSprite(NorthBridgePath);
            districtMap = RequireSprite(DistrictMapPath);
            network = RequireSprite(NetworkPath);
            if (boldFont == null || mediumFont == null)
                throw new MissingReferenceException("Inbox fonts are missing.");
        }

        private static void BuildHeader(RectTransform root, ICollection<RectTransform> rightTargets, ICollection<RectTransform> widthTargets,
            out TMP_Text creditsValue, out TMP_Text commandValue)
        {
            RectTransform header = CreateTopLeft("Header", root, 11f, 11f, 1650f, 97f);
            Button back = CreateButton("BackButton", header, 0f, 0f, 87f, 97f, DarkTop, DarkBottom, Border);
            back.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            Image backIcon = CreateImage("BackIcon", back.transform, RequireSprite(V3UiFoundationBuilder.CommanderBackIconPath), TextPrimary, false);
            SetCentered(backIcon.rectTransform, 58f, 58f);

            RectTransform logoPanel = CreateTopLeft("LogoPanel", header, 95f, 0f, 350f, 97f);
            CreateGradient(logoPanel, DarkTop, DarkBottom, Border);
            V3UiFoundationBuilder.AddMainMenuLogo(logoPanel, left: 9f, top: 7f, right: 9f, bottom: 7f);

            RectTransform titlePanel = CreateTopLeft("TitlePanel", header, 455f, 0f, 492f, 97f);
            CreateGradient(titlePanel, DarkTop, DarkBottom, Border);
            TMP_Text title = CreateText("Title", titlePanel, "COMMAND INBOX", 42f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            Stretch(title.rectTransform);
            widthTargets.Add(titlePanel);

            RectTransform credits = BuildResourceChip(header, "Credits", 957f, 0f, 285f, catalog.CreditsIcon, "CREDITS", "24,750", Amber, out creditsValue);
            RectTransform command = BuildResourceChip(header, "Command", 1249f, 0f, 273f, catalog.CommandIcon, "COMMAND", "8,430", Cyan, out commandValue);
            Button settings = CreateButton("SettingsButton", header, 1530f, 0f, 120f, 97f, DarkTop, DarkBottom, Border);
            Image settingsIcon = CreateImage("Icon", settings.transform, catalog.SettingsIcon, TextPrimary, false);
            SetCentered(settingsIcon.rectTransform, 61f, 61f);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
            rightTargets.Add(credits);
            rightTargets.Add(command);
            rightTargets.Add(settings.GetComponent<RectTransform>());
        }

        private static RectTransform BuildResourceChip(Transform parent, string name, float x, float y, float width, Sprite icon,
            string label, string value, Color accent, out TMP_Text valueText)
        {
            RectTransform chip = CreateTopLeft(name, parent, x, y, width, 97f);
            CreateGradient(chip, DarkTop, DarkBottom, Border);
            Image iconImage = CreateImage("Icon", chip, icon, accent, false);
            SetTopLeft(iconImage.rectTransform, 17f, 14f, 66f, 66f);
            TMP_Text labelText = CreateText("Label", chip, label, 20f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(labelText.rectTransform, 97f, 8f, width - 106f, 32f);
            valueText = CreateText("Value", chip, value, 34f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(valueText.rectTransform, 97f, 38f, width - 106f, 48f);
            return chip;
        }

        private static void BuildCategoryRail(RectTransform root, out Button[] buttons, out V3GradientGraphic[] gradients,
            out TMP_Text[] badges, out Button filter, out TMP_Text filterLabel)
        {
            RectTransform rail = CreateTopLeft("CategoryRail", root, 11f, 119f, 259f, 808f);
            string[] labels = { "ALL", "OPERATIONS", "ARIA", "REWARDS", "SYSTEM" };
            Color[] accents = { Cyan, new Color32(84, 170, 84, 255), new Color32(31, 157, 230, 255), Amber, TextPrimary };
            buttons = new Button[5];
            gradients = new V3GradientGraphic[5];
            badges = new TMP_Text[5];
            for (int i = 0; i < 5; i++)
            {
                Button button = CreateButton($"Category_{i}", rail, 0f, i * 100f, 259f, 88f,
                    i == 0 ? BlueTop : DarkTop, i == 0 ? BlueBottom : DarkBottom, i == 0 ? Cyan : Border);
                buttons[i] = button;
                gradients[i] = button.targetGraphic as V3GradientGraphic;
                RectTransform iconRoot = CreateTopLeft("IconRoot", button.transform, 18f, 16f, 58f, 56f);
                BuildCategoryIcon(iconRoot, i, accents[i]);
                TMP_Text label = CreateText("Label", button.transform, labels[i], i == 1 ? 23f : 26f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
                SetTopLeft(label.rectTransform, 88f, 6f, 126f, 75f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 15f;
                label.fontSizeMax = i == 1 ? 23f : 26f;
                RectTransform badge = CreateTopLeft("Badge", button.transform, 207f, 23f, 39f, 39f);
                CreateGradient(badge, Color.Lerp(accents[i], Color.white, .12f), Color.Lerp(accents[i], Color.black, .38f), accents[i]);
                badges[i] = CreateText("Count", badge, i == 0 ? "5" : i == 1 ? "2" : "1", 21f, boldFont, TextAlignmentOptions.Center, TextPrimary);
                Stretch(badges[i].rectTransform);
            }

            filter = CreateButton("FilterButton", rail, 8f, 716f, 239f, 74f, RaisedTop, RaisedBottom, Border);
            CreateFilterIcon(CreateTopLeft("FilterIcon", filter.transform, 42f, 22f, 37f, 34f), TextPrimary);
            filterLabel = CreateText("Label", filter.transform, "FILTERS", 27f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(filterLabel.rectTransform, 95f, 7f, 130f, 60f);
        }

        private static void BuildCategoryIcon(RectTransform root, int index, Color color)
        {
            if (index == 0)
            {
                Image icon = CreateImage("Envelope", root, envelope, color, false);
                Stretch(icon.rectTransform);
            }
            else if (index == 1)
            {
                Image icon = CreateImage("Operations", root, operations, color, false);
                Stretch(icon.rectTransform);
            }
            else if (index == 2)
            {
                RectTransform clip = root;
                clip.gameObject.AddComponent<RectMask2D>();
                RawImage portrait = CreateRaw("ARIA", clip, aria.texture, Color.white);
                AddCover(portrait, aria.texture);
            }
            else if (index == 3)
            {
                CreateGiftIcon(root, color);
            }
            else
            {
                Image icon = CreateImage("System", root, catalog.SettingsIcon, color, false);
                Stretch(icon.rectTransform);
            }
        }

        private static void BuildMessageList(RectTransform root, ICollection<RectTransform> widthTargets,
            out TMP_InputField search, out Button sort, out TMP_Text sortLabel, out Button markAllRead,
            out Button[] buttons, out V3GradientGraphic[] gradients, out TMP_Text[] titles, out TMP_Text[] senders,
            out TMP_Text[] times, out GameObject[] unreadBars, out GameObject emptyState)
        {
            RectTransform panel = CreateTopLeft("MessagePanel", root, 278f, 119f, 622f, 808f);
            CreateGradient(panel, DarkTop, DarkBottom, Border);
            widthTargets.Add(panel);
            search = CreateSearchInput(panel);
            sort = CreateButton("SortButton", panel, 404f, 16f, 199f, 54f, RaisedTop, RaisedBottom, Border);
            SetTopRight(sort.GetComponent<RectTransform>(), 19f, 16f, 199f, 54f);
            sortLabel = CreateText("Label", sort.transform, "NEWEST", 23f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(sortLabel.rectTransform, 24f, 4f, 123f, 46f);
            CreateDownChevron(CreateTopLeft("Chevron", sort.transform, 158f, 18f, 23f, 17f), TextMuted);

            string[] initialTitles = { "NORTH BRIDGE INTEL UPDATE", "DAILY OPERATION REPORT", "RANGER SQUAD UNLOCKED", "ARIA TACTICAL REVIEW", "COMMAND NETWORK NOTICE" };
            string[] initialSenders = { "From: Recon Command", "From: Operations Command", "From: Field Command", "From: ARIA", "From: System" };
            string[] initialTimes = { "09:42", "08:15", "07:30", "06:50", "06:10" };
            buttons = new Button[5];
            gradients = new V3GradientGraphic[5];
            titles = new TMP_Text[5];
            senders = new TMP_Text[5];
            times = new TMP_Text[5];
            unreadBars = new GameObject[5];
            for (int i = 0; i < 5; i++)
            {
                float y = 85f + i * 125f;
                Button message = CreateAnchoredButton($"Message_{i}", panel, 12f, 12f, y, 115f,
                    i == 0 ? new Color32(15, 70, 93, 255) : DarkTop,
                    i == 0 ? new Color32(2, 25, 37, 255) : DarkBottom,
                    i == 0 ? Cyan : Border);
                buttons[i] = message;
                gradients[i] = message.targetGraphic as V3GradientGraphic;
                RectTransform iconRoot = CreateTopLeft("IconRoot", message.transform, 16f, 11f, 76f, 91f);
                BuildMessageIcon(iconRoot, i);
                titles[i] = CreateText("Title", message.transform, initialTitles[i], 25f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
                SetHorizontalStretch(titles[i].rectTransform, 111f, 96f, 10f, 42f);
                senders[i] = CreateText("Sender", message.transform, initialSenders[i], 20f, mediumFont, TextAlignmentOptions.MidlineLeft, TextMuted);
                SetHorizontalStretch(senders[i].rectTransform, 111f, 96f, 52f, 40f);
                times[i] = CreateText("Time", message.transform, initialTimes[i], 21f, boldFont, TextAlignmentOptions.MidlineRight, i == 0 ? Cyan : TextMuted);
                SetTopRight(times[i].rectTransform, 31f, 14f, 70f, 36f);
                Image unread = CreateSolid("UnreadBar", message.transform, 0f, 0f, 9f, 68f, Cyan);
                SetTopRight(unread.rectTransform, 8f, 17f, 9f, 68f);
                unreadBars[i] = unread.gameObject;
            }

            emptyState = CreateTopLeft("EmptyState", panel, 20f, 276f, 582f, 120f).gameObject;
            TMP_Text empty = CreateText("Label", emptyState.transform, "NO MESSAGES MATCH THIS FILTER", 25f, boldFont, TextAlignmentOptions.Center, TextMuted);
            Stretch(empty.rectTransform);
            emptyState.SetActive(false);

            markAllRead = CreateAnchoredButton("MarkAllReadButton", panel, 22f, 25f, 724f, 68f, RaisedTop, RaisedBottom, Border);
            TMP_Text markAll = CreateText("Label", markAllRead.transform, "MARK ALL READ", 27f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            Stretch(markAll.rectTransform);
        }

        private static TMP_InputField CreateSearchInput(RectTransform panel)
        {
            RectTransform rect = CreateHorizontalStretch("SearchInput", panel, 22f, 232f, 16f, 54f);
            V3GradientGraphic background = CreateGradient(rect, DarkTop, DarkBottom, Border);
            CreateSearchIcon(CreateTopLeft("SearchIcon", rect, 16f, 14f, 29f, 29f), TextMuted);
            TMP_Text text = CreateText("Text", rect, string.Empty, 21f, mediumFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetHorizontalStretch(text.rectTransform, 55f, 14f, 2f, 49f);
            TMP_Text placeholder = CreateText("Placeholder", rect, "Search messages...", 21f, mediumFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetHorizontalStretch(placeholder.rectTransform, 55f, 14f, 2f, 49f);
            TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.targetGraphic = background;
            return input;
        }

        private static void BuildMessageIcon(RectTransform root, int index)
        {
            RectTransform badge = CreateTopLeft("Badge", root, 4f, 4f, 68f, 83f);
            Color accent = index switch { 0 => new Color32(55, 139, 75, 255), 1 => Green, 2 => Orange, 3 => Cyan, _ => TextMuted };
            CreateGradient(badge, Color.Lerp(accent, DarkTop, .55f), DarkBottom, accent);
            if (index == 0)
                CreateBridgeIcon(badge, accent);
            else if (index == 1)
                CreateBarsIcon(badge, Green);
            else if (index == 2)
            {
                Image icon = CreateImage("Ranger", badge, skirmish, Orange, false);
                SetTopLeft(icon.rectTransform, 10f, 12f, 48f, 57f);
            }
            else if (index == 3)
            {
                badge.gameObject.AddComponent<RectMask2D>();
                RawImage portrait = CreateRaw("ARIA", badge, aria.texture, Color.white);
                AddCover(portrait, aria.texture);
            }
            else
                CreateGlobeIcon(badge, TextMuted);
        }

        private static void BuildDetail(RectTransform root, ICollection<RectTransform> rightTargets,
            out TMP_Text title, out TMP_Text from, out TMP_Text date, out RawImage art, out TMP_Text body,
            out Button favorite, out Graphic favoriteStar, out Button markRead, out TMP_Text markReadLabel,
            out Button[] attachments, out TMP_Text[] attachmentTitles, out TMP_Text[] attachmentFiles, out TMP_Text[] attachmentStates)
        {
            RectTransform panel = CreateTopLeft("DetailPanel", root, 908f, 119f, 753f, 808f);
            CreateGradient(panel, DarkTop, DarkBottom, Border);
            rightTargets.Add(panel);
            title = CreateText("DetailTitle", panel, "NORTH BRIDGE INTEL UPDATE", 35f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetHorizontalStretch(title.rectTransform, 23f, 97f, 4f, 53f);
            from = CreateText("DetailFrom", panel, "From: <color=#77B936>Recon Command</color>", 21f, mediumFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(from.rectTransform, 23f, 56f, 420f, 34f);
            date = CreateText("DetailDate", panel, "Today, 09:42", 20f, mediumFont, TextAlignmentOptions.MidlineRight, TextMuted);
            SetTopRight(date.rectTransform, 21f, 56f, 180f, 34f);
            favorite = CreateButton("FavoriteButton", panel, 682f, 10f, 56f, 48f, DarkTop, DarkBottom, Color.clear);
            RectTransform starRoot = CreateTopLeft("Star", favorite.transform, 9f, 4f, 39f, 39f);
            V3StarGraphic star = starRoot.gameObject.AddComponent<V3StarGraphic>();
            star.color = TextMuted;
            favoriteStar = star;

            RectTransform artClip = CreateTopLeft("DetailArtClip", panel, 20f, 105f, 713f, 251f);
            artClip.gameObject.AddComponent<RectMask2D>();
            art = CreateRaw("DetailArt", artClip, northBridge.texture, Color.white);
            AddCover(art, northBridge.texture);
            CreateOverlay("DetailShade", artClip, new Color(0f, 0f, 0f, .03f), new Color(0f, 0f, 0f, .27f));

            body = CreateText("DetailBody", panel,
                "Recon assets confirm increased militia activity around North Bridge.\nMultiple supply convoys observed crossing into the East Ridge sector.\nRecommend strike window within the next 12 hours.",
                20f, mediumFont, TextAlignmentOptions.TopLeft, TextPrimary);
            SetHorizontalStretch(body.rectTransform, 24f, 24f, 375f, 105f);
            body.textWrappingMode = TextWrappingModes.Normal;
            CreateSolid("BodyDivider", panel, 23f, 488f, 707f, 3f, Border);
            TMP_Text attachmentHeading = CreateText("AttachmentsHeading", panel, "ATTACHMENTS (2)", 20f, boldFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetTopLeft(attachmentHeading.rectTransform, 24f, 493f, 260f, 35f);

            attachments = new Button[2];
            attachmentTitles = new TMP_Text[2];
            attachmentFiles = new TMP_Text[2];
            attachmentStates = new TMP_Text[2];
            string[] names = { "INTEL REPORT", "SCOUT MAP" };
            string[] files = { "NorthBridge_Intel.pdf", "NorthBridge_Map.png" };
            string[] sizes = { "1.4 MB", "2.1 MB" };
            for (int i = 0; i < 2; i++)
            {
                float x = 23f + i * 364f;
                Button attachment = CreateButton($"Attachment_{i}", panel, x, 532f, 350f, 123f, DarkTop, DarkBottom, Border);
                attachments[i] = attachment;
                RectTransform iconRoot = CreateTopLeft("IconRoot", attachment.transform, 16f, 19f, 65f, 80f);
                if (i == 0) CreateDocumentIcon(iconRoot, Green); else
                {
                    Image icon = CreateImage("Map", iconRoot, operations, Green, false);
                    Stretch(icon.rectTransform);
                }
                attachmentTitles[i] = CreateText("Title", attachment.transform, names[i], 20f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
                SetTopLeft(attachmentTitles[i].rectTransform, 91f, 15f, 179f, 34f);
                attachmentFiles[i] = CreateText("File", attachment.transform, files[i], 16f, mediumFont, TextAlignmentOptions.MidlineLeft, TextMuted);
                SetTopLeft(attachmentFiles[i].rectTransform, 91f, 48f, 180f, 31f);
                attachmentStates[i] = CreateText("State", attachment.transform, sizes[i], 16f, mediumFont, TextAlignmentOptions.MidlineLeft, Green);
                SetTopLeft(attachmentStates[i].rectTransform, 91f, 78f, 170f, 30f);
                CreateDownloadIcon(CreateTopLeft("Download", attachment.transform, 298f, 40f, 34f, 43f), TextPrimary);
            }

            markRead = CreateButton("MarkReadButton", panel, 20f, 704f, 260f, 82f, RaisedTop, RaisedBottom, Border);
            markReadLabel = CreateText("Label", markRead.transform, "MARK READ", 27f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            Stretch(markReadLabel.rectTransform);
            Button viewIntel = CreateButton("ViewIntelButton", panel, 294f, 704f, 439f, 82f, GreenTop, GreenBottom, Green);
            TMP_Text intelLabel = CreateText("Label", viewIntel.transform, "VIEW INTEL", 37f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(intelLabel.rectTransform, 71f, 5f, 290f, 70f);
            CreateChevron(CreateTopLeft("Chevron", viewIntel.transform, 376f, 27f, 25f, 29f), TextPrimary, true);
            viewIntel.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.DistrictDetail, true);
        }

        private static Button CreateButton(string name, Transform parent, float x, float y, float width, float height,
            Color top, Color bottom, Color border)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = CreateGradient(rect, top, bottom, border);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        private static Button CreateAnchoredButton(string name, Transform parent, float left, float right, float y, float height,
            Color top, Color bottom, Color border)
        {
            RectTransform rect = CreateHorizontalStretch(name, parent, left, right, y, height);
            V3GradientGraphic graphic = CreateGradient(rect, top, bottom, border);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        private static V3GradientGraphic CreateGradient(RectTransform rect, Color top, Color bottom, Color border)
        {
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.ConfigureCorners(Color.Lerp(top, Color.white, .04f), top, Color.Lerp(bottom, Color.black, .12f), bottom, border, border.a > .01f ? 3f : 0f);
            return gradient;
        }

        private static void CreateOverlay(string name, Transform parent, Color top, Color bottom)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, Color.clear, 0f);
            gradient.raycastTarget = false;
        }

        private static void CreateBackIcon(Transform parent, Color color)
        {
            CreateSolid("Shaft", parent, 28f, 46f, 38f, 6f, color);
            Image a = CreateSolid("Upper", parent, 22f, 35f, 27f, 6f, color);
            Image b = CreateSolid("Lower", parent, 22f, 57f, 27f, 6f, color);
            a.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -43f);
            b.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 43f);
        }

        private static void CreateFilterIcon(RectTransform root, Color color)
        {
            CreateSolid("Top", root, 1f, 2f, 35f, 5f, color);
            CreateStroke("Left", root, new Vector2(4f, 8f), new Vector2(18f, 20f), 5f, color);
            CreateStroke("Right", root, new Vector2(34f, 8f), new Vector2(18f, 20f), 5f, color);
            CreateSolid("Stem", root, 16f, 19f, 6f, 14f, color);
        }

        private static void CreateSearchIcon(RectTransform root, Color color)
        {
            RectTransform ringRoot = CreateTopLeft("Ring", root, 0f, 0f, 22f, 22f);
            V3RingGraphic ring = ringRoot.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, 4f, 32);
            CreateStroke("Handle", root, new Vector2(18f, 18f), new Vector2(30f, 30f), 4f, color);
        }

        private static void CreateGiftIcon(RectTransform root, Color color)
        {
            CreateSolid("Box", root, 8f, 24f, 42f, 29f, color);
            CreateSolid("Lid", root, 4f, 19f, 50f, 7f, Color.Lerp(color, Color.white, .15f));
            CreateSolid("RibbonV", root, 26f, 19f, 7f, 35f, DarkBottom);
            CreateSolid("RibbonH", root, 8f, 33f, 42f, 5f, DarkBottom);
            RectTransform bowLeft = CreateTopLeft("BowLeft", root, 12f, 4f, 18f, 16f);
            RectTransform bowRight = CreateTopLeft("BowRight", root, 29f, 4f, 18f, 16f);
            V3RingGraphic left = bowLeft.gameObject.AddComponent<V3RingGraphic>();
            V3RingGraphic right = bowRight.gameObject.AddComponent<V3RingGraphic>();
            left.Configure(color, 4f, 24);
            right.Configure(color, 4f, 24);
        }

        private static void CreateBridgeIcon(RectTransform root, Color color)
        {
            CreateSolid("Deck", root, 13f, 47f, 42f, 6f, color);
            CreateSolid("PierL", root, 17f, 30f, 6f, 24f, color);
            CreateSolid("PierR", root, 45f, 30f, 6f, 24f, color);
            CreateSolid("Top", root, 14f, 28f, 39f, 5f, color);
            CreateStroke("RoofA", root, new Vector2(15f, 27f), new Vector2(34f, 18f), 5f, color);
            CreateStroke("RoofB", root, new Vector2(34f, 18f), new Vector2(54f, 27f), 5f, color);
        }

        private static void CreateBarsIcon(RectTransform root, Color color)
        {
            CreateSolid("Bar0", root, 12f, 47f, 8f, 25f, color);
            CreateSolid("Bar1", root, 27f, 31f, 8f, 41f, color);
            CreateSolid("Bar2", root, 42f, 15f, 8f, 57f, color);
        }

        private static void CreateGlobeIcon(RectTransform root, Color color)
        {
            RectTransform globe = CreateTopLeft("Globe", root, 10f, 16f, 48f, 48f);
            V3RingGraphic ring = globe.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, 4f, 48);
            CreateSolid("Equator", root, 13f, 38f, 42f, 4f, color);
            CreateSolid("Axis", root, 32f, 19f, 4f, 42f, color);
        }

        private static void CreateDocumentIcon(RectTransform root, Color color)
        {
            CreateGradient(root, Color.Lerp(color, Color.white, .2f), Color.Lerp(color, Color.black, .42f), color);
            CreateSolid("Fold", root, 45f, 0f, 20f, 20f, DarkBottom);
            CreateSolid("Line0", root, 13f, 29f, 36f, 4f, DarkBottom);
            CreateSolid("Line1", root, 13f, 42f, 36f, 4f, DarkBottom);
            CreateSolid("Line2", root, 13f, 55f, 28f, 4f, DarkBottom);
        }

        private static void CreateDownloadIcon(RectTransform root, Color color)
        {
            CreateSolid("Stem", root, 14f, 0f, 6f, 26f, color);
            CreateStroke("Left", root, new Vector2(4f, 18f), new Vector2(17f, 31f), 5f, color);
            CreateStroke("Right", root, new Vector2(30f, 18f), new Vector2(17f, 31f), 5f, color);
            CreateSolid("Base", root, 3f, 35f, 28f, 5f, color);
        }

        private static void CreateChevron(RectTransform root, Color color, bool pointRight)
        {
            Vector2 tip = new(pointRight ? 22f : 1f, 9f);
            float tailX = pointRight ? 2f : 21f;
            CreateStroke("A", root, new Vector2(tailX, 1f), tip, 5f, color);
            CreateStroke("B", root, tip, new Vector2(tailX, 17f), 5f, color);
        }

        private static void CreateDownChevron(RectTransform root, Color color)
        {
            Vector2 tip = new(11.5f, 15f);
            CreateStroke("A", root, new Vector2(2f, 4f), tip, 5f, color);
            CreateStroke("B", root, tip, new Vector2(21f, 4f), 5f, color);
        }

        private static Image CreateStroke(string name, Transform parent, Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 screenDelta = end - start;
            Vector2 localDelta = new(screenDelta.x, -screenDelta.y);
            Image stroke = CreateImage(name, parent, null, color, false);
            RectTransform rect = stroke.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((start.x + end.x) * .5f, -(start.y + end.y) * .5f);
            rect.sizeDelta = new Vector2(localDelta.magnitude, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(localDelta.y, localDelta.x) * Mathf.Rad2Deg);
            return stroke;
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

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, TMP_FontAsset font,
            TextAlignmentOptions alignment, Color color)
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

        private static RectTransform CreateHorizontalStretch(string name, Transform parent, float left, float right, float y, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-(left + right), height), new Vector2(left, -y));
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

        private static void SetTopRight(RectTransform rect, float right, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(-right, -y);
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
            Transform found = root.Find(path);
            if (found == null)
                throw new MissingReferenceException($"Inbox V3 is missing {path}.");
            return found;
        }

        private static void AssignMenuScenePrefab(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                content = root.GetComponentInChildren<UIShellContentView>(true);
                if (content != null) break;
            }
            if (content == null)
                throw new InvalidOperationException("Menu scene is missing UIShellContentView.");
            SerializedObject serialized = new(content);
            SerializedProperty property = serialized.FindProperty("inboxContentPrefab");
            if (property == null)
                throw new MissingFieldException(nameof(UIShellContentView), "inboxContentPrefab");
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
                throw new FileNotFoundException($"Missing Inbox art: {path}");
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
                throw new FileNotFoundException(path);
            return sprite;
        }
    }
}
#endif
