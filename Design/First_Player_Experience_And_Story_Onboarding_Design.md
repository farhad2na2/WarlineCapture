# WarlineCapture First Player Experience And Story Onboarding Design

Date: 2026-07-10

Status: Active high-level FPE authority

Scope: First launch, first session, story onboarding, progressive menu disclosure, returning-player entry, and high-level experience requirements. This is not an implementation tracker.

## Experience Goal

A first-time player should not open WarlineCapture into a complex command-base menu full of unknown modes, resources, store routes, and settings. The first launch should place the player inside the crisis, establish the Commander and ARIA, and begin M01 before product complexity becomes visible.

The intended first impression is:

```text
The city is under attack.
Command has failed.
ARIA found me.
People need a decision now.
I understand my first order.
```

## Authority

This document supersedes the menu-first identity flow in `FTUE_And_Command_Assistant_Design.md`. That document remains authoritative for reusable tutorial steps and ARIA assistance behavior where it does not conflict with this story-first route.

Narrative context comes from `Campaign_Narrative_Bible.md`. Product direction comes from `AAA_Mobile_Game_Design_Document_v0_2.md`.

## First-Launch Rule

On a fresh profile, the primary route is:

```text
Minimal logo
-> Story cold open
-> ARIA emergency boot
-> Diegetic Commander identity
-> Guidance choice
-> M01 First Contact
-> First debrief and clue
-> Command-base menu reveal
```

Do not route a fresh player to the normal Main Menu first. Do not require the player to inspect Campaign, Operations, Skirmish, Store, Commander, or Settings before playing.

## First 90 Seconds

Timing is a target band for later validation, not a loading-time claim.

| Target time | Beat | Player experience |
|---:|---|---|
| 0-5 sec | Studio and title mark | Minimal branding over an audio cue; no button wall. |
| 5-25 sec | Cold-open panels | Morning in Sahrin is interrupted by a coordinated bombing, blackout, and emergency radio failure. Violence is implied through sound, smoke, broken services, and urgent response rather than gore. |
| 25-40 sec | ARIA emergency boot | Fragmented ARIA reports that command authentication is gone, civilians are trapped near Old Market, and an armed patrol is moving toward them. |
| 40-55 sec | Commander identity | ARIA asks the player to identify themselves. The player enters a name and chooses one of a small set of free portraits inside the emergency terminal. |
| 55-65 sec | Guidance choice | `Full Guidance`, `Tactical Hints`, or `Veteran`. The choice is framed as command support, not a settings tutorial. |
| 65-90 sec | M01 opens | The operation map appears, ARIA identifies the player's squad and confirmed hostile patrol, and the first selection action begins. |

The optimization target is first meaningful input close to 60 seconds. The hard experience rule is that narrative presentation cannot become a long unskippable movie before play.

## Cold Open: `Command Lost`

### Story Beat

Sahrin begins in ordinary life: market shutters rising, a clinic generator starting, a fuel truck entering the industrial road, and aircraft crossing the morning sky. A blast interrupts the market district. Power sectors disappear from a city diagram. Radio calls overlap. A second attack strikes a transit route.

The perspective moves to a damaged JRC command terminal. ARIA restarts with missing memory blocks and no senior command response. She detects one valid officer credential: the player.

Recommended final line before identity:

```text
ARIA: Command chain unavailable. Civilians are exposed. Identify yourself, Commander.
```

This is a design line, not final localized dialogue.

### What The Opening Must Establish

- The city was alive before it became a battlefield.
- The attacks are coordinated and target civilian systems as well as JRC.
- The player belongs to a local legitimate response force.
- ARIA is damaged but useful.
- An immediate threat exists and waiting has a cost.
- The first mission is a response, not an unexplained sandbox.

### What The Opening Must Not Do

- Explain the entire Civic Relay conspiracy.
- Show Qassem clearly or reveal ARIA's self-partition.
- depict graphic casualties.
- Use real flags, terrorist symbols, scripture, or news footage.
- Ask for account creation, notifications, tracking consent, purchases, or a review before value is demonstrated. Platform-required consent remains available at the legally required moment and should be as focused as possible.

## Commander Identity Inside The Story

Identity creation is a brief authentication step, not a separate profile setup screen.

Required on first launch:

- Commander name.
- One free portrait selected from a small, diverse set.
- Default title `Field Commander`.
- Default frame.

Deferred until after M01:

- Expanded portrait catalog.
- Frames, badges, titles, cosmetics, and unlock sources.
- Account linking.
- Detailed profile statistics.

The player can change identity later from Commander Profile. The first choice must never be monetized or irreversible.

If the player declines to enter a name, use a localized neutral fallback such as `Commander`. If a portrait is not chosen, use a neutral default portrait. Do not force a fixed gender through voice or dialogue.

## Guidance Choice

| Choice | First-session behavior |
|---|---|
| Full Guidance | ARIA introduces one action at a time, highlights valid targets, explains errors, and offers `Show Me` or bounded `Do It` where supported. Recommended default. |
| Tactical Hints | Required objective and contextual hints appear, but normal controls remain open. |
| Veteran | Minimal control instruction, all required objective and critical warning information retained. |

ARIA asks once and states that the choice can be changed later. `Off` remains available in Settings after the first mission, but critical safety and objective feedback cannot be disabled.

## M01 First Contact Experience

### Story Contract

The player's initial squad is the nearest surviving JRC unit. A confirmed armed Ash Line patrol is moving through an Old Market access road after the attack. Civilians are stranded beyond the patrol. The player's task is to intercept the cell and secure the corridor.

### Teaching Sequence

| Beat | Player action | ARIA purpose | Story meaning |
|---|---|---|---|
| 1. Find your unit | Tap/select the command squad. | Identify owned unit and selection state. | The Commander establishes contact with surviving forces. |
| 2. Move to cover | Issue a move order to a visible safe point. | Teach destination marker and order feedback. | The squad advances without exposing civilians. |
| 3. Confirm threat | Focus the visibly armed hostile patrol. | Explain hostile confirmation and civilian distinction. | The target is hostile due to weapon, action, and intelligence, not appearance. |
| 4. Engage | Issue attack and adjust position if needed. | Teach attack marker, range feedback, Hold/Stop recovery. | The player prevents the patrol from reaching the civilians. |
| 5. Secure corridor | Reach the objective area and clear remaining threat. | Reinforce objective tracking and mission completion. | Samira reports that civilians can move to safety. |

M01 should not expose building, production, Oil, Fuel, import/export, transport controls, missiles, full roster management, or dense progression. Disabled or absent controls are preferable to unexplained complexity.

### Failure And Recovery

- ARIA explains invalid taps and orders in context.
- A stuck player receives a progressively stronger hint, then optional `Show Me`.
- `Do It` is available only for a command the runtime can safely execute.
- Player touch cancels ARIA preview or takeover immediately.
- Tutorial defeat offers immediate retry and a concise explanation; it does not apply permanent penalties.
- A player can pause, adjust accessibility, or exit. Exit leads to a simplified command-base route with `Resume First Contact` dominant.

## First Debrief

The first result is short and narrative-led:

1. Confirm corridor secured and civilians moving.
2. Show no more than three immediately understandable performance facts.
3. Grant first-clear rewards without opening the full economy explanation.
4. Reveal that the patrol carried a revoked ARIA credential.
5. Let ARIA state that the attack was planned using command information.
6. Transition to the recovered command-base interface.

Recommended final beat:

```text
ARIA: This key was revoked before I was activated. Someone inside the old network expected us to respond.
```

## Main Menu As An Earned Headquarters

The command-base menu appears after M01 as the place the player has restored, not as the first screen of the game.

### First Reveal

| Surface | First-session state |
|---|---|
| Continue Campaign | Dominant and active; points to M02 Establish The Base. |
| Campaign chapter map | Active with M01 complete and M02 highlighted. |
| Commander Profile | Active but secondary; identity editing available. |
| Settings and accessibility | Active and reachable. |
| Story Archive | Active with prologue and M01 entries. |
| Operations | Visible only if needed for future expectation; locked with a clear chapter-based reason. |
| Skirmish | Hidden or softly locked until core controls are learned. |
| Store | Not promoted during the first session; may remain absent until an authored progression point. |
| Advanced currencies and resource explanations | Revealed when first relevant, not all at once. |

Locked content uses a reason and an unlock condition. The interface must not present a wall of equally weighted destinations.

### Progressive Disclosure

| Milestone | Newly emphasized capability |
|---|---|
| After M01 | Campaign, Commander, Settings, Story Archive. |
| After M02 | Build/production progression and relevant roster surfaces. |
| After Chapter 1 | Skirmish and broader roster experimentation. |
| When Operations is production-ready | Operations and district consequence. |
| When store onboarding is appropriate | Store entry without interrupting active story momentum. |

## Returning Player Entry

### Active Campaign Player

```text
Logo
-> Continue Operation card
-> Resume mission, view short recap, or enter command base
```

The full cold open never replays automatically. The player can replay it from Story Archive.

### Returning After Several Days

Show a short optional recap containing:

- Current chapter and mission.
- Last major revelation.
- Current operational objective.
- One ARIA recommendation.

Do not show a long recap before the player can reach the menu or mission.

### Experienced New Player

The player may skip narrative panels and select `Veteran`, but the mission objective, hostile confirmation rules, and one-sentence story context remain. Skipping does not remove the prologue from Story Archive.

### Existing Profile After This Flow Ships

Do not force an established player through first-launch identity or M01 again. Offer the prologue as a Story Archive item and route to the existing save's most relevant destination.

## Accessibility And Comfort

- Subtitles are on by default for all narrative and ARIA speech.
- Subtitle size, background, speaker label, and timing controls are reachable before M01 through a compact accessibility button.
- Every cinematic is skippable after its first frame and replayable later.
- Pause is available during narrative and gameplay where platform constraints allow.
- Critical information is not audio-only or color-only.
- Camera motion in the cold open is limited and can be reduced.
- Guidance can be increased or decreased at any time.
- Failure messaging is factual and does not shame the player.
- Touch targets and text follow the active mobile UX and safe-area contracts.

## First-Session Content Budget

The first session should teach only what the player needs to complete M01 and understand why M02 matters.

| Show now | Defer |
|---|---|
| Commander, ARIA, civilian danger, confirmed hostile patrol, select/move/attack, objective, result, first clue. | Full economy, store, multiple modes, all unit categories, roads, Oil/Fuel, import/export, boarding, missiles, profile cosmetics, Operations metrics. |

The first session target is approximately 6-10 minutes including the cold open, M01, debrief, and command-base reveal.

## Narrative Presentation Requirements

- Use approved pre-generated images or stylized project renders, not runtime generation.
- Maintain the same Commander portrait selected by the player in later UI story surfaces.
- ARIA has a dedicated visual identity separate from the Commander.
- The attack aftermath uses specific Sahrin locations and recurring civilian contacts.
- The first hostile units use actual insurgent character references.
- The first gameplay frame visually continues the final story panel so the transition feels direct.

Detailed sequence tiers and art rules are in `Narrative_Presentation_And_Cutscene_Design.md`.

## High-Level Success Criteria

- A fresh player does not see the normal complex Main Menu before the story and M01 route.
- The first meaningful tactical action occurs within 60-90 seconds under normal first-launch conditions.
- The player can identify the Commander, ARIA, civilians, and confirmed hostile patrol.
- M01 introduces no more than one control concept at a time.
- The player understands that the attack was coordinated and that the revoked ARIA credential is the first mystery clue.
- The first debrief leads naturally to M02.
- The command-base menu reveals complexity progressively.
- Cinematics, guidance, and accessibility are adjustable without losing required objective information.
- Returning players bypass the prologue and can resume efficiently.

## External UX Basis

The direction follows current Android onboarding guidance to demonstrate value before setup friction, request only critical information up front, and teach contextually rather than through a long preliminary tour:

- Android Developers, `Onboarding`: https://developer.android.com/design/ui/mobile/guides/patterns/onboarding
- Android Developers, `Google Play Instant games best practices` (immediate-gameplay guidance remains useful even though the product program has changed): https://developer.android.com/topic/google-play-instant/best-practices/games

These are experience references, not implementation dependencies.

## Deferred To Implementation Planning

Scene routing, save flags, exact loading behavior, sequence data, UI hierarchy, analytics events, test cases, asset lists, task ownership, and estimates will be specified only after this high-level flow is accepted.
