Status: needs fixes
Topic:
Gate 4 safe-area validation profiles are undefined
Docs reviewed:
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_UIUX_Implementation_High_Level_Spec.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
Finding:
The active UI task requires route-driven capture/safe-area tooling and says UI may use simulated safe-area/cutout assumptions if true device capture is unavailable. QA/HCI then waits for explicit safe-area/device assumptions. However, no active task or contract defines the exact simulated device profiles, inset values, notch/cutout shape, or pass/fail clearance threshold QA should use for Gate 4.
Why it matters:
UI could create one simulated safe-area profile while QA expects another, causing another avoidable handoff loop. It also lets a too-easy "safe area was simulated" claim pass without proving HUD, minimap, assistant panel, command controls, and result popup survive a realistic 20:9 landscape cutout.
Recommended fix:
Before or with the UI route-driven capture handoff, define a small Gate 4 safe-area evidence matrix. Minimum recommended profiles:
- `safe.none_16x9`: 1920x1080, zero inset baseline.
- `safe.rounded_20x9`: 2400x1080, conservative left/right/top/bottom inset margins for rounded-corner phones.
- `safe.cutout_left_20x9`: 2400x1080, landscape left camera cutout/notch exclusion zone.
For each profile, require the report to state resolution, inset rectangle(s), cutout rectangle(s), and whether each required surface is fully outside the blocked area with readable padding.
Affected lanes:
- UI
- QA/HCI
Needs user decision:
No for adding a minimum simulated profile matrix. User/device-specific validation can be added later if the user wants a named device.
Next task update needed:
Yes. Add exact simulated safe-area profile requirements to the UI and QA/HCI task files, or require UI to define them in `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md` before QA reruns.
