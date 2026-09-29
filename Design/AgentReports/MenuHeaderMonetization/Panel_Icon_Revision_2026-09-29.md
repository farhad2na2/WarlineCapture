# Panel and icon revision

## Completed native corrections

Credits artwork is contained within its header panel. Store uses a gold collection emblem; Armory uses a shield and crossed weapons. Builder and generated native prefab agree. Routes retain their existing targets.

Checked macOS wrapper run: `Logs/menu-header-icons-01.log`, exit 0. Builder, localization, original ARIA import, focused remediation, four normal captures and four long-name/maximum-Credits captures all report Passed. Compilation reported no errors. Source/document whitespace check passed.

Native visual inspection covered English wide, Persian wide, English 32-character name and maximum balance at 16:9, and Persian 32-character name and maximum balance at 16:9. Icons and balances remain contained. No new gameplay or physical-device acceptance was performed for these presentation corrections. Source/prefab/capture hashes: `candidate-icons-01.json`.

## Implemented full-panel artwork

[ARIA plus six commander scene review](Mockups/home-full-panels-v02.md) was approved by the user: “looks good implement that.” Seven separate shipping images were then generated from the canonical references. ARIA retains the exact original face asset over a full command-room background. Six commander scenes bind to the saved canonical selection, with native labels, shading, frames and controls.

Checked wrapper run `Logs/menu-header-panels-02.log` passed the native builder, localization, focused suite, eight normal/stress captures and six saved-identity captures. All six native commander captures were visually inspected: faces remain clear, art fills the panels and identity/CTA labels are readable. `Logs/menu-header-panels-01.log.dispatch.json` retains the failed initial Pipeline dispatch.

The completed-Campaign package was integrated subsequently. See [completion integration evidence](Campaign_Completion_Integration_2026-09-29.md) for its separate state, visual and input evidence.

Previous captures are retained in `After/before-panel-revision/`. Broader pending acceptance gates in `Final_Handoff.md` remain open.
