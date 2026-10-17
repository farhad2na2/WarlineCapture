using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    public interface IRigidAircraftGeometryDrawLookup
    {
        bool TryRead(Mesh mesh,int subMesh,out SkirmishRigidAircraftGeometryConfig.DrawSnapshot draw);
    }
    // Additional logical geometry metadata. The installed Transport resource and
    // its original source/draw records remain separate and unchanged.
    public sealed class SkirmishCanonicalAircraftGeometryConfig:ScriptableObject
    {
        public const string ResourceName="SkirmishCanonicalAircraftGeometry_V1";
        public int SchemaVersion=1;
        public Entry[] Roles=Array.Empty<Entry>();
        [Serializable] public sealed class Source
        {public byte VisualKind;public GameObject Prefab;public string Path,DependencyHash;}
        [Serializable] public sealed class Entry:IRigidAircraftGeometryDrawLookup
        {
            public SkirmishRoleKind Role;public string PrefabKey;
            public Source[] Sources=Array.Empty<Source>();
            public SkirmishRigidAircraftGeometryConfig.Draw[] Draws=Array.Empty<SkirmishRigidAircraftGeometryConfig.Draw>();
            [NonSerialized] Dictionary<(Mesh,int),SkirmishRigidAircraftGeometryConfig.DrawSnapshot> index;
            [NonSerialized] SkirmishRigidAircraftGeometryConfig.DrawSnapshot[] frozenDraws;
            [NonSerialized] string[] frozenSources;[NonSerialized] GameObject[] frozenPrefabs;
            [NonSerialized] SkirmishRoleKind frozenRole;[NonSerialized] string frozenKey;
            public bool Prepare()
            {
                if(!Supported(Role)||PrefabKey!=ExpectedKey(Role)||Sources==null||Draws==null||Sources.Length==0||Draws.Length==0)return false;
                if(index!=null)
                {
                    if(Role!=frozenRole||PrefabKey!=frozenKey||Sources.Length!=frozenSources.Length||Draws.Length!=frozenDraws.Length)return false;
                    for(int i=0;i<Sources.Length;i++)if(Sources[i]==null||Sources[i].Prefab==null||Sources[i].Prefab!=frozenPrefabs[i]||SourceKey(Sources[i])!=frozenSources[i])return false;
                    for(int i=0;i<Draws.Length;i++)if(!frozenDraws[i].Matches(Draws[i]))return false;
                    return true;
                }
                var kinds=new HashSet<byte>();var paths=new HashSet<string>(StringComparer.Ordinal);
                foreach(var s in Sources)if(s==null||s.VisualKind<1||s.VisualKind>3||s.Prefab==null||string.IsNullOrEmpty(s.Path)||!Hex(s.DependencyHash,32)||!kinds.Add(s.VisualKind)||!paths.Add(s.Path))return false;
                if(!kinds.Contains(1))return false;
                var prepared=new Dictionary<(Mesh,int),SkirmishRigidAircraftGeometryConfig.DrawSnapshot>();
                foreach(var d in Draws)
                {
                    if(d==null||d.Mesh==null||d.SubMesh<0||d.VertexCount<=0||d.DrawnIndexCount<=0||!Hex(d.GeometrySha256,64)||
                        !Finite(d.LocalBounds.center)||!Finite(d.LocalBounds.extents)||d.LocalBounds.extents.x<0||d.LocalBounds.extents.y<0||d.LocalBounds.extents.z<0||d.LocalBounds.size.sqrMagnitude<=0||prepared.ContainsKey((d.Mesh,d.SubMesh)))return false;
                    prepared.Add((d.Mesh,d.SubMesh),new SkirmishRigidAircraftGeometryConfig.DrawSnapshot(d));
                }
                frozenRole=Role;frozenKey=PrefabKey;frozenSources=new string[Sources.Length];frozenPrefabs=new GameObject[Sources.Length];
                for(int i=0;i<Sources.Length;i++){frozenSources[i]=SourceKey(Sources[i]);frozenPrefabs[i]=Sources[i].Prefab;}
                frozenDraws=new SkirmishRigidAircraftGeometryConfig.DrawSnapshot[Draws.Length];
                for(int i=0;i<Draws.Length;i++)frozenDraws[i]=new SkirmishRigidAircraftGeometryConfig.DrawSnapshot(Draws[i]);
                index=prepared;return true;
            }
            public bool SupportsVisual(byte visual)
            {if(!Prepare())return false;foreach(var s in Sources)if(s.VisualKind==visual)return true;return false;}
            public bool TryRead(Mesh mesh,int subMesh,out SkirmishRigidAircraftGeometryConfig.DrawSnapshot draw)
            {draw=default;return Prepare()&&index.TryGetValue((mesh,subMesh),out draw);}
            static string SourceKey(Source source)=>source.VisualKind+"|"+source.Path+"|"+source.DependencyHash;
        }
        [NonSerialized] Entry[] frozenEntries;
        public bool TryRole(SkirmishRoleKind role,string key,byte visual,out Entry result)
        {
            result=null;if(SchemaVersion!=1||Roles==null||Roles.Length!=6||!Supported(role)||key!=ExpectedKey(role))return false;
            if(frozenEntries!=null)
            {if(frozenEntries.Length!=Roles.Length)return false;for(int i=0;i<Roles.Length;i++)if(!ReferenceEquals(Roles[i],frozenEntries[i]))return false;}
            var seen=new HashSet<SkirmishRoleKind>();
            foreach(var entry in Roles)
            {if(entry==null||!seen.Add(entry.Role)||!entry.Prepare())return false;if(entry.Role==role)result=entry;}
            if(frozenEntries==null)frozenEntries=(Entry[])Roles.Clone();
            return result!=null&&result.SupportsVisual(visual);
        }
        public static SkirmishCanonicalAircraftGeometryConfig Load()=>Resources.Load<SkirmishCanonicalAircraftGeometryConfig>(ResourceName);
        public static bool Supported(SkirmishRoleKind role)=>role==SkirmishRoleKind.TransportHeli||role==SkirmishRoleKind.Drone||role==SkirmishRoleKind.Fighter||role==SkirmishRoleKind.Strike||role==SkirmishRoleKind.AttackHeli||role==SkirmishRoleKind.AttackHeliLight;
        public static string ExpectedKey(SkirmishRoleKind role)=>role switch
        {
            SkirmishRoleKind.TransportHeli=>"Unit_Veh_Helicopter_Transport",SkirmishRoleKind.Drone=>"Unit_Veh_Drone",
            SkirmishRoleKind.Fighter=>"Unit_Veh_Jet_02",SkirmishRoleKind.Strike=>"Unit_Veh_Jet_01",
            SkirmishRoleKind.AttackHeli=>"Unit_Veh_Helicopter_Attack",SkirmishRoleKind.AttackHeliLight=>"Unit_Veh_Helicopter_Attack_Small",_=>null
        };
        static bool Hex(string text,int count){if(text==null||text.Length!=count)return false;foreach(char c in text)if(!(c>='0'&&c<='9'||c>='a'&&c<='f'))return false;return true;}
        static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
    }
}
