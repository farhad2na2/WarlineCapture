using System;using System.Collections.Generic;using Unity.Jobs;
namespace Unity.Collections{
 public struct NativeParallelMultiHashMapIterator<K>{internal K Key;internal int Index;}
 public struct NativeParallelMultiHashMap<K,V>:IDisposable{
  static readonly bool SourceProbeCountersEnabled=Environment.GetEnvironmentVariable("WARLINE_SOURCE_WORKLOAD_COUNTERS")=="1";
  internal static long SourceProbeCalls,SourceProbeHits,SourceValuesVisited;
  internal sealed class Owner{internal readonly Dictionary<K,List<V>> Rows=new Dictionary<K,List<V>>();internal int Count,Capacity;}readonly Owner owner;
  public NativeParallelMultiHashMap(int cap,Allocator allocator){if(cap<0||cap>4194304)throw new NotSupportedException("Spatial map bound");owner=new Owner{Capacity=cap};}
  public int Count()=>owner?.Count??0;
  public int UniqueKeyCount()=>owner?.Rows.Count??0;
  internal static void ResetSourceProbeCounters(){SourceProbeCalls=0;SourceProbeHits=0;SourceValuesVisited=0;}
  internal int[] BuildSortedUniqueIntKeyIndexForSourceCandidate(out int uniqueCount,out long elapsedTicks){if(typeof(K)!=typeof(int))throw new NotSupportedException("Source candidate index is int-key only");var clock=System.Diagnostics.Stopwatch.StartNew();var keys=new int[owner?.Count??0];int used=0;if(owner!=null)foreach(var row in owner.Rows)for(int i=0;i<row.Value.Count;i++)keys[used++]=(int)(object)row.Key;Array.Sort(keys);uniqueCount=0;for(int i=0;i<keys.Length;i++)if(uniqueCount==0||keys[i]!=keys[uniqueCount-1])keys[uniqueCount++]=keys[i];clock.Stop();elapsedTicks=clock.ElapsedTicks;return keys;}
  public ParallelWriter AsParallelWriter()=>new ParallelWriter(owner);public struct ParallelWriter{readonly Owner owner;internal ParallelWriter(Owner value){owner=value;}public void Add(K key,V value){if(owner==null||owner.Count>=owner.Capacity)throw new InvalidOperationException("Spatial map capacity");if(!owner.Rows.TryGetValue(key,out var rows)){rows=new List<V>();owner.Rows.Add(key,rows);}rows.Insert(0,value);owner.Count++;}}
  public bool TryGetFirstValue(K key,out V value,out NativeParallelMultiHashMapIterator<K> iterator){iterator=new NativeParallelMultiHashMapIterator<K>{Key=key,Index=0};if(SourceProbeCountersEnabled)SourceProbeCalls++;if(owner!=null&&owner.Rows.TryGetValue(key,out var rows)&&rows.Count>0){value=rows[0];if(SourceProbeCountersEnabled){SourceProbeHits++;SourceValuesVisited++;}return true;}value=default;return false;}
  public bool TryGetNextValue(out V value,ref NativeParallelMultiHashMapIterator<K> iterator){iterator.Index++;if(owner!=null&&owner.Rows.TryGetValue(iterator.Key,out var rows)&&iterator.Index<rows.Count){value=rows[iterator.Index];if(SourceProbeCountersEnabled)SourceValuesVisited++;return true;}value=default;return false;}
  public JobHandle Dispose(JobHandle dependency){dependency.Complete();Dispose();return default;}public void Dispose(){owner?.Rows.Clear();}
 }
}
