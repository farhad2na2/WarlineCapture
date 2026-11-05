using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct SkirmishScenarioSpawnSystem
    {
        internal static bool CanResolveCheckpointUnit(EntityManager em, SkirmishNativeCheckpointSnapshotService.Actor saved)
        {
            return saved != null && saved.IsStructure == 0 &&
                SkirmishNativeCheckpointParts.TryReadComponent(saved.Parts, out SkirmishUnitRoleComponent _) &&
                SkirmishNativeCheckpointParts.TryReadComponent(saved.Parts, out LocalTransform _) &&
                TryResolvePrefabEntity(BuildPrefabEntityLookup(em), saved.PrefabKey, out Entity prefab) && em.Exists(prefab);
        }
        // Called by the native reconstruction owner after document compatibility
        // passes. Never create a bare ledger as a substitute for a baked prefab.
        internal static bool TryInstantiateCheckpointUnit(EntityManager em,
            SkirmishNativeCheckpointSnapshotService.Actor saved, out Entity actor, Game.Configs.SkirmishResolvedSetup setup = null, RuntimeActorBirthContext birth = default)
        {
            actor = Entity.Null;
            if (em == default || saved == null || saved.IsStructure != 0 || string.IsNullOrEmpty(saved.PrefabKey) ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.Parts, out SkirmishAttemptOwnedComponent owner) ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.Parts, out SkirmishUnitRoleComponent role) ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.Parts, out LocalTransform transform) ||
                owner.IsStructure != 0 || owner.StableObjectId.ToString() != saved.Id)
                return false;
            bool objectiveTruck = role.Role == Game.Skirmish.Contracts.SkirmishRoleKind.ConvoyLogic;
            if (!SkirmishNativeConvoyCheckpoint.TryValidateClassification(setup,saved,out _)) return false;
            var prefabs = BuildPrefabEntityLookup(em);
            if (!TryResolvePrefabEntity(prefabs, saved.PrefabKey, out Entity prefab) || !em.Exists(prefab)) return false;
            using var ownedQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState
            });
            using var owners = ownedQuery.ToComponentDataArray<SkirmishAttemptOwnedComponent>(Allocator.Temp);
            foreach (var existing in owners)
                if (existing.SessionId.Equals(owner.SessionId) && existing.StableObjectId.Equals(owner.StableObjectId)) return false;

            Entity created = em.Instantiate(prefab);
            birth.StampFresh(em, created, birth.Source.Grid);
            SkirmishRuntimeActorOwnership.DetachFromSourceScene(em, created);
            SetOrAdd(em, created, owner);
            SetOrAdd(em, created, role);
            SetOrAdd(em, created, transform);
            SetOrAdd(em, created, new Faction { Id = owner.FactionId });
            SetOrAdd(em, created, new UnitSourcePrefabKey { Value = new FixedString64Bytes(saved.PrefabKey) });
            SetOrAdd(em, created, new SkirmishSharedActorTag());
            SetOrAdd(em, created, new SkirmishVisualSpawnedComponent { Spawned = 1, FromRegistry = 1 });
            if (!em.HasComponent<CampaignMissionCombatSuppressedTag>(created)) em.AddComponent<CampaignMissionCombatSuppressedTag>(created);
            // Keep rebuilt actors inert until every owner/reference is prepared.
            if (!em.HasComponent<Disabled>(created)) em.AddComponent<Disabled>(created);
            if (objectiveTruck && !SkirmishNativeConvoyCheckpoint.TrySpecializeFresh(em,created,setup,saved,out _))
            { em.DestroyEntity(created); return false; }
            actor = created;
            return true;
        }
    }
}
