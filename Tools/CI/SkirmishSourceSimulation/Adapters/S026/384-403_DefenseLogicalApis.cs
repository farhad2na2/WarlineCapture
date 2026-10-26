using System;
using System.Collections.Generic;
namespace Unity.Collections
{
    // Source defense uses this only to deduplicate deferred component additions.
    public struct NativeParallelHashSet<T> : IDisposable
    {
        readonly HashSet<T> entries;
        public NativeParallelHashSet(int capacity, Allocator allocator)
        {
            if (capacity < 0 || capacity > 65536) throw new NotSupportedException("Defense set bound");
            entries = new HashSet<T>();
        }
        public bool Add(T value)
        {
            if (entries == null) throw new InvalidOperationException("Uncreated defense set");
            if (entries.Count >= 65536 && !entries.Contains(value)) throw new NotSupportedException("Defense set bound");
            return entries.Add(value);
        }
        public void Dispose() { entries?.Clear(); }
    }
}
namespace Unity.Entities
{
    public readonly struct EntityStorageInfoLookup
    {
        readonly EntityManager manager;
        internal EntityStorageInfoLookup(EntityManager em) { manager = em ?? throw new ArgumentNullException(nameof(em)); }
        public bool Exists(Entity entity) => manager.Exists(entity);public void Update(ref SystemState state){if(!object.ReferenceEquals(manager,state.EntityManager))throw new NotSupportedException("Storage lookup source owner changed");}
    }
}
namespace Unity.Profiling
{
    // Instrumentation only. No Unity profiler/native performance claim is made.
    public readonly struct ProfilerMarker
    {
        public ProfilerMarker(string name) { if (String.IsNullOrEmpty(name)) throw new ArgumentException(nameof(name)); }
        public Scope Auto() => default;
        public readonly struct Scope : IDisposable { public void Dispose() { } }
    }
}
