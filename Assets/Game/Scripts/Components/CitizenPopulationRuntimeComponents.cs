using Unity.Entities;
using Unity.Mathematics;

public enum CitizenGender : byte
{
    Male = 0,
    Female = 1
}

public enum CitizenLifeState : byte
{
    Alive = 0,
    Dead = 1
}

public enum CitizenStatus : byte
{
    AtHome = 0,
    GoingToWork = 1,
    AtWork = 2,
    GoingToShop = 3,
    AtShop = 4,
    GoingToCityHall = 5,
    GoingForWalk = 6,
    ReturningHome = 7,
    Fleeing = 8,
    RefugeeSeekingShelter = 9,
    AtRefugeeTent = 10,
    RelocatingToNewHouse = 11,
    LeavingWorld = 12,
    Dead = 13
}

public readonly struct CitizenPopulationTotals
{
    public readonly int Households;
    public readonly int TotalCitizens;
    public readonly int HousedCitizens;
    public readonly int RefugeeCitizens;
    public readonly int DeadCitizens;

    public CitizenPopulationTotals(int households, int totalCitizens, int housedCitizens, int refugeeCitizens, int deadCitizens)
    {
        Households = households;
        TotalCitizens = totalCitizens;
        HousedCitizens = housedCitizens;
        RefugeeCitizens = refugeeCitizens;
        DeadCitizens = deadCitizens;
    }
}

internal struct CitizenRecordComponent
{
    public int CitizenId;
    public Entity CitizenEntity;
    public int HouseholdId;
    public int HomeBuildingId;
    public int WorkBuildingId;
    public int PreferredShopBuildingId;
    public int LunchShopBuildingId;
    public int PreferredWalkBuildingId;
    public int PreferredCityHallBuildingId;
    public int CurrentTargetBuildingId;
    public CitizenGender Gender;
    public CitizenLifeState LifeState;
    public CitizenStatus Status;
    public float StateStartedAt;
    public float StateEndsAt;
}

internal struct CitizenHouseholdRecordComponent
{
    public int HouseholdId;
    public Entity HouseholdEntity;
    public int HomeBuildingId;
    public int MaleCitizenId;
    public int FemaleCitizenId;
    public int RefugeeTentBuildingId;
    public byte IsDisplaced;
}

internal sealed class VisibleCitizenComponent
{
    public int CitizenId;
    public Entity UnitEntity;
    public int2 GoalCell;
    public int TargetBuildingId;
}
