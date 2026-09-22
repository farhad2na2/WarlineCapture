using System;
using System.Collections.Generic;
using Game.Operations.Contracts;
using Game.Operations.Loop;

namespace Game.Operations.Content
{
    /// <summary>
    /// Mission result UI contract: outcome + Trust / Intel / Heat district deltas + Continue,
    /// with one-tap Practice surfaced from fail/partial/withdraw results.
    /// Programmer 2 shell binds this frame; Content owns projection only.
    /// </summary>
    public readonly struct OperationsDistrictDeltaLine
    {
        public OperationsDistrictDeltaLine(
            OperationsDistrictMetricKind metric,
            string labelKey,
            int before,
            int after)
        {
            Metric = metric;
            LabelKey = labelKey ?? string.Empty;
            Before = before;
            After = after;
            Delta = after - before;
        }

        public OperationsDistrictMetricKind Metric { get; }
        public string LabelKey { get; }
        public int Before { get; }
        public int After { get; }
        public int Delta { get; }
    }

    public readonly struct OperationsMissionResultUiFrame
    {
        public OperationsMissionResultUiFrame(
            string missionId,
            OperationsOutcomeKind outcome,
            string outcomeKey,
            OperationsDistrictDeltaLine[] deltas,
            string continueKey,
            bool practiceAvailable,
            string practiceKey,
            bool zeroCredits)
        {
            MissionId = missionId ?? string.Empty;
            Outcome = outcome;
            OutcomeKey = outcomeKey ?? string.Empty;
            Deltas = deltas ?? Array.Empty<OperationsDistrictDeltaLine>();
            ContinueKey = continueKey ?? string.Empty;
            PracticeAvailable = practiceAvailable;
            PracticeKey = practiceKey ?? string.Empty;
            ZeroCredits = zeroCredits;
        }

        public string MissionId { get; }
        public OperationsOutcomeKind Outcome { get; }
        public string OutcomeKey { get; }
        public OperationsDistrictDeltaLine[] Deltas { get; }
        public string ContinueKey { get; }
        public bool PracticeAvailable { get; }
        public string PracticeKey { get; }
        public bool ZeroCredits { get; }
    }

    public static class OperationsMissionResultProjection
    {
        static readonly OperationsDistrictMetricKind[] FeaturedMetrics =
        {
            OperationsDistrictMetricKind.Trust,
            OperationsDistrictMetricKind.IntelConfidence,
            OperationsDistrictMetricKind.Heat
        };

        public static bool TryRead(OperationsLoopSession loop, out OperationsMissionResultUiFrame frame)
        {
            frame = default;
            if (loop == null || !loop.TryReadResult(out OperationsResultFrame result))
                return false;

            string missionId = loop.MissionId;
            string slug = MissionSlug(missionId);
            string outcomeKey = OutcomeKey(slug, result.Outcome);
            OperationsDistrictDeltaLine[] deltas = ProjectFeaturedDeltas(result.Before, result.After);
            bool failLike = result.Outcome == OperationsOutcomeKind.Defeat ||
                            result.Outcome == OperationsOutcomeKind.Partial ||
                            result.Outcome == OperationsOutcomeKind.Withdrawn;
            bool zeroCredits = result.ReceivedCredits == 0 ||
                               result.Outcome != OperationsOutcomeKind.Victory;

            frame = new OperationsMissionResultUiFrame(
                missionId,
                result.Outcome,
                outcomeKey,
                deltas,
                "operations.result.continue",
                failLike,
                "operations.result.practice",
                zeroCredits);
            return true;
        }

        /// <summary>
        /// Continues from MissionResult (settlement complete) then opens Practice for the
        /// just-finished mission in one call — one-tap Practice from fail/result.
        /// </summary>
        public static bool TryContinueAndPractice(
            OperationsLoopSession loop,
            string commandId,
            out string reason)
        {
            reason = string.Empty;
            if (loop == null)
            {
                reason = "null_loop";
                return false;
            }

            if (!TryRead(loop, out OperationsMissionResultUiFrame ui) || !ui.PracticeAvailable)
            {
                reason = "practice_unavailable";
                return false;
            }

            string missionId = ui.MissionId;
            if (string.IsNullOrEmpty(missionId))
            {
                reason = "missing_mission";
                return false;
            }

            if (loop.ReadShell().Top == OperationsShellNames.MissionResult)
            {
                OperationsLoopStep continued = loop.Back();
                if (!continued.Accepted)
                {
                    reason = continued.Reason ?? "continue_failed";
                    return false;
                }
            }

            if (!loop.TryOffer(missionId, out OperationsOfferSaveData offer))
            {
                reason = "offer_missing";
                return false;
            }

            int district = DistrictNumber(offer.districtId);
            OperationsLoopStep openDistrict = loop.OpenDistrict(district);
            if (!openDistrict.Accepted)
            {
                reason = openDistrict.Reason ?? "district";
                return false;
            }

            OperationsLoopStep openBriefing = loop.OpenBriefing(offer.offerId);
            if (!openBriefing.Accepted)
            {
                reason = openBriefing.Reason ?? "briefing";
                return false;
            }

            OperationsCommandResult practice = loop.BeginPractice(commandId);
            if (!practice.Accepted)
            {
                reason = practice.ReasonCode.ToString();
                return false;
            }

            return true;
        }

        public static OperationsDistrictDeltaLine[] ProjectFeaturedDeltas(int[] before, int[] after)
        {
            before ??= Array.Empty<int>();
            after ??= Array.Empty<int>();
            var lines = new List<OperationsDistrictDeltaLine>(FeaturedMetrics.Length);
            for (int index = 0; index < FeaturedMetrics.Length; index++)
            {
                OperationsDistrictMetricKind metric = FeaturedMetrics[index];
                int metricIndex = (int)metric;
                int b = metricIndex < before.Length ? before[metricIndex] : 0;
                int a = metricIndex < after.Length ? after[metricIndex] : b;
                lines.Add(new OperationsDistrictDeltaLine(metric, LabelKey(metric), b, a));
            }

            return lines.ToArray();
        }

        static string LabelKey(OperationsDistrictMetricKind metric)
        {
            switch (metric)
            {
                case OperationsDistrictMetricKind.Trust:
                    return "operations.result.delta.trust";
                case OperationsDistrictMetricKind.IntelConfidence:
                    return "operations.result.delta.intel";
                case OperationsDistrictMetricKind.Heat:
                    return "operations.result.delta.heat";
                default:
                    return "operations.result.delta.generic";
            }
        }

        static string OutcomeKey(string slug, OperationsOutcomeKind outcome)
        {
            switch (outcome)
            {
                case OperationsOutcomeKind.Victory:
                    return "operations." + slug + ".result.victory";
                case OperationsOutcomeKind.Partial:
                    return "operations." + slug + ".result.partial";
                case OperationsOutcomeKind.Defeat:
                    return "operations." + slug + ".result.defeat";
                case OperationsOutcomeKind.Withdrawn:
                    return "operations." + slug + ".result.withdrawn";
                default:
                    return "operations." + slug + ".result.defeat";
            }
        }

        static string MissionSlug(string missionId)
        {
            if (string.IsNullOrEmpty(missionId))
                return "o001";
            int dot = missionId.LastIndexOf('.');
            return dot >= 0 && dot + 1 < missionId.Length ? missionId.Substring(dot + 1) : missionId;
        }

        static int DistrictNumber(string districtId)
        {
            for (int number = 1; number <= OperationsIdentityRules.DistrictCount; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == districtId)
                    return number;
            }

            throw new InvalidOperationException(districtId);
        }
    }
}
