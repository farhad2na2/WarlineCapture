# WarlineCapture Match Unit Command Behavior Spec

Date: 2026-06-15

This is the child implementation contract for selected-unit `HOLD`, `STOP`, and `SCAN` behavior in `SCN-08 RTS Battle HUD`.

Parent sources:

- `Match_HUD_And_Gameplay_Implementation_Spec.md` owns the visible HUD controls, feedback, command bar, command wheel, minimap, and match-screen state machine.
- `Match_Selection_Implementation_Spec.md` owns selection state, selected-unit capability projection, input suppression, and command targeting state.
- `Design/Architecture/hold_stop_scan_commands_implementation_plan.md` tracks the implementation phases.

This file owns the gameplay meaning of the commands. Parent specs may reference this document, but they must not contradict it.

## Architecture Contract

- UI buttons must request commands through `SelectionUiCommandSystem` and `ISelectionUiCommand`.
- Command intent must flow through ECS request data such as `RtsSelectionCommandIntentRequestElement`.
- Pointer/camera/targeting state must stay in `RtsSelectionRuntimeInputSystem` and `RtsSelectionInputStateComponent`.
- Hold and Stop selected-unit mutations belong to `FocusedUnitCommandSystem`, `RtsSelectionImmediateSelectedUnitCommandSystem`, or narrower command-specific ECS systems.
- Scan target-mode entry belongs to `RtsSelectionScanTargetModeCommandSystem`.
- Scan execution, scan patrol state, intel reveal, scan feed, and scan results belong to `ScanIntelCommandSystem` or narrower scan/intel command systems.
- HUD command mode/result feedback must be published through `SelectionHudFeedbackBoundary` and ECS feedback data.
- Prefer `ISystem`, Burst, jobs, data-only components, and `EntityCommandBuffer` for runtime command behavior.
- Do not introduce broad command managers, gameplay facades, UI-owned gameplay state, or direct child UI path writes.
- Do not change pathfinding constants or traversal rules as part of command behavior work.

## Shared Terms

| Term | Meaning |
|---|---|
| Hold anchor | The current world/cell position captured when Hold is accepted. |
| Hold leash | The maximum distance a holding unit may move away from its hold anchor while defending or retaliating. |
| Scan area | The player-selected world/cell target and radius for scan behavior. |
| Scan source | The selected scan-capable unit, selected building/source, or mission/faction global scan source that owns a scan. |
| Recon patrol | A selected-unit scan order where the unit moves through or around the scan area, reveals intel, and may engage enemies under policy. |
| Global scan | A mission/faction intel pulse that reveals at the target area without moving a selected unit and without attacking. |

## Source Priority For Scan

When the player presses `SCAN`, choose the source in this order:

1. If the current selection contains a scan-capable focused/primary unit, use that unit as the scan source.
2. If multiple selected units are scan-capable, issue the scan to eligible scan-capable units and report partial success for ineligible units.
3. If no selected scanner exists but a selected building/source can scan, use that source.
4. If no selected scan source exists and the mission/faction allows global tactical scan, use global scan.
5. Otherwise reject with `UnitCannotScan`, `ScanUnavailable`, or the closest typed reason exposed by the current command contract.

Global scan must not pretend that a selected non-scan unit performed the scan.

## Stop Command

`STOP` is an immediate selected-unit command. It cancels interruptible current orders and places each selected unit in its safest valid post-command state.

### Shared Stop Behavior

- Requires at least one selected eligible unit.
- Clears active command targeting mode and queued move state.
- Keeps the unit selected.
- Clears interruptible movement/path/attack/patrol/scan orders.
- Publishes command feedback through the HUD feedback queue.
- Rejects with `NoSelection` when nothing eligible is selected.
- Rejects with `NoStoppableOrder` or `CommandUnavailable` when selected units cannot accept Stop.

### Stop By Unit Type

| Unit Type | Stop Result |
|---|---|
| Soldier / infantry | Stop current movement and attack orders. Remain at current valid position. May return to idle or hold-neutral posture. |
| Ground vehicle | Stop current movement and attack orders. Reset vehicle kinematic speed/stall state. Remain at current valid position. |
| Helicopter / hover aircraft | Stop current movement order and hover/loiter at the current safe position if not in a locked landing/takeoff phase. |
| Fixed-wing jet, airborne | Fixed-wing aircraft do not freeze in the air. Stop means abort interruptible order, egress safely, and return to base/staging/landing profile. |
| Fixed-wing jet, landed idle | No movement needed. Clear interruptible pending orders and remain staged/landed. |
| Drone / UAV | Hover/loiter if capable; otherwise follow its authored safe stop profile. |
| Building / static source | Stop only applies if the building has an interruptible active ability, production action, turret order, or scan/support action. Otherwise reject/no-op according to UI policy. |

### Stop Acceptance Criteria

- Stop does not deselect units.
- Stop does not leave stale move, attack, scan, or support targeting mode active.
- Stop does not strand fixed-wing aircraft in invalid airborne idle state.
- Stop never teleports a unit.
- Non-interruptible takeoff, landing, extraction, deployment, or emergency states may reject Stop with `CommandLockedBySafetyPhase`.

## Hold Command

`HOLD` is an immediate selected-unit command. It anchors the unit near its current position and enables defensive fire. Hold is not a passive "stand there and die" state.

### Shared Hold Behavior

- Requires at least one selected eligible unit.
- Clears active movement/path/patrol/scan orders.
- Captures or refreshes a hold anchor.
- Applies a hold leash appropriate to the unit type.
- Keeps attack-capable units ready to fire defensively.
- Allows retaliation when attacked.
- Allows engagement when an enemy enters the unit's defensive range.
- Prevents unrestricted chase outside the hold leash.
- Keeps the unit selected.
- Publishes `HOLD POSITION` feedback and selected-unit status.

### Hold By Unit Type

| Unit Type | Hold Result |
|---|---|
| Soldier / infantry | Hold current ground anchor. Fire at enemies inside range. Retaliate if attacked. Do not chase beyond leash. |
| Ground vehicle | Hold current ground anchor. Rotate/turret-aim and fire at enemies inside range. Do not chase beyond leash. |
| Helicopter / hover aircraft | Hover or loiter around current anchor. Engage enemies inside defensive range. Do not chase beyond leash. |
| Fixed-wing jet, airborne | Loiter/orbit around current anchor if the aircraft has an authored loiter profile. Engage only targets allowed by hold policy and weapon readiness. If no loiter profile exists, reject Hold or convert to return/staging according to aircraft config. |
| Fixed-wing jet, landed idle | Remain staged/landed. Do not take off just because Hold was pressed. If attacked and the aircraft cannot fire from ground, an authored defensive scramble profile may take off and defend inside the hold area; otherwise it remains staged and relies on base defense. |
| Drone / UAV | Loiter around current anchor and engage detected enemies according to role/policy. |
| Building / static source | Hold keeps turret/sensor/source active within its local defensive arc/radius if applicable. |

### Hold Engagement Rules

- A holding unit may attack enemies already in range.
- A holding unit may fire back when attacked.
- A holding unit may move slightly for facing, formation, obstacle correction, or minimum weapon distance if the movement stays inside leash.
- A holding unit must not convert defensive fire into an unrestricted chase.
- Civilian-risk and target-confidence gates still apply.

### Hold Acceptance Criteria

- Hold is visually and mechanically distinct from Stop.
- Hold keeps units defensive and alive when threatened.
- Hold does not trigger scan patrol, runway takeoff, or movement to a new target area.
- Hold is refreshed by pressing Hold again unless a later design explicitly makes Hold a toggle.

## Scan Command

`SCAN` is an active recon/intel command. It is not the same as Hold.

- Hold means: guard this anchor and defend locally.
- Scan means: search the selected area, reveal contacts, and engage detected enemies according to scan policy.

### Shared Scan Behavior

- Pressing `SCAN` enters scan targeting mode when a valid scan source exists.
- Camera panning remains available while scan targeting is active.
- The UI click that opens Scan must not leak into a world scan.
- A valid world tap issues the scan to the chosen scan source.
- Invalid target rejects with typed feedback.
- Scan reveals eligible hidden/unknown/suspect units, buildings, hazards, traps, patrol hints, objective clues, or civilian-risk information.
- Accepted scan emits clear world/minimap/intel feedback.
- Accepted scan spends cooldown, charges, or resources only after target validation succeeds.
- One-shot scan targeting clears after an accepted scan order unless repeat scan is explicitly configured.

### Global Scan Behavior

Global scan is an intel pulse, not a unit order.

- It resolves at the tapped scan area.
- It does not move any selected unit.
- It does not attack or auto-engage.
- It reveals contacts and updates intel/minimap/feed according to scan rules.

### Selected-Unit Scan Behavior

Selected-unit scan is a recon patrol/order.

- The selected scan-capable unit moves through, around, or over the scan area.
- The scan source reveals contacts inside its scan radius or along its recon path.
- The scan source may engage enemies it finds, but only under its scan engagement policy.
- Scan engagement is bounded to the scan area/order and must not become unrestricted chase.
- Scan should not fire at low-confidence or high-civilian-risk targets unless the unit/profile explicitly allows it.
- If the scan source is attacked, it may defend itself.

### Scan By Unit Type

| Unit Type | Scan Result |
|---|---|
| Soldier / scout infantry | Move/sweep within the target area using normal movement. Reveal contacts inside sweep radius. Engage found enemies in range according to infantry policy. |
| Ground vehicle / radar vehicle | Move/patrol or sensor-sweep the target area. Reveal contacts in sensor radius. Engage found enemies if weapon role allows. |
| Helicopter | Fly/orbit the scan area. Reveal contacts below/near orbit. Engage found enemies if weapon role and civilian-risk policy allow. |
| Fixed-wing jet, landed idle | Taxi/take off through the runway flow, fly a recon pass over/near the scan area, reveal contacts, engage detected enemies if scan engagement policy allows, then return/land/stage when complete. |
| Fixed-wing jet, airborne | Divert to a recon pass over/near the scan area, reveal contacts, engage detected enemies if policy allows, then return/land/stage or continue patrol according to config. |
| Drone / UAV | Take off or move to the scan area, loiter/patrol, reveal contacts, engage if armed and policy allows, then return/continue according to config. |
| Building / radar tower | Pulse/reveal inside target/radius if the building/source has scan range and mission rules allow it. No movement. Attack only if it has a separate turret/weapon policy. |

### Scan Engagement Rules

- Global scan never attacks.
- Selected-unit scan may attack if the unit is armed, target confidence is high enough, civilian risk is acceptable, the target is inside scan/order bounds, and the weapon is ready.
- If an enemy attacks the scanning unit, the scanning unit may defend itself.
- Scan engagement must not drag the unit far outside the scan area unless the player gives an explicit Attack order.
- If the scan target flees outside the scan area, the scanning unit marks/tracks it but does not chase indefinitely by default.
- A scan-capable jet/drone may attack during the recon pass only when facing/position/weapon requirements are valid.

### Scan Completion

The scan order completes when one of these occurs:

- The configured scan duration ends.
- The scan path/pass completes.
- The scan source returns and lands/stages, if configured.
- The player issues Stop, Hold, Move, Attack, Return, or another interrupting command.
- The scan source is destroyed, disabled, or enters a locked safety state.

### Scan Acceptance Criteria

- Selecting a scan-capable jet and pressing Scan produces aircraft recon behavior, not a silent global reveal.
- A landed idle jet uses runway takeoff before scanning and returns/lands/stages when complete.
- An airborne jet scans by flying/reconning the target area, not by landing first.
- Scan-capable units engage detected enemies under policy.
- Non-scan units do not pretend to scan unless a global/faction scan source is available.

## Mixed Selection Rules

- If the selected group contains both scan-capable and non-scan units, only scan-capable units receive selected-unit Scan.
- If at least one selected unit accepts Scan and others cannot, return partial success feedback.
- If no selected unit can Scan and no global/faction scan is available, reject with `UnitCannotScan` or `ScanUnavailable`.
- Hold and Stop apply to every selected eligible unit; ineligible units return partial feedback.
- Mixed air/ground Hold must use each unit's own hold profile.
- Mixed air/ground Stop must use each unit's own safe stop profile.

## HUD Status Text Guidance

Use short tactical status text:

- Stop: `STOPPING`, `IDLE`, `RETURNING`, `LANDING`, `STAGED`
- Hold: `HOLDING`, `GUARDING`, `HOVERING`, `LOITERING`
- Scan: `SCANNING`, `RECON PASS`, `PATROLLING`, `RETURNING`, `LANDING`

Fixed-wing aircraft should never show `STOPPING` while frozen in the air. If Stop aborts its order, show `RETURNING`, `LANDING`, or `STAGED`.

## Validation Checklist

- Hold soldier: holds anchor, fires back, does not chase far.
- Hold vehicle: holds anchor, fires/turret-aims, does not chase far.
- Hold airborne jet: loiters if profile exists or rejects/returns by profile; does not freeze invalidly.
- Hold landed jet: remains staged unless authored defensive scramble is triggered.
- Stop soldier/vehicle: stops movement and clears path/order state.
- Stop airborne jet: aborts interruptible order and returns/stages safely.
- Scan global: reveals only; no unit movement or attack.
- Scan selected landed jet: takeoff, recon pass, reveal, bounded engagement, return/land.
- Scan selected airborne jet: recon area, reveal, bounded engagement, return/continue by config.
- Scan selected drone/helicopter: orbit/patrol, reveal, bounded engagement.
- Scan selected ground scanner: patrol/sweep, reveal, bounded engagement.
- Mixed selection: partial success/reject feedback is typed and visible.
