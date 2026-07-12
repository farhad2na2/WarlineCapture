using System;
using System.IO;
using Game.Composition;
using Game.Configs;
using Game.Editor;
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
            tests.MenuScene_HasHiddenTopLevelNarrativeLayerAndExactConfigs();
            tests.FreshProfile_SkipRequiresLiveConfirmationAndPublishesOneHandoff();
            tests.CompletedAndPendingProfiles_SelectCorrectStartupDisposition();
            tests.ReviewerMode_ProvidesNavigationWithoutMutatingCompletedProfile();
            tests.CommittedIdentity_SkipRoutesDirectlyAndPreservesSelection();
            Debug.Log("[FirstLaunchNarrativeMenuIntegrationValidation] result=Passed tests=5");
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
    public void MenuScene_HasHiddenTopLevelNarrativeLayerAndExactConfigs()
    {
        Scene scene = EditorSceneManager.OpenScene(FirstLaunchNarrativeMenuSceneInstaller.MenuScenePath, OpenSceneMode.Single);
        MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
        Assert.NotNull(bootstrap);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeConfig);
        Assert.NotNull(bootstrap.FirstLaunchSpeakerCatalog);
        Assert.NotNull(bootstrap.FirstLaunchPunctuationProfile);
        Assert.AreEqual(26, bootstrap.FirstLaunchNarrativeConfig.States.Count);
        Assert.AreEqual(bootstrap.UiCanvas.transform, bootstrap.FirstLaunchNarrativeView.transform.parent);
        Assert.AreEqual(bootstrap.UiCanvas.transform.childCount - 1, bootstrap.FirstLaunchNarrativeView.transform.GetSiblingIndex());
        Assert.AreEqual("NarrativeLayer", bootstrap.FirstLaunchNarrativeView.name);
        CanvasGroup group = bootstrap.FirstLaunchNarrativeView.GetComponent<CanvasGroup>();
        Assert.NotNull(group);
        Assert.AreEqual(0f, group.alpha);
        Assert.IsFalse(group.interactable);
        Assert.IsFalse(group.blocksRaycasts);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView.CommanderIdentityView);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView.GuidanceChoiceView);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView.SkipConfirmationView);
        Assert.NotNull(bootstrap.FirstLaunchNarrativeView.ReviewerControlsView);
        Assert.IsTrue(scene.IsValid());
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
        context.Helper.MatchHandoffRequested += () => handoffs++;
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
        Assert.AreEqual(FirstLaunchProfileState.HandoffPending, saved.firstLaunchStatus);
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
            pending.Helper.MatchHandoffRequested += () => count++;
            Assert.AreEqual(FirstLaunchNarrativeStartupDisposition.ResumeHandoff, pending.Helper.Initialize(
                pending.Sequence, pending.Speakers, pending.Punctuation, pending.View,
                Game.UI.Contracts.FallbackGameTextResolver.Instance, pending.SaveService, false));
            pending.Helper.Tick(0f);
            pending.Helper.Tick(0f);
            Assert.AreEqual(1, count);
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
        context.Helper.MatchHandoffRequested += () => handoffs++;
        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        skip.onClick.Invoke();
        Assert.IsFalse(context.Helper.IsSkipConfirmationPending);
        context.Helper.Tick(0f);
        Assert.AreEqual(1, handoffs);

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
        return new Context(
            root,
            saveService,
            instance,
            instance.GetComponent<NarrativeSequenceView>(),
            AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath),
            AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath),
            AssetDatabase.LoadAssetAtPath<NarrativePunctuationConfig>(FirstLaunchNarrativeConfigBuilder.PunctuationPath));
    }

    private sealed class Context : IDisposable
    {
        public readonly string Root;
        public readonly SaveService SaveService;
        public readonly GameObject Instance;
        public readonly NarrativeSequenceView View;
        public readonly NarrativeSequenceConfig Sequence;
        public readonly NarrativeSpeakerCatalog Speakers;
        public readonly NarrativePunctuationConfig Punctuation;
        public readonly FirstLaunchNarrativeCompositionSystemHelper Helper = new();
        public Context(string root, SaveService saveService, GameObject instance, NarrativeSequenceView view, NarrativeSequenceConfig sequence, NarrativeSpeakerCatalog speakers, NarrativePunctuationConfig punctuation) { Root = root; SaveService = saveService; Instance = instance; View = view; Sequence = sequence; Speakers = speakers; Punctuation = punctuation; }
        public void Dispose() { Helper.Shutdown(); UnityEngine.Object.DestroyImmediate(Instance); if (Directory.Exists(Root)) Directory.Delete(Root, true); }
    }
}
