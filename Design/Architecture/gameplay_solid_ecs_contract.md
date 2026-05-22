# Gameplay SOLID/ECS Architecture Contract

This contract defines the intended architecture for WarlineCapture gameplay code. It is written as a drift guardrail: existing debt may be grandfathered temporarily, but new gameplay work must move toward this shape instead of expanding mixed-responsibility code.

## Core Rule

Gameplay runtime is ECS data plus ECS systems. Unity object code exists only at the edges: authoring, baking, UI views, bootstrap composition, config assets, and editor tooling.

Runtime gameplay code must not introduce singleton access patterns. `static Instance`, global registries, and static service locators are migration debt unless the type is a pure, stateless data/math helper.

## Responsibilities

### Bootstrap

Bootstrap composes the application.

Allowed:
- Read serialized scene and config references.
- Register services.
- Install feature modules.
- Create or connect the ECS world.
- Start the application lifecycle.

Not allowed:
- Mission-specific behavior.
- Unit spawning policy.
- AI policy.
- Combat policy.
- Camera/framing policy.
- UI route rules.
- Asset-resolution policy.
- Static gameplay logging.

If a bootstrap change adds domain behavior, move it into a feature installer, ECS system, service, or config.

### ECS Components

Components hold data only. They should not own behavior beyond trivial value construction.

Expected names:
- `*Component` for `IComponentData`.
- `*Tag` for tag components.
- `*BufferElement` for `IBufferElementData`.

### ECS Systems

Systems own gameplay behavior. Systems should depend on ECS data and should not reach into UI views, scene objects, `AssetDatabase`, or static service facades.

Expected names:
- `*System`.

### Authoring And Baking

Authoring MonoBehaviours and Bakers exist only to convert Unity-authored references/config into ECS data.

Expected names:
- `*Authoring`.
- `*Baker`.

### UI

UI MonoBehaviours display data and emit commands. They do not own gameplay policy.

Expected names:
- `*View` for UI reference holders and widgets.
- Bridge/controller names are legacy-tolerated only where they already exist; new UI should prefer `View` plus command/event adapters.

### Config

ScriptableObjects describe data. They do not execute gameplay behavior.

Expected names:
- `*Config`.
- `*ConfigAsset` is accepted for existing scene/prefab config assets.

### Services

Services bridge external concerns such as logging, persistence, asset lookup, telemetry, and platform APIs. Gameplay systems should prefer ECS event/data streams; shell systems may depend on service interfaces.

Expected names:
- `I*Service` for abstractions.
- `*Service` for implementations.

Static service facades are legacy debt unless they are pure constants/math.

### Static State And Singletons

Static runtime state is not an acceptable gameplay dependency boundary. New gameplay code must not add:
- `static Instance` properties or fields.
- Singleton fallback lookups such as `SomeSystem.Instance`.
- Static service locators or `ResolveDependency<T>()` helpers.
- Static mutable gameplay state shared across systems.

Allowed static code is limited to pure, stateless operations:
- Math helpers.
- Deterministic value conversion.
- Constant lookup tables that do not own runtime state.
- Test-local helpers.

If a class needs runtime collaborators, pass them through bootstrap/installer composition, an explicit service interface at the shell edge, or ECS data/events. If a class needs shared gameplay state, represent it as ECS singleton components, normal components, or buffers owned by systems.

## Logging

Gameplay code must not add new calls to static logging facades such as `AILog.*` or direct `Debug.Log*`. New gameplay logging should use one of:
- ECS log event buffer processed by a shell logging system.
- An injected `ILogService` at the shell/service edge.
- A test-local logger implementation.

Existing `AILog` usage is grandfathered as migration debt and should be retired by domain slice.

## Refactor Direction

Use narrow migrations. Do not rewrite the entire project at once.

1. Introduce service interfaces and feature installers at the shell edge.
2. Move bootstrap domain behavior into feature installers and ECS startup systems.
3. Convert `static Instance` access and static runtime state into explicit injection or ECS singleton components.
4. Replace singleton fallback lookups with configured dependencies, ECS queries, or command/query ports.
5. Replace static logging with ECS log events plus a log flush service.
6. Convert mission-specific hardcoding into mission configs and systems.
7. Retire legacy class names only when touching that domain for real behavior work.

## Decision Test

For every class, answer:

> What single reason should cause this class to change?

If the answer mentions more than one domain or layer, split the responsibility before adding more behavior.
