using System.Globalization;

namespace Game.Configs
{
    /// <summary>
    /// Long-session runtime regression (program gate from 2026-11-17, once per runtime candidate): S026 Regular/Standard
    /// seed 104729 from the real menu, played through normal touch controls (periodic infantry purchases, no attack
    /// orders, ARIA off) until the deadline, then the result screen and MAIN MENU return. The harness emits exactly one
    /// <c>[SkirmishLongSessionRegression]</c> line; this type owns its verdict and text.
    /// </summary>
    public static class SkirmishLongSessionRegressionLog
    {
        public const string Marker = "[SkirmishLongSessionRegression]";
        public const string EnvironmentVariable = "WARLINE_LONG_SESSION";
        /// <summary>Owner S026 allocator failure appeared at 9:36; a shorter match does not exercise it.</summary>
        public const float MinimumMatchSeconds = 600f;
        public const double WallClockBudgetSeconds = 1800d;

        public struct Evidence
        {
            public string Catalog;
            public int Seed;
            public int AllocatorErrors, Exceptions, Errors;
            public bool ResultScreen, ReturnToMenu, Expiry;
            public string Outcome, EndReason, Abort, FirstError;
            public float Elapsed;
            public int Purchases, PurchaseFailures, PurchaseRefusedByCap;
            public int Archetypes, QueryShapes, QueryBlocks, QueryBudgetBlocks;
        }

        /// <summary>Passed: no allocator error or exception, deadline expiry reached, result shown, menu reached.
        /// Inconclusive: clean, but the match ended before the deadline (e.g. base lost) after at least ten minutes.
        /// Failed: anything else.</summary>
        public static string Verdict(in Evidence e)
        {
            if (e.AllocatorErrors > 0 || e.Exceptions > 0 || !e.ResultScreen || !e.ReturnToMenu) return "Failed";
            if (e.Expiry) return "Passed";
            return e.Elapsed >= MinimumMatchSeconds ? "Inconclusive" : "Failed";
        }

        /// <summary>Deadline end reasons (TimeLimit, TicketsAtDeadline, ObjectiveDeadline).</summary>
        public static bool IsExpiry(string endReason) =>
            endReason == "TimeLimit" || endReason == "TicketsAtDeadline" || endReason == "ObjectiveDeadline";

        public static string Format(in Evidence e)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0} catalog={1} seed={2} result={3} allocatorErrors={4} exceptions={5} errors={6} resultScreen={7} " +
                "returnToMenu={8} expiry={9} outcome={10} endReason={11} elapsed={12:F1} purchases={13} purchaseFailures={14} " +
                "archetypes={15} queryShapes={16} queryBlocks={17}/{18} abort={19} firstError={20} purchaseRefusedByCap={21}",
                Marker, e.Catalog ?? "none", e.Seed, Verdict(e), e.AllocatorErrors, e.Exceptions, e.Errors,
                e.ResultScreen ? 1 : 0, e.ReturnToMenu ? 1 : 0, e.Expiry ? 1 : 0, e.Outcome ?? "none", e.EndReason ?? "none",
                e.Elapsed, e.Purchases, e.PurchaseFailures, e.Archetypes, e.QueryShapes, e.QueryBlocks, e.QueryBudgetBlocks,
                string.IsNullOrEmpty(e.Abort) ? "none" : e.Abort.Replace(' ', '_'), Sanitize(e.FirstError), e.PurchaseRefusedByCap);
        }

        /// <summary>One token: the first 160 characters of the first runtime error, spaces as underscores.</summary>
        public static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "none";
            string line = text.Split('\n')[0].Trim();
            if (line.Length > 160) line = line.Substring(0, 160);
            return line.Replace(' ', '_').Replace('\t', '_');
        }

        /// <summary>True when a Unity log message is the Entities query block-allocator overflow.</summary>
        public static bool IsAllocatorError(string message) =>
            message != null && message.Contains("BlockAllocator") &&
            (message.Contains("Cannot exceed budget") || message.Contains("Cannot allocate"));
    }
}
