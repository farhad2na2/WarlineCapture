# M2 ARIA: one Do It per visible action

M2's rifle lesson previously treated a successful tab or item click as an unfinished action. Its readiness retry then selected the remaining controls automatically, allowing one Do It to open Build, choose Soldiers, select a rifle, and submit production.

The guidance executor now distinguishes a handled control click from accepted production. Opening Build consumes the action even when the drawer opens synchronously. Selecting Soldiers clears automatic item selection; selecting the rifle consumes another action. Only the fourth action invokes Produce. Only accepted production acknowledges the rifle lesson and closes the drawer. A rejected production click is not retried automatically. Barracks guidance also treats reopening its drawer and selecting its item as separate clicks.

The existing Build popup, mobile controls, English/Farsi lesson text, and authoritative production transaction remain in use. During M2 guided building, the drawer now reserves room for the existing ARIA rail. ARIA stays visible and receives pointer input above the modal backdrop. Other HUD sections stay hidden, and their original visibility and the rail canvas settings are restored when the drawer closes. No prefab or art changes are required.

Pointer QA also exposed a canvas ownership bug: `MenuBootstrapView.ApplyRuntimeUiMode` forced screen-space canvas transforms to unit scale every frame, overriding CanvasScaler and shifting hit targets until Unity refreshed rendering. Screen-space scale is now left to Unity; the existing world-space normalization is preserved. Overlay and camera canvas regression tests cover this.

## Validation

- Unity Editor focused tests: 119/119 passed, including synchronous and deferred drawer creation, four individual control counts after 120 presentation ticks between each action, rejected production, existing M2 placement/guidance and result settlement, Build drawer catalog and HUD restoration, canvas scale ownership, and production source architecture guardrails.
- Test output: `/private/tmp/warline-m2-single-click-final-tests.xml`.
- Live Editor M2: **Passed**, wrapper exit 0. Four explicit Do It actions produced button counts `1,0,0,0` → `1,1,0,0` → `1,1,1,0` → `1,1,1,1`, with three-second pauses and no automatic follow-up clicks. Each Do It passed an actual UI pointer raycast. The required rifle was then produced and delivered.
- Live log: `/private/tmp/warline-m2-single-click-verified.log`.
- Visually reviewed the Soldiers tab and selected rifle with the original ARIA rail beside the drawer. No overlapping controls. Initial failed probes are retained in `/private/tmp/warline-m2-single-click-*.log`; they exposed the on-demand drawer setup, HUD occlusion, and the canvas scale override before the passing run. The final probe runs after the rendered game frame.
- Evidence images stay in `/private/tmp/warline-m02-placement`, outside Design and Git.
