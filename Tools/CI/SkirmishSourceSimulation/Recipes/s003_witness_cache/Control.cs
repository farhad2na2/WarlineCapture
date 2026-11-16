using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;

internal static class WitnessCacheControl {
 static void Check(GridWalkableSourceWitness w, OperationMapGridSourceRecord s, DynamicBuffer<GridWalkable> b) {
  bool original=w.Matches(s,b), candidate=SourceWalkabilityWitnessCache.Matches(w,s,b);
  if(original!=candidate)throw new Exception("Cached witness differs from original");
 }
 static int Main() { try {
  var b=new DynamicBuffer<GridWalkable>(new[]{new GridWalkable{Value=1},new GridWalkable{Value=0},new GridWalkable{Value=1}});
  var w=GridWalkableSourceWitness.Capture("guid","hash",b);w.Generation=7;w.WorldSequence=42;
  var s=new OperationMapGridSourceRecord{GridGuid=new FixedString64Bytes("guid"),GridHash=new FixedString128Bytes("hash"),Generation=7,WorldSequence=42};
  Check(w,s,b);Check(w,s,b);
  b[0]=new GridWalkable{Value=0};Check(w,s,b);b[0]=new GridWalkable{Value=1};Check(w,s,b);
  var alias=b.AsNativeArray();alias[1]=new GridWalkable{Value=1};Check(w,s,b);alias[1]=new GridWalkable{Value=0};Check(w,s,b);
  alias.CopyFrom(new NativeArray<GridWalkable>(new[]{new GridWalkable{Value=0},new GridWalkable{Value=0},new GridWalkable{Value=1}}));Check(w,s,b);
  b[0]=new GridWalkable{Value=1};b.Add(new GridWalkable{Value=1});Check(w,s,b);b.RemoveAt(3);Check(w,s,b);
  b.Insert(0,new GridWalkable{Value=1});Check(w,s,b);b.RemoveAtSwapBack(0);Check(w,s,b);
  b.ResizeUninitialized(1);Check(w,s,b);b.Clear();Check(w,s,b);b.ResizeUninitialized(3);Check(w,s,b);
  var other=new DynamicBuffer<GridWalkable>(new[]{new GridWalkable{Value=1},new GridWalkable{Value=0},new GridWalkable{Value=1}});Check(w,s,other);
  s.Generation=8;Check(w,s,other);s.Generation=7;s.WorldSequence=43;Check(w,s,other);s.WorldSequence=42;s.GridHash=new FixedString128Bytes("wrong");Check(w,s,other);
  Console.WriteLine("[WitnessCacheControl] result=Passed mutation_alias_copy_identity=Passed native=0 readiness=0");return 0;
 } catch(Exception e){Console.Error.WriteLine(e);return 1;} }
}
