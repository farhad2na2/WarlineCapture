Lane: Gameplay
Task: P1 M01 sprite-atlas presenter first slice
Files changed:
- Assets/Game/Scripts/Components/MissionRuntimeComponents.cs
- Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs
- Assets/Game/Scripts/Campaign/Chapter01M01SpritePresenterCatalog.cs
- Assets/Game/Scripts/Campaign/Chapter01M01SpritePresenterCatalog.cs.meta
- Assets/Game/Scripts/Systems/MissionRuntimeSpritePresenterSystem.cs
- Assets/Game/Scripts/Systems/MissionRuntimeSpritePresenterSystem.cs.meta
- Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs
- Assets/Tests/Editor/Chapter01M01SpritePresenterTests.cs
- Assets/Tests/Editor/Chapter01M01SpritePresenterTests.cs.meta
- Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-presenter.md
Contracts touched:
- M01 now has a runtime sprite presenter component keyed by `MissionRuntimeEntityId`.
- Presenter coverage includes `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point`.
- Presenter sprite ids resolve against `Chapter01TacticalAtlasContract` using the covered runtime entity ids and `vfx.unit.destroyed.small`.
- Presenter state covers idle, move, attack, damaged, and destroyed.
- Destroyed feedback uses `vfx.unit.destroyed.small`; the M01 presenter contract does not require a separate `Destroyed` child GameObject.
- Presenter entries preserve the fixed-direction baked/contact-shadow requirement through component data and the existing manifest guardrail.
- M01 runtime now creates a non-attackable `decor.command_point` entity at the tactical map anchor and binds it to the same presenter path.
User-visible behavior:
- No approved final sprite-atlas renderer is visible yet; this is the runtime contract/wiring slice.
- M01 player squad, hostile patrol, and command-point proxy now carry sprite-presenter data that can drive the production 2D/isometric renderer.
- Existing M01 select, move, attack, objective, and result flows still pass PlayMode validation.
Validation run:
- `Chapter01M01SpritePresenterTests`: `/private/tmp/warlinecapture-m01-sprite-presenter-results.xml`
- `Chapter01M01PlayableRuntimeTests`: `/private/tmp/warlinecapture-m01-playable-results.xml`
- `Chapter01TacticalRuntimeBindingTests`: `/private/tmp/warlinecapture-chapter01-runtime-binding-results.xml`
- `Chapter01LegacyRuntimeGuardrailTests`: `/private/tmp/warlinecapture-m01-legacy-guardrails-results.xml`
- Additional sanity: `Chapter01M01PlayModeValidationTests`: `/private/tmp/warlinecapture-m01-sprite-presenter-playmode-results.xml`
Validation result:
- Passed: `Chapter01M01SpritePresenterTests` 3/3.
- Passed: `Chapter01M01PlayableRuntimeTests` 8/8.
- Passed: `Chapter01TacticalRuntimeBindingTests` 6/6.
- Passed: `Chapter01LegacyRuntimeGuardrailTests` 3/3.
- Passed: `Chapter01M01PlayModeValidationTests` 3/3.
- Presenter validation proves the three covered runtime ids resolve to atlas sprite ids and destruction resolves to `vfx.unit.destroyed.small`.
- Presenter validation proves move, attack, damaged, and destroyed states map to explicit sprite/VFX state data.
- PlayMode rerun initially exposed an ECS dependency exception in `MissionRuntimeSpritePresenterSystem`; fixed by completing the system dependency before reading movement/attack/death component lookups, then rerun passed without that exception.
- Remaining PlayMode log noise matches the prior gate treatment: Unity Entities Graphics/resource-GC `NullReferenceException`, headless URP `RenderTexture.Create failed`, preview-scene leak, and generic AI plan noise.
Known gaps:
- Major visual blocker: final approved M01 unit/building/vehicle sprite atlases and a real runtime `SpriteRenderer`/atlas renderer are not implemented in this slice.
- Major visual blocker: no meaningful close tactical camera visual capture is available from the headless validation path; visual approval still requires an in-editor/device capture after the renderer and final atlas art land.
- Legacy 3D `Model` children are still present in source prefabs and may still be used by non-M01 or fallback visual paths. This slice adds M01 presenter data and suppression tags but does not bulk-delete or globally disable legacy model systems.
- `FinalAtlasArtReady` is intentionally `0` for the first slice because current art is still `exists_needs_review`.
Cross-lane impacts:
- Art/gameplay can now target a concrete ECS presenter contract for final Chapter 1 atlas integration.
- QA/HCI should not treat this as visual readiness; it is readiness for the next visual-renderer/atlas slice.
- UI/Support/FTUE ids remain unchanged: `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `tutorial.move_target.cover_01` remain valid.
- PM should keep the next gameplay/art task scoped to final atlas renderer hookup and close tactical visual validation, not more legacy prefab polish.
Next recommended task:
- Implement the real M01 sprite atlas renderer that consumes `MissionRuntimeSpritePresenter`, uses approved sprite assets with fixed-direction baked/contact shadows, disables visible legacy model rendering for the covered M01 entities, and produces a close tactical camera capture for user validation.
