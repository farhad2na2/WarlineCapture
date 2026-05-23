Lane
Gameplay

Task
Review whether BuildingPlacementSystem is now only a compatibility facade and can be kept temporarily as a shell while callers migrate away.

Files changed
- Design/AgentReports/2026-05-23_gameplay-building-placement-facade-audit.md

Contracts touched
- No contract changes.
- Existing contract still correctly says BuildingPlacementSystem is legacy facade debt and may keep active placement lifecycle plus temporary facade methods during migration.

User-visible behavior
- No user-visible gameplay change.

Validation run
- Code inspection only.
- Reviewed BuildingPlacementSystem method/field ownership against Design/Architecture/gameplay_solid_ecs_contract.md.

Validation result
- BuildingPlacementSystem is not yet only a compatibility facade.
- It is a thinner facade than before, but it still owns real gameplay orchestration and mutable state beyond simple compatibility delegation.

Known gaps
- Active placement lifecycle/state still lives in BuildingPlacementSystem: _activePlacement, _activePlacementCost, BeginPlacement, CancelPlacement, ConfirmBuildingPlacement, UpdatePlacement, UpdatePlacementVisual, PlaceBuilding, ResolveCurrentPlacementFocusWorldPosition.
- Managed per-frame orchestration still lives there: Update, resource production tick routing, resource hauler update routing, destroyed building sync/update routing, barrier door update routing, pointer input handling, and building click routing.
- Resource hauler/path bridge logic remains there: UpdateResourceHaulers, IsHaulerAtBuildingApproach, TryAssignSelectedHaulerOrders, TryIssueHaulerMoveToBuilding, TryFindBuildingApproachCell, perimeter goal scoring, and related path request helpers.
- Runtime placement/spawn bridge logic remains there: TrySpawnRuntimeBuilding, TrySpawnRuntimeWallRun, TrySpawnRuntimeWallSegment, TrySpawnInitialBuilding, TryFindValidInitialBuildingOrigin, TryResolveInitialPlacementOrigin, SpawnInitialTestRoster, and runtime side-effect deferral/redirect helpers.
- Visual/grid helpers remain there: CreateBuildingVisualInstance, PositionBuildingObject, GetEffectivePlacementRect, GetFootprintCenter, CenterCellToOrigin, TryGetPrefabModelBounds, TransformBounds.
- Base breach approach-cell routing still has local tactical pathing helpers despite barrier target selection being extracted.
- Nested compatibility data types still live there: BuildingDefinition, RuntimeBuildingData, pending production DTOs, UI entry DTOs, and configured entry DTOs.

Cross-lane impacts
- Do not rename or remove BuildingPlacementSystem yet; other lanes and UI still rely on its public API.
- Treat it as a legacy compatibility facade in progress, not a completed facade.

Next recommended task
- Extract active placement lifecycle/state into a dedicated BuildingPlacementLifecycleSystem first. Keep BuildingPlacementSystem as the public shell that delegates begin/cancel/confirm/update-placement behavior to that system.
- After lifecycle extraction, move remaining generic grid/visual helpers by slice: visual instantiation/positioning to BuildingVisualSystem or a narrow placement visual boundary, runtime spawn/origin helpers to BuildingRuntimeCreationSystem/BuildingPlacementCommitSystem, and hauler/path bridge helpers to ResourceHaulerSystem or a dedicated hauler path boundary.
