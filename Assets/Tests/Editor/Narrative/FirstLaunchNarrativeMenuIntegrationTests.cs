using System;
using System.IO;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Runtime;
using NUnit.Framework;
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
            tests.FreshProfile_LanguageChoicePrecedesNarrativeAndPersistsPersian();
            tests.FreshProfile_SkipRequiresLiveConfirmationAndPublishesOneHandoff();
            tests.CompletedAndPendingProfiles_SelectCorrectStartupDisposition();
            tests.ReviewerMode_ProvidesNavigationWithoutMutatingCompletedProfile();
            tests.CommittedIdentity_SkipRoutesDirectlyAndPreservesSelection();
            Debug.Log("[FirstLaunchNarrativeMenuIntegrationValidation] result=Passed tests=6");
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
        Assert.AreEqual(17, bootstrap.FirstLaunchPersianLocale.Voices.Count);
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
        Assert.NotNull(persian);
        persian.onClick.Invoke();

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

        int handoffs = 0;
        context.Helper.MenuHandoffRequested += () => handoffs++;
        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        Button cancel = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "CancelButton");
        Button confirm = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "ConfirmButton");
        skip.onClick.Invoke();
        Assert.IsTrue(context.Helper.IsSkipConfirmationPending);
        cancel.onClick.Invoke();
        Assert.IsFalse(context.Helper.IsSkipConfirmationPending);
        Assert.AreEqual(0, handoffs);

        skip.onClick.Invoke();
        confirm.onClick.Invoke();
        context.Helper.Tick(0f);
        context.Helper.Tick(0f);
        Assert.AreEqual(1, handoffs);
        PlayerProfileSaveData saved = context.SaveService.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.Completed, saved.firstLaunchStatus);
        Assert.IsTrue(saved.firstLaunchSkipped);
        Assert.AreEqual("COMMANDER", saved.firstLaunchCommanderCallsign);
        Assert.AreEqual("Full", saved.firstLaunchGuidance);
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
            int count = 0;
            pending.Helper.MenuHandoffRequested += () => count++;
            Assert.AreEqual(FirstLaunchNarrativeStartupDisposition.ResumeHandoff, pending.Helper.Initialize(
                pending.Sequence, pending.Speakers, pending.Punctuation, pending.View,
                Game.UI.Contracts.FallbackGameTextResolver.Instance, pending.SaveService, false));
            pending.Helper.Tick(0f);
            pending.Helper.Tick(0f);
            Assert.AreEqual(1, count);
            Assert.AreEqual(
                FirstLaunchProfileState.Completed,
                pending.SaveService.LoadProfile().firstLaunchStatus);
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
        int handoffs = 0;
        context.Helper.MenuHandoffRequested += () => handoffs++;
        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        skip.onClick.Invoke();
        Assert.IsFalse(context.Helper.IsSkipConfirmationPending);
        context.Helper.Tick(0f);
        Assert.AreEqual(1, handoffs);

        PlayerProfileSaveData saved = context.SaveService.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.Completed, saved.firstLaunchStatus);
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
