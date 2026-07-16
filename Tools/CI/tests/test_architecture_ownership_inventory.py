#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "architecture_ownership_inventory.py"
SPEC = importlib.util.spec_from_file_location("architecture_ownership_inventory", SCRIPT)
assert SPEC and SPEC.loader
inventory = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = inventory
SPEC.loader.exec_module(inventory)


class ArchitectureOwnershipInventoryTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.revision = "a" * 40
        self.tree = "b" * 40
        for relative in {
            *inventory.AUTHORITY_PATHS,
            *(item["authorityPath"] for item in inventory.ACTIVE_WORK_OWNERS),
        }:
            self.write(relative, f"authority:{relative}\n")
        owner_ids = sorted({
            owner_id
            for domain in inventory.OWNER_DOMAINS
            for owner_id in domain["currentOwnerValidatorIds"]
        })
        self.write(
            "Design/AgentReports/ArchitectureMaturity/validator_registry.json",
            json.dumps({"validators": [{"id": owner_id} for owner_id in owner_ids]}),
        )

    def tearDown(self) -> None:
        self.temp.cleanup()

    def write(self, relative: str, value: str) -> Path:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(value, encoding="utf-8")
        return path

    def write_asmdef(self, name: str, references: list[str]) -> None:
        self.write(
            f"Assets/Game/{name}/{name}.asmdef",
            json.dumps({"name": name, "references": references}),
        )

    def seed_sources(self) -> None:
        self.write_asmdef("Assembly.A", ["Assembly.B"])
        self.write_asmdef("Assembly.B", ["Assembly.A", "Unity.Entities"])
        self.write(
            "Assets/Game/Scripts/RuntimeView.cs",
            "using UnityEngine;\n"
            "public class RuntimeView : MonoBehaviour\n"
            "{\n"
            "    private static int CurrentCount;\n"
            "    private void Update() { }\n"
            "}\n",
        )
        helper_lines = ["public static class LargeSystemHelper", "{"]
        helper_lines.extend(f"    // line {index}" for index in range(500))
        helper_lines.append("}")
        self.write(
            "Assets/Game/Scripts/Runtime/LargeSystemHelper.cs",
            "\n".join(helper_lines) + "\n",
        )
        self.write(
            "Assets/Game/Scripts/Editor/EditorOnly.cs",
            "public static class EditorOnly { private static int Ignored; }\n",
        )

    def test_live_inventory_covers_required_surfaces_and_cycles(self) -> None:
        self.seed_sources()
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        self.assertEqual(data["summary"]["productionSourceFileCount"], 2)
        self.assertEqual(data["summary"]["productionSourceOver500Count"], 1)
        self.assertEqual(data["summary"]["managedHelperCount"], 1)
        self.assertEqual(data["summary"]["runtimeLoopCount"], 1)
        self.assertEqual(data["summary"]["mutableStaticCandidateCount"], 1)
        self.assertEqual(data["assemblies"]["assemblyCount"], 2)
        self.assertEqual(data["assemblies"]["firstPartyEdgeCount"], 2)
        self.assertEqual(data["assemblies"]["cycleCount"], 1)
        self.assertEqual(data["assemblies"]["cycles"], [["Assembly.A", "Assembly.B"]])

    def test_runtime_scanner_ignores_comments_strings_and_editor_source(self) -> None:
        self.write_asmdef("Assembly.A", [])
        self.write(
            "Assets/Game/Scripts/Comments.cs",
            "using UnityEngine;\n"
            "public class Comments : MonoBehaviour\n"
            "{\n"
            "    // private static int FalseStatic;\n"
            "    private string text = \"private void Update() private static int Other;\";\n"
            "}\n",
        )
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        self.assertEqual(data["runtimeLoops"]["count"], 0)
        self.assertEqual(data["staticState"]["candidateCount"], 0)

    def test_outputs_regenerate_byte_identically(self) -> None:
        self.seed_sources()
        inventory.write_inventory(
            self.root, self.revision, self.tree, "out/first.json", "out/first.md"
        )
        inventory.write_inventory(
            self.root, self.revision, self.tree, "out/second.json", "out/second.md"
        )
        self.assertEqual((self.root / "out/first.json").read_bytes(), (self.root / "out/second.json").read_bytes())
        self.assertEqual((self.root / "out/first.md").read_bytes(), (self.root / "out/second.md").read_bytes())

    def test_authority_hashes_and_owner_paths_are_sorted_and_complete(self) -> None:
        self.seed_sources()
        data = inventory.build_inventory(self.root, self.revision, self.tree)
        authorities = data["sourceAuthorities"]
        self.assertEqual(
            [item["path"] for item in authorities],
            sorted({*inventory.AUTHORITY_PATHS, *(item["authorityPath"] for item in inventory.ACTIVE_WORK_OWNERS)}),
        )
        self.assertTrue(all(len(item["sha256"]) == 64 for item in authorities))
        owners = data["activeWorkOwnership"]["owners"]
        self.assertEqual([item["id"] for item in owners], ["audio", "first-launch", "operation-map", "ui-visual-lock"])
        self.assertTrue(all(item["protectedPaths"] == sorted(item["protectedPaths"]) for item in owners))

    def test_invalid_identity_and_missing_authority_fail_closed(self) -> None:
        with self.assertRaisesRegex(ValueError, "exact 40-character"):
            inventory.build_inventory(self.root, "short", self.tree)
        (self.root / inventory.AUTHORITY_PATHS[0]).unlink()
        with self.assertRaisesRegex(ValueError, "required authority is missing"):
            inventory.build_inventory(self.root, self.revision, self.tree)


if __name__ == "__main__":
    unittest.main()
