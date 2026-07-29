using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

public sealed class OperationMapRenderGuardEnvelopeDecisionTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new OperationMapRenderGuardEnvelopeDecisionTests();
            tests.StableRequiredEnvelopeInsideGuard_DoesNotRebuild();
            tests.RequiredEnvelopeLeavingGuard_RebuildsForCameraEnvelope();
            tests.CameraDiscontinuityAndExplicitForce_RebuildForCameraEnvelope();
            tests.VisualStateChange_RebuildsWithoutCameraMovement();
            tests.MapGenerationChange_RebuildsBeforeOtherReasons();
            tests.InitialView_RebuildsBeforeOtherReasons();
            tests.InvalidEnvelope_FailsClosedWithoutReason();
            Debug.Log("[OperationMapRenderGuardEnvelopeValidation] result=Passed tests=7");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[OperationMapRenderGuardEnvelopeValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void StableRequiredEnvelopeInsideGuard_DoesNotRebuild()
    {
        AssertDecision(Input(), OperationMapRenderRebuildReason.None);
    }

    [Test]
    public void RequiredEnvelopeLeavingGuard_RebuildsForCameraEnvelope()
    {
        OperationMapRenderGuardEnvelopeInput input = Input();
        input.RequiredEnvelope.Max.x = 5;
        AssertDecision(input, OperationMapRenderRebuildReason.CameraEnvelopeChanged);
    }

    [Test]
    public void CameraDiscontinuityAndExplicitForce_RebuildForCameraEnvelope()
    {
        OperationMapRenderGuardEnvelopeInput discontinuity = Input();
        discontinuity.CameraDiscontinuity = 1;
        AssertDecision(discontinuity, OperationMapRenderRebuildReason.CameraEnvelopeChanged);

        OperationMapRenderGuardEnvelopeInput forced = Input();
        forced.ForceRebuild = 1;
        AssertDecision(forced, OperationMapRenderRebuildReason.CameraEnvelopeChanged);
    }

    [Test]
    public void VisualStateChange_RebuildsWithoutCameraMovement()
    {
        OperationMapRenderGuardEnvelopeInput input = Input();
        input.DirtyStateChangeCount = 1;
        AssertDecision(input, OperationMapRenderRebuildReason.VisualStateChanged);
    }

    [Test]
    public void MapGenerationChange_RebuildsBeforeOtherReasons()
    {
        OperationMapRenderGuardEnvelopeInput input = Input();
        input.MapGenerationChanged = 1;
        input.DirtyStateChangeCount = 1;
        input.ForceRebuild = 1;
        AssertDecision(input, OperationMapRenderRebuildReason.MapGenerationChanged);
    }

    [Test]
    public void InitialView_RebuildsBeforeOtherReasons()
    {
        OperationMapRenderGuardEnvelopeInput input = Input();
        input.InitialViewApplied = 0;
        input.MapGenerationChanged = 1;
        AssertDecision(input, OperationMapRenderRebuildReason.InitialView);
    }

    [Test]
    public void InvalidEnvelope_FailsClosedWithoutReason()
    {
        OperationMapRenderGuardEnvelopeInput input = Input();
        input.RequiredEnvelope.Min.x = 4;
        input.RequiredEnvelope.Max.x = 3;

        Assert.That(
            OperationMapRenderGuardEnvelopeDecision.TryDecide(in input, out OperationMapRenderRebuildReason reason),
            Is.False);
        Assert.That(reason, Is.EqualTo(OperationMapRenderRebuildReason.None));
    }

    private static void AssertDecision(
        OperationMapRenderGuardEnvelopeInput input,
        OperationMapRenderRebuildReason expected)
    {
        Assert.That(
            OperationMapRenderGuardEnvelopeDecision.TryDecide(in input, out OperationMapRenderRebuildReason actual),
            Is.True);
        Assert.That(actual, Is.EqualTo(expected));
    }

    private static OperationMapRenderGuardEnvelopeInput Input()
    {
        return new OperationMapRenderGuardEnvelopeInput
        {
            InitialViewApplied = 1,
            RequiredEnvelope = Envelope(new int2(0, 0), new int2(2, 2)),
            GuardEnvelope = Envelope(new int2(-1, -1), new int2(3, 3))
        };
    }

    private static OperationMapRenderCellEnvelope Envelope(int2 min, int2 max)
    {
        return new OperationMapRenderCellEnvelope { Min = min, Max = max };
    }
}
