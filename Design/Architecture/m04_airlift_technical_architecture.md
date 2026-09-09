# M04 Airlift technical architecture

Status: implemented and accepted in the Editor; evidence and repository-wide audit limits are recorded separately in `Design/AgentReports/M04Airlift`. This document describes the implemented boundaries, not a completed QA signoff.

## Authoring and compatibility

M04 owns `saga.ch01.m04.airlift`, `scenario.ch01.m04.airlift` and `opmap.ch01.airlift_01`. Its separate logical map reuses the existing physical surface without editing it. A typed extraction definition projects passenger/carrier/aircraft roles, three world anchors, radii, required manifest count, hold duration, deadline and camera tour into the campaign catalog blob. Validation rejects conflicting defense/construction modes, missing roles or anchors, invalid counts/radii and incompatible transport restrictions. New objective and guidance enum values append to existing contracts.

The data builder creates eight rifle escorts, four protected specialists, one `Unit_Veh_APC_Fast`, one `Unit_Veh_Helicopter_Transport` and four hostile pursuers. The carrier key is the existing authored APC, not a synthetic prefab name. The physical road connects the specialist pickup at (801,427) to the landing zone at (1060,428); the helicopter must reach the departure area at (1085,465). Loaned vehicles explicitly disable consumption for this mission. Building, production and economy remain disabled, while normal transport and air commands remain available.

## Runtime ownership

The campaign spawn owner creates an attempt-owned manifest containing direct entity references. The manifest tracks initialized health, observed deaths and whether each specialist rode the APC. Its identity is the campaign session token, attempt ordinal and source version. It is cleared on new launch. Mission cleanup includes disabled entities so passengers hidden inside vehicles cannot survive a retry as stale mission actors.

`CampaignMissionRuntimeSystem.Extraction` reads the manifest and existing health, passenger, transform and aircraft components. It does not invent movement or boarding. Passengers remain valid while ECS `Disabled` hides their on-foot representation. A missing live entity is an integrity fault; zero health before initialization is not immediately interpreted as a death. Actual initialized health loss is latched. The manifest must contain exactly four protected specialists. Repeated entity references are rejected rather than counted as different people. Successful transfer is latched after all four have ridden the APC and boarded the helicopter, so later disembarkation cannot make an already-completed APC leg retroactively require the carrier again.

`TransportBoardingCommandSystem` owns boarding and disembarking; normal unit movement owns all travel. The extraction projection observes each specialist riding the canonical carrier and subsequently boarding the helicopter. A helicopter shortcut cannot satisfy the APC leg. With all four alive and transferred, twenty uninterrupted seconds inside a clear landing zone grant departure clearance. An active hostile in the radius resets the timer. Combat-suppressed dormant scenery does not contest it. Clearance alone does not complete the mission: the helicopter must be airborne in the departure radius with all four aboard.

The pure extraction rule utility gives failure precedence over simultaneous departure. A specialist death, helicopter loss, carrier loss before transfer, all escorts lost, roster integrity failure or ten-minute timeout fails the attempt. A carrier destroyed after successful transfer does not retroactively invalidate the rescue. The mission clock starts after the opening tour and follows simulation pause. Victory waits for the short finale presentation before projecting the result.

## Camera and UI boundaries

The opening captures the actual settled RTS camera, holds, visits the specialist team and landing zone, then returns to the captured pose. Existing skip and reduced-motion controls apply. The camera owner clamps a smooth focus against the viewport footprint, not just the target point: an edge-of-map point otherwise makes smoothing fight boundary correction indefinitely. VTOL altitude observation publishes the existing airborne flag once actual height clears boarding tolerance; it does not manufacture departure. Altitude helpers remain in the air-movement owner. Transport result structures remain in their existing command owner in a separate partial file.

The watchdog remains a recovery mechanism and is explicitly rejected by the normal-tour live acceptance probe.

Twelve extraction guidance prompts advance from observed selection, transport, movement and clearance facts. Only the initial plan explanation accepts acknowledgement. Contextual actions use existing tactical controls; the passive extraction HUD cannot change rescue facts. Its gateway rejects stale mission definitions and paused gameplay actions. Focus buttons submit normal camera requests. Guide/Pause/Settings retain the shared simulation-pause ownership contract.

The field guide reuses the existing 57-class inventory and paged loading behavior, with M04-specific availability notes and twelve transport lessons. The result modal yields to the guide for both defense and extraction missions. The result binder serializes its shared region reference and resolves a bounded parent for older scenes. It resets the region after the guide hide transition so the result cannot remain at zero alpha/scale. Locale changes refresh the formatted extraction status immediately, and a localized binding restores its value if another presentation layer writes placeholders.

The shared responsive section layout notifies the result owner after applying its geometry. The result owner then assigns objective/status columns and room for all four reward lines, preventing a later responsive refresh from restoring overlapping authored widths. Compact objective labels are localized independently of the full tutorial sentences.

M04 result details are typed separately from M03 defense details, including delivered specialists, losses, carrier history and transport failure causes.

## Narrative, localization and media

Seven text-free source panels provide fourteen authored aspect-ratio viewports plus one dedicated Laila portrait viewport for briefing, radio report and debrief. English and Persian captions remain separate from art. Laila, Samira, Dalia and ARIA follow the approved M3 visual references; the source hashes and identity constraints are recorded in the art manifest. Narrative media uses stable import GUIDs and addressable references. M04 never falls through to M03 tutorial recordings. Recorded M04 voices are not present: bilingual caption delivery is the explicit current audio fallback.

HUD numeric updates go through the shared localized binding. Fuel uses بنزین and oil uses نفت; optional compact numeric formatting is performed outside TMP's limited formatting syntax. Oil values refresh even when the slot is inactive. ARIA avoids applying Arabic shaping twice to RTL-aware text components. The selection health value and subtitle use the same runtime-observing localized binding; the health bar reserves a full value column for Persian health labels, and both mission transport descriptions have Persian catalog entries. The shared transport command result payload now has enough UTF-8 capacity for Persian rejection sentences; compact assistant metadata truncates safely while the HUD retains the full reason.

## Persistence and acceptance

First clear grants 500 commander XP, 2,500 credits, Laila's canonical pilot unlock and the two transport unit unlocks in the same profile transaction as the settlement receipt. Named M04 grants reject foreign missions and replay reward sets. Existing unit ownership is preserved without duplicate entries. Replays use the authored replay reward set, can improve best stars/time, and cannot duplicate a settled attempt. M04 does not advertise an unimplemented playable M05 node; the debrief supplies the narrative lead.

Editor acceptance uses isolated temporary saves. Focused tests cover passenger ownership, initialization/death distinction, interrupted clearance, failure precedence, real departure requirements, reward persistence and edge camera settling. The live probe deploys through the campaign gateway and issues real movement and transport requests; it never edits health, positions or outcome facts to obtain victory. It must observe all eighteen actors, normal camera completion, APC boarding, road travel, ground transfer, helicopter boarding, clearance, airborne departure, accepted settlement, debrief, localized result and campaign return. A wrapper exit alone is insufficient; every successful validation also needs its explicit pass marker. Android validation is excluded by the user's instruction.
