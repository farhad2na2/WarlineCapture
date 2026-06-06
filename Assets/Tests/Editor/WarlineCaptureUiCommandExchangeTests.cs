using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiCommandExchangeTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_CommandExchange.prefab";
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/CommandExchange/LayeredOneGo";

    [Test]
    public void CommandExchangeScreen_HasLayeredVisualLockHierarchy()
    {
        GameObject prefab = LoadPrefab();
        WarlineCaptureScreenSystem controller = prefab.GetComponent<WarlineCaptureScreenSystem>();
        Assert.NotNull(controller);
        Assert.AreEqual(WarlineCaptureRoute.CommandExchange, controller.Route);
        Assert.IsNull(prefab.GetComponent<Image>(), "Command Exchange must not bake the full target image into the root.");

        AssertChildren(
            prefab.transform,
            "ShellFill",
            "ShellFrame",
            "HeaderBar/BackButton/IconImage",
            "HeaderBar/TitleText",
            "HeaderBar/CreditsCounter/IconImage",
            "HeaderBar/MaterialsCounter/ValueText",
            "HeaderBar/AuthorityCounter/LabelText",
            "CategoryRail/FeaturedButton/IconImage",
            "CategoryRail/ArmoryButton/LabelText",
            "FeaturedPanel/FeaturedArtImage",
            "FeaturedPanel/CreditsReward/ValueText",
            "StarterPacksTitleText",
            "ReconPackCard/ArtImage",
            "BaseBuilderPackCard/PriceButton/LabelText",
            "ShopItemsTitleText",
            "AuthorityItem/ArtImage",
            "IntelDossierItem/PriceButton/LabelText",
            "DisabledPurchaseReasonText");
    }

    [Test]
    public void CommandExchangeScreen_UsesOneGoLayerPackAssets()
    {
        GameObject prefab = LoadPrefab();

        AssertImageSpritePath(prefab.transform, "ShellFrame", "Frames/screen_shell_frame.png");
        AssertImageSpritePath(prefab.transform, "HeaderBar", "Frames/header_bar_frame.png");
        AssertImageSpritePath(prefab.transform, "HeaderBar/CreditsCounter", "Frames/resource_counter_frame.png");
        AssertImageSpritePath(prefab.transform, "CategoryRail/FeaturedButton", "Buttons/nav_button_selected_background.png");
        AssertImageSpritePath(prefab.transform, "CategoryRail/ResourcesButton", "Buttons/nav_button_normal_background.png");
        AssertImageSpritePath(prefab.transform, "FeaturedPanel", "Cards/featured_offer_card_frame.png");
        AssertImageSpritePath(prefab.transform, "FeaturedPanel/FeaturedArtImage", "Content/art_recon_case.png");
        AssertImageSpritePath(prefab.transform, "ReconPackCard", "Cards/starter_pack_card_frame.png");
        AssertImageSpritePath(prefab.transform, "MaterialCacheItem", "Cards/shop_item_card_frame.png");
        AssertImageSpritePath(prefab.transform, "MaterialCacheItem/ArtImage", "Content/art_material_cache.png");
    }

    [Test]
    public void CommandExchangeScreen_PreservesLiveTextAndDisabledPurchaseContract()
    {
        GameObject prefab = LoadPrefab();

        AssertText(prefab.transform, "HeaderBar/TitleText", "COMMAND EXCHANGE");
        AssertText(prefab.transform, "CategoryRail/FeaturedButton/LabelText", "FEATURED");
        AssertText(prefab.transform, "FeaturedPanel/TitleText", "RECON STARTER PACK");
        AssertText(prefab.transform, "FeaturedPanel/AuthorityReward/LabelText", "COMMAND AUTHORITY");
        AssertText(prefab.transform, "ReconPackCard/TitleText", "RECON PACK");
        AssertText(prefab.transform, "AuthorityItem/TitleText", "COMMAND\nAUTHORITY");
        AssertText(prefab.transform, "DisabledPurchaseReasonText", "Purchases disabled until wallet, receipt validation, catalog, and grant services are implemented.");

        AssertPriceDisabled(prefab.transform, "FeaturedPanel/PriceButton");
        AssertPriceDisabled(prefab.transform, "ReconPackCard/PriceButton");
        AssertPriceDisabled(prefab.transform, "AuthorityItem/PriceButton");
        AssertPriceDisabled(prefab.transform, "NightOpsItem/PriceButton");
    }

    [Test]
    public void CommandExchangeScreen_UsesAnimatedTabAndProductButtons()
    {
        GameObject prefab = LoadPrefab();

        AssertAnimatedButton(prefab, "CategoryRail/FeaturedButton", true);
        AssertAnimatedButton(prefab, "CategoryRail/StarterPacksButton", false);
        AssertAnimatedButton(prefab, "ReconPackCard", true);
        AssertAnimatedButton(prefab, "CreditCacheItem", false);
        AssertAnimatedButton(prefab, "FeaturedPanel/PriceButton", false);
    }

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, PrefabPath);
        return prefab;
    }

    private static void AssertChildren(Transform root, params string[] paths)
    {
        foreach (string path in paths)
            Assert.NotNull(root.Find(path), path);
    }

    private static void AssertImageSpritePath(Transform root, string path, string expectedRelativePath)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.AreEqual($"{LayerRoot}/{expectedRelativePath}", AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertText(Transform root, string path, string expected)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        TMP_Text text = target.GetComponent<TMP_Text>();
        Assert.NotNull(text, path);
        Assert.AreEqual(expected, text.text, path);
    }

    private static void AssertPriceDisabled(Transform root, string path)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        Button button = target.GetComponent<Button>();
        Assert.NotNull(button, path);
        Assert.IsFalse(button.interactable, path);
    }

    private static void AssertAnimatedButton(GameObject prefab, string path, bool selected)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        Button button = target.GetComponent<Button>();
        Assert.NotNull(button, path);
        Assert.AreEqual(Selectable.Transition.Animation, button.transition, path);
        Assert.NotNull(target.GetComponent<Animator>(), path);

        WarlineCaptureButtonAnimationState state = target.GetComponent<WarlineCaptureButtonAnimationState>();
        Assert.NotNull(state, path);
        var serialized = new SerializedObject(state);
        Assert.AreEqual(selected ? "Selected" : "Normal", serialized.FindProperty("initialStateName").stringValue, path);
        Assert.AreEqual(selected, serialized.FindProperty("selectWithEventSystem").boolValue, path);
    }
}
