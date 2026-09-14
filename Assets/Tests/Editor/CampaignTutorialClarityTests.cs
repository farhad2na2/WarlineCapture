using System;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public sealed class CampaignTutorialClarityTests
{
    internal static readonly string[] Missions = {
        "saga.ch01.m01.first_contact", "saga.ch01.m02.establish_base", "saga.ch01.m03.radar_warning",
        "saga.ch01.m04.airlift", "saga.ch01.m05.breach_assault" };

    public static void Run()
    {
        try
        {
            var tests = new CampaignTutorialClarityTests();
            tests.EveryCampaignEntryAndRetryRequiresFullGuidance();
            tests.OldEarlyMissionSessionsStillProjectInstructions();
            tests.BreachLegacyReplayStartsAndAdvancesFromRealSelection();
            tests.CommandCameraStartsFortyUnitsAboveGround();
            tests.AttentionPulseWaitsResetsAndNeverChangesHitAreas();
            tests.AttentionPulseRespectsReducedMotion();
            tests.EdgeButtonCaptionsStayInsideScreen();
            Debug.Log("[CampaignTutorialClarity] result=Passed entries=45 legacySessions=18 camera=40 attention=verified");
            MatchHudAssistantUiSystemHelperTests.RunShowMeValidation();
            MissionTutorialNextActionValidation.Run();
            M05BreachAssaultRuleTests.RunFocusedValidation();
            if (ValidationExit.LastExitCode != 0) throw new Exception("M5 rule validation failed");
            M05BreachAssaultIntegrationTests.RunFocusedValidation();
            if (ValidationExit.LastExitCode != 0) throw new Exception("M5 integration validation failed");
            MissionReadinessArchitectureValidation.Run();
            if (ValidationExit.LastExitCode != 0) throw new Exception("Architecture validation failed");
            Debug.Log("[CampaignTutorialClarityFinal] result=Passed");
            ValidationExit.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); Debug.LogError("[CampaignTutorialClarity] result=Failed"); UnityEditor.EditorApplication.Exit(1); }
    }

    [Test]
    public void EveryCampaignEntryAndRetryRequiresFullGuidance()
    {
        var create = typeof(CampaignMissionLaunchSystem).GetMethod("CreateRuntime", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (string mission in Missions)
        foreach (MissionRunKind kind in new[] { MissionRunKind.FirstClear, MissionRunKind.Replay, MissionRunKind.Retry })
        foreach (NarrativeGuidanceMode mode in Enum.GetValues(typeof(NarrativeGuidanceMode)))
        {
            var launch = MissionLaunchPayloadFactory.Create(mission, "scenario", "map", MissionLaunchOriginKind.CampaignOperations,
                kind, mode, false, 1, "clarity", 1, 42);
            Assert.AreEqual(NarrativeGuidanceMode.Full, launch.Guidance, mission);
            Assert.IsTrue(launch.ReplayTutorialEnabled);
            var retry = MissionLaunchPayloadFactory.CreateRetry(launch, 2);
            Assert.AreEqual(NarrativeGuidanceMode.Full, retry.Guidance);
            Assert.IsTrue(retry.ReplayTutorialEnabled);
            var request = new CampaignMissionLaunchRequestElement { MissionId = new FixedString64Bytes(mission), Guidance = mode, RunKind = kind };
            var runtime = (CampaignMissionRuntimeComponent)create.Invoke(null, new object[] { request, 1u, new OperationMapReadinessComponent() });
            Assert.AreEqual(NarrativeGuidanceMode.Full, runtime.Guidance, "Direct/retry requests must use the same policy.");
            Assert.AreEqual(1, runtime.ReplayTutorialEnabled);
        }
        Assert.IsFalse(MissionLaunchPayloadFactory.RequiresFullTutorial("saga.ch02.future"));
    }

    [Test]
    public void OldEarlyMissionSessionsStillProjectInstructions()
    {
        foreach (string mission in new[] { Missions[0], Missions[1] })
        foreach (MissionRunKind kind in new[] { MissionRunKind.FirstClear, MissionRunKind.Replay, MissionRunKind.Retry })
        foreach (NarrativeGuidanceMode mode in Enum.GetValues(typeof(NarrativeGuidanceMode)))
        {
            using var world = new World("Old tutorial payload"); var em = world.EntityManager;
            var root = em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent),
                typeof(CampaignMissionAttemptFactsComponent), typeof(CampaignMissionGuidanceProjectionComponent));
            em.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            em.SetComponentData(root, new CampaignMissionRuntimeComponent { MissionId = new FixedString64Bytes(mission),
                SessionToken = "clarity", Phase = MissionPhaseKind.FindSquad, Version = 1, SourceVersion = 1,
                Guidance = mode, RunKind = kind, ReplayTutorialEnabled = 0 });
            em.CreateEntity(typeof(AssistantSettingsComponent)); // Off must not hide mission instructions.
            world.CreateSystem<CampaignMissionGuidanceProjectionSystem>().Update(world.Unmanaged);
            var guidance = em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            Assert.AreEqual(1, guidance.Active, mission + "/" + mode + "/" + kind);
            Assert.AreEqual(NarrativeGuidanceMode.Full, guidance.GuidanceMode);
            Assert.IsFalse(guidance.Title.IsEmpty); Assert.IsFalse(guidance.Body.IsEmpty);
        }
    }

    [Test]
    public void BreachLegacyReplayStartsAndAdvancesFromRealSelection()
    {
        using var world = new World("M5 saved Minimal replay"); var em = world.EntityManager;
        var root = em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent), typeof(CampaignMissionGuidanceProjectionComponent));
        var actor = em.CreateEntity(typeof(UnitHealth), typeof(Unity.Transforms.LocalTransform));
        em.SetComponentData(actor, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(actor, Unity.Transforms.LocalTransform.FromPosition(new float3(100, 0, 0)));
        em.SetComponentData(root, new CampaignMissionRuntimeComponent { MissionId = new FixedString64Bytes(Missions[4]),
            SessionToken = "legacy-m5", Phase = MissionPhaseKind.Engage, Version = 1, SourceVersion = 1,
            Guidance = NarrativeGuidanceMode.Minimal, RunKind = MissionRunKind.Replay, ReplayTutorialEnabled = 0 });
        em.AddComponentData(root, new CampaignMissionBreachState { Ready = 1, SourceVersion = 1,
            SessionToken = "legacy-m5", Support = actor });
        em.AddBuffer<CampaignMissionBreachMember>(root).Add(new CampaignMissionBreachMember { Entity = actor, Kind = 1 });
        em.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
        em.CreateEntity(typeof(AssistantSettingsComponent));
        var system = world.CreateSystem<CampaignMissionGuidanceProjectionSystem>(); system.Update(world.Unmanaged);
        var guidance = em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
        Assert.AreEqual(65001, guidance.GuidanceId); Assert.AreEqual(NarrativeGuidanceMode.Full, guidance.GuidanceMode);
        em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Add(
            new CampaignMissionGuidanceAcknowledgementRequestElement { SessionToken = "legacy-m5", GuidanceId = 65001 });
        system.Update(world.Unmanaged);
        Assert.AreEqual(65002, em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).GuidanceId);
        em.AddComponent<SelectedUnitTag>(actor); system.Update(world.Unmanaged);
        Assert.AreEqual(65003, em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).GuidanceId,
            "The selected unit completes selection; the next instruction is breaching the gate.");
    }

    [Test]
    public void CommandCameraStartsFortyUnitsAboveGround()
    {
        foreach (float ground in new[] { 0f, 12f })
        {
            var request = CampaignMissionSpawnSystem.CreateDefenseCommandView(new float3(80, ground, 40));
            Assert.AreEqual(40f, request.Perspective.x - ground);
            Assert.AreEqual(1, request.UseExplicitPerspective);
        }
    }

    [Test]
    public void AttentionPulseWaitsResetsAndNeverChangesHitAreas()
    {
        var go = new GameObject("Show Me", typeof(RectTransform), typeof(Image), typeof(Button));
        try
        {
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(150, 64);
            TutorialAttentionPulseView.Present(rect, 10, true, 1, 4);
            var border = rect.Find("AttentionBorder").GetComponent<V3GradientGraphic>();
            Assert.IsFalse(border.gameObject.activeSelf);
            TutorialAttentionPulseView.Present(rect, 15.2f, true, 1, 4);
            Assert.IsTrue(border.gameObject.activeSelf);
            Assert.IsFalse(border.raycastTarget);
            Assert.AreEqual(new Vector2(150, 64), rect.sizeDelta); Assert.AreEqual(Vector3.one, rect.localScale);
            TutorialAttentionPulseView.Present(rect, 16, true, 2, 4);
            Assert.IsFalse(border.gameObject.activeSelf, "A new action resets the idle delay.");
            TutorialAttentionPulseView.Present(rect, 20.5f, false, 2, 4);
            Assert.IsFalse(border.gameObject.activeSelf, "Hidden, disabled or waiting steps cannot pulse.");
            go.SetActive(false); go.SetActive(true);
            TutorialAttentionPulseView.Present(rect, 30, true, 2, 4);
            Assert.IsFalse(border.gameObject.activeSelf, "Reappearing Show Me starts a fresh delay.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void EdgeButtonCaptionsStayInsideScreen()
    {
        var go = new GameObject("Cue caption", typeof(RectTransform));
        try
        {
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(480, 96);
            var bounds = new Rect(-960, -540, 1920, 1080);
            foreach (float center in new[] { -900f, 900f, 0f })
            {
                AssistantHighlightPresentationSystemHelper.ClampCaptionToCanvas(rect, new Vector2(center, 0), bounds);
                Assert.That(center + rect.anchoredPosition.x - rect.rect.width / 2, Is.GreaterThanOrEqualTo(bounds.xMin));
                Assert.That(center + rect.anchoredPosition.x + rect.rect.width / 2, Is.LessThanOrEqualTo(bounds.xMax));
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void AttentionPulseRespectsReducedMotion()
    {
        Assert.IsFalse(float.IsNaN(TutorialAttentionPulseView.Opacity(float.MaxValue, 0, false)));
        Assert.AreEqual(0, TutorialAttentionPulseView.Opacity(3.9f, 4, false));
        Assert.That(TutorialAttentionPulseView.Opacity(5.2f, 4, false), Is.EqualTo(1).Within(.001));
        Assert.AreEqual(TutorialAttentionPulseView.Opacity(5, 4, true), TutorialAttentionPulseView.Opacity(6, 4, true));
    }
}
