# Support Ability Campaign Audit And Recommendation

Date: 2026-09-28

Status: Audit findings retained; recommendation revised on 2026-09-28 into the [Support design and agent build plan](SupportSystem/support_design_and_build_plan.md). The owner approved the four-ability scope and project-asset-based full-screen popup on 2026-09-28; [technical handoff](SupportSystem/IMPLEMENTATION.md) and [ordered tasks](SupportSystem/TASKS.md) now control implementation. No runtime or live balance changes have been made.

## Summary

No built mission teaches or uses match Support, and no Support runtime exists. Revised recommendation: implement and validate the shared runtime with Smoke Screen first, then add Precision Strike, Paratroopers and Supply Drop in stages. Do not retrofit Chapters 1-3. Introduce Support in Chapter 4 as part of the military escalation, teach it in CH04-M03 Split Front, reinforce it in CH04-M04, and combine it in CH04-M05. Keep Support earned, optional for mission success, and never sold.

## Current State

### Runtime

- The HUD `SupportCommand` button is a scan alias. `MatchOverlayCommandInputUiSystemHelper.CommandTabs.cs` enqueues `UiActionKind.Support`, and `UiActionRequestDispatchSystemHelper.cs` maps `Support` / `RightSupport` to `RtsSelectionCommandIntentKind.EnterScanTargetMode`.
- Missions hide or disable the button only through air/transport restrictions (`UiShellEcsGateway.MissionHudRestrictions.cs`, `MatchOverlayCommandControlsView.cs`).
- `SupportAbilityService`, named as the runtime owner in `Combat_Catalog_And_Upgrade_Design.md`, does not exist in code.
- Only `ability.radar_ping` is granted at runtime (`CampaignMissionProgressStore.RadarWarning.cs`, after CH01-M03). No other planned support unlock is wired.
- ARIA cannot use Support. `AssistantCommandIntentKind` contains only ShowRecommendation, SelectEntity, MoveToWorldPosition, AttackEntity, FocusCamera, StopAssistantControl, and CancelPreview.

### Mission Build Status

- Built: CH01-M01 through CH04-M01. None use a support ability.
- In progress: CH04-M02 Steel Push. `Design/AgentReports/CH04M02SteelPush/production_plan.md` states "No custom gameplay button", so Support is excluded by plan.

### Planned Unlocks (current design)

Source: ability availability matrix in `Combat_Catalog_And_Upgrade_Design.md`, backed by `BalanceConfigs/Combat_Balance_Config_v0_1.json`. A mission reward is first usable in the following mission.

| Ability | Unlocked after | First usable |
|---|---|---|
| Radar Ping | CH01-M03 Radar Warning | CH01-M04 |
| Evacuation Corridor | CH01-M04 Airlift | CH01-M05 |
| Field Repair | CH02-M01 Gridlock | CH02-M02 |
| Casualty Stabilize | CH02-M04 Power Relay | CH02-M05 |
| Drone Scan | CH03-M02 Safehouse Sweep | CH03-M03 |
| Breach Charge | CH03-M05 Network Break | CH04-M01 |
| Smoke Screen | CH04-M02 Steel Push | CH04-M03 |
| Precision Strike | CH04-M03 Split Front | CH04-M04 |
| Harbor Scan | CH04-M04 Grounded Signal | CH04-M05 |
| Naval Fire Support | CH04-M05 Armor Break | CH05-M01 |
| Rally Order | CH05-M02 Trust Under Fire | CH05-M03 |
| Supply Drop | CH05-M04 Last Corridor | CH05-M05 |

Precision Strike is configured with 1 charge per mission and a 120-second cooldown, and can hit only visible targets.

Missions that reference support behavior:

- CH04-M05 Armor Break: "ARIA asks permission for any bounded support action."
- CH05-M01 Citywide Alert: "approve or reject one bounded ARIA support action."

### Design Conflicts

- `SagaChapters/Saga_Chapter04_Air_And_Armor.md` forbids unrelated naval rewards in Chapter 4, but the unlock matrix grants Harbor Scan (CH04-M04) and Naval Fire Support (CH04-M05).
- Supply Drop unlocks after CH05-M04, so its first use would be the CH05-M05 finale, which must introduce nothing new.
- The Chapter 1-3 support unlocks are designed but not used by any built mission.
- Players see a Support button that behaves like Scan for 16 missions, which teaches the wrong meaning.

## Revised recommendation: runtime first

Build and validate Support before authoring missions around it. See the [complete design, ability definitions, campaign schedule, agent packets and evidence gates](SupportSystem/support_design_and_build_plan.md).

Recommended roster, delivered in stages:

| Ability | Build stage | Unlock / first use |
|---|---|---|
| Smoke Screen | First complete vertical slice, including real combat effect, native UI and ARIA consent infrastructure | After CH04-M02 / CH04-M03 |
| Precision Strike | Second ability; bounded attack-aircraft strike on one confirmed target | After CH04-M03 / CH04-M04 |
| Paratrooper Reinforcements | Reuse validated transport/descent/spawn systems for one fixed off-map rifle squad | After CH04-M04 / CH04-M05 |
| Supply Drop | Transport plane delivers a collectible crate of 40 tactical Materials; provisional amount | After CH05-M02 / CH05-M03 |

Use a full-screen popup matching Build to select an ability and inspect its details. Close it before battlefield targeting; confirm explicitly after preview. Keep Scan separate and preserve ARIA Play/Stop. The initial small-drawer mockups are superseded.

Do not retrofit Chapters 1–3. CH04-M02 keeps its existing combat and only awards the unlock after Support is ready. CH04-M03 teaches optional Smoke; CH04-M04 introduces optional Strike and retains ordinary transport/parachute gameplay; CH04-M05 combines abilities and provides the first authored Support-consent beat. Supply is taught before the finale. Remove unrelated Harbor Scan/Naval Fire Support rewards; no automatic replacement with extra Strike power. Retain canonical chapter rewards and Protocol Fragments.

Rules: mission success and stars never require Support; normal use is not an assisted penalty; Show Me is camera/highlight guidance only; ARIA needs fresh consent for each exact action. No paid Support power and no extra-charge mercy system in this scope. Deferred abilities and Rally Order need separate future design decisions rather than implicit unlock promises. A wider bomber run is outside this first roster.

## Monetization Recommendation For Support

Do not monetize Support power: no paid charges, paid in-match strikes, or paid early unlocks.

- Selling strikes conflicts with the product rule "No premium strike, revive, or resource can directly satisfy objectives or stars."
- Paid power in a single-player campaign pressures difficulty tuning toward purchase temptation.
- Paid strikes conflict with the recommended free-to-start plus one-time full-game unlock model.
- ARIA's planned CH04-M05 and CH05-M01 consent beats would ask the player to spend a paid resource at the most urgent campaign moments.

Safe monetization: cosmetic aircraft liveries, strike VFX variants that preserve readability, pilot callsigns and radio voice lines, and Support menu card art.

## Plan updates and approval gate

The [agent plan](SupportSystem/support_design_and_build_plan.md) now defines S0–S8: approval and authority synchronization, Smoke foundation, full-screen native UI, ARIA, campaign integration, Strike, Paratroopers, Supply, and acceptance evidence. Its S0 lists every canonical design/config file to synchronize after approval. Do not treat the old unlock matrix above as the approved future schedule; it records the audited current design.

Owner approval was recorded on 2026-09-28 for the [full-screen Support popup](SupportSystem/Mockups/05-support-fullscreen-v02-project-assets.png), [Smoke targeting](SupportSystem/Mockups/02-smoke-preview-v02.png), [ARIA consent](SupportSystem/Mockups/03-aria-consent-v01.png), and four-ability scope. Native visual review, automated runtime checks, normal-input journeys and real-player/device acceptance remain separate pending gates.
