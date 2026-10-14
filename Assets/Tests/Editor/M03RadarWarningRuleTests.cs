using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class M03RadarWarningRuleTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            M03RadarWarningRuleTests tests = new();
            tests.AllDeclaredMembersMustBeDefeatedAndActivated();
            tests.LastNativeDeathRefreshesBothElementsBeforeTerminalTransition();
            tests.LivingMemberKeepsItsElementUnresolved();
            tests.UnactivatedElementNeverResolvesFromDeathsAlone();
            tests.StaleDefenseAttemptCannotRefreshResolution();
            tests.LossWinsOverLastKill(0);
            tests.LossWinsOverLastKill(1);
            tests.LossWinsOverLastKill(2);
            tests.SquadLossDoesNotInventAnAdditionalDefeatCondition();
            tests.CameraAndProducerReadinessGateControlHandoff();
            tests.TerminalOutcomeDoesNotChange();
            tests.FinaleDelaysPresentationWithoutAdvancingOrOverridingOutcome();
            for (int civilianLoss = 0; civilianLoss <= 1; civilianLoss++)
                for (byte damaged = 0; damaged <= 1; damaged++)
                    tests.StarGoalsAreIndependent(civilianLoss, damaged);
            Debug.Log("[M03RadarWarningRuleValidation] result=Passed tests=16");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M03RadarWarningRuleValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void AllDeclaredMembersMustBeDefeatedAndActivated()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        facts.DefenseWaveActivated = 0;
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out _));
        facts.DefenseWaveActivated = 1;
        facts.HostileDefeatedCount = 6;
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out _));
        facts.HostileDefeatedCount = 7;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionOutcomeKind.Victory, result.Outcome);
    }

    [TestCase(0), TestCase(1), TestCase(2)]
    public void LossWinsOverLastKill(int loss)
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        if (loss == 0) facts.ForwardPostDestroyed = 1;
        if (loss == 1) facts.CoreBreached = 1;
        if (loss == 2) facts.HostileRosterIntegrityFault = 1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionOutcomeKind.Defeat, result.Outcome);
    }

    [Test]
    public void SquadLossDoesNotInventAnAdditionalDefeatCondition()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        facts.CommandSquadAlive = 0;
        facts.SquadLossCount = 8;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionOutcomeKind.Victory, result.Outcome);
    }

    [Test]
    public void CameraAndProducerReadinessGateControlHandoff()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        runtime.Phase = MissionPhaseKind.FindSquad;
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, false, out _));
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out CampaignMissionRuntimeComponent result));
        Assert.AreEqual(MissionPhaseKind.Engage, result.Phase);
        Assert.AreEqual(MissionOutcomeKind.None, result.Outcome);
    }

    [Test]
    public void FinaleDelaysPresentationWithoutAdvancingOrOverridingOutcome()
    {
        var runtime=Runtime(); var facts=VictoryFacts(); facts.FinalePresentationRequired=1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime,in facts,true,out runtime));
        Assert.AreEqual(MissionPhaseKind.SecureCorridor,runtime.Phase);
        Assert.AreEqual(MissionOutcomeKind.None,runtime.Outcome);
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime,in facts,true,out _));
        facts.FinalePresentationComplete=1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime,in facts,true,out var result));
        Assert.AreEqual(MissionOutcomeKind.Victory,result.Outcome);
        facts.ForwardPostDestroyed=1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime,in facts,true,out result));
        Assert.AreEqual(MissionOutcomeKind.Defeat,result.Outcome);
    }

    [Test]
    public void TerminalOutcomeDoesNotChange()
    {
        CampaignMissionRuntimeComponent runtime = Runtime();
        runtime.Phase = MissionPhaseKind.Result;
        runtime.Outcome = MissionOutcomeKind.Defeat;
        runtime.ReturnDestination = MissionReturnDestinationKind.CampaignOperations;
        CampaignMissionAttemptFactsComponent facts = VictoryFacts();
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out _));
    }

    [TestCase(0, (byte)0), TestCase(1, (byte)0), TestCase(0, (byte)1), TestCase(1, (byte)1)]
    public void StarGoalsAreIndependent(int civilianLoss, byte postDamaged)
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        BlobBuilderArray<CampaignMissionStarRuleBlob> stars = builder.Allocate(ref missions[0].StarRules, 3);
        stars[0] = new() { Rule = MissionStarRuleKind.CompleteMission, StarIndex = 1 };
        stars[1] = new() { Rule = MissionStarRuleKind.NoCivilianLoss, StarIndex = 2 };
        stars[2] = new() { Rule = MissionStarRuleKind.NoPostDamage, StarIndex = 3 };
        using BlobAssetReference<CampaignMissionCatalogBlob> blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Temp);
        Assert.IsTrue(CampaignMissionResultProjectionSystem.TryEvaluateStars(MissionOutcomeKind.Victory,
            190000, 2, civilianLoss, postDamaged, ref blob.Value.Missions[0].StarRules, out byte count));
        Assert.AreEqual(3 - civilianLoss - postDamaged, count);
        Assert.IsTrue(CampaignMissionResultProjectionSystem.TryEvaluateStars(MissionOutcomeKind.Defeat,
            190000, 2, civilianLoss, postDamaged, ref blob.Value.Missions[0].StarRules, out count));
        Assert.AreEqual(0, count);
    }

    [Test]
    public void LastNativeDeathRefreshesBothElementsBeforeTerminalTransition()
    {
        using var f = new ResolutionFixture();
        f.SetLastHealth(0);
        f.ObserveNativeDeaths();
        Assert.AreEqual(7, f.DefeatedMembers);
        Assert.AreEqual(2, f.ResolvedElements);
        var runtime = Runtime();
        var facts = VictoryFacts();
        facts.FinalePresentationRequired = 1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out var finale));
        Assert.AreEqual(MissionPhaseKind.SecureCorridor, finale.Phase);
        Assert.AreEqual(MissionOutcomeKind.None, finale.Outcome);
        facts.FinalePresentationComplete = 1;
        Assert.IsTrue(CampaignMissionDefenseRuleUtility.TryAdvance(in finale, in facts, true, out var victory));
        Assert.AreEqual(MissionOutcomeKind.Victory, victory.Outcome);
        Assert.AreEqual(2, f.ResolvedElements);
        // Native observer is idempotent; terminal metadata remains correct.
        f.Em.SetComponentData(f.Root, victory);
        f.ObserveNativeDeaths();
        Assert.AreEqual(2, f.ResolvedElements);
    }

    [Test]
    public void LivingMemberKeepsItsElementUnresolved()
    {
        using var f = new ResolutionFixture();
        f.ObserveNativeDeaths();
        Assert.AreEqual(6, f.DefeatedMembers);
        Assert.AreEqual(1, f.ResolvedElements);
        Assert.AreEqual(100, f.Em.GetComponentData<UnitHealth>(f.Last).Current);
        var runtime = Runtime(); var facts = VictoryFacts(); facts.HostileDefeatedCount = 6;
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out _));
    }

    [Test]
    public void UnactivatedElementNeverResolvesFromDeathsAlone()
    {
        using var f = new ResolutionFixture();
        f.SetLastHealth(0);
        var elements = f.Em.GetBuffer<CampaignMissionConvoyElementState>(f.Root);
        var last = elements[1]; last.Activated = 0; elements[1] = last;
        f.ObserveNativeDeaths();
        Assert.AreEqual(7, f.DefeatedMembers);
        Assert.AreEqual(1, f.ResolvedElements);
        var runtime = Runtime(); var facts = VictoryFacts(); facts.DefenseWaveActivated = 0;
        Assert.IsFalse(CampaignMissionDefenseRuleUtility.TryAdvance(in runtime, in facts, true, out _));
    }

    [Test]
    public void StaleDefenseAttemptCannotRefreshResolution()
    {
        using var f = new ResolutionFixture(); f.SetLastHealth(0);
        var defense = f.Em.GetComponentData<CampaignMissionDefenseStateComponent>(f.Root);
        defense.AttemptOrdinal = 2; f.Em.SetComponentData(f.Root, defense);
        f.ObserveNativeDeaths();
        Assert.AreEqual(6, f.DefeatedMembers);
        Assert.AreEqual(1, f.ResolvedElements);
    }

    private sealed class ResolutionFixture : IDisposable
    {
        private readonly World world = new("native convoy terminal resolution regression");
        private readonly SystemHandle death;
        public EntityManager Em => world.EntityManager;
        public readonly Entity Root, Last;
        public ResolutionFixture()
        {
            Root = Em.CreateEntity(typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionDefenseStateComponent));
            Em.SetComponentData(Root, Runtime());
            Em.SetComponentData(Root, new CampaignMissionDefenseStateComponent { SessionToken = "m03-test", AttemptOrdinal = 1, SourceVersion = 1 });
            Em.AddBuffer<CampaignMissionDefenseMember>(Root);
            Em.AddBuffer<CampaignMissionConvoyElementState>(Root);
            for (int i = 0; i < 7; i++)
            {
                var actor = Em.CreateEntity(typeof(UnitHealth), typeof(CampaignMissionUnitRoleComponent));
                Em.SetComponentData(actor, new UnitHealth { Current = i == 6 ? 100 : 0, Max = 100 });
                Em.SetComponentData(actor, new CampaignMissionUnitRoleComponent { SessionToken = "m03-test" });
                // Reacquire after structural actor creation; no retained stale DynamicBuffer.
                Em.GetBuffer<CampaignMissionDefenseMember>(Root).Add(new CampaignMissionDefenseMember { Entity = actor, ElementIndex = i < 3 ? 0 : 1, FactionId = 2, HealthInitialized = 1, Defeated = (byte)(i == 6 ? 0 : 1) });
                if (i == 6) Last = actor;
            }
            var elements = Em.GetBuffer<CampaignMissionConvoyElementState>(Root);
            elements.Add(new CampaignMissionConvoyElementState { Activated = 1, Resolved = 1 });
            elements.Add(new CampaignMissionConvoyElementState { Activated = 1, Resolved = 0 });
            death = world.GetOrCreateSystem<CampaignMissionDefenseDeathObservationSystem>();
        }
        public int DefeatedMembers { get { int count = 0; foreach (var m in Em.GetBuffer<CampaignMissionDefenseMember>(Root, true)) count += m.Defeated; return count; } }
        public int ResolvedElements { get { int count = 0; foreach (var e in Em.GetBuffer<CampaignMissionConvoyElementState>(Root, true)) count += e.Resolved; return count; } }
        public void SetLastHealth(int health) => Em.SetComponentData(Last, new UnitHealth { Current = health, Max = 100 });
        public void ObserveNativeDeaths() => death.Update(world.Unmanaged);
        public void Dispose() => world.Dispose();
    }

    internal static CampaignMissionRuntimeComponent Runtime() => new()
    {
        MissionId = "saga.ch01.m03.radar_warning", ScenarioId = "scenario.ch01.m03.radar_warning",
        OperationMapId = "opmap.ch01.convoy_approach_01", SessionToken = "m03-test", AttemptOrdinal = 1,
        Version = 1, SourceVersion = 1, DeterministicSeed = 3003001, Phase = MissionPhaseKind.Engage,
        LaunchOrigin = MissionLaunchOriginKind.CampaignOperations, RunKind = MissionRunKind.FirstClear
    };

    internal static CampaignMissionAttemptFactsComponent VictoryFacts() => new()
    {
        CommandSquadSpawned = 1, CommandSquadAlive = 1, ForwardPostBound = 1,
        HostileTotalCount = 7, HostileDefeatedCount = 7, DefenseWaveActivated = 1
    };
}
