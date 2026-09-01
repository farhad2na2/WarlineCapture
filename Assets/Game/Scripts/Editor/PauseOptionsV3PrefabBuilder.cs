#if UNITY_EDITOR
using System;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class PauseOptionsV3PrefabBuilder
    {
        internal const string PrefabPath = "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab";

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color SurfaceTop = new Color32(25, 35, 39, 253);
        private static readonly Color SurfaceBottom = new Color32(2, 8, 10, 255);
        private static readonly Color RaisedTop = new Color32(35, 47, 51, 255);
        private static readonly Color Border = new Color32(124, 142, 147, 255);
        private static readonly Color White = new Color32(242, 246, 243, 255);
        private static readonly Color Muted = new Color32(178, 190, 192, 255);
        private static readonly Color Cyan = new Color32(0, 198, 235, 255);
        private static readonly Color Green = new Color32(75, 216, 78, 255);
        private static readonly Color Amber = new Color32(255, 188, 30, 255);
        private static readonly Color Orange = new Color32(255, 79, 31, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/V3/Rebuild POP-07 Pause Options")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ClearChildren(root.transform);
                RectTransform rootRect = root.transform as RectTransform;
                Stretch(rootRect);
                rootRect.sizeDelta = Reference;

                RectTransform composition = CreateRect(
                    "V3Composition", root.transform,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    Reference, Vector2.zero);

                Image scrim = CreateSolid("Scrim", composition, new Color(0f, 0f, 0f, .58f), true);
                SetTopLeft(scrim.rectTransform, 0f, 0f, Reference.x, Reference.y);

                RectTransform shadow = CreateTopLeft("ModalShadow", composition, 463f, 119f, 760f, 672f);
                AddGradient(shadow, new Color(0f, 0f, 0f, .78f), new Color(0f, 0f, 0f, .90f), Color.clear, 0f);

                RectTransform modal = CreateTopLeft("PauseOptionsRoot", composition, 456f, 106f, 760f, 672f);
                AddGradient(modal, SurfaceTop, SurfaceBottom, Border, 3f);

                BuildHeader(modal, out Button close, out TMP_Text mission, out TMP_Text currentTime);
                BuildActionColumn(modal, out Button resume, out Button restart, out Button settings,
                    out Button help, out Button exit);
                BuildStatusColumn(modal, out TMP_Text objective, out TMP_Text squadsAlive,
                    out TMP_Text civilianRisk);
                BuildRestartConfirmation(modal, out GameObject restartConfirmation,
                    out Button restartConfirm, out Button restartCancel, out TMP_Text restartStatus);
                BuildHelpPanel(modal, out GameObject helpPanel, out Button helpClose);

                ConfigureAction(close, UiActionKind.ClosePause);
                ConfigureAction(resume, UiActionKind.ClosePause);
                ConfigureAction(settings, UiActionKind.OpenSettings);
                ConfigureAction(exit, UiActionKind.MatchMenu);

                PauseOptionsV3PopupView view = root.GetComponent<PauseOptionsV3PopupView>() ??
                    root.AddComponent<PauseOptionsV3PopupView>();
                view.Configure(
                    close, resume, restart, settings, help, exit,
                    restartConfirmation, restartConfirm, restartCancel, restartStatus,
                    helpPanel, helpClose,
                    mission, currentTime, objective, squadsAlive, civilianRisk);
                view.ShowDefault();

                MainMenuV3SectionLayoutView responsive = composition.gameObject
                    .AddComponent<MainMenuV3SectionLayoutView>();
                responsive.Configure(
                    Reference,
                    MainMenuV3SectionAlignment.Center,
                    shouldExpandToCanvasWidth: true,
                    targetsAnchoredToCenter: new[] { modal, shadow },
                    targetsExpandedAcrossWidth: new[] { scrim.rectTransform });

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[PauseOptionsV3PrefabBuilder] result=Passed actions=5 gradients=procedural borders=3 restart=runtime help=runtime icons=shared-v3");
        }

        [MenuItem("Game/UI/V3/Validate POP-07 Pause Options")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing POP-07 prefab: {PrefabPath}");

            PauseOptionsV3PopupView view = prefab.GetComponent<PauseOptionsV3PopupView>();
            if (view == null || view.CloseButton == null || view.ResumeButton == null ||
                view.RestartButton == null || view.SettingsButton == null || view.HelpButton == null ||
                view.ExitButton == null || view.RestartConfirmation == null || view.HelpPanel == null)
            {
                throw new MissingReferenceException("POP-07 runtime action and overlay bindings are incomplete.");
            }

            string[] required =
            {
                "V3Composition", "Scrim", "PauseOptionsRoot", "Header", "ActionColumn",
                "StatusColumn", "RestartConfirmation", "HelpPanel", "ObjectiveRow",
                "SquadsAliveRow", "CivilianRiskRow", "AutosaveRow"
            };
            for (int index = 0; index < required.Length; index++)
                if (Find(prefab.transform, required[index]) == null)
                    throw new MissingReferenceException($"POP-07 is missing {required[index]}.");

            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 15)
                throw new InvalidOperationException($"POP-07 requires at least 15 procedural gradients; found {gradients.Length}.");
            for (int index = 0; index < gradients.Length; index++)
            {
                SerializedObject serialized = new(gradients[index]);
                float width = serialized.FindProperty("borderWidth").floatValue;
                if (width > .001f && Mathf.Abs(width - 3f) > .001f)
                    throw new InvalidOperationException($"POP-07 {gradients[index].name} border must be exactly 3 px; found {width}.");
            }

            AssertAction(view.CloseButton, UiActionKind.ClosePause);
            AssertAction(view.ResumeButton, UiActionKind.ClosePause);
            AssertAction(view.SettingsButton, UiActionKind.OpenSettings);
            AssertAction(view.ExitButton, UiActionKind.MatchMenu);

            if (prefab.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true).Length != 1)
                throw new InvalidOperationException("POP-07 must serialize one responsive reference composition.");

            Image[] images = prefab.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                Sprite sprite = images[index].sprite;
                if (sprite == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(sprite);
                if (!path.StartsWith("Assets/Game/Art/UI/V3Shared/", StringComparison.Ordinal) &&
                    !path.StartsWith("Assets/Game/Art/UI/Generated/V3Shared/", StringComparison.Ordinal) &&
                    !path.StartsWith("Assets/Game/Art/UI/Icons/", StringComparison.Ordinal) &&
                    !path.StartsWith("Assets/Game/Art/UI/Generated/", StringComparison.Ordinal) &&
                    !path.StartsWith("Assets/Synty/InterfaceMilitaryCombatHUD/", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"POP-07 uses non-canonical sprite {path}.");
                }
            }
        }

        private static void BuildHeader(
            Transform modal,
            out Button close,
            out TMP_Text mission,
            out TMP_Text currentTime)
        {
            RectTransform header = CreateTopLeft("Header", modal, 3f, 3f, 754f, 158f);
            TMP_Text title = CreateText("TitleText", header, "PAUSED", 49f, boldFont,
                TextAlignmentOptions.Center, White);
            SetTopLeft(title.rectTransform, 90f, 12f, 574f, 64f);

            close = CreateGradientButton("CloseButton", header, 682f, 17f, 56f, 56f,
                RaisedTop, SurfaceBottom, Border, 3f);
            CreateLine("SlashA", close.transform, new Vector2(16f, 16f), new Vector2(40f, 40f), 5f, White);
            CreateLine("SlashB", close.transform, new Vector2(40f, 16f), new Vector2(16f, 40f), 5f, White);

            Image starLeft = CreateImage("MissionStarLeft", header,
                RequireSprite(V3UiFoundationBuilder.CommanderHeaderStarIconPath), Muted);
            SetTopLeft(starLeft.rectTransform, 158f, 79f, 25f, 25f);
            mission = CreateText("MissionText", header, "DOWNTOWN BREAKTHROUGH", 19f, mediumFont,
                TextAlignmentOptions.Center, Muted);
            SetTopLeft(mission.rectTransform, 188f, 75f, 378f, 33f);
            Image starRight = CreateImage("MissionStarRight", header,
                RequireSprite(V3UiFoundationBuilder.CommanderHeaderStarIconPath), Muted);
            SetTopLeft(starRight.rectTransform, 571f, 79f, 25f, 25f);

            BuildClock(header, 251f, 115f, 27f, Cyan);
            currentTime = CreateText("CurrentTimeText", header, "CURRENT TIME  14:32", 18f,
                mediumFont, TextAlignmentOptions.MidlineLeft, Muted);
            SetTopLeft(currentTime.rectTransform, 288f, 108f, 265f, 41f);
            CreateSolidTopLeft("HeaderDivider", modal, 15f, 158f, 730f, 3f, Border);
        }

        private static void BuildActionColumn(
            Transform modal,
            out Button resume,
            out Button restart,
            out Button settings,
            out Button help,
            out Button exit)
        {
            RectTransform column = CreateTopLeft("ActionColumn", modal, 16f, 174f, 447f, 482f);
            resume = BuildAction(column, "ResumeButton", 0f, "RESUME",
                V3UiFoundationBuilder.MatchMoveIconPath,
                new Color32(10, 118, 73, 255), new Color32(1, 42, 29, 255), Green, White);
            restart = BuildAction(column, "RestartButton", 96f, "RESTART MISSION",
                V3UiFoundationBuilder.ResetIconPath,
                new Color32(184, 120, 7, 255), new Color32(67, 36, 1, 255), Amber, Amber);
            settings = BuildAction(column, "SettingsButton", 192f, "OPTIONS",
                V3UiFoundationBuilder.MatchSettingsIconPath,
                new Color32(5, 105, 157, 255), new Color32(1, 34, 55, 255), Cyan, White);
            help = BuildAction(column, "HelpButton", 288f, "HELP",
                V3UiFoundationBuilder.MatchInfoIconPath,
                new Color32(7, 94, 141, 255), new Color32(1, 28, 46, 255), Cyan, White);
            exit = BuildAction(column, "ExitButton", 384f, "EXIT TO MAIN MENU",
                V3UiFoundationBuilder.MatchReturnIconPath,
                new Color32(179, 52, 12, 255), new Color32(61, 16, 5, 255), Orange, Orange);
        }

        private static Button BuildAction(
            Transform parent,
            string name,
            float y,
            string label,
            string iconPath,
            Color top,
            Color bottom,
            Color border,
            Color foreground)
        {
            Button button = CreateGradientButton(name, parent, 0f, y, 447f, 82f, top, bottom, border, 3f);
            Image icon = CreateImage("Icon", button.transform, RequireSprite(iconPath), foreground);
            SetTopLeft(icon.rectTransform, 41f, 15f, 52f, 52f);
            TMP_Text text = CreateText("LabelText", button.transform, label, 28f, boldFont,
                TextAlignmentOptions.Center, foreground);
            SetTopLeft(text.rectTransform, 105f, 6f, 318f, 69f);
            return button;
        }

        private static void BuildStatusColumn(
            Transform modal,
            out TMP_Text objective,
            out TMP_Text squadsAlive,
            out TMP_Text civilianRisk)
        {
            RectTransform status = CreateTopLeft("StatusColumn", modal, 478f, 174f, 266f, 482f);
            AddGradient(status, RaisedTop, SurfaceBottom, Border, 3f);

            BuildStatusHeading(status, "ObjectiveRow", 0f, "OBJECTIVE",
                V3UiFoundationBuilder.MatchJumpIconPath, White, out objective);
            SetTextLayout(objective, 14f, 38f, 238f, 41f, "Capture the Enemy HQ", 17f, White);
            TMP_Text progress = CreateText("ObjectiveState", status, "IN PROGRESS", 16f, boldFont,
                TextAlignmentOptions.MidlineLeft, Green);
            SetTopLeft(progress.rectTransform, 60f, 76f, 190f, 31f);
            Image objectiveIcon = CreateImage("ObjectiveStateIcon", status,
                RequireSprite(V3UiFoundationBuilder.MatchJumpIconPath), Green);
            SetTopLeft(objectiveIcon.rectTransform, 16f, 73f, 34f, 34f);
            CreateDivider(status, 114f);

            RectTransform squadRow = CreateTopLeft("SquadsAliveRow", status, 0f, 114f, 266f, 126f);
            TMP_Text squadHeading = CreateText("SquadsAliveHeading", squadRow, "SQUADS ALIVE", 18f,
                boldFont, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(squadHeading.rectTransform, 14f, 8f, 238f, 31f);
            Image squadIcon = CreateImage("SquadsIcon", squadRow,
                RequireSprite(V3UiFoundationBuilder.CampaignSquadIconPath), White);
            SetTopLeft(squadIcon.rectTransform, 15f, 45f, 42f, 48f);
            squadsAlive = CreateText("SquadsAliveText", squadRow, "5 / 5", 23f, boldFont,
                TextAlignmentOptions.MidlineLeft, Green);
            SetTopLeft(squadsAlive.rectTransform, 71f, 50f, 171f, 40f);
            BuildMeter(squadRow, 70f, 91f, 176f, Green, 5, 5);
            CreateDivider(status, 240f);

            RectTransform riskRow = CreateTopLeft("CivilianRiskRow", status, 0f, 243f, 266f, 105f);
            TMP_Text riskHeading = CreateText("CivilianRiskHeading", riskRow, "CIVILIAN RISK", 18f,
                boldFont, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(riskHeading.rectTransform, 14f, 5f, 238f, 31f);
            Image civilianIcon = CreateImage("CivilianIcon", riskRow,
                RequireSprite(V3UiFoundationBuilder.MatchCiviliansIconPath), Amber);
            SetTopLeft(civilianIcon.rectTransform, 15f, 45f, 42f, 42f);
            civilianRisk = CreateText("CivilianRiskText", riskRow, "MEDIUM", 22f, boldFont,
                TextAlignmentOptions.MidlineLeft, Amber);
            SetTopLeft(civilianRisk.rectTransform, 70f, 43f, 173f, 46f);
            CreateDivider(status, 348f);

            RectTransform autosaveRow = CreateTopLeft("AutosaveRow", status, 0f, 351f, 266f, 128f);
            TMP_Text saveHeading = CreateText("AutosaveHeading", autosaveRow, "AUTOSAVE", 18f,
                boldFont, TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(saveHeading.rectTransform, 14f, 5f, 238f, 31f);
            BuildSaveIcon(autosaveRow, 17f, 49f, 39f, Green);
            TMP_Text saveState = CreateText("AutosaveState", autosaveRow, "LAST SAVE", 17f, boldFont,
                TextAlignmentOptions.MidlineLeft, Green);
            SetTopLeft(saveState.rectTransform, 70f, 44f, 174f, 29f);
            TMP_Text saveTime = CreateText("AutosaveTime", autosaveRow, "14:30", 17f, mediumFont,
                TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(saveTime.rectTransform, 70f, 71f, 174f, 30f);
        }

        private static void BuildStatusHeading(
            Transform parent,
            string rowName,
            float y,
            string label,
            string iconPath,
            Color color,
            out TMP_Text detail)
        {
            RectTransform row = CreateTopLeft(rowName, parent, 0f, y, 266f, 114f);
            TMP_Text title = CreateText("Heading", row, label, 18f, boldFont,
                TextAlignmentOptions.MidlineLeft, color);
            SetTopLeft(title.rectTransform, 14f, 7f, 238f, 31f);
            detail = CreateText("Detail", row, string.Empty, 17f, mediumFont,
                TextAlignmentOptions.MidlineLeft, color);
        }

        private static void BuildRestartConfirmation(
            Transform modal,
            out GameObject panelObject,
            out Button confirm,
            out Button cancel,
            out TMP_Text status)
        {
            RectTransform panel = CreateTopLeft("RestartConfirmation", modal, 16f, 174f, 728f, 482f);
            AddGradient(panel, new Color32(38, 32, 22, 255), SurfaceBottom, Amber, 3f);
            panelObject = panel.gameObject;

            Image icon = CreateImage("RestartIcon", panel,
                RequireSprite(V3UiFoundationBuilder.ResetIconPath), Amber);
            SetTopLeft(icon.rectTransform, 320f, 42f, 88f, 88f);
            TMP_Text title = CreateText("Title", panel, "RESTART MISSION", 36f, boldFont,
                TextAlignmentOptions.Center, White);
            SetTopLeft(title.rectTransform, 54f, 143f, 620f, 58f);
            status = CreateText("RestartStatusText", panel,
                "RESTART THE CURRENT MISSION FROM THE BEGINNING?", 22f, mediumFont,
                TextAlignmentOptions.Center, Muted);
            SetTopLeft(status.rectTransform, 52f, 210f, 624f, 92f);

            cancel = CreateGradientButton("RestartCancelButton", panel, 38f, 362f, 303f, 85f,
                RaisedTop, SurfaceBottom, Border, 3f);
            TMP_Text cancelText = CreateText("LabelText", cancel.transform, "CANCEL", 28f, boldFont,
                TextAlignmentOptions.Center, White);
            Stretch(cancelText.rectTransform, 8f, 8f);

            confirm = CreateGradientButton("RestartConfirmButton", panel, 387f, 362f, 303f, 85f,
                new Color32(187, 120, 8, 255), new Color32(64, 34, 1, 255), Amber, 3f);
            TMP_Text confirmText = CreateText("LabelText", confirm.transform, "RESTART", 28f, boldFont,
                TextAlignmentOptions.Center, Amber);
            Stretch(confirmText.rectTransform, 8f, 8f);
        }

        private static void BuildHelpPanel(
            Transform modal,
            out GameObject panelObject,
            out Button close)
        {
            RectTransform panel = CreateTopLeft("HelpPanel", modal, 16f, 174f, 728f, 482f);
            AddGradient(panel, new Color32(16, 42, 55, 255), SurfaceBottom, Cyan, 3f);
            panelObject = panel.gameObject;

            Image icon = CreateImage("HelpIcon", panel,
                RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath), Cyan);
            SetTopLeft(icon.rectTransform, 25f, 20f, 54f, 54f);
            TMP_Text title = CreateText("Title", panel, "FIELD CONTROLS", 31f, boldFont,
                TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(title.rectTransform, 95f, 15f, 520f, 63f);

            BuildHelpRow(panel, 104f, V3UiFoundationBuilder.MatchSelectIconPath,
                "SELECT", "Tap a unit or drag over a squad.");
            BuildHelpRow(panel, 174f, V3UiFoundationBuilder.MatchMoveIconPath,
                "MOVE", "Select Move, then tap the destination.");
            BuildHelpRow(panel, 244f, V3UiFoundationBuilder.MatchAttackIconPath,
                "ATTACK", "Select Attack, then tap a hostile target.");
            BuildHelpRow(panel, 314f, V3UiFoundationBuilder.MatchHoldIconPath,
                "COMMAND WHEEL", "Hold on a selected unit to open quick commands.");

            close = CreateGradientButton("HelpCloseButton", panel, 499f, 388f, 196f, 67f,
                new Color32(7, 105, 157, 255), new Color32(1, 34, 55, 255), Cyan, 3f);
            TMP_Text closeText = CreateText("LabelText", close.transform, "BACK", 24f, boldFont,
                TextAlignmentOptions.Center, White);
            Stretch(closeText.rectTransform, 6f, 6f);
        }

        private static void BuildHelpRow(Transform parent, float y, string iconPath, string title, string body)
        {
            RectTransform row = CreateTopLeft(title + "HelpRow", parent, 25f, y, 678f, 57f);
            Image icon = CreateImage("Icon", row, RequireSprite(iconPath), Cyan);
            SetTopLeft(icon.rectTransform, 0f, 3f, 48f, 48f);
            TMP_Text titleText = CreateText("Title", row, title, 18f, boldFont,
                TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(titleText.rectTransform, 65f, 0f, 170f, 28f);
            TMP_Text bodyText = CreateText("Body", row, body, 16f, mediumFont,
                TextAlignmentOptions.MidlineLeft, Muted);
            SetTopLeft(bodyText.rectTransform, 65f, 25f, 595f, 29f);
        }

        private static void BuildMeter(Transform parent, float x, float y, float width, Color color, int filled, int count)
        {
            float segmentWidth = width / count;
            for (int index = 0; index < count; index++)
            {
                Color fill = index < filled ? color : (Color)new Color32(30, 40, 43, 255);
                CreateSolidTopLeft("Segment_" + index, parent, x + index * segmentWidth, y,
                    segmentWidth - 4f, 14f, fill);
            }
        }

        private static void BuildClock(Transform parent, float x, float y, float size, Color color)
        {
            RectTransform clock = CreateTopLeft("ClockIcon", parent, x, y, size, size);
            V3RingGraphic ring = clock.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, 3f, 40);
            CreateLine("Minute", clock, new Vector2(size * .5f, size * .5f),
                new Vector2(size * .5f, size * .20f), 3f, color);
            CreateLine("Hour", clock, new Vector2(size * .5f, size * .5f),
                new Vector2(size * .73f, size * .63f), 3f, color);
        }

        private static void BuildSaveIcon(Transform parent, float x, float y, float size, Color color)
        {
            RectTransform icon = CreateTopLeft("AutosaveIcon", parent, x, y, size, size);
            CreateSolidTopLeft("Body", icon, 2f, 2f, size - 4f, size - 4f, color);
            CreateSolidTopLeft("Label", icon, 9f, 3f, size - 18f, 12f, SurfaceBottom);
            CreateSolidTopLeft("Slot", icon, 9f, 24f, size - 18f, 10f, SurfaceBottom);
        }

        private static void CreateDivider(Transform parent, float y) =>
            CreateSolidTopLeft("Divider", parent, 3f, y, 260f, 3f, Border);

        private static void SetTextLayout(
            TMP_Text target, float x, float y, float width, float height,
            string value, float size, Color color)
        {
            target.text = value;
            target.fontSize = size;
            target.color = color;
            SetTopLeft(target.rectTransform, x, y, width, height);
        }

        private static Button CreateGradientButton(
            string name, Transform parent, float x, float y, float width, float height,
            Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = AddGradient(rect, top, bottom, border, borderWidth);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static void ConfigureAction(Button button, UiActionKind kind)
        {
            UIShellActionButtonView view = button.gameObject.GetComponent<UIShellActionButtonView>() ??
                button.gameObject.AddComponent<UIShellActionButtonView>();
            SerializedObject serialized = new(view);
            serialized.FindProperty("actionKind").enumValueIndex = (int)kind;
            serialized.FindProperty("payloadId").intValue = 0;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertAction(Button button, UiActionKind expected)
        {
            UIShellActionButtonView action = button.GetComponent<UIShellActionButtonView>();
            if (action == null || action.ActionKind != expected || action.PayloadId != 0)
                throw new InvalidOperationException($"{button.name} must dispatch {expected}.");
        }

        private static V3GradientGraphic AddGradient(
            RectTransform rect, Color top, Color bottom, Color border, float borderWidth)
        {
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            graphic.raycastTarget = false;
            return graphic;
        }

        private static TMP_Text CreateText(
            string name, Transform parent, string value, float size, TMP_FontAsset font,
            TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(100f, 40f), Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyles.Normal;
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(100f, 100f), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolid(string name, Transform parent, Color color, bool raycast)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(100f, 100f), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Image CreateSolidTopLeft(
            string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateSolid(name, parent, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static void CreateLine(
            string name, Transform parent, Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 delta = end - start;
            Image line = CreateSolidTopLeft(name, parent, 0f, 0f, delta.magnitude, thickness, color);
            RectTransform rect = line.rectTransform;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((start.x + end.x) * .5f, -(start.y + end.y) * .5f);
            rect.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static RectTransform CreateTopLeft(
            string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static RectTransform CreateRect(
            string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float insetX = 0f, float insetY = 0f)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-insetX * 2f, -insetY * 2f);
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

        private static void ClearChildren(Transform root)
        {
            for (int index = root.childCount - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(index).gameObject);
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing POP-07 shared sprite: {path}");
            return sprite;
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            if (boldFont == null || mediumFont == null)
                throw new FileNotFoundException("POP-07 requires the shared Oxanium font assets.");
        }
    }
}
#endif
