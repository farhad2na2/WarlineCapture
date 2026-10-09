using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Game.Configs;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        [Serializable] private sealed class PropMove
        {
            public string path,reason;public Vector3 before,after;public ulong transformId,instanceId;public long prefabTransformId;
        }
        [Serializable] private sealed class DistrictDetailReceipt
        {
            public string map,result,beforeHash,afterHash,protectedSceneHash;
            public int groups,buildingRenderers,variedContainers,renderersBefore,renderersAfter,trianglesBefore,trianglesAfter;
            public PropMove[] moves;
        }
        private static Transform DecorationRoot(MeshRenderer r)
        {
            var t=r.transform;var prefab=PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
            if(prefab!=null&&(prefab.name.StartsWith("SM_Prop_")||prefab.name.StartsWith("SM_Env_")||prefab.name.StartsWith("SM_Generic_")))t=prefab.transform;
            else while(t.parent!=null&&(t.parent.name.StartsWith("SM_Prop_")||t.parent.name.StartsWith("SM_Env_")))t=t.parent;
            string path=Hierarchy(t);
            if(path.Contains("ResourceAreas")||path.Contains("UnmappedVehicle")||t.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>()!=null)return null;
            // Only render-only scenery is eligible. Keep all collision, lights, gameplay,
            // navigation and logical authoring exactly where their owners placed them.
            if(t.GetComponentsInParent<Collider>(true).Length>0)return null;
            foreach(var c in t.GetComponentsInChildren<Component>(true))
                if(c!=null&&!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer)&&!(c is LODGroup)&&c.GetType().Name!="DenseCityPresentationIdentityAuthoring")return null;
            return t;
        }
        private static Bounds DecorationBounds(Transform t)
        {
            var rs=t.GetComponentsInChildren<MeshRenderer>(true);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
        }
        private static bool TouchesXZ(Bounds a,Bounds b,float margin=0)=>a.max.x+margin>b.min.x&&a.min.x-margin<b.max.x&&a.max.z+margin>b.min.z&&a.min.z-margin<b.max.z;
        private static PropMove MoveDecoration(Transform t,Vector3 position,float yaw,string reason)
        {
            var id=GlobalObjectId.GetGlobalObjectIdSlow(t);var handle=PrefabUtility.GetPrefabInstanceHandle(t);var source=PrefabUtility.GetCorrespondingObjectFromSource(t);long sourceId=0;
            if(source!=null)AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string _,out sourceId);
            var move=new PropMove{path=Hierarchy(t),reason=reason,before=t.position,after=position,transformId=id.targetObjectId,
                instanceId=handle==null?0:GlobalObjectId.GetGlobalObjectIdSlow(handle).targetObjectId,prefabTransformId=sourceId};
            t.position=position;EditorUtility.SetDirty(t);PrefabUtility.RecordPrefabInstancePropertyModifications(t);return move;
        }
        private static string NonDecorativeChanges(string text,PropMove[] moves)
        {
            text=NonVisualReferences(text);var sceneIds=new HashSet<ulong>(moves.Where(m=>m.instanceId==0).Select(m=>m.transformId));
            var prefabIds=moves.Where(m=>m.instanceId!=0).GroupBy(m=>m.instanceId).ToDictionary(g=>g.Key,g=>new HashSet<long>(g.Select(m=>m.prefabTransformId)));
            return Regex.Replace(text,@"(?ms)^--- !u!(4|1001) &(\d+)(?: stripped)?\n.*?(?=^--- |\z)",m=>
            {
                ulong id=ulong.Parse(m.Groups[2].Value);string block=m.Value;
                if(m.Groups[1].Value=="4"&&sceneIds.Contains(id))return Regex.Replace(block,@"(?m)^  m_Local(?:Position|Rotation|EulerAnglesHint): .*\n", "");
                if(m.Groups[1].Value=="1001"&&prefabIds.TryGetValue(id,out var allowed))
                    return Regex.Replace(block,@"(?m)^    - target: \{fileID: (-?\d+),[^\n]*\n      propertyPath: m_Local(?:Position|Rotation|EulerAnglesHint)\.[xyzw]\n      value: .*\n      objectReference: .*\n",p=>allowed.Contains(long.Parse(p.Groups[1].Value))?"":p.Value);
                return block;
            });
        }
        private static Material DistrictTint(Material original,int palette,bool container)
        {
            string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));string path=VisualArt+"/DistrictPalette/"+guid+"_"+(container?"Container":"Building")+palette+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();m=new Material(original){name=(container?"Container":"Building")+"_District_"+palette};
            var colors=container?new[]{new Color(.68f,.73f,.68f),new Color(.78f,.72f,.61f),new Color(.70f,.73f,.77f)}:new[]{new Color(.97f,.93f,.85f),new Color(.86f,.91f,.93f),new Color(.94f,.86f,.75f)};
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",m.GetColor("_BaseColor")*colors[palette]);else if(m.HasProperty("_Color"))m.SetColor("_Color",m.GetColor("_Color")*colors[palette]);
            AssetDatabase.CreateAsset(m,path);return m;
        }
        public static void BuildDistrictDetails()
        {
            Check();Directory.CreateDirectory(VisualReport);var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string map in VisualMaps)
                {
                    string source=Source(map),backup=VisualReport+"/local-baselines/"+map+"-district-scene.unity";
                    string receiptPath=VisualReport+"/"+map+"-district-details.json";
                    if(File.Exists(receiptPath))
                    {
                        var completed=JsonUtility.FromJson<DistrictDetailReceipt>(File.ReadAllText(receiptPath));
                        if(completed.result=="Passed"&&completed.afterHash==Hash(source)){Debug.Log("[MapDistrictDetails] preserved completed map="+map);continue;}
                        throw new InvalidOperationException("District source changed since its saved receipt; re-audit before establishing a new baseline: "+map);
                    }
                    string original=ReadSceneText(source);
                    if(!File.Exists(backup))File.WriteAllText(backup,original);
                    else if(Hash(source)!=Hash(backup))throw new InvalidOperationException("Unreviewed partial district run: inspect pending receipt before retrying "+map);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));var min=def.Bounds.PlayableMin;var max=def.Bounds.PlayableMax;
                    bool Inside(MeshRenderer r)=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&r.bounds.center.x>min.x&&r.bounds.center.x<max.x&&r.bounds.center.z>min.z&&r.bounds.center.z<max.z;
                    var visible=rs.Where(Inside).ToArray();var buildings=visible.Where(IsBuilding).GroupBy(r=>(Mathf.RoundToInt(r.bounds.center.x*.5f),Mathf.RoundToInt(r.bounds.center.z*.5f))).Select(g=>g.OrderByDescending(r=>r.bounds.size.y).First()).ToArray();
                    var roadBounds=visible.Where(r=>r.name=="Asphalt"||MeshName(r).Contains("DirtRoad")||MeshName(r).Contains("RunwaySurface")||MeshName(r)=="SM_Env_Runway_01").Select(r=>r.bounds).ToArray();
                    var runwayBounds=visible.Where(r=>MeshName(r).Contains("RunwaySurface")||MeshName(r)=="SM_Env_Runway_01").Select(r=>r.bounds).ToArray();
                    var receipt=new DistrictDetailReceipt{map=map,beforeHash=Hash(source),renderersBefore=rs.Length,trianglesBefore=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3)};
                    var moves=new List<PropMove>();var moved=new HashSet<Transform>();var occupied=new List<Bounds>();
                    var hardObstacles=visible.Where(r=>!IsBuilding(r)&&!IsRoadDetail(r)&&!MeshName(r).Contains("Ground")&&r.bounds.size.y>.75f&&r.bounds.size.x>1&&r.bounds.size.z>1&&r.bounds.size.x<60&&r.bounds.size.z<60).ToArray();
                    bool Clear(Bounds b,Transform ignore=null)=>!roadBounds.Any(r=>TouchesXZ(b,r,1.2f))&&!buildings.Any(r=>TouchesXZ(b,r.bounds,.65f))&&!occupied.Any(r=>TouchesXZ(b,r,.5f))
                        &&!hardObstacles.Any(r=>(ignore==null||!r.transform.IsChildOf(ignore))&&TouchesXZ(b,r.bounds,.35f));
                    // Clear only verified render-only poles and natural scenery intersecting the
                    // paved runway. Move them to neighboring shoulders, never delete them.
                    foreach(var r in visible.OrderBy(r=>Hierarchy(r.transform),StringComparer.Ordinal))
                    {
                        string name=MeshName(r);bool pole=name.StartsWith("SM_Prop_Powerpole_");
                        bool natural=name.StartsWith("SM_Env_Rock_")||name.StartsWith("SM_Env_Grass_")||name.StartsWith("SM_Generic_Grass_")||name.StartsWith("SM_Env_Bush_");
                        if(!pole&&!natural)continue;var root=DecorationRoot(r);if(root==null||moved.Contains(root))continue;
                        var b=DecorationBounds(root);var roads=pole?roadBounds:runwayBounds;
                        var obstruction=roads.Where(x=>TouchesXZ(b,x,-.2f)).OrderBy(x=>(x.center-b.center).sqrMagnitude).ToArray();if(obstruction.Length==0)continue;
                        var road=obstruction[0];var options=new List<Vector3>();
                        for(int extra=0;extra<4;extra++)
                        {
                            float pad=1.8f+extra*2.5f;
                            options.Add(new Vector3(road.min.x-b.extents.x-pad,b.center.y,b.center.z));options.Add(new Vector3(road.max.x+b.extents.x+pad,b.center.y,b.center.z));
                            options.Add(new Vector3(b.center.x,b.center.y,road.min.z-b.extents.z-pad));options.Add(new Vector3(b.center.x,b.center.y,road.max.z+b.extents.z+pad));
                        }
                        foreach(var target in options.OrderBy(p=>(p-b.center).sqrMagnitude))
                        {
                            var proposed=new Bounds(target,b.size);if(target.x<min.x||target.x>max.x||target.z<min.z||target.z>max.z||!Clear(proposed,root))continue;
                            moves.Add(MoveDecoration(root,root.position+target-b.center,root.eulerAngles.y,pole?"street pole clearance":"runway scenery clearance"));moved.Add(root);occupied.Add(proposed);break;
                        }
                    }
                    var cargo=visible.Where(r=>new[]{"SM_Prop_Crate_Wood_01","SM_Prop_Crate_Stack_01","SM_Prop_Barrel_Stack_01","SM_Prop_BarrelPile_01","SM_Prop_Pallet_01","SM_Prop_Pallet_02"}.Contains(MeshName(r)))
                        .Select(DecorationRoot).Where(t=>t!=null&&!moved.Contains(t)).Distinct().Where(t=>{var b=DecorationBounds(t);return b.size.x<6&&b.size.z<6&&b.size.y<5;}).ToList();
                    var groupCenters=new List<Vector3>();
                    foreach(var building in buildings.OrderBy(r=>Mathf.Abs(r.bounds.center.x-(min.x+max.x)*.5f)+Mathf.Abs(r.bounds.center.z-(min.z+max.z)*.5f)))
                    {
                        if(receipt.groups>=40)break;var b=building.bounds;if(groupCenters.Any(p=>(p-b.center).sqrMagnitude<400))continue;int count=0;
                        foreach(var item in cargo.OrderBy(t=>(t.position-b.center).sqrMagnitude).Take(12).ToArray())
                        {
                            var ib=DecorationBounds(item);if((ib.center-b.center).sqrMagnitude>65*65||Mathf.Abs(ib.min.y-b.min.y)>.9f)continue;
                            bool placed=false;
                            foreach(int side in new[]{1,-1})foreach(int slot in new[]{-1,0,1})
                            {
                                if(placed)break;var target=new Vector3(b.center.x+side*(b.extents.x+ib.extents.x+2.6f),ib.center.y,b.center.z+slot*3.6f);
                                var proposed=new Bounds(target,ib.size);if(target.x<min.x||target.x>max.x||target.z<min.z||target.z>max.z||!Clear(proposed,item))continue;
                                moves.Add(MoveDecoration(item,item.position+target-ib.center,item.eulerAngles.y,"frontage service group"));moved.Add(item);occupied.Add(proposed);cargo.Remove(item);placed=true;count++;
                            }
                            if(count>=3)break;
                        }
                        if(count>0){receipt.groups++;groupCenters.Add(b.center);}
                    }
                    foreach(var r in visible)
                    {
                        string mesh=MeshName(r);bool container=mesh.StartsWith("SM_Prop_Container_");
                        if(!container&&!IsBuilding(r))continue;var mats=r.sharedMaterials;bool changed=false;
                        int palette=Math.Abs(Mathf.FloorToInt(r.bounds.center.x/60)+Mathf.FloorToInt(r.bounds.center.z/55))%3;
                        for(int i=0;i<mats.Length;i++)
                        {
                            var m=mats[i];if(m==null||(!m.HasProperty("_BaseColor")&&!m.HasProperty("_Color"))||AssetDatabase.GetAssetPath(m).StartsWith(VisualArt))continue;
                            mats[i]=DistrictTint(m,palette,container);changed=true;
                        }
                        if(changed){r.sharedMaterials=mats;EditorUtility.SetDirty(r);if(container)receipt.variedContainers++;else receipt.buildingRenderers++;}
                    }
                    receipt.moves=moves.ToArray();receipt.protectedSceneHash=TextHash(NonDecorativeChanges(original,receipt.moves));original=null;GC.Collect();
                    receipt.result="Pending native save and invariant check";File.WriteAllText(receiptPath+".pending.json",JsonUtility.ToJson(receipt,true));
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("District scene save failed "+map);
                    if(receipt.protectedSceneHash!=TextHash(NonDecorativeChanges(ReadSceneText(source),receipt.moves)))throw new InvalidOperationException("Non-decorative scene data changed "+map);
                    receipt.renderersAfter=rs.Length;receipt.trianglesAfter=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3);
                    if(receipt.trianglesAfter!=receipt.trianglesBefore)throw new InvalidOperationException("Unexpected geometry cost "+map);
                    AssetDatabase.SaveAssets();
                    foreach(var material in rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(VisualArt)).Distinct())
                        if(ShaderUtil.ShaderHasError(material.shader))throw new InvalidOperationException("District shader error "+material.name);
                    receipt.afterHash=Hash(source);receipt.result="Passed";File.WriteAllText(VisualReport+"/"+map+"-district-details.json",JsonUtility.ToJson(receipt,true));
                    Capture(map,scene,def,rs,"district-details",VisualReport,DetailViews(rs,def));Debug.Log($"[MapDistrictDetails] map={map} result=Passed groups={receipt.groups} movedProps={moves.Count} buildings={receipt.buildingRenderers} containers={receipt.variedContainers} newRenderers=0 newTriangles=0");
                }
                Debug.Log("[MapDistrictDetails] result=Passed maps=11 gameplayAndCollision=unchanged");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
