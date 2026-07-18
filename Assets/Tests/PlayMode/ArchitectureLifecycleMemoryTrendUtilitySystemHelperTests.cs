using System;
using NUnit.Framework;

public sealed class ArchitectureLifecycleMemoryTrendUtilitySystemHelperTests
{
    [Test]
    public void CalculateMedian_OddSampleCount_ReturnsMiddleValueWithoutChangingInput()
    {
        long[] samples = { 9L, 1L, 5L };

        double median = ArchitectureLifecycleMemoryTrendUtilitySystemHelper.CalculateMedian(samples);

        Assert.That(median, Is.EqualTo(5d));
        CollectionAssert.AreEqual(new[] { 9L, 1L, 5L }, samples);
    }

    [Test]
    public void CalculateMedian_EvenSampleCount_ReturnsMeanOfMiddleValues()
    {
        long[] samples = { 40L, 10L, 30L, 20L };

        double median = ArchitectureLifecycleMemoryTrendUtilitySystemHelper.CalculateMedian(samples);

        Assert.That(median, Is.EqualTo(25d));
    }

    [Test]
    public void CalculateMedian_ExtremeLongValues_DoesNotOverflow()
    {
        long[] samples = { long.MinValue, long.MaxValue };

        double median = ArchitectureLifecycleMemoryTrendUtilitySystemHelper.CalculateMedian(samples);

        Assert.That(median, Is.EqualTo(-0.5d).Within(0.5d));
    }

    [Test]
    public void CalculateTheilSenSlopePerCycle_LinearSamples_ReturnsExactSlope()
    {
        long[] samples = { 100L, 164L, 228L, 292L, 356L };

        double slope = ArchitectureLifecycleMemoryTrendUtilitySystemHelper
            .CalculateTheilSenSlopePerCycle(samples);

        Assert.That(slope, Is.EqualTo(64d));
    }

    [Test]
    public void CalculateTheilSenSlopePerCycle_IsRobustToSingleOutlier()
    {
        long[] samples = { 100L, 110L, 120L, 10_000L, 140L, 150L, 160L };

        double slope = ArchitectureLifecycleMemoryTrendUtilitySystemHelper
            .CalculateTheilSenSlopePerCycle(samples);

        Assert.That(slope, Is.EqualTo(10d));
    }

    [Test]
    public void CalculateTheilSenSlopePerCycle_DecliningSamples_ReturnsNegativeSlope()
    {
        long[] samples = { 500L, 450L, 400L, 350L };

        double slope = ArchitectureLifecycleMemoryTrendUtilitySystemHelper
            .CalculateTheilSenSlopePerCycle(samples);

        Assert.That(slope, Is.EqualTo(-50d));
    }

    [Test]
    public void CalculateMethods_RejectMissingOrInsufficientSamples()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ArchitectureLifecycleMemoryTrendUtilitySystemHelper.CalculateMedian(null));
        Assert.Throws<ArgumentException>(() =>
            ArchitectureLifecycleMemoryTrendUtilitySystemHelper.CalculateMedian(Array.Empty<long>()));
        Assert.Throws<ArgumentException>(() =>
            ArchitectureLifecycleMemoryTrendUtilitySystemHelper.CalculateTheilSenSlopePerCycle(new[] { 1L }));
    }
}
