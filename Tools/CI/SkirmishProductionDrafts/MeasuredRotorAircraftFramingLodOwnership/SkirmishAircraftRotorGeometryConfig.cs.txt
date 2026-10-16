using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    public interface IRigidAircraftWorldDrawLookup
    {
        bool TryReadWorld(Mesh mesh,int subMesh,in SkirmishRigidAircraftGeometryConfig.DrawSnapshot draw,
            Matrix4x4 rendererToWorld,out Bounds bounds,out string failure);
    }
    // Logical draw-instance metadata. Mesh/submesh alone cannot identify a rotor
    // pivot: identical geometry may occur under different local axes or parents.
    public sealed class SkirmishAircraftRotorGeometryConfig : ScriptableObject
    {
        public const string ResourceName = "SkirmishAircraftRotorGeometry_V1";
        public int SchemaVersion = 1;
        public string NativeTemplateProvenanceSha256;
        public Binding[] Bindings = Array.Empty<Binding>();
        [Serializable] public sealed class Binding
        {
            public SkirmishRoleKind Role;
            public byte VisualKind, Axis;
            public Mesh Mesh;
            public int SubMesh;
            public string GeometrySha256, SourcePath, DependencyHash, RendererPath, BladePath;
            public Matrix4x4 MeshToBlade, BladeToVisualRoot;
            public float MinimumAxial, MaximumAxial, MaximumRadius;
        }
        public readonly struct Snapshot
        {
            public readonly SkirmishRoleKind Role;
            public readonly byte VisualKind, Axis;
            public readonly Mesh Mesh;
            public readonly int SubMesh;
            public readonly string GeometrySha256, SourcePath, DependencyHash, RendererPath, BladePath;
            public readonly Matrix4x4 MeshToBlade, BladeToVisualRoot;
            public readonly float MinimumAxial, MaximumAxial, MaximumRadius;
            public Snapshot(Binding b)
            {
                Role=b.Role;VisualKind=b.VisualKind;Axis=b.Axis;Mesh=b.Mesh;SubMesh=b.SubMesh;
                GeometrySha256=b.GeometrySha256;SourcePath=b.SourcePath;DependencyHash=b.DependencyHash;
                RendererPath=b.RendererPath;BladePath=b.BladePath;MeshToBlade=b.MeshToBlade;BladeToVisualRoot=b.BladeToVisualRoot;
                MinimumAxial=b.MinimumAxial;MaximumAxial=b.MaximumAxial;MaximumRadius=b.MaximumRadius;
            }
            public bool Matches(Binding b) => b!=null && Role==b.Role && VisualKind==b.VisualKind && Axis==b.Axis &&
                Mesh==b.Mesh && SubMesh==b.SubMesh && GeometrySha256==b.GeometrySha256 && SourcePath==b.SourcePath &&
                DependencyHash==b.DependencyHash && RendererPath==b.RendererPath && BladePath==b.BladePath &&
                MeshToBlade==b.MeshToBlade && BladeToVisualRoot==b.BladeToVisualRoot && MinimumAxial==b.MinimumAxial && MaximumAxial==b.MaximumAxial && MaximumRadius==b.MaximumRadius;
        }
        [NonSerialized] Snapshot[] frozen;
        [NonSerialized] string frozenProvenance;
        public bool Prepare()
        {
            if(SchemaVersion!=1 || !Hex(NativeTemplateProvenanceSha256,64) || Bindings==null || Bindings.Length==0)return false;
            if(frozen!=null)
            {
                if(frozen.Length!=Bindings.Length || frozenProvenance!=NativeTemplateProvenanceSha256)return false;
                for(int i=0;i<frozen.Length;i++)if(!frozen[i].Matches(Bindings[i]))return false;
                return true;
            }
            var keys=new HashSet<string>(StringComparer.Ordinal);
            foreach(var b in Bindings)
            {
                if(b==null || !HasRotors(b.Role) || b.VisualKind<1 || b.VisualKind>3 || b.Axis>2 || b.Mesh==null || b.SubMesh<0 ||
                    !Hex(b.GeometrySha256,64) || !Hex(b.DependencyHash,32) || string.IsNullOrEmpty(b.SourcePath) ||
                    string.IsNullOrEmpty(b.RendererPath) || string.IsNullOrEmpty(b.BladePath) || !Affine(b.MeshToBlade) || !Affine(b.BladeToVisualRoot) ||
                    !float.IsFinite(b.MinimumAxial) || !float.IsFinite(b.MaximumAxial) || !float.IsFinite(b.MaximumRadius) ||
                    b.MinimumAxial>b.MaximumAxial || b.MaximumRadius<=0 ||
                    !keys.Add(((byte)b.Role)+"|"+b.VisualKind+"|"+b.RendererPath+"|"+b.SubMesh))return false;
            }
            frozenProvenance=NativeTemplateProvenanceSha256;
            frozen=new Snapshot[Bindings.Length];for(int i=0;i<frozen.Length;i++)frozen[i]=new Snapshot(Bindings[i]);
            return true;
        }
        public bool TryRead(SkirmishRoleKind role,byte visual,Mesh mesh,int subMesh,byte axis,Matrix4x4 meshToBlade,
            string geometrySha,out Snapshot result)
        {
            result=default;if(!Prepare())return false;bool found=false;
            foreach(var b in frozen)
            {
                if(b.Role!=role || b.VisualKind!=visual || b.Mesh!=mesh || b.SubMesh!=subMesh || b.Axis!=axis ||
                    b.GeometrySha256!=geometrySha || !Approximately(b.MeshToBlade,meshToBlade))continue;
                // Multiple identical source instances are safe only when their
                // measured cylinder is identical. Never choose an arbitrary pivot.
                if(found && (result.MinimumAxial!=b.MinimumAxial || result.MaximumAxial!=b.MaximumAxial || result.MaximumRadius!=b.MaximumRadius))return false;
                result=b;found=true;
            }
            return found;
        }
        public bool HasRotorDraw(SkirmishRoleKind role,byte visual,Mesh mesh,int subMesh,string geometrySha)
        {
            if(!Prepare())return false;
            foreach(var b in frozen)if(b.Role==role&&b.VisualKind==visual&&b.Mesh==mesh&&b.SubMesh==subMesh&&b.GeometrySha256==geometrySha)return true;
            return false;
        }
        public bool TryReadFixedLod(SkirmishRoleKind role,byte visual,Mesh mesh,int subMesh,string geometrySha,
            Matrix4x4 rendererToVisualRoot,out Snapshot result)
        {
            result=default;if((visual!=2&&visual!=3)||!Prepare())return false;bool found=false;
            foreach(var b in frozen)
            {
                if(b.Role!=role||b.VisualKind!=visual||b.Mesh!=mesh||b.SubMesh!=subMesh||b.GeometrySha256!=geometrySha||
                    !Approximately(b.BladeToVisualRoot*b.MeshToBlade,rendererToVisualRoot))continue;
                if(found&&(result.Axis!=b.Axis||result.BladeToVisualRoot!=b.BladeToVisualRoot||result.MinimumAxial!=b.MinimumAxial||
                    result.MaximumAxial!=b.MaximumAxial||result.MaximumRadius!=b.MaximumRadius))return false;
                result=b;found=true;
            }
            return found;
        }
        public static bool HasRotors(SkirmishRoleKind role) => role==SkirmishRoleKind.TransportHeli || role==SkirmishRoleKind.Drone ||
            role==SkirmishRoleKind.AttackHeli || role==SkirmishRoleKind.AttackHeliLight;
        public static bool Approximately(Matrix4x4 a,Matrix4x4 b)
        {for(int i=0;i<16;i++)if(!float.IsFinite(a[i]) || !float.IsFinite(b[i]) || Mathf.Abs(a[i]-b[i])>1e-5f*Mathf.Max(1,Mathf.Abs(a[i])))return false;return true;}
        public static bool Affine(Matrix4x4 m)
        {for(int i=0;i<16;i++)if(!float.IsFinite(m[i]))return false;return m[3,0]==0 && m[3,1]==0 && m[3,2]==0 && m[3,3]==1 && Mathf.Abs(m.determinant)>1e-10f;}
        static bool Hex(string s,int n)
        {if(s==null || s.Length!=n)return false;foreach(char c in s)if(!(c>='0'&&c<='9'||c>='a'&&c<='f'))return false;return true;}
        public static SkirmishAircraftRotorGeometryConfig Load()=>Resources.Load<SkirmishAircraftRotorGeometryConfig>(ResourceName);
    }
}
