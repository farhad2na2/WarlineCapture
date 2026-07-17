#!/usr/bin/env python3

from __future__ import annotations

import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
SOURCE_PATH = ROOT / "Assets/Game/Scripts/Systems/WorldScopedComponentQueryCache.cs"
BEHAVIOR_TEST_PATH = ROOT / "Assets/Tests/Editor/WorldScopedComponentQueryCacheTests.cs"
PERFORMANCE_TEST_PATH = ROOT / "Assets/Tests/Editor/WorldScopedComponentQueryCachePerformanceValidation.cs"
CONTRACT_PATH = ROOT / "Design/Architecture/world_scoped_query_entity_cache_contract.md"


class ArchitectureWorldScopedCacheContractTests(unittest.TestCase):
    def test_contract_remains_one_narrow_explicit_world_owner(self) -> None:
        source = SOURCE_PATH.read_text(encoding="utf-8")
        self.assertIn("WorldScopedComponentQueryCache<T> : IDisposable", source)
        self.assertIn("where T : unmanaged, IComponentData", source)
        self.assertEqual(source.count("CreateEntityQuery("), 1)
        self.assertIn("World world = entityManager.World;", source)
        for forbidden in (
            "World.DefaultGameObjectInjectionWorld",
            "SystemBase",
            "MonoBehaviour",
            "static World",
            "static Entity",
            "static EntityQuery",
        ):
            self.assertNotIn(forbidden, source)

    def test_positive_negative_invalidation_rebind_and_recovery_are_explicit(self) -> None:
        source = SOURCE_PATH.read_text(encoding="utf-8")
        for required in (
            "EntityResolution.Unknown",
            "EntityResolution.Missing",
            "EntityResolution.Found",
            "TryGetSingleton(EntityManager entityManager, out Entity entity)",
            "public void Invalidate()",
            "private bool CanReuseResolvedEntity(EntityManager entityManager, EntityQuery query)",
            "entityManager.GetComponentOrderVersion<T>()",
            "entityManager.Exists(_entity)",
            "entityManager.HasComponent<T>(_entity)",
            "if (_componentType.IsEnableable)",
            "throw new NotSupportedException(",
            "ReleaseQuery();",
            "ResetEntityResolution();",
        ):
            self.assertIn(required, source)

    def test_disposal_is_terminal_idempotent_and_dead_world_safe(self) -> None:
        source = SOURCE_PATH.read_text(encoding="utf-8")
        self.assertIn("if (_disposed)\n                return;", source)
        self.assertIn("if (_queryCreated && _world != null && _world.IsCreated)", source)
        self.assertIn("throw new ObjectDisposedException(GetType().Name);", source)
        behavior = BEHAVIOR_TEST_PATH.read_text(encoding="utf-8")
        self.assertIn("Dispose_IsIdempotentAndRejectsFurtherUse", behavior)
        self.assertIn("Dispose_IsSafeAfterBoundWorldIsDestroyed", behavior)
        self.assertIn("result=Passed tests=11", behavior)

    def test_behavior_and_performance_cover_every_contract_path(self) -> None:
        behavior = BEHAVIOR_TEST_PATH.read_text(encoding="utf-8")
        for required in (
            "SingletonCache_ReusesPositiveLookup",
            "SingletonCache_CachesNegativeLookupUntilInvalidated",
            "SingletonCache_FailsClosedAfterPositiveCardinalityChanges",
            "SingletonCache_RecoversAfterResolvedEntityIsDestroyed",
            "SingletonCache_RecoversAfterResolvedEntityLosesComponent",
            "SingletonCache_RejectsEnableableComponentTypes",
            "Cache_RebuildsAgainstDifferentWorld",
        ):
            self.assertIn(required, behavior)
        performance = PERFORMANCE_TEST_PATH.read_text(encoding="utf-8")
        self.assertIn("SingletonLookupPaths_ReuseWithZeroRecurringManagedAllocation", performance)
        self.assertIn("singleton lookup must allocate zero recurring managed bytes", performance)
        self.assertIn("result=Passed tests=4", performance)
        self.assertIn("GovernedCaches_ReuseAndRebindWithZeroRecurringManagedAllocation", performance)

    def test_design_contract_freezes_domain_and_follow_up_boundaries(self) -> None:
        contract = CONTRACT_PATH.read_text(encoding="utf-8")
        for heading in (
            "## Ownership",
            "## Query Contract",
            "## Singleton Entity Contract",
            "## Disposal Contract",
            "## Performance And Thread Contract",
            "## Follow-Up Boundaries",
        ):
            self.assertIn(heading, contract)
        for task in ("AM-020", "AM-021", "AM-022"):
            self.assertIn(task, contract)
        self.assertIn("borrowed handle owned by the cache", contract)
        self.assertIn("complete dependent jobs", contract)
        self.assertIn("must not move into this generic utility", contract)


if __name__ == "__main__":
    unittest.main()
