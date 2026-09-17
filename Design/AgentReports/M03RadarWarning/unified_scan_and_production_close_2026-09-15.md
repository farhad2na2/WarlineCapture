# M3 Scan and production popup behavior

M3 now uses the existing Scan command in its normal position and with its normal icon/name. The Support slot no longer becomes a second radar command or displays radar charges/recharge. Its normal mission restrictions apply.

In M3, Scan requests the living radar vehicle's coverage sweep. The existing validated sensor transaction still owns its two uses and cooldown. Accepted scans publish through the existing scan-result presentation, including the world scan marker and localized contact count. Invalid attempts display the existing reason and do not spend a use. Scanning does not replace automatic convoy warnings or become a victory requirement; the optional lesson can be skipped while the defense remains on Hold.

Central English/Farsi instructions now refer to Scan. The lesson explains the coverage, optional use and cooldown; the command button keeps its stable name. Existing recorded narration describes radar coverage and does not need a new button to match it.

The build drawer now closes after every accepted unit-production request, including ordinary player clicks. Previously only guidance-triggered production closed it. Failed requests return before the close callback, so their error remains visible. The production queue continues after the drawer closes.

Validation:

- English Editor journey: actual build menu interactions and soldier purchase, skipped optional Scan, defensive Hold victory, three stars, debrief and localized Victory. Wrapper exit 0 (`/private/tmp/warline-m3-production-close-en-v2.log`).
- Farsi Scan: actual pointer click on the existing Scan control, one consumed use, cooldown, unchanged defensive command, next lesson, and localized contact feedback. Visual inspection found an existing feedback binding replacing formatted values with `{0}`; the binding now preserves the formatted runtime value. The corrected focused run passed its assertions but Unity crashed during shutdown (exit 139); the subsequent combined Farsi journey revalidated this with a clean exit.
- Sensor transaction (including one shared Scan result and no duplicate marker), duplicate/cooldown/sensor-death behavior, ten command-audio tests, nine production tests, and all 139 architecture tests passed. Combined regression wrapper exit 0 (`/private/tmp/warline-m3-scan-production-regressions.log`). Combined Farsi journey passed and exited 0 (`/private/tmp/warline-m3-scan-production-fa-v2.log`): actual Scan click and localized count, one consumed use, automatic popup closure, four recruited soldiers, defensive Hold victory, three stars, no civilian losses, finale, three debrief panels and localized Victory. The separate extra command is absent and Support retains its normal icon.
- Main Editor compiled without errors and its central localization asset was regenerated from the final English/Farsi source. `git diff --check` passed.

The production close check waits for the popup's existing hide animation, never clicks Close on behalf of the player, and verifies four soldiers in the production read model. The first combined Farsi assertion incorrectly compared the authored prefab name case-sensitively against normalized lowercase source IDs and reported zero. It now compares IDs case-insensitively and records the four live spawned units during gameplay, consistent with the production contract. Screenshots remain outside Design.
