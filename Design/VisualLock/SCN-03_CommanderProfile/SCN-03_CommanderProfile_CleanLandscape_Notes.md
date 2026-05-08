# SCN-03 Commander Profile Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-03_CommanderProfile/SCN-03_CommanderProfile_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted Main Menu, Saga, and Settings visual-lock style.
- Source reference: `Design/WarlineCapture_UIUX_Codex_Package/warlinecapture_uiux_spec_assets/SCN-03_commander_profile.jpg`.
- Do not use this PNG as a baked Unity UI background. Recreate it with separate profile portrait, tabs, stat tiles, reward track nodes, icon buttons, and 9-sliced panels.

## Implementation Notes

- Keep portrait, name, level badge, XP bar, tabs, stats, and reward track as separate interactive/data-bound elements.
- Use the filled blue selected-tab style established by Settings.
- Reward nodes should support claimed/current/locked states.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Commander Profile screen, matching the premium military strategy HUD style of the accepted WarlineCapture Main Menu, Saga Campaign, and Settings targets visible in this conversation. This is a new optimized landscape target, not an exact crop of the source reference.

Scene/backdrop: Dark tactical profile interface, graphite metal HUD frame, subtle blue grid texture, soft cyan accents, no busy full-screen illustration.

UI layout:
- Full-screen futuristic military HUD frame with dark graphite/black panels, cyan edge highlights, subtle bevels, and smooth shadows.
- Top header bar: left back button with polished military arrow icon, title "COMMANDER PROFILE", right gear/settings icon button.
- Left profile panel: commander portrait art slot in a beveled frame, name "Commander_7X", level badge "LV. 32", alliance line "Iron Guard / Member", yellow XP bar with "18,450 / 25,000".
- Tab row: "OVERVIEW" selected in filled blue, "UPGRADES", "STATS", "BADGES" inactive.
- Center/right stats area: four stat tiles with icons and labels: "Power Rating" "58,720", "Victories" "146", "Units Unlocked" "38", "Zones Controlled" "12".
- Bottom reward track panel: title "REWARD TRACK / SEASON 7", horizontal progression nodes with crates, badges, lock icons, and a highlighted current reward.

Style requirements:
- Match the accepted visual targets: dark beveled panels, cyan highlights, restrained blue selected states, yellow progress/reward accents, crisp readable typography, AAA military mobile polish.
- Keep portrait, tabs, stat tiles, reward nodes, and gear/back icons as separate replaceable UI parts in the visual language.
- Do not invent bright white borders. Use thin dark metal/cyan-gray bevels.
- No stretched UI, no baked single background panel pretending to be interactive UI, no watermark, no spec captions.
- Text must be legible and exactly as specified.
```
