using System;
using System.Collections.Generic;

public static class ArchitectureLifecycleMemoryTrendUtilitySystemHelper
{
    public static double CalculateMedian(IReadOnlyList<long> samples)
    {
        ValidateSamples(samples, minimumCount: 1);

        var ordered = new double[samples.Count];
        for (int index = 0; index < samples.Count; index++)
            ordered[index] = samples[index];

        Array.Sort(ordered);
        return CalculateMedianOfOrderedValues(ordered);
    }

    public static double CalculateTheilSenSlopePerCycle(IReadOnlyList<long> samples)
    {
        ValidateSamples(samples, minimumCount: 2);

        int slopeCount = checked(samples.Count * (samples.Count - 1) / 2);
        var slopes = new double[slopeCount];
        int slopeIndex = 0;
        for (int firstIndex = 0; firstIndex < samples.Count - 1; firstIndex++)
        {
            for (int secondIndex = firstIndex + 1; secondIndex < samples.Count; secondIndex++)
            {
                double delta = (double)samples[secondIndex] - samples[firstIndex];
                slopes[slopeIndex++] = delta / (secondIndex - firstIndex);
            }
        }

        Array.Sort(slopes);
        return CalculateMedianOfOrderedValues(slopes);
    }

    private static double CalculateMedianOfOrderedValues(double[] orderedValues)
    {
        int upperMiddleIndex = orderedValues.Length / 2;
        if ((orderedValues.Length & 1) != 0)
            return orderedValues[upperMiddleIndex];

        double lowerMiddle = orderedValues[upperMiddleIndex - 1];
        double upperMiddle = orderedValues[upperMiddleIndex];
        return lowerMiddle + ((upperMiddle - lowerMiddle) / 2d);
    }

    private static void ValidateSamples(IReadOnlyList<long> samples, int minimumCount)
    {
        if (samples == null)
            throw new ArgumentNullException(nameof(samples));
        if (samples.Count < minimumCount)
        {
            throw new ArgumentException(
                $"At least {minimumCount} memory samples are required.",
                nameof(samples));
        }
    }
}
