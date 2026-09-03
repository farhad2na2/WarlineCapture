using System.Collections.Generic;
using Game.Configs;
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameLocalizationCatalogTests
{
    private const string DisabledUiMarker =
        "[DisabledUiSubMeshValidation] result=Passed generatedSubMesh=Ignored restore=Passed";

    private GameLocalizationCatalog catalog;

    [UnityEditor.MenuItem("Game/Validation/Run Disabled UI SubMesh Focused")]
    public static void RunDisabledUiFocusedValidation()
    {
        GameLocalizationCatalogTests tests = new();
        try
        {
            tests.SetUp();
            tests.DisabledUi_KeepsTextMeshProFontMaterialAndReadableGlyphColor();
            Debug.Log(DisabledUiMarker);
            ValidationExit.Passed();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[DisabledUiSubMeshValidation] result=Failed");
            ValidationExit.Failed();
        }
        finally
        {
            tests.TearDown();
        }
    }

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
                        new GameLocalizedStringRecord("ui.acronym", "APC"),
                        new GameLocalizedStringRecord("ui.dynamic", "Dynamic panel"),
                        new GameLocalizedStringRecord("ui.generic_count", "{0} {1}"),
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
                        new GameLocalizedStringRecord("ui.acronym", "APC"),
                        new GameLocalizedStringRecord("ui.dynamic", "پنل پویا"),
                        new GameLocalizedStringRecord("ui.generic_count", "{0} {1}"),
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
    public void RuntimeSourceLookup_DoesNotTreatPlaceholderOnlyTemplateAsArbitraryCopy()
    {
        GameLocalization.Initialize(catalog, GameLocalization.PersianLocaleCode, persist: false);

        Assert.IsFalse(GameLocalization.TryGetBySource(
            "SELECT STORY LANGUAGE",
            out _,
            out string sourceResult));
        Assert.AreEqual("SELECT STORY LANGUAGE", sourceResult);

        Assert.IsFalse(GameLocalization.TryGetSourceByLocalized(
            "متن داستان فارسی",
            out _,
            out string localizedResult));
        Assert.AreEqual("متن داستان فارسی", localizedResult);
    }

    [Test]
    public void RuntimeBinder_SkipsViewsWithDedicatedNarrativeLocalization()
    {
        GameObject languageRoot = new("LanguageRoot", typeof(FirstLaunchLanguageChoiceView));
        GameObject languageTextObject = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        languageTextObject.transform.SetParent(languageRoot.transform, false);
        GameObject comicRoot = new("ComicRoot", typeof(NarrativeSequenceView));
        GameObject comicTextObject = new("Dialogue", typeof(RectTransform), typeof(TextMeshProUGUI));
        comicTextObject.transform.SetParent(comicRoot.transform, false);
        GameObject ordinaryTextObject = new("Ordinary", typeof(RectTransform), typeof(TextMeshProUGUI));

        try
        {
            Assert.IsTrue(V3LocalizationRuntimeBinder.IsSpecializedNarrativeText(
                languageTextObject.GetComponent<TMP_Text>()));
            Assert.IsTrue(V3LocalizationRuntimeBinder.IsSpecializedNarrativeText(
                comicTextObject.GetComponent<TMP_Text>()));
            Assert.IsFalse(V3LocalizationRuntimeBinder.IsSpecializedNarrativeText(
                ordinaryTextObject.GetComponent<TMP_Text>()));
        }
        finally
        {
            Object.DestroyImmediate(languageRoot);
            Object.DestroyImmediate(comicRoot);
            Object.DestroyImmediate(ordinaryTextObject);
        }
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
    public void PersianLocale_PreservesIntentionalLatinAcronymsInLeftToRightOrder()
    {
        GameLocalization.Initialize(catalog, GameLocalization.PersianLocaleCode, persist: false);
        GameObject root = new("LocalizedAcronym", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text text = root.GetComponent<TMP_Text>();
        V3LocalizedTextBinding binding = root.AddComponent<V3LocalizedTextBinding>();
        binding.Configure("ui.acronym", "APC", observeRuntimeChanges: false);

        try
        {
            binding.ApplyLocalization();

            Assert.AreEqual("APC", text.text);
            Assert.IsFalse(text.isRightToLeftText);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PersianLocale_EnablesSafeAutoSizingAndRestoresEnglishTypography()
    {
        GameLocalization.Initialize(catalog, GameLocalization.PersianLocaleCode, persist: false);
        GameObject root = new("LocalizedSizing", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text text = root.GetComponent<TMP_Text>();
        text.fontSize = 20f;
        text.enableAutoSizing = false;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 72f;
        V3LocalizedTextBinding binding = root.AddComponent<V3LocalizedTextBinding>();
        binding.Configure("ui.continue", "CONTINUE", observeRuntimeChanges: false);

        try
        {
            binding.ApplyLocalization();
            Assert.IsTrue(text.enableAutoSizing);
            Assert.AreEqual(20f, text.fontSizeMax);
            Assert.Less(text.fontSizeMin, text.fontSizeMax);

            GameLocalization.Initialize(catalog, GameLocalization.EnglishLocaleCode, persist: false);
            binding.ApplyLocalization();
            Assert.IsFalse(text.enableAutoSizing);
            Assert.AreEqual(20f, text.fontSize);
            Assert.AreEqual(18f, text.fontSizeMin);
            Assert.AreEqual(72f, text.fontSizeMax);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
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

    [Test]
    public void DisabledUi_KeepsTextMeshProFontMaterialAndReadableGlyphColor()
    {
        GameObject root = new("DisabledTextTest", typeof(RectTransform), typeof(CanvasRenderer));
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        GameObject subMeshObject = new(
            "GeneratedFallbackSubmesh",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TMP_SubMeshUI));
        subMeshObject.transform.SetParent(root.transform, false);
        Material originalMaterial = text.material;
        Color originalColor = new(0.9f, 0.8f, 0.7f, 1f);
        text.color = originalColor;

        try
        {
            UiDisabledMaterialUtility.SetDisabled(
                root,
                UiDisabledVisualReason.MissionRestriction,
                true);

            Assert.AreSame(originalMaterial, text.material,
                "Disabled TMP labels must retain their SDF font material so glyphs remain visible.");
            Assert.GreaterOrEqual(text.color.r, 0.58f);
            Assert.AreEqual(text.color.r, text.color.g, 0.001f);
            Assert.AreEqual(text.color.g, text.color.b, 0.001f);
            Assert.AreEqual(1f, text.color.a);

            Assert.DoesNotThrow(() => UiDisabledMaterialUtility.SetDisabled(
                    root,
                    UiDisabledVisualReason.MissionRestriction,
                    false),
                "Restoring a disabled command must ignore TMP's generated fallback submesh.");
            Assert.AreSame(originalMaterial, text.material);
            Assert.AreEqual(originalColor, text.color);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
