using System;
using System.Reflection;
using Game.Composition;
using Game.Configs;
using Game.UI.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class GameTextResolverAdapterTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new GameTextResolverAdapterTests();
            tests.Get_PreservesConfiguredAndFallbackBehavior();
            tests.TryGet_PreservesConfiguredAndMissingBehavior();
            tests.Format_FormatsConfiguredText();
            tests.Format_ReturnsFallbackWhenConfiguredFormatIsInvalid();
            tests.Format_FormatsFallbackWhenKeyIsMissing();
            tests.SubsystemRegistrationReset_ClearsPreviousTextAndAudioMappings();
            Debug.Log("[GameTextResolverAdapterValidation] result=Passed tests=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[GameTextResolverAdapterValidation] result=Failed");
            ValidationExit.Failed();
        }
        finally
        {
            ResetRuntimeState();
        }
    }

    [TearDown]
    public void TearDown()
    {
        ResetRuntimeState();
    }

    [Test]
    public void Get_PreservesConfiguredAndFallbackBehavior()
    {
        Configure("status.ready", "Ready");
        IGameTextResolver resolver = new GameTextResolverAdapter();

        Assert.AreEqual("Ready", resolver.Get("status.ready", "Fallback"));
        Assert.AreEqual("Unavailable", resolver.Get("status.missing", "Unavailable"));
        Assert.AreEqual("status.missing", resolver.Get("status.missing", null));
        Assert.AreEqual(string.Empty, resolver.Get(" ", null));
    }

    [Test]
    public void TryGet_PreservesConfiguredAndMissingBehavior()
    {
        Configure("status.ready", "Ready");
        IGameTextResolver resolver = new GameTextResolverAdapter();

        Assert.IsTrue(resolver.TryGet("status.ready", out string configured));
        Assert.AreEqual("Ready", configured);
        Assert.IsFalse(resolver.TryGet("status.missing", out string missing));
        Assert.IsNull(missing);
        Assert.IsFalse(resolver.TryGet(" ", out string blank));
        Assert.AreEqual(string.Empty, blank);
    }

    [Test]
    public void Format_FormatsConfiguredText()
    {
        Configure("unit.selected", "Selected {0}");
        IGameTextResolver resolver = new GameTextResolverAdapter();

        Assert.AreEqual("Selected Alpha", resolver.Format("unit.selected", "Fallback {0}", "Alpha"));
    }

    [Test]
    public void Format_ReturnsFallbackWhenConfiguredFormatIsInvalid()
    {
        Configure("unit.selected", "Selected {0");
        IGameTextResolver resolver = new GameTextResolverAdapter();

        Assert.AreEqual("Fallback {0}", resolver.Format("unit.selected", "Fallback {0}", "Alpha"));
    }

    [Test]
    public void Format_FormatsFallbackWhenKeyIsMissing()
    {
        GameText.Init(null);
        IGameTextResolver resolver = new GameTextResolverAdapter();

        Assert.AreEqual("Fallback Alpha", resolver.Format("unit.missing", "Fallback {0}", "Alpha"));
    }

    [Test]
    public void SubsystemRegistrationReset_ClearsPreviousTextAndAudioMappings()
    {
        Configure("status.old", "Old", "ui.status.old");
        Assert.IsTrue(GameText.IsInitialized);
        Assert.AreEqual("Old", GameText.Get("status.old"));
        Assert.AreEqual("ui.status.old", GameText.GetAudioEventId("status.old"));

        ResetRuntimeState();

        Assert.IsFalse(GameText.IsInitialized);
        Assert.IsFalse(GameText.TryGet("status.old", out _));
        Assert.IsFalse(GameText.TryGetAudioEventId("status.old", out _));

        Configure("status.new", "New", "ui.status.new");
        Assert.AreEqual("New", GameText.Get("status.new"));
        Assert.AreEqual("ui.status.new", GameText.GetAudioEventId("status.new"));
        Assert.IsFalse(GameText.TryGet("status.old", out _));
    }

    private static void Configure(string key, string value, string audioEventId = null)
    {
        GameStringsConfig config = ScriptableObject.CreateInstance<GameStringsConfig>();
        try
        {
            var entry = new GameStringConfigEntry();
            SetPrivateField(entry, "key", key);
            SetPrivateField(entry, "value", value);
            SetPrivateField(entry, "audioEventId", audioEventId);
            config.Entries.Add(entry);
            GameText.Init(config);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    private static void ResetRuntimeState()
    {
        MethodInfo method = typeof(GameText).GetMethod(
            "ResetRuntimeState",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method, "Expected GameText subsystem reset boundary.");
        method.Invoke(null, null);
    }

    private static void SetPrivateField<T>(T target, string fieldName, object value)
    {
        FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Expected private field '{fieldName}' on {typeof(T).Name}.");
        field.SetValue(target, value);
    }
}
