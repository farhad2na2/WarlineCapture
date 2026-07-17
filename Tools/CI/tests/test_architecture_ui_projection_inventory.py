import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
INVENTORY = ROOT / "Design/Architecture/ui_projection_allocation_inventory.md"
WORK_PACKAGES = ROOT / "Design/Architecture/WorkPackages"


class UiProjectionInventoryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.text = INVENTORY.read_text(encoding="utf-8")
        cls.surface_rows = [
            line for line in cls.text.splitlines()
            if re.match(r"^\| `UI-\d{3}` \|", line)
        ]

    def test_inventory_has_the_complete_unique_surface_sequence(self):
        identifiers = [
            re.match(r"^\| `(UI-\d{3})` \|", row).group(1)
            for row in self.surface_rows
        ]
        expected = [f"UI-{index:03d}" for index in range(1, 30)]
        self.assertEqual(expected, identifiers)

    def test_every_surface_row_has_all_required_columns(self):
        for row in self.surface_rows:
            columns = [column.strip() for column in row.strip("|").split("|")]
            self.assertEqual(8, len(columns), row)
            self.assertTrue(all(columns), row)

    def test_every_surface_routes_to_a_maturity_task(self):
        for row in self.surface_rows:
            self.assertRegex(row, r"`AM-\d{3}`", row)

    def test_all_bounded_work_packages_exist_and_are_referenced(self):
        for index in range(1, 24):
            package_id = f"AM-WP-{index:03d}"
            matches = sorted(WORK_PACKAGES.glob(f"am_wp_{index:03d}_*.md"))
            self.assertEqual(1, len(matches), package_id)
            self.assertIn(package_id, self.text)

    def test_work_packages_use_the_seven_section_contract(self):
        for index in range(1, 24):
            package = next(WORK_PACKAGES.glob(f"am_wp_{index:03d}_*.md"))
            headings = re.findall(r"^## ([1-7])\. ", package.read_text(encoding="utf-8"), re.MULTILINE)
            self.assertEqual(list("1234567"), headings, package.as_posix())

    def test_declared_route_and_popup_contracts_are_reconciled(self):
        required_contracts = (
            "Splash",
            "MainMenu",
            "Settings",
            "QuickCustomSetup",
            "Match",
            "Armory",
            "CommandFeed",
            "Campaign",
            "MissionBriefing",
            "Operations",
            "CommandExchange",
            "BuildDrawer",
            "ResourceExchange",
            "ThreatAlert",
            "Pause",
            "RewardUnlock",
        )
        route_section = self.text.split("## Route And Popup Reconciliation", 1)[1]
        for contract in required_contracts:
            self.assertIn(contract, route_section)

    def test_inventory_preserves_protected_ownership_boundaries(self):
        dependency_section = self.text.split("## Dependency-Safe Work While Phase 2 Is Active", 1)[1]
        for protected_domain in ("operation-map", "FirstLaunch", "audio", "UI visual-lock"):
            self.assertIn(protected_domain, dependency_section)


if __name__ == "__main__":
    unittest.main()
