internal sealed class CitizenPopulationReadModelSystem
{
    private CitizenPopulationTotals _totals;

    public CitizenPopulationTotals Totals => _totals;

    public void Reset()
    {
        _totals = default;
    }

    public void Refresh(
        CitizenPopulationTotalsSystem totalsSystem,
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection,
        bool syncSummaryEntity)
    {
        _totals = CitizenPopulationTotalsSystem.Calculate(totalsSystem, state, ecsProjection);
        if (syncSummaryEntity)
            ecsProjection.TryPublishSummary(_totals);
    }

    public void GetTotals(
        CitizenPopulationEcsProjectionSystem ecsProjection,
        out int households,
        out int totalCitizens,
        out int housedCitizens,
        out int refugeeCitizens,
        out int deadCitizens)
    {
        if (ecsProjection.TryGetTotalsFromEcs(out CitizenPopulationSummary summary))
        {
            households = summary.Households;
            totalCitizens = summary.TotalCitizens;
            housedCitizens = summary.HousedCitizens;
            refugeeCitizens = summary.RefugeeCitizens;
            deadCitizens = summary.DeadCitizens;
            return;
        }

        households = _totals.Households;
        totalCitizens = _totals.TotalCitizens;
        housedCitizens = _totals.HousedCitizens;
        refugeeCitizens = _totals.RefugeeCitizens;
        deadCitizens = _totals.DeadCitizens;
    }
}
