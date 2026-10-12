using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static bool CampaignTrafficSurface(MeshRenderer r)=>r.name=="Asphalt"||MeshName(r).Contains("DirtRoad")||MeshName(r).Contains("Sidewalk")||RunwayRenderer(r);
        static int RepairCampaignDestinationRoadClearance(string map,MeshRenderer[] rs,OperationMapDefinition def,RefinementProtection protection,List<string> journal,List<Tuple<string,Vector3,float>> views)
        {
            var groups=rs.SelectMany(r=>r.GetComponentsInParent<Transform>(true)).Where(t=>t.name.StartsWith("CampaignDestinationV2_20261011_",StringComparison.Ordinal)).Distinct().OrderBy(t=>t.name).ToArray();
            var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)).ToArray();
            var roadBins=DestinationObstacleBins(visible.Where(r=>CampaignTrafficSurface(r)||NaturalWater(r)).Select(r=>XzRect(r.bounds,.5f)).ToArray());
            var obstacleBins=DestinationObstacleBins(visible.Where(r=>!Hierarchy(r.transform).Contains("/CampaignDestination")&&(StandingScenery(r)||IsBuilding(r)||NaturalRock(r)||CampaignTrafficSurface(r)||NaturalWater(r))).Select(r=>XzRect(r.bounds,RunwayRenderer(r)?18:.7f)).ToArray());
            var inventory=JsonUtility.FromJson<CampaignVisualAudit20261011.Inventory>(File.ReadAllText("Design/AgentReports/CampaignVisualAudit20261011/inventory.json"));
            var missions=inventory.missions.Where(m=>(m.sourceScene.Contains("dense_city")?Source("DenseCity"):m.sourceScene.Replace("RuntimeBinding.unity","PreparedEntities.unity"))==Source(map)).ToArray();
            var configs=missions.ToDictionary(m=>m.index,m=>AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(m.mapPath));
            var lots=CampaignProtectedBuildingLots(map,configs.Values);
            var ground=visible.Where(r=>MeshName(r).StartsWith("Ground_")||MeshName(r).StartsWith("SM_Env_Ground_")||MeshName(r).StartsWith("SM_Env_SandDunes_")).ToArray();
            var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(def.MapSurfaceDataReference.AssetGUID));
            if(surface==null||!surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob))throw new Exception("Missing destination repair surface");int moved=0;
            using(blob)
            foreach(var group in groups)
            {
                var bounds=DecorationBounds(group);if(!DestinationObstructed(roadBins,XzRect(bounds,.1f))&&!lots.Any(l=>l.Overlaps(XzRect(bounds,.1f))))continue;
                var site=DestinationSites.Single(s=>group.name=="CampaignDestinationV2_20261011_"+s.mission+"_"+s.suffix);var config=configs[site.mission];var anchor=config.Anchors.ToArray().Single(a=>a.AnchorId.EndsWith("."+site.suffix,StringComparison.Ordinal));
                var occupied=groups.Where(g=>g!=group).Select(g=>XzRect(DecorationBounds(g),1)).ToArray();bool placed=false;int rejectedSpace=0,rejectedGround=0;var groundDiagnostics=new List<string>();
                foreach(var p in DestinationCandidates(anchor.Position,true))
                {
                    var proposed=new Bounds(new Vector3(p.x,bounds.center.y,p.z),bounds.size);var area=XzRect(proposed,.4f);
                    if(area.xMin<config.Bounds.PlayableMin.x+2||area.xMax>config.Bounds.PlayableMax.x-2||area.yMin<config.Bounds.PlayableMin.z+2||area.yMax>config.Bounds.PlayableMax.z-2||lots.Any(l=>l.Overlaps(area))||occupied.Any(l=>l.Overlaps(area))||DestinationObstructed(obstacleBins,area)){rejectedSpace++;continue;}
                    bool valid=true;float min=float.PositiveInfinity,max=float.NegativeInfinity;
                    foreach(var d in new[]{Vector2.zero,new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(1,1)})
                    {
                        var at=new Vector3(p.x+d.x*(bounds.extents.x+.2f),0,p.z+d.y*(bounds.extents.z+.2f));int2 cell=(int2)math.floor((new float2(at.x,at.z)-blob.Value.GridOrigin.xz)/blob.Value.CellSize);
                        if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample)){groundDiagnostics.Add(p+" missing grid");valid=false;break;}
                        if((sample.Flags&(MapSurfaceFlags.Road|MapSurfaceFlags.Highway|MapSurfaceFlags.Bridge))!=0||(sample.MovementMask&MapSurfaceMovementMask.Infantry)==0||sample.SlopeDegrees>6){groundDiagnostics.Add(p+" flags="+sample.Flags+" movement="+sample.MovementMask+" slope="+sample.SlopeDegrees);valid=false;break;}
                        if(!RunwayGroundSupport(ground,at,sample.Height,out float h)){groundDiagnostics.Add(p+" missing mesh support reference="+sample.Height);valid=false;break;}
                        min=Mathf.Min(min,h);max=Mathf.Max(max,h);
                    }
                    float allowedRelief=map=="DenseCity"?.35f:.15f;
                    if(!valid||max-min>allowedRelief){if(valid)groundDiagnostics.Add(p+" uneven ground delta="+(max-min));rejectedGround++;continue;}
                    var before=group.position;group.position+=new Vector3(p.x-bounds.center.x,max+.025f-bounds.min.y,p.z-bounds.center.z);protection.Moved.Add(group.GetEntityId().ToString());EditorUtility.SetDirty(group);PrefabUtility.RecordPrefabInstancePropertyModifications(group);
                    if(max-min>.15f)
                    {
                        var foundation=GameObject.CreatePrimitive(PrimitiveType.Cube);foundation.name="CampaignDestinationStoneFoundation";
                        UnityEngine.Object.DestroyImmediate(foundation.GetComponent<Collider>());
                        foundation.transform.position=new Vector3(p.x,(min+max)/2,p.z);
                        foundation.transform.localScale=new Vector3(bounds.size.x+.4f,max-min+.05f,bounds.size.z+.4f);
                        foundation.transform.SetParent(group,true);foundation.isStatic=true;protection.NewPresentationRoots.Add(foundation.transform);
                        string materialPath=FixArt+"/DestinationPads/DenseCity_StoneFoundation.mat";
                        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if(material==null)
                        {
                            var original=ground.SelectMany(r=>r.sharedMaterials).First(m=>m!=null&&m.shader.name=="Game/Environment/GroundMacroVariation");
                            material=new Material(original);material.SetTexture("_DistrictSurfaceMap",null);material.SetVector("_DistrictSurfaceConstant",new Vector4(0,1,0,0));material.SetFloat("_DistrictSurfaceStrength",1);
                            AssetDatabase.CreateAsset(material,materialPath);AssetDatabase.SaveAssetIfDirty(material);
                        }
                        foundation.GetComponent<MeshRenderer>().sharedMaterial=material;
                        journal.Add("GROUNDED STONE FOUNDATION "+group.name+" relief="+(max-min)+" bounds="+foundation.GetComponent<MeshRenderer>().bounds+" addedMaterialRows=1");
                    }
                    journal.Add("RELOCATED DESTINATION FROM ROAD OR FUNCTIONAL LOT "+group.name+" from="+before+" to="+group.position);views.Add(Tuple.Create("clear-"+site.mission+"-"+site.suffix,group.position,45f));placed=true;moved++;break;
                }
                if(!placed)File.WriteAllLines(FixReport+"/destination-ground-failure-"+map+"-"+site.mission+".txt",groundDiagnostics);
                if(!placed)throw new Exception("Destination has no clear authored-road frontage: "+map+" "+group.name+" bounds="+bounds+" anchor="+anchor.Position+" spaceRejected="+rejectedSpace+" groundRejected="+rejectedGround);
            }
            foreach(var group in groups)if(DestinationObstructed(roadBins,XzRect(DecorationBounds(group),.1f))||lots.Any(l=>l.Overlaps(XzRect(DecorationBounds(group),.1f))))throw new Exception("Destination still overlaps visible road "+group.name);
            if(moved>0)BuildDestinationPads(map,rs,def,groups.Select(g=>(DecorationBounds(g),DestinationSites.Single(s=>g.name=="CampaignDestinationV2_20261011_"+s.mission+"_"+s.suffix).kind)).ToList());
            journal.Add("destinationRoadClearance=Passed groups="+groups.Length+" relocated="+moved);return moved;
        }
    }
}
