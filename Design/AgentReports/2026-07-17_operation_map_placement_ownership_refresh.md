# Operation Map Placement Ownership Refresh

Date: 2026-07-17

## Scope

Refreshed the read-only placement-ownership evidence after operation-map extraction and architecture revision `f993c3084` changed pinned runtime inputs. The accepted `opmap-006` evidence remains immutable so its downstream navigation cross-reference stays valid.

## Current Evidence

- Source baseline: `1c2f07234946f13118fb5fb16692aa6176427c71`.
- Report: `2026-07-17_operation_map_placement_ownership_refresh.json`.
- Report SHA-256: `8317552fc81355a217d1462e2cef0d76e190193c81dc3df28bbc75d01ff47c8a`.
- Result remains `NeedsDecision`; counts remain 451 buildings, 29 vehicles, and 54 duplicate source-path groups.
- Runtime consumers increased from 7 to 8 because extracted `OperationMapSceneView` now owns typed building and vehicle placement bindings for staged activation.
- Building identity aggregate is unchanged: `87a26e3d33214e942e0075e461d66a91a45e0735bfe51455bb140c695149f65b`.
- Vehicle identity aggregate is now `55cafdb63bdb392537459dc4cdac7c01d310abf19859aa61a5bcb79fa56345a1`, reflecting the accepted current-map tank placement correction from `5c86a3ea2`.
- Combined identity payload is `4ccc408e733c160579628da5dc89149465aa68693274499e23ddf7a7dadc09dd`.

## Validation

- Two real Unity probe runs passed and produced byte-identical 1,428,639-byte JSON.
- Both outputs and the committed report have SHA-256 `8317552fc81355a217d1462e2cef0d76e190193c81dc3df28bbc75d01ff47c8a`.
- The probe retained fail-closed direct-input hashing and exact runtime-consumer discovery.
- Focused EditMode tests passed 54/54 with zero failures, skips, or inconclusive results.
- Non-ECS naming/architecture validation passed 9/9.
- Unity compilation completed with no C# errors during the probe and focused test runs.
- `git diff --check` passed.
- The broad production source-growth gate retained two unrelated failures introduced by upstream `f993c3084`: `MatchBootstrapCompositionSystemHelper.cs` and `ThreatDetectionWarningSystem.cs` exceed their prior reviewed baselines. This refresh does not modify either file.

Logs and outputs:

- `/private/tmp/opmap-placement-refresh-run1.log`
- `/private/tmp/opmap-placement-refresh-run2.log`
- `/private/tmp/opmap-placement-refresh-run1.json`
- `/private/tmp/opmap-placement-refresh-run2.json`
- `/private/tmp/opmap-placement-refresh-focused.xml`
- `/private/tmp/opmap-placement-refresh-focused.log`
- `/private/tmp/opmap-placement-refresh-naming.log`
- `/private/tmp/opmap-placement-refresh-source-growth.log`
