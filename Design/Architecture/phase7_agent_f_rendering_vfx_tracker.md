# Phase 7 Agent F Tracker - Rendering, Presentation, VFX, Camera, And Visual Boundaries

Purpose:
Reduce non-UI rendering/presentation `SystemBase` usage without damaging visuals. Agent F owns visual bridges, marker presentation, renderer/material/light updates, trails, missiles, particles, explosion VFX, visual quality bindings, and camera/presentation boundaries assigned by Agent A. Agent F must not convert Unity-object presentation into ugly or incomplete entity-only effects just to improve the `ISystem` percentage.

Branch:
`codex/phase7-agent-f-rendering-vfx`

Execution order:

1. Wait for Agent A to publish authoritative inventory rows and guardrails.
2. Pull only rows assigned to `AgentF`.
3. Classify each row as data request/result `ISystem`, split presentation boundary, managed presentation `SystemBase` exception, or retire/fold.
4. Convert pure ECS visual request/result systems first.
5. Preserve Unity presentation systems as counted managed `SystemBase` exceptions when they own `Renderer`, `Material`, `Light`, `Camera`, `ParticleSystem`, `LineRenderer`, `VisualEffect`, or other Unity objects that must tick.
6. Write handoffs under `Design/AgentReports/`; Agent A owns shared tracker integration.

Progress snapshot:

- Checklist progress: `0 / 89 complete (0.0%)`.
- In progress: `0`.
- Remaining open: `89`.
- Current target: `F0 - wait for Agent A inventory assignment`.
- Converted to `ISystem`: `0`.
- Split passive/managed boundaries: `0`.
- Managed presentation `SystemBase` exceptions: `0`.
- Retired/folded helpers: `0`.
- Validation status: `not started`.

Owned files:

- `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md`
- Agent F visual/presentation rows assigned by `Design/Architecture/systembase_to_isystem_inventory.md`
- Visual/VFX/camera focused tests or validation runners when practical
- Agent F handoff reports under `Design/AgentReports/`

Do not touch:

- UI Toolkit/Canvas views, UXML, USS, UI presenters, and menu systems.
- Building gameplay execution owned by Agent D.
- Road/city/citizen simulation owned by Agent E.
- Selection command intent owned by Agent C.
- Pure startup/config/diagnostic systems owned by Agent B.
- Scene/prefab/material/VFX assets unless the assigned row requires a documented visual validation slice.
- Shared trackers except this file.

Shared rules:

- Do not introduce `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or manager-style MonoBehaviour tickers.
- MonoBehaviours are view/reference holders only. They may hold serialized references and expose direct event methods, but they must not poll gameplay state.
- If Unity-object presentation must tick, keep it in a small counted managed `SystemBase` exception.
- Converted `ISystem` files must not hold or access `GameObject`, `Transform`, `Camera`, `Renderer`, `Material`, `Light`, `ParticleSystem`, `LineRenderer`, `VisualEffect`, `UnityEngine.Object`, `Object.Instantiate`, `Object.Destroy`, hierarchy paths, `Object.Find*`, or `Camera.main`.
- Do not replace ParticleSystem explosions, smoke, muzzle flashes, or trails with new entity visuals unless the user explicitly approves the art change.
- Do not downgrade visuals to meet an inheritance metric.
- Do not replace one broad visual bridge with one broad `ISystem`; split request/result data from managed presentation.
- Preserve Unity `.meta` files.

Reference documents:

- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`

Likely Agent F target families after Agent A review:

- Selection/building/road/command marker presentation.
- Unit, vehicle, projectile, missile, trail, and combat VFX presentation.
- Explosion, smoke, muzzle flash, particle, light, and material update bridges.
- Visual quality binding systems that apply ECS/settings data to rendering components.
- Runtime camera reference/presentation systems assigned by Agent A.
- Pure visual request/result data systems that can be converted to `ISystem`.

Likely not Agent F:

- UI Toolkit or Canvas screen rendering.
- Gameplay command validation, building production, road simulation, citizen simulation, or AI/faction logic.
- Runtime art redesign or entity-particle replacement unless explicitly approved.

## F0 - Intake And Visual Risk Classification

Goal:
Create a visual-safe worklist from Agent A's inventory.

- [ ] Read `Design/Architecture/systembase_to_isystem_inventory.md` after Agent A marks it ready.
- [ ] Filter rows assigned to `AgentF`.
- [ ] Copy row ids, type names, paths, dispositions, blockers, and validation gates into this tracker or an Agent F intake report.
- [ ] For each target, run `rg "<TypeName>" Assets/Game/Scripts Assets/Tests`.
- [ ] Record public methods/properties and callers, especially combat, building, selection, camera, and runtime composition systems.
- [ ] Record all Unity object blockers: `GameObject`, `Transform`, `Camera`, `Renderer`, `Material`, `Light`, `ParticleSystem`, `LineRenderer`, `TrailRenderer`, `VisualEffect`, `Mesh`, `SkinnedMeshRenderer`, `UnityEngine.Object`, and `ScriptableObject`.
- [ ] Record whether the system owns data request/result processing, visual state derivation, Unity object application, pooling, or asset references.
- [ ] Mark pure data rows as `DirectConvert`.
- [ ] Mark mixed rows as `SplitThenConvert`.
- [ ] Mark unavoidable Unity-object ticking rows as `ManagedPresentationSystemBaseException`.
- [ ] Return rows to Agent A when they are actually gameplay/UI/camera ownership outside Agent F scope.
- [ ] Update the progress snapshot denominator and current target.

Acceptance:

- Agent F has a concrete row list with visual risk per row.
- No art-changing conversion starts without explicit user approval.
- Managed presentation exceptions are visible and countable.

## F1 - Visual Request/Result Data Contracts

Goal:
Move gameplay-to-visual communication into ECS data so presentation systems do not own gameplay decisions.

- [ ] Inventory existing visual events, trace requests, impact events, marker requests, highlight state, and camera reference state.
- [ ] Coordinate with Agent C for selection/focus/command visual result data.
- [ ] Coordinate with Agent D for building placement, production, damage, and construction visual result data.
- [ ] Coordinate with Agent E for road, city, citizen, and environment visual result data.
- [ ] Define or reuse one-shot visual request buffers/entities for effects such as explosion, muzzle flash, projectile trail, selection pulse, or placement result.
- [ ] Define or reuse persistent visual state components for highlights, marker visibility, faction color, and quality settings.
- [ ] Ensure visual request data is ECS-friendly and references entities/source keys, not GameObjects.
- [ ] Ensure presentation systems consume requests without writing gameplay policy.
- [ ] Ensure one-shot visual requests are consumed or aged out deterministically.

Acceptance:

- Visual presentation has ECS inputs and does not call gameplay systems directly.
- Gameplay systems do not call Unity object visual APIs.

## F2 - Pure Visual Data Systems To ISystem

Goal:
Convert data-only visual processors while leaving Unity object application managed.

- [ ] Start with the lowest-risk pure ECS visual data row.
- [ ] Inspect lifecycle, query shape, update group/order, and consumers.
- [ ] Convert to `ISystem` only if no Unity object or managed presentation fields remain.
- [ ] Replace `Entities.ForEach` with `SystemAPI.Query`, explicit query iteration, or `IJobEntity`.
- [ ] Preserve request aging, result publication, marker state derivation, and quality-data calculations.
- [ ] Avoid direct renderer/material/light/camera/particle access.
- [ ] Add tests for result data when existing coverage is missing.
- [ ] Run focused validation before moving to the next row.

Acceptance:

- Data-only visual systems compile as `ISystem`.
- Unity object application remains outside converted systems.

## F3 - Managed Presentation SystemBase Exceptions

Goal:
Keep required Unity object ticking explicit, small, and counted.

- [ ] For each row with Unity object blockers, split pure ECS data derivation into an `ISystem` when practical.
- [ ] Keep renderer/material/light/camera/particle application in a small managed `SystemBase` exception.
- [ ] Do not move ticking presentation to MonoBehaviour.
- [ ] Do not create broad visual manager/facade systems.
- [ ] Keep managed exceptions read-only from gameplay perspective, except consuming visual requests and updating Unity presentation.
- [ ] Gate expensive work by visibility, request count, or changed state.
- [ ] Avoid hot-path allocations, LINQ, per-frame string formatting, or ungated logs.
- [ ] Record every managed exception in the Agent F handoff for Agent A's denominator.

Acceptance:

- Required Unity visuals are preserved.
- Managed exceptions are minimal and do not own gameplay policy.
- No new MonoBehaviour ticking is introduced.

## F4 - Particle, Explosion, Trail, And Combat VFX

Goal:
Preserve authored VFX quality while decoupling gameplay triggers from Unity object playback.

- [ ] Inventory explosion, smoke, muzzle flash, hit impact, projectile, missile, trail, and attack trace systems assigned to Agent F.
- [ ] Identify the authored effect type: `ParticleSystem`, `VisualEffect`, `LineRenderer`, `TrailRenderer`, light flash, mesh effect, or material animation.
- [ ] Keep authored ParticleSystem/VisualEffect assets unless the user explicitly approves replacement.
- [ ] Convert combat/VFX trigger data to ECS requests when possible.
- [ ] Keep playback/pooling of Unity VFX objects in a managed presentation `SystemBase` exception when ticking is required.
- [ ] Ensure `Play` calls are triggered by the managed presentation system consuming ECS requests, not by gameplay processors.
- [ ] Do not introduce `ExplosionVfxPresenter.Update()` or similar MonoBehaviour loops.
- [ ] Preserve pooling semantics, lifetime, scale, position, rotation, faction color, and quality gating.
- [ ] Validate visually or via smoke tests that effects still spawn at the right place and time.

Acceptance:

- Explosions and combat VFX look the same unless an explicit art-change task says otherwise.
- Gameplay emits ECS visual requests; presentation consumes them.
- No new MonoBehaviour update loop is introduced.

## F5 - Markers, Highlights, And Building/Road Visuals

Goal:
Preserve selection/build/road visual feedback while making ownership clear.

- [ ] Inventory marker/highlight systems for selection, focus, command, building placement, road placement, construction, and faction visuals.
- [ ] Coordinate data contracts with Agent C/D/E.
- [ ] Convert marker visibility/state derivation to `ISystem` when data-only.
- [ ] Keep mesh/material/renderer application in managed presentation exceptions.
- [ ] Preserve selection outlines, placement valid/invalid colors, road previews, building faction colors, and construction feedback.
- [ ] Avoid direct gameplay mutation from visual systems.
- [ ] Validate common user flows: select unit, target command, place building, place road, damage building.

Acceptance:

- Visual feedback remains equivalent to baseline.
- Visual systems consume ECS state and do not drive gameplay policy.

## F6 - Camera And Visual Quality Boundaries

Goal:
Keep camera/quality Unity object ownership managed while converting data policy where safe.

- [ ] Identify assigned camera reference, visual quality, or render config systems.
- [ ] Split quality policy/state calculation into ECS data when possible.
- [ ] Keep actual `Camera`, `RenderSettings`, renderer, material, volume, or pipeline API application in a managed `SystemBase` exception.
- [ ] Do not use `Camera.main` or hierarchy lookup.
- [ ] Preserve explicit serialized camera/reference assignment boundaries.
- [ ] Preserve low-quality setting behavior while avoiding accidental ignored quality config.
- [ ] Validate quality changes are applied and not responsible for gameplay FPS regressions unless evidence shows otherwise.

Acceptance:

- Camera and rendering object APIs are not in `ISystem`.
- Quality settings still apply through explicit references.

## F7 - Pooling And Instantiation Boundaries

Goal:
Reduce runtime `Object.Instantiate` where practical without changing visuals.

- [ ] Inventory visual object spawning/pooling in assigned systems.
- [ ] Prefer existing pools when available.
- [ ] If converting to Entity prefab is safe and visually equivalent, document the before/after and validate.
- [ ] If conversion would change visuals, keep Unity-object pooling in a managed presentation exception.
- [ ] Do not introduce Addressables or broad pooling architecture unless explicitly requested outside Phase 7.
- [ ] Keep gameplay entity spawning separate from visual object playback.
- [ ] Validate no per-frame instantiate/destroy loops are added.

Acceptance:

- Visual pooling remains behavior-preserving.
- Runtime allocation is not made worse.

## F8 - Retire Or Fold Visual Helpers

Goal:
Remove dead visual wrappers only when safe.

- [ ] Identify disabled, unused, or wrapper visual `SystemBase` types.
- [ ] Search code, serialized references, prefab references, and reflection before retiring.
- [ ] Do not delete referenced scripts without Agent A-approved serialized-reference migration.
- [ ] Fold pure data helper logic into static functions or a narrow `ISystem` only when ownership is obvious.
- [ ] Keep asset-linked presentation scripts when removing them would create missing scripts.
- [ ] Record retired/folded count in the progress snapshot.

Acceptance:

- No missing-script references introduced.
- Removed helpers are proven unused or replaced.

## F9 - Focused Validation Matrix

Goal:
Prove visuals still work and no forbidden ticking was introduced.

- [ ] Always run `git diff --check -- <changed files>`.
- [ ] Run architecture tests for no forbidden Unity blockers in converted `ISystem` files.
- [ ] Run an architecture check for no newly introduced MonoBehaviour `Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.
- [ ] Run visual quality validation if quality/camera rows changed.
- [ ] Run smoke validation for selection marker, command marker, building placement marker, road preview, projectile/missile, explosion, and attached light when touched.
- [ ] Check Unity logs for missing references, null renderer/material/light/particle errors, and destroyed system state errors.
- [ ] Capture before/after screenshots or short videos for any visual row that changes presentation code when practical.
- [ ] If Unity is locked, retry once, then use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` shadow validation when available.

Suggested commands:

```bash
git diff --check -- <changed files>
```

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture \
  -runTests -testPlatform EditMode \
  -logFile /private/tmp/warline-phase7-agent-f-editmode.log
```

Acceptance:

- Visual behavior touched by Agent F is validated or explicitly marked as needing manual visual QA.
- No new MonoBehaviour ticking is introduced.
- Managed presentation exceptions are counted.

## F10 - Handoff To Agent A

Goal:
Make visual integration safe and honest.

- [ ] Create a dated handoff report under `Design/AgentReports/`.
- [ ] Include inventory row ids, files changed, and final disposition.
- [ ] Include visual split map: ECS request/result systems, managed presentation exceptions, and retired helpers.
- [ ] Include converted-to-`ISystem`, split passive/managed-boundary, managed-presentation-exception, and retired/folded counts.
- [ ] Include visual validation notes, screenshots/video paths if captured, and log paths.
- [ ] Include any rows returned to Agent A for reclassification.
- [ ] Include any coordination notes for Agents C/D/E.
- [ ] Confirm this tracker progress snapshot is current.

Handoff template:

```markdown
# Phase 7 Agent F Handoff - YYYY-MM-DD

Branch:
`codex/phase7-agent-f-rendering-vfx`

Rows completed:
- `P7-####` - `TypeName` - `Converted/Split/ManagedException/Retired`

Visual split:
- Request/result data:
- Managed presentation exceptions:

Counts:
- Converted to ISystem:
- Split passive/managed boundaries:
- Managed presentation SystemBase exceptions:
- Retired/folded:

Validation:
- `git diff --check`: passed/failed
- Unity validation: passed/failed/not run, log path
- Visual QA: screenshot/video/manual/not run

Risks:
- ...
```

Completion criteria:

- Every Agent F row has final status.
- No converted `ISystem` owns Unity rendering, camera, particle, light, material, or GameObject APIs.
- Authored VFX quality is preserved unless the user approved an art change.
- No MonoBehaviour ticking introduced.
- Managed presentation exceptions are explicit and countable.
