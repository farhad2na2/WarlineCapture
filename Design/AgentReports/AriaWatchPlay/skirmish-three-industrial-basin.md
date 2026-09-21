# Skirmish 3 — Industrial Basin (S073)

English Watch run on seed 104729, scenario index 3. Placement audit passed (10/10 buildings, fail=None). Focused checks: AriaSkirmishPlanValidation 101, SkirmishScenarioValidation 26.

ARIA used the shipping touch driver after the approved API start. The match ended as a time-limit draw at 900s: player units lost 8, enemy units lost 1, no buildings destroyed, 155 completed gestures, result saved.

Planner fixes that landed before this run:

- Opening assembly no longer times out as a stuck tap loop before the assault deadline.
- Nearby on-screen contacts are contested during opening and rebuild instead of leaving the army idle.
- Industrial Basin uses the base-relative approach, not City Crossroads’ wide eastern flank.
- Advance map navigation waits out the settle window instead of reopening the map every second.

The force still never reached the southeast base. Enemy health stayed at the opening value after one early unit loss, and rebuilt squads were wiped at the northwest yard. Alternate building pads closer to the enemy failed the placement audit, so the proven northwest/southeast sites remain.

Evidence: `/private/tmp/warline-s3-aria/aria-s3-ib-en-result.json`.
