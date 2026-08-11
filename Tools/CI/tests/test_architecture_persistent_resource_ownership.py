import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "Tools/CI"))

import architecture_persistent_resource_ownership as ownership


class PersistentResourceOwnershipTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.report = ownership.build_report(ROOT)

    def test_categories_are_present(self):
        self.assertEqual(
            set(self.report["categories"]),
            {"persistentNativeContainers", "persistentQueries", "eventSubscriptions", "presentationRoots"},
        )

    def test_summary_reconciles(self):
        rows = [row for category in self.report["categories"].values() for row in category]
        summary = self.report["summary"]
        self.assertEqual(summary["totalResourceCount"], len(rows))
        self.assertEqual(
            summary["explicitOwnerCount"] + summary["gapCount"] + summary["protectedOwnerCount"],
            len(rows),
        )

    def test_protected_rows_are_not_claimed(self):
        for rows in self.report["categories"].values():
            for row in rows:
                if row["protectedOwnerIds"]:
                    self.assertEqual("protected-owner", row["status"])

    def test_exact_handoff_path_is_not_protected_by_broader_owner_glob(self):
        path = "Assets/Game/Scripts/Composition/OperationMapSceneLoadingSceneSystemHelper.cs"
        exclusions = [{
            "id": "operation-map",
            "protectedPaths": ["Assets/Game/Scripts/**/*OperationMap*.cs"],
            "handoffPaths": [path],
        }]
        self.assertEqual(ownership.protected_owner_ids(path, exclusions), [])
        self.assertEqual(
            ownership.protected_owner_ids(
                "Assets/Game/Scripts/Composition/OtherOperationMapOwner.cs",
                exclusions,
            ),
            ["operation-map"],
        )

    def test_native_rows_have_persistent_allocator_evidence(self):
        for row in self.report["categories"]["persistentNativeContainers"]:
            self.assertTrue(row["persistentAllocatorObserved"])

    def test_indirect_native_owners_are_included(self):
        rows = {
            (row["ownerType"], row["field"]): row
            for row in self.report["categories"]["persistentNativeContainers"]
        }
        for key in (
            ("DynamicBlockerComponent", "Blocked"),
            ("DynamicOccupancyComponent", "Occupied"),
            ("UnitPathGridSnapshot", "DynamicBlocked"),
            ("UnitPathLiveUnitSnapshot", "_entities"),
            ("UnitPathfindingSystem", "_pendingPathStream"),
        ):
            self.assertIn(key, rows)

    def test_grid_storage_containers_have_one_explicit_lifecycle_owner(self):
        rows = {
            (row["ownerType"], row["field"]): row
            for row in self.report["categories"]["persistentNativeContainers"]
        }
        for key in (
            ("DynamicBlockerComponent", "Counts"),
            ("DynamicBlockerComponent", "Blocked"),
            ("DynamicBlockerComponent", "FriendlyPassFactionIds"),
            ("DynamicOccupancyComponent", "Occupied"),
            ("PathPoolComponent", "Cells"),
        ):
            row = rows[key]
            self.assertEqual("explicit", row["status"], key)
            self.assertEqual(
                "RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage",
                row["creationOwner"],
                key,
            )
            self.assertEqual(
                "RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage",
                row["disposalOwner"],
                key,
            )

    def test_borrowed_query_context_is_not_a_persistent_owner(self):
        rows = self.report["categories"]["persistentQueries"]
        self.assertFalse(any(
            row["ownerType"] == "Context" and row["field"] == "GetBoundaryQuery"
            for row in rows
        ))

    def test_fixed_ui_lifecycles_are_explicit(self):
        rows = [row for category in self.report["categories"].values() for row in category]
        fixed = {
            ("RuntimeLogBuffer", "Application.logMessageReceived"),
            ("UIShellContentView", "_mainMenuPlayUi.FullMapPopupRequested"),
            ("UIShellContentView", "_mainMenuPlayUi.FullMapPopupCloseRequested"),
            ("UiShellEcsGateway", "boundaryQuery"),
            ("UiShellEcsGateway", "assistantMatchStartQuery"),
        }
        indexed = {
            (row["ownerType"], row.get("target", row.get("field", row.get("root")))): row
            for row in rows
        }
        for key in fixed:
            self.assertEqual("explicit", indexed[key]["status"], key)

    def test_non_protected_explicit_rows_name_both_owners(self):
        for rows in self.report["categories"].values():
            for row in rows:
                if row["status"] != "explicit":
                    continue
                self.assertNotEqual("unassigned", row["creationOwner"])
                self.assertNotEqual("unassigned", row["disposalOwner"])

    def test_markdown_is_deterministic(self):
        first = ownership.render_markdown(self.report)
        second = ownership.render_markdown(ownership.build_report(ROOT))
        self.assertEqual(first, second)

    def test_report_can_pin_an_implementation_baseline(self):
        report = ownership.build_report(ROOT, "HEAD^")
        self.assertNotEqual(self.report["baseline"]["commit"], report["baseline"]["commit"])
        self.assertEqual(40, len(report["baseline"]["commit"]))
        self.assertEqual(40, len(report["baseline"]["tree"]))


if __name__ == "__main__":
    unittest.main()
