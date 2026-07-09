using System;
using System.Collections.Generic;
using Game.UI.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class GameTextResolverTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new GameTextResolverTests();
            tests.FallbackResolver_ReturnsFallbackAndDoesNotResolveKey();
            tests.ConfiguredResolver_ReturnsConfiguredTextThroughContract();
            tests.Format_FormatsConfiguredText();
            tests.Format_ReturnsFallbackWhenConfiguredFormatIsInvalid();
            tests.Get_ReturnsFallbackWhenKeyIsMissing();
            Debug.Log("[GameTextResolverValidation] result=Passed tests=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[GameTextResolverValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void FallbackResolver_ReturnsFallbackAndDoesNotResolveKey()
    {
        IGameTextResolver resolver = FallbackGameTextResolver.Instance;

        Assert.AreEqual("Fallback text", resolver.Get("missing.key", "Fallback text"));
        Assert.IsFalse(resolver.TryGet("missing.key", out string value));
        Assert.AreEqual(string.Empty, value);
    }

    [Test]
    public void ConfiguredResolver_ReturnsConfiguredTextThroughContract()
    {
        IGameTextResolver resolver = CreateResolver("status.ready", "Ready");

        Assert.AreEqual("Ready", resolver.Get("status.ready", "Fallback"));
        Assert.IsTrue(resolver.TryGet("status.ready", out string value));
        Assert.AreEqual("Ready", value);
    }

    [Test]
    public void Format_FormatsConfiguredText()
    {
        IGameTextResolver resolver = CreateResolver("unit.selected", "Selected {0}");

        Assert.AreEqual("Selected Alpha", resolver.Format("unit.selected", "Fallback {0}", "Alpha"));
    }

    [Test]
    public void Format_ReturnsFallbackWhenConfiguredFormatIsInvalid()
    {
        IGameTextResolver resolver = CreateResolver("unit.selected", "Selected {0");

        Assert.AreEqual("Fallback {0}", resolver.Format("unit.selected", "Fallback {0}", "Alpha"));
    }

    [Test]
    public void Get_ReturnsFallbackWhenKeyIsMissing()
    {
        IGameTextResolver resolver = CreateResolver("status.ready", "Ready");

        Assert.AreEqual("Unavailable", resolver.Get("status.missing", "Unavailable"));
    }

    private static IGameTextResolver CreateResolver(string key, string value)
    {
        return new ConfiguredGameTextResolver(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [key] = value
        });
    }

    private sealed class ConfiguredGameTextResolver : IGameTextResolver
    {
        private readonly Dictionary<string, string> entries;

        public ConfiguredGameTextResolver(Dictionary<string, string> entries)
        {
            this.entries = entries;
        }

        public string Get(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            return entries.TryGetValue(key, out string value)
                ? value
                : fallback ?? key;
        }

        public bool TryGet(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = string.Empty;
                return false;
            }

            return entries.TryGetValue(key, out value);
        }

        public string Format(string key, string fallback, params object[] args)
        {
            string format = Get(key, fallback);
            if (string.IsNullOrEmpty(format) || args == null || args.Length == 0)
                return format;

            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return fallback ?? format;
            }
        }
    }
}
