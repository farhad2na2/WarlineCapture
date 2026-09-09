# M04 Airlift — production and implementation plan

Owner: current task. User authorized planning, documentation, implementation and Editor QA without further questions. Android validation is excluded. Preserve the uncommitted M3 implementation and the preferred M3 art alignment. This plan is the execution contract; evidence and deviations belong in `Design/AgentReports/M04Airlift/`.

## Experience and story

Mission `saga.ch01.m04.airlift` / scenario `scenario.ch01.m04.airlift`. A medical and engineering team is cut off beyond the road secured in M3. The Commander escorts an APC to collect every specialist, protects the landing zone and completes extraction. Captain Laila Nasser joins Dalia, Samira and ARIA. The team carries evidence that attacks target repair capability; the next operation is the fortified communications node in M5. Do not disclose the later Civic Relay or Qassem revelations.

Target play time: 6–9 minutes for a first-time player, with a readable opening and a recoverable tactical setback. Success depends on people reaching safety, not on eliminating every enemy. Prioritize a reliable transport loop and clear passenger accounting over additional unit unlock tutorials.

Sequence: campaign briefing → skippable bilingual opening comic → RTS camera overview → smooth pan/zoom to stranded team, approach and extraction zone → return to the exact initial RTS view → active mission → truthful extraction finale/debrief → actual results/rewards. Mission time starts after camera handoff. Skip and reduced-motion settings use the same camera owner and restore normal input. Pauses and the guide freeze mission time; changing language cannot restart a comic or issue a command.

## Gameplay contract

- Start with two four-person rifle squads, one loaned APC with ten seats, and four named objective specialists. There is spare capacity; no objective requires mixing ordinary troops with the specialist manifest.
- Specialists are friendly selectable passengers. A passenger is safe only when alive, belonging to this attempt and delivered to the authored extraction area. Missing entities are integrity failures, not rescued passengers. A destroyed carrier cannot count its occupants as safe.
- The APC must approach the team through a validated road. Use the real boarding, passenger manifest, movement and disembark commands. No button directly increments rescue counts, moves units by teleport, or edits combat health.
- Secure the landing zone against finite, signposted hostile groups. Show an objective passenger count and a contested/clear extraction state. A timed clear-zone hold begins only after the required people arrive. Leaving or becoming contested resets the hold; the UI explains why.
- Primary victory: all four specialists alive and extracted, mission carrier alive, extraction zone held clear for the configured dwell. Defeat: any required specialist dies, carrier is destroyed before delivery, the command squad is wiped out, or an explicit generous mission deadline expires. Integrity faults prevent settlement and expose a diagnostic reason.
- Stars: complete the rescue; lose no starting rifle member; finish within the authored mastery time. The speed star is optional. Civilians are required survivors and never merely a bonus-star tradeoff.
- Initial logistics context explains the carrier's fuel state. For this introductory mission the loaned transport is explicitly self-supplied; do not silently require an unbuilt refinery. Oil/Fuel header values still come from the canonical resource model. Persian labels use نفت and بنزین as requested.
- Construction and production remain disabled unless the live feasibility check establishes a compelling need. Transport is enabled; unrelated heavy armor, weapons, aircraft and naval purchases remain unavailable. Normal movement/attack/hold/stop stay available.
- APC capacity and real boarding are the first feasibility gate. The campaign catalog explicitly permits APC extraction to the secured landing point, followed by the airlift in the debrief, if helicopter boarding/landing is not dependable. Test helicopter capability before selecting that fallback; record the technical reason and make objective/tutorial copy truthful. No unavailable helicopter button is presented as an objective.

## Map and pressure

Reuse an existing physical map through a separate logical operation-map definition. Do not alter the shared physical terrain, buildings or other mission layouts. Survey actual grid walkability, wheel-surface restrictions, dynamic blockers, spawn footprints and boarding clearances. Author anchors for initial command, each squad, APC, four specialists, rescue staging, protected approach, alternate cover and extraction. Keep passengers within reachable boarding distance after approach and enough room to disembark.

Use a finite initial hostile screen and a warned counterattack. The rescue route should allow rifles to clear a threat before the APC commits. The short exposed route trades speed for damage risk; a protected staging point permits regrouping. Passive play must not win through inherited map defenses. Any dormant map actor policy is attempt-owned and restored on exit. Enemy movement and attacks remain ordinary ECS commands/combat. Warning times and routes must be verified at the actual simulation scale; do not infer meters from map-cell numbers.

## Comics, cinematic and two languages

Three narrative sequences: `seq.ch01.m04.brief`, `.comms`, `.debrief`. Plan six principal panels plus one radio insert: stranded specialists; Laila's extraction map; Commander/Dalia commit the escort; Laila reports the LZ state; safe specialist handover; aircraft departure and communications-node clue. Comms during combat uses a short nonblocking report, with its text preserved for review. Failure copy reports the actual outcome and never shows a successful rescue.

M3 is the art reference. Preserve Samira's mustard scarf, dark jacket, teal clothing and M3 face; Dalia's dark bun, sunglasses, headset and tan uniform; ARIA's short asymmetric cyan hologram. Establish one Laila reference before generating the remaining panels. Earlier comic artwork stays unchanged. Generate final bitmap art through the built-in image tool, inspect every output and its in-game crop, keep text outside the raster, and record prompts/references/hashes.

Author English and Persian together for every mission label, objective, state, tooltip, tutorial, guide topic, result and comic line. Use the shared localization binding and Persian font. Validate connected glyphs, reading order, numbers, punctuation, clipping and locale switching in both 16:9 and 20:9. Bilingual captions are required. External voice generation is not assumed authorized by a planning instruction after the earlier provider approval rejection; use the existing silent-caption/audio fallback without fabricated or mismatched voice clips, and document the media status explicitly.

## Tutorials, guide, ARIA and all classes

Contextual lessons: understand the manifest; select the escort; secure the road; select the APC; approach the team; board specialists; confirm all four aboard; drive to the LZ; clear the perimeter; disembark if required by the accepted lane; hold for extraction; review the outcome. Each action lesson completes from accepted command or authoritative facts. Explanations use Continue. SHOW ME highlights or focuses; DO IT opens the relevant existing command mode, never completes the gameplay action. Optional reference lessons may be skipped. Stop must cancel an accidental order without stranding the mission permanently.

Extend the reusable field guide with boarding/capacity, passenger selection, disembarking, escort positioning, LZ contesting, fuel context and extraction failure/recovery. Keep the existing canonical class inventory: active rifle/APC/specialist roles explained first; engineers/medics and helicopter crew explained in context; tanks, artillery, radar, trucks, planes and other later classes remain truthful reference entries; design-only naval identities remain explicitly unavailable. Numeric capabilities come from canonical configs, not copied balance tables.

ARIA prioritizes immediate passenger danger or an invalid transport action over tutorial chatter. Reports describe observable state: passenger count, carrier health, LZ blocked/clear and missing passenger. No omniscient hidden-unit counts, combat-time blocking comic or forced camera jump. Pause/guide/return and restart must restore assistant state and unsubscribe handlers.

## Architecture and implementation sequence

1. Repair HUD formatting/translation ownership and the compact action labels; run focused header/ARIA regressions and Editor captures.
2. Survey canonical transport capabilities and physical map, then freeze the accepted transport lane and exact geometry. Record source baseline and resource/transport semantics.
3. Add bounded immutable extraction authoring and projection, with new enum values appended. Validate definitions and catalog references. Add objective counts/facts and a pure outcome/star rule utility. Reuse the existing attempt, spawn, command, combat, camera, progression and settlement owners.
4. Build the separate mission/scenario/logical map and narrative assets through Editor builders. Wire M3 completion to real M4 deploy readiness; expose M5 availability separately from M5 playability. Replays use canonical reduced rewards and idempotent first-clear grants.
5. Implement extraction phase/facts, real transport commands, finite pressure, camera handoff, typed HUD intents and mission read models. Passenger manifest truth remains in transport ECS components; the UI never owns rescue truth. All new state is scoped by session token and attempt ordinal.
6. Add bilingual copy, authored tutorials, shared guide content, ARIA reports, final comic assets and campaign/briefing artwork. Avoid duplicating the entire M3 runtime to introduce one mission rule.
7. Exercise real deployment and completion, failure and recovery, results and restart/exit. Fix defects, rerun affected regressions and publish a reconciled implementation/QA report. Do not mark a failed or unrun gate passed.

## Acceptance and evidence

- Data: all referenced configs/prefabs/maps/anchors resolve; actual transport capacity fits four specialists; all spawn and route footprints are valid.
- Rules: full manifest required, carrier loss/passenger death win precedence, no off-zone extraction, contested/exit hold reset, timeout, stale/missing/duplicate member handling, replay settlement idempotency.
- Live Editor: menu → M4 → skip and complete opening/camera; actual boarding and passenger count; ordinary movement/combat → extraction → debrief → result. One successful strategy and one failed idle run. Exercise correcting an incomplete manifest, moving away from the LZ and re-entering, and losing a non-objective escort.
- UI: English/Persian, normal/large text, 16:9/20:9, resource changes without placeholder characters, correct currency icon when a mission repurposes the fuel slot, readable ARIA actions, guide open/close with time restoration.
- Lifecycle: replay, restart, pause-exit and cross-mission return clear attempt units and restore dormant map actors/input/time. No duplicate rewards or stale passenger count.
- Regression: affected M1–M3 mission, resource, transport, narrative/camera and shell checks. Audit source growth without weakening existing guards. Separate inherited failures from new regressions with evidence.
- Final report identifies implemented behavior, acceptance runs/logs/screenshots, performance observations and material remaining limits. Editor-only evidence is not represented as Android validation.

## Executed decisions — 9 September 2026

The live feasibility gate selected the complete APC → helicopter lane. The fallback described above was not used. The canonical ten-seat `Unit_Veh_APC_Fast` collects the four specialists, unloads at the LZ, and transfers them to the ten-seat `Unit_Veh_Helicopter_Transport`. All four must have ridden the APC. Clearance requires twenty uninterrupted seconds with every passenger aboard the helicopter inside a clear LZ. Victory additionally requires an airborne departure; the APC is required only until transfer completes. Missing roster entities are an explicit failed attempt with a retry reason and no victory grants.

The final pressure design is one finite group of four pursuers, announced in the briefing and activated after 120 simulation seconds. The proposed separate initial screen and alternate-cover branch were not authored. This keeps the first transport lesson approachable and rewards a fast, coordinated rescue. The measured automated successful route took about 74 simulation seconds, so an expert can leave before the patrol. The original 6–9 minute first-time estimate remains a design estimate, not a measured playtest result. The speed star is 420 seconds and the deadline is 600 seconds. Construction, production and economy remain disabled; both loaned vehicles are explicitly self-supplied.

The final seven images comprise Laila's flight briefing, Samira with the specialists, Dalia preparing the APC escort, Laila's loaded-aircraft report, safe handover, the recovered engineering records and ARIA's communications-node lead. Three skippable brief panels and three debrief panels use the existing narrative player. The single comms panel is a nonblocking report during play. Captions ship in English and Persian; M4 has no recorded voice clips. Laila's UI portrait is an authored Sprite viewport of her approved briefing source. No prior comic or commander raster was altered by M4 work.

The implementation retains the canonical 57-class reference inventory and adds twelve transport lessons. Availability, protected-specialist survival requirements and transport examples are scoped to M4. M5 is a narrative lead; no unimplemented playable M5 mission is exposed. First clear grants 500 XP, 2,500 credits, Laila's pilot identity, the APC and the transport helicopter in one saved settlement transaction. Replay grants the canonical reduced currency set and can improve stars/time without repeating named unlock grants.

Editor validation uses real campaign deployment, tactical command queues, boarding/disembarking, ordinary vehicle movement, guide/pause routing and result actions. It isolates saves and never edits actor positions, health or mission outcome facts to obtain victory. The carrier-loss scenario invokes the same Destroy command available in the UI. The idle attempt accelerates simulation to 4× without issuing orders. Pure rules cover the additional interrupted-hold, specialist-loss, integrity and deadline branches. The QA report distinguishes these checks from live scenarios and records inherited repository architecture failures separately.
