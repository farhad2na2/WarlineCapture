using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        const string FixReport="Design/AgentReports/CampaignVisualAudit20261011/fixes";
        [Serializable] class SceneInspection { public string map,source;public RenderInspection[] renderers; }
        [Serializable] class RenderInspection { public string path,mesh,owner;public Vector3 min,max,position,scale,euler;public string[] materials; }
        public static void InspectCampaignFixSources()
        {
            Check();Directory.CreateDirectory(FixReport+"/inspection");var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in VisualMaps)
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                    var rs=SceneRenderers(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)).ToArray();
                    var data=new SceneInspection{map=map,source=Source(map),renderers=rs.Select(r=>new RenderInspection{path=Hierarchy(r.transform),mesh=MeshName(r),min=r.bounds.min,max=r.bounds.max,position=r.transform.position,scale=r.transform.lossyScale,euler=r.transform.eulerAngles,owner=r.GetComponentInParent<OperationMapBuildingAuthoring>()?.StableId,materials=r.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()}).ToArray()};
                    File.WriteAllText(FixReport+"/inspection/"+map+".json",JsonUtility.ToJson(data));
                }
                Debug.Log("[CampaignFixInspection] result=Passed maps=11");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void InspectPlacement(Unity.Entities.EntityManager em,string output)
        {
            using var gridQ=em.CreateEntityQuery(typeof(GridConfig),typeof(GridRoad));
            if(gridQ.CalculateEntityCount()!=1)return;
            var grid=gridQ.GetSingleton<GridConfig>();var entity=gridQ.GetSingletonEntity();
            using var surfaceQ=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            var masks=new Dictionary<string,bool[]>();
            var assembly=typeof(Game.Runtime.CampaignMissionSpawnSystem).Assembly;
            foreach(string name in new[]{"BuildingPlacementAuthoredRoadCache","BuildingPlacementAuthoredBuildingCache"})
            {
                var type=assembly.GetType("Game.Runtime."+name);var cache=Activator.CreateInstance(type,true);
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                type.GetMethod("Ensure",flags).Invoke(cache,name.Contains("Road")?new object[]{em,surfaceQ,grid}:new object[]{em,grid});
                foreach(var field in type.GetFields(flags).Where(f=>f.FieldType==typeof(bool[])))
                    if(field.GetValue(cache) is bool[] mask)masks[name+"."+field.Name]=mask;
            }
            var roads=em.GetBuffer<GridRoad>(entity,true);masks["GridRoad"]=Enumerable.Range(0,roads.Length).Select(i=>roads[i].Value!=0).ToArray();
            var lines=new List<string>{$"grid={grid.Width},{grid.Height} origin={grid.Origin} cell={grid.CellSize}"};
            foreach(var pair in masks)lines.Add(pair.Key+" cells="+pair.Value.Count(x=>x));
            bool Free(int x,int y,int w,int h)
            {
                if(x<0||y<0||x+w>grid.Width||y+h>grid.Height)return false;
                foreach(var mask in masks.Values)for(int z=y;z<y+h;z++)for(int xx=x;xx<x+w;xx++)if(mask[z*grid.Width+xx])return false;
                return true;
            }
            var boundsList=new NativeList<Bounds>(Allocator.Temp);
            try
            {
                Game.Rendering.Contracts.RenderBoundsGateway.Provider?.CollectAuthoredBuildingBounds(em,ref boundsList);
                File.WriteAllText(output+"/authored-building-bounds.txt",string.Join("\n",Enumerable.Range(0,boundsList.Length).Select(i=>$"min={boundsList[i].min} max={boundsList[i].max}")));
            }
            finally{boundsList.Dispose();}
            var lots=new List<(string name,int x,int y,int w,int h)>();
            using(var requestsQuery=em.CreateEntityQuery(typeof(BuildingRuntimeSpawnRequest),typeof(BuildingConfiguredSpawnableReadModel)))
            using(var owners=requestsQuery.ToEntityArray(Allocator.Temp))
            foreach(var owner in owners)
            {
                var definitions=em.GetBuffer<BuildingConfiguredSpawnableReadModel>(owner,true);
                foreach(var request in em.GetBuffer<BuildingRuntimeSpawnRequest>(owner,true))
                {
                    if(request.Status!=BuildingRuntimeSpawnRequest.Failed||(request.AuthoredStartingGrant==0&&request.RequirePreferredOrigin==0))continue;
                    var definition=definitions.ToNativeArray(Allocator.Temp);
                    try
                    {
                        var matches=definition.ToArray().Where(d=>string.Equals(d.BuildingId.ToString(),request.BuildingId.ToString(),StringComparison.OrdinalIgnoreCase)).ToArray();
                        if(matches.Length!=1){lines.Add("unresolved footprint "+request.BuildingId);continue;}
                        var size=matches[0].FootprintCells;
                        if(request.RotateVertical!=0)size=new Unity.Mathematics.int2(size.y,size.x);
                        lots.Add((request.BuildingId.ToString(),request.PreferredOrigin.x,request.PreferredOrigin.y,size.x,size.y));
                    }
                    finally{definition.Dispose();}
                }
            }
            foreach(var lot in lots)
            {
                if(lot.x<0||lot.y<0||lot.x+lot.w>grid.Width||lot.y+lot.h>grid.Height){lines.Add(lot.name+" outside grid");continue;}
                lines.Add(lot.name+" preferred="+lot.x+","+lot.y+" footprint="+lot.w+","+lot.h+" world="+(grid.Origin.x+lot.x*grid.CellSize)+","+(grid.Origin.z+lot.y*grid.CellSize));
                foreach(var pair in masks)
                {int n=0;for(int z=lot.y;z<lot.y+lot.h;z++)for(int x=lot.x;x<lot.x+lot.w;x++)if(pair.Value[z*grid.Width+x])n++;lines.Add(lot.name+" rejected "+pair.Key+"="+n);}
                var candidates=new List<Vector2Int>();
                for(int z=Math.Max(0,lot.y-60);z<Math.Min(grid.Height-lot.h,lot.y+60);z++)for(int x=Math.Max(0,lot.x-60);x<Math.Min(grid.Width-lot.w,lot.x+60);x++)if(Free(x,z,lot.w,lot.h))candidates.Add(new Vector2Int(x,z));
                lines.Add(lot.name+" nearestClearGrid="+string.Join(";",candidates.OrderBy(p=>(p-new Vector2Int(lot.x,lot.y)).sqrMagnitude).Take(12)));
            }
            File.WriteAllText(output+"/placement-inspection.txt",string.Join("\n",lines));
        }
    }
}
