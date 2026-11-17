# ARIA: public-information audit and reaction to the base-under-attack warning

ARIA worker, 2026-11-18. Branch `missions/aria-base-alert`, cut from `missions/integration` at `0c49ffc93` (this already includes 7290b1ced and 3a62d3cd9). The work is code only and Unity was not run. It implements the orchestrator decision on the S002 lone tank: legibility first, measured counterplay second, and a guard tower only if the measurement fails.

## 1. Audit: what ARIA reads

| Place checked | What it reads | Verdict |
|---|---|---|
| `MatchHudAssistantUiSystemHelper.SkirmishWatch.cs` `ObserveMapThreat`: home threat, ranged attacker, column threat, tracked contact | `watchMap.PresentedContacts`, which holds the minimap markers the player sees: fog-visible units and last-seen ghosts | Public |
| Same file, home ground, far ground and attacker ground | Own base objective position, screen raycasts and own presented markers | Public |
| `AriaGroundBaseAssaultPolicy.StepCore` base-damage trigger (`AriaBaseDamagePolicy`) | `model.PlayerHealth` = `SkirmishBaseAssaultHudProjection.ReadPlayerBase().DisplayedCurrent`, the HUD base label | Public |
| Enemy base health (progress watchdog, assault) | `ReadEnemyBase().DisplayedCurrent`. It is fog-aware: the label shows "—" when hidden | Public |
| `SkirmishAriaPublicProjection`: `VisibleHostileCombat`, `VisibleHostileAir`, `ObserveSlippingHostile` (intercept rule 1ae08e59a, still live after the iteration-2 revert 88d29f828) | Every hostile passes `SkirmishFogService.IsVisibleToFaction(em, unit, 1)`. The intercept also uses own unit positions and the marked enemy objective (the HUD marker) | Public. A test scan counts the fog checks against the hostile reads |
| `ObserveIntercept` | Tap only while the fog-visible vehicle is on screen | Public |
| Squad cards, Attack/Focus/page buttons, catalog | The presented UI only | Public |
| **`AssistantThreatReadModelSystem.TryUpsertThreat`** (ARIA's "X under attack from Y" voice and text, the assistant attack recommendation `AssistantReadModelSystems.cs:408-418`, and the target lock `AssistantTargetLockReadModelSystem.cs:118`) | The damage event's source entity: its name, world position and entity, **with no fog check** | **Hidden-state channel, fixed.** In Skirmish, a shooter the player cannot see is now reported as "SOURCE UNKNOWN". Its position is replaced by the target's, distance is 0, and `HostileSource` is `Entity.Null`, so no recommendation or lock can target it. Once it is visible (fog or base vision), it is named again |

ARIA's touch planner never read `AssistantThreatReadModel` (checked: no consumer in `Aria*` or in the watch). Only ARIA's messages and recommendations did. No other hidden reads were found.

## 2. Interface: HUD warning to ARIA (`ISkirmishBaseAlertPresentation`)

The interface is defined in `Assets/Game/Scripts/UI/Components/ISkirmishBaseAlertPresentation.cs` (namespace `Game.UI.Runtime`). The HUD component that presents the warning implements it, either on the `SkirmishMatchView` GameObject or on a child of it. ARIA finds it with `GetComponentInChildren<ISkirmishBaseAlertPresentation>(true)`, probing every 30 frames until it is found. Nothing else needs to be wired.

| Member | Meaning (what the player sees) |
|---|---|
| `bool BaseAlertPresented` | The warning (row or banner, voice, minimap ping) is on screen now |
| `byte BaseAlertReason` | 1 = a hostile entered the base perimeter, 2 = the base took damage |
| `int BaseAlertSequence` | Increases by one at each new warning onset |
| `bool BaseAlertAttackerShown`, `Vector3 BaseAlertAttackerWorld` | The attacker the warning points at, only while the player can see it (fog or base vision) |
| `bool BaseAlertDefenderKnown` | A nearest-defender card is highlighted, on any page |
| `int BaseAlertDefenderSlot` | Slot 0 to 4 of that card on the visible page, or -1 if it is on another page |
| `Button BaseAlertDefenderButton` | The warning's control that pages the roster to that card (navigation only) |

For `missions/hud-base-threat` at `0ac831be4`, most members map directly:

| Interface member | Current HUD value |
|---|---|
| `BaseAlertPresented` | `BaseUnderFirePresented` |
| `BaseAlertAttackerShown` / `BaseAlertAttackerWorld` | `model.BaseAttackerVisible` / `model.BaseAttackerWorld` |
| `BaseAlertDefenderKnown` | `baseFireDefenderGroup != 0` |
| `BaseAlertDefenderButton` | `BaseUnderFireDefenderButton` |

Three members need new code in the HUD:
- **`BaseAlertDefenderSlot`:** needs the tray's visible slot for `baseFireDefenderGroup`.
- **`BaseAlertSequence`:** increment it when `SkirmishBaseFireTracker.Observe` returns true, and when a perimeter entry starts a warning.
- **`BaseAlertReason`:** set it when the warning starts (1 for perimeter entry, 2 for damage).

Until the HUD implements the interface, ARIA's own public trigger stays: the HUD base-health label falling, which is the same signal the damage warning uses.

## 3. Reaction (S002 planner; shared `AriaHomeDefensePolicy`)

**Trigger.** `BaseAlertPresented` is a home threat on the frame the player is warned. It has the same effect as the base-health drop and the presented minimap contact. The round reason becomes `ReasonBaseAlert` (3).

**Next touch.** The defence takes the next touch: an unfinished map navigation or assault inspection is dropped (`YieldToHomeDefense`). The defence closes the map itself.

**Target.** The attacker the warning points at replaces a minimap guess:
1. tap the unit itself when it is on screen;
2. otherwise, ground beside it.

**Defenders: the nearest suitable units** (`AriaHomeDefensePolicy.DefenderMask`, at most 2 cards):
- The highlighted nearest card is selected. If it does not fight armour, one anti-armour card is added.
- With no highlight, one anti-armour card is chosen, otherwise the first offered assault card.
- The choice is fixed once per round in `HomeDefenseMask`, so a moving highlight cannot add cards.
- If the nearest card is on another page, the warning's own control pages the roster to it, with one press per round. There is no page-by-page walk.

**Touch budget from warning to order** (lane cadence is about 1.75 s per touch; lane 14 round 1 took 5.4 s):

| Situation | Touches | Time at lane cadence |
|---|---|---|
| Attacker on screen, nearest card on page | card, Attack, tank (3) | about 5.25 s |
| Card on another page | page control first (4) | about 7 s |
| Rifle nearest | adds one card (4) | about 7 s |
| Camera away from home | adds the Home tap | — |

Round 2, escalation, the bounded give-up and the disarm are unchanged from 7290b1ced and 3a62d3cd9.

**Evidence lines:**
- `[AriaS002Trace] event=base_alert state=onset reason= seq= attackerShown= defenderKnown= defenderSlot= pageControl= homeDefenseStage= t=` is logged once per onset.
- `event=home_defense` lines gain `defenderMask=`, `alert=`, `alertAge=` (seconds since the warning onset, match clock) and `defenderSlot=`.

The step-2 measurement can read the reaction time directly from `state=ordered ... alertAge=`.

**Not changed:**
- The generic planner (S004, S026, S049, S050, S006) does not use the warning. It keeps its sweep, because `ProportionateDefense` is false there.
- Unit stats, rules, enemies, deadlines and forces.

## Tests

`AriaBaseAlertDefenseTests` adds 6 tests:
- defender mask rules;
- warning to order in 3 touches with no Home tap and no paging;
- the nearest card on another page, reached with one press of the warning's control;
- a nearest rifle squad plus one anti-armour card, latched;
- without the warning, the old trigger and path;
- a source scan: the warning read touches no simulation state, the projection fog checks, and the threat-row mask.

All 60 standalone policy tests pass (net8 harness). The Unity compile and lane have not run.
