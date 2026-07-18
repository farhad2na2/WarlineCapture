#!/usr/bin/env python3

from __future__ import annotations

import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
TEST_PATH = ROOT / "Assets/Tests/PlayMode/ArchitectureLifecycleMemoryPoolTrendPlayModeTests.cs"
COLLECTOR_PATH = ROOT / "Assets/Tests/PlayMode/ArchitectureMenuMatchLifecycleSnapshotCollector.cs"
EVALUATOR_PATH = ROOT / "Assets/Tests/PlayMode/ArchitectureLifecycleMemoryTrendUtilitySystemHelper.cs"
PACKAGE_PATH = ROOT / "Design/Architecture/WorkPackages/am_wp_026_lifecycle_memory_pool_trend.md"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


class ArchitectureLifecycleMemoryPoolTrendTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.test = read(TEST_PATH)
        cls.collector = read(COLLECTOR_PATH)
        cls.evaluator = read(EVALUATOR_PATH)
        cls.package = read(PACKAGE_PATH)

    def test_bounded_early_development_cycle_counts_are_frozen(self) -> None:
        self.assertRegex(self.test, r"WarmupCycleCount\s*=\s*1\s*;")
        self.assertRegex(self.test, r"MeasuredCycleCount\s*=\s*5\s*;")
        self.assertRegex(self.package, r"one warm-up cycle and five measured cycles")
        self.assertRegex(self.package, r"three-warm-up/twelve-measured extended trend is deferred")

    def test_collector_uses_established_profiler_and_owner_approved_pool_apis(self) -> None:
        for api in (
            "Profiler.GetTotalAllocatedMemoryLong()",
            "Profiler.GetTotalReservedMemoryLong()",
            "Profiler.GetMonoUsedSizeLong()",
            "Profiler.GetMonoHeapSizeLong()",
            "audioRuntime.PoolSize",
            "audioRuntime.ActiveSourceCount",
            "pathPool.Cells.Capacity",
        ):
            self.assertIn(api, self.collector)

    def test_structural_and_pool_counts_are_authoritative(self) -> None:
        for counter in (
            "TotalEntityCount",
            "LifecycleRootCount",
            "OperationMapRootCount",
            "AudioPoolSize",
            "PathPoolCapacity",
            "MissileTrailCreatedCount",
            "ImpactVfxCreatedCount",
        ):
            self.assertIn(f"actual.{counter}", self.test)
            self.assertIn(f"baseline.{counter}", self.test)

    def test_test_only_sources_do_not_force_gc_reflect_or_add_managed_systems(self) -> None:
        combined = "\n".join((self.test, self.collector, self.evaluator))
        forbidden = (
            r"\bGC\.Collect\s*\(",
            r"\bSystemBase\b",
            r"\bSystem\.Reflection\b",
            r"\bBindingFlags\b",
            r"\bGet(?:Field|Fields|Method|Methods|Property|Properties)\s*\(",
        )
        for pattern in forbidden:
            self.assertIsNone(re.search(pattern, combined), pattern)

    def test_memory_ceiling_crossings_are_reported_not_hidden(self) -> None:
        self.assertIn('string status = delta <= limitBytes ? "within" : "exceeded";', self.test)
        self.assertIn('string status = slope <= limitBytesPerCycle ? "within" : "exceeded";', self.test)
        self.assertRegex(self.package, r"Crossing a ceiling must be recorded with the exact value")
        self.assertRegex(self.package, r"does not override a green structural ownership/pool plateau")


if __name__ == "__main__":
    unittest.main()
