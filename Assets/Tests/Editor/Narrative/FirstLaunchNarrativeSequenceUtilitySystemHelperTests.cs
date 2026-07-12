using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Narrative.Runtime;
using NUnit.Framework;

public sealed class FirstLaunchNarrativeSequenceUtilitySystemHelperTests
{
    [Test]
    public void Configure_RejectsDuplicateUnknownAndDisconnectedStates()
    {
        FirstLaunchNarrativeSequenceUtilitySystemHelper runtime = new();
        Assert.IsFalse(runtime.Configure("A", new[]
        {
            State("A", next: "B"),
            State("A"),
        }));
        Assert.IsFalse(runtime.Configure("A", new[]
        {
            State("A", next: "missing"),
        }));
        Assert.IsFalse(runtime.Configure("A", new[]
        {
            State("A"),
            State("B"),
        }));
    }

    [Test]
    public void Timeline_EmitsAuthoredLinesAndTransitionsDeterministically()
    {
        FirstLaunchNarrativeSequenceUtilitySystemHelper runtime = CreateRuntime();
        List<FirstLaunchNarrativeSequenceOutput> outputs = new();
        runtime.Output += outputs.Add;

        Assert.IsTrue(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Start)));
        Assert.AreEqual("panel", runtime.CurrentStateId);
        Assert.AreEqual(-1, runtime.CurrentLineIndex);

        runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Tick,
            value: 0.49f,
            enabled: true));
        Assert.AreEqual(-1, runtime.CurrentLineIndex);
        runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Tick,
            value: 0.02f,
            enabled: true));
        Assert.AreEqual(0, runtime.CurrentLineIndex);

        runtime.Apply(CurrentIntent(runtime, FirstLaunchNarrativeSequenceIntentKind.DialogueAutoAdvance));
        Assert.AreEqual(0, runtime.CurrentLineIndex);
        runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Tick,
            value: 1.49f,
            enabled: true));
        Assert.AreEqual(1, runtime.CurrentLineIndex);
        runtime.Apply(CurrentIntent(runtime, FirstLaunchNarrativeSequenceIntentKind.DialogueAutoAdvance));
        runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Tick,
            value: 1f,
            enabled: true));
        Assert.AreEqual("route", runtime.CurrentStateId);
        Assert.IsFalse(runtime.IsRunning);
        CollectionAssert.Contains(
            outputs.ConvertAll(output => output.Kind),
            FirstLaunchNarrativeSequenceOutputKind.RouteReached);
    }

    [Test]
    public void ActionValidation_RejectsStaleTokensAndEmitsCurrentSkipOnce()
    {
        FirstLaunchNarrativeSequenceUtilitySystemHelper runtime = CreateRuntime();
        int skipCount = 0;
        runtime.Output += output =>
        {
            if (output.Kind == FirstLaunchNarrativeSequenceOutputKind.SkipRequested)
            {
                Assert.AreEqual("route", output.DestinationStateId);
                skipCount++;
            }
        };
        runtime.Apply(new FirstLaunchNarrativeSequenceIntent(FirstLaunchNarrativeSequenceIntentKind.Start));

        Assert.IsFalse(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Skip,
            runtime.CurrentStateId,
            runtime.TransitionToken + 1)));
        Assert.IsTrue(runtime.Apply(CurrentIntent(runtime, FirstLaunchNarrativeSequenceIntentKind.Skip)));
        Assert.AreEqual(1, skipCount);
    }

    [Test]
    public void NavigationPauseAndSeekRemainDeterministic()
    {
        FirstLaunchNarrativeSequenceUtilitySystemHelper runtime = CreateRuntime();
        runtime.Apply(new FirstLaunchNarrativeSequenceIntent(FirstLaunchNarrativeSequenceIntentKind.Start));
        Assert.IsTrue(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Pause)));
        Assert.IsFalse(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Tick,
            value: 100f,
            enabled: true)));
        Assert.AreEqual("panel", runtime.CurrentStateId);
        Assert.IsTrue(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Resume)));
        Assert.IsTrue(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.NextState)));
        Assert.AreEqual("route", runtime.CurrentStateId);
        Assert.IsTrue(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Seek,
            value: 0f)));
        Assert.AreEqual("panel", runtime.CurrentStateId);
    }

    private static FirstLaunchNarrativeSequenceUtilitySystemHelper CreateRuntime()
    {
        FirstLaunchNarrativeSequenceUtilitySystemHelper runtime = new();
        Assert.IsTrue(runtime.Configure("panel", new[]
        {
            State("panel", next: "route", skip: "route", duration: 3f, lineStarts: new[] { 0.5f, 2f }),
            State("route", NarrativeStateKind.RouteHandoff),
        }));
        return runtime;
    }

    private static FirstLaunchNarrativeSequenceIntent CurrentIntent(
        FirstLaunchNarrativeSequenceUtilitySystemHelper runtime,
        FirstLaunchNarrativeSequenceIntentKind kind)
    {
        return new FirstLaunchNarrativeSequenceIntent(kind, runtime.CurrentStateId, runtime.TransitionToken);
    }

    private static FirstLaunchNarrativeSequenceStateDefinition State(
        string id,
        NarrativeStateKind kind = NarrativeStateKind.PanelDialogue,
        string next = null,
        string skip = null,
        float duration = 1f,
        float[] lineStarts = null)
    {
        return new FirstLaunchNarrativeSequenceStateDefinition(
            id,
            kind,
            next,
            skip,
            duration,
            lineStarts ?? System.Array.Empty<float>());
    }
}
