using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Components;
using Game.Configs;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Game.Runtime.Pathfinding;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Own-force, public-objective planning only. The command owner still decides
    // acceptance; neither range nor this readback establishes an enemy contact.
    public static class SkirmishPublicSelectedApproachProjection
    {
        public static SkirmishPublicSelectedApproach Read(EntityManager em, Entity session, float3 publicObjective)
        {
            if (!Required(em, session)) return default;
            long began = Stopwatch.GetTimestamp(), allocated = GC.GetAllocatedBytesForCurrentThread();
            var orderVersion = em.EntityOrderVersion;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup = em.HasComponent<SkirmishResolvedSetupRecord>(session)
                ? em.GetComponentObject<SkirmishResolvedSetupRecord>(session)?.Setup : null;
            var result = IsSupportedS003Ordinary(state.CatalogId.ToString(), setup)
                ? ReadS003OrdinaryCore(em, session, publicObjective)
                : ReadCore(em, session, publicObjective);
            if (em.EntityOrderVersion != orderVersion)
                throw new InvalidOperationException("Own approach planning changed entity structure.");
            result.Required = true;
            result.TimerFrequency = Stopwatch.Frequency;
            result.ProjectionTicks = Stopwatch.GetTimestamp() - began;
            result.AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            return result;
        }

        // Same actual canonical row/classifier as the shared mission context.
        // Missing/malformed authored data stays required-and-unavailable rather
        // than silently becoming the old global-quorum path.
        static bool Required(EntityManager em, Entity session)
        {
            if (!em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return false;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (state.IsLegacy != 0) return false;
            var catalog = SkirmishBattleCatalogConfig.Load();
            if (catalog?.Entries == null) return true;
            int matches = 0; SkirmishBattleCatalogEntry selected = default;
            foreach (var row in catalog.Entries)
                if (row.ScenarioId == state.CatalogId.ToString()) { matches++; selected = row; }
            if (matches != 1) return true;
            var setup = em.HasComponent<SkirmishResolvedSetupRecord>(session)
                ? em.GetComponentObject<SkirmishResolvedSetupRecord>(session)?.Setup : null;
            var definition = em.HasComponent<SkirmishLaunchDefinitionRecord>(session)
                ? em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session)?.Definition : null;
            if (!GroundMissionContextFactory.TryResolveApproachKind(selected.MapId, definition, setup,
                out var kind, out _)) return true;
            return kind == AriaGroundApproachKind.AuthoredLogicalGround ||
                (kind == AriaGroundApproachKind.Ordinary && IsSupportedS003Ordinary(state.CatalogId.ToString(), setup));
        }

        internal static bool MatchesSourceRecord(OperationMapGridSourceRecord source, ActiveOperationMapComponent active, FixedString64Bytes mapId, FixedString128Bytes definitionHash, OperationMapGridBlob grid) =>
            source.Generation > 0 && source.Generation == active.Generation && source.MapId.Equals(active.OperationMapId) &&
            source.MapId.Equals(mapId) && source.DefinitionHash.Equals(definitionHash) && !source.DefinitionHash.IsEmpty &&
            source.GridGuid.Equals(grid.AssetGuid) && !source.GridGuid.IsEmpty && source.GridHash.Equals(grid.ContentHash) && !source.GridHash.IsEmpty;

        internal static bool MatchesOrdinaryGrid(GridConfig grid, OperationMapGridBlob sourceGrid) =>
            !sourceGrid.AssetGuid.IsEmpty && !sourceGrid.ContentHash.IsEmpty && grid.Width == sourceGrid.Dimensions.x &&
            grid.Height == sourceGrid.Dimensions.y && math.abs(grid.CellSize - sourceGrid.CellSize) <= 0.0001f &&
            math.all(math.abs(grid.Origin - sourceGrid.Origin) <= new float3(0.0001f));

        internal static bool IsSameSessionPlayerMember(SkirmishAttemptOwnedComponent owned, Faction faction, FixedString64Bytes sessionId) =>
            owned.FactionId == 1 && owned.IsStructure == 0 && owned.CapacityReleased == 0 &&
            owned.SessionId.Equals(sessionId) && faction.Id == owned.FactionId;

        internal static bool IsSupportedS003Ordinary(string catalogId, SkirmishResolvedSetup setup) =>
            catalogId == "S003" && setup != null && setup.CatalogId == catalogId && setup.DefinitionId == "skirmish.s003" &&
            setup.OperationMapId == "opmap.skirmish.desert_base_01" &&
            setup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault && setup.ArmyProfileId == SkirmishArmyProfileId.AirMobile &&
            setup.StartPackageId == SkirmishStartPackageId.FieldBase && setup.Readiness == SkirmishReadinessStage.Field;

        static SkirmishPublicSelectedApproach Unavailable(string reason) =>
            new SkirmishPublicSelectedApproach { UnavailableReason = reason };

        static SkirmishPublicSelectedApproach ReadCore(EntityManager em, Entity session, float3 objective)
        {
            if (!math.all(math.isfinite(objective)) || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(session))
                return Unavailable("Exact authored session and finite public objective required.");
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var launch = em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
            if (state.IsLegacy != 0 || setup == null || launch == null ||
                !launch.MatchesIdentity(setup, state) || launch.Definition?.CampaignMap == null ||
                setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                setup.ArmyProfileId != SkirmishArmyProfileId.GroundManeuver ||
                !((setup.StartPackageId == SkirmishStartPackageId.EstablishedBase && setup.Readiness == SkirmishReadinessStage.Established) ||
                  (setup.StartPackageId == SkirmishStartPackageId.FieldBase && setup.Readiness == SkirmishReadinessStage.Field)) ||
                !SkirmishMissionLogicalBindingProvider.TryRead(em, session, out _))
                return Unavailable("Authored source-bound ground session identity unavailable or changed.");
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable),
                typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            if (grids.CalculateEntityCount() != 1) return Unavailable("Unique actual source grid required.");
            Entity gridEntity = grids.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            var sourceGrid = launch.Definition.CampaignMap.GridMetadata;
            if (grid.Width != sourceGrid.Dimensions.x || grid.Height != sourceGrid.Dimensions.y ||
                grid.CellSize != sourceGrid.CellSize || !math.all(grid.Origin == (float3)sourceGrid.Origin) ||
                !SkirmishTypedSpawnConstraint.HasCompleteAllocator(em, gridEntity, grid))
                return Unavailable("Actual grid differs from the same authored source frame.");
            var publishedObjective = SkirmishPublicObjectiveProjection.Read(em, session);
            if (!publishedObjective.EnemyObjectiveAlive || publishedObjective.EnemyPosition.X != objective.x ||
                publishedObjective.EnemyPosition.Y != objective.y || publishedObjective.EnemyPosition.Z != objective.z)
                return Unavailable("Same current published objective required; hidden targets are not approach inputs.");
            int2 desired = GridUtils.WorldToCell(grid, objective);
            if (!GridUtils.InBounds(desired, grid.Width, grid.Height))
                return Unavailable("Public objective lies outside the actual source grid.");
            using var selectedQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(), ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitMove>());
            using var selected = new NativeList<Entity>(Allocator.Temp);
            SelectedMoveOrderCommandSystem.CollectSelectedMoveEntities(em, selectedQuery,
                em.GetEntityTypeHandle(), null, selected);
            if (selected.Length == 0) return Unavailable("No selected original move-eligible force.");
            var members = new List<SkirmishPublicApproachMember>(selected.Length);
            for (int i = 0; i < selected.Length; i++)
            {
                Entity unit = selected[i];
                if (!SelectionStateCompositionSystemHelper.IsCacheableSelectedMoveEntity(em, unit) ||
                    !SkirmishArmyGroupSystem.IsAlive(em, unit) || em.HasComponent<UnitAirMovement>(unit) ||
                    !em.HasComponent<SkirmishAttemptOwnedComponent>(unit) || !em.HasComponent<SkirmishArmyGroupMembershipComponent>(unit) ||
                    !em.HasComponent<Unity.Transforms.LocalTransform>(unit))
                    return Unavailable("Selected force is not a living owned ground move cohort.");
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId != 1 || owned.IsStructure != 0 || owned.CapacityReleased != 0 ||
                    !owned.SessionId.Equals(state.SessionId) || !em.HasComponent<Faction>(unit) ||
                    em.GetComponentData<Faction>(unit).Id != owned.FactionId)
                    return Unavailable("Foreign or released selected force cannot advertise an authored approach.");
                float3 position = em.GetComponentData<Unity.Transforms.LocalTransform>(unit).Position;
                var overlay = em.HasComponent<SkirmishRoleOverlayComponent>(unit)
                    ? em.GetComponentData<SkirmishRoleOverlayComponent>(unit) : default;
                uint groupId = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId;
                if (groupId == 0) return Unavailable("Selected owned actor has no actual army-group membership.");
                bool ownMoveGoal = em.HasComponent<UnitTarget>(unit) &&
                    (em.HasComponent<AttackMoveOrder>(unit) || em.HasComponent<ManualMoveOrderTag>(unit));
                int2 ownGoal = ownMoveGoal ? em.GetComponentData<UnitTarget>(unit).Cell : default;
                members.Add(new SkirmishPublicApproachMember {
                    GroupId = groupId, X = position.x, Y = position.y, Z = position.z,
                    HasOwnMoveGoal = ownMoveGoal, OwnGoalX = ownGoal.x, OwnGoalZ = ownGoal.y,
                    AcquisitionRadius = SkirmishFogService.ReadOwnAcquisitionRadius(em, unit, grid.CellSize),
                    StructureCapable = overlay.Damage > 0 && (overlay.TargetDomains & SkirmishTargetDomain.Structure) != 0
                });
            }
            var result = AriaGroundApproachPolicy.ObserveSelectedOwnForce(
                new AriaGroundPosition(objective.x, objective.y, objective.z), members);
            result.PublicObjectiveContactVisible = publishedObjective.EnemyTargetVisible;
            int leader = -1;
            for (int i = 0; i < members.Count; i++)
                if (members[i].AcquisitionRadius > 0 &&
                    (result.StructureCapableCount == 0 || members[i].StructureCapable)) { leader = i; break; }
            if (leader < 0) { result.UnavailableReason = "Selected own force has no original acquisition capability."; return result; }
            using var surfaces = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            var surfaceReader = new MapSurfacePathfindingSnapshot();
            if (!surfaceReader.TryCreateContext(em, surfaces, out var surface))
            { result.UnavailableReason = "Actual source surface unavailable; no flat planning fallback."; return result; }
            long goalBegan = Stopwatch.GetTimestamp();
            var moveOwner = new UnitMoveOrderSystem();
            var currentCells = moveOwner.BuildSelectedCurrentFootprintCells(em, grid, selected.AsArray());
            var localReservations = new HashSet<int>();
            var blocker = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            bool legal = moveOwner.TryPlanManualMoveGoal(em, grid,
                em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(), blocker.Blocked,
                blocker.FriendlyPassFactionIds, em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied,
                localReservations, currentCells, selected[leader], desired, leader, surface, out int2 goal);
            result.GoalEvaluationTicks = Stopwatch.GetTimestamp() - goalBegan;
            if (!legal) { result.UnavailableReason = "Original move owner found no legal approach goal."; return result; }
            float3 world = GridUtils.CellToWorldCenter(grid, goal);
            float distanceSquared = math.distancesq(world.xz, objective.xz);
            float radius = members[leader].AcquisitionRadius;
            result.LegalGoalWithinOwnAcquisitionRange = distanceSquared <= radius * radius;
            if (!result.LegalGoalWithinOwnAcquisitionRange)
            { result.UnavailableReason = "Original legal goal remains outside own acquisition envelope; source-fit/contact gate pending."; return result; }
            result.LegalGoalAvailable = true;
            result.GoalX = world.x; result.GoalY = world.y; result.GoalZ = world.z;
            // This is a legal leader planning candidate, not all-member accepted
            // movement, path reachability, visible contact, or completed approach.
            return result;
        }

        static SkirmishPublicSelectedApproach ReadS003OrdinaryCore(EntityManager em, Entity session, float3 objective)
        {
            if (!math.all(math.isfinite(objective)) || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(session))
                return Unavailable("Exact authored session and finite public objective required.");
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var launch = em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
            var catalog = SkirmishBattleCatalogConfig.Load();
            var rows = catalog?.Entries;
            SkirmishBattleCatalogEntry selectedRow = default; int matches = 0;
            if (rows != null) foreach (var row in rows) if (row.ScenarioId == state.CatalogId.ToString()) { selectedRow = row; matches++; }
            if (state.IsLegacy != 0 || setup == null || launch == null || !launch.MatchesIdentity(setup, state) ||
                matches != 1 || !GroundMissionContextFactory.TryResolveApproachKind(selectedRow.MapId, launch.Definition, setup, out var kind, out _) ||
                (kind != AriaGroundApproachKind.Ordinary && kind != AriaGroundApproachKind.AuthoredLogicalGround) ||
                setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                (setup.ArmyProfileId != SkirmishArmyProfileId.GroundManeuver && setup.ArmyProfileId != SkirmishArmyProfileId.AirMobile) ||
                !GroundMissionContext.SupportsOriginalGroundStart(setup.StartPackageId, setup.Readiness) ||
                (kind == AriaGroundApproachKind.AuthoredLogicalGround && (launch.Definition?.CampaignMap == null || !SkirmishMissionLogicalBindingProvider.TryRead(em, session, out _))))
                return Unavailable("Source-bound ground session identity unavailable or changed.");
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable),
                typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            if (grids.CalculateEntityCount() != 1) return Unavailable("Unique actual source grid required.");
            Entity gridEntity = grids.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            if (!SkirmishTypedSpawnConstraint.HasCompleteAllocator(em, gridEntity, grid))
                return Unavailable("Complete actual source grid allocator required.");
            if (kind == AriaGroundApproachKind.Ordinary)
            {
                if (!OperationMapMetadataUtility.TryResolveActiveMetadata(em, out var actualMap, out var hasActiveMap, out _) || !hasActiveMap || !actualMap.IsCreated ||
                    selectedRow.MapId != "DB" || actualMap.Value.OperationMapId.ToString() != SkirmishMapLayoutBuilder.DesertBaseMapId)
                    return Unavailable("Selected Campaign map and actual active metadata identity must agree.");
                using var roots = em.CreateEntityQuery(typeof(OperationMapRootComponent), typeof(ActiveOperationMapComponent), typeof(OperationMapGridSourceRecord));
                if (roots.CalculateEntityCount() != 1) return Unavailable("Original source definition grid record unavailable.");
                var root = roots.GetSingletonEntity();
                var active = em.GetComponentData<ActiveOperationMapComponent>(root);
                var source = em.GetComponentData<OperationMapGridSourceRecord>(root);
                var sourceGrid = actualMap.Value.Grid;
                if (!MatchesSourceRecord(source, active, actualMap.Value.OperationMapId, actualMap.Value.ContentHash, sourceGrid) || !MatchesOrdinaryGrid(grid, sourceGrid))
                    return Unavailable("Actual runtime grid differs from active source-map navigation identity.");
            }
            if (kind == AriaGroundApproachKind.AuthoredLogicalGround)
            {
                var sourceGrid = launch.Definition.CampaignMap.GridMetadata;
                if (grid.Width != sourceGrid.Dimensions.x || grid.Height != sourceGrid.Dimensions.y ||
                    grid.CellSize != sourceGrid.CellSize || !math.all(grid.Origin == (float3)sourceGrid.Origin))
                    return Unavailable("Actual grid differs from the same authored source frame.");
            }
            var publishedObjective = SkirmishPublicObjectiveProjection.Read(em, session);
            if (!publishedObjective.EnemyObjectiveAlive || publishedObjective.EnemyPosition.X != objective.x ||
                publishedObjective.EnemyPosition.Y != objective.y || publishedObjective.EnemyPosition.Z != objective.z)
                return Unavailable("Same current published objective required; hidden targets are not approach inputs.");
            int2 desired = GridUtils.WorldToCell(grid, objective);
            if (!GridUtils.InBounds(desired, grid.Width, grid.Height))
                return Unavailable("Public objective lies outside the actual source grid.");
            using var selectedQuery = AttackMovePlanning.CreateSelectedFighterQuery(em);
            using var selected = selectedQuery.ToEntityArray(Allocator.Temp);
            if (selected.Length == 0) return Unavailable("No selected original move-eligible force.");
            if (kind == AriaGroundApproachKind.Ordinary)
            {
                using var sourceRoots = em.CreateEntityQuery(typeof(OperationMapRootComponent), typeof(OperationMapGridSourceRecord));
                if (sourceRoots.CalculateEntityCount() != 1) return Unavailable("Source grid record unavailable.");
                var source = em.GetComponentData<OperationMapGridSourceRecord>(sourceRoots.GetSingletonEntity());
                if (!em.HasComponent<GridWalkableSourceWitness>(gridEntity)) return Unavailable("Original baked walkability witness unavailable.");
                var witness = em.GetComponentData<GridWalkableSourceWitness>(gridEntity);
                var walkable = em.GetBuffer<GridWalkable>(gridEntity, true);
                if (source.WorldSequence != OperationMapGridSourceRecordWriter.WorldSequence(em) || !witness.Matches(source, walkable)) return Unavailable("Original source walkability witness differs or is stale.");
            }

            using var fighters = new NativeList<Entity>(Allocator.Temp);
            AttackMovePlanning.CollectFighters(em, selected, FactionIdentity.PlayerFactionId, fighters);
            if (fighters.Length == 0) return Unavailable("No selected original attack-move fighter.");
            var members = new List<SkirmishPublicApproachMember>(fighters.Length);
            for (int i = 0; i < fighters.Length; i++)
            {
                Entity unit = fighters[i];
                if (!SelectionStateCompositionSystemHelper.IsCacheableSelectedMoveEntity(em, unit) ||
                    !SkirmishArmyGroupSystem.IsAlive(em, unit) || em.HasComponent<UnitAirMovement>(unit) ||
                    !em.HasComponent<SkirmishAttemptOwnedComponent>(unit) || !em.HasComponent<SkirmishArmyGroupMembershipComponent>(unit) ||
                    !em.HasComponent<Unity.Transforms.LocalTransform>(unit))
                    return Unavailable("Selected force is not a living owned ground move cohort.");
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!em.HasComponent<Faction>(unit) ||
                    !IsSameSessionPlayerMember(owned, em.GetComponentData<Faction>(unit), state.SessionId))
                    return Unavailable("Foreign or released selected force cannot advertise an authored approach.");
                float3 position = em.GetComponentData<Unity.Transforms.LocalTransform>(unit).Position;
                var overlay = em.HasComponent<SkirmishRoleOverlayComponent>(unit)
                    ? em.GetComponentData<SkirmishRoleOverlayComponent>(unit) : default;
                uint groupId = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId;
                if (groupId == 0) return Unavailable("Selected owned actor has no actual army-group membership.");
                bool ownMoveGoal = em.HasComponent<UnitTarget>(unit) &&
                    (em.HasComponent<AttackMoveOrder>(unit) || em.HasComponent<ManualMoveOrderTag>(unit));
                int2 ownGoal = ownMoveGoal ? em.GetComponentData<UnitTarget>(unit).Cell : default;
                members.Add(new SkirmishPublicApproachMember {
                    GroupId = groupId, X = position.x, Y = position.y, Z = position.z,
                    HasOwnMoveGoal = ownMoveGoal, OwnGoalX = ownGoal.x, OwnGoalZ = ownGoal.y,
                    AcquisitionRadius = SkirmishFogService.ReadOwnAcquisitionRadius(em, unit, grid.CellSize),
                    StructureCapable = overlay.Damage > 0 && (overlay.TargetDomains & SkirmishTargetDomain.Structure) != 0
                });
            }
            var result = AriaGroundApproachPolicy.ObserveSelectedOwnForce(
                new AriaGroundPosition(objective.x, objective.y, objective.z), members);
            result.PublicObjectiveContactVisible = publishedObjective.EnemyTargetVisible;
            int leader = -1;
            for (int i = 0; i < members.Count; i++)
                if (members[i].AcquisitionRadius > 0 &&
                    (result.StructureCapableCount == 0 || members[i].StructureCapable)) { leader = i; break; }
            if (leader < 0) { result.UnavailableReason = "Selected own force has no original acquisition capability."; return result; }
            using var surfaces = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            var surfaceReader = new MapSurfacePathfindingSnapshot();
            if (!surfaceReader.TryCreateContext(em, surfaces, out var surface))
            { result.UnavailableReason = "Actual source surface unavailable; no flat planning fallback."; return result; }
            long goalBegan = Stopwatch.GetTimestamp();
            var moveOwner = new UnitMoveOrderSystem();
            var currentCells = moveOwner.BuildSelectedCurrentFootprintCells(em, grid, fighters.AsArray());
            var localReservations = new HashSet<int>();
            var blocker = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            bool legal = moveOwner.TryPlanManualMoveGoal(em, grid,
                em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(), blocker.Blocked,
                blocker.FriendlyPassFactionIds, em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied,
                localReservations, currentCells, fighters[leader], desired, leader, surface, out int2 goal);
            result.GoalEvaluationTicks = Stopwatch.GetTimestamp() - goalBegan;
            if (!legal) { result.UnavailableReason = "Original move owner found no legal approach goal."; return result; }
            // The UI clicks the preliminary candidate, so replay the actual
            // filtered formation around that final click (not around objective).
            // Check the complete original formation. For a rifle-only support
            // cohort, every member must be in its own acquisition envelope;
            // this does not make the cohort capable of attacking structures.
            var formation = new int2[fighters.Length];
            if (!AttackMovePlanning.TryPlanAll(em, grid, em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(),
                blocker.Blocked, blocker.FriendlyPassFactionIds, em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied,
                fighters.AsArray(), goal, surface, formation))
            { result.UnavailableReason = "Actual filtered formation has an illegal fallback goal."; return result; }
            for (int i = 0; i < members.Count; i++)
            {
                if (result.StructureCapableCount > 0 && !members[i].StructureCapable) continue;
                float3 slot = GridUtils.CellToWorldCenter(grid, formation[i]);
                float radius = members[i].AcquisitionRadius;
                if (radius <= 0 || math.distancesq(slot.xz, objective.xz) > radius * radius)
                { result.UnavailableReason = "A relevant formation slot lies outside its own acquisition envelope."; return result; }
            }
            float3 world = GridUtils.CellToWorldCenter(grid, goal);
            result.LegalGoalWithinOwnAcquisitionRange = true;
            result.LegalGoalAvailable = true;
            result.GoalX = world.x; result.GoalY = world.y; result.GoalZ = world.z;
            // This is a legal leader planning candidate, not all-member accepted
            // movement, path reachability, visible contact, or completed approach.
            return result;
        }
    }
}
