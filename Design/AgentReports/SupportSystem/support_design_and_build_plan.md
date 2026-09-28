# Support: design proposal and agent implementation plan

Date: 2026-09-28

Status: owner approved the four-ability scope and project-asset-based full-screen visual direction on 2026-09-28. Ready for implementation; no runtime readiness is claimed. Follow [IMPLEMENTATION.md](IMPLEMENTATION.md) and [TASKS.md](TASKS.md) for technical contracts and executable work order. Canonical config synchronization is T01; this document does not itself change runtime, prefabs or saves.

## Decision requested

Approve a four-ability campaign roster delivered in stages: Smoke Screen, Precision Strike, Paratrooper Reinforcements, and Supply Drop. Build and validate Smoke Screen end to end first; do not attempt all four before proving the foundation. Use a full-screen Support selection popup matching the existing Build popup, with separate Scan and Support commands on the battlefield. All abilities are earned, optional, and never sold. ARIA can propose but cannot spend a charge without explicit consent for that exact action.

The two initial abilities are a first implementation slice, not the final roster. Precision Strike is an attack aircraft making one strike. A transport aircraft carries troops or supplies; it is not a bomber. A wider bombing run is a possible later, separate ability, not part of the approved scope requested here.

## Evidence and boundaries

Inspected on this working tree:

- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.CommandTabs.cs` treats `SupportCommand` as a Scan alias.
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestDispatchSystemHelper.cs` routes `Support` and `RightSupport` to `EnterScanTargetMode`.
- `Assets/Game/Scripts/Components/AssistantComponents.cs` has no Support proposal or execution intent. Existing enum values must retain their serialized meaning.
- No `class SupportAbilityService` was found in `Assets/Game/Scripts`. The design names a planned owner; that name is not evidence of a runtime.
- The combat balance JSON already defines Smoke Screen (2 charges, 1 Fuel, 35s cooldown), Precision Strike (1 charge, 4 Fuel, 120s), and Supply Drop (1 charge, 3 Fuel, 100s). It does not establish that any is playable.
- The mission catalog describes player-controlled parachute personnel and vehicle cargo drops in CH04-M04. Its runtime readiness is a separate verification gate; the current task has not revalidated it.
- The workspace contains unrelated ongoing changes. Agents must inspect the current diff and preserve other work.

## Recommended abilities

All numbers below are proposed starting values, not playtested balance. Existing catalog costs/cooldowns are retained where specified; new effect parameters are intentionally explicit for a first build.

| Ability / proposed ID | Player benefit | Starting allocation | Target / effect | First campaign use |
|---|---|---|---|---|
| Smoke Screen / `ability.smoke_screen` | Protect an exposed force during preparation or withdrawal | 2 charges; 1 tactical Fuel each; 35s cooldown | Visible valid ground area, radius 12 world metres, 15s duration; incoming direct ranged damage to ground units inside is multiplied by 0.65; no stacking | CH04-M03, unlocked after CH04-M02 |
| Precision Strike / `ability.precision_strike` | Weaken a confirmed armored or fortified threat | 1 charge; 4 tactical Fuel; retain 120s config cooldown | One visible, confirmed hostile ground vehicle or military building; 3s warning/approach, then one normal combat damage event; no area blast | CH04-M04, unlocked after CH04-M03 |
| Paratrooper Reinforcements / `ability.paratrooper_reinforcements` (new) | Reinforce a separated front | 1 charge; 6 tactical Fuel; no recharge | One standard, mission-approved rifle squad delivered by transport plane into a visible clear landing zone; population capacity and air-route safety required | CH04-M05, unlocked after CH04-M04 |
| Supply Drop / `ability.supply_drop` | Relieve a small Materials shortage | 1 charge; 3 tactical Fuel; retain 100s config cooldown | Transport plane drops one crate containing 40 tactical Materials into a secured landing zone; friendly ground-unit collection required | CH05-M03, unlocked after CH05-M02 |

The UI labels the third ability **Paratroopers** where space is limited. No Oil, Fuel, Credits, Command currency, persistent inventory stock, premium resources, or objective items are included in Supply Drop. Materials supply is a finite emergency tool, not a replacement for production or hauling.

Smoke is a readable cover abstraction, not invisibility or invulnerability. This deliberately replaces the catalog's vague incoming-accuracy wording with deterministic direct-ranged mitigation. The inspected direct-damage systems have no accuracy hook; IMPLEMENTATION.md identifies both UnitAttack contribution branches and BuildingDefense integration. Do not fake accuracy in text while implementing a different effect. Apply the multiplier once at authoritative damage resolution, at each direct-shot contribution before aggregation and predicted-health accounting, to ground units of either side inside the circle. It does not change outgoing damage, target confirmation, fog, movement, artillery splash, fire, or mission-script damage. Units lose the modifier immediately when leaving; overlapping smoke uses the strongest single modifier, never multiplication. Keep allied/enemy silhouettes and health readable through VFX.

Precision Strike starting damage: 250 raw damage through a narrow authoritative ECS impact owner and existing observation/death handling; this is provisional and must be tuned against the actual CH04 roster before exposure. No guaranteed kill, boss-health percentage scaling, objective-complete event, reveal, or immunity bypass. Eligible target classes and exclusions are data-driven. Reject civilian/protected buildings, airborne units, hidden or unconfirmed contacts, invulnerable objective actors, and targets outside the playable area. The aircraft approach is presentation of a bounded attack, not a new selectable aircraft. For this initial implementation all aircraft Support routes must be free of known active anti-air coverage; a discovered unsafe route before execution aborts without spending. Do not silently invent an interception simulator or make air defense irrelevant. Any later interceptable Support aircraft is a separate feature decision.

Smoke selected-vehicle convenience, if exposed, resolves to that vehicle's current ground position at preview; the zone stays fixed after deployment and never follows the unit.

Paratroopers use the existing parachute descent, landing validation, normal unit spawning, ownership, collision and population systems after those systems are verified. One standard rifle-squad prefab supplies its existing member count and normal stats. No free specialists, armor, player-selected unlimited manifest, replacement of dead unique characters, or direct objective capture on landing. Landed troops become ordinary controllable units; calling Support does not move the current selection. A proposed 5s approach precedes descent. Reserve population capacity at commit and release it if the flight cannot deliver. Preflight all landing slots before first release; if invalid, abort and release reservations. After the first valid passenger release, never refund the whole call or recall landed troops; resolve remaining passengers only to valid cells in the approved region, otherwise depart with the undeliverable remainder. Never spawn inside blocked geometry. During descent follow existing validated airborne vulnerability rules. Losses after a valid release do not refund the call.

Supply Drop uses the same proposed 5s transport approach and route/landing validation but spawns a crate, not a squad. A secured zone is visible, traversable, outside protected/objective exclusion areas, and has no confirmed hostile within a 15m radius. A selected friendly ground unit receives an explicit collect/interact order to approach within 2m; no global click grants Materials. Transfer as much of the 40 Materials as storage accepts, leave the remainder in the crate, and show `Storage full` when zero can transfer. Record remaining crate stock so save/reload cannot duplicate it. One claim transaction at a time; multiple units cannot collect the same stock twice. Crate stock is match-local, survives until collected or mission end, and never settles as an account reward. Crate destructibility is excluded from the first release.

## Distinguish Support from commanding aircraft

| Player action | System | Cost / ownership |
|---|---|---|
| Load owned troops into a transport, choose route, parachute or unload | Existing transport unit commands | Player owns the aircraft and manifest; existing Fuel, cargo, landing and vulnerability rules |
| Call Paratroopers | Support | Off-map fixed reinforcement allocation; consumes a Support charge, Fuel and population capacity |
| Call Supply Drop | Support | Off-map finite Materials crate; consumes charge and Fuel |
| Attack with an owned jet | Existing combat orders | Ordinary unit combat and operational costs |
| Call Precision Strike | Support | One target, one bounded off-map attack, one consent/commit |

Do not rename existing unload/drop actions to Support. Do not award the same troops both as aircraft cargo and off-map reinforcements. CH04-M04 teaches transport and drop decisions with existing units; its reward makes the off-map reinforcement call available afterward.

## Campaign and reward proposal

Existing mission primary objectives, stars, story facts, normal unit rewards and Protocol Fragments remain authoritative. Support never becomes a win/star prerequisite. Simple labels in mockups are interface layout examples; agents must bind real mission purpose/objectives from the canonical contract instead of copying those examples.

| Missions | Support role | Ability exposure and reward |
|---|---|---|
| CH01-M01, M02, M03, M04, M05 | Hidden | Keep existing Scan/Radar progression. No new Support reward or gameplay retrofit. |
| CH02-M01, M02, M03, M04, M05 | Hidden | No campaign Support menu or new Support reward. |
| CH03-M01, M02, M03, M04, M05 | Hidden | Existing Scan/Intel stays separate. No Support retrofit. |
| CH04-M01 | Hidden | Existing air-corridor mission remains unchanged. |
| CH04-M02 | Unlock reward | No in-match Support. Successful settlement unlocks Smoke Screen and Support access; next mission use only. |
| CH04-M03 | Introduce | One skippable Smoke lesson protects the player's G2G firing asset while preparing. Preserve the actual goal of stopping the hostile battery and surviving the diversion. Reward Precision Strike. |
| CH04-M04 | Reinforce / introduce Strike | Smoke optional; optional Strike against a visible confirmed air-defense target makes insertion safer. Preserve specialist delivery, relay hardware recovery and extraction. Replace Harbor Scan reward with Paratroopers. |
| CH04-M05 | Combine / ARIA consent | Smoke, Strike and Paratroopers optional. Reuse the prior mission's landing-zone vocabulary. ARIA proposes one Strike; approval or rejection both continue. Remove Naval Fire Support reward; preserve existing chapter/story rewards, with no extra strike charge. |
| CH05-M01 | ARIA consent under pressure | Same three abilities; one bounded proposal, no new ability. |
| CH05-M02 | Combine / unlock reward | Same three abilities; reward Supply Drop. Move Rally Order out of the charge-based Support proposal and retain it in the production-command backlog for a separate review. |
| CH05-M03 | Introduce Supply / combine | Optional crate lesson away from the critical evidence interaction. All four abilities usable. |
| CH05-M04 | Reinforce | All four usable; Supply can help local construction without replacing the evacuation/logistics task. Remove old Supply Drop unlock because it has moved earlier. |
| CH05-M05 | Combine only | Use only already taught, validated abilities. No new Support mechanic or reward prerequisite in the finale. |

Deferred catalog abilities: Evacuation Corridor, Field Repair, Casualty Stabilize, Drone Scan, Breach Charge, Harbor Scan and Naval Fire Support. Do not silently grant these in the campaign or promise their availability in Operations/Skirmish; those modes need a separate unlock and balance plan. Radar Ping remains Scan. Rally Order's persistent production routing is better assessed as a normal production command, not a scarce consumable. Area Bombing Run is deferred because footprint, friendly/civilian damage, confirmation, aircraft behavior and anti-air counterplay need a separate design. Field Repair is the next non-air candidate after the four-ability roster is validated.

If a later ability is not ready, omit its reward and exposure together; do not show a dead reward or fake functionality. Do not declare the approved four-ability scope complete when a stage is omitted.

## Mandatory project-asset binding

Owner correction: all Support imagery must use existing game models/artwork. Follow [Support asset manifest](support_asset_manifest.md). Precision Strike uses `Unit_Veh_Jet_01`; Paratroopers and Supply use `Unit_Veh_Plane_Transport`. Reuse the existing smoke grenade, parachute and supply assets; no invented A-10-like plane, propeller transport, weapon salvo or soldier roster. The current v02 mockup uses existing project artwork references; production sprites must be reused exactly or captured from verified source prefabs. Earlier generic imagery is rejected as model authority. S0 records visual mappings; S2/S5/S6/S7 capture and verify the relevant native assets.

## Player flow and visual contract

Use actual Campaign fonts, portrait assets, button artwork and layout components. ImageGen images propose composition; generated portraits/icons/terrain are not replacement production assets. The selection UI is a full-screen popup, a sibling of the Build popup: resource/title/close header, mission purpose and objective summary, four large cards in a 2x2 grid, selected-ability details on the right, one large green next-step button, and a persistent ARIA portrait with Play/Stop. Four abilities do not need category tabs. Do not add a production queue, rush button or purchase flow. While this modal owns input, underlying unit commands cannot receive clicks. Closing it restores the battlefield HUD with mission purpose/objectives, resources, ARIA, minimap and normal unit/command controls. Unit STOP and ARIA STOP are distinct actions.

1. Before unlock, Support is hidden. Scan retains its current dedicated path, mission restrictions and reveal rules. Do not gate Scan by Support ownership.
2. After unlock, Support is a blue command opening the full-screen popup. It never enters Scan or issues an order. Close any conflicting build/target mode through the existing UI lifecycle. Reuse Build modal layout patterns, focus/back behavior and pause policy without sharing its production command semantics.
3. Show available abilities with icon, benefit, charges left, exact tactical resource cost, and Ready/Cooldown/Unavailable reason. Show at most the next earned campaign unlock as a locked card; no wall of future abilities or prices. In the completed roster use four large cards, with scrolling/reflow at narrow widths instead of shrinking text. Card selection changes only the details panel. Show effect, payload, charges remaining, spend cost, timing and requirements. Before a world target is chosen, target/route requirements must be neutral text, not green validated checks.
4. After selecting a card, the popup action reads `Choose Area`, `Choose Target` or `Choose Landing Zone`. Pressing it closes the popup and enters the exclusive battlefield targeting mode without spending. World taps select a candidate, not execute it. Camera pan/zoom remain camera actions. Preview footprint, target identity or landing slots; invalid targets get both an icon and a textual reason. Dragging to pan never commits a target.
5. Confirmation displays the exact action and cost: `Deploy Smoke`, `Confirm Strike`, `Call Reinforcements`, or `Call Supply Drop`. Show `Charges left: 2` separately from `Cost: 1 charge + 1 Fuel` so allocation is not confused with spend. The generated abbreviated charge line is not final copy.
6. Cancel/back, changing command, mission exit, result entry or scene change remove the preview without spending. Touches consumed by a panel cannot pass through as world commands. Unit selection may be retained, but no prior targeting mode auto-resumes.
7. After commit, show one receipt, updated charges/resources, effect or aircraft ETA, and a brief on-world status. When charges reach zero show `No charges remaining`; do not foreground a cooldown that can never restore a charge. Normal cooldowns use simulation time and never regenerate charges.
8. Selection popup opening and closing must inherit the existing Build popup simulation/pause policy. S2 must trace and record the actual policy before wiring Support; this design task inspected the view but did not verify simulation timing. Do not set global time scale or independently resume a mission paused by another owner. Battlefield targeting and consent add no new pause policy. Honor the existing mission pause/tutorial/comic system; suspend input during narrative interruption and revalidate on resume. No time-based consent or automatic acceptance.

Minimum touch target 48 logical units at the target device scale; verify physical usability on device. Reflow for 16:9 and 20:9, safe areas, English and Persian RTL, existing large-text settings, and color-independent state labels. Preserve the existing ARIA identity and typography. The old compact-roster/drawer concepts are superseded for selection. During battlefield targeting the full-screen popup is closed; retain the current ability, footprint, cost, confirmation and cancellation. Back from targeting cancels without spend; reopening Support remembers the last selected card but revalidates availability.

## ARIA contract

`Show Me` and `Show Target` only explain, highlight or focus the camera. They never deploy smoke, call aircraft, move troops, collect a crate or spend resources. ARIA Play is not blanket Support consent.

A Support proposal includes a unique proposal ID, match instance ID, ability ID, exact target/entity generation or fixed ground position, effect/cost snapshot, state version and a 15-second simulation-time expiry. The read model shows target, effect and exact cost. Player `Approve` grants a one-use token bound to that proposal; `Decline` closes it without changing mission success or stars. Any changed target, cost, visibility, ownership, mission state or expired proposal requires fresh approval. Camera inspection does not change the bound target.

Append typed proposal/approve/decline Support intents to the existing contract without renumbering old enum values. A proposal cannot call execute directly. One active proposal at a time; rejected proposals do not immediately reappear for the same event. ARIA Stop/player override cancels pending proposals and uncommitted previews. It does not pretend to undo a strike already launched or erase deployed smoke; feedback states when an action is already committed. ARIA requests must use the same validation and spend path as direct player actions.

## Runtime boundaries and transaction rules

The following are proposed interfaces/owners, not files that already exist. Fit them to the repository's assembly and ECS ownership conventions after reading the relevant architecture rules; do not create a second global combat or wallet singleton.

| Responsibility | Proposed contract / owner | Required behavior |
|---|---|---|
| Definition | `SupportAbilityDefinition` / config adapter | Stable ID, supported modes, target kind, costs, charges, cooldown, effect parameters, presentation IDs, unlock moment, schema version |
| Match state | `SupportAbilityRuntimeState` in the authoritative match world | Ability charges, cooldown deadline, active effects, committed requests, flight/payload state; profile stores ownership only |
| Preview | `SupportTargetPreview` / UI read model | Pure validation result, footprint, target reference, cost and localized rejection code; cannot mutate combat |
| Request | `SupportUseRequest` | Match/request IDs, source, ability, target, expected state version, optional consent token |
| Execution | `SupportAbilityRequestSystem` and ECS effect systems (see technical contracts) | One shared Validate → Commit → Resolve path for player and ARIA; no direct damage/spawn/wallet writes from views |
| Effect | Combat/airborne adapters | Existing damage, spatial, spawning, parachute, resource and objective-observation pipelines |
| Persistence | Existing campaign/save owners plus versioned match snapshot | Durable unlocks separate from per-match consumption; idempotent rewards and executions |

At commit, revalidate mode, unlock, active mission, target, visibility/confirmation, exclusions, charges, cooldown, tactical Fuel including protected civilian floor, aircraft route if needed, population/storage constraints, and consent when source is ARIA. Reserve physical Fuel and decrement one charge atomically with acceptance of the effect/flight request; start cooldown once. Consume the Fuel reservation at effect resolution or first payload release; Smoke does both immediately. Never spend on preview or failed validation. Duplicate taps, request replay and duplicate approval return the existing result, not a second use. If scheduling fails before any effect, roll back all reservations/spend once.

Once an aircraft is accepted, revalidate before strike impact or payload release. If target is dead, hidden, protected, allegiance-changed, or route/landing invalid before resolution, cancel that bounded call and release surviving Fuel reservations and restore the charge once; restore that ability's prior cooldown state. A destroyed source depot is a real resource loss: never mint replacement Fuel. Never retarget automatically. Valid release/impact is the point after which ordinary combat losses receive no refund. Persist the request lifecycle to prevent reload refunds plus duplicated effects. A scene close disposes transient resources; it does not issue fresh rewards.

New mission/retry starts with its authored allocation and normal starting resources, not remaining stock from a previous attempt. Resume, if the existing game supports it, restores charges, cooldowns, effects, pending flight/crate stock and receipts without replaying mutations. Do not add a new resume feature for Support; unsupported mid-mission saves must follow the existing mission restart policy explicitly. Replaying earlier missions obeys that mission's Support allowance even on a late-game profile. Profile migration derives earned ownership from completed milestone missions once; it never unlocks Support in early mission contexts or deletes unrelated legacy inventory.

Telemetry: proposal, approved/declined/expired, preview rejection, committed use, resolved/aborted/refunded, charges, target type and source. Record ordinary ability use as gameplay telemetry, not an `assisted` penalty. Do not change star grading, rewards or difficulty based on use. No paid charges, paid unlocks, mid-match offers or failure-triggered mercy charges in this scope.

## Agent work packets and dependency order

These packets are handoff assignments; no implementation agents have been launched by this planning task.

| Packet | Depends on | Owned work | Completion evidence |
|---|---|---|---|
| S0: design/config synchronization | Owner approval recorded 2026-09-28 | Update canonical availability, effects, new Paratroopers definition, visual catalog mapping, all 25 mission Support rows, Chapter 4/5 reward text, feature matrix, and Steel Push unlock/debrief plan. Preserve unrelated rewards and config schema. | Reviewable diff; parse/schema and cross-document ID/unlock checks; unresolved conflicts recorded |
| S1: runtime foundation + Smoke | S0 | Shared request/state/validation/spend API, Smoke combat modifier and expiry, profile ownership and match reset rules; narrow test encounter | Tests prove actual damage difference, expiry, no stacking, atomic spend, duplicate prevention, invalid/cancel no-spend and restart behavior |
| S2: native UI + direct input | S1 contracts | Remove Support→Scan alias, full-screen popup, Build pause/focus parity, preview/confirm/cancel, localization, read-model feedback, restrictions and layout | Native English/Persian 16:9/20:9 captures; normal taps deploy/cancel; Scan regression including Radar mission and Operations path |
| S3: ARIA consent | S1/S2 | Typed proposals, exact consent token, camera-only guidance, expiry/override, read-model UI | Normal-input approve and decline; Stop/stale/double-approve rejection; no hidden troop orders |
| S4: Smoke campaign integration | S1–S3 pass | CH04-M02 settlement unlock and CH04-M03 skippable introduction, existing profile migration and reward display | Complete normal-input CH04-M02 reward/return → CH04-M03 use/skip/result/return, plus no-Support win |
| S5: Precision Strike | Shared foundation validated | Visible hostile validation, authoritative damage, air approach/route safety, result/refund lifecycle, CH04-M03 reward → CH04-M04 optional use | Actual damage through normal input; hidden/protected/dead/unsafe rejection; no-Support mission win |
| S6: Paratroopers | S5 and verified native airborne runtime | New definition, fixed squad delivery, route/landing/population reservation, CH04-M04 reward → CH04-M05 optional call | Plane approach, descent, valid landing, selection and orders; blocked/full-capacity/duplicate/reload tests; existing owned-transport drop regression |
| S7: Supply Drop | S6 delivery infrastructure | Crate stock and explicit collection, storage cap/remainder, CH05-M02 reward → M03 lesson → M04 reinforcement | Actual flight/crate/collection; zero-room and partial collection, concurrent collectors, reload, no account or Fuel/Oil grant |
| S8: campaign readiness | S4–S7 | Full approved mission journeys, balance, native device usability and handoff evidence | Separate reports for automated checks, normal-input paths, visual review, real-player and device acceptance |

Agents must re-audit source before implementation: this branch is active, and observed paths may evolve. Shared dispatch/contracts and authority files need one owner per change; packet boundaries do not authorize conflicting parallel writes. Tests and new validation runner names are to be implemented and documented by their owners, not assumed to exist already.

S0 exact authority targets: `Design/Combat_Catalog_And_Upgrade_Design.md`, `Design/BalanceConfigs/Combat_Balance_Config_v0_1.json`, matching `Design/VisualConfigs/Combat_Visual_Config_v0_1.json` entries, `Design/Campaign_Mission_High_Level_Design_Catalog.md`, `Design/Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`, `Design/SagaChapters/Saga_Chapter04_Air_And_Armor.md`, `Design/SagaChapters/Saga_Chapter05_Citywide_Command.md`, `Design/AgentReports/CH04M02SteelPush/production_plan.md`, and affected reward/narrative copy catalogs. Update old campaign grants for deferred abilities consistently; do not delete their catalog definitions or unrelated mode availability blindly.

S2 modal reuse reference: `Assets/Game/Scripts/UI/Screens/BuildDrawerView.cs` exposes catalog/detail/action and modal ARIA Stop bounds, but is not proof of pause behavior. Reuse visual and lifecycle conventions through a separate Support view and read model. Do not bind Support to build queue or placement commands.

S2 inspected entry points: `MatchOverlayCommandInputUiSystemHelper.CommandTabs.cs`, `UiActionRequestDispatchSystemHelper.cs`, `MatchOverlayCommandControlsView.cs`, `MatchHudRightQuickRailView.cs`, `UiShellEcsGateway.MissionHudRestrictions.cs`, and `AssistantHighlightPresentationSystemHelper.UiSurfaceGuidance.cs`. Rebind guidance indices/targets when removing the alias. Air/transport restrictions must be ability-specific; they must not disable ground Smoke simply because aircraft are prohibited.

## Required validation and readiness gates

Automated checks must cover config integrity and unlock order, independent Scan routing, invalid ground/hidden/protected/dead targets, insufficient or protected Fuel, population cap, cooldown/no charges, cancellation, repeated taps, same-frame resource contention, stale ARIA approval, pause/comic/result cleanup, reward idempotence, migration, and request/effect reload policy. Verify actual damage, actual landed controllable units and actual crate transfer; a queued request is not effect evidence.

Normal-input acceptance paths:

- Smoke: open → preview → cancel (unchanged resources) → deploy → observe protection and expiry → second use after cooldown → exhausted feedback → result and Campaign return.
- Strike: select valid confirmed target → cancel/confirm → visible approach → actual combat result; separate hidden/dead/unsafe cases and one ARIA rejection followed by normal manual play.
- ARIA: Show Me camera-only → approve one exact action → no second action; repeat with decline, Stop and stale target. Show successful mission completion without consenting.
- Paratroopers: preview landing → confirm → plane/descent/landing → select squad and issue an ordinary order; owned transport loading/drop must still work independently.
- Supply: preview → confirm → crate lands → explicitly collect with ground unit → Materials increase once, respecting capacity; finish without using a drop as a separate route.
- Complete each exposed mission with and without optional Support, including narrative interruption, actual result/reward settlement and return. Never substitute injected wins, old builds or editor-only probes for this gate.

Unity runs follow root AGENTS.md: keep Hub open/signed in; use RTK around `Tools/CI/invoke_unity_macos.sh` with explicit timeout/log for executeMethod/tests/captures, no macOS batchmode, no direct Unity executable or reset/termination workaround. Keep full logs and require the runner's explicit pass marker plus successful exit. Use Unity CLI/Pipeline only as allowed by the repository contract for live Editor operations; read the unity-cli skill before using it. No Unity execution was needed for this design-only task.

Do not call mockups player-ready. Evidence ledger at handoff:

| Gate | Current status |
|---|---|
| Design/ability scope approval | Owner approved 2026-09-28 |
| ImageGen visual-direction review | Owner approved 2026-09-28; v02 project-assets selection is current. Native fidelity remains a separate gate |
| Native implementation visual review | Not run; runtime not implemented |
| Automated runtime checks | Not run; no runtime changes in this task |
| Complete normal-input playthroughs | Not run |
| Real player/device acceptance | Pending; never inferred from the above |

Keep failed artifacts and candidate identifiers. Balance tuning may change quantities, radius, durations and damage after evidence; changing roster, consent semantics, paid-power policy, target protections or mission scope requires a new explicit design decision.

## Visual review package

- [05 — Full-screen Support popup](Mockups/05-support-fullscreen-v02-project-assets.png): current selection direction, replacing the small drawer. Uses the Build popup reference; all four abilities with Paratroopers selected.
- [02 — Smoke placement](Mockups/02-smoke-preview-v02.png): current battlefield targeting direction after the selection popup closes.
- [03 — ARIA consent](Mockups/03-aria-consent-v01.png): one exact Strike request, Show Target, decline and approval.
- [01 — Support drawer](Mockups/01-support-drawer-v02.png): superseded selection layout, retained for history only.
- [04 — Compact roster and Paratroopers](Mockups/04-airborne-roster-v01.png): superseded selection layout; landing-zone preview remains a useful illustration, with the roster closed in implementation.
- [Prompts, sources and visual QA](mockup_provenance.md).

Approval applies to composition, four-ability direction and staged plan, not generated replacement assets or runtime acceptance. Known mockup limitations: simplified mission text, illustrative terrain/roster, abbreviated cost wording, and the consent screen's STOP button at a different position. Native controls use the canonical order SELECT / MOVE / ATTACK / HOLD / STOP / SCAN / SUPPORT / BUILD. Native objectives use check/progress states; decorative stars in some concepts must not imply Support-based star requirements. Native portrait art comes from the existing asset catalog. The current v02 full-screen concept correctly shows neutral requirements before target evaluation; keep them unevaluated until a world target is chosen. Precision Strike artwork illustrates air support, not an authorization for multi-target or area damage. Card charge counts mean charges remaining, and the detail panel states the cost per use separately.
