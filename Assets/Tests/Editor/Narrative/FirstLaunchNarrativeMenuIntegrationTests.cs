using System;
using System.IO;
using System.Reflection;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FirstLaunchNarrativeMenuIntegrationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativeMenuIntegrationTests tests = new();
            tests.MenuScene_HasTopLevelNarrativeLayerAndExactConfigs();
            tests.MenuScene_FirstLaunchReferenceLayoutFillsEditorCanvas();
            tests.LanguageChoice_AwakeDoesNotOverrideCompositionVisibility();
            tests.LanguageChoice_AllControlsHaveRaycastTargets();
            tests.LanguageChoice_SelectionImmediatelyLocalizesOnlyShellCopy();
            tests.FreshProfile_LanguageChoicePrecedesNarrativeAndPersistsPersian();
            tests.SkipConfirmation_UsesV3ChromeAndPersianLocalization();
            tests.FreshProfile_SkipRequiresLiveConfirmationAndPublishesOneHandoff();
            tests.CompletedAndPendingProfiles_SelectCorrectStartupDisposition();
            tests.ReviewerMode_ProvidesNavigationWithoutMutatingCompletedProfile();
            tests.CommittedIdentity_SkipRoutesDirectlyAndPreservesSelection();
            Debug.Log("[FirstLaunchNarrativeMenuIntegrationValidation] result=Passed tests=11 pointerTargets=Passed languagePreview=Passed skip=v3-bilingual");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchNarrativeMenuIntegrationValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void LanguageChoice_AllControlsHaveRaycastTargets()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath);
        Assert.NotNull(prefab);

        Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
        Assert.AreEqual(3, buttons.Length);
        for (int i = 0; i < buttons.Length; i++)
        {
            Assert.NotNull(buttons[i].targetGraphic, $"{buttons[i].name} needs a visible pointer target.");
            Assert.IsTrue(buttons[i].targetGraphic.raycastTarget,
                $"{buttons[i].name} must accept real pointer raycasts, not only direct onClick invocation.");
        }
    }

    [Test]
    public void LanguageChoice_AwakeDoesNotOverrideCompositionVisibility()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            FirstLaunchLanguageChoiceView languageView = instance.GetComponent<FirstLaunchLanguageChoiceView>();
            Assert.NotNull(languageView);
            Assert.IsFalse(languageView.IsVisible);

            languageView.SetVisible(true);
            MethodInfo awake = typeof(FirstLaunchLanguageChoiceView).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awake);
            awake.Invoke(languageView, null);

            Assert.IsTrue(languageView.IsVisible,
                "Awake must not hide a selector already shown by MenuBootstrap initialization.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void LanguageChoice_SelectionImmediatelyLocalizesOnlyShellCopy()
    {
        GameLocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(
            V3UiLocalizationCatalogBuilder.CatalogPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            Assert.NotNull(catalog);
            Assert.NotNull(instance);
            GameLocalization.Initialize(catalog, GameLocalization.EnglishLocaleCode, persist: false);

            FirstLaunchLanguageChoiceView view = instance.GetComponent<FirstLaunchLanguageChoiceView>();
            view.Bind(_ => { });
            view.SetVisible(true);
            Button english = instance.transform.Find("Composition/EnglishButton")?.GetComponent<Button>();
            Button persian = instance.transform.Find("Composition/PersianButton")?.GetComponent<Button>();
            Assert.NotNull(english);
            Assert.NotNull(persian);

            persian.onClick.Invoke();
            Assert.AreEqual(GameLocalization.PersianLocaleCode, GameLocalization.CurrentLocaleCode);
            AssertOriginalRtlText(
                instance.transform.Find("Composition/Title")?.GetComponent<TMP_Text>(),
                "زبان داستان را انتخاب کنید");
            AssertOriginalRtlText(
                instance.transform.Find("Composition/InfoPanel/InfoText")?.GetComponent<TMP_Text>(),
                "بعداً می‌توانید این مورد را\nدر تنظیمات فرماندهی تغییر دهید.");
            AssertOriginalRtlText(
                instance.transform.Find("Composition/ContinueButton/Label")?.GetComponent<TMP_Text>(),
                "ادامه   ‹");

            // Language cards are language samples, not shell copy: each remains native.
            AssertOriginalRtlText(
                instance.transform.Find("Composition/EnglishButton/Language")?.GetComponent<TMP_Text>(),
                "ENGLISH");
            AssertOriginalRtlText(
                instance.transform.Find("Composition/PersianButton/Language")?.GetComponent<TMP_Text>(),
                "فارسی");

            english.onClick.Invoke();
            Assert.AreEqual(GameLocalization.EnglishLocaleCode, GameLocalization.CurrentLocaleCode);
            AssertOriginalRtlText(
                instance.transform.Find("Composition/Title")?.GetComponent<TMP_Text>(),
                "SELECT STORY LANGUAGE");
            AssertOriginalRtlText(
                instance.transform.Find("Composition/ContinueButton/Label")?.GetComponent<TMP_Text>(),
                "CONTINUE   ›");
        }
        finally
        {
            GameLocalization.SetLocale(GameLocalization.EnglishLocaleCode, persist: false);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MenuScene_HasTopLevelNarrativeLayerAndExactConfigs()
    {
        Scene scene = EditorSceneManager.OpenScene(FirstLaunchNarrativeMenuSceneInstaller.MenuScenePath, OpenSceneMode.Single);
        MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
        Assert.NotNull(bootstrap);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeConfig);
        Assert.NotNull(bootstrap.FirstLaunchSpeakerCatalog);
        Assert.NotNull(bootstrap.FirstLaunchPunctuationProfile);
        Assert.NotNull(bootstrap.FirstLaunchLanguageChoiceView);
        Assert.NotNull(bootstrap.FirstLaunchPersianLocale);
        Assert.IsTrue(bootstrap.FirstLaunchPersianLocale.RightToLeft);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Persian, bootstrap.FirstLaunchPersianLocale.Language);
        Assert.GreaterOrEqual(bootstrap.FirstLaunchPersianLocale.Voices.Count, 17,
            "The shared Persian locale may include later mission voices, but it must retain all 17 First Launch lines.");
        for (int i = 0; i < bootstrap.FirstLaunchPersianLocale.Voices.Count; i++)
        {
            NarrativeLocaleVoiceRecord voice = bootstrap.FirstLaunchPersianLocale.Voices[i];
            Assert.NotNull(voice.VoiceClip, voice.LineId);
            if (voice.LineId == "p14_commander")
            {
                Assert.NotNull(voice.FemaleVoiceClip);
                Assert.NotNull(voice.NeutralVoiceClip);
            }
        }
        Assert.AreEqual(26, bootstrap.FirstLaunchNarrativeConfig.States.Count);
        Assert.AreEqual(bootstrap.UiCanvas.transform, bootstrap.FirstLaunchNarrativeView.transform.parent);
        Assert.AreEqual(bootstrap.UiCanvas.transform.childCount - 2, bootstrap.FirstLaunchNarrativeView.transform.GetSiblingIndex());
        Assert.AreEqual(bootstrap.UiCanvas.transform.childCount - 1, bootstrap.FirstLaunchLanguageChoiceView.transform.GetSiblingIndex());
        Assert.AreEqual("NarrativeLayer", bootstrap.FirstLaunchNarrativeView.name);
        CanvasGroup group = bootstrap.FirstLaunchNarrativeView.GetComponent<CanvasGroup>();
        Assert.NotNull(group);
        Assert.AreEqual(1f, group.alpha);
        Assert.IsFalse(group.interactable);
        Assert.IsFalse(group.blocksRaycasts);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView.CommanderIdentityView);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView.GuidanceChoiceView);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView.SkipConfirmationView);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView.ReviewerControlsView);
        Assert.IsTrue(scene.IsValid());
    }

    [Test]
    public void MenuScene_FirstLaunchReferenceLayoutFillsEditorCanvas()
    {
        Assert.IsTrue(
            Attribute.IsDefined(typeof(MainMenuV3SectionLayoutView), typeof(ExecuteAlways), false),
            "V3 reference layouts must execute in Edit Mode so the Editor preview matches Play Mode.");

        Scene scene = EditorSceneManager.OpenScene(FirstLaunchNarrativeMenuSceneInstaller.MenuScenePath, OpenSceneMode.Single);
        MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
        Assert.NotNull(bootstrap);
        RectTransform canvasRect = bootstrap.UiCanvas.transform as RectTransform;
        MainMenuV3SectionLayoutView layout = bootstrap.FirstLaunchNarrativeView.transform
            .Find("SafeArea")?.GetComponent<MainMenuV3SectionLayoutView>();
        Assert.NotNull(canvasRect);
        Assert.NotNull(layout);

        layout.RefreshLayout();
        float expectedScale = Mathf.Min(
            canvasRect.rect.width / layout.ReferenceResolution.x,
            canvasRect.rect.height / layout.ReferenceResolution.y);
        Assert.AreEqual(expectedScale, layout.LastAppliedScale, 0.001f);
        Assert.AreEqual(expectedScale, layout.transform.localScale.x, 0.001f);
        Assert.Greater(expectedScale, 1f,
            "The 4800-wide Menu authoring canvas must not leave First Launch at raw 1672-pixel scale.");
        Assert.IsFalse(scene.isDirty,
            "Responsive Editor preview transforms must be driven and must not create scene overrides.");
    }

    [Test]
    public void FreshProfile_LanguageChoicePrecedesNarrativeAndPersistsPersian()
    {
        using Context context = CreateContext(new PlayerProfileSaveData());
        FirstLaunchNarrativeStartupDisposition disposition = context.Helper.Initialize(
            context.Sequence,
            context.Speakers,
            context.Punctuation,
            context.View,
            Game.UI.Contracts.FallbackGameTextResolver.Instance,
            context.SaveService,
            false,
            false,
            context.LanguageView,
            context.PersianLocale);

        Assert.AreEqual(FirstLaunchNarrativeStartupDisposition.AwaitingLanguage, disposition);
        Assert.IsFalse(context.Helper.IsPlaying);
        Assert.AreEqual(FirstLaunchProfileState.NotStarted, context.SaveService.LoadProfile().firstLaunchStatus);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Unselected.ToString(), context.SaveService.LoadProfile().firstLaunchLanguage);
        Assert.AreEqual(1f, context.LanguageView.GetComponent<CanvasGroup>().alpha);
        Assert.AreEqual(0f, context.View.GetComponent<CanvasGroup>().alpha);

        Button persian = Array.Find(
            context.LanguageInstance.GetComponentsInChildren<Button>(true),
            button => button.name == "PersianButton");
        Button continueButton = Array.Find(
            context.LanguageInstance.GetComponentsInChildren<Button>(true),
            button => button.name == "ContinueButton");
        Assert.NotNull(persian);
        Assert.NotNull(continueButton);
        persian.onClick.Invoke();

        PlayerProfileSaveData beforeConfirmation = context.SaveService.LoadProfile();
        Assert.IsFalse(context.Helper.IsPlaying);
        Assert.AreEqual(FirstLaunchProfileState.NotStarted, beforeConfirmation.firstLaunchStatus);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Unselected.ToString(), beforeConfirmation.firstLaunchLanguage);

        continueButton.onClick.Invoke();

        PlayerProfileSaveData saved = context.SaveService.LoadProfile();
        Assert.IsTrue(context.Helper.IsPlaying);
        Assert.AreEqual(FirstLaunchProfileState.InProgress, saved.firstLaunchStatus);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Persian.ToString(), saved.firstLaunchLanguage);
        Assert.AreEqual("فرمانده", saved.firstLaunchCommanderCallsign);
        Assert.AreEqual(0f, context.LanguageView.GetComponent<CanvasGroup>().alpha);
    }

    [Test]
    public void FreshProfile_SkipRequiresLiveConfirmationAndPublishesOneHandoff()
    {
        using Context context = CreateContext(new PlayerProfileSaveData());
        FirstLaunchNarrativeStartupDisposition disposition = context.Helper.Initialize(
            context.Sequence, context.Speakers, context.Punctuation, context.View,
            Game.UI.Contracts.FallbackGameTextResolver.Instance, context.SaveService, false);
        Assert.AreEqual(FirstLaunchNarrativeStartupDisposition.Playing, disposition);
        Assert.AreEqual(FirstLaunchProfileState.InProgress, context.SaveService.LoadProfile().firstLaunchStatus);

        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        Button cancel = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "CancelButton");
        Button confirm = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "ConfirmButton");
        skip.onClick.Invoke();
        Assert.IsTrue(context.Helper.IsSkipConfirmationPending);
        cancel.onClick.Invoke();
        Assert.IsFalse(context.Helper.IsSkipConfirmationPending);

        skip.onClick.Invoke();
        confirm.onClick.Invoke();
        context.Helper.Tick(0f);
        context.Helper.Tick(0f);
        PlayerProfileSaveData saved = context.SaveService.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.HandoffPending, saved.firstLaunchStatus);
        Assert.IsTrue(saved.firstLaunchSkipped);
        Assert.AreEqual("COMMANDER", saved.firstLaunchCommanderCallsign);
        Assert.AreEqual("Full", saved.firstLaunchGuidance);
        Assert.AreEqual(0f, context.View.GetComponent<CanvasGroup>().alpha,
            "Skipping must leave the cleared narrative layer hidden while the loading handoff owns the screen.");
    }

    [Test]
    public void SkipConfirmation_UsesV3ChromeAndPersianLocalization()
    {
        using Context context = CreateContext(new PlayerProfileSaveData());
        Assert.AreEqual(
            FirstLaunchNarrativeStartupDisposition.AwaitingLanguage,
            context.Helper.Initialize(
                context.Sequence, context.Speakers, context.Punctuation, context.View,
                Game.UI.Contracts.FallbackGameTextResolver.Instance, context.SaveService, false, false,
                context.LanguageView, context.PersianLocale));

        Button persian = Array.Find(context.LanguageInstance.GetComponentsInChildren<Button>(true), button => button.name == "PersianButton");
        Button continueButton = Array.Find(context.LanguageInstance.GetComponentsInChildren<Button>(true), button => button.name == "ContinueButton");
        Assert.NotNull(persian);
        Assert.NotNull(continueButton);
        persian.onClick.Invoke();
        continueButton.onClick.Invoke();

        Transform confirmation = context.Instance.transform.Find("SafeArea/SkipConfirmationSurface/Confirmation");
        Assert.NotNull(confirmation);
        Assert.NotNull(confirmation.GetComponent<V3GradientGraphic>(), "The skip modal must use sharp V3 chrome.");
        Assert.NotNull(confirmation.Find("CancelButton")?.GetComponent<V3GradientGraphic>());
        Assert.NotNull(confirmation.Find("ConfirmButton")?.GetComponent<V3GradientGraphic>());
        Assert.IsNull(confirmation.GetComponent<Image>(), "Legacy sliced-panel artwork must not remain on the V3 modal.");

        TMP_Text title = confirmation.Find("Title")?.GetComponent<TMP_Text>();
        TMP_Text body = confirmation.Find("Body")?.GetComponent<TMP_Text>();
        TMP_Text cancel = confirmation.Find("CancelButton/Label")?.GetComponent<TMP_Text>();
        TMP_Text confirm = confirmation.Find("ConfirmButton/Label")?.GetComponent<TMP_Text>();
        AssertOriginalRtlText(title, "به فرماندهی تاکتیکی برویم؟");
        AssertOriginalRtlText(body, "هویت پیش‌فرض فرمانده و راهنمایی کامل استفاده می‌شود. بعداً می‌توانید هر دو را تغییر دهید.");
        AssertOriginalRtlText(cancel, "ادامهٔ تماشا");
        AssertOriginalRtlText(confirm, "رد کردن مقدمه");
        StringAssert.Contains("NotoSansArabic", title?.font?.name);
        Assert.AreEqual(TextAlignmentOptions.MidlineRight, title?.alignment);
        Assert.AreEqual(TextAlignmentOptions.TopRight, body?.alignment);

        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        Assert.NotNull(skip);
        skip.onClick.Invoke();
        CanvasGroup group = context.View.SkipConfirmationView.GetComponent<CanvasGroup>();
        Assert.AreEqual(1f, group.alpha);
        Assert.IsTrue(group.interactable);
        Assert.IsTrue(group.blocksRaycasts);
    }

    private static void AssertOriginalRtlText(TMP_Text target, string expected)
    {
        Assert.NotNull(target);
        SerializedProperty originalText = new SerializedObject(target).FindProperty("originalText");
        Assert.NotNull(originalText, $"{target.name} must use the shared RTL-capable text component.");
        Assert.AreEqual(expected, originalText.stringValue);
    }

    [Test]
    public void CompletedAndPendingProfiles_SelectCorrectStartupDisposition()
    {
        using (Context completed = CreateContext(new PlayerProfileSaveData { firstLaunchStatus = FirstLaunchProfileState.Completed }))
        {
            Assert.AreEqual(FirstLaunchNarrativeStartupDisposition.EnterMenu, completed.Helper.Initialize(
                completed.Sequence, completed.Speakers, completed.Punctuation, completed.View,
                Game.UI.Contracts.FallbackGameTextResolver.Instance, completed.SaveService, false));
        }
        using (Context pending = CreateContext(new PlayerProfileSaveData { firstLaunchStatus = FirstLaunchProfileState.HandoffPending }))
        {
            Assert.AreEqual(FirstLaunchNarrativeStartupDisposition.ResumeHandoff, pending.Helper.Initialize(
                pending.Sequence, pending.Speakers, pending.Punctuation, pending.View,
                Game.UI.Contracts.FallbackGameTextResolver.Instance, pending.SaveService, false));
            pending.Helper.Tick(0f);
            pending.Helper.Tick(0f);
            Assert.AreEqual(FirstLaunchProfileState.HandoffPending, pending.SaveService.LoadProfile().firstLaunchStatus);
            Assert.AreEqual(0f, pending.View.GetComponent<CanvasGroup>().alpha,
                "A resumed handoff must not expose the cleared narrative panel as a white screen.");
        }
    }

    [Test]
    public void ReviewerMode_ProvidesNavigationWithoutMutatingCompletedProfile()
    {
        PlayerProfileSaveData original = new()
        {
            firstLaunchStatus = FirstLaunchProfileState.Completed,
            firstLaunchCommanderCallsign = "SAVED-COMMANDER"
        };
        using Context context = CreateContext(original);
        Assert.AreEqual(FirstLaunchNarrativeStartupDisposition.Playing, context.Helper.Initialize(
            context.Sequence, context.Speakers, context.Punctuation, context.View,
            Game.UI.Contracts.FallbackGameTextResolver.Instance, context.SaveService, false, true));

        CanvasGroup reviewerGroup = context.View.ReviewerControlsView.GetComponent<CanvasGroup>();
        Assert.AreEqual(1f, reviewerGroup.alpha);
        Button next = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "NextButton");
        Button debrief = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "JumpToDebriefButton");
        Button gameplay = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipToGameButton");
        Assert.NotNull(next);
        Assert.NotNull(debrief);
        Assert.NotNull(gameplay);

        next.onClick.Invoke();
        Assert.AreEqual("FL-P02", context.Helper.CurrentStateId);
        debrief.onClick.Invoke();
        Assert.AreEqual("FL-P19", context.Helper.CurrentStateId);
        gameplay.onClick.Invoke();
        Assert.AreEqual("first_launch.gameplay_placeholder", context.Helper.CurrentStateId);
        debrief.onClick.Invoke();
        Assert.AreEqual("FL-P19", context.Helper.CurrentStateId);
        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        Assert.NotNull(skip);
        skip.onClick.Invoke();
        Assert.AreEqual("first_launch.command_base_reveal", context.Helper.CurrentStateId);
        Assert.IsTrue(context.Helper.LastReviewerCompletion.Skipped);
        CollectionAssert.Contains(context.Helper.LastReviewerCompletion.EvidenceIds, "evidence.aria.revoked_credential_fragment");

        PlayerProfileSaveData saved = context.SaveService.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.Completed, saved.firstLaunchStatus);
        Assert.AreEqual("SAVED-COMMANDER", saved.firstLaunchCommanderCallsign);
    }

    [Test]
    public void CommittedIdentity_SkipRoutesDirectlyAndPreservesSelection()
    {
        PlayerProfileSaveData original = new()
        {
            firstLaunchStatus = FirstLaunchProfileState.InProgress,
            firstLaunchLastCompletedStateId = "first_launch.commander_identity",
            firstLaunchCommanderCallsign = "NIGHTFALL",
            firstLaunchCommanderDisplayName = "Farhad",
            firstLaunchCommanderPortraitIndex = 3,
            firstLaunchGuidance = "Contextual"
        };
        using Context context = CreateContext(original);
        Assert.AreEqual(FirstLaunchNarrativeStartupDisposition.Playing, context.Helper.Initialize(
            context.Sequence, context.Speakers, context.Punctuation, context.View,
            Game.UI.Contracts.FallbackGameTextResolver.Instance, context.SaveService, false));
        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        skip.onClick.Invoke();
        Assert.IsFalse(context.Helper.IsSkipConfirmationPending);
        context.Helper.Tick(0f);

        PlayerProfileSaveData saved = context.SaveService.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.HandoffPending, saved.firstLaunchStatus);
        Assert.AreEqual("NIGHTFALL", saved.firstLaunchCommanderCallsign);
        Assert.AreEqual("Farhad", saved.firstLaunchCommanderDisplayName);
        Assert.AreEqual(3, saved.firstLaunchCommanderPortraitIndex);
        Assert.AreEqual("Contextual", saved.firstLaunchGuidance);
    }

    private static Context CreateContext(PlayerProfileSaveData profile)
    {
        string root = Path.Combine(Path.GetTempPath(), "FirstLaunchMenuIntegration", Guid.NewGuid().ToString("N"));
        SaveService saveService = new(new JsonSaveRepository(root));
        saveService.SaveProfile(profile);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        GameObject languagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath);
        GameObject languageInstance = UnityEngine.Object.Instantiate(languagePrefab);
        return new Context(
            root,
            saveService,
            instance,
            instance.GetComponent<NarrativeSequenceView>(),
            languageInstance,
            languageInstance.GetComponent<FirstLaunchLanguageChoiceView>(),
            AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath),
            AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath),
            AssetDatabase.LoadAssetAtPath<NarrativePunctuationConfig>(FirstLaunchNarrativeConfigBuilder.PunctuationPath),
            AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(FirstLaunchNarrativeConfigBuilder.PersianLocalePath));
    }

    private sealed class Context : IDisposable
    {
        public readonly string Root;
        public readonly SaveService SaveService;
        public readonly GameObject Instance;
        public readonly NarrativeSequenceView View;
        public readonly GameObject LanguageInstance;
        public readonly FirstLaunchLanguageChoiceView LanguageView;
        public readonly NarrativeSequenceConfig Sequence;
        public readonly NarrativeSpeakerCatalog Speakers;
        public readonly NarrativePunctuationConfig Punctuation;
        public readonly NarrativeLocaleConfig PersianLocale;
        public readonly FirstLaunchNarrativeCompositionSystemHelper Helper = new();
        public Context(string root, SaveService saveService, GameObject instance, NarrativeSequenceView view, GameObject languageInstance, FirstLaunchLanguageChoiceView languageView, NarrativeSequenceConfig sequence, NarrativeSpeakerCatalog speakers, NarrativePunctuationConfig punctuation, NarrativeLocaleConfig persianLocale) { Root = root; SaveService = saveService; Instance = instance; View = view; LanguageInstance = languageInstance; LanguageView = languageView; Sequence = sequence; Speakers = speakers; Punctuation = punctuation; PersianLocale = persianLocale; }
        public void Dispose() { Helper.Shutdown(); UnityEngine.Object.DestroyImmediate(Instance); UnityEngine.Object.DestroyImmediate(LanguageInstance); if (Directory.Exists(Root)) Directory.Delete(Root, true); }
    }
}
