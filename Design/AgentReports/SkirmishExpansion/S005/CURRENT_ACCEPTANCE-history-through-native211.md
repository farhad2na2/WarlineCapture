# S005 current acceptance

S005 remains **InProgress**, with `playerReady=false`; the packaged mission is locked. Current source is `3986ae0d0abf89331a6eb6c464cf4897c5fdffa1f2ea82e47a16905f1a242030`. Native212 is running logistics-first passenger recovery and a drawer-aware native replacement strategy. Native206 passed on `ec23a5d3bdaf8754570d1967fd7fd8304322f651089e9f13a23a66ebfc49ecfe`, including the shared missing-producer feedback fix. Previous-source runs below retain their exact source in their evidence and are historical, rather than counted wins on this new candidate. Fixed budgets and physical geometry are unchanged.

| Gate | Current evidence | Scope |
|---|---|---|
| Fresh ARIA baseline | Passed, native188–193; `fresh-baseline-acceptance.json` | Six complete normal-speed Standard/Regular missions, three seeds each in English and Persian; Victory, frozen result and native return |
| Player Save/Continue | Passed, native187 FA and native194 EN; `player-save-resume-acceptance.json` | Same attempt and clock, paid recruitment receipt preserved once, 386 old battlefield entities removed; resumed mission, Victory, frozen result and return. Quiet paid queue only |
| In-combat Save/Continue | Passed, native203; `native203-combat-save-resume-evidence.json` | Native recruitment and 15 verified Attack orders; observed enemy-base damage; same attempt/clock/receipt restored, 378 old entities removed, zero command receipt violations; resumed Victory, frozen result and Main Menu. No active-projectile claim |
| Heavy APC | Passed, native206; `native206-heavy-apc-combat-save-resume-evidence.json` | Native producer defense, paid R2 and APC delivery, combat Save/Continue, resumed Victory/result/return; boarding and firing not certified by this run |
| Boarded passenger recovery | Native208 transport subgate passed; full mission failed. Native212 running | Three paused passengers restored/hidden/linked, natively unloaded and enabled; old physical world removed and paid receipt preserved. Failed frontal strategy did not recruit replacements |
| Producer-loss feedback | Focused205 passed | Missing friendly ground producer reports its required building, rather than army capacity; enemy-owned and destroyed producers remain unavailable |
| Replay | Passed, native188 | Native Replay; old battlefield removed and starting resources restored |
| ARIA-off controls | Passed, native195 | Automated native Touch squad selection and Attack; Victory, frozen result and Main Menu return. No human acceptance claimed |
| Camera controls | Passed, native196 | Selected squad with armed Attack mode; native map camera focus without troop orders |
| ARIA Stop/restart | Native control observations, native200 | Stop yielded and Play/Confirm restarted. Full run failed recorder quota; planned Stop also counted as takeover. Zero counted wins; failure preserved |
| Paid light helicopter lifecycle | Passed, native197; `native197-aircraft-cycle-evidence.json` | Paid readiness, helipad, logistics, helicopter delivery, flight, Return, touchdown, tanker refuel and second sortie |
| Objective edge behavior | Focused fixtures passed, tests198 | Original designated bases, surrender, simultaneous base destruction, paused clock and frozen terminal state; separate from normal-input edge journeys |
| Role/configuration legality | Focused checks passed, tests182 | 22 configured roles and 12 size/difficulty vectors; does not certify their complete native lifecycles |
| Checkpoint reconstruction | Focused checks passed, tests180 and183 | Schema 5 codec, actors, structures, typed references and transient whitelist; active native projectile/drop/passenger evidence remains pending |
| Shared-map visuals | Assistant native-screen QA performed | Existing Campaign CityEdgeAirfield; shared ground variation, 179 palms, 1191 detail placements, 9 courtyards/121 walls and 32 roof canopies. Owner final native-screen acceptance remains pending |
| Real device and human acceptance | Pending | No Android device connected; Editor Touch automation is separate from physical touch and unfamiliar-player acceptance |

Existing Campaign geometry is reused. The independent comparison confirms 21 pinned definition/grid/surface/ground assets unchanged. Shared scenery updates apply to every consumer of this Campaign source. Lighting stays fixed during the short match.

Remaining local certification includes complete Combined Arms role lifecycles, bilateral aircraft clearance/capacity, active native missile/drop and boarded-passenger recovery, supported global Support state and normal-input objective edge journeys. Additional sizes/difficulties need their own acceptance before exposure. Compilation, configuration fixtures and historical wins do not close those gates.

Failed evidence is preserved. See `README.md` and individual logs for history. Current actual map view: `native-192-opening-en.png`.

## Native206 and passenger follow-up

Native206 passed native R2 research, five-group producer defense, heavy APC purchase/delivery (280 materials), combat Save & Exit/Continue, resumed Victory, frozen result and MAIN MENU. Its source is `ec23a5d3bdaf8754570d1967fd7fd8304322f651089e9f13a23a66ebfc49ecfe`; log and wrapper receipt passed. [Evidence](native206-heavy-apc-combat-save-resume-evidence.json). Focused205 passed producer-loss feedback and existing shared production/material checks.

The current source `3986ae0d0abf89331a6eb6c464cf4897c5fdffa1f2ea82e47a16905f1a242030` adds an Editor-only boarded APC recovery/unload probe. Native206 is retained as historical evidence, not a win on this new candidate. Native207 failed an earlier moving-world count assertion (one observed; three present in the actual paused save). Native208 records the paused count and is running; no boarding acceptance is claimed yet. Full role/source-fit coverage, active projectile/drop recovery and real player/device acceptance remain pending.

Native208 passed passenger recovery/unload but failed the resumed manual mission after losing all infantry. The only survivor was a healthy heavy APC without a structure target domain. Its test-only deadline was shortened after that observation; no game state or outcome was injected. Native209 adds legitimate paid Rocketeer replacement through Build, preserving prices and income. Failed evidence: `native208-passenger-recovery-manual-failure.json`.

Native209 also passed strict passenger recovery/unload and a read-only scale/visibility observation, but its full manual strategy failed: materials50, no living structure counter and workshop `NoOilInput`. Buying the heavy APC before logistics exhausted the finite workshop input. Native210 buys the 100-Materials logistics truck first, then performs the same legal research/recruitment/recovery and paid counter replacement. Runtime economy, prices, starting resources and match deadline remain unchanged.

Native210 passed logistics delivery, strict passenger recovery/unload and continued normal fabrication after Continue. Its full manual strategy failed by repeatedly attempting an unsupported APC attack on the Structure base, preventing its replacement cycle. Native211 uses only the visible Structure-capable squad mask for base attacks and reacts to loss of the last such squad; it still buys replacements through real Build touches. No game target domains or other runtime rules changed.

Native211 passed strict passenger recovery/unload, then its manual fixture stalled because it required the squad tray and command bar while Build deliberately hid them. Native212 processes the current recruitment drawer before requiring army controls. Only Editor test strategy code changed; game UI/rules are unchanged. Failed evidence: `native211-passenger-recovery-drawer-strategy-failure.json`.
