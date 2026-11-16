using System.Diagnostics;
using Unity.Collections;

static class Program {
 static void Main() {
  Controls<int>("int");
  Controls<long>("generic-fallback");
  Benchmark<int>(Environment.GetEnvironmentVariable("CANDIDATE_DIR")??"unset");
 }
 static void Controls<K>(string label) {
  var map=new NativeParallelMultiHashMap<K,int>(8,Allocator.Temp);var w=map.AsParallelWriter();
  object neg=typeof(K)==typeof(int)?(object)(K)(object)(-17): (K)(object)17L;
  object dup=typeof(K)==typeof(int)?(object)(K)(object)(5): (K)(object)5L;
  w.Add((K)neg,1);w.Add((K)dup,2);w.Add((K)dup,3);
  Check(map,(K)neg,new[]{1},label+" negative");Check(map,(K)dup,new[]{3,2},label+" duplicate order");
  Check(map,(K)(typeof(K)==typeof(int)?(object)(K)(object)(-18):(K)(object)18L),Array.Empty<int>(),label+" miss");
  var alias=map;map.Dispose();Check(alias,(K)dup,Array.Empty<int>(),label+" dispose alias");
  Console.WriteLine("CONTROL PASS "+label);
 }
 static void Check<K>(NativeParallelMultiHashMap<K,int> map,K key,int[] expected,string name) {
  var got=new List<int>();if(map.TryGetFirstValue(key,out var v,out var it)){got.Add(v);while(map.TryGetNextValue(out v,ref it))got.Add(v);}
  if(!got.SequenceEqual(expected))throw new Exception(name+" got="+string.Join(',',got));
 }
 static void Benchmark<K>(string label) {
  const int keys=12000, probes=240000;var map=new NativeParallelMultiHashMap<K,int>(keys,Allocator.Temp);var w=map.AsParallelWriter();
  for(int i=0;i<keys;i++)w.Add((K)(object)i,i);
  var queries=new int[probes];var rng=new Random(73621);for(int i=0;i<probes;i++)queries[i]=keys+rng.Next(keys*8);
  long checksum=0;for(int warm=0;warm<2;warm++)foreach(int q in queries)if(map.TryGetFirstValue((K)(object)q,out var v,out _))checksum+=v;
  var clock=Stopwatch.StartNew();for(int round=0;round<5;round++)foreach(int q in queries)if(map.TryGetFirstValue((K)(object)q,out var v,out _))checksum+=v;clock.Stop();
  Console.WriteLine($"BENCH {label} keys={keys} probes={probes*5} elapsed_ms={clock.Elapsed.TotalMilliseconds:F3} misses={probes*5} checksum={checksum}");
 }
}
