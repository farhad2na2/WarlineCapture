#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class AriaCommandAssistantV3PrefabTests
{
    [Test]
    public void Prefab_UsesV3PortraitSharedIconsGradientsAndConstantBorders()
    {
        AriaCommandAssistantPopupView view = RequireView();
        Assert.IsTrue(view.TryBindHierarchy());

        Image portrait = FindNamed(view.transform, "AriaPortraitV3").GetComponent<Image>();
        Assert.AreEqual(
            AriaCommandAssistantV3PrefabBuilder.PortraitPath,
            AssetDatabase.GetAssetPath(portrait.sprite));
        Assert.NotNull(portrait.GetComponent<AspectRatioFitter>());

        Assert.AreEqual(
            V3UiFoundationBuilder.MatchHostileMarkerIconPath,
            AssetDatabase.GetAssetPath(FindNamed(view.transform, "TARGETIconV3").GetComponent<Image>().sprite));
        Assert.AreEqual(
            V3UiFoundationBuilder.MatchHoldIconPath,
            AssetDatabase.GetAssetPath(FindNamed(view.transform, "INTEGRITYIconV3").GetComponent<Image>().sprite));
        Assert.AreEqual(
            V3UiFoundationBuilder.MatchScanIconPath,
            AssetDatabase.GetAssetPath(FindNamed(view.transform, "RANGEIconV3").GetComponent<Image>().sprite));

        V3GradientGraphic[] gradients =
            view.CommandAssistantPanel.GetComponentsInChildren<V3GradientGraphic>(true);
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
    public void Prefab_IsReadableTopRightResponsiveAndOnlyPanelConsumesInput()
    {
        AriaCommandAssistantPopupView view = RequireView();
        RectTransform panel = view.CommandAssistantPanel;
        Assert.NotNull(panel);
        Assert.AreEqual(new Vector2(0f, 1f), panel.anchorMin);
        Assert.That(panel.anchoredPosition.x, Is.GreaterThanOrEqualTo(1100f));
        Assert.That(panel.rect.width, Is.GreaterThanOrEqualTo(500f));
        Assert.That(panel.rect.height, Is.GreaterThanOrEqualTo(680f));
        Assert.IsNull(view.LandscapeLayout.GetComponent<Graphic>());

        MainMenuV3SectionLayoutView layout =
            view.LandscapeLayout.GetComponent<MainMenuV3SectionLayoutView>();
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);
        Assert.AreEqual(1, layout.RightAnchoredTargets.Length);
        Assert.AreSame(panel, layout.RightAnchoredTargets[0]);
        Assert.NotNull(view.LandscapeLayout.GetComponent<AriaTutorialHudVariantLayoutView>());
    }

    [Test]
    public void Prefab_KeepsRuntimeBindingsAndV3TouchActions()
    {
        AriaCommandAssistantPopupView view = RequireView();
        Assert.IsTrue(view.TryBindHierarchy());
        Assert.NotNull(FindNamed(view.transform, "GoalRow0"));
        Assert.NotNull(FindNamed(view.transform, "AlertRow2"));
        Assert.NotNull(FindNamed(view.transform, "ReportRow1"));
        Assert.NotNull(FindNamed(view.transform, "TargetMarker2"));
        Assert.NotNull(FindNamed(view.transform, "EnabledKnob").GetComponent<V3DiscGraphic>());

        Button close = FindNamed(view.transform, "HeaderCloseButton").GetComponent<Button>();
        Button showMe = FindNamed(view.transform, "ShowMeButton").GetComponent<Button>();
        Assert.That((close.transform as RectTransform).rect.height, Is.GreaterThanOrEqualTo(48f));
        Assert.That((showMe.transform as RectTransform).rect.height, Is.GreaterThanOrEqualTo(56f));
        Assert.IsTrue(Contains(view.CommandAssistantPanel, close.transform as RectTransform));
        Assert.IsTrue(Contains(view.CommandAssistantPanel, showMe.transform as RectTransform));
    }

    public static void RunFocusedValidation()
    {
        var tests = new AriaCommandAssistantV3PrefabTests();
        int passed = 0;
        try
        {
            tests.Prefab_UsesV3PortraitSharedIconsGradientsAndConstantBorders(); passed++;
            tests.Prefab_IsReadableTopRightResponsiveAndOnlyPanelConsumesInput(); passed++;
            tests.Prefab_KeepsRuntimeBindingsAndV3TouchActions(); passed++;
            Debug.Log($"[AriaCommandAssistantV3FocusedValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[AriaCommandAssistantV3FocusedValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    private static AriaCommandAssistantPopupView RequireView()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            AriaCommandAssistantV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        AriaCommandAssistantPopupView view = prefab.GetComponent<AriaCommandAssistantPopupView>();
        Assert.NotNull(view);
        return view;
    }

    private static Transform FindNamed(Transform root, string targetName)
    {
        if (root == null)
            return null;
        if (root.name == targetName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindNamed(root.GetChild(i), targetName);
            if (match != null)
                return match;
        }
        return null;
    }

    private static bool Contains(RectTransform parent, RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        for (int i = 0; i < corners.Length; i++)
        {
            if (!parent.rect.Contains(parent.InverseTransformPoint(corners[i])))
                return false;
        }
        return true;
    }
}
#endif
