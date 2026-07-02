using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Game.Configs;
using Game.Runtime;

public sealed class GameStringsTests
{
    [TearDown]
    public void TearDown()
    {
        GameStrings.Init(null);
    }

    [Test]
    public void Format_ReplacesConfiguredPlaceholders()
    {
        GameStringsConfig config = ScriptableObject.CreateInstance<GameStringsConfig>();
        try
        {
            config.Entries.Add(CreateEntry("confirm_destroy", "Are you sure you want to destroy {0}?"));
            GameStrings.Init(config);

            string result = GameStrings.Format("confirm_destroy", "Soldier Tent");

            Assert.AreEqual("Are you sure you want to destroy Soldier Tent?", result);
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void Get_ReturnsKey_WhenEntryIsMissing()
    {
        GameStrings.Init(null);

        Assert.AreEqual("missing_key", GameStrings.Get("missing_key"));
    }

    private static GameStringConfigEntry CreateEntry(string key, string value)
    {
        var entry = new GameStringConfigEntry();
        SetPrivateField(entry, "key", key);
        SetPrivateField(entry, "value", value);
        return entry;
    }

    private static void SetPrivateField<T>(T target, string fieldName, object value)
    {
        FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Expected private field '{fieldName}' on {typeof(T).Name}.");
        field.SetValue(target, value);
    }
}
