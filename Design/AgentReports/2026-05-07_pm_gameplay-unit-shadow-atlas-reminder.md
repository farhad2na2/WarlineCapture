# PM Dispatch: Gameplay Unit Sprite Shadow Requirement

Date: 2026-05-07

## Trigger

The user flagged a prior instruction to the gameplay programmer: unit sprite atlases must bake unit shadows in the same direction as the tactical map shadows.

## Task Update

`Design/AgentTasks/gameplay_current.md` now includes this as an explicit sprite-atlas migration requirement.

## Required Direction

When gameplay audits or migrates unit/building prefabs from legacy 3D `Model` / separate `Destroyed` child rendering to sprite-atlas rendering:

- Unit sprite atlases must include baked/contact shadows or an equivalent explicit shadow layer.
- Shadow direction must match the tactical map's fixed lighting direction.
- Ground contact scale must make units feel planted on the 2D isometric map.
- Shadow handling must stay consistent across idle, move, attack, damaged, and destroyed frames.
- Runtime shadows must not conflict with baked map shadows.

## Cross-Lane State

- Gameplay owns the runtime atlas migration plan and validation hooks.
- UI should not solve unit-grounding or sprite shadow direction.
- Support/FTUE should keep asset checklist wording aligned with fixed-direction baked/contact shadows.
- QA/HCI should flag floating units, mismatched shadow direction, or inconsistent shadow treatment across animation states as visual-readability findings.
