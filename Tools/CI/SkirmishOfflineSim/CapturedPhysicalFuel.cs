using System;
using System.Collections.Generic;
using Game.Components;
namespace FuelPrototype {
// Rows are already the exact eligible ECS query members, in captured native query order.
// These assertions require native evidence. Synthetic mechanics controls set them explicitly.
public sealed class FuelBoundary {
 public bool UnitQueryComplete, StorageQueryComplete, BeforeConsumerBoundary, SharedSupplyInitialized, SteelPush, ContainsAircraft;
 public int ExpandedSessionCount; public string EvidenceSha256; public string[] StorageEntityIds;
}
public static class CapturedPhysicalFuel {
 static void Need(bool x,string s){if(!x)throw new ArgumentException(s);}
 static bool Finite(float x)=>float.IsFinite(x)&&Math.Abs(x)<=1e12f;
 public static float[] Advance(FuelBoundary boundary,UnitRow[] units,BuildingResourceStorageComponent[] stores){
 Need(boundary!=null&&boundary.UnitQueryComplete&&boundary.StorageQueryComplete&&boundary.BeforeConsumerBoundary,"exact complete native query orders at before-consumer boundary required");
 Need(boundary.SharedSupplyInitialized&&boundary.ExpandedSessionCount==1&&!boundary.SteelPush&&!boundary.ContainsAircraft,"only migrated single-session ground physical fuel supported; no aircraft/SteelPush/legacy routing");
 Need(boundary.EvidenceSha256!=null&&boundary.EvidenceSha256.Length==64&&System.Text.RegularExpressions.Regex.IsMatch(boundary.EvidenceSha256,"^[0-9a-f]{64}$"),"input evidence SHA256 required");
 Need(units!=null&&units.Length<=512&&stores!=null&&stores.Length<=4096,"bounded 512 unit / 4096 storage query rows required");
 Need(boundary.StorageEntityIds!=null&&boundary.StorageEntityIds.Length==stores.Length,"captured ordered storage entity identities required");
 var storeIds=new HashSet<string>();foreach(var id in boundary.StorageEntityIds)Need(!string.IsNullOrEmpty(id)&&storeIds.Add(id),"unique captured storage identities required");
 var ids=new HashSet<string>();
 foreach(var u in units){Need(u!=null&&!string.IsNullOrEmpty(u.Id)&&ids.Add(u.Id),"unique captured unit identities required");Need(u.CapturedValueMask==31&&u.QueryEligible&&!u.IsAircraft,"all five query component values and eligibility must be captured; aircraft unsupported");Need(u.State.Initialized<=1,"unsupported nonboolean Initialized");
 Need(Math.Abs((long)u.Grid.Cell.x)<=1048576&&Math.Abs((long)u.Grid.Cell.y)<=1048576&&Math.Abs((long)u.State.LastCell.x)<=1048576&&Math.Abs((long)u.State.LastCell.y)<=1048576,"cell bound prevents integer-delta overflow");
 Need(Finite(u.Consumption.GroundFuelPerCell)&&Finite(u.Consumption.AirFuelPerCell),"finite bounded fuel rates required");}
 foreach(var s in stores)Need(Finite(s.StoredFuelBarrels)&&Finite(s.ReservedFuelOutboundBarrels)&&Finite(s.CivilianFuelReserveBarrels)&&Finite(s.FuelBarrelsPerDay)&&Finite(s.OilBarrelsPerDay),"finite bounded physical fuel fields required");
 var kernel=new ProductionGroundFuelKernel(stores);var requested=kernel.Accumulate(units);var state=new SystemState();kernel.DrainRequestedFuel(ref state,requested);return requested.Copy();
 }
}
}
