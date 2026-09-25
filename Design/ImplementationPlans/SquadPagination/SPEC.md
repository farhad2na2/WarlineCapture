# Five-squad tray with separate pagination

Date: 2026-09-25. Source audit: `2bea6e6f1349595f3fd80da03a0350005e8bb555`.
Status: first ImageGen direction explicitly approved by the user in this task. Technical specification prepared; implementation and native acceptance pending.

## 1. Decision and scope

Restore the fifth squad card. Expanded skirmish displays five real groups per page, with separate Previous and Next buttons above the tray. Pagination is browsing, never selection, camera movement, or a troop order. Keep selection across pages.

The user approved `approved-direction.png`, the first mockup, and rejected the Army drawer as the immediate direction. Do not request visual-direction approval again for this implementation. The mockup's 12 groups, particular roles, objective, health values, and surrounding panel rearrangements are illustrative, not authored mission changes. Implement the roster area and required integrations; preserve the existing mission content, selected-unit panel, minimap, seven command controls, and visible ARIA Play/Stop.

This specification resolves small details absent from the image: endpoint behavior, empty slots, persistent multi-selection, a Clear button, and selection summary. Clear is necessary because existing expanded squad taps add to selection and paging currently provides an accidental way to clear it. Do not introduce an Army drawer, swiping, recruitment controls, new input modes, or new art generation.

Use the existing expanded-session capability gate, not a hardcoded scenario number. The audited code shares this behavior across expanded skirmish. The user's label “Skirmish 5” was not resolved to a catalog ID in this checkout; verify the actual visible entry during native validation and record its title and configuration. Do not invent `skirmish.s005` or change catalog publication to make a test run. Campaign and legacy skirmish retain their existing five category/slot controls and restrictions.

## 2. Current implementation: facts to preserve or replace

All paths below are repository-relative.

| Owner | Current behavior | Required change |
|---|---|---|
| `Assets/Game/Scripts/Skirmish/Contracts/SkirmishArmyContracts.cs` | `SkirmishArmyPaging.StandardPageSize = 4` | Single shared page-size constant becomes 5 |
| `Assets/Game/Scripts/Runtime/Skirmish/SkirmishExpandedPresentedOrders.cs` | Four out-slots; index 4 calls `TryAdvancePage`; advance wraps and calls `SkirmishArmySelectionService.Clear` | Five squad slots, separate navigation API, clamp without wrapping, no selection mutation |
| `Assets/Game/Scripts/Runtime/Skirmish/SkirmishArmyGroupSystem.cs` | Initializes page size; resolves living friendly groups in buffer order | Keep stable group IDs and ordering; use common size |
| `Assets/Game/Scripts/Runtime/Skirmish/SkirmishArmyDrawerProjection.cs` | Projects current page; clamps page after losses | Project five; expose identity/counts; keep selection independent from page |
| `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.CommandHeader.cs` | Card4 synthesized as NEXT with health 1 | All five cards come from living group data; no navigation health/card |
| `Assets/Game/Scripts/UI/Components/MatchHudSquadTrayView.cs` | Portrait resolver limited to `i < 4`; single selected slot | Resolve all five; expanded selected visuals come from group flags |
| `Assets/Game/Scripts/UI/Components/MatchHudSquadTrayView.Cards.cs` | Creates `NextPageArrow` inside card 5 | Remove this mechanism; separately authored pagination UI |
| `Assets/Game/Scripts/Systems/MatchHudSquadTraySelectionUiSystemHelper.cs` | Special page-index branch clears active slot | Every index 0–4 selects a group; page controls bypass squad-selection path |
| `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ExpandedOrders.cs` | Four-bit projections | Add fifth slot to every mask and separate page/clear gateway methods |

`SkirmishArmySelectionService.TrySelectGroup` already supports additive selection. `TryPresentedSlot` currently chooses `add = AnyPlayerSelected`. Keep that behavior: tapping a selected card is idempotent, not a new toggle. Add explicit Clear. Do not globally alter Campaign selection behavior.

## 3. Behavior contract

### Page calculation

Count only living player groups belonging to the active expanded session. Preserve the existing friendly-group buffer order; do not sort by health, selection, or role. New groups join in existing creation order. Dead groups leave the visible roster.

```text
pageSize = 5
pageCount = max(1, (totalGroups + 4) / 5)  // integer division
pageIndex = clamp(pageIndex, 0, pageCount - 1)
start = totalGroups == 0 ? 0 : pageIndex * 5 + 1
end = min(totalGroups, (pageIndex + 1) * 5)
canPrevious = pageIndex > 0
canNext = pageIndex + 1 < pageCount
```

Previous and Next move exactly one page on pointer release. No repeat-on-hold and no wrap. Disabled endpoint buttons stay in place and are visibly disabled. Paging neither selects nor deselects, issues no command, preserves active command mode and selection-panel subject, and never focuses the camera. UI pointer capture must prevent the release from becoming an Attack/Move/world click.

If losses remove the last page, clamp to the last valid page. Do not reset to page zero for ordinary updates. Production must not automatically open a new page. All fields in a displayed page must come from one coherent projection.

### Squad selection and identity

- All five cards represent actual groups. Index 4 is always the fifth group of the visible page.
- Selection is stored by `GroupId`/existing group flags and unit tags, never by visible ordinal alone.
- An unselected card adds that group to the selected set. An already selected card remains selected. No automatic order or camera change.
- Selected cards show a check and selected border. Multiple cards can show selected simultaneously. No selected border may follow a recycled slot onto a different group.
- Preserve selected living groups when browsing; returning to a page restores their checks from data.
- Show total selected group count and off-page selected count. Move/Attack/Hold continue to target the actual selected set, including off-page groups. Never silently restrict an order to the visible page.
- Clear explicitly clears this expanded army selection, selected unit tags, stale tray focus/selection-panel state through existing selection ownership, and nothing else. It does not stop movement or cancel existing orders. It preserves page and camera. Disable Clear when no groups are selected.
- On group death, use existing cleanup and recount living selected groups. Do not leave a dead group in selection counts.
- Freeze each card's displayed `GroupId` on pointer-down. On release, accept only if it still represents that same living group in this session; otherwise ignore the click. A death/reflow while the finger is down must not select a replacement group. New page/clear controls similarly accept only when still enabled in the current session. Implement this with a small input guard; do not redesign the global input system.

### Empty, blocked and lifecycle states

Keep five equal card footprints on partial pages. Unoccupied cards have dark disabled frames with no stale portrait, count, health, number, selection check, or NEXT label; they consume pointer input without affecting the world. Zero groups uses `NO SQUADS AVAILABLE`, five empty footprints, no page arrows, and Clear disabled.

Pause, results, startup failure, modal overlays, and any existing input restriction disable/block these controls consistently with the existing tray. Respect the current human-input/ARIA ownership handback path. Navigation itself must not implicitly start or stop ARIA. Unbind callbacks and reset cached group identity when the HUD/session changes. Legacy/Campaign re-entry must restore original portraits, labels, restrictions, and selection visuals; no pagination objects remain visible.

## 4. Native layout and visual specification

The builder reference canvas is **1672 × 941**, not 1920 × 1080. `ApplyMobileCommandLayout` currently places the tray at `(10,731,707,201)` and command rail at `(719,765,943,167)`, in top-left reference coordinates. Preserve those rectangles and responsive expansion rules.

Create a sibling `SquadPagination` under the footer, outside `SquadTray/Frame`'s HorizontalLayoutGroup. Align its left/right edges with the actual tray. At the reference size its rect is `(10,627,707,96)`, leaving 8 units before the existing tray. Anchor to the tray's top so aspect-ratio changes cannot detach it. Do not expand its width with the command rail. Ensure local canvas/raycast sorting makes the bar clickable above the world, without a full-screen transparent blocker.

```text
SquadPagination                         707 × 96
  NavigationRow                         height 64
    PreviousButton                      width 80
    RangeLabel                          flexible width 435
    NextButton                          width 80
    ClearButton                         width 88
    // Three gaps of 8. Total: 80 + 435 + 80 + 88 + 24 = 707.
  SelectionSummary                     y=68, height=28, width=707
SquadTray                               8 units below pagination root
  Frame
    SquadCard1 ... SquadCard5           original equal-width layout
```

Use flexible layout arithmetic rather than hardcoded right-edge positions.

When total groups <= 5, hide Previous, Next and RangeLabel; keep the same 96-unit root and place Clear at the trailing end if any groups are selected. Keep the summary in its normal location. This prevents squad cards from moving. When no selection and <=5 groups, hide Clear too and show only the summary (`Tap squads to select`, or zero-group copy). The root's empty area must not block unrelated battlefield input.

Reuse Oxanium Bold/Medium from `MatchHudV3PrefabBuilder` and its NotoSansArabic Persian font. Reuse existing V3 gradients, disabled material, sprites, audio, localization, and portrait resolver. No rasterized text. Arrow icons use existing suitable chevron artwork or native geometry; do not depend on unsupported TMP glyphs.

| Element | Treatment |
|---|---|
| Navigation background | Existing dark raised V3 surface, subtle neutral 2-unit border |
| Arrows | Existing blue button gradient/cyan edge; white single chevron; no portrait, number badge or health strip |
| Range | `SQUADS 1–5 OF 12`, centered, Oxanium Bold 24; no auto-size below 22 at reference size |
| Clear | Neutral dark utility button, localized `CLEAR`, 20–22 font; never red Stop/Attack styling |
| Summary | Medium 20; bright neutral text, off-page phrase highlighted amber; never health-green |
| Expanded normal card | Neutral frame; appropriate existing role portrait |
| Expanded selected card | Existing theme green border, 4 units, plus check badge; selection never color-only |
| Guidance | Existing yellow guidance cue remains distinguishable from green selection |
| Disabled/empty | Existing grayscale/disabled treatment; no bright active outline |

Card interiors: retain five-card footprints and existing portrait assets, adjusting only expanded-state child layout. Current tray padding is 4 on each side, spacing 5; at width 707 this yields `(707 - 8 - 20)/5 = 135.8` units per card and 193 usable height. Specify positions relative to each actual card, not assumed 139px width: portrait inset 7, top 8, height 110; role label y=122, height 30; health y=156, height 10, inset 9; alive-count line y=170, height 19. Badge top-left is compact current roster ordinal; selected check top-right. Use `AliveCount` (e.g. `8 units`), not `8/8` unless a genuine denominator is available. Health fill uses the existing real group-health projection, not the alive-count fraction. Role labels are localized from existing role catalog, fit up to two lines within their allocated area, and never overflow into adjacent cards. Health/counts must update from the same card model as the portrait. Campaign card layout remains unchanged.

The mockup numbers are roster positions, not invented immutable squad names: show `pageIndex * 5 + index + 1`; retain stable `GroupId` internally. Do not create new role portraits if an exact role asset is absent: use the established role-family fallback and accurate text.

At 1280×720, reference scaling is about .765: arrows/Clear must retain >=44×44 screen-pixel hit rectangles (64-reference-unit height gives ~49). Verify actual screen-space geometry with safe areas and canvas scaling, not this approximation. Capture 1280×720, 1920×1080 and 2400×1080 in en and fa-IR. Fit mobile safe areas without shrinking the original five cards or seven command buttons. Do not overlap minimap, feedback, selected-unit panel, ARIA or command-wheel surfaces. The pagination occupies only the left tray-width area.

### Localization

Add keys through the existing catalog/binding pipeline. Proposed copy:

| Key | English | Persian |
|---|---|---|
| `ui.skirmish.squads.range` | `SQUADS {0}–{1} OF {2}` | `گروه‌های {0} تا {1} از {2}` |
| `ui.skirmish.squads.previous` | `Previous squads` | `گروه‌های قبلی` |
| `ui.skirmish.squads.next` | `Next squads` | `گروه‌های بعدی` |
| `ui.skirmish.squads.clear` | `CLEAR` | `پاک کردن` |
| `ui.skirmish.squads.none_selected` | `Tap squads to select` | `برای انتخاب، روی گروه‌ها بزنید` |
| `ui.skirmish.squads.selected` | `{0} selected` | `{0} گروه انتخاب شده` |
| `ui.skirmish.squads.selected_off_page` | `{0} selected • {1} on other pages` | `{0} گروه انتخاب شده • {1} در صفحه‌های دیگر` |
| `ui.skirmish.squads.empty` | `NO SQUADS AVAILABLE` | `هیچ گروهی در دسترس نیست` |

Use existing locale-aware number formatting and RTL text shaping; do not reverse strings. In Persian mirror the roster/pagination reading direction consistently: first logical squad on the right, Previous on the right pointing right and Next pointing left. Logical index/GroupId semantics remain unchanged. Clear is at the trailing end. English visual row is Previous, Range, Next, Clear; Persian mirrors it. Role names already have localization; do not duplicate them. Validate Persian copy visually and record real player language acceptance separately.

## 5. Contract and ownership changes

Keep ECS state in Runtime, data contracts in UI.Contracts, and rendering/input observation in UI.Runtime. The view must not query EntityManager or issue army orders directly. Preserve existing assembly boundaries.

1. Set `SkirmishArmyPaging.StandardPageSize = 5`; make `PresentedSlots` reference that constant. All expanded projections/selectors use the same value. Search all references to page size, four-slot loops, and magic NEXT mappings; do not globally replace unrelated literal 4s. Newly initialized sessions use 5. Normalize any surviving expanded session with PageSize=4 to 5 once at the runtime owner boundary, clamp page, and preserve selected GroupIds. No checkpoint schema migration unless the actual serializer stores that field; the audited checkpoint files did not show PageSize serialization.
2. Extend `SkirmishPresentedSlot` with `GroupId` and member count data actually needed by rendering. Add slot4 to `TryReadPage` and every caller. Extend page metadata with page count, total group count, CanPrevious/CanNext, total selected group count, off-page selected count, and a roster revision if needed for safe stale-input handling. Do not fabricate health/occupancy for navigation.
3. Replace `TryAdvancePage` with `TryChangePage(em, session, int delta)` accepting only -1/+1, returning false at endpoints. It changes only PageIndex and projection. Remove index==PresentedSlots routing from `TryPresentedSlot`. Reject index >=5 as invalid.
4. In `IUiSkirmishGateway.cs`, `UiShellRuntimeGateway.Skirmish.cs`, and `UiShellEcsGateway.ExpandedOrders.cs`, add separate `TryChangeExpandedSquadPage(int delta)` and `TryClearExpandedSquadSelection()` methods. Extend `UiExpandedSquadPage`; remove/replace the ambiguous `NextPage` boolean throughout with explicit page metadata. Expose selection by displayed GroupId through an expanded-only guarded path while retaining legacy `MatchHudSquadTraySlot` APIs for Campaign.
5. Extend `UiMatchHudSquadTrayCardModel` additively for expanded group identity, selected flag, alive count and ordinal. Preserve legacy constructors/defaults so unrelated screens do not need a broad refactor. Render all five expanded cards from this data. Keep the masks in `UiExpandedSquadPage` at five bits (bits 0..4); all Assault, Selected, Structure, AttackOrder and Air masks include slot4.
6. Add an expanded-only pagination partial for `MatchHudSquadTrayView`, with serialized root/buttons/text references and public visible Previous/Next/Clear button accessors for ARIA. Bind/unbind once; no accumulating listeners or per-frame GameObject creation. Use explicit page/clear gateways; never call squad selection with a sentinel index. Update labels only when values/locale change.
7. Route expanded selected borders/checks from group flags rather than `_selectedSlot`. Keep `SetSelectedSlot`/enum compatibility for Campaign. Audit `RefreshMissionRestrictions`, `SetSelectedSlot`, `ApplyMissionDisabledTreatment` so one does not overwrite another's expanded visuals. Empty card visibility/interactability must not be restored by a legacy restriction refresh.
8. Add a focused builder partial, e.g. `MatchHudV3PrefabBuilder.SquadPagination.cs`, called after `ApplyMobileCommandLayout`, to create/wire the sibling bar and expanded card elements idempotently. Update the actual `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab` through Unity-supported authoring. Preserve original transforms for returning to Campaign. Remove legacy card5 `NextPageArrow` from authored or runtime remnants. Follow project Unity execution rules; do not hand-edit prefab YAML.

## 6. ARIA migration is required, not optional

Affected sources:

- `Assets/Game/Scripts/UI/Contracts/AriaSkirmishContracts.cs`
- `Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.SkirmishWatch.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.cs`
- `Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.AircraftCycleProbe.cs`
- Tests listed in section 8.

Add distinct observed `PreviousSquadPage`, `NextSquadPage`, `ClearSquadSelection` touch targets, plus publicly displayed page/selection metadata. `Squad4` becomes a real squad and must never be a navigation fallback. Observe the new real Buttons with the existing `ObserveWatchButton` raycast/visibility path. Hidden, occluded or disabled controls are unavailable. ARIA uses normal pointer actions on these controls; do not grant direct ECS navigation/selection shortcuts.

Search every `ExpandedNextPage`, `Target(view.Squad4,...)`, `slot < 4`, `VisibleCardButton(4)` and `TryReadPage` call. Classify each as a fifth squad or old navigation reference. The aircraft harness must look through all five AirMask bits and tap the separate arrow.

Old ARIA traversal relies on cyclic NEXT to return to a starting page. Implement bounded sweeps over non-wrapping arrows:

1. If a sweep starts in the middle, use Previous until page 0, then process pages in increasing order. A navigation tap is acknowledged only by observing a changed PageIndex; do not advance planner state just because output was emitted.
2. On each page, select the relevant visible un-ordered assault groups, issue orders through existing controls, and wait for their displayed order state. Do not mark a page complete while Attack is covered or unavailable.
3. At the last page, finish only when every page in the sweep was inspected and required structure-capable orders were observed. A page with no assault groups is inspected, not stuck. Return to page zero through Previous before the next periodic scan. A one-page roster has no navigation work.
4. If roster size/page membership changes, invalidate page-completion bookkeeping and begin a fresh bounded scan after the current acknowledged action; existing displayed orders prevent unnecessary duplicate issuance. Do not use `1 << (pageIndex & 31)` as unbounded evidence. Retain a bitset only with a verified maximum <=32 pages, otherwise use bounded traversal state appropriate to the actual group limit.
5. Selection persists now: before starting a new order batch that must exclude currently selected groups, ARIA taps the visible Clear button once and waits for displayed selected count zero, then accumulates the desired group cards. Do not clear on every update or immediately before Attack. Keep a clear/select/order substate so retries are idempotent. On changing page, previous-page selections are outside the new batch; Clear before building that batch. Aircraft return/service must similarly clear unrelated groups before selecting the aircraft. This is explicit planner intent, never a side effect of navigation.
6. Verify `SelectionCanDamageDesignatedBase`, `TryStructureColumn`, `StepAircraftCycle` and periodic reinforcement inspection under persistent selection. Page-local masks alone cannot establish capability of the entire cross-page selection; the explicit per-batch Clear sequence makes the planner's intended selection auditable. Human mixed selection continues to use existing gameplay capability rules.

Do not change ARIA combat policy, economic balance, recruitment thresholds, fog access, scenario publication, or mission outcomes to make this UI work. Preserve Play/Stop and human handback.

## 7. Implementation sequence and boundaries

Implement in this order; compile after each coherent stage. Read the applicable AGENTS.md and Unity CLI skill before executing Unity commands.

1. Runtime page size, five-slot projection, identity, non-destructive page service; focused army tests.
2. UI contracts/gateways, five-card renderer, group selection guard, Clear/summary. Update all signature consumers to compile.
3. Builder/prefab and English/Persian bindings. Capture native geometry before tuning.
4. ARIA observed buttons, traversal/selection acknowledgement and aircraft harness; planner regression tests.
5. Focused automated validation, native visual captures, complete normal-input mission/ARIA flow and return; record evidence.

Do not leave mixed 4/5 page sizes between stages. No new dependencies or new portrait artwork are needed. Do not build the second mockup. Keep source changes scoped to the shared expanded roster and its necessary consumers. Existing Campaign categories, command colors, mobile rail sizes and mission gates are regression constraints.

## 8. Validation and acceptance

Existing tests to extend, rather than replacing with mock-only pass markers:

- `Assets/Tests/Editor/SkirmishExpansion/SkirmishExpandedArmyTests.cs` — projection, selection and page compaction; contains explicit PageSize=4 fixtures to assess.
- `Assets/Tests/Editor/SkirmishExpansion/SkirmishExpandedAriaTests.cs` — old Squad4 page target assumptions.
- `Assets/Tests/Editor/SkirmishExpansion/SkirmishS002AriaHarnessTests.cs` — multiple fake NEXT targets and four-slot projection calls; migrate without weakening order assertions.
- `Assets/Tests/Editor/SkirmishExpansion/SkirmishExpandedVisualTests.cs` — native-facing layout expectations where appropriate.
- `Assets/Game/Scripts/Editor/AriaSkirmishPlanValidation.cs` and existing mobile geometry validation — run relevant existing regressions.

Required behavior cases:

| Case | Expected evidence |
|---|---|
| Totals 0,1,4,5,6,10,11,12 | Correct five-slot contents, ranges, partial/empty slots and page count |
| Fifth card on a full page | Selects correct GroupId, portrait and mask bit4; no page change |
| Next/Previous | Exactly one page, endpoints disabled/no wrap; selected IDs/tags/count, current orders, command mode, camera unchanged |
| Cross-page selection | Select groups on pages 0 and 1; summary/off-page counts correct; both receive a deliberate order; returning shows checks |
| Clear | Only selection changes; existing orders continue; page and camera unchanged |
| Loss/reinforcement | Clamp after final-page loss, no stale card/identity, new group discoverable without auto-navigation |
| Pointer held during reflow | Release never selects the new occupant of an old slot |
| Input modes/overlays | Paging and Clear cannot leak a world Move/Attack; covered controls unavailable to both human and ARIA |
| ARIA fifth squad | Can select slot4 as infantry or aircraft, never misreads it as NEXT |
| ARIA multiple pages | Real previous/next targets, acknowledged non-wrapping sweep, finite completion, preserved order verification |
| ARIA aircraft | Clears unrelated selection once, selects aircraft including slot4/later pages, returns/services/resumes using public inputs |
| Lifecycle | Bind/unbind/re-enable/retry causes one callback per click; Campaign/legacy return has no pagination leftovers |

For non-destructive assertions, compare all relevant state before/after the input, not only PageIndex. Use distinct group IDs and distinguishable data in fixtures so index/identity mistakes fail visibly.

Native capture matrix: en and fa-IR at 1280×720, 1920×1080, 2400×1080. Include first, middle and final page; mixed on/off-page selection; partial/zero group states (fixture captures allowed, marked as such); both ARIA running/stopped; a modal/command mode input check. Verify text fit, disabled endpoints, exact five cards, role portraits, health, hit targets, and all seven commands. The approved image is the visual direction; only native screenshots establish implementation fidelity.

Normal-input evidence must include the user's actual “Skirmish 5” entry and configuration, launch, selecting the fifth group, paging both directions, cross-page selection/order, Clear, ARIA Play/Stop/handback, result, and return. Use normal pointer input. Fixture-injected groups/outcomes, direct onClick invocation or an older candidate's win do not prove this playthrough. Also perform a shared expanded-session regression and Campaign/legacy entry/return check. If the actual entry is absent in this checkout, report that exact pending gate rather than claiming a substitute passed it.

On macOS use `rtk proxy Tools/CI/invoke_unity_macos.sh --timeout <seconds> --log <absolute-log> -- ...` for every Unity executeMethod, test run, prefab build and capture. Keep Hub open and signed in, never add `-batchmode`, invoke Unity directly, reset IPC, or terminate existing Editors to make room. Use a correctly prepared isolated validation project if ownership requires it, following repository tooling. Read the unity-cli skill for connected Editor operations; CLI build/run/test is not a substitute for the checked wrapper.

Give every run an explicit timeout and log. Preserve full logs and test-result XML; check exit code, expected pass marker and zero failures. If adding a focused `Game.Tests.Editor.SkirmishSquadPaginationValidation.RunFocusedValidation` runner, that name is a proposed new entry point, not an existing tool. Its final marker must be emitted only after all assertions pass: `[SkirmishSquadPagination] result=Passed`. A timeout, missing marker, failed assertion or project lock is failed evidence; do not recategorize a normal validation failure as licensing.

Store evidence under a dated subfolder of `Design/AgentReports/SkirmishSquadPagination/`, with source revision/configuration/locale/resolution/input method/run status. Keep failed attempts. Finish with four separate statuses: visual direction (approved), automated checks, complete normal-input playthroughs, and real player/device acceptance (pending unless actually performed). Do not claim player readiness from a mockup or compilation.

## 9. Delivery checklist

- [ ] Five real squad cards and a separate native pager; no NEXT squad alias anywhere.
- [ ] Page browsing is selection/order/camera neutral and input-safe.
- [ ] Persistent selection has correct per-card checks, off-page summary and explicit Clear.
- [ ] English/Persian layouts and small-screen hit targets verified.
- [ ] ARIA uses the new public controls and all five mask bits; complete traversal does not rely on wrapping.
- [ ] Builder changes, generated prefab changes, localization, tests and evidence are included together.
- [ ] Campaign/legacy behavior preserved; actual user-reported entry validated or clearly marked pending.
- [ ] Native/automated/playthrough/device acceptance reported separately, with limitations and failures retained.

## Reference provenance

`approved-direction.png` is the built-in ImageGen output explicitly approved in this conversation, copied unchanged into this handoff. It was generated using these actual project references:

- `Design/Roadmap/M01_M05_Readiness/Evidence/M03FinalWait/en-wait-area.png`
- `Design/VisualLockLayered/SCN-05_CampaignOperations/reference/SCN-05_CampaignOperationsV3_MissionSelect_Final_Target.png`

The mockup is not a screenshot of Skirmish 5. No game source, scene or prefab was changed while preparing this specification.
