# ECS Native Command Request System Conversion Example

## Purpose

Document a concrete pattern for converting a plain C# `*System` command helper into a real Unity ECS `ISystem` command processor.

Example target:
- Current: `TransportBoardingCommandSystem` as a public plain C# class with public `TryIssue...` methods.
- Desired: `TransportBoardingCommandSystem : ISystem` that consumes ECS request components, validates them, issues ECS orders, and publishes ECS command results for HUD feedback.

This document is an example solution pattern, not an implementation plan yet.

## Why Convert

The current direct-call shape is clear but not ECS-native:

```text
UI/input command route
-> holds/calls TransportBoardingCommandSystem public method
-> method validates immediately
-> method mutates ECS movement and boarding state
-> method returns Result to caller
```

The ECS-native shape decouples the caller from gameplay command execution:

```text
UI/input command route
-> writes ECS command request
-> HUD immediately shows "Boarding requested."
-> TransportBoardingCommandSystem : ISystem processes request
-> system writes movement and boarding ECS state if valid
-> system writes ECS command result
-> HUD feedback consumes result and shows accepted/failure message
```

The important UI rule is:

```text
Requested != Accepted
```

The UI can stay responsive without pretending the command has already succeeded.

## Naming Rule

Use `System` only when the type is a Unity ECS scheduled system:

```csharp
public partial struct TransportBoardingCommandSystem : ISystem
```

If a type remains a public plain C# class with public methods called by other classes, it should not use `System` unless it is already covered by an explicit legacy/refactor plan.

## Core Data Model

### Request

A request is a short-lived ECS command intent. It says what the player or runtime wants to attempt.

```csharp
public struct BoardTransportRequest : IComponentData
{
    public Entity Passenger;
    public Entity Transport;
    public Entity Source;
}
```

For board-all:

```csharp
public struct BoardAllSelectedTransportRequest : IComponentData
{
    public Entity Transport;
    public int MaxPassengers;
    public Entity Source;
}
```

Rules:
- Requests are data only.
- Requests do not imply success.
- Requests are consumed exactly once.
- Request creation may happen from UI/input boundary code, but gameplay validation must not happen in UI.

### Result

A result is a short-lived ECS event produced by the command system after validation.

```csharp
public enum BoardTransportResultCode : byte
{
    Accepted,
    NoSelection,
    InvalidPassenger,
    InvalidTransport,
    NotOwned,
    NoSeats,
    TransportNotLanded,
    TransportBusy,
    NoPath,
    NoApproachCell
}

public struct BoardTransportResult : IComponentData
{
    public BoardTransportResultCode Code;
    public Entity Passenger;
    public Entity Transport;
    public int OrderedCount;
}
```

Rules:
- Results are the only source of accepted/failed command feedback.
- Results should contain stable reason codes, not only text.
- UI-facing text is mapped in HUD feedback code from reason codes.

### Feedback

The HUD feedback path can show immediate request feedback and later result feedback.

Immediate UI feedback:

```text
Boarding requested.
```

Accepted result feedback:

```text
Boarding accepted.
Boarding 3 units.
```

Failure result feedback:

```text
Select a soldier first.
Select a transport.
No seats available.
Transport must land before boarding.
No path to transport.
```

Rules:
- UI feedback is honest about lifecycle state.
- UI does not need Debug logs to explain why nothing happened.
- Result messages can be transient; command prompts can remain persistent.

## ECS System Responsibility

`TransportBoardingCommandSystem : ISystem` should own command execution only:

```text
read BoardTransportRequest / BoardAllSelectedTransportRequest
-> validate current ECS world state
-> plan approach cell and movement target
-> issue UnitMove order components
-> add/update UnitTransportBoardingTarget state
-> emit BoardTransportResult
-> consume request
```

Allowed in the system:
- ECS component and buffer reads.
- Pure rule/helper calls.
- Burst-compatible jobs for candidate collection and scoring when useful.
- ECB for structural changes.
- Stable result reason codes.

Not allowed in the system:
- UI object mutation.
- Button state changes.
- Popup logic.
- Camera movement.
- GameObject hierarchy lookup.
- Direct HUD text formatting.
- Ungated runtime logs.

## Supporting Narrow Systems

Use the current gameplay ECS naming contract:

```text
Domain gameplay runtime types end in Entity, Component, or System.
```

That means these are still named as narrow `*System` boundaries even when they are plain stateless structs/classes and not Unity-scheduled `ISystem` types.

`UnitTransportBoardingQuerySystem`
- Read-only boarding checks.
- Answers questions like "is this entity a boardable transport?" or "how many seats are free?"

`UnitTransportBoardingRuleSystem`
- Pure boarding domain rules.
- Computes direct boarding distance, landed state, capacity policy, or eligibility rules.

`UnitTransportApproachCellSystem`
- Approach-cell search and reservation helper.
- Finds passenger boarding goals, disembark cells, and air-transport pickup cells from grid/pathing inputs.

`TransportBoardingCommandSystem`
- The future Unity ECS `ISystem` command processor.
- Consumes request components, validates world state through narrow systems, writes movement/boarding state, emits result components, and consumes requests.

Scoped data should be nested request/result/context structs where possible, not new standalone gameplay types with suffixes like `Query`, `Rule`, `Service`, `Adapter`, or `Context`.

Avoid adding broad `Manager`, `Controller`, `Facade`, or static service-locator layers.

## UI Flow

Passenger-first board:

```text
Soldier selected
-> tap Board button
-> UI arms Board mode and feedback says "Tap transport."
-> tap transport
-> UI/input writes BoardTransportRequest
-> feedback says "Boarding requested."
-> ECS command system validates and processes
-> result says "Boarding accepted." or exact failure reason
```

Transport-first board:

```text
Transport selected
-> tap Board button
-> UI arms Board mode and feedback says "Tap soldiers or Board All."
-> tap soldier or Board All
-> UI/input writes request
-> feedback says "Boarding requested."
-> ECS command system validates and processes
-> result says "Boarding accepted.", "Boarding 3 units.", or exact failure reason
```

Cancel:

```text
tap Cancel
-> UI/input writes CancelActiveCommandMode request or command-mode clear request
-> command-mode system clears mode
-> feedback/actions hide without showing "Command cancelled."
```

## Migration Steps

1. Add request and result components for boarding commands.
2. Add a small request writer at the current UI/input boundary.
3. Keep existing UI responsive by showing request-state feedback immediately.
4. Convert the plain command helper into an `ISystem` that consumes requests.
5. Move validation failure paths from return values to result reason codes.
6. Move HUD result text mapping into the existing command feedback path.
7. Remove direct references from selection/UI routing to the old command helper.
8. Keep existing narrow `*System` helper code where it is already pure and well-scoped.
9. Delete or rename the old plain class once no direct callers remain.
10. Add tests for request creation, system processing, result reasons, and HUD feedback mapping.

## Validation Checklist

- [ ] UI click writes a request and immediately shows requested feedback.
- [ ] Accepted request issues movement and boarding ECS state.
- [ ] Failed request emits an exact reason code.
- [ ] HUD maps reason codes to clear player text.
- [ ] Request is consumed exactly once.
- [ ] Result is transient and cleared after HUD consumption/lifetime expiry.
- [ ] No UI code directly mutates gameplay state.
- [ ] No direct caller holds a reference to the command processor.
- [ ] Command system uses ECB for structural changes.
- [ ] Focused tests cover accepted, rejected, board-all, no seats, no path, and transport-not-landed cases.

## Decision Point

If we keep direct public method calls, the honest name is:

```text
TransportBoardingCommandService
```

If we convert to ECS request/result processing, the correct name is:

```text
TransportBoardingCommandSystem
```

The preferred long-term ECS-native direction is the second option.
