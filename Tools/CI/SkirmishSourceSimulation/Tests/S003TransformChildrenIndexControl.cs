using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Game.Runtime;
using Unity.Entities;

// Isolated candidate only; the production/source bridge remains unchanged.
internal static class S003TransformChildrenIndexControl
{
    static Dictionary<Entity,List<Entity>> Candidate(IEnumerable<Entity> orderedNodes,
        Dictionary<Entity,Entity> parents, Dictionary<Entity,Entity[]> existing)
    {
        var byParent=new Dictionary<Entity,List<Entity>>();
        foreach(var pair in parents) {
            if(!byParent.TryGetValue(pair.Value,out var children)) byParent.Add(pair.Value,children=new List<Entity>());
            children.Add(pair.Key);
        }
        var result=new Dictionary<Entity,List<Entity>>();
        foreach(var parent in orderedNodes) {
            var list=new List<Entity>();var included=new HashSet<Entity>();
            if(existing.TryGetValue(parent,out var old)) foreach(var child in old) {
                if(included.Contains(child))throw new NotSupportedException("Duplicate existing Child entry");
                if(parents.TryGetValue(child,out var current)&&current==parent) {list.Add(child);included.Add(child);}
            }
            if(byParent.TryGetValue(parent,out var candidates)) foreach(var child in candidates)
                if(included.Add(child)) list.Add(child);
            result.Add(parent,list);
        }
        return result;
    }
    static void Check(Entity[] nodes,Dictionary<Entity,Entity> parents,Dictionary<Entity,Entity[]> old)
    {
        var actual=SourceMissionTransformBridge.BuildChildren(nodes,parents,old);
        var candidate=Candidate(nodes,parents,old);
        if(actual.Count!=candidate.Count || actual.Any(p=>!candidate.TryGetValue(p.Key,out var v)||!p.Value.SequenceEqual(v)))
            throw new InvalidOperationException("Ordered child projection differs");
    }
    static double Time(Action action){var w=Stopwatch.StartNew();action();w.Stop();return w.Elapsed.TotalMilliseconds;}
    static void Main()
    {
        var random=new Random(315004);
        for(int trial=0;trial<200;trial++) {
            var nodes=Enumerable.Range(1,random.Next(2,110)).Select(i=>new Entity{Index=i,Version=1}).ToArray();
            var parents=new Dictionary<Entity,Entity>();
            for(int i=1;i<nodes.Length;i++) if(random.Next(4)!=0) parents.Add(nodes[i],nodes[random.Next(i)]);
            var old=new Dictionary<Entity,Entity[]>();
            foreach(var parent in nodes) {
                var own=parents.Where(x=>x.Value==parent).Select(x=>x.Key).OrderBy(x=>random.Next()).ToArray();
                if(own.Length>0) old.Add(parent,own.Take(random.Next(own.Length+1)).ToArray());
            }
            Check(nodes,parents,old);
        }
        var duplicateParent=new Entity{Index=1,Version=1};var duplicateChild=new Entity{Index=2,Version=1};
        var duplicateParents=new Dictionary<Entity,Entity>{{duplicateChild,duplicateParent}};
        var duplicateOld=new Dictionary<Entity,Entity[]>{{duplicateParent,new[]{duplicateChild,duplicateChild}}};
        bool originalRejected=false,candidateRejected=false;
        try{SourceMissionTransformBridge.BuildChildren(new[]{duplicateParent,duplicateChild},duplicateParents,duplicateOld);}catch(NotSupportedException){originalRejected=true;}
        try{Candidate(new[]{duplicateParent,duplicateChild},duplicateParents,duplicateOld);}catch(NotSupportedException){candidateRejected=true;}
        if(!originalRejected||!candidateRejected)throw new InvalidOperationException("Duplicate-child rejection differs");
        // A stale child may be listed repeatedly when neither occurrence is kept.
        var stale=new Entity{Index=3,Version=1};
        Check(new[]{duplicateParent,duplicateChild},duplicateParents,
            new Dictionary<Entity,Entity[]>{{duplicateParent,new[]{stale,stale,duplicateChild}}});
        // An omitted parent is ignored by both projections; a child need not be in orderedNodes.
        Check(new[]{duplicateParent},new Dictionary<Entity,Entity>{{duplicateChild,duplicateParent},{stale,duplicateChild}},
            new Dictionary<Entity,Entity[]>());
        var large=Enumerable.Range(1,1200).Select(i=>new Entity{Index=i,Version=1}).ToArray();
        var relationships=new Dictionary<Entity,Entity>();for(int i=1;i<large.Length;i++)relationships.Add(large[i],large[(i-1)/4]);
        var existing=new Dictionary<Entity,Entity[]>();Check(large,relationships,existing);
        var baseline=new List<double>();var candidate=new List<double>();
        for(int i=0;i<12;i++) {
            Action a=()=>SourceMissionTransformBridge.BuildChildren(large,relationships,existing);
            Action b=()=>Candidate(large,relationships,existing);
            if((i&1)==0){baseline.Add(Time(a));candidate.Add(Time(b));}else{candidate.Add(Time(b));baseline.Add(Time(a));}
        }
        Console.WriteLine("[S003TransformChildrenIndexControl] result=Passed random=200 duplicate=Passed paired=12 sourceOnly=1");
        Console.WriteLine("original_ms="+string.Join(",",baseline.Select(x=>x.ToString("R",System.Globalization.CultureInfo.InvariantCulture))));
        Console.WriteLine("candidate_ms="+string.Join(",",candidate.Select(x=>x.ToString("R",System.Globalization.CultureInfo.InvariantCulture))));
    }
}
