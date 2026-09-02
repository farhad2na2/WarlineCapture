#if UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class IntelRevealV3PrefabTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            IntelRevealV3PrefabBuilder.Build();
            IntelRevealV3PrefabTests tests = new();
            tests.Prefab_IsResponsiveAndUsesConstantThreePixelBorders(); passed++;
            tests.EvidenceCards_ReuseOneAtlasWithoutStretching(); passed++;
            tests.CloseViewAndInspectActions_AreBound(); passed++;
            Debug.Log($"[IntelRevealV3Validation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[IntelRevealV3Validation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Prefab_IsResponsiveAndUsesConstantThreePixelBorders()
    {
        GameObject prefab = RequirePrefab();
        Assert.NotNull(prefab.GetComponent<UIPopupFrameView>());
        Assert.NotNull(prefab.GetComponent<IntelRevealV3PopupView>());

        MainMenuV3SectionLayoutView layout =
            prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);

        RectTransform frame = Find(prefab.transform, "Frame") as RectTransform;
        Assert.NotNull(frame);
        Assert.That(frame.sizeDelta.x, Is.EqualTo(1100f).Within(.1f));
        Assert.That(frame.sizeDelta.y, Is.EqualTo(756f).Within(.1f));

        Transform intelIcon = Find(prefab.transform, "IntelDocumentIcon");
        Assert.NotNull(intelIcon);
        Assert.NotNull(intelIcon.GetComponentInChildren<V3RingGraphic>(true));
        foreach (Image iconPart in intelIcon.GetComponentsInChildren<Image>(true))
            Assert.IsNull(iconPart.sprite, $"{iconPart.name} must remain procedural, not a placeholder sprite.");

        V3GradientGraphic[] gradients =
            prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.That(gradients.Length, Is.GreaterThanOrEqualTo(18));
        foreach (V3GradientGraphic gradient in gradients)
        {
            SerializedObject serialized = new(gradient);
            float width = serialized.FindProperty("borderWidth").floatValue;
            Color color = serialized.FindProperty("borderColor").colorValue;
            if (color.a > .01f)
                Assert.That(width, Is.EqualTo(3f).Within(.001f), gradient.name);
        }
    }

    [Test]
    public void EvidenceCards_ReuseOneAtlasWithoutStretching()
    {
        Transform cards = Find(RequirePrefab().transform, "EvidenceCards");
        Assert.NotNull(cards);
        RawImage[] evidence = cards.GetComponentsInChildren<RawImage>(true);
        Assert.AreEqual(3, evidence.Length);
        Texture shared = evidence[0].texture;
        Assert.NotNull(shared);
        Assert.That(
            AssetDatabase.GetAssetPath(shared),
            Does.EndWith("V3Shared/IntelReveal/POP08_EvidenceAtlas_V3.png"));

        for (int index = 0; index < evidence.Length; index++)
        {
            Assert.AreSame(shared, evidence[index].texture);
            AspectRatioFitter fitter = evidence[index].GetComponent<AspectRatioFitter>();
            Assert.NotNull(fitter);
            Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, fitter.aspectMode);
            for (int previous = 0; previous < index; previous++)
                Assert.AreNotEqual(evidence[previous].uvRect, evidence[index].uvRect);
        }
    }

    [Test]
    public void CloseViewAndInspectActions_AreBound()
    {
        GameObject instance = Object.Instantiate(RequirePrefab());
        try
        {
            IntelRevealV3PopupView view = instance.GetComponent<IntelRevealV3PopupView>();
            Assert.NotNull(view);
            Assert.NotNull(view.FooterCloseButton);
            Assert.NotNull(view.ViewIntelButton);
            Assert.AreEqual(3, view.InspectButtons.Length);
            view.Configure(view.FooterCloseButton, view.ViewIntelButton, view.InspectButtons);

            instance.SetActive(true);
            view.FooterCloseButton.onClick.Invoke();
            Assert.IsFalse(instance.activeSelf);

            bool viewed = false;
            instance.SetActive(true);
            view.ViewIntelRequested += () => viewed = true;
            view.ViewIntelButton.onClick.Invoke();
            Assert.IsTrue(viewed);
            Assert.IsFalse(instance.activeSelf);

            int inspected = -1;
            instance.SetActive(true);
            view.InspectRequested += index => inspected = index;
            view.InspectButtons[1].onClick.Invoke();
            Assert.AreEqual(1, inspected);
            Assert.IsTrue(instance.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            IntelRevealV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        return prefab;
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
}
#endif
