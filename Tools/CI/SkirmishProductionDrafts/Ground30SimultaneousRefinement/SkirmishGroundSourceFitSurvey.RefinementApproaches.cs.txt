#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        static void RebuildRefinementConvoyBays(SkirmishScenarioDefinitionConfig definition,EntityManager em,GridConfig grid)
        {
            if(definition.ObjectiveConfig.Kind!=SkirmishObjectiveKind.ConvoyEscort)return;
            var layout=definition.MapLayout;
            Require(layout.TryGetPad(1,SkirmishLegalPadKind.VehicleSpawn,out var vehicle),"Actual player vehicle pad required");
            var envelope=PilotExactAxisAlignedPadCells(layout,vehicle,grid);
            var prefab=FindNativePrefab(em,SkirmishConvoyTruckDefinition.ExistingChassisKey);
            Require(prefab!=Entity.Null&&em.HasComponent<UnitFootprint>(prefab),"Actual native Cargo Truck chassis footprint required");
            var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;
            int width=(envelope.width-2)/3,depth=footprint.y+2;
            Require(width>=footprint.x&&depth<=envelope.height,"Three actual truck footprints plus explicit cell gaps do not fit declared vehicle envelope");
            int left=envelope.x+(envelope.width-(3*width+2))/2,bottom=envelope.y+(envelope.height-depth)/2;
            var pads=new JArray(layout.Pads.Select(EncodePilotPad));var anchors=new JArray(layout.Anchors.Select(EncodePilotAnchor));
            for(int slot=0;slot<3;slot++)
            {
                string role="convoy.origin."+(slot+1);
                var pad=pads.OfType<JObject>().Single(p=>(string)p["RoleId"]==role);
                var anchor=anchors.OfType<JObject>().Single(a=>(string)a["RoleId"]==role);
                Require((int)pad["Kind"]==(int)SkirmishLegalPadKind.ObjectiveTruckSpawn&&(int)pad["FactionId"]==1,
                    "Exact three objective bay declarations required");
                float x=grid.Origin.x+(left+slot*(width+1)+width*.5f)*grid.CellSize;
                float z=grid.Origin.z+(bottom+depth*.5f)*grid.CellSize;
                pad["CenterX"]=x;pad["CenterZ"]=z;pad["WidthMetres"]=width*grid.CellSize;pad["DepthMetres"]=depth*grid.CellSize;
                anchor["WorldX"]=x;anchor["WorldZ"]=z;
                anchor["NormalizedU"]=(x-layout.OriginX)/layout.WorldWidthMetres;anchor["NormalizedV"]=(z-layout.OriginZ)/layout.WorldDepthMetres;
            }
            // Source-axis, cell-aligned authored boxes with real gaps; unchanged
            // compiler overlap and actual parking/source gates remain mandatory.
            JsonUtility.FromJsonOverwrite(new JObject{["pads"]=pads,["anchors"]=anchors}.ToString(Newtonsoft.Json.Formatting.None),layout);
        }

        static void AddRefinementDefenseApproaches(SkirmishScenarioDefinitionConfig definition,GridConfig grid)
        {
            if(definition.ObjectiveConfig.Kind!=SkirmishObjectiveKind.Breakthrough&&definition.ObjectiveConfig.Kind!=SkirmishObjectiveKind.ConvoyEscort)return;
            var layout=definition.MapLayout;
            string[] paths=AssetDatabase.FindAssets("Building_GuardTower t:Prefab",new[]{"Assets/Game/Prefabs/Buildings"})
                .Select(AssetDatabase.GUIDToAssetPath).Where(p=>System.IO.Path.GetFileNameWithoutExtension(p)=="Building_GuardTower").ToArray();
            Require(paths.Length==1,"Unique actual GuardTower prefab required");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);var authoring=prefab.GetComponent<BuildingDefinitionAuthoring>();
            Require(authoring!=null&&!EditorUtility.IsDirty(prefab),"Clean actual GuardTower placement metadata required");
            var footprint=authoring.ConfiguredFootprintCells;
            var anchors=new JArray(layout.Anchors.Select(EncodePilotAnchor));var pads=new JArray(layout.Pads.Select(EncodePilotPad));
            var routeKinds=new[]{SkirmishMeasuredRouteKind.FlankNorthRuins,SkirmishMeasuredRouteKind.FlankSouthSweep};
            for(int i=0;i<2;i++)
            {
                string role=i==0?"defense.approach.a":"defense.approach.b";
                Require(!layout.TryGetAnchor(role,out _)&&!layout.TryGetPad(role,out _),"Refinement cannot overwrite an existing authored approach");
                Require(layout.TryGetRoute(routeKinds[i],out var route)&&route.WaypointAnchorIds.Length>=3,
                    "Exact distinct source approach with an interior waypoint required: "+routeKinds[i]);
                Require(layout.TryGetAnchorById(route.WaypointAnchorIds[route.WaypointAnchorIds.Length-2],out var original),"Exact defender-side approach waypoint required");
                Require(layout.TryProjectLocalToMap(original.WorldX,original.WorldZ,out float x,out float z),"Source route point projection required");
                float half=(Math.Max(footprint.x,footprint.y)+1)*grid.CellSize;
                Require(x-half>=definition.CampaignMap.Bounds.PlayableMin.x&&x+half<=definition.CampaignMap.Bounds.PlayableMax.x&&
                    z-half>=definition.CampaignMap.Bounds.PlayableMin.z&&z+half<=definition.CampaignMap.Bounds.PlayableMax.z,"Approach envelope outside existing source");
                string id="anchor."+layout.LayoutId+"."+role;
                anchors.Add(new JObject{["AnchorId"]=id,["RoleId"]=role,["WorldX"]=original.WorldX,["WorldZ"]=original.WorldZ,
                    ["NormalizedU"]=original.NormalizedU,["NormalizedV"]=original.NormalizedV});
                pads.Add(new JObject{["PadId"]="pad."+layout.LayoutId+"."+role,["AnchorId"]=id,["RoleId"]=role,
                    ["Kind"]=(int)SkirmishLegalPadKind.DefenseApproach,["FactionId"]=2,["CenterX"]=original.WorldX,["CenterZ"]=original.WorldZ,
                    ["WidthMetres"]=half*2,["DepthMetres"]=half*2});
            }
            // Explicit authoring envelopes around existing logical approach points;
            // native placement must find actual source-legal footprints inside them.
            JsonUtility.FromJsonOverwrite(new JObject{["anchors"]=anchors,["pads"]=pads}.ToString(Newtonsoft.Json.Formatting.None),layout);
        }
    }
}
#endif
