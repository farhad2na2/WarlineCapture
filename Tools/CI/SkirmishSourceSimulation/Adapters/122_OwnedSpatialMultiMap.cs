using System;using System.Collections.Generic;using Unity.Jobs;
namespace Unity.Collections{
 public struct NativeParallelMultiHashMapIterator<K>{internal K Key;internal int Index;}
 public struct NativeParallelMultiHashMap<K,V>:IDisposable{
  internal sealed class Owner{internal readonly Dictionary<K,List<V>> Rows=new Dictionary<K,List<V>>();internal int Count,Capacity;}readonly Owner owner;
  public NativeParallelMultiHashMap(int cap,Allocator allocator){if(cap<0||cap>4194304)throw new NotSupportedException("Spatial map bound");owner=new Owner{Capacity=cap};}
  public ParallelWriter AsParallelWriter()=>new ParallelWriter(owner);public struct ParallelWriter{readonly Owner owner;internal ParallelWriter(Owner value){owner=value;}public void Add(K key,V value){if(owner==null||owner.Count>=owner.Capacity)throw new InvalidOperationException("Spatial map capacity");if(!owner.Rows.TryGetValue(key,out var rows)){rows=new List<V>();owner.Rows.Add(key,rows);}rows.Insert(0,value);owner.Count++;}}
  public bool TryGetFirstValue(K key,out V value,out NativeParallelMultiHashMapIterator<K> iterator){iterator=new NativeParallelMultiHashMapIterator<K>{Key=key,Index=0};if(owner!=null&&owner.Rows.TryGetValue(key,out var rows)&&rows.Count>0){value=rows[0];return true;}value=default;return false;}
  public bool TryGetNextValue(out V value,ref NativeParallelMultiHashMapIterator<K> iterator){iterator.Index++;if(owner!=null&&owner.Rows.TryGetValue(iterator.Key,out var rows)&&iterator.Index<rows.Count){value=rows[iterator.Index];return true;}value=default;return false;}
  public JobHandle Dispose(JobHandle dependency){dependency.Complete();Dispose();return default;}public void Dispose(){owner?.Rows.Clear();}
 }
}
