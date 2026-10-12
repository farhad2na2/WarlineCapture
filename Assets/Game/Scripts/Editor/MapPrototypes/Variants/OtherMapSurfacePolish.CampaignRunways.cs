using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        const string FixArt="Assets/Game/Art/MapBeautify/CampaignVisualFixes20261011";
        static bool RunwayRenderer(MeshRenderer r)=>new[]{"RunwaySurface","Campaign_RunwaySurface","SM_Env_Runway_01"}.Contains(MeshName(r))||MeshName(r).StartsWith("CampaignRunway20261011")||AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh).StartsWith(FixArt+"/Runways/");
        static bool OuterRunwayEnd(MeshRenderer r,MeshRenderer[] strips,bool left)
        {
            var b=r.GetComponent<MeshFilter>().sharedMesh.bounds;
            var p=r.transform.TransformPoint(new Vector3(left?b.min.x-1:b.max.x+1,b.center.y,b.center.z));
            foreach(var other in strips.Where(x=>x!=r))
            {
                var local=other.transform.InverseTransformPoint(p);var ob=other.GetComponent<MeshFilter>().sharedMesh.bounds;
                if(local.x>ob.min.x-.05f&&local.x<ob.max.x+.05f&&Mathf.Abs(local.z-ob.center.z)<ob.extents.z*.8f&&Mathf.Abs(local.y-ob.center.y)<.5f)return false;
            }
            return true;
        }
        static Mesh DetailedRunway(MeshRenderer r,bool left,bool right,string key)
        {
            var b=r.GetComponent<MeshFilter>().sharedMesh.bounds;var verts=new List<Vector3>();var asphalt=new List<int>();var paint=new List<int>();
            float y=b.min.y,sx=r.transform.lossyScale.x,sz=r.transform.lossyScale.z;
            void Quad(float x0,float z0,float x1,float z1,bool marking)
            {
                if(x1<x0)(x0,x1)=(x1,x0);if(z1<z0)(z0,z1)=(z1,z0);
                int n=verts.Count;float h=y+(marking?.008f:0);verts.AddRange(new[]{new Vector3(x0,h,z0),new Vector3(x0,h,z1),new Vector3(x1,h,z1),new Vector3(x1,h,z0)});
                (marking?paint:asphalt).AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            Quad(b.min.x,b.min.z,b.max.x,b.max.z,false);
            float edge=1.8f/sz;Quad(b.min.x,b.min.z+edge,b.max.x,b.min.z+edge+.18f/sz,true);Quad(b.min.x,b.max.z-edge-.18f/sz,b.max.x,b.max.z-edge,true);
            // World-aligned spacing keeps center dashes continuous across adjoining modules.
            float from=b.min.x+(left?35:0)/sx,to=b.max.x-(right?35:0)/sx;
            var axis=r.transform.TransformDirection(Vector3.right).normalized;
            float phase=Vector3.Dot(r.transform.position,axis);
            float start=Mathf.Floor((phase+from*sx)/14)*14;
            for(float world=start;world<phase+to*sx;world+=14)
            {
                float x0=Mathf.Max(from,(world-phase)/sx),x1=Mathf.Min(to,(world+6-phase)/sx);
                if(x1>x0)Quad(x0,b.center.z-.17f/sz,x1,b.center.z+.17f/sz,true);
            }
            int[] digits={0x3f,0x06,0x5b,0x4f,0x66,0x6d,0x7d,0x07,0x7f,0x6f};
            void End(bool atLeft)
            {
                float direction=atLeft?1:-1,edgeX=atLeft?b.min.x:b.max.x;
                float X(float metres)=>edgeX+direction*metres/sx;
                void Stripe(float start,float length,float z,float width)=>Quad(X(start),b.center.z+z/sz,X(start+length),b.center.z+(z+width)/sz,true);
                float usable=(b.size.z*sz-7)/2;
                for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)
                {
                    float z=side*(1.4f+i*(usable-1.4f)/4);Stripe(3,9,z-.65f,1.3f);
                }
                Quad(X(.7f),b.min.z+edge,X(.95f),b.max.z-edge,true);
                var heading=r.transform.TransformDirection(new Vector3(direction,0,0));
                int number=Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg,360)/10);if(number==0)number=36;
                for(int digit=0;digit<2;digit++)
                {
                    int bits=digits[digit==0?number/10:number%10];float offset=(digit==0?-3.3f:.7f)*-direction;
                    void Bar(int bit,float along,float across,float length,float width)
                    {if((bits&(1<<bit))==0)return;float z=offset-direction*across;Stripe(16+along,length,z-width*.5f,width);}
                    // Seven-segment stencil with open gaps stays readable at gameplay scale.
                    Bar(0,5.4f,1.25f,.5f,2.5f);Bar(1,3,2.5f,2.3f,.5f);Bar(2,.6f,2.5f,2.3f,.5f);
                    Bar(3,0,1.25f,.5f,2.5f);Bar(4,.6f,0,2.3f,.5f);Bar(5,3,0,2.3f,.5f);Bar(6,2.7f,1.25f,.5f,2.5f);
                }
                // Touchdown blocks flank the centre; the threshold remains unobstructed.
                if(b.size.x*sx>100)foreach(int side in new[]{-1,1})Stripe(38,9,side*7.2f-1.1f,2.2f);
            }
            if(left)End(true);if(right)End(false);
            var mesh=new Mesh{name="CampaignRunway20261011_"+key,subMeshCount=2};mesh.SetVertices(verts);mesh.SetTriangles(asphalt,0);mesh.SetTriangles(paint,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var saved=SaveVisualMesh(FixArt+"/Runways/"+key+".asset",mesh);
            saved.name="Campaign_RunwaySurface";EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);return saved;
        }
        public static void BuildCampaignRunwayEnds()
        {
            Check();Directory.CreateDirectory(FixReport+"/runways");var setup=EditorSceneManager.GetSceneManagerSetup();int ends=0,modules=0;
            try
            {
                foreach(string map in new[]{"DenseCity","CityEdgeAirfield","CH04M04GroundedSignal"})
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);var protection=new RefinementProtection(scene);
                    var strips=SceneRenderers(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&RunwayRenderer(r)).OrderBy(r=>r.bounds.center.x).ToArray();
                    var lines=new List<string>();int n=0;
                    foreach(var r in strips)
                    {
                        bool left=OuterRunwayEnd(r,strips,true),right=OuterRunwayEnd(r,strips,false);ends+=(left?1:0)+(right?1:0);
                        var mf=r.GetComponent<MeshFilter>();var before=mf.sharedMesh.bounds;mf.sharedMesh=DetailedRunway(r,left,right,map+"_"+n++);
                        if(Mathf.Abs(before.min.x-mf.sharedMesh.bounds.min.x)>.001f||Mathf.Abs(before.max.x-mf.sharedMesh.bounds.max.x)>.001f||Mathf.Abs(before.min.z-mf.sharedMesh.bounds.min.z)>.001f||Mathf.Abs(before.max.z-mf.sharedMesh.bounds.max.z)>.001f)throw new InvalidOperationException("Runway footprint changed");
                        EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);modules++;
                        lines.Add($"{Hierarchy(r.transform)} leftOuter={left} rightOuter={right} footprint={before.size.x}x{before.size.z}");
                    }
                    GradeRunwayDunes(map,SceneRenderers(scene),strips);
                    FinishRunwayMaterials(map,strips,SceneRenderers(scene));
                    ClearRunwayLooseScenery(map,SceneRenderers(scene),strips,protection);
                    string protectedHash=protection.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene)||protectedHash!=protection.Validate(scene))throw new InvalidOperationException("Runway protected data changed");
                    File.WriteAllText(FixReport+"/runways/"+map+".txt",string.Join("\n",lines)+"\nProtected transforms, colliders, owners, mission bindings and runway XZ footprint unchanged.\n");
                }
                if(modules!=5||ends!=6)throw new InvalidOperationException($"Expected five modules / six exterior ends, got {modules}/{ends}");
                Debug.Log($"[CampaignRunwayEnds] result=Passed modules={modules} outerEnds={ends} internalJoins=2");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
