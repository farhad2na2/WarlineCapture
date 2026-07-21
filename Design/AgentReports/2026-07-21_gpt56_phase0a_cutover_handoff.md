# GPT 5.6 Handoff: Phase 0A ECS Presentation Cutover

Date: 2026-07-21
From: Cursor Grok continuation on dense-city tracker
To: GPT 5.6
Tracker: `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`

Status: Completed by GPT 5.6. Continue from
`Design/AgentReports/2026-07-21_gpt56_phase0a_inventory_review_and_grok_handoff.md`.

## Why stop here

Lower-risk Phase 0 work and Phase 0A inventory scaffolding are ready. The next work permanently reclassifies/migrates accepted map visuals into SubScene ECS ownership. That cutover should be reviewed and driven by GPT 5.6.

## Already completed / ready in this worktree

- Phase 0 editor baseline probe schema v2
- Addressables evidence for the post-clear package; current `main` report is from `8f93dafe0` (shader-bundle dedupe, 119,993,045 bytes)
- Isolated deterministic-city fixture test
- Protected-root confirmation + approval request:
  `Design/AgentReports/2026-07-21_dense_city_phase0_protected_roots_and_approvals.md`
- Read-only inventory probe:
  `Assets/Game/Scripts/Editor/OperationMapEntityPresentationMigrationInventoryProbe.cs`
- Focused inventory tests:
  `Assets/Tests/Editor/OperationMapEntityPresentationMigrationInventoryProbeTests.cs`

## Still required before GPT 5.6 mutates ownership

1. Owner approvals in the protected-roots report:
   - hierarchy/semantic table
   - outside-grid presentation-only default
2. Android Phase 0 current-revision device baseline when device is available
3. Green inventory run evidence:
```bash
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/dense-city-inventory-tests.log -- \
  -nographics -runTests -testPlatform EditMode \
  -testFilter OperationMapEntityPresentationMigrationInventoryProbeTests \
  -testResults /private/tmp/dense-city-inventory-tests.xml

WARLINE_OPERATION_MAP_ENTITY_PRESENTATION_MIGRATION_INVENTORY_REPORT_PATH=/private/tmp/warline-operation-map-entity-presentation-migration-inventory.json \
WARLINE_OPERATION_MAP_ENTITY_PRESENTATION_MIGRATION_INVENTORY_SUMMARY_PATH=/private/tmp/warline-operation-map-entity-presentation-migration-inventory-summary.json \
Tools/CI/invoke_unity_macos.sh --timeout 1800 --log /private/tmp/dense-city-inventory-probe.log -- \
  -nographics -quit -executeMethod Game.Editor.OperationMapEntityPresentationMigrationInventoryProbe.Run
```

## GPT 5.6 scope

Start at Phase 0A after inventory evidence exists:

1. Review `MixedOrAmbiguous` and `UnresolvedPendingReview` rows
2. Join every building/vehicle placement exactly once
3. Produce explicit dispositions for scripts/lights/animation/particles/cross-refs
4. Design the candidate SubScene migration transaction
5. Only then implement `OperationMapEntityPresentationMigrationEditor` and cutover path

## Hard stop rules for GPT 5.6

- Do not regenerate dense city yet
- Do not delete/retire static chunks before accepted parity
- Do not mutate shared third-party prefab assets
- Do not classify by object/prefab/material names
- Keep rollback package byte-stable until acceptance

## Current measured package context

- Static chunks/sources after clear: 269 / 11,892
- Historical pre-clear labels remain in tracker for rollback comparison: 514 / 16,542
- Building placements currently measured by Phase 0 baseline as 432, not the historical 451
- Vehicle placements remain near the historical 29; confirm from inventory summary
