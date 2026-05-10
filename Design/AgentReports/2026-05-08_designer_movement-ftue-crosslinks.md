# Designer Movement FTUE Crosslinks

Lane: Designer

Task: Cross-link the large-scale grid movement design into FTUE, M01, and Chapter 1 tactical documentation without changing early-game scope.

Files changed:

- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md`

Contracts touched:

- Added `Design/WarlineCapture_LargeScale_Grid_Movement_Design.md` as a referenced design input for FTUE, M01, and Chapter 1 tactical production.
- Added explicit notes that the movement design clarifies acceptance/readability requirements and does not add extra M01 or early FTUE steps.

User-visible behavior:

- None in runtime.
- Early-game docs now make the movement-readability gate explicit for M01 and Chapter 1.

Validation run:

- `rg -n "WarlineCapture_LargeScale_Grid_Movement_Design|Movement scope note|Movement teaching note|large-scale grid movement" Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md Design/WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md`
- `git diff -- Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md Design/WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md`
- `git status --short Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md Design/WarlineCapture_M01_FirstContact_Production_Contract.md Design/WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md`

Validation result:

- Passed. All three docs reference the movement design and preserve existing early gameplay scope.

Known gaps:

- Runtime/UI validation still needs a focused M01 movement-readability review.

Cross-lane impacts:

- FTUE/Support should treat movement readability as a completion criterion for existing M01 steps.
- Gameplay/UI should ensure M01 selected state, move marker, attack marker, invalid target feedback, HUD current-order state, camera bounds, and result flow are visible in captures.
- QA/HCI can use the linked movement design during M01 readability review.

Next recommended task:

- Run or route a focused M01 readability audit against 16:9 and 20:9 captures using the movement design gate.
