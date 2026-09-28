# Ordered implementation tasks

Approval recorded: 2026-09-28. Overall state: FEATURE IMPLEMENTED; available native evidence complete; external readiness gates pending. Current evidence and failures are recorded in `implementation_progress.md`; production/device readiness is not yet established. Do not equate task planning, mockup approval or passing compilation with player readiness.

Read [IMPLEMENTATION.md](IMPLEMENTATION.md) for exact contracts and verified code paths. The old S0–S8 product packets remain useful grouping; the tasks below are the executable work order. Work sequentially unless the owner explicitly assigns separate agents. Keep one owner for shared enums, dispatch, catalog and settlement files.

## Task ledger

| ID | Status | Depends on | Deliverable |
|---|---|---|---|
| T00 | DONE | None | Baseline and resolved integration inventory |
| T01 | DONE | T00 | Canonical design/config synchronization and importer |
| T02 | DONE | T01 | Attempt state, target validation, requests and receipts |
| T03 | DONE | T02 | Atomic Fuel reservation and charge lifecycle |
| T04 | DONE | T03 | Smoke effect and real damage integration |
| T05 | DONE | T04 | Native full-screen popup and battlefield targeting |
| T06 | DONE | T05 | ARIA exact-action consent |
| T07 | DONE | T06 | Smoke normal-input vertical-slice acceptance |
| T08 | DONE | T07 | Precision Strike and aircraft presentation |
| T09 | DONE | T08 | Paratroopers using existing airborne runtime |
| T10 | DONE | T09 | Supply crate delivery and explicit collection |
| T11 | IMPLEMENTED (future content pending) | T07–T10 | Campaign grants, migration and mission exposure |
| T12 | EVIDENCE/HANDOFF COMPLETE (external gates pending) | T11 | Native visual, full mission and device evidence |

T11 may integrate Smoke after T07 while later abilities are built, but each later ability stays production-disabled until its own acceptance passes. A later task cannot be marked done by disabling its feature.

## T00 — bounded baseline audit

1. Read root AGENTS.md, git status and current diff. Record baseline commit, dirty paths and selected worktree in `implementation_progress.md`; preserve unrelated work. Do not make a new checkout without accounting for the active work.
2. Confirm symbols in IMPLEMENTATION section 2. Read architecture sections for ECS, assembly dependencies, UI edges, transport and performance.
3. Record exact rifle prefab/count from campaign production configuration, actual campaign target-reveal owner, mission resume/restart behavior, UI prefab registration owner, and collection move-request interface. These are bounded integration checks, not an invitation to redesign.
4. Verify payload crate and parachute prefab suitability through native inspection. Record existing GUIDs, never invent them. Correctly rendered existing portraits may be reused; defective green-screen references cannot establish model fidelity.
5. Trace Build popup pause behavior from UiShellFlowSystem and MissionDefensePauseSystem, then retain it for Support. Current code excludes Build from pause whitelist. Confirm later with native interaction.

Exit: no unresolved guessed API in the first Smoke slice. Later airborne-specific gaps can be tracked under T09 without blocking Smoke. If campaign reveal has no suitable API, implement the narrow positive-eligibility projection described in section 8; do not use permissive visibility defaults or ask the owner to choose class names.

## T01 — definitions and generated assets

- Merge `support_defaults.json` values into the authoritative balance catalog and synchronize visual catalog, mission Support rows, chapter rewards, feature matrix and Steel Push plan. Mark runtime maturity Designed until proven, even after design approval.
- Add typed Support configs/authoring and the Editor importer from section 3. Imported runtime assets reference existing prefabs/sprites; runtime reads no Design JSON.
- Add all 25 mission Support policies: hidden through CH04-M02 gameplay; Smoke from M03, Strike from M04, Paratroopers from M05, Supply from CH05-M03. Remove obsolete campaign grants for deferred Support while retaining unrelated mode definitions.
- Add production-readiness flag default false for all abilities; test-scene grants are explicit. Only enable through acceptance gates.
- Confirm all unrelated balance entries and mission rewards survive the diff. No broad reformat of the large catalogs.

Tests: duplicate/missing ID, invalid radius/NaN/cost, asset missing, unsupported mode, wrong unlock order, new-finale-mechanic rejection; import hash matches generated config. Runtime build can boot without Design directory.

## T02 — ECS contracts and validation

- Implement session/ability/request/receipt/rejection types and startup from sections 3–6.
- Project profile ownership once at startup, combine with mission policy, and default absent target eligibility to false.
- Use attempt identity on all transient state. Add bounded deduplication and version handling.
- Implement pure preview validation with no resource or world mutation. Add an explicit target-projection boundary; no visibility inference from renderer/selection.
- Clock advances only during active simulation. Establish system update order and lifecycle cleanup.

Tests: locked/restricted/missing-policy, stale attempt/entity, out-of-bounds, hidden/unconfirmed/protected, request replay and paused/result confirm. Assert complete unchanged snapshots on every rejection.

## T03 — Fuel and charge transaction

- Use physical storage reservation helpers and civilian floor. Do not use FactionEconomy.Fuel or account wallet as fallback.
- Build contribution plan, validate all copies, commit all or none. Correlate reservation records with request/attempt.
- On Smoke resolution consume immediately; on flight approach reserve then consume at release; abort releases surviving reservations and restores charge once.
- Preserve destroyed-depot loss rather than minting a refund. No extra ordinary aircraft movement Fuel cost for presentation flights.
- UI availability must account for held outbound reservations.

Tests: exact cost, shortage, multiple stores, civilian floor, contention with movement/production, duplicate confirm, abort twice, source destroyed, shutdown, no negative stock and no partial mutation. See numeric vectors in IMPLEMENTATION section 12.

## T04 — working Smoke

- Spawn fixed 12m radius, 15s smoke with 650-permille incoming direct-ranged modifier.
- Publish derived target cover before direct combat, reset on exit/expiry, do not stack.
- Patch both UnitAttackSystem damage contribution branches and prediction; patch BuildingDefenseAttackSystem. Keep damage observation and death behavior intact.
- Use existing smoke visuals through approved rendering/baked ECS boundary; never use visible smoke as substitute for changed damage.
- Create an isolated authored test encounter with canonical units and a known protected target; no production mission outcome changes.

Tests: 100→65, 10→7, 1→1, both factions, overlapping zones, expiry/exit, air/building/missile exclusions, two shots vs one aggregated write, real health/observation consistency.

## T05 — native UI and input

- Build SupportPopup prefab using approved v02 layout and exact existing artwork. Card grid, detail panel, mission context, resources, large next-step action, close/back and ARIA Play/Stop.
- Add shell registration/contracts/read model/gateway. Remove Support→Scan alias in both input binding and dispatch. Preserve all Scan paths/restrictions.
- Implement catalog→targeting→preview→confirm/cancel; cost is shown before commitment. Close popup before world targeting. Consume modal pointer events and block click-through.
- Localize all visible strings through existing English/Persian pipeline. Add no prototype labels or debug panels.
- Preview geometry/candidate identity is data-driven, invalid state names a reason, no spend on camera pan, taps or card selection.

Tests: replay listener duplication, pointer drag vs tap, close-button world click, modal conflict with Build, pause/settings/comic/result interruptions, Scan before/after Support unlock, safe areas and RTL screenshots.

## T06 — ARIA

- Add exact proposal fields and append intent enum values; same request pipeline as player.
- Implement ShowMe/ShowTarget as camera/highlight only. Approval is one action, one target, one token, one cost.
- Reject expiry/stale target/cost/catalog/version; never infer consent from Play. Decline and Stop are no-spend.
- Cancel proposal on override/exit, preserve committed-action truth. Do not try to undo a launched attack with Stop.

Tests: approve once, double approve, decline, Stop, 15-second expiry, target change, unrelated UI changes do not invalidate consent, ARIA Play never spends, no MoveOrder created by ShowMe.

## T07 — first runnable acceptance checkpoint

Run the authored encounter through normal mouse/touch input: open, read, cancel, deploy Smoke, watch effect, wait cooldown, second use, exhausted feedback, ARIA approve and decline, finish/return. Capture native 16:9 English and 20:9 Persian; compare assets to approved source art. Perform a no-Support win.

Exit: automated tests AND native interaction evidence pass. Record failed attempts. Do not proceed to new aircraft implementation while this first slice has unresolved resource, input, or consent defects. Production campaign exposure waits for T11 integration.

## T08 — Strike

- Reuse shared transaction and UI; single-target 250 damage at 3s approach.
- Add positive target eligibility projection, stand-off route validation, impact revalidation and exact abort behavior.
- Use Unit_Veh_Jet_01 visuals, no A-10 or rocket salvo, no autonomous selectable jet or duplicate normal attack.
- Append SupportStrike observation type; health/death/result continue through existing owners.

Tests: actual health change once, target lost/hidden/dead/protected, source lost, safe route vs AA target, no area damage, no forced objective completion; normal-input player and ARIA uses.

## T09 — Paratroopers

- Resolve actual manifest/count and bake transport/parachute/infantry assets. Use Unit_Veh_Plane_Transport.
- Reserve Fuel, population and all landing slots. Implement flight/approach, existing descent and release/settle ownership rules.
- Add narrow preflight/failure/reserved-slot integration to UnitTransportAirdropSystem without copying it or breaking ordinary transport behavior.
- Distinguish pre-release abort from partial delivery after first release; no refund/duplication after real deployment.

Tests: correct number/prefabs/faction/stats, landing collision, population including queued/reserved units, missing visual, route lost, ordinary owned transport board/drop regression. Normal input: call → plane → parachutes → landed controllable squad → ordinary move/attack.

## T10 — Supply

- Flight uses same transport but separate crate state. Payload is 40 Materials only, one crate.
- Add normal explicit collect interaction and move request, cancelled by replacement order.
- Transfer min(remaining, free capacity) through TryGrant, retaining uncollected stock. One serialized claim owner.
- Show remaining stock/full storage, cleanup at depletion/attempt end, no account settlement.

Tests: 590/600 +40 →600 and30 remains, full storage, two collectors, dead/enemy collector, new move override, crate not reachable, duplicate flight/collection, no Fuel/Oil/credits grant. Normal input observes plane, crate descent and collection.

## T11 — campaign and persistence

- Add whitelist and grant application to CampaignMissionProgressStore.Support.cs, plus profile migration. Do not convert duplicate Support ownership into blueprint rewards.
- Integrate Smoke reward after CH04-M02 and skippable lesson in CH04-M03. Add later rewards/lessons per approved schedule only after T08/T09/T10 gates.
- Canonical future mission content may still be unfinished. Implement feature hooks and keep its readiness explicitly pending; do not author whole replacement missions to claim completion of Support.
- Replays hide abilities in early contexts; fresh attempts reset charges; migration preserves legacy IDs. Support ownership is permanent, tactical stock is not.
- Implement only the existing supported restart/resume path. If resume exists, add stable-ID Support restore and receipts before enabling that path; no raw Entity save handles.

Tests: first-clear/duplicate reward, old profile migration, early replay, retry, quit/relaunch behavior and actual result/reward/Campaign return.

## T12 — readiness evidence and handoff

Keep six independent statuses: approved design, native visual review, automated checks, complete normal-input journeys, real-player acceptance, device acceptance. Report each independently in IMPLEMENTED_FEATURE_HANDOFF.md; the original planning baseline is not the current acceptance status. For every production-exposed mission collect both Support and no-Support completion, ARIA rejection, results/reward and return. No injected outcome, old candidate capture or compile-only substitute.

Test on the normal target device and measure UI/input responsiveness plus allocations. Architecture contract targets 0 B/frame recurring managed allocation after warmup; no per-frame LINQ/snapshot arrays. Don't claim measured performance without profiler evidence. If future missions or real devices are unavailable, report those gates pending with exact missing evidence, not “ready”.

## Validation runners and command contract

Create these **new** static runners under namespace `Game.Tests.Editor` in the Support test folder; methods must execute assertions and emit the marker only after all assertions finish. The implemented runners and current evidence are indexed in Evidence/VALIDATION_INDEX.md.

| Runner method to implement | Required log marker |
|---|---|
| SupportConfigValidation.RunFocusedValidation | `[SupportConfigValidation] result=Passed` |
| SupportRuntimeValidation.RunFocusedValidation | `[SupportRuntimeValidation] result=Passed` |
| SupportSmokeValidation.RunFocusedValidation | `[SupportSmokeValidation] result=Passed` |
| SupportUiAriaValidation.RunFocusedValidation | `[SupportUiAriaValidation] result=Passed` |
| SupportStrikeValidation.RunFocusedValidation | `[SupportStrikeValidation] result=Passed` |
| SupportParatrooperValidation.RunFocusedValidation | `[SupportParatrooperValidation] result=Passed` |
| SupportSupplyValidation.RunFocusedValidation | `[SupportSupplyValidation] result=Passed` |
| SupportCampaignValidation.RunFocusedValidation | `[SupportCampaignValidation] result=Passed` |

Use the repository's existing ValidationExit/assertion pattern. Verify test assembly symbols before running. On failure emit result=Failed with case identity and exit nonzero; never print pass in finally. Add tests for the behavior above, not screenshots of a successful log message.

After a new runner exists, run it through the required wrapper, for example:

```bash
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 600 \
  --log /private/tmp/support-smoke-T04.log -- \
  -quit -executeMethod Game.Tests.Editor.SupportSmokeValidation.RunFocusedValidation
rtk proxy rg -F '[SupportSmokeValidation] result=Passed' /private/tmp/support-smoke-T04.log
```

Both wrapper exit 0 and full-log pass marker are required. Use unique candidate/task log filenames. Archive logs under this package's Evidence directory after each run. Never pipe the wrapper through a lossy filter, bypass it, add macOS batchmode, terminate unrelated Unity processes or reset licensing IPC. Keep Hub open/signed in. A normal project-lock or timeout is failed validation, not license permission to switch routes. Check root AGENTS.md before every Unity lane; read unity-cli skill before live CLI operations.

Existing regression entry points inspected (global classes, no Game.Tests.Editor prefix):

- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` → `[ScriptArchitectureBoundaryValidation] result=Passed`
- `EcsBurstHotPathArchitectureTests.RunFocusedValidation` → `[EcsBurstHotPathArchitectureValidation] result=Passed`
- `VehicleFuelConsumptionSystemTests.RunFocusedValidation` → `[VehicleFuelConsumptionFocusedValidation] result=Passed`
- `UnitTransportValidationTests.RunBatchValidation` → `[UnitTransportValidation] result=Passed`

Run only relevant checks at each step: architecture for new runtime/assembly work; Fuel when touching reservations; transport when touching airborne paths. Run all relevant checks once at integration. Check current signatures before execution; these names are evidence from inspection, not proof they pass on this dirty tree.

## Required progress report after each task

Create/update `implementation_progress.md` with: task ID/status, exact files changed, source/config revisions, concrete behavior now working, command/log/marker and exit code, native capture paths, failures retained, known limitations, and next task. Name new APIs distinctly from reused ones. No need to ask owner to approve each task. Do not mark DONE with a known failed relevant test. If the assigned scope is only one task, leave a precise continuation note; if assigned the whole feature, continue in order.


## Cross-mode roadmap extension — 2026-09-28

The owner additionally requested Support planning for future Campaign missions, all 120 Skirmish battles and all 60 Operations. Follow [Roadmap/Support/PLAN.md](../../Roadmap/Support/PLAN.md) and its complete per-entry policy CSVs. Campaign remains the first implementation slice; X01–X05 then add separately validated mode adapters and coverage. This is authorization for the roadmap scope, not runtime acceptance or permission to expose old catalog availableModes without validation.
