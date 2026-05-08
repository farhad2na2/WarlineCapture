Status: blocked
Topic:
QA/HCI public M01 launch validation attempt review

Reviewed handoff:
`Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Finding:
QA/HCI attempted the PM-requested public launch validation after the UI Quick Custom launch handoff. The attempt is not accepted as validation evidence. Unity exited successfully, but the expected Test Runner XML files were not produced, and the logs show only startup/shutdown noise rather than usable test pass/fail output.

Accepted portion:
- QA/HCI correctly identified that the current evidence still does not prove the full user campaign path.
- QA/HCI correctly kept manual HCI/balance validation blocked.
- The report uses the standard lane/task/files/contracts/validation/gaps/cross-lane/next-task structure.

Blocked / not accepted:
- No accepted QA rerun exists for `PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute`.
- No end-to-end campaign launch smoke exists for `Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch`.
- No player-visible screenshot/capture exists showing the first visible state after campaign launch.
- The missing Test Runner XML means this QA attempt cannot be counted as a pass even though Unity returned exit code 0.

PM verification:
- `/private/tmp/warlinecapture-qa-hci-public-quickcustom-results.xml` is missing.
- `/private/tmp/warlinecapture-qa-hci-public-saga-editmode-results.xml` is missing.
- Retry logs end in editor shutdown and do not provide usable test-result evidence.

Cross-lane impacts:
- Gameplay/UI still own the public campaign launch evidence gap.
- QA/HCI should not keep rerunning the same blocked command until the owning lane provides a reliable validation path or player-visible evidence.
- The user should not be asked to run HCI/balance tests yet.
- Quick Custom remains partially supported by the UI lane's own PlayMode handoff, but PM cannot promote the whole public launch blocker to accepted until campaign path evidence lands.

Needs user decision:
No. This is an execution/evidence blocker for the agents, not a product decision.

Next recommended task:
Gameplay/UI should provide either:
- a true end-to-end campaign launch smoke proving `Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch` reaches `WarlineCaptureRoute.Match` with M01 production visuals and legacy `UI_Canvas` inactive; or
- a graphics/player-visible capture path showing that same campaign launch result.

After that lands, QA/HCI should rerun only the affected public-launch checks.
