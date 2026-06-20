# Phase 7 Agent F Tracker - Rendering, VFX, Visual Bridges, Camera, And Presentation Boundaries

Purpose:
Retire non-UI gameplay visual ownership by converting data-only visual state to focused `ISystem` processors and moving Unity object presentation, pooling, camera, material, light, renderer, ParticleSystem, and GameObject work into counted managed presentation `SystemBase` exceptions or ECS entity-prefab pipelines. Do not introduce updating MonoBehaviours.

Branch:
`codex/phase7-agent-f-rendering-vfx`

Progress snapshot:

- Checklist progress: `0 / 58 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `58`.
- Current target: `F0 - rendering/VFX inventory intake`.
- Data `ISystem` conversions completed: `0`.
- Managed presentation `SystemBase` exceptions created: `0`.
- Entity-prefab visual pipelines created: `0`.
- Retired managed ECS visual owners: `0`.
- Validation status: `not started`.

Ownership:

- Owns rendering, VFX, marker, trace, impostor, attached-light, destroyed visual, visual-quality, camera-adjacent, and visual bridge systems assigned by Agent A.
- Coordinates with Agent C for selection/order markers.
- Coordinates with Agent D for building visuals, destroyed visuals, foundation visuals, and markers.
- Coordinates with Agent E for road visuals, city visuals, decoration visuals, day/night, and citizen visible presentation.

Do not touch:

- Gameplay policy in selection, building, road, runtime-city, or citizen systems except through visual request/result contracts.
- UI Toolkit/Canvas implementation.
- Main Phase 7 tracker except through handoff reports.
- Any new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loop, or manager-style MonoBehaviour ticker. MonoBehaviours may hold references and expose callable presentation methods only.

Candidate examples to verify, not pre-approved:

- `UnitAttachedLightSystem`
- `UnitAttackTraceSystem`
- `UnitImpostorRenderSystem`
- `UnitAttackVfxRequestSystem`
- `CombatGameObjectVfxPlaybackSystem`
- `GroundMissileRocketTrailSystem`
- `AirMissileProjectileTrailSystem`
- `BuildingDestroyedVisualSystem`
- `BuildingFoundationVisualSystem`
- `RoadChunkVisualSystem`
- `RoadBuildPlacementVisualSystem`
- `SelectionOrderMarkerSystem`
- `SelectionScreenMarkerSystem`
- `BuildingSelectionMarkerSystem`
- `RuntimeCameraReferenceSystem`
- `RtsCameraSystem`
- `VisualQualitySettingsSystem`

## F0 - Inventory Intake

- [ ] Wait for Agent A inventory and guardrails.
- [ ] Pull Agent F rows and inspect source files and call sites.
- [ ] Classify each target as data-only visual state, entity-prefab visual pipeline, managed presentation `SystemBase` exception, camera `SystemBase` exception, view/reference-only MonoBehaviour, or retire/fold.
- [ ] Identify managed blockers: `GameObject`, `Transform`, `Camera`, `Renderer`, `Material`, `Light`, `UnityEngine.Object`, pooling, object instantiate/destroy, scene roots, public renderer interfaces.
- [ ] Identify gameplay policy that must move out before a boundary can stay as a managed presentation `SystemBase` exception.
- [ ] Identify any existing MonoBehaviour update/coroutine ownership and plan removal or non-Phase-7 deferral; do not add new ones.
- [ ] Write an initial Agent F handoff with target list and cross-agent dependencies.

Acceptance:

- No Unity object owner is planned as unmanaged `ISystem`.
- Every managed presentation exception has data-only inputs, no gameplay policy, and no MonoBehaviour update bridge.

## F1 - Visual Request/Result Contracts

- [ ] Inventory existing visual request components/buffers.
- [ ] Define or reuse explicit requests for VFX playback, traces, impostors, attached lights, destroyed visuals, road visuals, markers, and quality changes.
- [ ] Ensure gameplay systems only enqueue requests or update ECS visual state.
- [ ] Ensure managed presentation `SystemBase` exceptions consume requests/results without deciding gameplay.
- [ ] Add architecture validation for visual request ownership.

Acceptance:

- Visual presentation does not require gameplay systems to hold Unity object references.

## F2 - Entity-Prefab Visual Pipelines

- [ ] Identify visuals that can become ECS entity prefabs.
- [ ] Convert spawn/update/cleanup to `ISystem` with ECB playback and explicit lifetime components.
- [ ] Replace GameObject prefab fallback with entity prefab/source-key data before conversion.
- [ ] Preserve wrapper-aware contracts where existing tests require them.
- [ ] Run render-budget and visual smoke validation after each conversion.

Acceptance:

- Entity-prefab visuals do not allocate managed objects per frame.
- Cleanup is explicit and deterministic.

## F3 - VFX Playback And Trails

- [ ] Inspect missile launch/impact systems and trail/playback systems.
- [ ] Keep launch/impact gameplay policy in ECS processors.
- [ ] Move pooled GameObject or ParticleSystem playback to counted managed presentation `SystemBase` exceptions when Unity objects remain.
- [ ] Keep VFX presenter MonoBehaviours view/reference-only with no `Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.
- [ ] Convert request generation to `ISystem` where data-only.
- [ ] Convert trail lifetime/state ECS updates to `ISystem` where data-only.
- [ ] Run air missile, ground missile, combat/death, and VFX validation.

Acceptance:

- VFX playback does not own damage, targeting, or gameplay timing.
- Pooled GameObject/ParticleSystem work is a counted managed presentation `SystemBase` exception when it must tick Unity objects, not a gameplay `SystemBase` and not an updating MonoBehaviour.

## F4 - Markers, Selection Visuals, And Building Visuals

- [ ] Coordinate with Agent C before touching selection/order markers.
- [ ] Coordinate with Agent D before touching building markers, destroyed visuals, foundation visuals, or faction visuals.
- [ ] Convert marker visibility/state to `ISystem` where data-only.
- [ ] Move marker GameObject or renderer application to counted managed presentation `SystemBase` exceptions or entity-prefab visuals.
- [ ] Convert building destroyed/foundation/faction visual ECS state to `ISystem` where data-only.
- [ ] Run selection marker, building selection marker, building faction visual, and destroyed visual validations.

Acceptance:

- Selection/building visual state is data-driven.
- Presentation cannot change gameplay state.

## F5 - Camera And Quality Boundaries

- [ ] Inspect `RuntimeCameraReferenceSystem`, `RtsCameraSystem`, and `VisualQualitySettingsSystem`.
- [ ] Split camera input/application from ECS camera request/result data.
- [ ] Keep actual `Camera`, `Transform`, and quality asset application in counted managed presentation/config/camera `SystemBase` exceptions when ticking is required.
- [ ] Convert pure camera request/read-model state to `ISystem` where practical.
- [ ] Convert visual-quality ECS state to `ISystem` if it does not touch `ScriptableObject`, render pipeline assets, or Unity object settings.
- [ ] Run visual quality, camera smoke, and match runtime validation.

Acceptance:

- Camera and quality presentation boundaries are explicit and passive.
- No unmanaged `ISystem` touches Unity object settings.
- No camera or quality bridge uses MonoBehaviour `Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.

## F6 - Retire/Fold Visual Bridges

- [ ] Identify visual bridges that only forward data.
- [ ] Fold pure helpers into request producers or passive consumers.
- [ ] Delete empty ECS managed visual shells after call sites are removed.
- [ ] Preserve `.meta` files.
- [ ] Add guardrails preventing new broad visual facade shells.
- [ ] Run compile and graphics-capable smoke validation.

Acceptance:

- No managed ECS visual bridge remains without a concrete Unity object blocker.

## F7 - Agent F Completion

- [ ] Run `git diff --check`.
- [ ] Run render-budget validation.
- [ ] Run vehicle visual validation.
- [ ] Run missile VFX validation.
- [ ] Run attached-light validation.
- [ ] Run marker validations.
- [ ] Run visual-quality validation.
- [ ] Run graphics-capable match smoke.
- [ ] Run architecture guardrails.
- [ ] Write `Design/AgentReports/YYYY-MM-DD_phase7_agent_f_rendering_vfx_handoff.md`.

Handoff format:

- Checklist progress.
- Visual data systems converted.
- Managed presentation `SystemBase` exceptions created.
- Entity-prefab visual pipelines created.
- Managed ECS visual owners retired.
- Managed presentation `SystemBase` exceptions retained and counted.
- Cross-agent contracts changed.
- Validation commands and logs.
- Remaining blockers.
