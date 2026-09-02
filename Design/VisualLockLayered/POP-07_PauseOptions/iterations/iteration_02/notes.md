# POP-07 Pause Options — iteration 02

Target lock:

- `../../reference/POP-07_PauseOptionsV3_Final_Target.png`

Live route proof:

- `pause_options_live_16x9.png`
- `pause_options_live_20x9.png`

Changes accepted in this iteration:

- Rebuilt the pause composition around one centered V3 panel with constant three-pixel borders.
- Restored the target gradient treatment for Resume, Restart Mission, Options, Help, and Exit to Main Menu.
- Restricted decorative UI sprites to shared V3 atlas ownership; old Synty, legacy, and placeholder art is rejected by validation.
- Verified Resume, close, Options, Restart, Help, and Exit shell bindings, including pointer targets on every visible button.
- Verified the live Match route at 1920x1080 and 4800x2160. The panel remains centered and does not stretch; the ARIA rail remains pinned to the top-right safe edge.

Separate HUD follow-up found during the wide capture:

- A red match-HUD status/current-order strip can remain visible behind the modal near the ARIA rail at 20:9. It is not owned by the Pause prefab and is recorded in the screen audit for a Match HUD occlusion/alignment pass.
