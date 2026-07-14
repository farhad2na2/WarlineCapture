using System;
using System.IO;
using Game.Composition;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class FirstLaunchNarrativeCompositionBoundaryTests
{
    private const string CommanderStateId = "first_launch.commander_identity";
    private const string GuidanceStateId = "first_launch.guidance_choice";

    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativeCompositionBoundaryTests tests = new();
            tests.ProfileBoundary_ProjectsStartupDispositionWithoutUiOrEcs();
            tests.ProfileBoundary_PersistsProductionChoicesAndHandoffState();
            tests.ProfileBoundary_ReviewerChoicesDoNotMutateSavedProfile();
            tests.ShellBoundary_ReleasesStartupToMenuWithoutRouteRequest();
            Debug.Log("[FirstLaunchNarrativeCompositionBoundaryValidation] result=Passed tests=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[FirstLaunchNarrativeCompositionBoundaryValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void ProfileBoundary_ProjectsStartupDispositionWithoutUiOrEcs()
    {
        using ProfileContext context = CreateProfileContext(new PlayerProfileSaveData());
        Assert.IsFalse(context.Boundary.ShouldEnterMenu(false, false));
        Assert.IsFalse(context.Boundary.ShouldResumeHandoff(false));
        Assert.IsTrue(context.Boundary.ShouldEnterMenu(true, false));

        context.SaveService.SaveProfile(new PlayerProfileSaveData
        {
            firstLaunchStatus = FirstLaunchProfileState.HandoffPending
        });
        context.Boundary.Initialize(context.SaveService, CommanderStateId, GuidanceStateId);
        Assert.IsTrue(context.Boundary.ShouldResumeHandoff(false));
        Assert.IsFalse(context.Boundary.ShouldResumeHandoff(true));
    }

    [Test]
    public void ProfileBoundary_PersistsProductionChoicesAndHandoffState()
    {
        using ProfileContext context = CreateProfileContext(new PlayerProfileSaveData());
        context.Boundary.MarkInProgress(false);
        context.Boundary.CommitCommanderIdentity(new NarrativeCommanderIdentityData
        {
            Callsign = "NIGHTFALL",
            DisplayName = "Farhad"
        }, 3, true);
        context.Boundary.CommitGuidance(NarrativeGuidanceMode.Contextual, true);
        Assert.IsTrue(context.Boundary.HasCommittedCommanderIdentity());

        context.Boundary.MarkWatchedHandoff(new NarrativeHandoffResult
        {
            Completion = new NarrativeCompletionPayload
            {
                LastCompletedStateId = "first_launch.m01_handoff"
            }
        });

        PlayerProfileSaveData saved = context.SaveService.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.HandoffPending, saved.firstLaunchStatus);
        Assert.AreEqual("NIGHTFALL", saved.firstLaunchCommanderCallsign);
        Assert.AreEqual("Farhad", saved.firstLaunchCommanderDisplayName);
        Assert.AreEqual(3, saved.firstLaunchCommanderPortraitIndex);
        Assert.AreEqual("Contextual", saved.firstLaunchGuidance);
        Assert.IsTrue(saved.firstLaunchWatched);
        Assert.IsFalse(saved.firstLaunchSkipped);

        context.Boundary.MarkHandoffComplete();
        Assert.AreEqual(FirstLaunchProfileState.Completed, context.SaveService.LoadProfile().firstLaunchStatus);
    }

    [Test]
    public void ProfileBoundary_ReviewerChoicesDoNotMutateSavedProfile()
    {
        PlayerProfileSaveData original = new()
        {
            firstLaunchStatus = FirstLaunchProfileState.Completed,
            firstLaunchCommanderCallsign = "SAVED",
            firstLaunchGuidance = NarrativeGuidanceMode.Full.ToString()
        };
        using ProfileContext context = CreateProfileContext(original);
        context.Boundary.CommitCommanderIdentity(new NarrativeCommanderIdentityData
        {
            Callsign = "PREVIEW",
            DisplayName = "Reviewer"
        }, 4, false);
        context.Boundary.CommitGuidance(NarrativeGuidanceMode.Minimal, false);

        PlayerProfileSaveData saved = context.SaveService.LoadProfile();
        Assert.AreEqual("SAVED", saved.firstLaunchCommanderCallsign);
        Assert.AreEqual(NarrativeGuidanceMode.Full.ToString(), saved.firstLaunchGuidance);
        Assert.AreEqual(FirstLaunchProfileState.Completed, saved.firstLaunchStatus);
    }

    [Test]
    public void ShellBoundary_ReleasesStartupToMenuWithoutRouteRequest()
    {
        using World world = new(nameof(ShellBoundary_ReleasesStartupToMenuWithoutRouteRequest));
        EntityManager entityManager = world.EntityManager;
        Entity boundary = entityManager.CreateEntity(typeof(UiShellStartupDispositionComponent));
        entityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        FirstLaunchNarrativeShellCompositionSystemHelper shell = new();

        shell.SetStartupDisposition(FirstLaunchNarrativeStartupDisposition.Playing);
        shell.RequestHandoff();
        Assert.IsTrue(shell.TryPublishHandoff());
        Assert.IsFalse(shell.TryPublishHandoff());
        shell.Apply(entityManager, boundary);
        shell.Apply(entityManager, boundary);

        Assert.AreEqual(
            UiShellStartupDisposition.EnterMenu,
            entityManager.GetComponentData<UiShellStartupDispositionComponent>(boundary).Value);
        DynamicBuffer<UiShellRouteRequestComponent> requests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(UiShellStartupDisposition.EnterMenu, shell.StartupDisposition);
        Assert.IsFalse(shell.IsHandoffPending);
        Assert.IsTrue(shell.IsHandoffPublished);

        FirstLaunchNarrativeShellCompositionSystemHelper.ResetBoundary(entityManager, boundary);
        Assert.AreEqual(
            UiShellStartupDisposition.Pending,
            entityManager.GetComponentData<UiShellStartupDispositionComponent>(boundary).Value);
    }

    private static ProfileContext CreateProfileContext(PlayerProfileSaveData profile)
    {
        string root = Path.Combine(Path.GetTempPath(), "FirstLaunchProfileBoundary", Guid.NewGuid().ToString("N"));
        SaveService saveService = new(new JsonSaveRepository(root));
        saveService.SaveProfile(profile);
        FirstLaunchNarrativeProfileCompositionSystemHelper boundary = new();
        boundary.Initialize(saveService, CommanderStateId, GuidanceStateId);
        return new ProfileContext(root, saveService, boundary);
    }

    private readonly struct ProfileContext : IDisposable
    {
        public readonly string Root;
        public readonly SaveService SaveService;
        public readonly FirstLaunchNarrativeProfileCompositionSystemHelper Boundary;

        public ProfileContext(
            string root,
            SaveService saveService,
            FirstLaunchNarrativeProfileCompositionSystemHelper boundary)
        {
            Root = root;
            SaveService = saveService;
            Boundary = boundary;
        }

        public void Dispose()
        {
            Boundary.Reset();
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}
