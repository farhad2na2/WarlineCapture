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
            var result = ReadCore(em, session, publicObjective);
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
            return kind == AriaGroundApproachKind.AuthoredLogicalGround;
        }

        static SkirmishPublicSelectedApproach Unavailable(SkirmishPublicApproachUnavailableReason reason) =>
            new SkirmishPublicSelectedApproach { UnavailableReason = reason };

        static SkirmishPublicSelectedApproach ReadCore(EntityManager em, Entity session, float3 objective)
        {
            if (!math.all(math.isfinite(objective)) || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(session))
                return Unavailable(SkirmishPublicApproachUnavailableReason.ExactAuthoredSessionRequired);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var launch = em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
            if (state.IsLegacy != 0 || setup == null || launch == null ||
                !launch.MatchesIdentity(setup, state) || launch.Definition?.CampaignMap == null ||
                setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                setup.ArmyProfileId != SkirmishArmyProfileId.GroundManeuver ||
                !GroundMissionContext.SupportsOriginalGroundStart(setup.StartPackageId, setup.Readiness) ||
                !SkirmishMissionLogicalBindingProvider.TryRead(em, session, out _))
                return Unavailable(SkirmishPublicApproachUnavailableReason.AuthoredSourceSessionUnavailable);
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable),
                typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            if (grids.CalculateEntityCount() != 1) return Unavailable(SkirmishPublicApproachUnavailableReason.UniqueSourceGridRequired);
            Entity gridEntity = grids.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            var sourceGrid = launch.Definition.CampaignMap.GridMetadata;
            if (grid.Width != sourceGrid.Dimensions.x || grid.Height != sourceGrid.Dimensions.y ||
                grid.CellSize != sourceGrid.CellSize || !math.all(grid.Origin == (float3)sourceGrid.Origin) ||
                !SkirmishTypedSpawnConstraint.HasCompleteAllocator(em, gridEntity, grid))
                return Unavailable(SkirmishPublicApproachUnavailableReason.SourceGridMismatch);
            var publishedObjective = SkirmishPublicObjectiveProjection.Read(em, session);
            if (!publishedObjective.EnemyObjectiveAlive || publishedObjective.EnemyPosition.X != objective.x ||
                publishedObjective.EnemyPosition.Y != objective.y || publishedObjective.EnemyPosition.Z != objective.z)
                return Unavailable(SkirmishPublicApproachUnavailableReason.CurrentPublicObjectiveRequired);
            int2 desired = GridUtils.WorldToCell(grid, objective);
            if (!GridUtils.InBounds(desired, grid.Width, grid.Height))
                return Unavailable(SkirmishPublicApproachUnavailableReason.ObjectiveOutsideSourceGrid);
            using var selectedQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(), ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitMove>());
            using var selected = new NativeList<Entity>(Allocator.Temp);
            SelectedMoveOrderCommandSystem.CollectSelectedMoveEntities(em, selectedQuery,
                em.GetEntityTypeHandle(), null, selected);
            if (selected.Length == 0) return Unavailable(SkirmishPublicApproachUnavailableReason.NoSelectedMoveEligibleForce);
            var members = new List<SkirmishPublicApproachMember>(selected.Length);
            for (int i = 0; i < selected.Length; i++)
            {
                Entity unit = selected[i];
                if (!SelectionStateCompositionSystemHelper.IsCacheableSelectedMoveEntity(em, unit) ||
                    !SkirmishArmyGroupSystem.IsAlive(em, unit) || em.HasComponent<UnitAirMovement>(unit) ||
                    !em.HasComponent<SkirmishAttemptOwnedComponent>(unit) || !em.HasComponent<SkirmishArmyGroupMembershipComponent>(unit) ||
                    !em.HasComponent<Unity.Transforms.LocalTransform>(unit))
                    return Unavailable(SkirmishPublicApproachUnavailableReason.InvalidSelectedGroundCohort);
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId != 1 || owned.IsStructure != 0 || owned.CapacityReleased != 0 ||
                    !owned.SessionId.Equals(state.SessionId) || !em.HasComponent<Faction>(unit) ||
                    em.GetComponentData<Faction>(unit).Id != owned.FactionId)
                    return Unavailable(SkirmishPublicApproachUnavailableReason.ForeignOrReleasedForce);
                float3 position = em.GetComponentData<Unity.Transforms.LocalTransform>(unit).Position;
                var overlay = em.HasComponent<SkirmishRoleOverlayComponent>(unit)
                    ? em.GetComponentData<SkirmishRoleOverlayComponent>(unit) : default;
                uint groupId = em.GetComponentData<SkirmishArmyGroupMembershipComponent>(unit).GroupId;
                if (groupId == 0) return Unavailable(SkirmishPublicApproachUnavailableReason.MissingArmyGroup);
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
            if (leader < 0) { result.UnavailableReason = SkirmishPublicApproachUnavailableReason.NoAcquisitionCapableForce; return result; }
            using var surfaces = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            var surfaceReader = new MapSurfacePathfindingSnapshot();
            if (!surfaceReader.TryCreateContext(em, surfaces, out var surface))
            { result.UnavailableReason = SkirmishPublicApproachUnavailableReason.ActualSourceSurfaceUnavailable; return result; }
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
            if (!legal) { result.UnavailableReason = SkirmishPublicApproachUnavailableReason.NoLegalApproachGoal; return result; }
            float3 world = GridUtils.CellToWorldCenter(grid, goal);
            float distanceSquared = math.distancesq(world.xz, objective.xz);
            float radius = members[leader].AcquisitionRadius;
            result.LegalGoalWithinOwnAcquisitionRange = distanceSquared <= radius * radius;
            if (!result.LegalGoalWithinOwnAcquisitionRange)
            { result.UnavailableReason = SkirmishPublicApproachUnavailableReason.LegalGoalOutsideAcquisitionEnvelope; return result; }
            result.LegalGoalAvailable = true;
            result.GoalX = world.x; result.GoalY = world.y; result.GoalZ = world.z;
            // This is a legal leader planning candidate, not all-member accepted
            // movement, path reachability, visible contact, or completed approach.
            return result;
        }
    }
}
