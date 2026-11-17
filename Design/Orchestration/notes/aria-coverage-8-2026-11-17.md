# ARIA coverage-8: research, siege and producer buildings (lead decision, 2026-11-17)

Stacked on coverage-7 (ec329d0e2). Lead decision (within ARIA scope, no gate change): implement the coverage-7 plan.

- **Research** (`AriaResearchPolicy`, S002 adapter and generic planner): one readiness research per stage, through the
  ordinary upgrade control (Build, then the drawer readiness button), only when it unlocks something the mission offers and
  ARIA has not used (`ResearchNeedMask`: coverage kinds whose current quote fails only on readiness but pass at Full Arsenal,
  plus missing producer buildings below Established), after the first accepted assault and after the coverage purchases that
  are currently possible (`AriaCoverageProgress.Idle`), and only while materials ≥ research cost + 250 (the reserve). Yields
  to every other owner (home defence first); two attempts per stage; Full Arsenal is the last stage. S049 (Field):
  Established (240) unlocks tank, radar, apc_heavy and the producer buildings; Full Arsenal (480) unlocks siege everywhere.
- **Siege** is coverage kind 9 (bit 256, last in the purchase order); the sweep orders it (assault and structure role).
  Coverage done masks are int now.
- **Producer buildings** (`AriaBuildingPolicy`, generic planner): helipad then satellite dish, once each, when the mission
  offers a role that needs them (`producer.helipad` / `producer.intel_station` in the Full-Arsenal quote) and the player has
  none, affordable above the reserve, after coverage and research. Placement reuses `StepHomePadPlacement` (Home focus, Build
  drawer card — the dish card via `PadCard` —, legal home pad site, confirm); a confirmed preview that closes counts as built.
  No legal site / timeout / home raid: the open preview is cancelled and the attempt released; two attempts per building.
- **Traces:** `[AriaS002Trace] event=research state=started|queued|released|gave_up stage= cost= materials= need=` and
  `event=build kind=helipad|satellite_dish state=started|built|released|gave_up need= present= afford= padStage=`.
- **Guards:** first assault never delayed (all start after the latched first accepted assault); one purchase at a time
  (coverage, breacher, research and buildings each treat the others in flight as Busy); home defence pre-empts all.
- Tests: `AriaResearchBuildingTests` (5).
- Costs: S049 started at 330–500 materials in lane 12, so research there needs ≥ 490 and may simply not happen; siege needs
  ≥ 730 at Established. This is the reserve rule working as intended, not a defect.
