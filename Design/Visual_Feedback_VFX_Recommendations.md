# WarlineCapture Visual Feedback And VFX Recommendations

Date: 2026-05-06

## Purpose

This note recommends where WarlineCapture should add visual feedback, UI motion, and gameplay VFX to make the game feel responsive and professional. It is based on the current UI/UX specs, economy/reward design, combat catalog, audio design, visual-lock targets, and current project assets.

Map-view feedback follows `3D_SingleMap_Gameplay_Direction.md`: planning, briefing, minimap, deployment, threat, and battle views are UI/camera states over one 3D operation map. Feedback should bind to runtime world markers, command feedback, build footprints, combat VFX, objective anchors, and threat anchors over that same world.

The goal is not to add decoration everywhere. Feedback should make a player understand:

- The tap was accepted, rejected, or needs more information.
- A reward, resource, unlock, or mission result changed account state.
- A tactical command was issued, blocked, completed, or failed.
- A unit, base, district, objective, or timer needs attention.
- A popup, drawer, or screen now owns input.

## Existing Project Anchors

- UI visual style: dark graphite panels, cyan edge highlights, orange/gold CTA and reward accents, red warning accents, Oxanium typography.
- Main HUD surfaces: `SCN-08 RTS Battle HUD`, `PREFAB-02 Squad Tray`, `PREFAB-03 Build Drawer`, `SCN-10 Unit Command Wheel`.
- Popup surfaces: `POP-01 Threat Alert`, `POP-04 Reward Unlock`, `POP-05 Mission Result`, `POP-06 End of Day Report`, `POP-08 Intel Reveal`, `POP-09 Ability / Upgrade Detail`.
- Feedback contract: every visible UI element needs a feedback state in `UIUX_Gameplay_Element_Alignment.md`.
- Audio companion: use the event ids and assets in `Audio_Design_Guidelines.md`.
- Existing usable audio assets include `Assets/Game/Audio/UI`, `Assets/Game/Audio/Alerts`, and `Assets/Game/Audio/Gameplay`.
- Existing usable world FX include `Assets/PolygonMilitary/Prefabs/FX` smoke/fire/explosion prefabs until final 3D operation-map FX are produced.
- 3D operation-map production should add final stylized world and screen-space effects under the active game-art VFX folders, with ids referenced from visual config or operation-map metadata.
- Operation-map feedback anchors must resolve from runtime entities or `OperationMapDefinition` metadata. Do not place move/attack/build/objective VFX by reading pixels from preview art or by using a separate strategic-map image.

## Priority System

| Priority | Meaning |
|---|---|
| P0 | Needed for basic responsiveness and player comprehension. Add before broader content expansion. |
| P1 | Strong polish for reward, combat, mission, and progression loops. Add after the shared feedback layer exists. |
| P2 | Nice-to-have personality and premium feel once core loops are stable. |

## Shared UI Feedback Primitives

Create these as reusable Canvas helpers before per-screen polish:

| Primitive | Use | Motion / VFX | Assets |
|---|---|---|---|
| Button accepted pulse | Any accepted CTA, card, tab, command segment. | 60-100 ms press scale to 0.96, release to 1.03, settle to 1.0. Brief cyan/gold edge pulse based on button type. | Existing button sprites in `Assets/Game/Art/UI/Generated`, audio `ui_button_primary_click_01.wav`, `ui_button_secondary_click_01.wav`. |
| Locked/disabled wiggle | Locked cards, locked soldiers, unavailable support slots, invalid launch/build buttons. | 180-260 ms horizontal shake, lock icon flash, short red/orange reason tooltip or inline message. Respect reduced-motion by replacing shake with flash. | Lock badge sprites from UI kit; audio `ui_button_disabled_tap_01.wav` or `ui_card_locked_01.wav` when produced. |
| Selection frame | Selected mission node, squad card, command segment, district, unit card. | Cyan outline turns on immediately, 0.3 s soft pulse, optional tiny corner brackets. | Generated UI frames in `Assets/Game/Art/UI/Generated/*`; audio `ui_card_select_01.wav`. |
| Invalid action flash | Invalid command, invalid target, insufficient resources, invalid placement. | Red border flash, small error chip near source, blocked icon above target if world-facing. | UI warning frame/icon; audio `game_command_invalid_01.wav`, `game_resource_shortage_01.wav`, `game_build_invalid_placement_01.wav`. |
| Toast / reason chip | Explains why something failed or what changed. | Slide/fade from source edge, hold 1.4-2.0 s, fade out. | Shared `FeedbackToastView` prefab; icon from canonical resource/action type. |
| Resource flyout | Rewards, spends, refunds, resource gains. | Icon/value clones fly from source tile/card to matching header/HUD counter; counter bumps and increments. | Resource icons from reward/economy UI. New trail/glow sprite if needed. |
| Meter delta sweep | XP, health, trust, threat, intel confidence, build queue, star progress. | Old value marker stays briefly, fill animates to new value, positive/negative delta tag rises/fades. | Meter fills already used in UI prefabs; audio count tick for rewards. |
| Modal open/close | All blocking popups. | Scrim fade 0 to 70%, frame slides or scales in, content staggers 40-80 ms. Close reverses quickly. | Popup frame sprites; audio `ui_popup_open_01.wav`, `ui_popup_close_01.wav`. |
| Drawer open/close | Build drawer, command feed, inbox, side panels. | Slide from owning edge with slight overshoot; background HUD remains visible but dimmed if input is blocked. | Drawer frame sprites; audio `ui_drawer_open_01.wav`, `ui_drawer_close_01.wav`. |

## Shared Audio Pairing

Use audio as the confirmation layer for the visual feedback above. Visuals remain the source of truth for accessibility; audio reinforces the moment and should never be the only way to understand a state change.

| Feedback Moment | Recommended Event Id | Asset | Bus | Priority | Playback Rule |
|---|---|---|---|---|---|
| Accepted primary action | `UI.Button.Primary.Click` | `ui_button_primary_click_01.wav` | UI | Medium | Play on release after the action validates. |
| Accepted secondary action | `UI.Button.Secondary.Click` | `ui_button_secondary_click_01.wav` | UI | Medium | Use for normal non-primary buttons and navigation icons. |
| Cancel/back/destructive navigation | `UI.Button.Negative.Click` or `UI.Screen.Back` | `ui_button_negative_click_01.wav`, `ui_screen_back_01.wav` | UI | Medium | Use negative click for cancel/destructive choices; screen back for route transition. |
| Locked soldier/card tapped | `UI.Card.Locked` | `ui_card_locked_01.wav` | UI | Medium | If asset is missing, temporarily use `ui_button_disabled_tap_01.wav`. Pair with lock wiggle/reason chip. |
| Disabled button tapped | `UI.Button.Disabled.Tap` | `ui_button_disabled_tap_01.wav` | UI | Medium | Cooldown 450 ms per source. Pair with inline validation. |
| Validation reason chip appears | `UI.Feedback.Toast.Error` | `ui_feedback_toast_error_01.wav` | UI | Medium | Short rejected data tick. Use only when a new reason appears. |
| Positive toast appears | `UI.Feedback.Toast.Positive` | `ui_feedback_toast_positive_01.wav` | UI | Low | Soft data confirmation for non-critical success. |
| Resource flyout starts | `UI.Resource.Flyout.Start` | `ui_resource_flyout_start_01.wav` | UI | Low | Optional, only for meaningful rewards/spends. |
| Resource flyout lands on counter | `UI.Resource.Flyout.Arrive` | `ui_resource_flyout_arrive_01.wav` | UI | Medium | Counter bump and value update should happen on arrival. |
| Resource spend accepted | `UI.Resource.Spend` | `ui_resource_spend_01.wav` | UI | Medium | Drier/downward than reward arrival. Use for Operation action costs and purchases. |
| XP/currency count-up | `UI.Reward.CountTick` | `ui_reward_count_tick_01.wav` | UI | Low | Rate-limited. Pitch may rise for short reward counts only. |
| Meter positive delta | `UI.Meter.PositiveDelta` | `ui_meter_positive_delta_01.wav` | UI | Medium | Use for trust, security, intel, XP, readiness, and objective progress milestones. |
| Meter negative delta | `UI.Meter.NegativeDelta` | `ui_meter_negative_delta_01.wav` | UI/Alerts | Medium/High | Use UI bus for normal deltas, Alerts bus for critical threat/heat drops. |
| Standard popup opens/closes | `UI.Popup.Open` / `UI.Popup.Close` | `ui_popup_open_01.wav`, `ui_popup_close_01.wav` | UI | Medium | Screen-specific popups may override with darker/brighter variants. |
| Reward popup opens | `Reward.Popup.Open` | `ui_reward_popup_open_01.wav` | UI | High | Use for POP-04 and major profile/track claims. |
| Major unlock | `Reward.Unlock.Major` | `game_reward_unlock_major_01.wav` | SFX | High | 1.5-2.5 s stinger, but skippable/duckable if player taps continue. |
| Drawer opens/closes | `UI.Drawer.Open` / `UI.Drawer.Close` | `ui_drawer_open_01.wav`, `ui_drawer_close_01.wav` | UI | Medium | Matches Build Drawer, command feed, inbox, and side panels. |
| Command wheel segment focus | `UI.CommandWheel.SegmentFocus` | `ui_command_wheel_segment_focus_01.wav` | UI | Low | Cooldown 100 ms while dragging. |
| Invalid command/target | `Gameplay.Command.Invalid` | `game_command_invalid_01.wav` | SFX | High | Cooldown 450 ms. Stronger than disabled UI tap. |
| Resource shortage | `Gameplay.Resource.Shortage` | `game_resource_shortage_01.wav` | SFX | High | Only after a player action fails from shortage. |
| Unit under attack | `Alert.Unit.UnderAttack` | `alert_unit_under_attack_01.wav` | Alerts | High | Cooldown by squad, 8 s. Pair with red directional/vignette feedback. |
| Base breached / critical damage | `Alert.Base.Breached` or `Combat.Building.Damaged.Critical` | `alert_base_breached_01.wav`, `alert_building_damaged_critical_01.wav` | Alerts | Critical | Duck Music/Ambience briefly; cooldown by threat/building. |

## Recommended Feedback Matrix

| Priority | Surface / Moment | Trigger | Recommended Visual Feedback | Assets / IDs | Notes |
|---|---|---|---|---|---|
| P0 | Global buttons | Button press accepted. | Button depress/release pulse, edge highlight, optional ripple clipped to button. | `UI.Button.Primary.Click`, `UI.Button.Secondary.Click`; generated UI button frames. | This should be automatic through one shared button feedback component. |
| P0 | Locked soldier/unit/support card | Player taps a locked unit, soldier, gear, or support ability in Loadout, Armory, Build Drawer, or Reward Detail. | Wiggle the card, flash lock badge, show exact unlock reason chip. | `UI.Button.Disabled.Tap`; lock icon, unit portrait from visual config. | Use the user's soldier example here. Never leave tap silent. |
| P0 | Disabled primary CTA | Deploy/Launch/Confirm tapped while invalid. | CTA wiggle, red validation line appears above button, missing requirement chips pulse. | `UI.Button.Disabled.Tap`, validation icon. | Examples: not enough Fuel, invalid loadout, missing scenario setup. |
| P0 | Popup open/close | Any modal opens or closes. | Scrim fade; frame slides/scales in; header snaps first, body rows stagger. Close reverses. | `UI.Popup.Open`, `UI.Popup.Close`; popup frame sprites. | POP-01 can slam/slide faster; reward popups can scale up softer. |
| P0 | Build Drawer | Build button opens/closes drawer. | Drawer slides from right/bottom-right; category tabs fade in; queue rows stagger. | `UI.Drawer.Open`, `UI.Drawer.Close`; `PREFAB-03` frames. | Matches current visual-lock drawer expectation. |
| P0 | Command Wheel | Special/command wheel opens, segment selected, disabled segment tapped. | Radial unfold, segment hover/focus glow, selected segment expands 4-6%, disabled segment red flash. | `UI.CommandWheel.Open`, `UI.CommandWheel.SegmentFocus`, `Gameplay.Command.Invalid`. | Use data-driven availability from selected unit capability set. |
| P0 | Squad selection | Player taps squad card or selects unit group. | Squad card cyan outline, portrait bump, world selection ring appears around units, camera focus optional. | `Gameplay.Unit.Select.*`; `PREFAB-02` card frames. | Keeps HUD and battlefield state synchronized. |
| P0 | Valid command issued | Move, Attack, Hold, Stop, Breach, Load/Unload accepted. | Command button pulse, world target marker, short path/target line, selected units briefly ping. | `Gameplay.Command.Move.Confirm`, `Gameplay.Command.Attack.Confirm`, `Gameplay.Command.Breach.Confirm`. | For move: cyan path pips. For attack/breach: orange/red target reticle. |
| P0 | Invalid command target | Player taps impossible move/attack/build target. | Red X target marker at tap point, command segment/button flash, reason chip. | `Gameplay.Command.Invalid`, `Gameplay.Build.InvalidPlacement`. | Cooldown-limit to prevent spam. |
| P0 | Unit under attack | Friendly selected/important squad takes meaningful damage. | Squad card HP flash, red edge vignette toward direction, world hit marker over squad. | `Alert.Unit.UnderAttack`, light impact VFX. | Use red screen only for friendly/base damage, not every enemy hit. |
| P0 | Base breached / critical structure attacked | Enemy breaches base or owned building enters critical health. | Strong red directional screen flash, threat feed row expands, camera shake 0.1-0.2 s, building marker pulses. | `Alert.Base.Breached`, `alert_building_damaged_critical_01.wav`, smoke/fire FX. | Critical feedback; must also show text/icon for accessibility. |
| P0 | Threat alert | Threat detected by warning system. | Threat feed row pulses, optional POP-01 slide-in, route line on minimap, Jump CTA pulse. | `POP-01`, `Alert.Threat.*`, red/orange warning frames. | Non-critical threats should not block simulation. Critical threats may use modal. |
| P0 | Objective update/complete/fail | Objective progress meaningfully changes. | Objective row tick, progress fill sweep, checkmark stamp on complete, red cross/fail stamp on failure. | `Gameplay.Objective.Update`, `Gameplay.Objective.Complete`, `Gameplay.Objective.Failed`. | Avoid animating every numeric increment. |
| P0 | Resource shortage | Build/produce/deploy fails from resource shortage. | Missing resource counter shakes and flashes red; cost row highlights missing amount. | `Gameplay.Resource.Shortage`; resource icons. | More useful than only flashing the button. |
| P0 | Build placement | Placement mode starts, ghost moves, confirm succeeds/fails. | Valid operation-map socket/pad cyan-green outline, invalid socket red overlay, footprint ghost opacity changes, confirm spawns construction puff. | `Gameplay.Build.PlacementStart`, `Gameplay.Build.PlaceConfirm`, `Gameplay.Build.InvalidPlacement`; `FX_Smoke_Small_01`. | Operation-map metadata sockets should be the gameplay anchor. |
| P1 | Reward grant | Mission result, reward claim, level-up, first-clear. | Reward tile reveal, amount count-up, then eligible icons fly to their owning persistent counter or inventory destination; counter bumps on arrival. | `POP-04`, `POP-05`, `Reward.Item.Reveal`, `UI.Reward.CountTick`; reward icons. | Use for Credits, Command, Rush Tickets, XP, unlocks, and inventory items. Match Materials/Fuel/Oil deltas use battlefield resource feedback and never fly to account counters. |
| P1 | Major unlock | New unit/building/support/gear/cosmetic unlock. | Large item pedestal reveal, gold/cyan burst, unlock card flips in, item silhouette resolves to portrait/icon. | `Reward.Unlock.Major`; unit/building/support art from visual config. | POP-04 should own this, not Mission Result directly. |
| P1 | XP / level up | Commander XP changes or level threshold crossed. | XP bar fills with tick marks; level badge glows and bumps on threshold; optional POP-04 for milestone reward. | `UI.Progress.XP.Tick`, `Reward.Popup.Open`. | Keep count-up fast; mobile players should not wait too long. |
| P1 | Mission result stars | POP-05 opens and stars are awarded. | Stars reveal one by one, objective checklist rows stamp in, rewards appear after stars. | `Mission.Result.StarReveal`, `Mission.Result.StatReveal`, `Reward.Item.Reveal`. | Sequence should explain why rewards happened. |
| P1 | Production queued/complete | Unit or building production starts/completes. | Queue row slides in, progress bar starts, completed row glows and ejects ready badge; HUD notification ping. | `Gameplay.Production.QueueUnit`, `Gameplay.Production.Complete`. | Aggregate multiple completions to one notification. |
| P1 | Building damaged/destroyed | Building changes damage state or is destroyed. | Swap intact/damaged/destroyed visual state; add smoke/fire/scorch overlay; minimap marker flashes. | PolygonMilitary destroyed prefabs, `FX_Smoke_*`, `FX_Fire_*`, `Combat.Building.*`. | Gameplay docs require damage/destruction overlays, not terrain art changes. |
| P1 | Enemy hit/destroyed | Visible important enemy receives hit or dies. | Small hit spark/smoke puff, HP bar flash, destroyed enemy marker fades. | `Combat.Impact.Light`, `Combat.Explosion.Small`, existing FX prefabs. | Density-gate large battles. Do not overplay for every bullet. |
| P1 | Friendly unit death | Friendly selected/important unit dies. | Squad card portrait desaturates, count drops with red flash, world marker fades with smoke. | `Combat.Unit.Destroyed.Friendly`. | Higher priority than enemy deaths. |
| P1 | Ability cooldown/charges | Support ability used, unavailable, or comes off cooldown. | Button radial cooldown wipe, charge chip decrements, ready pulse when available. | Ability icons and `vfxCueId`/`audioCueId` from visual config. | Critical for command readability. |
| P1 | Intel scan/reveal | Drone scan starts/completes, POP-08 opens, confidence increases. | Map scan sweep, evidence cards decrypt/reveal, confidence meter sweep, archive badge pulse. | `Intel.Scan.Start`, `Intel.Evidence.Reveal`, `Intel.Confidence.Increase`; `POP-08`. | Good place for cyan radar/data VFX. |
| P1 | Operation action accepted | Patrol, Scan, Aid, Raid, Repair, Evacuate, Build Outpost accepted. | Action card locks in, resource cost flies out/down, district meter delta preview animates, feed row appears. | Operation action audio IDs; district/action icons. | Strategic feedback should show consequences immediately. |
| P1 | End of day report | Operation day resolves. | Rows stagger, positive deltas sweep green/cyan, negative threat/heat deltas pulse amber/red, resources count. | `POP-06`, `Operation.Report.PositiveDelta`, `Operation.Report.NegativeDelta`. | Group deltas by district to avoid noise. |
| P1 | Settings controls | Slider/toggle/dropdown/tab changed. | Toggle knob slide, slider tick, selected tab underline slide, dropdown expands/collapses. | Existing UI audio assets. | Also validates accessibility/reduced motion settings. |
| P2 | Main Menu mode cards | Mode card selected or unavailable. | Parallax key art shift, card select glow, locked mode badge wiggle. | `UI.Card.ModeSelect`, `UI.Card.Locked`. | Keep subtle; menu should feel premium, not busy. |
| P2 | Saga mission node | Node selected, locked, completed, reward claimable. | Selected node ring pulse, locked node shake, completed star twinkle, claimable chapter reward glow. | `UI.Card.MissionNode.Select`, `ui_card_locked_01.wav` when produced. | Use gold only for completion/rewards. |
| P2 | Minimap interaction | Player taps minimap or threat jump. | Minimap ripple at tap, camera focus line, destination bracket. | `Alert.Threat.Jump`, map UI frames. | Useful after threat alerts and objective taps. |
| P2 | Screen transitions | Route forward/back between shell screens. | Forward slide/fade for deeper flow, back slide opposite, header resource strip persists. | `UI.Screen.Forward`, `UI.Screen.Back`. | Use short 180-280 ms transitions. |
| P2 | Notification badges | Inbox/event/feed badge appears or count changes. | Badge pop-in and tiny ping; no repeat while visible. | `UI.Notification.Minor`. | Prevents badge blindness without annoyance. |
| P2 | Ambient HUD life | Battle HUD idle state. | Very subtle cyan scanner line, objective panel heartbeat only under active objective timer. | Custom UI shader/sprite. | Disable under reduced motion and low-power mode. |

## Gameplay-Specific VFX Recommendations

Planning VFX are UI overlays, minimap pings, and command-table markers over the same operation map. Battle VFX are world overlays or entity effects. The same threat or objective can have both: a planning/minimap ping and a world marker after the camera jumps.

| Gameplay Event | Trigger Source | VFX | Asset Direction |
|---|---|---|---|
| Move command | `RTSSelectionSystem` accepts move target. | Cyan tap marker, dotted path pips, unit selection rings pulse once. | Lightweight world-space decals, screen-space overlays, or line-renderer pips; avoid heavy particles. |
| Attack command | `RTSSelectionSystem` accepts target. | Orange/red target bracket locks onto target, small line from selected group. | UI/world overlay reticle, not full-screen effect. |
| Breach command | `BaseBreachOrderSystem` accepts valid breach. | Charge icon on wall/gate, short warning blink, small explosion/smoke at breach. | Reuse `FX_Explosion_Large_Dark_01` and `FX_Smoke_Medium_01` until 2D iso VFX exist. |
| Drone/radar scan | `ThreatWarningRuntimeState` or support ability. | Expanding cyan radar ring, scan cone/sweep, detected marker resolves. | 3D operation-map scan decals or screen-space command overlay, with final ids referenced by visual config. |
| Precision/naval strike | Support ability accepted. | Targeting reticle countdown, impact flash, smoke column. | Use combat catalog `vfxCueId`; final art should be stylized top-down readable. |
| Unit damage | Health changes meaningfully. | Floating small red damage tick or HP flash for selected units only; hit puff on important impacts. | Avoid text spam. HP bar flash is usually enough on mobile. |
| Healing/repair | Field repair, casualty stabilize, repair convoy. | Cyan/green repair sparks, plus icon pulse, health/meter fill sweep. | Separate world VFX and UI meter feedback. |
| Resource gain | Significant tactical resource gain. | Small resource icon rises from building/objective and fades, plus HUD counter bump. | Do not emit every economy tick. |
| Building completion | Construction completes. | Construction smoke clears, building outline turns cyan, ready badge pops. | Reuse smoke FX. |
| Critical warning | Base breach, timer warning, mission failure risk. | Directional red vignette, HUD threat row expansion, minimap marker pulse. | Full-screen red must be brief and severity-gated. |

## Popup Motion Recommendations

| Popup | Open Motion | Internal Sequence | Close Motion |
|---|---|---|---|
| POP-01 Threat Alert | Fast slide/scale in with red header flash. | ETA/route rows appear first, strength meter fills, Jump CTA pulses once. | Fade/slide out; threat remains in feed. |
| POP-02 Confirm Raid | Heavy modal slide in with darker scrim. | Intel/collateral/civilian meters sweep, cost row highlights. | Cancel closes normally; Confirm plays dispatch feedback before route. |
| POP-03 Build Placement | Drawer/modal hybrid from build area. | Footprint appears, socket states pulse, rotate tick animates ghost. | Confirm spawns placement puff; cancel fades ghost. |
| POP-04 Reward Unlock | Soft scale from 0.92 to 1.0 with gold/cyan burst. | Main item reveal, reward icons stagger, Continue appears last. | Continue sends resource flyouts before closing when relevant. |
| POP-05 Mission Result | Result frame slides up; victory/defeat stinger. | Outcome, stars, stats, objectives, rewards, buttons in that order. | Continue applies/flyouts rewards, then route transition. |
| POP-06 End of Day | Report frame slides from bottom or center. | Day header, district deltas, resource summary, save status. | Continue closes after save-complete feedback. |
| POP-08 Intel Reveal | Decryption/data scan open. | Evidence cards reveal, confidence meter increases, archive/view CTA appears. | View Intel routes forward; Close keeps evidence badge/read state updated. |
| POP-09 Ability / Upgrade Detail | Focus zoom from selected card/ability. | Icon/title first, availability and requirements next, CTA state last. | Reverse to source card or fade when source is off-screen. |

## Asset Production Backlog

| Asset | Need | Suggested Path |
|---|---|---|
| `FeedbackToastView` prefab | Shared reason/error/success chips. | `Assets/Game/Prefabs/UI/Components/FeedbackToastView.prefab` |
| `ResourceFlyoutView` prefab | Reward/resource icon flight to HUD/header counters. | `Assets/Game/Prefabs/UI/Components/ResourceFlyoutView.prefab` |
| `UiMotionFeedback` component | Shared button/card/modal/drawer motion with reduced-motion support. | `Assets/Game/Scripts/UI/Components/UiMotionFeedback.cs` |
| `WorldFeedbackMarker` prefab | Move/attack/invalid/scan markers anchored in world space. | `Assets/Game/Prefabs/UI/Components/WorldFeedbackMarker.prefab` |
| 2D iso scan VFX | Radar/drone/intel reveals. | `Assets/Game/Art/Generated/2DISO/VFX/scan_*` |
| 2D iso hit/impact VFX | Readable mobile combat impacts. | `Assets/Game/Art/Generated/2DISO/VFX/impact_*` |
| 2D iso resource/reward icons | Consistent flyouts and counters. | `Assets/Game/Art/UI/Generated/RewardIcons` |
| Popup transition animator clips | Consistent modal motion. | `Assets/Game/Animations/UI/Popups` |
| Drawer/command wheel animator clips | Consistent tactical overlay motion. | `Assets/Game/Animations/UI` |

## Implementation Order Recommendation

1. Add shared UI feedback primitives: accepted pulse, locked wiggle, invalid flash, toast chip, modal open/close, drawer open/close.
2. Wire the primitives into `Button`, card, route, popup, drawer, and command-wheel controllers.
3. Add reward flyouts and counter bumps for Mission Result, Reward Unlock, Commander Profile reward claims, and Operation End of Day.
4. Add tactical world feedback markers for move, attack, invalid target, build placement, threat jump, and objective focus.
5. Add critical combat feedback: unit under attack, base breached, building critical/destroyed, mission timer warning.
6. Replace temporary PolygonMilitary effects with final 3D operation-map VFX as the production art pipeline matures.

## Accessibility And Performance Rules

- Add a reduced-motion setting. Replace shake, long flyouts, and looping pulses with brief fades/flashes.
- Critical feedback must never rely on color only. Pair red/green changes with icons, text, shape, or motion.
- Pool flyouts, world markers, hit effects, and toasts. Do not instantiate/destroy during combat spam.
- Cooldown repeated invalid, threat, and under-attack feedback to avoid fatigue.
- Keep UI motion short: most interactions should complete in 100-280 ms; reward sequences can be longer but skippable/accelerated.
- Density-gate combat VFX and audio by camera visibility, selected/friendly importance, and threat severity.
