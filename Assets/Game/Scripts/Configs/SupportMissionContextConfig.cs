using System;
using UnityEngine;
namespace Game.Configs
{
    [CreateAssetMenu(menuName="Game/Support/Mission Context")]
    public sealed class SupportMissionContextConfig : ScriptableObject
    {
        public string MissionId,OperationMapId; public uint MissionSourceVersion,Revision=1;
        public Vector2 GroundMin,GroundMax; public Vector3 Entry,Release,Exit; public float Clearance=5;
        public int PopulationCeiling;public bool RouteAuthored;
        public SupportAuthoredGroundRegion[] Regions=Array.Empty<SupportAuthoredGroundRegion>();
        public bool TryValidate(out string error)
        {
            error=string.Empty;
            bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
            bool Point(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
            bool Bounds(Vector2 min,Vector2 max)=>Finite(min.x)&&Finite(min.y)&&Finite(max.x)&&Finite(max.y)&&min.x<max.x&&min.y<max.y;
            if(string.IsNullOrEmpty(MissionId)||System.Text.Encoding.UTF8.GetByteCount(MissionId)>61||string.IsNullOrEmpty(OperationMapId)||System.Text.Encoding.UTF8.GetByteCount(OperationMapId)>61||MissionSourceVersion==0||Revision==0||PopulationCeiling<0||!Bounds(GroundMin,GroundMax))error="Exact mission/map/version and finite ground bounds are required.";
            else if(Regions==null||Regions.Length==0)error="Positive authored ground knowledge is required.";
            else if(RouteAuthored&&(!Point(Entry)||!Point(Release)||!Point(Exit)||Release.y<12||!Finite(Clearance)||Clearance<=0))error="Authored flight route is invalid.";
            else foreach(var r in Regions)if(!Bounds(r.Min,r.Max)||r.Min.x<GroundMin.x||r.Min.y<GroundMin.y||r.Max.x>GroundMax.x||r.Max.y>GroundMax.y){error="Ground region exceeds the authored bounds.";break;}
            return error.Length==0;
        }
    }
    [Serializable] public struct SupportAuthoredGroundRegion
    {public Vector2 Min,Max;public bool Visible,Protected;}
}
