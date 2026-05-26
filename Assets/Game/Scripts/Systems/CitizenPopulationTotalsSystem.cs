internal sealed class CitizenPopulationTotalsSystem
{
    public bool HasCitizenData(CitizenPopulationStateSystem state)
    {
        return state.CitizenCount > 0;
    }

    public bool HasHouseholdData(CitizenPopulationStateSystem state, CitizenPopulationEcsProjectionSystem ecsProjection)
    {
        if (ecsProjection.HasWorld)
            return ecsProjection.HasHouseholdEntities();

        return state.HouseholdCount > 0;
    }

    public CitizenPopulationTotals Calculate(CitizenPopulationStateSystem state, CitizenPopulationEcsProjectionSystem ecsProjection)
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
