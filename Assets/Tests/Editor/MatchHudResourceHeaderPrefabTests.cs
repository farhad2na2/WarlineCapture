using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudResourceHeaderPrefabTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string MaterialsIconPath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_icon_04_materials_crate.png";
    private const string OilIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/Icons/matchhud_resource_oil_barrels.png";
    private const string FuelIconPath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_icon_06_fuel_jerrycan.png";

    [Test]
    public void MatchHudHeaderUsesCanonicalMatchResourcesAndIcons()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, PrefabPath);

        Transform strip = FindDescendant(prefab.transform, "ResourceStrip");
        Assert.NotNull(strip, "Match HUD must contain ResourceStrip.");
        Assert.IsNull(FindDirectChild(strip, "CreditsSlot"), "Persistent Credits must not appear in the Match HUD.");
        Assert.IsNull(FindDirectChild(strip, "SupplySlot"), "Legacy Supply must be represented as Materials.");
        Assert.AreEqual(5, strip.childCount, "ResourceStrip must contain three match resources, Civilian Risk, and its frame.");

        Assert.AreEqual(0, FindDirectChild(strip, "Frame")?.GetSiblingIndex(), "ResourceStrip frame must render behind its content");
        AssertSlot(strip, "MaterialsSlot", "Materials", MaterialsIconPath, 1);
        AssertSlot(strip, "OilSlot", "Oil", OilIconPath, 2);
        AssertSlot(strip, "FuelSlot", "Fuel", FuelIconPath, 3);
        AssertSlot(strip, "CivilianRiskSlot", "Civilian Risk", null, 4);
    }

    private static void AssertSlot(
        Transform strip,
        string slotName,
        string expectedLabel,
        string expectedIconPath,
        int expectedSiblingIndex)
    {
        Transform slot = FindDirectChild(strip, slotName);
        Assert.NotNull(slot, slotName);
        Assert.AreEqual(expectedSiblingIndex, slot.GetSiblingIndex(), $"{slotName} display order");

        TMP_Text label = FindDirectChild(slot, "Label")?.GetComponent<TMP_Text>();
        Assert.NotNull(label, $"{slotName}/Label");
        Assert.AreEqual(expectedLabel, label.text);

        if (string.IsNullOrEmpty(expectedIconPath))
            return;

        Image icon = FindDirectChild(slot, "Icon")?.GetComponent<Image>();
        Assert.NotNull(icon, $"{slotName}/Icon");
        Assert.NotNull(icon.sprite, $"{slotName}/Icon sprite");
        Assert.AreEqual(expectedIconPath, AssetDatabase.GetAssetPath(icon.sprite));
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDescendant(root.GetChild(i), name);
            if (result != null)
                return result;
        }

        return null;
    }
}
