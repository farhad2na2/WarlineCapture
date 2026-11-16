using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Game.Runtime
{
    // Opt-in measurements only. Original source calls and their order remain intact.
    internal static class SourceS004EnemyTiming
    {
        internal static readonly Dictionary<string,double> Seconds=new Dictionary<string,double>(StringComparer.Ordinal);
        internal static readonly Dictionary<string,int> Calls=new Dictionary<string,int>(StringComparer.Ordinal);
        static void Record(string name,long start) {
            double elapsed=(Stopwatch.GetTimestamp()-start)/(double)Stopwatch.Frequency;
            Seconds[name]=Seconds.TryGetValue(name,out var prior)?prior+elapsed:elapsed;
            Calls[name]=Calls.TryGetValue(name,out var count)?count+1:1;
        }
        internal static void Measure(string name,Action call) {long start=Stopwatch.GetTimestamp();try{call();}finally{Record(name,start);}}
        internal static T Measure<T>(string name,Func<T> call) {long start=Stopwatch.GetTimestamp();try{return call();}finally{Record(name,start);}}
        internal static void Reset(){Seconds.Clear();Calls.Clear();}
    }
}
