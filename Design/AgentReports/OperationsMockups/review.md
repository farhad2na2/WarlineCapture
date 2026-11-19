# Operations family HUD mockups v01 (ESCORT O002, REPAIR O003, RESCUE O005)

Generated with Codex built-in ImageGen (codex-cli 0.169.0-alpha, `-m gpt-6.2-sol`) on 2026-11-19 for owner visual-direction review (AGENTS.md "Mission UI").
References: the native Campaign HUD `../CH04M02SteelPush/VisualReview/en-hud-aria-5.png` (authoritative layout and style) and the native O001 Operations HUD
`../Operations/O001_Integration_20260922/manual-victory-1102/009.png` (header style only; its large centre panel is deliberately not reused).
Exact prompts: `o002-escort-hud-v01.prompt.txt`, `o003-repair-hud-v01.prompt.txt`, `o005-rescue-hud-v01.prompt.txt`.

Inspected:
- Native left unit card, minimap, category cards, colourful command strip, resource header and right ARIA panel are kept.
- Field guide, Show Me and Stop ARIA are visible on all three.
- One compact top-centre objective card holds the purpose, progress, timer and the family control: Go / Hold Convoy + route chip; Repair; Release Group / Board APC.
- The battlefield centre stays clear.

Known deviations, for the native implementation to correct:
- Show Me is green on O002 but blue on O003/O005; the native button style wins.
- O003's Materials "80/80" stands for the authored repair budget.
- Generated buildings, units, portraits and text are illustrative; native assets and copy are authoritative.

Status: owner approved all three v01 boards for implementation on 2026-11-19 ("Approve all three"). Visual direction only, not implementation evidence. Native-screen review, normal-input ARIA journeys and device acceptance are tracked separately.
