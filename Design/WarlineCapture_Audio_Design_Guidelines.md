# WarlineCapture Audio Design Guidelines

Version 0.1 - 2026-05-04

## Purpose

This document defines the audio plan for WarlineCapture as a AAA mobile RTS. It is written so a human developer or AI implementation agent can determine which audio event to play, when to play it, where it belongs in the UI or gameplay flow, what file should exist, and how the file can be generated.

Primary source documents:

- `Design/WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_Gameplay_Features_Detailed_Spec.md`
- `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- `Design/GAME_DESIGN_REFERENCE.md`

WarlineCapture is a mobile-first, grid-based military/civilian RTS with Campaign, Operations, and Skirmish modes. Audio must support tactical clarity, responsiveness, and premium polish without overwhelming phone speakers or masking important player feedback.

## Audio Direction

WarlineCapture should sound like a clean near-future command interface operating over a grounded 3D military/civilian operation map. The audio identity is tactical, controlled, modern, and readable.

Core pillars:

- **Tactical clarity:** The player must instantly understand critical warnings, successful actions, invalid actions, and objective changes.
- **Premium restraint:** UI sounds should be short, crisp, and lightly mechanical. Avoid arcade bleeps, cartoon impacts, harsh alarms, and long confirmation tails.
- **Readable combat:** Combat sound should communicate scale and threat without becoming a constant wall of gunfire on mobile speakers.
- **Layered urgency:** Music, ambience, UI, and warnings should escalate by game state instead of always sounding intense.
- **Accessibility first:** Every critical audio cue must have a visual equivalent. Audio can reinforce gameplay, but it must not be the only source of information.

## 3D Operation-Map Audio Rules

Audio must follow the 3D single-map direction in `WarlineCapture_3D_SingleMap_Gameplay_Direction.md`. Planning, briefing, minimap, deployment, threat, and battle views are UI/camera states over one operation map.

| Map Context | Audio Role | Examples |
|---|---|---|
| Planning / briefing / minimap | Planning, mission context, district pressure, route preview, menu readability. | Campaign node select, mission briefing preview, Operations district warning, minimap route ping. |
| Battle / deployment camera | Immediate command feedback, combat readability, build placement, objective changes, threat response. | Unit select, move confirm, attack confirm, invalid target, build valid/invalid, objective complete, unit under attack. |

Rules:

- Planning and briefing audio should stay restrained and UI-led; it should not sound like combat is already happening.
- Battle camera audio may use world-positioned or camera-relative cues when the event has a runtime entity or metadata anchor.
- Minimap, threat, and objective jumps should play a short focus cue only when the camera actually moves inside operation-map bounds.
- ARIA tutorial voice or sound cues must pair with visible highlights and must not be the only way to find an operation-map target.

## Audio Buses

Use these mixer buses from the beginning, even before the final mixer is built.

| Bus | Purpose | Settings control | Notes |
|---|---|---|---|
| `Master` | Final output | Master Volume | Parent of all buses. |
| `Music` | Menu, strategic, and combat music | Music Volume | Supports snapshots and intensity layers. |
| `Ambience` | City, base, battlefield beds | SFX Volume | Low priority. Ducks under warnings and voice. |
| `SFX` | Gameplay one-shots and loops | SFX Volume | Weapons, vehicles, buildings, production. |
| `UI` | Interface feedback | SFX Volume | Buttons, tabs, sliders, cards, popups. |
| `Voice` | Tutorial VO, commander barks, radio calls | Voice Volume | Highest intelligibility priority after critical alerts. |
| `Alerts` | Threat and failure warnings | SFX Volume | High-priority child of SFX or dedicated bus. Can duck Music/Ambience. |

Default relative mix targets:

| Bus | Default level |
|---|---:|
| Music | -12 dB |
| Ambience | -18 dB |
| SFX | -8 dB |
| UI | -10 dB |
| Voice | -6 dB |
| Alerts | -5 dB |

## Playback Rules

Global playback rules:

- All event ids use PascalCase namespaces: `UI.Button.Primary.Click`, `Gameplay.Objective.Complete`.
- All generated asset files use lowercase snake case.
- UI one-shots should usually be 40-350 ms.
- Critical alerts may be 700-1800 ms, but must be cooldown-limited.
- Tutorial VO must duck Music and Ambience by 4-7 dB while speaking.
- Threat, objective, and invalid-command sounds must not be masked by UI click sounds.
- Identical one-shot events must use a cooldown or variation rotation to prevent repetition fatigue.
- Mobile builds should prefer mono for UI and most gameplay one-shots, stereo for music/ambience, and short Vorbis/ADPCM clips for memory-sensitive categories.
- Do not play hover sounds on touch devices. Use press, release, drag, and selection sounds instead.

Priority levels:

| Priority | Meaning | Examples |
|---|---|---|
| `Critical` | Must be heard unless muted. Can duck music. | incoming threat, base breached, mission failed |
| `High` | Important state change. | objective complete, unit under attack, production complete |
| `Medium` | Normal action feedback. | button click, select unit, open drawer |
| `Low` | Flavor or ambience. | city bed, distant aircraft, map shimmer |

Cooldown defaults:

| Event family | Cooldown |
|---|---:|
| Button clicks | 40 ms |
| Slider ticks | 80 ms |
| Invalid command | 450 ms |
| Disabled/locked UI reject | 450 ms per source |
| Toast/reason chip | 1.0 s per message key |
| Resource flyout arrival | Aggregate by resource within 250 ms |
| Meter delta tick | 250 ms per meter |
| Unit selected | 150 ms |
| Production queued | 250 ms |
| Threat feed non-critical | 2.0 s |
| Critical alert | 6.0 s per threat type |
| Combat hit one-shots | distance/importance gated |

## Asset Naming

Use this format:

`<domain>_<category>_<description>_<variation>.wav`

Examples:

- `ui_button_primary_click_01.wav`
- `ui_button_negative_click_01.wav`
- `ui_popup_threat_open_01.wav`
- `ui_slider_tick_01.wav`
- `game_unit_select_infantry_01.wav`
- `game_command_move_confirm_01.wav`
- `game_objective_complete_01.wav`
- `alert_threat_ground_detected_01.wav`
- `music_menu_loop_01.wav`
- `amb_city_day_loop_01.wav`
- `vo_tutorial_move_units_01.wav`

Recommended folder layout:

```text
Assets/Game/Audio/
  Mixers/
  Events/
  Music/
  UI/
  Alerts/
  Gameplay/
  Ambience/
  Voice/
  GeneratedSource/
```

`GeneratedSource` is optional and can store prompts, tool exports, stems, and uncompressed masters. Unity-imported runtime files should live in the category folders.

## Universal UI Audio Library

These are shared across all screens unless a screen-specific override is listed.

| Event id | Trigger | Asset | Bus | Priority | Playback |
|---|---|---|---|---|---|
| `UI.Button.Primary.Click` | Primary CTA press/release success | `ui_button_primary_click_01.wav` | UI | Medium | One-shot on release if action is accepted. |
| `UI.Button.Secondary.Click` | Secondary button accepted | `ui_button_secondary_click_01.wav` | UI | Medium | Slightly softer than primary. |
| `UI.Button.Negative.Click` | Cancel, close, back, exit, destructive action | `ui_button_negative_click_01.wav` | UI | Medium | Darker, shorter, no alarm tone. |
| `UI.Button.Disabled.Tap` | Disabled button or locked card tapped | `ui_button_disabled_tap_01.wav` | UI | Medium | Low dry thud plus tiny digital reject. |
| `UI.Toggle.On` | Toggle switches on | `ui_toggle_on_01.wav` | UI | Medium | Short upward mechanical tick. |
| `UI.Toggle.Off` | Toggle switches off | `ui_toggle_off_01.wav` | UI | Medium | Short downward mechanical tick. |
| `UI.Slider.Tick` | Slider value changes by visible step | `ui_slider_tick_01.wav` | UI | Low | Cooldown 80 ms. |
| `UI.Tab.Select` | Tab changes visible panel | `ui_tab_select_01.wav` | UI | Medium | Short metallic data switch. |
| `UI.Dropdown.Open` | Dropdown expands | `ui_dropdown_open_01.wav` | UI | Medium | Compact unfolding sound. |
| `UI.Dropdown.Select` | Dropdown item selected | `ui_dropdown_select_01.wav` | UI | Medium | Lighter than primary click. |
| `UI.Card.Select` | Mode card, mission node, unit card, district card selected | `ui_card_select_01.wav` | UI | Medium | Confident but not loud. |
| `UI.Card.Locked` | Locked card tapped | `ui_card_locked_01.wav` | UI | Medium | Same family as disabled tap, with lock layer. |
| `UI.Popup.Open` | Standard modal opens | `ui_popup_open_01.wav` | UI | Medium | Short pneumatic frame-in. |
| `UI.Popup.Close` | Standard modal closes | `ui_popup_close_01.wav` | UI | Medium | Short frame-out. |
| `UI.Screen.Forward` | Navigate deeper into a flow | `ui_screen_forward_01.wav` | UI | Medium | Brief tactical sweep. |
| `UI.Screen.Back` | Navigate back/up | `ui_screen_back_01.wav` | UI | Medium | Short descending sweep. |
| `UI.Reward.CountTick` | XP/currency reward count-up tick | `ui_reward_count_tick_01.wav` | UI | Low | Rate-limited; pitch may rise. |
| `UI.Notification.Minor` | Non-critical inbox/event/social badge appears | `ui_notification_minor_01.wav` | UI | Low | Soft data ping. |

## Shared Visual Feedback Audio Layer

These cues support the reusable feedback primitives in `WarlineCapture_Visual_Feedback_VFX_Recommendations.md`. Prefer these shared ids for common feedback so screens do not invent one-off sounds for the same interaction.

| Event id | Trigger | Asset | Bus | Priority | Playback |
|---|---|---|---|---|---|
| `UI.Feedback.Toast.Error` | A validation/error reason chip appears | `ui_feedback_toast_error_01.wav` | UI | Medium | Short rejected data tick. Cooldown 1.0 s per message key. |
| `UI.Feedback.Toast.Positive` | A non-critical success/info chip appears | `ui_feedback_toast_positive_01.wav` | UI | Low | Soft confirmation; do not use for major rewards. |
| `UI.Resource.Flyout.Start` | Reward/resource icon begins flying from source to counter | `ui_resource_flyout_start_01.wav` | UI | Low | Optional; suppress when many flyouts start together. |
| `UI.Resource.Flyout.Arrive` | Reward/resource icon lands on header/HUD counter | `ui_resource_flyout_arrive_01.wav` | UI | Medium | Play when the visible counter bumps and value updates. Aggregate by resource within 250 ms. |
| `UI.Resource.Spend` | Resource spend is accepted and the counter decreases | `ui_resource_spend_01.wav` | UI | Medium | Drier/downward cue for Operation actions, build costs, purchases, and deploy costs. |
| `UI.Resource.Refund` | Spend is canceled/refunded before commitment | `ui_resource_refund_01.wav` | UI | Low | Light reverse cue; use only when a visible counter changes. |
| `UI.Meter.PositiveDelta` | Meter increases at a meaningful threshold | `ui_meter_positive_delta_01.wav` | UI | Medium | Trust, security, intel confidence, XP, readiness, objective progress. Not per frame. |
| `UI.Meter.NegativeDelta` | Meter decreases at a meaningful threshold | `ui_meter_negative_delta_01.wav` | UI | Medium | Use Alerts bus only when the decrease is critical. |
| `UI.Selection.FocusPulse` | A selected card/world-linked HUD element receives focus | `ui_selection_focus_pulse_01.wav` | UI | Low | Optional. Use sparingly for focus changes that are not taps. |
| `UI.Feedback.ReducedMotionFlash` | Reduced-motion substitute for shake/flyout emphasis | `ui_feedback_reduced_motion_flash_01.wav` | UI | Low | Same semantic cue as the original feedback, shorter and quieter. |

Asset production note: these assets may be generated after first implementation. Until then, `UI.Feedback.Toast.Error` can temporarily reuse `ui_button_disabled_tap_01.wav`, `UI.Resource.Flyout.Arrive` can reuse `ui_reward_count_tick_01.wav`, and `UI.Meter.PositiveDelta` can reuse `ui_tab_select_01.wav` at lower volume.

## Screen Audio Matrix

### SCN-01 - Splash / Loading

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene enter | `Music.Splash.Start` | `music_splash_intro_01.wav` | Music | Low | Play once, 2-4 s, then transition to menu loop. |
| Loading screen visible | `Ambience.Base.DistantLoop` | `amb_base_distant_loop_01.wav` | Ambience | Low | Loop quietly until route. |
| Progress bar reaches 25/50/75 percent | `UI.Loading.ProgressMilestone` | `ui_loading_progress_milestone_01.wav` | UI | Low | Optional, very subtle. Do not play per-percent. |
| Assets ready / continue appears | `UI.Loading.Ready` | `ui_loading_ready_01.wav` | UI | Medium | One-shot. |
| Auto-route to Main Menu | `UI.Screen.Forward` | `ui_screen_forward_01.wav` | UI | Medium | Play during transition fade. |

### SCN-02 - Main Menu / Mode Select

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene enter | `Music.Menu.Loop` | `music_menu_loop_01.wav` | Music | Low | Seamless loop, 60-120 s. |
| Mode card selected | `UI.Card.ModeSelect` | `ui_card_mode_select_01.wav` | UI | Medium | Stronger than generic card select. |
| Saga/Persistent/Quick card opens next screen | `UI.Screen.Forward` | `ui_screen_forward_01.wav` | UI | Medium | Follow selection sound by 80-120 ms if transition is animated. |
| Profile, Inbox, Store, Events, Ranking icons | `UI.Button.Secondary.Click` | `ui_button_secondary_click_01.wav` | UI | Medium | One-shot. |
| Resource plus button | `UI.Button.Resource.Click` | `ui_button_resource_click_01.wav` | UI | Medium | Slight currency shimmer, no casino feel. |
| Gear button | `UI.Button.Settings.Click` | `ui_button_settings_click_01.wav` | UI | Medium | Can reuse secondary click if asset budget is tight. |
| Live event badge appears | `UI.Notification.Minor` | `ui_notification_minor_01.wav` | UI | Low | Do not repeat while badge is already visible. |

### SCN-03 - Commander Profile

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene enter | `Music.Profile.Layer` | `music_profile_layer_01.wav` | Music | Low | Optional menu music layer, not a full new track. |
| Back arrow | `UI.Screen.Back` | `ui_screen_back_01.wav` | UI | Medium | One-shot. |
| Profile tabs | `UI.Tab.Select` | `ui_tab_select_01.wav` | UI | Medium | One-shot. |
| Reward track node selected | `UI.Card.RewardNode.Select` | `ui_card_reward_node_select_01.wav` | UI | Medium | Slight reward accent. |
| Claimable reward node tapped | `UI.Reward.Claim.Start` | `ui_reward_claim_start_01.wav` | UI | High | Leads into POP-04 if unlock exists. |
| XP bar animates | `UI.Progress.XP.Tick` | `ui_progress_xp_tick_01.wav` | UI | Low | Tick only on milestone changes, not every frame. |

### SCN-04 - Settings & Accessibility

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene/modal enter | `UI.Popup.Open` | `ui_popup_open_01.wav` | UI | Medium | Use if Settings opens as overlay; use `UI.Screen.Forward` if full scene. |
| Category tab changed | `UI.Tab.Select` | `ui_tab_select_01.wav` | UI | Medium | One-shot. |
| Master/Music/SFX/Voice slider moved | `UI.Slider.Tick` | `ui_slider_tick_01.wav` | UI | Low | Preview tick plays through affected bus where possible. |
| Toggle changed | `UI.Toggle.On` / `UI.Toggle.Off` | `ui_toggle_on_01.wav`, `ui_toggle_off_01.wav` | UI | Medium | Play after state change persists locally. |
| Dropdown opens/selects | `UI.Dropdown.Open` / `UI.Dropdown.Select` | `ui_dropdown_open_01.wav`, `ui_dropdown_select_01.wav` | UI | Medium | One-shot. |
| Back/apply | `UI.Screen.Back` | `ui_screen_back_01.wav` | UI | Medium | Save before route. |
| Reset/defaults confirmation | `UI.Settings.Reset` | `ui_settings_reset_01.wav` | UI | High | Use only after confirm, not on opening confirm dialog. |

### SCN-05 - Campaign Map

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene enter | `Music.CampaignMap.Loop` | `music_campaign_map_loop_01.wav` | Music | Low | Planning-focused, map-like, less intense than battle. Legacy builds may alias `Music.SagaMap.Loop`. |
| Map ambience | `Ambience.CityPlanning.Loop` | `amb_city_planning_loop_01.wav` | Ambience | Low | Low wind, distant city, distant rotor optional. Legacy builds may alias `Ambience.CityStrategic.Loop`. |
| Chapter dropdown | `UI.Dropdown.Open` / `UI.Dropdown.Select` | Shared | UI | Medium | One-shot. |
| Mission node selected | `UI.Card.MissionNode.Select` | `ui_card_mission_node_select_01.wav` | UI | Medium | Add small map ping. |
| Locked mission node | `UI.Card.Locked` | `ui_card_locked_01.wav` | UI | Medium | One-shot. |
| Difficulty changed | `UI.Difficulty.Select` | `ui_difficulty_select_01.wav` | UI | Medium | Slightly sharper for hard/brutal optional pitch. |
| Chapter reward opened | `UI.Reward.Claim.Start` | `ui_reward_claim_start_01.wav` | UI | High | Use when reward is claimable. |

### SCN-06 - Mission Briefing

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene enter | `Music.Briefing.Loop` | `music_briefing_loop_01.wav` | Music | Low | Tense planning loop. |
| Objective panel expands or receives focus | `UI.Intel.Panel.Focus` | `ui_intel_panel_focus_01.wav` | UI | Medium | Data-scan texture. |
| Enemy intel tile selected | `UI.Intel.Tile.Select` | `ui_intel_tile_select_01.wav` | UI | Medium | Short scan blip. |
| Reward tile selected | `UI.Card.RewardNode.Select` | `ui_card_reward_node_select_01.wav` | UI | Medium | One-shot. |
| Start Mission accepted | `UI.Button.DeployPrep.Click` | `ui_button_deploy_prep_click_01.wav` | UI | High | Strong CTA; route to Loadout. |
| Back | `UI.Screen.Back` | `ui_screen_back_01.wav` | UI | Medium | One-shot. |

### SCN-07 - Loadout / Squad Prep

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene enter | `Music.Loadout.Loop` | `music_loadout_loop_01.wav` | Music | Low | Can share briefing loop with additional percussion layer. |
| Unit card selected | `UI.Card.Unit.Select` | `ui_card_unit_select_01.wav` | UI | Medium | Small metal latch plus radio chirp. |
| Unit added to loadout | `Loadout.Unit.Add` | `game_loadout_unit_add_01.wav` | SFX | Medium | Clear positive lock-in. |
| Unit removed from loadout | `Loadout.Unit.Remove` | `game_loadout_unit_remove_01.wav` | SFX | Medium | Softer negative. |
| Support slot selected | `UI.Card.Support.Select` | `ui_card_support_select_01.wav` | UI | Medium | Use ability-like accent. |
| Gear card selected | `UI.Card.Gear.Select` | `ui_card_gear_select_01.wav` | UI | Medium | Short inventory click. |
| Loadout invalid / Deploy disabled tapped | `UI.Button.Disabled.Tap` | `ui_button_disabled_tap_01.wav` | UI | Medium | Pair with visible error. |
| Deploy accepted | `Mission.Deploy.Confirm` | `game_mission_deploy_confirm_01.wav` | SFX | High | Command radio chirp + heavy confirm. Then transition to battle music. |

### SCN-08 - RTS Battle HUD

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Match starts | `Music.Battle.Intensity01` | `music_battle_intensity_01_loop.wav` | Music | Low | Loop. Crossfade from deploy. |
| Battle escalation | `Music.Battle.Intensity02` / `Music.Battle.Intensity03` | `music_battle_intensity_02_loop.wav`, `music_battle_intensity_03_loop.wav` | Music | High | Crossfade by threat/combat state. |
| Battlefield bed | `Ambience.Battlefield.Loop` | `amb_battlefield_loop_01.wav` | Ambience | Low | Distant city, low wind, far vehicles. |
| Squad card selected | `Gameplay.Unit.Select` | `game_unit_select_generic_01.wav` | SFX | Medium | Unit type can override. Cooldown 150 ms. |
| Move command issued | `Gameplay.Command.Move.Confirm` | `game_command_move_confirm_01.wav` | SFX | Medium | One-shot. |
| Attack command issued | `Gameplay.Command.Attack.Confirm` | `game_command_attack_confirm_01.wav` | SFX | Medium | Sharper than move. |
| Hold/Stop command issued | `Gameplay.Command.Hold.Confirm` / `Gameplay.Command.Stop.Confirm` | `game_command_hold_confirm_01.wav`, `game_command_stop_confirm_01.wav` | SFX | Medium | Short radio/mechanical cue. |
| Special command issued | `Gameplay.Command.Special.Confirm` | `game_command_special_confirm_01.wav` | SFX | High | Use ability-specific override when available. |
| Invalid command | `Gameplay.Command.Invalid` | `game_command_invalid_01.wav` | SFX | High | Cooldown 450 ms. |
| Objective updated | `Gameplay.Objective.Update` | `game_objective_update_01.wav` | SFX | High | Play with objective tracker animation. |
| Objective complete | `Gameplay.Objective.Complete` | `game_objective_complete_01.wav` | SFX | High | Duck ambience slightly. |
| Star goal complete | `Gameplay.StarGoal.Complete` | `game_star_goal_complete_01.wav` | SFX | High | Rewarding but shorter than mission victory. |
| Resource shortage | `Gameplay.Resource.Shortage` | `game_resource_shortage_01.wav` | SFX | High | Only after player action fails. |
| Unit under attack | `Alert.Unit.UnderAttack` | `alert_unit_under_attack_01.wav` | Alerts | High | Cooldown by squad, 8 s. |
| Base breached | `Alert.Base.Breached` | `alert_base_breached_01.wav` | Alerts | Critical | Duck Music/Ambience, cooldown 12 s. |
| Build button opens drawer | `UI.Drawer.Open` | `ui_drawer_open_01.wav` | UI | Medium | Route to SCN-09 overlay. |
| Pause opened | `UI.Pause.Open` | `ui_pause_open_01.wav` | UI | Medium | Snapshot pauses/ducks battle music. |

### SCN-09 - Build Drawer / Production

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Drawer opens | `UI.Drawer.Open` | `ui_drawer_open_01.wav` | UI | Medium | One-shot. |
| Drawer closes | `UI.Drawer.Close` | `ui_drawer_close_01.wav` | UI | Medium | One-shot. |
| Category tab selected | `UI.Tab.Select` | `ui_tab_select_01.wav` | UI | Medium | One-shot. |
| Build item row selected | `UI.Card.BuildItem.Select` | `ui_card_build_item_select_01.wav` | UI | Medium | Short construction-tool accent. |
| Unit production queued | `Gameplay.Production.QueueUnit` | `game_production_queue_unit_01.wav` | SFX | Medium | One-shot. |
| Building placement mode starts | `Gameplay.Build.PlacementStart` | `game_build_placement_start_01.wav` | SFX | Medium | Leads to POP-03. |
| Queue item canceled | `Gameplay.Production.Cancel` | `game_production_cancel_01.wav` | SFX | Medium | Short negative. |
| Rush All accepted | `Gameplay.Production.Rush` | `game_production_rush_01.wav` | SFX | High | Stronger, brief energy/industrial accent. |
| Production complete | `Gameplay.Production.Complete` | `game_production_complete_01.wav` | SFX | High | Cooldown and aggregate if many items finish together. |

### SCN-10 - Unit Command / Command Wheel

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Command wheel opens | `UI.CommandWheel.Open` | `ui_command_wheel_open_01.wav` | UI | Medium | Radial mechanical unfold. |
| Command wheel closes | `UI.CommandWheel.Close` | `ui_command_wheel_close_01.wav` | UI | Medium | Short fold-out. |
| Wheel segment highlighted by touch drag | `UI.CommandWheel.SegmentFocus` | `ui_command_wheel_segment_focus_01.wav` | UI | Low | Cooldown 100 ms; do not play constantly while dragging. |
| Move selected | `Gameplay.Command.Move.Prime` | `game_command_move_prime_01.wav` | SFX | Medium | Await target position. |
| Attack selected | `Gameplay.Command.Attack.Prime` | `game_command_attack_prime_01.wav` | SFX | Medium | Await target. |
| Breach selected | `Gameplay.Command.Breach.Confirm` | `game_command_breach_confirm_01.wav` | SFX | High | Heavy tactical cue. |
| Extract/Rope Drop selected | `Gameplay.Transport.RopeDrop.Command` | `game_transport_ropedrop_command_01.wav` | SFX | High | Use only if command valid. |
| Disabled segment tapped | `Gameplay.Command.Invalid` | `game_command_invalid_01.wav` | SFX | High | Same invalid command cue. |

### SCN-11 - Operations Dashboard

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene enter | `Music.Operation.Loop` | `music_operation_loop_01.wav` | Music | Low | Operations tension, slower than Campaign Map. |
| Strategic ambience | `Ambience.Operation.RoomLoop` | `amb_operation_room_loop_01.wav` | Ambience | Low | Subtle command room, radio, distant city. |
| District selected | `UI.Card.District.Select` | `ui_card_district_select_01.wav` | UI | Medium | Map ping plus card select. |
| Intel Report button | `UI.Intel.Panel.Focus` | `ui_intel_panel_focus_01.wav` | UI | Medium | One-shot. |
| Black Market / Armory / Command Log | `UI.Button.Secondary.Click` | `ui_button_secondary_click_01.wav` | UI | Medium | One-shot. |
| Active warning appears | `Alert.Operation.Warning` | `alert_operation_warning_01.wav` | Alerts | High | Cooldown by warning id. |
| End Day tapped | `Operation.EndDay.Start` | `game_operation_end_day_start_01.wav` | SFX | High | Leads to POP-06. |

### SCN-12 - District Detail / Actions

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene/overlay enter | `UI.Screen.Forward` | `ui_screen_forward_01.wav` | UI | Medium | One-shot. |
| Action card selected | `UI.Card.OperationAction.Select` | `ui_card_operation_action_select_01.wav` | UI | Medium | Base select cue. |
| Patrol accepted | `Operation.Action.Patrol` | `game_operation_action_patrol_01.wav` | SFX | High | Radio patrol dispatch. |
| Drone Scan accepted | `Operation.Action.DroneScan` | `game_operation_action_drone_scan_01.wav` | SFX | High | Scan sweep. |
| Aid accepted | `Operation.Action.Aid` | `game_operation_action_aid_01.wav` | SFX | High | Positive civilian support cue. |
| Raid selected | `Operation.Action.Raid.Prime` | `game_operation_action_raid_prime_01.wav` | SFX | High | Opens POP-02 if confirmation required. |
| Repair accepted | `Operation.Action.Repair` | `game_operation_action_repair_01.wav` | SFX | High | Industrial repair tick. |
| Evacuate accepted | `Operation.Action.Evacuate` | `game_operation_action_evacuate_01.wav` | SFX | High | Transport dispatch cue. |
| Build Outpost accepted | `Operation.Action.BuildOutpost` | `game_operation_action_build_outpost_01.wav` | SFX | High | Construction confirm. |

### SCN-13 - Skirmish Setup

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Scene enter | `Music.CustomSetup.Loop` | `music_custom_setup_loop_01.wav` | Music | Low | Can reuse Loadout/Briefing loop. |
| Preset dropdown changed | `UI.Dropdown.Select` | `ui_dropdown_select_01.wav` | UI | Medium | One-shot. |
| Sliders changed | `UI.Slider.Tick` | `ui_slider_tick_01.wav` | UI | Low | Cooldown 80 ms. |
| Checkbox toggled | `UI.Toggle.On` / `UI.Toggle.Off` | Shared | UI | Medium | One-shot. |
| Map preview changed | `UI.MapPreview.Refresh` | `ui_map_preview_refresh_01.wav` | UI | Medium | Short satellite scan. |
| Config invalid / Launch disabled tapped | `UI.Button.Disabled.Tap` | `ui_button_disabled_tap_01.wav` | UI | Medium | Pair with visible validation message. |
| Launch Mission accepted | `Mission.Deploy.Confirm` | `game_mission_deploy_confirm_01.wav` | SFX | High | Route to MatchScene. |

## Popup Audio Matrix

### POP-01 - Threat Alert

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Non-blocking alert opens | `Alert.Threat.Open.Minor` | `alert_threat_open_minor_01.wav` | Alerts | High | Do not pause simulation. |
| Blocking/critical alert opens | `Alert.Threat.Open.Critical` | `alert_threat_open_critical_01.wav` | Alerts | Critical | Duck Music/Ambience 6 dB for 1.5 s. |
| Ground convoy detected | `Alert.Threat.GroundDetected` | `alert_threat_ground_detected_01.wav` | Alerts | Critical | Cooldown 6 s per threat type. |
| Air threat detected | `Alert.Threat.AirDetected` | `alert_threat_air_detected_01.wav` | Alerts | Critical | Distinct from ground, higher scan tone. |
| Jump to Threat tapped | `Alert.Threat.Jump` | `alert_threat_jump_01.wav` | Alerts | High | Camera/map focus begins immediately. |
| Close dismissed | `UI.Popup.Close` | `ui_popup_close_01.wav` | UI | Medium | Warning remains in feed. |

### POP-02 - Confirm Raid

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Popup opens | `UI.Popup.RaidConfirm.Open` | `ui_popup_raid_confirm_open_01.wav` | UI | High | Darker than standard modal. |
| Risk meter animates | `UI.RiskMeter.Tick` | `ui_risk_meter_tick_01.wav` | UI | Low | Optional, 3-5 ticks max. |
| Cancel | `UI.Button.Negative.Click` | `ui_button_negative_click_01.wav` | UI | Medium | Return to District Detail. |
| Confirm Raid | `Operation.Raid.Confirm` | `game_operation_raid_confirm_01.wav` | SFX | Critical | Radio dispatch plus heavy confirm; duck music briefly. |
| Close X | `UI.Button.Negative.Click` | `ui_button_negative_click_01.wav` | UI | Medium | Same as Cancel. |

### POP-03 - Build Placement

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Placement mode opens | `Gameplay.Build.PlacementStart` | `game_build_placement_start_01.wav` | SFX | Medium | One-shot. |
| Rotate | `Gameplay.Build.Rotate` | `game_build_rotate_01.wav` | SFX | Medium | Mechanical rotate tick. |
| Valid tile focus / ghost moves | `Gameplay.Build.ValidHover` | `game_build_valid_hover_01.wav` | SFX | Low | Optional, cooldown 250 ms. |
| Invalid tile focus / confirm fails | `Gameplay.Build.InvalidPlacement` | `game_build_invalid_placement_01.wav` | SFX | High | Cooldown 450 ms. |
| Confirm placement | `Gameplay.Build.PlaceConfirm` | `game_build_place_confirm_01.wav` | SFX | High | Spend resources and begin construction. |
| Cancel placement | `Gameplay.Build.PlaceCancel` | `game_build_place_cancel_01.wav` | SFX | Medium | Return to drawer/HUD. |

### POP-04 - Reward / Unlock

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Popup opens with normal reward | `Reward.Popup.Open` | `ui_reward_popup_open_01.wav` | UI | High | Positive but short. |
| New unit/building/support unlock | `Reward.Unlock.Major` | `game_reward_unlock_major_01.wav` | SFX | High | Can use 1.5-2.5 s stinger. |
| Currency/XP/items reveal | `Reward.Item.Reveal` | `ui_reward_item_reveal_01.wav` | UI | Medium | Variation rotation. |
| Count-up tick | `UI.Reward.CountTick` | `ui_reward_count_tick_01.wav` | UI | Low | Pitch rises only over short count-ups. |
| Continue | `UI.Button.Primary.Click` | `ui_button_primary_click_01.wav` | UI | Medium | One-shot. |

### POP-05 - Mission Result

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Victory result opens | `Mission.Result.Victory` | `music_stinger_victory_01.wav` | Music | Critical | 3-6 s stinger, then result loop. |
| Defeat result opens | `Mission.Result.Defeat` | `music_stinger_defeat_01.wav` | Music | Critical | 3-6 s stinger, controlled and not melodramatic. |
| Result loop | `Music.Result.Loop` | `music_result_loop_01.wav` | Music | Low | Calm post-mission loop. |
| Each star appears | `Mission.Result.StarReveal` | `ui_result_star_reveal_01.wav` | UI | High | One-shot per star, max 3. |
| Stat tile appears | `Mission.Result.StatReveal` | `ui_result_stat_reveal_01.wav` | UI | Low | Rate-limited. |
| Reward grid appears | `Reward.Item.Reveal` | `ui_reward_item_reveal_01.wav` | UI | Medium | Variation rotation. |
| Replay | `UI.Button.Secondary.Click` | `ui_button_secondary_click_01.wav` | UI | Medium | Then route. |
| Continue | `UI.Button.Primary.Click` | `ui_button_primary_click_01.wav` | UI | Medium | Applies rewards, route to source mode. |

### POP-06 - End of Day Report

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Popup opens | `Operation.EndDay.ReportOpen` | `game_operation_end_day_report_open_01.wav` | SFX | High | Strategic report stinger. |
| Positive district/trust delta | `Operation.Report.PositiveDelta` | `game_operation_report_positive_delta_01.wav` | SFX | Medium | Use for grouped positives. |
| Negative threat/heat delta | `Operation.Report.NegativeDelta` | `game_operation_report_negative_delta_01.wav` | SFX | High | Use sparingly; do not spam per row. |
| Resource summary count | `UI.Reward.CountTick` | `ui_reward_count_tick_01.wav` | UI | Low | Shared count tick. |
| Save starts | `Operation.Save.Start` | `game_operation_save_start_01.wav` | SFX | Medium | Optional. |
| Save complete | `Operation.Save.Complete` | `game_operation_save_complete_01.wav` | SFX | High | Only after persistence succeeds. |
| Save failed | `Operation.Save.Failed` | `alert_operation_save_failed_01.wav` | Alerts | Critical | Pair with visible error and retry. |

### POP-07 - Pause / Options

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Pause opens | `UI.Pause.Open` | `ui_pause_open_01.wav` | UI | Medium | Apply pause mixer snapshot. |
| Resume | `UI.Pause.Resume` | `ui_pause_resume_01.wav` | UI | Medium | Restore snapshot and simulation. |
| Restart Mission | `UI.Button.Negative.Click` | `ui_button_negative_click_01.wav` | UI | High | Confirmation popup should follow. |
| Options | `UI.Button.Settings.Click` | `ui_button_settings_click_01.wav` | UI | Medium | Opens SCN-04 overlay. |
| Help | `UI.Button.Secondary.Click` | `ui_button_secondary_click_01.wav` | UI | Medium | One-shot. |
| Exit to Main Menu confirm | `UI.Exit.Confirm` | `ui_exit_confirm_01.wav` | UI | High | Destructive navigation cue. |

### POP-08 - Intel Reveal

| Location / trigger | Event id | Asset | Bus | Priority | Playback rule |
|---|---|---|---|---|---|
| Popup opens | `Intel.Reveal.Open` | `game_intel_reveal_open_01.wav` | SFX | High | Scan/decryption cue. |
| Evidence card appears | `Intel.Evidence.Reveal` | `game_intel_evidence_reveal_01.wav` | SFX | Medium | Variation rotation. |
| Evidence inspected | `Intel.Evidence.Inspect` | `game_intel_evidence_inspect_01.wav` | SFX | Medium | Short paper/data zoom. |
| Intel confidence increases | `Intel.Confidence.Increase` | `game_intel_confidence_increase_01.wav` | SFX | High | Positive but analytical. |
| View Intel | `UI.Button.Primary.Click` | `ui_button_primary_click_01.wav` | UI | Medium | Route to archive. |
| Close | `UI.Popup.Close` | `ui_popup_close_01.wav` | UI | Medium | One-shot. |

## Gameplay Audio Matrix

### Unit Selection and Commands

| Event id | Trigger | Asset | Priority | Rule |
|---|---|---|---|---|
| `Gameplay.Unit.Select.Infantry` | Infantry/soldier squad selected | `game_unit_select_infantry_01.wav` | Medium | Short radio chirp or gear cloth; rotate 3 variations. |
| `Gameplay.Unit.Select.Vehicle` | APC, armored car, tank, missile launcher selected | `game_unit_select_vehicle_01.wav` | Medium | Short radio + servo click. |
| `Gameplay.Unit.Select.Air` | Drone, helicopter, jet, transport plane selected | `game_unit_select_air_01.wav` | Medium | Radio chirp plus faint rotor/avionics layer. |
| `Gameplay.Unit.Select.Civilian` | Civilian/refugee unit selected | `game_unit_select_civilian_01.wav` | Medium | Softer, non-combat tone. |
| `Gameplay.Command.Move.Confirm` | Valid move target accepted | `game_command_move_confirm_01.wav` | Medium | Play at command source/UI, not world position. |
| `Gameplay.Command.Attack.Confirm` | Valid target attack accepted | `game_command_attack_confirm_01.wav` | Medium | Slightly more aggressive. |
| `Gameplay.Command.Breach.Confirm` | Base breach order accepted | `game_command_breach_confirm_01.wav` | High | Heavy tactical cue. |
| `Gameplay.Command.Transport.Board` | Board APC/helicopter order accepted | `game_transport_board_command_01.wav` | Medium | Transport confirmation. |
| `Gameplay.Command.Invalid` | Invalid command or impossible target | `game_command_invalid_01.wav` | High | Cooldown 450 ms. |

### Combat and Damage

| Event id | Trigger | Asset | Priority | Rule |
|---|---|---|---|---|
| `Combat.Weapon.Rifle.Fire` | Rifle/SMG burst from important visible unit | `combat_weapon_rifle_fire_01.wav` | Low | Spatialized, culled by camera distance and density. |
| `Combat.Weapon.Sniper.Fire` | Marksman/sniper shot | `combat_weapon_sniper_fire_01.wav` | Medium | More distinctive, lower density. |
| `Combat.Weapon.Rocket.Fire` | RPG/rocket/missile launcher shot | `combat_weapon_rocket_fire_01.wav` | High | Important visible or threat shots only. |
| `Combat.Weapon.Cannon.Fire` | Tank cannon shot | `combat_weapon_cannon_fire_01.wav` | High | Low-end safe for mobile speakers. |
| `Combat.Impact.Light` | Bullet impact on ground/building | `combat_impact_light_01.wav` | Low | Randomized, density limited. |
| `Combat.Impact.Heavy` | Shell/rocket/tank impact | `combat_impact_heavy_01.wav` | High | Play if near camera or important target. |
| `Combat.Explosion.Small` | Small explosion | `combat_explosion_small_01.wav` | Medium | Distance and priority gated. |
| `Combat.Explosion.Large` | Building/vehicle large explosion | `combat_explosion_large_01.wav` | High | Can duck ambience briefly. |
| `Combat.Unit.Destroyed.Friendly` | Friendly unit dies | `combat_unit_destroyed_friendly_01.wav` | High | Use only for selected/nearby/important unit. |
| `Combat.Unit.Destroyed.Enemy` | Enemy important unit dies | `combat_unit_destroyed_enemy_01.wav` | Medium | Avoid playing for every enemy in large battles. |
| `Combat.Building.Damaged.Critical` | Owned building low health | `alert_building_damaged_critical_01.wav` | Critical | Cooldown by building, 10 s. |
| `Combat.Building.Destroyed.Friendly` | Owned building destroyed | `alert_building_destroyed_friendly_01.wav` | Critical | Duck music briefly. |

### Vehicles, Aircraft, and Transport

| Event id | Trigger | Asset | Priority | Rule |
|---|---|---|---|---|
| `Vehicle.Engine.APC.Loop` | APC visible/selected and moving | `vehicle_engine_apc_loop_01.wav` | Low | Spatial loop, distance limited. |
| `Vehicle.Engine.Tank.Loop` | Tank moving near camera | `vehicle_engine_tank_loop_01.wav` | Low | Spatial loop, low-pass by distance. |
| `Vehicle.Radar.Pulse` | Radar Tank scans / detects | `vehicle_radar_pulse_01.wav` | Medium | Detection can trigger alert variant. |
| `Aircraft.Helicopter.Loop` | Helicopter near camera | `aircraft_helicopter_loop_01.wav` | Medium | Limit concurrent loops. |
| `Aircraft.Jet.Flyby` | Jet crosses camera or strike action | `aircraft_jet_flyby_01.wav` | High | One-shot flyby, not looped. |
| `Transport.Helicopter.Land` | Helicopter lands for pickup | `transport_helicopter_land_01.wav` | Medium | Spatial. |
| `Transport.Boarding.Complete` | Passenger group boarded | `transport_boarding_complete_01.wav` | Medium | One per group, not per soldier. |
| `Transport.RopeDrop.Start` | First rope drop begins | `transport_ropedrop_start_01.wav` | High | Spatial near drop. |
| `Transport.RopeDrop.UnitDrop` | Individual rope drop | `transport_ropedrop_unit_drop_01.wav` | Low | Cooldown/aggregate if rapid. |

### Building, Production, and Economy

| Event id | Trigger | Asset | Priority | Rule |
|---|---|---|---|---|
| `Gameplay.Build.PlaceConfirm` | Building placement confirmed | `game_build_place_confirm_01.wav` | High | One-shot. |
| `Gameplay.Build.ConstructionLoop` | Selected visible construction active | `game_build_construction_loop_01.wav` | Low | Optional loop; limit concurrent. |
| `Gameplay.Build.Complete` | Building finishes | `game_build_complete_01.wav` | High | One per completed structure; aggregate if multiple. |
| `Gameplay.Production.QueueUnit` | Unit production queued | `game_production_queue_unit_01.wav` | Medium | One-shot. |
| `Gameplay.Production.Complete` | Unit/vehicle ready | `game_production_complete_01.wav` | High | Use faction/producer grouping. |
| `Gameplay.Resource.Gain` | Significant resource reward/gain | `game_resource_gain_01.wav` | Medium | Not for every economy tick. |
| `Gameplay.Resource.Shortage` | Player action fails from missing money/oil/fuel/capacity | `game_resource_shortage_01.wav` | High | Pair with visible shortage. |
| `Gameplay.Gate.Open` | Friendly road barrier gate opens | `game_gate_open_01.wav` | Low | Spatial only if near camera. |
| `Gameplay.Gate.Destroyed` | Gate destroyed / breach opened | `game_gate_destroyed_01.wav` | High | Can also trigger base breach alert. |

### Objectives, Missions, and Progression

| Event id | Trigger | Asset | Priority | Rule |
|---|---|---|---|---|
| `Gameplay.Objective.Update` | Objective progress changes meaningfully | `game_objective_update_01.wav` | High | Do not play for every numeric increment. |
| `Gameplay.Objective.Complete` | Required objective complete | `game_objective_complete_01.wav` | High | UI objective row animates. |
| `Gameplay.Objective.Failed` | Objective failed | `game_objective_failed_01.wav` | Critical | Duck music, visible warning. |
| `Gameplay.StarGoal.Complete` | Bonus/star goal complete | `game_star_goal_complete_01.wav` | High | Short premium reward cue. |
| `Mission.Timer.Warning` | Timer reaches important threshold | `alert_mission_timer_warning_01.wav` | Critical | Example: 60/30/10 seconds remaining. |
| `Mission.Victory` | Win condition achieved | `music_stinger_victory_01.wav` | Critical | Stop battle intensity after stinger tail or crossfade. |
| `Mission.Defeat` | Loss condition achieved | `music_stinger_defeat_01.wav` | Critical | Stop/duck battle audio. |

### Operations and Intel

| Event id | Trigger | Asset | Priority | Rule |
|---|---|---|---|---|
| `Operation.Warning.New` | New operation warning | `alert_operation_warning_01.wav` | High | Cooldown by warning id. |
| `Operation.ThreatLevel.Rises` | Global or district threat rises materially | `alert_operation_threat_rise_01.wav` | High | Not for every small delta. |
| `Operation.Trust.Rises` | Civilian trust/stability positive milestone | `game_operation_trust_rise_01.wav` | Medium | Positive strategic cue. |
| `Operation.Trust.Falls` | Civilian trust/stability negative milestone | `game_operation_trust_fall_01.wav` | High | Controlled negative cue. |
| `Intel.Scan.Start` | Drone scan starts | `game_intel_scan_start_01.wav` | Medium | Scan sweep. |
| `Intel.Scan.Complete` | Drone scan resolves | `game_intel_scan_complete_01.wav` | High | Follow with reveal if evidence found. |
| `Intel.Evidence.Reveal` | New evidence appears | `game_intel_evidence_reveal_01.wav` | Medium | Variation rotation. |
| `Intel.Confidence.Increase` | Intel confidence crosses threshold | `game_intel_confidence_increase_01.wav` | High | Use for meaningful thresholds only. |

## Tutorial and Voice Guidelines

Tutorial audio should teach with minimal friction. It must never interrupt urgent combat feedback.
The authored FTUE and ARIA assistant behavior that drives these cue and VO needs is defined in `WarlineCapture_FTUE_And_Command_Assistant_Design.md`.

Tutorial cue types:

| Event id | Trigger | Asset | Bus | Rule |
|---|---|---|---|---|
| `Tutorial.Step.Open` | Tutorial overlay/card appears | `tutorial_step_open_01.wav` | UI | Soft instructional cue. |
| `Tutorial.Step.Complete` | Player completes tutorial step | `tutorial_step_complete_01.wav` | UI | Positive but short. |
| `Tutorial.Step.Blocked` | Player attempts unavailable action during guided step | `tutorial_step_blocked_01.wav` | UI | Softer than invalid command. |
| `Tutorial.Highlight.Pulse` | UI highlight pulse begins | `tutorial_highlight_pulse_01.wav` | UI | Optional, one per highlight start. |
| `VO.Tutorial.*` | Tutorial narration line | `vo_tutorial_<topic>_<index>.wav` | Voice | Duck Music/Ambience. No overlapping VO. |

Initial tutorial VO file list:

| File | Suggested line intent |
|---|---|
| `vo_tutorial_welcome_01.wav` | Welcome the commander and introduce command view. |
| `vo_tutorial_select_squad_01.wav` | Tell player to select the highlighted squad card. |
| `vo_tutorial_move_units_01.wav` | Tell player to issue a move order to the marked location. |
| `vo_tutorial_attack_target_01.wav` | Tell player to target the enemy unit or structure. |
| `vo_tutorial_build_drawer_01.wav` | Explain opening Build / Production. |
| `vo_tutorial_place_building_01.wav` | Explain valid placement and confirm. |
| `vo_tutorial_threat_alert_01.wav` | Explain incoming threat warnings and Jump to Threat. |
| `vo_tutorial_objectives_01.wav` | Explain primary objectives and star goals. |
| `vo_tutorial_operation_dashboard_01.wav` | Explain district stability, trust, threat, and intel confidence. |
| `vo_tutorial_intel_before_raid_01.wav` | Explain why intel confidence matters before raids. |

VO style:

- Calm tactical commander or operations officer.
- Short, direct lines: 2-7 seconds.
- No real-world unit names, political references, or real conflicts.
- Avoid shouting except for explicitly critical alerts.
- Keep wording localizable. Store final text in localization files, not only in audio filenames.

## Music Plan

Music should be layered by mode and intensity. Do not create one loud battle loop and use it everywhere.

| Event id | Asset | Use | Notes |
|---|---|---|---|
| `Music.Splash.Start` | `music_splash_intro_01.wav` | App launch | 2-4 s logo sting. |
| `Music.Menu.Loop` | `music_menu_loop_01.wav` | Main Menu, Profile | 60-120 s loop, confident and clean. |
| `Music.CampaignMap.Loop` | `music_campaign_map_loop_01.wav` | Campaign Map | Planning and chapter progression. Legacy builds may alias `Music.SagaMap.Loop`. |
| `Music.Operation.Loop` | `music_operation_loop_01.wav` | Operations | Slower, investigative, city command room tension. |
| `Music.Briefing.Loop` | `music_briefing_loop_01.wav` | Briefing/Loadout/Custom Setup | Planning tension, light percussion. |
| `Music.Battle.Intensity01` | `music_battle_intensity_01_loop.wav` | Low combat | Sparse pulse, tactical bed. |
| `Music.Battle.Intensity02` | `music_battle_intensity_02_loop.wav` | Active combat | More percussion and low rhythm. |
| `Music.Battle.Intensity03` | `music_battle_intensity_03_loop.wav` | High threat/base breach | Urgent, but still readable. |
| `Mission.Result.Victory` | `music_stinger_victory_01.wav` | Victory | 3-6 s. |
| `Mission.Result.Defeat` | `music_stinger_defeat_01.wav` | Defeat | 3-6 s. |
| `Music.Result.Loop` | `music_result_loop_01.wav` | Result screen | Calm debrief. |

Music intensity triggers:

- Intensity 01: default match state, no active enemy within threat radius.
- Intensity 02: active combat near camera, objective timer active, or enemy squad advancing.
- Intensity 03: base breached, critical structure under attack, final objective in danger, or critical timer threshold.
- Return to lower intensity only after 8-12 seconds of calmer state to avoid rapid oscillation.

## Ambience Plan

| Event id | Asset | Use | Notes |
|---|---|---|---|
| `Ambience.Base.DistantLoop` | `amb_base_distant_loop_01.wav` | Splash/loading | Distant base, wind, distant rotors. |
| `Ambience.CityPlanning.Loop` | `amb_city_planning_loop_01.wav` | Campaign Map / planning camera | Soft city, distant traffic, wind. Legacy builds may alias `Ambience.CityStrategic.Loop`. |
| `Ambience.Operation.RoomLoop` | `amb_operation_room_loop_01.wav` | Operation Dashboard | Command room, radio static, low electronics. |
| `Ambience.Battlefield.Loop` | `amb_battlefield_loop_01.wav` | MatchScene | Wind, distant artillery, distant traffic/rotors. |
| `Ambience.Intel.ScanLoop` | `amb_intel_scan_loop_01.wav` | Intel/scan overlays | Subtle data-room loop while popup is visible. |

Ambience should sit below music and SFX. It should make screens feel alive without drawing attention.

## AI Audio Generation Guidelines

Use these rules when generating source audio with AI tools.

General export:

- Export WAV masters at 48 kHz, 24-bit when available.
- Trim silence at start and end for UI/SFX.
- Normalize one-shots to around -3 dB peak, then adjust in Unity mixer.
- Generate 3-5 variations for repeated sounds.
- Generate dry versions first. Add reverb in Unity only when needed.
- Avoid copyrighted references, brand names, real-world conflicts, radio samples, recognizable songs, or spoken lines copied from films/games.

### UI Sound Prompt Template

```text
Create a short premium mobile military RTS user interface sound effect.
Style: clean tactical HUD, subtle metal and digital components, modern command interface.
Duration: <duration>.
Emotion: <positive/neutral/negative/urgent>.
Must be crisp on phone speakers, no harsh high frequencies, no cartoon beeps, no long reverb tail.
Export as a dry one-shot WAV.
```

Examples:

| Asset | Prompt detail |
|---|---|
| `ui_button_primary_click_01.wav` | Duration 0.12 s. Positive accepted command. Compact metal click with soft digital confirmation. |
| `ui_button_disabled_tap_01.wav` | Duration 0.16 s. Negative blocked action. Low muted thud plus tiny rejected digital tick. |
| `ui_popup_threat_open_01.wav` | Duration 0.55 s. Urgent modal opening. Tactical warning panel slides in, red alert energy, no siren loop. |
| `ui_slider_tick_01.wav` | Duration 0.04 s. Tiny clean tick, very soft, designed for repeated slider movement. |
| `ui_command_wheel_open_01.wav` | Duration 0.28 s. Radial mechanical HUD unfolding with subtle servo/data texture. |

### Gameplay SFX Prompt Template

```text
Create a grounded stylized military RTS gameplay sound effect for a mobile game.
Subject: <unit/action/impact>.
Perspective: top-down strategy camera, not first-person.
Duration: <duration>.
Style: readable, polished, slightly stylized, works on phone speakers.
Avoid excessive sub-bass, realistic gore, harsh clipping, long cinematic tails, and copyrighted references.
Export as a dry WAV one-shot or seamless loop as specified.
```

Examples:

| Asset | Prompt detail |
|---|---|
| `game_command_move_confirm_01.wav` | Duration 0.25 s. Short radio chirp and map ping confirming a squad move order. |
| `game_build_place_confirm_01.wav` | Duration 0.55 s. Construction placement accepted, metal lock-in, hydraulic thump, subtle digital confirm. |
| `combat_weapon_cannon_fire_01.wav` | Duration 0.8 s. Top-down tank cannon shot, punchy but not deafening, no long battlefield echo. |
| `combat_explosion_large_01.wav` | Duration 1.4 s. Stylized large vehicle/building explosion, controlled low end, clear transient, short debris tail. |
| `transport_ropedrop_start_01.wav` | Duration 1.0 s. Helicopter rope deployment start, rotor wash hint, rope gear release, tactical and readable. |

### Alert Prompt Template

```text
Create a critical warning sound for a premium mobile military command interface.
Warning type: <ground threat/air threat/base breach/timer/save failed>.
Duration: <duration>.
Tone: urgent, authoritative, tactical, not cartoonish, not horror.
Must cut through music on phone speakers without painful high frequencies.
Include a short radio/electronic alert character, no continuous siren unless requested.
Export as WAV.
```

Examples:

| Asset | Prompt detail |
|---|---|
| `alert_threat_ground_detected_01.wav` | Duration 1.2 s. Ground convoy detected, low radar pulse, urgent command UI alert. |
| `alert_threat_air_detected_01.wav` | Duration 1.2 s. Air contact detected, higher radar sweep, sharper but not piercing. |
| `alert_base_breached_01.wav` | Duration 1.6 s. Critical perimeter breach, heavy warning pulse, radio static accent. |
| `alert_mission_timer_warning_01.wav` | Duration 0.9 s. Mission timer warning, tense countdown pulse, clean and short. |

### Music Prompt Template

```text
Create seamless loopable music for a premium mobile military RTS called WarlineCapture.
Mode: <main menu/saga map/operation dashboard/mission briefing/battle intensity>.
Duration: 60 to 120 seconds.
Style: modern tactical orchestral-electronic hybrid, restrained percussion, clean low end, no vocals, no copyrighted references.
Mood: <calm strategic/tension/planning/active combat/critical>.
Must loop cleanly and leave room for UI, voice, and alerts.
Export stereo WAV and provide loop start/end if available.
```

Music examples:

| Asset | Prompt detail |
|---|---|
| `music_menu_loop_01.wav` | Confident command hub, calm tactical pulse, subtle brass/electronic texture, not combat-heavy. |
| `music_operation_loop_01.wav` | Strategic city operation, investigative tension, subdued pulse, distant command-room energy. |
| `music_battle_intensity_01_loop.wav` | Low-intensity tactical battle bed, sparse percussion, no melody that fights alerts. |
| `music_battle_intensity_03_loop.wav` | Critical combat layer, urgent percussion and synth pulses, still clean for mobile speakers. |
| `music_stinger_victory_01.wav` | 3-6 s successful mission stinger, premium military triumph, no choir/vocals. |

### Voice Prompt Template

```text
Generate a calm tactical operations officer voice line for a mobile RTS tutorial.
Line: "<localized source line>"
Voice: clear, professional, composed, military command center tone.
Pacing: concise and intelligible.
No shouting unless marked critical. No real-world political references.
Export clean mono WAV, 48 kHz.
```

Voice processing:

- Use light compression and de-essing.
- Leave no music under generated VO source files.
- Keep original text in localization data.
- Use subtitles for all tutorial and radio voice lines.

## Initial Vertical Slice Asset Checklist

For the first playable audio pass, produce these assets before expanding to the full library.

### Required UI

- `ui_button_primary_click_01.wav`
- `ui_button_secondary_click_01.wav`
- `ui_button_negative_click_01.wav`
- `ui_button_disabled_tap_01.wav`
- `ui_toggle_on_01.wav`
- `ui_toggle_off_01.wav`
- `ui_slider_tick_01.wav`
- `ui_tab_select_01.wav`
- `ui_dropdown_open_01.wav`
- `ui_dropdown_select_01.wav`
- `ui_card_select_01.wav`
- `ui_popup_open_01.wav`
- `ui_popup_close_01.wav`
- `ui_screen_forward_01.wav`
- `ui_screen_back_01.wav`
- `ui_drawer_open_01.wav`
- `ui_drawer_close_01.wav`

### Required Gameplay

- `game_unit_select_infantry_01.wav`
- `game_unit_select_vehicle_01.wav`
- `game_unit_select_air_01.wav`
- `game_command_move_confirm_01.wav`
- `game_command_attack_confirm_01.wav`
- `game_command_invalid_01.wav`
- `game_build_place_confirm_01.wav`
- `game_build_invalid_placement_01.wav`
- `game_production_queue_unit_01.wav`
- `game_production_complete_01.wav`
- `game_objective_update_01.wav`
- `game_objective_complete_01.wav`
- `game_resource_shortage_01.wav`

### Required Alerts

- `alert_threat_ground_detected_01.wav`
- `alert_threat_air_detected_01.wav`
- `alert_unit_under_attack_01.wav`
- `alert_base_breached_01.wav`
- `alert_building_destroyed_friendly_01.wav`
- `alert_mission_timer_warning_01.wav`

### Required Music and Ambience

- `music_menu_loop_01.wav`
- `music_briefing_loop_01.wav`
- `music_battle_intensity_01_loop.wav`
- `music_battle_intensity_02_loop.wav`
- `music_stinger_victory_01.wav`
- `music_stinger_defeat_01.wav`
- `amb_city_strategic_loop_01.wav`
- `amb_battlefield_loop_01.wav`

### Required Tutorial

- `tutorial_step_open_01.wav`
- `tutorial_step_complete_01.wav`
- `tutorial_step_blocked_01.wav`
- `vo_tutorial_select_squad_01.wav`
- `vo_tutorial_move_units_01.wav`
- `vo_tutorial_attack_target_01.wav`
- `vo_tutorial_build_drawer_01.wav`
- `vo_tutorial_threat_alert_01.wav`

## Unity Implementation Guidelines

Recommended data structure:

```csharp
public enum WarlineCaptureAudioPriority
{
    Low,
    Medium,
    High,
    Critical
}

[Serializable]
public sealed class WarlineCaptureAudioEvent
{
    public string EventId;
    public AudioClip[] Variations;
    public AudioMixerGroup MixerGroup;
    public WarlineCaptureAudioPriority Priority;
    public float Volume = 1f;
    public float PitchMin = 0.98f;
    public float PitchMax = 1.02f;
    public float CooldownSeconds;
    public bool DuckMusic;
    public bool StopLowerPrioritySimilarEvents;
}
```

Recommended services:

- `WarlineCaptureAudioService`: central event playback by event id.
- `WarlineCaptureMusicService`: handles music loops, stingers, and intensity crossfades.
- `WarlineCaptureAudioSettingsService`: stores Master/Music/SFX/Voice volume and mute state.
- `WarlineCaptureAudioEventLibrary`: ScriptableObject mapping event ids to clips and rules.
- `WarlineCaptureAudioEmitterPool`: pooled AudioSource objects for one-shots and short spatial sounds.
- `WarlineCaptureCombatAudioLimiter`: limits repeated weapons, impacts, and vehicle loops.

Implementation rules:

- UI code should call audio by event id, never by raw clip reference.
- Gameplay systems should emit high-level audio events, not asset filenames.
- Combat audio should be camera-aware and importance-aware.
- Alerts should pass contextual keys such as threat id, district id, squad id, or building id for cooldown grouping.
- Settings sliders should update mixer exposed parameters immediately.
- A full mute must silence all buses except OS-level accessibility if later added.
- Pause should apply a mixer snapshot that lowers music/ambience and stops nonessential loop emitters.
- On scene change, stop scene-owned loops and let MusicService handle crossfades.

Suggested Unity event examples:

```csharp
audio.Play("UI.Button.Primary.Click");
audio.Play("Gameplay.Command.Invalid", contextKey: selectedUnitId);
audio.Play("Alert.Threat.GroundDetected", contextKey: threatEventId);
music.SetBattleIntensity(2);
music.PlayStinger("Mission.Result.Victory", nextLoop: "Music.Result.Loop");
```

## Import and Compression Guidelines

| Asset type | Unity load type | Compression | Channels | Notes |
|---|---|---|---|---|
| UI one-shots | Decompress On Load | ADPCM or PCM for tiny files | Mono | Lowest latency. |
| Alert one-shots | Decompress On Load | ADPCM/PCM | Mono | Must fire instantly. |
| Gameplay one-shots | Compressed In Memory or Decompress On Load by importance | ADPCM/Vorbis | Mono | Important command cues should be low latency. |
| Weapon density clips | Compressed In Memory | ADPCM | Mono | Use limiter. |
| Vehicle loops | Streaming or Compressed In Memory | Vorbis | Mono/Stereo by need | Limit concurrent loops. |
| Ambience loops | Streaming | Vorbis | Stereo | Low priority. |
| Music loops | Streaming | Vorbis | Stereo | Loop cleanly. |
| Tutorial VO | Compressed In Memory | Vorbis | Mono | Keep subtitles. |

## QA Checklist

Before shipping an audio pass:

- Every interactive UI element in the UI spec has accepted, disabled, cancel, selected, or unavailable feedback.
- Shared UI motion and VFX feedback follows `WarlineCapture_Visual_Feedback_VFX_Recommendations.md`, with audio reinforcing but not replacing visible feedback.
- No touch interaction produces repeated rapid-fire audio.
- Critical alerts are audible over battle music on phone speakers.
- Settings sliders correctly affect Master, Music, SFX/UI/Alerts, and Voice.
- Muting SFX also mutes UI and Alerts unless a separate accessibility policy is added.
- Music loops without clicks.
- Battle intensity does not oscillate rapidly.
- Tutorial VO ducks music/ambience and never overlaps itself.
- Repeated gameplay sounds have variations or cooldowns.
- No generated file contains recognizable copyrighted melodies, real-world radio chatter, or real-world political/conflict references.
- All critical audio events have matching visual UI feedback.

## Implementation Milestones

### Milestone 1 - Audio Foundations

- Create mixer buses and settings persistence.
- Create `WarlineCaptureAudioService`.
- Import the required vertical-slice UI, alert, gameplay, music, ambience, and tutorial assets.
- Wire universal UI feedback for buttons, tabs, toggles, sliders, cards, popups, and screen transitions.
- Wire shared visual-feedback audio for toasts, resource flyouts, meter deltas, locked/disabled rejects, and reduced-motion fallback cues.

### Milestone 2 - Tactical Match Audio

- Wire unit selection and command feedback.
- Wire objective update/complete/fail feedback.
- Wire threat alerts, base breach, unit under attack, and mission timer warnings.
- Add battle music intensity crossfades.
- Add basic combat and vehicle audio with density limiting.

### Milestone 3 - Strategic Modes and Progression

- Wire Campaign Map, Mission Briefing, Loadout, Operations Dashboard, District Detail, and Skirmish Setup.
- Wire reward/unlock, mission result, end-of-day report, and intel reveal popups.
- Add operation and intel ambience/music layers.

### Milestone 4 - Polish and Accessibility

- Add SFX variations.
- Tune mixer snapshots for pause, tutorial, critical alerts, and results.
- Verify phone speaker readability.
- Verify mute/volume/accessibility behavior.
- Finalize asset loudness and import settings.
