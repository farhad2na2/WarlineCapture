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
        Shape(row,"id","kind","stage","authority","roster","unit_query_order","storage_query_order","stores","steering_steps","expected");
        Need(S(row,"kind")=="body_transition"&&!string.IsNullOrWhiteSpace(S(row,"id")),"Explicit body case identity required");
        string stage=S(row,"stage");Need(stage=="ground_fuel_consumer"||stage=="vehicle_steering_utility","Unsupported body stage; no full-frame fallback");
        var authority=P(row,"authority");Shape(authority,"evidence_kind","evidence_sha256","before_boundary","caller_policy_sha256","grounding_policy_sha256","collision_policy_sha256","order_evidence_sha256","unit_query_complete","storage_query_complete","steering_query_complete","shared_supply_initialized","expanded_session_count","steel_push","contains_aircraft");
        string kind=S(authority,"evidence_kind");Need(kind=="captured_stage"||kind=="synthetic_control","Explicit captured-stage or synthetic provenance required");
        Need(!B(authority,"contains_aircraft"),"Aircraft stages unsupported");
        string evidence=Hash(authority,"evidence_sha256");Hash(authority,"caller_policy_sha256");Hash(authority,"grounding_policy_sha256");Hash(authority,"collision_policy_sha256");Hash(authority,"order_evidence_sha256");
        Need(S(authority,"before_boundary")== (stage=="ground_fuel_consumer"?"before-ground-fuel-consumer":"before-vehicle-steering-utility"),"Exact before-stage boundary authority required");
        var rosterArray=P(row,"roster");Need(rosterArray.ValueKind==JsonValueKind.Array&&rosterArray.GetArrayLength()>=1&&rosterArray.GetArrayLength()<=512,"Bounded full captured roster required");
        Need(kind!="captured_stage"||rosterArray.GetArrayLength()==52,"Actual S002 full52 prestate roster required");
        var roster=new Dictionary<string,JsonElement>();var entities=new HashSet<string>();
        foreach(var actor in rosterArray.EnumerateArray()) {
            Shape(actor,"id","entity","role","category","faction","components");string id=S(actor,"id");Need(!string.IsNullOrWhiteSpace(id)&&roster.TryAdd(id,actor),"Unique full roster identities required");
            var entity=P(actor,"entity");Shape(entity,"index","version");Need(I(entity,"index")>=0&&I(entity,"version")>=0&&entities.Add(I(entity,"index")+":"+I(entity,"version")),"Unique captured ECS identities required");
            Byte(actor,"role");Byte(actor,"category");Byte(actor,"faction");var components=P(actor,"components");Need(components.ValueKind==JsonValueKind.Object,"Full captured component prestate required");
            foreach(string name in new[]{"UnitGrid","UnitFootprint","UnitMovementBehavior","LocalTransform","Faction"})Need(components.TryGetProperty(name,out var core)&&core.ValueKind==JsonValueKind.Object,"Captured body prestate missing "+name);
            Need(Byte(P(components,"Faction"),"Id")==Byte(actor,"faction"),"Captured faction metadata/component identity differs");
        }
        var ids=roster.Keys.ToHashSet();string[] unitOrder=Order(row,"unit_query_order",ids,512);
        var expected=P(row,"expected");
        if(stage=="ground_fuel_consumer") {
            Need(P(row,"steering_steps").GetArrayLength()==0,"Fuel stage cannot infer steering/caller stage");
            var storageRows=P(row,"stores");Need(storageRows.ValueKind==JsonValueKind.Array&&storageRows.GetArrayLength()<=4096,"Bounded complete storage prestate required");
            var storageMap=new Dictionary<string,BuildingResourceStorageComponent>();
            foreach(var store in storageRows.EnumerateArray()) {Shape(store,"id","value");string id=S(store,"id");Need(!string.IsNullOrWhiteSpace(id)&&!storageMap.ContainsKey(id),"Unique captured store identities required");var value=P(store,"value").Deserialize<BuildingResourceStorageComponent>(Fields);AssertStorage(value,P(store,"value"));storageMap.Add(id,value);}
            string[] storeOrder=Order(row,"storage_query_order",storageMap.Keys.ToHashSet(),4096);Need(storeOrder.Length==storageMap.Count,"Every captured storage query row must be ordered");
            var units=unitOrder.Select(id=>FuelUnit(id,P(roster[id],"components"))).ToArray();var stores=storeOrder.Select(id=>storageMap[id]).ToArray();
            var boundary=new FuelBoundary{UnitQueryComplete=B(authority,"unit_query_complete"),StorageQueryComplete=B(authority,"storage_query_complete"),BeforeConsumerBoundary=true,SharedSupplyInitialized=B(authority,"shared_supply_initialized"),SteelPush=B(authority,"steel_push"),ContainsAircraft=B(authority,"contains_aircraft"),ExpandedSessionCount=I(authority,"expanded_session_count"),EvidenceSha256=evidence,StorageEntityIds=storeOrder};
            float[] requested=CapturedPhysicalFuel.Advance(boundary,units,stores);
            Shape(expected,"requested_bits","unit_states","stores");AssertBits(P(expected,"requested_bits"),requested.Select(Bits).ToArray(),"requested fuel");
            var expectedUnits=P(expected,"unit_states");Need(expectedUnits.GetArrayLength()==units.Length,"Complete expected fuel-state assertions required");
            for(int i=0;i<units.Length;i++){var value=expectedUnits[i];Shape(value,"id","last_cell","initialized");Need(S(value,"id")==units[i].Id&&I(value,"initialized")==units[i].State.Initialized,"Expected fuel state assertion differs");AssertBits(P(value,"last_cell"),new[]{units[i].State.LastCell.x,units[i].State.LastCell.y},"LastCell integers");}
            var expectedStores=P(expected,"stores");Need(expectedStores.GetArrayLength()==stores.Length,"Complete expected storage assertions required");for(int i=0;i<stores.Length;i++){Shape(expectedStores[i],"id","value");Need(S(expectedStores[i],"id")==storeOrder[i],"Expected storage identity differs");AssertStorage(stores[i],P(expectedStores[i],"value"));}
            return new{scope="CapturedGroundFuelStageOnly",evidence_kind=kind,evidence_sha256=evidence,unit_query_order=unitOrder,storage_query_order=storeOrder,requested_bits=requested.Select(Bits).ToArray(),unit_states=units.Select(u=>new{id=u.Id,last_cell=new[]{u.State.LastCell.x,u.State.LastCell.y},initialized=u.State.Initialized}).ToArray(),stores=stores.Select((s,i)=>new{id=storeOrder[i],value=s}).ToArray(),native_parity=false,complete_frame_derived=false,mission_frames_advanced=0,mission_qualified=false};
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
