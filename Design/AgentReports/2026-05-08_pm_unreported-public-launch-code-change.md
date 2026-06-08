Status: blocked
Topic:
Unreported public M01 launch-path code change

Source:
PM heartbeat review of workspace state after routing the public M01 launch blocker to Gameplay/UI.

Finding:
`Assets/Game/Scripts/UI/Shell/UIGameLaunchUtility.cs` has an uncommitted code change that appears to route `saga.ch01.m01.first_contact` through a new `StartM01ProductionRoute` path. No matching Gameplay or UI completion report has landed yet:
- Missing `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`
- Missing `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`

Why it matters:
The code change may be the start of the correct fix, but PM cannot accept it without the required public launch smoke evidence. The user is specifically blocked because manual launch still showed the old 3D prototype, so the acceptance gate must prove the first visible gameplay state is the current M01 production slice.

Additional risk:
The touched utility still depends on loaded-scene object/component discovery helpers. The active lane tasks prohibit adding new runtime scene-search behavior. The owning lane must either prove this is pre-existing/fallback-only and no new banned lookup behavior was introduced, or replace it with explicit references/services before acceptance.

Required handoff evidence:
- Entry path tested: Main Menu -> Saga Map -> Mission Briefing/Loadout -> Launch, plus Quick Custom if changed.
- Expected mission id: `saga.ch01.m01.first_contact`.
- Actual first visible gameplay state.
- Explicit confirmation that legacy `UI_Canvas`, old 3D gameplay, wrong scene, or wrong mission does not appear for the production path.
- Screenshot/capture path when practical.
- Focused Unity validation result and any log-health notes.
- Statement about scene-search/banned lookup compliance in touched files.

Affected lanes:
- Gameplay
- UI
- QA/HCI
- PM

Needs user decision:
No. The owner lane should finish the handoff and validation. User testing should wait until PM/QA accepts the public launch smoke.

Next task update needed:
No. `Design/AgentTasks/gameplay_current.md` and `Design/AgentTasks/ui_current.md` already route this blocker correctly.
