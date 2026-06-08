Gate:
M01 Critical Path Gate 1 - Gameplay Stability And Direction

Status: accepted

Reason:
The gameplay report closes the assigned M01 stability/direction task at the intended scope. The old `RuntimeCitySpawner` hitch is fixed, M01 roads are now validated against authored `TacticalMapDefinition` road surfaces, random/procedural city-road mutation is guarded out of fixed tactical M01, direct road/build entry points respect `MissionDoesNotAllowBuild`, day/night runtime visual mutation is disabled for active fixed tactical M01, and the legacy `Model` / `Destroyed` render migration is documented with tests and a concrete first-slice plan.

Validation accepted:
- `Chapter01LegacyRuntimeGuardrailTests`: 3/3 passed in `/private/tmp/warlinecapture-m01-legacy-guardrails-results.xml`.
- `Chapter01TacticalRuntimeBindingTests`: 6/6 passed in `/private/tmp/warlinecapture-chapter01-runtime-binding-results.xml`.
- `Chapter01M01PlayableRuntimeTests`: 8/8 passed in `/private/tmp/warlinecapture-m01-playable-results.xml`.
- `Chapter01M01PlayModeValidationTests`: 3/3 passed in `/private/tmp/warlinecapture-m01-log-cleanup-playmode-results.xml`.
- PlayMode log comparison supports the report claim that the prior `RuntimeCitySpawner=1350.3ms` hitch and broad `InitialBase` spawn are gone.
- Guardrail audit exists at `Design/M01_Legacy_Runtime_Guardrails.md`.

Validation still needed:
- Player/device or non-headless confirmation for the Unity Entities Graphics/resource-GC `NullReferenceException` before treating it as fully harmless.
- Visual validation after the sprite-atlas presenter lands; current headless URP render-target errors mean this pass is not visual-readiness evidence.
- Cleanup/classification for remaining generic AI plan noise if it appears in user-facing M01 logs.

Cross-lane notices:
- QA/HCI can downgrade the old city-spawn performance hitch and proceed once UI/Support gates are ready.
- UI and Support/FTUE can keep using `MissionDoesNotAllowBuild`; road/build entry points now obey the same contract.
- UI should treat day/night controls as legacy/future for current M01.
- Art/gameplay should treat the next production visual task as the M01 sprite-atlas presenter slice, not more legacy prefab polish.

Tracking updates:
No task-board files were edited in this heartbeat review. The PM should mark Gate 1 accepted in `Design/AgentTasks/M01_CRITICAL_PATH.md` during the next explicit task-board update.

Next gate/task:
Gameplay next recommended task is the first M01 animated sprite-atlas presenter slice for `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point`, including fixed-direction baked/contact shadows and close tactical camera validation.
