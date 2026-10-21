namespace Game.Skirmish.Contracts
{
    /// <summary>
    /// Read-only medal book for UI. Composition owns the concrete ledger and reloads it on <see cref="Refresh"/>.
    /// </summary>
    public interface ISkirmishMedalReader
    {
        bool Has(string catalogId, SkirmishDifficultyId difficulty);

        int CountFor(string mapId);

        int CountFor(string mapId, string objectiveId);

        /// <summary>Rows of this tier in the medal ledger. Not the selected district.</summary>
        int CountTier(SkirmishDifficultyId difficulty);

        void Refresh();
    }
}
