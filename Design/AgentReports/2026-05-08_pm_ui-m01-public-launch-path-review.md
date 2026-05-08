Status: needs fixes
Topic:
UI public M01 launch path handoff review

Reviewed handoff:
`Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`

Files reviewed:
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureGameLaunchUtility.cs`
- `Assets/Tests/Editor/WarlineCaptureUiQuickCustomTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

Finding:
The UI handoff materially improves the public launch blocker and provides useful validation for the Quick Custom path. `WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter` now branches for `saga.ch01.m01.first_contact`, keeps the WarlineCapture router/app canvas active, routes to `WarlineCaptureRoute.Match`, keeps `UI_Canvas` inactive, and preserves non-M01 legacy behavior. The reported focused validation is green, including a PlayMode Quick Custom public launch smoke that reaches Match with M01 map/runtime sprite-presenter evidence.

Accepted portion:
- Quick Custom public launch smoke is accepted as focused evidence for the direct/test path.
- Non-M01 legacy/sandbox behavior is preserved by mission-id branch.
- The handoff gives QA/HCI enough evidence to rerun the public Quick Custom smoke.

Remaining fixes / blockers:
- The campaign path the user also tested manually is not yet covered by end-to-end public launch smoke. The handoff reports campaign/loadout button wiring through EditMode tests, but the required blocker was specifically Main Menu -> Saga Map / campaign map -> First Contact -> Mission Briefing/Loadout -> Launch entering the old 3D prototype.
- No screenshot/player-visible capture was produced; headless PlayMode XML/log evidence is acceptable for automation, but a manual or graphics capture is still needed before asking the user for HCI/balance feedback.
- The touched runtime file still depends on loaded-scene discovery helpers using `Resources.FindObjectsOfTypeAll`. The report correctly notes these helpers pre-existed, but the new M01 production path now depends on them. This needs either a narrow PM-approved exception for this legacy bridge utility or a follow-up refactor to explicit references/services before final architecture acceptance.

Validation reviewed:
- `WarlineCaptureUiQuickCustomTests`: reported 16/16 passed.
- `WarlineCaptureUiSagaCampaignTests`: reported 8/8 passed.
- `PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute`: reported 1/1 passed.
- `Chapter01M01PlayModeValidationTests`: reported 4/4 passed.
- `git diff --check`: reported passed.

Cross-lane impacts:
- QA/HCI should rerun public Quick Custom launch smoke and add a true campaign path smoke or manual campaign verification.
- Gameplay should review whether `GameBootstrap.BeginGameplay()` is the correct production runtime entry for M01 when invoked from the WarlineCapture router path.
- UI should not consider the public launch blocker fully closed until campaign path evidence lands.
- PM should not ask the user to test balance/HCI yet; the next user-facing test should only happen after campaign path smoke confirms it no longer opens old 3D.

Needs user decision:
No immediate decision. PM recommendation remains: campaign/M01 public launch should open the production slice; old 3D must be clearly sandbox/legacy if retained.

Next recommended task:
QA/HCI should rerun the Quick Custom smoke and add end-to-end campaign launch verification. If campaign still opens legacy 3D, route the exact failing button/path back to UI/GamePlay. If campaign passes, PM can tell the user which path is ready to test.
