using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    // Owner-requested presentation work on the existing shared map. Never rebuilds
    // its terrain, navigation, authored entity scene or gameplay inventories.
    public static class CampaignAirfieldVisualAuthoring
    {
        private const string Folder="Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/";
        private const string Evidence="Design/MapVariants/Beautify/CityEdgeAirfield/20261007-field-frontage";
        private const string Dressing="SharedLandscapeDressing20261004";
        private const string SharedGroundPath="Assets/Game/Art/MapBeautify/DetailedDesertGround.mat";
        private const string MapGroundPath="Assets/Game/Art/MapBeautify/CityEdgeDetailedGround.mat";
        private static Material EnsureMapGround(Scene prepared)
        {
            var shared=AssetDatabase.LoadAssetAtPath<Material>(SharedGroundPath);
            if(shared==null) throw new InvalidOperationException("Shared ground material missing.");
            var map=AssetDatabase.LoadAssetAtPath<Material>(MapGroundPath);
            if(map==null)
            {
                // Preserve CityEdge's palette, then restore only its four edits
                // to the material still used by other Campaign maps.
                if(Vector4.Distance(shared.GetColor("_SandDryColor"),new Color(.80f,.68f,.48f))>.001f ||
                    Mathf.Abs(shared.GetFloat("_SandScrubCoverage")-.35f)>.001f)
                    throw new InvalidOperationException("Shared palette changed; inspect before restoring scoped parameters.");
                map=new Material(shared){name="CityEdgeDetailedGround"};
                AssetDatabase.CreateAsset(map,MapGroundPath);
                shared.SetColor("_SandDryColor",new Color(.92f,.83f,.65f));
                shared.SetColor("_SandWornColor",new Color(.67f,.60f,.46f));
                shared.SetColor("_SandScrubColor",new Color(.46f,.61f,.32f));
                shared.SetFloat("_SandScrubCoverage",.65f);
                EditorUtility.SetDirty(shared);AssetDatabase.SaveAssetIfDirty(shared);
            }
            int replaced=0;
            foreach(var renderer in prepared.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)))
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++) if(materials[i]==shared)
                {materials[i]=map;changed=true;replaced++;}
                if(changed) {renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);}
            }
            EditorSceneManager.MarkSceneDirty(prepared);
            Debug.Log("[CityEdgeGroundScope] replacedSlots="+replaced+" material="+MapGroundPath);
            return map;
        }
        public static void IsolateGroundPalette()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author in Edit mode.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Preserve unsaved scenes before authoring.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var prepared=EditorSceneManager.OpenScene(Folder+"PreparedEntities.unity",OpenSceneMode.Single);
                var map=EnsureMapGround(prepared);
                if(Vector4.Distance(map.GetColor("_SandDryColor"),new Color(.80f,.68f,.48f))>.001f)
                    throw new InvalidOperationException("CityEdge palette was not preserved.");
                if(!EditorSceneManager.SaveScene(prepared)) throw new IOException("Shared source save failed.");
                AssetDatabase.SaveAssets();
                Debug.Log("[CityEdgeGroundScope] result=Passed sharedConsumers=AllCityEdgeAirfield unrelatedMapPaletteRestored=1");
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void CaptureSouthernSector()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Capture in Edit mode.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Preserve unsaved scene work before capture.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Evidence);
            try
            {
                EditorSceneManager.OpenScene(Folder+"PreparedEntities.unity",OpenSceneMode.Single);
                var binding=EditorSceneManager.OpenScene(Folder+"RuntimeBinding.unity",OpenSceneMode.Additive);
                SceneManager.SetActiveScene(binding);
                Capture(binding,new Vector3(450,0,405),"south-west-before");
                Capture(binding,new Vector3(680,0,405),"south-centre-before");
                Capture(binding,new Vector3(900,0,405),"south-east-before");
                Debug.Log("[CampaignSouthernSectorReview] result=Captured physicalMapModified=0");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        public static void CaptureOpeningSector()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Capture in Edit mode.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Preserve unsaved scene work before capture.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Evidence);
            try
            {
                EditorSceneManager.OpenScene(Folder+"PreparedEntities.unity",OpenSceneMode.Single);
                var binding=EditorSceneManager.OpenScene(Folder+"RuntimeBinding.unity",OpenSceneMode.Additive);
                SceneManager.SetActiveScene(binding);
                Capture(binding,new Vector3(452,0,502.5f),"player-angle-shadow-coverage",new Vector3(75.6f,95,0));
                Debug.Log("[CampaignOpeningSectorReview] result=Captured physicalMapModified=0");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        public static void ApplyAndCapture()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit owned preview before authoring.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Unsaved scene work must be preserved.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Evidence);
            try
            {
                var prepared=EditorSceneManager.OpenScene(Folder+"PreparedEntities.unity",OpenSceneMode.Single);
                var binding=EditorSceneManager.OpenScene(Folder+"RuntimeBinding.unity",OpenSceneMode.Additive);
                SceneManager.SetActiveScene(binding);
                var ground=EnsureMapGround(prepared);
                if(ground==null || !ground.HasProperty("_SandPaletteStrength")) throw new InvalidOperationException("Landscape shader has not imported.");
                ground.SetFloat("_SandPaletteStrength",1);
                ground.SetColor("_BaseColor",Color.white);
                ground.SetColor("_SandDryColor",new Color(.80f,.68f,.48f));
                ground.SetColor("_SandWornColor",new Color(.55f,.46f,.32f));
                ground.SetColor("_SandScrubColor",new Color(.43f,.51f,.29f));
                ground.SetFloat("_SandScrubCoverage",.35f);
                ground.SetColor("_MacroTintA",new Color(.96f,.91f,.78f));
                ground.SetColor("_MacroTintB",new Color(1.05f,1.03f,.96f));
                ground.SetFloat("_MacroStrength",.30f);ground.SetFloat("_GroundDetailStrength",.52f);
                ground.SetFloat("_GroundDetailNormalStrength",.42f);ground.SetFloat("_DetailStrength",.09f);
                EditorUtility.SetDirty(ground);AssetDatabase.SaveAssetIfDirty(ground);
                RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
                Game.Composition.SkirmishS004CampaignLighting.ApplyDaylightFill();
                foreach(var light in binding.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)))
                    if(light.type==LightType.Directional)
                    {
                        light.color=new Color(1f,.96f,.89f);light.intensity=1.20f;
                        light.shadowStrength=.72f;light.transform.rotation=Quaternion.Euler(48,-32,0);
                    }
                foreach(var material in prepared.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))
                    .Where(r=>r.name.StartsWith("RoadChunk_")).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct())
                    Debug.Log("[CampaignAirfieldRoadMaterial] name="+material.name+" shader="+material.shader.name+" path="+AssetDatabase.GetAssetPath(material));
                var old=binding.GetRootGameObjects().FirstOrDefault(g=>g.name==Dressing);
                if(old!=null) UnityEngine.Object.DestroyImmediate(old);
                old=prepared.GetRootGameObjects().FirstOrDefault(g=>g.name==Dressing);
                if(old!=null) UnityEngine.Object.DestroyImmediate(old);
                var root=new GameObject(Dressing);SceneManager.MoveGameObjectToScene(root,prepared);
                var bounds=prepared.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))
                    .Where(r=>r.gameObject.activeInHierarchy && !r.name.StartsWith("Ground_") &&
                        !r.name.Contains("SandWear") && !r.name.Contains("Destroyed") &&
                        !r.GetComponentsInParent<Transform>().Any(t=>t.name.Contains("Fence")) && !r.name.Contains("Paint"))
                    .Select(r=>r.bounds).ToArray();
                // Trees grow in town-side verges outside the source's playable
                // rectangle. They cannot change any mission's movement contract.
                int palms=0,details=0,rejected=0;
                var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_03.prefab");
                var grass=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab");
                var rocks=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Generic/SM_Generic_Small_Rocks_01.prefab");
                var covers=new[]{
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_ClothCover_Small_01.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_ClothCover_Small_02.prefab")};
                var cart=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Stall_01.prefab");
                var pot=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Pot_Large_02.prefab");
                int roofShades=0;
                var dressedRoofs=new List<Vector3>();
                // Colour on selected existing town and field-compound roofs reads from the player's
                // high camera. These detached renderer-only accents never change
                // building health, selection bounds, navigation or source geometry.
                foreach(var house in bounds.Where(b=>b.min.y<1 && b.size.y>4 && b.size.y<25 &&
                    b.size.x>5 && b.size.x<20 && b.size.z>5 && b.size.z<20 &&
                    b.max.x<998 && b.center.x>300 && b.center.z>320 && b.center.z<690)
                    .OrderBy(b=>Mathf.Abs(b.center.z-502)+Mathf.Abs(b.center.x-380)))
                {
                    if(roofShades>=90 || dressedRoofs.Any(p=>Vector3.Distance(p,house.center)<10)) continue;
                    var roof=new Vector3(house.center.x,house.max.y+.1f,house.center.z);
                    var shade=AddVisual(covers[roofShades%2],root.transform,roof,(roofShades%4)*90,1.65f);
                    TintCanopy(shade,roofShades%4);
                    AddVisual(pot,root.transform,roof+new Vector3(2f,0,-1.8f),roofShades*43,.85f);
                    dressedRoofs.Add(house.center);roofShades++;details+=2;
                }
                int townGroups=0;
                // Existing town courtyards are outside the airfield's playable
                // sector. Add human-scale colour and greenery in clear gaps;
                // this is dressing on the shared town, not another town layout.
                foreach(float column in new[]{313f,332f,351f,370f,392f})
                for(int j=0;j<12;j++)
                {
                    var p=new Vector3(column+(j%3-1)*1.7f,0,430+j*18+(j%2)*3);
                    if(Overlaps(bounds,p,.7f)){rejected++;continue;}
                    AddVisual(tree,root.transform,p,j*137+column,.88f+(j%3)*.06f);palms++;
                    AddVisual(grass,root.transform,p+new Vector3(.3f,0,-.4f),j*29,.7f);details++;
                    var q=p+new Vector3(-5,0,2.5f);
                    if(townGroups>=24 || Overlaps(bounds,q,3.2f)) continue;
                    AddVisual(covers[townGroups%2],root.transform,q,(j%2)*90,1);
                    AddVisual(cart,root.transform,q+new Vector3(.4f,0,-.3f),(j%2)*90,1);
                    AddVisual(pot,root.transform,q+new Vector3(-1.7f,0,1.5f),j*31,1);
                    townGroups++;details+=3;
                }
                var grid=AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>("Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Grid.asset");
                // Validate the actual near-ground mesh, not the crown bounds.
                // The largest frontage scale must fit inside a source cell.
                float trunkRadius=0;
                int trunkVertices=0;
                foreach(var filter in tree.GetComponentsInChildren<MeshFilter>(true))
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    var local=tree.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if(local.y<-.1f || local.y>.75f) continue;
                    trunkRadius=Mathf.Max(trunkRadius,new Vector2(local.x,local.z).magnitude);
                    trunkVertices++;
                }
                if(trunkVertices==0 || trunkRadius*.87f*1.6f>=.5f)
                    throw new InvalidOperationException("Frontage palm trunk exceeds its existing blocked cell.");
                var blocked=new HashSet<Vector2Int>(grid.BlockedCells);
                var wall=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_01.prefab");
                var wallProbe=UnityEngine.Object.Instantiate(wall);
                var wallBounds=new Bounds(wallProbe.transform.position,Vector3.zero);
                foreach(var renderer in wallProbe.GetComponentsInChildren<MeshRenderer>()) wallBounds.Encapsulate(renderer.bounds);
                float wallLength=Mathf.Max(wallBounds.size.x,wallBounds.size.z);
                bool wallAlongX=wallBounds.size.x>=wallBounds.size.z;
                UnityEngine.Object.DestroyImmediate(wallProbe);
                if(wallLength<1) throw new InvalidOperationException("Wall asset has no measurable span.");
                int courtyards=0,wallSegments=0;
                var yards=new List<Vector3>();
                // Selected existing homes receive yards in the town backdrop.
                // Every wall stays outside the shared playable rectangle, so
                // it cannot obstruct a mission route or production pad.
                foreach(var house in bounds.Where(b=>b.min.y<1 && b.size.y>4 && b.size.y<25 &&
                    b.size.x>4 && b.size.x<20 && b.size.z>4 && b.size.z<20 &&
                    b.max.x<390 && b.center.x>270 && b.center.z>430 && b.center.z<690)
                    .OrderBy(b=>Mathf.Abs(b.center.z-500)))
                {
                    if(courtyards>=9 || yards.Any(p=>Vector3.Distance(p,house.center)<23)) continue;
                    float left=house.min.x-3,right=house.max.x+3,south=house.min.z-3,north=house.max.z+3;
                    if(right>=398) continue;
                    int before=wallSegments;
                    void Line(Vector3 a,Vector3 b)
                    {
                        var delta=b-a;float length=delta.magnitude;
                        int count=Mathf.Max(1,Mathf.RoundToInt(length/wallLength));
                        float yaw=delta.x==0 ? (wallAlongX?90:0) : (wallAlongX?0:90);
                        for(int k=0;k<count;k++)
                        {
                            var p=Vector3.Lerp(a,b,(k+.5f)/count);
                            if(Overlaps(bounds,p,.7f)) continue;
                            AddVisual(wall,root.transform,p,yaw,1);wallSegments++;
                        }
                    }
                    Line(new Vector3(left,0,south),new Vector3(left,0,north));
                    Line(new Vector3(right,0,south),new Vector3(right,0,north));
                    Line(new Vector3(left,0,north),new Vector3(right,0,north));
                    // Leave an obvious four metre entrance toward the street.
                    float middle=(left+right)*.5f;
                    Line(new Vector3(left,0,south),new Vector3(middle-2,0,south));
                    Line(new Vector3(middle+2,0,south),new Vector3(right,0,south));
                    if(wallSegments==before) continue;
                    var palmPosition=new Vector3(right-1.2f,0,north-1.2f);
                    if(!Overlaps(bounds,palmPosition,.65f)) {AddVisual(tree,root.transform,palmPosition,courtyards*137,.8f);palms++;}
                    yards.Add(house.center);courtyards++;
                }
                bool BlockedTrunk(Vector3 p)
                {
                    for(int z=Mathf.FloorToInt(p.z-.75f);z<=Mathf.FloorToInt(p.z+.75f);z++)
                    for(int x=Mathf.FloorToInt(p.x-.75f);x<=Mathf.FloorToInt(p.x+.75f);x++)
                        if(!blocked.Contains(new Vector2Int(x,z))) return false;
                    return true;
                }
                // Dress the existing fence verges through the playable frontage,
                // including the opening base view. The source grid already blocks
                // each trunk footprint; no new obstacle or placement restriction
                // is introduced for any Campaign or Skirmish consumer.
                var frontagePalms=new List<Vector3>();
                // The source fence belts are only one or two cells deep. Sampling
                // a three-cell footprint on a nine-metre row lattice skipped
                // most of them, leaving the actual opening compound bare.
                // A centred palm trunk fits inside its existing blocked cell;
                // the crown is visual only and never changes navigation.
                foreach(var cell in blocked.Where(c=>c.x>=405 && c.x<=995 && c.y>=462 && c.y<=615)
                    .OrderBy(c=>c.y).ThenBy(c=>c.x))
                {
                    var p=new Vector3(cell.x+.5f,0,cell.y+.5f);
                    if(Overlaps(bounds,p,1.5f) ||
                        frontagePalms.Any(t=>Vector3.Distance(t,p)<5.5f)) continue;
                    AddVisual(tree,root.transform,p,(cell.x*17+cell.y*31)%360,.72f+((cell.x+cell.y)%4)*.05f);
                    frontagePalms.Add(p);palms++;
                    AddVisual(grass,root.transform,p+new Vector3(.35f,0,-.4f),cell.x+cell.y,.8f);details++;
                    // Ground-level verge clusters give each palm a planted base.
                    // These small existing props are render-only too.
                    for(int k=0;k<3;k++)
                    {
                        var q=p+new Vector3((k-1)*.7f,0,.6f+(k%2)*.4f);
                        if(Overlaps(bounds,q,.45f)) continue;
                        AddVisual(k==1?rocks:grass,root.transform,q,cell.x*13+k*73,.45f+k*.1f);details++;
                    }
                }
                // Existing blocked verge cells can carry small street-life props.
                // Their complete ground footprint stays on the inherited obstacle;
                // none becomes a new gameplay building, wall, route or compound.
                var streetProps=new[]{cart,pot,
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Crate_Wood_01.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Rusted_Barrel_01.prefab")};
                var dressedVerge=new List<Vector3>();
                foreach(var cell in blocked.Where(c=>c.x>=405 && c.x<=995 && c.y>=320 && c.y<=690)
                    .OrderBy(c=>(c.x*73+c.y*137)%997))
                {
                    if(dressedVerge.Count>=80) break;
                    var p=new Vector3(cell.x+.5f,0,cell.y+.5f);
                    if(!BlockedTrunk(p) || Overlaps(bounds,p,.8f) ||
                        dressedVerge.Any(q=>Vector3.Distance(q,p)<9f) ||
                        frontagePalms.Any(q=>Vector3.Distance(q,p)<2f)) continue;
                    AddVisual(streetProps[dressedVerge.Count%streetProps.Length],root.transform,p,
                        (cell.x*17+cell.y*31)%360,.55f);
                    dressedVerge.Add(p);details++;
                }
                // Low foliage breaks up the sandy foreground without concealing
                // troops or claiming a walkable cell as a physical obstacle.
                // Use scattered clusters with varied scale, never a tiled carpet.
                for(int j=0;j<720;j++)
                {
                    var p=new Vector3(407+(j*97)%586,0,456+(j*53)%164);
                    if(Overlaps(bounds,p,1.4f)) {rejected++;continue;}
                    AddVisual(grass,root.transform,p,j*137,1.1f+(j%5)*.16f);details++;
                }
                // Irregular planted clusters along the source frontage provide
                // a readable foreground, rather than isolated specks over a vast
                // yard. Only low foliage enters walkable cells; it has no collider.
                for(int cluster=0;cluster<42;cluster++)
                {
                    var center=new Vector3(410+(cluster*37)%120,0,462+(cluster*23)%144);
                    for(int leaf=0;leaf<7;leaf++)
                    {
                        float angle=(cluster*137+leaf*53)*Mathf.Deg2Rad;
                        var p=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(.6f+leaf*.38f);
                        if(Overlaps(bounds,p,.7f)) continue;
                        AddVisual(grass,root.transform,p,cluster*71+leaf*37,1.1f+(leaf%3)*.3f);details++;
                    }
                }
                // Fill existing blocked fence verges, never a walkable cell.
                // Owner-approved southern town dressing. Keep trunks on existing
                // blocked verges or outside playable space; ground foliage has
                // no collision or gameplay ownership.
                for(int j=0;j<100;j++)
                {
                    var p=new Vector3(405+(j*43)%595,0,312+(j*29)%140);
                    if(Overlaps(bounds,p,1.2f)) {rejected++;continue;}
                    if(BlockedTrunk(p))
                    {
                        AddVisual(tree,root.transform,p,j*137,.85f+(j%4)*.07f);palms++;
                    }
                    AddVisual(grass,root.transform,p,j*73,.55f);details++;
                }
                for(int j=0;j<46;j++)
                {
                    var p=new Vector3(410+j*13,0,290+(j%3)*2);
                    if(Overlaps(bounds,p,1.3f)) {rejected++;continue;}
                    AddVisual(tree,root.transform,p,j*137,.9f+(j%3)*.08f);palms++;
                    AddVisual(grass,root.transform,p+new Vector3(1.1f,0,1),j*53,.85f);details++;
                }
                for(int j=0;j<30;j++)
                {
                    float z=350+j*11;
                    for(int x=409;x<440;x++)
                    {
                        var p=new Vector3(x,0,z+.5f);
                        if(!BlockedTrunk(p) || Overlaps(bounds,p,1.5f)) continue;
                        AddVisual(tree,root.transform,p,j*137,.9f);palms++;
                        AddVisual(grass,root.transform,p+new Vector3(.25f,0,.25f),j*37,.6f);details++;
                        break;
                    }
                }
                foreach(float x in new[]{377f,389f,1020f,1033f})
                for(int j=0;j<24;j++)
                {
                    float z=345+j*14+(j%3)*2;
                    var p=new Vector3(x,0,z);
                    if(Overlaps(bounds,p,3f)){rejected++;continue;}
                    AddVisual(tree,root.transform,p,(j*137)%360,1);palms++;
                    for(int k=0;k<3;k++)
                    {
                        var q=p+new Vector3((k-1)*2.2f,0,(k%2==0?2:-2));
                        AddVisual(k==1?rocks:grass,root.transform,q,j*79+k*43,.7f);details++;
                    }
                }
                // Small verge details remain outside the playable rectangle too.
                // Cluster spacing varies; there is no regular carpet across pads.
                for(int j=0;j<70;j++)
                {
                    float x=410+(j*47)%570,z=j%2==0?285:716;
                    var p=new Vector3(x,0,z);
                    if(Overlaps(bounds,p,2)){rejected++;continue;}
                    AddVisual(j%3==0?rocks:grass,root.transform,p,j*137,.8f);details++;
                }
                if(root.GetComponentsInChildren<Collider>(true).Length!=0 ||
                    root.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)
                    throw new InvalidOperationException("Dressing must contain renderers only.");
                EditorSceneManager.MarkSceneDirty(binding);
                EditorSceneManager.MarkSceneDirty(prepared);
                if(!EditorSceneManager.SaveScene(prepared)) throw new IOException("Shared scenery save failed.");
                if(!EditorSceneManager.SaveScene(binding)) throw new IOException("Shared presentation save failed.");
                Capture(binding,new Vector3(450,0,540),"town-edge");
                Capture(binding,new Vector3(452,0,502.5f),"player-angle",new Vector3(59.68f,75,0));
                Capture(binding,new Vector3(452,0,502.5f),"player-angle-wide",new Vector3(75.6f,95,0));
                Capture(binding,new Vector3(388,0,530),"town");
                Capture(binding,new Vector3(700,0,560),"airfield");
                Capture(binding,new Vector3(450,0,405),"south-west-after");
                Capture(binding,new Vector3(680,0,405),"south-centre-after");
                Capture(binding,new Vector3(900,0,405),"south-east-after");
                File.WriteAllText(Evidence+"/dressing.json",JsonUtility.ToJson(new Report{palms=palms,details=details,rejected=rejected,townGroups=townGroups,courtyards=courtyards,wallSegments=wallSegments,roofShades=roofShades},true));
                Debug.Log("[CampaignAirfieldVisualAuthoring] result=Passed palms="+palms+" details="+details+" townGroups="+townGroups+" terrainGridSurfaceUnchanged=1 sharedConsumers=AllCityEdgeAirfield");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        [Serializable] private sealed class Report {public int palms,details,rejected,townGroups,courtyards,wallSegments,roofShades;public string scope="Shared CityEdgeAirfield presentation";public bool playerReady=false;}
        private static bool Overlaps(Bounds[] bounds,Vector3 p,float margin)=>bounds.Any(b=>
            p.x>b.min.x-margin && p.x<b.max.x+margin && p.z>b.min.z-margin && p.z<b.max.z+margin);
        private static readonly Color[] canopyColors={new(.48f,.80f,.76f),new(.94f,.55f,.36f),new(1f,.82f,.45f),new(.46f,.68f,.90f)};
        private static void TintCanopy(GameObject canopy,int palette)
        {
            const string folder="Assets/Game/Art/MapBeautify/CityEdgeRoofAccents";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Game/Art/MapBeautify","CityEdgeRoofAccents");
            foreach(var renderer in canopy.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var original=materials[i];
                    if(original==null || !original.HasProperty("_BaseColor")) continue;
                    string sourceGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                    if(string.IsNullOrEmpty(sourceGuid)) throw new InvalidOperationException("Canopy material must be an existing asset.");
                    string path=folder+"/Canopy_"+sourceGuid+"_"+palette+".mat";
                    var tinted=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(tinted==null) {tinted=new Material(original);AssetDatabase.CreateAsset(tinted,path);}
                    tinted.SetColor("_BaseColor",canopyColors[palette]);
                    EditorUtility.SetDirty(tinted);AssetDatabase.SaveAssetIfDirty(tinted);
                    materials[i]=tinted;
                }
                renderer.sharedMaterials=materials;
            }
        }
        private static GameObject AddVisual(GameObject prefab,Transform parent,Vector3 p,float yaw,float scale)
        {
            if(prefab==null) throw new InvalidOperationException("Existing landscape prefab missing.");
            var instance=UnityEngine.Object.Instantiate(prefab,parent);
            instance.name="Landscape_"+prefab.name;
            instance.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
            // Taller crowns read above the existing courtyard roofs. The tree
            // trunks retain their authored blocked footprints; all dressing is render only.
            // Raise crowns above the frontage roofs without increasing the
            // horizontal footprint checked against the existing blocked cells.
            instance.transform.localScale=prefab.name == "SM_Env_Tree_03"
                ? new Vector3(1.6f,2.1f,1.6f)*scale : Vector3.one*scale;
            foreach(var c in instance.GetComponentsInChildren<Component>(true))
                if(c!=null && c is not Transform && c is not MeshRenderer && c is not MeshFilter)
                    UnityEngine.Object.DestroyImmediate(c);
            return instance;
        }
        private static void Capture(Scene scene,Vector3 focus,string label,Vector3? cameraOffset=null)
        {
            var go=new GameObject("LandscapeReviewCamera");SceneManager.MoveGameObjectToScene(go,scene);
            var c=go.AddComponent<Camera>();c.fieldOfView=50;c.nearClipPlane=.3f;c.farClipPlane=3000;
            c.transform.position=focus+(cameraOffset??new Vector3(36,65,-51));c.transform.LookAt(focus);
            var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            var previousPipeline=QualitySettings.renderPipeline;
            var lighting=go.AddComponent<Game.Composition.SkirmishS004CampaignLighting>();
            try {lighting.Configure(default,scene);c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Evidence+"/"+label+".png",image.EncodeToPNG());}
            finally {RenderTexture.active=previous;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);QualitySettings.renderPipeline=previousPipeline;}
        }
    }
}
