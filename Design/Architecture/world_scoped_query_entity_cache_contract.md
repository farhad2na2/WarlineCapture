# World-Scoped Query And Entity Cache Contract

## Purpose

`WorldScopedComponentQueryCache<T>` is the standard narrow cache primitive for one-component ECS query reuse and optional singleton-entity resolution. It owns no gameplay state, creates no World, never resolves `World.DefaultGameObjectInjectionWorld`, and does not replace domain command or queue owners.

## Ownership

- The consumer constructs one cache instance and supplies an explicit `EntityManager` on every operation.
- The cache binds to `EntityManager.World`; a different World triggers automatic query release and rebind.
- The consumer remains responsible for calling `Dispose` from its lifecycle owner. AM-021 assigns and proves those owners for existing consumers.
- The cache is a managed lifecycle utility, not an ECS system. It introduces no `SystemBase`, static state, service locator, runtime discovery, or update loop.

## Query Contract

- `Get(EntityManager)` creates one read-only or read/write single-component query according to the constructor setting.
- Repeated calls with the same live World return the same query without recurring managed allocation.
- A World change releases the old query when its World is still live, clears entity-resolution state, and creates a query in the new World.
- `Invalidate()` releases the current query, clears the World binding and both positive and negative entity-resolution state, and permits a fresh bind on the next operation.
- The returned `EntityQuery` is a borrowed handle owned by the cache. Callers must not dispose it or retain it across `Invalidate`, World rebind, or cache disposal, and the lifecycle owner must complete dependent jobs before those operations.

## Singleton Entity Contract

- `TryGetSingleton` caches a positive entity only after the query resolves exactly one entity.
- A positive entry is validated on every access with World liveness, the component order version, `EntityManager.Exists`, and `HasComponent<T>`.
- Structural changes involving `T` clear a positive entry before reuse.
- `TryGetSingleton` rejects enableable component types with `NotSupportedException`. Unity does not permit `GetSingletonEntity` for enableable queries, and this generic cache must not invent selection semantics for enabled/disabled candidates. `Get` remains valid for those query types.
- If the positive entity is destroyed or loses `T`, the cache clears it and immediately resolves a replacement from the existing query.
- A missing result is a deliberate negative cache entry. It remains missing until `Invalidate()` or World rebind, even if a matching entity is later created. The creating domain must invalidate after structural creation when it needs immediate visibility.
- Multiple matching entities retain `EntityQuery.GetSingletonEntity` fail-closed behavior, including after a positive result was cached; the cache does not silently choose one.

## Disposal Contract

- `Dispose()` is idempotent and releases the query when its World remains live.
- After disposal, `Get`, `TryGetSingleton`, and `Invalidate` throw `ObjectDisposedException`.
- If the bound World was already destroyed, disposal clears local state without attempting to dispose through a dead World.

## Performance And Thread Contract

- The cache is main-thread ECS lifecycle infrastructure and is not Burst/job data.
- Warm same-World `Get` and positive/negative `TryGetSingleton` paths must allocate zero recurring managed bytes.
- Adding a new production consumer requires extending the governed consumer/performance matrix; unregistered consumers fail closed.

## Follow-Up Boundaries

- AM-020 migrates cross-World mutable static state and hidden service-locator access to explicit owners that may use this contract.
- AM-021 assigns disposal owners to existing and future cache consumers.
- AM-022 adds broad World destruction/recreation, domain reload, scene reload, missing-singleton, and replaced-command-entity lifecycle tests.
- Domain-specific creation, buffer repair, naming, queue payloads, failure semantics, and overflow policy remain in their current domain caches. They must not move into this generic utility.
