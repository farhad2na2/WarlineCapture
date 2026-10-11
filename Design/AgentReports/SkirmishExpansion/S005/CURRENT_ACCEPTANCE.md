# S005 current acceptance

**Not technically ready / not player-ready.** Catalog remains InProgress. The owner will review visuals after technical readiness; real player/device acceptance is pending.

Current validated working-tree source: `8417a1d738f2ea61693becedd3e66ba85c2ba7592ee1c31606d5683b3d2869cb`. Compilation is clear. Shared aircraft pause fix and CI monitor repair are pushed in `241b417`; independent HUD/font/quality changes remain preserved separately. Physical Campaign map and gameplay budgets were not changed by this fix. See `native1046-air-pause-fixed-source-delta.json`.

Native1018 normal paid Strike flight revealed rotation drift while paused before Save. Native1044 reproduced the defect on the first zero-delta update. Native1046 passes five isolated pause-conservation cases with120 paused frames and positive-time controls. This proves the isolated aircraft-system fix; normal native Continue/Pause/Save remains pending.

Earlier source `cc14c593…`: native1024 passed paid delivery, native Touch selection/Move, measured post-order motion and death-capacity cleanup for all15 ground roles in an isolated funded fixture. Native1018 read-only placement audit passed20 living buildings across both factions, including the paid Airport. Native992 CampaignM2 and native1026 CampaignM5 bilingual journeys passed; M1/M3/M4 remain open. These retain their original full source hashes and scope.

Earlier source `816a3ae…`: native932–933 complete normal English/Persian manual victories and native934–939 six seeded ARIA victories remain historical baseline evidence. Native931 paid Heavy APC transport/checkpoint and native945–980 paid Strike taxi recovery are scoped proofs. They are not fresh whole-source victories after the aircraft fix.

Drone1040 and1048 failed before producer placement/queue;1045 failed external-fixture compilation. No Drone production defect is established. Failed evidence is retained.

Remaining technical work: normal aircraft flight recovery; bilateral aircraft infrastructure/full-profile capacity; required combat, transport, recon and checkpoint role effects; remaining result/replay/exposed presets; affected CampaignM1/M3/M4 regressions and required family/stress/cleanup/performance checks. Native visuals and real player/device acceptance remain separate.

Detailed historical evidence is preserved in `CURRENT_ACCEPTANCE-history-through-native1048.md`; `TECHNICAL_READINESS_CURRENT.json` records active runs and exact proof names.

Native1049: PassedScopedNormalInputRetainedFlightRecovery. Scoped normal-input recovery of the byte-identical original1018 actual saved flight; NEW Pause/Save quaternion conservation and subsequent flight, no fresh full-match certification. Proof `native1049-retained-flight-pause-save-continue-pass.json`.

Native1050: isolated Drone IntelStation/no-Airport smoke Failed; funding/readiness/death disclosed, no whole mission or full lifecycle certificate. Proof `native1050-drone-intelstation-no-airport-failure.json`.

Native1053 PASS current-source fa-IR manual-controls driver: Victory 132.146s, native receipt audit and frozen result/Main Menu return passed; wrapper0 and unchanged full source/material manifest. This is agent-operated normal input, not real human/device acceptance. Full mission gates remain open.
