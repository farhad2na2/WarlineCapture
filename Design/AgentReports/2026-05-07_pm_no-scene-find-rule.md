# PM Rule - No Runtime Scene Find Lookups

Date: 2026-05-07
Lane: PM
Status: accepted

## Trigger

User reported recurring Unity warnings related to `FindObjectOfType` usage and asked that agents avoid creating these warnings and fix them when they appear.

## Decision

Runtime WarlineCapture gameplay, UI, and Support/FTUE code must not introduce scene searches such as `FindObjectOfType`, `FindObjectsOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, or name/tag-based lookups.

Agents should wire references explicitly through serialized fields, prefab builders, runtime registries, mission/session data, typed providers, or service boundaries. Editor/build scripts may use searches only while constructing or validating generated scenes/prefabs, and the generated runtime components must store explicit references.

## Cross-Lane Notices

- Gameplay: validate no new scene-search warnings or banned runtime lookup calls in touched runtime files before reporting done.
- UI: continue using serialized controller/view references and typed assistant services; do not fall back to child-name or scene searches.
- Support/FTUE: keep assistant logic behind typed context/provider/executor APIs, not scene discovery.
- PM/QA: treat new scene-search warnings in touched production runtime paths as `needs fixes` unless explicitly accepted as a documented blocker.

## Files Changed

- `Design/WarlineCapture_Agent_Coordination_Workflow.md`
- `Design/AgentTasks/README.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-07_pm_no-scene-find-rule.md`

## Validation

Documentation-only PM policy update. No Unity validation required.
