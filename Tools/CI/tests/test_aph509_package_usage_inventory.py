from __future__ import annotations

import json
import subprocess
import tempfile
import unittest
from pathlib import Path

from Tools.CI.aph509_package_usage_inventory import (
    CANDIDATE_REMOVAL_BLOCKERS,
    EXPECTED_SUMMARY,
    ROOT,
    Evidence,
    build_report_data,
    classification,
    collect,
    package_row,
    render_json,
    render_report,
    summary_validation_errors,
)


class Aph509PackageUsageInventoryTests(unittest.TestCase):
    PROBE_PACKAGES = (
        "com.unity.ide.rider",
        "com.unity.ide.visualstudio",
        "com.unity.modules.cloth",
        "com.unity.modules.umbra",
        "com.unity.modules.wind",
    )

    def create_probe_repository(self, root: Path, extra_files: dict[str, str]) -> dict[str, Evidence]:
        dependencies = {package: "1.0.0" for package in self.PROBE_PACKAGES}
        files = {
            "Packages/manifest.json": json.dumps({"dependencies": dependencies}),
            "Packages/packages-lock.json": json.dumps({
                "dependencies": {
                    package: {
                        "version": "1.0.0",
                        "depth": 0,
                        "source": "builtin",
                        "dependencies": {},
                    }
                    for package in self.PROBE_PACKAGES
                }
            }),
            **extra_files,
        }
        for relative, content in files.items():
            path = root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding="utf-8")
        subprocess.run(["git", "init", "-q", str(root)], check=True)
        subprocess.run(["git", "-C", str(root), "add", "."], check=True)
        return {item.package: item for item in collect(root)}

    def test_source_evidence_wins_over_candidate_status(self) -> None:
        item = Evidence("pkg", "manifest-declared", "1", 0, "registry")
        item.source_files.add("Assets/Game/Foo.cs")
        self.assertEqual("usage-evidence-found", classification(item))

    def test_reverse_dependency_is_graph_required(self) -> None:
        item = Evidence("pkg", "lock-only-transitive", "1", 1, "registry")
        item.required_by.add("parent")
        self.assertEqual("dependency-graph-required", classification(item))

    def test_only_evidence_free_manifest_entry_is_candidate(self) -> None:
        item = Evidence("pkg", "manifest-declared", "1", 0, "registry")
        self.assertEqual("candidate-unused-static-only", classification(item))

        row = package_row(item)

        self.assertFalse(row["removalAuthorized"])
        self.assertEqual(list(CANDIDATE_REMOVAL_BLOCKERS), row["removalBlockers"])

    def test_external_editor_integration_remains_unproven(self) -> None:
        item = Evidence("com.unity.ide.rider", "manifest-declared", "1", 0, "registry")
        item.ambiguous_files.add(".gitignore")
        self.assertEqual("unproven-static-blind-spot", classification(item))
        self.assertIn("static-analysis-blind-spot-unresolved", package_row(item)["removalBlockers"])

    def test_explicit_static_probes_find_all_five_package_channels(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            rows = self.create_probe_repository(Path(directory), {
                ".idea/workspace.xml": "<project />\n",
                ".vsconfig": "{}\n",
                "Assets/Game/Scripts/Occlusion.cs": (
                    "using UnityEngine;\n"
                    "class OcclusionProof { void Apply(Renderer value) { "
                    "value.allowOcclusionWhenDynamic = true; } }\n"
                ),
                "Assets/Probe.prefab": "--- !u!183 &1\nCloth:\n",
                "Assets/Probe.unity": "--- !u!182 &1\nWindZone:\n",
            })

            self.assertTrue(all(
                classification(rows[package]) == "usage-evidence-found"
                for package in self.PROBE_PACKAGES
            ))
            self.assertEqual({"Assets/Probe.prefab"}, rows["com.unity.modules.cloth"].serialized_files)
            self.assertEqual({"Assets/Probe.unity"}, rows["com.unity.modules.wind"].serialized_files)

    def test_absent_static_probe_evidence_remains_candidate_only(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            rows = self.create_probe_repository(Path(directory), {})

            self.assertTrue(all(
                classification(rows[package]) == "candidate-unused-static-only"
                for package in self.PROBE_PACKAGES
            ))
            self.assertTrue(all(not package_row(rows[package])["removalAuthorized"]
                                for package in self.PROBE_PACKAGES))

    def test_ambiguous_external_editor_evidence_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            rows = self.create_probe_repository(Path(directory), {
                ".gitignore": "# Visual Studio / Rider\n.vs/\n.idea/\n",
                "Assets/NullOcclusion.unity": (
                    "--- !u!29 &1\n"
                    "OcclusionCullingSettings:\n"
                    "  m_OcclusionCullingData: {fileID: 0}\n"
                ),
            })

            for package in ("com.unity.ide.rider", "com.unity.ide.visualstudio"):
                self.assertEqual("unproven-static-blind-spot", classification(rows[package]))
                self.assertTrue(rows[package].ambiguous_files)
                self.assertIn(
                    "static-analysis-blind-spot-unresolved",
                    package_row(rows[package])["removalBlockers"],
                )
            self.assertEqual(
                "candidate-unused-static-only",
                classification(rows["com.unity.modules.umbra"]),
            )

    def test_report_is_sorted_and_deterministic(self) -> None:
        second = Evidence("z.pkg", "manifest-declared", "1", 0, "registry")
        first = Evidence("a.pkg", "manifest-declared", "1", 0, "registry")
        rendered_once = render_report([second, first])
        rendered_twice = render_report([first, second])
        self.assertEqual(rendered_once, rendered_twice)
        self.assertLess(rendered_once.index("`a.pkg`"), rendered_once.index("`z.pkg`"))
        self.assertIn("Source | Serialized | Build | Editor", rendered_once)
        self.assertIn("Package removal authorized: **false**", rendered_once)

    def test_json_is_sorted_and_deterministic(self) -> None:
        data = {
            "schemaVersion": 1,
            "packages": [package_row(Evidence("a.pkg", "manifest-declared", "1", 0, "registry"))],
        }

        rendered_once = render_json(data)
        rendered_twice = render_json(data)

        self.assertEqual(rendered_once, rendered_twice)
        self.assertEqual(data, json.loads(rendered_once))

    def test_summary_count_drift_fails_closed(self) -> None:
        drifted = dict(EXPECTED_SUMMARY)
        drifted["manifestDeclaredCount"] -= 1

        errors = summary_validation_errors(drifted)

        self.assertEqual(
            ["summary-mismatch:manifestDeclaredCount:expected=47:actual=46"],
            errors,
        )

    def test_collect_covers_all_required_usage_channels(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            package = "com.example.proof"
            guid = "0123456789abcdef0123456789abcdef"
            files = {
                "Packages/manifest.json": json.dumps({"dependencies": {package: "1.0.0"}}),
                "Packages/packages-lock.json": json.dumps({
                    "dependencies": {
                        package: {"version": "1.0.0", "depth": 0, "source": "embedded", "dependencies": {}}
                    }
                }),
                f"Packages/{package}/package.json": json.dumps({"name": package}),
                f"Packages/{package}/Runtime/Proof.asmdef": json.dumps({"name": "Example.Proof"}),
                f"Packages/{package}/Runtime/Proof.cs.meta": f"fileFormatVersion: 2\nguid: {guid}\n",
                "Assets/Game/Scripts/Consumer.cs": "using Example.Proof;\n",
                "Assets/Game/Config.asset": f"m_Script: {{fileID: 1, guid: {guid}, type: 3}}\n",
                "Assets/Editor/ProofWorkflow.cs": "// Example.Proof editor workflow\n",
                "Tools/CI/build.sh": f"# validates {package}\n",
            }
            for relative, content in files.items():
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(content, encoding="utf-8")
            subprocess.run(["git", "init", "-q", str(root)], check=True)
            subprocess.run(["git", "-C", str(root), "add", "."], check=True)

            item = collect(root)[0]

            self.assertEqual({"Assets/Game/Scripts/Consumer.cs"}, item.source_files)
            self.assertEqual({"Assets/Game/Config.asset"}, item.serialized_files)
            self.assertEqual({"Tools/CI/build.sh"}, item.build_files)
            self.assertEqual({"Assets/Editor/ProofWorkflow.cs"}, item.editor_files)

    def test_current_repository_matches_accepted_package_contract(self) -> None:
        data = build_report_data(ROOT)

        self.assertTrue(data["inventoryValid"])
        self.assertFalse(data["packageRemovalAuthorized"])
        self.assertEqual(EXPECTED_SUMMARY, data["summary"])
        self.assertTrue(data["inputEvidence"]["originPackageInputsMatch"])
        self.assertEqual(15, len(data["candidatePackages"]))
        self.assertEqual(2, len(data["staticBlindSpotPackages"]))
        self.assertTrue(all(not row["removalAuthorized"] for row in data["packages"]))

        rows = {row["package"]: row for row in data["packages"]}
        self.assertEqual("usage-evidence-found", rows["com.unity.addressables"]["classification"])
        self.assertEqual("usage-evidence-found", rows["com.unity.modules.androidjni"]["classification"])
        self.assertNotIn("com.unity.addressables", data["candidatePackages"])
        self.assertNotIn("com.unity.modules.androidjni", data["candidatePackages"])
        self.assertEqual(
            ["com.unity.ide.rider", "com.unity.ide.visualstudio"],
            data["staticBlindSpotPackages"],
        )
        self.assertEqual(
            "usage-evidence-found",
            rows["com.unity.modules.umbra"]["classification"],
        )


if __name__ == "__main__":
    unittest.main()
