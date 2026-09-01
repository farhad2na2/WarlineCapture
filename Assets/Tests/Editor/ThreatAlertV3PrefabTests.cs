#if UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class ThreatAlertV3PrefabTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            ThreatAlertV3PrefabBuilder.Build();
            ThreatAlertV3PrefabTests tests = new();
            tests.Prefab_HasTwoResponsiveV3StatesAndConstantBorders();
            tests.VehiclePreview_PreservesAspectAndReusesExistingArt();
            tests.JumpAndCloseButtons_ChangeLiveState();
            Debug.Log("[ThreatAlertV3Validation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ThreatAlertV3Validation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Prefab_HasTwoResponsiveV3StatesAndConstantBorders()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThreatAlertV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        ThreatAlertV3PopupView state = prefab.GetComponent<ThreatAlertV3PopupView>();
        Assert.NotNull(state);
        Assert.NotNull(state.AlertSurface);
        Assert.NotNull(state.RoutePreviewSurface);
        Assert.NotNull(state.RoutePreviewStrip);
        Assert.NotNull(state.RouteWorldOverlay);

        MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);

        RectTransform alert = state.AlertSurface.transform as RectTransform;
        Assert.That(alert.sizeDelta.x, Is.EqualTo(740f).Within(.1f));
        Assert.That(alert.sizeDelta.y, Is.EqualTo(610f).Within(.1f));

        V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.That(gradients.Length, Is.GreaterThanOrEqualTo(18));
        foreach (V3GradientGraphic gradient in gradients)
        {
            SerializedObject serialized = new(gradient);
            float borderWidth = serialized.FindProperty("borderWidth").floatValue;
            Color borderColor = serialized.FindProperty("borderColor").colorValue;
            if (borderColor.a > .01f)
                Assert.That(borderWidth, Is.EqualTo(3f).Within(.001f), gradient.name);
        }
    }

    [Test]
    public void VehiclePreview_PreservesAspectAndReusesExistingArt()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThreatAlertV3PrefabBuilder.PrefabPath);
        Transform previewTransform = FindDeepChild(prefab.transform, "VehiclePreview");
        Assert.NotNull(previewTransform);
        Image preview = previewTransform.GetComponent<Image>();
        AspectRatioFitter fitter = previewTransform.GetComponent<AspectRatioFitter>();
        Assert.NotNull(preview);
        Assert.NotNull(preview.sprite);
        Assert.That(AssetDatabase.GetAssetPath(preview.sprite), Does.Contain("SelectionSummary_VehicleSquad_512.png"));
        Assert.NotNull(fitter);
        Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, fitter.aspectMode);
    }

    [Test]
    public void JumpAndCloseButtons_ChangeLiveState()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThreatAlertV3PrefabBuilder.PrefabPath);
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            ThreatAlertV3PopupView state = instance.GetComponent<ThreatAlertV3PopupView>();
            Assert.NotNull(state);
            state.Configure(
                state.Scrim,
                state.AlertSurface,
                state.RoutePreviewSurface,
                state.RoutePreviewStrip,
                state.RouteWorldOverlay,
                state.JumpToThreatButton,
                state.AlertCloseButton,
                state.RouteCloseButton);
            state.ShowAlert();
            Assert.IsTrue(instance.activeSelf);
            Assert.IsFalse(state.IsRoutePreview);
            Assert.IsTrue(state.Scrim.activeSelf);

            state.JumpToThreatButton.onClick.Invoke();
            Assert.IsTrue(state.IsRoutePreview);
            Assert.IsFalse(state.Scrim.activeSelf);
            Assert.IsTrue(state.RoutePreviewStrip.activeSelf);
            Assert.IsTrue(state.RouteWorldOverlay.activeSelf);

            state.RouteCloseButton.onClick.Invoke();
            Assert.IsFalse(instance.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
