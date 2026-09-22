# CH02-M02 Supply Line production plan

Status: implemented and desktop-playable; manual and Watch ARIA campaign flows passed. See [validation evidence](AgentReports/CH02M02SupplyLine/VALIDATION.md). Device-performance and final release acceptance remain open.

Authority: [Chapter 2](SagaChapters/Saga_Chapter02_Broken_Grid.md), [mission contract](Campaign_Mission_High_Level_Design_Catalog.md#ch02-m02-supply-line), [sequence catalog](Campaign_Narrative_Sequence_And_Comic_Catalog.md#chapter-2-broken-grid), and [Demo 2 integration](Demo2_Asset_Integration_Guide.md).

## Mission specification

| Field | Implementation contract |
|---|---|
| MissionId | `saga.ch02.m02.supply_line` |
| Title | Supply Line |
| Mode | Campaign |
| ChapterOrDay | Chapter 2 Broken Grid, mission 2 |
| MissionArchetype | Infrastructure Repair |
| ThreatFamily | Hidden Cell |
| StoryFaction | Ash Line sabotage and logistics cell |
| TeachingGoal | Understand and protect automatic Oil extraction → Oil hauling → refinery conversion → Fuel hauling → storage. |
| AssistantTeachingHooks | Explain each chain link, reveal blocked hauler state, focus threats and reserve; ordinary selection/move/attack controls. Watch requires its own normal-input validation. |
| CityContext | Clinics, water pumps and relief vehicles share the reserve with JRC transport. |
| StoryQuestion | Where are the stolen Fuel shipments going? |
| CharacterBeat | Samira challenges military-only allocation; Dalia supports protecting the shared dependency. |
| EvidenceOrRevealBeat | Mandatory debrief correlates diverted Fuel with the dormant corridor from Gridlock; leads to the Old Market manifests. |
| CivilianLegitimacyContext | Armed sabotage teams attack the chain. Civilian freight visuals confer neither hostility nor loot. |
| NarrativePresentationTier | Captioned briefing/debrief with Samira, Dalia and ARIA; nonblocking in-mission radio. Final presentation requires review. |
| RequiredFeatureReadiness | Actual campaign extraction/conversion/delivery, alternate-lane recovery and civilian reserve allocation passed. Uses the high-level authored-chain fallback; free construction is disabled. |
| ScenarioSetup | `scenario.ch02.m02.supply_line`; eight rifles, one tray Oil hauler, one Fuel tanker; authored Oil pump/refinery/reserve depot, six finite armed hostiles. No purchases. Validated in manual and Watch ARIA playthroughs. |
| MapViewContract | `opmap.ch02.supply_yard_01`, `camera.ch02.m02.overview`, `camera.ch02.m02.battle`, `minimap.ch02.m02.supply_yard`. Separate logical definition over existing physical city data; typed Oil/refinery/storage, deployment, hostile and alternate-lane anchors. Traversal and alternate-lane recovery passed in both playthroughs. |
| EnvironmentAssetManifest | Demo 2 warehouse/container/loaded pallet/medical crate: project-owned linked wrappers under `Assets/Game/Prefabs/Environment/Demo2Adapted/Logistics`; attachments owned by the mission reserve depot, with one measured gameplay footprint. Oil pump/refinery from the canonical catalog. Source/output GUIDs and dependency hashes recorded by the builder. No vendor edits. |
| Objectives | Observe real Oil arrival and refinery output; observe Fuel arriving at mission storage; hold 40 barrels for 20 seconds after defeating attackers, with all three links and both haulers alive. |
| StarGoals | Complete; no rifle losses; complete within 480 seconds. |
| CivilianDistrictConsequences | Debrief names restored clinics, pumps and transport. No invented persistent district-stat grants. |
| Rewards | Candidate existing reward schema: 800 Commander XP, 4,000 Credits first clear; 300 Credits replay. Preview and settlement read the same mission definition. Economy review pending. |
| Unlocks | Market Lifeline progression link; deployment only when that content exists. |
| UISurfaces | Campaign chapter map, mission briefing, battle objective/HUD, field guide, narrative playback, result and debrief. EN/FA review required. |
| BalanceTargetBand | Standard; target 5–8 minutes, hard deadline 12 minutes; observed manual 4:24 and Watch 5:04. Warnings precede pressure, no premium recovery. Manual run is faster than target; broader balance review remains open. |
| ValidationPlan | Config/catalog/progression sanity; deterministic failure/hold rules; actual production/haul/navigation probe; manual and Watch wins; both locales/layouts; retry/replay/settlement; D2-V1–V6. |
| FailureRetryRules | Loss of squad, either hauler, any critical chain building or deadline causes defeat. Missing identity/initialization fails closed as an integrity diagnostic. Retry creates a fresh attempt and removes its owned buildings/units; no partial reward. |

## Acceptance tracking

Recorded desktop acceptance (full logs and limits in the validation report):

- S1 — passed: configuration, unique identity, catalog registration, mission progression and reward consistency.
- S2 — passed: extraction, real Oil load/unload, refinery consumption/conversion, real Fuel load/unload and reserve hold.
- S3 — passed: blocked route and prepared alternate lane recovery, visible reserve allocation, finite telegraphed pressure.
- S4 — runtime loss/pause/integrity and settlement tests passed; full retry/lifecycle stress coverage remains open: defeat cases, initialization integrity, paused clock, cleanup, retry, replay and exactly-once settlement.
- S5 — implemented; EN/FA reserve HUD inspected, captioned narrative flow passed; full device layout/voice acceptance remains open: briefing/comms/debrief, mandatory clue, field guide and normal-control ARIA guidance in EN/FA.
- S6 — passed: uninterrupted manual and ARIA wins through public menu entry, result/debrief and return.
- D2-V1–V6: source/output identity; visual readability; ownership/navigation; lifecycle/dependency scope; device cost; affected gameplay/regressions.

The readiness fallback does not waive real hauling, route recovery or reserve decisions. Focused code tests do not certify manual, ARIA, art or device acceptance.
