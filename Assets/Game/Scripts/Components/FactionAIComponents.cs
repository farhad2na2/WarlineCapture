using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct Faction : IComponentData
{
    public byte Id; // 0 = neutral, 1 = player, 2+ = hostile/AI factions.
}

public struct FactionEconomy : IComponentData
{
    public byte FactionId;
    public int Money;
    public float Oil;
    public float Fuel;
    public float OilIncomeRate;
    public float FuelIncomeRate;
    public float LastSellTime;
    public float LastLogTime;
}

public struct FactionEconomyPolicy : IComponentData
{
    public byte Enabled;
    public float IncomeMultiplier;
    public int OilSellPrice;
    public int FuelSellPrice;
    public float SellIntervalSeconds;
}

public struct AIBuildPlan : IComponentData
{
    public byte FactionId;
    public byte Enabled;
    public int NextBuildIndex;
    public int2 BaseCenterCell;
    public float BuildIntervalSeconds;
    public float LastBuildTime;
    public float LastLogTime;
}

public struct AIBuildPlanEntry : IBufferElementData
{
    public FixedString64Bytes BuildingId;
}

public struct AIProductionPlan : IComponentData
{
    public byte FactionId;
    public byte Enabled;
    public int NextUnitIndex;
    public int TargetProducedUnits;
    public int MaxQueuedUnits;
    public float UnitProductionIntervalSeconds;
    public float LastProductionTime;
    public float LastLogTime;
}

public struct AIProductionPlanEntry : IBufferElementData
{
    public FixedString64Bytes UnitId;
}

public enum AISquadPurpose : byte
{
    Attack = 0,
    Defend = 1,
    Scout = 2,
    Harass = 3
}

public enum AITargetKind : byte
{
    None = 0,
    Unit = 1,
    Building = 2,
    Threat = 3
}

public struct AITargetPrioritySetting : IComponentData
{
    public byte FactionId;
    public byte Priority;
}

public struct AISquadPlan : IComponentData
{
    public byte FactionId;
    public byte Enabled;
    public int MinUnits;
    public int MaxUnits;
    public int MaxActiveSquads;
    public int NextSquadId;
    public float LastLogTime;
}

public struct AISquad : IComponentData
{
    public int SquadId;
    public byte FactionId;
    public byte Purpose;
    public byte TargetFactionId;
    public byte TargetKind;
    public Entity TargetEntity;
    public int2 RallyCell;
    public int2 TargetCell;
    public int TargetScore;
    public int MinUnits;
    public int MaxUnits;
    public float LastOrderTime;
    public float LastLogTime;
}

public struct AISquadMember : IComponentData
{
    public Entity Squad;
    public int SquadId;
}

public struct AISquadUnit : IBufferElementData
{
    public Entity Unit;
}
