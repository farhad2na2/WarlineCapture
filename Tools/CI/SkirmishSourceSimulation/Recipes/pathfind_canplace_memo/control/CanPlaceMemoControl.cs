using System;
using System.Collections.Generic;

readonly record struct Key(int Moving, int MovingVersion, int Ignored, int IgnoredVersion,
    int Tx, int Tz, int Cx, int Cz, int Fx, int Fz, int Ix, int Iz, int Isx, int Isz,
    int Width, int Height, float CellSize, float OriginX, float OriginY, float OriginZ, byte Faction, bool Vehicle, bool Manual);

sealed class Memo : IDisposable
{
    readonly Dictionary<Key,bool> rows = new();
    readonly int capacity;
    readonly bool shadow;
    public int Count => rows.Count;
    public Memo(int capacity, bool shadow) { this.capacity=capacity; this.shadow=shadow; }
    public bool Resolve(Key key, Func<bool> original)
    {
        if(rows.TryGetValue(key,out bool value))
        {
            if(shadow)
            {
                bool actual=original();
                if(actual!=value) { rows.Clear(); throw new InvalidOperationException("shadow mismatch"); }
            }
            return value;
        }
        bool result=original();
        if(rows.Count<capacity) rows.Add(key,result);
        return result;
    }
    public void Dispose() => rows.Clear();
}

static class Program
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Key Base => new(7,2,9,3,10,11,12,13,2,1,20,21,3,4,100,80,1.5f,4,0,9,1,true,false);
    static void Main()
    {
        int calls=0;
        using(var memo=new Memo(8,false))
        {
            bool first=memo.Resolve(Base,()=>{calls++;return true;});
            bool hit=memo.Resolve(Base,()=>{calls++;return false;});
            Check(first&&hit&&calls==1,"same key must reuse original answer");
            foreach(var key in new[]{Base with{Tx=99},Base with{Cx=99},Base with{MovingVersion=4},Base with{IgnoredVersion=4},Base with{Fx=9},Base with{Manual=true},Base with{Faction=2},Base with{Width=101},Base with{CellSize=2},Base with{OriginY=1}})
                memo.Resolve(key,()=>{calls++;return false;});
            Check(calls==11,"target/current/context scalar changes must not alias");
        }
        using(var shadow=new Memo(4,true))
        {
            shadow.Resolve(Base,()=>true);
            try { shadow.Resolve(Base,()=>false); throw new Exception("mismatch not rejected"); }
            catch(InvalidOperationException e) when(e.Message=="shadow mismatch") { Check(shadow.Count==0,"mismatch must clear memo"); }
        }
        using(var bounded=new Memo(1,false))
        {
            bounded.Resolve(Base,()=>true);
            int overflowCalls=0;
            Check(!bounded.Resolve(Base with{Tx=55},()=>{overflowCalls++;return false;}),"full cache bypass result");
            Check(!bounded.Resolve(Base with{Tx=55},()=>{overflowCalls++;return false;}),"full cache must continue bypassing");
            Check(overflowCalls==2&&bounded.Count==1,"capacity overflow must not cache or change result");
        }
        var cleanup=new Memo(8,false);
        try { cleanup.Resolve(Base,()=>true); throw new ApplicationException("route failure"); }
        catch(ApplicationException) { }
        finally { cleanup.Dispose(); }
        Check(cleanup.Count==0,"request memo must clear after exception");
        Console.WriteLine("CanPlaceMemoControl PASS cases=4");
    }
}
