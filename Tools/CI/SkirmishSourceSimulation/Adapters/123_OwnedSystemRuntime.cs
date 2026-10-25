using System;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Reflection.Emit;using Unity.Collections;using Unity.Entities;
namespace Unity.Entities {
 public partial struct SystemState {
  public WorldClock WorldUnmanaged=>EntityManager.WorldUnmanaged;public Unity.Jobs.JobHandle Dependency; public uint LastSystemVersion;
  List<EntityQuery> required;
  public TimeData Time=>EntityManager.WorldUnmanaged.Time;
  public EntityQuery GetEntityQuery(params ComponentType[] types)=>EntityManager.CreateEntityQuery(types);
  public EntityQuery GetEntityQuery(EntityQueryDesc desc)=>EntityManager.CreateEntityQuery(desc);
  public ComponentLookup<T> GetComponentLookup<T>(bool readOnly=false)=>new ComponentLookup<T>(EntityManager);
  public ComponentTypeHandle<T> GetComponentTypeHandle<T>(bool readOnly=false)=>EntityManager.GetComponentTypeHandle<T>(readOnly);
  public EntityTypeHandle GetEntityTypeHandle()=>EntityManager.GetEntityTypeHandle();
  public void RequireForUpdate(EntityQuery q){if(required==null)required=new List<EntityQuery>();required.Add(q);}
  public void RequireForUpdate<T>()=>RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<T>()));
  public bool RequiredQueriesPresent=>required==null||required.All(x=>!x.IsEmptyIgnoreFilter);
  public void Begin(){SystemAPI.Bind(EntityManager,LastSystemVersion);}
  public void End(){LastSystemVersion=EntityManager.ChangeVersion;}
 }
 public struct RefRO<T>where T:struct{readonly EntityManager em;readonly Entity e;public RefRO(EntityManager m,Entity x){em=m;e=x;}public ref readonly T ValueRO=>ref em.Borrow<T>(e,false);}
 public struct RefRW<T>where T:struct{readonly EntityManager em;readonly Entity e;public RefRW(EntityManager m,Entity x){em=m;e=x;}public ref T ValueRW=>ref em.Borrow<T>(e,true);public ref readonly T ValueRO=>ref em.Borrow<T>(e,false);}
 internal interface IComponentCell{object Read();void Write(object value);}
 internal interface ITypedComponentCellRead<T>{T ReadTyped();}
 internal sealed class ComponentCell<T>:IComponentCell,ITypedComponentCellRead<T> where T:struct{public T Value;public ComponentCell(T value){Value=value;}public object Read()=>Value;public T ReadTyped()=>Value;public void Write(object value){Value=(T)value;}}
 public sealed partial class EntityManager {
  uint changeVersion;readonly Dictionary<Entity,Dictionary<Type,uint>> writes=new Dictionary<Entity,Dictionary<Type,uint>>();
  public uint ChangeVersion=>changeVersion;
  internal void MarkWrite(Entity e,Type t){if(changeVersion==uint.MaxValue)throw new NotSupportedException("Owned write-version domain exhausted");if(!writes.TryGetValue(e,out var r)){r=new Dictionary<Type,uint>();writes.Add(e,r);}r[t]=++changeVersion;}
  internal bool Changed(Entity e,Type t,uint since)=>writes.TryGetValue(e,out var r)&&r.TryGetValue(t,out var v)&&unchecked((int)(v-since))>0;
  internal ref T Borrow<T>(Entity e,bool writable)where T:struct{
   // Preserve canonical routing and all unusual/missing/buffer-only failure behavior.
   // Ordinary present cells need only one row/type lookup. No value or reference is cached.
   if(typeof(T)!=typeof(Game.Components.BuildingResourceStorageComponent)&&typeof(T)!=typeof(Game.Components.BuildingResourceProductionProfileComponent)&&TryLogicalDataRow(e,out var row)&&row.TryGetValue(typeof(T),out var value)){
    if(writable)MarkWrite(e,typeof(T));
    return ref ((ComponentCell<T>)value).Value;
   }
   return ref BorrowOriginal<T>(e,writable);
  }
  internal ref T BorrowOriginal<T>(Entity e,bool writable)where T:struct{
   if(!HasComponent<T>(e))throw new InvalidOperationException("Source reference requires present component");if(writable)MarkWrite(e,typeof(T));
   if(bank!=null&&bank.Contains(e)){
    int slot=bank.FreshSourceLifetime?bank.CurrentSlot(e):Array.IndexOf(bank.EntityIds,e.Index+":"+e.Version);
    if(typeof(T)==typeof(Game.Components.BuildingResourceStorageComponent))return ref ((T[])(object)bank.Storage)[slot];
    if(typeof(T)==typeof(Game.Components.BuildingResourceProductionProfileComponent))return ref ((T[])(object)bank.Profiles)[slot];
   }
   return ref ((ComponentCell<T>)data[e][typeof(T)]).Value;
  }
  public void CompleteDependencyBeforeRO<T>(){}public void CompleteDependencyBeforeRW<T>(){}
 }
 public static class SystemAPI {
  [ThreadStatic]static EntityManager current;[ThreadStatic]internal static uint LastVersion;
  public static void Bind(EntityManager em,uint lastVersion=0){current=em??throw new ArgumentNullException();LastVersion=lastVersion;}
  internal static EntityManager Manager=>EM;static EntityManager EM=>current??throw new InvalidOperationException("Owned system phase not bound");
  public static TimeData Time=>EM.WorldUnmanaged.Time;
  public static EntityStorageInfoLookup GetEntityStorageInfoLookup()=>new EntityStorageInfoLookup(EM);
  public static bool TryGetSingleton<T>(out T value){var q=EM.CreateEntityQuery(typeof(T));if(q.CalculateEntityCount()==0){value=default;return false;}value=q.GetSingleton<T>();return true;}public static DynamicBuffer<T> GetBuffer<T>(Entity e)=>EM.GetBuffer<T>(e);public static T GetSingleton<T>()=>EM.CreateEntityQuery(typeof(T)).GetSingleton<T>();
  public static Entity GetSingletonEntity<T>()=>EM.CreateEntityQuery(typeof(T)).GetSingletonEntity();
  public static RefRW<T> GetSingletonRW<T>()where T:struct=>new RefRW<T>(EM,GetSingletonEntity<T>());
  public static T GetComponent<T>(Entity e)=>EM.GetComponentData<T>(e);
  public static RefRW<T> GetComponentRW<T>(Entity e)where T:struct=>new RefRW<T>(EM,e);
  public static ComponentLookup<T> GetComponentLookup<T>(bool readOnly=false)=>new ComponentLookup<T>(EM);
  public static OwnedQuery<A> Query<A>()=>new OwnedQuery<A>(EM);
public static OwnedQuery<A,B> Query<A,B>()=>new OwnedQuery<A,B>(EM);
public static OwnedQuery<A,B,C> Query<A,B,C>()=>new OwnedQuery<A,B,C>(EM);
public static OwnedQuery<A,B,C,D> Query<A,B,C,D>()=>new OwnedQuery<A,B,C,D>(EM);
public static OwnedQuery<A,B,C,D,E> Query<A,B,C,D,E>()=>new OwnedQuery<A,B,C,D,E>(EM);
public static OwnedQuery<A,B,C,D,E,F> Query<A,B,C,D,E,F>()=>new OwnedQuery<A,B,C,D,E,F>(EM);
public static OwnedQuery<A,B,C,D,E,F,G> Query<A,B,C,D,E,F,G>()=>new OwnedQuery<A,B,C,D,E,F,G>(EM);
public static OwnedQuery<A,B,C,D,E,F,G,H> Query<A,B,C,D,E,F,G,H>()=>new OwnedQuery<A,B,C,D,E,F,G,H>(EM);
public static EntityQueryBuilder QueryBuilder()=>new EntityQueryBuilder(Allocator.Temp);public static BufferLookup<T> GetBufferLookup<T>(bool readOnly=false)=>EM.GetBufferLookup<T>();
 }

}
namespace Unity.Jobs {
 public interface IJobParallelFor{}public struct JobHandle{public bool IsCompleted=>true;public void Complete(){}public static JobHandle CombineDependencies(JobHandle a,JobHandle b)=>default;public static JobHandle CombineDependencies(JobHandle a,JobHandle b,JobHandle c)=>default;}
 public static class OwnedJobBackend{public static JobHandle Schedule<T>(this T job,JobHandle dependency)where T:struct,IJobEntity=>ScheduleParallel(job,dependency);public static JobHandle Schedule<T>(this T job,int count,int batch,JobHandle dependency=default)where T:struct,IJobParallelFor{dependency.Complete();if(count<0||count>65536||batch<1)throw new NotSupportedException("Original parallel-plan work bound");object boxed=job;var method=typeof(T).GetMethod("Execute");for(int i=0;i<count;i++)method.Invoke(boxed,new object[]{i});return default;}public static JobHandle ScheduleParallel<T>(this T job,JobHandle dependency)where T:struct,IJobEntity{dependency.Complete();Game.Runtime.OwnedEntityJobBackend.Execute(job,SystemAPI.Manager);return default;}
  // Source IJobFor.Schedule is sequential (the original job shares one scratch workspace).
  // Execution completes here; original path owner retains result and applies on its NEXT update.
  public static JobHandle Schedule<T>(this T job,int count,JobHandle dependency)where T:struct,IJobFor{dependency.Complete();if(count<0||count>32)throw new NotSupportedException("Owned original path batch bound");var m=typeof(T).GetMethod("Execute",new[]{typeof(int)})??throw new NotSupportedException("Source Execute missing");object boxed=job;for(int i=0;i<count;i++)m.Invoke(boxed,new object[]{i});return default;}
 }
}
namespace Unity.Collections{
 public struct NativeParallelHashMap<K,V>:IDisposable,IEnumerable<KeyValuePair<K,V>>{
  sealed class Owner{internal Dictionary<K,V> Rows;internal int Capacity;}readonly Owner owner;
  public NativeParallelHashMap(int capacity,Allocator a){if(capacity<0||capacity>4194304)throw new NotSupportedException("Hash map bound");owner=new Owner{Rows=new Dictionary<K,V>(capacity),Capacity=capacity};}
  Dictionary<K,V> Rows=>owner==null?throw new InvalidOperationException("Uncreated native hash map"):owner.Rows;
  public bool IsCreated=>owner!=null;public int Capacity{get=>owner.Capacity;set{if(value<Rows.Count||value>4194304)throw new NotSupportedException("Hash capacity bound");owner.Capacity=value;}}
  public int Count()=>Rows.Count;public bool ContainsKey(K key)=>Rows.ContainsKey(key);public bool TryGetValue(K key,out V value)=>Rows.TryGetValue(key,out value);
  public void Add(K key,V value){if(!TryAdd(key,value))throw new InvalidOperationException("Duplicate native map key");}public bool TryAdd(K key,V value){if(Rows.ContainsKey(key))return false;if(Rows.Count>=Capacity)throw new InvalidOperationException("Native capacity exhausted");Rows.Add(key,value);return true;}
  public V this[K key]{get=>Rows[key];set{if(!Rows.ContainsKey(key)&&Rows.Count>=Capacity)throw new InvalidOperationException("Native capacity exhausted");Rows[key]=value;}}
  public bool Remove(K key)=>Rows.Remove(key);public void Clear()=>Rows.Clear();public NativeArray<K> GetKeyArray(Allocator a)=>new NativeArray<K>(Rows.Keys.ToArray());public IEnumerator<KeyValuePair<K,V>> GetEnumerator()=>Rows.GetEnumerator();IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public Unity.Jobs.JobHandle Dispose(Unity.Jobs.JobHandle dependency){dependency.Complete();Dispose();return default;}public void Dispose(){Rows.Clear();}
 }
 public struct NativeHashSet<T>:IDisposable{readonly HashSet<T> rows;readonly int capacity;public NativeHashSet(int cap,Allocator a){if(cap<0||cap>4194304)throw new NotSupportedException();rows=new HashSet<T>();capacity=cap;}public bool Add(T x){if(!rows.Contains(x)&&rows.Count>=capacity)throw new NotSupportedException("HashSet capacity");return rows.Add(x);}public bool Contains(T x)=>rows.Contains(x);public void Dispose(){rows.Clear();}}
}
