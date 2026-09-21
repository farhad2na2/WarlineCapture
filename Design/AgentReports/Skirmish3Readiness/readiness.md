# Skirmish 3 — live player-view correction (2026-09-21)

The original opening was reproduced visually in the running Unity Editor. Its player deployment at (560,550) sat in mountain scenery. The old placement audit checked roads, sidewalks, and water, but not foundation elevation or slope.

Both deployments now use level sites on the northern/eastern industrial approach. Player spawn is (800,600); enemy spawn is (930,470). Playable bounds are (740,420)–(990,660). Camera ground-frustum bounds have 120 cells of padding so both HUD base-focus actions can center their targets. Industrial Basin starts at camera height 60 instead of 40. Other scenarios retain their camera framing.

The builder regenerates the deployment, camera metadata, and updated English/Farsi catalog descriptions. The layout audit checks every foundation cell for slope, elevation, and height variation, in addition to its existing road/water checks.

Live testing exposed a UnitGridMoveJob out-of-range final-waypoint read. Movement now validates the complete shared-pool range before reading it and repaths to the original destination on invalid ranges. Four regression cases cover truncated ranges, negative/overflowing starts, and segmented goals.

Validation:
- Scenario configuration checks: 27 passed (validation.txt).
- Movement/displacement checks: 7 passed (validation.txt).
- All 10 buildings at authored positions with level foundations and no road/sidewalk/water overlap (placement.txt).
- Four-soldier squad reached (840,580) through the displayed squad/Move controls; observed positions were (839.52,578.85), (836.65,581.65), (839.63,581.63), and (842.62,581.62).
- Both bases inspected visually through the HUD focus buttons. The enemy base was previously clamped off-screen; camera padding corrected it.
- Enemy forces traversed the battlefield and destroyed the undefended player base. The normal defeat screen displayed, at 133.18 simulated seconds, with 9 player losses, 3 enemy losses, and ResultSaved=1. This is a completion-flow check, not a victory or balance certification.

Final post-fix interactive checks:
- Visually inspected the player view at 1920x1080 and 2400x1080; restored 1920x1080 afterward.
- Used the result screen's Replay button and successfully loaded the same scenario again.
- Opened Build, selected Guard Tower, inspected its green valid preview, and confirmed Place Building. Simulation was paused while inspecting the preview and resumed after confirmation. The constructed player tower was present at origin (839,588); see construction.txt.
- Subsequent live console inspection found no new gameplay exceptions after the clean restart. The retained metadata-publication error at 15:35:17 UTC belongs to the earlier Play Mode session interrupted by script recompilation; the clean restart and Replay completed afterward.

This is focused Editor playtesting, not an Android device/performance certification or exhaustive difficulty/balance pass. The tested loss was an intentionally undefended inspection run, not a claim that the scenario cannot be won. No scripted victory, health edits, or resource grants were used.


Final handoff: scenario index 3 restarted through its normal return flow and paused in the Editor at 3.00 simulated seconds, Phase=Playing, StartupFailure=None. The final opening was visually inspected again at 1920x1080. Resume with the Editor Pause button.
