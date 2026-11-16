using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Runtime;
using Game.Skirmish.Contracts;

// Candidate source-config scalars only. This does not select active native
// definitions, create entities, place buildings, grant stock or seed runtime IDs.
static class StartupBuildingDefaultsProgram
{
    static int Main(string[] args)
    {
        try
        {
            using var doc=JsonDocument.Parse(System.IO.File.ReadAllBytes(args[0]));
            var root=doc.RootElement;var configs=root.GetProperty("configs").EnumerateArray().ToDictionary(v=>v.GetProperty("key").GetString(),v=>v.GetProperty("values"));
            var rates=root.GetProperty("facility_rates");var rows=new List<object>();
            foreach(var structure in root.GetProperty("structures").EnumerateArray())
            {
                string type=structure.GetProperty("StructureId").GetString();byte faction=structure.GetProperty("FactionId").GetByte();
                string key=SkirmishStructureIds.VisualKey(type);var c=configs[key];
                int I(string name)=>c.GetProperty(name).GetInt32();float F(string name)=>c.GetProperty(name).GetSingle();bool B(string name)=>c.GetProperty(name).GetBoolean();
                int health=Math.Max(1,I("maxHealth"));
                var storage=new JsonObject{["OilStorageCapacity"]=Math.Max(0,I("oilStorageCapacity")),["FuelStorageCapacity"]=Math.Max(0,I("fuelStorageCapacity")),["OilBarrelsPerDay"]=Math.Max(0f,F("oilBarrelsPerDay")),["FuelBarrelsPerDay"]=Math.Max(0f,F("fuelBarrelsPerDay"))};
                bool hasStorage=Math.Max(0,I("oilStorageCapacity"))>0||Math.Max(0,I("fuelStorageCapacity"))>0||Math.Max(0f,F("oilBarrelsPerDay"))>0||Math.Max(0f,F("fuelBarrelsPerDay"))>0;
                JsonObject weapon=null,attack=null;
                if(B("canAttack")&&Math.Max(0f,F("attackRange"))>0&&Math.Max(0,I("attackDamage"))>0)
                {
                    var color=c.GetProperty("attackTraceColor");var trace=new JsonObject{["x"]=color[0].GetSingle(),["y"]=color[1].GetSingle(),["z"]=color[2].GetSingle(),["w"]=color[3].GetSingle()};
                    attack=new JsonObject{["Range"]=Math.Max(0f,F("attackRange")),["CooldownSeconds"]=Math.Max(.01f,F("attackCooldownSeconds")),["Damage"]=Math.Max(0,I("attackDamage")),["TraceColor"]=trace,["TraceWidth"]=Math.Max(.01f,F("attackTraceWidth")),["TraceScrollSpeed"]=Math.Max(.1f,F("attackTraceScrollSpeed")),["TraceDashDensity"]=Math.Max(1f,F("attackTraceDashDensity")),["TraceVisibleSeconds"]=Math.Max(.01f,F("attackTraceVisibleSeconds")),["TracerEveryNthShot"]=Math.Max(1,I("attackTracerEveryNthShot"))};
                    weapon=JsonNode.Parse(attack.ToJsonString()).AsObject();weapon["MaxConcurrentAttacks"]=Math.Clamp(Math.Max(1,I("maxConcurrentAttacks")),1,4);
                }
                JsonObject recipe=null;
                if(B("materialFabricationEnabled")&&Math.Max(0,I("oilStorageCapacity"))>0&&Math.Max(0f,F("materialFabricationOilConsumedPerCycle"))>0&&Math.Max(0,I("materialFabricationMaterialsOutputPerCycle"))>0&&Math.Max(.01f,F("materialFabricationCycleDurationSeconds"))>0&&I("materialFabricationOutputCapacityPolicy")==0)
                    recipe=new JsonObject{["ProductionEnabled"]=1,["OutputCapacityPolicy"]=0,["OilConsumedPerCycle"]=Math.Max(0f,F("materialFabricationOilConsumedPerCycle")),["MaterialsOutputPerCycle"]=Math.Max(0,I("materialFabricationMaterialsOutputPerCycle")),["CycleDurationSeconds"]=Math.Max(.01f,F("materialFabricationCycleDurationSeconds"))};
                if(type==SkirmishStructureIds.FabricationDepot&&recipe!=null)
                {recipe["OilConsumedPerCycle"]=rates.GetProperty("materialInput").GetInt32();recipe["MaterialsOutputPerCycle"]=rates.GetProperty("materialOutput").GetInt32();recipe["CycleDurationSeconds"]=30;}
                JsonObject profile=null;
                if(hasStorage&&(type==SkirmishStructureIds.OilPump||type==SkirmishStructureIds.Refinery||type==SkirmishStructureIds.RefineryModule))
                {
                    bool refinery=type!=SkirmishStructureIds.OilPump;
                    profile=new JsonObject{["PeriodSeconds"]=60,["OilPerPeriod"]=refinery?0:rates.GetProperty("oilRate").GetInt32(),["FuelPerPeriod"]=refinery?rates.GetProperty("fuelOutput").GetInt32()*2:0,["OilPerFuelBatch"]=refinery?rates.GetProperty("fuelInput").GetInt32():0,["FuelPerBatch"]=refinery?rates.GetProperty("fuelOutput").GetInt32():0,["FuelBatchSeconds"]=30};
                    storage["OilBarrelsPerDay"]=profile["OilPerPeriod"].GetValue<int>();storage["FuelBarrelsPerDay"]=profile["FuelPerPeriod"].GetValue<int>();
                }
                SkirmishRoleOverlay? overlay=type.StartsWith(SkirmishStructureIds.Barracks,StringComparison.Ordinal)?CandidateBuildingOverlays.BarracksStructure():type==SkirmishStructureIds.Watchtower||type==SkirmishStructureIds.WatchtowerApproach?CandidateBuildingOverlays.WatchtowerStructure():type==SkirmishStructureIds.GroundStaging?CandidateBuildingOverlays.GroundStagingStructure():null;
                JsonObject targetPolicy=new JsonObject{["AllowedTargets"]=0,["Domain"]=4,["Visible"]=0};
                if(overlay.HasValue)
                {
                    var o=overlay.Value;var state=new SkirmishActorInitializationState{IsStructure=true,SharedActor=true,HasHealth=true,HasUnitAttack=attack!=null,HasBuildingWeapon=weapon!=null};
                    var values=new SkirmishActorOverlayValues{MaxHealth=o.MaxHealth,Damage=o.Damage,RangeWorld=o.RangeWorld,TargetDomains=(byte)o.TargetDomains};
                    SkirmishActorInitializationKernel.ApplyHealth(ref state,values);SkirmishActorInitializationKernel.BindWeapons(ref state,values,CandidateBuildingConstants.ShotIntervalSeconds);health=state.HealthMax;
                    if(attack!=null){attack["Damage"]=state.AttackDamage;attack["Range"]=state.AttackRange;attack["CooldownSeconds"]=state.AttackCooldown;}
                    if(weapon!=null){weapon["Damage"]=state.BuildingDamage;weapon["Range"]=state.BuildingRange;weapon["CooldownSeconds"]=state.BuildingCooldown;}
                    targetPolicy=new JsonObject{["AllowedTargets"]=state.AllowedTargets,["Domain"]=state.Domain,["Visible"]=state.Visible};
                }
                rows.Add(new{id=type+"."+faction,structure_id=type,source_key=key,faction,source_config_health=Math.Max(1,I("maxHealth")),health=new{Current=health,Max=health},storage=hasStorage?storage:null,unit_attack=attack,building_weapon=weapon,fabrication_recipe=recipe,production_profile=profile,target_policy=targetPolicy,active_definition_selection_proven=false,physical_state_initialized=false});
            }
            Console.WriteLine(JsonSerializer.Serialize(new{scope="CandidateSourceBuildingScalarDefaults_NotActiveNativeDefinitions",structures=rows,physical_startup_parity=false,full_mission_initialized=false,player_ready=false}));return 0;
        }
        catch(Exception e){Console.WriteLine(JsonSerializer.Serialize(new{result="Failed",error=e.Message,physical_startup_parity=false,player_ready=false}));return 1;}
    }
}
