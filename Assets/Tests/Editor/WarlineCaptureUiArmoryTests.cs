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

        AssertChildren(
            prefab.transform,
            "ShellFill",
            "ShellFrame",
            "HeaderBar",
            "HeaderBar/BackButton/IconImage",
            "HeaderBar/TitleText",
            "HeaderBar/CreditsCounter/IconImage",
            "HeaderBar/MaterialsCounter/ValueText",
            "CategoryRail/UnitsButton/IconImage",
            "CategoryRail/VehiclesButton/LabelText",
            "RosterPanel",
            "RosterPanel/RifleSquadCard/ArtImage",
            "RosterPanel/ApcArmorCard/ProgressFrame",
            "RosterPanel/ApcArmorCard/ProgressFill",
            "InspectionPanel",
            "InspectionPanel/SelectedArtImage",
            "InspectionPanel/TierTrack/TierPip_1",
            "InspectionPanel/PartsProgress/ProgressFrame",
            "InspectionPanel/ArmorStatRow/IconImage",
            "InspectionPanel/UpgradeLockedButton/LockIcon",
            "BottomTabBar/OwnedTab/LabelText",
            "DisabledReasonPanel/WarningIcon");
    }

    [Test]
    public void ArmoryScreen_UsesOneGoLayerPackAssets()
    {
        GameObject prefab = LoadPrefab();

        AssertImageSpritePath(prefab.transform, "ShellFrame", "screen_shell_frame.png");
        AssertImageSpritePath(prefab.transform, "HeaderBar", "header_bar_frame.png");
        AssertImageSpritePath(prefab.transform, "HeaderBar/CreditsCounter", "resource_counter_frame.png");
        AssertImageSpritePath(prefab.transform, "CategoryRail/UnitsButton", "category_button_selected_background.png");
        AssertImageSpritePath(prefab.transform, "CategoryRail/VehiclesButton", "category_button_normal_background.png");
        AssertImageSpritePath(prefab.transform, "RosterPanel", "roster_panel_frame.png");
        AssertImageSpritePath(prefab.transform, "RosterPanel/ApcArmorCard", "item_card_selected_frame.png");
        AssertImageSpritePath(prefab.transform, "RosterPanel/ApcArmorCard/ArtImage", "art_apc_armor.png");
        AssertImageSpritePath(prefab.transform, "InspectionPanel", "inspection_panel_frame.png");
        AssertImageSpritePath(prefab.transform, "InspectionPanel/SelectedArtImage", "art_apc_armor.png");
        AssertImageSpritePath(prefab.transform, "InspectionPanel/UpgradeLockedButton", "disabled_primary_button_background.png");
        AssertImageSpritePath(prefab.transform, "DisabledReasonPanel", "disabled_reason_frame.png");
    }

    [Test]
    public void ArmoryScreen_PreservesLiveTextAndDisabledUpgradeContract()
    {
        GameObject prefab = LoadPrefab();

        AssertText(prefab.transform, "HeaderBar/TitleText", "ARMORY");
        AssertText(prefab.transform, "HeaderBar/CreditsCounter/ValueText", "125,430");
        AssertText(prefab.transform, "CategoryRail/UnitsButton/LabelText", "UNITS");
        AssertText(prefab.transform, "RosterPanel/ApcArmorCard/TitleText", "APC ARMOR");
        AssertText(prefab.transform, "InspectionPanel/TitleText", "APC ARMOR UPGRADE");
        AssertText(prefab.transform, "InspectionPanel/PartsProgress/ValueText", "18 / 40");
        AssertText(prefab.transform, "InspectionPanel/UnlockSourceText", "Unlock source: Chapter 1 M03 Reward");

        Button upgradeButton = prefab.transform.Find("InspectionPanel/UpgradeLockedButton").GetComponent<Button>();
        Assert.NotNull(upgradeButton);
        Assert.IsFalse(upgradeButton.interactable, "Upgrade action remains disabled until upgrade service and inventory persistence exist.");
        AssertText(prefab.transform, "InspectionPanel/UpgradeLockedButton/LabelText", "NOT ENOUGH PARTS");
        AssertText(prefab.transform, "DisabledReasonPanel/ReasonText", "Upgrade service and inventory persistence pending.");
    }

    [Test]
    public void ArmoryScreen_UsesAnimatedTabAndCardButtons()
    {
        GameObject prefab = LoadPrefab();

        AssertAnimatedButton(prefab, "CategoryRail/UnitsButton", true);
        AssertAnimatedButton(prefab, "CategoryRail/VehiclesButton", false);
        AssertAnimatedButton(prefab, "RosterPanel/ApcArmorCard", true);
        AssertAnimatedButton(prefab, "BottomTabBar/OwnedTab", true);
        AssertAnimatedButton(prefab, "BottomTabBar/PartsTab", false);
    }

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArmoryPrefabPath);
        Assert.NotNull(prefab, ArmoryPrefabPath);
        return prefab;
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
