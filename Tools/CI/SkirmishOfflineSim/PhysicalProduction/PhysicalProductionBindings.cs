extern alias ActualCore;
// Data-only chunk/array bindings for the unchanged production method, not an ECS World emulator.
using System;using Game.Components;
namespace Unity.Burst{[AttributeUsage(AttributeTargets.Struct|AttributeTargets.Method)]public sealed class BurstCompileAttribute:Attribute{}}
namespace Unity.Collections{
 public enum Allocator{Temp}
 public readonly struct NativeArray<T>:IDisposable{readonly T[] data;public NativeArray(T[] a){data=a;}public int Length=>data?.Length??0;public T this[int i]{get=>data[i];set=>data[i]=value;}public void Dispose(){}}
}
namespace Unity.Entities{
 public interface IComponentData{}public interface ISystem{}public struct SystemState{public bool Enabled;}public sealed class SimulationSystemGroup{}
 [AttributeUsage(AttributeTargets.Struct)]public sealed class DisableAutoCreationAttribute:Attribute{}
 [AttributeUsage(AttributeTargets.Struct)]public sealed class UpdateInGroupAttribute:Attribute{public UpdateInGroupAttribute(Type type){}}
 public struct ComponentTypeHandle<T>{}
 public struct EntityManager{public ComponentTypeHandle<T> GetComponentTypeHandle<T>(bool readOnly)=>default;}
 public sealed class ArchetypeChunk{
 public BuildingResourceStorageComponent[] Storage;public BuildingResourceProductionProfileComponent[] Profile;
 public Unity.Collections.NativeArray<T> GetNativeArray<T>(ref ComponentTypeHandle<T> type){if(typeof(T)==typeof(BuildingResourceStorageComponent))return new((T[])(object)Storage);if(typeof(T)==typeof(BuildingResourceProductionProfileComponent)&&Profile!=null)return new((T[])(object)Profile);throw new NotSupportedException("Unknown production chunk data type");}
 public bool Has<T>(ref ComponentTypeHandle<T> type)=>typeof(T)==typeof(BuildingResourceProductionProfileComponent)&&Profile!=null;
 }
 public sealed class EntityQuery{
 readonly ArchetypeChunk[] chunks;public EntityQuery(ArchetypeChunk[] value){chunks=value;}
 public bool IsEmptyIgnoreFilter=>chunks.Length==0;
 public Unity.Collections.NativeArray<ArchetypeChunk> ToArchetypeChunkArray(Unity.Collections.Allocator allocator)=>new(chunks);
 }
}

// Forward actual installed Mathf, rather than creating another math implementation.
namespace UnityEngine{public static class Mathf{public static float Max(float a,float b)=>ActualCore::UnityEngine.Mathf.Max(a,b);public static int Min(int a,int b)=>ActualCore::UnityEngine.Mathf.Min(a,b);public static int FloorToInt(float f)=>ActualCore::UnityEngine.Mathf.FloorToInt(f);}}
