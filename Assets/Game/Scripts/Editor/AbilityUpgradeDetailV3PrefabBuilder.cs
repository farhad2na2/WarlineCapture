#if UNITY_EDITOR
using System;
using System.IO;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class AbilityUpgradeDetailV3PrefabBuilder
    {
        internal const string PrefabPath =
            "Assets/Game/Prefabs/UI/Popups/AbilityUpgradeDetailPopup.prefab";
        private const string ArmoryPrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab";
        internal const string ApcArtPath =
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_APC_Heavy_Card_512.png";
        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string LockIconPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Sprites/Icons_Map/ICON_MilitaryCombat_Map_Lock_01_Clean.png";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(27, 36, 41, 253);
        private static readonly Color DarkBottom = new Color32(2, 8, 11, 255);
        private static readonly Color RaisedTop = new Color32(40, 49, 53, 255);
        private static readonly Color RaisedBottom = new Color32(11, 17, 20, 255);
        private static readonly Color Line = new Color32(91, 106, 110, 255);
        private static readonly Color White = new Color32(239, 242, 238, 255);
        private static readonly Color Muted = new Color32(177, 183, 182, 255);
        private static readonly Color Cyan = new Color32(21, 184, 236, 255);
        private static readonly Color Lime = new Color32(140, 201, 44, 255);
        private static readonly Color Red = new Color32(222, 45, 52, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static Texture2D apcTexture;

        [MenuItem("Game/UI/V3/Rebuild POP-09 Ability Upgrade Detail")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            GameObject root = new("AbilityUpgradeDetailPopup", typeof(RectTransform));
            try
            {
                Stretch(root.GetComponent<RectTransform>());
                UIPopupFrameView popup = root.AddComponent<UIPopupFrameView>();
                AbilityUpgradeDetailV3PopupView state =
                    root.AddComponent<AbilityUpgradeDetailV3PopupView>();

                Image scrim = CreateSolid(
                    "Scrim", root.transform, new Color(0f, .01f, .018f, .66f), true);
                Stretch(scrim.rectTransform);
                RectTransform composition = CreateTopLeft(
                    "V3Composition", root.transform, 0f, 0f, Reference.x, Reference.y);
                RectTransform shadow = CreatePanel(
                    "FrameShadow", composition, 269f, 100f, 1130f, 803f,
                    new Color(0f, 0f, 0f, .55f), new Color(0f, 0f, 0f, .88f), Color.clear, 0f);

                FrameBindings bindings = BuildFrame(composition);
                composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                    Reference,
                    MainMenuV3SectionAlignment.Center,
                    shouldExpandToCanvasWidth: true,
                    targetsAnchoredToCenter: new[] { shadow, bindings.Frame },
                    targetsExpandedAcrossWidth: new[] { scrim.rectTransform });

                popup.Configure(
                    scrim.gameObject,
                    bindings.Frame.gameObject,
                    bindings.Header.gameObject,
                    bindings.Title,
                    bindings.Close,
                    bindings.Body,
                    bindings.Footer);
                state.Configure(bindings.ViewSource, bindings.Unlock);
                state.SetUnlocked(false);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[AbilityUpgradeDetailV3PrefabBuilder] result=Passed layout=target gradients=procedural borders=3 apc=reused-aspect-preserved actions=bound");
        }

        [MenuItem("Game/UI/V3/Capture POP-09 Ability Upgrade Detail Review")]
        public static void CaptureReview()
        {
            Build();
            Capture("/private/tmp/warline-ability-upgrade-v3-16x9.png", 1920, 1080);
            Capture("/private/tmp/warline-ability-upgrade-v3-20x9.png", 4800, 2160);
        }

        [MenuItem("Game/UI/V3/Validate POP-09 Ability Upgrade Detail")]
        public static void Validate()
        {
            if (apcTexture == null)
                LoadAssets();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponent<UIPopupFrameView>() == null ||
                prefab.GetComponent<AbilityUpgradeDetailV3PopupView>() == null)
                throw new MissingReferenceException("POP-09 runtime popup bindings are incomplete.");

            MainMenuV3SectionLayoutView layout =
                prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || layout.ReferenceResolution != Reference || !layout.ExpandToCanvasWidth)
                throw new InvalidOperationException("POP-09 must use the responsive 1672x941 composition.");

            RawImage apc = Find(prefab.transform, "ApcArtImage")?.GetComponent<RawImage>();
            AspectRatioFitter fitter = apc != null ? apc.GetComponent<AspectRatioFitter>() : null;
            if (apc == null || apc.texture != apcTexture || fitter == null ||
                fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("POP-09 must reuse the existing APC portrait without stretching.");

            AbilityUpgradeDetailV3PopupView state =
                prefab.GetComponent<AbilityUpgradeDetailV3PopupView>();
            if (state.ViewSourceButton == null || state.UnlockButton == null ||
                state.UnlockButton.interactable)
                throw new InvalidOperationException("POP-09 action state must expose View Source and a locked upgrade action.");

            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 17)
                throw new InvalidOperationException($"POP-09 requires layered procedural gradients; found {gradients}.");
            foreach (V3GradientGraphic gradient in prefab.GetComponentsInChildren<V3GradientGraphic>(true))
            {
                SerializedObject serialized = new(gradient);
                float width = serialized.FindProperty("borderWidth").floatValue;
                Color color = serialized.FindProperty("borderColor").colorValue;
                if (color.a > .01f && !Mathf.Approximately(width, 3f))
                    throw new InvalidOperationException($"POP-09 border on {gradient.name} is {width}px; every visible border must be 3px.");
            }

            Debug.Log($"[AbilityUpgradeDetailV3PrefabBuilder] validation=Passed gradients={gradients} borders=3 apc=reused actions=2");
        }

        private static FrameBindings BuildFrame(Transform parent)
        {
            RectTransform frame = CreatePanel(
                "Frame", parent, 279f, 108f, 1110f, 783f,
                DarkTop, DarkBottom, Line, 3f);
            RectTransform header = CreateTopLeft("Header", frame, 3f, 3f, 1104f, 63f);
            CreatePanel("HeaderFill", header, 0f, 0f, 1104f, 60f,
                new Color32(32, 41, 46, 255), new Color32(7, 13, 16, 255), Color.clear, 0f);
            TMP_Text title = CreateText(
                header, "TitleText", 31f, 4f, 650f, 53f,
                "ABILITY / UPGRADE DETAIL", 31f, White, TextAlignmentOptions.MidlineLeft, true);
            Button close = BuildTextButton(
                header, "CloseButton", 1038f, 9f, 55f, 45f,
                RaisedTop, RaisedBottom, Line, "X", 31f, White);
            CreateSolid("HeaderDivider", header, 0f, 60f, 1104f, 3f, Line);

            RectTransform body = CreateTopLeft("BodyRoot", frame, 0f, 0f, 1110f, 783f);
            BuildApcPanel(body);
            BuildEffectsPanel(body);
            FooterBindings footer = BuildFooter(body);
            return new FrameBindings(
                frame, header, body, footer.Root, title, close,
                footer.ViewSource, footer.Unlock);
        }

        private static void BuildApcPanel(Transform parent)
        {
            RectTransform panel = CreatePanel(
                "ApcUpgradePanel", parent, 18f, 65f, 535f, 570f,
                new Color32(20, 29, 34, 255), DarkBottom, Line, 3f);
            CreateText(panel, "UpgradeTitleText", 18f, 10f, 497f, 57f,
                "APC ARMOR UPGRADE", 42f, White, TextAlignmentOptions.MidlineLeft, true);
            Image chevrons = CreateImage(
                "UpgradeTrackIcon", panel,
                RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath), Cyan);
            SetTopLeft(chevrons.rectTransform, 20f, 72f, 38f, 43f);
            CreateText(panel, "UpgradeTypeText", 65f, 73f, 238f, 40f,
                "Upgrade Track", 24f, Cyan, TextAlignmentOptions.MidlineLeft, false);

            RectTransform artClip = CreateTopLeft("ApcArtViewport", panel, 3f, 119f, 529f, 316f);
            artClip.gameObject.AddComponent<RectMask2D>();
            RawImage art = CreateRawImage("ApcArtImage", artClip, apcTexture, Color.white);
            Stretch(art.rectTransform);
            AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = apcTexture.width / (float)apcTexture.height;
            Image shade = CreateSolid(
                "ApcArtShade", artClip, new Color(0f, .02f, .03f, .08f), false);
            Stretch(shade.rectTransform);
            CreateSolid("TargetDivider", panel, 3f, 435f, 529f, 3f, Line);

            CreateText(panel, "TargetIdLabel", 20f, 454f, 170f, 34f,
                "TARGET ID", 20f, Muted, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "TargetIdText", 20f, 493f, 430f, 48f,
                "upgrade.vehicle.apc_armor", 25f, White, TextAlignmentOptions.MidlineLeft, false);
            RectTransform targetFrame = CreatePanel(
                "TargetIconFrame", panel, 446f, 446f, 69f, 70f,
                new Color32(10, 42, 56, 255), DarkBottom, Line, 3f);
            Image target = CreateImage(
                "TargetIcon", targetFrame,
                RequireSprite(V3UiFoundationBuilder.MatchJumpIconPath), Cyan);
            SetTopLeft(target.rectTransform, 10f, 10f, 49f, 49f);
        }

        private static void BuildEffectsPanel(Transform parent)
        {
            RectTransform panel = CreatePanel(
                "EffectsPanel", parent, 553f, 65f, 539f, 570f,
                new Color32(17, 25, 29, 255), DarkBottom, Line, 3f);
            CreateText(panel, "EffectsTitleText", 15f, 10f, 295f, 38f,
                "ABILITY EFFECTS", 25f, Muted, TextAlignmentOptions.MidlineLeft, true);
            BuildEffectRow(
                panel, "ArmorHealthEffect", 54f,
                V3UiFoundationBuilder.MatchArmorIconPath,
                "Armor Health", "Increases total armor hit points.", "+20%", Cyan);
            BuildEffectRow(
                panel, "DamageResistanceEffect", 140f,
                V3UiFoundationBuilder.AttackIconPath,
                "Damage Resistance", "Reduces incoming damage from all sources.", "+15%", White);
            BuildEffectRow(
                panel, "MovementSpeedEffect", 226f,
                V3UiFoundationBuilder.MatchSpeedIconPath,
                "Movement Speed", "Improves APC movement speed.", "+10%", White);
            BuildAvailabilityRequirements(panel);
            BuildPrerequisiteRow(panel);
            BuildTierRow(panel);
        }

        private static void BuildEffectRow(
            Transform parent,
            string name,
            float y,
            string iconPath,
            string label,
            string description,
            string value,
            Color iconColor)
        {
            RectTransform row = CreatePanel(
                name, parent, 15f, y, 509f, 80f,
                RaisedTop, RaisedBottom, Line, 3f);
            Image icon = CreateImage("Icon", row, RequireSprite(iconPath), iconColor);
            SetTopLeft(icon.rectTransform, 15f, 15f, 51f, 51f);
            CreateText(row, "LabelText", 80f, 10f, 280f, 30f,
                label, 22f, White, TextAlignmentOptions.MidlineLeft, false);
            CreateText(row, "DescriptionText", 80f, 40f, 318f, 28f,
                description, 16f, Muted, TextAlignmentOptions.MidlineLeft, false);
            CreateText(row, "ValueText", 402f, 13f, 91f, 52f,
                value, 30f, Lime, TextAlignmentOptions.MidlineRight, true);
        }

        private static void BuildAvailabilityRequirements(Transform parent)
        {
            RectTransform row = CreatePanel(
                "AvailabilityRequirementsRow", parent, 15f, 320f, 509f, 122f,
                new Color32(20, 28, 32, 255), DarkBottom, Line, 3f);
            CreateSolid("CenterDivider", row, 254f, 3f, 3f, 116f, Line);

            Image pin = CreateImage(
                "AvailabilityIcon", row,
                RequireSprite(V3UiFoundationBuilder.OperationsMapPinIconPath), Muted);
            SetTopLeft(pin.rectTransform, 14f, 14f, 31f, 31f);
            CreateText(row, "AvailabilityLabel", 52f, 11f, 184f, 36f,
                "AVAILABILITY", 20f, Muted, TextAlignmentOptions.MidlineLeft, true);
            CreateText(row, "AvailabilityValue", 14f, 57f, 220f, 53f,
                "Loadout, Briefing,\nHUD, Intel Reveal, Store", 18f, Cyan,
                TextAlignmentOptions.TopLeft, false, true);

            Image requirements = CreateImage(
                "RequirementsIcon", row,
                RequireSprite(V3UiFoundationBuilder.SettingsIconPath), Muted);
            SetTopLeft(requirements.rectTransform, 272f, 13f, 34f, 34f);
            CreateText(row, "RequirementsLabel", 315f, 11f, 177f, 36f,
                "REQUIREMENTS", 20f, Muted, TextAlignmentOptions.MidlineLeft, true);
            CreateText(row, "PartsValueText", 272f, 52f, 116f, 37f,
                "18 / 40", 25f, White, TextAlignmentOptions.MidlineLeft, false);
            CreateText(row, "PartsCurrentText", 272f, 52f, 35f, 37f,
                "18", 25f, Red, TextAlignmentOptions.MidlineLeft, true);
            CreatePanel("PartsTrack", row, 272f, 91f, 220f, 16f,
                new Color32(31, 39, 42, 255), new Color32(12, 17, 19, 255), Color.clear, 0f);
            CreatePanel("PartsFill", row, 272f, 91f, 99f, 16f,
                new Color32(226, 51, 57, 255), new Color32(137, 21, 28, 255), Color.clear, 0f);
        }

        private static void BuildPrerequisiteRow(Transform parent)
        {
            RectTransform row = CreatePanel(
                "PrerequisiteRow", parent, 15f, 448f, 509f, 52f,
                RaisedTop, RaisedBottom, Line, 3f);
            Image icon = CreateImage(
                "Icon", row,
                RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath), Muted);
            SetTopLeft(icon.rectTransform, 15f, 11f, 28f, 30f);
            CreateText(row, "LabelText", 53f, 7f, 132f, 38f,
                "PREREQUISITE", 17f, Muted, TextAlignmentOptions.MidlineLeft, true);
            CreateText(row, "ValueText", 185f, 7f, 200f, 38f,
                "APC PLATFORM UPGRADE", 16f, Cyan, TextAlignmentOptions.MidlineLeft, true);
            CreateText(row, "CompleteText", 389f, 7f, 91f, 38f,
                "COMPLETED", 15f, Lime, TextAlignmentOptions.MidlineRight, true);
            CreateCheckMark(row, 484f, 17f, Lime);
        }

        private static void BuildTierRow(Transform parent)
        {
            RectTransform row = CreatePanel(
                "CurrentTierRow", parent, 15f, 506f, 509f, 52f,
                RaisedTop, RaisedBottom, Line, 3f);
            Image icon = CreateImage(
                "Icon", row,
                RequireSprite(V3UiFoundationBuilder.MatchRankBadgeIconPath), Muted);
            SetTopLeft(icon.rectTransform, 15f, 10f, 29f, 31f);
            CreateText(row, "LabelText", 53f, 7f, 152f, 38f,
                "CURRENT TIER", 17f, Muted, TextAlignmentOptions.MidlineLeft, true);
            CreateText(row, "ValueText", 205f, 7f, 132f, 38f,
                "TIER 2 / 5", 17f, Cyan, TextAlignmentOptions.MidlineLeft, true);
        }

        private static FooterBindings BuildFooter(Transform parent)
        {
            RectTransform root = CreateTopLeft("ButtonRow", parent, 18f, 653f, 1074f, 112f);
            Button viewSource = BuildIconButton(
                root, "ViewSourceButton", 0f, 0f, 440f, 104f,
                new Color32(8, 100, 175, 255), new Color32(1, 43, 86, 255), Cyan,
                V3UiFoundationBuilder.OperationsIntelIconPath,
                "VIEW SOURCE", string.Empty, White);
            Button unlock = BuildIconButton(
                root, "UnlockButton", 455f, 0f, 619f, 104f,
                new Color32(71, 127, 32, 255), new Color32(29, 70, 18, 255), Lime,
                LockIconPath,
                "NOT YET UNLOCKED", "Complete Requirements to Unlock", White);
            unlock.interactable = false;
            unlock.transition = Selectable.Transition.None;
            return new FooterBindings(root, viewSource, unlock);
        }

        private static Button BuildIconButton(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color top,
            Color bottom,
            Color border,
            string iconPath,
            string label,
            string sublabel,
            Color textColor)
        {
            RectTransform rect = CreatePanel(name, parent, x, y, width, height, top, bottom, border, 3f);
            V3GradientGraphic gradient = rect.GetComponent<V3GradientGraphic>();
            gradient.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = gradient;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.86f, .98f, 1f, 1f);
            colors.pressedColor = new Color(.62f, .82f, .92f, 1f);
            button.colors = colors;
            Image icon = CreateImage("Icon", rect, RequireSprite(iconPath), White);
            SetTopLeft(icon.rectTransform, 69f, 25f, 54f, 54f);
            float textX = 142f;
            CreateText(rect, "LabelText", textX, sublabel.Length == 0 ? 18f : 13f,
                width - textX - 18f, sublabel.Length == 0 ? 68f : 48f,
                label, sublabel.Length == 0 ? 29f : 27f, textColor,
                TextAlignmentOptions.MidlineLeft, true);
            if (sublabel.Length > 0)
            {
                CreateText(rect, "SublabelText", textX, 58f, width - textX - 18f, 33f,
                    sublabel, 18f, textColor, TextAlignmentOptions.MidlineLeft, false);
            }
            return button;
        }

        private static Button BuildTextButton(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color top,
            Color bottom,
            Color border,
            string label,
            float size,
            Color textColor)
        {
            RectTransform rect = CreatePanel(name, parent, x, y, width, height, top, bottom, border, 3f);
            V3GradientGraphic gradient = rect.GetComponent<V3GradientGraphic>();
            gradient.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = gradient;
            CreateText(rect, "LabelText", 4f, 2f, width - 8f, height - 4f,
                label, size, textColor, TextAlignmentOptions.Center, true);
            return button;
        }

        private static RectTransform CreatePanel(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Color top,
            Color bottom,
            Color border,
            float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, border, borderWidth);
            gradient.raycastTarget = false;
            return rect;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            string value,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            bool bold,
            bool wrap = false)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = bold ? boldFont : mediumFont;
            text.fontSize = size;
            text.fontWeight = bold ? FontWeight.Bold : FontWeight.Regular;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(9f, size * .72f);
            text.fontSizeMax = size;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateRawImage(
            string name, Transform parent, Texture texture, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolid(
            string name, Transform parent, Color color, bool raycast)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static void CreateCheckMark(Transform parent, float x, float y, Color color)
        {
            Image shortStroke = CreateSolid("CompleteCheckShort", parent, color, false);
            SetTopLeft(shortStroke.rectTransform, x, y + 9f, 10f, 3f);
            shortStroke.rectTransform.pivot = new Vector2(.5f, .5f);
            shortStroke.rectTransform.localEulerAngles = new Vector3(0f, 0f, -43f);

            Image longStroke = CreateSolid("CompleteCheckLong", parent, color, false);
            SetTopLeft(longStroke.rectTransform, x + 6f, y + 5f, 18f, 3f);
            longStroke.rectTransform.pivot = new Vector2(.5f, .5f);
            longStroke.rectTransform.localEulerAngles = new Vector3(0f, 0f, 47f);
        }

        private static Image CreateSolid(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            Image image = CreateSolid(name, parent, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static RectTransform CreateTopLeft(
            string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = V3UiPrefabFactory.CreateRect(
                name,
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(width, height),
                new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static void SetTopLeft(
            RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = Find(root.GetChild(index), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing POP-09 V3 sprite: {path}");
            return sprite;
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            apcTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ApcArtPath);
            if (boldFont == null || mediumFont == null || apcTexture == null)
                throw new FileNotFoundException("POP-09 V3 fonts or existing APC portrait are missing.");
        }

        private static void Capture(string outputPath, int width, int height)
        {
            GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject armoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArmoryPrefabPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("AbilityUpgradeV3CaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = height * .5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            GameObject canvasObject = new(
                "AbilityUpgradeV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
            scaler.matchWidthOrHeight = .5f;

            if (armoryPrefab != null)
            {
                GameObject background = UnityEngine.Object.Instantiate(armoryPrefab, canvasRect);
                Stretch(background.transform as RectTransform);
            }
            GameObject popup = UnityEngine.Object.Instantiate(popupPrefab, canvasRect);
            Stretch(popup.transform as RectTransform);
            Canvas.ForceUpdateCanvases();
            foreach (MainMenuV3SectionLayoutView layout in
                     canvasObject.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                layout.RefreshLayout();
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
                Debug.Log($"[AbilityUpgradeDetailV3PrefabBuilder] capture=Passed size={width}x{height} path={outputPath} scene={scene.name}");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(capture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private readonly struct FooterBindings
        {
            public FooterBindings(RectTransform root, Button viewSource, Button unlock)
            {
                Root = root;
                ViewSource = viewSource;
                Unlock = unlock;
            }

            public RectTransform Root { get; }
            public Button ViewSource { get; }
            public Button Unlock { get; }
        }

        private readonly struct FrameBindings
        {
            public FrameBindings(
                RectTransform frame,
                RectTransform header,
                RectTransform body,
                RectTransform footer,
                TMP_Text title,
                Button close,
                Button viewSource,
                Button unlock)
            {
                Frame = frame;
                Header = header;
                Body = body;
                Footer = footer;
                Title = title;
                Close = close;
                ViewSource = viewSource;
                Unlock = unlock;
            }

            public RectTransform Frame { get; }
            public RectTransform Header { get; }
            public RectTransform Body { get; }
            public RectTransform Footer { get; }
            public TMP_Text Title { get; }
            public Button Close { get; }
            public Button ViewSource { get; }
            public Button Unlock { get; }
        }
    }
}
#endif
