#if UNITY_EDITOR
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardUnlockV3PrefabTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            RewardUnlockV3PrefabBuilder.Build();
            RewardUnlockV3PrefabTests tests = new();
            tests.Prefab_UsesResponsiveV3ChromeAndSharedBrandLogo();
            tests.RewardArt_PreservesAspectAndReusesTheV3Asset();
            tests.ContinueButton_DismissesTheLivePopup();
            Debug.Log("[RewardUnlockV3Validation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[RewardUnlockV3Validation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Prefab_UsesResponsiveV3ChromeAndSharedBrandLogo()
    {
        GameObject prefab = RequirePrefab();
        UIPopupFrameView popup = prefab.GetComponent<UIPopupFrameView>();
        Assert.NotNull(popup);

        MainMenuV3SectionLayoutView layout =
            prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);

        Transform logo = Find(prefab.transform, "SharedMainMenuLogo");
        Assert.NotNull(logo);
        Image logoImage = logo.GetComponentInChildren<Image>(true);
        Assert.NotNull(logoImage);
        Assert.AreEqual(V3UiFoundationBuilder.MainMenuLogoPath,
            AssetDatabase.GetAssetPath(logoImage.sprite));

        V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.That(gradients.Length, Is.GreaterThanOrEqualTo(10));
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
    public void RewardArt_PreservesAspectAndReusesTheV3Asset()
    {
        RawImage art = Find(RequirePrefab().transform, "UnlockImage")?.GetComponent<RawImage>();
        Assert.NotNull(art);
        Assert.NotNull(art.texture);
        Assert.That(AssetDatabase.GetAssetPath(art.texture),
            Does.EndWith("POP04_RangerSquad_V3.png"));
        Assert.NotNull(art.GetComponent<AspectRatioFitter>());
    }

    [Test]
    public void ContinueButton_DismissesTheLivePopup()
    {
        GameObject instance = Object.Instantiate(RequirePrefab());
        try
        {
            UIPopupFrameView popup = instance.GetComponent<UIPopupFrameView>();
            Assert.NotNull(popup);
            Assert.NotNull(popup.CloseButton);
            Assert.AreEqual("ContinueButton", popup.CloseButton.name);
            instance.SetActive(true);
            popup.SendMessage("Awake", SendMessageOptions.RequireReceiver);
            popup.CloseButton.onClick.Invoke();
            Assert.IsFalse(instance.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RewardUnlockV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        return prefab;
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
