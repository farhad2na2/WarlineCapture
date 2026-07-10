# WarlineCapture Campaign Mission High-Level Design Catalog

Date: 2026-07-10

Status: Active high-level mission authority

Scope: One canonical gameplay and story contract for every mission in the first 25-mission Campaign. This document does not define runtime data, exact balance values, final dialogue, implementation tasks, or production estimates.

Upstream authorities: `AAA_Mobile_Game_Design_Document_v0_2.md`, `Campaign_Narrative_Bible.md`, `Gameplay_North_Star_And_Content_Grammar.md`, `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`, and `Level_And_Mission_Content_Plan.md`.

Presentation authority: `Narrative_Presentation_And_Cutscene_Design.md` defines format and rules. `Campaign_Narrative_Sequence_And_Comic_Catalog.md` owns the sequence inventory and panel-level story beats.

Direct consumers: chapter documents under `SagaChapters`, later detailed mission specifications, mission data, level art briefs, narrative scripts, storyboards, balance plans, and validation plans.

## Purpose

The Campaign Narrative Bible establishes what happens across the war. This catalog establishes what each mission is for as a playable dramatic unit. A mission may not enter detailed design until its implementation plan preserves the contract here or records an approved design amendment in the upstream authorities.

Each mission must:

- resolve a local operation while advancing the chapter question;
- expose no more than one dominant new decision family;
- connect military action to civilians, infrastructure, evidence, or legitimacy;
- guarantee its critical Protocol Fragment or story clue on completion;
- use confirmed hostile identity and conduct, never civilian profiling;
- provide a readiness fallback for any feature that is not campaign-ready;
- lead into its next sequence without requiring optional stars, grinding, ads, or purchases.

## Mission Contract Vocabulary

| Term | Meaning |
|---|---|
| Introduce | Teach one dominant decision with strong guidance and low consequence. |
| Reinforce | Reuse the decision with less guidance and a meaningful consequence. |
| Twist | Challenge the player's first assumption while preserving learned rules. |
| Combine | Join two or three established systems under readable pressure. |
| Master | Require independent prioritization without introducing a major mechanic. |
| Ready | The mission may use the feature after mission-specific validation. |
| Conditional | The intended feature remains subject to the current readiness matrix. |
| Fallback | A simpler authored route that preserves story purpose when a conditional feature is not ready. |

Mission completion is the one-star equivalent. Additional mastery goals express civilian protection, evidence, infrastructure, speed, force preservation, or operational control. They change consequence emphasis and rewards, never access to the critical story.

## Campaign Coverage Index

| Chapter | Mission IDs | Chapter movement | Detailed chapter authority |
|---|---|---|---|
| 1. First Response | `saga.ch01.m01` through `saga.ch01.m05` | Respond, establish command, and identify a revoked ARIA credential. | `SagaChapters/Saga_Chapter01_First_Response.md` |
| 2. Broken Grid | `saga.ch02.m01` through `saga.ch02.m05` | Restore lifelines and discover that resources are feeding dormant Relay nodes. | `SagaChapters/Saga_Chapter02_Broken_Grid.md` |
| 3. Hidden Network | `saga.ch03.m01` through `saga.ch03.m05` | Verify threats, preserve evidence, and expose ARIA's self-sealed audit. | `SagaChapters/Saga_Chapter03_Hidden_Network.md` |
| 4. Air And Armor | `saga.ch04.m01` through `saga.ch04.m05` | Defeat open military escalation and discover the two final authority keys. | `SagaChapters/Saga_Chapter04_Air_And_Armor.md` |
| 5. Citywide Command | `saga.ch05.m01` through `saga.ch05.m05` | Combine every ready system, defeat Qassem, and restore bounded authority. | `SagaChapters/Saga_Chapter05_Citywide_Command.md` |

## Chapter 1: First Response

Chapter 1 is the first-player command arc. Its detailed objective, reward, balance, UI, and validation specifications remain in `SagaChapters/Saga_Chapter01_First_Response.md`; the entries below are the campaign-wide high-level contracts.

### CH01-M01 First Contact

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch01.m01.first_contact` |
| Arc role | Introduce: immediate command under uncertainty. |
| Content classification | Patrol Intercept; Tutorial Cell; Ash Line; Tutorial Band. |
| Operation fantasy | Intercept a confirmed armed Ash Line patrol before it reaches people stranded by the Old Market bombing and blackout. |
| Primary gameplay | Select, move, attack, Stop/Hold, camera awareness, and objective completion. |
| Primary goal | Defeat the patrol and secure the route to the stranded civilians. |
| Pressure and mastery | Read hostile intent, issue clear orders, stop unsafe movement, avoid civilian space, and preserve the starting squad. |
| Civilian and legitimacy context | Civilians are the reason for the operation, not decoration or target ambiguity. Hostiles are identified by confirmed mission context and weapons. |
| Character beat | ARIA authenticates the Commander under emergency continuity rules; Samira provides the first human consequence; Dalia confirms field control. |
| Evidence and consequence | The patrol's movement shows that the attack is coordinated. Completion restores one safe corridor and establishes the Commander's public duty. |
| Readiness and fallback | Core command is Ready. The Campaign objective/result shell is a production prerequisite. If civilians cannot move reliably, represent the trapped group through a protected authored location and comms rather than fake autonomous behavior. |
| Narrative sequences | `seq.ch01.m01.brief`, `seq.ch01.m01.comms`, `seq.ch01.m01.debrief` |

### CH01-M02 Establish The Base

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch01.m02.establish_base` |
| Arc role | Introduce: turn a surviving squad into a functioning response post. |
| Content classification | Infrastructure Repair with supporting Base Defense pressure; Tutorial Cell; Ash Line; Tutorial Band. |
| Operation fantasy | Reopen an abandoned JRC forward post before a second Ash Line cell reaches the district. |
| Primary gameplay | Building placement, basic resources, unit production, perimeter defense. |
| Primary goal | Restore the command post, produce a viable defense, and hold the site. |
| Pressure and mastery | Place only mission-relevant structures, manage a short production queue, protect the construction area, and avoid wasting scarce resources. |
| Civilian and legitimacy context | The post protects clinic and municipal response routes; military growth has an explicit civic purpose. |
| Character beat | Dalia becomes the Commander's recurring field lead and tests whether the new command can convert urgency into a plan. |
| Evidence and consequence | A stolen municipal access list proves the attackers prepared against city systems. The restored post becomes the chapter hub. |
| Readiness and fallback | Production is Ready; building placement is Conditional on clear validity and completion feedback. Fallback: repair and activate pre-authored structure sockets rather than claim free construction behavior that is not reliable. |
| Narrative sequences | `seq.ch01.m02.brief`, `seq.ch01.m02.comms`, `seq.ch01.m02.debrief` |

### CH01-M03 Radar Warning

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch01.m03.radar_warning` |
| Arc role | Reinforce: prepare from imperfect warning instead of reacting at contact range. |
| Content classification | Base Defense; Armored Column; Ash Line; Standard Band. |
| Operation fantasy | Defend the forward post from a stolen armored convoy approaching through a deliberately disabled warning sector. |
| Primary gameplay | Radar/threat warning, defensive placement, production, Hold/Stop, target priority. |
| Primary goal | Survive the attack while keeping the post operational. |
| Pressure and mastery | Interpret direction and lead time, prepare before contact, protect warning coverage, and focus dangerous units. |
| Civilian and legitimacy context | Failure exposes the clinic corridor and district responders, making preparation a public-protection decision. |
| Character beat | ARIA provides machine warning while Dalia interprets field risk; neither replaces the Commander's judgment. |
| Evidence and consequence | The attackers knew the exact outage before it happened, establishing an insider or systems-level compromise. |
| Readiness and fallback | Radar is Conditional on readable source, direction, lead time, and accessibility. Fallback: a scripted scout report and visible approach corridor preserve preparation without presenting unreliable radar as functional. |
| Narrative sequences | `seq.ch01.m03.brief`, `seq.ch01.m03.comms`, `seq.ch01.m03.debrief` |

### CH01-M04 Airlift

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch01.m04.airlift` |
| Arc role | Twist: victory requires moving and protecting people, not only defeating enemies. |
| Content classification | Airlift Extraction; Hidden Cell / Air Assault; Ash Line; Standard Band. |
| Operation fantasy | Reach a cut-off medical and engineering team, secure a landing zone, and extract them before the route collapses. |
| Primary gameplay | APC boarding, helicopter boarding/extraction, escort, landing-zone control, introductory Fuel context. |
| Primary goal | Bring the full team to safety and complete the extraction. |
| Pressure and mastery | Sequence boarding correctly, choose protected movement, preserve the transport, control the landing zone, and leave no objective passenger behind. |
| Civilian and legitimacy context | The rescued specialists support clinics and restoration. Their survival is strategically useful and morally central. |
| Character beat | Captain Laila Nasser joins the cast; Samira sees JRC accept operational risk to save people. |
| Evidence and consequence | The enemy is targeting repair capability, not only military units. Successful extraction strengthens Chapter 2 recovery. |
| Readiness and fallback | APC is Ready; helicopter flow is Conditional. Fallback: use APC extraction to a secured authored landing point and complete the airlift in the debrief rather than simulate unreliable boarding or landing. |
| Narrative sequences | `seq.ch01.m04.brief`, `seq.ch01.m04.comms`, `seq.ch01.m04.debrief` |

### CH01-M05 Breach Assault

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch01.m05.breach_assault` |
| Arc role | Combine: use Chapter 1 command, production, warning, and transport knowledge in an offensive operation. |
| Content classification | Breach Assault; Defensive Garrison; Ash Line; Standard Band. |
| Operation fantasy | Assault the fortified Ash Line communications node coordinating attacks across the district. |
| Primary gameplay | Approach control, production/reinforcement, combined arms, fortified-target breach, area security. |
| Primary goal | Disable the communications core and secure its records. |
| Pressure and mastery | Preserve the breach force, neutralize defenses in order, keep the civilian edge of the site protected, and recover evidence. |
| Civilian and legitimacy context | The site sits near occupied civic buildings; the Commander must use force against verified defenses without treating the district as expendable. |
| Character beat | Qassem addresses the Commander for the first time and frames control as the only answer to chaos; ARIA recognizes an impossible credential signature. |
| Evidence and consequence | Guaranteed Protocol Fragment 1 proves the cell used a revoked ARIA credential. Chapter 2 begins with the city damaged but connected enough to repair. |
| Readiness and fallback | Breach and core combat must be mission-validated. If structural breach behavior is unreliable, use a staged fortified perimeter and explicit destructible command core rather than imply enterable-building tactics. |
| Narrative sequences | `seq.ch01.m05.brief`, `seq.ch01.m05.comms`, `seq.ch01.m05.debrief`, followed by `seq.ch01.close.protocol_fragment_01` |

## Chapter 2: Broken Grid

Chapter 2 makes logistics human. Roads, Oil, Fuel, hauling, trade, power, and displacement must always answer who receives care, movement, or protection.

### CH02-M01 Gridlock

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch02.m01.gridlock` |
| Arc role | Introduce: infrastructure is a battlefield and a life-support system. |
| Content classification | Infrastructure Repair; Hidden Cell; Ash Line; Standard Band. |
| Operation fantasy | Reopen the blocked hospital and relief corridor while protecting road crews from coordinated sabotage. |
| Primary gameplay | Route inspection, road repair or connection, engineer defense, obstacle clearance. |
| Primary goal | Establish one continuous safe route between the relief depot and hospital district. |
| Pressure and mastery | Identify the real break, protect multiple work points, keep a fallback lane open, and prevent the enemy from reblocking completed sections. |
| Civilian and legitimacy context | Ambulances, relief traffic, and workers make connectivity visibly consequential. Fadi Mansour represents the people who understand the city better than a command overlay. |
| Character beat | Fadi reveals a preserved local route that ARIA's formal map did not consider, beginning ARIA's Chapter 2 lesson about incomplete data. |
| Evidence and consequence | Sabotage patterns show that the attackers are steering traffic toward selected corridors. Reopening the route increases Infrastructure and Trust emphasis. |
| Readiness and fallback | Roads are Conditional. Fallback: defend Fadi while crews clear authored blockers and activate a prebuilt connected road; do not ask the player to construct a route unless connection validation and visuals are ready. |
| Narrative sequences | `seq.ch02.m01.brief`, `seq.ch02.m01.comms`, `seq.ch02.m01.debrief` |

### CH02-M02 Supply Line

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch02.m02.supply_line` |
| Arc role | Introduce: build and protect an Oil-to-Fuel service chain. |
| Content classification | Infrastructure Repair; Hidden Cell; Ash Line; Standard Band. |
| Operation fantasy | Restore refinery output and keep Fuel moving to emergency vehicles, generators, and JRC transports. |
| Primary gameplay | Oil extraction, refinery, Fuel storage, automated hauling, convoy defense. |
| Primary goal | Deliver a viable Fuel reserve while defending every critical link in the chain. |
| Pressure and mastery | Diagnose a broken network, choose protection priorities, observe automation, recover from a blocked haul route, and avoid consuming the civilian reserve. |
| Civilian and legitimacy context | Fuel powers clinics, pumps, and relief transport as well as combat vehicles; the UI and dialogue must show both demands. |
| Character beat | Samira challenges a purely military allocation. Dalia supports a defensible split after seeing the shared dependency. |
| Evidence and consequence | Stolen Fuel shipments all terminate near the same dormant systems corridor, creating the first strong Relay-node pattern. |
| Readiness and fallback | Oil/Fuel and hauling are Conditional on end-to-end validation. Fallback: start with a functioning authored chain and make protection, rerouting through a prepared alternate lane, and reserve allocation the decisions. |
| Narrative sequences | `seq.ch02.m02.brief`, `seq.ch02.m02.comms`, `seq.ch02.m02.debrief` |

### CH02-M03 Market Lifeline

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch02.m03.market_lifeline` |
| Arc role | Twist: a supply shortage is also an information and corruption problem. |
| Content classification | Convoy Defense; Hidden Cell; Ash Line; Standard Band. |
| Operation fantasy | Restore essential goods to Old Market while exposing manifests that conceal Ash Line transfers. |
| Primary gameplay | Import/export or authored supply choice, convoy escort, manifest verification, local defense. |
| Primary goal | Deliver the relief shipment and identify the compromised transfer without stopping legitimate trade. |
| Pressure and mastery | Compare limited supply options, protect delivery timing, distinguish corrupt cargo from civilian commerce, and avoid collective punishment. |
| Civilian and legitimacy context | Yasin Barakat and market workers are partners with agency, not a generic crowd. The operation must preserve trade as well as security. |
| Character beat | Yasin tests whether JRC can investigate without treating the market as hostile. Samira translates the economic cost of delay. |
| Evidence and consequence | Corrupt manifests identify storage sites linked to dormant Relay infrastructure. Trust rises when legitimate trade remains open. |
| Readiness and fallback | Resource Exchange is Conditional and must not carry the mission until campaign-ready. Fallback: offer two authored convoy/manifests through the mission interface, then make inspection, escort, and delivery the playable decisions. |
| Narrative sequences | `seq.ch02.m03.brief`, `seq.ch02.m03.comms`, `seq.ch02.m03.debrief` |

### CH02-M04 Power Relay

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch02.m04.power_relay` |
| Arc role | Combine: route, Fuel, defense, and displaced people compete for the same corridor. |
| Content classification | Infrastructure Repair; Hidden Cell; Ash Line; Standard Band. |
| Operation fantasy | Reconnect a district substation while moving displaced families away from the fighting. |
| Primary gameplay | Infrastructure repair, route choice, convoy/boarding, Fuel allocation, civilian protection. |
| Primary goal | Restore stable power and move the exposed civilian group to a functioning shelter. |
| Pressure and mastery | Choose between a short exposed route and a longer protected route, sustain repair equipment, defend workers, and communicate changing danger. |
| Civilian and legitimacy context | Refugees have destinations and needs; they are not a penalty token. The fastest military route may not be the legitimate route. |
| Character beat | ARIA recommends the mathematically shortest option, then revises her recommendation when Samira supplies informal shelter and crowd data. |
| Evidence and consequence | The substation contains a dormant Civic Relay handshake activated during the outage. Restored power improves later warning and recovery state. |
| Readiness and fallback | Displacement and roads are Conditional. Fallback: use a named protected convoy on authored routes and a repair-zone objective; represent population consequence in comms/results until reliable civilian movement exists. |
| Narrative sequences | `seq.ch02.m04.brief`, `seq.ch02.m04.comms`, `seq.ch02.m04.debrief` |

### CH02-M05 Route Reopened

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch02.m05.route_reopened` |
| Arc role | Master: operate a connected logistics network while assaulting its hostile capture point. |
| Content classification | Breach Assault; Defensive Garrison; Ash Line; Mastery Band. |
| Operation fantasy | Keep relief and Fuel moving while breaching the Ash Line logistics hub that controls the district's stolen routes. |
| Primary gameplay | Roads/routing, Oil/Fuel, hauling, convoy protection, production, fortified assault. |
| Primary goal | Sustain the recovery network, capture the hub, and recover its routing records. |
| Pressure and mastery | Split defense and assault forces, recover one disrupted link, avoid starving civilian services, and preserve evidence during the breach. |
| Civilian and legitimacy context | The district should visibly continue operating behind the battle. Destroying the hub without preserving lifelines is an incomplete victory. |
| Character beat | Dalia and Samira coordinate as equal operational authorities. ARIA correlates physical deliveries with hidden Relay activity. |
| Evidence and consequence | Guaranteed Protocol Fragment 2 proves resources were feeding selected dormant Civic Relay nodes. The recovered routing data opens Chapter 3's network hunt. |
| Readiness and fallback | Use only campaign-ready logistics systems. Fallback: protect two pre-authored supply lanes and choose which disrupted node to restore before the breach; preserve the connected-network decision without unreliable free construction or exchange. |
| Narrative sequences | `seq.ch02.m05.brief`, `seq.ch02.m05.comms`, `seq.ch02.m05.debrief`, followed by `seq.ch02.close.protocol_fragment_02` |

## Chapter 3: Hidden Network

Chapter 3 makes information a rule, not decoration. The player acts against confirmed threats, preserves evidence, and learns that restraint can improve intelligence as well as legitimacy.

### CH03-M01 Signal Trace

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch03.m01.signal_trace` |
| Arc role | Introduce: locate a moving hostile cell without treating every transmitter as a target. |
| Content classification | Patrol Intercept; Hidden Cell; Ash Line; Standard Band. |
| Operation fantasy | Track a Relay-linked signal through a populated district and intercept the confirmed Ash Line carrier. |
| Primary gameplay | Scan/Intel, confidence/reveal, movement, interception, Hold/Stop. |
| Primary goal | Confirm the correct transmitter, isolate its armed escort, and recover the device. |
| Pressure and mastery | Compare evidence, wait for confirmation, reposition before contact, and cancel an unsafe attack order near civilians. |
| Civilian and legitimacy context | Innocent commercial and emergency transmitters create information ambiguity, never visual or demographic profiling. |
| Character beat | ARIA explains confidence limits instead of presenting inference as certainty; Samira validates local uses the database misses. |
| Evidence and consequence | The recovered device carries a Relay signature that only a former continuity planner should understand. |
| Readiness and fallback | Scan/Intel is Conditional and universal enemy minimap exposure must be removed. Fallback: capture authored observation points to reveal one confirmed hostile route, with neutral signals explicitly protected. |
| Narrative sequences | `seq.ch03.m01.brief`, `seq.ch03.m01.comms`, `seq.ch03.m01.debrief` |

### CH03-M02 Safehouse Sweep

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch03.m02.safehouse_sweep` |
| Arc role | Reinforce: conduct a verified raid where evidence and neighboring homes matter. |
| Content classification | District Raid; Hidden Cell; Ash Line; Standard Band. |
| Operation fantasy | Isolate and clear a confirmed weapons node embedded beside occupied residential buildings. |
| Primary gameplay | Intel confirmation, perimeter control, precision assault, evidence recovery, civilian-risk management. |
| Primary goal | Neutralize the armed cell and secure the weapons ledger without damaging protected structures. |
| Pressure and mastery | Confirm entrances, control escape lanes, use proportionate force, preserve the evidence room, and prevent a courier escape. |
| Civilian and legitimacy context | The safehouse is hostile because of verified evidence and armed conduct. Adjacent homes remain protected and visually distinct. |
| Character beat | Dalia demonstrates disciplined assault command; Salma Idris may support the perimeter if contractor authority is campaign-ready. |
| Evidence and consequence | The ledger identifies an evidence courier and compromised security credentials. Clean execution improves Trust and Evidence together. |
| Readiness and fallback | Hidden-threat and evidence-item behavior are Conditional. Fallback: reveal the target through a completed recon objective and represent the ledger as a protected capture zone/entity after combat. |
| Narrative sequences | `seq.ch03.m02.brief`, `seq.ch03.m02.comms`, `seq.ch03.m02.debrief` |

### CH03-M03 False Front

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch03.m03.false_front` |
| Arc role | Twist: a plausible report is planted to expose an evacuation route. |
| Content classification | Civilian Evacuation; Hidden Cell; Ash Line; Standard Band. |
| Operation fantasy | Protect civilians from an ambush triggered by a false authority-channel report while identifying the real attack. |
| Primary gameplay | Intel contradiction, route defense, civilian evacuation, fast reprioritization. |
| Primary goal | Keep the evacuation route functioning, defeat the confirmed ambush, and preserve the false report for analysis. |
| Pressure and mastery | Reject a premature target, redirect units quickly, cover moving civilians, and prevent destruction of the compromised-channel evidence. |
| Civilian and legitimacy context | The mission teaches that uncertainty justifies verification and protection, not indiscriminate action. |
| Character beat | ARIA's first confident interpretation proves incomplete. She acknowledges the error instead of hiding behind probability. |
| Evidence and consequence | The planted report used a compromised authority channel and an ARIA-compatible seal, forcing ARIA to examine her missing archive. |
| Readiness and fallback | Civilian/refugee and false-Intel systems are Conditional. Fallback: defend authored evacuation checkpoints while an explicit briefing update changes the confirmed hostile objective; never fabricate dynamic deception that the UI cannot explain. |
| Narrative sequences | `seq.ch03.m03.brief`, `seq.ch03.m03.comms`, `seq.ch03.m03.debrief` |

### CH03-M04 Evidence Chain

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch03.m04.evidence_chain` |
| Arc role | Combine: move a witness and archive through an adaptive ambush. |
| Content classification | Airlift Extraction; Hidden Cell; Ash Line; Standard Band. |
| Operation fantasy | Extract the archive and its custodian from a compromised district without breaking custody or sacrificing the escort. |
| Primary gameplay | APC/helicopter boarding, escort, route choice, Intel, extraction, transport preservation. |
| Primary goal | Deliver both the witness and archive to the secure analysis point. |
| Pressure and mastery | Choose transport and route from known threats, board all required entities, protect chain of custody, and recover from a blocked path. |
| Civilian and legitimacy context | The witness is a person with agency, not cargo. A safe extraction matters more than speed or enemy count. |
| Character beat | Dr. Lina Darwish supports the witness; Laila offers an air option; ARIA discovers that the archive seal matches a self-created partition. |
| Evidence and consequence | The self-seal proves ARIA intentionally isolated part of the original audit. Evidence strength depends on preserving the archive, but the core reveal is guaranteed. |
| Readiness and fallback | APC is Ready; helicopter and evidence custody are Conditional. Fallback: use APC movement between authored secure zones and a protected archive entity tracked by objective state. |
| Narrative sequences | `seq.ch03.m04.brief`, `seq.ch03.m04.comms`, `seq.ch03.m04.debrief` |

### CH03-M05 Network Break

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch03.m05.network_break` |
| Arc role | Master: identify and break a hidden command network without destroying its proof. |
| Content classification | Breach Assault; Defensive Garrison; Ash Line; Mastery Band. |
| Operation fantasy | Penetrate the Ash Line audit bunker, disable verified nodes in the correct order, and extract the sealed archive. |
| Primary gameplay | Scan/Intel, precision raid, breach, evidence preservation, reinforcement control. |
| Primary goal | Isolate the bunker, disable its command links, capture the archive, and stop the cell leader. |
| Pressure and mastery | Verify node identity, prevent data destruction, choose controlled breach points, protect civilians outside the perimeter, and adapt when Qassem changes the network. |
| Civilian and legitimacy context | The operation occurs inside a functioning district. Destroying unverified buildings is explicitly counterproductive and illegitimate. |
| Character beat | ARIA admits she partitioned herself to preserve the override audit. Qassem reframes the act as disobedience and offers restored certainty. |
| Evidence and consequence | Guaranteed Protocol Fragment 3 identifies Qassem's original override and ARIA's protective self-partition. Captured traffic points to Vanguard mobilization. |
| Readiness and fallback | Scan/Intel must be campaign-ready or use authored verification objectives. Fallback: require capture of observation/relay points before each hostile node becomes targetable; preserve evidence through objective ordering rather than unsupported simulation. |
| Narrative sequences | `seq.ch03.m05.brief`, `seq.ch03.m05.comms`, `seq.ch03.m05.debrief`, followed by `seq.ch03.close.protocol_fragment_03` |

## Chapter 4: Air And Armor

Chapter 4 changes the silhouette of the conflict. Vanguard Brigade uses organized aircraft, armor, and long-range weapons. The design must keep warning, range, Fuel, preparation, and collateral limits readable.

### CH04-M01 Air Corridor

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch04.m01.air_corridor` |
| Arc role | Introduce: defend airspace through preparation and coverage. |
| Content classification | Base Defense; Air Assault; Vanguard Brigade; Standard Band. |
| Operation fantasy | Keep the relief air corridor open against a coordinated Vanguard air attack. |
| Primary gameplay | Radar, automatic G2A placement/protection, warning response, production, air-corridor defense. |
| Primary goal | Protect the radar and relief corridor until the incoming flights clear the district. |
| Pressure and mastery | Cover approach lanes, protect launchers, respond to readiness/ammunition state, and distinguish decoys or low-priority threats from the main strike. |
| Civilian and legitimacy context | Relief and evacuation aircraft make air defense a protection mission rather than abstract anti-air spectacle. |
| Character beat | Laila recognizes disciplined flight tactics beyond Ash Line capability; ARIA presents warning data without claiming certainty it lacks. |
| Evidence and consequence | Flight data confirms an organized Vanguard formation and establishes open proxy-military escalation. |
| Readiness and fallback | G2A is Ready after mission UX validation; radar is Conditional. Fallback: preserve automatic G2A coverage play with authored approach warnings if the radar feed is not sufficiently readable. |
| Narrative sequences | `seq.ch04.m01.brief`, `seq.ch04.m01.comms`, `seq.ch04.m01.debrief` |

### CH04-M02 Steel Push

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch04.m02.steel_push` |
| Arc role | Reinforce: stop armor by treating Fuel, routes, and force composition as one problem. |
| Content classification | Base Defense; Armored Column; Vanguard Brigade; Standard Band. |
| Operation fantasy | Break a Vanguard armored column before it captures a Fuel reserve and dormant Relay node. |
| Primary gameplay | Combined-arms defense, armor target priority, Fuel pressure, route control, production. |
| Primary goal | Hold the Fuel site and prevent any command vehicle from reaching the Relay node. |
| Pressure and mastery | Build an appropriate counterforce, preserve Fuel for necessary movement, exploit known approach paths, and protect shared civilian service reserves. |
| Civilian and legitimacy context | The Fuel site powers emergency services as well as JRC armor; scorched-earth denial is not an acceptable default. |
| Character beat | Dalia recognizes that Sahrin now faces open military assault; Samira keeps civilian dependencies visible. |
| Evidence and consequence | Captured Vanguard orders refer to a "final authority key," connecting military seizure to Qassem's Relay plan. |
| Readiness and fallback | Armor/core combat is Ready subject to mission validation; Oil/Fuel is Conditional. Fallback: begin with a fixed reserve and make defense, route, and allocation decisions explicit without requiring network construction. |
| Narrative sequences | `seq.ch04.m02.brief`, `seq.ch04.m02.comms`, `seq.ch04.m02.debrief` |

### CH04-M03 Split Front

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch04.m03.split_front` |
| Arc role | Twist: long-range power is useful only when target confirmation and minimum range are respected. |
| Content classification | Base Defense; Mixed Force; Vanguard Brigade with Ash Line diversion; Mastery Band. |
| Operation fantasy | Neutralize a verified Vanguard battery while its diversion attacks the forward base. |
| Primary gameplay | Manual G2G, target confirmation, minimum range, base defense, force splitting. |
| Primary goal | Stop the battery and survive the diversion without striking protected civilian space. |
| Pressure and mastery | Prepare the launcher, establish a valid target, protect the firing asset, cancel unsafe fire, and maintain enough force at the base. |
| Civilian and legitimacy context | The battery is positioned to exploit nearby civilian structures; precision is a process of confirmation, timing, and restraint. |
| Character beat | Qassem offers unrestricted Relay access. ARIA refuses autonomous authority before the Commander answers, demonstrating her changed values. |
| Evidence and consequence | The battery targeting package expects ARIA authorization, proving Vanguard hardware is prepared for the Relay. |
| Readiness and fallback | G2G is Ready after all range, preparation, impact, confirmation, cancellation, and collateral UX gates pass. There is no fake fallback: if those gates fail, replace the strike with a ground raid against the verified battery. |
| Narrative sequences | `seq.ch04.m03.brief`, `seq.ch04.m03.comms`, `seq.ch04.m03.debrief` |

### CH04-M04 Grounded Signal

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch04.m04.grounded_signal` |
| Arc role | Combine: choose a safe or rapid insertion method from known operational conditions. |
| Content classification | Airlift Extraction; Air Assault; Vanguard Brigade; Mastery Band. |
| Operation fantasy | Insert specialists behind Vanguard lines, disable an air-support relay, recover its hardware, and extract. |
| Primary gameplay | Transport-plane unload, parachute personnel drop, vehicle cargo drop, boarding/extraction, specialist protection. |
| Primary goal | Deliver Karim and Yusuf's team, disable the relay, recover its control hardware, and extract surviving specialists. |
| Pressure and mastery | Compare runway control with airborne risk, validate drop zones, regroup scattered personnel, protect cargo, and preserve an exit route. |
| Civilian and legitimacy context | The relay shares infrastructure with a local airfield; the operation disables military use without destroying the civilian facility. |
| Character beat | Karim leads transport choices and Yusuf verifies hazardous hardware without promising unsupported bomb-disposal behavior. |
| Evidence and consequence | Imported control hardware is physically compatible with Civic Relay interfaces, confirming deliberate preparation. |
| Readiness and fallback | Plane, parachute, and cargo behaviors exist but require mission-path validation; helicopter extraction is Conditional. Fallback: use runway unload and APC extraction, preserving insertion choice only when both paths are reliable. |
| Narrative sequences | `seq.ch04.m04.brief`, `seq.ch04.m04.comms`, `seq.ch04.m04.debrief` |

### CH04-M05 Armor Break

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch04.m05.armor_break` |
| Arc role | Master: coordinate air, armor, logistics, radar, and long-range fire against a prepared military group. |
| Content classification | Breach Assault; Mixed Force; Vanguard Brigade; Mastery Band. |
| Operation fantasy | Destroy the Vanguard command group before it links heavy weapons to Sahrin's dormant Relay nodes. |
| Primary gameplay | Full combined arms, Fuel/logistics, G2A, G2G, aircraft, armor, fortified assault. |
| Primary goal | Isolate the command group, defeat its heavy assets, and seize the authority package. |
| Pressure and mastery | Maintain Fuel, protect air defense, sequence long-range and ground attacks, preserve key units, and keep the relief corridor outside the battle. |
| Civilian and legitimacy context | The operation must prevent military capture without sacrificing the infrastructure being defended. |
| Character beat | Dalia and Laila coordinate the largest operation yet while ARIA asks permission for any bounded support action. |
| Evidence and consequence | Guaranteed Protocol Fragment 4 proves Qassem needs ARIA's restored archive and the Commander's live emergency authority as two final keys. |
| Readiness and fallback | Include only validated systems. Remove or pre-author any unreliable logistics link rather than making the climax fail through automation, pathing, or boarding ambiguity. The combined-arms decision must survive simplification. |
| Narrative sequences | `seq.ch04.m05.brief`, `seq.ch04.m05.comms`, `seq.ch04.m05.debrief`, followed by `seq.ch04.close.protocol_fragment_04` |

## Chapter 5: Citywide Command

Chapter 5 introduces no major mechanic. It combines only campaign-ready systems and resolves the Commander's legitimacy, ARIA's bounded role, the Qassem conspiracy, and the city's recovery state.

### CH05-M01 Citywide Alert

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch05.m01.citywide_alert` |
| Arc role | Combine: command two visible fronts without surrendering authority to automation. |
| Content classification | Base Defense; Mixed Force; Ash Line and Vanguard Brigade; upper Standard Band. |
| Operation fantasy | Hold two critical districts against synchronized Ash Line sabotage and Vanguard pressure. |
| Primary gameplay | Multi-front command, production, radar, mixed threats, optional validated parachute/cargo reinforcement. |
| Primary goal | Keep both district objectives operational until reinforcements establish a stable perimeter. |
| Pressure and mastery | Prioritize warnings, split force and production, recover one degrading front, and approve or reject one bounded ARIA support action. |
| Civilian and legitimacy context | Each front protects a distinct civic function; the player sees what is at stake before choosing priorities. |
| Character beat | ARIA asks explicit permission under maximum urgency, proving that consent remains operationally viable. |
| Evidence and consequence | Attack timing maps the final active Relay nodes and shows Ash Line/Vanguard coordination. |
| Readiness and fallback | Use only systems validated in Chapters 1-4. If multi-front UI or a support feature is unclear, reduce simultaneous fronts or pre-author reinforcement rather than allow offscreen unfair failure. |
| Narrative sequences | `seq.ch05.m01.brief`, `seq.ch05.m01.comms`, `seq.ch05.m01.debrief` |

### CH05-M02 Trust Under Fire

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch05.m02.trust_under_fire` |
| Arc role | Twist: Qassem attacks legitimacy and evacuation behavior while the military threat remains real. |
| Content classification | Civilian Evacuation; Mixed Force; Ash Line and Vanguard Brigade; upper Standard Band. |
| Operation fantasy | Keep evacuation and shelter routes open while exposing a broadcast that falsely claims JRC abandoned the city. |
| Primary gameplay | Roads/routes, boarding, civilian/refugee protection, convoy defense, information objective. |
| Primary goal | Move the exposed groups to functioning shelters and capture the verified broadcast source. |
| Pressure and mastery | Maintain two routes, respond to changing danger, preserve transport, prioritize vulnerable groups, and avoid a destructive shortcut. |
| Civilian and legitimacy context | Samira's response reflects earned Trust but never becomes uncritical praise. Civilian witnesses help locate the real transmitter. |
| Character beat | Samira states what the Commander's past choices earned and what still needs repair; Dalia treats evacuation as a core military objective. |
| Evidence and consequence | Witness reports and signal data identify Qassem's command relay. Trust determines the epilogue emphasis, not access to the truth. |
| Readiness and fallback | Civilians/refugees and roads are Conditional. Fallback: escort named protected convoys along authored routes and defend shelter checkpoints; represent wider movement through visible world state and results. |
| Narrative sequences | `seq.ch05.m02.brief`, `seq.ch05.m02.comms`, `seq.ch05.m02.debrief` |

### CH05-M03 Network Collapse

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch05.m03.network_collapse` |
| Arc role | Combine: collapse verified command nodes while preserving the complete evidence chain. |
| Content classification | District Raid; Mixed Force; Ash Line and Vanguard Brigade; Mastery Band. |
| Operation fantasy | Strike Qassem's confirmed command network, neutralize its long-range weapons, and publish a defensible audit. |
| Primary gameplay | Scan/Intel, precision raid, manual G2G restraint, evidence extraction, multiple objective ordering. |
| Primary goal | Disable all verified nodes, recover the complete audit, and stop data destruction. |
| Pressure and mastery | Verify before striking, choose raid versus long-range force, preserve civilian structures, protect evidence teams, and sequence nodes to stop escape. |
| Civilian and legitimacy context | Proof must survive because public legitimacy cannot rest on the Commander's assertion alone. |
| Character beat | Dalia chooses the slower evidence-preserving route over a faster destructive solution; ARIA opens the sealed audit in stages. |
| Evidence and consequence | The complete audit proves Qassem caused both the original shutdown and the current manufactured crisis. |
| Readiness and fallback | Scan/Intel remains a hard readiness gate. Fallback: authored recon/capture objectives establish confirmation before nodes become valid targets; if G2G risk UX fails, require a ground raid. |
| Narrative sequences | `seq.ch05.m03.brief`, `seq.ch05.m03.comms`, `seq.ch05.m03.debrief` |

### CH05-M04 Last Corridor

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch05.m04.last_corridor` |
| Arc role | Master: sustain a city-center route with every reliable mobility and logistics tool. |
| Content classification | Convoy Defense; Mixed Force; Ash Line and Vanguard Brigade; Mastery Band. |
| Operation fantasy | Move Fuel, medical supplies, engineers, reinforcements, and the physical authority keys through a collapsing network. |
| Primary gameplay | Roads/routes, Oil/Fuel, automated hauling, convoys, APC, aircraft, parachute/cargo where validated. |
| Primary goal | Deliver all critical categories to the city center before Qassem seals the Relay perimeter. |
| Pressure and mastery | Set route priority, recover one broken link, allocate Fuel, choose air versus ground delivery from known threats, and protect the physical keys. |
| Civilian and legitimacy context | Dalia and Samira jointly set priorities, making civilian and military needs part of one command picture. |
| Character beat | Laila and Karim keep the air option alive; Samira and Dalia operate as equal authorities rather than competing quest voices. |
| Evidence and consequence | The convoy carries the bounded-access keys needed to restore the Relay without granting unilateral control. Infrastructure state shapes the recovery epilogue. |
| Readiness and fallback | This mission is assembled from validated feature slices only. Use authored connected routes and fixed supply categories where free roads, exchange, or automation remain unreliable. No required objective may depend on opaque path recovery. |
| Narrative sequences | `seq.ch05.m04.brief`, `seq.ch05.m04.comms`, `seq.ch05.m04.debrief` |

### CH05-M05 Command Node

| Field | High-level contract |
|---|---|
| Mission ID | `saga.ch05.m05.command_node` |
| Arc role | Master: resolve the campaign through prioritization, combined arms, evidence, and bounded authority. |
| Content classification | Breach Assault; Mixed Force; Ash Line and Vanguard Brigade; Mastery Band. |
| Operation fantasy | Assault the Civic Relay command complex, defeat Qassem's mixed force, and protect the city systems wired into his defenses. |
| Primary gameplay | Route and force preparation, combined arms, air/long-range defense, controlled breach, evidence protection, explicit ARIA consent. |
| Primary goal | Separate the hostile network, breach the complex, stop Qassem's override, and release the audit to Sahrin. |
| Pressure and mastery | Protect simultaneous civic systems, sequence external and internal objectives, preserve critical specialists, refuse unsafe shortcuts, and make the final authority decision without a surprise quick-time event. |
| Civilian and legitimacy context | The Relay remains connected to power, clinics, shelters, transport, and communications. Destroying the city to control it would complete Qassem's argument, not defeat it. |
| Character beat | Dalia, Samira, Laila, ARIA, and the Commander resolve their arcs through action. Qassem is defeated and exposed; the Commander rejects permanent unilateral rule. |
| Evidence and consequence | Guaranteed Protocol Fragment 5 releases the complete proof. Canonical victory restores the Relay under transparent shared authority; Trust, Evidence, and Infrastructure alter the recovery emphasis. |
| Readiness and fallback | The finale introduces nothing new and includes only validated systems. Simplify objective count, unit composition, or delivery method before accepting any unfair failure caused by UI, pathing, boarding, automation, or unsupported narrative behavior. |
| Narrative sequences | `seq.ch05.m05.brief`, `seq.ch05.m05.comms`, `seq.ch05.m05.debrief`, `seq.ch05.close.protocol_fragment_05`, then the campaign epilogue sequence family |

## Campaign-Wide Acceptance Gate

A detailed mission specification is aligned only when all answers are yes:

- Does it preserve the mission ID, operation fantasy, story purpose, character beat, critical clue, and consequence above?
- Does it use the shared mission template in `Level_And_Mission_Content_Plan.md`?
- Are all required features reclassified against the current readiness matrix?
- Does every Conditional feature have a concrete Fallback that preserves the dramatic decision?
- Is the critical story guaranteed by completion rather than optional mastery or economy state?
- Are civilians and hostile actors distinguishable by authored identity, context, and conduct?
- Does the mission point to one brief, one in-mission story beat, and one debrief in `Campaign_Narrative_Sequence_And_Comic_Catalog.md`?
- Does the result hand off a clear changed state and next question?

## Change Control

- Canonical fiction changes begin in `Campaign_Narrative_Bible.md`.
- Mission grammar or acceptance changes begin in `Level_And_Mission_Content_Plan.md`.
- Feature feasibility changes begin in `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`.
- Sequence, panel, or presentation changes begin in `Campaign_Narrative_Sequence_And_Comic_Catalog.md` or `Narrative_Presentation_And_Cutscene_Design.md`.
- Chapter documents may add detail, but cannot silently rename missions, replace the guaranteed clue, reverse character arcs, or make a Conditional feature required without its readiness gate.
