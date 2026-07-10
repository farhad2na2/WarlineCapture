# WarlineCapture Mission Result State Spec

Date: 2026-05-23
Owner: Design
Surface: `POP-05 Mission Result`

2026-07-10 narrative amendment: Campaign results must carry the immediate human consequence, character response, and mandatory clue/reveal defined by `Campaign_Narrative_Bible.md`. First-launch M01 uses the debrief-to-command-base reveal in `First_Player_Experience_And_Story_Onboarding_Design.md`.

This spec defines the full result/debrief screen for the 3D single-map WarlineCapture direction. `POP-05` is one reusable result surface with multiple runtime states, not separate unrelated victory/loss screens.

## Purpose

`POP-05 Mission Result` appears when a match ends, the player withdraws, or the operation is forcibly resolved. It must explain what happened, why the outcome was assigned, what rewards or penalties apply, what district/civilian consequences changed, and where the player goes next.

The screen must work for Campaign, Operations, Skirmish, and custom game modes without changing the core layout.

## Result States

| State Id | Player-Facing Header | Trigger | Star/Score Behavior | Reward Behavior | Primary CTA |
|---|---|---|---|---|---|
| `VictoryComplete` | `OPERATION COMPLETE` / mission-specific success title | Required objective group completed and failure conditions not triggered. | Show earned stars, objective checks, best-result improvement, and optional mastery goals. | Grant authored clear rewards, first-clear bonuses, commander XP, and mode rewards. | `CONTINUE` |
| `PartialSuccess` | `OBJECTIVE SECURED` / `PARTIAL SUCCESS` | Primary objective completed but major secondary, civilian, loss, timer, or district condition failed. | Show 1-2 stars depending on authored goals. Failed optional goals remain visible. | Grant reduced authored rewards and commander XP if configured. | `CONTINUE` |
| `DefeatFailed` | `OPERATION FAILED` | Required objective failed, command unit destroyed, base lost, timer expired, or critical civilian condition breached. | Show zero or minimum stars based on mission config. Failed objective rows explain the reason. | Grant only fail-safe participation rewards if configured; do not imply a full clear. | `RETRY OPERATION` |
| `Withdrawn` | `FORCE WITHDRAWN` | Player abandons, retreats, exits through pause confirmation, or an authored extraction/withdraw state ends the match. | Show no clear stars unless the mission explicitly allows extraction scoring. | Grant extracted/recovered rewards only; lost resources/units are listed separately. | `RETURN TO MAP` |
| `SimulationResolved` | `OPERATION RESOLVED` | Operations mode auto-resolves an action without live battle, or a future async result arrives. | Show operation outcome metrics instead of combat stars when no live combat happened. | Grant authored Operation rewards and district deltas. | `VIEW DISTRICT` |

`VictoryComplete` is the current visual-lock hero target. `DefeatFailed`, `PartialSuccess`, and `Withdrawn` need their own target-lock references because the emotional tone and CTA priority differ. They should reuse the same implementation layer family wherever possible.

## Shared Layout

All states use the same shell:

- Result header: outcome title, mission title, mode/source label, operation area, difficulty, duration.
- Mission snapshot: 3D operation image or fallback environment art.
- Star/objective panel: star row, required objectives, optional goals, and failed/complete states.
- Performance stats: enemies defeated, units lost, civilians saved/harmed, resources spent, buildings captured/destroyed, oil/fuel recovered when relevant.
- Rewards panel: commander XP, Credits, Supplies, Fuel, Intel, unlocks, and explicit zero/reduced reward rows.
- Consequence panel: Civilian Safety, District Trust, Hostile Influence, Infrastructure, and mode-specific deltas.
- Narrative beat: one concise outcome, character response, and evidence/reveal row when authored. It must distinguish mandatory story progress from optional evidence.
- Bottom action bar: retry/replay, loadout/settings route if applicable, continue/return primary CTA.

Text, values, objective rows, reward rows, CTA labels, star count, and consequence deltas must be live UI data. Frames, icons, stars, fills, and background art are separate visual layers.

## State-Specific Presentation

### VictoryComplete

- Accent: gold with restrained olive success indicators.
- Hero message: success and next operational implication.
- CTA order: `REPLAY`, source-specific route, `CONTINUE`.
- Rewards: full clear grants and first-clear bonuses if eligible.
- Consequences: positive civilian/district deltas can be shown as gains; negative side effects still appear.

### PartialSuccess

- Accent: gold mixed with amber caution.
- Hero message: objective achieved, but cost or missed goal is called out.
- CTA order: `REPLAY`, `ADJUST LOADOUT` when supported, `CONTINUE`.
- Rewards: reduced or partial grant rows must clearly say why.
- Consequences: show both success and cost. Do not hide civilian/infrastructure damage behind the success title.

### DefeatFailed

- Accent: warning amber/red, not celebratory gold.
- Hero message: direct failure reason such as `COMMAND SQUAD LOST`, `OBJECTIVE FAILED`, `CIVILIAN THRESHOLD BREACHED`, `BASE OVERRUN`, or `TIME EXPIRED`.
- CTA order: `ADJUST LOADOUT`, `RETRY OPERATION`, `RETURN TO MAP`.
- Rewards: participation rewards are allowed only when configured. Display `No Clear Reward` for clear-only grants.
- Consequences: district/civilian penalties are shown clearly.

### Withdrawn

- Accent: muted amber/olive.
- Hero message: withdrawal confirmed, recovered assets listed.
- CTA order: `REPLAY` only if the match can be restarted, `RETURN TO MAP`, `MAIN MENU`.
- Rewards: only extracted/recovered rewards and saved progress. Lost cargo or abandoned objectives are separate rows.
- Consequences: withdrawal can reduce trust/security or preserve units depending on mission rules.

### SimulationResolved

- Accent: neutral command-base style.
- Hero message: auto-resolved operation result, not live combat victory language.
- CTA order: `VIEW DISTRICT`, `OPERATION REPORT`, `CONTINUE`.
- Rewards: Operation rewards and daily deltas.
- Consequences: district metrics take priority over combat stats.

## Required Result Data

`MissionResultData` must include:

- `ResultState`
- `ResultReasonCode`
- `SourceMode`: Campaign, Operations, Skirmish, Custom, Tutorial
- `MissionId`
- `OperationMapId`
- `DifficultyId`
- `DurationSeconds`
- `PrimaryObjectiveState`
- `ObjectiveRows`
- `StarGoalRows`
- `EarnedStarCount`
- `PreviousBestStarCount`
- `Stats`: enemies defeated, friendly units lost, civilians saved, civilians harmed, buildings captured/destroyed, resources spent, oil recovered, fuel recovered
- `RewardRows`
- `ConsequenceRows`
- `UnlockedRows`
- `NextRoutes`
- `CanReplay`
- `CanRetry`
- `CanAdjustLoadout`
- `NarrativeOutcomeId`
- `CharacterResponseId`
- `MandatoryStoryBeatId`
- `OptionalEvidenceRows`
- `TrustDelta`
- `EvidenceDelta`
- `InfrastructureDelta`

## Reason Codes

Reason codes must be authored and user-readable:

| Reason Code | Display Text |
|---|---|
| `AllRequiredObjectivesComplete` | All required objectives complete. |
| `PrimaryObjectiveCompleteSecondaryFailed` | Primary objective secured; secondary goals failed. |
| `CommandUnitDestroyed` | Command squad lost. |
| `BaseDestroyed` | Forward base destroyed. |
| `ObjectiveTargetEscaped` | Target escaped the operation area. |
| `TimerExpired` | Operation timer expired. |
| `CivilianThresholdBreached` | Civilian safety threshold breached. |
| `PlayerWithdrawn` | Forces withdrawn by commander order. |
| `OperationAutoResolved` | Operation resolved from district command. |

## Route Rules

| Source Mode | Continue Route | Retry Route | Secondary Route |
|---|---|---|---|
| Campaign | `SCN-05 Campaign Map` or next briefing if chained. | Restart same mission from `SCN-06`/`SCN-07` depending on loadout rules. | `SCN-07 Loadout / Squad Prep` when available. |
| Operations | `SCN-11 Operations Dashboard` or district detail. | Retry only if operation action permits. | `SCN-12 District Detail Actions`. |
| Skirmish | `SCN-13 Skirmish Setup`. | Relaunch same preset/custom setup. | `SCN-02 Main Menu`. |
| Custom | Custom setup screen / Skirmish setup variant. | Relaunch same custom rules. | `SCN-02 Main Menu`. |
| Tutorial / first-launch M01 | Play the first debrief and revoked-ARIA-credential clue, then reveal the command-base menu with M02 highlighted. | Retry current tutorial mission. | Simplified command base with `Resume First Contact` after deliberate exit. |

## Visual-Lock Requirements

- One implementation layer pack can serve all states if frames, icons, stars, fills, and CTAs remain separate.
- At minimum, target-lock references are required for `VictoryComplete`, `DefeatFailed`, `PartialSuccess`, and `Withdrawn`.
- `SimulationResolved` can initially use the `PartialSuccess` shell with Operation-specific copy until its own target is needed.
- Defeat and withdrawal must not reuse victory wings or celebratory emblems as the main header art.
- Result snapshots may be state-specific, but must come from separate source images, not from cutting target mockups.

## Acceptance Tests

- Victory, defeat, partial success, and withdrawal can all render from `MissionResultData`.
- Each state shows the correct header, reason, star count, CTA order, rewards, and route.
- Failed objectives remain visible with readable failure reasons.
- Clear-only rewards are hidden or disabled on defeat/withdrawal and never look granted.
- Civilian and district consequence rows always render, including zero deltas.
- Campaign results show the authored character/clue beat and never gate mandatory Protocol Fragments behind star count, purchases, or optional evidence.
- Trust, Evidence, and Infrastructure changes explain the triggering actions rather than appearing as unexplained meters.
- Continue/retry routes match the source mode.
- No target-lock text or values are baked into implementation layers.
