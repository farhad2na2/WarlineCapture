using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiAssistantPanelTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab";
    private const string SectionPanelPath = "Assets/Game/Art/UI/Generated/Settings/Frames/Settings_SectionPanel_9Slice.png";
    private const string ButtonAnimatorPath = "Assets/Game/Animations/UI/WarlineCaptureButtonStates.overrideController";

    [Test]
    public void AssistantPanelPrefab_HasRequiredHierarchyAndLiveText()
    {
        GameObject prefab = LoadPrefab();
        Assert.NotNull(prefab.GetComponent<AssistantPanelView>());
        Assert.IsNull(prefab.GetComponent<Image>(), "Root must remain a container so frame/fill layers stay replaceable.");

        AssertChildren(
            prefab,
            "BackgroundTint",
            "FrameChrome",
            "Header",
            "Header/TitleText",
            "Header/StatusText",
            "Header/SignalBadge/LabelText",
            "AssistantTabs",
            "AssistantTabs/Tab_Next/LabelText",
            "AssistantTabs/Tab_Why/LabelText",
            "AssistantTabs/Tab_Plan/LabelText",
            "AssistantTabs/Tab_Goals/LabelText",
            "RecommendationPanel",
            "RecommendationPanel/RecommendationTitleText",
            "RecommendationPanel/RecommendationBodyText",
            "RecommendationPanel/RecommendationChips",
            "RecommendationPanel/RecommendationChips/Chip_Primary/LabelText",
            "RecommendationPanel/RecommendationChips/Chip_Tactical/LabelText",
            "RecommendationPanel/RecommendationChips/Chip_Risk/LabelText",
            "ActionButtons",
            "ActionButtons/ShowMeButton/LabelText",
            "ActionButtons/DoItButton/LabelText",
            "ActionButtons/StopButton/LabelText");
    }

    [Test]
    public void AssistantPanelPrefab_WiresFutureBindingReferences()
    {
        AssistantPanelView view = LoadPrefab().GetComponent<AssistantPanelView>();

        Assert.NotNull(view.TitleText);
        Assert.NotNull(view.StatusText);
        Assert.NotNull(view.RecommendationTitleText);
        Assert.NotNull(view.RecommendationBodyText);
        Assert.NotNull(view.AssistantTabs);
        Assert.NotNull(view.RecommendationChips);
        Assert.NotNull(view.ShowMeButton);
        Assert.NotNull(view.DoItButton);
        Assert.NotNull(view.StopButton);
        Assert.AreEqual(4, view.TabLabels.Length);
        Assert.AreEqual(3, view.ChipLabels.Length);
        Assert.AreEqual("NEXT", view.TabLabels[0].text);
        Assert.AreEqual("WHY", view.TabLabels[1].text);
        Assert.AreEqual("PLAN", view.TabLabels[2].text);
        Assert.AreEqual("GOALS", view.TabLabels[3].text);
    }

    [Test]
    public void AssistantPanelPrefab_UsesExistingChromeAndAnimatedButtons()
    {
        GameObject prefab = LoadPrefab();
        AssertImageSpritePath(prefab, "FrameChrome", SectionPanelPath, Image.Type.Sliced);
        AssertImageSpritePath(prefab, "RecommendationPanel", SectionPanelPath, Image.Type.Sliced);
        AssertAnimatedButton(prefab, "AssistantTabs/Tab_Next");
        AssertAnimatedButton(prefab, "AssistantTabs/Tab_Why");
        AssertAnimatedButton(prefab, "AssistantTabs/Tab_Plan");
        AssertAnimatedButton(prefab, "AssistantTabs/Tab_Goals");
        AssertAnimatedButton(prefab, "ActionButtons/ShowMeButton");
        AssertAnimatedButton(prefab, "ActionButtons/DoItButton");
        AssertAnimatedButton(prefab, "ActionButtons/StopButton");
    }

    [Test]
    public void AssistantPanelPrefab_RootUsesFixedDockSizedRect()
    {
        RectTransform rect = (RectTransform)LoadPrefab().transform;
        Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMin);
        Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMax);
        Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.pivot);
        Assert.AreEqual(new Vector2(660f, 620f), rect.sizeDelta);
        Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
    }

    [Test]
    public void AssistantPanelView_BindRecommendationUpdatesLiveTextAndChipVisibility()
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab());
        try
        {
            AssistantPanelView view = instance.GetComponent<AssistantPanelView>();
            view.BindRecommendation("RECOMMENDED: DRONE SCAN", "Intel is low near the port.", new[] { "Drone scan", "Port breach" }, true, false, true);

            Assert.AreEqual("RECOMMENDED: DRONE SCAN", view.RecommendationTitleText.text);
            Assert.AreEqual("Intel is low near the port.", view.RecommendationBodyText.text);
            Assert.AreEqual("Drone scan", view.ChipLabels[0].text);
            Assert.AreEqual("Port breach", view.ChipLabels[1].text);
            Assert.IsTrue(view.ChipLabels[0].transform.parent.gameObject.activeSelf);
            Assert.IsTrue(view.ChipLabels[1].transform.parent.gameObject.activeSelf);
            Assert.IsFalse(view.ChipLabels[2].transform.parent.gameObject.activeSelf);
            Assert.IsTrue(view.ShowMeButton.interactable);
            Assert.IsFalse(view.DoItButton.interactable);
            Assert.IsTrue(view.StopButton.interactable);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void AssistantPanel_ShowMeDoItStopButtonsExposeExpectedStates()
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab());
        try
        {
            AssistantPanelView view = instance.GetComponent<AssistantPanelView>();
            view.SetActionAvailability(canShow: false, canExecute: true, canStop: false);

            Assert.IsFalse(view.ShowMeButton.interactable);
            Assert.IsTrue(view.DoItButton.interactable);
            Assert.IsFalse(view.StopButton.interactable);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void AssistantPanelText_UsesOxaniumAndNoWrap()
    {
        foreach (TMP_Text text in LoadPrefab().GetComponentsInChildren<TMP_Text>(true))
        {
            Assert.NotNull(text.font, text.name);
            StringAssert.Contains("Oxanium", text.font.name, text.name);
            Assert.AreEqual(TextWrappingModes.NoWrap, text.textWrappingMode, text.name);
        }
    }

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, PrefabPath);
        return prefab;
    }

    private static void AssertChildren(GameObject prefab, params string[] paths)
    {
        foreach (string path in paths)
            Assert.NotNull(prefab.transform.Find(path), $"{PrefabPath} missing {path}");
    }

    private static void AssertImageSpritePath(GameObject root, string imagePath, string expectedSpritePath, Image.Type expectedType)
    {
        Transform transform = root.transform.Find(imagePath);
        Assert.NotNull(transform, imagePath);
        Image image = transform.GetComponent<Image>();
        Assert.NotNull(image, imagePath);
        Assert.NotNull(image.sprite, imagePath);
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), imagePath);
        Assert.AreEqual(expectedType, image.type, imagePath);
    }

    private static void AssertAnimatedButton(GameObject root, string buttonPath)
    {
        Transform transform = root.transform.Find(buttonPath);
        Assert.NotNull(transform, buttonPath);
        Button button = transform.GetComponent<Button>();
        Assert.NotNull(button, buttonPath);
        Assert.AreEqual(Selectable.Transition.Animation, button.transition, buttonPath);
        Animator animator = transform.GetComponent<Animator>();
        Assert.NotNull(animator, buttonPath);
        Assert.NotNull(animator.runtimeAnimatorController, buttonPath);
        Assert.AreEqual(ButtonAnimatorPath, AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), buttonPath);
    }
}
