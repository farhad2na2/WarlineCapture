using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
        static bool LandscapeLoose(MeshRenderer r)
        {
            string n=MeshName(r);
            bool natural=new[]{"Small_Rocks","Bush","Grass"}.Any(n.Contains);
            bool industrial=new[]{"Crate_Wood","Crate_Stack","Barrel_","Pallet_","Tire_","Generator_","Spool_"}.Any(n.Contains);
            float limit=natural?3:5;
            return (natural||industrial)&&r.bounds.size.x<limit&&r.bounds.size.z<limit&&r.bounds.size.y<(natural?2.5f:4.5f)&&
                !Hierarchy(r.transform).Contains("CampaignDestination")&&!Hierarchy(r.transform).Contains("CampaignMarketStock");
        }
        static int GroupCampaignLooseScenery(string map,MeshRenderer[] all,OperationMapDefinition def,RefinementProtection protection,List<string> journal)
        {
            var inventory=JsonUtility.FromJson<CampaignVisualAudit20261011.Inventory>(File.ReadAllText("Design/AgentReports/CampaignVisualAudit20261011/inventory.json"));
            var missions=inventory.missions.Where(m=>(m.sourceScene.Contains("dense_city")?Source("DenseCity"):m.sourceScene.Replace("RuntimeBinding.unity","PreparedEntities.unity"))==Source(map)).ToArray();
            var configs=missions.Select(m=>AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(m.mapPath)).ToArray();
            var anchors=configs.SelectMany(m=>m.Anchors.ToArray()).ToArray();
            var buildLots=CampaignProtectedBuildingLots(map,configs);
            var visible=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)).ToArray();
            var roots=visible.Where(LandscapeLoose).Select(DestinationParent).Where(t=>t!=null&&t.GetComponentsInChildren<MeshRenderer>(true).All(LandscapeLoose)).Distinct().OrderBy(Hierarchy).ToArray();
            var features=visible.Where(r=>(RefinementBuilding(r)||NaturalRock(r))&&r.bounds.size.y>3&&r.bounds.size.x<60&&r.bounds.size.z<60).Select(r=>(renderer:r,bounds:r.bounds,building:RefinementBuilding(r))).ToArray();
            var obstacles=visible.Where(r=>StandingScenery(r)||NaturalRock(r)||CampaignTrafficSurface(r)||NaturalWater(r)||LandscapeLoose(r)).ToArray();
            var obstacleBins=DestinationObstacleBins(obstacles.Select(r=>XzRect(r.bounds,RunwayRenderer(r)?18:.6f)).ToArray());
            var occupied=new List<Bounds>();int count=0;
            var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(def.MapSurfaceDataReference.AssetGUID));
            if(surface==null||!surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob))throw new Exception("Missing landscape support "+map);
            using(blob)
            foreach(var root in roots)
            {
                var b=DecorationBounds(root);
                if(b.min.y>1.2f||b.min.y<-.6f||!anchors.Any(a=>(a.Position-b.center).sqrMagnitude<65*65))continue;
                // Leave existing clusters alone; move only isolated ground-level fragments.
                if(features.Any(f=>Vector3.Distance(f.bounds.ClosestPoint(b.center),b.center)<8))continue;
                bool natural=root.GetComponentsInChildren<MeshRenderer>(true).Any(r=>NaturalRock(r)||MeshName(r).Contains("Bush")||MeshName(r).Contains("Grass"));
                var nearby=features.Where(f=>(natural||f.building)&&(f.bounds.center-b.center).sqrMagnitude<45*45).OrderBy(f=>(f.bounds.center-b.center).sqrMagnitude).Take(4);
                bool moved=false;
                foreach(var feature in nearby)
                {
                    var fb=feature.bounds;
                    foreach(float padding in new[]{1.2f,2.4f,3.6f})foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
                    {
                        if(moved)break;
                        var p=new Vector3(fb.center.x+sx*(fb.extents.x+b.extents.x+padding),0,fb.center.z+sz*(fb.extents.z+b.extents.z+padding));
                        if((p-new Vector3(b.center.x,0,b.center.z)).sqrMagnitude>45*45)continue;
                        var proposed=new Bounds(new Vector3(p.x,b.center.y,p.z),b.size);var area=XzRect(proposed,.25f);
                        if(area.xMin<def.Bounds.PlayableMin.x+2||area.xMax>def.Bounds.PlayableMax.x-2||area.yMin<def.Bounds.PlayableMin.z+2||area.yMax>def.Bounds.PlayableMax.z-2||buildLots.Any(l=>l.Overlaps(area)))continue;
                        if(anchors.Any(a=>a.Kind!=OperationMapAnchorKind.Camera&&(a.Position-p).sqrMagnitude<36)||occupied.Any(o=>TouchesXZ(o,proposed,.6f)))continue;
                        if(DestinationObstructed(obstacleBins,area))continue;
                        bool supported=true;float low=float.PositiveInfinity,high=float.NegativeInfinity;
                        foreach(var d in new[]{Vector2.zero,new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(1,1)})
                        {
                            var at=new float2(p.x+d.x*(b.extents.x+.2f),p.z+d.y*(b.extents.z+.2f));
                            int2 cell=(int2)math.floor((at-blob.Value.GridOrigin.xz)/blob.Value.CellSize);
                            if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample)||(sample.Flags&(MapSurfaceFlags.Road|MapSurfaceFlags.Highway|MapSurfaceFlags.Bridge))!=0||(sample.MovementMask&MapSurfaceMovementMask.Infantry)==0||sample.SlopeDegrees>8){supported=false;break;}
                            low=Mathf.Min(low,sample.Height);high=Mathf.Max(high,sample.Height);
                        }
                        if(!supported||high-low>.25f)continue;
                        var old=root.position;root.position+=new Vector3(p.x-b.center.x,high-b.min.y+.015f,p.z-b.center.z);
                        protection.Moved.Add(root.GetEntityId().ToString());EditorUtility.SetDirty(root);PrefabUtility.RecordPrefabInstancePropertyModifications(root);occupied.Add(DecorationBounds(root));
                        journal.Add("GROUP "+Hierarchy(root)+" from="+old+" to="+root.position+" beside="+Hierarchy(feature.renderer.transform));count++;moved=true;
                    }
                    if(moved)break;
                }
            }
            return count;
        }
        static int FinishCampaignCanalEnds(string map,MeshRenderer[] all,List<string> journal,List<Tuple<string,Vector3,float>> views)
        {
            var waters=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&NaturalWater(r)).ToArray();var unique=new HashSet<string>();int count=0;
            foreach(var r in waters)
            {
                var b=r.bounds;bool alongZ=b.size.z>=b.size.x;float width=alongZ?b.size.x:b.size.z;
                if(width<3||width>12||r.GetComponentsInChildren<Collider>(true).Length>0)continue;
                if(r.sharedMaterials.Any(m=>m==null||m.shader.name!="Game/Environment/CampaignWater")){journal.Add("UNCHANGED WATER SHADER "+Hierarchy(r.transform));continue;}
                if(waters.Any(w=>w!=r&&w.bounds.max.y>b.max.y+.01f&&w.bounds.min.x<=b.min.x+.5f&&w.bounds.max.x>=b.max.x-.5f&&w.bounds.min.z<=b.min.z+.5f&&w.bounds.max.z>=b.max.z-.5f))continue;
                var mf=r.GetComponent<MeshFilter>();if(mf?.sharedMesh==null||AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith(FixArt+"/CanalEndsV2/"))continue;
                bool Terminal(bool min)=>!waters.Any(w=>w!=r&&Mathf.Abs((alongZ?(min?w.bounds.max.z-b.min.z:w.bounds.min.z-b.max.z):(min?w.bounds.max.x-b.min.x:w.bounds.min.x-b.max.x)))<.7f&&Mathf.Abs(alongZ?w.bounds.center.x-b.center.x:w.bounds.center.z-b.center.z)<1);
                bool start=Terminal(true),end=Terminal(false);if(!start&&!end)continue;
                var original=mf.sharedMesh;if(original.subMeshCount!=1)throw new Exception("Canal finish expects one water material");
                var mesh=new Mesh{name=original.name,indexFormat=original.indexFormat};var vertices=original.vertices.ToList();var colors=original.colors.ToList();var triangles=original.triangles.ToList();
                if(colors.Count==0)colors=Enumerable.Repeat(new Color(1,1,1,0),vertices.Count).ToList();
                if(colors.Count!=vertices.Count)throw new Exception("Canal finish requires authored bank colors");
                void Quad(Vector3 a,Vector3 c,Vector3 d,Vector3 e,Color shade,Vector3 normal)
                {if(Vector3.Dot(Vector3.Cross(c-a,d-a),normal)<0){(a,e)=(e,a);(c,d)=(d,c);}int n=vertices.Count;vertices.AddRange(new[]{a,c,d,e}.Select(r.transform.InverseTransformPoint));colors.AddRange(Enumerable.Repeat(shade,4));triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
                void Cap(bool min)
                {
                    float edge=alongZ?(min?b.min.z:b.max.z):(min?b.min.x:b.max.x),direction=min?1:-1;
                    Vector3 P(float across,float inward,float y)=>alongZ?new Vector3(b.center.x+across,y,edge+direction*inward):new Vector3(edge+direction*inward,y,b.center.z+across);
                    float y=b.max.y+.08f;var concrete=new Color(.46f,.44f,.38f,1);var recess=new Color(.14f,.17f,.15f,1);
                    // A maintenance headwall closes the existing channel inside its authored footprint.
                    var a=P(-width*.5f,.02f,y);var c=P(-width*.5f,1.15f,y);var d=P(width*.5f,1.15f,y);var e=P(width*.5f,.02f,y);
                    Quad(a,c,d,e,concrete,Vector3.up);
                    var low=Vector3.down*.28f;var inward=alongZ?new Vector3(0,0,direction):new Vector3(direction,0,0);Quad(c,d,d+low,c+low,new Color(.29f,.30f,.27f,1),inward);Quad(a,e,e+low,a+low,concrete,-inward);
                    for(float across=-width*.5f+.5f;across<width*.5f-.3f;across+=.45f)
                    {a=P(across,.28f,y+.003f);c=P(across,.85f,y+.003f);d=P(across+.12f,.85f,y+.003f);e=P(across+.12f,.28f,y+.003f);Quad(a,c,d,e,recess,Vector3.up);}
                    views.Add(Tuple.Create("canal-end-"+count,P(0,0,y),38f));count++;journal.Add("CANAL HEADWALL "+Hierarchy(r.transform)+" end="+P(0,0,y));
                }
                if(start)Cap(true);if(end)Cap(false);mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                string id=TextHash(GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).Substring(0,20);string assetPath=FixArt+"/CanalEndsV2/"+map+"_"+id+".asset";if(!unique.Add(assetPath))throw new Exception("Canal instance asset collision");
                mf.sharedMesh=SaveVisualMesh(assetPath,mesh);mf.sharedMesh.name=original.name;EditorUtility.SetDirty(mf.sharedMesh);AssetDatabase.SaveAssetIfDirty(mf.sharedMesh);
                r.sharedMaterials=r.sharedMaterials.Select((m,i)=>{if(m.GetFloat("_BankColors")>.5f)return m;string path=FixArt+"/CanalEndsV2/"+map+"_"+id+"_"+i+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(m);AssetDatabase.CreateAsset(material,path);}material.SetFloat("_BankColors",1);Save(material);return material;}).ToArray();EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
            }
            return count;
        }
        public static void BuildCampaignLandscapeFinish()
        {
            Check();Directory.CreateDirectory(FixReport+"/landscape");var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in VisualMaps)
                {
                    string receipt=FixReport+"/landscape/"+map+".txt";
                    if(File.Exists(receipt))throw new Exception("Landscape pass already has evidence; inspect before repeating "+map);
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);var rs=SceneRenderers(scene);var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));var protection=new RefinementProtection(scene);var journal=new List<string>();var views=NaturalViews(rs.Where(r=>Within(r,def)).ToArray(),def).ToList();
                    int restoredCurbs=RestoreOccludedCampaignCurbs(rs,journal);
                    int grouped=GroupCampaignLooseScenery(map,rs,def,protection,journal),caps=FinishCampaignCanalEnds(map,rs,journal,views),roadEnds=FinishCampaignRoadTerminals(map,rs,def,journal,views);
                    string hash=protection.Validate(scene);if(grouped+caps+roadEnds+restoredCurbs>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Landscape source invariant failed "+map);}
                    File.WriteAllLines(receipt,journal.Concat(new[]{"restoredOccludedCurbs="+restoredCurbs+" grouped="+grouped+" roadEnds="+roadEnds+" canalEnds="+caps+" protectedHash="+hash}));
                    Capture(map,scene,def,rs,"after",FixReport+"/landscape",views.ToArray());
                    Debug.Log("[CampaignLandscapeFinish] map="+map+" grouped="+grouped+" roadEnds="+roadEnds+" canalEnds="+caps);
                }
                Debug.Log("[CampaignLandscapeFinish] result=Passed maps=11");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
