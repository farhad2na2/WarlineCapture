# POP-05 Mission Result High-End Layer Prompt

Use case: ui-mockup
Asset type: AAA mobile RTS Mission Result popup target plus separated implementation layer atlas.

Primary request:
Create a high-end WarlineCapture Mission Result popup landscape target at 1672 x 941, matching the existing premium dark graphite/cyan/gold military HUD style, with a cinematic 2D isometric tactical background and a large polished result modal.

Reference:
Use `reference/POP-05_MissionResult_Landscape_Target.png` for quality, composition, and the current visual-lock content contract.

Canonical content:
- Outcome: VICTORY
- Mission: Downtown Breakthrough
- Duration: 08:42
- Difficulty: Hard
- Stars: 3
- Stats: Enemies Defeated 42; Units Lost 3; Buildings Captured 2; Civilians Safe 18
- Rewards: CommanderXP 2,450; Credits 12,800; Supply Crate 1; Unlock Fragments 25
- Consequences: keep as a separate optional runtime row layer, but do not show it in the current target composition.
- Objectives: Capture the Downtown District completed; Destroy Enemy Command Center completed; Rescue All Civilians completed
- Buttons: Replay, Continue

Layer atlas request:
Create a separate clean layer atlas on flat chroma key. The atlas must separate:
- background tactical art
- modal frame and modal fill
- victory emblem
- star filled icon and star empty icon
- stat card frame, reward card frame, objective row frame, consequence row frame
- replay button background and continue button background
- stat icons: target/enemies, shield/units lost, flag/buildings, civilians
- reward icons: CommanderXP, Credits, SupplyCrate, UnlockFragments
- objective complete icon

Layer rules:
- No reusable layer may contain text.
- Reward cards must not include icons or values.
- Button backgrounds must not include labels.
- Consequence row frame must be a separate 9-slice sprite.
