# APH-707 Transport Boarding Decomposition

Date: 2026-07-11
Status: Complete

## Result

`TransportBoardingCommandSystem` remains the single unmanaged `ISystem` owner with the same public API, update ordering, query state, and original source GUID. Its 3,226-line implementation is decomposed into ten cohesive partial files:

- lifecycle and query ownership
- command routing and result projection
- transport-first boarding
- passenger-first boarding
- board-all planning
- candidate and selection resolution
- approach and ramp resolution
- disembark planning
- airdrop planning
- disembark application

The retained owner is 181 lines. Every new partial is between 241 and 416 lines, below the frozen 500-line review threshold, and no new `*SystemHelper` type or assembly boundary was introduced.

## Behavior Preservation

A token-level reconstruction compared the original struct body with the ordered partial bodies: both contain exactly 16,571 C# tokens and match exactly. The split therefore moves methods without rewriting their implementation.

Source-reading tests now target the owning partials, and the pure-ECS guard scans every partial. The non-ECS inventory scanner now resolves base types across partial declarations instead of treating each base-less partial as a new plain runtime system.

Two pre-existing direct `UnitPathRequest` writes found while validating the corrected inventory were centralized without changing behavior:

- transport boarding recovery reuses `UnitMoveOrderRequestSystem.ApplyTargetPathMoveOrder` with its existing command buffer;
- the DR-001 scenario uses the centralized path-request setter.

## Validation

| Gate | Result | Evidence |
|---|---|---|
| Token reconstruction | Exact match, `16,571 / 16,571` tokens | local deterministic comparison |
| Transport behavior | Passed `78/78` | `/private/tmp/warline-aph707-transport-final.log` |
| Selection/input | Passed `60/60` | `/private/tmp/warline-aph707-selection-r3.log` |
| Scenario lab | Passed `15/15` | `/private/tmp/warline-aph707-scenario.log` |
| Extraction contracts | Passed `33/33` | `/private/tmp/warline-aph707-extraction-r3.xml` |
| DR-001 scenario | Passed `2/2` | `/private/tmp/warline-aph707-dr001.xml` |
| Transport performance/allocation | Passed | `/private/tmp/warline-aph707-performance.log` |
| Gateway PlayMode flow | Passed `1/1` | `/private/tmp/warline-aph707-playmode-gateway.xml` |
| Production source growth | Passed `15/15` | `/private/tmp/warline-aph707-growth-final.log` |
| ECS/Burst architecture | Passed `10/10` | `/private/tmp/warline-aph707-burst-final.log` |
| Assembly boundaries | Passed `31/31` | `/private/tmp/warline-aph707-boundary.log` |
| Runtime build | Passed, zero errors | `dotnet build Game.Runtime.csproj --no-restore` |
| Editor-test build | Passed, zero errors | `dotnet build Game.Tests.Editor.csproj --no-restore` and Unity compilation |
| PlayMode build | Passed, zero errors | `dotnet build Game.Tests.PlayMode.csproj --no-restore` |
| Diff hygiene | Passed | `git diff --check` |

The broader `GameSceneTransportBoardingPlayModeTests` fixture passed `8/9`. Its remaining failure expects a soldier parachute visual height offset of `1.0`, while the existing runtime value is `1.2`. The decomposition does not modify visual height, airdrop visual code, or test fixtures, and all airdrop behavior/scenario coverage relevant to this structural slice remains green. This existing visual-contract mismatch is retained as residual follow-up rather than changing visual behavior inside APH-707.

## Residual Risk

The partial split improves ownership and reviewability but does not create compile-time boundaries between routing, planning, and application. Future changes must preserve the documented call direction and keep planning free of mutation until approach selection completes. The separate non-ECS conversion gate now correctly recognizes partial systems and advances to its pre-existing naming-escape inventory debt; that broader inventory cleanup is outside this behavior-preserving decomposition.
