using Game.Components;

namespace Game.Runtime
{
    internal sealed class CitizenPopulationReadModelCompositionSystemHelper
    {
        public struct State
        {
            public CitizenPopulationTotals Totals;
        }

        private CitizenPopulationTotals _totals;

        public CitizenPopulationTotals Totals => _totals;

        public static void Reset(CitizenPopulationReadModelCompositionSystemHelper system, ref State state)
        {
            if (system != null)
            {
                system.Reset();
                return;
            }

            state.Totals = default;
        }

        public void Reset()
        {
            _totals = default;
        }

        public static void Refresh(
            CitizenPopulationReadModelCompositionSystemHelper system,
            ref State state,
            CitizenPopulationTotalsCompositionSystemHelper totalsSystem,
            CitizenPopulationStateCompositionSystemHelper populationState,
            CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
            bool syncSummaryEntity)
        {
            if (system != null)
            {
                system.Refresh(totalsSystem, populationState, ecsProjection, syncSummaryEntity);
                return;
            }

            state.Totals = CitizenPopulationTotalsCompositionSystemHelper.Calculate(totalsSystem, populationState, ecsProjection);
            if (syncSummaryEntity)
                ecsProjection.TryPublishSummary(state.Totals);
        }

        public void Refresh(
            CitizenPopulationTotalsCompositionSystemHelper totalsSystem,
            CitizenPopulationStateCompositionSystemHelper state,
            CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
            bool syncSummaryEntity)
        {
            _totals = CitizenPopulationTotalsCompositionSystemHelper.Calculate(totalsSystem, state, ecsProjection);
            if (syncSummaryEntity)
                ecsProjection.TryPublishSummary(_totals);
        }

        public static void GetTotals(
            CitizenPopulationReadModelCompositionSystemHelper system,
            ref State state,
            CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
            out int households,
            out int totalCitizens,
            out int housedCitizens,
            out int refugeeCitizens,
            out int deadCitizens)
        {
            if (system != null)
            {
                system.GetTotals(
                    ecsProjection,
                    out households,
                    out totalCitizens,
                    out housedCitizens,
                    out refugeeCitizens,
                    out deadCitizens);
                return;
            }

            GetTotalsState(
                state.Totals,
                ecsProjection,
                out households,
                out totalCitizens,
                out housedCitizens,
                out refugeeCitizens,
                out deadCitizens);
        }

        public void GetTotals(
            CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
            out int households,
            out int totalCitizens,
            out int housedCitizens,
            out int refugeeCitizens,
            out int deadCitizens)
        {
            GetTotalsState(
                _totals,
                ecsProjection,
                out households,
                out totalCitizens,
                out housedCitizens,
                out refugeeCitizens,
                out deadCitizens);
        }

        private static void GetTotalsState(
            CitizenPopulationTotals totals,
            CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
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

            households = totals.Households;
            totalCitizens = totals.TotalCitizens;
            housedCitizens = totals.HousedCitizens;
            refugeeCitizens = totals.RefugeeCitizens;
            deadCitizens = totals.DeadCitizens;
        }
    }
}
