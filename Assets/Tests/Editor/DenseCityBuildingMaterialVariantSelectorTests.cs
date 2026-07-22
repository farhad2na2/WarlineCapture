using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityBuildingMaterialVariantSelectorTests
{
    [Test]
    public void Select_PreservesExistingPositionSeedFacadeChoice()
    {
        DenseCityBuildingMaterialSelection selection =
            DenseCityBuildingMaterialVariantSelector.Select(
                new Vector3(10f, 0f, -5f),
                24681357u,
                GeneratedCityBuildingRole.House,
                true,
                false);

        Assert.That(selection.ApplyVariant, Is.True);
        Assert.That(selection.FacadeTintIndex, Is.EqualTo(3));
        Assert.That(selection.PaletteIndex, Is.EqualTo(3));
    }

    [Test]
    public void Select_PreservesExistingShop05OriginalAndToneRules()
    {
        DenseCityBuildingMaterialSelection original =
            DenseCityBuildingMaterialVariantSelector.Select(
                FindOriginalShopPosition(),
                24681357u,
                GeneratedCityBuildingRole.Shop,
                true,
                true);
        DenseCityBuildingMaterialSelection recolored =
            DenseCityBuildingMaterialVariantSelector.Select(
                new Vector3(10f, 0f, -5f),
                24681357u,
                GeneratedCityBuildingRole.Shop,
                true,
                true);

        Assert.That(original.UseOriginalShopMaterial, Is.True);
        Assert.That(original.ShopToneIndex, Is.EqualTo(-1));
        Assert.That(original.PaletteIndex, Is.Zero);
        Assert.That(recolored.UseOriginalShopMaterial, Is.False);
        Assert.That(recolored.ShopToneIndex, Is.EqualTo(3));
        Assert.That(recolored.PaletteIndex, Is.EqualTo(4));
    }

    [Test]
    public void Select_SkipsCivicAndUnsupportedMaterialFamilies()
    {
        Assert.That(
            DenseCityBuildingMaterialVariantSelector.Select(
                Vector3.zero, 1u, GeneratedCityBuildingRole.Civic, true, false).ApplyVariant,
            Is.False);
        Assert.That(
            DenseCityBuildingMaterialVariantSelector.Select(
                Vector3.zero, 1u, GeneratedCityBuildingRole.Other, false, false).ApplyVariant,
            Is.False);
        Assert.That(
            () => DenseCityBuildingMaterialVariantSelector.Select(
                Vector3.zero, 1u, GeneratedCityBuildingRole.None, true, false),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static Vector3 FindOriginalShopPosition()
    {
        for (int x = 0; x < 100; x++)
        {
            Vector3 position = new(x, 0f, 0f);
            DenseCityBuildingMaterialSelection selection =
                DenseCityBuildingMaterialVariantSelector.Select(
                    position,
                    24681357u,
                    GeneratedCityBuildingRole.Shop,
                    true,
                    true);
            if (selection.UseOriginalShopMaterial)
                return position;
        }

        throw new InvalidOperationException("Test range did not contain an original Shop_05 selection.");
    }
}
