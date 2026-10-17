# S005 player readiness

S005 remains **InProgress** and is not yet player-ready. It reuses the shared Campaign CityEdgeAirfield map. Shared visual improvements apply to every mission using that map.

The latest engineering pass fixes transparent loading-layer input blocking after returning to setup, isolates validation save profiles, adds Save & Exit / Continue feedback and recovery cleanup, and prioritizes a structure-capable force in S005 ARIA recruitment. Checkpoint schema 5 covers native missile and transport visual reconstruction.

Native 177 passed a fresh EN mission at normal speed: Victory, frozen result, native Adjust Setup return, zero receipt violations. It is historical after subsequent source changes.

Native 179 restored the same saved attempt, clock and paid recruitment receipt, removed all 386 old battlefield entities, and reached Victory at 323.4599 seconds with frozen result and native Adjust Setup return. Its overall acceptance **failed** because the initial Save/Continue probe's production touch was outside the ARIA-only receipt observer. It counted zero wins. The fix records and audits this separate native touch stream before starting the ARIA audit; a new run is required.

Failed evidence is preserved in native171 through native176 and native179 reports/logs. Native 176's resumed battle stalled; the ARIA structure-force correction addressed the missing replacement squad. Focused ARIA regressions passed in tests-178.

Native 184 passed the complete audited EN Save/Exit/Continue journey, resumed Victory at 327.369 seconds, frozen result and native Adjust Setup return with zero receipt violations. Native 185 failed because the probe compared logical Persian text against the UI’s shaped presentation glyphs; its failed evidence is preserved. The probe now selects the saved setup’s native Continue control by semantic identity and still dispatches actual Touch input.

Native 187 passed the complete historical-source FA Save/Exit/Continue journey: same attempt, elapsed clock and paid recruitment receipt; all 386 old battlefield entities removed; resumed Victory at 338.8087 seconds; fully localized frozen result and native Adjust Setup return; zero input audit violations. Recovery journeys are recorded separately from the fresh ARIA seed baseline.

Historical baseline source: `080fabd62e1f9def14cd31070d0b11ca1a44819787ec9f2f0c7da0b5feabdc92`. Tests 186 ARIA/harness regressions passed. Tests 180 passed 13 transient checks, tests 182 passed 12 configuration vectors and existing-mission regressions, and tests 183 passed the recovery staging suite before the latest selector/profile-only changes. Final independent source hashing agrees with the native census.

Native 188–193 **passed all six fresh ARIA baseline matches** on source `080fab` at Standard/Regular: three seeds each in EN and FA, all Victory at normal speed, all original player bases intact, zero unexpected input or command receipt violations, frozen results and native returns. Native 188 also passed Replay conservation: 386 old entities removed and starting faction resources restored. See `fresh-baseline-acceptance.json`.

Native 194 passed the complete historical-source EN Save/Exit/Continue journey: same attempt, clock and paid receipt; 386 old battlefield entities removed; Victory at 323.3262 seconds; frozen result and native Adjust Setup return; zero input audit violations. See `player-save-resume-acceptance.json` for the bilingual quiet-save scope.

Native 195 passed the ARIA-off FA mission through native squad cards and Attack controls: Victory at 146.3515 seconds, 84 gestures, 17 verified Attack orders, original player base intact, frozen result and native Main Menu return. It is automated Touch input, not human acceptance. Native 196 passed the selected-squad/armed-Attack camera test with no troop orders added. Tests 198 passed objective edge fixtures; normal-input edge journeys remain separate.

Native 197 passed the paid light-helicopter lifecycle through native Touch: readiness, helipad, logistics, delivery, first sortie, Return, touchdown, tanker refuel (+6 shared fuel, available 307 against reserve 259) and second sortie; 360 accepted Touch samples. See `native197-aircraft-cycle-evidence.json`. This is one aircraft lifecycle, not complete Combined Arms capability certification. Remaining gates include active native missile/drop and boarded-passenger recovery, supported capability lifecycle/bilateral aircraft source fit, device and human acceptance. No Android device is connected. The larger sizes and additional difficulties are not certified by the Standard/Regular baseline. Focused fixtures and historical wins do not satisfy the remaining gates.

Prior detailed history is preserved in README-history-through-native137.md and the individual reports.

Native 199 passed an additional fresh EN Victory/result/return with no interventions; its late Stop parameter did not exercise Stop. Native 200 observed native Stop and Play/Confirm restart, then Victory/frozen result/return, but overall acceptance failed because the recorder’s three run slots were occupied. The planned Stop touch also registered a takeover. It counts zero wins; see native200-stop-restart-failure.json. No audit counter or quota was weakened.

See CURRENT_ACCEPTANCE.md for the concise current gate summary.

Native 202 failed the new combat probe because earlier driver gesture counts were omitted from the probe’s input assertion; failed evidence is preserved. The corrected probe retains every completed gesture and strict receipt equality. Native 203 passed paid AA recruitment, observed base damage from native Attack orders, combat Save/Exit/Continue and resumed Victory/frozen result/Main Menu return: 378 old entities removed, the same attempt/clock/paid receipt preserved, zero receipt violations. Its source is 810abba; subsequent Editor-only heavy APC coverage uses source 15b41d and is being checked by native204. Active missile/drop/passenger coverage is still not claimed.

Native206 passed defended paid heavy APC delivery, in-combat Save/Continue and resumed full Victory/result/return on source `ec23a5d3bdaf8754570d1967fd7fd8304322f651089e9f13a23a66ebfc49ecfe`. Focused205 passed the shared missing-producer feedback fix. Native204 failure is retained: its ground producer was lost before purchase and the UI originally mislabeled the refusal as capacity. Fixed budgets were preserved.

Current candidate: `e6332a314c54f95bb4a18058b3c9e00916c1e3cf0f71d596a63f85d115517581`; native207 is checking native boarded-passenger Save/Continue/unload. See [CURRENT_ACCEPTANCE.md](CURRENT_ACCEPTANCE.md) for current status. Older wins do not count on this candidate.

Native207 failed because it compared a moving-world one-passenger count with the paused save containing three passengers. Native208 corrected that assertion and passed three-passenger recovery, hidden visuals, links and native Exit All; its full manual journey failed after losing all infantry. The failed evidence is retained. Current source `005c7ab72a705ea453e90858882dfb8fd00dd35bafcf2aeb404ba104b9e1709e` adds an Editor-only native paid replacement strategy; native209 is running. No publication or readiness claim has changed.
