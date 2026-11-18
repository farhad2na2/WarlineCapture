namespace Game.UI.Runtime
{
    /// <summary>State of one Skirmish result criterion, judged by its own condition.</summary>
    public enum SkirmishCriterionState : byte
    {
        /// <summary>The criterion's own condition was not met.</summary>
        Failed,
        /// <summary>The condition was met and its star was awarded.</summary>
        Earned,
        /// <summary>The condition was met, but stars are only awarded with a victory.</summary>
        MetNeedsVictory
    }

    /// <summary>
    /// The three Base Assault star criteria. Each one reports its own condition so the
    /// result panel never marks, for example, "Keep squad losses low" as failed when no
    /// squad was lost. The star count is unchanged: one for the victory, one for losing
    /// no building and one for losing <see cref="LowLossLimit"/> units or fewer.
    /// </summary>
    public readonly struct SkirmishStarCriteria
    {
        public const int LowLossLimit = 2;

        public readonly bool Victory;
        public readonly int BuildingsLost;
        public readonly int UnitsLost;

        public SkirmishStarCriteria(bool victory, int buildingsLost, int unitsLost)
        {
            Victory = victory;
            BuildingsLost = buildingsLost < 0 ? 0 : buildingsLost;
            UnitsLost = unitsLost < 0 ? 0 : unitsLost;
        }

        /// <summary>Criterion 1: destroy the enemy main base (the victory itself).</summary>
        public SkirmishCriterionState DestroyEnemyBase => Victory ? SkirmishCriterionState.Earned : SkirmishCriterionState.Failed;

        /// <summary>Criterion 2: protect your base. The star needs zero buildings lost (the main base counts).</summary>
        public SkirmishCriterionState ProtectBase => Judge(BuildingsLost == 0);

        /// <summary>Criterion 3: keep squad losses at or below the limit.</summary>
        public SkirmishCriterionState LowLosses => Judge(UnitsLost <= LowLossLimit);

        public byte Stars
        {
            get
            {
                if (!Victory)
                    return 0;
                int earned = 1;
                if (BuildingsLost == 0)
                    earned++;
                if (UnitsLost <= LowLossLimit)
                    earned++;
                return (byte)earned;
            }
        }

        /// <summary>Bit i set when criterion i+1 met its own condition, regardless of the result.</summary>
        public byte MetMask =>
            (byte)((DestroyEnemyBase != SkirmishCriterionState.Failed ? 1 : 0) |
                   (ProtectBase != SkirmishCriterionState.Failed ? 2 : 0) |
                   (LowLosses != SkirmishCriterionState.Failed ? 4 : 0));

        /// <summary>Bit i set when criterion i+1 earned its star.</summary>
        public byte EarnedMask =>
            (byte)((DestroyEnemyBase == SkirmishCriterionState.Earned ? 1 : 0) |
                   (ProtectBase == SkirmishCriterionState.Earned ? 2 : 0) |
                   (LowLosses == SkirmishCriterionState.Earned ? 4 : 0));

        private SkirmishCriterionState Judge(bool met) =>
            !met ? SkirmishCriterionState.Failed
            : Victory ? SkirmishCriterionState.Earned
            : SkirmishCriterionState.MetNeedsVictory;
    }
}
