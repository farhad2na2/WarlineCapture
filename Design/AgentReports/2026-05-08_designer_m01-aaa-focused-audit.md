# Designer M01 AAA Focused Audit

Lane: Designer

Task: Focused M01 audit against the four AAA design points: player fantasy, first 10 minutes, readable scale, and cohesive presentation.

Files changed:

- `Design/AgentReports/2026-05-08_designer_m01-aaa-focused-audit.md`

Contracts touched:

- None. This is a design audit only.

Sources reviewed:

- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/M01_FirstContact_Production_Contract.md`
- `Design/LargeScale_Grid_Movement_Design.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_public-launch-handoff-workspace-review.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md`
- `Design/AgentReports/2026-05-08_qa-hci_gameplay-m01-opening-control-window-validation.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-m01-opening-control-validation-review.md`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_CaptureMatrix_ContactSheet.png`
- `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/M01_SafeAreaProfile_CaptureMatrix_ContactSheet.png`

## Summary

M01 is correctly scoped and directionally aligned with the updated AAA mobile premise. The current design and reports support the intended first mission: one player rifle squad, one hostile patrol, select/move/attack, objective completion, and result popup.

However, M01 should not yet be treated as final AAA-ready from a designer/HCI standpoint. The reports prove important systems and route fixes, but the latest accepted blocker work still needs a fresh independent QA/HCI pass and current visual captures that show the final post-fix first-control experience.

Designer status:

```text
M01 design direction: accepted
M01 gameplay scope: accepted
M01 AAA first-10-minutes readiness: needs fixes / final HCI proof
M01 visual-readability readiness: needs fresh post-fix capture review
```

## Audit Against The Four AAA Points

### 1. Player Fantasy

Status: mostly aligned.

What works:

- The mission objective `Destroy the forward patrol` supports the new offensive-command premise.
- The map reads as a civilian/infrastructure district, not an abstract arena.
- The player has a clear military response squad and a hostile patrol target.
- The tactical terrain and minimap support the idea of preparing and executing a targeted operation.

Design issues:

- The threat feed still contains generic/global alerts such as `Enemy Air Detected`, `Structure Under Attack`, and `Ally Under Attack`. In an infantry-only first mission, this muddies the premise and implies systems that M01 is not teaching.
- The selected detail panel says `Rifleman Male IV`, which is implementation/catalog language, not player-facing command language. It should read as `Rifle Squad`, `Command Squad`, or the canonical M01 squad name.
- The ARIA collapsed button says `NEXT` but does not communicate the operation premise or next action in the reviewed public launch captures.

Recommendation:

- Tailor M01 HUD copy to the offensive-command premise: hostile patrol, civilian corridor, command squad, move to cover, neutralize patrol.
- Hide or replace non-M01 threat feed rows during M01 unless they are real authored context.

### 2. First 10 Minutes

Status: structurally correct, not fully proven visually.

What works:

- The critical path and reports now enforce the right golden path:
  `launch -> see/select rifle squad -> move to cover -> attack hostile patrol -> result`.
- Gameplay reports now claim the opening-control window prevents immediate lethal hostile fire.
- Automated validation reports now cover a public campaign route to result popup.
- Infantry-only scope is preserved.

Design issues:

- The public launch screenshots available for visual audit do not show the complete final post-fix golden path after the atlas-quad/opening-control fix. They show launch and selected-first-control, but not fresh post-fix move/attack/invalid/result capture states from the same final runtime presentation.
- The initial public launch state is still cognitively dense for a first-time player: objectives, threat feed, resource bar, squad tray, command controls, minimap, ARIA button, and enemy patrol are all visible at once.
- There is no visible first-action instruction in the public launch capture beyond the collapsed ARIA button and selected `MOVE` control. For a AAA first 10 minutes, the player's next action should be unmistakable.

Recommendation:

- Run a fresh post-fix M01 capture matrix from the public route: match start, squad selected, move command accepted, attack command accepted, invalid command, assistant open, result popup.
- For M01 only, reduce or sequence nonessential HUD noise until the first move/attack lesson is complete.

### 3. Readable Scale

Status: promising, but not final.

What works:

- The 20:9 public launch capture gives enough horizontal tactical context to read player squad, road, and hostile patrol.
- The terrain is far closer to a premium tactical map than the earlier flat/brown/legacy scene evidence.
- The older route/safe-area matrices prove the UI has strong marker language for move, attack, invalid command, and objective flow.
- The latest gameplay report claims four distinct soldier renderers under one squad entity and tactical-scale projectile traces.

Design issues:

- In the 16:9 public launch capture, the hostile patrol sits at the far right edge and is partially out of the player’s focus area. That weakens first-contact readability.
- The player squad reads as a clump at gameplay scale. The four-soldier identity is more visible in the HUD card than in the world.
- The selected-state world marker in reviewed public captures appears as a bright cyan square/plate. It is visible, but it feels temporary and can overpower the squad rather than frame it.
- Move/attack/invalid markers are clearly visible in older route-safe captures, but those captures use an older/proxy tactical presentation. The latest public art path needs equivalent proof.

Recommendation:

- Adjust M01 first camera/framing so player squad, destination cover, and hostile patrol are all readable without the hostile patrol sitting on the edge of frame.
- Keep the selection marker visible, but refine it toward a tactical ring/ground bracket instead of a filled cyan plate.
- Require fresh 16:9 and 20:9 captures for selected, move, attack, and invalid states using the latest atlas-quad runtime.

### 4. Cohesive Presentation

Status: improved, with copy and temporary-art gaps.

What works:

- HUD chrome, minimap, objective panel, command bar, and result popup share a coherent WarlineCapture style.
- The tactical terrain now visually supports the 2D isometric premium direction.
- The result popup reads strongly and gives a satisfying first mission endpoint.
- The app-shell and tactical HUD look like one product more than the earlier route/proxy captures did.

Design issues:

- Some HUD text is truncated in key places, especially objective/star rows and selected entity labels.
- M01 still mixes first-mission teaching with non-M01 combat noise in the threat feed.
- Temporary unit/art readiness remains explicit in reports (`FinalAtlasArtReady = 0`). That can be acceptable for a temporary HCI pass, but not final visual signoff.
- The selected unit name and generic feed wording weaken the updated field-commander/hostile-faction premise.

Recommendation:

- Treat current M01 art as acceptable for focused HCI only if PM/user accepts temporary-art review. Do not treat it as final visual signoff.
- Do a small M01 copy pass before user-facing review: objective text, threat feed rows, selected panel title, ARIA prompt, invalid command text.

## Findings

### P0 - Final Gate 4 Cannot Pass Without Fresh Independent QA/HCI Evidence

The project has strong automated proof and accepted lane evidence, but the PM review still says final Gate 4 needs independent QA/HCI validation after workspace refresh. Designer audit agrees.

Required before final acceptance:

- public route golden playthrough verified in the active QA workspace
- fresh post-fix visual captures from the latest runtime presentation
- first-control readability confirmed by visual review, not only tests

### P1 - M01 First-Control Screen Is Too Noisy For First-Time AAA Onboarding

The launch state shows objectives, threat feed, resources, squad tray, command buttons, minimap, ARIA, and enemy patrol at once. This is acceptable for a systems test, but not yet ideal for a first player minute.

Suggested fix:

- keep the existing HUD layout, but suppress or sequence irrelevant M01 feed rows and make the first actionable instruction explicit.

### P1 - Hostile Patrol And Squad Readability Need Final Visual Proof

The latest reports say the squad is four soldiers and the patrol is readable, but the available public screenshots make the enemy patrol edge-biased and the player squad still visually clumped.

Suggested fix:

- fresh 16:9/20:9 captures after the opening-control/atlas fix
- camera/framing adjustment if the hostile patrol remains edge-cropped
- selection marker polish if the cyan plate remains final-looking

### P1 - M01 Copy Does Not Fully Match The Updated Premise

`Destroy the forward patrol` works. `Enemy Air Detected`, `Rifleman Male IV`, and generic alert rows do not.

Suggested fix:

- rewrite M01-specific HUD/feed labels around `command squad`, `hostile patrol`, `civilian corridor`, and `neutralize`.

### P2 - Result Popup Is Strong Enough For Current Review

The result popup reads as a premium endpoint and supports the M01 loop. It can remain as-is for this stage unless QA finds interaction or layout issues.

## Recommended Next Work

1. QA/HCI refreshes the final validation workspace and runs the public-route M01 golden playthrough.
2. Generate a fresh post-fix capture matrix from the latest runtime presentation.
3. Designer/QA reviews captures for:
   - first-control clarity
   - player squad readability
   - hostile patrol readability
   - selected state
   - move marker
   - attack marker
   - invalid command recovery
   - objective/result confirmation
   - 16:9 and 20:9 safe-area behavior
4. UI/Support do a tiny M01-only copy/noise pass if QA confirms the threat feed and labels are still present.

Validation run:

- Reviewed current M01 critical path, M01 contract, movement design, latest Gameplay/UI/QA/PM reports.
- Visually inspected public launch and selected-first-control captures at 16:9 and 20:9.
- Visually inspected route/safe-area capture contact sheets for move, attack, invalid, assistant, and result states.

Validation result:

- M01 is directionally aligned with AAA mobile design.
- M01 is not ready for final AAA acceptance until fresh post-fix public-route visual evidence and independent QA/HCI review land.

Known gaps:

- This audit did not run Unity or interact with the build.
- Latest gameplay opening-control report did not include a fresh full visual capture matrix for every state after the atlas-quad fix.

Cross-lane impacts:

- QA/HCI owns final validation.
- Gameplay owns any camera/framing, first-control, unit readability, projectile/VFX, or atlas-runtime defects.
- UI owns HUD copy/noise, selected panel label, command state, and marker presentation issues if confirmed in fresh captures.
- Support/FTUE owns ARIA prompt clarity if the first action remains unclear.

Next recommended task:

Refresh QA/HCI workspace and run the final public M01 HCI pass with a fresh capture matrix from the current runtime state.
