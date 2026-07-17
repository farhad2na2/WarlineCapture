#!/usr/bin/env python3

from __future__ import annotations

import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
STRESS_PATH = ROOT / "Assets/Tests/PlayMode/ArchitectureMenuMatchLifecycleStressPlayModeTests.cs"
COLLECTOR_PATH = ROOT / "Assets/Tests/PlayMode/ArchitectureMenuMatchLifecycleSnapshotCollector.cs"
TRANSITION_PATH = ROOT / "Assets/Tests/PlayMode/Aph805MenuMatchMenuLifecyclePlayModeTests.cs"
WORK_PACKAGE_PATH = ROOT / "Design/Architecture/WorkPackages/am_wp_025_menu_match_lifecycle_stress.md"
TRACKER_PATH = ROOT / "Design/Architecture/post_hardening_architecture_maturity_tracker.md"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


class ArchitectureMenuMatchLifecycleStressTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.stress = read(STRESS_PATH)
        cls.collector = read(COLLECTOR_PATH)
        cls.transitions = read(TRANSITION_PATH)
        cls.work_package = read(WORK_PACKAGE_PATH)
        cls.tracker = read(TRACKER_PATH)

    def test_early_development_cycle_counts_are_frozen(self) -> None:
        self.assertRegex(
            self.stress,
            r"private\s+const\s+int\s+WarmupCycleCount\s*=\s*1\s*;",
        )
        self.assertRegex(
            self.stress,
            r"private\s+const\s+int\s+MeasuredCycleCount\s*=\s*10\s*;",
        )
        self.assertEqual(len(re.findall(r"\bWarmupCycleCount\b", self.stress)), 2)
        self.assertEqual(len(re.findall(r"\bMeasuredCycleCount\b", self.stress)), 2)

    def test_stress_reuses_production_transition_fixture(self) -> None:
        required_calls = (
            "Aph805MenuMatchMenuLifecyclePlayModeTests.PrepareStableMenu(context)",
            "Aph805MenuMatchMenuLifecyclePlayModeTests.EnterStableMatch(context)",
            "Aph805MenuMatchMenuLifecyclePlayModeTests.ReturnToStableMenu(context)",
        )
        for call in required_calls:
            self.assertIn(call, self.stress)

        self.assertNotIn("UiShellRuntimeGateway", self.stress)
        self.assertIn("UiShellRuntimeGateway.TryEnqueueRouteRequest", self.transitions)
        self.assertIn("UiShellRouteIntent.EnterMatch", self.transitions)
        self.assertIn("UiShellRouteIntent.ReturnToMainMenu", self.transitions)

    def test_new_stress_sources_avoid_managed_system_reflection_and_polling(self) -> None:
        forbidden_patterns = {
            "SystemBase": r"\bSystemBase\b",
            "reflection namespace": r"\bSystem\.Reflection\b",
            "binding flags": r"\bBindingFlags\b",
            "reflection member lookup": r"\bGet(?:Field|Fields|Method|Methods|Property|Properties)\s*\(",
            "reflection invocation": r"\b(?:MethodInfo|FieldInfo|PropertyInfo)\b|\.Invoke\s*\(",
            "production frame polling": r"\b(?:Update|LateUpdate|FixedUpdate)\s*\(",
            "local transition polling": r"\bWaitUntil\s*\(",
        }
        for path, source in ((STRESS_PATH, self.stress), (COLLECTOR_PATH, self.collector)):
            for label, pattern in forbidden_patterns.items():
                self.assertIsNone(re.search(pattern, source), f"{path.name} contains {label}")

    def test_diagnostics_are_bounded_to_cycles_five_and_ten(self) -> None:
        self.assertRegex(self.stress, r"for\s*\(int\s+cycle\s*=\s*1\s*;\s*cycle\s*<=\s*MeasuredCycleCount")
        self.assertRegex(self.stress, r"if\s*\(cycle\s*%\s*5\s*==\s*0\s*\)")
        self.assertNotIn("TestContext.Progress.WriteLine(", self.stress)
        self.assertEqual(self.stress.count("TestContext.Out.WriteLine("), 4)
        self.assertEqual(self.stress.count("matchBaseline.ToCompactString()"), 1)
        self.assertEqual(self.stress.count("menuBaseline.ToCompactString()"), 1)
        self.assertEqual(self.stress.count("match.ToCompactString()"), 1)
        self.assertEqual(self.stress.count("menu.ToCompactString()"), 1)

    def test_package_and_tracker_defer_extended_stress(self) -> None:
        self.assertRegex(
            self.work_package,
            r"(?is)one warm-up cycle.*exactly 10 measured production route cycles",
        )
        self.assertRegex(
            self.work_package,
            r"(?is)former 100-cycle extended stress policy is deferred",
        )
        self.assertRegex(
            self.tracker,
            r"(?is)AM-023.*one warm-up plus 10 measured automated Menu-to-Match-to-Menu cycles",
        )
        self.assertRegex(
            self.tracker,
            r"(?is)former 100-cycle extended stress policy is deferred",
        )


if __name__ == "__main__":
    unittest.main()
