using System;
using System.Reflection;
using Game.Composition;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;

internal static class S073LegacyQueueOutcomeControls
{
    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static EntityManager Manager()
    {
        var em = new EntityManager();
        int next = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        return em;
    }

    static QuickGameConfig Config(int scenario = 3)
    {
        var config = QuickGameConfig.Defaults;
        config.ScenarioIndex = scenario;
        config.MapSeed = 104729;
        return config;
    }

    static int Sessions(EntityManager em)
    {
        using var query = em.CreateEntityQuery(typeof(SkirmishMatchState));
        return query.CalculateEntityCount();
    }

    static void Main()
    {
        var em = Manager();
        var boundary = em.CreateEntity();
        em.AddComponentData(boundary, new MatchStartQueueComponent { LastRequestId = 9 });
        var requests = em.AddBuffer<MatchStartRequestElement>(boundary);
        requests.Add(new MatchStartRequestElement { RequestId = 9 });
        var results = em.AddBuffer<MatchStartResultElement>(boundary);
        results.Add(new MatchStartResultElement { RequestId = 9, Status = MatchStartStatusKind.Failed });
        em.AddComponentData(boundary, new MatchStartProgressComponent { Progress01 = 0.5f });
        Need(SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(em, Config()), "valid S073 legacy queue rejected");
        using (var query = em.CreateEntityQuery(typeof(SkirmishMatchState)))
        {
            Need(query.CalculateEntityCount() == 1, "session count changed");
            var entity = query.GetSingletonEntity();
            var state = em.GetComponentData<SkirmishMatchState>(entity);
            Need(state.ScenarioIndex == 3 && state.Seed == 104729 && state.Phase == SkirmishPhase.Queued,
                "original queued identity/phase changed");
            Need(state.SessionId.Length == 32 && em.HasBuffer<SkirmishActionRequest>(entity) &&
                 em.HasBuffer<SkirmishTrackedUnit>(entity) && em.HasComponent<SkirmishLaunchSnapshot>(entity) &&
                 !em.HasComponent<SkirmishExpandedSessionComponent>(entity), "launch components changed");
            var snapshot = em.GetComponentObject<SkirmishLaunchSnapshot>(entity).Configuration;
            Need(snapshot.ScenarioIndex == 3 && snapshot.MapSeed == 104729, "normalized config snapshot changed");
        }
        Need(em.GetComponentData<MatchStartQueueComponent>(boundary).LastRequestId == 9 &&
             requests.Length == 0 && results.Length == 0 &&
             em.GetComponentData<MatchStartProgressComponent>(boundary).Progress01 == 0f,
            "start boundary retry cleanup changed");

        var campaign = Manager();
        var campaignRoot = campaign.CreateEntity();
        campaign.AddBuffer<CampaignMissionLaunchRequestElement>(campaignRoot)
            .Add(new CampaignMissionLaunchRequestElement { SchemaVersion = 1, MissionId = new FixedString64Bytes("pending") });
        Need(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(campaign, Config()) && Sessions(campaign) == 0,
            "Campaign request exclusion failed");

        var existing = Manager();
        var existingMatch = existing.CreateEntity();
        existing.AddComponentData(existingMatch, new SkirmishMatchState { Phase = SkirmishPhase.Queued });
        Need(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(existing, Config()) && Sessions(existing) == 1,
            "existing session exclusion failed");

        var pending = Manager();
        var pendingBoundary = pending.CreateEntity();
        pending.AddComponentData(pendingBoundary, new MatchStartQueueComponent { IsStartPending = 1, LastRequestId = 4 });
        Need(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(pending, Config()) && Sessions(pending) == 0 &&
             pending.GetComponentData<MatchStartQueueComponent>(pendingBoundary).LastRequestId == 4,
            "pending start exclusion failed");

        var wrong = Manager();
        Need(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(wrong, Config(4)) && Sessions(wrong) == 0,
            "non-S073 identity admitted");

        var enforce = typeof(ContentAccessRuntime).GetField("enforceOwnership", BindingFlags.NonPublic | BindingFlags.Static);
        Need(enforce != null, "original content access enforcement field changed");
        bool previous = (bool)enforce.GetValue(null);
        try
        {
            enforce.SetValue(null, true);
            var denied = Manager();
            Need(!SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(denied, Config()) && Sessions(denied) == 0,
                "denied content access admitted S073");
        }
        finally { enforce.SetValue(null, previous); }

        var queued = new SkirmishMatchState { ScenarioIndex = 3, Phase = SkirmishPhase.Queued };
        Need(!SkirmishOutcomeRules.Evaluate(ref queued, false, false, 10f, true, true) &&
             queued.Phase == SkirmishPhase.Queued && queued.ElapsedSeconds == 0f &&
             queued.Outcome == SkirmishOutcome.None, "queued outcome phase gate changed");
        var playing = new SkirmishMatchState { ScenarioIndex = 3, Phase = SkirmishPhase.Playing };
        Need(SkirmishOutcomeRules.Evaluate(ref playing, true, false, 1f, true) &&
             playing.Phase == SkirmishPhase.Finished && playing.Outcome == SkirmishOutcome.Victory &&
             playing.Reason == SkirmishEndReason.MainBaseDestroyed && playing.ElapsedSeconds == 1f,
            "base-destruction outcome policy changed");

        var live = Manager();
        Entity liveSession = live.CreateEntity();
        Entity playerBase = live.CreateEntity();
        Entity enemyBase = live.CreateEntity();
        Entity gameplayEntity = live.CreateEntity();
        live.AddComponentData(playerBase, new UnitHealth { Current = 100, Max = 100 });
        live.AddComponentData(enemyBase, new UnitHealth { Current = 100, Max = 100 });
        live.AddComponentData(liveSession, new SkirmishMatchState
        {
            ScenarioIndex = 3,
            Phase = SkirmishPhase.Queued,
            PlayerMainBase = playerBase,
            EnemyMainBase = enemyBase
        });
        live.AddComponentData(gameplayEntity, new RuntimeGameplayStateComponent
        {
            PlayRequested = 1,
            SimulationActive = 1,
            SelectionModeActive = 1,
            BuildModeActive = 1,
            FullscreenMapOpen = 1
        });

        var liveMatch = live.GetComponentData<SkirmishMatchState>(liveSession);
        var liveGameplay = live.GetComponentData<RuntimeGameplayStateComponent>(gameplayEntity);
        Need(!SkirmishOutcomeRules.Evaluate(live, ref liveMatch, 10f, ref liveGameplay) &&
             liveMatch.Phase == SkirmishPhase.Queued && liveMatch.ElapsedSeconds == 0f &&
             liveMatch.Outcome == SkirmishOutcome.None && liveGameplay.SimulationActive == 1,
            "live queued session crossed original outcome phase gate");

        liveMatch.Phase = SkirmishPhase.Preparing;
        Need(!SkirmishOutcomeRules.Evaluate(live, ref liveMatch, 10f, ref liveGameplay) &&
             liveMatch.Phase == SkirmishPhase.Preparing && liveMatch.ElapsedSeconds == 0f,
            "live preparing session crossed original outcome phase gate");

        liveMatch.Phase = SkirmishPhase.Playing;
        Need(!SkirmishOutcomeRules.Evaluate(live, ref liveMatch, 1f, ref liveGameplay) &&
             liveMatch.Phase == SkirmishPhase.Playing && liveMatch.ElapsedSeconds == 1f,
            "live playing session did not advance from actual base health");
        live.SetComponentData(enemyBase, new UnitHealth { Current = 0, Max = 100 });
        Need(SkirmishOutcomeRules.Evaluate(live, ref liveMatch, 1f, ref liveGameplay) &&
             liveMatch.Phase == SkirmishPhase.Finished && liveMatch.Outcome == SkirmishOutcome.Victory &&
             liveGameplay.PlayRequested == 0 && liveGameplay.SimulationActive == 0 &&
             liveGameplay.SelectionModeActive == 0 && liveGameplay.BuildModeActive == 0 &&
             liveGameplay.FullscreenMapOpen == 1,
            "live base destruction or native gameplay deactivation changed");

        Console.WriteLine("[S073LegacyQueueOutcomeControls] result=Passed queued=1 snapshot=1 seed=1 content_denied=1 campaign_excluded=1 existing_excluded=1 pending_start_excluded=1 retry_cleanup=1 non_s073=1 outcome_policy=1 live_phase_gates=1 actual_base_health=1 gameplay_deactivation=1 scope=BoundedSourceFixture_NotMissionReadiness");
    }
}
