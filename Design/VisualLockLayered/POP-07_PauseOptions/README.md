# POP-07 Pause Options V3

Status: V3 match pause popup reference saved. The final V3 alias preserves the selected pause overlay for implementation planning.
Date: 2026-08-29

## Saved V3 Reference

- Pause options final: `reference/POP-07_PauseOptionsV3_Final_Target.png`
- Pause options active target: `reference/POP-07_PauseOptionsV3_Target.png`
- Pause options iteration mirror: `reference/POP-07_PauseOptionsV3_Target_v01.png`
- Source generation: `/Users/farhad/.codex/generated_images/019e0cb1-e941-7eb0-b318-63b09c645a05/call_nHWGjnuyIAXgVr2ILcrVOJDR.png`
- Existing prefab source: `Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab`

## Direction

- Use the current Match HUD V3 as the paused background context.
- Keep the background dimmed and inactive, with the paused battlefield and HUD still readable behind the modal.
- Use sharp rectangular V3 panels, solid color fills, large mobile buttons, strong drop shadows, and bright cyan, green, amber, blue, and red action colors.
- Avoid the older beveled/gold pause popup frame language.
- Keep the real pause action list: `RESUME`, `RESTART MISSION`, `OPTIONS`, `HELP`, and `EXIT TO MAIN MENU`.
- Keep the small close `X`, mission line `Downtown Breakthrough`, and time row `Current Time 14:32`.
- Keep match resources behind the popup as `Materials`, `Oil`, `Fuel`, and `Civilian Risk`; do not show menu resources such as Credits or Command.
- Keep all labels, resource values, mission status, autosave status, and buttons live at implementation time.
- Do not use water, sea, rivers, canals, docks, coast, naval imagery, diamonds, gems, or unrelated currencies.
