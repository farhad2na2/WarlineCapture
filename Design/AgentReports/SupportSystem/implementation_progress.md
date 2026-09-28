# Support implementation progress

Baseline: `48eb8d1c0`, current checkout `/Users/farhad/Projects/WarlineCapture`.

Current: all four Campaign Support abilities implemented; 56 focused and 119 related regression cases passed, with complete normal-input journeys and native captures. Current HEAD `f419cd462`; unrelated monetization/roadmap work preserved. Future canonical missions, player/device acceptance and allocation profiling remain pending. See `TASKS.md`, `IMPLEMENTED_FEATURE_HANDOFF.md` and `Evidence/VALIDATION_INDEX.md` for final status. Existing Editor PID 98258 was reused throughout; Pipeline now 7800 after domain reload.

## Preserved pre-existing changes

The checkout already contained Steel Push gameplay/presentation/localization work and a monetization report. Those changes were preserved. Concurrent work advanced HEAD through `67a2fea48` to `e3dd2bb72`; it was not reverted. The two untracked monetization deliverables are outside this implementation. No Support commit has been made while validation is incomplete.

## Integration inventory

- Approved source: full-screen v02 project-assets mockup; visual direction approved 2026-09-28.
- Pipeline installed (0.6.0-exp.1). The existing Warline GUI Editor is now connected on port 7801 (PID 98258); all subsequent validations use the wrapper's explicit `--reuse` path. Hub and unrelated Fig remain open. The available in-Editor MCP bridge targets Fig, so Warline commands use the CLI with its exact project path.
- Build is excluded from the mission pause whitelist; Support follows this behavior.
- Campaign target visibility has no general positive eligibility API; Support requires explicit authored ground regions/target projection. Missing data rejects.
- Campaign pending-resume launches `MissionRunKind.Retry` (`UiCampaignMissionProjectionSystem.cs:338`), with a new attempt/session via the launch owner. No in-flight world snapshot restore exists; Support resets with that attempt. Airborne manifest and crate/parachute native inspection remain to be resolved before their integration.

## Initial Smoke-slice ledger (historical; current ledger in TASKS.md)

| Task | State | Current deliverable / missing evidence |
|---|---|---|
| T00 | In progress | First-slice interfaces resolved. Later aircraft manifest/collection and native asset inspection remain deferred to their gated tasks. |
| T01 | Focused assertions passed | Four typed definitions, 25 policies, importer, source icons/prefabs and production-disabled runtime resources. Balance and visual catalog changes preserve unrelated sections. Regenerated from the current canonical hash; all production-readiness flags remain false. |
| T02 | Focused assertions passed | Attempt identity, pure preview, bounded receipts/deduplication, active-simulation clock, policy/ownership checks, positive authored ground regions and cleanup. Missing positive ground data fails closed. |
| T03 | Focused assertions passed | Smoke's immediate physical Fuel transaction prepares sorted storage copies before writes; protects existing outbound/civilian reserves. Aircraft reservations remain gated. |
| T04 | Focused assertions passed | Fixed 12 m / 15 s / 650 permille zones; both unit shot branches and building defense apply mitigation before predicted health/aggregation. Presentation uses existing smoke particles. |
| T05 | Focused assertions passed | Full-screen four-card popup, independent Support/Scan dispatch, targeting footprint, explicit confirm/cancel, drag camera inspection, English/Persian copy, Play/Stop. Menu registration passed (`shells=1`). Native English 1920x1080 and Persian 2400x1080 captures are under `Evidence/NativePopup/`. Normal-input testing found and repaired missing GraphicRaycasters after modal Canvas elevation; repaired journey is running. |
| T06 | Focused assertions passed | Exact-action ARIA consent, one approval/request, expiry/decline/Stop, camera-only Show Target and interruption cancellation. |
| T07 | Pending | In progress. An explicit Smoke test grant uses the real Steel Push attempt, phase, grid and finite Fuel reserve. Campaign/briefing/narrative are reached through touch. First native journey failed on popup click-through, retained in Evidence; repair rerun underway. Full combat/result/return, Persian and no-Support runs remain required. |
| T08–T10 | Gated | Strike, Paratroopers and Supply runtime implementation must follow T07 acceptance. Their cards/configs are definitions, not working effects. |
| T11–T12 | Pending | Campaign grants/migration, lessons, full result/return journeys, player/device acceptance and performance measurements. No production ability is enabled. |

### Initial Smoke-slice files and ownership (historical)

- New gameplay contracts: `Components/SupportAbilityComponents.cs`, `SupportEffectComponents.cs`.
- New catalog boundary: `Configs/Support*`, `Authorings/SupportAbilityCatalogAuthoring.cs`, `Composition/CampaignMissionCatalogProjection.Support.cs`, `Editor/SupportAbilityCatalogBuilder.cs`.
- New simulation owners: `Systems/Support/*`. Recurring clock, cleanup and cover ticks are Burst annotated; low-cardinality confirmed requests own validation and immediate spend.
- Reused authoritative owners: `UnitAttackSystem.cs` (both paths), `BuildingDefenseAttackSystem.cs`, and `BuildingResourceStorageTransferSystemHelper` (reservation/consume on copies).
- New presentation/input edges: `Rendering/Systems/SupportSmokePresentationSystem.cs`, `SupportPopupView`, `SupportAbilityCardView`, `SupportTargetingInputUiSystemHelper`, UI contract/gateway/read-model/intent files.
- Reused shell/command edges: `UIShellContentView`, Menu registration builder, command tabs/controls/rail, action dispatcher, normal-pointer composition and ARIA camera guidance. Scan guidance now references ScanButton, replacing its old Support alias.
- New focused test runners: `SupportConfigValidation`, `SupportRuntimeValidation`, `SupportSmokeValidation`, `SupportUiAriaValidation`. Aggregate `SupportSmokeSliceValidation` also invokes existing assembly, hot-path and vehicle Fuel checks. Focused Support runner passed 16 assertions on the repaired source. The unchanged repository-wide checks remain failed and separate; no aggregate pass is claimed.
- Existing architecture thresholds remain unchanged. Three concrete event/presentation boundary classifications were added; recurring systems remain Burst paths.

### Source/config revisions

Current canonical balance SHA-256: `34a07c6da8da9dcfe74d7b8ed284f3bd4a5bbeaa3bd913c197842f7a11b2e1c4`.

Current visual catalog SHA-256: `8e7fe79127da91622e88e8f2e372a86032a967534f4194faabf5d8448d7ea103`.

Generated assets now match the current balance hash, include final Menu registration and English/Persian copy. All four abilities remain production-disabled.

## Validation evidence retained

All full logs below are retained in `Evidence/`. Every Unity command used `rtk proxy Tools/CI/invoke_unity_macos.sh`, GUI licensing, an explicit timeout and unique log. Hub stayed open. No alternate execution route or licensing recovery was attempted.

| Run | Command suffix / log | Result |
|---|---|---|
| 01 | `--timeout 600 --log /private/tmp/support-catalog-T01-20260928-01.log -- -quit -executeMethod Game.Editor.SupportAbilityCatalogBuilder.Build` | Failed: C# errors, no pass marker. Using-variable index setter and ambiguous ushort `math.min` were corrected afterward. |
| 02 | `--timeout 600 --log /private/tmp/support-popup-T05-20260928-02.log -- -quit -executeMethod Game.Editor.SupportPopupPrefabBuilder.Build` | Builder logged catalog/popup pass markers and generated assets. Wrapper-owned Editor PID 92937 still owns the project after logging shutdown; successful process exit is not established. This is not a validation pass. Menu scene path/registration and final source were corrected afterward, not rerun. |
| 03 | `--timeout 600 --log /private/tmp/support-config-T01-20260928-03.log -- -quit -executeMethod Game.Tests.Editor.SupportConfigValidation.RunFocusedValidation` | Failed: wrapper tool reported exit 0 but required marker absent; log stops before assertions. Project ownership by PID 92937 verified through native open-project error and sanitized process inspection. Wrapper stdout retained separately. |

Independent checks: `rtk git diff --check` passed; both JSON catalogs parse; structural comparison to current HEAD confirms all unrelated catalog sections are identical and only the approved Support/deferred-Campaign entries changed. These checks do not prove C# compilation or gameplay. `rtk gain` inspected measured global savings (65.2K tokens, 1.6%); this is tooling evidence only.

## Recovery and connected-Editor validation

The owner explicitly authorized recovery and required reuse of the existing project. Only verified, completed validation Editors (92937 and 97833) were terminated during recovery. No unrelated Editor, Hub, licensing client or package manager was stopped. The healthy Warline Editor (98258) remains open and is reused; no alternate project is opened.

`invoke_unity_macos.sh --reuse` dispatches the exact executeMethod through Pipeline on the Editor update loop, captures full logs and an exit receipt, and times out without terminating the Editor. Async normal-input journeys retain their log until the actual result. GUI licensing and the repository wrapper remain mandatory.

| Evidence | Result |
|---|---|
| Smoke slice runs 04–05 | Failed compiler/safe-mode and then legacy unused assembly reference; full logs retained. |
| Smoke slice runs 08–09 | Four focused Support runners passed; unused GPU dependency still failed the aggregate. |
| Slice 10–11 | Scheduled delay callbacks did not execute in the background Editor; no marker, not a pass. Update-loop dispatch replaced delayCall. |
| Slice 12 | Focused runners passed; unused Composition GPU dependency removed and imported. Aggregate now fails existing `Game.UI.Runtime -> Game.Components` dependency, unchanged from HEAD. |
| Hot-path 01 | Failed existing array snapshot debt: 37 snapshots against ceiling 0. Thresholds unchanged; no Support runtime snapshot was added. |
| Vehicle Fuel regression 01 | Passed 7 tests, wrapper exit 0 and required marker. |
| Native popup 01 | Failed capture-bound check before correct RenderTexture assignment. |
| Native popup 02 | Renderer marker passed but visual inspection failed Persian glyph/static localization; corrected through the existing localized-text boundary. Not accepted as a visual pass. |
| Native popup 03–04 | English/Persian render and visual inspection passed. Captures are prefab-only evidence. |
| Focused Support 01 | Passed Config 1, Runtime 5, Smoke 4, UI/ARIA 5; wrapper exit 0 and all markers present. Includes authored Fuel scope and missing-reserve failure. Repository-wide architecture remains separate and failed. |
| Normal Smoke journey 01 | Failed native modal click-through: raised Canvas lacked a GraphicRaycaster. Source repaired for popup and targeting bar; full log and native screenshots retained. |
| Normal Smoke journey 02 | Failed: targeting bar stretched over the battlefield and intercepted world taps. Fixed reference height; added two-aspect native layout/raycaster assertions. |
| Focused Support 02 | Passed 16 assertions: Config 2, Runtime 5, Smoke 4, UI/ARIA 5; wrapper exit 0 and all required markers. |
| Normal Smoke journey 03 | Failed probe expectation: same declined action cannot be reproposed. Runtime correctly rejected it. Probe now selects a fresh target before asking again and handles normal squad/lesson steps. |
| Normal Smoke journey 04 | Native player/ARIA commits, two Fuel, cover, cooldown, expiry, exhaustion and a real normal-input ARIA victory passed. Overall failed: hidden Support instance reappeared over the result, blocking return; Smoke was used before combat, so no live damage-under-Smoke evidence. Full log and captures retained. |

The first native test exposed an actual input issue absent from pure gateway tests. Both Support UI roots now require their own Canvas/raycaster, and targeting waits for the shell's modal-hide sequence before accepting a fresh world gesture. The test uses actual touch events rather than Button callbacks or direct effect requests. It does not inject health, victory, positions, clocks or Fuel. Explicit setup grants only Smoke and positive test ground knowledge; this is test-encounter evidence, not production Campaign visibility acceptance.

Aircraft tasks remain gated by T07. Existing repository-wide failures are retained; they have not been weakened, reclassified as passes or used to claim production readiness.

## Evidence gates

- Design approval: complete (existing approval).
- Automated checks: 16 focused Support assertions and 7 vehicle Fuel regressions passed; aggregate architecture/hot-path failed on existing repository issues.
- Native visual review: English/Persian prefab renders inspected; normal-input modal repairs still undergoing journey validation.
- Normal-input mission journeys: first run failed and retained; repaired run underway. Both locales and no-Support completion pending.
- Real-player acceptance: pending.
- Device acceptance/performance: pending.

## Native repair after attempt 04

Support hide now deactivates and destroys its registered popup instance instead of retaining it in the popup layer. The journey asserts that a closed popup is gone. Existing smoke Circle emission is rotated onto the ground and its rise speed reduced, preserving source particles. The second charge is now held until real friendly combat damage, using visible Watch/Stop controls and ordinary touch before returning to Support; no health, time, position or outcome is injected. Native rerun pending.

Focused Support run 04 passed all 16 assertions after the native lifecycle/particle repair, wrapper exit 0, all four runner markers plus `[SupportAutomatedValidation] result=Passed`. Full log retained. Focused run 03 failed dispatch during a transient Editor reload connection; it produced no assertions/log and is not a pass. Normal Smoke run 05 is underway in the same Editor.

Normal Smoke attempt 05 passed the native close/reopen guard and observed live DirectFire events 10/11 (12 damage per hit, health 1184 then 1172) under 650-permille cover. Overall failed on a probe assertion that did not permit intentional ARIA takeover/resume. Its normal Start confirmation needed one test-driver stage retry; no gameplay state changed. Full failed log retained. The probe now completes both Watch and Start touches before releasing its device, allows only the intentional resume, rate-limits the squad lesson touch, and captures combat Smoke.

The canonical rifle manifest is `M02EstablishBaseConfigBuilder.ConfigureBarracksProduction`: four instances of `Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab`, GUID `ff73c8b7cbf844778916da330e9cd3e3`. T09 must read the actual authored Barracks config and verify this runtime manifest before deployment; no fabricated squad size.

Attempt 06 (Persian 20:9) reached Engage but failed the test driver's on-screen tap guard: an EditorApplication.update callback reported Screen=1839x1016 while game Canvas pixelRect was 2400x1080, making its legitimate Support center (2021,106) fail the Editor-context raycast bounds. Full failed log retained. The test-only normal touch driver now runs in a MonoBehaviour Update in the player loop; this changes no gameplay authority. Probe assertions now distinguish intentional ARIA takeover/resume and complete the real Start confirmation. The popup context displays the current mission name/objective rather than a troop order. Smoke source particles are cleared before horizontal footprint configuration, preventing prewarmed source-plume particles from surviving reconfiguration.

Canonical Barracks manifest verified in `Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset`: quantity=4, unit GUID `ff73c8b7cbf844778916da330e9cd3e3`.

## Native Persian acceptance — attempt 07

`--reuse --timeout 700 --log /private/tmp/support-smoke-journey-20260928-07.log -- -executeMethod Game.Editor.SupportSmokeJourneyProbe.RunPersian`: wrapper exit 0, `[SupportSmokeJourney] result=Passed` and bridge pass marker. Player-loop context=2400x1080. All Campaign/briefing/squad, open/close/preview/drag, Show/decline/fresh proposal/approve, first expiry/cooldown, actual combat takeover and second use, exhaustion, normal ARIA, real victory/debrief, first-clear settlement and Campaign return passed. Two live DirectFire events under cover were observed (12 damage, health 1166 then 1154). No driver stage correction was needed. Captures in `Evidence/SmokeJourney/20260928-154421-fa-IR-smoke/`; popup RTL, source art and mission context visually inspected.

T07 still needs English and no-Support complete journeys. Native visual tuning preserves the mesh-particle source scale instead of the previously too-small dots; one final English field inspection is pending. Support read models now expose active simulation so an open popup closes on terminal/pause interruption. The English journey intentionally opens Support late in combat and requires its removal before the real result return; no outcome or combat state is injected.

## Native English acceptance — attempt 08

Wrapper exit 0, `[SupportSmokeJourney] result=Passed locale=en smoke=True` and bridge pass marker in `Evidence/support-smoke-journey-20260928-08.log`. Native 1920×1080 journey observed two DirectFire hits under cover (12 damage, health 1166 then 1154), two exact commits, real cooldown/expiry, exhaustion, ARIA, victory, settlement and return. The late popup interruption was removed before result interaction. Captures: `Evidence/SmokeJourney/20260928-155829-en-smoke/`.

One native mouse briefing advance handed control away from the probe touch actuator; its test-only device was restarted. The mid-mission comic correctly closed Support, so the test-only driver needed a normal Support touch to reopen exhausted feedback. Neither action wrote gameplay state. These interventions are recorded in the full log. The driver now handles narrative advances in every phase and normally reopens after comic interruption. Immediate commit screenshots precede particle buildup; a five-second established-effect capture was added for the remaining visual check. No-Support attempt 09 is underway in the same Editor. Aircraft work remains gated by this final T07 check.

Focused automated run 06 passed all 16 assertions and builder markers, wrapper exit 0; full log retained at `Evidence/support-focused-20260928-06.log`. Real-player and device/performance acceptance remain pending, and existing repository architecture failures remain failed.

## T07 normal-input checkpoint and T08 implementation

No-Support attempt 10 passed, wrapper exit 0 and `[SupportSmokeJourney] result=Passed ... smoke=False ... victory=result=settlement=return`; `Evidence/support-smoke-journey-20260928-10.log` and captures in `Evidence/SmokeJourney/20260928-160952-en-without-support/`. No grants, Smoke or injected outcomes were used. Attempt 09 was deliberately aborted to repair the test driver's narrative hit-point selection and ARIA hand ownership; full failed evidence retained. T07's resource/input/consent checkpoint is now passed across English, Persian and no-Support journeys. Established Smoke visual tuning remains a separate T12 native field review item.

T08 implemented: explicit mission-owned target knowledge from authored defense-wave activation; exact target-generation/version consent; mandatory authored entry/release/exit route with known-AA segment checks; bounded ECS presentation flight; physical Fuel held during approach and consumed at release; one 250-damage health write and appended SupportStrike observation; safe pre-release abort with surviving reservations released and one charge restored; cleanup records preserve externally destroyed flight ownership; in-flight receipts are pinned against rejected-input floods. No owned autonomous jet is spawned.

`SupportStrikeValidation` run 01 passed six assertions, wrapper exit 0 and marker, full log retained. This initial run preceded the latest import of cleanup, jet impact-VFX and native journey additions; rerun pending. Source asset capture 01 failed with missing imported capture type; not accepted as visual evidence. The new files have now been explicitly imported into the same Editor. Native player and ARIA Strike journeys are prepared, not yet passed. T08 remains in progress; T09–T11 have not been marked complete or exposed to production.

Strike focused run 02 passed eight assertions, including external flight destruction cleanup and paused reservations, wrapper exit 0 and full required marker; `Evidence/support-strike-focused-20260928-02.log`. Post-Strike Smoke/UI/Fuel/consent focused run 07 passed all 16 assertions, wrapper exit 0, markers and log retained. Source asset capture 02 rendered five assets in a temporary preview scene inside the same Editor, without replacing the working scene or project. Native source images in `Evidence/SourceAssets/20260928-163046/` were inspected: jet GUID `bff0a8791ec564632b7bddbe3e544ac4`, transport `c0213638ba0c6466fadcd5bbe8fe0149`, crate `4a304209bbfe02249bd92db44aa22f94`, parachute `a567fd209adc22643bffbc9030f3005a`, Smoke VFX `2455f9e97989bb34b863456d2c2f67de`. These source renders establish asset suitability, not native flight or collection acceptance. Smoke's gray-background source render is faint; established battlefield visibility remains pending. Native ARIA Strike journey 01 is running in the existing Editor.

## T08 native attempts 01–02 retained

Both failed test-driver timing, not a permissive runtime workaround. Attempt 01 waited for friendly damage and reached victory without selecting a Strike target. Attempt 02 attempted before the authored hostile wave revealed; a read-only live audit showed eligibility visible=0 for every suppressed reserve, correctly refusing selection. The corrected driver waits for positive authored hostile knowledge, stops real ARIA, then targets through normal touch/camera pan. Full failed logs: Evidence/support-strike-journey-20260928-01.log and -02.log. Existing Editor remains reused; enter-play attempt 12 failed because compilation was still active and is retained in /private/tmp/support-enterplay-20260928-12.log.

### Airborne integration audit additions

Verified canonical rifle recipe: `Prefab_BuildingDefinition_Building_Barrack_Config.asset` quantity 4, soldier GUID ff73c8b7cbf844778916da330e9cd3e3 (`Unit_Chr_Soldier_Male_02_Alt_04`). Existing baked transport source c0213638ba0c6466fadcd5bbe8fe0149 resolves soldier parachute a567fd209adc22643bffbc9030f3005a. Ordinary `UnitTransportAirdropSystem` supports narrow reserved-slot integration; preserve 3.4s descent, 0.65s interval, minimum height 12. Its native renderer does not require UnitHealth/Grid on the bounded flight. Infantry classification can use existing UnitMovementBehavior.UsesVehicleMotion plus ground/structure exclusions, without per-frame source-name strings. Queued production summaries must be included in an explicitly authored Campaign cap. Grounding must use MapSurfaceSpawnGrounding and reserve footprints against walkability, dynamic blocked/occupied bits and other Support claims.

Campaign persistence audit: SaveService.NormalizeProfile is the profile normalization boundary. Approved future mission IDs are saga.ch04.m03.split_front, saga.ch04.m04.grounded_signal and saga.ch05.m02.trust_under_fire. Existing first-clear named-grant validation and application must change together; duplicate new Support ownership must remain idempotent. CH04-M02 currently has XP/Credits only; builder and native reward display both require the Smoke grant. Future gameplay assets are not yet authored, so their mission lessons and device acceptance remain pending.

## Smoke Campaign settlement hook (T11 partial, allowed after T07)

Added named Smoke grant whitelist and idempotent application in CampaignMissionProgressStore.Support.cs, completed-Steel-Push-only SupportProfileMigration in SaveService normalization, CH04-M02 mission asset and rebuild recipe third reward, English/Persian reward copy. Four focused cases passed through reused Editor wrapper, exit 0: /private/tmp/support-campaign-focused-20260928-01.log, marker [SupportCampaignValidation] result=Passed tests=4 scope=Smoke later-grants=pending. Duplicate ownership creates no blueprint bonus; legacy unrelated IDs preserved; unavailable/unfinished milestone adds nothing; wrong mission/replay/amount rejected; early replay still hidden. Later grants, native reward visual verification and future lessons remain pending.

T08 attempt 03 retained (real victory before acceptance). The short-lived vanguard was killed by normal combat while selecting; driver now waits for main-armored target >600 health. Strike domain narrowed explicitly to ground vehicles or military structures as approved; infantry disallowed. Catalog/source-version replacement aborts unreleased reservations; ordinary pause retains. Focused run 03 used stale imported eight-case assembly and is retained as such. Focused run 04 failed the new test fixture because its deliberately rejected preview was not recreated before retry; fixture repaired, current nine-case rerun pending.

### Current focused verification after Smoke grant integration

Current Strike runner passed 9 cases (wrapper 0, marker) at Evidence/support-strike-focused-20260928-05.log. Current Support foundation passed 16 cases and rebuilt all four cards/25 policies with productionEnabled=0 (wrapper 0, marker) at Evidence/support-focused-20260928-08.log. Four Smoke Campaign cases remain passed at Evidence/support-campaign-focused-20260928-01.log. Exact CH04-M02 reward card English/Persian rows imported; builder text also updated. This is automated/source evidence; native real reward observation is next. Task ledger now reflects implemented work. T04 remains in progress for explicit parachute/cargo/rope descent exclusions before airborne Support integration. T07 interaction/resource/consent checkpoint passed; established Smoke battlefield appearance remains a separate T12 visual gate.

### Native attempts 04–05 / integration corrections

Attempt 04 failed real bootstrap because Smoke reward display key used Support prefix rather than required mission prefix. Fixed asset/rebuild/localization to mission.reward.smoke_screen. Added actual mission-asset contract regression; five Campaign cases pass (wrapper 0) at Evidence/support-campaign-focused-20260928-02.log. Foundation 16 pass including parachute/cargo/rope exclusions at Evidence/support-focused-20260928-09.log; T04 implemented.

Attempt 05 reached the main armored wave, stopped real ARIA and panned normally. Target tap did not yield valid preview before ordinary combat finished. Pointer adapter used terrain hit XZ, which can lie behind raised models. Reused existing FocusableUnitLookupCameraSystemHelper projected selection-hitbox geometry for pointer Strike identification; authority still revalidates visibility, faction, military domain, costs and knowledge version. Dead/hidden candidates skipped at the presentation picker. Added raised-target pointer regression with deliberately displaced ground ray point and no-spend assertions. Native rerun and current new six-case UI validation remain pending. Failed full logs 04–05 are retained.

### T08 native attempt 06: actual overlay evidence

Six UI/ARIA cases pass, including raised-model pointer selection with displaced terrain hit (wrapper 0; Evidence/support-ui-focused-20260928-01.log). Native 06 failed before preview. Full touch evidence establishes the concrete cause: final target tap at (453.32,582.18) hit SelectedSquadPanel. The driver assumed a fixed left margin and stopped retargeting after a blocked tap. Fixed by checking EventSystem raycast for every proposed world tap, panning normally until no UI overlay blocks it, and retrying a fresh tap while still targeting. Screen-hitbox production picker improvement remains tested, but the native failure itself is attributed to this observed driver hit, not an inferred terrain hit. Full failed run retained at Evidence/support-strike-journey-20260928-06.log.

### T08 native attempt 07 — actual baked visual integration failure

The corrected normal pointer selected the visible Battle Tank, but preview returned NotReady. Live registry inspection confirmed that Jet 01, Jet 02 and the transport use UnitDetailedVisualReference, not detachable UnitModelPrefabReference. Retained failed journey 20260928-171837-en-strike-aria and full log support-strike-journey-20260928-07.log. The adapter now clones the actual linked baked hierarchy, preserving remapped visual references and removing all root gameplay components before reservation. Added an embedded-hierarchy isolation/cleanup regression. Native acceptance remains pending; no production flag is enabled.

Strike focused run 06 passed ten assertions with wrapper exit 0 and required marker (`Evidence/support-strike-focused-20260928-06.log`). Embedded source visual references map to the cloned linked hierarchy, gameplay root components are removed, abort destroys cloned visuals while preserving source prefabs and releases Fuel once. Native ARIA journey 08 is running in the same Editor; actual visibility remains a separate gate.

T08 native ARIA journey 08 passed interaction/combat/result/settlement/return with wrapper exit 0, one SupportStrike observation (250 damage, health after 640), four physical Fuel consumed and one exhausted charge; `Evidence/support-strike-journey-20260928-08.log`, captures `SmokeJourney/20260928-173222-en-strike-aria/`. The aircraft image shows a shadow but no clear jet, so aircraft visibility remains pending. UI inspection found consent copy still naming Smoke/one Fuel for other abilities; it is being corrected to actual selected ability/cost before final acceptance. Added non-interactive flight status and a settlement ownership assertion for the next run. No production readiness claim.

T08 native player journey 09 passed full normal-input combat/result/settlement/return, wrapper exit 0 and marker, one 250 damage observation and exactly four Fuel. The new assertion verified Smoke ownership after the real first-clear settlement. Evidence `SmokeJourney/20260928-174342-en-strike-player/` and `Evidence/support-strike-journey-20260928-09.log`. UI focused run 02 passed seven tests including read-only reserved Fuel/status. Native aircraft is still out of frame because the test route was anchored at the distant relay (release projects x=3616 on a 1920 viewport); it is now authored at the mission main_spawn convoy anchor. The tiny flight banner is being corrected with the existing responsive section layout. These are separate failed visual evidence; T08 remains in progress until the corrected capture and ARIA consent run pass.

### Additional bounded airborne/collection audit

Existing `UnitTransportPassengerStateSystem.BoardPassenger` calls `UnitTransportVisualUtility.SetPassengerHidden` and preserves visual-scale restoration records; use that helper rather than only disabling a canonical soldier root (which would leave child visuals visible). Existing CargoDrop descent can animate the separate plain Materials crate without UnitHealth/Grid/Footprint: FinishDrop removes the descent and schedules canopy cleanup, and no Footprint means no vehicle settling. Normal collection order replacement must clear Support collection ownership in both `UnitMoveOrderSystem.IssueGroupedManualMoveOrder` and its central ClearMovementOrderComponents path; grouped manual movement has its own removal list. Queued population summaries contain historical ProducedCount (do not count it) and current QueuedCount; configured unit/production source rows identify infantry. The canonical production recipe is `BuildingDefinitionAuthoringConfig.Productions`. Future Campaign policy population ceilings remain explicitly unauthored (zero means NotReady), with a positive ceiling authored only for an acceptance encounter until future mission content exists.

### T08 complete; T09 started

Corrected native ARIA journey 10 passed wrapper exit 0 and required marker: `Evidence/support-strike-journey-20260928-10.log`, `SmokeJourney/20260928-175347-en-strike-aria/`. Inspected actual baked jet crossing the battlefield in aircraft/impact images (partly occluded by the existing selection/minimap HUD), legible four-Fuel reservation banner, exact Precision Strike/four-Fuel consent, one 250-damage observation (health after 280), exhausted charge, actual victory/result/settlement/return, and `[SupportNativeCampaign] firstClear=settled smokeUnlock=owned`. T08 mechanics/normal-input player and ARIA gates passed; broader unobstructed composition, Persian airborne presentation and device/player gates remain T12 pending. Begin T09 canonical four-rifle manifest, reserved population/grounded slots and existing descent integration.

### T09 automated slice, 2026-09-28 18:20 UTC
Actual Barracks manifest (four canonical rifle soldiers) and existing transport/parachute prefabs now bake through UnitPrefabRegistry. Population ceiling must be explicitly authored; absent ceiling is NotReady. Preflight reserves disabled real infantry and distinct grounded landing footprints. Existing transport boarding, descent and settle owners remain authoritative. Release rechecks population, all held slots, surface grounding and safe route; first release consumes six Fuel, later failure preserves deployed soldiers without refund. Lifecycle control becomes ordinary only after settle. Seven focused tests passed; all 88 ordinary transport regression tests passed. Native touch journey remains in progress; production readiness stays disabled. Logs: support-paratrooper-focused-20260928-01.log, support-transport-regression-20260928-01.log, support-ui-build-20260928-07.log.

### T09 native normal-input acceptance, 2026-09-28 18:26 UTC
PASS: support-paratrooper-journey-20260928-01.log, exit0, evidence SmokeJourney/20260928-182010-en-paratroopers. Full touch Campaign→brief/story→Engage→Support→six-Fuel reservation→actual transport/descent→four grounded ordinary friendly soldiers→touch selection and ordinary movement >2m→ARIA normal combat→Victory→real Smoke ownership settlement→Campaign return. Native visual composition is a separate pending T12 gate: close camera puts most aircraft/canopies outside capture, though actual model rendering is present. No claimed real-player/device acceptance. T09 mechanic/normal-input gate passed; T10 begins.

### T10 Supply runtime and focused checks, 2026-09-28 18:39 UTC
Eight tests PASS in focused logs 01 and 02 (exit0). One separate stock-bearing crate reserves landing space and three physical Fuel, remains hidden before actual release, then uses ordinary cargo descent and canopy cleanup. Explicit selected-ground-unit collection enters a typed ECS request, normal move owner and serialized claim. Grouped/immediate/target-only/clear movement removes collection ownership, including same-goal replacement. Transfer through TryGrant(Reward) yields 590/600→600 and30 stock, full storage does not mutate version, later available capacity consumes only remainder. Enemy/dead/air/wrong-attempt rejection, duplicate collector, source-flight deletion and attempt cleanup passed. Normal-input production/collection/full-mission gate remains in progress; no device/performance acceptance claimed.

### T10 native attempt 01 — failed, retained
Actual transport→crate/canopy descent→grounded40Materials→normal infantry production spend→selected tank→explicit Collect touch and normal path all ran. Collection did NOT pass: normal path fallback stopped tank2.79m from crate (goal753,424, actual752,423), correctly preserving all40stock/noGrant. Deliberately ended probe with failure and wrapper exit1 before claiming a mission pass. Repair uses canonical path placement/surface eligibility for interaction cells, prefers crate centre, rejects a path fallback outside2m without stock mutation. Also replacing Paratrooper Campaign combat suppression with source-only transit suppression to retain ordinary descending target vulnerability.


### Supply interaction repair and shared transport regression
- Native Supply attempt 01 remains failed evidence: the selected tank finished a normal path fallback approximately 2.8 m from the crate and transferred no Materials.
- Collection now uses ordinary placement and surface-footprint checks, prefers the crate centre, and validates the actual final path cell. A fallback outside 2 m cancels the collection order and reports LandingBlocked without changing stock/resources. Segmented intermediate paths remain ordinary movement.
- An arrived collector retains its claim when storage is full beyond the movement timeout; accepted capacity-limited transfer releases the claim and retains remaining crate stock.
- SupportSupplyValidation attempt 03 failed because the existing Editor was still compiling. Attempt 04 passed all 9 checks; full log and exit receipt retained.
- SupportParatrooperValidation attempt 02 passed all 7 checks after replacing the unintended combat-suppression tag with an attacker-only transit tag. Descending infantry remains targetable and cannot attack/select/move until grounded.
- Transport regression attempt 02 called an incorrect method and failed; attempt 03 used the verified RunBatchValidation entry point and passed all 88 checks. Both logs retained.
- Support UI builder 09 passed. Native Supply attempt 02 is running in the same existing project/Editor; no new Editor was launched.


### Native Supply attempt 02: correct capacity transfer, failed test expectation
The real soldier reached the landed crate through ordinary pathfinding. Normal production bought one 20-Materials unit, leaving 180/200; collection granted 20 Materials and retained 20 in the crate at 200/200. The test incorrectly expected all 40 after making only 20 slots. This is retained as failed journey evidence, not counted as a completed native journey. The test now buys enough ordinary production to make 40 slots before collection. No resource, position, health or outcome injection was used.

### Campaign hooks implementation
Added the four exact first-clear reward/source/amount whitelist pairs, set-like ownership application without blueprint conversion, and versioned profile normalization derived only from completed milestones. Added the approved optional lesson schedule and EN/FA copy; the existing Close action skips it without spending or adding objectives. Added an optional SupportMissionContext authoring/config/baker plus an unmanaged projection for exact mission/map/source-version ground knowledge, route and population ceiling. Missing or ambiguous contexts clear the projection and fail closed. Future canonical mission content remains unavailable; no replacement mission was authored and production exposure remains gated.


### Native Supply attempt 03: invalid second-production precondition, retained failure
The real Barracks consumed 20 Materials for the first production. Its item then reported “Barracks production slots are full” and the queue stayed at 100%. Reopening the automatically closed Build drawer, closing it, and completing an ordinary guided Move were all performed through real touch input; no second production was accepted. The driver was ended as failed before any outcome claim. The native validation now checks min(40, free capacity), exact retained stock and the full-storage popup, which is the specified Supply behavior. Focused tests already cover subsequent remainder collection and crate depletion. This does not weaken the runtime collection contract or claim a 40-Materials native transfer when only twenty slots are available.

### Supply native run 04 and final cleanup audit

Run 04 retained as failed evidence: normal Supply delivery consumed exactly 3 Fuel; one ordinary rifle purchase freed 20 Materials; selected infantry collected 20, leaving 20 in the real crate at 200/200 storage with no active claim. The native driver opened its late interruption popup before starting ARIA because the attempt had exceeded 80 seconds. A supplemental normal Play gesture then hit ARIA's modal blocker. This is not full journey acceptance. Corrected the probe to require active ARIA and prior actions before the late interruption.

Added cleanup ownership at real Support passenger release. The existing airborne code registers the deferred canopy entity through its ECB, so a passenger destroyed during descent retains cleanup ownership; the lifecycle removes the separate canopy and cleanup entity without refunding already spent Fuel. Held passengers remain ordinary immediate cleanup. Added a focused death-during-descent regression.

### Latest focused regression evidence

`support-paratrooper-focused-20260928-08.log`: PASS, 9 assertions/cases including released passenger death cleanup; wrapper 0 and exit receipt 0. `support-supply-focused-20260928-05.log`: PASS, 9; transport regression 04 PASS, 88; move regression 01 PASS, 21; path regression 01 PASS, 3. All four ran sequentially in the existing GUI Editor with full logs/receipts archived.

Architecture boundary 02 fails on the existing Game.UI.Runtime → Game.Components reference. Hotpath 02 reports 45 snapshots against ceiling 0: the 37 baseline plus 8 Support helper snapshots (4 landing/population and 4 supply request/path placement). Do not describe all snapshot findings as baseline; these helpers need further allocation/performance work before production acceptance. No ceiling was raised. Classification 01 reports nine baseline systems plus SupportCatalogDisposalSystem; the local disposal annotation will be corrected after the active journey.

The wrapper's Hub process check fails under the filesystem/process sandbox even while Hub is running. Host permission for the checked wrapper resolves this; it is not a licensing fault. Same Editor PID 98258 was preserved through domain reload, which restarted Pipeline on port 7800.

### T10 native Supply acceptance

`support-supply-journey-20260928-05.log`: PASS, wrapper 0, receipt 0. Full normal-input journey in `Evidence/SmokeJourney/20260928-193935-en-supply`: 3 Fuel consumed, actual canopy/crate descent, 40 landed Materials, ordinary Barracks purchase frees 20 slots, infantry selection → Collect → real pathfinding → exactly 20 accepted/20 remain at 200/200 with no active claim; ARIA normal gestures, actual Victory, settlement, permanent Smoke first-clear unlock and Campaign return. Failed runs 01–04 remain archived. Full remainder/depletion and capacity 590/600 are focused assertions, not claimed as native 40-Materials collection.

Native field review found the full-scale transport filling the close camera view and obscuring descent. Added a 0.25 presentation-only scale for Support transports, preserved by flight movement; jet scale, logical route, landing slots, payload scale and timing remain unchanged. Final native captures pending. Removed repeated Supply landing-plan snapshots from approach frames: positive landing/resource/route validation remains at actual release before Fuel consumption. Disposal system now annotates its pure callbacks for Burst.

### Support snapshot debt correction

Removed all eight new ToEntityArray/ToComponentDataArray sites without altering architecture ceilings. Landing reservations and population read component storage directly through chunks. Supply store resolution reads chunks; collection builds only the occupants intersecting the nine candidates' padded bounds, passing those bounded native views to the existing placement validator. Supply approach no longer invokes landing snapshots each frame. These are request/release boundaries; allocation/profile acceptance still requires measurement. Focused/path/native revalidation pending below.

### Final focused rerun and wrapper handoff

Paratrooper 09 assertions passed but CLI handoff timed out, so it was retained as failed wrapper evidence. The checked wrapper now schedules the Editor callback two seconds after the CLI returns, avoiding assertion work racing the Pipeline response; it retains the same existing Editor, log, timeout and fail-closed exit receipt requirements. Paratrooper 10 passes all 9 cases with wrapper/receipt 0. Final Supply 06 passes 9, Strike 08 passes 10, transport regression 05 passes 88.

Architecture hotpath 03 now reports 37 snapshots (unchanged baseline) against ceiling 0; classification 02 lists only the nine pre-existing systems and no Support file. The assembly boundary is confirmed pre-existing in HEAD's Game.UI.Runtime.asmdef. These repository-wide failures remain preserved, with no raised ceiling or softened assertion.

Host `adb devices -l` succeeded and returned no attached devices. Android/player/profiler acceptance remains pending rather than claimed.

### Paratrooper native run 02 retained as failure

Final current-code deployment passed all four actual descents, Fuel=6, friendly live landing and ordinary move >=2 m. The aircraft now fits the view, but the legacy unscaled door offset places early descents too far behind the scaled transport. Added a Support-only multiplication of the source door offset by the flight scale, with an assertion against the actual drop start; ordinary transport remains untouched. Capture now observes established descent rather than the first release frame.

The run then failed ARIA's real preparation loop before Victory. No `support-before-result` screenshot exists, so an opened late popup is not established as the cause (the earlier live commentary inferred that too strongly). The driver now waits for the reinforcement's complete ordinary move, returns normal selection to the guided front tank through real touch, and allows the late interruption only during ARIA Waiting after the actual 0x1FF preparation acknowledgements. This preserves the failed native evidence; normal mission completion must pass a rerun.

### Native driver ownership and release-anchor final verification

The probe's second touch actuator could consume ARIA touches while preparation was still active. Late interruption now requires real preparation acknowledgements and ARIA Waiting, records intentional normal player takeover, retries the real Support button, and requires the actual popup before capturing interruption evidence. It never marks a queued-but-consumed touch as a visible popup. Paratrooper move verification waits for complete normal movement and normal tank reselection before ARIA.

Paratrooper 12 PASS 9 with the scaled source-door assertion; ordinary transport 07 PASS 88; Fuel 03 PASS 7, each full log and receipt 0. The wrapper handoff delay is now 10 seconds to keep cold assertion startup out of the Pipeline response deadline. This changes no validation assertions, timeout, pass marker, or Editor/project ownership. Earlier handoff failures are retained.

Current Paratrooper run 03 captures in `SmokeJourney/20260928-201007-en-paratroopers`: transport recognizable; established canopies visibly descend over the landing area; normal soldier move screenshot exists. Full mission still running at this entry.

### T09 final native acceptance

`support-paratrooper-journey-20260928-03.log`: PASS with required native marker, wrapper 0 and receipt 0. Captures: `SmokeJourney/20260928-201007-en-paratroopers`. Current code: 0.25 source transport presentation, scaled door release point, established source canopies visible in the landing area, four live canonical rifle soldiers, exactly 6 Fuel, ordinary touch selection/movement >=2 m, complete normal movement and front-tank reselection, ARIA real preparation/combat, intentional normal player takeover and *actual* Support popup visible, actual Victory, first-clear settlement/Smoke unlock and Campaign return. Failed run 02 is retained; no outcome or position injection was used.

### T10 final native acceptance

`support-supply-journey-20260928-06.log`: PASS, wrapper 0 and receipt 0; captures `SmokeJourney/20260928-201537-en-supply`. Current scaled source transport, actual source canopy and plain crate are visible. The first-release canopy image is cropped high; the landed capture shows the source canopy and crate in the field. Exactly 3 Fuel, 40 stock, ordinary Barracks purchase, actual infantry collection, 20 accepted and 20 retained at full storage; no account resource grant. Real ARIA preparation/combat, intentional player takeover, actual Support popup interruption, Victory, settlement and Campaign return pass. Earlier failed journeys remain retained.

Smoke restart attempts using numbered 09/10 log filenames were rejected by the wrapper's evidence-preservation guard before validation began because those files already existed. The fresh `/private/tmp/support-smoke-final-20260928-2023.log` runs in the same already-playing Editor. No second Editor/project was opened.

### Final Smoke / T12 available evidence completion

`support-smoke-final-20260928-2023.log`: PASS, wrapper 0 and receipt 0. Native captures `SmokeJourney/20260928-202247-en-smoke/`: established source Smoke visible at five seconds, real cover and two mitigated damage observations, exact two one-Fuel commits, real cooldown/expiry, no-spend preview/drag/Show Target/Decline, exhausted feedback, actual ARIA combat, Victory, first-clear settlement/owned Smoke and Campaign return. No gameplay outcome/resource/health/position/time injection. Source and visual catalog hashes remain unchanged from the recorded canonical values.

T00–T10 completed. T11 feature hooks/migration/reward implemented; future mission content/use/skip journeys explicitly pending. T12 available native/evidence/handoff complete; real player, connected Android/mobile responsiveness and recurring allocation profiler acceptance remain pending. Production flags remain false. Final `git diff --check` passes. All failed evidence and baseline architecture failures are retained.

### Commit review localization preservation repair

The final diff review caught an unintended earlier serialized localization edit: unrelated Persian entries were missing and the Smoke reward key was duplicated. Reconstructed both locale sections from HEAD, overlaid only Support keys and the intended Steel Push reward label. All 3202 original unique entries per locale survive byte-for-byte except that intentional label; both locales now contain 3274 unique matching keys and exactly one Smoke reward key. Preservation evidence: `Evidence/support-localization-preservation-20260928.log`. Reimported in the same existing Editor. Native popup/lesson repair validation passes ten EN/FA captures, wrapper and receipt 0 in `support-popup-commit-repair-20260928-2044.log`; no truncation. Gameplay code and complete native mission evidence remain unchanged.
