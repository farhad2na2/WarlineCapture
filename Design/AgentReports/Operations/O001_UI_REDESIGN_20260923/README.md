# Operation 1 interface redesign

The SCN-11 Operations dashboard was never removed. `OperationsMissionScreenView.Install` disabled every child of the authored dashboard and displayed a plain mission briefing in its place. The mission HUD also placed its action panel across the center of the battlefield.

The revised menu keeps the district map, readiness rail, warnings, theater header, and command bar visible. Street Signals now appears as a compact card inside the existing theater briefing area. In the match, only small Mission Guide and ARIA Play/Stop buttons persist in the upper-right ARIA area. Mission objectives, status, remaining time, Save & Exit, and mission actions open as a right-edge popup. It starts closed, leaving the selected-unit area at upper left and the central battlefield clear. The separate ARIA tutorial portrait is hidden during Operations so that it does not cover the battlefield or duplicate ARIA controls.

The image-generated concepts used for layout direction are [Operations dashboard](dashboard-mission-mockup.png) and [match HUD with guide](hud-guide-popup-mockup.png). They are visual references; the shipped UI is native Unity UI and retains the actual O001 actions and localization.

The restored dashboard now binds credits, command, day, action points, district-wide security/trust/threat/heat/supply averages, and actual incidents to the saved Operations run. A fresh profile shows a new-city state instead of the prefab's fixed Day 12, 24,750 credits, 8,430 command, readiness percentages, and three warnings. The map and secondary command destinations remain the existing SCN-11 presentation; this work does not certify the wider Operations campaign.

Editor evidence on Unity 6000.5.2f1 through `Tools/CI/invoke_unity_macos.sh`:

- `/private/tmp/o001-dashboard-localization.log`: `[ConfiguredUiTables] result=Passed configEntries=405` for English/Farsi UI strings.
- `/private/tmp/o001-dashboard-live-smoke-3.log`: `[OperationsReconLaunchSmokeValidation] result=Passed` for dashboard hierarchy and fresh values, compact closed HUD, and Mission Guide open/close. Captures: `Build/EditorEvidence/O001OperationsDashboard.png`, `O001SharedWorldLaunch.png`, `O001MissionGuidePopup.png`.
- `/private/tmp/o001-dashboard-return-smoke-2.log`: `[OperationsReconLaunchSmokeValidation] result=Passed journey=deploy-withdraw-save-return-redeploy`, including saved Day 1/AP 2 reflected on the returned dashboard.
- `/private/tmp/o001-aria-evidence-wait.log`: `[OperationsReconLaunchSmokeValidation] result=Passed journey=aria-victory-return input=visible-touch seed=1102 ap=2`. ARIA completed three scans, waited for the full evidence recovery channel, extracted, saved Victory, and returned to Operations. The earlier failed ARIA runs identified and corrected a recovery retry loop: pressing Recover again reset its 15-second channel.

The seed-1102 Editor journey is playable by manual English/Farsi input (recorded in the integration evidence) and by ARIA visible input. Wider seed/difficulty coverage, unfamiliar-player review, and the rest of the Operations campaign remain outside this evidence.
