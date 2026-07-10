# APH-401 Audio Memory Playback Capture

- Task: `APH-401`
- Capture target: `Menu`
- Capture result: `Succeeded`
- Captured UTC: `2026-07-10T10:46:28.7606450Z`
- Exact commit: `8e1f21c2a4326ff08371d621e808f38f79b2b197`
- Dirty worktree: `true`
- Unity: `6000.5.2f1`
- Active build target: `Android`
- Invocation: `Game.Editor.AudioMemoryPlaybackCapture.RunMenu`
- Catalog SHA-256: `5fc0c2ea16fd65ae311fcf73667a4600dfff105cd8146d106b1db58ed83aabd5`
- JSON: `Design/AgentReports/aph-401_audio-memory-playback-menu.json`
- Markdown: `Design/AgentReports/aph-401_audio-memory-playback-menu.md`
- Raw profiler: `/private/tmp/warline-aph401-audio-memory-menu.raw`
- Raw profiler bytes: `113,988,564`
- Raw profiler SHA-256: `d5d79cd1e122fa7e6d18779f263f86575047b356d3c66f2a9b71055d3bf7a501`
- Raw profiler retention: Transient local profiler evidence; regenerate with the recorded invocation when the local file expires.
- Memory contract: CatalogRuntimeMemoryBytes, loaded clip counts, and bus totals are authoritative audio-residency measurements. Process and Mono counters are context only because the editor capture harness allocates report metadata.

## Phase Summary

| Phase | Time (s) | Event ID | Hash | Status | Catalog bytes | Process allocated (context) | Process reserved (context) | Mono used (context) | Mono heap (context) | Pool | Active |
|---|---:|---|---:|---|---:|---:|---:|---:|---:|---:|---:|
| menu-before-controlled-playback | 0.381 | None | 0 | NotRequested | 45,448,237 | 1,429,032,604 | 1,935,401,704 | 1,584,418,816 | 1,742,422,016 | 8 | 0 |
| menu-after-ui-primary-click | 1.095 | UI.Button.Primary.Click | 3161187545 | Presented | 45,448,237 | 1,488,738,300 | 1,967,862,376 | 1,589,256,192 | 1,742,422,016 | 8 | 0 |
| menu-after-music-loop | 1.805 | Music.Menu.Loop | 3629030835 | Presented | 45,616,296 | 1,548,195,390 | 2,000,820,360 | 1,594,634,240 | 1,742,422,016 | 8 | 1 |

## menu-before-controlled-playback

- Snapshot time: `0.381 s`
- Event: `None`
- Event hash: `0`
- Event status: `NotRequested`
- Triggered at: `Unavailable`
- Requested at: `Unavailable`
- Processed at: `Unavailable`
- Observed at: `0.381 s`
- Catalog clips: `234`
- Loaded catalog clips: `225`
- Catalog runtime memory: `45,448,237 bytes`
- Total allocated memory: `1,429,032,604 bytes`
- Total reserved memory: `1,935,401,704 bytes`
- Mono used memory: `1,584,418,816 bytes`
- Mono heap memory: `1,742,422,016 bytes`
- Source pool: `8`
- Active sources: `0`

### Bus Totals

| Bus | Runtime bytes | Clips | Loaded clips |
|---|---:|---:|---:|
| Alerts | 350,548 | 4 | 4 |
| Ambience | 1,420 | 2 | 0 |
| Music | 4,970 | 7 | 0 |
| SFX | 2,462,278 | 40 | 40 |
| UI | 850,698 | 18 | 18 |
| Voice | 41,778,323 | 163 | 163 |

### Catalog Clip Runtime State

| Asset | Buses | Events | Load state | Runtime bytes |
|---|---|---|---|---:|
| Assets/Game/Audio/Alerts/alert_base_breached_01.wav | Alerts | Alert.Base.Breached | Loaded | 107,041 |
| Assets/Game/Audio/Alerts/alert_threat_critical_01.wav | Alerts | Alert.Threat.Critical | Loaded | 96,457 |
| Assets/Game/Audio/Alerts/alert_threat_minor_01.wav | Alerts | Alert.Threat.Minor | Loaded | 71,761 |
| Assets/Game/Audio/Alerts/alert_unit_under_attack_01.wav | Alerts | Alert.Unit.UnderAttack | Loaded | 75,289 |
| Assets/Game/Audio/Ambience/amb_base_distant_loop_01.wav | Ambience | Ambience.Base.DistantLoop | Unloaded | 710 |
| Assets/Game/Audio/Ambience/amb_city_day_loop_01.wav | Ambience | Ambience.City.DayLoop | Unloaded | 710 |
| Assets/Game/Audio/Gameplay/game_build_place_invalid_01.wav | SFX | Gameplay.Build.Place.Invalid | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_build_place_valid_01.wav | SFX | Gameplay.Build.Place.Valid | Loaded | 54,121 |
| Assets/Game/Audio/Gameplay/game_command_attack_accepted_01.wav | SFX | Gameplay.Command.Attack.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_hold_accepted_01.wav | SFX | Gameplay.Command.Hold.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_move_accepted_01.wav | SFX | Gameplay.Command.Move.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_rejected_01.wav | SFX | Gameplay.Command.Rejected | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_scan_accepted_01.wav | SFX | Gameplay.Command.Scan.Accepted | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_command_scan_targeting_01.wav | SFX | Gameplay.Command.Scan.Targeting | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_command_stop_returning_01.wav | SFX | Gameplay.Command.Stop.Returning | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_explosion_large_01.wav | SFX | Gameplay.Explosion.Large | Loaded | 111,599 |
| Assets/Game/Audio/Gameplay/game_explosion_small_01.wav | SFX | Gameplay.Explosion.Small | Loaded | 70,321 |
| Assets/Game/Audio/Gameplay/game_impact_bullet_01.wav | SFX | Gameplay.Impact.Bullet | Loaded | 16,561 |
| Assets/Game/Audio/Gameplay/game_objective_complete_01.wav | SFX | Gameplay.Objective.Complete | Loaded | 78,817 |
| Assets/Game/Audio/Gameplay/game_objective_failed_01.wav | SFX | Gameplay.Objective.Failed | Loaded | 78,817 |
| Assets/Game/Audio/Gameplay/game_objective_progress_01.wav | SFX | Gameplay.Objective.Progress | Loaded | 57,649 |
| Assets/Game/Audio/Gameplay/game_production_complete_01.wav | SFX | Gameplay.Production.Complete | Loaded | 71,761 |
| Assets/Game/Audio/Gameplay/game_production_queued_01.wav | SFX | Gameplay.Production.Queued | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_accepted_01.wav | SFX | Gameplay.ResourceExchange.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_cancelled_01.wav | SFX | Gameplay.ResourceExchange.Cancelled | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_completed_01.wav | SFX | Gameplay.ResourceExchange.Completed | Loaded | 57,649 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_queue_started_01.wav | SFX | Gameplay.ResourceExchange.QueueStarted | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_rejected_01.wav | SFX | Gameplay.ResourceExchange.Rejected | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_rushed_01.wav | SFX | Gameplay.ResourceExchange.Rushed | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_unit_aircraft_flyby_01.wav | SFX | Gameplay.Unit.Aircraft.Flyby | Loaded | 111,599 |
| Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_flight_01.wav | SFX | Gameplay.Unit.Engine.Aircraft.Flight | Loaded | 270,001 |
| Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_takeoff_01.wav | SFX | Gameplay.Unit.Engine.Aircraft.Takeoff | Loaded | 92,929 |
| Assets/Game/Audio/Gameplay/game_unit_engine_helicopter_flight_01.wav | SFX | Gameplay.Unit.Engine.Helicopter.Flight | Loaded | 54,121 |
| Assets/Game/Audio/Gameplay/game_unit_engine_vehicle_move_01.wav | SFX | Gameplay.Unit.Engine.Vehicle.Move | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_air_01.wav | SFX | Gameplay.Unit.Select.Air | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_infantry_01.wav | SFX | Gameplay.Unit.Select.Infantry | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_vehicle_01.wav | SFX | Gameplay.Unit.Select.Vehicle | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_vehicle_destroyed_01.wav | SFX | Gameplay.Unit.Vehicle.Destroyed | Loaded | 92,401 |
| Assets/Game/Audio/Gameplay/game_unit_vehicle_engine_01.wav | SFX | Gameplay.Unit.Vehicle.Engine | Loaded | 64,705 |
| Assets/Game/Audio/Gameplay/game_weapon_air_missile_launch_01.wav | SFX | Gameplay.Weapon.AirMissile.Launch | Loaded | 56,879 |
| Assets/Game/Audio/Gameplay/game_weapon_fire_small_arms_01.wav | SFX | Gameplay.Weapon.Fire.SmallArms | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_flight_01.wav | SFX | Gameplay.Weapon.Missile.Flight | Loaded | 52,357 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_impact_01.wav | SFX | Gameplay.Weapon.Missile.Impact | Loaded | 82,345 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_launch_01.wav | SFX | Gameplay.Weapon.Missile.Launch | Loaded | 70,321 |
| Assets/Game/Audio/Gameplay/game_weapon_rifle_fire_01.wav | SFX | Gameplay.Weapon.Rifle.Fire | Loaded | 18,481 |
| Assets/Game/Audio/Gameplay/game_weapon_vehicle_cannon_fire_01.wav | SFX | Gameplay.Weapon.VehicleCannon.Fire | Loaded | 54,001 |
| Assets/Game/Audio/Music/music_briefing_loop_01.wav | Music | Music.Briefing.Loop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_match_calm_loop_01.wav | Music | Music.Match.CalmLoop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_match_combat_loop_01.wav | Music | Music.Match.CombatLoop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_menu_loop_01.wav | Music | Music.Menu.Loop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_result_defeat_01.wav | Music | Music.Result.Defeat | Unloaded | 710 |
| Assets/Game/Audio/Music/music_result_victory_01.wav | Music | Music.Result.Victory | Unloaded | 710 |
| Assets/Game/Audio/Music/music_splash_intro_01.wav | Music | Music.Splash.Intro | Unloaded | 710 |
| Assets/Game/Audio/UI/ui_button_disabled_tap_01.wav | UI | UI.Button.Disabled.Tap | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_negative_click_01.wav | UI | UI.Button.Negative.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_primary_click_01.wav | UI | UI.Button.Primary.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_secondary_click_01.wav | UI | UI.Button.Secondary.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_card_locked_01.wav | UI | UI.Card.Locked | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_card_select_01.wav | UI | UI.Card.Select | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_drawer_close_01.wav | UI | UI.Drawer.Close | Loaded | 47,065 |
| Assets/Game/Audio/UI/ui_drawer_open_01.wav | UI | UI.Drawer.Open | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_feedback_toast_error_01.wav | UI | UI.Feedback.Toast.Error | Loaded | 57,649 |
| Assets/Game/Audio/UI/ui_feedback_toast_positive_01.wav | UI | UI.Feedback.Toast.Positive | Loaded | 57,649 |
| Assets/Game/Audio/UI/ui_popup_close_01.wav | UI | UI.Popup.Close | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_popup_open_01.wav | UI | UI.Popup.Open | Loaded | 47,065 |
| Assets/Game/Audio/UI/ui_screen_back_01.wav | UI | UI.Screen.Back | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_screen_forward_01.wav | UI | UI.Screen.Forward | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_slider_tick_01.wav | UI | UI.Slider.Tick | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_tab_select_01.wav | UI | UI.Tab.Select | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_toggle_off_01.wav | UI | UI.Toggle.Off | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_toggle_on_01.wav | UI | UI.Toggle.On | Loaded | 43,537 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_action_place_choose_footprint_01.wav | Voice | VO.ARIA.Message.BuildDrawerActionPlaceChooseFootprint | Loaded | 397,043 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_aircraft_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyAircraft | Loaded | 314,489 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_buildings_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyBuildings | Loaded | 295,437 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_default_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyDefault | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_name_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyName | Loaded | 181,129 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_select_item_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptySelectItem | Loaded | 373,759 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_soldiers_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptySoldiers | Loaded | 306,021 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_vehicles_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyVehicles | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_connecting_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureConnecting | Loaded | 460,547 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_global_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureGlobalQueueFull | Loaded | 555,803 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_invalid_selection_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureInvalidSelection | Loaded | 280,619 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_missing_producer_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureMissingProducer | Loaded | 375,875 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_missing_producer_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureMissingProducerNamed | Loaded | 420,329 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_not_enough_money_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureNotEnoughMoney | Loaded | 331,423 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureQueueFull | Loaded | 515,585 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_queue_full_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureQueueFullNamed | Loaded | 502,883 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_global_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortGlobalQueueFull | Loaded | 339,889 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_missing_producer_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortMissingProducer | Loaded | 257,335 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_not_enough_money_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortNotEnoughMoney | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortQueueFull | Loaded | 303,905 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_queue_full_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortQueueFullNamed | Loaded | 259,451 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_requires_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortRequiresNamed | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_unavailable_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortUnavailable | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_unavailable_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureUnavailable | Loaded | 430,913 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_instruction_cannot_place_here_01.wav | Voice | VO.ARIA.Message.BuildDrawerInstructionCannotPlaceHere | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_instruction_place_pending_confirm_01.wav | Voice | VO.ARIA.Message.BuildDrawerInstructionPlacePendingConfirm | Loaded | 456,313 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_placement_invalid_01.wav | Voice | VO.ARIA.Message.BuildDrawerPlacementInvalid | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_aircraft_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyAircraft | Loaded | 422,445 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_buildings_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyBuildings | Loaded | 397,043 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_default_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyDefault | Loaded | 202,297 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_soldiers_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadySoldiers | Loaded | 405,511 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_vehicles_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyVehicles | Loaded | 411,861 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_success_production_queued_01.wav | Voice | VO.ARIA.Message.BuildDrawerSuccessProductionQueued | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_success_recruitment_queued_01.wav | Voice | VO.ARIA.Message.BuildDrawerSuccessRecruitmentQueued | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_building_placed_01.wav | Voice | VO.ARIA.Message.BuildFeedbackBuildingPlaced | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_drawer_not_ready_01.wav | Voice | VO.ARIA.Message.BuildFeedbackDrawerNotReady | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_no_active_placement_01.wav | Voice | VO.ARIA.Message.BuildFeedbackNoActivePlacement | Loaded | 257,335 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_place_building_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlaceBuilding | Loaded | 174,779 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_place_on_valid_ground_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlaceOnValidGround | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_placement_cancelled_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlacementCancelled | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancel_unavailable_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelUnavailable | Loaded | 259,451 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancelled_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelled | Loaded | 206,531 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancelled_named_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelledNamed | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_clear_unavailable_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionClearUnavailable | Loaded | 248,867 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_cleared_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueCleared | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_cleared_sentence_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueClearedSentence | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_empty_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueEmpty | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_requested_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionRequested | Loaded | 306,021 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_rotated_90_01.wav | Voice | VO.ARIA.Message.BuildFeedbackRotated90 | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_instruction_confirm_01.wav | Voice | VO.ARIA.Message.BuildPlacementInstructionConfirm | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_status_drag_to_position_01.wav | Voice | VO.ARIA.Message.BuildPlacementStatusDragToPosition | Loaded | 195,947 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_status_valid_ground_01.wav | Voice | VO.ARIA.Message.BuildPlacementStatusValidGround | Loaded | 189,597 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_default_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleDefault | Loaded | 174,779 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_fallback_subject_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleFallbackSubject | Loaded | 147,261 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_named_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleNamed | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_confirm_destroy_01.wav | Voice | VO.ARIA.Message.ConfirmDestroy | Loaded | 284,853 |
| Assets/Game/Audio/Voice/ARIA/aria_message_create_first_01.wav | Voice | VO.ARIA.Message.CreateFirst | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_drag_building_to_final_position_01.wav | Voice | VO.ARIA.Message.DragBuildingToFinalPosition | Loaded | 227,699 |
| Assets/Game/Audio/Voice/ARIA/aria_message_match_feedback_blocked_civilian_zone_01.wav | Voice | VO.ARIA.Message.MatchFeedbackBlockedCivilianZone | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_match_feedback_tactical_map_not_ready_01.wav | Voice | VO.ARIA.Message.MatchFeedbackTacticalMapNotReady | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_not_enough_money_01.wav | Voice | VO.ARIA.Message.NotEnoughMoney | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_soldier_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSoldierSingular | Loaded | 164,195 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_squad_count_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSquadCount | Loaded | 191,713 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_squad_selected_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSquadSelected | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_unit_plural_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackUnitPlural | Loaded | 153,611 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_unit_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackUnitSingular | Loaded | 149,377 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_vehicle_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackVehicleSingular | Loaded | 159,961 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_cargo_drop_blocked_01.wav | Voice | VO.ARIA.Message.TacticalAirdropCargoDropBlocked | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_emergency_drop_visual_missing_01.wav | Voice | VO.ARIA.Message.TacticalAirdropEmergencyDropVisualMissing | Loaded | 272,153 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_no_clear_landing_zone_01.wav | Voice | VO.ARIA.Message.TacticalAirdropNoClearLandingZone | Loaded | 282,737 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_parachute_visual_missing_01.wav | Voice | VO.ARIA.Message.TacticalAirdropParachuteVisualMissing | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_attack_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedAttackDescription | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_attack_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedAttackTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_board_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBoardDescription | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_board_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBoardTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_build_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBuildDescription | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_build_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBuildTitle | Loaded | 168,429 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_destroy_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedDestroyDescription | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_destroy_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedDestroyTitle | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_hold_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedHoldDescription | Loaded | 263,685 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_hold_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedHoldTitle | Loaded | 181,129 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_move_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedMoveDescription | Loaded | 240,401 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_move_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedMoveTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_return_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedReturnDescription | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_return_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedReturnTitle | Loaded | 187,481 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_scan_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedScanDescription | Loaded | 250,985 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_scan_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedScanTitle | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_stop_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedStopDescription | Loaded | 274,269 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_stop_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedStopTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_attack_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeAttackDescription | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_attack_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeAttackTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardDescription | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_description_transport_to_passenger_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardDescriptionTransportToPassenger | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_build_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBuildDescription | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_build_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBuildTitle | Loaded | 168,429 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_move_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeMoveDescription | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_move_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeMoveTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_scan_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeScanDescription | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_scan_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeScanTitle | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_prompt_passenger_to_transport_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardPromptPassengerToTransport | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_prompt_transport_to_passenger_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardPromptTransportToPassenger | Loaded | 325,073 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_select_unit_first_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardSelectUnitFirst | Loaded | 223,465 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_selected_unit_cannot_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardSelectedUnitCannotBoard | Loaded | 253,101 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_tap_units_to_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardTapUnitsToBoard | Loaded | 212,881 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardUnavailable | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_attack_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionAttack | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionBoard | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_build_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionBuild | Loaded | 333,539 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_hold_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionHold | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_move_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionMove | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_scan_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionScan | Loaded | 208,649 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_select_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionSelect | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_special_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionSpecial | Loaded | 223,465 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_stop_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionStop | Loaded | 312,371 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_build_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonBuildUnavailable | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_camera_jump_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonCameraJumpUnavailable | Loaded | 253,101 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_command_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonCommandUnavailable | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_insufficient_fuel_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInsufficientFuel | Loaded | 206,531 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_insufficient_resources_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInsufficientResources | Loaded | 240,401 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_invalid_passenger_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInvalidPassenger | Loaded | 297,553 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_invalid_transport_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInvalidTransport | Loaded | 365,291 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_disembark_cell_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoDisembarkCell | Loaded | 312,371 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_eligible_passengers_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoEligiblePassengers | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoSelection | Loaded | 286,969 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_scan_cooldown_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonScanCooldown | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_scan_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonScanUnavailable | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_blocked_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetBlocked | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_not_attackable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetNotAttackable | Loaded | 236,167 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_not_enemy_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetNotEnemy | Loaded | 225,583 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_out_of_bounds_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetOutOfBounds | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_unreachable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetUnreachable | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_transport_full_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTransportFull | Loaded | 200,181 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_transport_passenger_missing_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTransportPassengerMissing | Loaded | 322,955 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_hold_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableHoldNoSelection | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_scan_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableScanNoSelection | Loaded | 320,839 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_stop_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableStopNoSelection | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_air_defense_auto_engage_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackAirDefenseAutoEngage | Loaded | 416,095 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_boarding_transport_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackBoardingTransport | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_camera_follow_active_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackCameraFollowActive | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_camera_follow_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackCameraFollowUnavailable | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_building_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedBuilding | Loaded | 236,167 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_unit_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedUnit | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_units_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedUnits | Loaded | 280,619 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_exiting_passengers_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackExitingPassengers | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_exiting_unit_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackExitingUnit | Loaded | 189,597 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_follow_target_lost_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackFollowTargetLost | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_holding_current_position_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackHoldingCurrentPosition | Loaded | 225,583 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_loading_transport_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackLoadingTransport | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_missile_launched_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackMissileLaunched | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_rts_camera_restored_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackRtsCameraRestored | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_complete_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanComplete | Loaded | 337,773 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_contacts_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanContacts | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_one_contact_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanOneContact | Loaded | 195,947 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_ordered_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanOrdered | Loaded | 392,809 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_stopped_selected_units_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackStoppedSelectedUnits | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_unit_returning_to_base_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackUnitReturningToBase | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_units_returning_to_base_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackUnitsReturningToBase | Loaded | 276,385 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_air_attack_type_01.wav | Voice | VO.ARIA.Message.WarningAirAttackType | Loaded | 208,649 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_count_suffix_01.wav | Voice | VO.ARIA.Message.WarningAttackCountSuffix | Loaded | 200,181 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_eta_seconds_01.wav | Voice | VO.ARIA.Message.WarningAttackEtaSeconds | Loaded | 420,329 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_eta_suffix_01.wav | Voice | VO.ARIA.Message.WarningAttackEtaSuffix | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_ground_attack_type_01.wav | Voice | VO.ARIA.Message.WarningGroundAttackType | Loaded | 267,919 |

## menu-after-ui-primary-click

- Snapshot time: `1.095 s`
- Event: `UI.Button.Primary.Click`
- Event hash: `3161187545`
- Event status: `Presented`
- Triggered at: `0.385 s`
- Requested at: `0.385 s`
- Processed at: `0.414 s`
- Observed at: `1.095 s`
- Catalog clips: `234`
- Loaded catalog clips: `225`
- Catalog runtime memory: `45,448,237 bytes`
- Total allocated memory: `1,488,738,300 bytes`
- Total reserved memory: `1,967,862,376 bytes`
- Mono used memory: `1,589,256,192 bytes`
- Mono heap memory: `1,742,422,016 bytes`
- Source pool: `8`
- Active sources: `0`

### Bus Totals

| Bus | Runtime bytes | Clips | Loaded clips |
|---|---:|---:|---:|
| Alerts | 350,548 | 4 | 4 |
| Ambience | 1,420 | 2 | 0 |
| Music | 4,970 | 7 | 0 |
| SFX | 2,462,278 | 40 | 40 |
| UI | 850,698 | 18 | 18 |
| Voice | 41,778,323 | 163 | 163 |

### Catalog Clip Runtime State

| Asset | Buses | Events | Load state | Runtime bytes |
|---|---|---|---|---:|
| Assets/Game/Audio/Alerts/alert_base_breached_01.wav | Alerts | Alert.Base.Breached | Loaded | 107,041 |
| Assets/Game/Audio/Alerts/alert_threat_critical_01.wav | Alerts | Alert.Threat.Critical | Loaded | 96,457 |
| Assets/Game/Audio/Alerts/alert_threat_minor_01.wav | Alerts | Alert.Threat.Minor | Loaded | 71,761 |
| Assets/Game/Audio/Alerts/alert_unit_under_attack_01.wav | Alerts | Alert.Unit.UnderAttack | Loaded | 75,289 |
| Assets/Game/Audio/Ambience/amb_base_distant_loop_01.wav | Ambience | Ambience.Base.DistantLoop | Unloaded | 710 |
| Assets/Game/Audio/Ambience/amb_city_day_loop_01.wav | Ambience | Ambience.City.DayLoop | Unloaded | 710 |
| Assets/Game/Audio/Gameplay/game_build_place_invalid_01.wav | SFX | Gameplay.Build.Place.Invalid | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_build_place_valid_01.wav | SFX | Gameplay.Build.Place.Valid | Loaded | 54,121 |
| Assets/Game/Audio/Gameplay/game_command_attack_accepted_01.wav | SFX | Gameplay.Command.Attack.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_hold_accepted_01.wav | SFX | Gameplay.Command.Hold.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_move_accepted_01.wav | SFX | Gameplay.Command.Move.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_rejected_01.wav | SFX | Gameplay.Command.Rejected | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_scan_accepted_01.wav | SFX | Gameplay.Command.Scan.Accepted | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_command_scan_targeting_01.wav | SFX | Gameplay.Command.Scan.Targeting | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_command_stop_returning_01.wav | SFX | Gameplay.Command.Stop.Returning | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_explosion_large_01.wav | SFX | Gameplay.Explosion.Large | Loaded | 111,599 |
| Assets/Game/Audio/Gameplay/game_explosion_small_01.wav | SFX | Gameplay.Explosion.Small | Loaded | 70,321 |
| Assets/Game/Audio/Gameplay/game_impact_bullet_01.wav | SFX | Gameplay.Impact.Bullet | Loaded | 16,561 |
| Assets/Game/Audio/Gameplay/game_objective_complete_01.wav | SFX | Gameplay.Objective.Complete | Loaded | 78,817 |
| Assets/Game/Audio/Gameplay/game_objective_failed_01.wav | SFX | Gameplay.Objective.Failed | Loaded | 78,817 |
| Assets/Game/Audio/Gameplay/game_objective_progress_01.wav | SFX | Gameplay.Objective.Progress | Loaded | 57,649 |
| Assets/Game/Audio/Gameplay/game_production_complete_01.wav | SFX | Gameplay.Production.Complete | Loaded | 71,761 |
| Assets/Game/Audio/Gameplay/game_production_queued_01.wav | SFX | Gameplay.Production.Queued | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_accepted_01.wav | SFX | Gameplay.ResourceExchange.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_cancelled_01.wav | SFX | Gameplay.ResourceExchange.Cancelled | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_completed_01.wav | SFX | Gameplay.ResourceExchange.Completed | Loaded | 57,649 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_queue_started_01.wav | SFX | Gameplay.ResourceExchange.QueueStarted | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_rejected_01.wav | SFX | Gameplay.ResourceExchange.Rejected | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_rushed_01.wav | SFX | Gameplay.ResourceExchange.Rushed | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_unit_aircraft_flyby_01.wav | SFX | Gameplay.Unit.Aircraft.Flyby | Loaded | 111,599 |
| Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_flight_01.wav | SFX | Gameplay.Unit.Engine.Aircraft.Flight | Loaded | 270,001 |
| Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_takeoff_01.wav | SFX | Gameplay.Unit.Engine.Aircraft.Takeoff | Loaded | 92,929 |
| Assets/Game/Audio/Gameplay/game_unit_engine_helicopter_flight_01.wav | SFX | Gameplay.Unit.Engine.Helicopter.Flight | Loaded | 54,121 |
| Assets/Game/Audio/Gameplay/game_unit_engine_vehicle_move_01.wav | SFX | Gameplay.Unit.Engine.Vehicle.Move | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_air_01.wav | SFX | Gameplay.Unit.Select.Air | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_infantry_01.wav | SFX | Gameplay.Unit.Select.Infantry | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_vehicle_01.wav | SFX | Gameplay.Unit.Select.Vehicle | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_vehicle_destroyed_01.wav | SFX | Gameplay.Unit.Vehicle.Destroyed | Loaded | 92,401 |
| Assets/Game/Audio/Gameplay/game_unit_vehicle_engine_01.wav | SFX | Gameplay.Unit.Vehicle.Engine | Loaded | 64,705 |
| Assets/Game/Audio/Gameplay/game_weapon_air_missile_launch_01.wav | SFX | Gameplay.Weapon.AirMissile.Launch | Loaded | 56,879 |
| Assets/Game/Audio/Gameplay/game_weapon_fire_small_arms_01.wav | SFX | Gameplay.Weapon.Fire.SmallArms | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_flight_01.wav | SFX | Gameplay.Weapon.Missile.Flight | Loaded | 52,357 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_impact_01.wav | SFX | Gameplay.Weapon.Missile.Impact | Loaded | 82,345 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_launch_01.wav | SFX | Gameplay.Weapon.Missile.Launch | Loaded | 70,321 |
| Assets/Game/Audio/Gameplay/game_weapon_rifle_fire_01.wav | SFX | Gameplay.Weapon.Rifle.Fire | Loaded | 18,481 |
| Assets/Game/Audio/Gameplay/game_weapon_vehicle_cannon_fire_01.wav | SFX | Gameplay.Weapon.VehicleCannon.Fire | Loaded | 54,001 |
| Assets/Game/Audio/Music/music_briefing_loop_01.wav | Music | Music.Briefing.Loop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_match_calm_loop_01.wav | Music | Music.Match.CalmLoop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_match_combat_loop_01.wav | Music | Music.Match.CombatLoop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_menu_loop_01.wav | Music | Music.Menu.Loop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_result_defeat_01.wav | Music | Music.Result.Defeat | Unloaded | 710 |
| Assets/Game/Audio/Music/music_result_victory_01.wav | Music | Music.Result.Victory | Unloaded | 710 |
| Assets/Game/Audio/Music/music_splash_intro_01.wav | Music | Music.Splash.Intro | Unloaded | 710 |
| Assets/Game/Audio/UI/ui_button_disabled_tap_01.wav | UI | UI.Button.Disabled.Tap | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_negative_click_01.wav | UI | UI.Button.Negative.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_primary_click_01.wav | UI | UI.Button.Primary.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_secondary_click_01.wav | UI | UI.Button.Secondary.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_card_locked_01.wav | UI | UI.Card.Locked | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_card_select_01.wav | UI | UI.Card.Select | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_drawer_close_01.wav | UI | UI.Drawer.Close | Loaded | 47,065 |
| Assets/Game/Audio/UI/ui_drawer_open_01.wav | UI | UI.Drawer.Open | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_feedback_toast_error_01.wav | UI | UI.Feedback.Toast.Error | Loaded | 57,649 |
| Assets/Game/Audio/UI/ui_feedback_toast_positive_01.wav | UI | UI.Feedback.Toast.Positive | Loaded | 57,649 |
| Assets/Game/Audio/UI/ui_popup_close_01.wav | UI | UI.Popup.Close | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_popup_open_01.wav | UI | UI.Popup.Open | Loaded | 47,065 |
| Assets/Game/Audio/UI/ui_screen_back_01.wav | UI | UI.Screen.Back | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_screen_forward_01.wav | UI | UI.Screen.Forward | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_slider_tick_01.wav | UI | UI.Slider.Tick | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_tab_select_01.wav | UI | UI.Tab.Select | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_toggle_off_01.wav | UI | UI.Toggle.Off | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_toggle_on_01.wav | UI | UI.Toggle.On | Loaded | 43,537 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_action_place_choose_footprint_01.wav | Voice | VO.ARIA.Message.BuildDrawerActionPlaceChooseFootprint | Loaded | 397,043 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_aircraft_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyAircraft | Loaded | 314,489 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_buildings_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyBuildings | Loaded | 295,437 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_default_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyDefault | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_name_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyName | Loaded | 181,129 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_select_item_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptySelectItem | Loaded | 373,759 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_soldiers_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptySoldiers | Loaded | 306,021 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_vehicles_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyVehicles | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_connecting_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureConnecting | Loaded | 460,547 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_global_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureGlobalQueueFull | Loaded | 555,803 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_invalid_selection_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureInvalidSelection | Loaded | 280,619 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_missing_producer_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureMissingProducer | Loaded | 375,875 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_missing_producer_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureMissingProducerNamed | Loaded | 420,329 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_not_enough_money_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureNotEnoughMoney | Loaded | 331,423 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureQueueFull | Loaded | 515,585 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_queue_full_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureQueueFullNamed | Loaded | 502,883 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_global_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortGlobalQueueFull | Loaded | 339,889 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_missing_producer_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortMissingProducer | Loaded | 257,335 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_not_enough_money_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortNotEnoughMoney | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortQueueFull | Loaded | 303,905 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_queue_full_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortQueueFullNamed | Loaded | 259,451 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_requires_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortRequiresNamed | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_unavailable_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortUnavailable | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_unavailable_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureUnavailable | Loaded | 430,913 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_instruction_cannot_place_here_01.wav | Voice | VO.ARIA.Message.BuildDrawerInstructionCannotPlaceHere | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_instruction_place_pending_confirm_01.wav | Voice | VO.ARIA.Message.BuildDrawerInstructionPlacePendingConfirm | Loaded | 456,313 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_placement_invalid_01.wav | Voice | VO.ARIA.Message.BuildDrawerPlacementInvalid | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_aircraft_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyAircraft | Loaded | 422,445 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_buildings_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyBuildings | Loaded | 397,043 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_default_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyDefault | Loaded | 202,297 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_soldiers_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadySoldiers | Loaded | 405,511 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_vehicles_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyVehicles | Loaded | 411,861 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_success_production_queued_01.wav | Voice | VO.ARIA.Message.BuildDrawerSuccessProductionQueued | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_success_recruitment_queued_01.wav | Voice | VO.ARIA.Message.BuildDrawerSuccessRecruitmentQueued | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_building_placed_01.wav | Voice | VO.ARIA.Message.BuildFeedbackBuildingPlaced | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_drawer_not_ready_01.wav | Voice | VO.ARIA.Message.BuildFeedbackDrawerNotReady | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_no_active_placement_01.wav | Voice | VO.ARIA.Message.BuildFeedbackNoActivePlacement | Loaded | 257,335 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_place_building_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlaceBuilding | Loaded | 174,779 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_place_on_valid_ground_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlaceOnValidGround | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_placement_cancelled_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlacementCancelled | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancel_unavailable_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelUnavailable | Loaded | 259,451 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancelled_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelled | Loaded | 206,531 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancelled_named_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelledNamed | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_clear_unavailable_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionClearUnavailable | Loaded | 248,867 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_cleared_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueCleared | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_cleared_sentence_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueClearedSentence | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_empty_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueEmpty | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_requested_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionRequested | Loaded | 306,021 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_rotated_90_01.wav | Voice | VO.ARIA.Message.BuildFeedbackRotated90 | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_instruction_confirm_01.wav | Voice | VO.ARIA.Message.BuildPlacementInstructionConfirm | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_status_drag_to_position_01.wav | Voice | VO.ARIA.Message.BuildPlacementStatusDragToPosition | Loaded | 195,947 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_status_valid_ground_01.wav | Voice | VO.ARIA.Message.BuildPlacementStatusValidGround | Loaded | 189,597 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_default_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleDefault | Loaded | 174,779 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_fallback_subject_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleFallbackSubject | Loaded | 147,261 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_named_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleNamed | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_confirm_destroy_01.wav | Voice | VO.ARIA.Message.ConfirmDestroy | Loaded | 284,853 |
| Assets/Game/Audio/Voice/ARIA/aria_message_create_first_01.wav | Voice | VO.ARIA.Message.CreateFirst | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_drag_building_to_final_position_01.wav | Voice | VO.ARIA.Message.DragBuildingToFinalPosition | Loaded | 227,699 |
| Assets/Game/Audio/Voice/ARIA/aria_message_match_feedback_blocked_civilian_zone_01.wav | Voice | VO.ARIA.Message.MatchFeedbackBlockedCivilianZone | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_match_feedback_tactical_map_not_ready_01.wav | Voice | VO.ARIA.Message.MatchFeedbackTacticalMapNotReady | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_not_enough_money_01.wav | Voice | VO.ARIA.Message.NotEnoughMoney | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_soldier_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSoldierSingular | Loaded | 164,195 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_squad_count_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSquadCount | Loaded | 191,713 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_squad_selected_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSquadSelected | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_unit_plural_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackUnitPlural | Loaded | 153,611 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_unit_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackUnitSingular | Loaded | 149,377 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_vehicle_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackVehicleSingular | Loaded | 159,961 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_cargo_drop_blocked_01.wav | Voice | VO.ARIA.Message.TacticalAirdropCargoDropBlocked | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_emergency_drop_visual_missing_01.wav | Voice | VO.ARIA.Message.TacticalAirdropEmergencyDropVisualMissing | Loaded | 272,153 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_no_clear_landing_zone_01.wav | Voice | VO.ARIA.Message.TacticalAirdropNoClearLandingZone | Loaded | 282,737 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_parachute_visual_missing_01.wav | Voice | VO.ARIA.Message.TacticalAirdropParachuteVisualMissing | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_attack_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedAttackDescription | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_attack_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedAttackTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_board_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBoardDescription | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_board_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBoardTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_build_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBuildDescription | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_build_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBuildTitle | Loaded | 168,429 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_destroy_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedDestroyDescription | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_destroy_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedDestroyTitle | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_hold_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedHoldDescription | Loaded | 263,685 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_hold_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedHoldTitle | Loaded | 181,129 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_move_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedMoveDescription | Loaded | 240,401 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_move_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedMoveTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_return_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedReturnDescription | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_return_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedReturnTitle | Loaded | 187,481 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_scan_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedScanDescription | Loaded | 250,985 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_scan_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedScanTitle | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_stop_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedStopDescription | Loaded | 274,269 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_stop_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedStopTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_attack_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeAttackDescription | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_attack_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeAttackTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardDescription | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_description_transport_to_passenger_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardDescriptionTransportToPassenger | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_build_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBuildDescription | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_build_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBuildTitle | Loaded | 168,429 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_move_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeMoveDescription | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_move_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeMoveTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_scan_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeScanDescription | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_scan_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeScanTitle | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_prompt_passenger_to_transport_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardPromptPassengerToTransport | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_prompt_transport_to_passenger_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardPromptTransportToPassenger | Loaded | 325,073 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_select_unit_first_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardSelectUnitFirst | Loaded | 223,465 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_selected_unit_cannot_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardSelectedUnitCannotBoard | Loaded | 253,101 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_tap_units_to_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardTapUnitsToBoard | Loaded | 212,881 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardUnavailable | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_attack_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionAttack | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionBoard | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_build_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionBuild | Loaded | 333,539 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_hold_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionHold | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_move_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionMove | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_scan_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionScan | Loaded | 208,649 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_select_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionSelect | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_special_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionSpecial | Loaded | 223,465 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_stop_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionStop | Loaded | 312,371 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_build_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonBuildUnavailable | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_camera_jump_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonCameraJumpUnavailable | Loaded | 253,101 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_command_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonCommandUnavailable | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_insufficient_fuel_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInsufficientFuel | Loaded | 206,531 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_insufficient_resources_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInsufficientResources | Loaded | 240,401 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_invalid_passenger_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInvalidPassenger | Loaded | 297,553 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_invalid_transport_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInvalidTransport | Loaded | 365,291 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_disembark_cell_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoDisembarkCell | Loaded | 312,371 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_eligible_passengers_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoEligiblePassengers | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoSelection | Loaded | 286,969 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_scan_cooldown_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonScanCooldown | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_scan_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonScanUnavailable | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_blocked_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetBlocked | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_not_attackable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetNotAttackable | Loaded | 236,167 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_not_enemy_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetNotEnemy | Loaded | 225,583 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_out_of_bounds_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetOutOfBounds | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_unreachable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetUnreachable | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_transport_full_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTransportFull | Loaded | 200,181 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_transport_passenger_missing_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTransportPassengerMissing | Loaded | 322,955 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_hold_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableHoldNoSelection | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_scan_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableScanNoSelection | Loaded | 320,839 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_stop_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableStopNoSelection | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_air_defense_auto_engage_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackAirDefenseAutoEngage | Loaded | 416,095 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_boarding_transport_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackBoardingTransport | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_camera_follow_active_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackCameraFollowActive | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_camera_follow_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackCameraFollowUnavailable | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_building_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedBuilding | Loaded | 236,167 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_unit_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedUnit | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_units_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedUnits | Loaded | 280,619 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_exiting_passengers_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackExitingPassengers | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_exiting_unit_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackExitingUnit | Loaded | 189,597 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_follow_target_lost_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackFollowTargetLost | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_holding_current_position_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackHoldingCurrentPosition | Loaded | 225,583 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_loading_transport_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackLoadingTransport | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_missile_launched_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackMissileLaunched | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_rts_camera_restored_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackRtsCameraRestored | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_complete_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanComplete | Loaded | 337,773 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_contacts_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanContacts | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_one_contact_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanOneContact | Loaded | 195,947 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_ordered_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanOrdered | Loaded | 392,809 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_stopped_selected_units_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackStoppedSelectedUnits | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_unit_returning_to_base_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackUnitReturningToBase | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_units_returning_to_base_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackUnitsReturningToBase | Loaded | 276,385 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_air_attack_type_01.wav | Voice | VO.ARIA.Message.WarningAirAttackType | Loaded | 208,649 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_count_suffix_01.wav | Voice | VO.ARIA.Message.WarningAttackCountSuffix | Loaded | 200,181 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_eta_seconds_01.wav | Voice | VO.ARIA.Message.WarningAttackEtaSeconds | Loaded | 420,329 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_eta_suffix_01.wav | Voice | VO.ARIA.Message.WarningAttackEtaSuffix | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_ground_attack_type_01.wav | Voice | VO.ARIA.Message.WarningGroundAttackType | Loaded | 267,919 |

## menu-after-music-loop

- Snapshot time: `1.805 s`
- Event: `Music.Menu.Loop`
- Event hash: `3629030835`
- Event status: `Presented`
- Triggered at: `1.098 s`
- Requested at: `1.119 s`
- Processed at: `1.123 s`
- Observed at: `1.805 s`
- Catalog clips: `234`
- Loaded catalog clips: `226`
- Catalog runtime memory: `45,616,296 bytes`
- Total allocated memory: `1,548,195,390 bytes`
- Total reserved memory: `2,000,820,360 bytes`
- Mono used memory: `1,594,634,240 bytes`
- Mono heap memory: `1,742,422,016 bytes`
- Source pool: `8`
- Active sources: `1`

### Bus Totals

| Bus | Runtime bytes | Clips | Loaded clips |
|---|---:|---:|---:|
| Alerts | 350,548 | 4 | 4 |
| Ambience | 1,420 | 2 | 0 |
| Music | 173,029 | 7 | 1 |
| SFX | 2,462,278 | 40 | 40 |
| UI | 850,698 | 18 | 18 |
| Voice | 41,778,323 | 163 | 163 |

### Catalog Clip Runtime State

| Asset | Buses | Events | Load state | Runtime bytes |
|---|---|---|---|---:|
| Assets/Game/Audio/Alerts/alert_base_breached_01.wav | Alerts | Alert.Base.Breached | Loaded | 107,041 |
| Assets/Game/Audio/Alerts/alert_threat_critical_01.wav | Alerts | Alert.Threat.Critical | Loaded | 96,457 |
| Assets/Game/Audio/Alerts/alert_threat_minor_01.wav | Alerts | Alert.Threat.Minor | Loaded | 71,761 |
| Assets/Game/Audio/Alerts/alert_unit_under_attack_01.wav | Alerts | Alert.Unit.UnderAttack | Loaded | 75,289 |
| Assets/Game/Audio/Ambience/amb_base_distant_loop_01.wav | Ambience | Ambience.Base.DistantLoop | Unloaded | 710 |
| Assets/Game/Audio/Ambience/amb_city_day_loop_01.wav | Ambience | Ambience.City.DayLoop | Unloaded | 710 |
| Assets/Game/Audio/Gameplay/game_build_place_invalid_01.wav | SFX | Gameplay.Build.Place.Invalid | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_build_place_valid_01.wav | SFX | Gameplay.Build.Place.Valid | Loaded | 54,121 |
| Assets/Game/Audio/Gameplay/game_command_attack_accepted_01.wav | SFX | Gameplay.Command.Attack.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_hold_accepted_01.wav | SFX | Gameplay.Command.Hold.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_move_accepted_01.wav | SFX | Gameplay.Command.Move.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_rejected_01.wav | SFX | Gameplay.Command.Rejected | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_command_scan_accepted_01.wav | SFX | Gameplay.Command.Scan.Accepted | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_command_scan_targeting_01.wav | SFX | Gameplay.Command.Scan.Targeting | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_command_stop_returning_01.wav | SFX | Gameplay.Command.Stop.Returning | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_explosion_large_01.wav | SFX | Gameplay.Explosion.Large | Loaded | 111,599 |
| Assets/Game/Audio/Gameplay/game_explosion_small_01.wav | SFX | Gameplay.Explosion.Small | Loaded | 70,321 |
| Assets/Game/Audio/Gameplay/game_impact_bullet_01.wav | SFX | Gameplay.Impact.Bullet | Loaded | 16,561 |
| Assets/Game/Audio/Gameplay/game_objective_complete_01.wav | SFX | Gameplay.Objective.Complete | Loaded | 78,817 |
| Assets/Game/Audio/Gameplay/game_objective_failed_01.wav | SFX | Gameplay.Objective.Failed | Loaded | 78,817 |
| Assets/Game/Audio/Gameplay/game_objective_progress_01.wav | SFX | Gameplay.Objective.Progress | Loaded | 57,649 |
| Assets/Game/Audio/Gameplay/game_production_complete_01.wav | SFX | Gameplay.Production.Complete | Loaded | 71,761 |
| Assets/Game/Audio/Gameplay/game_production_queued_01.wav | SFX | Gameplay.Production.Queued | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_accepted_01.wav | SFX | Gameplay.ResourceExchange.Accepted | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_cancelled_01.wav | SFX | Gameplay.ResourceExchange.Cancelled | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_completed_01.wav | SFX | Gameplay.ResourceExchange.Completed | Loaded | 57,649 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_queue_started_01.wav | SFX | Gameplay.ResourceExchange.QueueStarted | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_rejected_01.wav | SFX | Gameplay.ResourceExchange.Rejected | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_resource_exchange_rushed_01.wav | SFX | Gameplay.ResourceExchange.Rushed | Loaded | 47,065 |
| Assets/Game/Audio/Gameplay/game_unit_aircraft_flyby_01.wav | SFX | Gameplay.Unit.Aircraft.Flyby | Loaded | 111,599 |
| Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_flight_01.wav | SFX | Gameplay.Unit.Engine.Aircraft.Flight | Loaded | 270,001 |
| Assets/Game/Audio/Gameplay/game_unit_engine_aircraft_takeoff_01.wav | SFX | Gameplay.Unit.Engine.Aircraft.Takeoff | Loaded | 92,929 |
| Assets/Game/Audio/Gameplay/game_unit_engine_helicopter_flight_01.wav | SFX | Gameplay.Unit.Engine.Helicopter.Flight | Loaded | 54,121 |
| Assets/Game/Audio/Gameplay/game_unit_engine_vehicle_move_01.wav | SFX | Gameplay.Unit.Engine.Vehicle.Move | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_air_01.wav | SFX | Gameplay.Unit.Select.Air | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_infantry_01.wav | SFX | Gameplay.Unit.Select.Infantry | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_select_vehicle_01.wav | SFX | Gameplay.Unit.Select.Vehicle | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_unit_vehicle_destroyed_01.wav | SFX | Gameplay.Unit.Vehicle.Destroyed | Loaded | 92,401 |
| Assets/Game/Audio/Gameplay/game_unit_vehicle_engine_01.wav | SFX | Gameplay.Unit.Vehicle.Engine | Loaded | 64,705 |
| Assets/Game/Audio/Gameplay/game_weapon_air_missile_launch_01.wav | SFX | Gameplay.Weapon.AirMissile.Launch | Loaded | 56,879 |
| Assets/Game/Audio/Gameplay/game_weapon_fire_small_arms_01.wav | SFX | Gameplay.Weapon.Fire.SmallArms | Loaded | 43,537 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_flight_01.wav | SFX | Gameplay.Weapon.Missile.Flight | Loaded | 52,357 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_impact_01.wav | SFX | Gameplay.Weapon.Missile.Impact | Loaded | 82,345 |
| Assets/Game/Audio/Gameplay/game_weapon_missile_launch_01.wav | SFX | Gameplay.Weapon.Missile.Launch | Loaded | 70,321 |
| Assets/Game/Audio/Gameplay/game_weapon_rifle_fire_01.wav | SFX | Gameplay.Weapon.Rifle.Fire | Loaded | 18,481 |
| Assets/Game/Audio/Gameplay/game_weapon_vehicle_cannon_fire_01.wav | SFX | Gameplay.Weapon.VehicleCannon.Fire | Loaded | 54,001 |
| Assets/Game/Audio/Music/music_briefing_loop_01.wav | Music | Music.Briefing.Loop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_match_calm_loop_01.wav | Music | Music.Match.CalmLoop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_match_combat_loop_01.wav | Music | Music.Match.CombatLoop | Unloaded | 710 |
| Assets/Game/Audio/Music/music_menu_loop_01.wav | Music | Music.Menu.Loop | Loaded | 168,769 |
| Assets/Game/Audio/Music/music_result_defeat_01.wav | Music | Music.Result.Defeat | Unloaded | 710 |
| Assets/Game/Audio/Music/music_result_victory_01.wav | Music | Music.Result.Victory | Unloaded | 710 |
| Assets/Game/Audio/Music/music_splash_intro_01.wav | Music | Music.Splash.Intro | Unloaded | 710 |
| Assets/Game/Audio/UI/ui_button_disabled_tap_01.wav | UI | UI.Button.Disabled.Tap | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_negative_click_01.wav | UI | UI.Button.Negative.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_primary_click_01.wav | UI | UI.Button.Primary.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_button_secondary_click_01.wav | UI | UI.Button.Secondary.Click | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_card_locked_01.wav | UI | UI.Card.Locked | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_card_select_01.wav | UI | UI.Card.Select | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_drawer_close_01.wav | UI | UI.Drawer.Close | Loaded | 47,065 |
| Assets/Game/Audio/UI/ui_drawer_open_01.wav | UI | UI.Drawer.Open | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_feedback_toast_error_01.wav | UI | UI.Feedback.Toast.Error | Loaded | 57,649 |
| Assets/Game/Audio/UI/ui_feedback_toast_positive_01.wav | UI | UI.Feedback.Toast.Positive | Loaded | 57,649 |
| Assets/Game/Audio/UI/ui_popup_close_01.wav | UI | UI.Popup.Close | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_popup_open_01.wav | UI | UI.Popup.Open | Loaded | 47,065 |
| Assets/Game/Audio/UI/ui_screen_back_01.wav | UI | UI.Screen.Back | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_screen_forward_01.wav | UI | UI.Screen.Forward | Loaded | 54,121 |
| Assets/Game/Audio/UI/ui_slider_tick_01.wav | UI | UI.Slider.Tick | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_tab_select_01.wav | UI | UI.Tab.Select | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_toggle_off_01.wav | UI | UI.Toggle.Off | Loaded | 43,537 |
| Assets/Game/Audio/UI/ui_toggle_on_01.wav | UI | UI.Toggle.On | Loaded | 43,537 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_action_place_choose_footprint_01.wav | Voice | VO.ARIA.Message.BuildDrawerActionPlaceChooseFootprint | Loaded | 397,043 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_aircraft_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyAircraft | Loaded | 314,489 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_buildings_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyBuildings | Loaded | 295,437 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_default_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyDefault | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_name_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyName | Loaded | 181,129 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_select_item_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptySelectItem | Loaded | 373,759 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_soldiers_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptySoldiers | Loaded | 306,021 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_empty_vehicles_01.wav | Voice | VO.ARIA.Message.BuildDrawerEmptyVehicles | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_connecting_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureConnecting | Loaded | 460,547 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_global_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureGlobalQueueFull | Loaded | 555,803 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_invalid_selection_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureInvalidSelection | Loaded | 280,619 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_missing_producer_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureMissingProducer | Loaded | 375,875 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_missing_producer_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureMissingProducerNamed | Loaded | 420,329 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_not_enough_money_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureNotEnoughMoney | Loaded | 331,423 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureQueueFull | Loaded | 515,585 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_queue_full_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureQueueFullNamed | Loaded | 502,883 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_global_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortGlobalQueueFull | Loaded | 339,889 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_missing_producer_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortMissingProducer | Loaded | 257,335 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_not_enough_money_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortNotEnoughMoney | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_queue_full_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortQueueFull | Loaded | 303,905 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_queue_full_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortQueueFullNamed | Loaded | 259,451 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_requires_named_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortRequiresNamed | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_short_unavailable_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureShortUnavailable | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_failure_unavailable_01.wav | Voice | VO.ARIA.Message.BuildDrawerFailureUnavailable | Loaded | 430,913 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_instruction_cannot_place_here_01.wav | Voice | VO.ARIA.Message.BuildDrawerInstructionCannotPlaceHere | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_instruction_place_pending_confirm_01.wav | Voice | VO.ARIA.Message.BuildDrawerInstructionPlacePendingConfirm | Loaded | 456,313 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_placement_invalid_01.wav | Voice | VO.ARIA.Message.BuildDrawerPlacementInvalid | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_aircraft_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyAircraft | Loaded | 422,445 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_buildings_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyBuildings | Loaded | 397,043 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_default_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyDefault | Loaded | 202,297 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_soldiers_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadySoldiers | Loaded | 405,511 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_ready_vehicles_01.wav | Voice | VO.ARIA.Message.BuildDrawerReadyVehicles | Loaded | 411,861 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_success_production_queued_01.wav | Voice | VO.ARIA.Message.BuildDrawerSuccessProductionQueued | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_drawer_success_recruitment_queued_01.wav | Voice | VO.ARIA.Message.BuildDrawerSuccessRecruitmentQueued | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_building_placed_01.wav | Voice | VO.ARIA.Message.BuildFeedbackBuildingPlaced | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_drawer_not_ready_01.wav | Voice | VO.ARIA.Message.BuildFeedbackDrawerNotReady | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_no_active_placement_01.wav | Voice | VO.ARIA.Message.BuildFeedbackNoActivePlacement | Loaded | 257,335 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_place_building_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlaceBuilding | Loaded | 174,779 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_place_on_valid_ground_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlaceOnValidGround | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_placement_cancelled_01.wav | Voice | VO.ARIA.Message.BuildFeedbackPlacementCancelled | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancel_unavailable_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelUnavailable | Loaded | 259,451 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancelled_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelled | Loaded | 206,531 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_cancelled_named_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionCancelledNamed | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_clear_unavailable_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionClearUnavailable | Loaded | 248,867 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_cleared_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueCleared | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_cleared_sentence_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueClearedSentence | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_queue_empty_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionQueueEmpty | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_production_requested_01.wav | Voice | VO.ARIA.Message.BuildFeedbackProductionRequested | Loaded | 306,021 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_feedback_rotated_90_01.wav | Voice | VO.ARIA.Message.BuildFeedbackRotated90 | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_instruction_confirm_01.wav | Voice | VO.ARIA.Message.BuildPlacementInstructionConfirm | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_status_drag_to_position_01.wav | Voice | VO.ARIA.Message.BuildPlacementStatusDragToPosition | Loaded | 195,947 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_status_valid_ground_01.wav | Voice | VO.ARIA.Message.BuildPlacementStatusValidGround | Loaded | 189,597 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_default_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleDefault | Loaded | 174,779 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_fallback_subject_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleFallbackSubject | Loaded | 147,261 |
| Assets/Game/Audio/Voice/ARIA/aria_message_build_placement_title_named_01.wav | Voice | VO.ARIA.Message.BuildPlacementTitleNamed | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_confirm_destroy_01.wav | Voice | VO.ARIA.Message.ConfirmDestroy | Loaded | 284,853 |
| Assets/Game/Audio/Voice/ARIA/aria_message_create_first_01.wav | Voice | VO.ARIA.Message.CreateFirst | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_drag_building_to_final_position_01.wav | Voice | VO.ARIA.Message.DragBuildingToFinalPosition | Loaded | 227,699 |
| Assets/Game/Audio/Voice/ARIA/aria_message_match_feedback_blocked_civilian_zone_01.wav | Voice | VO.ARIA.Message.MatchFeedbackBlockedCivilianZone | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_match_feedback_tactical_map_not_ready_01.wav | Voice | VO.ARIA.Message.MatchFeedbackTacticalMapNotReady | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_not_enough_money_01.wav | Voice | VO.ARIA.Message.NotEnoughMoney | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_soldier_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSoldierSingular | Loaded | 164,195 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_squad_count_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSquadCount | Loaded | 191,713 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_squad_selected_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackSquadSelected | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_unit_plural_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackUnitPlural | Loaded | 153,611 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_unit_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackUnitSingular | Loaded | 149,377 |
| Assets/Game/Audio/Voice/ARIA/aria_message_selection_feedback_vehicle_singular_01.wav | Voice | VO.ARIA.Message.SelectionFeedbackVehicleSingular | Loaded | 159,961 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_cargo_drop_blocked_01.wav | Voice | VO.ARIA.Message.TacticalAirdropCargoDropBlocked | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_emergency_drop_visual_missing_01.wav | Voice | VO.ARIA.Message.TacticalAirdropEmergencyDropVisualMissing | Loaded | 272,153 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_no_clear_landing_zone_01.wav | Voice | VO.ARIA.Message.TacticalAirdropNoClearLandingZone | Loaded | 282,737 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_airdrop_parachute_visual_missing_01.wav | Voice | VO.ARIA.Message.TacticalAirdropParachuteVisualMissing | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_attack_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedAttackDescription | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_attack_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedAttackTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_board_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBoardDescription | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_board_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBoardTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_build_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBuildDescription | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_build_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedBuildTitle | Loaded | 168,429 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_destroy_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedDestroyDescription | Loaded | 229,817 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_destroy_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedDestroyTitle | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_hold_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedHoldDescription | Loaded | 263,685 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_hold_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedHoldTitle | Loaded | 181,129 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_move_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedMoveDescription | Loaded | 240,401 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_move_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedMoveTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_return_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedReturnDescription | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_return_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedReturnTitle | Loaded | 187,481 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_scan_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedScanDescription | Loaded | 250,985 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_scan_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedScanTitle | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_stop_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedStopDescription | Loaded | 274,269 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_accepted_stop_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerAcceptedStopTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_attack_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeAttackDescription | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_attack_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeAttackTitle | Loaded | 179,013 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardDescription | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_description_transport_to_passenger_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardDescriptionTransportToPassenger | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_board_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBoardTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_build_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBuildDescription | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_build_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeBuildTitle | Loaded | 168,429 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_move_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeMoveDescription | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_move_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeMoveTitle | Loaded | 172,663 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_scan_description_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeScanDescription | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_banner_mode_scan_title_01.wav | Voice | VO.ARIA.Message.TacticalBannerModeScanTitle | Loaded | 183,247 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_prompt_passenger_to_transport_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardPromptPassengerToTransport | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_prompt_transport_to_passenger_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardPromptTransportToPassenger | Loaded | 325,073 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_select_unit_first_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardSelectUnitFirst | Loaded | 223,465 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_selected_unit_cannot_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardSelectedUnitCannotBoard | Loaded | 253,101 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_tap_units_to_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardTapUnitsToBoard | Loaded | 212,881 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_board_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandBoardUnavailable | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_attack_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionAttack | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_board_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionBoard | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_build_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionBuild | Loaded | 333,539 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_hold_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionHold | Loaded | 267,919 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_move_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionMove | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_scan_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionScan | Loaded | 208,649 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_select_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionSelect | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_special_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionSpecial | Loaded | 223,465 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_instruction_stop_01.wav | Voice | VO.ARIA.Message.TacticalCommandInstructionStop | Loaded | 312,371 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_build_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonBuildUnavailable | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_camera_jump_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonCameraJumpUnavailable | Loaded | 253,101 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_command_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonCommandUnavailable | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_insufficient_fuel_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInsufficientFuel | Loaded | 206,531 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_insufficient_resources_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInsufficientResources | Loaded | 240,401 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_invalid_passenger_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInvalidPassenger | Loaded | 297,553 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_invalid_transport_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonInvalidTransport | Loaded | 365,291 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_disembark_cell_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoDisembarkCell | Loaded | 312,371 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_eligible_passengers_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoEligiblePassengers | Loaded | 335,657 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonNoSelection | Loaded | 286,969 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_scan_cooldown_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonScanCooldown | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_scan_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonScanUnavailable | Loaded | 217,115 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_blocked_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetBlocked | Loaded | 193,831 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_not_attackable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetNotAttackable | Loaded | 236,167 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_not_enemy_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetNotEnemy | Loaded | 225,583 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_out_of_bounds_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetOutOfBounds | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_target_unreachable_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTargetUnreachable | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_transport_full_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTransportFull | Loaded | 200,181 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_reason_transport_passenger_missing_01.wav | Voice | VO.ARIA.Message.TacticalCommandReasonTransportPassengerMissing | Loaded | 322,955 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_hold_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableHoldNoSelection | Loaded | 293,321 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_scan_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableScanNoSelection | Loaded | 320,839 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_command_unavailable_stop_no_selection_01.wav | Voice | VO.ARIA.Message.TacticalCommandUnavailableStopNoSelection | Loaded | 301,787 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_air_defense_auto_engage_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackAirDefenseAutoEngage | Loaded | 416,095 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_boarding_transport_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackBoardingTransport | Loaded | 204,415 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_camera_follow_active_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackCameraFollowActive | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_camera_follow_unavailable_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackCameraFollowUnavailable | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_building_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedBuilding | Loaded | 236,167 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_unit_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedUnit | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_destroyed_selected_units_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackDestroyedSelectedUnits | Loaded | 280,619 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_exiting_passengers_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackExitingPassengers | Loaded | 221,349 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_exiting_unit_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackExitingUnit | Loaded | 189,597 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_follow_target_lost_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackFollowTargetLost | Loaded | 219,233 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_holding_current_position_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackHoldingCurrentPosition | Loaded | 225,583 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_loading_transport_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackLoadingTransport | Loaded | 210,765 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_missile_launched_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackMissileLaunched | Loaded | 185,363 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_rts_camera_restored_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackRtsCameraRestored | Loaded | 242,517 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_complete_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanComplete | Loaded | 337,773 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_contacts_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanContacts | Loaded | 214,999 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_one_contact_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanOneContact | Loaded | 195,947 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_scan_ordered_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackScanOrdered | Loaded | 392,809 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_stopped_selected_units_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackStoppedSelectedUnits | Loaded | 238,283 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_unit_returning_to_base_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackUnitReturningToBase | Loaded | 231,933 |
| Assets/Game/Audio/Voice/ARIA/aria_message_tactical_feedback_units_returning_to_base_01.wav | Voice | VO.ARIA.Message.TacticalFeedbackUnitsReturningToBase | Loaded | 276,385 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_air_attack_type_01.wav | Voice | VO.ARIA.Message.WarningAirAttackType | Loaded | 208,649 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_count_suffix_01.wav | Voice | VO.ARIA.Message.WarningAttackCountSuffix | Loaded | 200,181 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_eta_seconds_01.wav | Voice | VO.ARIA.Message.WarningAttackEtaSeconds | Loaded | 420,329 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_attack_eta_suffix_01.wav | Voice | VO.ARIA.Message.WarningAttackEtaSuffix | Loaded | 246,751 |
| Assets/Game/Audio/Voice/ARIA/aria_message_warning_ground_attack_type_01.wav | Voice | VO.ARIA.Message.WarningGroundAttackType | Loaded | 267,919 |
