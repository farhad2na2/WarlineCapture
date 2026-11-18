# S002 Desert Base: base-under-fire threat legibility (diagnosis)

HUD worker, 2026-11-17. The orchestrator requested a code and log diagnosis before any base-under-fire change is merged. Unity was not run. Line references are to `missions/integration` at `b346a4b9c`. The log is `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/owner-playtest/warline-designer-s002-20261117.log`, written **the log** below. All times are match-clock seconds unless marked *audio clock*.

## What happened (log facts)

| Fact | Evidence |
|---|---|
| A single enemy Battle Tank (420 HP) attacks. At t=0.09 it receives a Move order to (949, 344.7), about 66 m from the player Barracks at (1001, 303.5). At t=66.91 it receives Attack on the Barracks. | log:2346, log:2416. initial_world is at log:2107 |
| The first hit lands at t=67.22. Each hit does 28 damage, about one per second, on an 800 HP Barracks. The Barracks reaches 72/800 at t=92.63 and falls at about 1:35. **The response window from the first hit is about 28 s.** | log:2441 to log:2955. Per-hit cadence is at log:2463, 2498, 2520 and later (`requestedAt` steps of about 1.0 s) |
| The tank fires from cell (1001, 277), about 26 m south of the Barracks. Its health stays at **420/420** through the last logged hit. No player unit ever engages it. | log:2463 to log:2885 (`attacker ... hp=420/420`) |
| The three same-seed replays repeat this pattern. First hits come at 70.88, 68.07 and 68.54. The base reaches 72/800 between 93.95 and 97.17. | log:4843 to 5128, log:6443 to 6740, log:7931 to 8139 |
| In replay 1 the designer made 11 touches and had a Move order accepted (`TacticalBannerAcceptedMoveTitle`) before the tank's Attack order at t=70.44. The base still fell. | log:4467 to 4788, log:4670, log:4818 |
| The generic "unit under attack" alert is **suppressed** on every hit. | log:2463 (`Suppressed Alert.Unit.UnderAttack reason=GenericUnderAttackAlertDisabled`) |
| ARIA's ground-attack voice line (`VO.ARIA.Message.WarningGroundAttackType`) first plays at *audio clock* 118.11. The first hit was requested at *audio clock* 101.49, so the line plays **about 16.6 s after the first hit**, when the base is near half health (inferred from the shared AudioDiag clock). It then repeats about every 8 s. | log:2463 vs log:2485; repeats at log:2674, 2907, 2942. Culled at log:3002 |

## What the player is shown when the base is hit

1. **Top HUD strip.** The `YOUR MAIN BASE` button shows the number dropping (for example 772/800). It has no colour change, flash, direction or "under fire" state. Sources: `Assets/Game/Scripts/UI/Components/SkirmishMatchView.cs:58-62` (button), `:106` (`SetBaseLabel` each frame) and `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Skirmish.cs:141-147` (label from `SkirmishBaseAssaultHudProjection`).
2. **Threat warning strip.** In Skirmish this strip stays empty. `MatchHudThreatVisibilityView` is driven only by `ThreatWarningPresentationState` (`Assets/Game/Scripts/Systems/ThreatWarningPresentationState.cs:19-36`). That state is fed by campaign defense records (`ThreatWarningResolveSystem.cs:21` requires `CampaignMissionRootComponent`) or by `ThreatDetector` sensor sweeps (`ThreatDetectionWarningSystem.cs:45-60`). The S002 starting force has no detector (log:2107), and the log contains no threat-warning presentation lines.
3. **Generic under-attack audio.** This is silenced by design: "Leave generic damage alerts silent so ARIA owns match-start threat voice" (`Assets/Game/Scripts/Systems/UnitAttackSystem.cs:455-469`).
4. **ARIA.** `AssistantThreatReadModelSystem` turns every damage observation on a player-owned target into a threat row (`Assets/Game/Scripts/UI/Shell/Ecs/AssistantThreatReadModelSystem.cs:129-195`). `AssistantMessagePrioritySystem` writes "BARRACKS under attack from Battle Tank; damage 28" and maps it to the ground-attack voice line (`AssistantMessagePrioritySystem.cs:328-351`). The text goes to the ARIA assistant panel (`AssistantPanelUiSystemHelper.ApplyReadModel` → `AriaCommandAssistantPopupView`), which the player must open. During normal play the player hears the voice line with a delay of about 16 s and gets no direction. I did not verify whether a narration subtitle appears over the HUD.
5. **World cues.** The Barracks shows a recent-damage health bar (`UnitAttackSystem.cs:435-447`). Impact effects on your own base are always presentable, because `SkirmishFogConsumerUtility.PlayerImpactVisible` returns true for a visible own target (`Assets/Game/Scripts/Runtime/Skirmish/SkirmishFogConsumerUtility.cs:25-27`). **These cues appear only if the camera is already on the base.** The owner's camera was elsewhere ("the attacker was outside my view").
6. **Minimap.** It presents only fog-visible contacts, and ARIA reads the same presented contacts (`MatchHudAssistantUiSystemHelper.SkirmishWatch.cs:792-826`). A hidden shooter does not appear there.

## Fog and vision at the base

- Player vision comes only from faction-1 units' acquisition radius (aggro or attack range) and from `ThreatDetector` radii. No separate vision radius exists (`SkirmishFogService.cs:216-236, 269-279`). The Barracks is not an observer, and **firing does not reveal the shooter**.
- No player unit engaged the tank (420/420 throughout), so probably no observer had it within acquisition range. If so, the tank was hidden while it destroyed the base. That would explain why the owner "saw no shot … that told me where to respond". This is consistent with the log but not proven by it, because the log has no fog-state line. **Owner/Unity check:** confirm the tank's `SkirmishContactSightComponent` at t=67 to 95 in the next lane run.
- The opening camera frames the player rifle `role.rifle.1.0.0` at (983, 318.5), about 23 m from the Barracks (log:2082). The brief tells the player to send the tank and APC to the highway and "keep one infantry squad at home" (`skirmish.s002.brief`). The HUD does not mark which squad that is.

## What ARIA is shown

- ARIA's touch planner treats a home threat only as a **presented minimap contact** within `AriaPublicHomeThreatPolicy` radius of the labelled home (`MatchHudAssistantUiSystemHelper.SkirmishWatch.cs:801-826`). Its "slipping hostile" intercept also requires fog visibility (`Runtime/Skirmish/SkirmishAriaPublicProjection.cs:366-422`, visibility check at `:400`). So ARIA sees no more than the player does.
- **Possible hidden-information channel (for the ARIA owner, not changed here).** The assistant threat row uses the damage source's name and world position without a fog check (`AssistantThreatReadModelSystem.cs:156-162, 187`). ARIA's text can therefore name a shooter the player cannot see. The position is not shown, but it is stored on the row.

## Time and touches to land a defensive order

- **Time.** About 28 s from the first hit to the fall. The only early cue (ARIA's voice line) arrives about 16 s in, which leaves about 12 s. The tank needs about 67 s to arrive, and nothing reports it en route unless the player scouts its route ("scout the south sweep").
- **Touches with the current HUD, for a first-time player who has noticed the number dropping.**
  1. Tap `YOUR BASE`. This frames the Barracks: camera only, S002 height 100, +30 x offset (`Assets/Game/Scripts/Composition/SkirmishPresentationSystem.cs:75`, `Focus` at `:169-240`).
  2. Find the home squad's card in the tray. The tray shows 5 cards per page (`UiExpandedSquadPage` Group0..4), and nothing marks the home squad. Paging may take one or more extra taps (`MatchHudSquadTrayView.Pagination.cs:389-403`).
  3. Tap the card to select it.
  4. Tap Attack, then tap the tank. **If the tank is hidden there is nothing to tap.** The fallback is Move plus a ground tap near the base: 2 taps without a target.
- **Minimum 4 taps after noticing, 5 or more with paging.** The decisive gap is not tap count. It is **(a)** no direction to the shooter and **(b)** no way to tell which of about 20 listed infantry is the home squad. A third open question is whether the home infantry can hurt a 420 HP tank inside the window. The garrison includes 4 Rocketeers (log:2107), so this belongs to the owner's "if the warning and order work and the reserve still cannot intervene" tuning step. It is not a HUD question.

## Implications for the pending base-under-fire HUD change

- Trigger on the first drop in the player's **own** base health. This is public and already in `UiSkirmishModel.PlayerHealth`.
- Point the warning **toward the attacker only when it is fog-visible**. Otherwise point toward the base. This matches the owner's direction and closes no gap the fog intends.
- One tap reuses `UiSkirmishAction.FocusPlayer`, which is camera only and issues no unit orders.
- The "easy to reach" defender must come from **own** group positions only. Highlight its card; do not auto-select it.
- If fog proves the tank was hidden at its firing position, the HUD alone cannot give the player a shot to aim at. That pushes the decision toward the owner's guard-tower or vision option, which the orchestrator is asking the owner about.

## Outcome (2026-11-18)

The orchestrator chose legibility first. It is implemented on `missions/hud-base-threat` in `080fca838` and `d9e5beaad`.

- **Base vision.** Both factions' designated bases see out to `SkirmishBaseVisionPolicy.RevealRadius`: 50 m (Battle Tank `attackRange`, `Unit_Veh_Tank_USA.prefab`), plus 1 cell for the tank's 3×3 combat radius, plus the base's own combat radius, plus 0.25 cell. This is the same test `UnitAttackSystem.cs:267-275` uses to decide that a tank can fire, so a tank that can hit the base is always visible.
- **Lead's 66 m figure.** The 66 m in the lead's brief is the tank's *staging* Move destination (log:2346). It **fired from 26.5 m** (log:2463 `cell=(1001,277)` against the base at (1001, 303.5)). Both are covered.
- **Alert perimeter.** The perimeter is the same radius, with 15 m exit hysteresis and a 10 s re-arm. Front-line contact cannot raise it. The bases are about 712 m apart on S002 (log:2107), and only a hostile already within firing reach of the HQ crosses it. Because the perimeter equals the reveal radius, the alert never reports a hidden unit.
