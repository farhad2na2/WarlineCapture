# WarlineCapture Campaign Narrative And Content Redesign Recommendations

Date: 2026-07-10

Status: Accepted high-level direction; retained as the audit and recommendation record

Audit baseline commit: `8a52df3e96f82e0d100100d04b397361e31191b0`

## Purpose

This document records the professional game-design audit, recommendations, and proposed direction for aligning WarlineCapture's campaign, missions, chapters, Commander identity, ARIA, narrative presentation, and current gameplay features.

It was created before detailed story writing and now records the audit basis for the active high-level design set.

## Status And Boundaries

- This is a recommendation and audit document, not an implementation contract.
- Active authority now lives in `AAA_Mobile_Game_Design_Document_v0_2.md`, `Campaign_Narrative_Bible.md`, `First_Player_Experience_And_Story_Onboarding_Design.md`, `Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`, and `Narrative_Presentation_And_Cutscene_Design.md`.
- Working proper names remain subject to legal, cultural, localization, and trademark review even though the high-level direction is accepted.
- No mission, cutscene, chapter, or runtime feature should be marked implemented based on this document.
- Detailed scripts, dialogue, image prompts, mission configs, balance values, and implementation task IDs belong in later approved documents.
- Current-tree implementation evidence takes precedence over old progress claims when classifying feature maturity.

## Executive Recommendation

WarlineCapture should preserve its current five-chapter, five-mission campaign shape, but redesign it around one connected mystery rather than treating it mainly as a sequence of mechanic tutorials.

The recommended product direction is:

1. Keep the player as a customizable Field Commander.
2. Develop ARIA from tactical helper into the campaign's central companion and mystery link.
3. Give the hostile network a specific fictional identity, operational method, and human leadership instead of relying only on generic hostile-cell language.
4. Connect every major implemented gameplay system to at least one authored introduction mission and one later mastery mission.
5. Use civilian trust, infrastructure recovery, and evidence quality as visible consequences of the player's operational style.
6. Present story through short, data-driven tactical motion-comic sequences made from approved AI-generated stills, live captions, ARIA narration, sound, and restrained motion.
7. Rebuild the missing mission/objective/result/progression product layer before claiming the campaign is playable.
8. Rewrite and cross-link the canonical design documents after the premise, ARIA arc, presentation style, and ending model are approved. This high-level alignment was completed on 2026-07-10.

## Audit Baseline

### Current Runtime Product Reality

The current product build is a deep sandbox-style Match, not a wired campaign build.

| Area | Current-tree finding | Evidence |
|---|---|---|
| Build scenes | Build content contains Menu and additive Match. | `ProjectSettings/EditorBuildSettings.asset` |
| Match content | Match uses one fixed 2048x1024 configuration with large authored unit, building, prop, and vehicle sets. | `Assets/Game/Scripts/Composition/MatchSceneView.cs`; `Assets/Game/Configs/Scene/` |
| Campaign route | No current campaign/chapter/mission-selection route launches authored missions. | `Assets/Game/Scripts/UI/Contracts/UIRoute.cs`; `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab` |
| Mission payload | Match launch requests do not carry a mission, chapter, story sequence, objective set, or reward context. | `Assets/Game/Scripts/Components/MatchStartComponents.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs` |
| Objectives | No current runtime mission-objective evaluator owns authored objective completion. HUD objective rows are presentation defaults. | `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`; `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs` |
| Result flow | A result model and static prefab exist, but the ECS runtime gateway does not publish a live result. | `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`; `Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab` |
| Progression | Save data has aggregate counters and generic unlock arrays, but not authoritative chapter/mission/story progress. | `Assets/Game/Scripts/Persistence/SaveDataModel.cs` |
| Narrative playback | No current dialogue, briefing, comic-panel, VideoClip, or campaign narrative-sequence player is present. | Current `Assets/Game/Scripts` and `Assets/Tests` inventory |
| Historical product layer | Previous Campaign, Operation, Reward, Profile, Objective, and related runtime code was deleted in cleanup commit `a88141fcb`. | Git history |

### Feature Maturity Vocabulary

| State | Meaning |
|---|---|
| Implemented | The mechanic is wired into the active Match and can create player-facing gameplay. |
| Partial | A usable mechanic exists, but a significant rule, presentation, balance, or integration gap remains. |
| Scaffolded | Substantial runtime/UI/config work exists, but the feature is disabled, unreachable, or missing live bootstrap data. |
| Designed | The feature is described in active design documents but is not present as a current player-facing runtime system. |
| Historical | The feature or implementation existed previously but is not present in the current runtime tree. |

## Professional Assessment

| Area | Rating | Assessment |
|---|---:|---|
| Gameplay-system potential | 8/10 | The Match contains unusual systemic depth for a mobile RTS, including logistics, transport, missiles, civilians, construction, AI, and tactical command. |
| Existing campaign design | 5/10 | The five-chapter teaching structure is usable, but only Chapter 1 is detailed and it still contains unresolved content contracts. |
| Feature-to-content alignment | 4/10 | Several mature mechanics have no campaign introduction, dramatic purpose, or mastery mission. |
| Narrative and motivation | 2/10 | The current material provides premise and mission context but no antagonist arc, mystery structure, supporting cast, chapter transitions, or ending. |
| Documentation reliability | 3/10 | Active documents conflict with each other and with the current runtime tree; some progress claims refer to deleted systems. |

## Existing Strengths To Preserve

- The proactive Field Commander fantasy is stronger than a passive base-defense premise.
- One large 3D operation map supports tactical continuity between briefing, planning, combat, and consequence.
- The existing mission grammar has useful archetypes, visible objectives, star goals, civilian consequences, and mobile duration targets.
- ARIA already has a defined voice: calm, direct, tactical, subordinate to the player, and cancellable.
- Civilian households, displacement, refugees, infrastructure, and logistics can produce consequences beyond enemy destruction.
- Oil, Fuel, transport, air defense, artillery, construction, and resource exchange can create operational stories without inventing unrelated mechanics.
- Mission Result design already anticipates victory, partial success, defeat, withdrawal, rewards, and district consequences.
- The five-chapter structure is small enough to author coherently and large enough to support escalation.

## Primary Design Problems

### 1. The Campaign Is Currently A Curriculum, Not A Dramatic Arc

The chapter plan explains what mechanics the player learns, but not what the Commander wants, what the enemy wants, what changes after each victory, what ARIA is hiding or discovering, or why the next mission cannot be ignored.

### 2. Mature Mechanics Lack Authored Purpose

Automated Oil-to-Fuel logistics, player transport planes, parachute deployment, vehicle cargo drops, ground-to-air interception, ground-to-ground missiles, civilians/refugees, and ARIA recommendations are much more developed than the May campaign outlines acknowledge.

### 3. The Product Layer Is Missing

Story presentation should not be attached directly to the fixed Match scene. A mission context, objective model, result model, progression state, and story-sequence state are prerequisites for a campaign that can be tested and maintained.

### 4. ARIA Is Functional But Not Yet A Character

ARIA currently has a role, tone, and tactical interface, but no personal stakes, vulnerability, changing relationship with the Commander, or chapter-level transformation.

### 5. Civilian Consequences Are Abstract

Trust, infrastructure, refugees, and civilian safety need recurring human context. Without two or three recurring civilian/field contacts, these systems risk reading as meters rather than consequences.

### 6. The Enemy Is Too Generic

Repeated references to hostile or terrorist cells explain target legality but do not create memorable opposition. A fictional named network with a coherent method and leader is needed. It should not be based on a real faction, government, ethnicity, or current conflict.

### 7. Current Documents Do Not Provide One Reliable Source Order

The old GDD DOCX, five-chapter plan, Chapter 1 details, runtime cleanup, ARIA trackers, economy terminology, M01 IDs, and project dashboard disagree. Later agents cannot safely infer which claim is current.

### 8. Feature Volume Risks Mobile Cognitive Overload

The game should not expose every resource, command, transport mode, missile type, civilian system, and ARIA control option in the opening chapter. Mechanics need controlled introduction, reinforcement, combination, and mastery.

## Recommended Narrative Direction

### Working Campaign Concept

Working title: `Shattered Relay` (refined during high-level narrative design)

A coordinated blackout fractures the city's emergency and command network. ARIA activates with missing memory sectors and appoints the player as emergency Field Commander. Early hostile transmissions contain an obsolete ARIA authentication signature that should no longer exist.

As the Commander restores roads, fuel production, communications, air defense, and district control, evidence shows that the hostile network is not simply destroying the city. It is deliberately reconnecting selected infrastructure and forcing Response Command to restore the rest.

ARIA eventually discovers that she partitioned her own memory after the previous command chain was compromised. The partitions also keep a dormant citywide command-and-weapons network sealed. The hostile network needs the Commander to restore the city and ARIA to reconstruct the authorization path.

The final campaign question is not only whether the enemy is defeated. It is who should control the restored network and under what accountability.

### Core Themes

- Command requires judgment, not only firepower.
- Information can be incomplete without being false.
- Restoring infrastructure can create both safety and vulnerability.
- Civilian trust is operational power, not decorative morality.
- Automation is useful only while authority remains explicit and accountable.
- Victory should leave the city more governable, not merely less populated by enemies.

### Tone

- Near-future military and civic crisis.
- Serious and tense without graphic civilian suffering.
- Hopeful recovery remains possible.
- Fictional conflict, places, organizations, and characters.
- ARIA is not comic relief, a mascot, or an unrestricted super-AI.
- The Commander is not a superhero; success comes from preparation, prioritization, and responsible force.

## Commander Arc

| Chapter | Commander role |
|---|---|
| 1. First Response | Earn immediate tactical legitimacy and learn to command under ARIA guidance. |
| 2. Broken Grid | Become responsible for logistics, infrastructure, and civilian survival, not only combat outcomes. |
| 3. Hidden Network | Learn to judge uncertain evidence and accept responsibility for precision decisions. |
| 4. Air And Armor | Command strategic firepower while controlling escalation and collateral risk. |
| 5. Citywide Command | Decide the future command model of the city after proving full operational mastery. |

The selected Commander name, portrait, frame, and title should appear in narrative UI overlays. Full-body Commander art should not be required for every sequence because it would multiply production variants and reduce player identification.

## ARIA Arc

| Chapter | ARIA state |
|---|---|
| 1. Operational guide | Confident tactical instructor with unexplained data gaps. |
| 2. Systems partner | Coordinates logistics and detects patterns that contradict the official attack model. |
| 3. Unreliable archive | Acknowledges missing memory and asks the Commander to validate her conclusions against external evidence. |
| 4. Contested authority | The enemy attempts to exploit ARIA's credentials; bounded control and player override gain narrative meaning. |
| 5. Accountable intelligence | ARIA reveals why she partitioned herself and accepts the Commander's final authority over the restored network. |

ARIA should be incomplete and vulnerable, not secretly evil. The relationship should progress through demonstrated trust, transparent uncertainty, and explicit player authority.

## Antagonist And Supporting Cast Guidance

Final names and biographies should be written in the later narrative bible. This proposal recommends these functions:

| Role | Narrative function |
|---|---|
| Hostile network | A named fictional organization that uses infrastructure, misinformation, logistics, and former command credentials as weapons. |
| Network leader | A former command/logistics strategist who believes centralized control is the only path to order. The leader should mirror the Commander's temptation to value control over consent. |
| Field operations lead | A recurring military contact who grounds mission consequences in personnel, routes, and tactical reality. |
| Civilian infrastructure liaison | A recurring civilian voice who makes roads, fuel, housing, power, refugees, and collateral damage emotionally legible. |
| ARIA | The persistent companion, intelligence source, and mystery thread linking every chapter. |

Keep the recurring cast small. Three stable voices plus the Commander are more valuable than many one-mission characters.

## Mystery And Evidence Model

The main story should not require hidden grinding.

- Every chapter completion grants one mandatory `Protocol Fragment` or equivalent major revelation.
- Each mission first-clear grants a story beat that advances the central question.
- Optional objectives can grant supporting evidence, character context, or alternative interpretations.
- Three-star mastery must not be required to understand the main ending.
- Store purchases, Rush Tickets, Command Authority, or premium resources must never reveal story-critical evidence.
- Operations mode may provide supplemental evidence and district context, but Campaign completion must remain sufficient for the canonical conclusion.
- A Story Archive should provide chapter recap, recovered fragments, key characters, and replayable narrative sequences.

## Recommended Campaign And Feature Alignment

| Chapter | Operational fantasy | Primary mechanic exposure | Supporting systems | Major revelation |
|---|---|---|---|---|
| 1. First Response | Establish command during the first coordinated attacks. | Selection, movement, attack, building, production, warnings, basic boarding and breach. | ARIA guidance, basic civilian safety, result/stars. | Enemy traffic uses an obsolete ARIA credential. |
| 2. Broken Grid | Restore and defend the district logistics network. | Roads, Oil extraction, refinery conversion, Fuel storage, automated haulers, convoy protection. | Resource exchange after runtime enablement, refugees, repair/build placement. | Attacks are routing power and supplies toward selected command nodes. |
| 3. Hidden Network | Separate genuine threats from manipulated evidence. | Scan, Intel, raids, civilian-risk decisions, evidence extraction. | APC/helicopter boarding, Airlift, trust and infrastructure consequences. | ARIA's archive was intentionally partitioned after command compromise. |
| 4. Air And Armor | Control escalation across air, armor, and long-range fire. | Automatic G2A defense, radar/satellite support, manual G2G missiles, aircraft, transport-plane airdrop and cargo drop. | Fuel pressure, combined arms, tactical follow cinematics. | The enemy needs ARIA to unlock a dormant citywide command network. |
| 5. Citywide Command | Coordinate all systems across simultaneous objectives. | Full combined-arms and logistics mastery. | Multi-objective civilian protection, evidence, resource pressure, ARIA bounded control. | The Commander reconstructs the truth and decides the network's future. |

## Feature Exposure Recommendations

| Feature | Current maturity | First authored use | Mastery use | Design rule |
|---|---|---|---|---|
| Core command/combat | Implemented | Chapter 1 Mission 1 | Every finale | Teach selection, movement, attack, Hold, and Stop before expanding command density. |
| Building placement | Implemented/Partial | Chapter 1 Mission 2 | Chapter 2 and 5 | Do not imply construction time until a construction lifecycle exists. |
| Unit production | Implemented | Chapter 1 Mission 2 | Logistics and siege missions | Queue pressure should be a decision, not only waiting. |
| Roads | Partial/Scaffolded | Chapter 2 | Chapter 5 route crisis | Enable a clear player entry before making road construction mission-critical. |
| Oil/Fuel logistics | Implemented/Partial | Chapter 2 | Chapter 4 combined arms | Teach network placement and route protection, not truck micromanagement. |
| Resource exchange | Scaffolded | Chapter 2 after enablement | Chapter 5 emergency supply | A campaign objective must never depend on disabled recipes or paid Rush access. |
| APC/helicopter boarding | Implemented | Chapter 1 Mission 4 | Chapter 3 extraction | Boarding should solve a spatial/timing problem, not exist only as animation. |
| Plane/parachute/cargo drop | Implemented | Chapter 4 | Chapter 5 multi-front reinforcement | Contrast safe runway unloading with faster, riskier airborne deployment. |
| Automatic G2A launcher | Implemented | Chapter 4 Mission 1 | Chapter 4/5 saturation defense | Teach radar coverage, positioning, and protection rather than manual target tapping. |
| Manual G2G launcher | Implemented | Chapter 4 middle | Chapter 5 final approach | Make range, minimum range, preparation time, and civilian risk explicit. |
| Scan/Intel | Partial | Chapter 3 | Chapter 5 deception | Fix universal enemy minimap exposure before Intel becomes mission-critical. |
| Civilians/refugees | Partial | Light Chapter 1 exposure | Chapter 2/3/5 consequence | Show human context and HUD state before using severe penalties. |
| ARIA recommendations | Partial | Chapter 1 | Every chapter | Narrative claims must match actual executable command support. |
| Tactical follow attack cinematic | Implemented | Later Chapter 3 or 4 | Selected finale beats | Use for authored hero moments, not every ordinary attack. |

## Chapter Mission Grammar

Each five-mission chapter should follow this escalation:

| Slot | Purpose |
|---|---|
| Mission 1: Introduce | Create a clear need for the chapter's main mechanic and teach its first decision. |
| Mission 2: Test | Reuse the mechanic with less guidance and a meaningful consequence. |
| Mission 3: Twist | Change an assumption, introduce counterplay, or reveal that the apparent problem is incomplete. |
| Mission 4: Combine | Combine the chapter mechanic with one or two previously mastered systems. |
| Mission 5: Mastery And Reveal | Demand independent command, resolve the chapter threat, and reveal the next major story fragment. |

Every detailed mission specification should eventually include:

- `MissionId`
- `ScenarioSetupId`
- `OperationMapId`
- `StoryQuestion`
- `OpeningSequenceId`
- `InMissionNarrativeBeatIds`
- `MidMissionReversal`
- `PostMissionSequenceId`
- `ClueOrFragmentId`
- `PrimaryMechanic`
- `SupportingMechanics`
- `ARIAState`
- exact starting roster and allowed catalog
- exact objectives and failure conditions
- exact star goals
- exact rewards and unlocks
- civilian/district consequences
- outcome variants
- retry/replay rules
- implementation dependencies
- validation and visual-proof requirements

## Narrative Presentation Recommendation

### Recommended Format

Use a `tactical motion-comic` or `command dossier` presentation:

- approved generated still artwork
- slow camera pan, zoom, and restrained parallax
- panel transitions and occasional map/data overlays
- live localized captions and speaker names
- ARIA subtitle and optional narration
- music, radio texture, and focused sound effects
- no runtime generative-AI dependency

This format fits the command interface, keeps production manageable, hides small continuity imperfections better than photoreal video, and remains editable for localization and story changes.

### Sequence Tiers

| Sequence type | Recommended scope |
|---|---|
| Campaign prologue | 4-6 panels, approximately 35-60 seconds. |
| Chapter opening | 4-6 panels, approximately 30-50 seconds. |
| Mission introduction | 2-3 panels, approximately 15-25 seconds. |
| Mid-mission beat | ARIA message, radio line, objective update, or one short in-world camera beat. |
| Post-mission stinger | 1-2 panels, approximately 5-15 seconds. |
| Chapter ending | 4-7 panels, approximately 30-60 seconds. |
| Final ending | A longer authored sequence with limited metric-driven epilogue variants. |

Durations are starting constraints, not locked balance values. The player must remain able to advance text manually.

### AI Image Production Rules

- Generate production artwork offline and commit approved outputs as normal game assets.
- Begin from approved Unity scene captures, map views, unit references, character sheets, and ARIA references whenever possible.
- Use image editing or reference-conditioned generation to preserve recognizable buildings, vehicles, uniforms, terrain, and lighting.
- Do not generate every panel independently from text-only prompts.
- Establish one style bible, palette, camera language, costume sheet, faction marking sheet, and character reference pack before scaling.
- Keep all readable text outside generated images.
- Compose for both 16:9 and 20:9 safe areas.
- Store source prompts, reference paths, output IDs, approval status, and revision history in a manifest.
- Require human review for character continuity, vehicle geometry, weapons, insignia, hands/faces, civilian depiction, terrain alignment, and accidental real-world symbols.
- Do not market generated narrative stills as gameplay screenshots.

### Player Experience Requirements

- `Tap To Continue`
- `Skip Sequence`
- `Skip Previously Seen`
- `Pause Auto Advance`
- subtitles always available
- narration volume and enable setting
- replay from Story Archive
- short recap after returning from a long absence
- no story-critical information communicated only through audio
- no reward loss for skipping a sequence

### Technical Direction At Recommendation Level

- Use one reusable, data-driven narrative sequence player.
- Do not create bespoke code or a unique scene for every mission sequence.
- Link opening and post-mission sequence IDs from future mission definitions.
- Store seen sequence IDs and recovered fragment IDs in campaign save state.
- Use the installed Unity Timeline package only where it adds real value, such as a chapter hero sequence or synchronized audio/camera beat.
- Prefer the reusable data player for ordinary still-panel sequences.
- Load only the active sequence art and release it after transition into Match or the next route.
- Keep narrative UI separate from ECS simulation policy.

Unity Timeline reference: `https://docs.unity3d.com/ja/6000.0/Manual/com.unity.timeline.html`

### Recommended First Visual Slice

Do not generate the entire campaign immediately.

First prove:

1. Campaign prologue: approximately 4 panels.
2. Chapter 1 opening or M01 setup: approximately 3 panels.
3. M01 post-mission clue: approximately 2 panels.
4. Chapter 1 ending/reveal: approximately 4 panels.

This is approximately 13 approved stills, plus reusable UI and overlays. Scale only after visual continuity, mobile readability, pacing, loading, skip/replay, and story tone are accepted.

## Mode Narrative Roles

| Mode | Recommended narrative role |
|---|---|
| Campaign | Canonical authored story, Commander/ARIA relationship, major Protocol Fragments, and final resolution. |
| Operations | Optional district pressure, supplemental evidence, recurring consequences, and stories between campaign chapters. It must not be required to understand the main ending. |
| Skirmish | Non-canonical command exercises, simulations, experiments, and balance/replay mode. |

Campaign must be completed as a coherent product before Operations is used to carry essential story exposition.

## Consequence And Ending Recommendation

Use existing design concepts rather than adding a dialogue-tree-heavy RPG layer.

Three aggregate campaign dimensions are recommended:

| Dimension | Represents |
|---|---|
| Trust | Civilian safety, proportional force, rescue outcomes, and command legitimacy. |
| Evidence | Intel quality, recovered fragments, confirmed targets, and avoidance of false conclusions. |
| Infrastructure | Roads, logistics, fuel, civic structures, and the city's ability to recover. |

These values should affect debrief language, supporting-character outcomes, available final options, and epilogue details.

Recommended ending structure:

- One canonical central revelation for every player who completes the campaign.
- One final command decision with clearly explained consequences.
- A small set of epilogue variants based on Trust, Evidence, and Infrastructure.
- No requirement to earn every star or optional fragment to understand what happened.
- The strongest restoration outcome may require broadly responsible play, but the base ending must remain complete and satisfying.

## Design Guardrails

- Do not make ARIA the secret villain as a simple twist.
- Do not remove the player's authority by letting ARIA solve campaign missions automatically.
- Do not introduce unrestricted ARIA control that exceeds implemented bounded command support.
- Do not create story-critical paid evidence, premium endings, or monetized mission solutions.
- Do not require three-star completion to see the central ending.
- Do not use real-world factions, current conflicts, governments, religions, or ethnic groups as direct antagonist templates.
- Do not use generic hostile-cell language as the only enemy characterization.
- Do not add a new mechanic solely to support a story beat when an existing system can express it.
- Do not expose disabled Resource Exchange or Road controls as required campaign actions.
- Do not make automatic Oil/Fuel logistics into mandatory truck micromanagement.
- Do not teach automatic G2A defense as a manual reflex-targeting mechanic.
- Do not use G2G missiles without clear minimum range, impact timing, and civilian-risk feedback.
- Do not author cutscene text into raster images.
- Do not create one-off cutscene scenes or controllers for every mission.
- Do not claim campaign implementation completion without a playable route, objective evaluation, result, save, retry, and validation evidence.

## Documentation Alignment Recommendation

### New Proposed Documents

| Document | Purpose |
|---|---|
| `Design/Campaign_Narrative_Bible.md` | Approved world, factions, characters, Commander arc, ARIA arc, mystery, chapter reveals, terminology, and ending structure. |
| `Design/Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md` | Code-verified feature maturity, dependencies, first teaching mission, reinforcement, mastery, and validation evidence. |
| `Design/Narrative_Presentation_And_Cutscene_Design.md` | High-level sequence format, visual language, AI-assisted asset policy, continuity, audio, accessibility, and story archive. |
| `Design/First_Player_Experience_And_Story_Onboarding_Design.md` | Story-first cold open, identity, direct M01 entry, first debrief, and progressive menu disclosure. |
| Later implementation tracker | Stable task IDs, dependencies, owners, progress, commands, evidence, and handoff log. Explicitly deferred until the high-level set is accepted. |

### Canonical Documents To Update After Approval

1. `Design/README.md`
2. Root `README.md`
3. `Design/AAA_Mobile_Game_Design_Document_v0_1.md` and the old DOCX authority statement
4. `Design/GAME_DESIGN_REFERENCE.md`
5. `Design/Gameplay_North_Star_And_Content_Grammar.md`
6. `Design/Level_And_Mission_Content_Plan.md`
7. `Design/FTUE_And_Command_Assistant_Design.md`
8. `Design/ARIA_Assistant_ECS_Design.md`
9. `Design/Mission_Result_State_Spec.md`
10. `Design/Gameplay_Features_High_Level_Spec.md`
11. `Design/Gameplay_Features_Detailed_Spec.md`
12. `Design/Field_Logistics_Oil_Fuel_Design.md`
13. `Design/Automated_Fuel_Logistics_Design.md`
14. `Design/Resource_Logistics_Exchange_Design.md`
15. `Design/Combat_Catalog_And_Upgrade_Design.md`
16. All files under `Design/SagaChapters/`
17. `Design/Project_State_Source.json`, followed by dashboard regeneration

### Required Documentation Status Fields

Every rewritten authority document should state:

- owner
- status
- last verified date
- verified commit
- source-of-truth precedence
- implementation maturity vocabulary
- superseded documents
- related runtime owners
- related validation commands
- unresolved decisions

### Automated Documentation Validation

Later implementation should add checks for:

- referenced local paths exist
- mission IDs are unique
- sequence IDs are unique
- chapter and mission ordering is valid
- each mission references existing feature/catalog IDs
- each story-critical clue is granted by a reachable mission
- every mission has opening/result/retry behavior
- every implemented-status claim links to current code or a current validation artifact
- no active document links to a deleted runtime owner without a historical label
- no conflicting canonical camera, map, minimap, reward, or economy IDs

## Recommended Work Order

### Phase A: Product Decision Lock

- Approve or reject the working campaign premise.
- Approve the five-chapter structure.
- Approve the Commander and ARIA arcs.
- Approve the tactical motion-comic presentation.
- Approve the canonical-ending plus epilogue-variant model.
- Lock fictional setting and antagonist boundaries.

### Phase B: Source-Of-Truth Repair

- Mark old conflicting GDD sections as superseded.
- Classify every relevant feature as Implemented, Partial, Scaffolded, Designed, or Historical.
- Resolve M01 ID conflicts and Chapter 1 content blockers.
- Correct ARIA scope claims and economy terminology.
- Update the project-state source before regenerating its dashboard.

### Phase C: Narrative Bible

- Name the city, hostile network, network leader, field lead, and civilian liaison.
- Write the central mystery, chapter reveals, Commander arc, ARIA arc, themes, tone, and ending logic.
- Define story terminology and spoiler boundaries.

### Phase D: Campaign And Feature Matrix

- Assign every major mechanic to introduction, reinforcement, and mastery missions.
- Remove or defer mechanics that are not player-reachable.
- Define chapter-level resource exposure and cognitive-load limits.

### Phase E: Mission Authoring

- Rewrite Chapter 1 with exact IDs and narrative beats.
- Detail Chapters 2-5 using the required mission schema.
- Connect mission outcomes to Trust, Evidence, Infrastructure, rewards, and story progression.

### Phase F: Narrative Presentation Specification

- Define sequence assets, UI, persistence, skip/replay, audio, localization, image production, memory/loading, and tests.
- Create the implementation tracker before runtime code changes.

### Phase G: Runtime Product Foundation

- Reintroduce mission context, authored objectives, results, progression, and save state through the current architecture.
- Keep the fixed Match fallback intact until the campaign route is validated.

### Phase H: Vertical Slice

- Implement prologue, M01 opening, playable M01 objective/result loop, M01 clue, Chapter 1 ending preview, save/replay, and visual proof.
- Validate on mobile before scaling image and mission production.

### Phase I: Campaign Scale-Out

- Expand one chapter at a time.
- Do not mass-generate story art ahead of approved scripts and gameplay contracts.
- Revalidate feature maturity and campaign dependencies at every chapter gate.

## Approval Decisions

The user approved movement into high-level design and added the Middle Eastern setting, unique-character, explicit insurgent-antagonist, wider-war, and story-first first-launch requirements. The decisions below are therefore accepted as high-level working direction; proper names remain working names.

| Decision ID | Recommendation | Status |
|---|---|---|
| `CNR-DEC-001` | Keep five chapters with five missions each. | Accepted high-level |
| `CNR-DEC-002` | Use the refined `Shattered Relay` working mystery premise. | Accepted high-level |
| `CNR-DEC-003` | ARIA is incomplete and self-partitioned, not secretly evil. | Accepted high-level |
| `CNR-DEC-004` | Use a small recurring cast: ARIA, one field lead, one civilian infrastructure liaison, and one antagonist leader. | Accepted high-level |
| `CNR-DEC-005` | Use tactical motion-comic sequences generated offline from approved references. | Accepted high-level |
| `CNR-DEC-006` | Use one reusable data-driven sequence player; reserve Timeline for exceptional sequences. | Accepted high-level |
| `CNR-DEC-007` | Use one canonical revelation and limited Trust/Evidence/Infrastructure epilogue variants. | Accepted high-level |
| `CNR-DEC-008` | Campaign contains the complete central story; Operations adds optional context only. | Accepted high-level |
| `CNR-DEC-009` | Rebuild mission/objective/result/progression foundations before full campaign content production. | Accepted high-level |

## Evidence And Primary References

### Current Design Authorities Reviewed

- `Design/README.md`
- `Design/AAA_Mobile_Game_Design_Document_v0_1.md`
- `Design/AAA_Mobile_Game_Design_Document_v0_1.docx`
- `Design/GAME_DESIGN_REFERENCE.md`
- `Design/3D_SingleMap_Gameplay_Direction.md`
- `Design/Gameplay_North_Star_And_Content_Grammar.md`
- `Design/Command_Offensive_Premise_Alignment.md`
- `Design/Level_And_Mission_Content_Plan.md`
- `Design/FTUE_And_Command_Assistant_Design.md`
- `Design/ARIA_Assistant_ECS_Design.md`
- `Design/Mission_Result_State_Spec.md`
- `Design/Economy_Reward_Design.md`
- `Design/Combat_Catalog_And_Upgrade_Design.md`
- `Design/Field_Logistics_Oil_Fuel_Design.md`
- `Design/Automated_Fuel_Logistics_Design.md`
- `Design/Resource_Logistics_Exchange_Design.md`
- `Design/SagaChapters/README.md`
- all five active chapter files under `Design/SagaChapters/`
- `Design/Project_State_Source.json`
- `Design/Project_State_Dashboard.md`

### Current Runtime Areas Reviewed

- active scenes and build settings
- Match scene composition and scene configs
- tactical commands and combat
- building placement and production
- road construction
- Oil/Fuel production, hauling, storage, and consumption
- Resource Exchange config, ECS, UI, and bootstrap state
- transport boarding, helicopter rope drop, plane airdrop, and cargo drop
- ground and air missile launcher systems
- scan/intel and minimap behavior
- civilians, households, displacement, and refugees
- ARIA recommendations, messages, command intent, narration, and UI
- UI shell routes, Match launch, objective rows, result gateway, and persistence

## Next Step After High-Level Alignment

Review and accept the active high-level design set before creating step-by-step implementation plans. The next phase should define implementation tracks for first-player routing, Campaign mission/objective/result/persistence foundations, narrative sequence playback, Story Archive, character/art production, and one M01 story-to-gameplay vertical slice. Do not mass-generate final dialogue or story art before those implementation plans and visual references are approved.
