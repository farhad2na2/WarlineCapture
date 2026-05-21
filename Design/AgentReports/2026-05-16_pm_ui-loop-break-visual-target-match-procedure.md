# PM UI Loop-Break: Visual Target-Match Procedure

Date: 2026-05-16
Lane: PM
Target lane: UI
Status: active

## Reason

The UI lane is looping because the task framing allowed semantically wired implementation, passing tests, and partial capture evidence to be treated as progress while screenshots still did not visually match the approved target-lock mockups.

The issue is not only UI execution. PM instructions were too ambiguous:

- Narrow fixes were accepted without a separate hard visual-complete gate.
- Older task text remained below the current assignment and still contained stale "accepted" and "as closely as implementation allows" language.
- The task asked for implementation before requiring target decomposition and blocker identification.
- Live TMP/data binding was framed strongly enough that UI could preserve old shells instead of replacing them with the target composition.
- Reports were allowed to claim completion from tests/captures even when the capture was visually nonmatching.

## Decision

UI must stop coding and produce a visual target-match plan before any more implementation.

Rejected as visual-complete:

- `Design/AgentReports/2026-05-16_ui_pop05-scn02-approved-target-implementation.md`
- `Design/AgentReports/2026-05-16_ui_pop05-scn02-target-match-fix.md`
- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v6.md` for 100% target-lock match. V6 remains accepted only for narrow SCN-08 fixes already recorded.

## Required Next UI Report

`Design/AgentReports/2026-05-16_ui_visual-target-match-plan-pop05-scn02-scn08.md`

Surfaces:

- `SCN-02_MainMenu`
- `POP-05_MissionResult`
- `SCN-08_RTSBattleHUD` / M01 Match HUD

Required contents per surface:

- Approved target image path.
- Latest implementation capture path.
- Region-by-region mismatch table covering layout, scale, position, color, chrome depth, typography, icon/sprite quality, background, button treatment, and density.
- Exact prefab/root objects that must move, resize, be rebuilt, or be deleted.
- Exact `Design/VisualLockLayered/...` layer sprites/assets to use.
- Old shell/prefab pieces that must be replaced.
- `can reach 100% visual target match with current assets: yes/no`.
- Missing assets/data and blocker owner if the answer is no.
- Implementation sequence and screenshot proof plan if the answer is yes.

## Acceptance Rule

UI completion cannot be based on passing tests, semantic data binding, or "compositionally aligned" screenshots.

A surface is complete only when a fresh screenshot visually matches the approved target mockup region by region, with a direct target comparison included in the report. Tests are secondary proof only.

## Routing

UI owns the planning report first. PM/user must review that plan before UI resumes implementation.

If the plan shows missing Art/Atlas layers or Gameplay-owned scene/camera blockers, route those blockers before more UI iteration.
