# Operation-Map ARIA Objective Anchor Binding

Date: 2026-07-16

## Scope

- Carry a bounded `OperationMapAnchorId` from runtime objective rows through assistant goal and recommendation read models.
- Preserve that typed id in the UI-to-ECS command request.
- Resolve `AssistantTargetKind.Objective` only from the single active, generation-matched, metadata-ready `OperationMapBlob`.
- Queue the existing smooth camera-focus request without adding an update loop, scene lookup, managed collection, or loader dependency.

The separate objective-list focus/jump workflow remains open.

## Architecture

- Map ownership: immutable `OperationMapAnchorBlob` position and kind.
- Scenario/mission ownership: the stable objective anchor id written into `MatchObjectiveRuntimeElement`.
- Assistant ECS ownership: goal projection, recommendation precedence, versioned request validation, and typed lookup through small pure utilities.
- Shell ownership: existing camera request execution and bounds clamp.

Entity, cell, and explicit world-position targets retain precedence. An anchor-only objective emits a non-executable `SHOW ME` camera recommendation. Missing, stale, failed, wrong-kind, duplicate-root, or non-finite metadata fails closed.

## Performance

- Fixed strings and blob lookup remain unmanaged.
- Lookup runs only while consuming a pending assistant command request.
- No per-frame scan, allocation, asset search, scene search, or new system was introduced.

## Validation

- Focused assistant EditMode: `28 / 28` passed.
- Camera/minimap ownership probe: two byte-identical completed-state runs, SHA-256 `c3685d11cbde1f26f8fe1b9f219463466b69db1e2d5ee31091fe68f9ba6dbbef`.
- Camera/minimap ownership tests: `33 / 33` passed.
- Phase 0 ownership tests: `26 / 26` passed.
- Source-growth and non-ECS naming gates: `24 / 24` passed after extracting the new logic from reviewed large system files.
- Unity compiler errors: `0` across focused runs.
- Logs: `/private/tmp/opmap-aria-anchor-*.log` and matching XML files.
