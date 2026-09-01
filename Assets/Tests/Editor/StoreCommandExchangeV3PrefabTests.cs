#if UNITY_EDITOR
using System;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StoreCommandExchangeV3PrefabTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN14_StoreCommandExchangeContent.prefab";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            StoreCommandExchangeV3PrefabBuilder.Build();
            StoreCommandExchangeV3PrefabTests suite = new();
            suite.MenuScene_AssignsStoreAndMainMenuRoutesToIt(); passed++;
            suite.Prefab_UsesResponsiveTargetStructureSharedArtAndConstantBorders(); passed++;
            suite.CatalogSelection_UpdatesDetailsAndKeepsPurchaseUnavailable(); passed++;
            suite.RouteMountsStoreWithoutReplacingTheSharedHeader(); passed++;
            Debug.Log($"[StoreCommandExchangeV3PrefabTests] result=Passed tests={passed} purchase=DesignedUnavailable");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[StoreCommandExchangeV3PrefabTests] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void MenuScene_AssignsStoreAndMainMenuRoutesToIt()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.NotNull(content.StoreContentPrefab);
        Assert.AreEqual("SCN14_StoreCommandExchangeContent", content.StoreContentPrefab.name);

        Transform storeButton = Find(content.MainMenuContentPrefab.transform, "StoreButton");
        Assert.NotNull(storeButton);
        UIShellRouteButtonView route = storeButton.GetComponentInChildren<UIShellRouteButtonView>(true);
        Assert.NotNull(route);
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, route.Intent);
        Assert.AreEqual(UIRoute.CommandExchange, route.Route);
        Assert.IsTrue(route.PushHistory);

        AssertBackRoute(content.StoreContentPrefab, "BackButton");
        AssertBackRoute(content.StoreContentPrefab, "CloseButton");
    }

    [Test]
    public void Prefab_UsesResponsiveTargetStructureSharedArtAndConstantBorders()
    {
        GameObject prefab = RequirePrefab();
        StoreCommandExchangeV3View view = prefab.GetComponent<StoreCommandExchangeV3View>();
        Assert.NotNull(view);
        Assert.AreEqual(6, view.CategoryButtons.Length);
        Assert.AreEqual(4, view.OfferButtons.Length);
        Assert.IsFalse(view.PurchaseButton.interactable);

        string[] required =
        {
            "Header", "StoreBrand", "CategoryRail", "OffersPanel", "DetailPanel",
            "BackButton", "EligibilityPanel", "PurchaseButton"
        };
        for (int i = 0; i < required.Length; i++)
            Assert.NotNull(Find(prefab.transform, required[i]), required[i]);

        MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);
        Assert.GreaterOrEqual(layout.RightAnchoredTargets.Length, 7);
        SerializedProperty widthTargets = new SerializedObject(layout).FindProperty("widthExpandedTargets");
        Assert.NotNull(widthTargets);
        Assert.GreaterOrEqual(widthTargets.arraySize, 3);

        RawImage detail = Find(prefab.transform, "DetailArt").GetComponent<RawImage>();
        Assert.NotNull(detail.texture);
        AspectRatioFitter fitter = detail.GetComponent<AspectRatioFitter>();
        Assert.NotNull(fitter);
        Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, fitter.aspectMode);

        int bordered = 0;
        V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.GreaterOrEqual(gradients.Length, 34,
            "SCN-14 must use visible directional gradients, not flat panel colors.");
        for (int i = 0; i < gradients.Length; i++)
        {
            SerializedObject serialized = new(gradients[i]);
            Color borderColor = serialized.FindProperty("borderColor").colorValue;
            float borderWidth = serialized.FindProperty("borderWidth").floatValue;
            if (borderColor.a <= .01f)
                continue;
            bordered++;
            Assert.AreEqual(3f, borderWidth, .001f,
                $"{gradients[i].name} must use the common visible 3 px V3 border.");
        }
        Assert.GreaterOrEqual(bordered, 27);

        string[] approvedRoots =
        {
            "Assets/Game/Art/UI/V3Shared/",
            "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/"
        };
        RawImage[] artImages = prefab.GetComponentsInChildren<RawImage>(true);
        for (int i = 0; i < artImages.Length; i++)
        {
            Texture texture = artImages[i].texture;
            if (texture == null)
                continue;
            string path = AssetDatabase.GetAssetPath(texture);
            Assert.IsTrue(path.StartsWith(approvedRoots[0], StringComparison.Ordinal) ||
                          path.StartsWith(approvedRoots[1], StringComparison.Ordinal),
                $"{artImages[i].name} must reuse shared V3 or existing Armory art: {path}");
        }
    }

    [Test]
    public void CatalogSelection_UpdatesDetailsAndKeepsPurchaseUnavailable()
    {
        GameObject instance = UnityEngine.Object.Instantiate(RequirePrefab());
        try
        {
            StoreCommandExchangeV3View view = instance.GetComponent<StoreCommandExchangeV3View>();
            Assert.NotNull(view);
            TMP_Text heading = Find(instance.transform, "OffersHeading").GetComponent<TMP_Text>();
            TMP_Text detailTitle = Find(instance.transform, "DetailTitle").GetComponent<TMP_Text>();
            TMP_Text purchaseLabel = Find(instance.transform, "PurchaseButton").Find("Label").GetComponent<TMP_Text>();
            RawImage detailArt = Find(instance.transform, "DetailArt").GetComponent<RawImage>();
            Texture firstTexture = detailArt.texture;

            view.CategoryButtons[3].onClick.Invoke();
            Assert.AreEqual("ARMORY OFFERS", heading.text);
            Assert.AreEqual("RANGER PARTS CASE", detailTitle.text);

            view.OfferButtons[2].onClick.Invoke();
            Assert.AreEqual("SUPPORT DRONE KIT", detailTitle.text);
            Assert.AreEqual("PURCHASE 280", purchaseLabel.text);
            Assert.AreNotSame(firstTexture, detailArt.texture);
            Assert.IsFalse(view.PurchaseButton.interactable,
                "Purchase cannot activate before wallet, receipt, catalog, profile, and reward-grant services exist.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void RouteMountsStoreWithoutReplacingTheSharedHeader()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });
        GameObject headerBefore = RegionChild(content.ShellView, UIShellRegionId.HeaderRegion);
        content.InstallMenuRouteBody(UIRoute.CommandExchange);
        GameObject headerAfter = RegionChild(content.ShellView, UIShellRegionId.HeaderRegion);
        Assert.AreSame(headerBefore, headerAfter);
        GameObject mounted = RegionChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.NotNull(mounted.GetComponent<StoreCommandExchangeV3View>());
        Assert.IsNull(Find(mounted.transform, "HeaderContent"),
            "Store owns its target composition but must not instantiate the legacy shared-header prefab.");
    }

    private static void AssertBackRoute(GameObject prefab, string name)
    {
        UIShellRouteButtonView route = Find(prefab.transform, name)?.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(route, name);
        Assert.AreEqual(UiShellRouteIntent.BackMenuRoute, route.Intent, name);
        Assert.AreEqual(UIRoute.MainMenu, route.Route, name);
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, PrefabPath);
        return prefab;
    }

    private static GameObject RegionChild(UIShellView shell, UIShellRegionId id)
    {
        Assert.IsTrue(shell.TryGetRegion(id, out UIShellRegionView region));
        Assert.NotNull(region.ContentRoot);
        Assert.Greater(region.ContentRoot.childCount, 0, id.ToString());
        return region.ContentRoot.GetChild(0).gameObject;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }
        return null;
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
