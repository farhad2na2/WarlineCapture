using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Runtime;

namespace Game.Editor
{
    public static class BuildPlacementConfirmationBarPrefabSetupEditor
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab";
        internal const string StatusChipSpritePath = "Assets/Game/Art/UI/Panels/scn08_status_segment_strip.png";

        [MenuItem("Game/UI/Setup Build Placement Confirmation Bar Prefab")]
        public static void Setup()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BuildPlacementConfirmationBarView view = root.GetComponent<BuildPlacementConfirmationBarView>();
                if (view == null)
                    view = root.AddComponent<BuildPlacementConfirmationBarView>();

                var serialized = new SerializedObject(view);
                RectTransform rootRect = root.GetComponent<RectTransform>();
                CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = root.AddComponent<CanvasGroup>();
                Image rootImage = root.GetComponent<Image>();
                if (rootImage == null)
                    rootImage = root.AddComponent<Image>();

                Sprite panelFrame = GetSprite(serialized, "panelFrameSprite");
                Sprite statusChip = GetSprite(serialized, "statusChipSprite");
                if (statusChip == null)
                {
                    statusChip = AssetDatabase.LoadAssetAtPath<Sprite>(StatusChipSpritePath);
                    if (statusChip == null)
                        throw new MissingReferenceException($"Missing placement status-chip sprite at {StatusChipSpritePath}.");

                    SetObject(serialized, "statusChipSprite", statusChip);
                }
                Sprite secondaryButton = GetSprite(serialized, "secondaryButtonSprite");
                Sprite goldActionButton = GetSprite(serialized, "goldActionButtonSprite");
                Sprite squareButton = GetSprite(serialized, "squareButtonSprite");
                Sprite instructionStrip = GetSprite(serialized, "instructionStripSprite");
                Sprite creditsIcon = GetSprite(serialized, "creditsIconSprite");
                Sprite timeIcon = GetSprite(serialized, "timeIconSprite");
                Sprite cancelIcon = GetSprite(serialized, "cancelIconSprite");
                Sprite rotateIcon = GetSprite(serialized, "rotateIconSprite");
                Sprite confirmIcon = GetSprite(serialized, "confirmIconSprite");
                Sprite infoIcon = GetSprite(serialized, "infoIconSprite");

                ApplySprite(rootImage, panelFrame, Image.Type.Sliced);
                rootImage.raycastTarget = true;
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                TMP_Text titleText = CreateText("Title", rootRect, new Vector2(0.07f, 0.67f), new Vector2(0.39f, 0.94f), 26, TextAlignmentOptions.Left, new Color(0.96f, 0.88f, 0.67f));
                RectTransform statusChipRect = CreateImage("StatusChip", rootRect, new Vector2(0.405f, 0.68f), new Vector2(0.555f, 0.93f), statusChip, new Color(0.14f, 0.30f, 0.12f, 0.92f), false);
                TMP_Text statusText = CreateText("Status", statusChipRect, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), 17, TextAlignmentOptions.Center, new Color(0.62f, 0.98f, 0.35f));
                CreateImage("CreditsIcon", rootRect, new Vector2(0.60f, 0.70f), new Vector2(0.645f, 0.92f), creditsIcon, Color.white, false);
                TMP_Text costText = CreateText("Cost", rootRect, new Vector2(0.645f, 0.68f), new Vector2(0.735f, 0.94f), 22, TextAlignmentOptions.Left, Color.white);
                CreateImage("TimeIcon", rootRect, new Vector2(0.77f, 0.70f), new Vector2(0.815f, 0.92f), timeIcon, Color.white, false);
                TMP_Text durationText = CreateText("Duration", rootRect, new Vector2(0.815f, 0.68f), new Vector2(0.92f, 0.94f), 22, TextAlignmentOptions.Left, Color.white);

                Button cancelButton = CreateButton("CancelButton", "CANCEL", rootRect, new Vector2(0.03f, 0.27f), new Vector2(0.38f, 0.63f), secondaryButton, cancelIcon, new Color(0.18f, 0.17f, 0.15f, 0.96f));
                Button rotateButton = CreateButton("RotateButton", string.Empty, rootRect, new Vector2(0.47f, 0.24f), new Vector2(0.55f, 0.65f), squareButton, rotateIcon, new Color(0.13f, 0.14f, 0.13f, 0.92f));
                Button confirmButton = CreateButton("ConfirmButton", "CONFIRM", rootRect, new Vector2(0.65f, 0.27f), new Vector2(0.97f, 0.63f), goldActionButton, confirmIcon, new Color(0.72f, 0.48f, 0.11f, 0.98f));

                RectTransform instructionStripRect = CreateImage("InstructionStrip", rootRect, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.21f), instructionStrip, new Color(0.05f, 0.06f, 0.055f, 0.88f), false);
                CreateImage("InfoIcon", instructionStripRect, new Vector2(0.27f, 0.13f), new Vector2(0.325f, 0.88f), infoIcon, Color.white, false);
                TMP_Text instructionText = CreateText("Instruction", instructionStripRect, new Vector2(0.33f, 0.05f), new Vector2(0.83f, 0.90f), 15, TextAlignmentOptions.Left, new Color(0.80f, 0.79f, 0.72f));

                SetObject(serialized, "root", rootRect);
                SetObject(serialized, "titleText", titleText);
                SetObject(serialized, "statusText", statusText);
                SetObject(serialized, "costText", costText);
                SetObject(serialized, "durationText", durationText);
                SetObject(serialized, "instructionText", instructionText);
                SetObject(serialized, "cancelButton", cancelButton);
                SetObject(serialized, "rotateButton", rotateButton);
                SetObject(serialized, "confirmButton", confirmButton);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Sprite GetSprite(SerializedObject serialized, string propertyName)
        {
            return serialized.FindProperty(propertyName).objectReferenceValue as Sprite;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            serialized.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static TMP_Text CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = GetOrCreateRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TMP_Text text = rect.GetComponent<TMP_Text>();
            if (text == null)
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, fontSize * 0.55f);
            text.fontSizeMax = fontSize;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateImage(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite, Color color, bool raycastTarget)
        {
            RectTransform rect = GetOrCreateRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = rect.GetComponent<Image>();
            if (image == null)
                image = rect.gameObject.AddComponent<Image>();
            ApplySprite(image, sprite, Image.Type.Simple);
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = sprite != null;
            return rect;
        }

        private static Button CreateButton(string name, string label, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Sprite backgroundSprite, Sprite iconSprite, Color backgroundColor)
        {
            RectTransform rect = CreateImage(name, parent, anchorMin, anchorMax, backgroundSprite, backgroundColor, true);
            Image image = rect.GetComponent<Image>();
            ApplySprite(image, backgroundSprite, Image.Type.Sliced);

            Button button = rect.GetComponent<Button>();
            if (button == null)
                button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            if (iconSprite != null)
            {
                Vector2 iconMin = string.IsNullOrEmpty(label) ? new Vector2(0.20f, 0.18f) : new Vector2(0.13f, 0.20f);
                Vector2 iconMax = string.IsNullOrEmpty(label) ? new Vector2(0.80f, 0.82f) : new Vector2(0.30f, 0.80f);
                CreateImage("Icon", rect, iconMin, iconMax, iconSprite, Color.white, false);
            }

            if (!string.IsNullOrEmpty(label))
                CreateText("Label", rect, new Vector2(0.25f, 0.10f), new Vector2(0.93f, 0.90f), 18, TextAlignmentOptions.Center, Color.white).text = label;

            return button;
        }

        private static RectTransform GetOrCreateRect(string name, RectTransform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<RectTransform>();

            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void ApplySprite(Image image, Sprite sprite, Image.Type type)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = type;
            image.color = Color.white;
        }
    }
}
