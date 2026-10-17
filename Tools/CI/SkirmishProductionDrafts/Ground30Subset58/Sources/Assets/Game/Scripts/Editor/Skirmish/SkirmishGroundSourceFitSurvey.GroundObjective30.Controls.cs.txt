#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static void RunGroundObjective30CollectionControls()
        {
            var input=ReadGround30Envelope();var missions=(JArray)input["missions"];
            Require(missions.Count==30&&missions.Sum(m=>((JArray)m["canonical_size_rows"]).Count)==90,
                "Actual input must retain thirty missions and ninety canonical size vectors");
            Require(missions.Select(m=>(string)m["source_candidate"]["sourcePath"]).Distinct().Count()==4&&
                missions.All(m=>(bool)m["publication_enabled"]==false),"Actual input retains four source groups and no publication");
            Action<JObject>[] corruptions={
                v=>((JArray)v["missions"]).RemoveAt(0),
                v=>v["missions"][1]=v["missions"][0].DeepClone(),
                v=>v["missions"][0]["canonical_size_rows"][0]["player_rifle"]="999",
                v=>v["missions"][0]["definition_id"]="foreign",
                v=>v["missions"][0]["publication_enabled"]=true,
                v=>v["missions"][0]["typed_objective_areas"]["measurement"]=new JObject{["callerApproved"]=true},
                v=>v["missions"][0]["view_intent"]["scalars"]=new JObject{["height"]=18},
                v=>v["authority"]["source_survey"]["sha256"]=new string('0',64)
            };
            foreach(var corrupt in corruptions)
            {
                var value=(JObject)input.DeepClone();corrupt(value);bool rejected=false;
                try{ValidateGround30Envelope(value);}catch(InvalidOperationException){rejected=true;}
                Require(rejected,"Actual input validator accepted a corrupted budget, source authority or proof");
            }
            var shape=new int2(2,3);var behavior=new UnitMovementBehavior{UsesVehicleMotion=1,AllowIdleWander=0};
            var from=new int2(10,20);var to=new int2(30,40);
            string expected=Ground30PathQueryKey(1,shape,behavior,from,to);
            Require(Ground30PathQueryKey(1,shape,behavior,from,to)==expected,"Identical native path inputs may reuse one query");
            Require(new[]{
                Ground30PathQueryKey(2,shape,behavior,from,to),
                Ground30PathQueryKey(1,new int2(3,3),behavior,from,to),
                Ground30PathQueryKey(1,shape,new UnitMovementBehavior{UsesVehicleMotion=0},from,to),
                Ground30PathQueryKey(1,shape,new UnitMovementBehavior{UsesVehicleMotion=1,AllowIdleWander=1},from,to),
                Ground30PathQueryKey(1,shape,behavior,to,from),
                Ground30PathQueryKey(1,shape,behavior,from,new int2(30,41))
            }.All(value=>value!=expected),"Native path reuse must separate faction, hull, behavior and ordered endpoints");
            Debug.Log("[SkirmishGroundObjective30CollectionControls] result=Passed controls=3 corruptions=8 streamingWorldsCreated=0 sourceFitCertified=0");
        }
    }
}
#endif
