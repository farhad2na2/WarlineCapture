using Game.Components;
using Game.Runtime;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

public sealed class TacticalFollowAttackCinematicHelperTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.PhaseBoundaries_SelectExpectedShot());
            passed++;
            RunCase(test => test.TimeScale_RampsOutOfSlowMotionAtImpactEnd());
            passed++;
            RunCase(test => test.LaunchShot_FramesJetAndTargetDirection());
            passed++;
            RunCase(test => test.MissilePathShot_FramesProjectileTravel());
            passed++;
            RunCase(test => test.ImpactShot_FramesExplosionNearTarget());
            passed++;
            RunCase(test => test.FlyoverShot_TracksJetThenFallsBackWithoutNaN());
            passed++;
            RunCase(test => test.BuildPose_UsesSnapDampingOnlyForPhaseEntry());
            passed++;
            RunCase(test => test.BuildTarget_UsesAttackImpactTarget());
            passed++;
            RunCase(test => test.BuildInitialState_UsesTypedCinematicContract());
            passed++;
            RunCase(test => test.EvaluateStateProgress_TriggersProjectileImpactAndFlyover());
            passed++;
            UnityEngine.Debug.Log($"[TacticalFollowAttackCinematicHelperValidation] result=Passed tests={passed}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError($"[TacticalFollowAttackCinematicHelperValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void PhaseBoundaries_SelectExpectedShot()
    {
        Assert.AreEqual(
            TacticalFollowAttackCinematicPhase.Launch,
            TacticalFollowAttackCinematicHelper.EvaluatePhase(0f, out float launchElapsed));
        Assert.AreEqual(0f, launchElapsed);

        Assert.AreEqual(
            TacticalFollowAttackCinematicPhase.MissilePath,
            TacticalFollowAttackCinematicHelper.EvaluatePhase(
                TacticalFollowAttackCinematicHelper.LaunchDurationSeconds,
                out float missileElapsed));
        Assert.AreEqual(0f, missileElapsed, 0.0001f);

        Assert.AreEqual(
            TacticalFollowAttackCinematicPhase.Impact,
            TacticalFollowAttackCinematicHelper.EvaluatePhase(
                TacticalFollowAttackCinematicHelper.LaunchDurationSeconds +
                TacticalFollowAttackCinematicHelper.MissilePathDurationSeconds,
                out float impactElapsed));
        Assert.AreEqual(0f, impactElapsed, 0.0001f);

        Assert.AreEqual(
            TacticalFollowAttackCinematicPhase.Flyover,
            TacticalFollowAttackCinematicHelper.EvaluatePhase(
                TacticalFollowAttackCinematicHelper.LaunchDurationSeconds +
                TacticalFollowAttackCinematicHelper.MissilePathDurationSeconds +
                TacticalFollowAttackCinematicHelper.ImpactDurationSeconds,
                out float flyoverElapsed));
        Assert.AreEqual(0f, flyoverElapsed, 0.0001f);

        Assert.AreEqual(
            TacticalFollowAttackCinematicPhase.None,
            TacticalFollowAttackCinematicHelper.EvaluatePhase(
                TacticalFollowAttackCinematicHelper.TotalDurationSeconds,
                out float noneElapsed));
        Assert.AreEqual(0f, noneElapsed, 0.0001f);
        Assert.IsTrue(TacticalFollowAttackCinematicHelper.IsFinished(
            TacticalFollowAttackCinematicHelper.TotalDurationSeconds));
    }

    [Test]
    public void TimeScale_RampsOutOfSlowMotionAtImpactEnd()
    {
        float rampEnd =
            TacticalFollowAttackCinematicHelper.LaunchDurationSeconds +
            TacticalFollowAttackCinematicHelper.MissilePathDurationSeconds +
            TacticalFollowAttackCinematicHelper.ImpactDurationSeconds;
        float rampStart = rampEnd - TacticalFollowAttackCinematicHelper.TimeScaleRampSeconds;
        Assert.AreEqual(
            TacticalFollowAttackCinematicHelper.SlowMotionTimeScale,
            TacticalFollowAttackCinematicHelper.EvaluateTimeScale(0f),
            0.0001f);
        Assert.AreEqual(
            TacticalFollowAttackCinematicHelper.SlowMotionTimeScale,
            TacticalFollowAttackCinematicHelper.EvaluateTimeScale(rampStart - 0.01f),
            0.0001f);

        float midRamp = TacticalFollowAttackCinematicHelper.EvaluateTimeScale(
            rampStart + TacticalFollowAttackCinematicHelper.TimeScaleRampSeconds * 0.5f);
        Assert.Greater(midRamp, TacticalFollowAttackCinematicHelper.SlowMotionTimeScale);
        Assert.Less(midRamp, 1f);
        Assert.AreEqual(1f, TacticalFollowAttackCinematicHelper.EvaluateTimeScale(rampEnd), 0.0001f);
        Assert.AreEqual(
            1f,
            TacticalFollowAttackCinematicHelper.EvaluateTimeScale(
                TacticalFollowAttackCinematicHelper.TotalDurationSeconds + 1f),
            0.0001f);
    }

    [Test]
    public void LaunchShot_FramesJetAndTargetDirection()
    {
        TacticalFollowAttackCinematicHelper.ShotContext context = CreateContext(hasJet: true);
        TacticalFollowAttackCinematicHelper.Shot shot = TacticalFollowAttackCinematicHelper.EvaluateShot(
            TacticalFollowAttackCinematicPhase.Launch,
            0f,
            context);
        Assert.Less(math.distance(shot.CameraPosition, context.JetPosition), 15f);
        Assert.Greater(math.dot(math.normalizesafe(shot.LookAt - context.JetPosition), context.AttackDirection), 0f);
        Assert.AreEqual(30f, shot.FieldOfView, 0.0001f);
    }

    [Test]
    public void MissilePathShot_FramesProjectileTravel()
    {
        TacticalFollowAttackCinematicHelper.ShotContext context = CreateContext(hasJet: true);
        float phaseElapsed = TacticalFollowAttackCinematicHelper.MissilePathDurationSeconds * 0.5f;
        TacticalFollowAttackCinematicHelper.Shot shot = TacticalFollowAttackCinematicHelper.EvaluateShot(
            TacticalFollowAttackCinematicPhase.MissilePath,
            phaseElapsed,
            context);

        float projectileProgress = TacticalFollowAttackCinematicHelper.EvaluateProjectileProgress(
            TacticalFollowAttackCinematicHelper.LaunchDurationSeconds + phaseElapsed);
        float3 expectedProjectile = math.lerp(context.LaunchPosition, context.ImpactPosition, projectileProgress);
        Assert.Less(math.distance(shot.CameraPosition, expectedProjectile), 20f);
        Assert.Greater(math.dot(math.normalizesafe(shot.LookAt - expectedProjectile), context.AttackDirection), 0f);
        Assert.AreEqual(38f, shot.FieldOfView, 0.0001f);
    }

    [Test]
    public void ImpactShot_FramesExplosionNearTarget()
    {
        TacticalFollowAttackCinematicHelper.ShotContext context = CreateContext(hasJet: true);
        TacticalFollowAttackCinematicHelper.Shot shot = TacticalFollowAttackCinematicHelper.EvaluateShot(
            TacticalFollowAttackCinematicPhase.Impact,
            TacticalFollowAttackCinematicHelper.ImpactDurationSeconds * 0.5f,
            context);
        Assert.Less(math.distance(shot.CameraPosition, context.ImpactPosition), 15f);
        Assert.Less(math.distance(shot.LookAt, context.ImpactPosition), 4f);
        Assert.AreEqual(36f, shot.FieldOfView, 0.0001f);
    }

    [Test]
    public void FlyoverShot_TracksJetThenFallsBackWithoutNaN()
    {
        TacticalFollowAttackCinematicHelper.ShotContext context = CreateContext(hasJet: true);
        TacticalFollowAttackCinematicHelper.Shot shot = TacticalFollowAttackCinematicHelper.EvaluateShot(
            TacticalFollowAttackCinematicPhase.Flyover,
            TacticalFollowAttackCinematicHelper.FlyoverDurationSeconds * 0.9f,
            context);
        Assert.Less(
            math.distance(shot.LookAt, context.JetPosition),
            math.distance(shot.LookAt, context.ImpactPosition));

        TacticalFollowAttackCinematicHelper.ShotContext fallback = CreateContext(hasJet: false);
        TacticalFollowAttackCinematicHelper.Shot fallbackShot = TacticalFollowAttackCinematicHelper.EvaluateShot(
            TacticalFollowAttackCinematicPhase.Flyover,
            TacticalFollowAttackCinematicHelper.FlyoverDurationSeconds * 0.75f,
            fallback);
        Assert.IsTrue(math.all(math.isfinite(fallbackShot.CameraPosition)));
        Assert.IsTrue(math.all(math.isfinite(fallbackShot.LookAt)));
    }

    [Test]
    public void BuildPose_UsesSnapDampingOnlyForPhaseEntry()
    {
        TacticalFollowAttackCinematicHelper.Shot shot = TacticalFollowAttackCinematicHelper.EvaluateShot(
            TacticalFollowAttackCinematicPhase.Impact,
            0f,
            CreateContext(hasJet: true));
        TacticalFollowCameraPoseComponent snapPose =
            TacticalFollowAttackCinematicHelper.BuildPose(shot, snapToShot: true);
        TacticalFollowCameraPoseComponent dampedPose =
            TacticalFollowAttackCinematicHelper.BuildPose(shot, snapToShot: false);
        Assert.AreEqual(TacticalFollowCameraPoseSource.TemporaryMissile, snapPose.Source);
        Assert.AreEqual(0f, snapPose.PositionDampingSeconds);
        Assert.Greater(dampedPose.PositionDampingSeconds, 0f);
    }

    [Test]
    public void BuildTarget_UsesAttackImpactTarget()
    {
        Entity target = new Entity { Index = 12, Version = 1 };
        float3 impactPosition = new(30f, 0f, 5f);
        TacticalFollowCameraTargetComponent cameraTarget =
            TacticalFollowAttackCinematicHelper.BuildTarget(target, impactPosition, new float3(1f, 0f, 0f));
        Assert.AreEqual(1, cameraTarget.Valid);
        Assert.AreEqual(TacticalFollowCameraTargetKind.AttackImpact, cameraTarget.TargetKind);
        Assert.AreEqual(target, cameraTarget.TargetEntity);
        AssertFloat3(impactPosition, cameraTarget.Center);
    }

    [Test]
    public void BuildInitialState_UsesTypedCinematicContract()
    {
        Entity source = new Entity { Index = 2, Version = 1 };
        Entity target = new Entity { Index = 12, Version = 1 };
        float3 launchPosition = new(0f, 10f, 0f);
        float3 impactPosition = new(30f, 0f, 5f);

        TacticalFollowAttackCinematicStateComponent state =
            TacticalFollowAttackCinematicHelper.BuildInitialState(
                source,
                target,
                launchPosition,
                impactPosition,
                impactPosition - launchPosition,
                requestedStartTime: 42f,
                lastEndedElapsedTime: 7f);

        Assert.AreEqual(1, state.Active);
        Assert.AreEqual(TacticalFollowAttackCinematicAttackKind.FollowedAirInstantHit, state.AttackKind);
        Assert.AreEqual(TacticalFollowAttackCinematicPhase.Launch, state.LastAppliedPhase);
        Assert.AreEqual(42f, state.RequestedStartTime, 0.0001f);
        Assert.AreEqual(source, state.SourceEntity);
        Assert.AreEqual(target, state.TargetEntity);
        AssertFloat3(launchPosition, state.ProjectilePosition);
        Assert.AreEqual(0f, state.ProjectileProgress, 0.0001f);
        Assert.AreEqual(0, state.LaunchEventTriggered);
        Assert.AreEqual(0, state.ProjectileActive);
        Assert.AreEqual(0, state.ImpactEventTriggered);
        Assert.AreEqual(0, state.FlyoverEventTriggered);
        Assert.AreEqual(0, state.Completed);
        Assert.AreEqual(TacticalFollowAttackCinematicAbortReason.None, state.AbortReason);
        Assert.AreEqual(7f, state.LastEndedElapsedTime, 0.0001f);
    }

    [Test]
    public void EvaluateStateProgress_TriggersProjectileImpactAndFlyover()
    {
        TacticalFollowAttackCinematicStateComponent state =
            TacticalFollowAttackCinematicHelper.BuildInitialState(
                new Entity { Index = 2, Version = 1 },
                new Entity { Index = 12, Version = 1 },
                new float3(0f, 10f, 0f),
                new float3(30f, 0f, 5f),
                new float3(1f, 0f, 0.1f),
                requestedStartTime: 0f,
                lastEndedElapsedTime: 0f);

        state.ElapsedUnscaledSeconds = TacticalFollowAttackCinematicHelper.ProjectileLaunchBeatSeconds + 0.05f;
        state = TacticalFollowAttackCinematicHelper.EvaluateStateProgress(state);
        Assert.AreEqual(1, state.LaunchEventTriggered);
        Assert.AreEqual(1, state.ProjectileActive);
        Assert.AreEqual(0, state.ImpactEventTriggered);
        Assert.Greater(state.ProjectileProgress, 0f);
        Assert.Less(state.ProjectileProgress, 1f);

        state.ElapsedUnscaledSeconds = TacticalFollowAttackCinematicHelper.ImpactEventBeatSeconds;
        state = TacticalFollowAttackCinematicHelper.EvaluateStateProgress(state);
        Assert.AreEqual(1, state.ImpactEventTriggered);
        Assert.AreEqual(0, state.ProjectileActive);
        Assert.AreEqual(1f, state.ProjectileProgress, 0.0001f);
        AssertFloat3(state.ImpactPosition, state.ProjectilePosition);

        state.ElapsedUnscaledSeconds =
            TacticalFollowAttackCinematicHelper.LaunchDurationSeconds +
            TacticalFollowAttackCinematicHelper.MissilePathDurationSeconds +
            TacticalFollowAttackCinematicHelper.ImpactDurationSeconds +
            0.01f;
        state = TacticalFollowAttackCinematicHelper.EvaluateStateProgress(state);
        Assert.AreEqual(1, state.FlyoverEventTriggered);

        state.ElapsedUnscaledSeconds = TacticalFollowAttackCinematicHelper.TotalDurationSeconds;
        state = TacticalFollowAttackCinematicHelper.EvaluateStateProgress(state);
        Assert.AreEqual(1, state.Completed);
        Assert.AreEqual(TacticalFollowAttackCinematicAbortReason.Completed, state.AbortReason);
    }

    private static TacticalFollowAttackCinematicHelper.ShotContext CreateContext(bool hasJet)
    {
        return new TacticalFollowAttackCinematicHelper.ShotContext(
            new float3(0f, 10f, 0f),
            new float3(30f, 0f, 5f),
            new float3(1f, 0f, 0.1f),
            new float3(8f, 12f, 1f),
            hasJet);
    }

    private static void AssertFloat3(float3 expected, float3 actual)
    {
        Assert.AreEqual(expected.x, actual.x, 0.0001f);
        Assert.AreEqual(expected.y, actual.y, 0.0001f);
        Assert.AreEqual(expected.z, actual.z, 0.0001f);
    }

    private static void RunCase(Action<TacticalFollowAttackCinematicHelperTests> testCase)
    {
        testCase(new TacticalFollowAttackCinematicHelperTests());
    }
}
#endif
