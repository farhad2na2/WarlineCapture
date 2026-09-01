#if UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConfirmRaidV3PrefabTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            ConfirmRaidV3PrefabBuilder.Build();
            ConfirmRaidV3PrefabTests tests = new();
            tests.Prefab_IsResponsiveAndUsesConstantThreePixelBorders();
            tests.TargetMap_ReusesExistingArtAndPreservesAspect();
            tests.CancelAndConfirmButtons_CloseWithDistinctOutcomes();
            Debug.Log("[ConfirmRaidV3Validation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ConfirmRaidV3Validation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Prefab_IsResponsiveAndUsesConstantThreePixelBorders()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConfirmRaidV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        Assert.NotNull(prefab.GetComponent<UIPopupFrameView>());
        Assert.NotNull(prefab.GetComponent<ConfirmRaidV3PopupView>());

        MainMenuV3SectionLayoutView layout =
            prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);

        RectTransform frame = Find(prefab.transform, "Frame") as RectTransform;
        Assert.NotNull(frame);
        Assert.That(frame.sizeDelta.x, Is.EqualTo(1008f).Within(.1f));
        Assert.That(frame.sizeDelta.y, Is.EqualTo(688f).Within(.1f));

        V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.That(gradients.Length, Is.GreaterThanOrEqualTo(28));
        foreach (V3GradientGraphic gradient in gradients)
        {
            SerializedObject serialized = new(gradient);
            float borderWidth = serialized.FindProperty("borderWidth").floatValue;
            Color borderColor = serialized.FindProperty("borderColor").colorValue;
            if (borderColor.a > .01f)
                Assert.That(borderWidth, Is.EqualTo(3f).Within(.001f), gradient.name);
        }

        foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
        {
            string path = image.sprite != null ? AssetDatabase.GetAssetPath(image.sprite) : string.Empty;
            Assert.That(path, Does.Not.Contain("chrome_06_gold_action_button_bg"));
            Assert.That(path, Does.Not.Contain("scn08_v02_panel_frame_large"));
        }
    }

    [Test]
    public void TargetMap_ReusesExistingArtAndPreservesAspect()
    {
        Transform mapTransform = Find(
            AssetDatabase.LoadAssetAtPath<GameObject>(ConfirmRaidV3PrefabBuilder.PrefabPath).transform,
            "TargetMapImage");
        Assert.NotNull(mapTransform);
        RawImage map = mapTransform.GetComponent<RawImage>();
        AspectRatioFitter fitter = mapTransform.GetComponent<AspectRatioFitter>();
        Assert.NotNull(map);
        Assert.NotNull(map.texture);
        Assert.That(AssetDatabase.GetAssetPath(map.texture),
            Does.Contain("SCN05_SahrinMissionMap_V3.png"));
        Assert.NotNull(fitter);
        Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, fitter.aspectMode);
    }

    [Test]
    public void CancelAndConfirmButtons_CloseWithDistinctOutcomes()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConfirmRaidV3PrefabBuilder.PrefabPath);
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            ConfirmRaidV3PopupView view = instance.GetComponent<ConfirmRaidV3PopupView>();
            Assert.NotNull(view);
            view.Configure(view.CancelButton, view.ConfirmButton);
            instance.SetActive(true);
            view.CancelButton.onClick.Invoke();
            Assert.IsFalse(instance.activeSelf);
            Assert.IsFalse(view.WasConfirmed);

            instance.SetActive(true);
            view.Configure(view.CancelButton, view.ConfirmButton);
            view.ConfirmButton.onClick.Invoke();
            Assert.IsFalse(instance.activeSelf);
            Assert.IsTrue(view.WasConfirmed);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
