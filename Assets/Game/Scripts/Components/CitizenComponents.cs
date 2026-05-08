using Unity.Entities;

public struct CitizenTag : IComponentData
{
}

public struct CitizenHouseholdTag : IComponentData
{
}

public struct CitizenIdentity : IComponentData
{
    public int CitizenId;
    public byte Gender;
    public byte LifeState;
    public byte Status;
}

public struct CitizenHouseholdRef : IComponentData
{
    public int HouseholdId;
    public Entity HouseholdEntity;
}

public struct CitizenHomeTarget : IComponentData
{
    public int HomeBuildingId;
    public int CurrentTargetBuildingId;
}

public struct CitizenAssignmentsData : IComponentData
{
    public int WorkBuildingId;
    public int PreferredShopBuildingId;
    public int LunchShopBuildingId;
    public int PreferredWalkBuildingId;
    public int PreferredCityHallBuildingId;
}

public struct CitizenTimersData : IComponentData
{
    public float StateStartedAt;
    public float StateEndsAt;
}

public struct CitizenHouseholdData : IComponentData
{
    public int HouseholdId;
    public int HomeBuildingId;
    public int MaleCitizenId;
    public int FemaleCitizenId;
    public int RefugeeTentBuildingId;
    public byte IsDisplaced;
}

public struct CitizenPopulationSummary : IComponentData
{
    public int Households;
    public int TotalCitizens;
    public int HousedCitizens;
    public int RefugeeCitizens;
    public int DeadCitizens;
}

public struct CitizenPopulationSummaryTag : IComponentData
{
}

public struct CivilianUnitTag : IComponentData
{
}
