using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FuelPrototype;
using Game.Components;
using Unity.Mathematics;
using Unity.Transforms;

// Stage-local mechanics only. Callers, collision, grounding and orders must supply
// their actual pre-boundary decisions; this endpoint never derives a whole frame.
internal static class CapturedBodyTransition
{
    static readonly JsonSerializerOptions Fields = new() { IncludeFields = true };
    static void Need(bool ok,string message) { if(!ok) throw new ArgumentException(message); }
    static JsonElement P(JsonElement value,string name) => value.GetProperty(name);
    static string S(JsonElement value,string name) => P(value,name).GetString();
    static bool B(JsonElement value,string name) => P(value,name).GetBoolean();
    static int I(JsonElement value,string name) => P(value,name).GetInt32();
    static float F(JsonElement value,string name) { float f=P(value,name).GetSingle();Need(float.IsFinite(f),"Finite explicit component/input value required: "+name);return f; }
    static int Bits(float f) => BitConverter.SingleToInt32Bits(f);
    static void Shape(JsonElement value,params string[] fields)
    {
        Need(value.ValueKind==JsonValueKind.Object,"Object required");
        var actual=value.EnumerateObject().Select(p=>p.Name).ToArray();
        Need(actual.Length==fields.Length&&actual.Distinct().Count()==actual.Length&&actual.All(fields.Contains),"Exact field set required: "+string.Join(",",fields));
    }
    static string Hash(JsonElement value,string name)
    {
        string hash=S(value,name);Need(hash!=null&&System.Text.RegularExpressions.Regex.IsMatch(hash,"^[0-9a-f]{64}$"),"Explicit authority SHA256 required: "+name);return hash;
    }
    static int2 Int2(JsonElement value) {Shape(value,"x","y");return new(I(value,"x"),I(value,"y"));}
    static float3 Float3(JsonElement value) {Shape(value,"x","y","z");return new(F(value,"x"),F(value,"y"),F(value,"z"));}
    static byte Byte(JsonElement value,string name) => P(value,name).GetByte();
    static string[] Order(JsonElement row,string name,HashSet<string> available,int maximum)
    {
        var array=P(row,name);Need(array.ValueKind==JsonValueKind.Array&&array.GetArrayLength()<=maximum,"Bounded explicit query order required");
        string[] ids=array.EnumerateArray().Select(v=>v.GetString()).ToArray();Need(ids.All(id=>!string.IsNullOrWhiteSpace(id)&&available.Contains(id))&&ids.Distinct().Count()==ids.Length,"Complete ordered identities required: "+name);return ids;
    }
    static UnitRow FuelUnit(string id,JsonElement components)
    {
        var grid=P(components,"UnitGrid");Shape(grid,"Cell");var faction=P(components,"Faction");Shape(faction,"Id");
        var behavior=P(components,"UnitMovementBehavior");Shape(behavior,"AllowIdleWander","UsesVehicleMotion");
        var fuel=P(components,"UnitFuelConsumption");Shape(fuel,"GroundFuelPerCell","AirFuelPerCell","Enabled");
        var state=P(components,"UnitFuelConsumptionState");Shape(state,"LastCell","Initialized");
        Need(!components.TryGetProperty("UnitAirMovement",out var air)||air.ValueKind==JsonValueKind.Null,"Aircraft query membership unsupported");
        return new UnitRow {Id=id,CapturedValueMask=31,QueryEligible=true,IsAircraft=false,
            Grid=new UnitGrid{Cell=Int2(P(grid,"Cell"))},Faction=new Faction{Id=Byte(faction,"Id")},
            Movement=new UnitMovementBehavior{AllowIdleWander=Byte(behavior,"AllowIdleWander"),UsesVehicleMotion=Byte(behavior,"UsesVehicleMotion")},
            Consumption=new UnitFuelConsumption{GroundFuelPerCell=F(fuel,"GroundFuelPerCell"),AirFuelPerCell=F(fuel,"AirFuelPerCell"),Enabled=Byte(fuel,"Enabled")},
            State=new UnitFuelConsumptionState{LastCell=Int2(P(state,"LastCell")),Initialized=Byte(state,"Initialized")}};
    }
    static UnitFuelConsumptionState CertifiedAircraftNoop(JsonElement row,JsonElement components)
    {
        var grids=P(row,"fuel_grids");Need(grids.ValueKind==JsonValueKind.Array&&grids.GetArrayLength()==1,"Unique actual before-consumer grid required for aircraft no-op certificate");
        var grid=P(grids[0],"config");var captured=P(row,"captured_grid_config");
        foreach(string name in new[]{"Width","Height"})Need(I(grid,name)==I(captured,name)&&I(grid,name)>0,"Captured/native aircraft grid dimensions differ");
        Need(Bits(F(grid,"CellSize"))==Bits(F(captured,"CellSize"))&&F(grid,"CellSize")>0,"Positive matching actual cell size required");
        var origin=P(grid,"Origin");var reference=P(captured,"Origin");foreach(string axis in new[]{"x","y","z"})Need(Bits(F(origin,axis))==Bits(F(reference,axis)),"Captured/native aircraft grid origin differs");
        Need(P(components,"UnitAirMovement").ValueKind==JsonValueKind.Object&&P(components,"LocalTransform").ValueKind==JsonValueKind.Object,"Actual aircraft transform/membership required");
        var fuel=P(components,"UnitFuelConsumption");Need(Byte(fuel,"Enabled")==1,"Narrow initialized enabled aircraft no-op certificate required");
        var state=P(components,"UnitFuelConsumptionState");Shape(state,"LastCell","Initialized");Need(Byte(state,"Initialized")==1,"Uninitialized aircraft fuel unsupported");
        var position=P(P(components,"LocalTransform"),"Position");var frame=new GridCoordinateFrame{OriginX=F(origin,"x"),OriginY=F(origin,"y"),OriginZ=F(origin,"z"),CellSize=F(grid,"CellSize")};
        var effective=GridCoordinateKernel.WorldToCell(frame,F(position,"x"),F(position,"z"));var last=Int2(P(state,"LastCell"));
        Need(effective.X==last.x&&effective.Z==last.y,"Moving aircraft fuel unsupported: no source early-continue certificate");
        return new UnitFuelConsumptionState{LastCell=last,Initialized=1};
    }
    static CapturedVehicleState Vehicle(string id,JsonElement row)
    {
        var c=P(row,"components");Need(!c.TryGetProperty("UnitAirMovement",out var air)||air.ValueKind==JsonValueKind.Null,"Aircraft steering unsupported");var footprint=P(c,"UnitFootprint");Shape(footprint,"Size");
        var behavior=P(c,"UnitMovementBehavior");Shape(behavior,"AllowIdleWander","UsesVehicleMotion");
        var movement=P(c,"UnitVehicleMovement");Shape(movement,"TurnSpeedDegrees","Acceleration","Braking","RearPivotOffset");
        var kinetics=P(c,"UnitVehicleKinematics");Shape(kinetics,"CurrentSpeed","StallSeconds");
        var move=P(c,"UnitMove");Shape(move,"Speed","WalkSpeed","RoadSpeedMultiplier","ArriveDistance");
        var pose=P(c,"LocalTransform");Shape(pose,"Position","Rotation","Scale");var rotation=P(pose,"Rotation");Shape(rotation,"value");var q=P(rotation,"value");Shape(q,"x","y","z","w");
        return new CapturedVehicleState{Id=id,Role=Byte(row,"role"),Category=Byte(row,"category"),Faction=Byte(row,"faction"),
            Footprint=new UnitFootprint{Size=Int2(P(footprint,"Size"))},Behavior=new UnitMovementBehavior{AllowIdleWander=Byte(behavior,"AllowIdleWander"),UsesVehicleMotion=Byte(behavior,"UsesVehicleMotion")},
            Movement=new UnitVehicleMovement{TurnSpeedDegrees=F(movement,"TurnSpeedDegrees"),Acceleration=F(movement,"Acceleration"),Braking=F(movement,"Braking"),RearPivotOffset=F(movement,"RearPivotOffset")},
            Kinematics=new UnitVehicleKinematics{CurrentSpeed=F(kinetics,"CurrentSpeed"),StallSeconds=F(kinetics,"StallSeconds")},
            Transform=new LocalTransform{Position=Float3(P(pose,"Position")),Rotation=new quaternion(F(q,"x"),F(q,"y"),F(q,"z"),F(q,"w")),Scale=F(pose,"Scale")},
            CapturedSpeed=F(move,"Speed"),CapturedWalkSpeed=F(move,"WalkSpeed"),CapturedRoadMultiplier=F(move,"RoadSpeedMultiplier")};
    }
    static int[] VehicleBits(CapturedVehicleState s) => new[]{Bits(s.Transform.Position.x),Bits(s.Transform.Position.y),Bits(s.Transform.Position.z),Bits(s.Transform.Rotation.value.x),Bits(s.Transform.Rotation.value.y),Bits(s.Transform.Rotation.value.z),Bits(s.Transform.Rotation.value.w),Bits(s.Transform.Scale),Bits(s.Kinematics.CurrentSpeed),Bits(s.Kinematics.StallSeconds)};
    static void AssertBits(JsonElement expected,int[] actual,string label)
    {Need(expected.ValueKind==JsonValueKind.Array&&expected.GetArrayLength()==actual.Length&&expected.EnumerateArray().Select(v=>v.GetInt32()).SequenceEqual(actual),"Expected poststate assertion differs: "+label);}
    static void AssertStorage(BuildingResourceStorageComponent actual,JsonElement expected)
    {
        var fields=typeof(BuildingResourceStorageComponent).GetFields(BindingFlags.Instance|BindingFlags.Public);Shape(expected,fields.Select(f=>f.Name).ToArray());
        foreach(var field in fields) {var value=P(expected,field.Name);bool same=field.FieldType==typeof(float)?Bits((float)field.GetValue(actual))==Bits(value.GetSingle()):field.FieldType==typeof(uint)?(uint)field.GetValue(actual)==value.GetUInt32():Convert.ToInt64(field.GetValue(actual))==value.GetInt64();Need(same,"Expected storage assertion differs: "+field.Name);}
    }
    internal static object Run(JsonElement row)
    {
        BodyTransitionSourcePins.RequireVerified();
        bool globalFuel=row.TryGetProperty("prestate_scope",out var prestateScope)&&prestateScope.GetString()=="complete_global_fuel_query";
        if(globalFuel)Shape(row,"id","kind","stage","authority","roster","unit_query_order","storage_query_order","stores","steering_steps","expected","prestate_scope","storage_order_proof","fuel_grids","captured_grid_config");
        else Shape(row,"id","kind","stage","authority","roster","unit_query_order","storage_query_order","stores","steering_steps","expected");
        Need(S(row,"kind")=="body_transition"&&!string.IsNullOrWhiteSpace(S(row,"id")),"Explicit body case identity required");
        string stage=S(row,"stage");Need(!globalFuel||stage=="ground_fuel_consumer","Global fuel query scope is stage-specific");Need(stage=="ground_fuel_consumer"||stage=="vehicle_steering_utility","Unsupported body stage; no full-frame fallback");
        var authority=P(row,"authority");Shape(authority,"evidence_kind","evidence_sha256","before_boundary","caller_policy_sha256","grounding_policy_sha256","collision_policy_sha256","order_evidence_sha256","unit_query_complete","storage_query_complete","steering_query_complete","shared_supply_initialized","expanded_session_count","steel_push","contains_aircraft");
        string kind=S(authority,"evidence_kind");Need(kind=="captured_stage"||kind=="synthetic_control","Explicit captured-stage or synthetic provenance required");
        Need(globalFuel||!B(authority,"contains_aircraft"),"Aircraft stages unsupported");
        string evidence=Hash(authority,"evidence_sha256");Hash(authority,"caller_policy_sha256");if(globalFuel){Need(P(authority,"grounding_policy_sha256").ValueKind==JsonValueKind.Null&&P(authority,"collision_policy_sha256").ValueKind==JsonValueKind.Null,"Fuel boundary must explicitly leave upstream grounding/collision unclaimed");}else{Hash(authority,"grounding_policy_sha256");Hash(authority,"collision_policy_sha256");}Hash(authority,"order_evidence_sha256");
        Need(S(authority,"before_boundary")== (stage=="ground_fuel_consumer"?"before-ground-fuel-consumer":"before-vehicle-steering-utility"),"Exact before-stage boundary authority required");
        var rosterArray=P(row,"roster");Need(rosterArray.ValueKind==JsonValueKind.Array&&rosterArray.GetArrayLength()>=1&&rosterArray.GetArrayLength()<=512,"Bounded full captured roster required");
        Need(globalFuel||kind!="captured_stage"||rosterArray.GetArrayLength()==52,"Actual S002 full52 prestate roster required");
        var roster=new Dictionary<string,JsonElement>();var entities=new HashSet<string>();
        foreach(var actor in rosterArray.EnumerateArray()) {
            Shape(actor,"id","entity","role","category","faction","components");string id=S(actor,"id");Need(!string.IsNullOrWhiteSpace(id)&&roster.TryAdd(id,actor),"Unique full roster identities required");
            var entity=P(actor,"entity");Shape(entity,"index","version");Need(I(entity,"index")>=0&&I(entity,"version")>=0&&entities.Add(I(entity,"index")+":"+I(entity,"version")),"Unique captured ECS identities required");
            if(!globalFuel){Byte(actor,"role");Byte(actor,"category");}Byte(actor,"faction");var components=P(actor,"components");Need(components.ValueKind==JsonValueKind.Object,"Full captured component prestate required");
            foreach(string name in new[]{"UnitGrid","UnitFootprint","UnitMovementBehavior","LocalTransform","Faction"})Need(components.TryGetProperty(name,out var core)&&core.ValueKind==JsonValueKind.Object,"Captured body prestate missing "+name);
            if(globalFuel){var role=P(components,"SkirmishUnitRoleComponent");if(role.ValueKind==JsonValueKind.Null)Need(P(actor,"role").ValueKind==JsonValueKind.Null&&P(actor,"category").ValueKind==JsonValueKind.Null,"Foreign role metadata must remain absent");else Need(Byte(actor,"role")==Byte(role,"Role")&&Byte(actor,"category")==Byte(role,"Category"),"Captured role metadata differs");}
            Need(Byte(P(components,"Faction"),"Id")==Byte(actor,"faction"),"Captured faction metadata/component identity differs");
        }
        var ids=roster.Keys.ToHashSet();string[] unitOrder=Order(row,"unit_query_order",ids,512);
        Need(!globalFuel||unitOrder.Length==roster.Count,"Complete global fuel query membership/order required");
        var expected=P(row,"expected");
        if(stage=="ground_fuel_consumer") {
            Need(P(row,"steering_steps").GetArrayLength()==0,"Fuel stage cannot infer steering/caller stage");
            var storageRows=P(row,"stores");Need(storageRows.ValueKind==JsonValueKind.Array&&storageRows.GetArrayLength()<=4096,"Bounded complete storage prestate required");
            var storageMap=new Dictionary<string,BuildingResourceStorageComponent>();
            foreach(var store in storageRows.EnumerateArray()) {Shape(store,"id","value");string id=S(store,"id");Need(!string.IsNullOrWhiteSpace(id)&&!storageMap.ContainsKey(id),"Unique captured store identities required");var value=P(store,"value").Deserialize<BuildingResourceStorageComponent>(Fields);AssertStorage(value,P(store,"value"));storageMap.Add(id,value);}
            string[] storeOrder=Order(row,"storage_query_order",storageMap.Keys.ToHashSet(),4096);Need(storeOrder.Length==storageMap.Count,"Every captured storage query row must be ordered");
            if(globalFuel){var proof=P(row,"storage_order_proof");Shape(proof,"native_full_order_proven","observed_prefix","unordered_unvisited_tail","completion_index","completion_count","evidence_sha256");Need(!B(proof,"native_full_order_proven")&&Hash(proof,"evidence_sha256")==evidence,"Explicit partial native storage order proof required");var prefix=P(proof,"observed_prefix").EnumerateArray().Select(v=>v.GetString()).ToArray();var tail=P(proof,"unordered_unvisited_tail").EnumerateArray().Select(v=>v.GetString()).ToArray();Need(prefix.Length==49&&tail.Length==2&&I(proof,"completion_count")==2&&I(proof,"completion_index")>=0&&I(proof,"completion_index")<2&&storeOrder.Length==51&&storeOrder.Take(prefix.Length).SequenceEqual(prefix)&&storeOrder.Skip(prefix.Length).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(tail.OrderBy(x=>x,StringComparer.Ordinal)),"Exact observed prefix and two explicit tail hypotheses required");}
            var ground=new List<UnitRow>();var fullStates=new Dictionary<string,UnitFuelConsumptionState>();var certifiedAircraft=new List<string>();
            foreach(string id in unitOrder){var components=P(roster[id],"components");if(globalFuel&&P(components,"UnitAirMovement").ValueKind!=JsonValueKind.Null){fullStates.Add(id,CertifiedAircraftNoop(row,components));certifiedAircraft.Add(id);}else ground.Add(FuelUnit(id,components));}
            Need(!globalFuel||B(authority,"contains_aircraft")== (certifiedAircraft.Count>0),"Global aircraft membership fact must be retained");
            var units=ground.ToArray();var stores=storeOrder.Select(id=>storageMap[id]).ToArray();
            var boundary=new FuelBoundary{UnitQueryComplete=B(authority,"unit_query_complete"),StorageQueryComplete=B(authority,"storage_query_complete"),BeforeConsumerBoundary=true,SharedSupplyInitialized=B(authority,"shared_supply_initialized"),SteelPush=B(authority,"steel_push"),ContainsAircraft=false,ExpandedSessionCount=I(authority,"expanded_session_count"),EvidenceSha256=evidence,StorageEntityIds=storeOrder};
            float[] requested=CapturedPhysicalFuel.Advance(boundary,units,stores);
            foreach(var unit in units)fullStates.Add(unit.Id,unit.State);
            Shape(expected,"requested_bits","unit_states","stores");AssertBits(P(expected,"requested_bits"),requested.Select(Bits).ToArray(),"requested fuel");
            var expectedUnits=P(expected,"unit_states");Need(expectedUnits.GetArrayLength()==unitOrder.Length,"Complete expected fuel-state assertions required");
            for(int i=0;i<unitOrder.Length;i++){var value=expectedUnits[i];var state=fullStates[unitOrder[i]];Shape(value,"id","last_cell","initialized");Need(S(value,"id")==unitOrder[i]&&I(value,"initialized")==state.Initialized,"Expected fuel state assertion differs");AssertBits(P(value,"last_cell"),new[]{state.LastCell.x,state.LastCell.y},"LastCell integers");}
            var expectedStores=P(expected,"stores");Need(expectedStores.GetArrayLength()==stores.Length,"Complete expected storage assertions required");for(int i=0;i<stores.Length;i++){Shape(expectedStores[i],"id","value");Need(S(expectedStores[i],"id")==storeOrder[i],"Expected storage identity differs");AssertStorage(stores[i],P(expectedStores[i],"value"));}
            return new{scope=globalFuel?"ActualFuelBoundaryConditionalOnUnvisitedTailCompletion":"CapturedGroundFuelStageOnly",native_full_storage_order_proven=false,universal_tail_invariance_proven=false,unvisited_tail_order_is_completion_hypothesis=globalFuel,evidence_kind=kind,evidence_sha256=evidence,unit_query_order=unitOrder,storage_query_order=storeOrder,requested_bits=requested.Select(Bits).ToArray(),global_contains_aircraft=B(authority,"contains_aircraft"),ground_kernel_rows=units.Length,certified_aircraft_noop_ids=certifiedAircraft.ToArray(),moving_aircraft_supported=false,unit_states=unitOrder.Select(id=>new{id,last_cell=new[]{fullStates[id].LastCell.x,fullStates[id].LastCell.y},initialized=fullStates[id].Initialized}).ToArray(),stores=stores.Select((s,i)=>new{id=storeOrder[i],value=s}).ToArray(),native_parity=false,complete_frame_derived=false,mission_frames_advanced=0,mission_qualified=false};
        }
        Need(B(authority,"steering_query_complete"),"Complete captured steering order authority required");
        Need(P(row,"stores").GetArrayLength()==0&&P(row,"storage_query_order").GetArrayLength()==0,"Steering utility cannot infer fuel stage");
        var steps=P(row,"steering_steps");Need(steps.ValueKind==JsonValueKind.Array&&steps.GetArrayLength()==unitOrder.Length&&steps.GetArrayLength()>=1,"Complete explicit steering step order required");
        Shape(expected,"units");var expectedActors=P(expected,"units");Need(expectedActors.GetArrayLength()==steps.GetArrayLength(),"Complete expected steering assertions required");var output=new List<object>();
        for(int i=0;i<steps.GetArrayLength();i++) {
            var step=steps[i];Shape(step,"id","input","caller_evidence_sha256","order_evidence_sha256");string id=S(step,"id");Need(id==unitOrder[i],"Captured steering order differs");Hash(step,"caller_evidence_sha256");Hash(step,"order_evidence_sha256");
            var input=P(step,"input");Shape(input,"direction","turn_threshold_radians","max_speed","delta_time","max_distance");var direction=P(input,"direction");Need(direction.GetArrayLength()==3,"Actual caller direction required");
            var state=Vehicle(id,roster[id]);var explicitStep=new ExplicitVehicleStep(new float3(direction[0].GetSingle(),direction[1].GetSingle(),direction[2].GetSingle()),F(input,"turn_threshold_radians"),F(input,"max_speed"),F(input,"delta_time"),F(input,"max_distance"));
            bool moved=CapturedVehicleSteering.Step(ref state,explicitStep);int[] bits=VehicleBits(state);var assertion=expectedActors[i];Shape(assertion,"id","moved","post_bits");Need(S(assertion,"id")==id&&B(assertion,"moved")==moved,"Expected moved/identity assertion differs");AssertBits(P(assertion,"post_bits"),bits,"vehicle transform/kinematics");output.Add(new{id,moved,post_bits=bits});
        }
        return new{scope="CapturedVehicleSteeringUtilityOnly",evidence_kind=kind,evidence_sha256=evidence,units=output,native_parity=false,complete_frame_derived=false,mission_frames_advanced=0,mission_qualified=false};
    }
}
