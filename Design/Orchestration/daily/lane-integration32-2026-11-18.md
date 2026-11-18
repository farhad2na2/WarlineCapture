# Lane integration 32 — 2026-11-18 (lane worker 32)

Brief: `WarlineCapture-program-logs/briefs/lane-integration32-2026-11-18.md`. Logs: `WarlineCapture-program-logs/2026-11-18/lane32/`.
Startup: no Editor owned WarlineCapture (only Fig/Fig-SquadPlay Editors, untouched), no `Library/LibraryInitializing`, disk_guard ok before every launch.
Every launch passed the startup check (`Library Redirect Path` at line 92-93). Focused tests ran without `-quit`; each Editor was stopped by its own verified PID after its marker, at 195-198 s (12992, 14526, 16369, 17651). Captures and the queue exited by themselves.

## 1. Merge
- `missions/codex-batch-040` (b96400ead) merged `--no-ff` as 758261d1b, no conflicts.
- Compile clean: compile1.log:1677, 0 `error CS`. Python 149 OK (python.txt:33).

## 2. Focused tests
| Check | Result |
|---|---|
| LandedHelicopterSquadSelection (Codex 041) | Passed (landed.log:487) |
| SkirmishAircraftFollowFramingTests | Passed tests=5 (follow.log:487) |
| SkirmishCameraFramingTests | Passed tests=10 (framing.log:510) |
| SkirmishMeasuredRotorAircraftFramingTests | **Failed as expected** at VerifyInstalled (rotor.log:505): `Actual source dependency changed for TransportHeli: path=Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab installed=d995b6ec0d4f44df809f5e18c57dad96 measured=06da3cf2cc3f881bda20b65457b1c3c2`. Stamps not regenerated. |

## 3. S026 helicopter capture: Failed twice (same failures)
- heli.log:3837 / heli2.log:3692: `result=Failed reason=flightAssertionsFailed ... failures=airframeFractionNotMeasured measuredRows=0;switchBackRoleNotInfantry aircraftSelected=0 selectedRoles=[Rifle/Infantry] selectedCardRole=[]`.
- The Codex 041 fix works natively: `followApplied bounds=CarriedMeasured distance=58.4` (heli.log:3035) comes before the outbound Move (:3111). The helicopter took off, turned 178°, touched down on the patch (toPatch 0.00) and switched back.
- Frames: selected, mid-turn, approach and touchdown show the whole airframe and rotor, not just the shadow. Heading is readable. The airframe is about 1/8 of the view width. The pad is visible from mid-turn on.
- Camera follow: 34-39 distinct positions, lens distance 58.4-62.0, camera-to-aircraft 60.4-123.4. No `ownAirAboveCamera=1`.
- Switch-back-settled shows a 4-soldier infantry squad and the helicopter deselected. But the squad's card is on another tray page (`selectedOffPage=1`), so the check reads an empty card role.
- `airframeSource=Nominal` on every row. That is probably the stale geometry stamps from step 2 (unverified).
- The old air-01..04 frames were deleted after the new set existed. Committed in 568e4ee96. Attempt-1 frames are kept in lane32/heli1-frames.

## 4. S004 counted runs: 3/3 Victory, queue rc=0, no AriaBlocked
| Job | Result | Time (% of deadline) | Unused units |
|---|---|---|---|
| 104733 (warm-up) | Victory | 199.4 s (18.46%) | apc_fast, attack_heli, car, drone |
| 130367 | Victory | 204.3 s (18.92%) | aa, apc_fast, attack_heli, drone (aa gave_up t=104.9, tried=8) |
| 155925 | Victory | 386.9 s (35.82%) | attack_heli, drone |
- MenuJourney and the terminal probe (MAIN MENU) passed on all three jobs.
- transport_heli was ordered at t≈21 and apc_armored at t≈34. Radar was ordered in every job.
- No `[AriaObjective] event=coverage` line was logged.

## 5. S002 captures (committed fbdbae672)
- **Opening, en and fa-IR:** composed and captured poses now match (yaw 65/65, ground 996.9,308.4 vs 996.4,308.5; s002-en.log:8679, s002-fa.log:5878).
  - The Barracks are centred and clear of the HUD, with infantry squads beside them.
  - No tank or tank approach is in view.
- **Alert focus:** `impactRingVisible=1 shotLineVisible=0 homeInPlayArea=1 attackerInPlayArea=0` (s002-alert.log:48279).
  - The base is at the lower-left edge, partly under the tray. An impact glow is visible.
  - The attacker is not in view.
  - The alert rows fit their text.

## Open
- Stale aircraft dependency stamps: these need regeneration via CreateAdditionalAircraftMetadata in staging (Codex 040). They probably cause the S026 `airframeFractionNotMeasured` failure.
- S026 switch-back check: the selected card is off-page.
- S002 alert framing leaves the attacker out of view.
- S004 130367: the aa support order gave up.
