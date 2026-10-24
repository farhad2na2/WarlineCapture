// Data/container bindings for source-linked production jobs; no World or gameplay system emulator.
using System;using System.Collections.Generic;using Unity.Entities;
namespace UnityEngine { internal static class NoEngineRuntime {} }
namespace Unity.Burst { [AttributeUsage(AttributeTargets.Struct|AttributeTargets.Method)]public sealed class BurstCompileAttribute:Attribute{} }
namespace Unity.Collections {
 [AttributeUsage(AttributeTargets.Field)]public sealed class ReadOnlyAttribute:Attribute{}
 public readonly struct NativeArray<T> {readonly T[] values;readonly Func<int,T> read;readonly int length;public NativeArray(T[] v){values=v;read=null;length=v?.Length??0;}public NativeArray(int n,Func<int,T> getter){values=null;read=getter;length=n;}public bool IsCreated=>values!=null||read!=null;public int Length=>length;public T this[int i]{get{if((uint)i>=(uint)length)throw new IndexOutOfRangeException();return values!=null?values[i]:read(i);}}}
 public readonly struct NativeBitArray {readonly bool[] values;public NativeBitArray(bool[] v){values=v;}public bool IsCreated=>values!=null;public int Length=>values?.Length??0;public bool IsSet(int i)=>values[i];}
}
namespace Unity.Jobs { internal static class NoJobScheduler {} }
namespace Unity.Entities {
 public interface IComponentData{}public interface IBufferElementData{}public interface IJobEntity{}
 [AttributeUsage(AttributeTargets.Struct)]public sealed class WithNoneAttribute:Attribute {public WithNoneAttribute(params Type[] types){}}
 [AttributeUsage(AttributeTargets.Parameter)]public sealed class EntityIndexInQueryAttribute:Attribute{}
 public struct Entity:IEquatable<Entity>{public int Index,Version;public static Entity Null=>default;public bool Equals(Entity x)=>Index==x.Index&&Version==x.Version;public override bool Equals(object x)=>x is Entity e&&Equals(e);public override int GetHashCode()=>HashCode.Combine(Index,Version);public static bool operator==(Entity a,Entity b)=>a.Equals(b);public static bool operator!=(Entity a,Entity b)=>!a.Equals(b);}
 public sealed class ComponentLookup<T> where T:struct {
 readonly Dictionary<Entity,T> values;readonly HashSet<Entity> observed;
 public ComponentLookup(HashSet<Entity> identities,Dictionary<Entity,T> present){observed=identities;values=present;}
 public bool HasComponent(Entity e){if(!observed.Contains(e))throw new NotSupportedException("Unknown component-presence fact: "+typeof(T).Name+" on "+e.Index+":"+e.Version);return values.ContainsKey(e);}
 public T this[Entity e]{get{if(!HasComponent(e))throw new NotSupportedException("Missing component value: "+typeof(T).Name);return values[e];}}
 }
 public sealed class DeferredVehicleCommand {public string Stage,Operation,Component;public int SortKey;public Entity Entity;public object Value;}
 public sealed class DeferredVehicleCommands {public readonly List<DeferredVehicleCommand> Values=new();public void Add(DeferredVehicleCommand c){if(Values.Count>=4096)throw new NotSupportedException("Deferred command work bound exceeded");Values.Add(c);}}
 public struct EntityCommandBuffer {public readonly struct ParallelWriter {
 readonly DeferredVehicleCommands sink;readonly string stage;public ParallelWriter(DeferredVehicleCommands s,string name){sink=s;stage=name;}
 public void RemoveComponent<T>(int sortKey,Entity e)=>sink.Add(new(){Stage=stage,SortKey=sortKey,Entity=e,Operation="remove",Component=typeof(T).Name});
 public void AddComponent<T>(int sortKey,Entity e)where T:struct=>AddComponent(sortKey,e,default(T));
 public void AddComponent<T>(int sortKey,Entity e,T value)where T:struct=>sink.Add(new(){Stage=stage,SortKey=sortKey,Entity=e,Operation="add",Component=typeof(T).Name,Value=value});
 }}
}
