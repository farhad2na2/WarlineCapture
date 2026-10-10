using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        [Serializable] private sealed class RefinementReceipt
        {
            public string map,result,beforeHash,afterHash,protectedHash,protectionKind;
            public string canonicalOwnershipHash,supportVerificationHash;
            public int independentAttachmentIdentitiesRemoved;
            public VisualAttachment[] attachments;
            public int supportedProps,parkedVehicles,vehiclePalettes,roofMaterials,groundMaterials,waterEdges,wearStamps,renderersBefore,renderersAfter,trianglesBefore,trianglesAfter;
            public PropMove[] moves;
        }
        private static readonly Dictionary<string,Material> RefinementMaterials=new Dictionary<string,Material>();
        private static Material RefinementMaterial(Material original,string category,string key,Action<Material> configure)
        {
            string path=RefinementArt+"/"+category+"/"+key+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original))+".mat";
            if(RefinementMaterials.TryGetValue(path,out var cached))return cached;
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();m=new Material(original){name=category+"_"+key};AssetDatabase.CreateAsset(m,path);}
            configure(m);Save(m);RefinementMaterials[path]=m;return m;
        }
        private static void AssignMaterials(MeshRenderer r,Func<Material,Material> apply)
        {
            r.sharedMaterials=r.sharedMaterials.Select(m=>m==null?null:apply(m)).ToArray();
            EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        private static Transform VehicleRoot(MeshRenderer r)
        {
            if(!MeshName(r).StartsWith("SM_Veh_") || r.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>()!=null)return null;
            var t=r.transform;while(t.parent!=null&&t.parent.name.StartsWith("SM_Veh_"))t=t.parent;
            if(!Hierarchy(t).Contains("/Vehicles/")||t.GetComponentsInParent<Collider>(true).Length>0)return null;
            if(t.GetComponentsInChildren<Component>(true).Any(c=>c!=null&&!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer)&&!(c is LODGroup)&&c.GetType().Name!="DenseCityPresentationIdentityAuthoring"))return null;
            return t;
        }
        private static bool RefinementBuilding(MeshRenderer r)
        {
            var b=r.bounds;
            return IsBuilding(r)||(Intact(r)&&b.size.y>3&&b.size.x>3&&b.size.z>3&&b.size.x<100&&b.size.z<100&&
                r.GetComponentsInParent<Transform>(true).Any(t=>new[]{"Shop_","House_","Apartment_","Warehouse_","Factory_","Hangar_","Office_"}.Any(t.name.StartsWith)));
        }
        private static bool RoofSupport(MeshRenderer roof,Vector3 p,Vector3 size,out float y)
        {
            y=0;var mesh=roof.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)return false;
            var collider=roof.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
            try
            {
                float low=float.PositiveInfinity,high=float.NegativeInfinity;
                foreach(var d in new[]{Vector2.zero,new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(1,1)})
                {
                    var ray=new Ray(new Vector3(p.x+d.x*(size.x*.5f+.08f),roof.bounds.max.y+3,p.z+d.y*(size.z*.5f+.08f)),Vector3.down);
                    if(!collider.Raycast(ray,out var hit,roof.bounds.size.y+6)||hit.normal.y<.72f)return false;
                    low=Mathf.Min(low,hit.point.y);high=Mathf.Max(high,hit.point.y);
                }
                y=high+.025f;return high-low<.25f;
            }
            finally{UnityEngine.Object.DestroyImmediate(collider);}
        }
        private static bool GroundedAtCurrent(Bounds b,MeshRenderer[] grounds,bool centerOnly=false)
        {
            foreach(var ground in grounds.Where(r=>TouchesXZ(r.bounds,b)&&r.bounds.max.y>b.min.y-1.3f&&r.bounds.min.y<b.min.y+.6f))
            {
                var mesh=ground.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                var collider=ground.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
                try
                {
                    bool supported=true;
                    foreach(var d in centerOnly?new[]{Vector2.zero}:new[]{Vector2.zero,new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(1,1)})
                    {
                        var origin=new Vector3(b.center.x+d.x*b.extents.x*.7f,b.min.y+.6f,b.center.z+d.y*b.extents.z*.7f);
                        if(!collider.Raycast(new Ray(origin,Vector3.down),out var hit,1.9f)||hit.normal.y<.55f){supported=false;break;}
                    }
                    if(supported)return true;
                }
                finally{UnityEngine.Object.DestroyImmediate(collider);}
            }
            return false;
        }
        private static void FixUnsupported(RefinementReceipt receipt,MeshRenderer[] visible,List<PropMove> moves,RefinementProtection protection)
        {
            var structures=visible.Where(r=>RefinementBuilding(r)||MeshName(r).ToLowerInvariant().Contains("platform")).ToArray();
            var staticRoofs=structures.Where(r=>r.bounds.size.y>3).ToArray();
            var occupied=new List<Bounds>();var unresolved=new List<string>();
            var grounds=visible.Where(r=>r.bounds.size.x>100||r.bounds.size.z>100||MeshName(r).StartsWith("SM_Env_Ground_")||Hierarchy(r.transform).Contains("/Ground/")).ToArray();
            // Terrain tiles include hills in their AABB. They cannot obstruct a ray-verified roof.
            var obstacles=visible.Where(r=>!grounds.Contains(r)&&!RefinementBuilding(r)&&r.bounds.size.y>.1f&&r.bounds.size.x<100&&r.bounds.size.z<100).ToArray();
            var roofOffsets=(from x in new[]{0,-1,1,-2,2} from z in new[]{0,-1,1,-2,2} select new Vector2(x*.15f,z*.15f)).ToArray();
            bool Candidate(MeshRenderer r)
            {
                string n=MeshName(r),path=Hierarchy(r.transform);
                // Detached house furnishings live in migrated render-only or landscape dressing groups.
                // Industrial props in their authored ground compounds are not rooftop candidates.
                bool houseFurnishing=n.StartsWith("SM_Prop_")&&(path.Contains("SharedLandscapeDressing")||path.Contains("/RenderOnly/Props/"));
                return (RoofProp(r)||houseFurnishing||n.StartsWith("SM_Bld_Roof_Cap")||(path.Contains("/RenderOnly/Infrastructure/")&&n.StartsWith("SM_Prop_Pipeline_")&&(n.Contains("Platform")||n.Contains("Support")))) &&
                    !new[]{"Wire","Powerpole","Powerline","Street_Light","Net","Cable","Ladder","ClothCover"}.Any(n.Contains);
            }
            var candidates=visible.Where(Candidate).Select(DecorationRoot).Where(t=>t!=null).Distinct().ToArray();
            foreach(var root in candidates.Where(t=>!candidates.Any(parent=>parent!=t&&t.IsChildOf(parent))).OrderBy(t=>t.name.Contains("Pipeline_")?0:1).ThenBy(Hierarchy))
            {
                var b=DecorationBounds(root);if(b.min.y<.5f)continue;
                // Side-mounted air conditioners and awnings are valid when their backs touch a wall.
                bool wallFixture=new[]{"Airconditioner","Awning","PowerBox","Sign_","Pipeline_"}.Any(root.name.Contains);
                // Overlapping orphan platforms can otherwise falsely support one another.
                bool platform=root.name.Contains("Pipeline_")&&root.name.Contains("Platform");
                var currentSupports=visible.Where(r=>!r.transform.IsChildOf(root)&&
                    (!platform||(!MeshName(r).Contains("Platform")&&!MeshName(r).Contains("WaterTank")))).ToArray();
                if(GroundedAtCurrent(b,currentSupports,true)||(wallFixture&&WallAttachedAtCurrent(b,structures.Where(r=>!platform||!MeshName(r).Contains("Platform")).ToArray(),root)))continue;
                if(b.min.y<3.5f&&TryGroundDecoration(root,b,grounds,out var grounded))
                {moves.Add(MoveDecoration(root,grounded,root.eulerAngles.y,"orphan low furnishing grounded at its existing footprint"));protection.Moved.Add(root.GetEntityId().ToString());receipt.supportedProps++;continue;}
                bool placed=false;
                foreach(var roof in staticRoofs.Where(r=>(r.bounds.center-b.center).sqrMagnitude<=110*110).OrderBy(r=>r.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>()==null?0:1).ThenBy(r=>(r.bounds.center-b.center).sqrMagnitude))
                {
                    if((roof.bounds.center-b.center).sqrMagnitude>110*110)continue;
                    var rb=roof.bounds;
                    foreach(var offset in roofOffsets)
                    {
                        var p=new Vector3(rb.center.x+rb.size.x*offset.x,0,rb.center.z+rb.size.z*offset.y);
                        if(!RoofSupport(roof,p,b.size,out float y))continue;p.y=y+b.extents.y;
                        var proposed=new Bounds(p,b.size);if(occupied.Any(x=>x.Intersects(proposed))||!FitsRepairedFootprint(roof.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>(),proposed))continue;
                        if(obstacles.Any(r=>!r.transform.IsChildOf(root)&&r!=roof&&r.bounds.Intersects(proposed)))continue;
                        moves.Add(MoveDecoration(root,root.position+p-b.center,root.eulerAngles.y,"detached roof decoration placed on a ray-verified supported roof"));
                        protection.Moved.Add(root.GetEntityId().ToString());
                        AttachRepairedDecoration(root,roof.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>(),protection);
                        occupied.Add(proposed);receipt.supportedProps++;placed=true;break;
                    }
                    if(placed)break;
                }
                // A tank or detached industrial platform can use a clear service yard
                // when every nearby rooftop is occupied. Keep the same scene bounds,
                // ray-check the whole footprint, and leave roads and other props clear.
                if(!placed&&(platform||root.name.Contains("WaterTank")))
                {
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(receipt.map));
                    var offsets=(from x in new[]{0,-4,4,-8,8,-12,12,-16,16} from z in new[]{0,-4,4,-8,8,-12,12,-16,16} select new Vector2(x,z)).OrderBy(v=>v.sqrMagnitude);
                    foreach(var offset in offsets)
                    {
                        var p=b.center+new Vector3(offset.x,0,offset.y);
                        if(p.x-b.extents.x<def.Bounds.PlayableMin.x||p.x+b.extents.x>def.Bounds.PlayableMax.x||p.z-b.extents.z<def.Bounds.PlayableMin.z||p.z+b.extents.z>def.Bounds.PlayableMax.z)continue;
                        var footprint=new Bounds(p,b.size);
                        if(obstacles.Any(r=>!r.transform.IsChildOf(root)&&TouchesXZ(footprint,r.bounds,.6f))||
                            visible.Any(r=>(r.name=="Asphalt"||MeshName(r).Contains("Runway")||MeshName(r).Contains("DirtRoad"))&&TouchesXZ(footprint,r.bounds,.6f)))continue;
                        foreach(var ground in grounds.Where(r=>!NaturalWater(r)&&TouchesXZ(footprint,r.bounds)).OrderByDescending(r=>r.bounds.max.y))
                        {
                            if(!RoofSupport(ground,p,b.size,out float y))continue;
                            p.y=y+b.extents.y;var proposed=new Bounds(p,b.size);
                            if(occupied.Any(x=>x.Intersects(proposed)))continue;
                            moves.Add(MoveDecoration(root,root.position+p-b.center,root.eulerAngles.y,"orphan industrial decoration grounded in a clear nearby service yard"));
                            protection.Moved.Add(root.GetEntityId().ToString());occupied.Add(proposed);receipt.supportedProps++;placed=true;break;
                        }
                        if(placed)break;
                    }
                }
                if(!placed)unresolved.Add(Hierarchy(root)+" bounds="+b);
            }
            if(unresolved.Count>0)throw new InvalidOperationException("No supported roof for detached decorations (roof candidates="+staticRoofs.Length+"):\n"+string.Join("\n",unresolved));
        }
        private static void RefineParking(RefinementReceipt receipt,MeshRenderer[] visible,OperationMapDefinition def,List<PropMove> moves)
        {
            var roads=visible.Where(r=>r.name=="Asphalt"||MeshName(r).Contains("Runway")||MeshName(r).Contains("DirtRoad")).Select(r=>r.bounds).ToArray();
            var hard=visible.Where(r=>r.bounds.size.y>.6f&&r.bounds.size.x>.5f&&r.bounds.size.z>.5f&&r.bounds.size.x<100&&r.bounds.size.z<100).ToArray();
            var vehicles=visible.Select(VehicleRoot).Where(t=>t!=null).Distinct().OrderBy(t=>t.position.x).ThenBy(t=>t.position.z).ToArray();
            for(int i=0;i<vehicles.Length;i++)
            {
                var root=vehicles[i];var b=DecorationBounds(root);if(b.size.x>15||b.size.z>15||b.size.y>8)continue;
                if(i%7==3)
                {
                    foreach(var offset in new[]{new Vector3(0,0,11),new Vector3(0,0,-11),new Vector3(11,0,0),new Vector3(-11,0,0),new Vector3(0,0,18),new Vector3(0,0,-18)})
                    {
                        var p=b.center+offset;var proposed=new Bounds(p,b.size);
                        if(p.x-b.extents.x<def.Bounds.PlayableMin.x||p.x+b.extents.x>def.Bounds.PlayableMax.x||p.z-b.extents.z<def.Bounds.PlayableMin.z||p.z+b.extents.z>def.Bounds.PlayableMax.z)continue;
                        if(roads.Any(x=>TouchesXZ(proposed,x,1.5f))||hard.Any(r=>!r.transform.IsChildOf(root)&&TouchesXZ(proposed,r.bounds,1.2f)))continue;
                        moves.Add(MoveDecoration(root,root.position+offset,root.eulerAngles.y,"parking gap with adjacent clear service bay"));receipt.parkedVehicles++;break;
                    }
                }
                else if(i%4==1)
                {
                    var move=MoveDecoration(root,root.position,root.eulerAngles.y,"alternating parked vehicle direction inside the existing bay");
                    root.Rotate(Vector3.up,180,Space.World);
                    var after=DecorationBounds(root);root.position+=b.center-after.center;
                    move.after=root.position;EditorUtility.SetDirty(root);PrefabUtility.RecordPrefabInstancePropertyModifications(root);moves.Add(move);receipt.parkedVehicles++;
                }
                int palette=i%4;
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(MeshName(r).Contains("Glass")||MeshName(r).Contains("Wheel"))continue;
                    AssignMaterials(r,m=>RefinementMaterial(m,"Vehicles",palette.ToString(),v=>
                    {
                        var color=new[]{new Color(.80f,.86f,.78f),new Color(.72f,.79f,.84f),new Color(.94f,.82f,.69f),new Color(.77f,.76f,.71f)}[palette];
                        if(v.HasProperty("_BaseColor"))v.SetColor("_BaseColor",m.GetColor("_BaseColor")*color);
                    }));receipt.vehiclePalettes++;
                }
            }
        }
        private sealed class ActivityField
        {
            public readonly int Width,Height;public readonly Color[] Pixels;public readonly Vector4 Rect;public int Stamps;
            public ActivityField(OperationMapDefinition def)
            {
                Rect=new Vector4(def.Bounds.PlayableMin.x-35,def.Bounds.PlayableMin.z-35,def.Bounds.PlayableMax.x-def.Bounds.PlayableMin.x+70,def.Bounds.PlayableMax.z-def.Bounds.PlayableMin.z+70);
                Width=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Rect.z*1.6f)),512,2048);Height=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Rect.w*1.6f)),512,2048);Pixels=new Color[Width*Height];
            }
            public void Stamp(float x,float z,float rx,float rz,int channel,float strength)
            {
                int x0=Mathf.Clamp(Mathf.FloorToInt((x-rx-Rect.x)/Rect.z*Width),0,Width-1),x1=Mathf.Clamp(Mathf.CeilToInt((x+rx-Rect.x)/Rect.z*Width),0,Width-1);
                int z0=Mathf.Clamp(Mathf.FloorToInt((z-rz-Rect.y)/Rect.w*Height),0,Height-1),z1=Mathf.Clamp(Mathf.CeilToInt((z+rz-Rect.y)/Rect.w*Height),0,Height-1);
                for(int j=z0;j<=z1;j++)for(int i=x0;i<=x1;i++)
                {
                    float dx=(Rect.x+(i+.5f)/Width*Rect.z-x)/rx,dz=(Rect.y+(j+.5f)/Height*Rect.w-z)/rz;
                    float value=Mathf.Clamp01(1-dx*dx-dz*dz)*strength;
                    int id=j*Width+i;var c=Pixels[id];c[channel]=Mathf.Max(c[channel],value);Pixels[id]=c;
                }
                Stamps++;
            }
            public Texture2D Save(string map)
            {
                var texture=new Texture2D(Width,Height,TextureFormat.RGBA32,false,true);texture.SetPixels(Pixels);texture.Apply();
                string path=RefinementArt+"/Activity/"+map+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.maxTextureSize=2048;
                foreach(var platform in new[]{"Android","iPhone"}){var settings=importer.GetPlatformTextureSettings(platform);settings.name=platform;settings.overridden=true;settings.maxTextureSize=2048;settings.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(settings);}
                importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
        private static void ActivityWear(RefinementReceipt receipt,MeshRenderer[] visible,OperationMapDefinition def)
        {
            var field=new ActivityField(def);
            foreach(var b in visible.Where(RefinementBuilding).Select(r=>r.bounds))
            {
                field.Stamp(b.min.x,b.center.z,2,b.extents.z+1,3,.75f);field.Stamp(b.max.x,b.center.z,2,b.extents.z+1,3,.65f);
                field.Stamp(b.center.x,b.min.z,b.extents.x+1,2,3,.75f);field.Stamp(b.center.x,b.max.z,b.extents.x+1,2,3,.6f);
                field.Stamp(b.center.x,b.min.z-1,1.7f,2.4f,0,.45f);
            }
            foreach(var r in visible.Where(r=>NaturalRock(r)&&r.bounds.size.y>1.2f))
            {
                var b=r.bounds;field.Stamp(b.center.x,b.center.z,b.extents.x+2,b.extents.z+2,2,.8f);
            }
            foreach(var root in visible.Select(VehicleRoot).Where(t=>t!=null).Distinct())
            {
                var b=DecorationBounds(root);field.Stamp(b.center.x,b.center.z,1.5f,2.3f,0,.95f);
                bool vertical=b.size.z>b.size.x;
                for(int side=-1;side<=1;side+=2)
                    field.Stamp(b.center.x+(vertical?side*1.2f:-3),b.center.z+(vertical?-3:side*1.2f),vertical?.28f:4,vertical?4:.28f,1,.8f);
            }
            var roads=visible.Where(r=>r.name=="Asphalt"&&Mathf.Min(r.bounds.size.x,r.bounds.size.z)>3).ToArray();
            foreach(var r in roads)
            {
                var b=r.bounds;bool vertical=b.size.z>b.size.x;
                // Broad, quiet shoulder dust plus short paired tire paths at the authored ends.
                if(vertical){field.Stamp(b.min.x,b.center.z,1.1f,b.extents.z,3,.65f);field.Stamp(b.max.x,b.center.z,1.1f,b.extents.z,3,.65f);}
                else{field.Stamp(b.center.x,b.min.z,b.extents.x,1.1f,3,.65f);field.Stamp(b.center.x,b.max.z,b.extents.x,1.1f,3,.65f);}
            }
            var texture=field.Save(receipt.map);receipt.wearStamps=field.Stamps;
            foreach(var r in visible.Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.shader.name=="Game/Environment/GroundMacroVariation")))
            {
                AssignMaterials(r,m=>m.shader.name!="Game/Environment/GroundMacroVariation"?m:RefinementMaterial(m,"Ground",receipt.map,g=>{g.SetTexture("_ActivityMap",texture);g.SetVector("_ActivityRect",field.Rect);g.SetFloat("_ActivityStrength",.85f);}));receipt.groundMaterials++;
            }
        }
        private static Mesh BankedWater(MeshRenderer r,MeshRenderer[] waters,string map,int index)
        {
            var b=r.bounds;bool alongZ=b.size.z>=b.size.x;
            bool adjacentMin=waters.Any(w=>w!=r&&Mathf.Abs((alongZ?w.bounds.max.z-b.min.z:w.bounds.max.x-b.min.x))<.6f&&Mathf.Abs(alongZ?w.bounds.center.x-b.center.x:w.bounds.center.z-b.center.z)<1);
            bool adjacentMax=waters.Any(w=>w!=r&&Mathf.Abs((alongZ?w.bounds.min.z-b.max.z:w.bounds.min.x-b.max.x))<.6f&&Mathf.Abs(alongZ?w.bounds.center.x-b.center.x:w.bounds.center.z-b.center.z)<1);
            // Close only the existing sub-metre tile joints. Road-sized interruptions retain caps.
            float x0=b.min.x,x1=b.max.x,z0=b.min.z,z1=b.max.z;
            if(alongZ){if(adjacentMin)z0-=.217f;if(adjacentMax)z1+=.217f;}else{if(adjacentMin)x0-=.217f;if(adjacentMax)x1+=.217f;}
            var v=new List<Vector3>();var c=new List<Color>();var triangles=new List<int>();
            void Quad(Vector3 a,Vector3 d,Vector3 e,Vector3 f,Color color)
            {
                int n=v.Count;Vector3 Local(Vector3 point){var p=r.transform.InverseTransformPoint(point);return new Vector3(Mathf.Round(p.x*10000)/10000,Mathf.Round(p.y*10000)/10000,Mathf.Round(p.z*10000)/10000);}
                v.Add(Local(a));v.Add(Local(d));v.Add(Local(e));v.Add(Local(f));
                for(int k=0;k<4;k++)c.Add(color);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            float y=b.center.y,lip=Mathf.Min(.36f,Mathf.Min(b.size.x,b.size.z)*.06f),top=y+.09f;
            Quad(new Vector3(x0,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z1),new Vector3(x1,y,z0),new Color(1,1,1,0));
            void Edge(Vector3 a,Vector3 d,Vector3 inset)
            {
                var shade=new Color(.41f,.41f,.37f,1);a.y=top;d.y=top;
                Quad(a,d,d+inset,a+inset,shade);
                var ai=a+inset;var di=d+inset;var low=new Vector3(0,-.16f,0);
                Quad(ai,di,di+low,ai+low,new Color(.25f,.29f,.27f,1));
            }
            if(alongZ||!adjacentMin)Edge(new Vector3(x0,top,z0),new Vector3(x0,top,z1),new Vector3(lip,0,0));
            if(alongZ||!adjacentMax)Edge(new Vector3(x1,top,z1),new Vector3(x1,top,z0),new Vector3(-lip,0,0));
            if(!alongZ||!adjacentMin)Edge(new Vector3(x1,top,z0),new Vector3(x0,top,z0),new Vector3(0,0,lip));
            if(!alongZ||!adjacentMax)Edge(new Vector3(x0,top,z1),new Vector3(x1,top,z1),new Vector3(0,0,-lip));
            string key=WaterMeshKey(v.ToArray(),triangles.ToArray(),c.ToArray());
            string path=RefinementArt+"/WaterMeshes/Bank_"+key+".asset";
            var shared=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(shared!=null)return shared;
            var mesh=new Mesh{name="CampaignWaterBank_"+key};mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            return SaveVisualMesh(path,mesh);
        }
        public static void BuildRefinement()
        {
            Check();RefinementMaterials.Clear();Directory.CreateDirectory(RefinementArt);Directory.CreateDirectory(RefinementArt+"/WaterMeshes");AssetDatabase.Refresh();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in VisualMaps)
                {
                    string source=Source(map),receiptPath=RefinementReport+"/"+map+"-result.json";
                    if(File.Exists(receiptPath))
                    {
                        var done=JsonUtility.FromJson<RefinementReceipt>(File.ReadAllText(receiptPath));
                        if(done.result=="Passed"&&done.afterHash==Hash(source)){Debug.Log("[MapRefinement] preserved "+map);continue;}
                        throw new InvalidOperationException("Inspect incomplete or changed refinement receipt "+map);
                    }
                    var audit=ReadRefinementAudit(map);
                    if(audit.sourceHash!=Hash(source))throw new InvalidOperationException("Refinement source differs from audited baseline "+map);
                    Directory.CreateDirectory(RefinementReport+"/local-baselines");string backup=RefinementReport+"/local-baselines/"+map+".unity";
                    if(!File.Exists(backup))File.Copy(source,backup,false);else if(Hash(backup)!=audit.sourceHash)throw new InvalidOperationException("Baseline changed "+map);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var rs=SceneRenderers(scene);
                    var protection=new RefinementProtection(scene);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,30)).ToArray();
                    var receipt=new RefinementReceipt{map=map,beforeHash=audit.sourceHash,renderersBefore=rs.Length,trianglesBefore=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3)};
                    Debug.Log("[MapRefinement] checking supports map="+map);
                    var moves=new List<PropMove>();FixUnsupported(receipt,visible,moves,protection);
                    var vehiclePoses=visible.Select(VehicleRoot).Where(t=>t!=null).Distinct().ToDictionary(t=>t,t=>new Tuple<Vector3,Quaternion>(t.position,t.rotation));
                    RefineParking(receipt,visible,def,moves);
                    foreach(var pair in vehiclePoses)if(pair.Key.position!=pair.Value.Item1||pair.Key.rotation!=pair.Value.Item2)protection.Moved.Add(pair.Key.GetEntityId().ToString());
                    foreach(var r in visible.Where(r=>RefinementBuilding(r)||MeshName(r).Contains("Roof_Cap")))
                    {
                        int variant=Math.Abs(Mathf.RoundToInt(r.bounds.center.x/11)+Mathf.RoundToInt(r.bounds.center.z/17))%3;
                        AssignMaterials(r,m=>m.renderQueue>=2450?m:RefinementMaterial(m,"Roofs",variant.ToString(),roof=>
                        {
                            roof.shader=Shader.Find("Game/Environment/CampaignBuildingWeathering");roof.SetFloat("_Weathering",.8f);
                            foreach(var property in new[]{"_BaseMap","_Albedo_Map","_MainTex"})
                                if(m.HasProperty(property)&&m.GetTexture(property)!=null){roof.SetTexture("_BaseMap",m.GetTexture(property));roof.SetTextureScale("_BaseMap",m.GetTextureScale(property));roof.SetTextureOffset("_BaseMap",m.GetTextureOffset(property));break;}
                            if(m.HasProperty("_BaseColor"))roof.SetColor("_BaseColor",m.GetColor("_BaseColor"));else if(m.HasProperty("_Color"))roof.SetColor("_BaseColor",m.GetColor("_Color"));
                            roof.SetColor("_RoofTint",new[]{new Color(.45f,.43f,.37f),new Color(.42f,.47f,.48f),new Color(.56f,.48f,.38f)}[variant]);
                            roof.SetColor("_DustColor",new Color(.67f,.61f,.50f));roof.SetFloat("_Smoothness",.10f);
                        }));receipt.roofMaterials++;
                    }
                    ActivityWear(receipt,visible,def);
                    var waters=visible.Where(NaturalWater).ToArray();
                    for(int i=0;i<waters.Length;i++)
                    {
                        var r=waters[i];var filter=r.GetComponent<MeshFilter>();filter.sharedMesh=BankedWater(r,waters,map,i);EditorUtility.SetDirty(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                        AssignMaterials(r,m=>RefinementMaterial(m,"Water",map,w=>{w.shader=Shader.Find("Game/Environment/CampaignWater");w.SetFloat("_BankColors",1);}));receipt.waterEdges++;
                    }
                    receipt.moves=moves.ToArray();receipt.attachments=protection.Attachments.ToArray();receipt.protectionKind="native-component-and-transform-invariants-v1";receipt.protectedHash=protection.Validate(scene);GC.Collect();
                    AssetDatabase.SaveAssets();receipt.result="Pending native invariant check";File.WriteAllText(receiptPath+".pending.json",JsonUtility.ToJson(receipt,true));
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed "+map);
                    if(receipt.protectedHash!=protection.Validate(scene))throw new InvalidOperationException("Protected scene data changed "+map);
                    receipt.renderersAfter=SceneRenderers(scene).Length;receipt.trianglesAfter=rs.Sum(r=>(r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length??0)/3);
                    if(receipt.renderersAfter!=receipt.renderersBefore)throw new InvalidOperationException("Unexpected added scene renderer "+map);
                    foreach(var m in rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(RefinementArt)).Distinct())
                        if(m.shader==null||ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Refinement shader error "+m.name);
                    receipt.afterHash=Hash(source);receipt.result="Passed";File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt,true));
                    Debug.Log($"[MapRefinement] map={map} result=Passed props={receipt.supportedProps} parking={receipt.parkedVehicles} roofs={receipt.roofMaterials} waterEdges={receipt.waterEdges} wearStamps={receipt.wearStamps} triangleDelta={receipt.trianglesAfter-receipt.trianglesBefore}");
                }
                Debug.Log("[MapRefinement] result=Passed maps=11 protectedSceneData=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
