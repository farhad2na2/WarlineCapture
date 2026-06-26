internal sealed class CitizenPopulationTotalsCompositionSystemHelper
{
    public static bool HasCitizenData(CitizenPopulationTotalsCompositionSystemHelper system, CitizenPopulationStateCompositionSystemHelper state)
    {
        return system != null
            ? system.HasCitizenData(state)
            : HasCitizenDataState(state);
    }

    public bool HasCitizenData(CitizenPopulationStateCompositionSystemHelper state)
    {
        return HasCitizenDataState(state);
    }

    public static bool HasHouseholdData(
        CitizenPopulationTotalsCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection)
    {
        return system != null
            ? system.HasHouseholdData(state, ecsProjection)
            : HasHouseholdDataState(state, ecsProjection);
    }

    public bool HasHouseholdData(CitizenPopulationStateCompositionSystemHelper state, CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection)
    {
        return HasHouseholdDataState(state, ecsProjection);
    }

    public static CitizenPopulationTotals Calculate(
        CitizenPopulationTotalsCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection)
    {
        return system != null
            ? system.Calculate(state, ecsProjection)
            : CalculateState(state, ecsProjection);
    }

    public CitizenPopulationTotals Calculate(CitizenPopulationStateCompositionSystemHelper state, CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection)
    {
        return CalculateState(state, ecsProjection);
    }

    private static bool HasCitizenDataState(CitizenPopulationStateCompositionSystemHelper state)
    {
        return state.CitizenCount > 0;
    }

    private static bool HasHouseholdDataState(CitizenPopulationStateCompositionSystemHelper state, CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection)
    {
        if (ecsProjection.HasWorld)
            return ecsProjection.HasHouseholdEntities();

        return state.HouseholdCount > 0;
    }

    private static CitizenPopulationTotals CalculateState(CitizenPopulationStateCompositionSystemHelper state, CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection)
    {
        int aliveCitizens = 0;
        int deadCitizens = 0;
        int housedCitizens = 0;
        int refugeeCitizens = 0;
        foreach (CitizenRecordComponent citizen in state.CitizensById.Values)
        {
            if (citizen.LifeState == CitizenLifeState.Dead)
            {
                deadCitizens++;
                continue;
            }

            aliveCitizens++;
            if (citizen.Status == CitizenStatus.RefugeeSeekingShelter || citizen.Status == CitizenStatus.AtRefugeeTent)
                refugeeCitizens++;
            else
                housedCitizens++;
        }

        return new CitizenPopulationTotals(
            ecsProjection.GetHouseholdEntityCount(state.HouseholdCount),
            aliveCitizens,
            housedCitizens,
            refugeeCitizens,
            deadCitizens);
    }
}
