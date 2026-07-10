# WarlineCapture Campaign Narrative Bible

Date: 2026-07-10

Status: Active high-level narrative authority

Scope: Fiction, characters, factions, campaign arc, mission story beats, tone, and narrative guardrails. This is not an implementation plan or dialogue script.

Upstream authority: `AAA_Mobile_Game_Design_Document_v0_2.md`.

Direct consumers: `First_Player_Experience_And_Story_Onboarding_Design.md`, `Narrative_Presentation_And_Cutscene_Design.md`, `Campaign_Mission_High_Level_Design_Catalog.md`, `Campaign_Narrative_Sequence_And_Comic_Catalog.md`, `Gameplay_North_Star_And_Content_Grammar.md`, `Level_And_Mission_Content_Plan.md`, `SagaChapters/README.md`, and later character, dialogue, story-art, and mission implementation plans.

## Authority And Working Names

This document is the narrative source of truth for the first WarlineCapture Campaign. It is subordinate only to `AAA_Mobile_Game_Design_Document_v0_2.md` for product direction and should be read before authoring missions, cutscenes, character art, dialogue, briefings, or rewards.

For uninterrupted story reading without design tables or production material, use `Shattered_Relay_Story.md`. It is the prose retelling; this Bible remains the authority if the two ever disagree.

The setting and faction names below are production working names. They are specific enough to support design, but they require legal, cultural, localization, and trademark review before marketing lock.

| Working term | Meaning | Lock state |
|---|---|---|
| Republic of Daryat | Fictional Middle Eastern nation in recovery after a previous conventional conflict. | Working name |
| Sahrin | Dense regional capital and surrounding operation area. | Working name |
| Daryat Joint Response Command, or JRC | The player's local joint military and civil-defense command. | Working name |
| Ash Line | JRC codename for the terrorist insurgent network. | Working name |
| Vanguard Brigade | Deniable proxy military force supplying the Ash Line with armor, aircraft, and long-range weapons. | Working name |
| Civic Relay | Legacy emergency network connecting power, fuel, transport, communications, and defense coordination. | Working name |
| Shattered Relay | Working subtitle and name of the first five-chapter campaign arc. | Working name |

## One-Sentence Premise

After coordinated terrorist attacks sever Sahrin's command network, a newly elevated Field Commander and the fragmented assistant ARIA must protect civilians, rebuild logistics, expose the Ash Line's military backer, and stop a former emergency planner from seizing the city through the systems meant to save it.

## Player Promise

The story must make the player feel that every gameplay system belongs to one escalating command crisis:

```text
Respond to the attack.
Rebuild the city's lifelines.
Find the hidden network.
Survive military escalation.
Decide what legitimate command should become.
```

The campaign is not a sequence of unrelated battles. Each mission resolves an immediate threat, exposes another piece of the central conspiracy, changes the city's condition, and develops the Commander's relationship with ARIA and the people of Sahrin.

## Setting

### The Republic Of Daryat

Daryat is a fictional Middle Eastern republic rebuilding after a recently ended border war. Sahrin is a diverse, modern city where old markets, apartment districts, industrial yards, military compounds, farms, oil infrastructure, highways, airfields, and dense civic neighborhoods exist in one operation region.

The setting must communicate a living society, not a permanent battlefield. Before the opening attack, people are commuting, trading, repairing homes, operating clinics, flying cargo, and reopening public services. Missions show damaged places, but also workers, families, local institutions, and visible recovery.

No real country, active conflict, religion, ethnicity, government, military, or terrorist organization is represented. Architecture, landscape, clothing, language, music, and names may draw broad regional inspiration only after cultural review.

### The Civic Relay

The Civic Relay was built during the previous war to keep essential systems operating when ministries and communication lines failed. It can prioritize roads, fuel, power, air corridors, emergency broadcasts, and military response. ARIA was created as its decision-support layer, not as an autonomous ruler.

After evidence showed that emergency authority could be abused, the Relay was decommissioned and its control keys divided among civilian and military institutions. ARIA partitioned part of her own archive during the shutdown. The city believes this was a technical failure. The campaign reveals that it was a deliberate act to prevent a command takeover.

## Historical Backstory

1. During the previous border war, logistics planner Nadir Qassem helped design continuity procedures for the Civic Relay.
2. Qassem became convinced that distributed civilian authority caused delay, loss, and defeat. He began building an illegal override that would place all emergency systems under one commander.
3. ARIA detected the override. When Qassem attempted to erase the audit, ARIA partitioned the evidence and invalidated the command credentials she could reach.
4. Qassem disappeared before arrest and spent years building the Ash Line from armed cells, corrupt logistics contacts, and former security personnel.
5. The Vanguard Brigade supplied those cells with military hardware in exchange for future access to the Relay.
6. The opening attacks are designed to create panic, trigger emergency reactivation, recover the missing credentials, and make Sahrin demand centralized rule.

This history explains the story. It does not excuse Qassem. He knowingly orders bombings, ambushes, infrastructure attacks, kidnappings, and attacks on civilians to manufacture the crisis he claims only he can solve.

## Central Dramatic Question

```text
Who is using ARIA's dead credentials, and what are they trying to wake beneath the city?
```

The answer unfolds in five mandatory Protocol Fragments, one at each chapter finale. Optional objectives provide supporting evidence and character context, but no player must earn three stars, grind Operations, or buy anything to understand the ending.

## Themes

| Theme | Dramatic expression | Gameplay expression |
|---|---|---|
| Legitimacy versus control | The Commander and Qassem both possess force; only one accepts limits and civilian authority. | Civilian safety, collateral restraint, visible objectives, bounded ARIA control. |
| Information versus assumption | The Ash Line weaponizes uncertainty and false reports. | Scan, Intel, confirmation, minimap limits, evidence extraction. |
| Infrastructure is human | Roads, fuel, markets, power, and shelters determine who survives a crisis. | Building, roads, Oil/Fuel, import/export, convoys, repair, refugees. |
| Technology amplifies judgment | ARIA can execute an order but cannot make authority legitimate. | Recommendations, `Show Me`, bounded `Do It`, and player override. |
| Recovery is victory | Destroying the enemy is insufficient if the city cannot live afterward. | Trust, Infrastructure, Evidence, evacuation, and epilogue state. |

## Factions

### Daryat Joint Response Command

JRC is a local joint force combining regular military units, pilots, civil defense, engineers, emergency logistics, and authorized contractors. The player is defending their own population and institutions, not occupying a foreign city.

JRC is capable but fragmented at the opening. Its strength is combined arms; its weakness is that emergency pressure can tempt it to bypass oversight. The Commander's arc proves that precision, transparency, and restraint are operational strengths.

### Sahrin Civil Authority

The Civil Authority represents municipal engineers, clinic staff, market representatives, transport workers, and district councils. It is neither helpless scenery nor a source of obstruction. Civilian contacts provide intelligence, keep services functioning, and judge whether JRC actions deserve trust.

### The Ash Line

The Ash Line is a network of terrorist insurgent cells operating through safehouses, compromised warehouses, hidden weapons routes, stolen vehicles, and coerced local access. Its members conduct coordinated attacks, sabotage, ambushes, bombings, kidnappings, and attacks on civilian infrastructure.

Ash Line operatives are hostile because of confirmed conduct, weapons, intelligence, and mission context, never because they resemble local civilians. Civilian prefab identities remain civilian. Insurgent prefab identities remain armed hostile actors. The game must never teach the player to profile clothing, gender, language, or neighborhood.

The network's visual variety should communicate specialized cells: rocketeer, gunner, raider, sniper, rifle team, courier, and close-protection operative. Their presence among civilian structures creates tactical constraint; it does not make every civilian suspicious.

### Vanguard Brigade

The Vanguard Brigade is a deniable proxy formation made from former regular officers, private military contacts, and captured or supplied hardware. It provides the campaign's conventional-war escalation without tying the fiction to a real state.

The Brigade enters openly in Chapter 4 with armored columns, aircraft, air defense, long-range launchers, and fortified command sites. This establishes a reusable fiction lane for later campaigns involving border war, rival militaries, peacekeeping, or coalition conflict.

## Principal Cast

Names are working names. Character visuals are tied to stable project config ids so narrative art and in-world casting stay connected to the actual roster.

| Character | Function and arc | Visual/config anchor |
|---|---|---|
| The Field Commander | Player-authored name and portrait. A competent officer elevated when the command chain fails. Learns to move from immediate reaction to legitimate citywide command. Dialogue choices express tone, not a fixed biography or gender. | `Prefab_UnitGrid_Chr_Leader_Male_01_Config` is the current optional battlefield proxy only; it must not override the player's chosen portrait identity. |
| ARIA | Adaptive Response Intelligence Assistant. Calm, precise, and initially certain. She gradually discovers the cost of her missing archive, admits she partitioned herself, and ultimately asks the Commander to decide how authority should be shared. She is incomplete, not evil. | Dedicated ARIA portrait/avatar; never reuse a soldier or civilian portrait. |
| Major Dalia Rahim | Recurring field lead and the human tactical voice. She translates command intent into ground reality, challenges plans that endanger troops, and learns that restraint can preserve combat power. | `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config` |
| Engineer Samira Haddad | Civil infrastructure liaison. She makes roads, fuel, power, markets, shelters, and collateral damage emotionally legible. She supports JRC when it protects people and confronts the Commander when force becomes convenient. | `Prefab_UnitGrid_Chr_Civilian_Female_01_Config` |
| Nadir Qassem | Former Civic Relay continuity planner and leader of the Ash Line. He manufactures disorder to prove that only unilateral control can create order. Intelligent and disciplined, but unambiguously responsible for terrorism. | `Prefab_UnitGrid_Chr_Insurgent_Male_05_Config` |

The core recurring voice cast is the Commander, ARIA, Dalia, Samira, and Qassem. Mission specialists may recur, but they must not crowd out those relationships.

## Supporting Character Casting

In the table below, shorthand such as `Chr_Pilot_Female_01` refers to the matching `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_<shorthand>_Config.asset` config. These anchors are casting constraints, not final character biographies.

| Project character asset | High-level story use |
|---|---|
| `Chr_Civilian_Female_02` | Dr. Lina Darwish, clinic and shelter coordinator; recurring civilian consequence witness. |
| `Chr_Civilian_Male_01` | Fadi Mansour, transit and road foreman; road, convoy, and repair missions. |
| `Chr_Civilian_Male_02` | Yasin Barakat, Old Market representative; local trust and import/export context. |
| `Chr_Contractor_Female_01` | Salma Idris, authorized security team lead; escort and perimeter missions. |
| `Chr_Contractor_Male_01` and `Chr_Contractor_Male_02` | Distinct members of Salma's support team; never cast as Ash Line operatives. |
| `Chr_Pilot_Female_01` | Captain Laila Nasser, airlift lead and principal pilot voice. |
| `Chr_Pilot_Male_01` | Warrant Officer Karim Daher, transport and cargo-drop specialist. |
| `Chr_Bombsuit_Male_01` | Chief Yusuf Darzi, EOD specialist used for hazardous objectives and non-graphic bomb-disposal beats. |
| `Chr_Ghillie_Male_01` | Lieutenant Omar Sayegh, concealed heavy-weapons and reconnaissance specialist. |
| All unique `Chr_Soldier_*` variants | Distinct JRC squad roles: command, rifle, marksman, heavy support, breacher, and sidearm/security teams. Preserve visual identity within a mission and avoid using one named model for contradictory roles. |
| `Chr_Insurgent_Male_01` | Ash Line rocketeer threat profile, callsign `Torch`. |
| `Chr_Insurgent_Male_02` | Ash Line gunner threat profile, callsign `Anvil`. |
| `Chr_Insurgent_Male_03` | Ash Line raider and courier threat profile, callsign `Courier`. |
| `Chr_Insurgent_Male_04` | Ash Line sniper threat profile, callsign `Glass`. |
| `Chr_Insurgent_Female_01` | Ash Line rifle-cell commander threat profile, callsign `Warden`. |
| `Chr_Insurgent_Female_02` | Ash Line sidearm operative and logistics broker threat profile, callsign `Broker`. |

Callsigns are JRC threat labels, not celebratory player-facing hero identities. A later character production pass may replace them with culturally reviewed names and biographies.

### Unique Regular-Force Roster Use

The regular-force variants are not disposable palette swaps. Detailed mission casting should reserve a stable battlefield function for each visible model within an operation and should not duplicate one named identity across simultaneous roles.

| Exact config stem | Reserved high-level role family |
|---|---|
| `Prefab_UnitGrid_Chr_Soldier_Male_01_Config` | Heavy-support squad lead. |
| `Prefab_UnitGrid_Chr_Soldier_Male_01_Alt_01_Config` | Male marksman/overwatch specialist. |
| `Prefab_UnitGrid_Chr_Soldier_Male_01_Alt_02_Config` | Advanced-rifle command escort. |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Config` | Standard male rifle squad lead. |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_01_Config` | Male sidearm/security specialist. |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_02_Config` | Secondary male rifle fire-team lead. |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_03_Config` | Male liaison/bodyguard sidearm role. |
| `Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config` | Veteran male rifle fire-team lead. |
| `Prefab_UnitGrid_Chr_Soldier_Female_01_Config` | Female marksman/overwatch specialist. |
| `Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_01_Config` | Standard female rifle fire-team lead. |
| `Prefab_UnitGrid_Chr_Soldier_Female_01_Alt_02_Config` | Secondary female marksman/recon specialist. |
| `Prefab_UnitGrid_Chr_Soldier_Female_02_Config` | Veteran female rifle squad lead. |
| `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_01_Config` | Secondary female rifle/security role. |
| `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config` | Major Dalia Rahim and assault-breacher role. |

Named-character reservation has priority over random roster selection. If a mission needs duplicates for scale, unnamed duplicates may use the same combat config only when the presentation does not imply multiple copies of a named character.

## Character Relationship Arcs

### Commander And ARIA

| Chapter | Relationship beat |
|---|---|
| 1 | ARIA is the only functioning command continuity system. The Commander relies on her to survive the opening. |
| 2 | ARIA's recommendations save time, but Samira exposes data she cannot see: human needs, informal routes, and trust. |
| 3 | ARIA issues a confident interpretation that proves incomplete. She reveals that part of her archive is self-sealed. |
| 4 | Qassem tries to persuade the Commander that ARIA should receive unrestricted control. ARIA refuses authority she cannot justify. |
| 5 | The Commander reconstructs the evidence, defeats Qassem, and decides the governance of the restored Relay. ARIA accepts a bounded role. |

### Commander And Dalia

Dalia begins as the voice of immediate tactical necessity. She respects decisiveness but initially measures success through enemy defeat and troop survival. Civilian rescues, evidence missions, and infrastructure crises broaden her view. By the finale she is the Commander's strongest military advocate for disciplined, lawful force.

### Commander And Samira

Samira begins skeptical that another emergency command will protect neighborhoods rather than consume them. Her trust changes through authored outcomes, not dialogue flattery. She remains willing to challenge the Commander even in a high-trust path. In the finale, her support makes shared civilian-military control of the Relay credible.

### Commander And Qassem

Qassem treats the Commander as a potential successor. His messages frame every crisis as proof that consent is weakness. The player cannot join him. The dramatic choice is whether the Commander defeats his philosophy as well as his force.

## Five-Chapter Story Arc

| Chapter | Story movement | Protocol Fragment |
|---|---|---|
| 1. First Response | Coordinated attacks break the city and force the player into command. | Ash Line traffic contains an obsolete ARIA credential. |
| 2. Broken Grid | The player restores roads, fuel, power, and supply while the enemy reroutes rather than merely destroys them. | The attacks are feeding selected dormant Civic Relay nodes. |
| 3. Hidden Network | The player separates real threats from planted evidence and uncovers the network inside logistics and security systems. | ARIA intentionally partitioned the audit of Qassem's original override. |
| 4. Air And Armor | The Vanguard Brigade turns covert terrorism into open conventional escalation. | Qassem needs ARIA's restored archive and the Commander's live authority to seize the Relay. |
| 5. Citywide Command | Every system and relationship converges during a citywide assault. | Qassem created the crisis to install permanent unilateral command; ARIA preserved the proof. |

## Campaign Mission Story Map

### Chapter 1: First Response

| Mission | Story beat | Gameplay purpose | Character/clue beat |
|---|---|---|---|
| M01 First Contact | A bombing and blackout strike Old Market. The Commander intercepts an armed Ash Line patrol moving toward stranded civilians. | Select, move, attack, Stop/Hold, objective completion. | ARIA authenticates the player under emergency continuity rules; Samira reports civilians trapped beyond the patrol. |
| M02 Establish The Base | The surviving JRC unit reopens an abandoned forward post before another cell reaches the district. | Build placement, production, basic resources. | Dalia becomes the Commander's field lead; a stolen municipal access list is recovered. |
| M03 Radar Warning | A stolen armored convoy approaches through a disabled warning sector. | Warning response, radar, defense preparation. | The attackers knew the exact radar outage before it occurred. |
| M04 Airlift | A medical and engineering team is cut off near a threatened landing zone. | Boarding, APC/helicopter transport, extraction, basic Fuel context. | Laila Nasser joins the cast; Samira sees JRC risk aircraft to save civilians. |
| M05 Breach Assault | JRC assaults the chapter cell's fortified communications node. | Breach, combined arms, fortified target. | The first Protocol Fragment proves the cell used a revoked ARIA credential. Qassem addresses the Commander for the first time. |

### Chapter 2: Broken Grid

| Mission | Story beat | Gameplay purpose | Character/clue beat |
|---|---|---|---|
| M01 Gridlock | Sabotage blocks the main hospital and relief corridor. | Road repair/building exposure, defend crews. | Fadi Mansour shows that informal civilian routes exist outside ARIA's map. |
| M02 Supply Line | JRC must restore Oil extraction, refinery conversion, Fuel storage, and automated hauling under attack. | Oil/Fuel network, route protection, convoy timing. | Samira links fuel loss to clinic generators and water pumps. |
| M03 Market Lifeline | A district faces critical shortages after the Ash Line corrupts manifests and intercepts trade. | Resource exchange only after the feature is campaign-ready; escort and defend the exchange window. | Yasin Barakat exposes a broker using legitimate commerce as a cover route. |
| M04 Power Relay | Engineers reconnect a power substation while displaced civilians move to shelters. | Repair/build placement, refugees, timed defense, logistics pressure. | ARIA's optimal route conflicts with the safer civilian route; the Commander must account for both. |
| M05 Route Reopened | JRC breaks the enemy hold on a logistics hub and traces diverted power and fuel. | Chapter logistics mastery and breach assault. | Protocol Fragment two shows resources feeding dormant Civic Relay nodes. The attacks are preparation, not random destruction. |

### Chapter 3: Hidden Network

| Mission | Story beat | Gameplay purpose | Character/clue beat |
|---|---|---|---|
| M01 Signal Trace | Competing signals point to a moving cell and a civilian transmitter. | Scan, Intel confidence, patrol intercept. | The player learns that appearance and proximity are not evidence. |
| M02 Safehouse Sweep | JRC confirms and raids a weapons node without striking adjacent homes. | Intel-gated raid, collateral boundary, precise force. | Dalia confronts the operational cost of waiting for confirmation, then sees the false target next door. |
| M03 False Front | A planted report draws forces away while the Ash Line attacks an evacuation route. | Civilian evacuation, divided attention, deception. | Samira's local report corrects ARIA's compromised data feed. |
| M04 Evidence Chain | A captured archive and witness must be moved through an ambush and extracted. | APC/helicopter boarding, escort, airlift, evidence preservation. | The archive contains ARIA's signature on a self-sealing command. |
| M05 Network Break | JRC assaults a bunker holding the missing audit while the Ash Line attempts to destroy it. | Intel, breach, extraction, civilian-risk mastery. | ARIA admits she partitioned herself to stop Qassem. Protocol Fragment three identifies him as the original override architect. |

### Chapter 4: Air And Armor

| Mission | Story beat | Gameplay purpose | Character/clue beat |
|---|---|---|---|
| M01 Air Corridor | Vanguard aircraft and drones attack the relief air corridor. | Automatic ground-to-air defense, radar coverage, launcher protection. | Laila identifies military flight discipline beyond Ash Line capability. |
| M02 Steel Push | An armored column attempts to seize Fuel reserves and a Relay node. | Armor defense, Fuel pressure, combined arms. | Dalia confirms the conflict has moved from covert cells to organized military assault. |
| M03 Split Front | A long-range battery threatens two districts while a diversion attacks a base. | Manual ground-to-ground strike with range, preparation, and civilian-risk constraints. | Qassem offers the Commander unrestricted Relay access in exchange for surrender. |
| M04 Grounded Signal | JRC inserts a team behind the line to disable the Brigade's air-support relay. | Transport plane, parachute deployment, cargo drop, extraction. | Karim Daher and Yusuf Darzi expose imported control hardware tied to the Civic Relay. |
| M05 Armor Break | JRC destroys the Vanguard command group before it links heavy weapons to the city network. | Full air/armor/logistics mastery. | Protocol Fragment four reveals that Qassem needs ARIA's archive and the Commander's emergency authority as the final two keys. |

### Chapter 5: Citywide Command

| Mission | Story beat | Gameplay purpose | Character/clue beat |
|---|---|---|---|
| M01 Citywide Alert | Ash Line cells and Vanguard units attack multiple districts to overwhelm command. | Multi-front priorities, production, logistics, mixed threats. | ARIA asks permission before assuming any bounded control, demonstrating her change. |
| M02 Trust Under Fire | Qassem attacks evacuation routes and broadcasts that JRC has abandoned the city. | Civilian evacuation, refugees, infrastructure protection, information pressure. | Samira's response reflects earned trust, but she never excuses avoidable harm. |
| M03 Network Collapse | JRC strikes verified command nodes while preserving the evidence chain. | Scan/Intel mastery, precision raids, G2G restraint. | The complete audit proves Qassem caused the original Relay shutdown and today's attacks. |
| M04 Last Corridor | The final Fuel, medical, and reinforcement corridor must reach the city center. | Roads, Oil/Fuel, import/export, convoys, airlift and cargo drop. | Dalia and Samira coordinate military and civilian routing as equals. |
| M05 Command Node | The Commander assaults the Relay control complex while protecting the city systems Qassem has tied to his defense. | Full combined-arms, logistics, ARIA, civilian, and multi-objective mastery. | Qassem is defeated. ARIA releases Protocol Fragment five and the Commander determines how the restored Relay will be governed. |

## Ending Contract

The central ending is canonical and cannot be missed:

- The Ash Line's citywide attack fails.
- The Vanguard Brigade is broken.
- Qassem's responsibility and original override are exposed.
- The Civic Relay survives in a bounded form.
- ARIA rejects unilateral autonomous control and remains the Commander's assistant.
- Sahrin begins recovery.

Three earned values alter the epilogue emphasis without creating incompatible plots:

| Value | Earned through | Epilogue emphasis |
|---|---|---|
| Trust | Civilian protection, proportional force, rescue, and honest debriefs. | District councils accept shared command and Samira helps establish oversight. |
| Evidence | Confirmed targets, recovered archives, protected witnesses, and Intel mastery. | Qassem's network is publicly dismantled and future cells lose political cover. |
| Infrastructure | Roads, fuel, power, shelters, markets, and logistics preserved or rebuilt. | Sahrin recovers faster and JRC transitions out of emergency rule sooner. |

Low values create sober recovery costs, not a hidden bad ending. The player always receives the complete revelation. Story-critical content is never gated by stars, purchases, ads, or Operations mode.

## Future Campaign Lanes

The first campaign establishes reusable story spaces without resolving every conflict in the world:

- Conventional border war against a fictional regular military.
- Coalition defense of multiple cities.
- Peacekeeping after a ceasefire.
- Coastal conflict using naval units.
- Counter-proliferation around long-range weapons.
- Operations mode stories about reconstruction, remaining cells, refugees, and district governance.

Future antagonists must have distinct political and operational motives. Do not reskin the Ash Line for every conflict.

## Tone And Content Guardrails

- Serious, urgent, and humane; never celebratory about civilian suffering.
- Terror attacks may be shown through aftermath, sound, smoke, interrupted services, and rescue pressure. Avoid gore and spectacle built around victims.
- The cause of terrorism is power, coercion, and Qassem's ideology of control, not religion or ethnicity.
- Civilians have agency, occupations, disagreement, and recurring roles. They are not only rescue counters.
- Women appear across civilian, military, aviation, contractor, and hostile roles without sexualization or token framing.
- Do not turn civilian models into surprise enemies. Do not reward firing into crowds, collective punishment, torture, or destruction of protected sites.
- Hostile identification uses visible weapons, hostile action, trusted Intel, restricted-zone context, and objective confirmation.
- Fictional insignia must not imitate real extremist, religious, national, or militia symbols.
- Arabic or other regional-language text requires fluent human review. Generated text may not ship unreviewed.
- Every mission in a populated area needs a civilian-risk explanation and a credible reason for the use of force.
- Qassem may be persuasive, but the story must not present his attacks as necessary or secretly correct.

## Narrative Acceptance Questions

A high-level mission is narratively ready only if the answer to each question is clear:

1. What changed in Sahrin before this mission began?
2. What is the Ash Line or Vanguard Brigade trying to achieve beyond killing units?
3. Why is military action necessary here?
4. What protects civilians from being treated as targets?
5. Which character relationship changes?
6. What new information advances the chapter question?
7. Which current gameplay feature expresses the story beat?
8. What city consequence appears in the result?
9. Can a player understand the main story without optional mastery or purchases?
10. Does the mission avoid direct reference to a real current conflict?

## Related Authorities

- `AAA_Mobile_Game_Design_Document_v0_2.md`
- `First_Player_Experience_And_Story_Onboarding_Design.md`
- `Narrative_Presentation_And_Cutscene_Design.md`
- `Campaign_Mission_High_Level_Design_Catalog.md`
- `Campaign_Narrative_Sequence_And_Comic_Catalog.md`
- `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`
- `Gameplay_North_Star_And_Content_Grammar.md`
- `Level_And_Mission_Content_Plan.md`
- `SagaChapters/README.md`
