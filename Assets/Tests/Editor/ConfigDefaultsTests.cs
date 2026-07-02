using NUnit.Framework;
using UnityEngine;
using Game.Configs;

public sealed class ConfigDefaultsTests
{
    [Test]
    public void UnitGridAuthoringConfig_DefaultsToRequestableCharacterPrice()
    {
        UnitGridAuthoringConfig config = ScriptableObject.CreateInstance<UnitGridAuthoringConfig>();
        try
        {
            Assert.IsTrue(config.CanRequest);
            Assert.AreEqual(10000, config.Price);
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void BuildingDefinitionAuthoringConfig_DefaultsToRequestableBuildingPrice()
    {
        BuildingDefinitionAuthoringConfig config = ScriptableObject.CreateInstance<BuildingDefinitionAuthoringConfig>();
        try
        {
            Assert.IsTrue(config.CanRequest);
            Assert.AreEqual(20000, config.Price);
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }
}
