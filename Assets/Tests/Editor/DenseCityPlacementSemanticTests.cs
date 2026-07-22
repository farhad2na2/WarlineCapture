using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityPlacementSemanticTests
{
    [Test]
    public void PrefabFootprint_AcceptsSupportedExplicitPlacementCategories()
    {
        var prefab = new GameObject("PlacementSemanticTest");
        try
        {
            DenseCityPresentationCategory[] categories =
            {
                DenseCityPresentationCategory.GameplayBuildingIntact,
                DenseCityPresentationCategory.Vegetation,
                DenseCityPresentationCategory.Prop
            };
            for (int index = 0; index < categories.Length; index++)
            {
                DenseCityPresentationCategory category = categories[index];
                var footprint = new DenseMiddleEasternCityEditModeBuilder.PrefabFootprint(
                    prefab,
                    8f,
                    6f,
                    4f,
                    1f,
                    category);

                Assert.That(footprint.PresentationCategory, Is.EqualTo(category));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void PrefabFootprint_RejectsUnclassifiedPlacementCategory()
    {
        var prefab = new GameObject("PlacementSemanticTest");
        try
        {
            Assert.That(
                () => new DenseMiddleEasternCityEditModeBuilder.PrefabFootprint(
                    prefab,
                    8f,
                    6f,
                    4f,
                    1f,
                    DenseCityPresentationCategory.Unknown),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }
}
