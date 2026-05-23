using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiArmoryTests
{
    private const string ArmoryPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_Armory.prefab";
    private const string ArmoryLayerRoot = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo";

    [Test]
    public void ArmoryScreen_HasLayeredVisualLockHierarchy()
    {
        GameObject prefab = LoadPrefab();
        WarlineCaptureScreenController controller = prefab.GetComponent<WarlineCaptureScreenController>();
        Assert.NotNull(controller);
        Assert.AreEqual(WarlineCaptureRoute.Armory, controller.Route);
        Assert.IsNull(prefab.GetComponent<Image>(), "Armory must not bake the full target image into the screen root.");

        Transform visual = VisualRoot(prefab);
        AssertChildren(
            visual,
            "ShellFill_Viewport/ShellFill",
            "HeaderBar/LogoPanel",
            "HeaderBar/BackButton/IconImage",
            "HeaderBar/TitleText",
            "HeaderBar/CreditsCounter/IconImage",
            "HeaderBar/SuppliesCounter/ValueText",
            "CategoryRail/UnitsButton/IconImage",
            "CategoryRail/VehiclesButton/LabelText",
            "RosterPanel",
            "RosterPanel/RiflemanCard/ArtImage_Viewport/ArtImage",
            "RosterPanel/RiflemanCard/Progress/ProgressFrame",
            "RosterPanel/RiflemanCard/Progress/ProgressFill",
            "RosterPanel/TransportHelicopterCard/StateIcon",
            "InspectionPanel/Frame",
            "InspectionPanel/SelectedArtImage_Viewport/SelectedArtImage",
            "InspectionPanel/PartsProgress/ProgressFrame",
            "InspectionPanel/HealthStatRow/IconImage",
            "InspectionPanel/MoveAbility/IconImage",
            "InspectionPanel/EquipButton/DisabledIcon",
            "BottomTabBar/OwnedTab/LabelText",
            "DisabledReasonPanel/WarningIcon");
    }

    [Test]
    public void ArmoryScreen_UsesOneGoLayerPackAssets()
    {
        GameObject prefab = LoadPrefab();
        Transform visual = VisualRoot(prefab);

        AssertImageSpritePath(visual, "ShellFill_Viewport/ShellFill", "scn19_background_21x9_no_ui.png");
        AssertImageSpritePath(visual, "HeaderBar/LogoPanel", "scn19_header_logo_panel_bg.png");
        AssertImageSpritePath(visual, "HeaderBar/ResourcePanel", "scn19_header_resource_panel_bg.png");
        AssertImageSpritePath(visual, "HeaderBar/CreditsCounter/IconImage", "scn19_resource_credits_coin.png");
        AssertImageSpritePath(visual, "CategoryRail/UnitsButton", "scn19_category_button_selected_frame.png");
        AssertImageSpritePath(visual, "CategoryRail/VehiclesButton", "scn19_category_button_default_frame.png");
        AssertImageSpritePath(visual, "RosterPanel/RiflemanCard", "scn19_roster_card_selected_frame.png");
        AssertImageSpritePath(visual, "RosterPanel/RiflemanCard/ArtImage_Viewport/ArtImage", "scn19_art_rifleman_male_ii.png");
        AssertImageSpritePath(visual, "InspectionPanel/Frame", "scn19_inspection_panel_frame.png");
        AssertImageSpritePath(visual, "InspectionPanel/SelectedArtImage_Viewport/SelectedArtImage", "scn19_art_rifleman_male_ii.png");
        AssertImageSpritePath(visual, "InspectionPanel/UpgradeButton", "scn19_cta_primary_gold_frame.png");
        AssertImageSpritePath(visual, "InspectionPanel/EquipButton", "scn19_cta_disabled_frame.png");
        AssertImageSpritePath(visual, "DisabledReasonPanel/Frame", "scn19_small_status_chip_frame.png");
    }

    [Test]
    public void ArmoryScreen_PreservesLiveTextAndDisabledUpgradeContract()
    {
        GameObject prefab = LoadPrefab();
        Transform visual = VisualRoot(prefab);

        AssertText(visual, "HeaderBar/TitleText", "ARMORY");
        AssertText(visual, "HeaderBar/CreditsCounter/ValueText", "187,540");
        AssertText(visual, "CategoryRail/UnitsButton/LabelText", "UNITS");
        AssertText(visual, "RosterPanel/RiflemanCard/TitleText", "RIFLEMAN MALE II");
        AssertText(visual, "InspectionPanel/TitleText", "RIFLEMAN MALE II");
        AssertText(visual, "InspectionPanel/PartsProgress/ValueText", "38 / 60");
        AssertText(visual, "InspectionPanel/SourceRow/ValueText", "Barracks Level 4");

        Button equipButton = visual.Find("InspectionPanel/EquipButton").GetComponent<Button>();
        Assert.NotNull(equipButton);
        Assert.IsFalse(equipButton.interactable, "Equip remains disabled until roster equipment persistence exists.");
        AssertText(visual, "InspectionPanel/EquipButton/LabelText", "EQUIP");
        AssertText(visual, "DisabledReasonPanel/ReasonText", "Equip is disabled until roster equipment persistence is connected.");
    }

    [Test]
    public void ArmoryScreen_UsesAnimatedTabAndCardButtons()
    {
        GameObject prefab = LoadPrefab();
        Transform visual = VisualRoot(prefab);

        AssertAnimatedButton(visual, "CategoryRail/UnitsButton", true);
        AssertAnimatedButton(visual, "CategoryRail/VehiclesButton", false);
        AssertAnimatedButton(visual, "RosterPanel/RiflemanCard", true);
        AssertAnimatedButton(visual, "BottomTabBar/OwnedTab", true);
        AssertAnimatedButton(visual, "BottomTabBar/PartsTab", false);
    }

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArmoryPrefabPath);
        Assert.NotNull(prefab, ArmoryPrefabPath);
        return prefab;
    }

    private static Transform VisualRoot(GameObject prefab)
    {
        Transform visual = prefab.transform.Find("SCN19_LayeredCanvas");
        Assert.NotNull(visual);
        return visual;
    }

    private static void AssertChildren(Transform root, params string[] paths)
    {
        foreach (string path in paths)
            Assert.NotNull(root.Find(path), path);
    }

    private static void AssertImageSpritePath(Transform root, string path, string expectedFileName)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.AreEqual($"{ArmoryLayerRoot}/{expectedFileName}", AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertText(Transform root, string path, string expected)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        TMP_Text text = target.GetComponent<TMP_Text>();
        Assert.NotNull(text, path);
        Assert.AreEqual(expected, text.text, path);
    }

    private static void AssertAnimatedButton(Transform root, string path, bool selected)
    {
        Transform target = root.Find(path);
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
