# Designer Large-Scale Grid Movement Design

Lane: Designer

Task: Evaluate the README `large-scale grid-based movement` promise and create an AAA mobile design around it.

Files changed:

- `Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`
- `Design/README.md`
- `README.md`

Contracts touched:

- Added a new design source for large-scale grid movement.
- Updated the design index reading order and core gameplay list.
- Linked the new movement design from the root README.

User-visible behavior:

- None in runtime.
- Documentation now defines when large-scale grid movement is a foundation capability, M01 player-facing feature, Chapter 1 tactical design arc, Operation consequence layer, and Production Scale promise.

Validation run:

- `rg -n "LargeScale_Grid_Movement|large-scale grid|large-scale movement|Large-Scale Grid" README.md Design/README.md Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`
- `test -f Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`
- `git status --short README.md Design/README.md Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`

Validation result:

- Passed. The new design doc exists and is linked from both README and `Design/README.md`.

Known gaps:

- This is a design/documentation pass only. Runtime validation still needs gameplay/UI/QA ownership through M01 and Chapter 1 gates.
- The movement design should later be cross-linked from M01 and Chapter 1 tactical production docs if PM accepts it as canonical.

Cross-lane impacts:

- Gameplay should use the movement design when validating metadata, pathfinding, command outcomes, route pressure, and balance probes.
- UI should use it when validating selected state, command mode, destination markers, rejected command feedback, minimap jumps, and mobile readability.
- QA/HCI should use it as a review checklist for movement readability captures.
- Designer/PM should use it to decide when the README movement promise is accurate at each stage.

Next recommended task:

- Route a focused M01 movement-readability review against the new design gates: selected squad, move marker, attack marker, invalid target feedback, HUD current order, objective/result flow, and 16:9/20:9 readability.
