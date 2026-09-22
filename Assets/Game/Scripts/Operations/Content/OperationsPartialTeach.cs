using Game.Operations.Loop;

namespace Game.Operations.Content
{
    /// <summary>
    /// Teaches Partial: Conclude appears with plain stakes when the authoritative
    /// partial predicate is true; Withdraw remains a separate confirmation.
    /// Does not stamp Victory or mutate conclude/withdraw semantics.
    /// </summary>
    public readonly struct OperationsPartialTeachFrame
    {
        public OperationsPartialTeachFrame(
            bool concludeVisible,
            string concludeTitleKey,
            string concludeStakesKey,
            bool withdrawVisible,
            string withdrawTitleKey,
            string withdrawBodyKey,
            bool withdrawSeparateFromConclude)
        {
            ConcludeVisible = concludeVisible;
            ConcludeTitleKey = concludeTitleKey ?? string.Empty;
            ConcludeStakesKey = concludeStakesKey ?? string.Empty;
            WithdrawVisible = withdrawVisible;
            WithdrawTitleKey = withdrawTitleKey ?? string.Empty;
            WithdrawBodyKey = withdrawBodyKey ?? string.Empty;
            WithdrawSeparateFromConclude = withdrawSeparateFromConclude;
        }

        public bool ConcludeVisible { get; }
        public string ConcludeTitleKey { get; }
        public string ConcludeStakesKey { get; }
        public bool WithdrawVisible { get; }
        public string WithdrawTitleKey { get; }
        public string WithdrawBodyKey { get; }
        /// <summary>Always true — Withdraw must never share the Conclude control.</summary>
        public bool WithdrawSeparateFromConclude { get; }
    }

    public static class OperationsPartialTeach
    {
        public static bool TryRead(OperationsLoopSession loop, out OperationsPartialTeachFrame frame)
        {
            frame = default;
            if (loop == null || !loop.HasMission || loop.Phase != OperationsLoopPhase.Active || loop.MissionTerminal)
                return false;

            bool conclude = loop.ConcludeAvailable;
            bool withdraw = false;
            if (loop.TryReadHud(out OperationsHudFrame hud))
                withdraw = hud.WithdrawAvailable;

            if (!conclude && !withdraw)
                return false;

            string slug = MissionSlug(loop.MissionId);
            frame = new OperationsPartialTeachFrame(
                conclude,
                "operations.teach.partial.conclude.title",
                "operations." + slug + ".result.partial",
                withdraw,
                "operations.teach.partial.withdraw.title",
                "operations.aria.withdraw",
                true);
            return true;
        }

        static string MissionSlug(string missionId)
        {
            if (string.IsNullOrEmpty(missionId))
                return "o001";
            int dot = missionId.LastIndexOf('.');
            return dot >= 0 && dot + 1 < missionId.Length ? missionId.Substring(dot + 1) : missionId;
        }
    }
}
