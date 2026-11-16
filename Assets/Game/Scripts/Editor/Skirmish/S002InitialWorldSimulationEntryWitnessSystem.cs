#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Editor
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class S002InitialWorldSimulationEntryWitnessSystem : SystemBase
    {
        readonly Dictionary<string, ulong> entriesAfterSpawn = new Dictionary<string, ulong>();
        protected override void OnUpdate()
        {
            if (!S002InitialWorldBundleCapture.Enabled) return;
            using var query = EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            using var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var handle = GetComponentTypeHandle<SkirmishExpandedSessionComponent>(true);
            foreach (var chunk in chunks)
            {
                var sessions = chunk.GetNativeArray(ref handle);
                foreach (var session in sessions)
                {
                    if (session.CatalogId.ToString() != "S002" || session.SpawnComplete != 1) continue;
                    string id = session.SessionId.ToString(); entriesAfterSpawn.TryGetValue(id, out ulong count);
                    if (count == ulong.MaxValue) throw new InvalidOperationException("Simulation entry witness exhausted.");
                    entriesAfterSpawn[id] = count + 1;
                }
            }
        }
        internal ulong Entries(string sessionId) => entriesAfterSpawn.TryGetValue(sessionId, out ulong count) ? count : 0;
    }
}
#endif
