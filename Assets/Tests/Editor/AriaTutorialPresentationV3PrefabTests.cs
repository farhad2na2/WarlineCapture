#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class AriaTutorialPresentationV3PrefabTests
{
    [Test]
    public void Prefab_UsesV3PortraitProceduralGradientsAndConstantBorders()
    {
        GameObject prefab = RequirePrefab();
        AriaTutorialBriefingView view =
            prefab.GetComponentInChildren<AriaTutorialBriefingView>(true);
        Assert.NotNull(view);
        Assert.IsTrue(view.TryBindHierarchy());
        Assert.AreEqual(
            AriaTutorialBriefingPrefabBuilder.PortraitPath,
            AssetDatabase.GetAssetPath(view.PortraitImage.sprite));
        Assert.NotNull(view.PortraitImage.GetComponent<AspectRatioFitter>());

        V3GradientGraphic[] gradients =
            view.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.That(gradients.Length, Is.GreaterThanOrEqualTo(7));
        for (int i = 0; i < gradients.Length; i++)
        {
            SerializedObject serialized = new(gradients[i]);
            float border = serialized.FindProperty("borderWidth").floatValue;
            Assert.That(border == 0f || Mathf.Approximately(border, 3f), Is.True,
                $"{gradients[i].name} uses a non-V3 border width of {border}.");
        }
    }

    [Test]
    public void Prefab_PlacesAllActionsInsideTopRightPanelInTargetOrder()
    {
        AriaTutorialBriefingView view = RequirePrefab()
            .GetComponentInChildren<AriaTutorialBriefingView>(true);
        RectTransform panel = view.BriefingLayout;
        RectTransform doIt = view.DoItButton.transform as RectTransform;
        RectTransform showMe = view.ShowMeButton.transform as RectTransform;
        RectTransform skip = view.CloseButton.transform as RectTransform;

        Assert.AreEqual(new Vector2(0f, 1f), panel.anchorMin);
        Assert.That(panel.anchoredPosition.x, Is.GreaterThanOrEqualTo(1100f));
        Assert.That(doIt.anchoredPosition.x, Is.LessThan(showMe.anchoredPosition.x));
        Assert.That(showMe.anchoredPosition.x, Is.LessThan(skip.anchoredPosition.x));
        Assert.That(doIt.rect.height, Is.GreaterThanOrEqualTo(72f));
        Assert.That(showMe.rect.height, Is.GreaterThanOrEqualTo(72f));
        Assert.That(skip.rect.height, Is.GreaterThanOrEqualTo(72f));
    }

    [Test]
    public void Prefab_HasResponsiveUltrawideGuideWithoutFullscreenRaycastBlocker()
    {
        AriaTutorialBriefingView view = RequirePrefab()
            .GetComponentInChildren<AriaTutorialBriefingView>(true);
        MainMenuV3SectionLayoutView layout =
            view.GetComponent<MainMenuV3SectionLayoutView>();
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);
        Assert.AreEqual(4, layout.RightAnchoredTargets.Length);
        Assert.NotNull(view.GetComponent<AriaTutorialHudVariantLayoutView>());
        Assert.NotNull(view.FirstStepGuideRoot);
        Assert.IsNull(view.transform.Find("TutorialInputBlocker"));
        Assert.IsNull(view.GetComponent<Graphic>(),
            "The full-screen tutorial section must not consume battlefield input.");

        GameObject canvasObject = new("TutorialHeaderVariantCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject popupObject = null;
        try
        {
            RectTransform canvas = canvasObject.GetComponent<RectTransform>();
            canvas.sizeDelta = new Vector2(1672f, 941f);
            RectTransform header = CreateRect("HeaderContent", canvas);
            RectTransform resource = CreateRect("ResourceStrip", header);
            RectTransform settings = CreateRect("SettingsButton", header);
            RectTransform pause = CreateRect("PauseButton", header);
            RectTransform embeddedAria = CreateRect("AriaAssistantButton", header);
            settings.anchoredPosition = new Vector2(1042f, -10f);
            pause.anchoredPosition = new Vector2(1122f, -10f);

            popupObject = Object.Instantiate(RequirePrefab(), canvas);
            AriaTutorialBriefingView liveView =
                popupObject.GetComponentInChildren<AriaTutorialBriefingView>(true);
            liveView.gameObject.SetActive(true);
            AriaTutorialHudVariantLayoutView variant =
                liveView.GetComponent<AriaTutorialHudVariantLayoutView>();
            variant.RefreshLayout();

            Assert.AreEqual(369f, resource.anchoredPosition.x);
            Assert.AreEqual(978f, settings.anchoredPosition.x);
            Assert.AreEqual(1054f, pause.anchoredPosition.x);
            Assert.IsFalse(embeddedAria.gameObject.activeSelf);

            variant.RestoreLayout();
            Assert.AreEqual(1042f, settings.anchoredPosition.x);
            Assert.AreEqual(1122f, pause.anchoredPosition.x);
            Assert.IsTrue(embeddedAria.gameObject.activeSelf);
        }
        finally
        {
            if (popupObject != null)
                Object.DestroyImmediate(popupObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    public static void RunFocusedValidation()
    {
        var tests = new AriaTutorialPresentationV3PrefabTests();
        int passed = 0;
        try
        {
            tests.Prefab_UsesV3PortraitProceduralGradientsAndConstantBorders(); passed++;
            tests.Prefab_PlacesAllActionsInsideTopRightPanelInTargetOrder(); passed++;
            tests.Prefab_HasResponsiveUltrawideGuideWithoutFullscreenRaycastBlocker(); passed++;
            Debug.Log($"[AriaTutorialPresentationV3Validation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[AriaTutorialPresentationV3Validation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            AriaTutorialBriefingPrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        return prefab;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject value = new(name, typeof(RectTransform));
        RectTransform rect = value.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(100f, 60f);
        return rect;
    }
}
#endif
