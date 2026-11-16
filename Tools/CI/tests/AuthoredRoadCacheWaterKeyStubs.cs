using System;
using System.Collections;
using System.Collections.Generic;
namespace Unity.Collections { public enum Allocator { Temp } public struct NativeArray<T>:IDisposable,IEnumerable<T>{public void Dispose(){} public IEnumerator<T> GetEnumerator()=>new List<T>().GetEnumerator(); IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();} }
namespace Unity.Mathematics {
 public struct float2 { public float x,y; public float2(float x,float y){this.x=x;this.y=y;} public static float2 operator +(float2 a,float2 b)=>new(a.x+b.x,a.y+b.y); public static float2 operator -(float2 a,float2 b)=>new(a.x-b.x,a.y-b.y); public static float2 operator *(float2 a,float b)=>new(a.x*b,a.y*b); public static float2 operator /(float2 a,float b)=>new(a.x/b,a.y/b); }
 public struct float3 {public float x,y,z; public float2 xz=>new(x,z); public float3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static bool operator ==(float3 a,float3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z; public static bool operator !=(float3 a,float3 b)=>!(a==b); public override bool Equals(object o)=>false;public override int GetHashCode()=>0; public static float3 operator +(float3 a,float3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z); public static float3 operator *(float3 a,float b)=>new(a.x*b,a.y*b,a.z*b); }
 public struct int2 {public int x,y;public int2(int x,int y){this.x=x;this.y=y;} public static implicit operator int2(float2 f)=>new((int)f.x,(int)f.y);}
 public struct float4x4 {public static float4x4 identity=>default;}
 public static class math {public static bool all(bool b)=>b;public static float2 floor(float2 v)=>new((float)Math.Floor(v.x),(float)Math.Floor(v.y));public static float2 ceil(float2 v)=>new((float)Math.Ceiling(v.x),(float)Math.Ceiling(v.y));public static int2 max(int a,int2 b)=>new(Math.Max(a,b.x),Math.Max(a,b.y));public static int2 max(int2 a,int b)=>new(Math.Max(a.x,b),Math.Max(a.y,b));public static int2 min(int2 a,int2 b)=>new(Math.Min(a.x,b.x),Math.Min(a.y,b.y));public static float dot(float2 a,float2 b)=>a.x*b.x+a.y*b.y;public static float2 abs(float2 a)=>new(Math.Abs(a.x),Math.Abs(a.y));public static float abs(float a)=>Math.Abs(a);public static float min(float a,float b)=>Math.Min(a,b);public static float max(float a,float b)=>Math.Max(a,b);public static float3 transform(float4x4 m,float3 v)=>v;public static float4x4 inverse(float4x4 m)=>m;}
}
namespace Unity.Entities {
 using Unity.Collections; using Unity.Mathematics;
 public struct Entity {public int Index,Version;public static Entity Null=>default;public static bool operator ==(Entity a,Entity b)=>a.Index==b.Index&&a.Version==b.Version;public static bool operator !=(Entity a,Entity b)=>!(a==b);public override bool Equals(object o)=>false;public override int GetHashCode()=>0;}
 public sealed class World {public bool IsCreated=>true;public EntityManager EntityManager=>default;}
 public struct EntityManager {public World World=>new();public bool Exists(Entity e)=>true;public bool HasComponent<T>(Entity e)=>true;public T GetComponentData<T>(Entity e)=>default;public EntityQuery CreateEntityQuery(params ComponentType[] c)=>new();public DynamicBuffer<T> GetBuffer<T>(Entity e,bool b=false)=>default;}
 public struct ComponentType {public static ComponentType ReadOnly<T>()=>default;}
 public struct EntityQuery:IDisposable {public static int EntityCount=1;public int CalculateEntityCount()=>EntityCount;public Entity GetSingletonEntity()=>default;public bool IsEmptyIgnoreFilter=>false;public NativeArray<Entity> ToEntityArray(Allocator a)=>default;public void Dispose(){}}
 public struct DynamicBuffer<T>:IEnumerable<T> {public bool IsCreated=>true;public int Length=>0;public T this[int i]=>default;public IEnumerator<T> GetEnumerator()=>new List<T>().GetEnumerator();IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();}
}
namespace UnityEngine {
 using Unity.Mathematics;
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static implicit operator float3(Vector3 v)=>new(v.x,v.y,v.z);public static implicit operator Vector3(float3 v)=>new(v.x,v.y,v.z);}
 public struct Vector2Int {public int x,y;}
 public struct Bounds {public Vector3 min,max,center;}
 public struct Matrix4x4 {public static implicit operator float4x4(Matrix4x4 m)=>default;}
 public class Object {public static T[] FindObjectsByType<T>(FindObjectsInactive i,FindObjectsSortMode s)=>Array.Empty<T>();}
 public enum FindObjectsInactive {Include} public enum FindObjectsSortMode {None}
 public class Scene {public bool IsValid()=>true;}
 public class GameObject {public Scene scene=>new();}
 public class Transform {public Matrix4x4 localToWorldMatrix;}
 public class MeshRenderer {public GameObject gameObject=>new();public Transform transform=>new();public Bounds bounds;public T GetComponent<T>()=>default;}
 public class MeshFilter {public Mesh sharedMesh;}
 public class Mesh {public Bounds bounds;}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int FloorToInt(float a)=>(int)Math.Floor(a);public static int CeilToInt(float a)=>(int)Math.Ceiling(a);}
}
namespace Game.Components {
 using Unity.Entities;using Unity.Mathematics;
 public struct MapSurfaceComponent {public object SurfaceBlob;public bool HasSurfaceData;public float CellSize;public float3 GridOrigin;}
 public struct MapSurfaceSceneOverlayRevision {public uint Value;}
 public struct OperationMapSidewalkFootprint {public float3 WorldMin,WorldMax,LocalMin,LocalMax;public float4x4 WorldToLocal;}
 public struct GridConfig {public int Width,Height;public float CellSize;public float3 Origin;}
 public struct GridRoadSidewalk {public byte Value;}
 public struct SkirmishMatchState {}
}
namespace Game.Rendering.Contracts {public interface IRenderBoundsPresentation {} public static class RenderBoundsGateway {public static IRenderBoundsPresentation Provider;}}
namespace Game.Runtime {
 using Game.Components;using Unity.Entities;using Unity.Mathematics;
 internal static class SkirmishWaterPlacement {internal static bool[] CreateMask(EntityManager e,GridConfig g)=>null;}
 internal static class BuildingPlacementRoadSurfaceMask {internal static void Append(EntityManager e,Entity a,GridConfig g,bool[] m){}}
 internal static class BuildingPlacementValidationUtilitySystemHelper {internal static bool IsFootprintInsideGrid(UnityEngine.Vector2Int a,UnityEngine.Vector2Int b,GridConfig g)=>true;}
 internal static class RoadGridProjectionSystem {internal enum RoadFootprintKind {Sidewalk} internal static bool TryGetFootprintKind(UnityEngine.Transform t,bool b,out RoadFootprintKind k){k=default;return false;}}
}
