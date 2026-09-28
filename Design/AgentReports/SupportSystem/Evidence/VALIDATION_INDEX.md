# Support validation index

Focused cases verify runtime contracts; native journeys, visual review, real-player and device acceptance are independent. Failed logs remain in this directory.

| Full log | Required pass evidence | Exit receipt |
|---|---|---|
| [support-config-focused-20260928-03.log](support-config-focused-20260928-03.log) | `[SupportConfigValidation] result=Passed tests=2` | 0 |
| [support-runtime-focused-20260928-10.log](support-runtime-focused-20260928-10.log) | `[SupportRuntimeValidation] result=Passed tests=5` | 0 |
| [support-smoke-focused-20260928-04.log](support-smoke-focused-20260928-04.log) | `[SupportSmokeValidation] result=Passed tests=4` | 0 |
| [support-ui-aria-focused-20260928-04.log](support-ui-aria-focused-20260928-04.log) | `[SupportUiAriaValidation] result=Passed tests=7` | 0 |
| [support-strike-focused-20260928-08.log](support-strike-focused-20260928-08.log) | `[SupportStrikeValidation] result=Passed tests=10` | 0 |
| [support-paratrooper-focused-20260928-12.log](support-paratrooper-focused-20260928-12.log) | `[SupportParatrooperValidation] result=Passed tests=9` | 0 |
| [support-supply-focused-20260928-06.log](support-supply-focused-20260928-06.log) | `[SupportSupplyValidation] result=Passed tests=9` | 0 |
| [support-campaign-focused-20260928-03.log](support-campaign-focused-20260928-03.log) | `[SupportCampaignValidation] result=Passed tests=6` | 0 |
| [support-mission-integration-20260928-01.log](support-mission-integration-20260928-01.log) | `[SupportMissionIntegrationValidation] result=Passed tests=4` | 0 |
| [support-fuel-regression-20260928-03.log](support-fuel-regression-20260928-03.log) | `[VehicleFuelConsumptionFocusedValidation] result=Passed tests=7` | 0 |
| [support-transport-regression-20260928-07.log](support-transport-regression-20260928-07.log) | `[UnitTransportValidation] result=Passed tests=88` | 0 |
| [support-move-regression-20260928-01.log](support-move-regression-20260928-01.log) | `[UnitMoveOrderFocusedValidation] result=Passed tests=21` | 0 |
| [support-path-regression-20260928-01.log](support-path-regression-20260928-01.log) | `[UnitPathfindingFocusedPerformanceValidation] result=Passed tests=3` | 0 |

## Repository architecture failures

- Assembly boundary 02: existing `Game.UI.Runtime` references `Game.Components` in HEAD.
- Hotpath 03: 37 baseline entity/component snapshots against ceiling 0. Support adds no such snapshot site after its request-time chunk conversion.
- Classification 02: nine pre-existing systems; no remaining Support finding.
- No ceiling/assertion was weakened. These failures remain in their complete logs and nonzero receipts.

## Native and external gates

All rows below have the actual `[SupportSmokeJourney] result=Passed` marker and exit receipt 0. The wrappers completed with exit 0; earlier failures remain retained.

| Journey | Full log | Native captures |
|---|---|---|
| Smoke English final | [final](support-smoke-final-20260928-2023.log) | [captures](SmokeJourney/20260928-202247-en-smoke/) |
| Smoke Persian | [07](support-smoke-journey-20260928-07.log) | [captures](SmokeJourney/20260928-154421-fa-IR-smoke/) |
| No Support | [10](support-smoke-journey-20260928-10.log) | [captures](SmokeJourney/20260928-160952-en-without-support/) |
| Strike player | [09](support-strike-journey-20260928-09.log) | [captures](SmokeJourney/20260928-174342-en-strike-player/) |
| Strike ARIA | [10](support-strike-journey-20260928-10.log) | [captures](SmokeJourney/20260928-175347-en-strike-aria/) |
| Paratroopers final | [03](support-paratrooper-journey-20260928-03.log) | [captures](SmokeJourney/20260928-201007-en-paratroopers/) |
| Supply final | [06](support-supply-journey-20260928-06.log) | [captures](SmokeJourney/20260928-201537-en-supply/) |

Popup/lesson native capture 05 covers English 16:9 and Persian 20:9 with no TMP truncation. No Android device is attached; real-player and target-device acceptance and allocation profiling remain pending. See the handoff and `implementation_progress.md` for details and failed attempts.
