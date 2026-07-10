# WarlineCapture Campaign Narrative Sequence And Comic Catalog

Date: 2026-07-10

Status: Active high-level campaign sequence authority

Scope: Complete sequence inventory and high-level panel/communication beats for the first 25-mission Campaign. This is not a final script, storyboard package, image-generation prompt library, runtime schema, or implementation plan.

Upstream authorities: `AAA_Mobile_Game_Design_Document_v0_2.md`, `Campaign_Narrative_Bible.md`, `First_Player_Experience_And_Story_Onboarding_Design.md`, and `Narrative_Presentation_And_Cutscene_Design.md`.

Mission authority: `Campaign_Mission_High_Level_Design_Catalog.md` owns the playable purpose and outcome of each mission.

Direct consumers: chapter documents, later scripts, storyboards, character/location art briefs, audio and localization plans, sequence-player implementation, Story Archive implementation, and narrative validation.

## Purpose

This catalog closes the gap between a campaign story map and producible narrative content. Every critical-path mission has a traceable briefing, in-mission character beat, and debrief. Every chapter has an opening and a Protocol Fragment close. The Campaign has a complete opening, identity handoff, canonical ending, consequence emphasis, and postscript.

The sequence system must make the campaign feel continuous without interrupting tactical play excessively. It uses a tactical motion-comic language: authored layered images, restrained parallax and camera movement, selective animation, portrait communications, subtitles, sound design, and music. Gameplay remains the central action.

## Authority Boundaries

| Authority | Owns |
|---|---|
| `Campaign_Narrative_Bible.md` | Canonical setting, cast, mystery, facts, chapter reveals, and ending. |
| `Narrative_Presentation_And_Cutscene_Design.md` | Format, tiers, duration bands, continuity, accessibility, cultural review, and AI-assisted asset policy. |
| This catalog | Exact campaign sequence inventory, stable sequence IDs, high-level panel beats, speakers, transitions, and Story Archive grouping. |
| `Campaign_Mission_High_Level_Design_Catalog.md` | The playable operation before and after each sequence. |
| Later script/storyboard packages | Final dialogue, shot composition, timing, acting, animation, sound cues, and approved art. |

If a later script changes a canonical fact or mission outcome, it must update the upstream authority first. If it adds or removes a critical-path sequence, it must update this catalog and its coverage totals.

## Sequence Grammar

| Tier | Campaign use | Target form |
|---|---|---|
| A | Campaign prologue, final pre-assault where warranted, and canonical epilogue. | Full-screen tactical motion comic, 5-10 panels, strong audio arc. |
| B | Chapter opening and Protocol Fragment close. | Full-screen tactical motion comic, 4-8 panels. |
| C | Mission brief and debrief. | Two to four economical panels or an interactive briefing surface with equivalent story coverage. |
| D | In-mission communication. | Three-to-eight-second portrait/voice/subtitle beat that does not steal control or obscure urgent state. |
| E | Story Archive recap and consequence review. | Player-paced replay of already unlocked sequences, evidence, and recaps. |

Every mission uses this three-beat rhythm:

```text
Brief: what changed, who is affected, and what the Commander must do.
Comms: a character or evidence beat that changes the player's understanding during action.
Debrief: what the operation changed, what it cost, and what question comes next.
```

Final dialogue is intentionally absent. Panel numbering describes information order, not locked cinematography.

## Stable ID Convention

| Content | Pattern | Example |
|---|---|---|
| Prologue | `seq.prologue.<slug>` | `seq.prologue.command_lost` |
| Chapter opening | `seq.chNN.open.<slug>` | `seq.ch02.open.broken_grid` |
| Mission brief | `seq.chNN.mNN.brief` | `seq.ch03.m02.brief` |
| In-mission communication | `seq.chNN.mNN.comms` | `seq.ch03.m02.comms` |
| Mission debrief | `seq.chNN.mNN.debrief` | `seq.ch03.m02.debrief` |
| Chapter close | `seq.chNN.close.protocol_fragment_NN` | `seq.ch03.close.protocol_fragment_03` |
| Epilogue | `seq.campaign.epilogue.<slug>` | `seq.campaign.epilogue.canonical` |
| Postscript | `seq.campaign.postscript.<slug>` | `seq.campaign.postscript.recovery_watch` |

IDs are stable content identities. Changing panel count, art, audio, or final wording does not justify renaming an ID.

## Complete Inventory

| Sequence family | Count | Required on critical path |
|---|---:|---|
| Prologue and Commander identity bridge | 2 | Yes on first launch; replayable afterward. |
| Chapter openings | 5 | Yes, with first-launch Chapter 1 opening allowed to flow directly from the prologue. |
| Mission briefs | 25 | Yes; Chapter 1 M01 may be interactive after its opening sequence. |
| In-mission communications | 25 | Yes, but timing may defer during urgent player input. |
| Mission debriefs | 25 | Yes; skippable after result state is safely recorded. |
| Protocol Fragment chapter closes | 5 | Yes; the core reveal cannot depend on optional mastery. |
| Canonical epilogue | 1 | Yes after Chapter 5 completion. |
| Trust, Evidence, and Infrastructure emphasis sequences | 3 | Exactly the applicable state variation is composed with the canonical ending. |
| Recovery postscript | 1 | Yes on first completion; optional on replay. |
| Total planned sequence records | 92 | Full high-level Campaign coverage. |

All 92 records unlock into the Story Archive as viewed or earned. The three consequence records contain authored high/low state alternatives; they do not create mutually exclusive secret endings.

## Prologue And Identity

### `seq.prologue.command_lost`

Tier A, first-launch Campaign bookend, 7 panels.

1. Morning Sahrin: Old Market opens, clinic staff receive supplies, road crews work, aircraft cross a functioning skyline.
2. Coordinated blasts and a power failure cut across separate districts; the violence is sudden, not the city's normal identity.
3. JRC command channels fragment into unanswered calls, damaged relays, and contradictory status markers.
4. Dalia's field unit pulls survivors from a disabled convoy while Samira reports civilians isolated beyond a blocked street.
5. A dormant Civic Relay terminal powers on; ARIA states that the command chain cannot be authenticated.
6. The system finds one valid emergency continuity candidate: the player-defined Commander.
7. The tactical map resolves around Old Market and hands control toward the first identity decision and operation.

Voices: ARIA, Dalia, Samira, restrained civilian/emergency radio texture. No Qassem reveal.

Transition: continue directly into `seq.prologue.commander_identity`; do not expose the full main menu before the first playable command.

### `seq.prologue.commander_identity`

Interactive story bridge, equivalent to Tier C, 2 visual states.

1. The damaged command roster presents the player's selected or default Commander identity as the only valid continuity authority.
2. ARIA confirms the chosen name/portrait and requests the first bounded order while the Old Market route remains visible behind the interface.

The player may use a default identity and continue immediately. Identity selection expresses authorship but never changes the Commander's competence, gender assumptions, access to story, or canonical role.

Transition: `seq.ch01.open.first_response`, then `seq.ch01.m01.brief` as a short interactive handoff.

## Chapter 1: First Response

### `seq.ch01.open.first_response`

Tier B, 5 panels.

1. A district map loses power, roads, and command links in quick succession.
2. Dalia marks the few surviving JRC squads and an abandoned forward post.
3. Samira identifies civilians, clinic access, and municipal crews beyond the attacks.
4. ARIA shows that the strikes form a coordinated pattern but cannot identify the author.
5. The Commander takes field authority; the immediate question is whether a response can become a functioning command.

### Mission Sequence Catalog

| Mission and IDs | Brief panel beats | In-mission communication | Debrief panel beats | Primary voices |
|---|---|---|---|---|
| M01 First Contact: `seq.ch01.m01.brief`, `seq.ch01.m01.comms`, `seq.ch01.m01.debrief` | 1. Confirmed armed patrol approaches the blocked civilian route. 2. ARIA identifies the first command actions. 3. Dalia hands tactical control to the Commander. | Samira reports that civilians remain beyond the patrol; ARIA distinguishes the confirmed hostiles from protected civilian locations. | 1. The route is secured and responders advance. 2. Recovered patrol orders show coordination beyond a spontaneous attack. 3. Dalia reports another cell moving toward the abandoned post. | ARIA, Dalia, Samira |
| M02 Establish The Base: `seq.ch01.m02.brief`, `seq.ch01.m02.comms`, `seq.ch01.m02.debrief` | 1. The abandoned post is the only viable district command point. 2. Dalia identifies what must be restored and defended. 3. The nearby clinic route establishes the civic reason for the base. | A recovered municipal access list is found during the defense; ARIA confirms it was stolen before the attack. | 1. The post is operational and local response channels return. 2. Dalia accepts the recurring field-lead role. 3. A warning sector goes dark as an armored convoy moves. | Dalia, ARIA, Samira |
| M03 Radar Warning: `seq.ch01.m03.brief`, `seq.ch01.m03.comms`, `seq.ch01.m03.debrief` | 1. A warning feed shows an uncertain approach through the disabled sector. 2. Dalia identifies defensible lanes. 3. ARIA explains the available lead time without overstating certainty. | When the convoy commits, ARIA reports that the attackers timed their movement to the outage; Dalia redirects the defense. | 1. The post survives and the clinic corridor remains protected. 2. Logs prove the exact outage was known in advance. 3. An isolated medical/engineering team calls for extraction. | ARIA, Dalia |
| M04 Airlift: `seq.ch01.m04.brief`, `seq.ch01.m04.comms`, `seq.ch01.m04.debrief` | 1. The cut-off team and threatened landing zone are established. 2. Laila explains the extraction window. 3. Samira identifies why every specialist matters to recovery. | As the landing zone opens or closes, Laila communicates the transport state while Samira confirms the last objective passenger. | 1. The team reaches safety and the aircraft departs. 2. Laila joins the response network. 3. Their reports identify the fortified communications node coordinating the chapter attacks. | Laila, Samira, Dalia, ARIA |
| M05 Breach Assault: `seq.ch01.m05.brief`, `seq.ch01.m05.comms`, `seq.ch01.m05.debrief` | 1. The fortified node is shown beside protected civic structures. 2. Dalia presents the controlled assault purpose. 3. ARIA identifies the records that must survive. | Qassem addresses the Commander during the breach, claiming that fragmented authority caused the crisis; ARIA detects an obsolete credential signature. | 1. The command core falls and district attacks lose coordination. 2. ARIA isolates the impossible credential. 3. The team prepares to reconstruct it rather than declare the conspiracy solved. | Dalia, ARIA, Qassem, Samira |

### `seq.ch01.close.protocol_fragment_01`

Tier B, 6 panels.

1. Recovered traffic reconstructs an obsolete ARIA credential embedded in Ash Line orders.
2. ARIA confirms the credential was revoked during the Civic Relay shutdown.
3. Dalia asks whether the network has an insider; ARIA cannot answer.
4. Qassem's first clean transmission tells the Commander that the city has already chosen emergency rule by needing them.
5. Samira interrupts the argument with the practical truth: roads, Fuel, power, and clinics are failing now.
6. The district map expands into a broken city grid, opening Chapter 2.

Story Archive unlock: Protocol Fragment 1, Qassem's first transmission, and the Chapter 1 district state.

## Chapter 2: Broken Grid

### `seq.ch02.open.broken_grid`

Tier B, 5 panels.

1. The city survives the first attacks but relief traffic stops at blocked roads and powerless intersections.
2. A clinic generator runs low while Fuel sits stranded behind a damaged corridor.
3. Samira maps roads, refinery, market, power, shelters, and workers as one living system.
4. ARIA offers the formal network; Fadi marks local routes and dependencies missing from its data.
5. The Commander is asked to restore lifelines while the enemy continues steering them.

### Mission Sequence Catalog

| Mission and IDs | Brief panel beats | In-mission communication | Debrief panel beats | Primary voices |
|---|---|---|---|---|
| M01 Gridlock: `seq.ch02.m01.brief`, `seq.ch02.m01.comms`, `seq.ch02.m01.debrief` | 1. Hospital and relief traffic are trapped behind deliberate blocks. 2. Fadi identifies the work points and a preserved local lane. 3. Dalia marks the sabotage threat. | Fadi reveals that ARIA's shortest route is physically unusable while a locally maintained lane can be reopened. | 1. Relief traffic moves through the restored corridor. 2. The sabotage pattern appears designed to steer movement rather than stop it. 3. Fuel shortages become the next system-level threat. | Fadi, Samira, ARIA, Dalia |
| M02 Supply Line: `seq.ch02.m02.brief`, `seq.ch02.m02.comms`, `seq.ch02.m02.debrief` | 1. The damaged Oil-to-Fuel chain is shown link by link. 2. Samira identifies military and civilian demand. 3. Dalia defines the defense perimeter. | A stolen tanker or manifest reveals that diverted Fuel repeatedly traveled toward the same dormant corridor. | 1. The reserve reaches emergency services and JRC vehicles. 2. ARIA correlates the destination with decommissioned Relay maps. 3. Old Market reports a separate supply manipulation. | Samira, Dalia, ARIA |
| M03 Market Lifeline: `seq.ch02.m03.brief`, `seq.ch02.m03.comms`, `seq.ch02.m03.debrief` | 1. Old Market shortages and legitimate trade are established. 2. Yasin presents contradictory manifests without accusing the whole market. 3. The Commander must deliver relief and verify the compromised transfer. | Yasin identifies a real local trade pattern that distinguishes the corrupt shipment from ordinary commerce. | 1. Essential goods reach the market. 2. The compromised manifest links storage sites around Relay infrastructure. 3. A power outage displaces families near one of those sites. | Yasin, Samira, ARIA |
| M04 Power Relay: `seq.ch02.m04.brief`, `seq.ch02.m04.comms`, `seq.ch02.m04.debrief` | 1. The dark substation, exposed families, and two routes are shown together. 2. ARIA presents the shortest route. 3. Samira adds shelter capacity and crowd information. | ARIA revises her recommendation when human context changes the safe route, explicitly acknowledging the missing data. | 1. Power and shelter access return. 2. The substation emits a dormant Civic Relay handshake. 3. The handshake leads to the logistics hub controlling stolen routes. | ARIA, Samira, Dalia, Dr. Lina |
| M05 Route Reopened: `seq.ch02.m05.brief`, `seq.ch02.m05.comms`, `seq.ch02.m05.debrief` | 1. The active relief network and hostile logistics hub are presented as one system. 2. Dalia and Samira divide defense and assault priorities. 3. ARIA identifies routing records that must survive. | During the operation, a delivery activates another dormant Relay node, proving the network is being fed deliberately. | 1. Lifelines remain open while the hub falls. 2. Recovered records align Fuel, power, and trade diversions. 3. ARIA assembles the second Protocol Fragment. | Samira, Dalia, ARIA, Qassem |

### `seq.ch02.close.protocol_fragment_02`

Tier B, 6 panels.

1. Fuel deliveries, power handshakes, road diversions, and market storage sites appear on one city map.
2. The lines form a deliberate pattern around dormant Civic Relay nodes.
3. ARIA confirms the attacks were feeding selected systems, not simply destroying infrastructure.
4. Samira states that someone is using human need as activation traffic.
5. Qassem tells the Commander that a city under stress will authorize what it rejected in peace.
6. A moving encrypted signal leaves the logistics network and enters a populated district, opening Chapter 3.

Story Archive unlock: Protocol Fragment 2, restored-lifeline state, and the Relay node map.

## Chapter 3: Hidden Network

### `seq.ch03.open.hidden_network`

Tier B, 5 panels.

1. Sahrin's restored streets carry civilians, responders, ordinary transmitters, and concealed hostile traffic together.
2. A visual montage contrasts confirmed Ash Line weapons and evidence with protected civilian life, establishing the identification rule.
3. ARIA admits that formal databases cannot classify every signal or person safely.
4. Dalia and Samira define the joint standard: verify, isolate, act, preserve proof.
5. The moving Relay signature becomes the first target of a network investigation.

### Mission Sequence Catalog

| Mission and IDs | Brief panel beats | In-mission communication | Debrief panel beats | Primary voices |
|---|---|---|---|---|
| M01 Signal Trace: `seq.ch03.m01.brief`, `seq.ch03.m01.comms`, `seq.ch03.m01.debrief` | 1. Multiple legitimate and suspicious transmitters are established. 2. ARIA explains confidence limits. 3. Samira names protected local uses. | New evidence confirms one armed escort and changes a signal from uncertain to targetable; unsafe alternatives remain protected. | 1. The correct device is recovered without attacking innocent transmitters. 2. Its Relay signature requires continuity-era knowledge. 3. The device points to a verified safehouse. | ARIA, Samira, Dalia |
| M02 Safehouse Sweep: `seq.ch03.m02.brief`, `seq.ch03.m02.comms`, `seq.ch03.m02.debrief` | 1. The verified weapons node and adjacent homes are visually separated. 2. Dalia defines perimeter and evidence priorities. 3. The escape route is established. | A courier attempts to move the ledger, forcing the Commander to contain rather than indiscriminately destroy the block. | 1. The cell is neutralized and homes remain protected. 2. The ledger exposes an evidence courier network and compromised credentials. 3. One planted report begins moving through an authority channel. | Dalia, ARIA, Salma if used |
| M03 False Front: `seq.ch03.m03.brief`, `seq.ch03.m03.comms`, `seq.ch03.m03.debrief` | 1. A credible report threatens an evacuation route. 2. ARIA presents the current interpretation and uncertainty. 3. The Commander deploys without receiving permission to attack an unconfirmed site. | Contradictory field evidence proves the first interpretation incomplete; ARIA admits the error and identifies the real ambush. | 1. Evacuees survive and the ambush is defeated. 2. The false report carries an ARIA-compatible authority seal. 3. ARIA begins checking whether her own missing archive created it. | ARIA, Samira, Dalia |
| M04 Evidence Chain: `seq.ch03.m04.brief`, `seq.ch03.m04.comms`, `seq.ch03.m04.debrief` | 1. The witness, archive, and extraction choices are established. 2. Dr. Lina explains the human risk. 3. Laila and Dalia present known route/air conditions. | The archive authenticates with an ARIA self-seal while the ambush adapts to the chosen route. | 1. Witness and archive reach analysis. 2. ARIA confirms the seal was created by her own earlier process. 3. The protected archive identifies the audit bunker. | ARIA, Dr. Lina, Laila, Dalia |
| M05 Network Break: `seq.ch03.m05.brief`, `seq.ch03.m05.comms`, `seq.ch03.m05.debrief` | 1. Verified bunker nodes and protected district structures are mapped. 2. ARIA identifies the sealed archive. 3. Dalia defines a controlled breach. | Qassem tries to force data destruction and tells ARIA her missing memory is proof of failure; ARIA chooses preservation and admits the partition. | 1. The network breaks and the audit survives. 2. ARIA states that she sealed it to prevent an override. 3. Vanguard mobilization appears in the recovered traffic. | ARIA, Dalia, Qassem, Samira |

### `seq.ch03.close.protocol_fragment_03`

Tier B, 7 panels.

1. The recovered audit begins before the current attacks, during the Civic Relay shutdown.
2. Qassem's original override design appears under his continuity-planner authority.
3. ARIA detects the attempted erasure and invalidates the credentials she can reach.
4. She partitions the remaining proof inside her own archive, losing access to part of herself.
5. Present-day ARIA acknowledges that her incompleteness was a deliberate protective act.
6. Dalia and Samira accept the evidence but insist that ARIA's action does not grant her future authority without oversight.
7. Organized aircraft and armored columns cross into Sahrin's operation area, opening Chapter 4.

Story Archive unlock: Protocol Fragment 3, ARIA's self-seal record, and the confirmed Qassem audit excerpt.

## Chapter 4: Air And Armor

### `seq.ch04.open.air_and_armor`

Tier B, 6 panels.

1. The visual language changes from hidden cells to organized columns, flight formations, radar tracks, and long-range batteries.
2. Laila identifies disciplined aviation tactics and military-grade coordination.
3. Dalia distinguishes Vanguard Brigade from Ash Line cells while confirming their operational alliance.
4. Samira marks the relief corridors and Fuel sites exposed by the escalation.
5. ARIA shows dormant Relay nodes inside the military advance.
6. The Commander prepares to use strategic force without abandoning confirmation or civilian protection.

### Mission Sequence Catalog

| Mission and IDs | Brief panel beats | In-mission communication | Debrief panel beats | Primary voices |
|---|---|---|---|---|
| M01 Air Corridor: `seq.ch04.m01.brief`, `seq.ch04.m01.comms`, `seq.ch04.m01.debrief` | 1. Relief aircraft, radar coverage, and hostile approach lanes are shown together. 2. Laila explains the human cost of losing the corridor. 3. ARIA identifies G2A coverage and readiness constraints. | Laila recognizes a coordinated Vanguard strike pattern as the attack commits, allowing a fair tactical response. | 1. Relief flights clear the district. 2. Flight data confirms Vanguard organization beyond Ash Line capability. 3. Ground columns move toward the Fuel reserve. | Laila, ARIA, Dalia |
| M02 Steel Push: `seq.ch04.m02.brief`, `seq.ch04.m02.comms`, `seq.ch04.m02.debrief` | 1. The armored route, Fuel site, civilian services, and Relay node are established. 2. Dalia identifies composition and approach risk. 3. Samira identifies the reserve that cannot be sacrificed casually. | A disabled command vehicle yields an order referencing a "final authority key" while the main column continues. | 1. The site remains operational and the column breaks. 2. Vanguard orders connect the attack to Relay access. 3. A long-range battery begins a diversion against the forward base. | Dalia, Samira, ARIA |
| M03 Split Front: `seq.ch04.m03.brief`, `seq.ch04.m03.comms`, `seq.ch04.m03.debrief` | 1. The verified battery, minimum-range constraint, protected structures, and base diversion are shown. 2. ARIA explains confirmation and cancellation. 3. Dalia assigns the split defense. | Qassem offers ARIA complete memory and the Commander unrestricted control; ARIA refuses autonomous authority before tactical action continues. | 1. The battery and diversion fail without an unsafe strike. 2. Its targeting package expects ARIA authorization. 3. Imported Relay-compatible hardware is traced to an air-support site. | Qassem, ARIA, Dalia, Samira |
| M04 Grounded Signal: `seq.ch04.m04.brief`, `seq.ch04.m04.comms`, `seq.ch04.m04.debrief` | 1. Runway, airborne, cargo, and extraction conditions are compared. 2. Karim presents the transport tradeoff. 3. Yusuf identifies the hardware that must be recovered. | After insertion, Yusuf confirms the device is Relay-compatible while Laila reports the evolving extraction window. | 1. The specialists return with the control hardware. 2. Physical compatibility proves deliberate preparation. 3. The hardware identifies Vanguard's command group and link schedule. | Karim, Yusuf, Laila, ARIA |
| M05 Armor Break: `seq.ch04.m05.brief`, `seq.ch04.m05.comms`, `seq.ch04.m05.debrief` | 1. Vanguard command, heavy assets, Fuel/logistics, and the relief boundary appear in one operational picture. 2. Dalia and Laila define combined priorities. 3. ARIA identifies the authority package to seize. | As the link begins, ARIA asks explicit permission for one bounded support action and reports the two-key handshake. | 1. Vanguard command is broken and the authority package is recovered. 2. ARIA confirms that Qassem needs her archive and the Commander's live authority. 3. A citywide synchronized attack begins before either key can be secured elsewhere. | Dalia, Laila, ARIA, Qassem |

### `seq.ch04.close.protocol_fragment_04`

Tier B, 6 panels.

1. Vanguard hardware attempts to connect to a dormant Relay node.
2. The handshake requests ARIA's restored archive as the system knowledge key.
3. It also requests the current Commander's emergency credential as the live legitimacy key.
4. ARIA explains that neither alone can complete the override.
5. Qassem launches simultaneous attacks designed to force both keys into the central command node.
6. Sahrin's districts light with warnings as Chapter 5 begins.

Story Archive unlock: Protocol Fragment 4, Vanguard authority package, and the two-key diagram.

## Chapter 5: Citywide Command

### `seq.ch05.open.citywide_command`

Tier B, 7 panels.

1. The five established districts appear in their current Trust, Evidence, and Infrastructure condition.
2. Ash Line cells strike local systems while Vanguard units pressure major corridors.
3. Dalia reports military fronts; Samira reports shelters, clinics, roads, and workers; Laila reports the air corridor.
4. ARIA presents one combined command picture, explicitly separating facts, predictions, and recommendations.
5. Qassem declares that the crisis proves the city requires one permanent authority.
6. The Commander retains explicit approval over ARIA and assigns the first two fronts.
7. The final chapter question is stated through action: save the city without becoming Qassem's answer.

### Mission Sequence Catalog

| Mission and IDs | Brief panel beats | In-mission communication | Debrief panel beats | Primary voices |
|---|---|---|---|---|
| M01 Citywide Alert: `seq.ch05.m01.brief`, `seq.ch05.m01.comms`, `seq.ch05.m01.debrief` | 1. Two district objectives and their civic functions are established. 2. Mixed Ash Line/Vanguard threats and travel time are shown. 3. ARIA offers bounded support but waits for consent. | At peak pressure, ARIA requests approval for one typed support action and remains immediately overridable. | 1. Both districts stabilize. 2. Attack timing maps the last active Relay nodes. 3. Qassem shifts pressure toward evacuation routes and public broadcasts. | ARIA, Dalia, Samira, Laila |
| M02 Trust Under Fire: `seq.ch05.m02.brief`, `seq.ch05.m02.comms`, `seq.ch05.m02.debrief` | 1. Evacuation groups, shelter capacity, and route threats are shown. 2. Qassem's false abandonment broadcast begins. 3. Samira states what the city actually needs from command. | A civilian witness identifies the true broadcast relay while Samira's response reflects the player's earned Trust state. | 1. Evacuees reach shelter and the relay is secured. 2. Witness and signal evidence point to Qassem's final network. 3. Dalia argues that proof must survive the next strike. | Samira, Dalia, ARIA, Dr. Lina/Yasin as appropriate |
| M03 Network Collapse: `seq.ch05.m03.brief`, `seq.ch05.m03.comms`, `seq.ch05.m03.debrief` | 1. Verified nodes, unverified protected structures, long-range risk, and evidence teams are separated. 2. ARIA shows the audit release path. 3. Dalia presents the slower evidence-preserving option. | The audit opens in stages and confirms Qassem's authorship while the player still must protect its physical chain. | 1. The network collapses and the complete audit survives. 2. Qassem is publicly tied to the original shutdown and current attacks. 3. The last physical access keys must cross the city center corridor. | Dalia, ARIA, Qassem, Samira |
| M04 Last Corridor: `seq.ch05.m04.brief`, `seq.ch05.m04.comms`, `seq.ch05.m04.debrief` | 1. Fuel, medicine, engineers, reinforcements, and authority keys are mapped against ground and air routes. 2. Dalia and Samira jointly set priorities. 3. Laila and Karim present the air option. | A route fails or degrades as authored; the cast reports consequences and alternatives without silently taking control. | 1. Critical supplies and keys reach the city center. 2. Established characters occupy their final operational roles. 3. The Relay complex and its wired civic systems become the last objective. | Dalia, Samira, Laila, Karim, ARIA |
| M05 Command Node: `seq.ch05.m05.brief`, `seq.ch05.m05.comms`, `seq.ch05.m05.debrief` | Tier A treatment: 1. The city systems tied to the complex are shown. 2. Every principal character states an operational responsibility, not a speech. 3. Qassem begins the override. 4. ARIA confirms she will wait for explicit authority. 5. The Commander orders the approach. | During the command-choice phase, Qassem demands unilateral activation; ARIA releases the audit, explains the bounded options, and waits while gameplay remains the decision surface. | 1. The override fails and Qassem's force is defeated. 2. The audit reaches the city. 3. Dalia and Samira secure military/civil authority together. 4. ARIA asks what governance follows emergency command. | Commander choice text, ARIA, Qassem, Dalia, Samira, Laila |

### `seq.ch05.close.protocol_fragment_05`

Tier B flowing directly from M05 debrief, 8 panels.

1. The complete audit aligns Qassem's original override, attempted erasure, and ARIA's self-partition.
2. Current operation records align bombings, false reports, Fuel diversions, Relay activations, and Vanguard hardware.
3. Qassem's manufactured-crisis strategy becomes publicly legible.
4. The Commander rejects permanent unilateral control.
5. Dalia returns military emergency authority to a bounded mandate.
6. Samira and district representatives accept shared civilian oversight.
7. ARIA remains active as a transparent decision-support system with explicit consent and auditability.
8. Sahrin's command map changes from hostile override to recovery coordination.

Story Archive unlock: Protocol Fragment 5, complete Qassem audit, governance resolution, and the canonical ending path.

## Campaign Epilogue

### `seq.campaign.epilogue.canonical`

Tier A, 9 panels. These facts are guaranteed by Campaign completion.

1. The Civic Relay complex is secured and disconnected from Qassem's override.
2. Ash Line command collapses; remaining Vanguard forces withdraw or surrender under later detailed continuity.
3. The complete audit is released to the city rather than retained as secret command evidence.
4. Dalia supervises disciplined stand-down and continued protection.
5. Samira joins civil representatives and workers restoring essential systems.
6. Laila reopens the relief air corridor while clinics and markets resume activity.
7. ARIA displays her bounded permissions, audit trail, and ability to be overridden.
8. The Commander returns emergency authority to a transparent shared structure.
9. A recovered Sahrin is shown as a living city with damage, work, and future purpose, not a frozen victory tableau.

### Consequence Emphasis Sequences

The epilogue composes one authored state from each value family after the canonical panels. Each record supports a high-state and low-state recovery treatment.

| Sequence ID | High-state emphasis | Low-state recovery cost | Facts that cannot change |
|---|---|---|---|
| `seq.campaign.epilogue.trust_emphasis` | District councils, witnesses, and residents participate confidently in shared oversight. | Recovery begins under skepticism, protest, and a longer legitimacy repair process. | The Commander rejects permanent unilateral rule and civilian authority participates. |
| `seq.campaign.epilogue.evidence_emphasis` | Qassem's network, financiers, and Vanguard links are broadly documented. | Core guilt is proven, but parts of the support network remain contested or hidden. | Qassem caused the manufactured crisis and the complete critical-path audit survives. |
| `seq.campaign.epilogue.infrastructure_emphasis` | Roads, power, Fuel, clinics, shelters, and trade recover quickly. | Shortages, displacement, and repairs extend the emergency transition. | Sahrin survives and recovery begins. |

No combination is labeled the secret, true, paid, or bad ending. Consequence panels reflect operational style while preserving the canonical resolution.

### `seq.campaign.postscript.recovery_watch`

Tier C/E postscript, 3 panels.

1. The Story Archive closes on the five reconstructed Protocol Fragments and the player's city-state emphasis.
2. ARIA alerts the Commander to unresolved external Vanguard links while clearly classifying them as future investigation, not an unfinished current ending.
3. The restored operations map opens for replay or future content with the Campaign marked complete.

## Character And Visual Continuity Contract

- Use the exact character/config anchors in `Campaign_Narrative_Bible.md`; do not recast a named character because a generated image looks better.
- The Commander portrait is player-authored. Battlefield proxy art may not silently replace that identity in story panels.
- ARIA has one dedicated non-soldier visual identity that evolves through state, framing, and interface integrity rather than unrelated faces.
- Dalia, Samira, Qassem, Laila, Karim, Yusuf, Fadi, Yasin, Dr. Lina, and Salma retain stable face, age band, body type, clothing family, equipment, and role silhouettes across all appearances.
- Ash Line and Vanguard remain visually distinct. Insurgents do not inherit civilian identity assets; Vanguard reads as an organized proxy military.
- Sahrin locations remain geographically and materially recognizable across damage and recovery states.
- Regional inspiration must remain fictional and culturally reviewed. Do not use real conflict insignia, terrorist symbols, national flags, religious caricature, or visual shorthand that makes local civilian identity suspicious.
- AI image generation may support offline concept and production workflows only under the review, provenance, continuity, licensing, and correction rules in `Narrative_Presentation_And_Cutscene_Design.md`.

## Audio, Localization, And Accessibility Contract

- Every spoken line has a subtitle and speaker identity where the presentation format permits.
- Critical facts survive with audio muted and do not depend on color, a rapid flash, or tiny evidence text.
- Tier D communication defers during urgent targeting, placement, boarding, or warning interactions and can replay in a log.
- Briefings and debriefs are skippable after required state is safely recorded; all viewed critical-path sequences are replayable in the Story Archive.
- Localized text expansion, right-to-left support where selected, reading speed, subtitle background, and safe-area layout are validated before final timing lock.
- Radio processing may establish context but cannot reduce intelligibility of objectives, warnings, names, or Protocol Fragment facts.

## Storyboard Handoff Requirements

Each later storyboard package must identify:

- sequence ID and tier;
- mission or chapter entry/exit state;
- panel count and information purpose per panel;
- cast and approved reference package;
- location and its before/during/after continuity state;
- subtitle/dialogue draft and localization allowance;
- sound, music, and transition intent;
- interactive or gameplay handoff;
- skip, replay, offline, and accessibility behavior;
- cultural, legal, continuity, and asset-provenance review status.

This list defines handoff content, not an implementation schema.

## Campaign Narrative Acceptance Gate

The high-level sequence layer is aligned only when:

- all 25 missions retain a brief, communication, and debrief ID;
- every brief states changed context, affected people, and command purpose;
- every in-mission beat changes understanding without removing control;
- every debrief records consequence and opens the next question;
- every chapter close guarantees its Protocol Fragment;
- the prologue reaches playable command before the full menu;
- the finale explains Qassem, ARIA's self-partition, the two authority keys, and bounded Relay governance on the critical path;
- Trust, Evidence, and Infrastructure alter emphasis without hiding canonical truth;
- every named character and location follows continuity and cultural review rules;
- all critical content is subtitled, skippable where appropriate, replayable, and available offline.

## Future Detail Boundary

The next narrative-production layer may add final scripts, individual storyboard frames, approved character/location reference sheets, audio direction, image prompts, and implementation records. It may not silently change this inventory or claim high-level approval for unreviewed generated art. Those are production deliverables, not missing high-level Campaign design.
