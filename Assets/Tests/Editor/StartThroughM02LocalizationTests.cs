using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Composition;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using RTLTMPro;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class StartThroughM02LocalizationTests
{
    private GameLocalizationCatalog catalog;
    private string previousLocale;
    private readonly List<UnityEngine.Object> owned = new();

    [SetUp] public void SetUp()
    {
        previousLocale = GameLocalization.CurrentLocaleCode;
        catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
        GameLocalization.Initialize(catalog, "fa-IR", false);
    }
    [TearDown] public void TearDown()
    {
        foreach (var item in owned) if (item != null)
        {
            if (item is GameObject root)
                foreach (var binding in root.GetComponentsInChildren<V3LocalizedTextBindingView>(true)) typeof(V3LocalizedTextBindingView).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(binding, null);
            UnityEngine.Object.DestroyImmediate(item);
        }
        owned.Clear();
        GameLocalization.Initialize(catalog, previousLocale, false);
    }

    [Test] public void EveryConfiguredStringResolvesThroughSourceAndKeyInEveryPublishedLocale()
    {
        V3UiLocalizationCatalogBuilder.ValidateAllLocaleTables(catalog);
        foreach (var locale in catalog.Locales)
        {
            GameLocalization.SetLocale(locale.LocaleCode, false);
            foreach (var entry in V3UiLocalizationCatalogBuilder.ReadUiStringConfigs())
            {
                string expected = locale.LocaleCode == catalog.SourceLocaleCode ? entry.source :
                    entry.translations.Single(t => t.locale == locale.LocaleCode).value;
                Assert.That(GameLocalization.Get(entry.key), Is.EqualTo(expected), locale.LocaleCode + "/" + entry.key);
                Assert.That(GameLocalization.GetBySource(entry.source), Is.EqualTo(expected), "Source alias: " + entry.source);
            }
        }
    }

    [Test] public void FirstLaunchStaticIdentityAndGuidanceHaveExplicitBindings()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab");
        var view = prefab.GetComponent<NarrativeSequenceView>();
        var serialized = new SerializedObject(view);
        var targets = serialized.FindProperty("localizedTextTargets");
        var bound = new HashSet<UnityEngine.Object>();
        for (int i = 0; i < targets.arraySize; i++) bound.Add(targets.GetArrayElementAtIndex(i).objectReferenceValue);
        var sources = V3UiLocalizationCatalogBuilder.ReadUiStringConfigs().Select(e => e.source).ToHashSet();
        int matched = 0;
        foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
        {
            string source = text is RTLTextMeshPro rtl ? rtl.OriginalText : text.text;
            if (!sources.Contains(source)) continue;
            Assert.That(bound.Contains(text), Is.True, text.name + ": " + source);
            matched++;
        }
        Assert.That(matched, Is.GreaterThanOrEqualTo(40), "Do not silently lose identity/guidance binding coverage.");
    }

    [Test] public void LoadingProgressRendersEveryPhaseAndHidesTechnicalFailureDetails()
    {
        var root = Own(new GameObject("Loading", typeof(RectTransform)));
        var target = MakeText(root.transform);
        var view = root.AddComponent<UIShellLoadingProgressView>();
        view.Configure(null, null, target, 100);
        var apply = typeof(UIShellLoadingProgressView).GetMethod("ApplyProgress", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var entry in V3UiLocalizationCatalogBuilder.ReadUiStringConfigs().Where(e => e.key.StartsWith("ui.loading.") && e.key != "ui.loading.map_unload_failed"))
        {
            apply.Invoke(view, new object[] { .5f, entry.source });
            Assert.That(target.OriginalText, Is.EqualTo(entry.translations.Single(t => t.locale == "fa-IR").value), entry.source);
        }
        apply.Invoke(view, new object[] { .5f, "Map unload failed: internal/path/Exception" });
        Assert.That(target.OriginalText, Is.EqualTo(GameLocalization.Get("ui.loading.map_unload_failed")));
        Assert.That(target.OriginalText, Does.Not.Contain("Exception"));
    }

    [Test] public void DirectWritersReplaceAuthoredBindingsAndSurviveLanguageSwitch()
    {
        var root = Own(new GameObject("Text", typeof(RectTransform)));
        var text = MakeText(root.transform);
        text.text = "CONTINUE";
        var binding = text.gameObject.AddComponent<V3LocalizedTextBindingView>();
        binding.Configure("ui.common.continue", "CONTINUE");
        // EditMode-created behaviours do not receive the player OnEnable callback.
        typeof(V3LocalizedTextBindingView).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(binding, null);
        UiLocalizedText.Set(text, "PLAYER CONTROL");
        Assert.That(text.OriginalText, Is.EqualTo("کنترل بازیکن"));
        GameLocalization.SetLocale("en", false);
        Assert.That(text.OriginalText, Is.EqualTo("PLAYER CONTROL"));
        GameLocalization.SetLocale("fa-IR", false);
        Assert.That(text.OriginalText, Is.EqualTo("کنترل بازیکن"));
        binding.Configure("ui.catalog.structure", "STRUCTURE");
        binding.ApplyLocalization();
        Assert.That(text.OriginalText, Is.EqualTo("سازه"));
    }

    [Test] public void BuildingAndRecruitmentMessagesTranslateMetadataAndAllResourceFailures()
    {
        var resolver = new GameTextResolverAdapter();
        var building = Item(BuildDrawerCategory.Buildings, "Barracks");
        var soldier = Item(BuildDrawerCategory.Soldiers, "Rifleman Male IV");
        foreach (var item in new[] { building, soldier })
        {
            NoLatinWords(BuildDrawerCatalogPresentationSystemHelper.FormatReadyInstruction(resolver, item));
            NoLatinWords(BuildDrawerCatalogPresentationSystemHelper.FormatPrimarySuccessInstruction(resolver, item));
            foreach (var failure in new[] { BuildingUiCommandFailure.InsufficientMaterials, BuildingUiCommandFailure.InsufficientCredits,
                         BuildingUiCommandFailure.InsufficientCreditsAndMaterials, BuildingUiCommandFailure.MissingProducerBuilding,
                         BuildingUiCommandFailure.ProductionQueueFull })
            {
                NoLatinWords(BuildDrawerCatalogPresentationSystemHelper.FormatFailureMessage(resolver, failure, "Barracks", 5));
                NoLatinWords(BuildDrawerCatalogPresentationSystemHelper.FormatInstructionFailureMessage(resolver, failure, "Barracks", item, true, 5));
            }
        }
        Assert.That(UiLocalizedText.CatalogLabel(soldier.DisplayName), Is.EqualTo("تفنگدار مرد ۴"));
        Assert.That(UiLocalizedText.SelectionLabel("Barracks (1006,330)"), Is.EqualTo("سربازخانه (1006,330)"));
        Assert.That(UiLocalizedText.SelectionLabel("ECHO-7"), Is.EqualTo("ECHO-7"));
        NoLatinWords(UiLocalizedText.PlacementStatus("Barracks: Valid placement (1006,330) 40x20"));
        NoLatinWords(UiLocalizedText.PlacementStatus("Blocked by road or blocker (1006,330) 40x20"));
    }

    [Test] public void ProductionQueueReplacesItsAuthoredPlaceholderWithTheLocalizedUnitName()
    {
        var root = Own(new GameObject("Queue", typeof(RectTransform)));
        var target = MakeText(root.transform);
        target.text = "GUARD TOWER";
        var binding = target.gameObject.AddComponent<V3LocalizedTextBindingView>();
        binding.Configure("ui.v3.guard_tower.bbeb962d6a", "GUARD TOWER");
        binding.ApplyLocalization();
        var view = root.AddComponent<BuildDrawerQueueItemView>();
        typeof(BuildDrawerQueueItemView).GetField("nameText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, target);
        view.Bind(1, "Rifleman Male IV", "Barracks", "00:05", .1f, null, true);
        Assert.That(target.OriginalText, Is.EqualTo("تفنگدار مرد ۴"));
    }

    [Test] public void TutorialHeaderUsesSharedLocaleBeforeAndAfterItsFirstReadModel()
    {
        var root = Own(UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab")));
        var view = root.GetComponentInChildren<AriaTutorialBriefingView>(true);
        typeof(AriaTutorialBriefingView).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
        NoLatinWords(view.ProgressText.text);
        // The first read model can still have its legacy RTL flag unset.
        view.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel());
        NoLatinWords(view.ProgressText.text);
        NoLatinWords(view.CurrentInstructionBody);
    }

    [Test] public void FutureLocaleUsesSameRuntimePathsAndIncompleteTablesFailValidation()
    {
        var testCatalog = Own(ScriptableObject.CreateInstance<GameLocalizationCatalog>());
        GameLocalizedStringRecord R(string key, string value) => new(key, value);
        var en = new GameLocaleTable("en", "English", "EN", false, null, new[] {R("b", "Barracks"), R("ready", "PLACE: choose a location for {0}."), R("loading", "Loading match")});
        var de = new GameLocaleTable("de", "Deutsch", "DE", false, null, new[] {R("b", "Kaserne"), R("ready", "PLATZIEREN: Standort für {0} wählen."), R("loading", "Gefecht laden")});
        testCatalog.Configure("en", new[] { en, de });
        Assert.DoesNotThrow(() => V3UiLocalizationCatalogBuilder.ValidateAllLocaleTables(testCatalog));
        GameLocalization.Initialize(testCatalog, "de", false);
        Assert.That(UiLocalizedText.CatalogLabel("Barracks"), Is.EqualTo("Kaserne"));
        var target = MakeText(Own(new GameObject("Future locale", typeof(RectTransform))).transform);
        UiLocalizedText.Set(target, "Loading match");
        Assert.That(target.OriginalText, Is.EqualTo("Gefecht laden"));
        testCatalog.Configure("en", new[] { en, new GameLocaleTable("de", "Deutsch", "DE", false, null, new[] { R("b", "Kaserne"), R("ready", "Fehlender Platzhalter") }) });
        var error = Assert.Throws<InvalidOperationException>(() => V3UiLocalizationCatalogBuilder.ValidateAllLocaleTables(testCatalog));
        Assert.That(error.Message, Does.Contain("missing loading").And.Contain("format arguments differ for ready"));
    }

    private static BuildDrawerCatalogItem Item(BuildDrawerCategory category, string name) => new(category, null, name, "STRUCTURE", "", 90, 150, 30, new Vector2Int(40, 20), null, null, null);
    private static void NoLatinWords(string value) => Assert.That(System.Text.RegularExpressions.Regex.IsMatch(System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", ""), "[A-Za-z]{2,}"), Is.False, value);
    private T Own<T>(T obj) where T : UnityEngine.Object { owned.Add(obj); return obj; }
    private static RTLTextMeshPro MakeText(Transform parent)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<RTLTextMeshPro>();
    }

    public static void PrepareAndValidate()
    {
        V3UiLocalizationCatalogBuilder.ApplyConfiguredUiStrings();
        RunFocusedValidation();
    }

    public static void RunFocusedValidation()
    {
        int passed = 0;
        var failures = new List<string>();
        foreach (var fixture in new[] { typeof(StartThroughM02LocalizationTests), typeof(GameLocalizationCatalogTests) })
        foreach (var method in fixture.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute), true)))
        {
            object test = Activator.CreateInstance(fixture);
            try
            {
                foreach (var setup in fixture.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute), true))) setup.Invoke(test, null);
                method.Invoke(test, null); passed++;
                Debug.Log("[StartM02Localization] pass=" + method.Name);
            }
            catch (Exception error)
            {
                failures.Add(method.Name);
                Debug.LogError("[StartM02Localization] fail=" + method.Name + " " + (error.InnerException ?? error));
            }
            finally
            {
                foreach (var teardown in fixture.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute), true))) teardown.Invoke(test, null);
            }
        }
        V3UiLocalizationCatalogBuilder.ValidateBindingsAndCoverage();
        new M03PresentationTests().AllTwelveAriaLessonsFitBothLanguagesAndTextSizes();
        Debug.Log("[StartM02Localization] M3AriaLayout=Passed combinations=48");
        if (failures.Count != 0) throw new InvalidOperationException("Localization regressions: " + string.Join(", ", failures));
        Debug.Log("[StartM02Localization] result=Passed tests=" + passed);
    }
}
