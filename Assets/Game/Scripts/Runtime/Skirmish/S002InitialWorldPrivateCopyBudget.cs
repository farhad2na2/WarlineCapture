#if UNITY_EDITOR
using System;
using System.Threading;

namespace Game.Runtime
{
    // Observer copy limits, never gameplay budgets. Grid/artifact bounds match the
    // existing offline source exporter; query/record limits protect temporary copies.
    internal sealed class S002InitialWorldPrivateCopyBudget
    {
        internal const int MaximumGridElements=16*1024*1024;
        internal const int MaximumEntities=1000000;
        internal const int MaximumChunks=262144;
        internal const int MaximumMapCapacity=4000000;
        internal const long MaximumBytes=256L*1024*1024;
        internal sealed class Ledger
        {
            internal readonly long Limit;
            internal long Used;
            internal Ledger(long maximum){if(maximum<=0||maximum>MaximumBytes)throw new InvalidOperationException("Invalid private copy budget");Limit=maximum;}
            internal void Charge(long count,int elementSize,long maximumCount,string label,long extra=32)
            {
                if(count<0||count>maximumCount||elementSize<=0||extra<0)throw new InvalidOperationException("Private capture element bound: "+label);
                long bytes;try{bytes=checked(count*elementSize+extra);}catch(OverflowException){throw new InvalidOperationException("Private capture byte overflow: "+label);}
                if(bytes>Limit-Used)throw new InvalidOperationException("Private capture aggregate byte bound: "+label);
                Used+=bytes;
            }
        }
        [ThreadStatic] static Ledger current;
        [ThreadStatic] static int depth;
        sealed class Scope:IDisposable
        {
            readonly Ledger owner;readonly int level,thread;bool disposed;
            internal Scope(Ledger owner,int level){this.owner=owner;this.level=level;thread=Thread.CurrentThread.ManagedThreadId;}
            public void Dispose()
            {
                if(disposed)return;
                if(thread!=Thread.CurrentThread.ManagedThreadId||!ReferenceEquals(current,owner)||depth!=level)throw new InvalidOperationException("Private capture budget scopes require same-thread stack order");
                depth--;if(depth==0)current=null;disposed=true;
            }
        }
        internal static IDisposable Begin()
        {
            if(current==null)current=new Ledger(MaximumBytes);
            if(depth==int.MaxValue)throw new InvalidOperationException("Private capture scope depth exhausted");
            depth++;return new Scope(current,depth);
        }
        internal static void Charge(long count,int elementSize,long maximumCount,string label,long extra=32)
        {if(current==null)throw new InvalidOperationException("Private capture copy requires an active budget scope");current.Charge(count,elementSize,maximumCount,label,extra);}
        internal static void Bound(long count,long maximum,string label)
        {if(count<0||count>maximum)throw new InvalidOperationException("Private capture count/capacity bound: "+label);}
    }
}
#endif
