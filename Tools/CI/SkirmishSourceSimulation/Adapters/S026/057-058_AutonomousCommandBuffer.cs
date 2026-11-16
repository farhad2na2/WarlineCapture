using System;using System.Linq;using System.Reflection;using System.Collections.Generic;using Unity.Collections;
namespace Unity.Entities {
// Owned logical ECB backend: source commands, source playback boundary, no native allocator claim.
public struct EntityCommandBuffer:IDisposable {
 internal sealed class Command{internal int Key,Sequence;internal Type Type;internal Entity Entity;internal string Op;internal object Value;}
 internal sealed class Owner{internal static int NextIdentity;internal readonly int Identity=System.Threading.Interlocked.Increment(ref NextIdentity);internal readonly List<Command> Rows=new List<Command>();internal int Deferred;internal bool Played,Disposed;}
 readonly Owner owner;
 public EntityCommandBuffer(Allocator a){owner=new Owner();}
 void Alive(){if(owner==null||owner.Played||owner.Disposed||owner.Rows.Count>=65536)throw new NotSupportedException("Bounded ECB ownership required");}
 void Put(int key,Entity e,Type t,object value,string op){Alive();owner.Rows.Add(new Command{Key=key,Sequence=owner.Rows.Count,Entity=e,Type=t,Value=value,Op=op});}
 Entity Deferred(string op,object value){Alive();if(owner.Identity<=0)throw new NotSupportedException("ECB identity exhausted");var e=new Entity{Index=-checked(++owner.Deferred),Version=owner.Identity};Put(0,e,null,value,op);return e;}
 public Entity CreateEntity()=>Deferred("create",null);
 public Entity Instantiate(Entity prefab)=>Deferred("instantiate",prefab);
 public void AddComponent<T>(Entity e,T value)=>Put(0,e,typeof(T),value,"add");
 public void AddComponent<T>(Entity e)=>AddComponent(e,Activator.CreateInstance<T>());
 public void SetComponent<T>(Entity e,T value)=>Put(0,e,typeof(T),value,"set");
 public void RemoveComponent<T>(Entity e)=>Put(0,e,typeof(T),null,"remove");
 public DynamicBuffer<T> AddBuffer<T>(Entity e){var buffer=new DynamicBuffer<T>(new T[0]);Put(0,e,typeof(T),buffer,"addbuffer");return buffer;}
 public void DestroyEntity(Entity e)=>Put(0,e,null,null,"destroy");
 public ParallelWriter AsParallelWriter()=>new ParallelWriter(this);
 static object Fixup(object value,Type type,Dictionary<Entity,Entity> map,int ownerIdentity,int depth=0){
  if(depth>32)throw new NotSupportedException("ECB reference depth bound");
  if(type==typeof(Entity)){var e=(Entity)value;if(e.Index>=0)return e;if(e.Version!=ownerIdentity||!map.TryGetValue(e,out var real))throw new InvalidOperationException("Foreign/uncreated deferred Entity reference");return real;}
  if(value==null||!type.IsValueType||type.IsPrimitive||type.IsEnum||type.Namespace=="Unity.Collections"||type.Namespace=="Unity.Collections.LowLevel.Unsafe")return value;
  object boxed=value;foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance))if(field.FieldType==typeof(Entity)||field.FieldType.IsValueType&&!field.FieldType.IsPrimitive&&!field.FieldType.IsEnum)field.SetValue(boxed,Fixup(field.GetValue(boxed),field.FieldType,map,ownerIdentity,depth+1));return boxed;
 }
 public void Playback(EntityManager em){
  if(owner==null||owner.Played||owner.Disposed)throw new NotSupportedException("ECB playback exactly once");
  if(owner.Deferred>0&&(!em.LogicalAllocatorAdmitted||em.AllocateLogicalIdentity==null))throw new NotSupportedException("ECB creation requires explicit logical identity profile before playback");
  owner.Played=true;var map=new Dictionary<Entity,Entity>();
  foreach(var command in owner.Rows.OrderBy(c=>c.Key).ThenBy(c=>c.Sequence)){
   if(command.Op=="create"){map.Add(command.Entity,em.CreateEntity());continue;}
   if(command.Op=="instantiate"){var prefab=(Entity)Fixup(command.Value,typeof(Entity),map,owner.Identity);map.Add(command.Entity,em.Instantiate(prefab));continue;}
   var entity=(Entity)Fixup(command.Entity,typeof(Entity),map,owner.Identity);
   if(command.Op=="destroy"){em.DestroyEntity(entity);continue;}
   if(!em.Exists(entity))throw new InvalidOperationException("Deferred unknown target");
   if(command.Op=="addbuffer"){typeof(EntityManager).GetMethod("AttachBuffer",BindingFlags.Instance|BindingFlags.NonPublic).MakeGenericMethod(command.Type).Invoke(em,new[]{(object)entity,((IBufferOwner)command.Value).Clone()});continue;}if(command.Op=="remove")em.RemoveComponent(entity,command.Type);
   else{var value=Fixup(command.Value,command.Type,map,owner.Identity);if(command.Op=="set")em.SetComponent(entity,command.Type,value);else if(em.HasComponent(entity,command.Type))em.SetComponent(entity,command.Type,value);else em.AddComponent(entity,command.Type,value);}
  }
 }
 public void Dispose(){if(owner!=null)owner.Disposed=true;}
 public struct ParallelWriter{readonly EntityCommandBuffer buffer;internal ParallelWriter(EntityCommandBuffer value){buffer=value;}public void AddComponent<T>(int key,Entity e,T value)=>buffer.Put(key,e,typeof(T),value,"add");public void AddComponent<T>(int key,Entity e)=>AddComponent(key,e,Activator.CreateInstance<T>());public void RemoveComponent<T>(int key,Entity e)=>buffer.Put(key,e,typeof(T),null,"remove");public void SetComponent<T>(int key,Entity e,T value)=>buffer.Put(key,e,typeof(T),value,"set");}
}
}
