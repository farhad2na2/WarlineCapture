#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "architecture_owner_risk_ranking.py"
sys.path.insert(0, str(SCRIPT.parent))
SPEC = importlib.util.spec_from_file_location("architecture_owner_risk_ranking", SCRIPT)
assert SPEC and SPEC.loader
ranking = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = ranking
SPEC.loader.exec_module(ranking)


class ArchitectureOwnerRiskRankingTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.revision = "a" * 40
        self.tree = "b" * 40
        self.write(
            ranking.OWNERSHIP_PATH,
            json.dumps({
                "activeWorkOwnership": {
                    "owners": [{
                        "id": "audio",
                        "protectedPaths": ["Assets/Game/Scripts/Audio/**"],
                        "status": "active",
                    }],
                },
            }),
        )
        self.write(
            ranking.LIFECYCLE_PATH,
            json.dumps({
                "categories": {
                    "nativeContainers": [{"path": "Assets/Game/Scripts/Systems/AlphaSystem.cs"}],
                    "queryCaches": [{"path": "Assets/Game/Scripts/Systems/AlphaSystem.cs"}],
                },
            }),
        )
        self.write(ranking.ASSEMBLY_PATH, "{}\n")
        self.write(ranking.TOOL_PATH, "tool\n")
        self.write(ranking.TEST_PATH, "tests\n")
        self.write("Design/runtime.md", "measured\n")

    def tearDown(self) -> None:
        self.temp.cleanup()

    def write(self, relative: str, value: str) -> Path:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(value, encoding="utf-8")
        return path

    def seed_sources(self) -> None:
        self.write(
            "Assets/Game/Scripts/Systems/AlphaSystem.cs",
            "public sealed class AlphaSystem {\n"
            " private int _state;\n"
            " private int _other;\n"
            " private readonly object _cache;\n"
            " private int _first, _second;\n"
            " public int Count { get; private set; }\n"
            " public void Build() {}\n"
            " public void Update() { int localValue = 1; }\n"
            " public void Validate() {}\n"
            " public void Read() {}\n"
            " public void Write() {}\n"
            " public void Reset() {}\n"
            "}\n",
        )
        self.write(
            "Assets/Game/Scripts/Systems/BuildingBetaSystem.cs",
            "public sealed class BuildingBetaSystem { public AlphaSystem Read() => null; }\n",
        )
        self.write(
            "Assets/Game/Scripts/UI/GammaView.cs",
            "public sealed class GammaView { public AlphaSystem Build() => null; }\n",
        )
        self.write(
            "Assets/Game/Scripts/Audio/ProtectedAudio.cs",
            "public sealed class ProtectedAudio { public AlphaSystem Play() => null; }\n",
        )

    def build(self) -> dict:
        return ranking.build_ranking(
            self.root,
            self.revision,
            self.tree,
            changes={
                "Assets/Game/Scripts/Systems/AlphaSystem.cs": 12,
                "Assets/Game/Scripts/Systems/BuildingBetaSystem.cs": 6,
                "Assets/Game/Scripts/UI/GammaView.cs": 3,
            },
            measured_runtime=[{
                "averageMilliseconds": 2.0,
                "attribution": "focused fixture",
                "currency": "focused",
                "metric": "2 ms",
                "path": "Assets/Game/Scripts/Systems/AlphaSystem.cs",
                "selectionEligible": True,
                "source": "Design/runtime.md",
            }],
            responsibility_audits=[{
                "initialAllowedPaths": ["Assets/Game/Scripts/Systems/AlphaSystem.cs"],
                "modificationScope": "alpha-runtime",
                "path": "Assets/Game/Scripts/Systems/AlphaSystem.cs",
                "responsibilities": ["build", "update", "validate", "read"],
            }],
            verify_git=False,
        )

    def test_scores_all_five_dimensions_and_keeps_size_out_of_composite(self) -> None:
        self.seed_sources()
        data = self.build()
        alpha = next(row for row in data["screenedOwners"] if row["path"].endswith("AlphaSystem.cs"))
        self.assertEqual(set(alpha["scores"]), {
            "changeFrequency", "coupling", "measuredRuntimeCost", "responsibilityCount", "stateOwnership"
        })
        self.assertEqual(alpha["compositeScore"], sum(alpha["scores"].values()))
        self.assertNotIn("lines", alpha["scores"])
        self.assertEqual(alpha["stateSlotCount"], 6)
        self.assertTrue(alpha["updateExposure"]["recurring"])
        self.assertEqual(alpha["updateExposure"]["scoreContribution"], 0)

    def test_dependency_fan_in_and_protected_owner_visibility(self) -> None:
        self.seed_sources()
        data = self.build()
        alpha = next(row for row in data["screenedOwners"] if row["path"].endswith("AlphaSystem.cs"))
        protected = next(row for row in data["screenedOwners"] if row["path"].endswith("ProtectedAudio.cs"))
        self.assertEqual(alpha["dependencyFanIn"], 3)
        self.assertEqual(protected["editEligibility"], "protected")
        self.assertFalse(protected["firstWaveSelected"])
        self.assertIn("protected by audio", protected["selectionReason"])
        self.assertEqual(data["protectedOwners"], [protected])
        self.assertEqual(protected["protectedMatchedPattern"], "Assets/Game/Scripts/Audio/**")

    def test_first_wave_is_limited_to_three_distinct_scopes(self) -> None:
        self.seed_sources()
        data = self.build()
        selected = [row for row in data["screenedOwners"] if row["firstWaveSelected"]]
        self.assertLessEqual(len(selected), 3)
        self.assertEqual(len({row["modificationScope"] for row in selected}), len(selected))

    def test_shared_allowed_path_prevents_parallel_selection(self) -> None:
        self.seed_sources()
        shared = "Assets/Game/Scripts/Systems/AlphaSystem.cs"
        data = ranking.build_ranking(
            self.root,
            self.revision,
            self.tree,
            changes={},
            measured_runtime=[
                {
                    "averageMilliseconds": 1.0,
                    "attribution": "fixture",
                    "currency": "focused",
                    "metric": "1 ms",
                    "path": shared,
                    "selectionEligible": True,
                    "source": "Design/runtime.md",
                },
                {
                    "averageMilliseconds": 1.0,
                    "attribution": "fixture",
                    "currency": "focused",
                    "metric": "1 ms",
                    "path": "Assets/Game/Scripts/Systems/BuildingBetaSystem.cs",
                    "selectionEligible": True,
                    "source": "Design/runtime.md",
                },
            ],
            responsibility_audits=[
                {
                    "initialAllowedPaths": [shared],
                    "modificationScope": "alpha",
                    "path": shared,
                    "responsibilities": ["alpha"],
                },
                {
                    "initialAllowedPaths": [shared],
                    "modificationScope": "beta",
                    "path": "Assets/Game/Scripts/Systems/BuildingBetaSystem.cs",
                    "responsibilities": ["beta"],
                },
            ],
            verify_git=False,
        )
        candidates = [row for row in data["rankedCandidates"] if row["path"] in {
            shared, "Assets/Game/Scripts/Systems/BuildingBetaSystem.cs"
        }]
        self.assertEqual(sum(row["firstWaveSelected"] for row in candidates), 1)
        self.assertTrue(any("initial allowed path already selected" in row["selectionReason"] for row in candidates))

    def test_empty_allowed_path_audit_fails_closed(self) -> None:
        self.seed_sources()
        with self.assertRaisesRegex(ValueError, "initialAllowedPaths"):
            ranking.build_ranking(
                self.root,
                self.revision,
                self.tree,
                changes={},
                measured_runtime=[],
                responsibility_audits=[{
                    "initialAllowedPaths": [],
                    "modificationScope": "empty",
                    "path": "Assets/Game/Scripts/Systems/AlphaSystem.cs",
                    "responsibilities": ["invalid"],
                }],
                verify_git=False,
            )

    def test_unmeasured_owner_remains_null_and_is_rejected(self) -> None:
        self.seed_sources()
        data = self.build()
        beta = next(row for row in data["screenedOwners"] if row["path"].endswith("BuildingBetaSystem.cs"))
        self.assertIsNone(beta["scores"]["responsibilityCount"])
        self.assertIsNone(beta["scores"]["measuredRuntimeCost"])
        self.assertIsNone(beta["compositeScore"])
        self.assertIn("unmeasured dimensions remain null", beta["selectionReason"])

    def test_ambiguous_simple_type_names_do_not_create_false_edges(self) -> None:
        self.seed_sources()
        self.write(
            "Assets/Game/Scripts/Other/SecondAlpha.cs",
            "public sealed class AlphaSystem {}\n",
        )
        data = self.build()
        alpha = next(
            row for row in data["screenedOwners"]
            if row["path"] == "Assets/Game/Scripts/Systems/AlphaSystem.cs"
        )
        self.assertEqual(alpha["dependencyFanIn"], 0)
        self.assertGreater(alpha["ambiguousSimpleTypeReferencesExcluded"], 0)

    def test_unrelated_method_name_does_not_create_type_dependency(self) -> None:
        self.write("Assets/Game/Scripts/Types/Target.cs", "public sealed class Target {}\n")
        self.write(
            "Assets/Game/Scripts/Systems/MethodNameOnly.cs",
            "public sealed class MethodNameOnly { public void Target() {} }\n",
        )
        data = ranking.build_ranking(
            self.root,
            self.revision,
            self.tree,
            changes={},
            measured_runtime=[],
            responsibility_audits=[],
            verify_git=False,
        )
        method_owner = next(
            row for row in data["screenedOwners"] if row["path"].endswith("MethodNameOnly.cs")
        )
        self.assertEqual(method_owner["dependencyFanOut"], 0)

    def test_recursive_owner_globs_match_zero_or_multiple_segments(self) -> None:
        self.assertTrue(ranking.recursive_glob_match(
            "Assets/Game/Scripts/FirstLaunchRoot.cs",
            "Assets/Game/Scripts/**/FirstLaunch*.cs",
        ))
        self.assertTrue(ranking.recursive_glob_match(
            "Assets/Game/Scripts/Narrative/Flow/FirstLaunchRoot.cs",
            "Assets/Game/Scripts/**/FirstLaunch*.cs",
        ))
        self.assertTrue(ranking.recursive_glob_match(
            "Assets/Game/Scripts/UI/RootAudio.cs",
            "Assets/Game/Scripts/UI/**/*Audio*.cs",
        ))
        self.assertFalse(ranking.recursive_glob_match(
            "Assets/Game/Scripts/UI/RootVisual.cs",
            "Assets/Game/Scripts/UI/**/*Audio*.cs",
        ))

    def test_outputs_regenerate_byte_identically_with_injected_evidence(self) -> None:
        self.seed_sources()
        first = self.build()
        second = self.build()
        self.assertEqual(
            json.dumps(first, indent=2, sort_keys=True),
            json.dumps(second, indent=2, sort_keys=True),
        )
        self.assertEqual(ranking.render_markdown(first), ranking.render_markdown(second))

    def test_invalid_identity_and_missing_authority_fail_closed(self) -> None:
        self.seed_sources()
        with self.assertRaisesRegex(ValueError, "exact 40-character"):
            ranking.build_ranking(
                self.root, "short", self.tree, changes={}, measured_runtime=[], verify_git=False
            )
        (self.root / ranking.ASSEMBLY_PATH).unlink()
        with self.assertRaisesRegex(ValueError, "required authority is missing"):
            ranking.build_ranking(
                self.root, self.revision, self.tree, changes={}, measured_runtime=[], verify_git=False
            )

    def test_git_identity_rejects_mixed_tree_and_governed_source_drift(self) -> None:
        self.seed_sources()
        subprocess.run(["git", "init", "-q"], cwd=self.root, check=True)
        subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=self.root, check=True)
        subprocess.run(["git", "config", "user.name", "Architecture Test"], cwd=self.root, check=True)
        subprocess.run(["git", "add", "."], cwd=self.root, check=True)
        subprocess.run(["git", "commit", "-q", "-m", "fixture"], cwd=self.root, check=True)
        revision = subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=self.root, check=True, capture_output=True, text=True
        ).stdout.strip()
        tree = subprocess.run(
            ["git", "rev-parse", "HEAD^{tree}"], cwd=self.root, check=True, capture_output=True, text=True
        ).stdout.strip()
        ranking.build_ranking(
            self.root,
            revision,
            tree,
            changes={},
            measured_runtime=[],
            responsibility_audits=[],
        )
        with self.assertRaisesRegex(ValueError, "does not belong"):
            ranking.build_ranking(
                self.root,
                revision,
                "c" * 40,
                changes={},
                measured_runtime=[],
                responsibility_audits=[],
            )
        untracked = self.write("Assets/Game/Scripts/Untracked.cs", "public sealed class Untracked {}\n")
        with self.assertRaisesRegex(ValueError, "untracked worktree changes"):
            ranking.build_ranking(
                self.root,
                revision,
                tree,
                changes={},
                measured_runtime=[],
                responsibility_audits=[],
            )
        untracked.unlink()
        self.write("Assets/Game/Scripts/Systems/AlphaSystem.cs", "public sealed class Drift {}\n")
        with self.assertRaisesRegex(ValueError, "differ from the baseline"):
            ranking.build_ranking(
                self.root,
                revision,
                tree,
                changes={},
                measured_runtime=[],
                responsibility_audits=[],
            )


if __name__ == "__main__":
    unittest.main()
