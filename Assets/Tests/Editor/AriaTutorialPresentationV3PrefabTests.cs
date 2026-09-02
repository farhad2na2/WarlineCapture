#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class AriaTutorialPresentationV3PrefabTests
{
    private const string MatchHudPath =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";

    [Test]
    public void MatchHud_OwnsTheOnlyTutorialAriaPanel()
    {
        GameObject matchHud = RequirePrefab(MatchHudPath);
        Transform aria = FindDeepChild(matchHud.transform, "AriaAssistantButton");
        Assert.NotNull(aria);

        AriaTutorialBriefingView view = aria.GetComponent<AriaTutorialBriefingView>();
        Assert.NotNull(view);
        Assert.IsTrue(view.TryBindHierarchy());
        Assert.IsFalse(view.BriefingLayout.gameObject.activeSelf);
        Assert.IsNull(view.CloseButton, "The permanent ARIA tutorial panel must not contain Skip.");
        Assert.AreEqual(
            AriaTutorialBriefingPrefabBuilder.PortraitPath,
            AssetDatabase.GetAssetPath(view.PortraitImage.sprite));
        Assert.NotNull(view.PortraitImage.GetComponent<AspectRatioFitter>());
    }

    [Test]
    public void MatchHud_EmbedsOnlyDoItAndShowMeActions()
    {
        GameObject matchHud = RequirePrefab(MatchHudPath);
        AriaTutorialBriefingView view = FindDeepChild(
            matchHud.transform,
            "AriaAssistantButton").GetComponent<AriaTutorialBriefingView>();

        Assert.AreEqual(view.DoItButton.transform.parent, view.ShowMeButton.transform.parent);
        Assert.That((view.DoItButton.transform as RectTransform).rect.height, Is.GreaterThanOrEqualTo(55f));
        Assert.That((view.ShowMeButton.transform as RectTransform).rect.height, Is.GreaterThanOrEqualTo(55f));
        Assert.AreEqual(2, view.BriefingLayout.GetComponentsInChildren<Button>(true).Length);
    }

    [Test]
    public void Popup_DoesNotContainASecondTutorialSurface()
    {
        GameObject popup = RequirePrefab(AriaTutorialBriefingPrefabBuilder.PrefabPath);
        Assert.IsNull(popup.transform.Find("TutorialBriefingSurface"));
        Assert.IsNull(popup.GetComponentInChildren<AriaTutorialBriefingView>(true));
        Assert.IsNull(FindDeepChild(popup.transform, "TutorialCloseButton"));
    }

    [Test]
    public void EnglishAndFarsi_ReuseTheSameFixedPanelFootprint()
    {
        GameObject instance = Object.Instantiate(RequirePrefab(MatchHudPath));
        try
        {
            RectTransform aria = FindDeepChild(
                instance.transform,
                "AriaAssistantButton") as RectTransform;
            AriaTutorialBriefingView view = aria.GetComponent<AriaTutorialBriefingView>();
            Vector2 panelSize = aria.sizeDelta;
            Vector2 guidanceSize = view.BriefingLayout.sizeDelta;

            view.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel());
            view.SetPresentationVisible(true);
            view.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel(true));

            Assert.AreEqual(panelSize, aria.sizeDelta);
            Assert.AreEqual(guidanceSize, view.BriefingLayout.sizeDelta);
            Assert.IsTrue(view.TitleText.isRightToLeftText);
            Assert.IsTrue(view.BodyText.isRightToLeftText);
            Assert.IsNull(view.CloseButton);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    public static void RunFocusedValidation()
    {
        var tests = new AriaTutorialPresentationV3PrefabTests();
        int passed = 0;
        try
        {
            tests.MatchHud_OwnsTheOnlyTutorialAriaPanel(); passed++;
            tests.MatchHud_EmbedsOnlyDoItAndShowMeActions(); passed++;
            tests.Popup_DoesNotContainASecondTutorialSurface(); passed++;
            tests.EnglishAndFarsi_ReuseTheSameFixedPanelFootprint(); passed++;
            Debug.Log($"[AriaTutorialPresentationV3Validation] result=Passed tests={passed} panels=1 actions=2 skip=absent");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[AriaTutorialPresentationV3Validation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    private static GameObject RequirePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.NotNull(prefab, path);
        return prefab;
    }

    private static Transform FindDeepChild(Transform root, string targetName)
    {
        if (root == null)
            return null;
        if (root.name == targetName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
