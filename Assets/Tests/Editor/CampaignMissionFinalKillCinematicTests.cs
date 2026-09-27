using Game.Components;
using Game.Missions.Contracts;
using Game.Rendering;
using Game.Runtime;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class CampaignMissionFinalKillCinematicTests
{
    private static readonly FixedString64Bytes Session = "final-kill-session";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new CampaignMissionFinalKillCinematicTests();
            tests.TimeScale_EasesIntoSlowMotionAndBackToNormal(); passed++;
            tests.Trigger_OnlyWhenTheLastLiveHostileFallsDuringCombat(); passed++;
            tests.Shot_LooksFromNearestFriendlyTowardVictim(); passed++;
            tests.CameraRestore_OnlyWhenMissionIsStillInCombatWithoutOutcome(); passed++;
            tests.System_StartsShotAndHoldsResultWhenLastHostileDies(); passed++;
            tests.System_ReleasesHoldOnDefeatAndNewAttempt(); passed++;
            tests.System_DoesNotTriggerWhileOtherHostilesRemain(); passed++;
            tests.System_DoesNotInterruptOpeningCameraTour(); passed++;
            tests.Shake_AttenuatesDecaysAndStaysBounded(); passed++;
            Debug.Log($"[CampaignMissionFinalKillCinematicValidation] result=Passed tests={passed}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[CampaignMissionFinalKillCinematicValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void TimeScale_EasesIntoSlowMotionAndBackToNormal()
    {
        Assert.AreEqual(1f, CampaignMissionFinalKillCinematicHelper.EvaluateTimeScale(0f));
        float easing = CampaignMissionFinalKillCinematicHelper.EvaluateTimeScale(
            CampaignMissionFinalKillCinematicHelper.EaseInSeconds * 0.5f);
        Assert.Less(easing, 1f);
        Assert.Greater(easing, CampaignMissionFinalKillCinematicHelper.SlowTimeScale);
        Assert.AreEqual(
            CampaignMissionFinalKillCinematicHelper.SlowTimeScale,
            CampaignMissionFinalKillCinematicHelper.EvaluateTimeScale(1f), 0.0001f);
        float recovering = CampaignMissionFinalKillCinematicHelper.EvaluateTimeScale(
            (CampaignMissionFinalKillCinematicHelper.HoldEndSeconds +
             CampaignMissionFinalKillCinematicHelper.DurationSeconds) * 0.5f);
        Assert.Greater(recovering, CampaignMissionFinalKillCinematicHelper.SlowTimeScale);
        Assert.Less(recovering, 1f);
        Assert.AreEqual(1f, CampaignMissionFinalKillCinematicHelper.EvaluateTimeScale(
            CampaignMissionFinalKillCinematicHelper.DurationSeconds));
        Assert.IsTrue(CampaignMissionFinalKillCinematicHelper.IsFinished(
            CampaignMissionFinalKillCinematicHelper.DurationSeconds));
    }

    [Test]
    public void Trigger_OnlyWhenTheLastLiveHostileFallsDuringCombat()
    {
        Assert.IsTrue(CampaignMissionFinalKillCinematicHelper.ShouldTrigger(
            1, 0, MissionPhaseKind.Engage, MissionPhaseKind.Engage, MissionOutcomeKind.None));
        Assert.IsTrue(CampaignMissionFinalKillCinematicHelper.ShouldTrigger(
            3, 0, MissionPhaseKind.Engage, MissionPhaseKind.Result, MissionOutcomeKind.Victory),
            "Simultaneous deaths that resolve victory in the same frame still earn the shot.");
        Assert.IsFalse(CampaignMissionFinalKillCinematicHelper.ShouldTrigger(
            0, 0, MissionPhaseKind.Engage, MissionPhaseKind.Engage, MissionOutcomeKind.None));
        Assert.IsFalse(CampaignMissionFinalKillCinematicHelper.ShouldTrigger(
            2, 1, MissionPhaseKind.Engage, MissionPhaseKind.Engage, MissionOutcomeKind.None));
        Assert.IsFalse(CampaignMissionFinalKillCinematicHelper.ShouldTrigger(
            1, 0, MissionPhaseKind.Result, MissionPhaseKind.Result, MissionOutcomeKind.Victory));
        Assert.IsFalse(CampaignMissionFinalKillCinematicHelper.ShouldTrigger(
            1, 0, MissionPhaseKind.Engage, MissionPhaseKind.Engage, MissionOutcomeKind.Defeat));
        Assert.IsFalse(CampaignMissionFinalKillCinematicHelper.ShouldTrigger(
            1, 0, MissionPhaseKind.InteractiveBrief, MissionPhaseKind.InteractiveBrief, MissionOutcomeKind.None));
    }

    [Test]
    public void Shot_LooksFromNearestFriendlyTowardVictim()
    {
        float3 victim = new(10f, 2f, 20f);
        float3 friendly = new(10f, 2f, 0f);
        RuntimeCameraFocusRequestComponent shot =
            CampaignMissionFinalKillCinematicHelper.BuildShot(victim, friendly, true, 123f);

        Assert.AreEqual(1, shot.Requested);
        Assert.AreEqual(1, shot.Smooth);
        Assert.AreEqual(1, shot.UseExplicitPerspective);
        Assert.AreEqual(0f, shot.Perspective.z, 0.01f, "Yaw must face north from the friendly toward the victim.");
        Assert.AreEqual(victim.y + CampaignMissionFinalKillCinematicHelper.ShotHeightAboveVictim, shot.Perspective.x, 0.001f);
        Assert.AreEqual(CampaignMissionFinalKillCinematicHelper.ShotPitchDegrees, shot.Perspective.y, 0.001f);
        Assert.AreEqual(CampaignMissionFinalKillCinematicHelper.ShotFieldOfView, shot.Perspective.w, 0.001f);
        Assert.AreEqual(victim.z - CampaignMissionFinalKillCinematicHelper.MaxStandOffDistance, shot.World.z, 0.001f);

        RuntimeCameraFocusRequestComponent west =
            CampaignMissionFinalKillCinematicHelper.BuildShot(new float3(0f, 0f, 0f), new float3(4f, 0f, 0f), true, 0f);
        Assert.AreEqual(270f, west.Perspective.z, 0.01f);

        RuntimeCameraFocusRequestComponent fallback =
            CampaignMissionFinalKillCinematicHelper.BuildShot(victim, default, false, -30f);
        Assert.AreEqual(330f, fallback.Perspective.z, 0.01f);
        Assert.AreEqual(victim, fallback.World);
    }

    [Test]
    public void CameraRestore_OnlyWhenMissionIsStillInCombatWithoutOutcome()
    {
        Assert.IsTrue(CampaignMissionFinalKillCinematicHelper.ShouldRestoreCamera(
            MissionPhaseKind.Engage, MissionOutcomeKind.None));
        Assert.IsFalse(CampaignMissionFinalKillCinematicHelper.ShouldRestoreCamera(
            MissionPhaseKind.SecureCorridor, MissionOutcomeKind.None));
        Assert.IsFalse(CampaignMissionFinalKillCinematicHelper.ShouldRestoreCamera(
            MissionPhaseKind.Result, MissionOutcomeKind.Victory));
    }

    [Test]
    public void System_StartsShotAndHoldsResultWhenLastHostileDies()
    {
        using var fixture = new Fixture();
        Entity hostile = fixture.AddUnit(FactionIdentity.EnemyFactionId, new float3(0f, 0f, 30f), 10);
        fixture.AddUnit(FactionIdentity.PlayerFactionId, new float3(0f, 0f, 0f), 10);
        fixture.Tick();
        Assert.IsFalse(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root));

        fixture.Kill(hostile);
        fixture.Tick();

        CampaignMissionFinalKillCinematicComponent cinematic =
            fixture.Em.GetComponentData<CampaignMissionFinalKillCinematicComponent>(fixture.Root);
        Assert.AreEqual(1, cinematic.Active);
        Assert.AreEqual(1u, cinematic.Version);
        Assert.AreEqual(new float3(0f, 0f, 30f), cinematic.VictimPosition);
        Assert.IsTrue(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root));

        RuntimeCameraFocusRequestComponent focus = fixture.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(fixture.Focus);
        Assert.AreEqual(1, focus.Requested);
        Assert.AreEqual(1, focus.UseExplicitPerspective);
        Assert.AreEqual(0f, focus.Perspective.z, 0.01f);
    }

    [Test]
    public void System_ReleasesHoldOnDefeatAndNewAttempt()
    {
        using var fixture = new Fixture();
        Entity hostile = fixture.AddUnit(FactionIdentity.EnemyFactionId, new float3(5f, 0f, 5f), 10);
        fixture.Tick();
        fixture.Kill(hostile);
        fixture.Tick();
        Assert.IsTrue(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root));

        CampaignMissionRuntimeComponent runtime = fixture.Em.GetComponentData<CampaignMissionRuntimeComponent>(fixture.Root);
        runtime.Outcome = MissionOutcomeKind.Defeat;
        fixture.Em.SetComponentData(fixture.Root, runtime);
        fixture.Tick();
        Assert.IsFalse(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root));

        runtime.Outcome = MissionOutcomeKind.None;
        fixture.Em.SetComponentData(fixture.Root, runtime);
        Entity second = fixture.AddUnit(FactionIdentity.EnemyFactionId, new float3(5f, 0f, 5f), 10);
        fixture.Tick();
        fixture.Kill(second);
        fixture.Tick();
        Assert.IsTrue(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root));

        runtime.AttemptOrdinal++;
        fixture.Em.SetComponentData(fixture.Root, runtime);
        fixture.Tick();
        Assert.IsFalse(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root),
            "A retry must never inherit the previous attempt's result hold.");
    }

    [Test]
    public void System_DoesNotTriggerWhileOtherHostilesRemain()
    {
        using var fixture = new Fixture();
        Entity first = fixture.AddUnit(FactionIdentity.EnemyFactionId, new float3(0f, 0f, 10f), 10);
        fixture.AddUnit(FactionIdentity.EnemyFactionId, new float3(0f, 0f, 20f), 10);
        Entity civilian = fixture.AddUnit(FactionIdentity.NeutralFactionId, new float3(0f, 0f, 30f), 10);
        fixture.Tick();
        fixture.Kill(first);
        fixture.Kill(civilian);
        fixture.Tick();
        Assert.IsFalse(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root));
    }

    [Test]
    public void System_DoesNotInterruptOpeningCameraTour()
    {
        using var fixture = new Fixture();
        fixture.Em.AddComponentData(fixture.Root, new CampaignMissionOpeningPresentationComponent
        {
            SessionToken = Session,
            Stage = 1
        });
        Entity openingHostile = fixture.AddUnit(FactionIdentity.EnemyFactionId, new float3(0f, 0f, 30f), 10);
        fixture.AddUnit(FactionIdentity.PlayerFactionId, new float3(0f, 0f, 0f), 10);
        fixture.Tick();
        fixture.Kill(openingHostile);
        fixture.Tick();

        Assert.IsFalse(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root));
        Assert.AreEqual(0, fixture.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(fixture.Focus).Requested,
            "A kill during the opening must not replace the tour with a close-up camera request.");

        CampaignMissionOpeningPresentationComponent opening =
            fixture.Em.GetComponentData<CampaignMissionOpeningPresentationComponent>(fixture.Root);
        opening.Stage = 6;
        fixture.Em.SetComponentData(fixture.Root, opening);
        Entity laterHostile = fixture.AddUnit(FactionIdentity.EnemyFactionId, new float3(0f, 0f, 20f), 10);
        fixture.Tick();
        fixture.Kill(laterHostile);
        fixture.Tick();

        Assert.IsTrue(CampaignMissionFinalKillCinematicSystem.IsHoldingMissionResult(fixture.Em, fixture.Root),
            "The final-kill shot remains available once the opening has handed control back.");
    }

    [Test]
    public void Shake_AttenuatesDecaysAndStaysBounded()
    {
        Assert.AreEqual(CombatCameraShakeHelper.LargeExplosionTrauma,
            CombatCameraShakeHelper.ResolveEventTrauma(Game.Configs.AudioEventIds.GameplayExplosionLargeHash));
        Assert.AreEqual(0f, CombatCameraShakeHelper.ResolveEventTrauma(0u));

        float near = CombatCameraShakeHelper.Attenuate(1f, 0f, 24f);
        float mid = CombatCameraShakeHelper.Attenuate(1f, 40f, 24f);
        float far = CombatCameraShakeHelper.Attenuate(1f, 500f, 24f);
        Assert.AreEqual(1f, near, 0.0001f);
        Assert.Less(mid, near);
        Assert.AreEqual(0f, far);

        Assert.AreEqual(0f, CombatCameraShakeHelper.Decay(0.1f, 1f));
        Assert.Less(CombatCameraShakeHelper.Decay(0.8f, 0.1f), 0.8f);

        for (float time = 0f; time < 3f; time += 0.013f)
        {
            float2 offset = CombatCameraShakeHelper.EvaluateOffset(1f, time, 24f);
            Assert.LessOrEqual(math.length(offset), CombatCameraShakeHelper.MaximumMaxOffset * math.SQRT2 + 0.0001f);
        }

        Assert.AreEqual(float2.zero, CombatCameraShakeHelper.EvaluateOffset(0f, 1.3f, 24f));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly World _world = new("Final kill cinematic fixture");
        private readonly SystemHandle _system;
        private int _frame;

        public EntityManager Em => _world.EntityManager;
        public readonly Entity Root;
        public readonly Entity Focus;

        public Fixture()
        {
            Root = Em.CreateEntity(typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent));
            Em.SetComponentData(Root, new CampaignMissionRuntimeComponent
            {
                MissionId = "saga.test.final_kill",
                SessionToken = Session,
                AttemptOrdinal = 1,
                Phase = MissionPhaseKind.Engage,
                Outcome = MissionOutcomeKind.None
            });
            Focus = Em.CreateEntity(typeof(RuntimeCameraFocusRequestComponent));
            _system = _world.GetOrCreateSystem<CampaignMissionFinalKillCinematicSystem>();
        }

        public Entity AddUnit(byte factionId, float3 position, int health)
        {
            Entity entity = Em.CreateEntity(
                typeof(CampaignMissionUnitRoleComponent), typeof(Faction), typeof(UnitHealth),
                typeof(UnitCombat), typeof(LocalTransform));
            Em.SetComponentData(entity, new CampaignMissionUnitRoleComponent { SessionToken = Session });
            Em.SetComponentData(entity, new Faction { Id = factionId });
            Em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
            Em.SetComponentData(entity, new UnitCombat { CanAttack = 1 });
            Em.SetComponentData(entity, LocalTransform.FromPosition(position));
            return entity;
        }

        public void Kill(Entity entity)
        {
            UnitHealth health = Em.GetComponentData<UnitHealth>(entity);
            health.Current = 0;
            Em.SetComponentData(entity, health);
        }

        public void Tick()
        {
            _world.SetTime(new TimeData(++_frame * 0.016, 0.016f));
            _system.Update(_world.Unmanaged);
        }

        public void Dispose() => _world.Dispose();
    }
}
#endif
