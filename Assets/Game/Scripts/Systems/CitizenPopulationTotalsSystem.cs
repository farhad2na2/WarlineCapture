internal sealed class CitizenPopulationTotalsSystem
{
    public static bool HasCitizenData(CitizenPopulationTotalsSystem system, CitizenPopulationStateSystem state)
    {
        return system != null
            ? system.HasCitizenData(state)
            : HasCitizenDataState(state);
    }

    public bool HasCitizenData(CitizenPopulationStateSystem state)
    {
        return HasCitizenDataState(state);
    }

    public static bool HasHouseholdData(
        CitizenPopulationTotalsSystem system,
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection)
    {
        return system != null
            ? system.HasHouseholdData(state, ecsProjection)
            : HasHouseholdDataState(state, ecsProjection);
    }

    public bool HasHouseholdData(CitizenPopulationStateSystem state, CitizenPopulationEcsProjectionSystem ecsProjection)
    {
        return HasHouseholdDataState(state, ecsProjection);
    }

    public static CitizenPopulationTotals Calculate(
        CitizenPopulationTotalsSystem system,
        CitizenPopulationStateSystem state,
        CitizenPopulationEcsProjectionSystem ecsProjection)
    {
        return system != null
            ? system.Calculate(state, ecsProjection)
            : CalculateState(state, ecsProjection);
    }

    public CitizenPopulationTotals Calculate(CitizenPopulationStateSystem state, CitizenPopulationEcsProjectionSystem ecsProjection)
    {
        return CalculateState(state, ecsProjection);
    }

    private static bool HasCitizenDataState(CitizenPopulationStateSystem state)
    {
        return state.CitizenCount > 0;
    }

    private static bool HasHouseholdDataState(CitizenPopulationStateSystem state, CitizenPopulationEcsProjectionSystem ecsProjection)
    {
        if (ecsProjection.HasWorld)
            return ecsProjection.HasHouseholdEntities();

        return state.HouseholdCount > 0;
    }

    private static CitizenPopulationTotals CalculateState(CitizenPopulationStateSystem state, CitizenPopulationEcsProjectionSystem ecsProjection)
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
