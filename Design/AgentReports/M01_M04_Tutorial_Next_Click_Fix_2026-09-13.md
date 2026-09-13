# M1–M4 tutorial next-click guidance

## Reported problem

In M3, Build opened on the Buildings category with its normal gold selected-tab appearance. Clicking that already selected category did nothing, and ARIA did not identify the next item or placement action. The Build popup also hid the tutorial rail.

## Changes

- Keep the existing Build visual design. Draw a separate, labeled tutorial frame around the next actionable control, above popup canvases, without intercepting touches.
- Resolve construction guidance from the live category and item selection. Skip an already selected Buildings tab. For M3, suggest a road barrier (or available guard tower); selecting a Barracks does not satisfy the defense lesson. Follow selection with Place, then the enabled placement confirmation. Invalid footprints keep the existing preview/error and never receive a confirm cue.
- Keep ARIA visible beside Build for M3 construction and recruitment as well as M2. Keep the popup fitted to the rail through its opening animation.
- Recruitment follows Build → Soldiers → rifle squad → Produce. Show Me points without purchasing or silently selecting another control. Each Do It invokes one visible control. Suppress repeat recruitment cues while an accepted order is pending.
- Only selecting the actual Barracks acknowledges M2's Barracks selection lesson. Recover to Buildings if a different tab is open. The Materials explanation points to its Continue action rather than presenting the resource strip as a clickable control.
- An M1 attack click no longer suppresses later tutorial instructions. Existing command-button → world-target narration and mission progression remain in use.
- M3/M4 automatically present next-click cues with the active instruction. Move/Board guide the relevant selection, command, and world destination. Suppress repeated movement cues during an accepted journey. Hold/Stop recover missing selection; Stop's resume-Hold instruction follows the current mission state.
- M4 target positions come from live ECS actors and mission destinations. Hidden passengers track their transport. Require all four specialists for team selection. Guide the passenger chip, then Exit All, rather than repeatedly reopening the same panel.
- Preserve authoritative completion: construction, squad arrival, holding, boarding, unloading, combat, and extraction advance on accepted simulation facts. A UI highlight itself does not complete an objective. Combat/landing-clearance waiting lessons do not suggest a fake completion click.
- Recognize M3 defenses built through the player placement flow. That flow publishes ownership records rather than scripted spawn requests; watching only scripted requests left the defense lesson stuck after a successful placement. Capture an attempt baseline so existing map defenses do not complete the lesson.
- Add all ten new captions, with Farsi translations, to `Assets/Game/Configs/Localization/StartThroughM02UiStrings.json` for the shared catalog builder and future languages.

## Screen and action coverage

| Mission | Instruction sequence covered |
| --- | --- |
| M1 | Select squad; Move → destination; inspect/engage → enemy; subsequent combat/corridor guidance; attempt reset |
| M2 | Open Build; correct category/Barracks; Place → valid Confirm; Materials Continue; Build → Soldiers → rifle → Produce; wait for actual production/defense |
| M3 | Warning → inspection; explanation Continue; defense category/item/Place/Confirm; select/Move/destination/arrival; Hold; Stop/resume Hold; radar; rifle recruitment; combat milestones |
| M4 | Plan Continue; carrier selection/Move/pickup; four-person selection/Board/carrier; carrier Move/landing; passenger chip/Exit All; aircraft and team selection/Board/aircraft; clearance wait; Move/departure |

## Validation

Focused Editor validation entry point: `MissionTutorialNextActionValidation.Run`.
Live M3 construction entry point: `MissionTutorialNextActionValidation.RunBuildAcceptance`.
Architecture entry point: `MissionReadinessArchitectureValidation.Run`.

- Architecture: **139 passed, 0 failed**, across nine fixtures. Final log: `/private/tmp/warline-tutorial-architecture-final.log`. Source-size and ownership guardrails remain unchanged.
- Focused Editor checks: **passed**. M1 guidance (14), M2 guidance (42), shared ARIA UI (24), M3 rules (12), M4 integration (10), plus nine targeted checks for next controls, popup layering/touch pass-through, pending/rejected production, live M4 targets, player-built defense completion, and CI guard inputs. Log: `/private/tmp/warline-tutorial-completion-final.log`.
- Live M3 construction: **passed**, wrapper exit code 0. Entered M3, advanced opening instructions, opened Build through ARIA, clicked the highlighted defense card, clicked Place, and clicked the enabled placement confirmation. Verified authoritative progression to guidance 45005 (squad movement). The QA driver stages the preview at a validator-approved location; selection, purchase confirmation, building ownership, and lesson advancement use the real runtime paths.
- Visually inspected the final Farsi captures for defense selection, Place, and Confirm at 1920×1080. The labeled gold cue tracks each actual control and ARIA remains beside the popup. Captures: `/private/tmp/warline-m03-editor-probe/tutorial-build-1.png`, `tutorial-build-2.png`, and `tutorial-build-confirm.png`. No evidence images were added to Design.
- A concurrent validation launch timed out during startup; its wrapper closed that owned Editor. Repeating the same approved wrapper on its own completed successfully. The user's Editor was not terminated.
- Scope: automated M1–M4 guidance/integration coverage and live M3 construction QA. This pass is not a new start-to-victory manual playthrough of all four missions; no Android validation was performed.
