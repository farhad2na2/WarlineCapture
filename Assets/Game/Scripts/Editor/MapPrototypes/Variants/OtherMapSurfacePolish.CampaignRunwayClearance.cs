using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static bool RunwayGroundSupport(MeshRenderer[] all, Vector3 p, float referenceHeight, out float height)
        {
            height=float.NegativeInfinity;
            foreach(var r in all)
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||!Intact(r))continue;
                var b=r.bounds;if(!PlanarContains(b,p,0)||b.min.y>referenceHeight+2||b.max.y<referenceHeight-3)continue;
                string name=MeshName(r);
                if(!(name.StartsWith("Ground_")||name.StartsWith("SM_Env_Ground_")||name.StartsWith("SM_Env_SandDunes_")||r.sharedMaterials.Any(m=>m!=null&&m.shader.name=="Game/Environment/GroundMacroVariation")))continue;
                var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
                try{if(collider.Raycast(new Ray(new Vector3(p.x,referenceHeight+3,p.z),Vector3.down),out var hit,7)&&hit.normal.y>.7f)height=Mathf.Max(height,hit.point.y);}
                finally{UnityEngine.Object.DestroyImmediate(collider);}
            }
            return !float.IsNegativeInfinity(height);
        }
        static Transform RunwayLooseScenery(MeshRenderer r)
        {
            string name=MeshName(r);
            if(!(NaturalRock(r)||name.Contains("Small_Rocks")||name.Contains("Bush")||name.Contains("Grass")||name.Contains("Runway_Barrier")))return null;
            if(r.GetComponentInParent<OperationMapBuildingAuthoring>()!=null)return null;
            var generated=r.GetComponentInParent<DenseCityPresentationIdentityAuthoring>();
            var accepted=r.GetComponentInParent<OperationMapEntityPresentationIdentityAuthoring>();
            if(generated!=null&&generated.Role!=OperationMapEntityPresentationRole.RenderOnly||accepted!=null&&accepted.Role!=OperationMapEntityPresentationRole.RenderOnly)return null;
            Transform root=generated!=null?generated.transform:accepted!=null?accepted.transform:DecorationRoot(r);
            if(root==null||root.GetComponentsInParent<Collider>(true).Length>0)return null;
            foreach(var c in root.GetComponentsInChildren<Component>(true))
                if(c==null||!(c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup||c is DenseCityPresentationIdentityAuthoring||c is OperationMapEntityPresentationIdentityAuthoring))return null;
            var b=DecorationBounds(root);return b.size.x<25&&b.size.z<25?root:null;
        }
        static void ClearRunwayLooseScenery(string map,MeshRenderer[] all,MeshRenderer[] strips,RefinementProtection protection)
        {
            var candidates=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)).Select(RunwayLooseScenery).Where(t=>t!=null).Distinct().ToArray();
            var obstacles=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&(StandingScenery(r)||IsBuilding(r)||NaturalRock(r)))
                .Select(r=>(root:r.transform,bounds:r.bounds)).ToArray();
            var roads=all.Where(r=>r.name=="Asphalt"||MeshName(r).Contains("DirtRoad")||RunwayRenderer(r)).Select(r=>r.bounds).ToArray();
            var mapDefinition=AssetDatabase.LoadAssetAtPath<Game.Configs.OperationMapDefinition>(Definition(map));
            var moved=new List<Bounds>();var lines=new List<string>();
            foreach(var root in candidates)
            {
                var before=DecorationBounds(root);var runway=strips.FirstOrDefault(s=>TouchesXZ(before,s.bounds,.2f)&&before.max.y>s.bounds.min.y+.03f);if(runway==null)continue;
                var strip=runway.bounds;var options=new List<Vector3>();
                for(int pad=3;pad<=12;pad+=3)foreach(float along in new[]{0f,-5f,5f,-10f,10f,-18f,18f})
                {
                    options.Add(new Vector3(before.center.x+along,before.center.y,strip.min.z-before.extents.z-pad));
                    options.Add(new Vector3(before.center.x+along,before.center.y,strip.max.z+before.extents.z+pad));
                }
                bool placed=false;
                foreach(var center in options.OrderBy(p=>(p-before.center).sqrMagnitude))
                {
                    var proposed=new Bounds(center,before.size);var rect=XzRect(proposed,1);
                    if(rect.xMin<mapDefinition.Bounds.PlayableMin.x||rect.xMax>mapDefinition.Bounds.PlayableMax.x||rect.yMin<mapDefinition.Bounds.PlayableMin.z||rect.yMax>mapDefinition.Bounds.PlayableMax.z)continue;
                    if(roads.Any(b=>TouchesXZ(proposed,b,2))||moved.Any(b=>TouchesXZ(proposed,b,1))||obstacles.Any(o=>o.root!=root&&!o.root.IsChildOf(root)&&TouchesXZ(proposed,o.bounds,1)))continue;
                    Vector3 delta=center-before.center;
                    if(!RunwayGroundSupport(all,before.center,strip.max.y,out float originalSupport)||!RunwayGroundSupport(all,center,strip.max.y,out float targetSupport))continue;
                    if(Mathf.Abs(targetSupport-originalSupport)>1)continue;delta.y=targetSupport-originalSupport;
                    root.position+=delta;EditorUtility.SetDirty(root);PrefabUtility.RecordPrefabInstancePropertyModifications(root);protection.Moved.Add(root.GetEntityId().ToString());
                    moved.Add(DecorationBounds(root));lines.Add(Hierarchy(root)+" oldCenter="+before.center+" newCenter="+DecorationBounds(root).center);placed=true;break;
                }
                if(!placed)lines.Add("UNRESOLVED "+Hierarchy(root)+" overlaps runway; no clear verified shoulder site");
            }
            Directory.CreateDirectory(FixReport+"/runways");File.WriteAllLines(FixReport+"/runways/"+map+"-loose-clearance.txt",lines);
            Debug.Log("[CampaignRunwayLooseClearance] map="+map+" moved="+moved.Count+" unresolved="+lines.Count(l=>l.StartsWith("UNRESOLVED")));
        }
    }
}
