// Ground-only production statement extraction. See extraction.json; no ECS scheduling implied.
using System;
using Unity.Mathematics;
using Game.Components;
namespace FuelPrototype {
public sealed class ValueRef<T> where T:struct { public T Value; public ref T ValueRW=>ref Value; public ref readonly T ValueRO=>ref Value; public ValueRef(T v){Value=v;} }
public sealed class UnitRow { public string Id; public byte CapturedValueMask; public bool QueryEligible, IsAircraft; public UnitGrid Grid; public Faction Faction; public UnitMovementBehavior Movement; public UnitFuelConsumption Consumption; public UnitFuelConsumptionState State; }
public sealed class NativeArray<T> { readonly T[] values; public NativeArray(int n){values=new T[n];} public int Length=>values.Length; public T this[int i]{get=>values[i];set=>values[i]=value;} public T[] Copy()=> (T[])values.Clone(); }
// Only storage-ref enumeration is shimmed. Write-through must occur before an early break.
public sealed class StorageQuery { public BuildingResourceStorageComponent[] Values; public System.Collections.Generic.IEnumerable<StorageRef> Enumerate(){for(int i=0;i<Values.Length;i++)yield return new StorageRef(Values,i);} }
public readonly struct StorageRef { readonly BuildingResourceStorageComponent[] rows;readonly int index;public StorageRef(BuildingResourceStorageComponent[] r,int i){rows=r;index=i;} public ref BuildingResourceStorageComponent ValueRW=>ref rows[index]; }
public struct SystemState {}
public readonly struct Attempt { public readonly int SessionId; }
public readonly struct AttemptLookup { public bool HasComponent(int x)=>false; public Attempt this[int x]=>throw new InvalidOperationException("legacy expanded routing unsupported"); }
public readonly struct Session { public readonly int SessionId; }
public sealed class ProductionGroundFuelKernel {
 readonly StorageQuery query; public ProductionGroundFuelKernel(BuildingResourceStorageComponent[] storage){query=new StorageQuery{Values=storage};}
 public NativeArray<float> Accumulate(UnitRow[] rows){
 var requestedFuelByFaction=new NativeArray<float>(256);var requestedExpandedFuelByFaction=new NativeArray<float>(256);
 bool hasExpanded=false;var attemptLookup=new AttemptLookup();var session=new Session();int entity=0;
 foreach(var row in rows){
 var unitGrid=new ValueRef<UnitGrid>(row.Grid);var faction=new ValueRef<Faction>(row.Faction);var movement=new ValueRef<UnitMovementBehavior>(row.Movement);var consumption=new ValueRef<UnitFuelConsumption>(row.Consumption);
 if(consumption.ValueRO.Enabled==0)continue;
 // Unsupported modes rejected by adapter. Production ground cell expression retained.
 ref UnitFuelConsumptionState stateRw=ref row.State;bool isAircraft=false;int2 cell=unitGrid.ValueRO.Cell;
                if (stateRw.Initialized == 0)
                {
                    stateRw.LastCell = cell;
                    stateRw.Initialized = 1;
                    continue;
                }

                int2 delta = cell - stateRw.LastCell;
                int movedCells = math.abs(delta.x) + math.abs(delta.y);
                if (movedCells <= 0)
                    continue;

                bool isFuelUsingUnit = movement.ValueRO.UsesVehicleMotion != 0 || isAircraft;
                if (!isFuelUsingUnit)
                {
                    stateRw.LastCell = cell;
                    continue;
                }

                float fuelPerCell = isAircraft
                    ? math.max(0f, consumption.ValueRO.AirFuelPerCell)
                    : math.max(0f, consumption.ValueRO.GroundFuelPerCell);
                float requestedFuel = movedCells * fuelPerCell;
                if (requestedFuel > 0f)
                {
                    bool expandedUnit = hasExpanded && attemptLookup.HasComponent(entity) &&
                        attemptLookup[entity].SessionId.Equals(session.SessionId);
                    if (expandedUnit)
                        requestedExpandedFuelByFaction[faction.ValueRO.Id] += requestedFuel;
                    else
                        requestedFuelByFaction[faction.ValueRO.Id] += requestedFuel;
                }

                stateRw.LastCell = cell;
 }return requestedFuelByFaction;
}
        public void DrainRequestedFuel(ref SystemState state, NativeArray<float> requestedFuelByFaction)
        {
            for (int factionId = 0; factionId < requestedFuelByFaction.Length; factionId++)
            {
                float remaining = requestedFuelByFaction[factionId];
                if (remaining <= 0f)
                    continue;

                foreach (StorageRef storageRef in query.Enumerate())
                {
                    ref BuildingResourceStorageComponent storage = ref storageRef.ValueRW;
                    if (storage.OwnerFactionId != (byte)factionId ||
                        !IsUsableFuelStorage(storage))
                    {
                        continue;
                    }

                    float availableFuel = math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels - storage.CivilianFuelReserveBarrels);
                    float drained = math.min(availableFuel, remaining);
                    if (drained <= 0f)
                        continue;

                    storage.StoredFuelBarrels = math.max(0f, storage.StoredFuelBarrels - drained);
                    storage.Version++;
                    remaining -= drained;
                    if (remaining <= 0.001f)
                        break;
                }
            }
        }
        private static bool IsUsableFuelStorage(in BuildingResourceStorageComponent storage)
        {
            return storage.FuelStorageCapacity > 0 &&
                   storage.FuelBarrelsPerDay <= 0f &&
                   storage.OilBarrelsPerDay <= 0f;
        }
}
}
