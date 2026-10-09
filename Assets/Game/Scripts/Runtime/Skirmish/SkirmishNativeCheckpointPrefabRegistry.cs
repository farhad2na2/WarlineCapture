using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // Uses the live native configuration lists, which can be populated after a
    // context is cached. No Editor API, instance ID or arbitrary prefab loading.
    internal sealed class SkirmishNativeCheckpointPrefabRegistry
    {
        private const string Prefix = "native-unit/";
        private readonly IReadOnlyList<GameObject> prefabs;
        private readonly IReadOnlyDictionary<string, GameObject> aliases;

        internal SkirmishNativeCheckpointPrefabRegistry(IReadOnlyList<GameObject> prefabs,
            IReadOnlyDictionary<string, GameObject> aliases)
        { this.prefabs = prefabs; this.aliases = aliases; }

        internal string Key(GameObject prefab)
        {
            if (prefab == null) return null;
            string key = Prefix + SpawnableKeyText.Normalize(prefab.name);
            return Resolve(key) == prefab ? key : null;
        }

        internal GameObject Resolve(string key)
        {
            if (key == null || !key.StartsWith(Prefix, StringComparison.Ordinal)) return null;
            string name = key.Substring(Prefix.Length);
            if (name.Length == 0 || name != SpawnableKeyText.Normalize(name)) return null;
            GameObject found = null;
            bool ambiguous = false;
            void Visit(GameObject candidate)
            {
                if (candidate == null || SpawnableKeyText.Normalize(candidate.name) != name) return;
                if (found != null && found != candidate) ambiguous = true;
                found = candidate;
            }
            if (prefabs != null)
                for (int i = 0; i < prefabs.Count; i++) Visit(prefabs[i]);
            if (aliases != null)
                foreach (var pair in aliases) Visit(pair.Value);
            return ambiguous ? null : found;
        }

        internal bool Validate(out int count)
        {
            var known = new HashSet<GameObject>();
            if (prefabs != null) foreach (var prefab in prefabs) if (prefab != null) known.Add(prefab);
            if (aliases != null) foreach (var pair in aliases) if (pair.Value != null) known.Add(pair.Value);
            count = known.Count;
            foreach (var prefab in known) if (Key(prefab) == null) return false;
            return count > 0;
        }

        internal static SkirmishNativeCheckpointPrefabRegistry Find(EntityManager em, Entity session)
        {
            if (em == default || !em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return null;
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<SkirmishNativeBuildingCheckpointRecord>(), ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState
            });
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId != id) continue;
                var owner = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(entity);
                if (owner.Building != null && owner.Building.CombatEntity == entity && owner.Prefabs != null) return owner.Prefabs;
            }
            return null;
        }
    }
}
