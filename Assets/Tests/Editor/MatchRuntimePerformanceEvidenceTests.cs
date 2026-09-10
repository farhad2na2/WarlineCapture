using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Editor;
using NUnit.Framework;

public sealed class MatchRuntimePerformanceEvidenceTests
{
    [TestCase(-1L, 10d, false)]
    [TestCase(0L, 10d, true)]
    [TestCase(1L, 10d, false)]
    [TestCase(0L, 21d, false)]
    public void Acceptance_RequiresMeasuredAllocationAndUnchangedFrameBudget(long allocatedBytes, double p95, bool expected)
    {
        Type owner = typeof(MatchRuntimeShellSmokeValidation);
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var frames = (List<double>)owner.GetField("BaselineFrameTimesMs", flags).GetValue(null);
        double[] savedFrames = frames.ToArray();
        try
        {
            frames.Clear();
            for (int i = 0; i < 180; i++) frames.Add(10d);
            object budget = Activator.CreateInstance(owner.GetNestedType("PerformanceRegressionAcceptedBaseline", BindingFlags.NonPublic));
            Set(budget, "editorP95FrameBudgetMs", 20d);
            Set(budget, "currentThreadAllocatedBytesBudget", 0L);
            Set(budget, "minimumFrameCount", 180);
            Set(budget, "minimumUnitCount", 700);
            Set(budget, "minimumRuntimeBuildingCount", 600);
            Set(budget, "minimumVisibleModelEstimate", 40);
            object counts = Activator.CreateInstance(owner.GetNestedType("BaselineEntityCounts", BindingFlags.NonPublic));
            Set(counts, "UnitCount", 700);
            Set(counts, "RuntimeBuildingCount", 600);
            Set(counts, "VisibleModelEstimate", 40);
            object[] arguments = { budget, allocatedBytes, p95, counts, null };
            bool actual = (bool)owner.GetMethod("TryValidatePerformanceRegressionAcceptedBaseline", flags).Invoke(null, arguments);
            Assert.That(actual, Is.EqualTo(expected), arguments[4] as string);
            if (allocatedBytes < 0) StringAssert.Contains("unavailable, not zero", arguments[4] as string);
        }
        finally
        {
            frames.Clear();
            frames.AddRange(savedFrames);
        }
    }

    private static void Set(object value, string field, object data) => value.GetType().GetField(field).SetValue(value, data);
}
