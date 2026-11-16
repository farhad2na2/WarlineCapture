using System;
using Unity.Collections;

internal static class SourceNativeListNoResizeControl
{
    private static void Main()
    {
        var list=new NativeList<int>(2,Allocator.Persistent);int capacity=list.Capacity;
        list.AddNoResize(4);list.AsParallelWriter().AddNoResize(9);
        if(list.Length!=2 || list.Capacity!=capacity || list[0]!=4 || list[1]!=9)throw new Exception("Preallocated native list changed unexpectedly");
        bool full=false;try{list.AddNoResize(12);}catch(InvalidOperationException){full=true;}
        if(!full || list.Length!=2 || list.Capacity!=capacity)throw new Exception("Full no-resize list must reject without mutation");
        list.Clear();list.AddNoResize(7);if(list.Length!=1 || list[0]!=7 || list.Capacity!=capacity)throw new Exception("Clear must retain capacity");
        var zero=new NativeList<byte>(0,Allocator.Persistent);bool zeroRejected=false;
        try{zero.AddNoResize(1);}catch(InvalidOperationException){zeroRejected=true;}
        if(!zeroRejected || zero.Length!=0 || zero.Capacity!=0)throw new Exception("Zero-capacity list must reject");
        list.Dispose();zero.Dispose();
        Console.WriteLine("[SourceNativeListNoResizeControl] result=Passed sameCapacity=1 overflow=Rejected zero=Rejected clear=Passed fixtureOnly=1 nativeParity=Unproven");
    }
}
