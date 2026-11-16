using System;
using Unity.Collections;
using Unity.Entities;

internal struct SourceDeferredBufferRow : IBufferElementData { public Entity Target; public int Value; }
internal static class SourceDeferredBufferControl
{
    private static void Main()
    {
        var em=new EntityManager();int next=1;em.LogicalAllocatorAdmitted=true;
        em.AllocateLogicalIdentity=()=>new Entity{Index=next++,Version=1};
        var parent=em.CreateEntity();var commands=new EntityCommandBuffer(Allocator.Temp);
        var child=commands.CreateEntity();var staged=commands.AddBuffer<SourceDeferredBufferRow>(parent);
        staged.Add(new SourceDeferredBufferRow{Target=child,Value=42});
        if(em.HasBuffer<SourceDeferredBufferRow>(parent))throw new Exception("Buffer appeared before playback");
        commands.Playback(em);var result=em.GetBuffer<SourceDeferredBufferRow>(parent);
        if(result.Length!=1||result[0].Value!=42||result[0].Target.Index<=0||!em.Exists(result[0].Target))throw new Exception("Deferred entity buffer fixup failed");
        staged.Add(new SourceDeferredBufferRow{Value=7});if(result.Length!=1)throw new Exception("Staged buffer alias survived playback");
        result.Insert(0,new SourceDeferredBufferRow{Value=11});result.Insert(2,new SourceDeferredBufferRow{Value=13});
        if(result.Length!=3||result[0].Value!=11||result[1].Value!=42||result[2].Value!=13)throw new Exception("Buffer insert order differs");
        bool badIndex=false;try{result.Insert(4,new SourceDeferredBufferRow());}catch(ArgumentOutOfRangeException){badIndex=true;}
        if(!badIndex||result.Length!=3)throw new Exception("Invalid buffer insert mutated rows");
        bool repeated=false;try{commands.Playback(em);}catch(NotSupportedException){repeated=true;}
        if(!repeated)throw new Exception("Repeated playback accepted");commands.Dispose();
        var other=em.CreateEntity();var foreignOwner=new EntityCommandBuffer(Allocator.Temp);var foreign=foreignOwner.CreateEntity();
        var rejected=new EntityCommandBuffer(Allocator.Temp);rejected.AddBuffer<SourceDeferredBufferRow>(other).Add(new SourceDeferredBufferRow{Target=foreign});
        bool foreignRejected=false;try{rejected.Playback(em);}catch(InvalidOperationException){foreignRejected=true;}
        if(!foreignRejected||em.HasBuffer<SourceDeferredBufferRow>(other))throw new Exception("Foreign deferred reference was accepted");
        rejected.Dispose();foreignOwner.Dispose();
        Console.WriteLine("[SourceDeferredBufferControl] result=Passed deferredFixup=Passed beforePlayback=Absent alias=Isolated insert=Passed badIndex=Rejected repeated=Rejected foreign=Rejected fixtureOnly=1 nativeParity=Unproven");
    }
}
