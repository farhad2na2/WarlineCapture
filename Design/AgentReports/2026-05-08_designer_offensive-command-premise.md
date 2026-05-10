# Designer Offensive Command Premise Alignment

Lane: Designer

Task: Align the WarlineCapture premise around a proactive field commander preparing and executing targeted operations against hostile factions embedded in civilian districts, while keeping existing gameplay and UI visual targets unchanged.

Files changed:

- `README.md`
- `Design/README.md`
- `Design/WarlineCapture_Command_Offensive_Premise_Alignment.md`
- `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`

Contracts touched:

- Added a new premise alignment doc.
- Updated the root README product direction language.
- Added the premise doc to the design index and reading order.
- Updated the north-star fantasy and design pillars from passive stabilization toward proactive command operations.
- Updated FTUE premise/story beats to frame M01-M05 as targeted operations, without changing the step sequence.

User-visible behavior:

- None in runtime.
- Documentation now frames the player as a field commander preparing and executing operations against hostile factions hidden in civilian districts.

Validation run:

- `rg -n "Command_Offensive|hostile factions|embedded|stabiliz|keeping the city alive|keep the city" README.md Design/README.md Design/WarlineCapture_Command_Offensive_Premise_Alignment.md Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`
- `git diff -- README.md Design/README.md Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md Design/WarlineCapture_LargeScale_Grid_Movement_Design.md Design/WarlineCapture_Command_Offensive_Premise_Alignment.md`
- `git status --short README.md Design/README.md Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md Design/WarlineCapture_LargeScale_Grid_Movement_Design.md Design/WarlineCapture_Command_Offensive_Premise_Alignment.md`

Validation result:

- Passed. The new premise appears in README, design index, north-star, FTUE, and movement design docs.
- The remaining stabilization language is either historical/comparison language in the new premise doc, a cosmetic id, or route-level language rather than the main player fantasy.

Known gaps:

- The authored AAA mobile GDD still contains older stabilization-heavy wording and should be updated in a later doc pass if this premise is accepted.
- Saga chapter docs may need light narrative copy edits later so mission briefings consistently speak in terms of targeted operations and hostile cells.

Cross-lane impacts:

- No gameplay or UI visual target changes are required.
- UI and gameplay should keep current M01 scope.
- Future narrative, briefing, ARIA, Operation, and marketing copy should prefer hostile faction/cell/hidden network terminology over broad real-world loaded labels.

Next recommended task:

- Run a focused copy audit of Saga Chapter 1 mission briefings and Operation action labels to align wording with the offensive-command premise while preserving all mechanics and UI targets.
