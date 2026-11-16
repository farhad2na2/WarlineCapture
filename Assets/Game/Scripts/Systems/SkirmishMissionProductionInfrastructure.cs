using System;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Campaign anchors describe Campaign ownership. Expanded Skirmish production
    // is owned by the selected attempt, including its legitimate later construction.
    internal static class SkirmishMissionProductionInfrastructure
    {
        private static readonly FixedString128Bytes HelipadBuildingId = new("building_helipad");

        internal static bool TryResolveOwnership(
            EntityManager em, out bool missionOwned, out Entity session, out string error)
        {
            missionOwned = false;
            session = Entity.Null;
            error = null;
            if (em == default || em.World == null || !em.World.IsCreated)
                return Fail("A live source world is required for production infrastructure ownership.", out error);

            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            using var setups = em.CreateEntityQuery(typeof(SkirmishResolvedSetupRecord));
            using var catalogs = em.CreateEntityQuery(typeof(SkirmishProductionCatalogRecord));
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            int sessionCount = sessions.CalculateEntityCount();
            int setupCount = setups.CalculateEntityCount();
            int catalogCount = catalogs.CalculateEntityCount();
            int matchCount = matches.CalculateEntityCount();
            if (sessionCount > 1 || setupCount > 1 || catalogCount > 1 || matchCount > 1)
                return Fail("Ambiguous Skirmish production infrastructure ownership.", out error);

            if (sessionCount == 0)
            {
                bool requiresExpanded = false;
                if (matchCount == 1)
                {
                    int index = em.GetComponentData<SkirmishMatchState>(matches.GetSingletonEntity()).ScenarioIndex;
                    // These are the same named coded launch contracts used by
                    // SkirmishLaunchProjection.TryQueueCore, not an army/map guess.
                    requiresExpanded = RequiresExpandedOwner(index);
                }
                using var actors = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
                using var definitions = em.CreateEntityQuery(typeof(SkirmishLaunchDefinitionRecord));
                if (setupCount != 0 || catalogCount != 0 || requiresExpanded ||
                    !actors.IsEmptyIgnoreFilter || !definitions.IsEmptyIgnoreFilter)
                    return Fail("Expanded Skirmish production infrastructure has lost its source session.", out error);
                return true;
            }

            session = sessions.GetSingletonEntity();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (state.IsLegacy > 1)
                return Fail("Skirmish production infrastructure has an invalid ownership mode.", out error);
            if (state.IsLegacy != 0)
            {
                if (setupCount != 0 || catalogCount != 0 || matchCount == 1 &&
                    RequiresExpandedOwner(em.GetComponentData<SkirmishMatchState>(matches.GetSingletonEntity()).ScenarioIndex))
                    return Fail("Legacy Skirmish has conflicting expanded production ownership records.", out error);
                session = Entity.Null;
                return true;
            }
            if (setupCount != 1 || catalogCount != 1 ||
                setups.GetSingletonEntity() != session || catalogs.GetSingletonEntity() != session)
                return Fail("Expanded Skirmish production infrastructure requires its same-session setup and catalog.", out error);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session)?.Setup;
            var catalog = em.GetComponentObject<SkirmishProductionCatalogRecord>(session)?.Catalog;
            if (setup == null || catalog == null || !ReferenceEquals(catalog.Setup, setup) ||
                state.SessionId.Length == 0 || state.CatalogId.ToString() != setup.CatalogId ||
                state.DefinitionId.ToString() != setup.DefinitionId || state.ContentVersion != setup.ContentVersion ||
                state.SetupHash != setup.SetupHash || state.Seed != setup.Seed ||
                state.SizeId != setup.SizeId || state.DifficultyId != setup.DifficultyId ||
                state.ObjectiveKind != setup.ObjectiveKind)
                return Fail("Expanded Skirmish production infrastructure setup/catalog identity is missing or changed.", out error);
            if (matchCount != 1)
                return Fail("Expanded Skirmish production infrastructure has lost its native match.", out error);
            var match = em.GetComponentData<SkirmishMatchState>(matches.GetSingletonEntity());
            if (matches.GetSingletonEntity() != session || match.SessionId != state.SessionId || match.Seed != state.Seed)
                return Fail("Skirmish match and production infrastructure have different attempt identities.", out error);
            missionOwned = true;
            return true;
        }

        internal static bool TryValidateHelipadRows(
            EntityManager em, Entity session,
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> rows,
            in GridConfig grid, out string error, bool pruneDeadRows = true)
        {
            if (grid.Width <= 0 || grid.Height <= 0 || !math.isfinite(grid.CellSize) || grid.CellSize <= 0f)
                return Fail("The runtime grid is invalid for mission-owned helipad binding.", out error);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using var owners = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishStructureIdentityComponent), typeof(RuntimeBuildingCombatInfo));
            using var entities = owners.ToEntityArray(Allocator.Temp);
            using var deadRows = new NativeList<int>(rows.Length, Allocator.Temp);
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (!row.BuildingId.Equals(HelipadBuildingId))
                    continue;
                if (row.BuildingRuntimeId <= 0 || row.SlotIndex < 0 ||
                    !math.all(math.isfinite(row.WorldPosition)) ||
                    !GridUtils.InBounds(row.Cell, grid.Width, grid.Height) ||
                    !GridUtils.WorldToCell(in grid, row.WorldPosition).Equals(row.Cell))
                    return Fail("Mission-owned helipad row has invalid slot geometry or identity.", out error);
                for (int prior = 0; prior < i; prior++)
                {
                    var previous = rows[prior];
                    if (previous.BuildingId.Equals(HelipadBuildingId) &&
                        previous.BuildingRuntimeId == row.BuildingRuntimeId &&
                        (previous.FactionId != row.FactionId || previous.SlotIndex == row.SlotIndex))
                        return Fail("Mission-owned helipad has duplicate or conflicting runtime slot owners.", out error);
                }
                if (!TryFindRegisteredOwner(em, in state.SessionId, in row, entities,
                        out Entity actor, out RuntimeBuildingEntity building))
                    return Fail("Mission-owned helipad row has no unique registered attempt owner.", out error);
                if (building.IsDestroyed || !SkirmishArmyGroupSystem.IsAlive(em, actor))
                    deadRows.Add(i);
            }
            // Validate the complete input before any change. A dead real owner is
            // ordinary unavailability, never a reason to erase ground rows.
            if (pruneDeadRows)
                for (int i = deadRows.Length - 1; i >= 0; i--)
                    rows.RemoveAt(deadRows[i]);
            error = null;
            return true;
        }

        // Initialization smoke only. Later destruction is ordinary unavailability;
        // the runtime binder never recreates or continuously demands starting grants.
        internal static bool TryValidateInitialHelipadGrants(
            EntityManager em, DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> rows,
            in GridConfig grid, out string error)
        {
            if (!TryResolveOwnership(em, out bool missionOwned, out Entity session, out error))
                return false;
            if (!missionOwned)
                return true;
            if (!TryValidateHelipadRows(em, session, rows, in grid, out error, pruneDeadRows: false))
                return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var grants = setup.Structures;
            if (grants == null)
                return Fail("Initial mission production infrastructure grants are missing.", out error);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            using var owners = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishStructureIdentityComponent), typeof(RuntimeBuildingCombatInfo));
            using var entities = owners.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < grants.Length; i++)
            {
                var grant = grants[i];
                if (SkirmishStructureIds.ProducerFor(grant.StructureId) != SkirmishProducerKind.Helipad)
                    continue;
                int expected = 0;
                for (int prior = 0; prior <= i; prior++)
                    if (grants[prior].FactionId == grant.FactionId &&
                        SkirmishStructureIds.ProducerFor(grants[prior].StructureId) == SkirmishProducerKind.Helipad)
                        expected++;
                int actual = 0;
                for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                {
                    var row = rows[rowIndex];
                    if (!row.BuildingId.Equals(HelipadBuildingId) || row.FactionId != grant.FactionId)
                        continue;
                    bool earlier = false;
                    for (int prior = 0; prior < rowIndex; prior++)
                        earlier |= rows[prior].BuildingId.Equals(HelipadBuildingId) &&
                            rows[prior].BuildingRuntimeId == row.BuildingRuntimeId;
                    if (!earlier && TryFindRegisteredOwner(em, in state.SessionId, in row, entities,
                            out Entity actor, out RuntimeBuildingEntity building) &&
                        !building.IsDestroyed && SkirmishArmyGroupSystem.IsAlive(em, actor))
                        actual++;
                }
                if (actual < expected)
                    return Fail($"Initial mission helipad grant is missing a living runtime slot owner for faction {grant.FactionId}: expected {expected}, actual {actual}.", out error);
            }
            error = null;
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }

        private static bool RequiresExpandedOwner(int index) => index > 7 ||
            index == SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex ||
            index == SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex ||
            index == SkirmishPresetConfig.DesertBaseAirMobileEstablishedScenarioIndex ||
            index == SkirmishPresetConfig.DesertBaseCombinedArmsFieldScenarioIndex;

        private static bool TryFindRegisteredOwner(
            EntityManager em, in FixedString64Bytes sessionId,
            in BuildingFactionProductionSpawnPointReadModel row, NativeArray<Entity> owners,
            out Entity actor, out RuntimeBuildingEntity building)
        {
            actor = Entity.Null;
            building = null;
            int count = 0;
            for (int i = 0; i < owners.Length; i++)
            {
                Entity entity = owners[i];
                var info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (info.RuntimeBuildingId != row.BuildingRuntimeId || info.OwnerFactionId != row.FactionId ||
                    !owned.SessionId.Equals(sessionId) || owned.FactionId != row.FactionId || owned.IsStructure == 0 ||
                    em.GetComponentData<SkirmishStructureIdentityComponent>(entity).Producer != SkirmishProducerKind.Helipad)
                    continue;
                if (!em.HasComponent<SkirmishNativeBuildingCheckpointRecord>(entity))
                    continue;
                var candidate = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(entity)?.Building;
                if (candidate == null || candidate.CombatEntity != entity || candidate.Id != row.BuildingRuntimeId ||
                    candidate.Instance == null || !candidate.HasOwnerFaction || candidate.OwnerFactionId != row.FactionId ||
                    candidate.ProductionSpawnLocalPositions == null || row.SlotIndex >= candidate.ProductionSpawnLocalPositions.Length)
                    continue;
                actor = entity;
                building = candidate;
                count++;
            }
            return count == 1;
        }
    }
}
