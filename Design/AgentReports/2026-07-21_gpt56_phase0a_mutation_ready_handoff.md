# GPT 5.6 Handoff: Phase 0A Ready For Candidate SubScene Mutation

Date: 2026-07-21
From: Cursor Grok Phase 0A continuation
To: GPT 5.6
Tracker: `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`

## Status

Phase 0 is complete. Phase 0A non-mutating inventory/classification evidence is green. The next permanent ownership work should be driven by GPT 5.6.

## Completed Grok-safe evidence

| Item | Result | Report |
|---|---|---|
| Android Phase 0 baseline | CharacterizationCaptured | `2026-07-21_dense_city_phase0_android_baseline.md` |
| Vehicle ECS already-produced | 22/22 ready | `2026-07-21_dense_city_phase0a_vehicle_ecs_conversion_inventory.md` |
| RuntimeBuildingEntity deps | 432 require managed interim | `2026-07-21_dense_city_phase0a_runtime_building_entity_dependency_inventory.md` |
| Building attachment ownership | 1324 intact + 272 destroyed; 0 conflicts | `2026-07-21_dense_city_phase0a_building_attachment_ownership_inventory.md` |
| Six-role owner classification | 432 / 22 / 9090 / catalogs; 0 rejected | `2026-07-21_dense_city_phase0a_owner_classification.md` |

Prior inventory/dry-run hashes remain valid from `2026-07-21_gpt56_phase0a_inventory_review_and_grok_handoff.md`.

## GPT 5.6 start here

1. First candidate SubScene hierarchy creation (copy, do not mutate accepted sources).
2. Building ECS gameplay conversion replacing `RuntimeBuildingEntity` managed presentation.
3. Attached-visual bake under intact/destroyed entity roots using the attachment inventory identities.
4. Vehicle duplicate authored-visual cleanup only where still needed after hide-on-spawn.
5. Presentation-kind flip to `EntityScene` only after Editor + Android candidate acceptance.

## Hard stops

- Do not mutate accepted source scene / static package / Addressables ownership until candidate bake + parity pass.
- Do not delete/retire the static rollback package in the same change as cutover.
- Do not classify by names/proximity.
- Keep GameplayBuilding managed-boundary interim explicit until ECS building conversion lands.

## Tracker progress at handoff

**17 / 144 (12%)** — Phase 0 complete; Phase 0A `CandidateTransactionReadyPendingMutation`; mutation gates open for GPT 5.6.

See also: `Design/AgentReports/2026-07-21_dense_city_phase0a_mutation_readiness_gate.md`.
