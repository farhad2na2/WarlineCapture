using System;
using System.Collections.Generic;

namespace Game.Editor
{
    internal readonly struct OperationMapRenderCapacitySweepInput
    {
        internal OperationMapRenderCapacitySweepInput(
            string sampleIdentity,
            OperationMapRenderPolicyKey policy,
            int requiredPartRows)
        {
            SampleIdentity = sampleIdentity;
            Policy = policy;
            RequiredPartRows = requiredPartRows;
        }

        internal string SampleIdentity { get; }
        internal OperationMapRenderPolicyKey Policy { get; }
        internal int RequiredPartRows { get; }
    }

    internal readonly struct OperationMapRenderCapacitySweepResult
    {
        internal OperationMapRenderCapacitySweepResult(
            OperationMapRenderPolicyKey policy,
            int sweepSampleCount,
            int peakRequiredPartRows,
            int capacity,
            int headroomCount)
        {
            Policy = policy;
            SweepSampleCount = sweepSampleCount;
            PeakRequiredPartRows = peakRequiredPartRows;
            Capacity = capacity;
            HeadroomCount = headroomCount;
        }

        internal OperationMapRenderPolicyKey Policy { get; }
        internal int SweepSampleCount { get; }
        internal int PeakRequiredPartRows { get; }
        internal int Capacity { get; }
        internal int HeadroomCount { get; }
    }

    internal static class OperationMapRenderCapacitySweep
    {
        private const int CapacityPercent = 120;
        private const int PercentScale = 100;

        internal static bool TryCalculate(
            IReadOnlyList<OperationMapRenderCapacitySweepInput> inputs,
            out OperationMapRenderCapacitySweepResult[] results,
            out string error)
        {
            results = Array.Empty<OperationMapRenderCapacitySweepResult>();
            if (inputs == null || inputs.Count == 0)
            {
                error = "Capacity sweep requires at least one input.";
                return false;
            }

            Dictionary<OperationMapRenderPolicyKey, PolicyAccumulator> byPolicy = new();
            for (int index = 0; index < inputs.Count; index++)
            {
                OperationMapRenderCapacitySweepInput input = inputs[index];
                if (string.IsNullOrWhiteSpace(input.SampleIdentity))
                {
                    error = $"Capacity sweep input {index} has an empty sample identity.";
                    return false;
                }

                if (!OperationMapRenderPolicyClassifier.TryValidate(input.Policy, out error))
                {
                    error = $"Capacity sweep input {index} has invalid policy: {error}";
                    return false;
                }

                if (input.RequiredPartRows < 0)
                {
                    error =
                        $"Capacity sweep input {index} has negative required part rows: " +
                        $"{input.RequiredPartRows}.";
                    return false;
                }

                if (!byPolicy.TryGetValue(input.Policy, out PolicyAccumulator accumulator))
                {
                    accumulator = new PolicyAccumulator();
                    byPolicy.Add(input.Policy, accumulator);
                }

                if (!accumulator.SampleIdentities.Add(input.SampleIdentity))
                {
                    error =
                        $"Duplicate capacity sweep sample '{input.SampleIdentity}' for one policy.";
                    return false;
                }

                if (input.RequiredPartRows > accumulator.PeakRequiredPartRows)
                    accumulator.PeakRequiredPartRows = input.RequiredPartRows;
            }

            HashSet<string> canonicalSamples = null;
            foreach (KeyValuePair<OperationMapRenderPolicyKey, PolicyAccumulator> entry in byPolicy)
            {
                if (canonicalSamples == null)
                {
                    canonicalSamples = entry.Value.SampleIdentities;
                    continue;
                }

                if (!canonicalSamples.SetEquals(entry.Value.SampleIdentities))
                {
                    error =
                        "Every render policy must cover the identical canonical sweep sample set.";
                    return false;
                }
            }

            OperationMapRenderPolicyKey[] sortedPolicies =
                new OperationMapRenderPolicyKey[byPolicy.Count];
            byPolicy.Keys.CopyTo(sortedPolicies, 0);
            Array.Sort(sortedPolicies, ComparePolicies);

            results = new OperationMapRenderCapacitySweepResult[sortedPolicies.Length];
            for (int index = 0; index < sortedPolicies.Length; index++)
            {
                OperationMapRenderPolicyKey policy = sortedPolicies[index];
                PolicyAccumulator accumulator = byPolicy[policy];
                long scaled = (long)accumulator.PeakRequiredPartRows * CapacityPercent;
                long capacity = (scaled + PercentScale - 1L) / PercentScale;
                if (capacity > int.MaxValue)
                {
                    results = Array.Empty<OperationMapRenderCapacitySweepResult>();
                    error =
                        $"Capacity exceeds Int32 for policy {policy.Bucket}: {capacity}.";
                    return false;
                }

                int acceptedCapacity = (int)capacity;
                results[index] = new OperationMapRenderCapacitySweepResult(
                    policy,
                    accumulator.SampleIdentities.Count,
                    accumulator.PeakRequiredPartRows,
                    acceptedCapacity,
                    acceptedCapacity - accumulator.PeakRequiredPartRows);
            }

            error = null;
            return true;
        }

        private static int ComparePolicies(
            OperationMapRenderPolicyKey left,
            OperationMapRenderPolicyKey right)
        {
            int comparison = ((byte)left.Bucket).CompareTo((byte)right.Bucket);
            if (comparison != 0)
                return comparison;

            comparison = left.Layer.CompareTo(right.Layer);
            if (comparison != 0)
                return comparison;

            comparison = left.RenderingLayerMask.CompareTo(right.RenderingLayerMask);
            if (comparison != 0)
                return comparison;

            comparison =
                ((byte)left.MotionVectorMode).CompareTo((byte)right.MotionVectorMode);
            if (comparison != 0)
                return comparison;

            return ((byte)left.ShadowFlags).CompareTo((byte)right.ShadowFlags);
        }

        private sealed class PolicyAccumulator
        {
            internal readonly HashSet<string> SampleIdentities =
                new(StringComparer.Ordinal);
            internal int PeakRequiredPartRows;
        }
    }
}
