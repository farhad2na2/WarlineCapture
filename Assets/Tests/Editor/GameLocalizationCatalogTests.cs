using System.Collections.Generic;
using Game.Configs;
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class GameLocalizationCatalogTests
{
    private GameLocalizationCatalog catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = ScriptableObject.CreateInstance<GameLocalizationCatalog>();
        catalog.Configure(
            GameLocalization.EnglishLocaleCode,
            new[]
            {
                new GameLocaleTable(
                    GameLocalization.EnglishLocaleCode,
                    "English",
                    "EN",
                    false,
                    null,
                    new[]
                    {
                        new GameLocalizedStringRecord("ui.continue", "CONTINUE"),
                        new GameLocalizedStringRecord("ui.dynamic", "Dynamic panel"),
                        new GameLocalizedStringRecord(
                            "ui.passengers",
                            "PASSENGERS {0}/{1} | SOLDIERS {2}/{3}")
                    }),
                new GameLocaleTable(
                    GameLocalization.PersianLocaleCode,
                    "فارسی",
                    "FA",
                    true,
                    null,
                    new[]
                    {
                        new GameLocalizedStringRecord("ui.continue", "ادامه"),
                        new GameLocalizedStringRecord("ui.dynamic", "پنل پویا"),
                        new GameLocalizedStringRecord(
                            "ui.passengers",
                            "مسافران {0}/{1} | سربازان {2}/{3}")
                    }),
                new GameLocaleTable(
                    "de",
                    "Deutsch",
                    "DE",
                    false,
                    null,
                    new[] { new GameLocalizedStringRecord("ui.continue", "WEITER") })
            });
    }

    [TearDown]
    public void TearDown()
    {
        GameText.Init(null);
        GameLocalization.Initialize(null, GameLocalization.EnglishLocaleCode, persist: false);
        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void LocaleLookup_UsesStableKeyAndEnglishFallback()
    {
        GameLocalization.Initialize(catalog, GameLocalization.PersianLocaleCode, persist: false);

        Assert.AreEqual("ادامه", GameLocalization.Get("ui.continue", "CONTINUE"));
        Assert.AreEqual("Missing fallback", GameLocalization.Get("ui.missing", "Missing fallback"));
        Assert.IsTrue(GameLocalization.IsRightToLeft);
    }

    [Test]
    public void RuntimeSourceLookup_CoversDynamicallyCreatedUi()
    {
        GameLocalization.Initialize(catalog, GameLocalization.PersianLocaleCode, persist: false);

        Assert.IsTrue(GameLocalization.TryGetBySource("Dynamic panel", out string key, out string value));
        Assert.AreEqual("ui.dynamic", key);
        Assert.AreEqual("پنل پویا", value);
    }

    [Test]
    public void GameTextTryGet_ResolvesKeysOwnedOnlyBySharedCatalog()
    {
        GameText.Init(null);
        GameLocalization.Initialize(catalog, GameLocalization.PersianLocaleCode, persist: false);

        Assert.IsTrue(GameText.TryGet("ui.continue", out string value));
        Assert.AreEqual("ادامه", value);
    }

    [Test]
    public void RuntimeSourceLookup_LocalizesConfiguredDynamicTemplates()
    {
        GameLocalization.Initialize(catalog, GameLocalization.PersianLocaleCode, persist: false);

        Assert.IsTrue(GameLocalization.TryGetBySource(
            "PASSENGERS 2/4 | SOLDIERS 2/3",
            out string key,
            out string value));
        Assert.AreEqual("ui.passengers", key);
        Assert.AreEqual("مسافران 2/4 | سربازان 2/3", value);
    }

    [Test]
    public void LocalizedRuntimeValue_CanRecoverItsEnglishTemplateForLocaleSwitching()
    {
        GameLocalization.Initialize(catalog, GameLocalization.PersianLocaleCode, persist: false);

        Assert.IsTrue(GameLocalization.TryGetSourceByLocalized(
            "مسافران 2/4 | سربازان 2/3",
            out string key,
            out string source));
        Assert.AreEqual("ui.passengers", key);
        Assert.AreEqual("PASSENGERS 2/4 | SOLDIERS 2/3", source);
    }

    [Test]
    public void LocaleMetadata_DrivesSettingsWithoutScreenSpecificLanguageLists()
    {
        GameLocalization.Initialize(catalog, "de", persist: false);

        CollectionAssert.AreEqual(new[] { "EN", "FA", "DE" }, GameLocalization.GetLocaleShortLabels());
        Assert.AreEqual(2, GameLocalization.GetLocaleIndex("de"));
        Assert.AreEqual("de", GameLocalization.GetLocaleCode(2));
        Assert.AreEqual("WEITER", GameLocalization.Get("ui.continue", "CONTINUE"));
    }

    [Test]
    public void MissingTranslation_FallsBackToEnglishSource()
    {
        GameLocalization.Initialize(catalog, "de", persist: false);

        Assert.AreEqual("Dynamic panel", GameLocalization.Get("ui.dynamic", "fallback"));
    }

    [Test]
    public void PersianSeeder_PreservesReviewedValuesAndRejectsUnknownProse()
    {
        Dictionary<string, string> english = new()
        {
            ["known"] = "MISSION BRIEFING",
            ["reviewed"] = "CONTINUE",
            ["unknown"] = "Unreviewed prose that is not in the project glossary"
        };
        Dictionary<string, string> persian = new()
        {
            ["reviewed"] = "ترجمه بازبینی‌شده"
        };

        int added = V3PersianUiTranslationSeeder.FillMissing(english, persian);

        Assert.AreEqual(1, added);
        Assert.AreEqual("توجیه مأموریت", persian["known"]);
        Assert.AreEqual("ترجمه بازبینی‌شده", persian["reviewed"]);
        Assert.IsFalse(persian.ContainsKey("unknown"));
    }

    [Test]
    public void SettingsLocaleModel_CarriesFutureLocaleCodeWithoutANewScreenEnum()
    {
        LocalizationSettingsModel model = default;
        model = SettingsService.SetLocaleCode(model, "de");

        Assert.AreEqual("de", SettingsService.ResolveLocaleCode(model));
    }

    [Test]
    public void EveryV3UiPrefab_HasSharedEnglishAndPersianCoverage()
    {
        V3UiLocalizationCatalogBuilder.ValidateBindingsAndCoverage();
    }
}
