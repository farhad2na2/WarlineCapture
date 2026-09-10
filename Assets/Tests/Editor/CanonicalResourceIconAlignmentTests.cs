using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;

public sealed class CanonicalResourceIconAlignmentTests
{
    private const string MainMenuPrefab =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
    private const string MatchHudPrefab =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string BuildDrawerPrefab =
        "Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab";
    private const string PlacementBarPrefab =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab";
    private const string ResourceExchangePrefab =
        "Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab";
    private const string MissionBriefingPrefab =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab";
    private const string MissionResultPrefab =
        "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab";

    private const string CreditsIcon = "Assets/Game/Art/UI/Resources/resource_credits.png";
    private const string CommandIcon = "Assets/Game/Art/UI/Resources/resource_command.png";
    private const string MaterialsIcon = "Assets/Game/Art/UI/Resources/resource_materials.png";
    private const string OilIcon = "Assets/Game/Art/UI/Resources/resource_oil.png";
    private const string FuelIcon = "Assets/Game/Art/UI/Resources/resource_fuel.png";
    private const string RushIcon = "Assets/Game/Art/UI/Resources/resource_rush.png";
    private const string V3MaterialsIcon = "Assets/Game/Art/UI/Generated/V3Shared/Icons/v3_icon_materials.png";
    private const string V3OilIcon = "Assets/Game/Art/UI/Generated/V3Shared/Icons/v3_icon_oil.png";
    private const string V3FuelIcon = "Assets/Game/Art/UI/Generated/V3Shared/Icons/v3_icon_fuel.png";

    [Test]
    public void PersistentHeader_UsesOnlyCanonicalPersistentResourceIcons()
    {
        AssertUses(MainMenuPrefab, CreditsIcon, CommandIcon);
        AssertDoesNotUse(MainMenuPrefab, MaterialsIcon, OilIcon, FuelIcon);
    }

    [Test]
    public void MatchSurfaces_UseOnlyCanonicalMatchResourceIcons()
    {
        AssertUses(MatchHudPrefab, V3MaterialsIcon, V3OilIcon, V3FuelIcon);
        var hud=AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudPrefab);
        var header=hud.GetComponentsInChildren<Transform>(true).Single(item=>item.name=="ResourceStrip");
        Assert.That(header, Is.Not.Null);
        var sprites=header.GetComponentsInChildren<Image>(true).Select(image=>AssetDatabase.GetAssetPath(image.sprite)).ToArray();
        Assert.That(sprites, Does.Not.Contain(CreditsIcon));
        Assert.That(sprites, Does.Not.Contain(CommandIcon));

        AssertUses(BuildDrawerPrefab, V3MaterialsIcon, V3OilIcon, V3FuelIcon);
        AssertDoesNotUse(BuildDrawerPrefab, CreditsIcon, CommandIcon, MaterialsIcon, OilIcon, FuelIcon);

        AssertUses(PlacementBarPrefab, V3MaterialsIcon, V3OilIcon, V3FuelIcon);
        AssertDoesNotUse(PlacementBarPrefab, CreditsIcon, CommandIcon, MaterialsIcon, OilIcon, FuelIcon);

        AssertUses(ResourceExchangePrefab, MaterialsIcon, OilIcon, FuelIcon, RushIcon);
        AssertDoesNotUse(ResourceExchangePrefab, CreditsIcon, CommandIcon);
    }

    [Test]
    public void RewardSurfaces_UseCanonicalIconsForDisplayedCanonicalResources()
    {
        AssertUses(MissionBriefingPrefab, CreditsIcon);
        AssertUses(MissionResultPrefab, CreditsIcon, MaterialsIcon);
    }

    private static void AssertUses(string prefabPath, params string[] iconPaths)
    {
        string yaml = ReadPrefab(prefabPath);
        for (int i = 0; i < iconPaths.Length; i++)
        {
            string guid = RequireGuid(iconPaths[i]);
            Assert.That(
                yaml,
                Does.Contain($"guid: {guid}"),
                $"{prefabPath} does not use canonical icon {iconPaths[i]}.");
        }
    }

    private static void AssertDoesNotUse(string prefabPath, params string[] iconPaths)
    {
        string yaml = ReadPrefab(prefabPath);
        for (int i = 0; i < iconPaths.Length; i++)
        {
            string guid = RequireGuid(iconPaths[i]);
            Assert.That(
                yaml,
                Does.Not.Contain($"guid: {guid}"),
                $"{prefabPath} incorrectly uses {iconPaths[i]}.");
        }
    }

    private static string ReadPrefab(string prefabPath)
    {
        Assert.That(File.Exists(prefabPath), Is.True, $"Missing prefab {prefabPath}.");
        return File.ReadAllText(prefabPath);
    }

    private static string RequireGuid(string assetPath)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        Assert.That(guid, Is.Not.Empty, $"Missing canonical resource icon {assetPath}.");
        return guid;
    }
}
#endif
