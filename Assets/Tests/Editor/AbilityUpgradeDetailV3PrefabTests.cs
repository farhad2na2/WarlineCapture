#if UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class AbilityUpgradeDetailV3PrefabTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            AbilityUpgradeDetailV3PrefabBuilder.Build();
            AbilityUpgradeDetailV3PrefabTests tests = new();
            tests.Prefab_IsResponsiveAndUsesConstantThreePixelBorders(); passed++;
            tests.ApcArt_ReusesExistingPortraitAndPreservesAspect(); passed++;
            tests.ViewSourceAndUnlockStates_AreBound(); passed++;
            Debug.Log($"[AbilityUpgradeDetailV3Validation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[AbilityUpgradeDetailV3Validation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Prefab_IsResponsiveAndUsesConstantThreePixelBorders()
    {
        GameObject prefab = RequirePrefab();
        Assert.NotNull(prefab.GetComponent<UIPopupFrameView>());
        Assert.NotNull(prefab.GetComponent<AbilityUpgradeDetailV3PopupView>());

        MainMenuV3SectionLayoutView layout =
            prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);

        RectTransform frame = Find(prefab.transform, "Frame") as RectTransform;
        Assert.NotNull(frame);
        Assert.That(frame.sizeDelta.x, Is.EqualTo(1110f).Within(.1f));
        Assert.That(frame.sizeDelta.y, Is.EqualTo(783f).Within(.1f));

        V3GradientGraphic[] gradients =
            prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.That(gradients.Length, Is.GreaterThanOrEqualTo(17));
        foreach (V3GradientGraphic gradient in gradients)
        {
            SerializedObject serialized = new(gradient);
            float width = serialized.FindProperty("borderWidth").floatValue;
            Color color = serialized.FindProperty("borderColor").colorValue;
            if (color.a > .01f)
                Assert.That(width, Is.EqualTo(3f).Within(.001f), gradient.name);
        }

        Assert.NotNull(Find(prefab.transform, "ArmorHealthEffect"));
        Assert.NotNull(Find(prefab.transform, "DamageResistanceEffect"));
        Assert.NotNull(Find(prefab.transform, "MovementSpeedEffect"));
        Assert.NotNull(Find(prefab.transform, "AvailabilityRequirementsRow"));
        Assert.NotNull(Find(prefab.transform, "PrerequisiteRow"));
        Assert.NotNull(Find(prefab.transform, "CurrentTierRow"));
    }

    [Test]
    public void ApcArt_ReusesExistingPortraitAndPreservesAspect()
    {
        Transform artTransform = Find(RequirePrefab().transform, "ApcArtImage");
        Assert.NotNull(artTransform);
        RawImage art = artTransform.GetComponent<RawImage>();
        AspectRatioFitter fitter = artTransform.GetComponent<AspectRatioFitter>();
        Assert.NotNull(art);
        Assert.NotNull(art.texture);
        Assert.AreEqual(
            AbilityUpgradeDetailV3PrefabBuilder.ApcArtPath,
            AssetDatabase.GetAssetPath(art.texture));
        Assert.NotNull(fitter);
        Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, fitter.aspectMode);
    }

    [Test]
    public void ViewSourceAndUnlockStates_AreBound()
    {
        GameObject instance = Object.Instantiate(RequirePrefab());
        try
        {
            AbilityUpgradeDetailV3PopupView view =
                instance.GetComponent<AbilityUpgradeDetailV3PopupView>();
            Assert.NotNull(view);
            Assert.NotNull(view.ViewSourceButton);
            Assert.NotNull(view.UnlockButton);
            view.Configure(view.ViewSourceButton, view.UnlockButton);

            bool viewed = false;
            instance.SetActive(true);
            view.ViewSourceRequested += () => viewed = true;
            view.ViewSourceButton.onClick.Invoke();
            Assert.IsTrue(viewed);
            Assert.IsFalse(instance.activeSelf);

            bool unlocked = false;
            instance.SetActive(true);
            view.UnlockRequested += () => unlocked = true;
            view.SetUnlocked(false);
            view.UnlockButton.onClick.Invoke();
            Assert.IsFalse(unlocked);
            view.SetUnlocked(true);
            view.UnlockButton.onClick.Invoke();
            Assert.IsTrue(unlocked);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            AbilityUpgradeDetailV3PrefabBuilder.PrefabPath);
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
