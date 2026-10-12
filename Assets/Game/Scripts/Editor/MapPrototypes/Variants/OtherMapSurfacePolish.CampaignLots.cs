using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static Rect XzRect(Bounds b,float margin=0)=>Rect.MinMaxRect(b.min.x-margin,b.min.z-margin,b.max.x+margin,b.max.z+margin);
        static bool StandingScenery(MeshRenderer r)
        {
            string name=MeshName(r);
            return r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&r.bounds.size.y>=1&&
                (name.StartsWith("SM_Bld_")||name.StartsWith("SM_Prop_")||name.Contains("Tree")||name.Contains("Palm"));
        }
        public static void ClearCitywideAuthoredLots()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();var journal=new List<string>();
            try
            {
                var scene=EditorSceneManager.OpenScene(CH05M01CitywideAlertConfigBuilder.EntityScenePath,OpenSceneMode.Single);
                var map=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(CH05M01CitywideAlertConfigBuilder.MapPath);
                var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(CH05M01CitywideAlertConfigBuilder.SurfacePath);
                if(!surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob))throw new Exception("Missing lot support");
                using(blob)
                {
                    var protection=new RefinementProtection(scene);var rs=SceneRenderers(scene);var solid=rs.Where(StandingScenery).ToArray();
                    var lots=CH05M01CitywideAlertConfigBuilder.BuildingLots.Select(l=>new Rect(l.xMin-1,l.yMin-1,l.width+2,l.height+2)).ToArray();
                    var roots=solid.Where(r=>lots.Any(l=>l.Overlaps(XzRect(r.bounds,1)))).Select(r=>r.GetComponentInParent<DenseCityPresentationIdentityAuthoring>()).Distinct().ToArray();
                    if(roots.Any(r=>r==null||r.Role!=OperationMapEntityPresentationRole.RenderOnly||r.GetComponentInParent<OperationMapBuildingAuthoring>()!=null))throw new Exception("Required lot overlaps a gameplay building owner");
                    foreach(var group in roots.GroupBy(i=>i.name.Contains("Village_Wall")?"garden-wall-group":i.StableId).OrderBy(g=>g.Key,StringComparer.Ordinal))
                    {
                        var members=group.ToArray();var identity=members[0];var t=identity.transform;var renderers=members.SelectMany(i=>i.GetComponentsInChildren<MeshRenderer>(true)).Where(StandingScenery).ToArray();if(renderers.Length==0)continue;
                        if(members.Any(i=>i.GetComponentsInChildren<Collider>(true).Length!=0))throw new Exception("Lot scenery has authored collision; needs separate geometry review "+t.name);
                        var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                        var old=t.position;bool placed=false;
                        for(int radius=6;radius<=90&&!placed;radius+=3)
                        for(int step=0;step<24&&!placed;step++)
                        {
                            float angle=step*Mathf.PI/12;var delta=new Vector3(Mathf.Round(Mathf.Cos(angle)*radius),0,Mathf.Round(Mathf.Sin(angle)*radius));
                            var candidate=new Bounds(bounds.center+delta,bounds.size);var area=XzRect(candidate,1);
                            if(area.xMin<map.Bounds.PlayableMin.x+2||area.xMax>map.Bounds.PlayableMax.x-2||area.yMin<map.Bounds.PlayableMin.z+2||area.yMax>map.Bounds.PlayableMax.z-2||lots.Any(l=>l.Overlaps(area)))continue;
                            if(map.Anchors.ToArray().Any(a=>a.Kind!=OperationMapAnchorKind.Camera&&a.Kind!=OperationMapAnchorKind.Objective&&new Rect(a.Position.x-Mathf.Max(5,a.Radius),a.Position.z-Mathf.Max(5,a.Radius),Mathf.Max(5,a.Radius)*2,Mathf.Max(5,a.Radius)*2).Overlaps(area)))continue;
                            if(solid.Any(r=>!members.Any(i=>r.transform.IsChildOf(i.transform)||r.transform==i.transform)&&XzRect(r.bounds,.4f).Overlaps(area)))continue;
                            bool supported=true;float low=float.PositiveInfinity,high=float.NegativeInfinity;
                            for(int z=Mathf.FloorToInt(area.yMin);z<Mathf.CeilToInt(area.yMax)&&supported;z++)
                            for(int x=Mathf.FloorToInt(area.xMin);x<Mathf.CeilToInt(area.xMax);x++)
                            {
                                int2 cell=(int2)math.floor((new float2(x+.5f,z+.5f)-blob.Value.GridOrigin.xz)/blob.Value.CellSize);
                                if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample)||(sample.Flags&(MapSurfaceFlags.Road|MapSurfaceFlags.Highway|MapSurfaceFlags.Bridge))!=0||(sample.MovementMask&MapSurfaceMovementMask.Infantry)==0||sample.SlopeDegrees>10){supported=false;break;}
                                low=Mathf.Min(low,sample.Height);high=Mathf.Max(high,sample.Height);
                            }
                            if(!supported||high-low>.6f)continue;
                            foreach(var member in members)
                            {
                                var before=member.transform.position;
                                member.transform.position+=delta+new Vector3(0,high-Mathf.Max(0,bounds.min.y),0);
                                protection.Moved.Add(member.transform.GetEntityId().ToString());EditorUtility.SetDirty(member.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(member.transform);
                                journal.Add(member.StableId+" "+member.name+" from="+before+" to="+member.transform.position);
                            }
                            placed=true;
                        }
                        if(!placed)throw new Exception("No safe landscaping site for "+identity.StableId);
                    }
                    if(solid.Any(r=>lots.Any(l=>l.Overlaps(XzRect(r.bounds,1)))))throw new Exception("Authored lot remains obstructed");
                    string protectedHash=protection.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)||protectedHash!=protection.Validate(scene))throw new Exception("Protected scene data changed");
                }
                Directory.CreateDirectory(FixReport+"/placements");File.WriteAllText(FixReport+"/placements/citywide-landscaping.txt",string.Join("\n",journal));
                Debug.Log($"[CitywideLotLandscaping] result=Passed moved={journal.Count} buildings=unchanged roads=unchanged anchors=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
