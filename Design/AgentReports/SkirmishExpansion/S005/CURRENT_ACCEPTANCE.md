# S005 current acceptance

**Not technically ready / not player-ready.** Catalog remains InProgress. The owner will review visuals after technical readiness; real player/device acceptance is pending.

Current main working-tree source: `2de5dafe83cb8b8226a562562cdbe40300d284f9113cc696d90865ce656318b2`. Main compilation completed with zero errors after integrating the three-file M1 guidance patch. Shared aircraft pause fix and CI monitor repair are pushed in `241b417`; independent HUD/font/quality changes remain preserved separately. Physical Campaign map and gameplay budgets are unchanged by these fixes. See `native1058-M1-guidance-integration-source-delta.json`.

Native1018 normal paid Strike flight revealed rotation drift while paused before Save. Native1044 reproduced the defect on the first zero-delta update. On source `8417a1d7…`, native1046 passed five isolated pause-conservation cases with120 paused frames and positive-time controls. Native1049 then passed normal native Continue/Pause/Save of the byte-identical original1018 checkpoint, conserved the new paused flight state, and resumed positive flight. That is scoped recovery of the same paid attempt, not a fresh whole-match flight certificate.

Earlier source `cc14c593…`: native1024 passed paid delivery, native Touch selection/Move, measured post-order motion and death-capacity cleanup for all15 ground roles in an isolated funded fixture. Native1018 read-only placement audit passed20 living buildings across both factions, including the paid Airport. Native992 CampaignM2 and native1026 CampaignM5 bilingual journeys passed. These retain their original full source hashes and scope.

T7 native M1 run1042 passed the actual presented Rifle Squad guidance, normal card Touch, all-four selection, objectives, Victory and Campaign return in English/Persian. Independent review found no actionable production defects. The exact three-file production patch is integrated in main and compiles; T7's whole source differs from main, so its scoped M1 proof is not a whole current-main certification. CampaignM3/M4 remain open.

On source `8417a1d7…`, native1053 passed a fresh normal-input Persian S005 match: Victory132.146s, strict native receipt audit, frozen result and Main Menu return. English and fresh ARIA qualification remain open. The later main delta is only the three M1 UI files; the1053 proof retains its original hash.

Earlier source `816a3ae…`: native932–933 complete normal English/Persian manual victories and native934–939 six seeded ARIA victories remain historical baseline evidence. Native931 paid Heavy APC transport/checkpoint and native945–980 paid Strike taxi recovery are scoped proofs. They are not fresh whole-source victories after the aircraft fix.

Drone1040/1048/1050 failed before producer placement/queue;1045 failed external-fixture compilation. Native1051 reached three actual placement taps but none enabled Confirm in its bounded sample. That does not establish map impossibility or a Drone production defect. Failed evidence is retained. Improved1057 Drone and1052 Helipad fixtures compile without errors and await the main heavy Play slot.

Remaining technical work: bilateral aircraft infrastructure/full-profile capacity; required combat, transport, recon and checkpoint role effects; current English/ARIA journeys; remaining result/replay/exposed presets; affected CampaignM3/M4 regressions and required family/stress/cleanup/performance checks. Mobile quality changed independently from MSAA0 to4 and its existing performance-contract conflict remains unresolved. Native visuals and real player/device acceptance remain separate.

Detailed historical evidence is preserved in `CURRENT_ACCEPTANCE-history-through-native1048.md`; `TECHNICAL_READINESS_CURRENT.json` records active runs and exact proof names.

Agent-operated native input is recorded separately from real human/device acceptance. No current whole-source player-ready claim is made.
