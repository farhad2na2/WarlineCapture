using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        const string BridgeContinuityReport=WaterRoadReport+"/bridge-refinement";
        static readonly string[] BridgeContinuityMaps={"AshLinePort","CH05M02TrustUnderFire","DenseCity"};
        public static void AuditBridgeContinuity()
        {
            Check();Directory.CreateDirectory(BridgeContinuityReport);var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string map in BridgeContinuityMaps)
                {
                    string source=Source(map),before=Hash(source);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var rs=SceneRenderers(scene);var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,80)).ToArray();
                    var sites=WaterBridgeSites(visible);var lines=new List<string>();int n=0;
                    foreach(var site in sites)
                    {
                        lines.Add($"BRIDGE {n++} center={site.bounds.center:F3} size={site.bounds.size:F3} deckY={site.deckY:F3} axis={site.axis:F3}");
                        foreach(var r in site.decks.Take(2))
                        {
                            var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                            lines.Add($"DECK {Hierarchy(r.transform)} mesh={AssetDatabase.GetAssetPath(mesh)} localBounds={mesh.bounds} rotation={r.transform.eulerAngles} scale={r.transform.lossyScale}");
                            lines.Add("HEIGHTS "+string.Join(";",mesh.vertices.Select(r.transform.TransformPoint).GroupBy(v=>Mathf.Round(v.y*100)/100).OrderBy(g=>g.Key).Select(g=>$"{g.Key}:{g.Count()}")));
                            lines.Add("MATERIALS "+string.Join(";",r.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
                        }
                        foreach(var r in visible.Where(r=>IsSurface(r)&&!NaturalWater(r)).OrderBy(r=>(r.bounds.ClosestPoint(site.bounds.center)-site.bounds.center).sqrMagnitude).Take(12))
                            lines.Add($"ROAD {Hierarchy(r.transform)} mesh={MeshName(r)} center={r.bounds.center:F3} size={r.bounds.size:F3} rotation={r.transform.eulerAngles} materials="+string.Join(";",r.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
                        lines.Add("WATER "+string.Join(";",visible.Where(NaturalWater).OrderBy(r=>(r.bounds.ClosestPoint(site.bounds.center)-site.bounds.center).sqrMagnitude).Take(3).Select(r=>$"{r.name} bounds={r.bounds}")));
                    }
                    File.WriteAllLines(BridgeContinuityReport+"/"+map+"-audit.txt",lines);
                    var first=sites[0];var center=first.bounds.center;center.y=first.deckY;
                    Capture(map,scene,def,rs,"before",BridgeContinuityReport,new[]{Tuple.Create("bridge-player",center,Mathf.Max(42,first.bounds.size.magnitude*1.0f)),Tuple.Create("bridge-oblique",center,Mathf.Max(38,first.bounds.size.magnitude*.85f))});
                    if(before!=Hash(source))throw new InvalidOperationException("Audit changed scene");
                }
                Debug.Log("[BridgeContinuityAudit] result=Passed scenes=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void InspectBridgeContinuity()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(Source("DenseCity"),OpenSceneMode.Single);var rs=SceneRenderers(scene);
                var lines=new List<string>();
                foreach(var r in rs.Where(r=>Hierarchy(r.transform).Contains(BridgeSurfacePrefix)))
                {
                    lines.Add($"{Hierarchy(r.transform)} active={r.gameObject.activeInHierarchy} enabled={r.enabled} forceOff={r.forceRenderingOff} bounds={r.bounds} position={r.transform.position} scale={r.transform.lossyScale} mesh={r.GetComponent<MeshFilter>().sharedMesh.name}");
                    Debug.Log("[BridgeVisibility] "+lines.Last());
                }
                bool oldBackfaces=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
                try
                {
                    foreach(float z in new[]{270f,273f,276f})
                    {
                        var point=new Vector3(1281,0,z);
                        foreach(var r in rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&PlanarContains(r.bounds,point,.01f)))
                        {
                            var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                            var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
                            try
                            {
                                Physics.SyncTransforms();
                                if(collider.Raycast(new Ray(new Vector3(point.x,r.bounds.max.y+1,point.z),Vector3.down),out var hit,r.bounds.size.y+2))
                                    lines.Add($"HIT z={z} y={hit.point.y:F5} normal={hit.normal} renderer={Hierarchy(r.transform)}");
                            }
                            finally{UnityEngine.Object.DestroyImmediate(collider);}
                        }
                    }
                }
                finally{Physics.queriesHitBackfaces=oldBackfaces;}
                File.WriteAllLines(BridgeContinuityReport+"/visibility.txt",lines);
                var flat=rs.Where(r=>r.name=="DenseCity_FlatPaving").ToArray();foreach(var r in flat)r.enabled=false;
                Capture("DenseCity",scene,AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition("DenseCity")),rs,"without-flat-paving",BridgeContinuityReport,new[]{Tuple.Create("bridge-player",new Vector3(1281,.08f,273),34f)});
                foreach(var r in flat)r.enabled=true;
                var groups=rs.Where(r=>Hierarchy(r.transform).Contains(BridgeSurfacePrefix+"0/")).ToArray();
                foreach(var r in rs.Where(r=>!groups.Contains(r)))r.enabled=false;
                Capture("DenseCity",scene,AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition("DenseCity")),groups,"isolated",BridgeContinuityReport,new[]{Tuple.Create("bridge-player",new Vector3(1281,.08f,273),34f)});
                Debug.Log("[BridgeVisibility] result=Passed inspectionOnly=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }

        const string BridgeSurfacePrefix=WaterDressingPrefix+"BridgeSurface_";
        const string BridgeSurfaceArt=WaterRoadArt+"/BridgeSurfaces";
        [Serializable] sealed class BridgeContinuityReceipt
        {
            public string map,beforeHash,afterHash,protectedHash;
            public int bridges,addedMaterialRows;
            public string[] measurements;
        }
        public static void BuildBridgeContinuity()
        {
            Check();Directory.CreateDirectory(BridgeSurfaceArt);Directory.CreateDirectory(BridgeContinuityReport);AssetDatabase.Refresh();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var sidewalkSource=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Roads/Road_Asphalt_With_Sidewalk/Road_Asphalt_With_Sidewalk_Straight.prefab")
                    .GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="Sidewalk").sharedMesh;
                var paint=BridgeSolid("LanePaint",new Color(.78f,.77f,.70f));
                var concrete=BridgeSolid("DeckStone",new Color(.53f,.51f,.44f));
                foreach(string map in BridgeContinuityMaps)
                {
                    string source=Source(map);var receipt=new BridgeContinuityReceipt{map=map,beforeHash=Hash(source)};
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);
                    foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith(BridgeSurfacePrefix,StringComparison.Ordinal)).ToArray())
                    {
                        if(t.GetComponentsInChildren<Component>(true).Any(c=>!(c is Transform||c is MeshRenderer||c is MeshFilter)))throw new InvalidOperationException("Bridge surface acquired nonvisual state");
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                    }
                    var protection=new RefinementProtection(scene);var rs=SceneRenderers(scene);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,80)).ToArray();
                    var sites=WaterBridgeSites(visible);var journal=new List<string>();var views=new List<Tuple<string,Vector3,float>>();int index=0;
                    foreach(var site in sites)
                    {
                        Vector3 axis=site.axis,across=Vector3.Cross(Vector3.up,axis),center=site.bounds.center;center.y=site.deckY;
                        float length=Mathf.Abs(axis.x)*site.bounds.size.x+Mathf.Abs(axis.z)*site.bounds.size.z;
                        float width=Mathf.Abs(across.x)*site.bounds.size.x+Mathf.Abs(across.z)*site.bounds.size.z;
                        var road=visible.Where(r=>r.name=="Asphalt"&&r.bounds.size.x<20&&r.bounds.size.z<20)
                            .Where(r=>Mathf.Abs(Vector3.Dot(r.bounds.center-center,across))<2)
                            .OrderBy(r=>(r.bounds.center-center).sqrMagnitude).FirstOrDefault();
                        if(road==null)throw new InvalidOperationException("Bridge lacks an aligned approach road: "+map+" "+index);
                        var walk=visible.Where(r=>MeshName(r)==sidewalkSource.name&&r.sharedMaterial!=null).OrderBy(r=>(r.bounds.center-center).sqrMagnitude).First();
                        var original=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==WaterDressingPrefix+"Bridge_"+index);
                        var group=new GameObject(BridgeSurfacePrefix+index).transform;group.position=center;group.rotation=Quaternion.LookRotation(axis);group.SetParent(original.parent,true);group.gameObject.isStatic=true;
                        protection.NewPresentationRoots.Add(group);
                        float surfaceY=BridgeRoadHeight(center,axis,length,road,visible);
                        float roadY=Mathf.Max(site.deckY+.025f,surfaceY+.025f)-center.y;
                        float walkTop=Mathf.Max(roadY+.15f,walk.bounds.max.y-center.y);
                        float roadWidth=5f,walkWidth=Mathf.Min(2.5f,(width-roadWidth-.85f)*.5f);
                        if(walkWidth<.65f)throw new InvalidOperationException("Insufficient existing bridge width for sidewalks");
                        string key=map+"_"+index;
                        var asphalt=BridgeCopyMaterial(key+"_Asphalt",road.sharedMaterial);
                        // World-projected road detail is shared with the approaches; road-end masks do not belong on the span.
                        asphalt.SetFloat("_RoadBlendStrength",0);asphalt.SetFloat("_RoadTerminalStrength",0);Save(asphalt);
                        var paving=BridgeCopyMaterial(key+"_Sidewalk",walk.sharedMaterial);
                        paving.SetFloat("_RoadBlendStrength",0);paving.SetFloat("_RoadTerminalStrength",0);Save(paving);
                        var roadMesh=new BridgeMeshBuilder();
                        roadMesh.Quad(-roadWidth*.5f,-length*.5f,roadWidth*.5f,length*.5f,roadY,0);
                        // Two 2.2 m dashes per 10 m road module, matching the established city street pattern.
                        float approach=map=="DenseCity"?0:10;
                        float start=-length*.5f-approach,end=length*.5f+approach;
                        for(float z=start+1.3f;z<end;z+=5)
                            roadMesh.Quad(-.065f,z,.065f,Mathf.Min(z+2.2f,end),roadY+.012f,1);
                        foreach(float sign in new[]{-1f,1f})
                        {
                            float x=sign*(roadWidth*.5f-.22f);
                            roadMesh.Quad(x-.045f,-length*.5f,x+.045f,length*.5f,roadY+.012f,1);
                        }
                        BridgeRenderer(group,"BridgeAsphaltAndLines",key+"_Road",roadMesh.Build("BridgeAsphaltAndLines",2),new[]{asphalt,paint});
                        var copies=new List<CombineInstance>();var b=sidewalkSource.bounds;int pieces=Mathf.CeilToInt(length/5);float segment=length/pieces;
                        for(int n=0;n<pieces;n++)foreach(float sign in new[]{-1f,1f})
                        {
                            // Preserve the actual road's stone/kerb mesh and atlas UVs, with the kerb facing the traffic lane.
                            float z=-length*.5f+(n+.5f)*segment;
                            var matrix=Matrix4x4.TRS(new Vector3(sign*(roadWidth*.5f+walkWidth*.5f),walkTop-b.size.y*.5f,z),Quaternion.Euler(0,sign>0?-90:90,0),new Vector3(segment/b.size.x,1,walkWidth/b.size.z))*Matrix4x4.Translate(-b.center);
                            copies.Add(new CombineInstance{mesh=sidewalkSource,transform=matrix});
                        }
                        var sidewalks=new Mesh{name="BridgeSidewalkPaving"};sidewalks.CombineMeshes(copies.ToArray(),true,true);
                        BridgeRenderer(group,"BridgeSidewalkPaving",key+"_Sidewalk",sidewalks,new[]{paving});
                        var structure=new BridgeMeshBuilder();float outer=roadWidth*.5f+walkWidth;
                        foreach(float sign in new[]{-1f,1f})
                        {
                            // Outside deck fascia and shallow cross beams expose the existing deck's depth without moving the crossing.
                            structure.Box(new Vector3(sign*(outer-.12f),roadY-.38f,0),new Vector3(.3f,.7f,length),0);
                            foreach(float endSign in new[]{-1f,1f})structure.Box(new Vector3(sign*(outer-.48f),roadY-.55f,endSign*(length*.5f-1.0f)),new Vector3(.85f,1.0f,1.0f),0);
                        }
                        BridgeRenderer(group,"BridgeDeckFaces",key+"_Structure",structure.Build("BridgeDeckFaces",1),new[]{concrete});
                        receipt.bridges++;receipt.addedMaterialRows+=4;
                        journal.Add($"bridge={index} length={length:F3} width={width:F3} roadWidth={roadWidth:F3} sidewalkWidth={walkWidth:F3} roadY={roadY+center.y:F3} sidewalkY={walkTop+center.y:F3} road={Hierarchy(road.transform)} owner={Hierarchy(group.parent)}");
                        if(index<2){views.Add(Tuple.Create("bridge-"+index+"-player",center,Mathf.Max(34,length*1.12f)));views.Add(Tuple.Create("bridge-"+index+"-oblique",center,Mathf.Max(30,length*.95f)));}
                        index++;
                    }
                    if(map=="DenseCity")CutBridgePaving(visible,sites);
                    foreach(var m in visible.Where(NaturalWater).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.shader!=null&&m.shader.name==WaterShaderName&&MapOwnedMaterial(AssetDatabase.GetAssetPath(m),map)).Distinct())
                    {
                        // Persist new shader properties explicitly; a first shader import can otherwise serialize its default.
                        var serialized=new SerializedObject(m);var floats=serialized.FindProperty("m_SavedProperties.m_Floats");
                        SerializedProperty detail=null;
                        for(int i=0;i<floats.arraySize;i++)if(floats.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue=="_SurfaceDetail")detail=floats.GetArrayElementAtIndex(i).FindPropertyRelative("second");
                        if(detail==null){int i=floats.arraySize;floats.InsertArrayElementAtIndex(i);var entry=floats.GetArrayElementAtIndex(i);entry.FindPropertyRelative("first").stringValue="_SurfaceDetail";detail=entry.FindPropertyRelative("second");}
                        detail.floatValue=1f;serialized.ApplyModifiedPropertiesWithoutUndo();
                        m.SetFloat("_SurfaceDetail",1f);m.SetFloat("_Smoothness",.4f);
                        m.SetColor("_BaseColor",new Color(.18f,.335f,.37f));Save(m);
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(m),ImportAssetOptions.ForceSynchronousImport);
                        if(Mathf.Abs(m.GetFloat("_SurfaceDetail")-1)>.001f)throw new InvalidOperationException("Fine water ripple strength failed to persist: "+AssetDatabase.GetAssetPath(m));
                        Debug.Log("[BridgeWater] material="+AssetDatabase.GetAssetPath(m)+" fineRipples="+m.GetFloat("_SurfaceDetail"));
                    }
                    receipt.protectedHash=protection.Validate(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene)||protection.Validate(scene)!=receipt.protectedHash)throw new InvalidOperationException("Bridge refinement changed protected scene state");
                    receipt.afterHash=Hash(source);receipt.measurements=journal.ToArray();
                    File.WriteAllText(BridgeContinuityReport+"/"+map+"-receipt.json",JsonUtility.ToJson(receipt,true));
                    Capture(map,scene,def,SceneRenderers(scene),"after",BridgeContinuityReport,views.ToArray());
                    foreach(string shader in new[]{GroundShaderName,WaterShaderName})if(ShaderUtil.ShaderHasError(Shader.Find(shader)))throw new InvalidOperationException("Bridge capture shader error: "+shader);
                    Debug.Log($"[BridgeContinuity] map={map} bridges={receipt.bridges} addedRows={receipt.addedMaterialRows} protectedState=unchanged");
                }
                Debug.Log("[BridgeContinuity] result=Passed collisionAndGameplay=unchanged requiresRenderReconciliation=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        static float BridgeRoadHeight(Vector3 center,Vector3 axis,float length,MeshRenderer road,MeshRenderer[] visible)
        {
            float y=road.bounds.max.y;
            // Sample the approaches as well: the redundant city paving is cut out beneath the span.
            foreach(float offset in new[]{0f,-length*.5f-1f,length*.5f+1f})
                if(WaterGroundHeight(center+axis*offset,visible,out float hit))y=Mathf.Max(y,hit);
            return y;
        }
        public static void ValidateBridgeContinuityPackaging()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int bridges=0,rows=0;
            try
            {
                foreach(string map in BridgeContinuityMaps)
                {
                    string source=Source(map),hash=Hash(source);
                    var receipt=JsonUtility.FromJson<BridgeContinuityReceipt>(File.ReadAllText(BridgeContinuityReport+"/"+map+"-receipt.json"));
                    if(hash!=receipt.afterHash)throw new InvalidOperationException("Bridge source differs from reviewed receipt: "+map);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var protection=new RefinementProtection(scene);
                    var rs=SceneRenderers(scene);var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,80)).ToArray();
                    var original=visible.Where(r=>!Hierarchy(r.transform).Contains(BridgeSurfacePrefix)).ToArray();
                    var sites=WaterBridgeSites(original);
                    var groups=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith(BridgeSurfacePrefix,StringComparison.Ordinal)).ToArray();
                    int expected=map=="DenseCity"?8:2;
                    if(groups.Length!=expected||sites.Count!=expected||receipt.bridges!=expected)throw new InvalidOperationException("Bridge count mismatch: "+map);
                    for(int i=0;i<expected;i++)
                    {
                        var group=groups.Single(t=>t.name==BridgeSurfacePrefix+i);var site=sites[i];
                        var oldGroup=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==WaterDressingPrefix+"Bridge_"+i);
                        if(group.parent!=oldGroup.parent||group.GetComponentsInChildren<Component>(true).Any(c=>c==null||!(c is Transform||c is MeshFilter||c is MeshRenderer)))throw new InvalidOperationException("Bridge ownership or component mismatch");
                        var renderers=group.GetComponentsInChildren<MeshRenderer>(true);
                        if(renderers.Length!=3||renderers.Sum(r=>r.sharedMaterials.Length)!=4)throw new InvalidOperationException("Bridge row count mismatch");
                        foreach(var r in renderers)
                            if(!r.enabled||!r.gameObject.activeInHierarchy||r.sharedMaterials.Any(m=>m==null)||!AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).StartsWith(BridgeSurfaceArt+"/",StringComparison.Ordinal))throw new InvalidOperationException("Invalid bridge visual asset");
                        var axis=site.axis;var across=Vector3.Cross(Vector3.up,axis);var center=site.bounds.center;center.y=site.deckY;
                        float length=Mathf.Abs(axis.x)*site.bounds.size.x+Mathf.Abs(axis.z)*site.bounds.size.z;
                        var road=original.Where(r=>r.name=="Asphalt"&&r.bounds.size.x<20&&r.bounds.size.z<20&&Mathf.Abs(Vector3.Dot(r.bounds.center-center,across))<2).OrderBy(r=>(r.bounds.center-center).sqrMagnitude).First();
                        float expectedY=Mathf.Max(site.deckY+.025f,BridgeRoadHeight(center,axis,length,road,original)+.025f);
                        var deck=renderers.Single(r=>r.name=="BridgeAsphaltAndLines");
                        if(Mathf.Abs(deck.bounds.min.y-expectedY)>.002f)throw new InvalidOperationException($"Bridge road height is not reproducible: {map}/{i} current={deck.bounds.min.y} expected={expectedY}");
                        bridges++;rows+=4;
                    }
                    foreach(var m in visible.Where(NaturalWater).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&MapOwnedMaterial(AssetDatabase.GetAssetPath(m),map)).Distinct())
                        if(m.shader.name!=WaterShaderName||Mathf.Abs(m.GetFloat("_SurfaceDetail")-1)>.001f)throw new InvalidOperationException("Water shader refinement was not persisted");
                    if(map=="DenseCity"&&AssetDatabase.GetAssetPath(visible.Single(r=>r.name=="DenseCity_FlatPaving").GetComponent<MeshFilter>().sharedMesh)!=BridgeSurfaceArt+"/DenseCity_PavingBridgeCutouts.asset")throw new InvalidOperationException("City bridge paving cutouts missing");
                    var views=sites.Take(2).SelectMany((site,i)=>new[]{Tuple.Create("bridge-"+i+"-player",new Vector3(site.bounds.center.x,site.deckY,site.bounds.center.z),Mathf.Max(34,site.bounds.size.magnitude*1.05f)),Tuple.Create("bridge-"+i+"-oblique",new Vector3(site.bounds.center.x,site.deckY,site.bounds.center.z),Mathf.Max(30,site.bounds.size.magnitude*.9f))}).ToArray();
                    Capture(map,scene,def,rs,"final",BridgeContinuityReport,views);
                    protection.Validate(scene);
                    if(Hash(source)!=hash)throw new InvalidOperationException("Bridge validation changed source scene");
                    Debug.Log($"[BridgeContinuityValidation] map={map} bridges={expected} rows={expected*4} ownership=Passed heightReproduction=Passed savedWater=Passed sourceUnchanged=true");
                }
                foreach(string shader in new[]{GroundShaderName,WaterShaderName})if(ShaderUtil.ShaderHasError(Shader.Find(shader)))throw new InvalidOperationException("Bridge capture shader error: "+shader);
            }
            finally{RestoreVisualReviewSetup(setup);}
            Game.Editor.CampaignRenderReconciliation20261011.Inspect();
            Game.Editor.OperationMapRenderDatabaseBuilder.RunSourceParityValidation();
            Debug.Log($"[BridgeContinuityIntegration] result=Passed bridges={bridges} addedRows={rows} sourceParity=Passed");
        }
        // Remove only the redundant visual paving inside an existing bridge. The authoritative
        // road/collision scene and the source mesh asset remain untouched.
        static void CutBridgePaving(MeshRenderer[] visible,List<BridgeSite> sites)
        {
            var renderer=visible.Single(r=>r.name=="DenseCity_FlatPaving");var filter=renderer.GetComponent<MeshFilter>();var source=filter.sharedMesh;
            if(AssetDatabase.GetAssetPath(source)==BridgeSurfaceArt+"/DenseCity_PavingBridgeCutouts.asset")return;
            var positions=source.vertices;var normals=source.normals;var colors=source.colors;var tangents=source.tangents;
            var uvs=Enumerable.Range(0,8).Select(_=>new List<Vector4>()).ToArray();for(int c=0;c<8;c++)source.GetUVs(c,uvs[c]);
            BridgeClipVertex Vertex(int i)=>new BridgeClipVertex{position=renderer.transform.TransformPoint(positions[i]),normal=normals.Length==positions.Length?normals[i]:Vector3.up,color=colors.Length==positions.Length?colors[i]:Color.white,tangent=tangents.Length==positions.Length?tangents[i]:Vector4.zero,uv=uvs.Select(u=>u.Count==positions.Length?u[i]:Vector4.zero).ToArray()};
            var output=new List<BridgeClipVertex>();var submeshes=new List<int[]>();int changed=0;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var indices=source.GetTriangles(sub);var triangles=new List<int>();
                for(int i=0;i<indices.Length;i+=3)
                {
                    var pieces=new List<List<BridgeClipVertex>>{new List<BridgeClipVertex>{Vertex(indices[i]),Vertex(indices[i+1]),Vertex(indices[i+2])}};
                    foreach(var site in sites)
                    {
                        var next=new List<List<BridgeClipVertex>>();var b=site.bounds;
                        foreach(var poly in pieces)
                        {
                            if(poly.Max(v=>v.position.x)<=b.min.x||poly.Min(v=>v.position.x)>=b.max.x||poly.Max(v=>v.position.z)<=b.min.z||poly.Min(v=>v.position.z)>=b.max.z){next.Add(poly);continue;}
                            var inside=poly;
                            foreach(var plane in new[]{new Vector3(1,0,-b.min.x),new Vector3(-1,0,b.max.x),new Vector3(0,1,-b.min.z),new Vector3(0,-1,b.max.z)})
                            {
                                var outside=ClipBridgePolygon(inside,-plane);if(outside.Count>=3)next.Add(outside);
                                inside=ClipBridgePolygon(inside,plane);if(inside.Count<3)break;
                            }
                            if(inside.Count>=3)changed++;
                        }
                        pieces=next;
                    }
                    foreach(var poly in pieces)
                    {
                        int first=output.Count;output.AddRange(poly);
                        for(int j=1;j<poly.Count-1;j++)triangles.AddRange(new[]{first,first+j,first+j+1});
                    }
                }
                submeshes.Add(triangles.ToArray());
            }
            if(changed==0)throw new InvalidOperationException("No redundant city bridge paving found");
            var mesh=new Mesh{name=source.name+"_BridgeCutouts",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=source.subMeshCount};
            mesh.SetVertices(output.Select(v=>renderer.transform.InverseTransformPoint(v.position)).ToList());mesh.SetNormals(output.Select(v=>v.normal).ToList());
            if(colors.Length==positions.Length)mesh.SetColors(output.Select(v=>v.color).ToList());if(tangents.Length==positions.Length)mesh.SetTangents(output.Select(v=>v.tangent).ToList());
            for(int c=0;c<8;c++)if(uvs[c].Count==positions.Length)mesh.SetUVs(c,output.Select(v=>v.uv[c]).ToList());
            for(int sub=0;sub<source.subMeshCount;sub++)mesh.SetTriangles(submeshes[sub],sub);mesh.RecalculateBounds();
            string path=BridgeSurfaceArt+"/DenseCity_PavingBridgeCutouts.asset";filter.sharedMesh=SaveVisualMesh(path,mesh);filter.sharedMesh.UploadMeshData(false);EditorUtility.SetDirty(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            File.WriteAllLines(BridgeContinuityReport+"/paving-cutout.txt",new[]{"source="+AssetDatabase.GetAssetPath(source),"derivedVisualMesh="+path,"bridgeFootprints="+sites.Count,"intersectedTriangles="+changed,"beforeTriangles="+source.triangles.Length/3,"afterTriangles="+submeshes.Sum(t=>t.Length)/3,"collision=unchanged"});
            Debug.Log($"[BridgePavingCutout] result=Passed footprints={sites.Count} clippedTriangles={changed} collision=unchanged");
        }
        sealed class BridgeClipVertex
        {
            public Vector3 position,normal;public Vector4 tangent;public Color color;public Vector4[] uv;
            public static BridgeClipVertex Lerp(BridgeClipVertex a,BridgeClipVertex b,float t)=>new BridgeClipVertex{position=Vector3.Lerp(a.position,b.position,t),normal=Vector3.Lerp(a.normal,b.normal,t).normalized,tangent=Vector4.Lerp(a.tangent,b.tangent,t),color=Color.Lerp(a.color,b.color,t),uv=a.uv.Select((v,i)=>Vector4.Lerp(v,b.uv[i],t)).ToArray()};
        }
        static List<BridgeClipVertex> ClipBridgePolygon(List<BridgeClipVertex> input,Vector3 plane)
        {
            var result=new List<BridgeClipVertex>();if(input.Count==0)return result;
            float Distance(BridgeClipVertex v)=>v.position.x*plane.x+v.position.z*plane.y+plane.z;
            var a=input[input.Count-1];float da=Distance(a);
            foreach(var b in input)
            {
                float db=Distance(b);if((da>=0)!=(db>=0))result.Add(BridgeClipVertex.Lerp(a,b,da/(da-db)));
                if(db>=0)result.Add(b);a=b;da=db;
            }
            return result;
        }

        static Material BridgeSolid(string name,Color color)
        {
            string path=BridgeSurfaceArt+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.15f);m.enableInstancing=true;Save(m);return m;
        }
        static Material BridgeCopyMaterial(string name,Material source)
        {
            string path=BridgeSurfaceArt+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(source){name=name};AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(source);return m;
        }
        static void BridgeRenderer(Transform parent,string name,string key,Mesh mesh,Material[] materials)
        {
            mesh.RecalculateBounds();var saved=SaveVisualMesh(BridgeSurfaceArt+"/"+key+".asset",mesh);
            saved.UploadMeshData(false);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<MeshRenderer>().sharedMaterials=materials;
        }
        sealed class BridgeMeshBuilder
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<int>[] faces={new List<int>(),new List<int>()};
            public void Quad(float x0,float z0,float x1,float z1,float y,int material)
            { Face(new Vector3(x0,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z1),new Vector3(x1,y,z0),material); }
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material)
            {int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});faces[material].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public void Box(Vector3 center,Vector3 size,int material)
            {
                Vector3 a=center-size*.5f,b=center+size*.5f;
                Face(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),material);
                Face(new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),new Vector3(a.x,a.y,b.z),material);
                Face(new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),material);
                Face(new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),material);
                Face(new Vector3(b.x,a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),material);
                Face(new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),material);
            }
            public Mesh Build(string name,int submeshes)
            {var m=new Mesh{name=name,subMeshCount=submeshes};m.SetVertices(vertices);m.SetUVs(0,vertices.Select(v=>new Vector2(v.x,v.z)).ToList());for(int i=0;i<submeshes;i++)m.SetTriangles(faces[i],i);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }

    }
}
