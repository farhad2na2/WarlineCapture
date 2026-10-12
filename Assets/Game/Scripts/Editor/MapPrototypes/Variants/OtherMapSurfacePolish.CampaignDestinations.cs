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
        sealed class DestinationSite { public int mission;public string suffix,kind;public DestinationSite(int m,string s,string k){mission=m;suffix=s;kind=k;} }
        static readonly DestinationSite[] DestinationSites={
            new(1,"comms_focus","camp"),new(2,"ground_sensor","service"),new(3,"specialists","airfield"),
            new(4,"archive","records"),new(5,"hospital","aid"),new(5,"site_a","repair"),new(5,"site_b","repair"),
            new(6,"oil","fuel"),new(6,"refinery","service"),new(6,"storage","fuel"),
            new(7,"manifest","market"),new(7,"corrupt_manifest","market"),new(7,"delivery","market"),
            new(8,"repair","service"),new(8,"shelter","domestic"),new(9,"relief_goal","aid"),new(9,"fuel_goal","fuel"),new(9,"records","records"),
            new(10,"manifest","market"),new(11,"delivery","domestic"),new(12,"delivery","domestic"),new(13,"specialists","aid"),
            new(14,"core","service"),new(15,"ground_sensor","service"),new(16,"civilians","aid"),new(17,"civilians","domestic"),
            new(18,"relay","service"),new(18,"civilian_terminal","domestic"),new(19,"relief","aid"),new(19,"radar","service"),
            new(20,"clinic","aid"),new(20,"utility","service"),new(21,"north_shelter","domestic"),new(21,"south_shelter","domestic"),new(21,"relay","service"),
            // All three unresolved nodes get the same neutral service dressing; no suspect is disclosed.
            new(22,"node_1","service"),new(22,"node_2","service"),new(22,"node_3","service"),new(22,"audit_gate","records"),
            new(23,"medicine_receiver","aid"),new(23,"fuel_receiver","fuel"),new(23,"key_receiver","records"),
            new(24,"clinic","aid"),new(24,"utility","service"),new(24,"core_audit","service"),new(24,"audit_release","records")
        };
        static GameObject CreateDestinationGroup(string name,string kind)
        {
            var root=new GameObject(name);
            Bounds Add(string asset,float x,float z,float yaw=0,float maxSize=6,float baseY=0)
            {
                string path="Assets/PolygonMilitary/Prefabs/"+(asset.StartsWith("Buildings/")?asset:"Props/"+asset)+".prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new Exception("Missing destination prop "+path);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
                foreach(var c in go.GetComponentsInChildren<Component>(true))
                    if(c!=null&&!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer)&&!(c is LODGroup))UnityEngine.Object.DestroyImmediate(c);
                go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;
                var b=DecorationBounds(go.transform);float size=Mathf.Max(b.size.x,b.size.z);
                if(size>maxSize)go.transform.localScale*=maxSize/size;
                b=DecorationBounds(go.transform);go.transform.position+=new Vector3(x-b.center.x,baseY-b.min.y,z-b.center.z);
                foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
                return DecorationBounds(go.transform);
            }
            switch(kind)
            {
                case "aid":
                    Add("Buildings/SM_Bld_Tent_Cover_01",0,1,0,6);
                    var table=Add("SM_Prop_Table_Outdoor_01",0,0,0,2.5f);
                    Add("Military/SM_Prop_MedicalBox_01",-.45f,0,0,1,table.max.y+.015f);
                    Add("Military/SM_Prop_MedicalBox_01",.45f,0,0,1,table.max.y+.015f);
                    Add("SM_Prop_WaterTank_02",4,1,0,2.5f);Add("SM_Prop_Crate_Wood_02",-4,.7f,12,2);
                    Add("SM_Prop_Chair_Folding_01",0,-2,180,1.3f);break;
                case "airfield":
                    Add("Buildings/SM_Bld_Tent_Cover_01",0,1,0,6);
                    Add("SM_Prop_Chair_Folding_01",-1,1,180,1.3f);Add("SM_Prop_Chair_Folding_01",1,1,180,1.3f);
                    Add("Military/SM_Prop_Crate_Stack_01",4,1,0,2.5f);Add("SM_Prop_Cone_01",-3,-2);Add("SM_Prop_Cone_01",3,-2);break;
                case "service": case "repair":
                    Add("SM_Prop_Generator_Large_01",0,1,0,4.3f);Add("SM_Prop_Cabinet_01",3.1f,1,180,2);
                    Add("SM_Prop_PowerBox_02",-3.1f,1,0,2);Add("SM_Prop_Cable_01",1,-1,25,1.5f);
                    Add("SM_Prop_Crate_Wood_02",-2,-1.8f,9,1.8f);Add("SM_Prop_Cone_01",3,-2);break;
                case "fuel":
                    Add("SM_Prop_Gaspump_01",-2,1,0,2);Add("Military/SM_Prop_Barrel_Stack_01",1,1,0,3);
                    Add("SM_Prop_Pallet_02",3.5f,-1,0,2);Add("SM_Prop_GasCan_01",-.6f,-1.5f,20,1);
                    Add("SM_Prop_GasCan_01",.3f,-1.5f,-10,1);Add("SM_Prop_Cone_01",-3,-1.5f);break;
                case "records":
                    Add("SM_Prop_Umbrella_01",0,1,0,4.5f);Add("SM_Prop_Table_Outdoor_01",0,0,0,2.5f);
                    Add("SM_Prop_Cabinet_01",-2.5f,1,0,2);Add("SM_Prop_Crate_Wood_02",2.5f,1,10,2);
                    Add("SM_Prop_Chair_Folding_01",0,-1.5f,180,1.3f);break;
                case "market":
                    Add("SM_Prop_Cart_Stall_01",-2.4f,1,0,3.4f);Add("SM_Prop_Cart_Stall_01",2.4f,1,0,3.4f);
                    Add("SM_Prop_Basket_03",-2,-1.5f);Add("SM_Prop_Pot_01",-3.2f,-1.2f);Add("SM_Prop_Basket_03",2,-1.5f);
                    Add("Rugs/SM_Prop_Rug_Rolls_01",3.1f,-1.2f,10,1.5f);Add("SM_Prop_Crate_Wood_02",0,2.3f,0,1.3f);break;
                case "camp":
                    Add("Military/SM_Prop_Crate_Stack_Cover_01",-2,1,0,3);Add("SM_Prop_Table_Metal",1,1,0,2.5f);
                    Add("Military/SM_Prop_Bed_SleepingBag_Roll_01",-2,-1.5f);Add("Military/SM_Prop_Bed_SleepingBag_Roll_01",-1,-1.5f);
                    Add("SM_Prop_Chair_Folding_01",1,-1,180,1.3f);break;
                default:
                    Add("SM_Prop_Umbrella_02",0,1,0,4.5f);Add("SM_Prop_Table_Outdoor_01",0,0,0,2.5f);
                    Add("SM_Prop_Chair_Plastic_01",-1.5f,0,90,1.3f);Add("SM_Prop_Chair_Plastic_01",1.5f,0,-90,1.3f);
                    Add("SM_Prop_WaterTank_02",3.5f,1,0,2.5f);Add("SM_Prop_Basket_03",-3,-1);Add("Rugs/SM_Prop_Rug_Rolls_01",-3,1,12,1.5f);break;
            }
            var bounds=DecorationBounds(root.transform);
            foreach(Transform child in root.transform)child.position-=new Vector3(bounds.center.x,0,bounds.center.z);
            root.isStatic=true;return root;
        }
        public static void BuildCampaignDestinationDressing()=>BuildCampaignDestinationDressingFor(VisualMaps,false);
        public static void BuildCampaignMissingDestinationDressing()
        {
            var maps=VisualMaps.Where(m=>File.Exists(FixReport+"/destinations/"+m+".txt")&&File.ReadAllText(FixReport+"/destinations/"+m+".txt").Contains("UNRESOLVED")).ToArray();
            BuildCampaignDestinationDressingFor(maps,true);
            foreach(var map in maps)if(File.ReadAllText(FixReport+"/destinations/"+map+".txt").Contains("UNRESOLVED"))throw new Exception("Destination remains unresolved "+map);
            Debug.Log("[CampaignMissingDestinations] result=Passed maps="+maps.Length);
        }
        static void BuildCampaignDestinationDressingFor(IEnumerable<string> selectedMaps,bool compactMissing)
        {
            Check();Directory.CreateDirectory(FixReport+"/destinations-v2");var setup=EditorSceneManager.GetSceneManagerSetup();int total=0;
            var inventory=JsonUtility.FromJson<CampaignVisualAudit20261011.Inventory>(File.ReadAllText("Design/AgentReports/CampaignVisualAudit20261011/inventory.json"));
            try
            {
                foreach(string mapName in selectedMaps)
                {
                    string source=Source(mapName);
                    var missions=inventory.missions.Where(m=>(m.sourceScene.Contains("dense_city")?Source("DenseCity"):m.sourceScene.Replace("RuntimeBinding.unity","PreparedEntities.unity"))==source).ToArray();
                    var sites=DestinationSites.Where(s=>missions.Any(m=>m.index==s.mission)).ToArray();if(sites.Length==0)continue;
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    // Replace only this task's unaccepted pilot groups; verify they contain presentation alone.
                    foreach(var pilot in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("CampaignDestination20261011_",StringComparison.Ordinal)).ToArray())
                    {
                        if(pilot.GetComponentsInChildren<Component>(true).Any(c=>c==null||!(c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup)))throw new Exception("Pilot group acquired nonvisual ownership");
                        UnityEngine.Object.DestroyImmediate(pilot.gameObject);
                    }
                    var rs=SceneRenderers(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)).ToArray();
                    var protection=new RefinementProtection(scene);var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(mapName));
                    var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(def.MapSurfaceDataReference.AssetGUID));
                    if(surface==null||!surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob))throw new Exception("Missing destination ground "+mapName);
                    var obstacles=rs.Where(r=>StandingScenery(r)||IsBuilding(r)||CampaignTrafficSurface(r)||NaturalWater(r)).ToArray();
                    var roots=rs.Where(r=>!Hierarchy(r.transform).Contains("/CampaignDestination")&&!Hierarchy(r.transform).Contains("/CampaignMarketStock")).Select(DestinationParent).Where(t=>t!=null).Distinct().Where(t=>t.name.StartsWith("SM_Prop_",StringComparison.Ordinal) && t.GetComponentsInChildren<MeshRenderer>(true).All(r=>!IsSurface(r)&&!IsBuilding(r)&&!RunwayRenderer(r))).ToArray();
                    var obstacleRects=obstacles.Select(o=>XzRect(o.bounds,RunwayRenderer(o)?18:1)).ToArray();
                    var obstacleBins=DestinationObstacleBins(obstacleRects);
                    var parentCandidates=roots.Select(t=>(root:t,position:t.position)).ToArray();
                    var existingGroups=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("CampaignDestinationV2_20261011_",StringComparison.Ordinal)).ToDictionary(t=>t.name);
                    var configs=missions.Select(m=>AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(m.mapPath)).ToArray();
                    var buildLots=CampaignProtectedBuildingLots(mapName,configs);
                    var pads=new List<(Bounds bounds,string kind)>();var occupied=new List<Bounds>();var journal=new List<string>();var views=new List<Tuple<string,Vector3,float>>();
                    using(blob)
                    foreach(var site in sites)
                    {
                        var mission=missions.Single(m=>m.index==site.mission);var config=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(mission.mapPath);
                        var matches=config.Anchors.ToArray().Where(a=>a.AnchorId.EndsWith("."+site.suffix,StringComparison.Ordinal)).ToArray();if(matches.Length!=1)throw new Exception("Ambiguous destination "+mission.missionId+" "+site.suffix);
                        var anchor=matches[0];string groupName="CampaignDestinationV2_20261011_"+site.mission+"_"+site.suffix;
                        existingGroups.TryGetValue(groupName,out var existing);
                        if(existing!=null){var eb=DecorationBounds(existing);occupied.Add(eb);pads.Add((eb,site.kind));journal.Add(groupName+" retained verified existing group");continue;}
                        // Shared physical sites receive one consistent group across mission consumers.
                        if(pads.Any(p=>p.kind==site.kind&&(p.bounds.center-anchor.Position).sqrMagnitude<144)){journal.Add(groupName+" uses neighboring shared group");continue;}
                        bool frontage=mapName=="DenseCity"&&compactMissing;
                        var group=frontage?CreateFrontageDestination(groupName,site.kind):mapName=="DenseCity"||compactMissing?CreateCompactDestination(groupName,site.kind):CreateDestinationGroup(groupName,site.kind);var bounds=DecorationBounds(group.transform);bool placed=false;int rejectedBounds=0,rejectedObstacles=0,rejectedParent=0,rejectedSurface=0;
                        foreach(var p in DestinationCandidates(anchor.Position,mapName=="DenseCity"||compactMissing))
                        {
                            if(placed)break;
                            var proposed=new Bounds(new Vector3(p.x,bounds.extents.y,p.z),bounds.size);var rect=XzRect(proposed,frontage?.35f:.8f);
                            if(rect.xMin<config.Bounds.PlayableMin.x+2||rect.xMax>config.Bounds.PlayableMax.x-2||rect.yMin<config.Bounds.PlayableMin.z+2||rect.yMax>config.Bounds.PlayableMax.z-2||buildLots.Any(l=>l.Overlaps(rect))){rejectedBounds++;continue;}
                            if(DestinationObstructed(obstacleBins,rect)||occupied.Any(b=>XzRect(b,3).Overlaps(rect))){rejectedObstacles++;continue;}
                            float parentRadius=mapName=="CH04M05ArmorBreak"?90:35;
                            var parent=parentCandidates.Where(t=>(t.position-p).sqrMagnitude<parentRadius*parentRadius).OrderBy(t=>(t.position-p).sqrMagnitude).Select(t=>t.root).FirstOrDefault();if(parent==null){rejectedParent++;continue;}
                            bool supported=true;float low=float.PositiveInfinity,high=float.NegativeInfinity;
                            for(int z=Mathf.FloorToInt(rect.yMin);z<Mathf.CeilToInt(rect.yMax)&&supported;z++)
                            for(int x=Mathf.FloorToInt(rect.xMin);x<Mathf.CeilToInt(rect.xMax);x++)
                            {
                                int2 cell=(int2)math.floor((new float2(x+.5f,z+.5f)-blob.Value.GridOrigin.xz)/blob.Value.CellSize);
                                if(!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample)||(sample.Flags&(MapSurfaceFlags.Road|MapSurfaceFlags.Highway|MapSurfaceFlags.Bridge))!=0||(sample.MovementMask&MapSurfaceMovementMask.Infantry)==0||sample.SlopeDegrees>6){supported=false;break;}
                                low=Mathf.Min(low,sample.Height);high=Mathf.Max(high,sample.Height);
                            }
                            if(!supported||high-low>.15f){rejectedSurface++;continue;}
                            group.transform.position=new Vector3(p.x,high+.025f,p.z);group.transform.SetParent(parent,true);protection.NewPresentationRoots.Add(group.transform);
                            occupied.Add(DecorationBounds(group.transform));pads.Add((DecorationBounds(group.transform),site.kind));journal.Add(groupName+" kind="+site.kind+" anchor="+anchor.Position+" group="+group.transform.position+" owner="+Hierarchy(parent));
                            views.Add(Tuple.Create(site.mission+"-"+site.suffix,group.transform.position,45f));placed=true;total++;
                        }
                        if(!placed){UnityEngine.Object.DestroyImmediate(group);journal.Add(groupName+" UNRESOLVED: no verified clear off-road site near destination; bounds="+rejectedBounds+" scenery="+rejectedObstacles+" parent="+rejectedParent+" ground="+rejectedSurface);}
                    }
                    if(views.Count>0)BuildDestinationPads(mapName,rs,def,pads);
                    string hash=protection.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Destination source invariant failed "+mapName);
                    File.WriteAllLines(FixReport+"/destinations/"+mapName+".txt",journal);Capture(mapName,scene,def,SceneRenderers(scene),"after",FixReport+"/destinations-v2",views.ToArray());
                    Debug.Log("[CampaignDestinationDressing] map="+mapName+" groups="+views.Count+" unresolved="+journal.Count(l=>l.Contains("UNRESOLVED")));
                }
                Debug.Log("[CampaignDestinationDressing] result=Passed groups="+total+" requiresPerSiteVisualReview=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
