import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("rebase_current_rows.py")
spec = importlib.util.spec_from_file_location("rebase_current_rows", SCRIPT)
rebase = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rebase)
generator_spec = importlib.util.spec_from_file_location("source_map_host_generator", SCRIPT.with_name("generate.py"))
source_map_host_generator = importlib.util.module_from_spec(generator_spec)
generator_spec.loader.exec_module(source_map_host_generator)


class RebaseCurrentOwnerRowsTests(unittest.TestCase):
    def test_proof_seed_requires_current_generator_and_exact_outputs(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            generator = root / "generate.py"
            generator.write_text("current generator")
            seed = root / "seed"
            generated = seed / "generated"
            generated.mkdir(parents=True)
            outputs = {}
            for name in ("AutonomousWorld.cs", "StartingWorld.cs", "SourceSimulationProgram.cs", "SourceDerivedClearance.cs"):
                path = generated / name
                path.write_text(name)
                outputs[str(path)] = __import__("hashlib").sha256(path.read_bytes()).hexdigest()
            inputs = {str(generator): __import__("hashlib").sha256(generator.read_bytes()).hexdigest()}
            receipt = {"status": "GeneratedSourceOnly", "mission_ready": False, "inputs": inputs, "outputs": outputs}
            (seed / "generation-receipt.json").write_text(json.dumps(receipt))
            old_root, old_generator, old_seed = (source_map_host_generator.ROOT,
                                                 source_map_host_generator.GENERATOR,
                                                 source_map_host_generator.PROOF_SEED)
            try:
                source_map_host_generator.ROOT = root
                source_map_host_generator.GENERATOR = generator
                source_map_host_generator.configure_proof_seed(seed)
                self.assertEqual(source_map_host_generator.PROOF_SEED, seed)
                receipt["inputs"][str(generator)] = "0" * 64
                (seed / "generation-receipt.json").write_text(json.dumps(receipt))
                with self.assertRaisesRegex(ValueError, "current source-map generator"):
                    source_map_host_generator.configure_proof_seed(seed)
            finally:
                source_map_host_generator.ROOT = old_root
                source_map_host_generator.GENERATOR = old_generator
                source_map_host_generator.PROOF_SEED = old_seed

    def test_starting_world_generator_binds_exact_seed_and_rejects_external_seed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            seed = root / "T Logs" / "source-map-seed-r1"
            seed.mkdir(parents=True)
            old_root = source_map_host_generator.ROOT
            try:
                source_map_host_generator.ROOT = root
                wrapper = source_map_host_generator.starting_world_seed_wrapper(seed)
                self.assertIn("T Logs/source-map-seed-r1", wrapper)
                self.assertIn("seed=root/", wrapper)
                self.assertIn("module.configure_proof_seed(seed)", wrapper)
                self.assertNotIn("seed_path", wrapper)
                with self.assertRaisesRegex(ValueError, "Repository-owned source-map proof seed"):
                    source_map_host_generator.starting_world_seed_wrapper(Path(temporary).parent)
                alias = source_map_host_generator.seed_output_wrapper(seed, "AutonomousWorld.cs")
                self.assertIn("T Logs/source-map-seed-r1", alias)
                self.assertIn("AutonomousWorld.cs", alias)
                with self.assertRaisesRegex(ValueError, "exact output required"):
                    source_map_host_generator.seed_output_wrapper(seed, "Unknown.cs")
            finally:
                source_map_host_generator.ROOT = old_root

    def test_appends_exact_missing_row_and_preserves_derived_override(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            owner = root / "Assets" / "Owner.cs"
            owner.parent.mkdir(parents=True)
            owner.write_text("class CurrentOwner {}\n")
            canonical_row = {"id": "CurrentOwner.cs", "kind": "finite_adapter", "path": "Assets/Owner.cs",
                             "role": "data_decoder", "contains_original_gameplay": False}
            derived_override = {"id": "DerivedHost.cs", "kind": "finite_adapter", "path": "T Logs/host.cs",
                                "role": "host_glue", "contains_original_gameplay": False}
            shared_override = {"id": "CurrentOverride.cs", "kind": "finite_adapter", "path": "T Logs/CurrentOverride.cs",
                               "role": "host_glue", "contains_original_gameplay": False}
            canonical = {"runtime_backend": "TypedGroundS026V1", "profile": {"lane": "S026"},
                         "sources": [canonical_row, {**shared_override, "path": "Assets/CurrentOverride.cs"}]}
            derived = {"runtime_backend": "TypedGroundS026V1", "profile": {"lane": "S026"},
                       "sources": [derived_override, shared_override]}
            with patch.object(rebase, "ROOT", root):
                merged, evidence = rebase.merge_missing_rows(canonical, derived)
            self.assertEqual(merged["sources"], [derived_override, shared_override, canonical_row])
            self.assertEqual(evidence["appended_row_ids"], ["CurrentOwner.cs"])
            self.assertEqual(evidence["source_pins"][str(owner)], rebase.pin(owner)["sha256"])

    def test_rejects_missing_owner_path_conflicting_with_derived_row(self):
        canonical = {"runtime_backend": "TypedGroundS026V1", "profile": {}, "sources": [
            {"id": "CanonicalOwner", "kind": "finite_adapter", "path": "Assets/Owner.cs",
             "role": "data_decoder", "contains_original_gameplay": False}]}
        derived = {"runtime_backend": "TypedGroundS026V1", "profile": {}, "sources": [
            {"id": "DifferentDerivedOwner", "kind": "finite_adapter", "path": "Assets/Owner.cs",
             "role": "data_decoder", "contains_original_gameplay": False}]}
        with self.assertRaisesRegex(ValueError, "Canonical owner path conflicts"):
            rebase.merge_missing_rows(canonical, derived)

    def test_rejects_conflicting_profiles(self):
        with self.assertRaisesRegex(ValueError, "profiles differ"):
            rebase.merge_missing_rows({"runtime_backend": "A", "profile": {}, "sources": []},
                                      {"runtime_backend": "B", "profile": {}, "sources": []})


if __name__ == "__main__":
    unittest.main()
