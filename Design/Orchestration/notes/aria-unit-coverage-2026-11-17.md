# ARIA unit coverage, S002 (gate 3), 2026-11-17

Evidence: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lead-s002-gate3/unity.log` (`[SkirmishGateUnits]`),
actor trace `Design/AgentReports/SkirmishExpansion/S002/_Evidence/candidates/a2279367…/s002-aria-rs-104731-en-1.jsonl.actors.jsonl`.

## 1. Corrected "available" model
- `role.convoy_logic` is not available in S002. Army profile G (`Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishArmy_GroundManeuver.asset`
  `allowedRoleIds`) does not list it, so `SkirmishProductionCatalog.Create` (`profile.Allows(overlay.RoleId)`) never adds a
  convoy recipe; S002 Standard has `player_objective_trucks=0` (INITIAL_SETUP_MATRIX). It appeared only because the gate
  model resolves roles by runtime prefab key, and `SkirmishRoleCatalogConfig.RuntimePrefabKey` maps both ConvoyLogic and
  LogisticsTruck to `Unit_Veh_Truck_Tray`. Fixed: a role is listed only when the catalog overlay's own RoleKind matches.
- `role.logistics_truck` (2) and `role.tanker` (1) are in the player's starting roster (`player_logistics_support=3`) and are
  profile-allowed and producible. They haul Oil/Fuel on their own (`UnitResourceHaulOrder`, haul phases 1–4 in the 104731
  actor trace) with no player order. They are available, but their use is automatic. The model now reports them as
  `automatic=` when a player unit of that role is seen hauling (2 s poll), and they are no longer counted as `unused`.
- Corrected S002 Standard list: rifle, gunner, rocketeer, car, apc_armored, tank (starting, assault), logistics_truck,
  tanker (starting, support/automatic), plus producible: marksman, breacher (Barracks), apc_fast, radar (GroundStaging; radar
  needs readiness Established, and S002 starts at readiness 2). No producer structure is missing at start.

## 2. Why each genuinely available type is unused, and the smallest use
| Type | Cause | Smallest human-plausible use |
|---|---|---|
| car, apc_armored, tank | Fielded on roster page 1. Used in 130365. In 104731 the NEXT tray swipe overshot page 1 to page 2, and PREV went back to page 0; the loop repeated from t≈55 to 82 (gestures 55.6/59.1/62.2/65.9/69.0/…). Page 1 was never ordered. | Page by exactly one page: a shorter tray drag (one card-page width), or tap the tray's page arrow if one is presented. On overshoot, step back one page with the same short drag. Needs a Unity check of the tray scroll. |
| marksman | Never produced. ARIA's S002 production is rifles (`FallbackGroundRecruit`, infantry < 16), rocketeers (only after the structure force is lost), AA (hostile air) and trucks (air profile only). | After the assault is issued and no recruit is pending, queue one marksman squad when affordable. The existing periodic page inspection and re-sweep send the new group toward the base. |
| breacher | Never produced (same as marksman). Breacher is structure-capable (`IsStructureAssaultRole`). | Same as marksman. Also use it as the structure-force replacement when rocketeers are gone; this would also cover the 155923 fa-IR stall (rifles-only force at the base, 2026-11-17 q1). |
| apc_fast | Never produced (GroundStaging vehicle). | Same recruit step from the Vehicles drawer, once, after the assault order. |
| radar | Never produced, and not an assault role (`SkirmishExpandedPresentedOrders.IsAssaultRole` excludes Radar), so the sweep would never order it. | Produce one radar after the assault order, then one ordinary order: select its card, then Move or AttackMove onto ground behind the column. Its detector widens public sight (fog), which also helps spot slipping vehicles. |

Not implemented (not small or isolated): items 2–5 need new drawer targets (`ObserveCatalogTarget` with the role prefabs),
affordability and pending flags in `SkirmishAriaPublicProjection`, observation and adapter fields, and a policy step. Radar
also needs a non-assault order path. The paging fix needs Unity to verify the tray scroll distance. Suggested order: paging
fix (restores fielded vehicles), then breacher (structure value), then marksman and apc_fast, then radar. One recruit at a
time, only after `AssaultIssued`, so the opening and the first assault are not delayed.
