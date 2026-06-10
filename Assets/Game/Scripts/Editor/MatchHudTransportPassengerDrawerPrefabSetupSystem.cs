using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MatchHudTransportPassengerDrawerPrefabSetupSystem
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string PanelFrameName = "Frame";
    private const string ChipName = "PassengerChip";
    private const string DrawerName = "TransportPassengerDrawer";

    public static void Apply()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            MatchHudSelectionPanelView selectionPanel = root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            if (selectionPanel == null)
                throw new System.InvalidOperationException("SCN08_MatchHudContent is missing MatchHudSelectionPanelView.");

            GameObject selectedSquadPanel = GetSerializedReference<GameObject>(selectionPanel, "selectedSquadPanel");
            Transform panelFrame = selectedSquadPanel != null
                ? FindDirectChild(selectedSquadPanel.transform, PanelFrameName)
                : null;
            if (panelFrame == null)
                throw new System.InvalidOperationException("SCN08_MatchHudContent is missing SelectedSquadPanel/Frame.");

            DestroyExisting(panelFrame, ChipName);
            DestroyExisting(panelFrame, DrawerName);

            TMP_Text titleText = GetSerializedReference<TMP_Text>(selectionPanel, "titleText");
            Sprite chipSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01/scn08_ability_chip_frame.png");
            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01/scn08_selected_entity_panel_frame.png");
            Sprite healthSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01/scn08_health_bar_small_frame.png");
            Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01/scn08_small_square_button_frame.png");

            GameObject chip = CreateImageObject(ChipName, panelFrame, chipSprite, new Color(1f, 1f, 1f, 1f), true);
            RectTransform chipRect = chip.GetComponent<RectTransform>();
            ConfigureRect(chipRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -278f), new Vector2(270f, 54f));
            Button chipButton = chip.AddComponent<Button>();
            chipButton.targetGraphic = chip.GetComponent<Image>();
            TMP_Text chipLabel = CreateText("Label", chip.transform, "PASSENGERS 0/0", titleText, 26, TextAlignmentOptions.Center);
            ConfigureRect(chipLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject drawer = CreateImageObject(DrawerName, panelFrame, panelSprite, new Color(1f, 1f, 1f, 0.96f), true);
            drawer.SetActive(false);
            RectTransform drawerRect = drawer.GetComponent<RectTransform>();
            ConfigureRect(drawerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(375f, -35f), new Vector2(380f, 430f));
            MatchHudTransportPassengerDrawerView drawerView = drawer.AddComponent<MatchHudTransportPassengerDrawerView>();

            TMP_Text header = CreateText("Header", drawer.transform, "PASSENGERS 0/0", titleText, 28, TextAlignmentOptions.Center);
            ConfigureRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(-36f, 44f));

            GameObject empty = CreateTextPanel("EmptyState", drawer.transform, "NO PASSENGERS ONBOARD", titleText, 23);
            ConfigureRect(empty.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -18f), new Vector2(-42f, 72f));

            GameObject scroll = CreateUiObject("Scroll View", drawer.transform);
            RectTransform scrollRectTransform = scroll.GetComponent<RectTransform>();
            ConfigureRect(scrollRectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -22f), new Vector2(-44f, -154f));
            ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = CreateUiObject("Viewport", scroll.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            ConfigureRect(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewportRect;

            GameObject content = CreateUiObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            ConfigureRect(contentRect, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 0f));
            contentRect.pivot = new Vector2(0.5f, 1f);
            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 8f;
            layoutGroup.padding = new RectOffset(12, 12, 8, 8);
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            MatchHudTransportPassengerItemView itemView = CreatePassengerItem(content.transform, healthSprite, buttonSprite, titleText);

            GameObject footer = CreateUiObject("Footer", drawer.transform);
            ConfigureRect(footer.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 48f), new Vector2(-38f, 72f));
            HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.spacing = 12f;
            footerLayout.padding = new RectOffset(10, 10, 8, 8);

            Button exitAll = CreateTextButton("ExitAllButton", footer.transform, "EXIT ALL", buttonSprite, titleText, 22, out TMP_Text exitAllLabel);
            Button close = CreateTextButton("CloseButton", footer.transform, "CLOSE", buttonSprite, titleText, 22, out TMP_Text closeLabel);

            SerializedObject drawerObject = new(drawerView);
            SetObject(drawerObject, "drawerRoot", drawer);
            SetObject(drawerObject, "headerText", header);
            SetObject(drawerObject, "emptyStateRoot", empty);
            SetObject(drawerObject, "emptyStateText", empty.GetComponentInChildren<TMP_Text>(true));
            SetObject(drawerObject, "contentRoot", contentRect);
            SetObject(drawerObject, "itemTemplate", itemView);
            SetObject(drawerObject, "exitAllButton", exitAll);
            SetObject(drawerObject, "exitAllLabel", exitAllLabel);
            SetObject(drawerObject, "closeButton", close);
            SetObject(drawerObject, "closeLabel", closeLabel);
            drawerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject panelObject = new(selectionPanel);
            SetObject(panelObject, "passengerChipRoot", chip);
            SetObject(panelObject, "passengerChipButton", chipButton);
            SetObject(panelObject, "passengerChipLabel", chipLabel);
            SetObject(panelObject, "passengerDrawer", drawerView);
            panelObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static MatchHudTransportPassengerItemView CreatePassengerItem(Transform parent, Sprite healthSprite, Sprite buttonSprite, TMP_Text templateText)
    {
        GameObject row = CreateImageObject("PassengerItemView", parent, null, new Color(0.04f, 0.045f, 0.035f, 0.86f), true);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 86f);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 86f;

        Image portrait = CreateImageObject("Portrait", row.transform, null, new Color(0.15f, 0.15f, 0.12f, 1f), false).GetComponent<Image>();
        ConfigureRect(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(58f, 58f));

        TMP_Text name = CreateText("Name", row.transform, "Passenger", templateText, 21, TextAlignmentOptions.Left);
        ConfigureRect(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(92f, 16f), new Vector2(-178f, 30f));

        TMP_Text role = CreateText("Role", row.transform, "SOLDIER", templateText, 16, TextAlignmentOptions.Left);
        ConfigureRect(role.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(92f, -10f), new Vector2(-178f, 24f));

        GameObject healthFrame = CreateImageObject("HealthFrame", row.transform, healthSprite, Color.white, false);
        ConfigureRect(healthFrame.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(111f, 14f), new Vector2(-194f, 16f));
        Image healthFill = CreateImageObject("HealthFill", healthFrame.transform, null, new Color(0.56f, 0.78f, 0.28f, 1f), false).GetComponent<Image>();
        ConfigureRect(healthFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TMP_Text healthText = CreateText("Health", row.transform, "100/100", templateText, 14, TextAlignmentOptions.Right);
        ConfigureRect(healthText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-146f, 15f), new Vector2(78f, 20f));

        Button exit = CreateTextButton("ExitButton", row.transform, "EXIT", buttonSprite, templateText, 18, out _);
        ConfigureRect(exit.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(82f, 54f));

        MatchHudTransportPassengerItemView itemView = row.AddComponent<MatchHudTransportPassengerItemView>();
        SerializedObject itemObject = new(itemView);
        SetObject(itemObject, "portraitImage", portrait);
        SetObject(itemObject, "nameText", name);
        SetObject(itemObject, "roleText", role);
        SetObject(itemObject, "healthFillImage", healthFill);
        SetObject(itemObject, "healthText", healthText);
        SetObject(itemObject, "exitButton", exit);
        itemObject.ApplyModifiedPropertiesWithoutUndo();

        return itemView;
    }

    private static GameObject CreateTextPanel(string name, Transform parent, string text, TMP_Text templateText, int fontSize)
    {
        GameObject root = CreateUiObject(name, parent);
        TMP_Text label = CreateText("Label", root.transform, text, templateText, fontSize, TextAlignmentOptions.Center);
        ConfigureRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return root;
    }

    private static Button CreateTextButton(string name, Transform parent, string text, Sprite sprite, TMP_Text templateText, int fontSize, out TMP_Text label)
    {
        GameObject root = CreateImageObject(name, parent, sprite, Color.white, true);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        label = CreateText("Label", root.transform, text, templateText, fontSize, TextAlignmentOptions.Center);
        ConfigureRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static GameObject CreateImageObject(string name, Transform parent, Sprite sprite, Color color, bool raycast)
    {
        GameObject obj = CreateUiObject(name, parent);
        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return obj;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, TMP_Text template, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUiObject(name, parent);
        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = template != null ? template.font : label.font;
        label.fontSharedMaterial = template != null ? template.fontSharedMaterial : label.fontSharedMaterial;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = new Color(0.92f, 0.88f, 0.58f, 1f);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        obj.layer = parent.gameObject.layer;
        return obj;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static void DestroyExisting(Transform parent, string childName)
    {
        Transform existing = FindDirectChild(parent, childName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }

    private static T GetSerializedReference<T>(Object target, string propertyName) where T : Object
    {
        SerializedObject serialized = new(target);
        return serialized.FindProperty(propertyName)?.objectReferenceValue as T;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }
}
