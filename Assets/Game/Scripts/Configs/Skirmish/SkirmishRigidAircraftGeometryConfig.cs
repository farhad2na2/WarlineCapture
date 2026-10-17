using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    // Read-only build metadata, never a replacement render mesh or culling box.
    public sealed class SkirmishRigidAircraftGeometryConfig:ScriptableObject,IRigidAircraftGeometryDrawLookup
    {
        public const string ResourceName="SkirmishRigidAircraftGeometry_Transport";
        public int SchemaVersion=1;
        [Serializable] public sealed class Source
        {public GameObject Prefab;public string Path,DependencyHash;}
        [Serializable] public sealed class Draw
        {public Mesh Mesh;public int SubMesh,VertexCount,DrawnIndexCount;public Bounds LocalBounds;public string GeometrySha256;}
        public Source[] Sources=Array.Empty<Source>();
        public Draw[] Draws=Array.Empty<Draw>();
        public readonly struct DrawSnapshot
        {
            public readonly Mesh Mesh;public readonly int SubMesh,VertexCount,DrawnIndexCount;public readonly Bounds LocalBounds;public readonly string GeometrySha256;
            public DrawSnapshot(Draw d){Mesh=d.Mesh;SubMesh=d.SubMesh;VertexCount=d.VertexCount;DrawnIndexCount=d.DrawnIndexCount;LocalBounds=d.LocalBounds;GeometrySha256=d.GeometrySha256;}
            public bool Matches(Draw d)=>d!=null&&Mesh==d.Mesh&&SubMesh==d.SubMesh&&VertexCount==d.VertexCount&&DrawnIndexCount==d.DrawnIndexCount&&LocalBounds.center.Equals(d.LocalBounds.center)&&LocalBounds.size.Equals(d.LocalBounds.size)&&GeometrySha256==d.GeometrySha256;
        }
        readonly struct SourceSnapshot
        {
            readonly GameObject prefab;readonly string path,hash;
            public SourceSnapshot(Source s){prefab=s.Prefab;path=s.Path;hash=s.DependencyHash;}
            public bool Matches(Source s)=>s!=null&&prefab!=null&&prefab==s.Prefab&&path==s.Path&&hash==s.DependencyHash;
        }
        [NonSerialized] Dictionary<(Mesh,int),DrawSnapshot> index;
        [NonSerialized] DrawSnapshot[] indexedDraws;
        [NonSerialized] SourceSnapshot[] indexedSources;
        public static SkirmishRigidAircraftGeometryConfig Load()=>Resources.Load<SkirmishRigidAircraftGeometryConfig>(ResourceName);
        public bool IsValid
        {
            get
            {
                if(SchemaVersion!=1||Sources==null||Sources.Length!=3||Draws==null||Draws.Length==0)return false;
                var paths=new HashSet<string>(StringComparer.Ordinal);var keys=new HashSet<(Mesh,int)>();
                foreach(var source in Sources)if(source==null||source.Prefab==null||string.IsNullOrEmpty(source.Path)||!paths.Add(source.Path)||!Hex(source.DependencyHash,32))return false;
                foreach(var draw in Draws)
                    if(draw==null||draw.Mesh==null||draw.SubMesh<0||draw.VertexCount<=0||draw.DrawnIndexCount<=0||!Hex(draw.GeometrySha256,64)||
                        !Finite(draw.LocalBounds.center)||!Finite(draw.LocalBounds.extents)||draw.LocalBounds.extents.x<0||draw.LocalBounds.extents.y<0||draw.LocalBounds.extents.z<0||
                        draw.LocalBounds.size.sqrMagnitude<=0||!keys.Add((draw.Mesh,draw.SubMesh)))return false;
                return true;
            }
        }
        public bool TryRead(Mesh mesh,int subMesh,out DrawSnapshot result)
        {
            result=default;return TryPrepareIndex()&&index.TryGetValue((mesh,subMesh),out result);
        }
        // Freeze validated values; reject later public mutation instead of
        // silently rebuilding a potentially stale metadata object.
        public bool TryPrepareIndex()
        {
            if(index!=null)
            {
                if(SchemaVersion!=1||Draws==null||Sources==null||Draws.Length!=indexedDraws.Length||Sources.Length!=indexedSources.Length)return false;
                for(int i=0;i<Draws.Length;i++)if(!indexedDraws[i].Matches(Draws[i]))return false;
                for(int i=0;i<Sources.Length;i++)if(!indexedSources[i].Matches(Sources[i]))return false;
                return true;
            }
            if(!IsValid)return false;
            index=new Dictionary<(Mesh,int),DrawSnapshot>();indexedDraws=new DrawSnapshot[Draws.Length];indexedSources=new SourceSnapshot[Sources.Length];
            for(int i=0;i<Draws.Length;i++){var frozen=new DrawSnapshot(Draws[i]);indexedDraws[i]=frozen;index.Add((frozen.Mesh,frozen.SubMesh),frozen);}
            for(int i=0;i<Sources.Length;i++)indexedSources[i]=new SourceSnapshot(Sources[i]);
            return true;
        }
        static bool Hex(string value,int length)
        {if(value==null||value.Length!=length)return false;foreach(char c in value)if(!(c>='0'&&c<='9'||c>='a'&&c<='f'))return false;return true;}
        static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
    }
}
