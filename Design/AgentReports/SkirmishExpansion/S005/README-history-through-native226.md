Current candidate update: `cc925f0db2e59eb48d8fce71138a0df1a0bab3bd23389ba922e3cb1a899adc70`; Editor-only manual release observation and strict finish audit corrected. Native226 is running. Native220–225 are complete 6/6 passes on source3986 and remain historical for this new source. Game rules and visual assets did not change.

# S005 validation evidence

S005 remains **InProgress**, `playerReady=false`, and locked in the shipped catalog. [Current acceptance](CURRENT_ACCEPTANCE.md) is the concise status record.

Frozen source: `3986ae0d0abf89331a6eb6c464cf4897c5fdffa1f2ea82e47a16905f1a242030`.

- [Native212](native212-boarded-apc-save-resume-evidence.json) passed normal paid logistics/R2/heavy APC production, boarding, Save/Continue, typed passenger recovery and unloading, paid replacement recruitment, resumed manual Victory, frozen result and Main Menu return. Wrapper and completion receipt are zero; no injected game state or human intervention.
- [Focused213](tests-213-frozen-shared-materials.log) passed shared production/material/movement/ARIA/checkpoint checks.
- [Fresh bilingual baseline](frozen-212-baseline-plan.json) is running Native220–225, three seeds per locale; historical wins are excluded.

Existing Campaign CityEdgeAirfield is reused. Owner-authorized shared art changes affect all consumers, with ground variation, props, palms, courtyards, separating walls and fixed daylight. Mission follow-up probes did not change physical geometry, prices, caps, starting resources or match deadlines.

Earlier source passes and all failures remain intact. [History through Native211](README-history-through-native211.md) includes the earlier passenger-count, economy, target-domain and drawer-strategy failures. Their sources and acceptance scopes remain separately recorded.

Complete role lifecycles, bilateral aircraft clearance/capacity, active native projectile/drop recovery, Support, additional configuration/stress journeys, owner final visual review and physical device/unfamiliar-player acceptance remain pending. An Editor touch pass is separate from actual phone/player acceptance.

Native214 reached Victory/result/return, but the Editor exited with signal9 before writing the completion receipt. It counts zero wins; [failure](native214-editor-exit-failure.json) and scene backup are preserved. The same candidate is being retried after a clean wrapper-driven Editor reopen.

**Native212 audit limitation found during follow-up:** the recovery/boarding/unload stream passed its exact command audit, and the resumed mission reached native Victory/result/return, but the subsequent manual touch driver was not connected to command receipt observation. Its manual finish branch did not enforce that audit. The full resumed manual certification remains pending; an Editor recorder correction and native rerun are required. No gameplay outcome or source rule was changed.
