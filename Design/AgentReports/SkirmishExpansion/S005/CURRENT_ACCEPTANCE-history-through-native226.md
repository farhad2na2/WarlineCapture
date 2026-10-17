Current candidate update: `cc925f0db2e59eb48d8fce71138a0df1a0bab3bd23389ba922e3cb1a899adc70`; Editor-only manual release observation and strict finish audit corrected. Native226 is running. Native220–225 are complete 6/6 passes on source3986 and remain historical for this new source. Game rules and visual assets did not change.

# S005 current acceptance

S005 remains **InProgress**, `playerReady=false`, and locked in the packaged catalog. The current frozen source is `3986ae0d0abf89331a6eb6c464cf4897c5fdffa1f2ea82e47a16905f1a242030`; an independent census confirms all 38,010 source files. Existing Campaign CityEdgeAirfield geometry, gameplay budgets and the match deadline are unchanged.

| Gate | Current candidate evidence | Scope |
|---|---|---|
| Boarded APC Save/Continue and manual win | **Passed Native212**, [evidence](native212-boarded-apc-save-resume-evidence.json) | Native logistics truck, R2 research, heavy APC delivery, boarding, Save & Exit, Continue and unloading; two saved passengers restored with typed links and hidden visuals, then unloaded; 386 old physical entities removed; paid receipt preserved once; paid Rocketeer replacement; Victory at 619.704 seconds, frozen result and Main Menu. No human/device acceptance |
| Shared runtime/production checks | **Passed Focused213**, [log](tests-213-frozen-shared-materials.log) | Shared materials, movement, production, army, economy, checkpoint, ARIA and touch checks. Missing/dead/enemy-owned producers remain unavailable and report the required building |
| Fresh ARIA baseline | **Running Native220–225** | Three Regular/Standard seeds in each of English and Persian, against this exact source. No historical wins included; each requires normal input/speed, zero violations, Victory, frozen result, native return and wrapper receipt |
| Final native visual review | Pending owner review | Existing approved dense direction; fixed lighting; shared visual changes apply to every mission consuming this Campaign source |
| Physical device and unfamiliar players | Pending | No attached Android device; Editor automation does not certify phone touch, thermal performance or human usability |

Earlier source evidence is retained as historical: Native188–193 fresh bilingual baseline; 187/194 quiet Save/Continue; 203/206 combat recovery; 195 manual Persian win; 196 camera/no-orders; 197 paid light-helicopter flight/Return/refuel/second sortie; focused objective/recovery/configuration checks. Their individual reports pin their source and cannot count as current candidate wins. Failed Native207–211 evidence remains preserved. See [history](CURRENT_ACCEPTANCE-history-through-native211.md).

Remaining certification includes all enabled Combined Arms role lifecycles, bilateral aircraft clearance/capacity, native active missile/drop recovery, Support and normal-input objective edges, additional exposed difficulty/size journeys and the required stress plan. Native212 closes boarded passenger recovery; it does not certify all these other capabilities.

Shared scenery includes ground variation, palms, details, courtyards, separating walls and roof canopies. A prior independent comparison verified 21 pinned Campaign geometry assets unchanged. Lighting remains fixed for the short match. No mission-owned physical map derivative was added.

Native214 reached Victory/result/return, but the Editor exited with signal9 before writing the completion receipt. It counts zero wins; [failure](native214-editor-exit-failure.json) and scene backup are preserved. The same candidate is being retried after a clean wrapper-driven Editor reopen.

**Native212 audit limitation found during follow-up:** the recovery/boarding/unload stream passed its exact command audit, and the resumed mission reached native Victory/result/return, but the subsequent manual touch driver was not connected to command receipt observation. Its manual finish branch did not enforce that audit. The full resumed manual certification remains pending; an Editor recorder correction and native rerun are required. No gameplay outcome or source rule was changed.
