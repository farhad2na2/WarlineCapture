using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    // A presentation-only layer on the existing city; navigation, owners and mission pins stay intact.
    public static class MapDenseCityBeautify
    {
        internal const string Version = "dense-city-v6";
        private const string Source = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        private const string Binding = "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity";
        private const string Art = "Assets/Game/Art/MapBeautify/DenseCity";
        private const string LayerName = "BeautifyDenseCityDetails";

        public static void BuildVisuals()
        {
            var pins = AssetDatabase.FindAssets("t:OperationMapDefinition", new[] { "Assets/Game/Configs/OperationMaps" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => new SerializedObject(AssetDatabase.LoadMainAssetAtPath(p)).FindProperty("sourceBinding.sourceOperationMapId")?.stringValue == "opmap.skirmish.desert_base_01")
                .ToDictionary(p => p, MapVariantPreparationInventory.FileHash);
            if (pins.Count != 18) throw new InvalidOperationException("Dense City mission count changed: " + pins.Count);
            MapVariantBuilder.EnsureFolder(Art);
            var scene = EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
            var generated = scene.GetRootGameObjects().Single(r => r.name.StartsWith("Generated_Giant"));
            var domain = generated.transform.Find("RenderOnly");
            var previous = domain.Find(LayerName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var owners = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).OrderBy(o => o.StableId).ToArray();
            foreach(var owner in owners)
            {
                var shade=owner.IntactVisualRoot?.transform.Find("BeautifyRoofShade");
                if(shade!=null) UnityEngine.Object.DestroyImmediate(shade.gameObject);
            }
            string ownerState = OwnerState(owners);
            var surface = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MapSurfaceAuthoring>(true)).ToArray();
            var surfaceState = surface.Select(s => EditorJsonUtility.ToJson(s)).ToArray();
            var root = new GameObject(LayerName); root.transform.SetParent(domain, false);
            var random = new System.Random(44031);
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
            var clear = pins.Keys.Select(p => AssetDatabase.LoadAssetAtPath<Game.Configs.OperationMapDefinition>(p))
                .SelectMany(d => d.Anchors.ToArray()).Where(a => a.Kind != OperationMapAnchorKind.None).Select(a => (p: new Vector2(a.Position.x, a.Position.z), r: Mathf.Max(a.Radius, 0f) + (a.Kind is OperationMapAnchorKind.Objective or OperationMapAnchorKind.Base or OperationMapAnchorKind.Build or OperationMapAnchorKind.Resource ? 8f : 3f))).ToArray();
            bool NearUnit(Vector2 p) => clear.Any(c => (p - c.p).sqrMagnitude < c.r * c.r);
            var terrain = scene.GetRootGameObjects().Single(r => r.name.StartsWith("Authored")).transform.Find("RenderOnly/Terrain");
            var terrainRenderers = terrain.GetComponentsInChildren<MeshRenderer>(true);
            var colliders = terrainRenderers.Where(r => r.GetComponent<MeshFilter>() != null).Select(r =>
            {
                var c = r.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh; return c;
            }).ToArray();
            Physics.SyncTransforms();
            float Height(Vector2 p, float fallback)
            {
                return Physics.Raycast(new Vector3(p.x, 300f, p.y), Vector3.down, out var hit, 600f) ? hit.point.y : fallback;
            }
            int vegetation = 0, painted = 0, roofDetails = 0, groundMaterials = 0;
            try
            {
                // Preserve source atlas UVs and geometry, replacing olive macro tints with warm sand.
                foreach (var r in terrainRenderers)
                {
                    r.sharedMaterials = r.sharedMaterials.Select(m => SandMaterial(m)).ToArray(); groundMaterials++;
                }
                var infrastructure = domain.Find("Infrastructure").GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in infrastructure.Where(r => r.name.StartsWith("SM_Env_Ground_")))
                {
                    r.sharedMaterial = Textured("OutskirtsSand", new Color(.65f, .51f, .32f), .85f); groundMaterials++;
                }
                var roads = infrastructure.Where(r => r.name == "Asphalt" || r.name == "Dirt").OrderBy(r => r.bounds.center.x).ThenBy(r => r.bounds.center.z).ToArray();
                var roadMaterial = Clone(AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/Gray.mat"), "WornAsphalt");
                roadMaterial.SetFloat("_BaseColorInfluence", 0f);
                roadMaterial.SetColor("_BaseColor", new Color(.37f, .36f, .34f));
                roadMaterial.SetColor("_MacroTintA", new Color(.82f, .83f, .82f));
                roadMaterial.SetColor("_MacroTintB", new Color(1.08f, 1.04f, .98f));
                EditorUtility.SetDirty(roadMaterial);
                var paving = new Paint(root.transform, Textured("CityStreetAsphalt", new Color(.34f,.33f,.31f), .25f), "FlatPaving");
                var kerbs = new Paint(root.transform, Textured("KerbPaving", new Color(.5f,.47f,.4f), .25f), "KerbPaving");
                var paper = new Paint(root.transform, Solid("PaperDebris", new Color(.65f,.62f,.53f)), "PaperDebris");
                var rubble = new Paint(root.transform, Solid("SmallRubble", new Color(.38f,.33f,.25f)), "SmallRubble");
                var white = new Paint(root.transform, Solid("FadedLanePaint", new Color(.68f, .65f, .56f)), "Lanes");
                var dark = new Paint(root.transform, Solid("RoadCracks", new Color(.16f, .15f, .13f)), "Cracks");
                var sand = new Paint(root.transform, Solid("RoadSand", new Color(.61f, .46f, .29f)), "SandSpill");
                for (int i = 0; i < roads.Length; i++)
                {
                    var r = roads[i]; r.sharedMaterial = r.name == "Dirt" ? Textured("CityStreetAsphalt", new Color(.34f, .33f, .31f), .25f) : roadMaterial;
                    var c = new Vector2(r.bounds.center.x, r.bounds.center.z);
                    float y = r.bounds.max.y + .018f;
                    var forward3 = r.transform.TransformDirection(Vector3.up);
                    var forward = new Vector2(forward3.x, forward3.z).normalized;
                    if (forward.sqrMagnitude < .5f) forward = r.bounds.size.x > r.bounds.size.z ? Vector2.right : Vector2.up;
                    var side = new Vector2(-forward.y, forward.x);
                    if (r.name == "Dirt")
                    {
                        r.enabled = false;
                        paving.Strip(c-forward*5f,c+forward*5f,8.7f,y);
                        foreach(float sign in new[]{-1f,1f})
                        {
                            var edge=c+side*sign*4.9f;
                            kerbs.Strip(edge-forward*5f,edge+forward*5f,1f,y+.035f);
                            for(int k=-5;k<=5;k++) dark.Strip(edge+forward*k-side*.49f,edge+forward*k+side*.49f,.025f,y+.045f);
                        }
                    }
                    if (r.transform.parent.name.Contains("Straight"))
                    {
                        white.Strip(c - forward * 3.5f, c - forward * 1.3f, .13f, y);
                        white.Strip(c + forward * 1.3f, c + forward * 3.5f, .13f, y); painted += 2;
                    }
                    if (i % 1 == 0)
                    {
                        var p = c + side * Range(-2.5f, 2.5f); float a = Range(0, 6.28f);
                        for (int j = 0; j < 5; j++)
                        {
                            a += Range(-.7f, .7f); var next = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range(.5f, 1.2f);
                            dark.Strip(p, next, .07f, y + .008f); p = next; painted++;
                        }
                    }
                    foreach(float sign in new[]{-1f,1f})
                    for(int j=0;j<38;j++)
                    {
                        var p=c+side*sign*Range(5.6f,10.5f)+forward*Range(-5f,5f);
                        float groundY=Height(p,r.bounds.center.y);
                        rubble.Blob(p,Range(.035f,.18f),groundY+.025f,random);
                        if(j%13==0) paper.Strip(p,p+new Vector2(.2f,.3f),.2f,groundY+.03f);
                    }
                    if (i % 3 != 0) continue;
                    foreach (float sign in new[] { -1f, 1f })
                    {
                        var edge = c + side * sign * 4.8f;
                        sand.Blob(edge, Range(.8f, 2.2f), y + .015f, random); painted++;
                        var center = c + side * sign * 6.1f;
                        if (NearUnit(center)) continue;
                        for (int j = 0; j < 12; j++)
                        {
                            var p = center + new Vector2(Range(-1.7f, 1.7f), Range(-1.7f, 1.7f));
                            if (NearUnit(p)) continue;
                            string path = j % 4 == 0 ? MapVariantKits.Pebbles[j % MapVariantKits.Pebbles.Length] : MapVariantKits.GrassClumps[j % MapVariantKits.GrassClumps.Length];
                            if (LowPrefab(path, root.transform, new Vector3(p.x, Height(p, r.bounds.center.y), p.y), Range(0, 360f), .72f) != null) vegetation++;
                        }
                    }
                }
                paving.Save(); kerbs.Save(); rubble.Save(); paper.Save(); white.Save(); dark.Save(); sand.Save();
                DenseCityRoadKerbRepair.TrimGeneratedKerbs();
                // Cloth shades, rooftop storage and laundry stay inside existing building footprints.
                var fabric=new[]{new Color(.72f,.28f,.16f),new Color(.15f,.43f,.38f),new Color(.82f,.62f,.24f),new Color(.7f,.61f,.43f)};
                int clothIndex=0;
                foreach(var cover in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).Where(r => r.name.Contains("ClothCover") && r.name.Contains("Cloth_")))
                    cover.sharedMaterial=Solid("MarketFabric"+(clothIndex%4),fabric[clothIndex++%4]);
                var stalls=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.name.StartsWith("SM_Prop_Cart_Stall_01")).ToArray();
                int stallIndex=0;
                foreach(var stall in stalls)
                {
                    foreach(var child in stall.transform.Cast<Transform>().Where(c=>c.name.StartsWith("DenseCity_MarketAwning_")).ToArray())
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    var b=stall.bounds;
                    var awning=new Paint(stall.transform,Solid("StallFabric"+(stallIndex%4),fabric[stallIndex%4]),"MarketAwning_"+stallIndex++);
                    var sourceMesh=stall.GetComponent<MeshFilter>().sharedMesh;
                    var v=sourceMesh.vertices;var t=sourceMesh.triangles;
                    for(int j=0;j<t.Length;j+=3)
                    {
                        var a=stall.transform.TransformPoint(v[t[j]]);var c=stall.transform.TransformPoint(v[t[j+1]]);var d=stall.transform.TransformPoint(v[t[j+2]]);
                        if(Mathf.Min(a.y,Mathf.Min(c.y,d.y)) < b.max.y-.65f) continue;
                        var normal=Vector3.Cross(c-a,d-a).normalized;
                        if(normal.y<.2f) continue;
                        var lift=Vector3.up*.009f;
                        awning.Triangle3(a+lift,c+lift,d+lift);
                    }
                    awning.Save();
                    for(int j=0;j<3;j++)
                    {
                        var stock=stall.transform.Find("BeautifyStallStock"+j);if(stock!=null)UnityEngine.Object.DestroyImmediate(stock.gameObject);
                        var basket=LowPrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Basket_0"+(j+2)+".prefab",stall.transform,new Vector3(b.center.x+(j-1)*.6f,b.min.y+.8f,b.center.z),0f,.5f);
                        basket.name="BeautifyStallStock"+j;
                    }
                }
                int selected = 0;
                foreach (var owner in owners.Where(o => o.StableId.StartsWith("densecity.")).OrderBy(o => (o.transform.position-new Vector3(1792f,0f,740f)).sqrMagnitude))
                {
                    if (selected++ % 6 != 0 || roofDetails >= 400 || owner.IntactVisualRoot == null) continue;
                    var old = owner.IntactVisualRoot.transform.Find("BeautifyRoofShade"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                    var renderers = owner.IntactVisualRoot.GetComponentsInChildren<MeshRenderer>(true);
                    if (renderers.Length == 0) continue;
                    var bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                    if (bounds.size.x < 5f || bounds.size.z < 5f) continue;
                    string path = "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ClothCover_0" + (roofDetails % 4 + 1) + ".prefab";
                    var shade = VisualPrefab(path, owner.IntactVisualRoot.transform); shade.name = "BeautifyRoofShade";
                    var shadeBounds = BoundsOf(shade);
                    float scale = Mathf.Min(bounds.size.x * .72f / shadeBounds.size.x, bounds.size.z * .72f / shadeBounds.size.z, 1f);
                    shade.transform.localScale *= scale;
                    shadeBounds = BoundsOf(shade);
                    shade.transform.position += new Vector3(bounds.center.x - shadeBounds.center.x, bounds.max.y + .04f - shadeBounds.min.y, bounds.center.z - shadeBounds.center.z);
                    foreach(var r in shade.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial=Solid("RoofFabric"+(roofDetails%4),fabric[roofDetails%4]);
                    var laundry=new Paint(shade.transform,Solid("LaundryIvory",new Color(.74f,.68f,.55f)),"RoofLaundry_"+owner.StableId);
                    float ropeY=bounds.max.y+1.2f;
                    for(int j=0;j<4;j++)
                    {
                        float x=bounds.center.x-1.5f+j*.85f;
                        float z=bounds.center.z;
                        laundry.Quad3(new Vector3(x,ropeY,z),new Vector3(x+.58f,ropeY,z),new Vector3(x+.58f,ropeY-.7f,z),new Vector3(x,ropeY-.7f,z));
                    }
                    laundry.Save();
                    roofDetails++;
                }
            }
            finally { foreach (var c in colliders) UnityEngine.Object.DestroyImmediate(c); }
            if (OwnerState(owners) != ownerState || surface.Where((s, i) => EditorJsonUtility.ToJson(s) != surfaceState[i]).Any())
                throw new InvalidOperationException("Dense City gameplay authoring changed.");
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, Source)) throw new IOException("Dense City visual scene save failed.");
            var binding = EditorSceneManager.OpenScene(Binding, OpenSceneMode.Additive); SceneManager.SetActiveScene(binding);
            var oldLight = binding.GetRootGameObjects().SingleOrDefault(r => r.name == "BeautifyLighting"); if (oldLight != null) UnityEngine.Object.DestroyImmediate(oldLight);
            var lightObject = new GameObject("BeautifyLighting"); SceneManager.MoveGameObjectToScene(lightObject, binding);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.color = new Color(1f, .88f, .72f); light.intensity = 1.15f;
            light.shadows = LightShadows.Soft; light.shadowBias = .04f; light.shadowNormalBias = .2f;
            lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f); RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.54f, .57f, .61f);
            RenderSettings.ambientEquatorColor = new Color(.40f, .36f, .31f); RenderSettings.ambientGroundColor = new Color(.21f, .18f, .14f); RenderSettings.ambientIntensity = 1f;
            if (!EditorSceneManager.SaveScene(binding, Binding)) throw new IOException("Dense City lighting save failed.");
            foreach (var pin in pins) if (MapVariantPreparationInventory.FileHash(pin.Key) != pin.Value) throw new InvalidOperationException("Mission pin changed: " + pin.Key);
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Match.unity", OpenSceneMode.Single);
            MapVariantBeautifyPipeline.CaptureMissionCameras("opmap.skirmish.desert_base_01", "DenseCity", "after");
            Debug.Log($"[DenseCityBeautify] version={Version} result=Passed missions={pins.Count} gameplay=Unchanged materials={groundMaterials} groundCover={vegetation} roadDetails={painted} roofShades={roofDetails}");
        }

        private static void ConfigureRoadWear(Material material)
        {
            string path=Art+"/StreetWearV2.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null)
            {
                const int size=512;texture=new Texture2D(size,size,TextureFormat.RGB24,true){name="StreetWear",wrapMode=TextureWrapMode.Repeat,anisoLevel=4};
                var centers=new Vector2[10,10];var random=new System.Random(8821);
                for(int y=0;y<10;y++)for(int x=0;x<10;x++)centers[x,y]=new Vector2((x-1)*64+12+(float)random.NextDouble()*40,(y-1)*64+12+(float)random.NextDouble()*40);
                var colors=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float grain=Mathf.PerlinNoise(x/3.1f+41,y/3.1f+7),weather=Mathf.PerlinNoise(x/32f+13,y/32f+91);
                    float value=.45f+grain*.10f;
                    if(weather>.68f)value=.54f+grain*.07f;
                    if(weather<.25f)value=.40f+grain*.07f;
                    float first=10000,second=10000;var p=new Vector2(x,y);
                    for(int yy=y/64;yy<=y/64+2;yy++)for(int xx=x/64;xx<=x/64+2;xx++)
                    {
                        float d=Vector2.Distance(p,centers[xx,yy]);if(d<first){second=first;first=d;}else if(d<second)second=d;
                    }
                    if(second-first<.45f && weather<.42f)value=.16f;
                    colors[y*size+x]=new Color(value,value,value);
                }
                texture.SetPixels(colors);texture.Apply(true,false);AssetDatabase.CreateAsset(texture,path);
            }
            material.SetTexture("_DesertDetailMap",texture);material.SetTexture("_GreenDetailMap",texture);
            material.SetFloat("_GroundDetailTiling",.085f);material.SetFloat("_GroundDetailStrength",.42f);
            material.SetColor("_BaseColor",new Color(.39f,.38f,.35f));EditorUtility.SetDirty(material);
        }

        public static void TuneStreetSurfaceAndCapture()
        {
            ConfigureRoadWear(AssetDatabase.LoadAssetAtPath<Material>(Art+"/CityStreetAsphalt.mat"));AssetDatabase.SaveAssets();
            MapVariantBeautifyPipeline.CaptureMissionCameras("opmap.skirmish.desert_base_01","DenseCity","after");
            Debug.Log("[DenseCityBeautify] surfaceTuning result=Passed scope=RenderMaterialOnly");
        }

        public static void FinishStreetWearAndCapture()
        {
            ConfigureRoadWear(AssetDatabase.LoadAssetAtPath<Material>(Art+"/CityStreetAsphalt.mat"));
            var scene=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
            var owners=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).OrderBy(o=>o.StableId).ToArray();var state=OwnerState(owners);
            var detail=scene.GetRootGameObjects().Single(r=>r.name.StartsWith("Generated_Giant")).transform.Find("RenderOnly/"+LayerName);
            var old=detail.Find("DenseCity_PaperDebris");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var rubble=detail.Find("DenseCity_SmallRubble").GetComponent<MeshFilter>();var points=rubble.sharedMesh.vertices;
            var paper=new Paint(detail,Solid("PaperDebris",new Color(.65f,.62f,.53f)),"PaperDebris");int sheets=0;
            for(int i=0;i<points.Length;i+=13*13)
            {
                var p=rubble.transform.TransformPoint(points[i]);var c=new Vector2(p.x,p.z);
                paper.Strip(c,c+new Vector2(.2f,.3f),.2f,p.y+.01f);sheets++;
            }
            paper.Save();
            if(OwnerState(owners)!=state)throw new InvalidOperationException("Street wear changed gameplay authoring.");
            AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene,Source))throw new IOException("Street wear save failed.");
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Match.unity",OpenSceneMode.Single);
            MapVariantBeautifyPipeline.CaptureMissionCameras("opmap.skirmish.desert_base_01","DenseCity","after");
            Debug.Log($"[DenseCityBeautify] streetWear result=Passed paperSheets={sheets} gameplay=Unchanged");
        }

        public static void RepairFabricAndCapture()
        {
            var scene=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
            var owners=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)).OrderBy(o=>o.StableId).ToArray();
            var state=OwnerState(owners);int repaired=0;
            foreach(var filter in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.name.StartsWith("DenseCity_RoofLaundry_") || f.name.StartsWith("DenseCity_MarketAwning_")))
            {
                var mesh=filter.sharedMesh;if(mesh.name.EndsWith("_FabricNormalsFixed"))continue;var original=mesh.vertices;var indices=mesh.triangles;
                var v=new List<Vector3>(original);var t=new List<int>();
                bool awning=filter.name.StartsWith("DenseCity_MarketAwning_");
                if(awning)
                {
                    for(int i=0;i<indices.Length;i+=6)
                    {
                        var a=filter.transform.TransformPoint(original[indices[i]]);var b=filter.transform.TransformPoint(original[indices[i+1]]);var c=filter.transform.TransformPoint(original[indices[i+2]]);
                        if(Vector3.Cross(b-a,c-a).normalized.y>.2f)t.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});
                    }
                }
                else
                {
                    for(int i=0;i<indices.Length;i+=12)
                    {
                        t.AddRange(indices.Skip(i).Take(6));
                        for(int j=0;j<6;j+=3)
                        {
                            int k=v.Count;v.AddRange(new[]{original[indices[i+j+2]],original[indices[i+j+1]],original[indices[i+j]]});t.AddRange(new[]{k,k+1,k+2});
                        }
                    }
                }
                mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.name+="_FabricNormalsFixed";EditorUtility.SetDirty(mesh);repaired++;
            }
            if(OwnerState(owners)!=state)throw new InvalidOperationException("Fabric repair changed gameplay authoring.");
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Match.unity",OpenSceneMode.Single);
            MapVariantBeautifyPipeline.CaptureMissionCameras("opmap.skirmish.desert_base_01","DenseCity","after");
            Debug.Log($"[DenseCityBeautify] fabricRepair result=Passed meshes={repaired} gameplay=Unchanged");
        }

        private static string OwnerState(IEnumerable<OperationMapBuildingAuthoring> owners) => string.Join("|", owners.Select(o => $"{o.StableId}:{o.OriginCell}:{o.FootprintCells}:{o.MaxHealth}:{o.BlockerPolicy}:{o.transform.localToWorldMatrix}"));
        private static Material Clone(Material source, string name)
        {
            string path = $"{Art}/{name}.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(source); AssetDatabase.CreateAsset(m, path); }
            return m;
        }
        private static Material Solid(string name, Color color)
        {
            var m = Clone(AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_A.mat"), name);
            m.shader = Shader.Find("Universal Render Pipeline/Lit"); m.SetTexture("_BaseMap", null); m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP");
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .05f); EditorUtility.SetDirty(m); return m;
        }
        private static Material Textured(string name, Color color, float strength)
        {
            var material=Solid(name,color);
            MapVariantBeautify.ConfigureGroundDetail(material,strength);
            material.SetFloat("_BaseColorInfluence",1f);
            if(strength<.5f) { material.SetFloat("_MacroStrength",.15f); material.SetFloat("_GroundDetailTiling",.5f); }
            if(name=="CityStreetAsphalt") ConfigureRoadWear(material);
            return material;
        }
        private static Material SandMaterial(Material source)
        {
            string key = source.name.StartsWith("Warm_") ? source.name : "Warm_" + source.name;
            var m = Clone(source, key);
            if (m.HasProperty("_MacroTintA"))
            {
                m.SetColor("_MacroTintA", new Color(1f, .86f, .61f)); m.SetColor("_MacroTintB", new Color(.81f, .67f, .43f));
                MapVariantBeautify.ConfigureGroundDetail(m, .85f);
            }
            EditorUtility.SetDirty(m); return m;
        }
        private static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true); var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
        }
        private static GameObject VisualPrefab(string path, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var c in go.GetComponentsInChildren<Component>(true)) if (c is Collider or Rigidbody or MonoBehaviour) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }
        private static GameObject LowPrefab(string path, Transform parent, Vector3 p, float yaw, float maxHeight)
        {
            var go = VisualPrefab(path, parent); var bounds = BoundsOf(go);
            float scale = Mathf.Min(1.2f, maxHeight / Mathf.Max(.01f, bounds.size.y));
            go.transform.localScale *= scale; go.transform.rotation = Quaternion.Euler(0, yaw, 0); bounds = BoundsOf(go);
            go.transform.position += p - new Vector3(bounds.center.x, bounds.min.y + .03f, bounds.center.z);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
        private sealed class Paint
        {
            private readonly Transform parent; private readonly Material material; private readonly string name;
            private readonly List<Vector3> vertices = new(); private readonly List<int> triangles = new();
            public Paint(Transform p, Material m, string n) { parent = p; material = m; name = n; }
            public void Strip(Vector2 a, Vector2 b, float width, float y)
            {
                Vector2 side = new Vector2(-(b-a).y, (b-a).x).normalized * width * .5f; int i = vertices.Count;
                foreach (var p in new[] { a-side, a+side, b+side, b-side }) vertices.Add(new Vector3(p.x,y,p.y));
                triangles.AddRange(new[] { i,i+1,i+2,i,i+2,i+3 });
            }
            public void Triangle3(Vector3 a,Vector3 b,Vector3 c)
            {
                int i=vertices.Count;vertices.AddRange(new[]{a,b,c});triangles.AddRange(new[]{i,i+1,i+2});
            }
            public void Quad3(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int i=vertices.Count;vertices.AddRange(new[]{a,b,c,d,a,b,c,d});
                triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3,i+6,i+5,i+4,i+7,i+6,i+4});
            }
            public void Blob(Vector2 c, float radius, float y, System.Random random)
            {
                int i = vertices.Count; vertices.Add(new Vector3(c.x,y,c.y));
                for(int j=0;j<12;j++) { float a=j*Mathf.PI/6f, r=radius*(.65f+(float)random.NextDouble()*.35f); vertices.Add(new Vector3(c.x+Mathf.Cos(a)*r,y,c.y+Mathf.Sin(a)*r)); }
                for(int j=0;j<12;j++) triangles.AddRange(new[] { i,i+1+(j+1)%12,i+1+j });
            }
            public void Save()
            {
                var mesh = new Mesh { name = "DenseCity_" + name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices.Select(parent.InverseTransformPoint).ToList()); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string path=$"{Art}/{mesh.name}.asset"; var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(old!=null) { old.Clear(); old.indexFormat=IndexFormat.UInt32; old.vertices=mesh.vertices; old.triangles=mesh.triangles; old.RecalculateNormals(); old.RecalculateBounds(); EditorUtility.SetDirty(old); UnityEngine.Object.DestroyImmediate(mesh); mesh=old; }
                else AssetDatabase.CreateAsset(mesh,path);
                var go = new GameObject(mesh.name, typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=material; go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }
    }
}
