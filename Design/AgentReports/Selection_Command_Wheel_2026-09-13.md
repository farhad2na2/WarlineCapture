# Selection command wheel

Request: replace the static helicopter command preview with the selected unit's portrait and move Destroy, Return, Camera, and Board out of the selection panel into four large radial sectors.

## Implementation

- Removed the static BLACK HAWK information card. The center disc reads the live selection portrait, including soldiers, vehicles, aircraft, transports, buildings, and mixed selections.
- Replaced the six legacy radial commands with Destroy, Return, Camera, and Board. The permanent bottom command rail continues to provide movement/combat commands.
- Retired the visible four-button selection grid. The wheel dispatches through the existing selection action bindings and honors their enabled state; it closes before dispatch so targeting and camera mode changes work normally.
- Clearing selection closes the wheel. Changing selection while it is open refreshes the portrait and action availability.
- Moved Commands below the portrait into its own 72-unit-high button with a 10-unit image gap. The portrait itself is no longer a button. Shifted health/status down and preserved the compact transport layout.
- Reused the existing localization keys for all four labels. No new hardcoded translated strings or image assets.
- M4 boarding guidance now highlights Commands, then Board, then the transport destination. Its Do It action follows the same single-click sequence.
- Updated both the focused prefab upgrade and normal HUD builder so regeneration preserves this design.

## Editor validation

`SelectionCommandWheelValidation.Run` builds the prefab through PrefabUtility, tests all six portrait categories, checks exactly-once action dispatch and unavailable-action rejection, verifies deselection closes the wheel, and checks the opener's position and height. It also runs the installed shell wheel test to cover independently mounted HUD sections.

English and Farsi captures cover 1920×1080 and the compact transport/passenger layout at 2400×1080. Screenshots are kept under `/private/tmp/warline-selection-wheel-*.png`, outside Design.

The broader audit also found earlier ARIA/minimap layout lookups and a per-frame layout loop. These now use saved references and the Canvas presentation callback, with the dock/content regression rerun. No architecture baseline was relaxed.

Validation log: `/private/tmp/warline-selection-wheel-acceptance.log`.

Final result: wrapper exit 0; wheel behavior and installed shell checks passed; ARIA/minimap content and docking regression passed; architecture validation passed all 139 checks across nine fixtures. The generated prefab was copied back to the main project after verification.
