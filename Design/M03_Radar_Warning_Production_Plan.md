# M03 Radar Warning — Detailed Production Plan

Date: 2026-09-08

Status: Proposed plan; implementation has not started

Mission: `saga.ch01.m03.radar_warning`

Scenario: `scenario.ch01.m03.radar_warning`

Logical map: `opmap.ch01.convoy_approach_01`

This plan makes M03 a complete, bilingual Campaign experience: a comic briefing, a cinematic battlefield handoff, a readable convoy defense, contextual tutorials, a reusable field guide, ARIA assistance, a truthful result, and a story bridge to M04. It includes gameplay class coverage and a separate C# responsibility map. “All classes” means every catalog unit family is accounted for; Chapter 1 does not require the player to learn every later-game vehicle and weapon in one mission.

The player promise is: **“I understood the warning, prepared my own defense, and protected the post because my decisions mattered.”** The emotional progression is uncertainty → anticipation → useful preparation → pressure → recovery → earned relief → a new mystery.

Companion documents:

- [Technical architecture and class responsibilities](Architecture/m03_radar_warning_technical_architecture.md).
- [Dependency-ordered implementation and acceptance tracker](Architecture/m03_radar_warning_implementation_tracker.md).

## 1. Authority, current evidence, and planning assumptions

The Chapter 1 spec, high-level mission catalog, narrative bible, and sequence catalog remain the story authorities. This document proposes detailed resolutions where those sources leave decisions open. It does not mark proposed functionality as implemented or silently supersede an accepted decision.

| Finding from the current checkout | Consequence for this plan |
|---|---|
| The latest M02 tracker records 39/39 Editor items accepted and explicitly makes M02 a non-combat build/production tutorial. Its older architecture still describes a delayed defense. | Use the latest accepted M02 product contract. M03 must introduce defensive time pressure rather than assume players already survived M02 combat. |
| M02 settlement reveals M03, but the inspected mission/scenario catalogs do not contain a playable M03 definition. | M03 needs its complete data, map binding, launch, and settlement path. An unlocked map entry is not playable readiness. |
| `ThreatWarningRuntimeStateComponent` contains pending type, ETA, count, and version. | Source, confidence, route, attempt identity, expiration, and focus require additive contracts. |
| `ThreatAlertV3PopupView` has alert and route-preview surfaces, but its jump button currently switches visual state. | A route-preview screenshot is insufficient: prove actual camera focus and a route bound to the same warning. |
| Real detector scanning exists in `ThreatDetectionWarningSystem`; scripted delayed-wave warning also exists. | Reuse both producers through one warning arbitration path. Do not build a separate M03 sensor simulation. |
| `ability.radar_ping` is cataloged as `designReadyNeedsCode`. The inspected runtime references do not establish a working support activation path. | Treat Radar Ping as planned implementation with an explicit readiness gate. A reward icon cannot stand in for the ability. |
| Current Unity assets disagree with the older balance JSON: for example Guard Tower is 22,000 Credits / 50 Materials / 30 seconds, versus 1,760 Materials / 20 seconds in the JSON. Barracks production is now populated. | Freeze costs, squad quantities, weapon behavior, and unlock semantics from current runtime assets, then reconcile relevant design rows. Never import old tables over accepted runtime tuning. |
| The current APC Fast asset has `canAttack: 0`. The satellite dish has `threatDetectionKind: 2` (Air); Radar Tank has kind 1 (Ground). | Use the non-shooting APC honestly. Recommended ground sensor is a mission-loaned Radar Tank, preserving the dish's Air capability. |
| M01/M02 use one mission runtime, objective projection, guidance projection, result projection, and progression store. | Extend those owners with default-safe data. Preserve all M01/M02 routes. |

Assumptions for planning: M3 means Chapter 1 Mission 3; the two languages are English (`en`) and Persian/Farsi (`fa-IR`); the requested output is a plan, not runtime implementation or final media generation. Android/Samsung acceptance remains an explicit later gate; M02's documented deferral is not a device pass for M03.

## 2. Scope and proposed decisions

| Area | Recommended M03 commitment | Boundary |
|---|---|---|
| Mission | Defend the established forward post against one stolen convoy, divided into a readable vanguard and main body. | Same convoy, finite authored membership; no endless waves or surprise reinforcements. |
| New learning | Understand a warning, inspect its route, prepare a defense, use Hold/Stop, interpret a sensor refresh. | Reinforce M02 construction/production without replaying its entire tutorial. |
| Main roster | Two rifle squads, existing post/Barracks, one existing sensor, optional rifle reinforcement, tower and barrier placement. | No mandatory armor purchase, transportation, fuel supply, upgrades, aircraft, missiles, or naval units. |
| Tactical choice | Establish a forward interception position or defend nearer the post with a reserve. | Both must work; guidance suggests a plan and accepts alternatives. |
| Radar Ping | Two mission-loaned uses, 60-second simulation cooldown, through a live ground-capable sensor. First clear awards permanent ownership. | Exact charge/cooldown values are proposed, aligned with the design catalog. Ability is optional for victory but required to meet the full feature acceptance. |
| Road Barrier | Temporary mission access solves the existing Chapter 2 permanent-unlock conflict. | Do not move the global Chapter 2 gate. Barrier behavior must pass route-blocking and recovery tests. |
| Guard Tower | Temporary build access; permanent first-clear unlock. | Uses real building combat and real construction spend. |
| Ground sensor | Start with one mission-loaned `Unit_Veh_Radar_Tank`, parked near the post with validated coverage. | No required vehicle movement/production or permanent Radar Tank unlock. Sensor loss disables Ping while scout warnings preserve a winnable mission. |
| Narrative | Three canonical sequences, six principal comic panels, one comms insert; Dalia and ARIA lead, Samira provides the M04 extraction hook. | No Qassem reveal, revoked-credential proof, or resolution reserved for later missions. |
| Guide | Twelve short mission topics plus class reference cards in existing shell presentation. | Offline, searchable by topic/category; no conversational AI or new account requirement. |
| Guidance | Existing Full, Contextual, Minimal settings; critical threat information survives every setting. | Tutorial completion never gates combat victory. |

Proposed narrowing of the older five-building M03 catalog: show Tower, Barrier, and the already-established Barracks production entry. Keep Tent hidden because it duplicates M02's producer role. Keep the Air-only dish out of this ground-defense build lesson. The loaned Ground Radar Tank supplies sensor functionality without changing global building capabilities. Record this as a scope reconciliation before implementation; do not silently rewrite the chapter spec.

## 3. Mission flow, clock, objectives, and outcome

### 3.1 Player journey

| Beat | Target timing | Player experience and proof of understanding |
|---|---|---|
| Campaign entry | Before deployment | M02 victory reveals Radar Warning. Briefing lists defense, convoy, clinic protection, and genuine first-clear rewards. |
| Comic briefing | About 25–40 seconds in autoplay; manual reading unlimited | Three panels establish the outage, two defensible positions, and an honest warning estimate. Skip is available. |
| Battlefield handoff | About 10–12 seconds; skippable | Start visibly at the normal RTS camera, zoom and pan together to the post/clinic and approach fork, then smoothly restore the original RTS framing. No hidden enemy time advances. |
| First actionable warning | At active simulation start, `t=0` | Medium-severity scout report shows approach direction and estimated first contact in 60–75 seconds. The player can focus the route immediately. |
| Preparation | `t=0–60` | Choose a defense position, start a tower/barrier or rifle order, move squads, then Hold. Reading a long guide uses the explicit pause route. |
| Vanguard contact | Target `t=60–90` | One armed light vehicle with a small escort validates the initial defense. A fresh observation confirms the actual route. |
| Reassessment | After contact; nominal `t=90–150` | Main-body warning arrives before its contact. ARIA identifies the changed evidence; Dalia suggests keeping a reserve. Player repositions or uses Ping. |
| Main-body pressure | Nominal `t=150–240` | Remaining convoy follows its authored route. A warned fork creates one reposition decision. No hidden branch selection based on the player's build. |
| Resolve | Typical complete attempt targeted at 5–7 minutes of active play | Defeat all required convoy members while keeping the post and inner core boundary safe. End immediately when terminal facts resolve. |
| Debrief/result | About 20–35 seconds, reading speed dependent | Freeze/settle the result internally, play the truthful debrief comic with the M04 hook, then show the Victory popup with actual losses, stars and rewards before menu return. Preserve the latest accepted M02 order. |

These are tuning hypotheses, not measured durations. Current weapon ranges and squad damage may resolve small engagements much faster than the legacy 5–7 minute target. First tune route lengths, positioning, finite convoy spacing, and decision density. If good play consistently finishes earlier, document a duration-band adjustment instead of adding invulnerability, empty waits, hidden waves, or inflated health. Report active simulation duration separately from wall time spent reading, paused, loading, or in comics.

### 3.2 Clock contract

- The active mission clock begins only after world readiness and the cinematic/input handoff complete. Skipping reaches that same boundary once.
- First-warning lead time measures warning publication to predicted **first contact at the authored defensive contact line**, not to hidden entity activation. Store and test both timestamps.
- Full-guide first exposure may open a paused explanation through the existing pause owner. The player sees “Paused”; all gameplay, construction, cooldowns, and mission ETA use the same paused simulation clock.
- Normal short ARIA tips and in-combat comms do not pause or steal control. The long field guide opens through Pause; its exit restores the previous pause state.
- An ETA is an estimate. New contact/path data can change it. At zero without a verified arrival, show “Contact expected” or “Estimate outdated,” not an invented exact arrival.
- No difficulty scaling by UI language, voice duration, tutorial button sequence, or amount of time spent reading a paused panel.

### 3.3 Objectives and stars

| Identity | Exact meaning proposed for implementation |
|---|---|
| `obj.ch01.m03.survive_convoy` | The forward-post role is alive when every required convoy member is resolved as defeated. A timer alone cannot award victory. |
| `obj.ch01.m03.prevent_core_breach` | No living member of the required hostile convoy crosses the authored inner core boundary. Entry to the outer defensive approach is normal gameplay. |
| `obj.ch01.m03.destroy_convoy` | Every required member of both authored convoy elements is defeated; suppressed or not-yet-activated members remain outstanding. Missing entities are an integrity fault, not a kill. |
| `star.ch01.m03.complete_mission` | One star for victory. |
| `star.ch01.m03.no_civilian_deaths` | Additional independent star for zero mission civilian deaths. |
| `star.ch01.m03.no_base_damage` | Additional independent star for no health damage to the forward-post role during active play. Tower/barrier damage does not count as post damage. |

Victory requires all primary conditions. Defeat occurs on post destruction or a confirmed inner-core breach. The chapter's “minor base damage allowed” remains true: ranged damage can hit the post before any hostile crosses the inner core. Civilian loss affects its star, not automatic defeat. Test each independent star combination, including a clean post with a civilian loss. Squad loss is reported and balanced, not an extra undeclared failure condition.

Define simultaneous events once: after authoritative damage/death and movement complete for the tick, validate living breach entrants; then resolve loss conditions before victory. A dead vehicle does not breach through its wreck bounds. A post destroyed on the same tick as the last hostile dies is defeat. The UI cannot change this priority.

### 3.4 Retry, replay, and rewards

Retry reconstructs the same seed, route variant, roster, economy, guidance settings, and sensor access. It resets attempt-only warnings, charges, cooldowns, orders, losses, and tutorial acknowledgements. Failed match spending does not reduce persistent resources. Ordinary suspend/resume follows the app's existing behavior; this plan does not assume a new mid-battle save system.

First clear grants canonical Commander XP, Credits, Guard Tower, Radar Ping, and availability of `saga.ch01.m04.airlift`. Amounts come from the reviewed Chapter 1 reward source and must be resolved before the mission asset is accepted. Replays grant only the reduced canonical replay reward and can improve best stars. Duplicate conversion applies only to a legitimate first-clear grant where the player already owns the item; reopening a result must never mint Blueprint Parts. M04 availability and M04 deploy readiness remain separate until M04 has real content.

## 4. Map, roster, and every class

### 4.1 Map grammar

Reuse the accepted physical city through a separately authored logical view. The M02 post and the clinic remain recognizable. The logical window must contain an approach road, a visible fork, two defensive positions, a protected civilian side corridor, a sensor location, and a legible inner-core boundary. Do not assume the existing M02 camera window is large enough for vehicle ranges and honest 60-second contact lead time.

Required proposed anchors:

| Anchor suffix, under `anchor.ch01.m03.` | Use |
|---|---|
| `post`, `inner_core`, `clinic`, `civilian_safe` | Authoritative mission roles and protection boundaries. |
| `player_alpha`, `player_bravo`, `reserve` | Squad starts and safe reserve position. |
| `sensor`, `tower_forward`, `tower_inner`, `barrier_forward`, `barrier_inner` | Valid build/detection locations; footprint validation uses real grid and renderer bounds. |
| `convoy_staging`, `fork`, `lane_a_contact`, `lane_b_contact`, `main_body_staging` | Convoy route identity and contact ETA calculation. |
| `intro_post`, `intro_approach`, `return_rts`, `finale_post` | Obstruction-tested presentation only. |

The two routes are readable alternatives in one local battlefield, not two distant fronts. Protected clinic traffic stays outside hostile staging and placement sockets. The barrier may channel vehicles into a valid alternate lane or be attacked by an armed convoy member; it must not create a pathfinding deadlock. Do not promise a speed debuff that the physical obstruction does not implement.

### 4.2 Proposed exact vertical-slice roster

| Role | Catalog identity | Starting proposal | Validation requirement |
|---|---|---:|---|
| Alpha and Bravo | `Unit_Chr_Soldier_Male_02_Alt_04` | 2 squads | Preserve canonical four-soldier squad production/selection semantics; do not confuse four members with four squad orders. |
| Optional reinforcement | Same rifle identity through `Building_Barrack` | 0 at start; one affordable order | Use existing four-member production quantity and real queue/cost. |
| Vanguard vehicle | `Unit_Veh_Light_Armored_Car` | 1 | Real hostile faction, armed combat, route, and threat state. |
| Vanguard escort | `Unit_Chr_Insurgent_Male_02` | 2 individuals, grouped for convoy accounting | Actual asset combat and faction compatibility verified before tuning. |
| Main-body vehicle | `Unit_Veh_Light_Armored_Car` | 1 | Distinct convoy member/role, predeclared in the manifest. |
| Main-body carrier | `Unit_Veh_APC_Fast` | 1 | Non-shooting vehicle whose approach threatens breach. No invented turret fire or passenger disembark system. |
| Main-body escort | `Unit_Chr_Insurgent_Male_02` | 2 individuals | Same accepted irregular family; avoid introducing long-range specialists here. |
| Protected civilians | Existing `Unit_Chr_Civilian_*` variants | Proposed 4, exact safe-route variants frozen at map gate | Not commandable or auto-targetable; counted only in the civilian star. |
| Forward post | Existing accepted M02 base-role binding | 1 | Reuse exact live building-health owner; do not substitute an unimplemented Command Post prefab. |
| Barracks | `Building_Barrack` | 1 completed | Continuity with M02; deterministic reconstruction, not an assumed persistent battle snapshot. |
| Sensor | `Unit_Veh_Radar_Tank` | 1 mission-loaned vehicle | Current asset is Ground, radius 240 cells, non-attacking. Prove actual coverage/faction and selection behavior; no production unlock. |

These are proposed counts, not a claim of balance. The explicit manifest has three vehicles and four escort individuals across two elements. If the formation/route proves too fast or too fragile, change authored counts/spacing and retest; keep asset damage/health unchanged unless a measured balance issue justifies a bounded change.

### 4.3 Full class coverage policy

The [catalog appendix](M03_Radar_Warning_Class_Coverage.md) accounts for all 57 unit identities in the inspected design catalog. Its “implemented” labels are catalog claims, not M03 validation passes. Runtime assets and actual behavior decide usable content.

| Class/family | M03 exposure | Guide responsibility |
|---|---|---|
| Rifle / general infantry | Playable, required existing skill | Select, move, range, focus fire, Hold, Stop, reserve use. |
| Long-range infantry | Reference only | Explain range/fragility only after checking actual weapon metadata; no forced sniper lesson. |
| Breach / heavy infantry | Later Campaign | Explain specialized assault role; keep advanced actions out of M03 commands. |
| Anti-armor / siege infantry | Reference only | Show actual role tags, not appearance-based assumptions. The ghillie entry is cataloged anti-armor/siege and needs semantic audit before naming it a sniper. |
| Commander / leader | Narrative identity or later roster | The player commands; no new hero deployment obligation. |
| Contractor / security | Reference only | Friendly/neutral/enemy status derives from scenario faction, not clothing. |
| Insurgent infantry | Hostile escort | Confirmed hostile identity and tactical priority; civilian appearance is not hostility evidence. |
| Civilians | Protected, non-commandable | Safe routes and civilian star; no offensive actions. |
| Pilots / aircrew | M04 bridge only | Later transport role; no flight tutorial in M03. |
| Light armored car | Main armed convoy threat | Distinguish shooting vehicle from unarmed carrier; warn before contact. |
| APC variants | One enemy carrier; remaining variants in reference | Transport/armor role does not imply a working weapon. Boarding/unloading stays later. |
| Tank | Later Campaign | Armor/weapon role; not a mandatory first defense counter. |
| Radar vehicle | Mission-loaned active sensor; optional movement | Sensor support is distinct from a combat tank. No vehicle production unlock is silently added. |
| Trucks / tanker | Later logistics | Transport/resource function; no new Fuel/Oil economy. |
| Recon drone | Later reconnaissance | No omniscient reveal claim. |
| Attack helicopters / jets | Later air chapter | Existing attack cinematics remain outside M03's required gameplay. |
| Transport helicopter / plane | M04 preview | Rescue motivation only, without performing an unavailable transport command. |
| Ground/air missile launchers | Later advanced combat | Separate weapon roles and targeting constraints; do not trust legacy role tags alone. |
| Six sea units | Future/design-only | No deploy button or playable claim while Unity prefabs are missing. |

## 5. Economy and meaningful defensive choice

Use actual runtime costs and production quantity. Initial proposal: **50,000 Credits and 100 Materials**, one completed Barracks, two starting rifle squads, and one existing sensor. No passive income is required. Fuel/Oil are hidden and no mission-required unit may stall for an unseen fuel shortage; verify the convoy's authored fuel state without changing the global logistics rule.

Current observed building costs: Tower 22,000/50, Barrier 6,000/15, Dish 20,000/45, each with a 30-second duration. A rifle member asset costs 10,000, but a Barracks order produces four members; the accepted transaction must determine whether the actual order cost is per entry or multiplied. **The proposed budget cannot be accepted until that exact quantity/cost calculation is captured.**

Balance around at least two affordable complete strategies: tower plus barrier with the initial squads; and a reinforcement-focused reserve plan. If the loaned sensor is lost, scout reports and visible approach information preserve a recovery path without requiring an unavailable replacement purchase. Compute both Credit and Material remainder for each path. Target 10–25% end float for representative successful runs, not for every extreme player strategy. Record when the provisional 50,000/100 budget misses that target and tune the authored budget/choices from real transactions; do not hide resource grants in ARIA actions.

Placement previews show coverage and physical obstruction truth. Use authored valid areas rather than one mandatory coordinate, so placement is a decision. Exact resources are spent by existing transactions; cancel/refund behavior follows the same construction rules as normal play and is explained when relevant.

## 6. Comic and narrative production

### 6.1 Storyboard and art direction

Use the accepted Warline character designs, palette, military UI, city landmarks, and portrait identities. Cinematic comic staging should convey a human consequence without overloading panels with HUD diagrams. Art contains no baked dialogue, localized numbers, or essential instructions. English and Persian share art with separately reviewed 16:9 and 20:9 crops. Do not mirror faction emblems, vehicles, geography, or character handedness for RTL.

| Panel/sequence | Composition | Story and gameplay purpose | Motion/audio |
|---|---|---|---|
| `M03-B01`, `seq.ch01.m03.brief` | Close view of the interrupted warning feed with the dark sector visible behind ARIA. | The source is incomplete; the approach is plausible, not magically confirmed. | Restrained screen flicker; brief signal break; reduced-motion still. |
| `M03-B02`, same | Dalia at the restored post, with the approach road and clinic access legible in depth. | Show two defensible positions and what they protect. | Slow depth drift; distant engines beneath dialogue. |
| `M03-B03`, same | Commander viewpoint over the route display; warning range rather than a false exact countdown. | Hand decision-making to the player. | One clean transition into the real map. |
| `M03-C01`, `seq.ch01.m03.comms` | Compact evidence insert: outage timestamp correlated with the convoy's commitment. | Attack timing suggests foreknowledge. It does not identify the culprit. | Radio only in combat; full insert available in the guide/archive afterward. |
| `M03-D01`, `seq.ch01.m03.debrief` | Post and clinic corridor after the defense. | Relief must match actual losses/damage. Use localized variant lines and crop/overlay states. | Settle after engine noise; no forced celebratory camera on casualties. |
| `M03-D02`, same | ARIA and Dalia compare outage schedule and recovered orders. | Establish the precise new clue without revealing Chapter 1's final credential proof. | Quiet diagnostic motif; no villain speech. |
| `M03-D03`, same | Radio call from an isolated medical/engineering team; safe destination visible on a map. | Motivate M04 Airlift through people who need help. | Samira radio hook; end on resolve. |

Deliver seven source panels, fourteen reviewed aspect crops, and metadata for focal points, safe text zones, asset identities, and memory size. Outcome variation should first reuse a truthful composition with different copy; add new damage/casualty art only if the clean composition would contradict the recorded result. Defeat uses a concise result explanation and retry advice, not the victory debrief.

### 6.2 Bilingual story script draft

Each row is one stable line identity with paired text. Persian is a production draft requiring fluent editorial and in-game RTL review. Do not generate final voice until terminology, character names, runtime claims, and playable timing are frozen. Names should ultimately resolve through the existing speaker catalog.

| Line ID / speaker | English | فارسی |
|---|---|---|
| `m03.brief.01` ARIA | The warning sector is offline. A field report places an armored convoy on the approach road. | سامانهٔ هشدار این بخش از کار افتاده است. طبق گزارش میدانی، یک کاروان زرهی در جادهٔ ورودی دیده شده است. |
| `m03.brief.02` Dalia | We can hold them at the junction or defend closer to the post. Keep the clinic corridor open. | می‌توانیم در تقاطع جلویشان را بگیریم یا نزدیک‌تر به پایگاه دفاع کنیم. مسیر درمانگاه را باز نگه دارید. |
| `m03.brief.03` ARIA | We have roughly a minute to prepare. The route is an estimate until we confirm contact. | حدود یک دقیقه برای آماده‌شدن فرصت داریم. تا پیش از تأیید تماس، مسیر اعلام‌شده قطعی نیست. |
| `m03.comms.01` ARIA | Their movement began at the exact moment the warning sector went dark. | حرکت آن‌ها دقیقاً هم‌زمان با قطع سامانهٔ هشدار آغاز شد. |
| `m03.comms.02` Dalia | Then someone gave them the schedule. Keep a squad ready for the second approach. | پس کسی زمان‌بندی را به آن‌ها داده است. یک گروه را برای مسیر دوم آماده نگه دارید. |
| `m03.debrief.clean` Dalia | The post is intact. The clinic corridor is still open. | پایگاه سالم مانده است. مسیر درمانگاه هنوز باز است. |
| `m03.debrief.damaged` Dalia | The post is damaged, but it is still operational. | پایگاه آسیب دیده، اما هنوز عملیاتی است. |
| `m03.debrief.losses` Dalia | We held the post, but civilians were lost. We need a safer defense next time. | پایگاه را حفظ کردیم، اما غیرنظامیان جان باختند. دفعهٔ بعد باید دفاع امن‌تری داشته باشیم. |
| `m03.debrief.evidence` ARIA | These orders name the outage before it happened. That is evidence of advance knowledge. | در این دستورها، قطعی پیش از وقوعش ثبت شده است. این مدرکی از اطلاع قبلی است. |
| `m03.debrief.m04` Samira | A medical and engineering team is cut off beyond the road. They need an airlift. | یک تیم پزشکی و مهندسی آن سوی جاده گرفتار شده است. آن‌ها به تخلیهٔ هوایی نیاز دارند. |

Line conditions matter: `comms.02` requires the second approach to have been reported; `debrief.clean` requires no post damage and safe corridor truth. If civilian losses occurred, use the loss line and avoid an unqualified safety claim. Skipping marks presentation progress without creating combat/evidence facts. Story evidence is awarded from the actual mission completion event, then replayable independently of rewards.

## 7. Cinematic direction and camera ownership

User direction, 2026-09-08: follow M1/M2's **RTS start → smooth zoom and pan to important areas → return to RTS**. The first visible map frame must be the player's normal RTS framing, not an already-zoomed cinematic viewpoint. Comic completion hands off to that camera tour. M03 stores its start framing and restores that same valid focus, perspective/zoom, and orientation before control begins.

The inspected M1 owner has an initial RTS hold and establishing/hostile/return stages with a final threshold of 15,000 ms. M2's specialized stage thresholds are 750 / 3,250 / 4,250 / 6,750 ms, with 2.25-second focus/return requests and a one-second focus hold. These are code stage thresholds, not newly measured wall-clock durations. Later M2 acceptance moves its final focus to the canonical Barracks lot for placement. For M3, the user's requested return is the M03 starting tactical context, with both important areas authored inside its camera bounds.

| Shot | Trigger and duration proposal | Camera/control behavior | Abort/fallback |
|---|---|---|---|
| `shot.m03.intro.rts_start` | Ready map and comic handoff; first 0.75 seconds | Hold normal RTS view; capture starting focus, zoom/perspective and orientation. Gameplay clock not started. | Reduced motion stays here with optional static highlights. |
| `shot.m03.intro.post` | Smooth pan+zoom 2.25 seconds, then hold 1 second | Frame the restored post, nearby sensor and clinic corridor together. Motion begins progressively from the first frame. | Obstruction-tested pose; skip begins safe handback. |
| `shot.m03.intro.approach` | Smooth pan+zoom 2.25 seconds, then hold 1.5 seconds | Move to the fork/defensive lane, showing geography rather than hidden convoy positions. | No target snap before zoom settles; safe elevated focus. |
| `shot.m03.intro.return` | Smooth pan+zoom 2.25 seconds, then about 0.5 seconds settle | Restore the saved original RTS context. Complete handoff only when the camera/request path has actually settled. | Skip restores the same valid pose and ownership once. |
| `shot.m03.warning.focus` | Only on player Jump/SHOW ME, roughly 0.5–1 second | Focus the warning's world anchor; preserve selection and pending placement state. | Drag/command cancels the focus; unresolved anchor disables action with a reason. |
| `shot.m03.contact` | Player-requested inspection only during combat | Optional short emphasis on confirmed convoy; no forced letterbox or slow motion. | Danger, input, target death, pause, and result preempt it. |
| `shot.m03.finale` | Terminal victory after combat has resolved, 2–4 seconds | Show surviving post or truthful damage composition before result/debrief. | Skip, defeat, missing anchor, or reduced motion uses static result transition. |

The proposed full opening totals about 10.5 seconds, before any measured adjustment. Reuse `CampaignMissionOpeningPresentationComponent`, `CampaignMissionSpawnSystem.OpeningPresentation.cs`, `CampaignMissionPatrolOrderSystem`'s opening stages, `RuntimeCameraFocusRequestComponent`, and the existing RTS smooth-focus/perspective owners. Extend this path with bounded authored shot data; do not create a second cinematic camera system. The jet attack cinematic is a separate feature. No cinematic owns damage, warning timers, convoy activation, or victory. Sound priority is critical threat → essential tactical line → optional character flavor → ambience. One speech line at a time; captions retain critical facts if narration is muted or interrupted.

Camera evidence must show the starting RTS frame, the first moving frame, mid-transition, each focus/hold, mid-return, and final RTS frame at both aspect ratios. Verify position/focus/zoom/orientation restoration within explicit tolerances defined at the camera task, simultaneous pan/zoom progress, no blocked sightlines, and no input lock after skip. A smooth-looking endpoint screenshot alone cannot establish smooth motion.

## 8. Tutorials, guide, and ARIA

### 8.1 Fact-driven tutorial sequence

All step IDs below use prefix `tutorial.ch01.m03.`. A button press is an acknowledgement only for explanation steps; action steps require accepted command results or authoritative facts. Steps already satisfied by competent play are skipped. Neither an optional building nor a support use becomes a hidden victory requirement.

| Step suffix | Trigger → completion | SHOW ME | DO IT / authority |
|---|---|---|---|
| `01.read_warning` | First published warning → explicit read/dismiss or relevant route inspection | Highlight severity, source, confidence, and ETA. | Open the real warning surface only. |
| `02.inspect_route` | Valid warning → matching focus/route inspection acknowledgement | Focus exact route and contact line. | Camera focus only; never issues a movement order. |
| `03.choose_defense` | Approach understood → a valid defense choice/action or imminent contact | Show forward and inner positions with coverage previews. | No automatic strategic choice. |
| `04.build_option` | Player chooses construction → accepted placement transaction, or skip | Highlight real Build item and current valid footprint. | Only the chosen item and valid footprint, with real cost/confirmation; no purchase without explicit action. |
| `05.position_squads` | Squad out of useful position → accepted movement then actual arrival, or alternative viable position | Highlight selected squad and suggested lane. | One bounded Move request using normal reachability/faction validation. |
| `06.hold` | Squad arrives → accepted Hold command, or preexisting Hold truth | Show actual Hold button and resulting order banner. | One Hold command to the explicit friendly selection. |
| `07.stop` | Pending order or optional guide practice → accepted Stop/cancel result | Explain the actual command semantics and preserved selection. | One Stop command, never “stop the whole battle.” |
| `08.refresh` | New warning uncertainty and valid sensor → successful Ping or player skip | Show support availability, charges, and sensor coverage. | One validated Ping; cooldown/charge decremented only on acceptance. |
| `09.reinforce` | Player has chosen production and it is affordable → accepted queue then completion feedback | Open existing Barracks/rifle queue path. | One explicit production transaction; no repeated auto-queue. |
| `10.priority` | Confirmed dangerous vehicle at contact → hostile progress/defeat | Highlight why the confirmed armed vehicle matters. | Recommendation only during the first teaching pass; player chooses attacks. |
| `11.adapt` | Main-body route confirmed → reposition or maintained viable defense | Focus the warned second lane, not a hidden target. | No combat takeover. |
| `12.result` | Outcome settled → result review/dismiss | Explain the exact star lost and one useful retry lesson. | Navigation only. |

At imminent contact, suppress optional build/production teaching and prioritize the threat. Full mode explains each new concept; Contextual mode intervenes on first exposure or sustained uncertainty; Minimal mode keeps the objective and critical alerts. A 30-second ignored-warning escalation increases chip prominence once, with no forced takeover. Use cooldowns and an active recommendation identity so opening/closing panels cannot replay the same voice repeatedly.

### 8.2 Bilingual tutorial and warning copy draft

| Key suffix under `m03.` | English | فارسی |
|---|---|---|
| `title` | Radar Warning | هشدار راداری |
| `objective.defend` | Keep the forward post operational. | پایگاه مقدم را عملیاتی نگه دارید. |
| `objective.breach` | Keep the convoy outside the inner perimeter. | اجازه ندهید کاروان وارد محدودهٔ داخلی شود. |
| `objective.convoy` | Stop every vehicle and escort in the convoy. | همهٔ خودروها و نیروهای همراه کاروان را متوقف کنید. |
| `warning.estimated` | Estimated approach — field report | مسیر احتمالی — گزارش میدانی |
| `warning.confirmed` | Convoy route confirmed | مسیر کاروان تأیید شد |
| `warning.eta` | Estimated contact in {0} | زمان تقریبی تا تماس: {0} |
| `warning.stale` | Estimate outdated. Check the approach. | برآورد دیگر به‌روز نیست. مسیر ورودی را بررسی کنید. |
| `tutorial.read` | Read the direction and contact estimate before choosing a defense. | پیش از انتخاب محل دفاع، جهت حرکت و زمان تقریبی تماس را بررسی کنید. |
| `tutorial.route` | Inspect the marked approach. Your squads will keep their orders. | مسیر ورودی مشخص‌شده را بررسی کنید. دستور گروه‌ها تغییر نمی‌کند. |
| `tutorial.choose` | Defend the junction or keep a reserve near the post. | در تقاطع دفاع کنید یا یک گروه ذخیره نزدیک پایگاه نگه دارید. |
| `tutorial.build` | Place your chosen defense on valid ground. Check its cost first. | سازهٔ دفاعی انتخاب‌شده را در محل مجاز قرار دهید. ابتدا هزینهٔ آن را بررسی کنید. |
| `tutorial.move` | Move a squad into position before contact. | پیش از تماس با دشمن، یک گروه را در موضع دفاعی مستقر کنید. |
| `tutorial.hold` | Use Hold to defend this position. | برای دفاع از این موضع، «حفظ موضع» را انتخاب کنید. |
| `tutorial.stop` | Use Stop to cancel the squad's current order. | برای لغو دستور فعلی گروه، «توقف» را انتخاب کنید. |
| `tutorial.ping` | Use Radar Ping to refresh information from an active sensor. | برای به‌روزرسانی اطلاعات حسگر فعال، از پالس رادار استفاده کنید. |
| `tutorial.produce` | Queue one rifle squad if you want a reserve. | اگر به نیروی ذخیره نیاز دارید، تولید یک گروه تفنگدار را در صف قرار دهید. |
| `tutorial.priority` | The armed vehicle is firing. Keep the carrier out of the inner perimeter. | خودروی مسلح در حال شلیک است. نگذارید نفربر وارد محدودهٔ داخلی شود. |
| `tutorial.adapt` | The main body is approaching the other lane. Keep a reserve ready. | بخش اصلی کاروان از مسیر دیگر نزدیک می‌شود. نیروی ذخیره را آماده نگه دارید. |
| `ping.no_sensor` | No active sensor covers this approach. | هیچ حسگر فعالی این مسیر ورودی را پوشش نمی‌دهد. |
| `ping.empty` | No new contact confirmed. | تماس تازه‌ای تأیید نشد. |
| `ping.cooldown` | Radar Ping ready in {0}. | پالس رادار تا {0} دیگر آماده می‌شود. |
| `result.base_damage` | The post survived, but damage cost one star. | پایگاه حفظ شد، اما آسیب‌دیدگی آن باعث از دست رفتن یک ستاره شد. |
| `result.breach` | A convoy vehicle crossed the inner perimeter. Try intercepting earlier. | یکی از خودروهای کاروان وارد محدودهٔ داخلی شد. دفعهٔ بعد زودتر جلوی آن را بگیرید. |
| `guide.pause` | Field guide — battle paused | راهنمای میدانی — نبرد متوقف است |

Parameterized values are localized templates, not sentence fragments. Dynamic ETA/charge text should not have prerecorded spoken numbers that become stale. Speech can say “Check the updated estimate” while the live caption shows the exact current value. Final Hold/Stop copy must match validated behavior, including auto-engagement after Stop.

### 8.3 Field guide content and navigation

The guide can be opened from ARIA, Pause, a warning help action, and the result's relevant retry topic. Reuse existing shell popup/navigation ownership. Return to the exact entry surface, selection, and pause state. Long-form guide viewing never consumes the player's defense time silently.

Twelve core topics: mission purpose; reading warnings; source and confidence; Jump versus Move; preparing Tower/Barrier; rifle squads and production; Hold versus Stop; Radar Ping/sensor availability; convoy vehicle roles and target priority; core/clinic protection; stars/rewards/retry; class reference. Each topic contains one diagram or real UI capture, a short explanation, an example, a common mistake, and a “show this control” action only if the live target is valid.

Guide class cards include name, role, actual command capabilities, strengths, limits, unlock context, and portrait. Read numeric values from the canonical catalog projection. Later classes remain reference entries with truthful availability; naval design-only cards carry a clear unavailable state. No duplicated handwritten damage/range table. No story spoilers beyond M03.

### 8.4 ARIA command and personality limits

ARIA is procedural, observant, and willing to state uncertainty. Dalia contributes field judgment; Samira keeps civilian stakes present. Their dialogue supports a competent Commander and does not scold the player for using a different valid strategy.

SHOW ME only previews. DO IT performs one named, current, validated action after the player's explicit click. Give Control, if shown through the existing assistant, is restricted to one preview/action sequence and is visibly cancellable; it cannot manage the entire defense, spend repeatedly, attack unconfirmed contacts, or alter objectives. Player input, pause, scene change, result, dead target, lost ownership, stale recommendation, or expired attempt token cancels assistance. Rejection returns a specific localized reason without charge/spend or tutorial success.

ARIA must not congratulate a tower before completion, call a dismissed alert a stopped convoy, claim a Ping revealed enemies outside coverage, or announce civilian safety after recorded losses. Positive feedback should name the player's causal success: “Your reserve stopped the carrier before the post.” Only use it when those facts are available; otherwise use the simpler truthful “The convoy has been stopped.”

## 9. Localization, voice, readability, and accessibility

Use the existing `V3UiLocalizationCatalog.asset`, `GameText`, `GameLocalization`, speaker catalog, and narrative locale/audio pipeline. English and `fa-IR` receive every M03 string: Campaign card, briefing, objectives, warning state, build/production feedback, tutorial, guide/class card, pause, result, reward, retry, and archive. No second catalog, per-language prefab tree, or baked image text.

Preserve semantic reading order while handling Persian shaping, punctuation, mixed Latin callsigns, quantities, brackets, and time strings. Do not reverse stored text manually or mirror geographical direction. Unicode's bidirectional model distinguishes logical storage from display order; use the established renderer/shaping path and verify mixed-direction cases visually. [Unicode Bidirectional Algorithm](https://www.unicode.org/reports/tr9/).

Required review combinations: both languages × 16:9/20:9 × normal/large text; normal/reduced motion; voice on/off; subtitles on/off with critical warning information always visually available. Add longest localized line, mixed-script player identity, Arabic/Persian digit variants where supported, wrapping, safe area, live locale switching, and back navigation. Do not shrink Persian text until it fits at the expense of readability.

Switching language invalidates displayed text and pending speech, retains the same semantic step, and never resets the encounter. Long comic dialogue may finish only under an explicit stable policy; recommended behavior is stop old-language speech and allow replay of the current line in the selected language. Store guide progress and “seen” markers by stable ID, independent of translation.

Produce English and Persian voice manifests with line ID, locale, speaker, approved text hash, clip path, actual duration, priority, and caption binding. All mandatory story/tutorial lines receive both languages; UI-only guide prose need not become hours of narration. Core play works offline and silently. Existing font assets already have unrelated working-tree changes; preserve them during this planning task and audit ownership before any future font generation.

## 10. Fun-factor acceptance and tuning experiments

Fun is a measured playtest outcome, not a checkbox inferred from passing unit tests. The first slice should prove choice, readable consequences, recovery, and pacing before expensive final art/audio.

| Hypothesis | Measure | Proposed acceptance target / response if missed |
|---|---|---|
| A warning changes what the player does. | First meaningful focus/move/build after warning; post-run explanation. | At least 80% of first-time test players can explain direction and ETA. If missed, fix the source/focus presentation before adding more voice. |
| Preparation pays off. | Compare early useful preparation, late reaction, and idle control runs. | Clear reduction in damage/losses from preparation; idle must not reliably earn three stars. |
| There are real options. | Tower/barrier plan versus reinforcement/reserve plan on the same seeds. | Both achieve repeatable victory; no single mandatory placement click. |
| Pressure remains fair. | Actual warning lead, unseen damage, contact at camera/UI transitions. | Initial actionable lead within 45–75 seconds; no damage before a fair warning/control boundary. |
| Mistakes are recoverable. | Bad initial position, one squad lost, missed first Ping, sensor unavailable. | At least one viable recovery route for ordinary errors; failure explanation identifies a controllable action. |
| The mission stays active. | Idle time, longest period without a meaningful decision, replay completion. | Avoid unexplained dead periods over roughly 15 seconds after first contact. Change encounter spacing, not speech volume. |
| Assistance helps without replacing play. | Completed manual actions, repeated hints, player override. | No unsolicited combat takeover; no repeated identical voice in the same unchanged state. |
| Players want another attempt. | Immediate fun/fairness ratings and voluntary replay intention. | Directional target: median fun/fairness at least 4/5, at least 60% willing to retry for a better result. Report sample size and qualitative objections. |

Retain the chapter's initial balance hypotheses: one-star victory 75–90%, two-or-more stars 45–70%, three stars 20–40%, zero civilian deaths as the intended result. Define denominators as completed first attempts from players new to M03, separated by guidance mode and prior RTS familiarity. Initial 6–10-person rounds are diagnostic, not statistical certification. Test English and Persian players in each iteration and report cohort sizes; a fluent language QA reviewer separately checks all text/voice. Broader outcome-rate confidence requires a larger, planned sample.

Tune in this order: map/route reachability → information lead and visibility → player choice → available force/affordability → convoy spacing and target priorities → feedback → numbers → final polish. Change one explanatory variable per comparison. Suggested comparisons: 60 versus 75 seconds first lead; one versus two guided hints before contact; forward tower versus inner reserve; clear versus degraded sensor information. Use deterministic seeds for comparisons without claiming cross-platform bitwise combat determinism.

## 11. Delivery gates and unresolved decisions

| Gate | Reviewable result | Exit condition |
|---|---|---|
| G0 Contract | Identity, latest M02 baseline, playable roster, real cost/quantity matrix, map feasibility, ground-sensor choice, breach rules. | Every required capability has an exact owner and a feasible acceptance path. |
| G1 Graybox | Start → warning → prepare → two convoy elements → victory/defeat → retry. | Both defensive strategies work; warnings and terminal facts are truthful. |
| G2 Complete interaction | Full/Contextual/Minimal, guide, typed ARIA, Ping, real result/settlement, provisional bilingual media. | No tutorial deadlocks, fake actions, stale warning focus, duplicate grants, or locale-dependent difficulty. |
| G3 Playable acceptance | Representative recorded playthroughs and fun/recovery comparisons. | Gameplay timing is stable enough to freeze copy and final media. |
| G4 Content complete | Final seven-panel art set, crops, bilingual story/tutorial voice, guide, cinematics. | Actual in-game capture and listening review in both languages, including damaged/loss outcomes. |
| G5 Editor closeout | Focused tests, regressions, architecture/source-growth, lifecycle, memory/GC, multi-aspect capture. | Complete evidence and honest remaining device status. |
| G6 Device release | Android/Samsung device route if scheduled/authorized for the release. | Real device performance, touch, font/audio, thermal and memory evidence; otherwise explicitly deferred. |

G0 must prove the loaned Ground Radar Tank in the live scenario, actual four-member production cost, logical map dimensions versus weapon/contact range, barrier rerouting/breach behavior, reward quantities/duplicate semantics, and M04's visible-but-not-yet-playable state. The dish's Air-only enum is already resolved by source inspection. The recommended default decisions above allow planning to proceed; any changed scope must be recorded with consequences for copy, tests, and dependencies.

A scout-report fallback can make a prototype honest while detector integration is unfinished. It must be labeled “Field report,” must not render exact hidden enemy positions, and does not close the full Radar Ping acceptance item. No fallback may preserve a “functional radar” claim while omitting the function.

### Risk register and cut policy

| Risk | Early signal | Mitigation and acceptance consequence |
|---|---|---|
| Camera starts zoomed in or snaps before easing | First visible/first moving frame disagrees with M1/M2 reference. | Fix existing opening request/handshake; measure progressive pan/zoom and return. Opening camera is required scope, not optional polish. |
| Existing warning paths overwrite or clear each other | Dismissing or pausing removes an active route/ETA. | One canonical ledger plus presentation acknowledgements; pause and duplicate-source tests block acceptance. |
| Radar appears functional but cannot detect Ground | Air-only dish or unavailable ability wired to the tutorial. | Loan the actual Ground Radar Tank and implement/validate Ping. Scout fallback is prototype-only for the full requested scope. |
| Every class becomes a compulsory lesson | Guide and HUD overwhelm the first warning. | Keep all classes in the reference matrix, with five exact active identities and selected protected civilian variants; teach only M03 concepts. |
| Real combat resolves too quickly for intended pacing | Consistent early wipes or long idle gaps after first engagement. | Adjust routes, spacing, choices and force count from playtests; revise duration hypothesis if necessary. Do not pad with invulnerability or reading. |
| Barrier causes a softlock or hidden convoy bypass | No valid footprint-sized route, repeated path retries, or convoy inside the core unexpectedly. | Validate alternative/breach behavior at graybox stage. Keep the barrier in full-scope acceptance; do not silently ship a decorative obstacle. |
| Wrong legacy economy makes a recommended plan unaffordable | Real four-member order cost differs from authoring assumptions. | Freeze transaction-based cost matrix before publishing budget/copy. Resource top-ups from tutorial code are forbidden. |
| Persian changes difficulty or loses information | Reading/voice takes longer while ETA counts down; mixed-direction numbers reorder. | Shared simulation/pause policy, keyed text, real RTL/voice QA and language-switch tests. |
| Tutorial/camera work regresses M01/M02 | Shared helper grows or old intro/result sequence changes. | Bounded extraction, no source-growth exceptions by default, targeted M01/M02 regressions at each shared seam. |
| Final media is produced against unstable gameplay | Art or voice describes a feature later removed or changed. | Provisional assets through G3; final production follows stable copy and accepted timing. |
| Device quality is claimed from Editor | Device gate is absent but completion says release-ready. | Separate Editor and device completion with explicit evidence and deferral. |

If delivery needs narrowing, cut optional flavor speech, extra class-card illustrations, or optional in-combat inspection emphasis first. Keep the opening RTS tour, core comic beats, both languages, complete class reference coverage, truthful warning/Ping behavior, guide, ARIA command safety, retry/settlement, and required architecture checks. A scope reduction is recorded explicitly; it is never reported as completion of the original full plan.

## 12. Local source register

- [Chapter 1 M03 specification](SagaChapters/Saga_Chapter01_First_Response.md).
- [Campaign mission high-level contracts](Campaign_Mission_High_Level_Design_Catalog.md).
- [Narrative sequence/comic catalog](Campaign_Narrative_Sequence_And_Comic_Catalog.md).
- [Latest accepted M02 tracker](Architecture/m02_establish_base_implementation_tracker.md).
- [Feature readiness/exposure matrix](Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md).
- [FTUE and command assistant](FTUE_And_Command_Assistant_Design.md), [ARIA ECS design](ARIA_Assistant_ECS_Design.md).
- [Shared localization contract](../Documentation/V3_UI_LOCALIZATION.md).
- [ECS ownership contract](Architecture/gameplay_solid_ecs_contract.md), [source-growth baseline](Architecture/production_source_growth_baseline.md).

The companion architecture lists the inspected source files and exact proposed changes. No Unity runtime validation, gameplay acceptance, translation approval, or asset production is claimed by this planning document.
