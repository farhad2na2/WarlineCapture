# Campaign validation diagnosis

This is a read-only source/evidence analysis. No mission readiness is certified by this report.

## Selection replay

`CampaignTutorialEntryEditorProbe.Journey.cs` follows the rendered `_directTutorialTarget` with no current selection/mode consistency check. `MatchHudAssistantUiSystemHelper.Watch.cs:59` already documents that the decorative frame can lag selection and reads the current public target plus real Move/Attack control instead. Native Select toggles enter/exit selection mode and resets queued targeting/drag; the source does not establish that the Select click itself removes `SelectedUnitTag`.

The companion 1020 registers after generic `TickGameView` and skips every additional Canvas callback with the same frame number. Generic `TickGameView` has no matching frame suppression. Consequently callback ordering alone does not cover subsequent `Canvas.willRenderCanvases` invocations. T7 experiment 1031 reordered owned callbacks and deferred generic input during contacts, but still stalled lesson 2 (45 paired native contacts, zero unexpected samples/interventions). Its stale guard saw zero deferrals, while a later bounded read-only Canvas observer recorded 120 rows with satisfied source selection, including selected count 4 and `NeedsSelection=false`. These observations support the multiple-Canvas-callback mismatch; the refined 1032 fixture observes all Canvas callbacks while actuating at most once per frame. Neither fixture edits actors, selection, orders, resources, clocks, health or outcomes. The 3-second persistent-cue failure remains enabled.

Prepared Editor-only guard: `/private/tmp/delegation-campaign-journey-guard.patch`. It waits for a stale Select cue to settle and fails after 3 seconds, preserving all normal victory/result/return and timeout gates. It is not applied to either checkout.

## M3 native defeat

Native1013 is a real defeat, with no evidence yet distinguishing core breach, protected post destruction or roster-integrity failure. `CampaignMissionDefenseRuleUtility.IsFailure` uses those three predicates; friendly deaths alone are not its defeat predicate.

The driver made only six zero-length world taps. Its first two were logged `drag=True expected=4` at `(1165.70,492.96)`, with identical start/end. These are not blue-box group-selection drags. The normal pointer Focus path clears prior selection and selects exactly the clicked entity (`FocusedUnitLifecycleCompositionSystemHelper.FocusUnitEntity`). M3 `UiMissionTutorialTarget.NeedsSelection` checks only the guidance source; group bounds/required count are supplied only while it still needs selection. Therefore the current driver can advance after selecting one rifle and fail to move/Hold all intended defenders. The source roster has two four-person squads, whereas the older isolated rifle defense fixture explicitly commands all eight. That older fixture injects individual source orders and cannot replace normal-input acceptance.

Next useful M3 check: read-only records of all friendly cohort positions, selected/Hold/attack tags, attack range/AutoEngage, hostile wave states and exact failure facts while a normal-input driver uses actual visible selection rectangles or native squad cards. Do not change budgets, combat balance or outcome predicates to compensate for this driver strategy before that evidence exists.

## T7 scope

Original T7 branch `codex/map-visual-refinement` at `5d48ecd66` was preserved. Its exact state/untracked inventory is saved in `delegation-campaign-t7-original-state.json`; only Pipeline port 7801→7802 was locally modified. Old-source 1031 failed evidence is separate.

A reversible new `codex/s005-parallel-validation` branch was created at `origin/main` 94d949c92 after the owned QA fully closed. All old untracked visual evidence was retained. The current T7 pre-import source fingerprint is `f03851c1b8761a95569b1b313a219a57924876ea99d87215387157627d595b53`. Exact comparison with main cc14c5 has two differences: the preserved Editor Pipeline port configuration and a missing generated SceneDependencyCache metadata file. Runtime scripts, logical mission configs, physical map files and materials otherwise match; the full-source identities remain distinct and must be reported separately.

The prepared `CampaignDefenseReadOnly1033.cs` observer records all authored defenders/hostiles, cohort positions, selection/Hold/movement/engagement, health/range/AutoEngage and the exact three defeat predicates every 0.25 seconds. It catches its own observer errors to a separate file and never logs a gameplay error or changes state. It is not yet compiled or executed.


Current-runtime T7 M1 1032 also failed at lesson 1. Three native contacts were dispatched/completed with zero unexpected input or physical interventions, but all were zero-length fallback taps; no group box drag occurred. The final native screenshot shows two selected soldiers and a guide partly behind the command HUD. The bounded cue-shape observer omitted `publicHasTarget`, so its default `NeedsSelection=false` must not be treated as a satisfied selection. This does not establish a persistent stale Select cue defect. The whole T7 source fingerprint remained f03851c1b8761a95569b1b313a219a57924876ea99d87215387157627d595b53 before/after.

1034 is a follow-up validation-only driver requiring actual visible group drags and normal public-selection FullMap camera recovery when the group box is unavailable. The map input source suppresses the next world click and invokes only camera movement, so this recovery issues no troop order. A read-only observer records the public target availability, panel wording, visible group guide, exact guidance source selection and all current-session role cohorts to distinguish a projection problem from input strategy. Camera recovery is bounded; all original Victory/result/return gates remain.


1034 and 1036 failed closed on actual group-guide obstruction. The scalar observer in 1036 records four current-session command soldiers, all 125/125, zero selected. At the settled native pose, guide lo=(938.29,90.12) raycasts to `V3GradientLayer,SelectCommand`; hi=(1188.80,315.23) and midpoint are clear. Public target is available, NeedsSelection=true, RequiredSelectionCount=4, DragSelection=true. Camera focus through the existing FullMap reaches the same clamped pose and does not make the lower corner usable.

1038 deliberately reproduced ordinary partial selection with normal taps at the public target. It is failed journey evidence. One of four healthy friendlies became selected, while every available partial-cohort sample retained public NeedsSelection=true/Required4/DragSelection=true. The public fallback actor changed after selection moved an entity between archetypes. No native partial-cohort sample falsely reported selection satisfied. Therefore the proposed M1-only projection patch is **not applied** and its suspected failure mode is unproven in this native run. Earlier inference is narrowed accordingly.

1040 tests the supported FullMap ZoomIn button with normal Touch hold before refocus and close, using only public group bounds. This supplies camera recovery without any camera transform or actor manipulation.


1040 failed within the bounded normal recovery. The FullMap ZoomIn gesture was dispatched/completed, but this control zooms the map projection, not the world camera (`MatchHudMinimapInputUiSystemHelper.HandleZoomHeldChanged` only changes `_minimapZoomedIn`). The public target then falls outside the cropped presented projection, preventing the subsequent focus. This control cannot resolve world-HUD occlusion. The native partial-selection screenshot is retained in 1038 screens; it shows the selected single soldier, pressed-Select guidance and unchanged lesson 1, consistent with NeedsSelection=true.

A separate supported native group action exists: the visible RIFLE SQUAD tray card calls MatchHudSquadTraySelectionUiSystemHelper.SelectSlot; for M1 guided phases it builds the full required four-person mission squad. This is normal UI input and preserves the real four-selected runtime gate. It can establish compatibility while the unusable world drag guidance remains explicitly pending. No source correction has been applied.


1041 established scoped native M1 compatibility with the existing visible Rifle Squad card: English and Farsi both selected all four through normal card Touch, completed actual objectives, native Victory, result and Campaign return. Two contacts completed, zero unexpected samples/physical interventions, persistent stale cue guard zero. This pass precedes the guidance source patch and does not resolve the misleading inaccessible world-drag instruction.

A narrow T7-only guidance correction is now under native validation: if M1's drag corners are obstructed while in Select mode, guide the existing visible/interactable/uncovered Rifle Squad card and explain the four-person selection in English/Farsi. It invokes no card callback or gameplay state. Other mission selection semantics remain unchanged. T7 retains the older air movement source; no whole-current-main certification is implied.

The proposed synthetic edit-mode card test failed before the native test. A transient read-only diagnostic identified Screen2437x536 versus unrendered Canvas1920x1080, so its card center(960,540) is outside actual safeArea height536; Image.depth=-1 and no raycast hits. The helper correctly rejects this synthetic geometry. This fixture result is retained as failed evidence, not a production success or proof of a broken native card. Root requested the meaningful native rendered-scene proof next; 1042 requires actual presented card cue, normal card Touch, all-four runtime selection, Victory and return in both languages.
