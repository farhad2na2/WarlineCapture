using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        const string WaterDressingPrefix="CampaignWaterDressing20261016_";
        const string MilitaryProps="Assets/PolygonMilitary/Prefabs/Props/";
        [Serializable] sealed class WaterDressingReceipt
        {
            public string map,beforeHash,afterHash,protectedHash;
            public int groups,renderRows,materialAssignments;
            public string[] journal;
        }
        sealed class BridgeSite
        {
            public Bounds bounds;public Vector3 axis;public float deckY;
            public readonly List<MeshRenderer> decks=new List<MeshRenderer>();
        }
        public static void BuildWaterBridgeDressing()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            string output=WaterRoadReport+"/dressing";Directory.CreateDirectory(output);
            try
            {
                foreach(string map in VisualMaps)
                {
                    if(!(map=="AshLinePort"||map=="DenseCity"||map.StartsWith("CH05",StringComparison.Ordinal)))continue;
                    string source=Source(map),before=Hash(source);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    // Rebuild only this task's visual-only groups; retain every original owner and component.
                    var previousGroups=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                        .Where(t=>t.name.StartsWith(WaterDressingPrefix,StringComparison.Ordinal)).ToArray();
                    foreach(var group in previousGroups)
                    {
                        if(group.GetComponentsInChildren<Component>(true).Any(c=>c==null||!(c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup)))throw new InvalidOperationException("Dressing group acquired nonvisual state");
                        UnityEngine.Object.DestroyImmediate(group.gameObject);
                    }
                    var protection=new RefinementProtection(scene);var all=SceneRenderers(scene);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var visible=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,80)).ToArray();
                    var parents=visible.Select(DestinationParent).Where(t=>t!=null&&t.name.StartsWith("SM_Prop_",StringComparison.Ordinal))
                        .Select(t=>CanonicalWaterParent(t)).Where(t=>t!=null).Distinct().ToArray();
                    var existing=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                        .Where(t=>t.name.StartsWith(WaterDressingPrefix,StringComparison.Ordinal)).ToDictionary(t=>t.name);
                    var journal=new List<string>();var views=new List<Tuple<string,Vector3,float>>();
                    var receipt=new WaterDressingReceipt{map=map,beforeHash=before};
                    Transform Group(string name,Vector3 position)
                    {
                        name=WaterDressingPrefix+name;
                        if(existing.ContainsKey(name))return null;
                        var parent=parents.OrderBy(t=>(t.position-position).sqrMagnitude).FirstOrDefault();
                        if(parent==null||Vector3.Distance(parent.position,position)>100)
                        {
                            if(name.Contains("_Bridge_"))throw new InvalidOperationException("No nearby scenery owner for "+map+" "+name);
                            journal.Add("SKIPPED "+name+" no nearby canonical scenery owner position="+position);return null;
                        }
                        var group=new GameObject(name).transform;group.position=position;group.SetParent(parent,true);
                        group.gameObject.isStatic=true;protection.NewPresentationRoots.Add(group);receipt.groups++;
                        journal.Add(name+" owner="+Hierarchy(parent)+" position="+position);return group;
                    }
                    var sites=WaterBridgeSites(visible);
                    int index=0;
                    foreach(var site in sites)
                    {
                        var center=site.bounds.center;center.y=site.deckY;
                        var group=Group("Bridge_"+index,center);views.Add(Tuple.Create("bridge-"+index,center,Mathf.Max(38,site.bounds.size.magnitude*1.1f)));index++;
                        if(group==null)continue;
                        Vector3 axis=site.axis,side=new Vector3(-axis.z,0,axis.x);
                        float halfLength=Mathf.Abs(axis.x)*site.bounds.extents.x+Mathf.Abs(axis.z)*site.bounds.extents.z;
                        float halfWidth=Mathf.Abs(side.x)*site.bounds.extents.x+Mathf.Abs(side.z)*site.bounds.extents.z;
                        float yaw=Mathf.Atan2(axis.x,axis.z)*Mathf.Rad2Deg;
                        foreach(float end in new[]{-1f,1f})foreach(float bank in new[]{-1f,1f})
                        {
                            var corner=center+axis*(end*(halfLength-.8f))+side*(bank*(halfWidth-.45f));
                            AddWaterProp(group,MilitaryProps+"SM_Prop_LightPole_01.prefab",corner,yaw+bank*90,4.4f,true);
                            AddWaterProp(group,MilitaryProps+"SM_Prop_Street_Light_01.prefab",corner-side*bank*.35f+Vector3.up*4.1f,yaw+bank*90,1.1f,false);
                            // Short wing wall follows the outside of the approach; it never closes the lane.
                            var wall=center+axis*(end*(halfLength+1.0f))+side*(bank*(halfWidth+.25f));wall.y-=.35f;
                            AddWaterProp(group,MilitaryProps+"SM_Prop_Barrier_03.prefab",wall,yaw,2.2f,false);
                        }
                        // Existing harbour bridges already have full supports. Narrow canal decks need visual abutment faces.
                        if(halfLength<12)
                        {
                            foreach(float end in new[]{-1f,1f})
                            {
                                var support=center+axis*(end*(halfLength-1));support.y-=.45f;
                                AddWaterSupport(group,support,yaw,halfWidth*2,2.4f);
                            }
                        }
                        var post=center-axis*(halfLength+2.3f)+side*(halfWidth+2.1f);
                        if(WaterGroundHeight(post,visible,out float groundY))
                        {
                            post.y=groundY;
                            AddWaterProp(group,MilitaryProps+"Military/SM_Prop_Barrier_Base_Row_01.prefab",post,yaw+90,2.5f,false);
                        }
                    }
                    int bankIndex=0;
                    foreach(var water in visible.Where(NaturalWater).OrderByDescending(w=>w.bounds.size.x*w.bounds.size.z).Take(6))
                    {
                        var b=water.bounds;bool alongZ=b.size.z>b.size.x;Vector3 axis=alongZ?Vector3.forward:Vector3.right;
                        Vector3 side=alongZ?Vector3.right:Vector3.forward;float half=alongZ?b.extents.x:b.extents.z;
                        float length=alongZ?b.size.z:b.size.x;
                        foreach(float fraction in new[]{-.36f,.27f})foreach(float sign in new[]{-1f,1f})
                        {
                            var position=b.center+axis*(length*fraction)+side*(sign*(half+.65f));
                            if(!WaterGroundHeight(position,visible,out float y)||Mathf.Abs(y-b.max.y)>2)continue;
                            if(visible.Any(r=>CampaignTrafficSurface(r)&&PlanarContains(r.bounds,position,1.2f)))continue;
                            position.y=y;var group=Group("Bank_"+bankIndex,position);bankIndex++;
                            if(group==null)continue;
                            AddWaterProp(group,"Assets/Game/Prefabs/Environment/Decorations/SM_Env_Grass_03.prefab",position,bankIndex*137.5f,1.3f,false);
                            AddWaterProp(group,"Assets/PolygonMilitary/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab",position+axis*.7f,bankIndex*71, .8f,false);
                            if(map=="AshLinePort"||map=="CH05M02TrustUnderFire")AddWaterBollard(group,position+axis*1.8f);
                        }
                    }
                    int endIndex=0;
                    foreach(var water in visible.Where(NaturalWater))
                    {
                        var mesh=water.GetComponent<MeshFilter>()?.sharedMesh;
                        if(mesh==null||!AssetDatabase.GetAssetPath(mesh).Contains("/CanalEndsV2/"))continue;
                        if(!ChannelUvs(mesh,out var channel,out var flows,out var sides))continue;
                        int flags=Mathf.RoundToInt(flows[0].w);if((flags&16)==0)continue;
                        var origin=mesh.vertices[0];var flow=(Vector3)flows[0];var side=(Vector3)sides[0];
                        foreach(int end in new[]{0,1})
                        {
                            if((flags&(end==0?4:8))==0)continue;
                            var edge=water.transform.TransformPoint(origin+side*(channel[0].z*.5f)+flow*(channel[0].w*end));
                            var outward=water.transform.TransformDirection(flow)*(end==0?-1:1);
                            var position=edge+outward*1.8f;
                            if(!WaterGroundHeight(position,visible,out float y))continue;
                            position.y=y;var group=Group("CanalEnd_"+endIndex,position);endIndex++;
                            if(group==null)continue;
                            var across=water.transform.TransformDirection(side);
                            foreach(float sign in new[]{-1f,1f})
                            {
                                var clump=position+across*sign*2.8f;
                                if(!WaterGroundHeight(clump,visible,out float gy))continue;clump.y=gy;
                                AddWaterProp(group,"Assets/Game/Prefabs/Environment/Decorations/SM_Env_Grass_03.prefab",clump,endIndex*51+sign*30,1.4f,false);
                            }
                            if(views.Count<4)views.Add(Tuple.Create("canal-end-"+endIndex,edge,38f));
                        }
                    }
                    // A map-owned copy on the participating renderer resolves shared/role-conflicted joins.
                    var data=new WaterRoadMap{map=map,def=def,report=new WaterRoadMapReport{map=map}};
                    CollectRoadJoins(data,visible);CollectBlendRoles(data,visible);
                    foreach(var pair in data.joins.SelectMany(j=>new[]{(renderer:j.dirtRenderer,role:"earth"),(renderer:j.pavedRenderer,role:"paved")}).Distinct())
                    {
                        var materials=pair.renderer.sharedMaterials;bool changed=false;
                        for(int i=0;i<materials.Length;i++)
                        {
                            var m=materials[i];if(m==null||m.shader==null||m.shader.name!=GroundShaderName)continue;
                            string path=AssetDatabase.GetAssetPath(m);
                            if(MapOwnedMaterial(path,map)&&data.roles[m].Count==1)continue;
                            string copy=WaterRoadArt+"/JoinMaterials/"+map+"_"+AssetDatabase.AssetPathToGUID(path)+"_"+pair.role+".mat";
                            Directory.CreateDirectory(Path.GetDirectoryName(copy));var owned=AssetDatabase.LoadAssetAtPath<Material>(copy);
                            if(owned==null){owned=new Material(m);AssetDatabase.CreateAsset(owned,copy);}
                            materials[i]=owned;changed=true;receipt.materialAssignments++;
                        }
                        if(changed){pair.renderer.sharedMaterials=materials;EditorUtility.SetDirty(pair.renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(pair.renderer);}
                    }
                    string protectedHash=protection.Validate(scene);
                    if(receipt.groups>0||receipt.materialAssignments>0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        if(!EditorSceneManager.SaveScene(scene)||protection.Validate(scene)!=protectedHash)throw new InvalidOperationException("Water dressing changed protected source state "+map);
                    }
                    receipt.afterHash=Hash(source);receipt.protectedHash=protectedHash;
                    var groups=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith(WaterDressingPrefix,StringComparison.Ordinal)).ToArray();
                    receipt.renderRows=groups.Sum(t=>t.GetComponentsInChildren<MeshRenderer>(true).Sum(r=>r.sharedMaterials.Length));
                    var branchRows=new SortedDictionary<string,int>();
                    foreach(var group in groups)foreach(var r in group.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var parts=Hierarchy(r.transform).Split('/');string branch=parts[0]+"/"+parts[1];
                        branchRows.TryGetValue(branch,out int count);branchRows[branch]=count+r.sharedMaterials.Length;
                    }
                    File.WriteAllLines(output+"/"+map+"-ledger.txt",branchRows.Select(p=>p.Key+"="+p.Value));
                    receipt.journal=journal.ToArray();File.WriteAllText(output+"/"+map+".json",JsonUtility.ToJson(receipt,true));
                    foreach(var group in groups)
                        if(group.GetComponentsInChildren<Component>(true).Any(c=>c==null||!(c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup)))throw new InvalidOperationException("Nonvisual dressing component");
                    if(views.Count==0&&groups.Length>0)views.Add(Tuple.Create("bank",groups[0].position,38f));
                    Capture(map,scene,def,SceneRenderers(scene),"after",output,views.Take(4).ToArray());
                    Debug.Log($"[WaterBridgeDressing] map={map} groupsAdded={receipt.groups} totalRows={receipt.renderRows} materialAssignments={receipt.materialAssignments} protectedState=unchanged");
                }
                Debug.Log("[WaterBridgeDressing] result=Passed gameplay=unchanged requiresRenderReconciliation=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        static Transform CanonicalWaterParent(Transform t)
        {
            var dense=t.GetComponentInParent<DenseCityPresentationIdentityAuthoring>();
            if(dense!=null)return dense.Role==OperationMapEntityPresentationRole.RenderOnly?dense.transform:null;
            var regular=t.GetComponentInParent<OperationMapEntityPresentationIdentityAuthoring>();
            return regular!=null?(regular.Role==OperationMapEntityPresentationRole.RenderOnly?regular.transform:null):t;
        }
        static List<BridgeSite> WaterBridgeSites(MeshRenderer[] visible)
        {
            var sites=new List<BridgeSite>();
            foreach(var deck in visible.Where(r=>MeshName(r)=="SM_Env_Bridge_01"||MeshName(r)=="SM_Env_Bridge_02").OrderBy(r=>r.bounds.center.x).ThenBy(r=>r.bounds.center.z))
            {
                var site=sites.FirstOrDefault(s=>TouchesXZ(s.bounds,deck.bounds,.15f));
                if(site==null){var axis=deck.transform.forward;axis.y=0;axis.Normalize();site=new BridgeSite{bounds=deck.bounds,axis=axis,deckY=deck.transform.position.y};sites.Add(site);}
                else site.bounds.Encapsulate(deck.bounds);
                site.decks.Add(deck);
            }
            return sites;
        }
        static bool WaterGroundHeight(Vector3 p,MeshRenderer[] visible,out float y)
        {
            y=float.NegativeInfinity;
            foreach(var r in visible.Where(r=>!NaturalWater(r)&&IsSurface(r)&&PlanarContains(r.bounds,p,.05f)))
            {
                var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
                try{Physics.SyncTransforms();if(collider.Raycast(new Ray(new Vector3(p.x,r.bounds.max.y+1,p.z),Vector3.down),out var hit,r.bounds.size.y+2)&&hit.normal.y>.7f)y=Mathf.Max(y,hit.point.y);}
                finally{UnityEngine.Object.DestroyImmediate(collider);}
            }
            return !float.IsNegativeInfinity(y);
        }
        static GameObject AddWaterProp(Transform parent,string path,Vector3 position,float yaw,float size,bool height)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Missing water dressing prop "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.rotation=Quaternion.Euler(0,yaw,0);
            foreach(var c in go.GetComponentsInChildren<Component>(true))if(c!=null&&!(c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup))UnityEngine.Object.DestroyImmediate(c);
            var b=DecorationBounds(go.transform);float extent=height?b.size.y:Mathf.Max(b.size.x,b.size.z);
            go.transform.localScale*=size/Mathf.Max(extent,.01f);b=DecorationBounds(go.transform);
            go.transform.position+=new Vector3(position.x-b.center.x,position.y-b.min.y,position.z-b.center.z);
            go.transform.SetParent(parent,true);foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;return go;
        }
        static void AddWaterSupport(Transform parent,Vector3 top,float yaw,float width,float height)
        {
            var go=AddWaterProp(parent,"Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Bridge_Support_01.prefab",top,yaw,width,false);
            var b=DecorationBounds(go.transform);go.transform.localScale=new Vector3(go.transform.localScale.x,go.transform.localScale.y*height/b.size.y,go.transform.localScale.z);
            b=DecorationBounds(go.transform);go.transform.position+=Vector3.up*(top.y-b.max.y);
        }
        static void AddWaterBollard(Transform parent,Vector3 p)
        {
            const string path=WaterRoadArt+"/MooringIron.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.16f,.19f,.18f));material.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(material,path);}
            void Part(string name,Vector3 position,Vector3 scale)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.position=position;go.transform.localScale=scale;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.transform.SetParent(parent,true);go.isStatic=true;
            }
            Part("MooringBase",p+Vector3.up*.06f,new Vector3(.65f,.06f,.65f));
            Part("MooringPost",p+Vector3.up*.43f,new Vector3(.28f,.37f,.28f));
            Part("MooringCap",p+Vector3.up*.82f,new Vector3(.55f,.09f,.55f));
        }
    }
}
