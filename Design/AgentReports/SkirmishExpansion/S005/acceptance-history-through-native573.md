# S005 current acceptance

**In progress. Not player-ready.** Current candidate pre-run fingerprint: `c720b1373affdd0ee1b205979f871c55a879c2134a10577429b5fe3e2d2cc8ca`. Native561 is running; post-run fingerprint and full normal-input result remain pending.

S005 reuses the existing Campaign dense-city physical source. Its current logical configuration places the starting army beside the town, uses the existing southern industrial strip for the enemy supply yard, and keeps Standard/Regular defaults and all scenario budgets. The opening camera now targets the starting army. No physical scene was modified.

## Verified separately

- Automated current-source checks: run560 rebuilt S005 without changing its scenario bytes; all 12 setup combinations and existing mission regressions passed.
- Native startup and visual evidence on the preceding candidate: run559 passed with 14 buildings and 32 mission units. The actual opening shows the dense town. See `native559-dense-opening-startup-pass.json` and `native559-dense-town-opening.png`. Its source was d7af73ff5777794cfacdf900fd31666b2016251f5d1417dc4ce2d881e9feda31; it is historical after the camera/rebuild fix.
- Checkpoint capture in run559: 46 actors, 4,931 map references, two banks; persistence, decoding and fresh recovery were not certified.
- Materials: all 562 are unchanged against the earlier proof; see `final-561-all-materials-pre-proof.json`.
- Current normal-input English journey: native561 passed the menu and acceptance census; complete ARIA/result/return is still running.

## Pending acceptance

Current-source English/Farsi seed journeys, both manual approaches, all-role native production/combat/service/transport checks, bilateral aircraft capacity, full paid aircraft return/refuel/second-sortie cycle, fresh native save/recovery, visual review and performance, and separate real player/device acceptance remain open. Automated setup checks and startup captures do not satisfy these gates.

## Preserved failure evidence

Native549/550/551 and 554 failed starting-facility source fit. Run551 identified 36,097 building rejections, 4,296 road/water rejections and eight original-context rejections for the enemy refinery. Run556 caught overlapping physical staging/infantry pads; the corrected pads passed run558 and run560. All logs and JSON evidence remain. Earlier acceptance history is retained in `acceptance-history-before-native561.md`.

Native561 ended as failed: menu/consent passed, but a Rifle spawned inside an authored building and ARIA stayed in its opening waiting state. Source pre/post fingerprints matched. Shared building blocker origin repair is in progress; the previous candidate and its passes are historical after this change.

## Native 565 — current source, failed reinforcement delivery

Independent pre/post source hash matches `208c1adf0f585b1bf77ba8a25d5684fac2776bc72bd30ea175f3cbd4aaccdb09`. All 562 material hashes also match. Shared foundation blocker regression tests and setup matrix passed. Native opening actor centres no longer overlap source building foundations. Normal-input ARIA run still failed (`ariaStopped:Blocked`): paid rocketeer recruitment remained pending, four structure attackers, 12 infantry, 380 materials; both bases remained at 800 HP. No counted win or player readiness. Native 566 repeats the run for read-only producer queue inspection.

## Native 566/567 and town frontage candidate

Repeated dense-quarter startup failed for enemy barracks, with 43,525 building-footprint rejections and 7,100 road/water rejections. Both failures retained, source hash unchanged. Both logical deployments now translate to Z=650 together (X=935 / 1387.8); source scene, mission budgets and base separation remain unchanged. Rebuild 568 passed all 12 configurations and scenario byte-preservation. Native 569 is running on independent pre-source hash `8ba1b4f16775369c5fce191c4fb24905d5bc602552dd035dcc4047852679213c`. Native legality, visuals, queue delivery, complete journey and final post-hash remain pending.

## Delivery repair candidate — native 573 running

Source pre-hash `be4e45b96c468826de1b7ccb745d9900c68e4c42570e4f2d7298be586e583deb`. Expanded legal landing search to 96 cells and completes each perimeter before yielding, preserving all existing collision/surface/clearance checks. Focused 570 (three tests), shared delivery lifecycle 571, and 12-configuration matrix 572 passed. Native 573 delivered the paid rocketeer squad: infantry 12→16, structure attackers 4→8, rocketeer pending false. Complete battle/result/return, post-hash and all other acceptance gates still pending; playerReady=false. All 562 materials, physical scene and scenario budget asset remain unchanged.
